using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace StoreRotationConfig
{
    [BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
    [BepInDependency("com.sigurd.csync", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string GUID = "pacoito.StoreRotationConfig", PLUGIN_NAME = "StoreRotationConfig", VERSION = "1.1.0";
        public static Config Settings { get; internal set; }

        public static ManualLogSource StaticLogger;

        private void Awake()
        {
            StaticLogger = Logger;

            try
            {
                Settings = new(Config);

                Harmony harmony = new(PLUGIN_NAME);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                StaticLogger.LogInfo($"{PLUGIN_NAME} is loaded!");
            }
            catch (Exception e)
            {
                StaticLogger.LogError($"Error while initializing: '{e.Message}'\nSource: '{e.Source}'");
            }
        }
    }
}