using GameNetcodeStuff;
using HarmonyLib;
using StoreRotationConfig.Compatibility;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for syncing already-purchased unlockable items with the client.
    /// </summary>
    [HarmonyPatch]
    internal class SyncShipUnlockablesPatch
    {
        /// <summary>
        ///     Whether already-purchased unlockables have been successfully synced or not; reset upon returning to main menu.
        /// </summary>
        public static bool UnlockablesSynced { get; private set; } = false;

        /// <summary>
        ///     Fills 'storedItems' parameter immediately before 'SyncShipUnlockablesClientRpc()' is called on the server.  
        /// </summary>
        ///     ... (StartOfRound:1663)
        ///     if(this.attachedVehicle != null)
        ///     {
        ///         vehicleID = this.attachedVehicle.vehicleID;
        ///     }
        ///     this.SyncShipUnlockablesClientRpc(playerSuitIDs, this.shipRoomLights.areLightsOn, placeableObjectPositions.ToArray(),
        ///         placeableObjectRotations.ToArray(), placeableObjects.ToArray(),
        ///         // storedItems.ToArray(),
        ///         -> Delegate(),
        ///         scrapValues.ToArray(), itemSaveData.ToArray(), vehicleID);
        ///     )
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesServerRpc))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SyncShipUnlockablesServerRpcTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).End().MatchBack(true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldarg_0))
            .Advance(10)
            .SetInstructionAndAdvance(Transpilers.EmitDelegate((List<int> storedItems) =>
                {
                    // Probably not needed; list is always empty (hence the need for this).
                    storedItems.Clear();

                    // Actually add the IDs of every item currently in storage to the 'storedItems' list.
                    for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++)
                    {
                        if (StartOfRound.Instance.unlockablesList.unlockables[i].inStorage)
                        {
                            storedItems.Add(i);
                        }
                    }

                    // Return as array for the 'SyncShipUnlockablesClientRpc()' call.
                    return storedItems.ToArray();
                }
            )).InstructionEnumeration();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesServerRpc))]
        [HarmonyPostfix]
        private static void SyncShipUnlockablesServerRpcPost(StartOfRound __instance)
        {
            // Return if local client is not hosting the server.
            if (!__instance.IsHost)
            {
                return;
            }

            // Manually trigger a store rotation on the server.
            Plugin.Terminal?.RotateShipDecorSelection();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPostfix]
        private static void SyncShipUnlockablesClientRpcPost(StartOfRound __instance, UnlockablesList ___unlockablesList, int[] placeableObjects, int[] storedItems)
        {
            // Return if local client is hosting the server.
            if (__instance.IsHost)
            {
                return;
            }

            // Check if unlockables need to be synced.
            if (!UnlockablesSynced)
            {
                // Sync purchased suits.
                Resources.FindObjectsOfTypeAll<UnlockableSuit>().Select(suit => ___unlockablesList.unlockables[suit.suitID]).Do(item =>
                {
                    if (item != null)
                    {
                        item.hasBeenUnlockedByPlayer = !item.alreadyUnlocked;
                    }
                });

                // Sync purchased ship objects.
                placeableObjects.Select(furnitureID => ___unlockablesList.unlockables[furnitureID]).Do(item =>
                {
                    if (item != null)
                    {
                        item.hasBeenUnlockedByPlayer = !item.alreadyUnlocked;
                    }
                });

                // Sync ship objects in storage.
                storedItems.Select(storedID => ___unlockablesList.unlockables[storedID]).Do(item =>
                {
                    if (item != null)
                    {
                        item.hasBeenUnlockedByPlayer = !item.alreadyUnlocked;
                        item.inStorage = true;
                    }
                });

                // Sync cozy lights (neither suit nor placeable object).
                if (Object.FindObjectOfType<CozyLights>() != null)
                {
                    UnlockableItem? item = ___unlockablesList.unlockables.Find(item => string.CompareOrdinal(item.unlockableName, "Cozy lights") == 0);
                    if (item != null)
                    {
                        item.hasBeenUnlockedByPlayer = true;
                    }
                }

                // Unlockables should be properly synced at this point.
                UnlockablesSynced = true;
            }

            // Manually trigger a store rotation on the local client.
            Plugin.Terminal?.RotateShipDecorSelection();
        }

        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPrefix]
        private static void MenuManagerStartPre()
        {
            // Reset local client sync status upon entering (or returning to) main menu.
            // Plugin.Settings.ConfigSynced = false;
            UnlockablesSynced = false;
            // ...

            // Handle 'TerminalFormatter' compatibility here since I can't seem to get it to load before my mod, despite the soft dependency.
            if (Plugin.Settings != null && Plugin.Settings.TERMINAL_FORMATTER_COMPAT.Value && TerminalFormatterCompatibility.Enabled
                && !TerminalFormatterCompatibility.Patched)
            {
                Plugin.StaticLogger?.LogInfo($"Patching 'TerminalFormatter'...");

                // Patch 'TerminalFormatter.Nodes.Store.GetNodeText' to display discounts assigned to the rotating store.
                Plugin.Harmony?.PatchAll(typeof(TerminalFormatterCompatibility));

                // Unpatch 'relativeScroll' tweak (already present in 'TerminalFormatter').
                Plugin.Harmony?.Unpatch(AccessTools.Method(typeof(PlayerControllerB), "ScrollMouse_performed"),
                    HarmonyPatchType.Transpiler, Plugin.GUID);

                // Toggle patched status to avoid running more than once (every menu reload).
                TerminalFormatterCompatibility.Patched = true;

                Plugin.StaticLogger?.LogInfo($"'TerminalFormatter' patched!");
            }
        }
    }
}