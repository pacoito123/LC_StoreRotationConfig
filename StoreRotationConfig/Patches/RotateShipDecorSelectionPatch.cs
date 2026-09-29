using HarmonyLib;
using StoreRotationConfig.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;

using static StoreRotationConfig.Api.RotationItemsAPI;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for <c>Terminal.RotateShipDecorSelection()</c> method; overrides vanilla method, but should functionally be the same.
    /// </summary>
    internal static class RotateShipDecorSelectionPatch
    {
        /// <summary>
        ///     Fills <c>Terminal.ShipDecorSelection</c> list with items, reading from the configuration file.
        /// </summary>
        /// <param name="shipDecorSelection">List containing items currently in the store rotation.</param>
        /// <param name="random">Seeded <c>Random</c> instance used for generating a new store rotation.</param>
        private static void RotateShipDecorSelection(List<TerminalNode> shipDecorSelection, Random random)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                return;
            }

            // Return if config file instance is null, just in case.
            if (Plugin.Settings == null)
            {
                Plugin.Logger.LogError("Configuration could not be loaded or is missing; rotating store won't work.");

                return;
            }

            // Obtain values from the config file.
            int maxItems = Math.Abs(Plugin.Settings.MAX_ITEMS.Value),
                minItems = Math.Abs(Plugin.Settings.MIN_ITEMS.Value);
            bool stockAll = Plugin.Settings.STOCK_ALL.Value,
                sortItems = Plugin.Settings.SORT_ITEMS.Value;
            // ...

            // Check if 'Terminal.ShipDecorSelection' list is empty (first load).
            if (shipDecorSelection.Count == 0)
            {
                // Fill 'AllItems' list with every purchasable, non-persistent item.
                StartOfRound.Instance.unlockablesList.unlockables.DoIf(
                    condition: item => item.shopSelectionNode != null && !item.alwaysInStock
                        && (!Plugin.Settings.REMOVE_PURCHASED.Value || !item.hasBeenUnlockedByPlayer),
                    action: RegisterItem);

                // Check if there is a whitelist specified in the config file, AND the 'stockAll' setting is not enabled.
                if (Plugin.Settings.ITEM_WHITELIST.Value.Length > 0 && !Plugin.Settings.STOCK_ALL.Value)
                {
                    // Obtain names specified in the config file and trim them.
                    List<string> whitelist = [.. Plugin.Settings.ITEM_WHITELIST.Value.Split(',').Select(name => name.Trim())];

                    // Attempt to add items to the 'PermanentItems' list, if they match a whitelisted name.
                    AllItems.DoIf(
                        condition: item => item.shopSelectionNode != null && whitelist.Contains(item.shopSelectionNode.creatureName),
                        action: AddPermanentItem);

                    Plugin.Logger.LogInfo($"{PermanentItems.Count} items permanently added to the rotating store!");
                }

                // Check if there is a blacklist specified in the config file.
                if (Plugin.Settings.ITEM_BLACKLIST.Value.Length > 0)
                {
                    // Obtain names specified in the config file and trim them.
                    List<string> blacklist = [.. Plugin.Settings.ITEM_BLACKLIST.Value.Split(',').Select(name => name.Trim())];

                    // Attempt to remove items from the 'AllItems' list, if they match a blacklisted name.
                    int itemsBlacklisted = AllItems.RemoveAll(item => blacklist.Contains(item.shopSelectionNode.creatureName));

                    Plugin.Logger.LogInfo($"{itemsBlacklisted} items removed from the rotating store.");
                }

                // Check if 'stockAll' setting is enabled.
                if (stockAll)
                {
                    // Check if 'sortItems' setting is enabled.
                    if (sortItems)
                    {
                        // Sort 'AllItems' list alphabetically.
                        AllItems.Sort((x, y) => string.Compare(x.shopSelectionNode.creatureName, y.shopSelectionNode.creatureName, StringComparison.Ordinal));
                    }

                    // Fill store rotation with every item in the 'AllItems' list.
                    if (StoreRotationNetworker.Instance != null)
                    {
                        StoreRotationNetworker.Instance.StoreRotation.Clear();

                        foreach (UnlockableItem item in AllItems)
                        {
                            StoreRotationNetworker.Instance.StoreRotation.Add(item);
                        }
                    }
                    // ...

                    Plugin.Logger.LogInfo($"All {AllItems.Count} items added to the store rotation!");
                }
            }

            // Return if 'stockAll' setting is enabled, since the store rotation list has already been filled at this point.
            if (stockAll)
            {
                return;
            }

            Plugin.Logger.LogInfo("Rotating store...");

            // Clear previous store rotation.
            shipDecorSelection.Clear();

            // Use 'minItems' for 'maxItems', if the former is greater than the latter.
            if (minItems > maxItems)
            {
                Plugin.Logger.LogWarning("Value for 'minItems' is larger than 'maxItems', using it instead...");

                maxItems = minItems;
            }

            // Obtain a random number of items using the map seed, or use a fixed number if 'minItems' and 'maxItems' are equal.
            int numItems = (minItems != maxItems) ? random.Next(minItems, maxItems + 1) : maxItems;

            // Create 'storeRotation' list (for sorting), and clone the 'AllItems' list (for item selection).
            List<UnlockableItem> storeRotation = [with(numItems)], allItems = [.. AllItems];

            // Check if there are permanent items to add.
            if (PermanentItems.Count > 0)
            {
                // Remove whitelisted items from the 'allItems' cloned list and add them directly to the 'storeRotation' list.
                PermanentItems.Do(item =>
                {
                    _ = allItems.Remove(item);
                    storeRotation.Add(item);
                });
            }

            // Iterate for every item to add to the store rotation, exiting early if there are no more items in the 'allItems' cloned list.
            for (int i = 0; i < numItems && allItems.Count != 0; i++)
            {
                // Obtain a random item from the 'allItems' cloned list.
                int index = random.Next(0, allItems.Count);

                // Add random item to the 'storeRotation' list, and remove it from the 'allItems' cloned list.
                storeRotation.Add(allItems[index]);
                allItems.RemoveAt(index);
            }

            // Check if 'sortItems' setting is enabled, and if there's more than one item in the 'storeRotation' list.
            if (sortItems && storeRotation.Count > 1)
            {
                // Sort 'storeRotation' list alphabetically.
                storeRotation.Sort((x, y) => string.Compare(x.shopSelectionNode.creatureName, y.shopSelectionNode.creatureName, StringComparison.Ordinal));
            }

            // Fill store rotation with every item in the 'storeRotation' list.
            if (StoreRotationNetworker.Instance != null)
            {
                StoreRotationNetworker.Instance.StoreRotation.Clear();

                foreach (UnlockableItem item in storeRotation)
                {
                    StoreRotationNetworker.Instance.StoreRotation.Add(item);
                }
            }
            // ...
        }

        /// <summary>
        ///     Inserts a call to 'RotateShipDecorSelectionPatch.RotateShipDecorSelection()', followed by a return instruction.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         Random random = new Random(StartOfRound.Instance.randomMapSeed + 65);
        /// 
        ///         -> RotateShipDecorSelection(this.ShipDecorSelection, random);
        ///         -> return;
        /// 
        ///         this.ShipDecorSelection.Clear();
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RotateShipDecorSelection_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo shipDecorSelectionInfo = typeof(Terminal).GetField(nameof(Terminal.ShipDecorSelection), BindingFlags.Instance | BindingFlags.Public);
            MethodInfo listClearInfo = typeof(List<TerminalNode>).GetMethod(nameof(List<>.Clear), BindingFlags.Instance | BindingFlags.Public);
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, shipDecorSelectionInfo),
                new(OpCodes.Callvirt, listClearInfo));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match Terminal 'ShipDecorSelection' List clearing.");

                return instructions;
            }

            MethodInfo rotateShipDecorSelectionPatchInfo = typeof(RotateShipDecorSelectionPatch).GetMethod(nameof(RotateShipDecorSelection), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, shipDecorSelectionInfo),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, rotateShipDecorSelectionPatchInfo),
                new(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }
}