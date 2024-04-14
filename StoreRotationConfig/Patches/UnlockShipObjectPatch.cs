using HarmonyLib;
using UnityEngine;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'StartOfRound.UnlockShipObject()' method; removes items from store rotation if 'stockPurchased' setting is disabled.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), "UnlockShipObject", [typeof(int)])]
    internal class UnlockShipObjectPatch
    {
        // Cached terminal instance.
        private static Terminal terminal;

        private static void Prefix(StartOfRound __instance, int unlockableID)
        {
            // Check if the 'stockPurchased' setting is enabled.
            if (Plugin.Settings.STOCK_PURCHASED)
            {
                return;
            }

            // Obtain item from list of purchasable items.
            UnlockableItem item = __instance.unlockablesList.unlockables[unlockableID];

            // Check if item has already been purchased.
            if (!item.hasBeenUnlockedByPlayer && !item.alreadyUnlocked)
            {
                // Remove item from 'RotateShipDecorSelectionPatch.AllItems' list.
                if (RotateShipDecorSelectionPatch.AllItems.Remove(item))
                {
                    // Ensure cached terminal instance exists.
                    if (terminal == null)
                    {
                        terminal = Object.FindObjectOfType<Terminal>();
                    }

                    // Remove item from 'Terminal.ShipDecorSelection' list.
                    if (!terminal.ShipDecorSelection.Remove(item.shopSelectionNode))
                    {
                        Plugin.StaticLogger.LogWarning($"Unlockable #{unlockableID} was not found in the store rotation.");
                    }
                }
                else
                {
                    Plugin.StaticLogger.LogWarning($"Unlockable #{unlockableID} was not found in the list of purchasable items.");
                }
            }
        }
    }
}