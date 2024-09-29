# StoreRotationConfig

[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/pacoito/StoreRotationConfig?style=for-the-badge&logo=thunderstore&color=mediumseagreen
)](https://thunderstore.io/c/lethal-company/p/pacoito/StoreRotationConfig/)
[![GitHub Releases](https://img.shields.io/github/v/release/pacoito123/LC_StoreRotationConfig?display_name=tag&style=for-the-badge&logo=github&color=steelblue
)](https://github.com/pacoito123/LC_StoreRotationConfig/releases)
[![License](https://img.shields.io/github/license/pacoito123/LC_StoreRotationConfig?style=for-the-badge&logo=github&color=teal
)](https://github.com/pacoito123/LC_StoreRotationConfig/blob/main/LICENSE)

> Configure the number of items in each store rotation, show them all, remove purchases, sort them, and/or enable sales for them.

## Description

Simple mod that adds configurability to the number of items that show up in the store every week.

Intended for when there's a large number of modded items (suits, furniture, etc.) in the store, and the vanilla store rotation makes it too unlikely to ever see a desired item in stock.

Compatible with `v45`, `v49`, `v50`, `v56`, `v61`, and `v64`.

Uses [CSync (v5.0.1 and above)](https://thunderstore.io/c/lethal-company/p/Sigurd/CSync) by [Lordfirespeed](https://github.com/Lorefirespeed) to sync config settings between host and clients.

**NOTE:** In case an older version of CSync is needed, usually due to mods that have not yet updated to the latest versions, refer to the following table for which specific version of this mod to downgrade to:

| CSync           | StoreRotationConfig |
| :-------------: | :-----------------: |
| v5.0.1          | `v2.1.0+`           |
| v4.1.0          | `v2.0.1`            |
| v3.1.1          | `v1.3.0`            |

## Configuration

### General

By default, the number of available items in the store is increased from **4-5** (vanilla) to **8-12**, but this range can be configured via the `minItems` and `maxItems` config settings. Set both numbers to the same value to have a fixed number of items in every rotation. If `minItems` is larger than `maxItems`, both numbers are set to the larger value. To avoid any issues with negative numbers, the absolute value of these two settings is used when generating the store rotation.

Alternatively, the `showAll` setting (off by default) can be enabled to simply add every purchasable item to the store rotation. Partly intended for fixing name conflict issues when buying stuff at the terminal, but there should be no problems using it during a regular run.

Enabling the `removePurchased` setting (off by default) will prevent already-purchased items from showing up in future store rotations, and will also immediately remove newly-purchased items from the current rotation.

To guarantee an item showing up in the store rotation, its name can be added to the comma-separated `itemWhitelist` setting, which adds the specified items to every store rotation separate from the range of items defined by the `minItems` and `maxItems` settings. Likewise, to prevent items from ever showing up in the store rotation, its name can be added to `itemBlacklist`.

### Rotation sales

As of `v2.3.0`, items in the rotating shop can be configured to occasionally go on sale. By default, there's a **33% chance** for **1-5** items to go on sale with a discount ranging from **10-50%** and rounded to the nearest ten, but everything is configurable.

The `saleChance` setting controls the percentage chance for rotating items to go on sale, with the sales system disabling itself completely if set to **0**. The number of items that can be on sale at a time can be configured by the `minSaleItems` and `maxSaleItems` settings, and the amount that can be discounted can be configured by the `minDiscount` and `maxDiscount`. Whether or not discounts should be rounded to the nearest ten, like the regular store, is determined by the `roundToNearestTen` setting.

### Client-side tweaks

The store rotation can be displayed in alphabetical order by enabling the `sortItems` setting (off by default).

For cases where having too many items in the store rotation causes scrolling to skip over several lines, either with `stockAll` enabled or with a high `minItems`/`maxItems` value, enabling the `relativeScroll` setting (on by default) will adapt scrolling to a certain number of lines at a time, determined by the `linesToScroll` setting (20 by default), and relative to the length of the currently shown terminal page.

These settings are not synced with the host, and can be freely toggled without causing any issues.

## Compatibility

The patched `Terminal.RotateShipDecorSelection()` method is functionally the same as vanilla, only with some configurability added, so it _should_ play nicely with other mods (as long as they don't also clear the `Terminal.ShipDecorSelection` list to generate their own, or forcibly add items without checking if they're already present in it).

There's also the possibility of something going wrong if the `Terminal.ShipDecorSelection` list is required by another mod immediately after joining a lobby, but prior to the ship unlockables sync; the list is only filled _after_ a successful sync with the host, and it remains empty until then. So far I haven't encountered any issues with it, but if any incompatibilities _are_ found, please let me know in the [relevant thread](https://discord.com/channels/1168655651455639582/1212542584610881557) in the Lethal Company Modding Discord server, or [open an issue on GitHub](https://github.com/pacoito123/LC_StoreRotationConfig/issues).

The sales system is completely separate from the regular store item sales, so it _shouldn't_ conflict with other mods that may modify these sales (e.g. allowing more items to be on sale). What _is_ likely to break, however, is displaying the discount number in the store page (e.g. `50% OFF!`) if another mod is changing the terminal store page. The discount should still apply regardless, but I'll try to patch any incompatibilities found as soon as possible.

The `relativeScroll` tweak is not limited to just the store page, and could potentially fix scrolling issues in other terminal pages, but it could also be incompatible or cause issues with other mods that modify or set the `PlayerControllerB.terminalScrollVertical` value.

~~**NOTE:** This mod is _technically_ server-side, but clients need the mod installed to be able to see and purchase any of the additional items added to the vanilla store rotation. Similarly, joining a lobby that doesn't have this mod installed will not modify the store rotation.~~

**NOTE:** As of `v2.0.0`, this mod is now required to be installed on **both host and clients**, though I have commented code ready to once again make it (technically) server-side, if `CSync` reimplements the ability to join a lobby with either client or host missing a mod that depends on it.

---

![alt](https://files.catbox.moe/z3fzcw.png "Store rotation with every vanilla item available for purchase in v56 in alphabetical order, 4 of which are on sale.")
