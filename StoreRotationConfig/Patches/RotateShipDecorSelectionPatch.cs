using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'Terminal.RotateShipDecorSelection()' method; overrides vanilla method, but should functionally be the same.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), methodName: nameof(Terminal.RotateShipDecorSelection))]
    internal class RotateShipDecorSelectionPatch
    {
        // Cached list of every purchasable, non-persistent item available in the store.
        public static List<UnlockableItem> AllItems { get; private set; }

        // Cached list of items to always add to the rotating store.
        public static List<UnlockableItem> PermanentItems { get; private set; }

        /// <summary>
        ///     Fills 'Terminal.ShipDecorSelection' list with items, reading from the configuration file.
        /// </summary>
        /// <param name="shipDecorSelection">List containing items currently in the store rotation.</param>
        /// <param name="random">Seeded 'Random' instance used for generating a new store rotation.</param>
        private static void RotateShipDecorSelection(List<TerminalNode> shipDecorSelection, Random random)
        {
            // Return if client has not yet fully synced with the host.
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer && !SyncShipUnlockablesPatch.UnlockablesSynced)
            // && !Plugin.Settings.ConfigSynced)
            {
                Plugin.StaticLogger.LogInfo("Waiting for sync from server before rotating store...");

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
                // Initialize 'AllItems' list with its capacity set to the total number of unlockable items.
                AllItems = new(StartOfRound.Instance.unlockablesList.unlockables.Count);

                // Fill 'AllItems' list with every purchasable, non-persistent item.
                StartOfRound.Instance.unlockablesList.unlockables.DoIf(
                    condition: item => item.shopSelectionNode != null && !item.alwaysInStock
                        && (Plugin.Settings.STOCK_PURCHASED || (!item.hasBeenUnlockedByPlayer && !item.alreadyUnlocked)),
                    action: AllItems.Add);

                // Check if there is a whitelist specified in the config file.
                if (!Plugin.Settings.STOCK_ALL && Plugin.Settings.ITEM_WHITELIST != "")
                {
                    // Obtain names specified in the config file and trim them.
                    List<string> whitelist = Plugin.Settings.ITEM_WHITELIST.Value.Split(',').Select(name => name.Trim()).ToList();

                    // Initialize 'PermanentItems' with the capacity set to the number of names in the whitelist.
                    PermanentItems = new(whitelist.Count);

                    // Attempt to add items to the 'PermanentItems' list, if they match a whitelisted name.
                    AllItems.DoIf(item => whitelist.Contains(item.shopSelectionNode?.creatureName), PermanentItems.Add);

                    Plugin.StaticLogger.LogInfo($"{PermanentItems.Count} items permanently added to the rotating store!");
                }

                // Check if there is a blacklist specified in the config file.
                if (Plugin.Settings.ITEM_BLACKLIST != "")
                {
                    // Obtain names specified in the config file and trim them.
                    List<string> blacklist = Plugin.Settings.ITEM_BLACKLIST.Value.Split(',').Select(name => name.Trim()).ToList();

                    // Attempt to remove items from the 'AllItems' list, if they match a blacklisted name.
                    int itemsBlacklisted = AllItems.RemoveAll(item => blacklist.Contains(item.shopSelectionNode?.creatureName));

                    Plugin.StaticLogger.LogInfo($"{itemsBlacklisted} items removed from the rotating store.");
                }

                // Check if 'stockAll' setting is enabled.
                if (stockAll)
                {
                    // Check if 'sortItems' setting is enabled.
                    if (sortItems)
                    {
                        // Sort 'AllItems' list alphabetically.
                        AllItems.Sort((x, y) => string.Compare(x.shopSelectionNode?.creatureName, y.shopSelectionNode?.creatureName));
                    }

                    // Fill store rotation with every item in the 'AllItems' list.
                    AllItems.ForEach(item => shipDecorSelection.Add(item.shopSelectionNode));
                }
            }

            // Return if 'stockAll' setting is enabled, since the store rotation list has already been filled at this point.
            if (stockAll)
            {
                return;
            }

            Plugin.StaticLogger.LogInfo("Rotating store...");

            // Clear previous store rotation.
            shipDecorSelection.Clear();

            // Use 'minItems' for 'maxItems', if the former is greater than the latter.
            if (minItems > maxItems)
            {
                Plugin.StaticLogger.LogWarning("Value for 'minItems' is larger than 'maxItems', using it instead...");

                maxItems = minItems;
            }

            // Obtain a random number of items using the map seed, or use a fixed number if 'minItems' and 'maxItems' are equal.
            int numItems = (minItems != maxItems) ? random.Next(minItems, maxItems + 1) : maxItems;

            // Create 'storeRotation' list (for sorting), and clone the 'AllItems' list (for item selection).
            List<UnlockableItem> storeRotation = new(numItems), allItems = new(AllItems);

            // Check if there is a whitelist specified in the config file.
            if (Plugin.Settings.ITEM_WHITELIST != "" && PermanentItems?.Count > 0)
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
                storeRotation.Sort((x, y) => string.Compare(x.shopSelectionNode?.creatureName, y.shopSelectionNode?.creatureName));
            }

            // Fill store rotation with every item in the 'storeRotation' list.
            storeRotation.ForEach(item => shipDecorSelection.Add(item.shopSelectionNode));

            Plugin.StaticLogger.LogInfo("Store rotated!");
        }

        /// <summary>
        ///     Inserts a call to 'RotateShipDecorSelectionPatch.RotateShipDecorSelection()', followed by a return instruction.
        /// </summary>
        ///     ... (Terminal:1564)
        ///     Random random = new Random(StartOfRound.Instance.randomMapSeed + 65);
        ///     
        ///     -> StoreRotationConfig.Patches.RotateShipDecorSelectionPatch.RotateShipDecorSelection(this.ShipDecorSelection, random);
        ///     -> return;
        ///     
        ///     this.ShipDecorSelection.Clear();
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.ShipDecorSelection))),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(List<TerminalNode>), nameof(List<TerminalNode>.Clear))))
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.ShipDecorSelection))),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, AccessTools.Method(typeof(RotateShipDecorSelectionPatch), nameof(RotateShipDecorSelection))),
                new(OpCodes.Ret)
            ).InstructionEnumeration();
        }
    }
}