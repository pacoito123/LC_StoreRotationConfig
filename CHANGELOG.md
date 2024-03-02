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