using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Configure the number of items in each store rotation, show them all, remove purchased items, or sort them.
    /// </summary>
    [BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
    [BepInDependency("com.sigurd.csync", "5.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string GUID = "pacoito.StoreRotationConfig", PLUGIN_NAME = "StoreRotationConfig", VERSION = "2.2.1";
        internal static ManualLogSource StaticLogger { get; private set; }

        /// <summary>
        ///     Plugin configuration instance.
        /// </summary>
        public static Config Settings { get; private set; }

        /// <summary>
        ///     Cached terminal instance.
        /// </summary>
        /// <remarks>An error will be thrown if the Terminal cannot be found or is missing.</remarks>
        public static Terminal Terminal
        {
            get
            {
                // Ensure cached terminal instance exists.
                if (_terminal == null)
                {
                    Terminal = FindObjectOfType<Terminal>();
                }

                return _terminal;
            }
            private set => _terminal = value ?? throw new ArgumentNullException("_terminal", "Terminal GameObject not found...");
        }
        private static Terminal _terminal;

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