using HarmonyLib;
using StoreRotationConfig.Networking;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for adding sales to the store rotation and applying their discounts.
    /// </summary>
    internal static class TerminalItemSalesPatches
    {
        /// <summary>
        ///     Obtain sales for the current rotation, if any are rolled.
        /// </summary>
        /// <param name="itemsInRotation">Number of items in the current rotation.</param>
        /// <returns>Array holding discount values for the current rotation, if any are rolled.</returns>
        internal static int[]? RollSales(int itemsInRotation)
        {
            if (Plugin.Settings == null)
            {
                Plugin.Logger.LogError("Configuration could not be loaded or is missing! Store rotation won't work...");

                return null;
            }

            // Obtain values from the config file.
            float saleChance = Math.Clamp(Plugin.Settings.SALE_CHANCE.Value, 0.0f, 100.0f);
            int minSaleItems = Math.Abs(Plugin.Settings.MIN_SALE_ITEMS.Value),
                maxSaleItems = Math.Abs(Plugin.Settings.MAX_SALE_ITEMS.Value);
            int minDiscount = Math.Clamp(Plugin.Settings.MIN_DISCOUNT.Value, 0, 100),
                maxDiscount = Math.Clamp(Plugin.Settings.MAX_DISCOUNT.Value, 0, 100);
            bool roundToNearestTen = Plugin.Settings.ROUND_TO_NEAREST_TEN.Value;
            // ...

            // Return if sales are disabled (set to '0').
            if (saleChance == 0)
            {
                return null;
            }

            // Initialize 'Random' instance using the same seed as vanilla sales.
            Random salesRandom = new(StartOfRound.Instance.randomMapSeed + 90);

            // Return if failed roll for any sales at all.
            if ((float)salesRandom.NextDouble() > saleChance / 100.0f)
            {
                Plugin.Logger.LogInfo("No items on sale for this rotation...");

                return null;
            }

            // Create array with discount values for this rotation.
            int[] sales = new int[itemsInRotation];

            // Use 'minSaleItems' for 'maxSaleItems', if the former is greater than the latter.
            if (minSaleItems > maxSaleItems)
            {
                Plugin.Logger.LogWarning("Value for 'minSaleItems' is larger than 'maxSaleItems', using it instead...");

                maxSaleItems = minSaleItems;
            }

            // Use 'minDiscount' for 'maxDiscount', if the former is greater than the latter.
            if (minDiscount > maxDiscount)
            {
                Plugin.Logger.LogWarning("Value for 'minDiscount' is larger than 'maxDiscount', using it instead...");

                maxDiscount = minDiscount;
            }

            // Obtain number of items with sales for this rotation.
            int itemsOnSale = salesRandom.Next(minSaleItems, maxSaleItems + 1),
                remainingItems = itemsOnSale;

            // Return if no items are on sale for this rotation.
            if (itemsOnSale <= 0)
            {
                Plugin.Logger.LogInfo("No items on sale for this rotation...");

                return null;
            }

            // Iterate for every item present in the store rotation.
            for (int i = 0; i < sales.Length && remainingItems != 0; i++)
            {
                if ((float)salesRandom.NextDouble() < ((float)remainingItems / (sales.Length - i)))
                {
                    // Obtain random discount value to apply.
                    int discount = salesRandom.Next(minDiscount, maxDiscount + 1);

                    // Round discount to the nearest ten (like the regular store) if configured to do so.
                    if (roundToNearestTen)
                    {
                        discount = (int)Math.Round(discount / 10.0f) * 10;
                    }

                    // Set discount at the current index.
                    sales[i] = discount;

                    // Set one less item to be given sales.
                    remainingItems--;
                }
            }

            Plugin.Logger.LogInfo($"{itemsOnSale - remainingItems} items on sale!");

            return sales;
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

        /// <summary>
        ///     Apply discount to the item being purchased, if it has one.
        /// </summary>
        /// <param name="unlockableNode"><c>TerminalNode</c> of the item being purchased.</param>
        /// <param name="totalCostOfItems">Price of the item being purchased, as a ref parameter.</param>
        private static void ApplyDiscount(TerminalNode unlockableNode, ref int totalCostOfItems)
        {
            // Return if routing to a moon.
            if (unlockableNode.buyRerouteToMoon != -1)
            {
                return;
            }

            List<TerminalNode>? shipDecorSelection = (Plugin.Terminal != null) ? Plugin.Terminal.ShipDecorSelection : null;

            if (shipDecorSelection == null)
            {
                return;
            }

            // Obtain index in the current store rotation for the purchased item.
            int rotationIndex = (!unlockableNode.buyUnlockable) ? shipDecorSelection.IndexOf(unlockableNode)
                : shipDecorSelection.FindIndex(node => node.shipUnlockableID == unlockableNode.shipUnlockableID);

            if (rotationIndex == -1)
            {
                return;
            }

            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError($"StoreRotationNetworker instance is missing! No discount could be applied to '{unlockableNode.creatureName}'...");

                return;
            }

            // Obtain synced information for the purchased item.
            StoreRotationEntry entry = StoreRotationNetworker.Instance.StoreRotation[rotationIndex];

            if (entry.UnlockableDiscount > 0)
            {
                // Set discounted price for the purchase, if there is a discount.
                totalCostOfItems = entry.GetDiscountedPrice(unlockableNode);

                Plugin.Logger.LogDebug($"Applying discount of '{entry.UnlockableDiscount}%' to '{unlockableNode.creatureName}': '{entry.UnlockablePrice}' -> '{totalCostOfItems}'");
            }
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
                new(OpCodes.Ldloc_S), // V_15
                new(OpCodes.Callvirt), // List<TerminalNode>.get_Item()
                new(OpCodes.Ldfld, itemCostInfo),
                new(OpCodes.Box, typeof(int)));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match TerminalNode 'itemCost' field parameter in store page item display.");

                return instructions;
            }

            CodeInstruction loadRotationIndex = codeMatcher.Instruction; // Ldloc.s V_15

            MethodInfo appendDiscountTagInfo = typeof(TerminalItemSalesPatches).GetMethod(nameof(AppendDiscountTag), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.Advance(2)
            .SetInstructionAndAdvance(loadRotationIndex)
            .SetInstruction(
                new(OpCodes.Call, appendDiscountTagInfo))
            .InstructionEnumeration();
        }

        /// <summary>
        ///     Append discount tag to an item being displayed in the <c>Terminal</c> store, if it has one.
        /// </summary>
        /// <param name="unlockableNode"><c>TerminalNode</c> of the item being displayed.</param>
        /// <param name="rotationIndex">Index in the current store rotation of the item being displayed.</param>
        /// <returns>Price of the item being displayed, with its discount tag included.</returns>
        private static string AppendDiscountTag(TerminalNode unlockableNode, int rotationIndex)
        {
            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError($"StoreRotationNetworker instance is missing! No discount could be applied to '{unlockableNode.creatureName}'...");

                return $"{unlockableNode.itemCost}";
            }

            // Obtain synced information for the item being displayed.
            StoreRotationEntry entry = StoreRotationNetworker.Instance.StoreRotation[rotationIndex];

            // Return formatted price string to display.
            return entry.GetTerminalPriceString(unlockableNode);
        }
    }
}