// using HarmonyLib;

namespace StoreRotationConfig.Patches
{
    /* /// <summary>
    ///     Patch for 'MenuManager.Start()' method; resets config sync status upon loading the main menu.
    /// </summary>
    /// <remarks>Not really needed, mostly here in case 'CSync' reimplements the ability to join hosts who don't have this mod installed.</remarks>
    [HarmonyPatch(typeof(MenuManager), "Start")]
    internal class MenuManagerStartPatch
    {
        private static void Prefix()
        {
            // Set config sync status to false (initial value).
            Plugin.Settings.ConfigSynced = false;
        }
    } */
}