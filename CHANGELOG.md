### [2.2.0]

Transpilers now used in place of some prefix patches, some minor refactoring.
- Switched to using Transpilers for `Terminal.RotateShipDecorSelection()` and `PlayerControllerB.ScrollMouse_performed()` patches.
	- Should be much better for compatibility with any other mods that might potentially want to patch these methods as well.
	- From initial testing, everything seems to be working fine, but please let me know if any issues are encountered.
- Changed `maxItems` and `minItems` to use their absolute values when rotating the store, to avoid any issues with negative numbers in the configuration file.
- Modifying `linesToScroll` in-game (e.g. through `LethalConfig`) should now apply changes immediately, instead of until after scrolling on a different terminal page.
- Updated minimum `CSync` library dependency to patch `v5.0.1`.
	- Previous release also works with `v5.0.1`, and is recommended.

### [2.1.0]

Update to 'CSync' v5, more configuration for terminal scrolling.
- Updated `CSync` library dependency to `v5.0.0`.
	- Updated `README.md` notes regarding `CSync` version compatibility.
- Added `linesToScroll` client-side setting (20 by default).
	- Determines the number of lines the terminal should scroll at a time.
	- Requires `relativeScroll` to be enabled.

### [2.0.1]

Fixes for 'showPurchased' and 'relativeScroll' settings.
- Purchased items should now properly sync between clients when `showPurchased` is set to disabled.
	- `Terminal.RotateShipDecorSelection()` now waits until after `StartOfRound.SyncShipUnlockablesClientRpc()` is executed when first joining a lobby.
	- Newly-purchased items should now be properly removed from store rotations for every client, not just the host.
- `relativeScroll` scroll amount should no longer apply an additional time for each player in the lobby.
	- `GameNetworkManager.Instance.localPlayerController` was being used instead of the actual `PlayerControllerB` instance calling `PlayerControllerB.ScrollMouse_performed()`.
- Added a few additional messages to print to console, mostly for debugging purposes.

### [2.0.0]

Update to 'CSync' v4; support for v3 relegated to previous release.
- Updated `CSync` library dependency requirement to `v4.1.0`, making it compatible with other mods that use `CSync v4`.
- Added note to `README.md` suggesting downgrading to `v1.3.0` if `CSync v3` is needed.
- Added commented code in case `CSync` reimplements the ability to join a lobby with either the client or host missing this mod.

### [1.3.0]

Added setting to configure whether already-purchased items should show up in the store rotation.
- Added `showPurchased` server-side setting (on by default).
	- Determines whether or not to include already-purchased items in both the current store rotation and any future ones.
	- Also immediately removes them from the current store rotation, if disabled.
- Next release will target `CSync v4`, this specific version can be downgraded to if `CSync v3` compatibility is required.

### [1.2.0]

Minor (optional) tweak to terminal scrolling.
- Added experimental `relativeScroll` client-side setting (off by default).
	- Adapts the terminal scroll amount to the number of lines in the current terminal page.
	- Should fix an edge case where scrolling would skip over several lines if there were too many items in the store rotation.
- Changed `sortItems` setting to be client-side instead of synced.
- Added note to `README.md` regarding using `CSync v4` with this mod.

### [1.1.2]

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

### [1.1.1]

Switch to 'CSync' library fork.
- Switched to the [CSync fork](https://thunderstore.io/c/lethal-company/p/Owen3H/CSync/) maintained by [Lordfirespeed](https://github.com/Lorefirespeed).
	- Thanks for the heads-up ([#1](https://github.com/pacoito123/LC_StoreRotationConfig/issues/1)), [Fl4sHNox](https://github.com/Fl4sHNoX)!

### [1.1.0]

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

### [1.0.1]

Hotfix for 'stockAll' setting.
- Fixed indexing error, `stockAll` setting should work properly now.
	- Forgot it wasn't a standard iteration, and that items in the list were removed with every cycle, so it would inevitably try to access an index out of bounds as `i` increased.
	- More changes were made changing the version number than fixing this issue...

### [1.0.0]

Initial release.
- Config file created, along with `Config` class.
- Options `minItems`, `maxItems`, and `showAll` added to `Config` class.
- Implemented patch for `RotateShipDecorSelection()` method in `Terminal` class.
- Added additional checks (e.g. if `minItems` is greater than `maxItems`).
- Made `maxItems` upper bound inclusive (`Random.Next(minValue, maxValue)` isn't).
- Added icon, changelog, and manifest files.