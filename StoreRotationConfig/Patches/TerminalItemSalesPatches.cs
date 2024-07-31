using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Unity.Netcode;

namespace StoreRotationConfig.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalItemSalesPatches
    {
        public static Dictionary<TerminalNode, int> RotationSales { get; private set; }

        [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyPostfix]
        private static void SetRotationSales(Terminal __instance)
        {
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

            int itemsOnSale = random.Next(maxSaleItems - (int)(maxSaleItems * saleChance) + 1, maxSaleItems + 1);
            if (itemsOnSale <= 0)
            {
                Plugin.StaticLogger.LogInfo("No items on sale for this rotation...");

                return;
            }

            itemsOnSale = Math.Clamp(itemsOnSale, minSaleItems, maxSaleItems);

            RotationSales = new(__instance.ShipDecorSelection.Count);

            List<TerminalNode> storeRotation = new(__instance.ShipDecorSelection);
            for (int i = 0; i < itemsOnSale && storeRotation.Count != 0; i++)
            {
                int discount = random.Next(minDiscount, maxDiscount + 1);

                if (Plugin.Settings.ROUND_TO_NEAREST_TEN)
                {
                    discount = (int)Math.Round(discount / 10.0f) * 10;
                }

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
                    if (Plugin.Settings.SALE_CHANCE != 0 && RotationSales.ContainsKey(StartOfRound.Instance.unlockablesList
                        .unlockables[node.shipUnlockableID]?.shopSelectionNode))
                    {
                        TerminalNode item = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID].shopSelectionNode;

                        Plugin.StaticLogger.LogDebug($"Applying discount of {RotationSales[item]} to {item.creatureName}...");

                        return item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f));
                    }
                    else
                    {
                        return totalCostOfItems;
                    }
                }))
            .Insert(new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Terminal), "totalCostOfItems")))
            .InstructionEnumeration();
        }

        [HarmonyPatch("TextPostProcess")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TextPostProcessTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "\n{0}  //  ${1}"))
            .MatchForward(false, new CodeMatch(OpCodes.Pop))
            .Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.ShipDecorSelection))),
                new(OpCodes.Ldloc_S, 13),
                new(OpCodes.Ldloc_S, 14))
            .Insert(Transpilers.EmitDelegate((List<TerminalNode> storeRotation, StringBuilder sb, int index) =>
                {
                    if (Plugin.Settings.SALE_CHANCE != 0 && RotationSales != null && RotationSales.ContainsKey(storeRotation[index]))
                    {
                        TerminalNode item = storeRotation[index];

                        Plugin.StaticLogger.LogDebug($"Appending {RotationSales[item]} to {item.creatureName}...");

                        _ = sb.Replace(item.itemCost.ToString(), (item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f)))
                            .ToString()).Append($"   ({RotationSales[item]}% OFF!)");
                    }
                }
            )).InstructionEnumeration();
        }
    }
}