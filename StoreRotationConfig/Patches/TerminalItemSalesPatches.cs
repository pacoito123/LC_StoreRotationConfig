using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Netcode;

using static StoreRotationConfig.Api.RotationSalesAPI;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for adding sales to the store rotation.
    /// </summary>
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalItemSalesPatches
    {
        [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyPostfix]
        private static void SetRotationSales(Terminal __instance)
        {
            // Return if 'saleChance' setting is disabled (set to '0').
            if (Plugin.Settings == null || Plugin.Settings.SALE_CHANCE == 0)
            {
                return;
            }

            // Return if client has not yet fully synced with the host.
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer && !SyncShipUnlockablesPatch.UnlockablesSynced)
            // && !Plugin.Settings.ConfigSynced)
            {
                Plugin.StaticLogger?.LogInfo("Waiting for sync from server before assigning sales...");

                return;
            }

            // Initialize 'Random' instance using the same seed as vanilla sales.
            Random random = new(StartOfRound.Instance.randomMapSeed + 90);

            // Return if no items are on sale for this rotation.
            if (random.Next(0, 100) > Plugin.Settings.SALE_CHANCE - 1)
            {
                Plugin.StaticLogger?.LogInfo("No items on sale for this rotation...");

                return;
            }

            // Obtain values from the config file.
            int minSaleItems = Math.Abs(Plugin.Settings.MIN_SALE_ITEMS),
                maxSaleItems = Math.Abs(Plugin.Settings.MAX_SALE_ITEMS);
            int minDiscount = Plugin.Settings.MIN_DISCOUNT,
                maxDiscount = Plugin.Settings.MAX_DISCOUNT;
            // ...

            // Use 'minSaleItems' for 'maxSaleItems', if the former is greater than the latter.
            if (minSaleItems > maxSaleItems)
            {
                Plugin.StaticLogger?.LogWarning("Value for 'minSaleItems' is larger than 'maxSaleItems', using it instead...");

                maxSaleItems = minSaleItems;
            }

            // Use 'minSaleItems' for 'maxDiscount', if the former is greater than the latter.
            if (minDiscount > maxDiscount)
            {
                Plugin.StaticLogger?.LogWarning("Value for 'minDiscount' is larger than 'maxDiscount', using it instead...");

                maxDiscount = minDiscount;
            }

            // Obtain number of items with sales for this rotation.
            int itemsOnSale = random.Next(minSaleItems, maxSaleItems + 1);

            // Return if no items are on sale for this rotation.
            if (itemsOnSale <= 0)
            {
                Plugin.StaticLogger?.LogInfo("No items on sale for this rotation...");

                return;
            }

            // Initialize 'RotationSales' dictionary with its capacity set to however many items are to be on sale.
            ResetSales(itemsOnSale);

            // Clone the 'Terminal.ShipDecorSelection' list for item selection.
            List<TerminalNode> storeRotation = new(__instance.ShipDecorSelection);

            // Iterate for every item that is to be on sale, exiting early if there are no more items in the 'storeRotation' cloned list.
            for (int i = 0; i < itemsOnSale && storeRotation.Count != 0; i++)
            {
                // Obtain random discount value to apply.
                int discount = random.Next(minDiscount, maxDiscount + 1);

                // Round discount to the nearest ten (like the regular store) if the 'roundToNearestTen' setting is enabled.
                if (Plugin.Settings.ROUND_TO_NEAREST_TEN)
                {
                    discount = (int)Math.Round(discount / 10.0f) * 10;
                }

                // Obtain random index of the item to apply the discount to.
                int index = random.Next(0, storeRotation.Count);

                // Register item discount and remove it from the 'storeRotation' cloned list.
                _ = AddItemDiscount(storeRotation[index], discount);
                storeRotation.RemoveAt(index);
            }

            Plugin.StaticLogger?.LogInfo($"{CountSales()} items on sale!");
        }

        /// <summary>
        ///     Applies a rotating item's discount (if it has one assigned in the current rotation) right before its purchase.
        /// </summary>
        ///     ... (Terminal:630)
        ///     else if (node.buyRerouteToMoon != -1 || node.shipUnlockableID != -1)
        ///     {
        ///         this.totalCostOfItems = node.itemCost;
        ///         
        ///         -> this.totalCostOfItems = call(node, this.totalCostOfItems);
        ///     }
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyPriority(Priority.High)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TerminalLoadNewNodeIfAffordableTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldfld, AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.itemCost))),
                new(OpCodes.Stfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .Advance(2)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .InsertAndAdvance(Transpilers.EmitDelegate((TerminalNode node, int totalCostOfItems) =>
                {
                    // Leave total cost of purchase unchanged if routing to a moon.
                    if (node.buyRerouteToMoon != -1)
                    {
                        return totalCostOfItems;
                    }

                    // Obtain item currently selected for purchase.
                    UnlockableItem? item = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID];

                    // Return if 'salesChance' is disabled OR the 'RotationSales' dictionary doesn't contain a discount for the currently selected item.
                    if (Plugin.Settings == null || Plugin.Settings.SALE_CHANCE == 0 || !IsOnSale(item.shopSelectionNode))
                    {
                        return totalCostOfItems;
                    }

                    // Obtain discounted item price and discount value.
                    int price = GetDiscountedPrice(item.shopSelectionNode, out int discount);

                    Plugin.StaticLogger?.LogDebug($"Applying discount of {discount}% to '{item.shopSelectionNode.creatureName}'...");

                    // Apply discount to the total cost of the purchase.
                    return price;
                }))
            .Insert(new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .InstructionEnumeration();
        }

        /// <summary>
        ///     Displays rotating item discounts and their modified prices in the store page.
        /// </summary>
        ///     ... (Terminal:344)
        ///     for (int m = 0; m &lt; this.ShipDecorSelection.Count; m++)
        ///     {
        ///         stringBuilder5.Append(string.Format("\n{0}  //  ${1}", this.ShipDecorSelection[m].creatureName,
        ///             // this.ShipDecorSelection[m].itemCost));
        ///             -> call(this.ShipDecorSelection)));
        ///     }
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch("TextPostProcess")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TextPostProcessTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldstr, "\n{0}  //  ${1}"),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.ShipDecorSelection))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.itemCost))))
            .SetInstructionAndAdvance(Transpilers.EmitDelegate((TerminalNode item) =>
                {
                    // Return string containing full cost if 'salesChance' is disabled OR the item about to be displayed isn't currently on sale.
                    if (Plugin.Settings == null || Plugin.Settings.SALE_CHANCE == 0 || !IsOnSale(item, out int discount))
                    {
                        return $"{item.itemCost}";
                    }

                    Plugin.StaticLogger?.LogDebug($"Appending sale tag of '{discount}%' to {item.creatureName}...");

                    // Return string containing the discounted price and discount amount to display in the store page. 
                    return GetTerminalString(item);
                }))
            .SetOperandAndAdvance(typeof(string))
            .InstructionEnumeration();
        }
    }
}