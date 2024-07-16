using HarmonyLib;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for syncing already-purchased unlockable items with the client.
    /// </summary>
    [HarmonyPatch]
    internal class SyncShipUnlockablesPatch
    {
        /// <summary>
        ///     Whether already-purchased unlockables have been successfully synced or not; reset upon returning to main menu.
        /// </summary>
        public static bool UnlockablesSynced { get; private set; } = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        private static void SyncShipUnlockablesClientPost()
        {
            // Return if local game instance is hosting the server.
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // Return if unlockables are already synced with the host; toggle unlockable sync status if not.
            if (UnlockablesSynced)
            {
                Plugin.StaticLogger.LogDebug("Purchased unlockable items already synced with the host.");

                return;
            }
            UnlockablesSynced = true;

            // Manually trigger a store rotation.
            Plugin.Terminal.RotateShipDecorSelection();
        }

        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPrefix]
        private static void MenuManagerStartPre()
        {
            // Reset local client sync status upon entering (or returning to) main menu.
            // Plugin.Settings.ConfigSynced = false;
            UnlockablesSynced = false;
            // ...
        }
    }
}