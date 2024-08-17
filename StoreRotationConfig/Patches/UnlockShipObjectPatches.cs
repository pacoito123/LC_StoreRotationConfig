using HarmonyLib;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for removing purchased items from both current and future store rotations.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    internal class UnlockShipObjectPatches
    {
        [HarmonyPatch("UnlockShipObject", typeof(int))]
        [HarmonyPrefix]
        private static void UnlockShipObjectPre(int unlockableID)
        {
            // Return if the local game instance is not hosting the server, or if 'stockPurchased' setting is enabled.
            if ((!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) || (Plugin.Settings?.STOCK_PURCHASED.Value).GetValueOrDefault(true))
            {
                return;
            }

            // Attempt to remove item from the store rotation.
            Plugin.StaticLogger?.LogDebug($"Attempting to remove unlockable #{unlockableID} on server...");
            RemoveItemFromRotation(unlockableID);
        }

        [HarmonyPatch(nameof(StartOfRound.BuyShipUnlockableClientRpc), typeof(int), typeof(int))]
        [HarmonyPrefix]
        private static void BuyShipUnlockableClientPre(int newGroupCreditsAmount, int unlockableID = -1)
        {
            // Return if local game instance is hosting the server, or if the 'stockPurchased' setting is enabled.
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || (Plugin.Settings?.STOCK_PURCHASED.Value).GetValueOrDefault(true))
            {
                return;
            }

            // Attempt to remove item from the store rotation.
            Plugin.StaticLogger?.LogDebug($"Attempting to remove unlockable #{unlockableID} on client...");
            RemoveItemFromRotation(unlockableID);
        }

        /// <summary>
        ///     Helper function for removing items from both 'RotateShipDecorSelectionPatch.AllItems' and 'Terminal.ShipDecorSelection' lists.
        /// </summary>
        /// <param name="unlockableID">ID of the unlockable item to remove.</param>
        private static void RemoveItemFromRotation(int unlockableID)
        {
            // Return if no item was successfully purchased.
            if (unlockableID == -1)
            {
                return;
            }

            // Obtain item from the list of purchasable items.
            UnlockableItem item = StartOfRound.Instance.unlockablesList.unlockables[unlockableID];

            // Return if item is not present in the 'UnlockablesList.unlockables' list, OR its shop node does not exist.
            if (item == null || item.shopSelectionNode == null)
            {
                Plugin.StaticLogger?.LogWarning($"Unlockable #{unlockableID} and/or its terminal node could not be found.");

                return;
            }

            // Return if item has already been purchased (likely a redundant check, but done just in case).
            if (item.hasBeenUnlockedByPlayer || item.alreadyUnlocked)
            {
                Plugin.StaticLogger?.LogWarning($"Item '{item.shopSelectionNode.creatureName}' has already been purchased.");

                return;
            }

            Plugin.StaticLogger?.LogDebug($"Removing item '{item.shopSelectionNode.creatureName}' from the store rotation...");

            // Attempt to remove item from the 'RotateShipDecorSelectionPatch.AllItems' list.
            if ((RotateShipDecorSelectionPatch.AllItems?.Remove(item)).GetValueOrDefault())
            {
                // Attempt to remove item from the 'Terminal.ShipDecorSelection' list.
                if (Plugin.Terminal == null || !Plugin.Terminal.ShipDecorSelection.Remove(item.shopSelectionNode))
                {
                    Plugin.StaticLogger?.LogWarning($"Item '{item.shopSelectionNode.creatureName}' could not be removed from the store rotation.");
                }
            }
            else
            {
                Plugin.StaticLogger?.LogWarning($"Item '{item.shopSelectionNode.creatureName}' could not be found in the list of purchasable items.");
            }

            // Attempt to remove item from the 'RotateShipDecorSelectionPatch.PermanentItems' list.
            if ((RotateShipDecorSelectionPatch.PermanentItems?.Remove(item)).GetValueOrDefault())
            {
                Plugin.StaticLogger?.LogDebug($"Item '{item.shopSelectionNode.creatureName}' was removed from the list of permanent items.");
            }
        }
    }
}