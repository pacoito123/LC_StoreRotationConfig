using HarmonyLib;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for syncing already-purchased unlockable items with the client.
    /// </summary>
    internal static class SyncShipUnlockablesPatch
    {
        /// <summary>
        ///     Whether already-purchased unlockables have been successfully synced or not; reset upon returning to main menu.
        /// </summary>
        public static bool UnlockablesSynced { get; private set; }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesServerRpc))]
        [HarmonyPostfix]
        private static void SyncShipUnlockablesServerRpc_Postfix()
        {
            // Return if local client is not hosting the server.
            if (!NetworkManager.Singleton.IsHost)
            {
                return;
            }

            // Manually trigger a store rotation on the server.
            Plugin.Terminal.RotateShipDecorSelection();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPostfix]
        private static void SyncShipUnlockablesClientRpc_Postfix()
        {
            // Return if local client is hosting the server.
            if (NetworkManager.Singleton.IsHost)
            {
                return;
            }

            // Check if unlockables need to be synced.
            if (!UnlockablesSynced)
            {
                // Unlockables should be properly synced at this point.
                UnlockablesSynced = true;
            }

            // Manually trigger a store rotation on the local client.
            Plugin.Terminal.RotateShipDecorSelection();
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
        [HarmonyPrefix]
        private static void MenuManagerStart_Prefix()
        {
            // Reset local client sync status upon entering (or returning to) main menu.
            UnlockablesSynced = false;
        }
    }
}