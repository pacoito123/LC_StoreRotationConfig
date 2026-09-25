using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using StoreRotationConfig.Patches;
using System;
using UnityEngine;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Configure the number of items in each store rotation, show them all, remove purchases, sort them, and/or enable sales for them.
    /// </summary>
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>
        ///     BepInEx Plugin information.
        /// </summary>
        public const string PLUGIN_GUID = "pacoito.StoreRotationConfig", PLUGIN_NAME = "StoreRotationConfig", VERSION = "3.0.0";
        internal static new ManualLogSource Logger => field ??= BepInEx.Logging.Logger.CreateLogSource(PLUGIN_NAME);
        internal static Harmony Harmony => field ??= new(PLUGIN_GUID);

        /// <summary>
        ///     Plugin configuration instance.
        /// </summary>
        public static Config Settings { get; private set; } = null!;

        /// <summary>
        ///     Cached terminal instance.
        /// </summary>
        public static Terminal Terminal
        {
            get
            {
                if (field == null)
                {
                    field = FindAnyObjectByType<Terminal>(FindObjectsInactive.Exclude);
                }

                return field;
            }
        }

        private void Awake()
        {
            try
            {
                // Initialize 'Config' instance.
                Settings = new(Config);

                // Harmony.PatchAll(typeof(NetworkingInitPatches));

                // Apply all patches.
                Harmony.PatchAll(typeof(RotateShipDecorSelectionPatch));
                Harmony.PatchAll(typeof(SyncShipUnlockablesPatch));
                Harmony.PatchAll(typeof(TerminalItemSalesPatches));
                Harmony.PatchAll(typeof(TerminalScrollMousePatch));
                Harmony.PatchAll(typeof(UnlockShipObjectPatches));
                // ...

                Logger.LogInfo($"{PLUGIN_NAME} v{VERSION} loaded!");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while initializing '{PLUGIN_NAME}': {e}");
            }
        }
    }
}