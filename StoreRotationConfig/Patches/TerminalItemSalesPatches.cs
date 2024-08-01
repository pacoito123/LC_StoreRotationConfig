using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for adding sales to the store rotation.
    /// </summary>
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalItemSalesPatches
    {
        // Cached dictionary of discount values, using its respective item node as key.
        public static Dictionary<TerminalNode, int> RotationSales { get; private set; }

        [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyPostfix]
        private static void SetRotationSales(Terminal __instance)
        {
            // Return if 'saleChance' setting is disabled (set to '0').
            if (Plugin.Settings.SALE_CHANCE == 0)
            {
                return;
            }

            // Return if client has not yet fully synced with the host.
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer && !SyncShipUnlockablesPatch.UnlockablesSynced)
            // && !Plugin.Settings.ConfigSynced)
            {
                Plugin.StaticLogger.LogInfo("Waiting for sync from server before assigning sales...");

                return;
            }

            // Initialize 'Random' instance using the same seed as vanilla sales.
            Random random = new(StartOfRound.Instance.randomMapSeed + 90);

            // Obtain values from the config file.
            float saleChance = 100f / Plugin.Settings.SALE_CHANCE;

            int minSaleItems = Math.Abs(Plugin.Settings.MIN_SALE_ITEMS),
                maxSaleItems = Math.Abs(Plugin.Settings.MAX_SALE_ITEMS);
            int minDiscount = Plugin.Settings.MIN_DISCOUNT,
                maxDiscount = Plugin.Settings.MAX_DISCOUNT;
            // ...

            // Use 'maxItems' for 'minItems' if the latter is greater than the former.
            if (minSaleItems > maxSaleItems)
            {
                Plugin.StaticLogger.LogWarning("Value for 'minSaleItems' is larger than 'maxSaleItems', using it instead...");

                maxSaleItems = minSaleItems;
            }

            // Obtain number of items with sales for this rotation, using parameters from the config file.
            int itemsOnSale = random.Next(maxSaleItems - (int)(maxSaleItems * saleChance) + 1, maxSaleItems + 1);

            // Return if no items are on sale for this rotation.
            if (itemsOnSale <= 0)
            {
                Plugin.StaticLogger.LogInfo("No items on sale for this rotation...");

                return;
            }

            // Initialize 'RotationSales' dictionary with its capacity set to however many items are to be on sale.
            RotationSales = new(itemsOnSale);

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

                // Add item to the 'RotationSales' dictionary along with its discount, and remove it from the 'storeRotation' cloned list.
                RotationSales[storeRotation[index]] = discount;
                storeRotation.RemoveAt(index);
            }

            Plugin.StaticLogger.LogInfo($"{RotationSales.Count} item(s) on sale!");

            /* TODO: Use for a future feature.
            __instance.ShipDecorSelection.DoIf(
                condition: RotationSales.ContainsKey,
                action: item =>
                {
                    item.itemCost -= (int)(item.itemCost * (RotationSales[item] / 100f));
                    Plugin.StaticLogger.LogDebug($"Discount of {RotationSales[item]} applied to {item.creatureName}!");
                }); */
        }

        /// <summary>
        ///     Applies a rotating item's discount (if it has one assigned in the current rotation) right before its purchase.
        /// </summary>
        ///     ... (Terminal:630)
        ///     else if (node.buyRerouteToMoon != -1 || node.shipUnlockableID != -1)
        ///     {
        ///         // this.totalCostOfItems = node.itemCost;
        ///         
        ///         -> this.totalCostOfItems = call(node, this.totalCostOfItems);
        ///     }
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TerminalLoadNewNodeIfAffordableTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldfld, AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.itemCost))),
                new(OpCodes.Stfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .InsertAndAdvance(Transpilers.EmitDelegate((TerminalNode node, int totalCostOfItems) =>
                {
                    // Obtain node of the item currently selected for purchase.
                    TerminalNode item = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID]?.shopSelectionNode;

                    // Check if 'salesChance' is enabled and the 'RotationSales' dictionary contains a discount for the item to purchase.
                    if (Plugin.Settings.SALE_CHANCE != 0 && RotationSales.ContainsKey(item))
                    {
                        Plugin.StaticLogger.LogDebug($"Applying discount of {RotationSales[item]} to {item.creatureName}...");

                        // Apply discount to the total cost of the purchase.
                        return item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f));
                    }
                    else
                    {
                        // Leave total cost of the purchase unchanged.
                        return node.itemCost;
                    }
                }))
            .Insert(new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .InstructionEnumeration();
        }

        /// <summary>
        ///     Displays rotating item discounts and their modified prices in the store page.
        ///     Dynamic operands maintain compatibility between game versions.
        /// </summary>
        ///     ... (Terminal:344)
        ///     for (int m = 0; m &lt; this.ShipDecorSelection.Count; m++)
        ///     {
        ///         stringBuilder*.Append(string.Format("\n{0}  //  ${1}", this.ShipDecorSelection[m].creatureName, this.ShipDecorSelection[m].itemCost));
        ///         
        ///         -> call(this.ShipDecorSelection, stringBuilder*, m);
        ///     }
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch("TextPostProcess")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TextPostProcessTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldstr, "\n{0}  //  ${1}"),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.ShipDecorSelection))));

            // Obtain operand for the StringBuilder instance using cloned instructions.
            object sbOperand = matcher.Clone().MatchBack(false, new CodeMatch(OpCodes.Newobj)).Advance(1).Operand;

            // Obtain operand for the loop index using cloned instructions.
            object indexOperand = matcher.Clone().Advance(3).Operand;

            return matcher.MatchForward(false, new CodeMatch(OpCodes.Pop))
                .Advance(1)
                .InsertAndAdvance(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.ShipDecorSelection))),
                    new(OpCodes.Ldloc_S, sbOperand),
                    new(OpCodes.Ldloc_S, indexOperand))
                .Insert(Transpilers.EmitDelegate((List<TerminalNode> storeRotation, StringBuilder sb, int index) =>
                    {
                        // Obtain item about to be displayed in the store page.
                        TerminalNode item = storeRotation[index];

                        // Check if 'salesChance' is enabled and the 'RotationSales' dictionary contains a discount for the item about to be displayed.
                        if (Plugin.Settings.SALE_CHANCE != 0 && RotationSales != null && RotationSales.ContainsKey(storeRotation[index]))
                        {
                            Plugin.StaticLogger.LogDebug($"Appending {RotationSales[item]} to {item.creatureName}...");

                            // Replace old price with the discounted price, and append sale tag to the displayed text.
                            _ = sb.Replace(item.itemCost.ToString(), (item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f))).ToString())
                                .Append($"   ({RotationSales[item]}% OFF!)");
                        }
                    }
                )).InstructionEnumeration();
        }
    }
}