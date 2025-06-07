# Changelog

## [2.6.1]

Actually included the updated plugin file this time...

## [2.6.0]

Recompiled and updated for v72!

- Removed (nearly) all my patches to sync already-purchased and/or in-storage unlockable items, since vanilla appears to now be handling it properly.
  - Before v70, `UnlockableItem.hasBeenUnlockedByPlayer` wasn't being set for unlockable items upon joining a lobby, and `SyncShipUnlockablesClientRpc()`'s `storedItems` parameter sent to clients was always just an empty array.

## [2.5.1]

Removed goofy TerminalFormatter compatibility, also recompiled for v69.

- **NOTE:** [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter) `v0.2.24` is currently incompatible; rotating item discounts aren't displayed (though they still apply), and any available items above the vanilla cap are not shown (but they can still be purchased).
  - Make sure to update to a newer version once available!

## [2.5.0]

Basic API added for managing rotating items, fixed rotating shop desync with 'removePurchased' setting.

- Created `RotationItemsAPI` class, which contains several helper methods for interacting with which items can appear in store rotations.
  - Meant for other mods to use to allow items to appear in store rotations, or to simply add them to the list of permanent items.
- Modified `RotateShipDecorSelectionPatch` and `UnlockShipObjectPatches` to use the new API.
- Tweaked a few things with the config file.
  - Orphaned nodes (old entries) are now cleared when launching the game.
  - Configuration file no longer saves after every single `Bind()` call, which could very slightly impact loading performance.
  - Renamed `stockPurchased` setting to `removePurchased`, and inverted its function.
- Fixed a desync that could happen with the `removePurchased` setting enabled when joining a lobby with already-purchased items, or items placed in storage.
  - Added transpiler for `StartOfRound.SyncShipUnlockablesServerRpc()` to actually fill the `storedItems` list before sending it to all clients.
    - When an item is put into storage, its `PlaceableShipObject` instance is despawned, but `StartOfRound.SyncShipUnlockablesServerRpc()` uses `Object.FindObjectsOfType<PlaceableShipObject>()` to fill the `storedItems` list, so it ends up always being empty.
  - Added postfix for `SyncShipUnlockablesClientRpc()` to update each of the ship unlockables' `hasBeenUnlockedByPlayer` and `inStorage` fields on all clients.
  - Ship purchases, as well as ship objects in storage, should now be properly synced upon joining a lobby.
  - This also fixes clients not being able to see items in storage when joining a lobby, though there's probably a better way to implement it.
- Refactored patch for `StartOfRound.UnlockShipObject()` a bit.

## [2.4.1]

Confirming compatibility with v61, but also a minor fix.

- Everything seems to be working correctly in `v61`, but I'll keep an eye out if anything breaks in the latest updates.
- Fixed `relativeScroll` setting being accidentally inverted in the previous release.

## [2.4.0]

Basic API added for rotation sales, enabled 'Nullable' in the project file.

- Created `RotationSalesAPI` class, which contains several helper methods for interacting with the rotation sales system.
  - Meant for other mods to use to check if an item is on sale, obtain its discount value and discounted price, add or remove discounts for specific items, among other things.
- Modified `TerminalItemSalesPatches` and `TerminalFormatterCompatibility` to use the new API.
- Enabled `Nullable` value types in the `.csproj` file so the compiler can yell at me if I forget a null check somewhere.
  - Should fix any current and (hopefully) future issues regarding null types (e.g. issue [#3](https://github.com/pacoito123/LC_StoreRotationConfig/issues/3)).
- Added `terminalFormatterCompat` setting to toggle compatibility with [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter).
  - This setting will be removed soon-ish, once compatibility is handled from their end.

## [2.3.3]

Miscellaneous fixes for various issues.

- Purchasing items now immediately removes from the permanent item list (**NOT** the config file's `itemWhitelist` itself) when `stockPurchased` is set to disabled.
- Made `saleChance` determine sales likelihood more accurately now.
  - My original goal was to recreate how the vanilla game handles sales, only parameterized to allow for configuration; however, due to integer rounding, a sales chance of e.g. `85%` and above could end up being exactly the same as `100%`, depending on the `maxSaleItems` setting.
- Fixed `Terminal.TextPostProcess()` transpiler occasionally replacing other items' displayed prices when appending a sale tag to a rotating item.
  - Also simplified it significantly by doing what was used for the [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter) compatibility transpiler.
- Made `minDiscount` be used for `maxDiscount` when `minDiscount` is greater than `maxDiscount`, just like the other range settings.
- Added a couple more null checks and debug messages.

## [2.3.2]

Compatibility with TerminalFormatter's modified store, among other things.

- Added [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter) as a soft dependency.
  - _I think?_
- Sales for rotating items should now display with [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter) installed.
  - Added transpiler for `Store.GetNodeText()` to display discounted prices and amounts in the store page whenever an item is on sale.
  - Temporary fix until proper compatibility can be made.
- `relativeScroll` now unpatches itself if [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter) is installed, since it already includes it.
- Patching is done upon loading into the main menu for the first time.

## [2.3.1]

Compatibility patch for Lategame Upgrades (and likely other moon-routing price adjustment mods).

- Fixed compatibility with [Lategame Upgrades](https://thunderstore.io/c/lethal-company/p/malco/Lategame_Upgrades)' `Efficient Engines` upgrade.
  - `Terminal.LoadNewNodeIfAffordable()` transpiler no longer removes instructions or touches `Terminal.totalCostOfItems` when routing to a moon.
  - Changed priority of `Terminal.LoadNewNodeIfAffordable()` to `High` (600), so it's applied earlier.

## [2.3.0]

Configurable item sales, whitelisting, and blacklisting.

- Implemented configurable sales for items in the rotating shop.
  - Added postfix for `Terminal.RotateShipDecorSelection()` to assign rotating item sales after every quota.
  - Added transpiler for `Terminal.TextPostProcess()` to show discounts in the store page.
    - Compatibility patches might be needed for items that also modify the store page (e.g. `TerminalFormatter`); further testing is needed.
  - Added transpiler for `Terminal.LoadNewNodeIfAffordable()` to actually apply the rotating item discounts right before a purchase.
  - Configuration settings added:
    - `saleChance` determines the likelihood for ANY item to be on sale in every store rotation, with the value `0` disabling the entire sale system.
    - `minSaleItems` and `maxSaleItems` control the number of items that can be on sale at a time.
    - `minDiscount` and `maxDiscount` control the amount an item can be discounted by.
    - `roundToNearestTen` rounds sale amounts to the nearest ten (like the regular store).
- Added both a whitelist and a blacklist for items available in the rotating shop.
  - `itemWhitelist` guarantees an item to be in stock every rotation, while `itemBlacklist` prevents them from ever showing up.
  - Both config settings are a comma-separated list of the exact item names shown in the terminal store page.
  - `itemWhitelist` will take priority over `itemBlacklist`, if an item is found in both lists.
  - `itemWhitelist` adds items separate from the range specified by the `minItems` and `maxItems` settings.
  - If an item name is not found, it will fail silently, but shouldn't cause any further issues.
- Minor fixes for `Terminal.RotateShipDecorSelection()` patch.
  - `maxItems` value is now used instead of `minItems` when `minItems` is greater than `maxItems`, as intended.
  - Fixed items not sorting alphabetically with `stockAll` disabled.
  - Alphabetical sort now uses `TerminalNode.creatureName` (name displayed in the store page) instead of `UnlockableItem.unlockableName`.
- Changed default value for `relativeScroll` to enabled.
- Everything should still be compatible with every game version since `v45`.
- Updated `LICENSE` name and year.

## [2.2.1]

Compatibility with v56, previous game versions should still work.

- Updated `StartOfRound.SyncShipUnlockablesClientRpc()` patch target reference to not include any parameters.
  - Additional parameter `vehicleID` was added in `v55`, and each parameter was previously declared explicitly in the patch (despite not actually using any).
- Changed log level of plugin load message to `Info`.

## [2.2.0]

Transpilers now used in place of some prefix patches, some minor refactoring.

- Switched to using Transpilers for `Terminal.RotateShipDecorSelection()` and `PlayerControllerB.ScrollMouse_performed()` patches.
  - Should be much better for compatibility with any other mods that might potentially want to patch these methods as well.
  - From initial testing, everything seems to be working fine, but please let me know if any issues are encountered.
- Changed `maxItems` and `minItems` to use their absolute values when rotating the store, to avoid any issues with negative numbers in the configuration file.
- Modifying `linesToScroll` in-game (e.g. through `LethalConfig`) should now apply changes immediately, instead of until after scrolling on a different terminal page.
- Updated minimum `CSync` library dependency to patch `v5.0.1`.
  - Previous release also works with `v5.0.1`, and is recommended.

## [2.1.0]

Update to 'CSync' v5, more configuration for terminal scrolling.

- Updated `CSync` library dependency to `v5.0.0`.
  - Updated `README.md` notes regarding `CSync` version compatibility.
- Added `linesToScroll` client-side setting (20 by default).
  - Determines the number of lines the terminal should scroll at a time.
  - Requires `relativeScroll` to be enabled.

## [2.0.1]

Fixes for 'stockPurchased' and 'relativeScroll' settings.

- Purchased items should now properly sync between clients when `stockPurchased` is set to disabled.
  - `Terminal.RotateShipDecorSelection()` now waits until after `StartOfRound.SyncShipUnlockablesClientRpc()` is executed when first joining a lobby.
  - Newly-purchased items should now be properly removed from store rotations for every client, not just the host.
- `relativeScroll` scroll amount should no longer apply an additional time for each player in the lobby.
  - `GameNetworkManager.Instance.localPlayerController` was being used instead of the actual `PlayerControllerB` instance calling `PlayerControllerB.ScrollMouse_performed()`.
- Added a few additional messages to print to console, mostly for debugging purposes.

## [2.0.0]

Update to 'CSync' v4; support for v3 relegated to previous release.

- Updated `CSync` library dependency requirement to `v4.1.0`, making it compatible with other mods that use `CSync v4`.
- Added note to `README.md` suggesting downgrading to `v1.3.0` if `CSync v3` is needed.
- Added commented code in case `CSync` reimplements the ability to join a lobby with either the client or host missing this mod.

## [1.3.0]

Added setting to configure whether already-purchased items should show up in the store rotation.

- Added `stockPurchased` server-side setting (on by default).
  - Determines whether or not to include already-purchased items in both the current store rotation and any future ones.
  - Also immediately removes them from the current store rotation, if disabled.
- Next release will target `CSync v4`, this specific version can be downgraded to if `CSync v3` compatibility is required.

## [1.2.0]

Minor (optional) tweak to terminal scrolling.

- Added experimental `relativeScroll` client-side setting (off by default).
  - Adapts the terminal scroll amount to the number of lines in the current terminal page.
  - Should fix an edge case where scrolling would skip over several lines if there were too many items in the store rotation.
- Changed `sortItems` setting to be client-side instead of synced.
- Added note to `README.md` regarding using `CSync v4` with this mod.

## [1.1.2]

Minor adjustments to project and code.

- Switched from using `Config.Instance` to the actual `Config` instance in the `Plugin` class, mainly in preparation for eventually updating to `CSync v4`.
- Clarified some stuff in `README.md`, mostly in the compatibility section.
  - Verified compatibility with both `v45` _and_ `v50` (only `v49` had been previously tested).
  - Added mention of the `Terminal.ShipDecorSelection` list intentionally not being modified when joining a lobby without the mod installed on the host computer.
- Modified `StoreRotationConfig.csproj` to include additional features.
  - Debug symbols are now embedded into the built `StoreRotationConfig.dll` plugin.
    - Enables better stack traces without having to include `StoreRotationConfig.pdb` in every release.
    - Also hides user file paths, in case that's a concern when reporting a bug.
  - `LICENSE` file ([MIT](https://github.com/pacoito123/LC_StoreRotationConfig/blob/v1.1.2/LICENSE)) is now packaged into the built `StoreRotationConfig.dll` plugin.
  - `IDE0051` warning is now ignored (to remove unused warning from `Plugin.Awake()` method).
- Added a few more missing comments.
  - Added comments to the individual configuration entries in `Config` class.
  - Added comments briefly describing the `Config`, `Plugin`, and `RotateShipDecorSelection` classes.

## [1.1.1]

Switch to 'CSync' library fork.

- Switched to the [CSync fork](https://thunderstore.io/c/lethal-company/p/Sigurd/CSync/) maintained by [Lordfirespeed](https://github.com/Lorefirespeed).
  - Thanks for the heads-up ([#1](https://github.com/pacoito123/LC_StoreRotationConfig/issues/1)), [Fl4sHNox](https://github.com/Fl4sHNoX)!

## [1.1.0]

Configuration now done through 'CSync' library; some refactoring and tweaks.

- `Config` class now uses [CSync](https://thunderstore.io/c/lethal-company/p/Owen3H/CSync/) by [Owen3H](https://github.com/Owen3H) to ensure parity between host and clients.
  - Now the patched `Terminal.RotateShipDecorSelection()` method only runs for clients **after** receiving config settings from the host, otherwise it simply executes the vanilla method.
    - This makes it no longer possible to purchase additional items in servers that either don't have this mod, or don't have the same config settings as the client (e.g. if `stockAll` setting is set to `true` on client-side only).
  - There should be no need to delete the old config file, names were kept the same.
- Added `sortItems` option to display the rotating store in alphabetical order.
  - If used with `stockAll`, the sorted list is cached to avoid having to generate it more than once.
- Changed most list iterations to use lambda expressions (arrow functions).
- Split `Plugin.cs` into several files, for organizational purposes.
- Updated `README.md` to contain more information.
- Added comments all throughout the source code.

## [1.0.1]

Hotfix for 'stockAll' setting.

- Fixed indexing error, `stockAll` setting should work properly now.
  - Forgot it wasn't a standard iteration, and that items in the list were removed with every cycle, so it would inevitably try to access an index out of bounds as `i` increased.
  - More changes were made changing the version number than fixing this issue...

## [1.0.0]

Initial release.

- Config file created, along with `Config` class.
- Options `minItems`, `maxItems`, and `showAll` added to `Config` class.
- Implemented patch for `RotateShipDecorSelection()` method in `Terminal` class.
- Added additional checks (e.g. if `minItems` is greater than `maxItems`).
- Made `maxItems` upper bound inclusive (`Random.Next(minValue, maxValue)` isn't).
- Added icon, changelog, and manifest files.
