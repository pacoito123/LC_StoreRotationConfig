using HarmonyLib;
using StoreRotationConfig.Networking;
using System.Collections.Generic;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for removing purchased items from current and future store rotations.
    /// </summary>
    internal static class UnlockShipObjectPatches
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
            if (unlockableID < 0 || Plugin.Settings == null || !Plugin.Settings.REMOVE_PURCHASED.Value)
            {
                return;
            }

            if (Plugin.Terminal == null)
            {
                Plugin.Logger.LogError("Could not find Terminal instance! Store rotation doesn't exist...");

                return;
            }

            // Obtain index in the current store rotation for the purchased item.
            List<TerminalNode>? shipDecorSelection = Plugin.Terminal.ShipDecorSelection;
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