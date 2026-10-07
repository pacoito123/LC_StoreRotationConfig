using HarmonyLib;
using StoreRotationConfig.Networking;
using System.Collections.Generic;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for removing purchased items from current and future store rotations.
    /// </summary>
    internal static class UnlockShipObjectPatch
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.BuyShipUnlockableServerRpc))]
        [HarmonyPrefix]
        private static void BuyShipUnlockableServerRpc_Prefix(StartOfRound __instance, int unlockableID)
        {
            // Only run on the server.
            if (__instance.__rpc_exec_stage is not NetworkBehaviour.__RpcExecStage.Execute)
            {
                return;
            }

            // Return if unlockable ID is invalid, or purchased items are not configured to be removed.
            if (unlockableID < 0 || Plugin.Settings?.REMOVE_PURCHASED.Value != true)
            {
                return;
            }

            List<TerminalNode>? shipDecorSelection = (Plugin.Terminal != null) ? Plugin.Terminal.ShipDecorSelection : null;

            // Obtain index in the current store rotation for the purchased item.
            int rotationIndex = shipDecorSelection?.FindIndex(node => node != null && node.shipUnlockableID == unlockableID) ?? -1;

            // Return if item being purchased is not present in the current store rotation.
            if (rotationIndex < 0 || shipDecorSelection == null || rotationIndex >= shipDecorSelection.Count)
            {
                return;
            }

            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError("StoreRotationNetworker instance is missing! Could not remove purchased item...");

                return;
            }

            // Attempt to remove purchased item from store rotation.
            StoreRotationNetworker.Instance.StoreRotation.RemoveAt(rotationIndex);
        }
    }
}