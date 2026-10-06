using BepInEx.Configuration;
using StoreRotationConfig.Patches;
using System;
using System.Collections.Generic;

namespace StoreRotationConfig
{
    /// <summary>
    ///     Class containing and defining plugin configuration options.
    /// </summary>
    public sealed class Config
    {
        /// <summary>
        ///     Set of items to always include in every store rotation.
        /// </summary>
        /// <remarks>Takes priority over blacklist.</remarks>
        public HashSet<UnlockableItem> WhitelistedItems => field ??= [];

        /// <summary>
        ///     Set of items to always exclude from every store rotation.
        /// </summary>
        public HashSet<UnlockableItem> BlacklistedItems => field ??= [];

        /// <summary>
        ///     Minimum number of items in the store rotation.
        /// </summary>
        public ConfigEntry<int> MIN_ITEMS { get; private set; }

        /// <summary>
        ///     Maximum number of items in the store rotation.
        /// </summary>
        public ConfigEntry<int> MAX_ITEMS { get; private set; }

        /// <summary>
        ///     Make every item available in the store rotation.
        /// </summary>
        public ConfigEntry<bool> STOCK_ALL { get; private set; }

        /// <summary>
        ///     Remove purchased items from the current and future store rotations. If disabled, allows purchased items to show up again
        ///     in future store rotations.
        /// </summary>
        public ConfigEntry<bool> REMOVE_PURCHASED { get; private set; }

        /// <summary>
        ///     The comma-separated names of items that will be guaranteed to show up in every store rotation. Whitelisted items
        ///     are always added on top of the range defined by the 'minItems' and 'maxItems' settings, and take priority over the
        ///     blacklist. Has no effect with the 'stockAll' setting enabled.
        ///     Example: \"Bee suit,Goldfish,Television\".
        /// </summary>
        public ConfigEntry<string> ITEM_WHITELIST { get; private set; }

        /// <summary>
        ///     The comma-separated names of items that will never show up in the store rotation. You're a mean one, Mr. Grinch.
        ///     Example: "Bee suit,Goldfish,Television"
        /// </summary>
        public ConfigEntry<string> ITEM_BLACKLIST { get; private set; }

        /// <summary>
        ///     The percentage chance for ANY item to be on sale in the store rotation. Setting this to '0' disables the entire
        ///     sales system.
        /// </summary>
        public ConfigEntry<float> SALE_CHANCE { get; private set; }

        /// <summary>
        ///     The minimum number of items that can be on sale at a time.
        /// </summary>
        public ConfigEntry<int> MIN_SALE_ITEMS { get; private set; }

        /// <summary>
        ///     The maximum number of items that can be on sale at a time.
        /// </summary>
        public ConfigEntry<int> MAX_SALE_ITEMS { get; private set; }

        /// <summary>
        ///     The minimum discount to apply to items on sale.
        /// </summary>
        public ConfigEntry<int> MIN_DISCOUNT { get; private set; }

        /// <summary>
        ///     The maximum discount to apply to items on sale.
        /// </summary>
        public ConfigEntry<int> MAX_DISCOUNT { get; private set; }

        /// <summary>
        ///     Round rotation store discounts to the nearest ten (like the regular store).
        /// </summary>
        public ConfigEntry<bool> ROUND_TO_NEAREST_TEN { get; private set; }

        /// <summary>
        ///     Display rotation store discounts in advertisements (like regular tools).
        /// </summary>
        public ConfigEntry<bool> DISPLAY_AD_DISCOUNTS { get; private set; }

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
        ///     Constructor for initializing plugin configuration.
        /// </summary>
        /// <param name="cfg">BepInEx configuration file.</param>
        public Config(ConfigFile cfg)
        {
            // Disable saving config after a call to 'Bind()' is made.
            cfg.SaveOnConfigSet = false;

            // Bind config entries to the config file.
            MIN_ITEMS = cfg.Bind("General", "minItems", 8, "Minimum number of items in the store rotation.");
            MAX_ITEMS = cfg.Bind("General", "maxItems", 12, "Maximum number of items in the store rotation.");
            STOCK_ALL = cfg.Bind("General", "stockAll", false, "Make every item available in the store rotation.");
            REMOVE_PURCHASED = cfg.Bind("General", "removePurchased", true, "Remove purchased items from the current and future store rotations."
                + "If enabled, prevents purchased items from showing up again in future store rotations, and removes them from the current one.");
            ITEM_WHITELIST = cfg.Bind("General", "itemWhitelist", "", "The comma-separated names of items that will be guaranteed to show up "
                + "in every store rotation. Whitelisted items are always added on top of the range defined by the 'minItems' and 'maxItems' settings, and take priority over the blacklist. "
                + "Has no effect with the 'stockAll' setting enabled.\nExample: \"Bee suit,Goldfish,Television\"");
            ITEM_BLACKLIST = cfg.Bind("General", "itemBlacklist", "", "The comma-separated names of items that will never show up in the store "
                + "rotation. You're a mean one, Mr. Grinch.\nExample: \"Bee suit,Goldfish,Television\"");

            SALE_CHANCE = cfg.Bind("Sales", "saleChance", 100 / 3.0f, new ConfigDescription("The percentage chance for ANY "
                + "item to be on sale in the store rotation. Setting this to '0' disables the entire sales system.", new AcceptableValueRange<float>(0.0f, 100.0f)));
            MIN_SALE_ITEMS = cfg.Bind("Sales", "minSaleItems", 1, "The minimum number of items that can be on sale at a time.");
            MAX_SALE_ITEMS = cfg.Bind("Sales", "maxSaleItems", 5, "The maximum number of items that can be on sale at a time.");
            MIN_DISCOUNT = cfg.Bind("Sales", "minDiscount", 10, new ConfigDescription("The minimum discount to apply "
                + "to items on sale.", new AcceptableValueRange<int>(1, 100)));
            MAX_DISCOUNT = cfg.Bind("Sales", "maxDiscount", 50, new ConfigDescription("The maximum discount to apply "
                + "to items on sale.", new AcceptableValueRange<int>(1, 100)));
            ROUND_TO_NEAREST_TEN = cfg.Bind("Sales", "roundToNearestTen", true, "Round rotation store discounts to the nearest ten "
                + "(like the regular store).");
            DISPLAY_AD_DISCOUNTS = cfg.Bind("Sales", "displayAdDiscounts", true, "Display rotation store discounts in advertisements "
                + "(like regular tools).");

            SORT_ITEMS = cfg.Bind("Miscellaneous", "sortItems", false, "Sort every item in the store rotation alphabetically.");
            RELATIVE_SCROLL = cfg.Bind("Miscellaneous", "relativeScroll", true, "Adapt terminal scroll to the number of lines in the current terminal "
                + "page, instead of a flat value. Should fix cases where scrolling skips over several lines, which is especially noticeable when enabling 'stockAll' with a large number of items "
                + "added to the rotating store.");
            LINES_TO_SCROLL = cfg.Bind("Miscellaneous", "linesToScroll", 20, new ConfigDescription("Number of lines to scroll at a time with "
                + "'relativeScroll' enabled.", new AcceptableValueRange<int>(1, 28)));
            // ...

            // Reset cached text if 'linesToScroll' is updated in-game.
            LINES_TO_SCROLL.SettingChanged += static (_, _) => TerminalScrollMousePatch.CurrentText = string.Empty;

            // Refresh whitelisted and blacklisted items if they are updated in-game.
            ITEM_WHITELIST.SettingChanged += RefreshConfigLists;
            ITEM_BLACKLIST.SettingChanged += RefreshConfigLists;

            // Refresh store rotation if settings are updated in-game.
            MIN_ITEMS.SettingChanged += RefreshRotation;
            MAX_ITEMS.SettingChanged += RefreshRotation;
            STOCK_ALL.SettingChanged += RefreshRotation;
            REMOVE_PURCHASED.SettingChanged += RefreshRotation;
            ITEM_BLACKLIST.SettingChanged += RefreshRotation;

            SALE_CHANCE.SettingChanged += RefreshRotation;
            MIN_SALE_ITEMS.SettingChanged += RefreshRotation;
            MAX_SALE_ITEMS.SettingChanged += RefreshRotation;
            MIN_DISCOUNT.SettingChanged += RefreshRotation;
            MAX_DISCOUNT.SettingChanged += RefreshRotation;
            ROUND_TO_NEAREST_TEN.SettingChanged += RefreshRotation;

            SORT_ITEMS.SettingChanged += RefreshRotation;
            // ...

            // Remove old config settings.
            cfg.OrphanedEntries.Clear();

            // Re-enable saving and save config.
            cfg.SaveOnConfigSet = true;
            cfg.Save();
        }

        /// <summary>
        ///     Refresh store rotation, if hosting the server.
        /// </summary>
        private static void RefreshRotation(object obj, EventArgs args)
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.IsHost && Plugin.Terminal != null)
            {
                Plugin.Terminal.RotateShipDecorSelection();
            }
        }

        /// <summary>
        ///     Refresh whitelisted and blacklisted items configuration.
        /// </summary>
        internal void RefreshConfigLists()
        {
            WhitelistedItems.Clear();
            BlacklistedItems.Clear();

            // Return if unlockable items are not yet loaded.
            if (StartOfRound.Instance == null || StartOfRound.Instance.unlockablesList == null)
            {
                return;
            }

            // Split configured whitelisted and blacklisted items by comma, and remove all spaces.
            string[] whitelist = ITEM_WHITELIST.Value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries),
                blacklist = ITEM_BLACKLIST.Value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);

            // Iterate for every registered unlockable item.
            for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables?.Count; i++)
            {
                UnlockableItem? item = StartOfRound.Instance.unlockablesList.unlockables[i];

                // Skip item if missing or lacking a valid Terminal node.
                if (item == null || item.shopSelectionNode == null)
                {
                    continue;
                }

                // Obtain item Terminal display name and unlockable name, and remove all spaces.
                string displayName = string.Join(string.Empty, item.shopSelectionNode.creatureName.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries)),
                    unlockableName = string.Join(string.Empty, item.unlockableName.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

                // Attempt to find item to whitelist using either Terminal display name or unlockable name.
                int whitelistIndex = Array.FindIndex(whitelist, whitelistName => whitelistName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
                    || whitelistName.Equals(unlockableName, StringComparison.OrdinalIgnoreCase));

                if (whitelistIndex != -1)
                {
                    // Add item to whitelist, if found.
                    _ = WhitelistedItems.Add(item);

                    continue;
                }

                // Attempt to find item to blacklist using either Terminal display name or unlockable name.
                int blacklistIndex = Array.FindIndex(blacklist, blacklistName => blacklistName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
                    || blacklistName.Equals(unlockableName, StringComparison.OrdinalIgnoreCase));

                if (blacklistIndex != -1)
                {
                    // Add item to blacklist, if found.
                    _ = BlacklistedItems.Add(item);
                }
            }
        }

        /// <summary>
        ///     Refresh whitelisted and blacklisted items configuration.
        /// </summary>
        private void RefreshConfigLists(object obj, EventArgs args)
        {
            RefreshConfigLists();
        }
    }
}