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