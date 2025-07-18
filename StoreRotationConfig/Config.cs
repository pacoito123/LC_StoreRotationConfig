using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using HarmonyLib;
using StoreRotationConfig.Patches;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Class containing and defining plugin configuration options, with some entries being synced between host and clients.
    /// </summary>
    [DataContract]
    public class Config : SyncedConfig2<Config>
    {
        /// <summary>
        ///     Minimum number of items in the store rotation.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> MIN_ITEMS { get; private set; }

        /// <summary>
        ///     Maximum number of items in the store rotation.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> MAX_ITEMS { get; private set; }

        /// <summary>
        ///     Make every item available in the store rotation.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<bool> STOCK_ALL { get; private set; }

        /// <summary>
        ///     Remove purchased items from the current and future store rotations. If disabled, allows purchased items to show up again
        ///     in future store rotations.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<bool> REMOVE_PURCHASED { get; private set; }

        /// <summary>
        ///     The comma-separated names of items that will be guaranteed to show up in every store rotation. Whitelisted items
        ///     are always added on top of the range defined by the 'minItems' and 'maxItems' settings, and take priority over the
        ///     blacklist. Has no effect with the 'stockAll' setting enabled.
        ///     Example: \"Bee suit,Goldfish,Television\".
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<string> ITEM_WHITELIST { get; private set; }

        /// <summary>
        ///     The comma-separated names of items that will never show up in the store rotation. You're a mean one, Mr. Grinch.
        ///     Example: "Bee suit,Goldfish,Television"
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<string> ITEM_BLACKLIST { get; private set; }

        /// <summary>
        ///     The percentage chance for ANY item to be on sale in the store rotation. Setting this to '0' disables the entire
        ///     sales system.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> SALE_CHANCE { get; private set; }

        /// <summary>
        ///     The minimum number of items that can be on sale at a time.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> MIN_SALE_ITEMS { get; private set; }

        /// <summary>
        ///     The maximum number of items that can be on sale at a time.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> MAX_SALE_ITEMS { get; private set; }

        /// <summary>
        ///     The minimum discount to apply to items on sale.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> MIN_DISCOUNT { get; private set; }

        /// <summary>
        ///     The maximum discount to apply to items on sale.
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<int> MAX_DISCOUNT { get; private set; }

        /// <summary>
        ///     Round rotation store discounts to the nearest ten (like the regular store).
        /// </summary>
        [field: SyncedEntryField] public SyncedEntry<bool> ROUND_TO_NEAREST_TEN { get; private set; }

        /// <summary>
        ///     Sort every item in the store rotation alphabetically.
        /// </summary>
        public ConfigEntry<bool> SORT_ITEMS { get; private set; }

        /// <summary>
        ///     Adapt terminal scroll to the number of lines in the current terminal page, instead of a flat value. Should fix
        ///     cases where scrolling skips over several lines, which is especially noticeable when enabling 'stockAll' with a
        ///     large number of items added to the rotating store.
        /// </summary>
        public ConfigEntry<bool> RELATIVE_SCROLL { get; private set; }

        /// <summary>
        ///     Number of lines to scroll at a time with 'relativeScroll' enabled.
        /// </summary>
        public ConfigEntry<int> LINES_TO_SCROLL { get; private set; }

        /// <summary>
        ///     Constructor for initializing plugin configuration. Registers instance in 'ConfigManager', binds entries to configuration file,
        ///     and defines code to execute after a successful sync.
        /// </summary>
        /// <param name="cfg">BepInEx configuration file.</param>
        public Config(ConfigFile cfg) : base(Plugin.GUID)
        {
            // Disable saving config after a call to 'Bind()' is made.
            cfg.SaveOnConfigSet = false;

            // Bind config entries to the config file.
            MIN_ITEMS = cfg.BindSyncedEntry("General", "minItems", 8, "Minimum number of items in the store rotation.");
            MAX_ITEMS = cfg.BindSyncedEntry("General", "maxItems", 12, "Maximum number of items in the store rotation.");
            STOCK_ALL = cfg.BindSyncedEntry("General", "stockAll", false, "Make every item available in the store rotation.");
            REMOVE_PURCHASED = cfg.BindSyncedEntry("General", "removePurchased", false, "Remove purchased items from the current and future store rotations."
                + "If enabled, prevents purchased items from showing up again in future store rotations, and removes them from the current one.");
            ITEM_WHITELIST = cfg.BindSyncedEntry("General", "itemWhitelist", "", "The comma-separated names of items that will be guaranteed to show up "
                + "in every store rotation. Whitelisted items are always added on top of the range defined by the 'minItems' and 'maxItems' settings, and take priority over the blacklist. "
                + "Has no effect with the 'stockAll' setting enabled.\nExample: \"Bee suit,Goldfish,Television\"");
            ITEM_BLACKLIST = cfg.BindSyncedEntry("General", "itemBlacklist", "", "The comma-separated names of items that will never show up in the store "
                + "rotation. You're a mean one, Mr. Grinch.\nExample: \"Bee suit,Goldfish,Television\"");

            SALE_CHANCE = cfg.BindSyncedEntry("Sales", "saleChance", 33, new ConfigDescription("The percentage chance for ANY "
                + "item to be on sale in the store rotation. Setting this to '0' disables the entire sales system.", new AcceptableValueRange<int>(0, 100)));
            MIN_SALE_ITEMS = cfg.BindSyncedEntry("Sales", "minSaleItems", 1, "The minimum number of items that can be on sale at a time.");
            MAX_SALE_ITEMS = cfg.BindSyncedEntry("Sales", "maxSaleItems", 5, "The maximum number of items that can be on sale at a time.");
            MIN_DISCOUNT = cfg.BindSyncedEntry("Sales", "minDiscount", 10, new ConfigDescription("The minimum discount to apply "
                + "to items on sale.", new AcceptableValueRange<int>(1, 100)));
            MAX_DISCOUNT = cfg.BindSyncedEntry("Sales", "maxDiscount", 50, new ConfigDescription("The maximum discount to apply "
                + "to items on sale.", new AcceptableValueRange<int>(1, 100)));
            ROUND_TO_NEAREST_TEN = cfg.BindSyncedEntry("Sales", "roundToNearestTen", true, "Round rotation store discounts to the nearest ten "
                + "(like the regular store).");

            SORT_ITEMS = cfg.Bind("Miscellaneous", "sortItems", false, "Sort every item in the store rotation alphabetically.");
            RELATIVE_SCROLL = cfg.Bind("Miscellaneous", "relativeScroll", true, "Adapt terminal scroll to the number of lines in the current terminal "
                + "page, instead of a flat value. Should fix cases where scrolling skips over several lines, which is especially noticeable when enabling 'stockAll' with a large number of items "
                + "added to the rotating store.");
            LINES_TO_SCROLL = cfg.Bind("Miscellaneous", "linesToScroll", 20, new ConfigDescription("Number of lines to scroll at a time with "
                + "'relativeScroll' enabled.", new AcceptableValueRange<int>(1, 28)));
            // ...

            // Reset cached text if 'linesToScroll' is updated in-game.
            LINES_TO_SCROLL.SettingChanged += new((_, _) =>
            {
                TerminalScrollMousePatch.CurrentText = "";
            });

            // Remove old config settings.
            ClearOrphanedEntries(cfg);

            // Re-enable saving and save config.
            cfg.SaveOnConfigSet = true;
            cfg.Save();

            // Register to sync config files between host and clients.
            ConfigManager.Register(this);
        }

        /// <summary>
        ///     Remove old (orphaned) configuration entries.
        /// </summary>
        /// <remarks>Obtained from: https://lethal.wiki/dev/intermediate/custom-configs#better-configuration</remarks>
        /// <param name="config">The config file to clear orphaned entries from.</param>
        private void ClearOrphanedEntries(ConfigFile config)
        {
            // Obtain 'OrphanedEntries' dictionary from ConfigFile through reflection.
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
            Dictionary<ConfigDefinition, string>? orphanedEntries = (Dictionary<ConfigDefinition, string>?)orphanedEntriesProp.GetValue(config);

            // Clear orphaned entries.
            orphanedEntries?.Clear();
        }
    }
}