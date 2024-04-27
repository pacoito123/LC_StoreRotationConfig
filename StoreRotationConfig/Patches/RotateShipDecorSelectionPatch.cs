using HarmonyLib;
using System;
using System.Collections.Generic;
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

        [HarmonyPriority(Priority.VeryHigh)]
        private static bool Prefix(Terminal __instance)
        {
            // Return if client has not yet fully synced with the host.
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer && !SyncShipUnlockablesPatch.UnlockablesSynced)
            // && !Plugin.Settings.ConfigSynced)
            {
                Plugin.StaticLogger.LogInfo("Waiting for sync from server before rotating store...");

                // Return false to stop vanilla method from executing.
                return false;
            }

            // Obtain values from config file.
            int maxItems = Plugin.Settings.MAX_ITEMS.Value,
                minItems = Plugin.Settings.MIN_ITEMS.Value;
            bool stockAll = Plugin.Settings.STOCK_ALL.Value,
                sortItems = Plugin.Settings.SORT_ITEMS.Value;
            // ...

            // Check if 'Terminal.ShipDecorSelection' list is empty (first load).
            if (__instance.ShipDecorSelection.Count == 0)
            {
                // Initialize 'AllItems' list with specified capacity.
                AllItems = new(StartOfRound.Instance.unlockablesList.unlockables.Count);

                // Fill 'AllItems' list with every purchasable, non-persistent item.
                StartOfRound.Instance.unlockablesList.unlockables.DoIf(
                    condition: item => item.shopSelectionNode != null && !item.alwaysInStock
                        && (Plugin.Settings.STOCK_PURCHASED || (!item.hasBeenUnlockedByPlayer && !item.alreadyUnlocked)),
                    action: AllItems.Add);

                // Check if 'stockAll' setting is enabled.
                if (stockAll)
                {
                    // Check if 'sortItems' setting is enabled.
                    if (sortItems)
                    {
                        // Sort 'AllItems' list alphabetically.
                        AllItems.Sort((x, y) => string.Compare(x.unlockableName, y.unlockableName));
                    }

                    // Fill store rotation with every item in 'AllItems'.
                    AllItems.ForEach(item => __instance.ShipDecorSelection.Add(item.shopSelectionNode));
                }
            }

            // Return false if 'stockAll' setting is enabled, since the store rotation list has already been filled at this point.
            if (stockAll)
            {
                return false;
            }

            Plugin.StaticLogger.LogInfo("Rotating store...");

            // Clear previous store rotation list.
            __instance.ShipDecorSelection.Clear();

            // Use 'maxItems' for 'minItems' if the latter is greater than the former.
            minItems = (minItems < maxItems) ? minItems : maxItems;

            // Obtain a random number of items using the map seed, or use a fixed number if 'minItems' and 'maxItems' are equal.
            Random random = new(StartOfRound.Instance.randomMapSeed + 65);
            int num = (minItems != maxItems) ? random.Next(minItems, maxItems + 1) : maxItems;

            // Create 'storeRotation' list (for sorting), and clone 'AllItems' list (for item selection).
            List<UnlockableItem> storeRotation = new(num), allItems = new(AllItems);

            // Iterate for every item to add to the store rotation (and exit early if there are no more items in 'allItems' cloned list).
            for (int i = 0; i < num && allItems.Count != 0; i++)
            {
                // Add random item to 'storeRotation' list, and remove it from 'allItems' cloned list.
                int index = random.Next(0, allItems.Count);
                storeRotation.Add(allItems[index]);
                allItems.RemoveAt(index);
            }

            // Sort 'storeRotation' list alphabetically if 'sortItems' is enabled.
            if (sortItems && storeRotation.Count > 1)
            {
                storeRotation.Sort((x, y) => string.Compare(x.unlockableName, y.unlockableName));
            }

            // Fill store rotation with every item in 'storeRotation' list.
            storeRotation.ForEach(item => __instance.ShipDecorSelection.Add(item.shopSelectionNode));

            Plugin.StaticLogger.LogInfo("Store rotated!");

            // Return false to stop vanilla method from executing.
            return false;
        }
    }
}