# StoreRotationConfig

> Configure the number of purchasable items in each store rotation.

## Description

Simple mod that adds configurability to the number of items that show up in the store every week.

Intended for when there's a large number of modded items (suits, furniture, etc.) in the store, and the vanilla store rotation makes it too unlikely to ever see a desired item in stock.

## Configuration

By default, the number of available items in the store is increased from **4-5** (vanilla) to **8-12**, but this range can be configured. Set both numbers to the same value to have a fixed number of items every time.

There's also a setting to have every possible item in stock, but it hasn't been thoroughly tested and could be slow or cause issues with a large number of items. It might be useful for debugging name conflict issues when trying to buy an item, but should probably not be enabled in an actual game.

## Compatibility

The patched `Terminal.RotateShipDecorSelection()` method is functionally the same code as vanilla, only with some configurability added, so it _should_ play nicely with other mods (as long as they don't also clear the `Terminal.ShipDecorSelection` list, or forcibly add their own items without checking if they're already present in it).

**NOTE:** This mod should _probably_ be installed on both clients and the host, just to be safe; further testing is needed to determine if any desync issues occur when only the host or clients have mod active.

---

![alt](https://files.catbox.moe/v3y5tp.png "Store with 10 items available for purchase.")