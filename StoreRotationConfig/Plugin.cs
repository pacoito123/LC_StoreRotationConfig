using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Simple mod that adds configurability to the number of items that show up in the store every week.
    /// </summary>
    [BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
    [BepInDependency("com.sigurd.csync", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string GUID = "pacoito.StoreRotationConfig", PLUGIN_NAME = "StoreRotationConfig", VERSION = "1.1.2";
        internal static ManualLogSource StaticLogger { get; private set; }

        /// <summary>
        ///     Plugin configuration instance.
        /// </summary>
        public static Config Settings { get; private set; }

        private void Awake()
        {
            StaticLogger = Logger;

            try
            {
                Settings = new(Config);
                _ = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), $"{GUID}");

                StaticLogger.LogInfo($"{PLUGIN_NAME} loaded!");
            }
            catch (Exception e)
            {
                StaticLogger.LogError($"Error while initializing '{PLUGIN_NAME}': {e}");
            }
        }
    }
}