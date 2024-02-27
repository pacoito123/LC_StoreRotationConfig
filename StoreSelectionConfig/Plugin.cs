using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace StoreSelectionConfig
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            try
            {
                Harmony harmony = new(PluginInfo.PLUGIN_GUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception) { }
        }
    }
}