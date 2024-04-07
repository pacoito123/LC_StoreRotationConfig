using HarmonyLib;
using System;
using System.Collections.Generic;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'Terminal.RotateShipDecorSelection()' method; replaces vanilla unless only the client has this mod installed.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), methodName: nameof(Terminal.RotateShipDecorSelection))]
    internal class RotateShipDecorSelectionPatch
    {
        // Cached list of every purchasable, non-persistent item available in the store.
        private static List<UnlockableItem> allItems;

        [HarmonyPriority(Priority.VeryHigh)]
        private static bool Prefix(Terminal __instance)
        {
            // Check if config is synced to client; execute vanilla method if not.
            if (!Config.IsHost && !Config.Synced)
            {
                Plugin.StaticLogger.LogInfo("Waiting for config sync...");

                // Return true to execute vanilla method.
                return true;
            }

            // Obtain values from config file (synced).
            int maxItems = Plugin.Settings.MAX_ITEMS,
                minItems = Plugin.Settings.MIN_ITEMS;
            bool stockAll = Plugin.Settings.STOCK_ALL,
                sortItems = Plugin.Settings.SORT_ITEMS;
            // ...

            // Check if either 'Terminal.ShipDecorSelection' or 'allItems' list is empty (first load).
            if (__instance.ShipDecorSelection.Count == 0 || allItems.Count == 0)
            {
                // Initialize 'allItems' list with specified capacity.
                allItems = new(StartOfRound.Instance.unlockablesList.unlockables.Count);

                // Fill 'allItems' list with every purchasable, non-persistent item.
                StartOfRound.Instance.unlockablesList.unlockables.DoIf(
                    condition: item => item.shopSelectionNode != null && !item.alwaysInStock,
                    action: allItems.Add);

                // Check if 'stockAll' setting is enabled.
                if (stockAll)
                {
                    // Sort 'allItems' list alphabetically if 'sortItems' setting is enabled.
                    if (sortItems)
                    {
                        allItems.Sort((x, y) => string.Compare(x.unlockableName, y.unlockableName));
                    }

                    // Fill store rotation with every item in 'allItems'.
                    allItems.ForEach(item => __instance.ShipDecorSelection.Add(item.shopSelectionNode));
                }
            }

            // Return false if 'stockAll' setting is enabled, since store rotation list has already been filled at this point.
            if (stockAll)
            {
                return false;
            }

            // Clear previous store rotation list.
            __instance.ShipDecorSelection.Clear();

            // Use 'maxItems' for 'minItems' if the latter is greater than the former.
            minItems = (minItems < maxItems) ? minItems : maxItems;

            // Obtain a random number of items using the map seed, or use a fixed number if 'minItems' and 'maxItems' are equal.
            Random random = new(StartOfRound.Instance.randomMapSeed + 65);
            int num = (minItems != maxItems) ? random.Next(minItems, maxItems + 1) : maxItems;

            // Create 'storeRotation' list (for sorting), and clone 'allItems' list (for item selection).
            List<UnlockableItem> storeRotation = new(num), allItemsClone = new(allItems);

            // Iterate for every item to add to the store rotation (and exit early if there are no more items in 'allItemsClone' list).
            for (int i = 0; i < num && allItemsClone.Count != 0; i++)
            {
                // Add random item to 'storeRotation' list, and remove it from 'allItems' cloned list.
                int index = random.Next(0, allItemsClone.Count);
                storeRotation.Add(allItemsClone[index]);
                allItemsClone.RemoveAt(index);
            }

            // Sort 'storeRotation' list alphabetically if 'sortItems' is enabled.
            if (sortItems && storeRotation.Count > 1)
            {
                storeRotation.Sort((x, y) => string.Compare(x.unlockableName, y.unlockableName));
            }

            // Fill store rotation with every item in 'storeRotation' list.
            storeRotation.ForEach(item => __instance.ShipDecorSelection.Add(item.shopSelectionNode));

            // Return false to stop vanilla method from executing.
            return false;
        }
    }
}