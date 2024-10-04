using HarmonyLib;

using static StoreRotationConfig.Api.RotationItemsAPI;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for removing purchased items from current and future store rotations.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    internal class UnlockShipObjectPatches
    {
        /// <summary>
        ///     Remove an item from current and future store rotations, and also from the list of permanent items.
        /// </summary>
        /// <param name="item">'UnlockableItem' instance of the item to remove.</param>
        /// <param name="unlockableID">The ID of the item to remove (only used for logging).</param>
        private static void RemoveFromRotation(UnlockableItem? item, int unlockableID = -1)
        {
            // Return if item OR its shop node does not exist.
            if (item == null || item.shopSelectionNode == null)
            {
                Plugin.StaticLogger?.LogWarning($"Item #{unlockableID} and/or its terminal node could not be found.");

                return;
            }

            Plugin.StaticLogger?.LogDebug($"Attempting to remove item '{item.unlockableName}' from the store rotation on local client...");

            // Attempt to remove item from the 'RotateShipDecorSelectionPatch.PermanentItems' list.
            if (RemovePermanentItem(item))
            {
                Plugin.StaticLogger?.LogDebug($"Removed item '{item.shopSelectionNode.creatureName}' from the list of permanent items.");
            }

            // Attempt to remove item from the current store rotation.
            if (Plugin.Terminal?.ShipDecorSelection.Remove(item.shopSelectionNode) == true)
            {
                Plugin.StaticLogger?.LogDebug($"Removed item '{item.shopSelectionNode.creatureName}' from the current store rotation.");
            }

            // Attempt to remove item from future store rotations.
            if (UnregisterItem(item))
            {
                Plugin.StaticLogger?.LogDebug($"Removed item '{item.shopSelectionNode.creatureName}' from future store rotations.");
            }
        }

        [HarmonyPatch("UnlockShipObject")]
        [HarmonyPrefix]
        private static void UnlockShipObjectPre(StartOfRound __instance, int unlockableID)
        {
            // Return if not running from server, no item was successfully purchased, OR the 'removePurchased' setting is not enabled.
            if (!__instance.IsHost || unlockableID == -1 || Plugin.Settings?.REMOVE_PURCHASED.Value == false)
            {
                return;
            }

            // Attempt to remove purchased item from store rotation on server.
            RemoveFromRotation(__instance.unlockablesList?.unlockables[unlockableID], unlockableID);
        }

        [HarmonyPatch(nameof(StartOfRound.BuyShipUnlockableClientRpc))]
        [HarmonyPrefix]
        private static void BuyShipUnlockableClientRpcPre(StartOfRound __instance, int unlockableID = -1)
        {
            // Return if running from server, no item was successfully purchased, OR the 'removePurchased' setting is not enabled.
            if (__instance.IsHost || unlockableID == -1 || Plugin.Settings?.REMOVE_PURCHASED.Value == false)
            {
                return;
            }

            // Attempt to remove purchased item from store rotation on clients.
            RemoveFromRotation(__instance.unlockablesList?.unlockables[unlockableID], unlockableID);
        }
    }
}