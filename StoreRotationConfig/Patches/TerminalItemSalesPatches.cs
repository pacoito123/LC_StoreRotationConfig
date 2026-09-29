using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using static StoreRotationConfig.Api.RotationSalesAPI;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for adding sales to the store rotation.
    /// </summary>
    internal static class TerminalItemSalesPatches
    {
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyPostfix]
        private static void SetRotationSales_Postfix(Terminal __instance)
        {
            if (!__instance.IsHost)
            {
                return;
            }

            // Return if 'saleChance' setting is disabled (set to '0').
            if (Plugin.Settings.SALE_CHANCE.Value == 0)
            {
                return;
            }

            // Initialize 'Random' instance using the same seed as vanilla sales.
            Random random = new(StartOfRound.Instance.randomMapSeed + 90);

            // Return if no items are on sale for this rotation.
            if (random.Next(0, 100) > Plugin.Settings.SALE_CHANCE.Value - 1)
            {
                Plugin.Logger.LogInfo("No items on sale for this rotation...");

                return;
            }

            // Obtain values from the config file.
            int minSaleItems = Math.Abs(Plugin.Settings.MIN_SALE_ITEMS.Value),
                maxSaleItems = Math.Abs(Plugin.Settings.MAX_SALE_ITEMS.Value);
            int minDiscount = Plugin.Settings.MIN_DISCOUNT.Value,
                maxDiscount = Plugin.Settings.MAX_DISCOUNT.Value;
            // ...

            // Use 'minSaleItems' for 'maxSaleItems', if the former is greater than the latter.
            if (minSaleItems > maxSaleItems)
            {
                Plugin.Logger.LogWarning("Value for 'minSaleItems' is larger than 'maxSaleItems', using it instead...");

                maxSaleItems = minSaleItems;
            }

            // Use 'minSaleItems' for 'maxDiscount', if the former is greater than the latter.
            if (minDiscount > maxDiscount)
            {
                Plugin.Logger.LogWarning("Value for 'minDiscount' is larger than 'maxDiscount', using it instead...");

                maxDiscount = minDiscount;
            }

            // Obtain number of items with sales for this rotation.
            int itemsOnSale = random.Next(minSaleItems, maxSaleItems + 1);

            // Return if no items are on sale for this rotation.
            if (itemsOnSale <= 0)
            {
                Plugin.Logger.LogInfo("No items on sale for this rotation...");

                return;
            }

            // Clear 'RotationSales' dictionary.
            ClearSales();

            // Clone the 'Terminal.ShipDecorSelection' list for item selection.
            List<TerminalNode> storeRotation = [.. __instance.ShipDecorSelection];

            // Iterate for every item that is to be on sale, exiting early if there are no more items in the 'storeRotation' cloned list.
            for (int i = 0; i < itemsOnSale && storeRotation.Count != 0; i++)
            {
                // Obtain random discount value to apply.
                int discount = random.Next(minDiscount, maxDiscount + 1);

                // Round discount to the nearest ten (like the regular store) if the 'roundToNearestTen' setting is enabled.
                if (Plugin.Settings.ROUND_TO_NEAREST_TEN.Value)
                {
                    discount = (int)Math.Round(discount / 10.0f) * 10;
                }

                // Obtain random index of the item to apply the discount to.
                int index = random.Next(0, storeRotation.Count);

                // Register item discount and remove it from the 'storeRotation' cloned list.
                _ = AddItemDiscount(storeRotation[index], discount);
                storeRotation.RemoveAt(index);
            }

            Plugin.Logger.LogInfo($"{CountSales()} items on sale!");
        }

        /// <summary>
        ///     Applies a rotating item's discount (if it has one assigned in the current rotation) right before its purchase.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         else if (node.buyRerouteToMoon != -1 || node.shipUnlockableID != -1)
        ///         {
        ///             this.totalCostOfItems = node.itemCost;
        ///             -> this.totalCostOfItems = TerminalItemSalesPatches.ApplyDiscount(node, this.totalCostOfItems);
        ///         }
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNodeIfAffordable))]
        [HarmonyPriority(Priority.High)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TerminalLoadNewNodeIfAffordable_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo itemCostInfo = typeof(TerminalNode).GetField(nameof(TerminalNode.itemCost), BindingFlags.Instance | BindingFlags.Public);
            FieldInfo totalCostOfItemsInfo = typeof(Terminal).GetField(nameof(Terminal.totalCostOfItems), BindingFlags.Instance | BindingFlags.NonPublic);
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, itemCostInfo),
                new(OpCodes.Stfld, totalCostOfItemsInfo));

            if (codeMatcher.Advance(1).IsInvalid)
            {
                Plugin.Logger.LogError("Could not match Terminal 'totalCostOfItems' field assignment.");

                return instructions;
            }

            MethodInfo applyDiscountInfo = typeof(TerminalItemSalesPatches).GetMethod(nameof(ApplyDiscount), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.Insert(
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, totalCostOfItemsInfo),
                new(OpCodes.Call, applyDiscountInfo))
            .InstructionEnumeration();
        }

        private static void ApplyDiscount(TerminalNode node, ref int totalCostOfItems)
        {
            // Return if routing to a moon, or unlockable ID is invalid.
            if (node.buyRerouteToMoon != -1 || StartOfRound.Instance == null || StartOfRound.Instance.unlockablesList == null
                || StartOfRound.Instance.unlockablesList.unlockables == null || StartOfRound.Instance.unlockablesList.unlockables.Count <= node.shipUnlockableID)
            {
                return;
            }

            // Obtain item currently selected for purchase.
            UnlockableItem? item = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID];

            // Return if selected item was not found.
            if (item == null)
            {
                Plugin.Logger.LogError($"Unlockable item at index {node.shipUnlockableID} missing!");

                return;
            }

            // Return if 'salesChance' is disabled OR the 'RotationSales' dictionary doesn't contain a discount for the currently selected item.
            if (Plugin.Settings == null || Plugin.Settings.SALE_CHANCE.Value == 0 || !IsOnSale(item.shopSelectionNode))
            {
                return;
            }

            // Obtain discounted item price and discount value.
            totalCostOfItems = GetDiscountedPrice(item.shopSelectionNode, out int discount);

            Plugin.Logger.LogDebug($"Applying discount of {discount}% to '{item.shopSelectionNode.creatureName}'...");
        }

        /// <summary>
        ///     Displays rotating item discounts and their modified prices in the store page.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         for (int m = 0; m &lt; this.ShipDecorSelection.Count; m++)
        ///         {
        ///             stringBuilder5.Append(string.Format("\n{0}  //  Price: ${1}", this.ShipDecorSelection[m].creatureName,
        ///                 // this.ShipDecorSelection[m].itemCost
        ///                 -> TerminalItemSalesPatches.AppendDiscountTag(this.ShipDecorSelection)
        ///             ));
        ///         }
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.TextPostProcess))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TextPostProcessTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: false,
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldstr, "[unlockablesSelectionList]"),
                new(OpCodes.Ldstr, "[No items available]"));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match '[unlockablesSelectionList]' parameter in store page.");

                return instructions;
            }

            FieldInfo itemCostInfo = typeof(TerminalNode).GetField(nameof(TerminalNode.itemCost), BindingFlags.Instance | BindingFlags.Public);
            _ = codeMatcher.MatchForward(useEnd: false,
                new(OpCodes.Ldfld, itemCostInfo),
                new(OpCodes.Box, typeof(int)));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match TerminalNode 'itemCost' field parameter in store page item display.");

                return instructions;
            }

            MethodInfo appendDiscountTagInfo = typeof(TerminalItemSalesPatches).GetMethod(nameof(AppendDiscountTag), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.SetInstructionAndAdvance(
                new(OpCodes.Call, appendDiscountTagInfo))
            .RemoveInstruction()
            .InstructionEnumeration();
        }

        private static string AppendDiscountTag(TerminalNode item)
        {
            // Return string containing full cost if 'salesChance' is disabled OR the item about to be displayed isn't currently on sale.
            if (Plugin.Settings == null || Plugin.Settings.SALE_CHANCE.Value == 0 || !IsOnSale(item, out int discount))
            {
                return $"{item.itemCost}";
            }

            Plugin.Logger.LogDebug($"Appending sale tag of '{discount}%' to {item.creatureName}...");

            // Return string containing the discounted price and discount amount to display in the store page. 
            return GetTerminalString(item);
        }
    }
}