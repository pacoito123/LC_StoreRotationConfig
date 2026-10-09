using HarmonyLib;
using StoreRotationConfig.Compatibility;
using System;

namespace StoreRotationConfig.Patches
{
    internal static class MenuManagerPatch
    {
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void MenuManagerStart_Prefix()
        {
            if (GameNetworkManager.Instance == null || !GameNetworkManager.Instance.firstTimeInMenu)
            {
                return;
            }

            if (TerminalStuffCompatibility.Enabled)
            {
                try
                {
                    Plugin.Harmony.PatchAll(typeof(TerminalStuffCompatibility));
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"Error while patching compatibility for 'TerminalStuff': {e}");
                }
            }

            /* if (InteractiveStoreCompatibility.Enabled)
            {
                try
                {
                    Plugin.Harmony.PatchAll(typeof(InteractiveStoreCompatibility));
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"Error while patching compatibility for 'TerminalStuff': {e}");
                }
            } */

            if (TerminalUtilsCompatibility.Enabled)
            {
                try
                {
                    Plugin.Harmony.PatchAll(typeof(TerminalUtilsCompatibility));
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"Error while patching compatibility for 'TerminalUtils': {e}");
                }

                if (TerminalFormatterCompatibility.Enabled)
                {
                    try
                    {
                        Plugin.Harmony.PatchAll(typeof(TerminalFormatterCompatibility));
                    }
                    catch (Exception e)
                    {
                        Plugin.Logger.LogError($"Error while patching compatibility for 'TerminalFormatter': {e}");
                    }
                }
            }
        }
    }
}