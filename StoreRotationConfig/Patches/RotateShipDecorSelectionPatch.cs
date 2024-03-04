using System;
using System.Collections.Generic;
using HarmonyLib;

// Patches for 'Terminal.RotateShipDecorSelection()'.
namespace StoreRotationConfig.Patches
{
    [HarmonyPatch(typeof(Terminal), methodName: nameof(Terminal.RotateShipDecorSelection))]
    public static class RotateShipDecorSelectionPatch
    {
        // Cached list of every purchasable, non-persistent item available in the store.
        private static readonly List<UnlockableItem> allItems = new(StartOfRound.Instance.unlockablesList.unlockables.Count);

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
            int maxItems = Config.Instance.MAX_ITEMS,
                minItems = Config.Instance.MIN_ITEMS;
            bool stockAll = Config.Instance.STOCK_ALL,
                sortItems = Config.Instance.SORT_ITEMS;
            // ...

            // Check if store rotation list is empty (first load).
            if (__instance.ShipDecorSelection.Count == 0)
            {
                // Clear any items from previous lobbies/sessions (just in case).
                allItems.Clear();

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

            // Return false if 'stockAll' setting is enabled, since store rotation list is already filled at this point.
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
            for (int i = 0; i < num; i++)
            {
                // Exit loop if all items have been added to the store rotation.
                if (allItemsClone.Count < 1)
                {
                    break;
                }

                // Add random item to 'storeRotation' list, and remove it from 'allItems' cloned list.
                int index = random.Next(0, allItemsClone.Count);
                storeRotation.Add(allItemsClone[index]);
                allItemsClone.RemoveAt(index);
            }

            // Sort store selection alphabetically if 'sortItems' is enabled.
            if (sortItems && storeRotation.Count > 1)
            {
                storeRotation.Sort((x, y) => string.Compare(x.unlockableName, y.unlockableName));
            }

            // Fill store rotation with every item in 'storeRotation' list.
            storeRotation.ForEach(item => __instance.ShipDecorSelection.Add(item.shopSelectionNode));

            // Return false to avoid executing vanilla method.
            return false;
        }
    }
}