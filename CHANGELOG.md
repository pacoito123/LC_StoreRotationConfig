### [1.0.0]

Initial release.
- Config file created, along with `Config` class.
- Options `minItems`, `maxItems`, and `showAll` added to `Config` class.
- Implemented patch for `RotateShipDecorSelection()` method in `Terminal` class.
- Added additional checks (e.g. if `minItems` is greater than `maxItems`).
- Made `maxItems` upper bound inclusive (`Random.Next(minValue, maxValue)` isn't).
- Added icon, changelog, and manifest files.