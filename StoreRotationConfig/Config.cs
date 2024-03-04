using System.Runtime.Serialization;
using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using Unity.Netcode;

namespace StoreRotationConfig
{
    [DataContract]
    public class Config : SyncedConfig<Config>
    {
        [DataMember] public SyncedEntry<int> MIN_ITEMS { get; private set; }
        [DataMember] public SyncedEntry<int> MAX_ITEMS { get; private set; }
        [DataMember] public SyncedEntry<bool> STOCK_ALL { get; private set; }
        [DataMember] public SyncedEntry<bool> SORT_ITEMS { get; private set; }

        public Config(ConfigFile cfg) : base(Plugin.GUID)
        {
            // Register to sync config files between host and clients.
            ConfigManager.Register(this);

            // Bind config entries to config file.
            MIN_ITEMS = cfg.BindSyncedEntry("General", "minItems", 8, "Minimum number of items in store rotation.");
            MAX_ITEMS = cfg.BindSyncedEntry("General", "maxItems", 12, "Maximum number of items in store rotation.");
            STOCK_ALL = cfg.BindSyncedEntry("General", "stockAll", false, "Make every item available in store rotation.");
            SORT_ITEMS = cfg.BindSyncedEntry("General", "sortItems", false, "Sort every item in store rotation alphabetically.");
            // ...

            // Function to run once config is synced.
            SyncComplete += new((_, _) =>
            {
                // Check if the local client running is the server host.
                if (!IsHost && !NetworkManager.Singleton.IsServer)
                {
                    Plugin.StaticLogger.LogInfo("Config synced! ⭮ Rotating store...");

                    // Manually trigger a store rotation after config sync.
                    Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
                    terminal?.ShipDecorSelection.Clear();
                    terminal?.RotateShipDecorSelection();
                }
            });
        }
    }
}