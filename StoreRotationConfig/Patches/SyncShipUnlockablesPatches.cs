using GameNetcodeStuff;
using HarmonyLib;
using StoreRotationConfig.Compatibility;
using Unity.Netcode;

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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        private static void SyncShipUnlockablesClientPost()
        {
            // Return if local game instance is hosting the server.
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // Return if unlockables are already synced with the host; toggle unlockable sync status if not.
            if (UnlockablesSynced)
            {
                Plugin.StaticLogger?.LogDebug("Purchased unlockable items already synced with the host.");

                return;
            }
            UnlockablesSynced = true;

            // Manually trigger a store rotation.
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