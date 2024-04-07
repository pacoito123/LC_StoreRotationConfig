# StoreRotationConfig

[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/pacoito/StoreRotationConfig?style=for-the-badge&logo=thunderstore&color=mediumseagreen
)](https://thunderstore.io/c/lethal-company/p/pacoito/StoreRotationConfig/)
[![GitHub Releases](https://img.shields.io/github/v/release/pacoito123/LC_StoreRotationConfig?display_name=tag&style=for-the-badge&logo=github&color=steelblue
)](https://github.com/pacoito123/LC_StoreRotationConfig/releases)
[![License](https://img.shields.io/github/license/pacoito123/LC_StoreRotationConfig?style=for-the-badge&logo=github&color=teal
)](https://github.com/pacoito123/LC_StoreRotationConfig/blob/main/LICENSE)

> Configure the number of purchasable items in each store rotation, or simply show them all.

## Description

Simple mod that adds configurability to the number of items that show up in the store every week.

Intended for when there's a large number of modded items (suits, furniture, etc.) in the store, and the vanilla store rotation makes it too unlikely to ever see a desired item in stock.

Compatible with `v45`, `v49`, and `v50`.

Uses [CSync (v3.1.1 and below)](https://thunderstore.io/c/lethal-company/p/Sigurd/CSync) by [Owen3H](https://github.com/Owen3H) (forked by [Lordfirespeed](https://github.com/Lorefirespeed)) to sync config settings between host and clients.

## Configuration

By default, the number of available items in the store is increased from **4-5** (vanilla) to **8-12**, but this range can be configured via the `minItems` and `maxItems` config settings. Set both numbers to the same value to have a fixed number of items in every rotation.

Alternatively, the `showAll` setting can be toggled to simply add every purchasable item to the store rotation. Partly intended for fixing name conflict issues when buying stuff at the terminal, but there should be no problems using it during a regular run.

The store rotation can also be displayed in alphabetical order by toggling the `sortItems` setting.

## Compatibility

The patched `Terminal.RotateShipDecorSelection()` method is functionally the same as vanilla, only with some configurability added, so it _should_ play nicely with other mods (as long as they don't also clear the `Terminal.ShipDecorSelection` list to generate their own, or forcibly add items without checking if they're already present in it).

There's also the possibility of something going wrong if the `Terminal.ShipDecorSelection` list is needed by another mod immediately after joining a lobby, but prior to the config file sync; the patched method to fill the list with additional items only runs _after_ a successful sync, so it remains in its vanilla state until then. So far I haven't encountered any issues with it, but if any incompatibilities _are_ found, please let me know in the [relevant thread](https://discord.com/channels/1168655651455639582/1212542584610881557) in the Lethal Company Modding Discord server, or [open an issue on GitHub](https://github.com/pacoito123/LC_StoreRotationConfig/issues).

**NOTE:** This mod is _technically_ server-side, but clients need the mod installed to be able to see and purchase any of the additional items added to the vanilla store rotation. Similarly, joining a lobby that doesn't have this mod installed will not modify the store rotation.

---

![alt](https://files.catbox.moe/o35ptg.png "Store with every vanilla item available for purchase, in alphabetical order.")