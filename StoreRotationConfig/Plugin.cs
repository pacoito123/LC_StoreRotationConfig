﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using StoreRotationConfig.Patches;
using System;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Configure the number of items in each store rotation, show them all, remove purchases, sort them, and/or enable sales for them.
    /// </summary>
    [BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
    [BepInDependency("com.sigurd.csync", "5.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string GUID = "pacoito.StoreRotationConfig", PLUGIN_NAME = "StoreRotationConfig", VERSION = "2.6.1";
        internal static ManualLogSource? StaticLogger { get; private set; }

        /// <summary>
        ///     Harmony instance for patching.
        /// </summary>
        internal static Harmony? Harmony { get; private set; }

        /// <summary>
        ///     Plugin configuration instance.
        /// </summary>
        public static Config? Settings { get; private set; }

        /// <summary>
        ///     Cached terminal instance.
        /// </summary>
        public static Terminal? Terminal
        {
            get
            {
                if (field == null)
                {
                    Terminal = FindObjectOfType<Terminal>();
                }

                return field;
            }
            private set;
        }

        private void Awake()
        {
            StaticLogger = Logger;

            try
            {
                // Initialize 'Config' and 'Harmony' instances.
                Settings = new(Config);
                Harmony = new(GUID);
                //

                // Apply all patches, except for compatibility ones.
                Harmony.PatchAll(typeof(RotateShipDecorSelectionPatch));
                Harmony.PatchAll(typeof(SyncShipUnlockablesPatch));
                Harmony.PatchAll(typeof(TerminalItemSalesPatches));
                Harmony.PatchAll(typeof(TerminalScrollMousePatch));
                Harmony.PatchAll(typeof(UnlockShipObjectPatches));
                // ...

                StaticLogger.LogInfo($"'{PLUGIN_NAME}' loaded!");
            }
            catch (Exception e)
            {
                StaticLogger.LogError($"Error while initializing '{PLUGIN_NAME}': {e}");
            }
        }
    }
}