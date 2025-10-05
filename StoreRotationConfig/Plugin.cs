using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using StoreRotationConfig.Patches;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Configure the number of items in each store rotation, show them all, remove purchases, sort them, and/or enable sales for them.
    /// </summary>
    [BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string GUID = "pacoito.StoreRotationConfig", PLUGIN_NAME = "StoreRotationConfig", VERSION = "3.0.0";
        internal static ManualLogSource StaticLogger { get; private set; } = null!;

        /// <summary>
        ///     Harmony instance for patching.
        /// </summary>
        internal static Harmony Harmony { get; private set; } = null!;

        /// <summary>
        ///     Plugin configuration instance.
        /// </summary>
        public static Config Settings { get; private set; } = null!;

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

                NetcodePatcher(); // Patches your netcode, patches your netcode, patches your netcode...
                // Harmony.PatchAll(typeof(NetworkingInitPatches));

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

        private static void NetcodePatcher()
        {
            Type[] types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = [.. e.Types.Where(type => type != null)];
            }

            foreach (Type type in types)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length > 0)
                    {
                        _ = method.Invoke(null, null);
                    }
                }
            }
        }
    }
}