using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using System.Runtime.Serialization;
// using Unity.Netcode;
// using UnityEngine;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Class containing and defining plugin configuration options, with some entries being synced between host and clients.
    /// </summary>
    [DataContract]
    public class Config : SyncedConfig<Config>
    {
        /// <summary>
        ///     Minimum number of items in the store rotation.
        /// </summary>
        [field: DataMember] public SyncedEntry<int> MIN_ITEMS { get; private set; }

        /// <summary>
        ///     Maximum number of items in the store rotation.
        /// </summary>
        [field: DataMember] public SyncedEntry<int> MAX_ITEMS { get; private set; }

        /// <summary>
        ///     Make every item available in the store rotation.
        /// </summary>
        [field: DataMember] public SyncedEntry<bool> STOCK_ALL { get; private set; }

        /// <summary>
        ///     Include already-purchased items in the store rotation. If disabled, prevents purchased items from showing up again
        ///     in future store rotations, and removes them from the current one.
        /// </summary>
        [field: DataMember] public SyncedEntry<bool> STOCK_PURCHASED { get; private set; }

        /// <summary>
        ///     Sort every item in the store rotation alphabetically.
        /// </summary>
        public ConfigEntry<bool> SORT_ITEMS { get; private set; }

        /// <summary>
        ///     [EXPERIMENTAL] Adapt terminal scroll to the number of lines in the current terminal page, instead of a flat value.
        ///     Should fix cases where scrolling skips over several lines, which is especially noticeable when enabling 'stockAll'
        ///     with a large number of items in the store.
        /// </summary>
        public ConfigEntry<bool> RELATIVE_SCROLL { get; private set; }

        /* /// <summary>
        ///     Whether config has been successfully synced with the host or not; reset upon returning to main menu.
        /// </summary>
        /// <remarks>Not really needed, mostly here in case 'CSync' reimplements the ability to join hosts who don't have this mod installed.</remarks>
        public bool ConfigSynced { get; internal set; } = false; */

        /// <summary>
        ///     Constructor for initializing plugin configuration. Registers instance in 'ConfigManager', binds entries to configuration file,
        ///     and defines code to execute after a successful sync.
        /// </summary>
        /// <param name="cfg">BepInEx configuration file.</param>
        public Config(ConfigFile cfg) : base(Plugin.GUID)
        {
            // Register to sync config files between host and clients.
            ConfigManager.Register(this);

            // Bind config entries to config file.
            MIN_ITEMS = cfg.BindSyncedEntry("General", "minItems", 8, "Minimum number of items in the store rotation.");
            MAX_ITEMS = cfg.BindSyncedEntry("General", "maxItems", 12, "Maximum number of items in the store rotation.");
            STOCK_ALL = cfg.BindSyncedEntry("General", "stockAll", false, "Make every item available in the store rotation.");
            STOCK_PURCHASED = cfg.BindSyncedEntry("General", "stockPurchased", true, "Include already-purchased items in the store rotation. "
                + "If disabled, prevents purchased items from showing up again in future store rotations, and removes them from the current one.");

            SORT_ITEMS = cfg.Bind("Miscellaneous", "sortItems", false, "Sort every item in the store rotation alphabetically.");
            RELATIVE_SCROLL = cfg.Bind("Miscellaneous", "relativeScroll", false, "[EXPERIMENTAL] Adapt terminal scroll to the "
                + "number of lines in the current terminal page, instead of a flat value. Should fix cases where scrolling skips over several lines, which is "
                + "especially noticeable when enabling 'stockAll' with a large number of items in the store.");
            // ...

            // Function to run once config is synced.
            /* InitialSyncCompleted += new((_, _) =>
            {
                // Return if local game instance is hosting the server.
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    return;
                }

                Plugin.StaticLogger.LogInfo("Config synced! ⭮ Rotating store...");

                // Set config sync status to true (successfully synced).
                ConfigSynced = true;

                // Manually trigger a store rotation after config sync.
                Terminal terminal = Object.FindObjectOfType<Terminal>();
                terminal?.ShipDecorSelection.Clear();
                terminal?.RotateShipDecorSelection();
            }); */
        }
    }
}