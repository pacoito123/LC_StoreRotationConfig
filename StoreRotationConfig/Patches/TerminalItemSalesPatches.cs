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
            // TODO: Return if disabled on config.

            // Return if client has not yet fully synced with the host.
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer && !SyncShipUnlockablesPatch.UnlockablesSynced)
            // && !Plugin.Settings.ConfigSynced)
            {
                Plugin.StaticLogger.LogInfo("Waiting for sync from server before assigning sales...");

                return;
            }

            Random random = new(StartOfRound.Instance.randomMapSeed + 90);

            // TODO: Config settings for parameter; add checks for minItems > maxItems, etc.

            int minItems = 1, maxItems = 5, minSale = 10, maxSale = 80;
            int saleChance = Math.Clamp(50, 0, 100);

            // TODO: Round to nearest ten?

            int itemsOnSale = Math.Clamp(random.Next(maxItems - (int)(maxItems * 100f / saleChance) + 1, maxItems + 1), minItems, maxItems);
            if (itemsOnSale <= 0)
            {
                Plugin.StaticLogger.LogWarning("No items on sale for this rotation...");
                return;
            }

            RotationSales = new(__instance.ShipDecorSelection.Count);

            List<TerminalNode> storeRotation = new(__instance.ShipDecorSelection);
            for (int i = 0; i < itemsOnSale && storeRotation.Count != 0; i++)
            {
                int index = random.Next(0, storeRotation.Count);

                // Add item to the 'RotationSales' dictionary, and remove it from the 'storeRotation' cloned list.
                RotationSales[storeRotation[index]] = random.Next(minSale, maxSale + 1);
                storeRotation.RemoveAt(index);
            }

            Plugin.StaticLogger.LogDebug($"{RotationSales.Count} item(s) on sale!");

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
                    // TODO: Skip if disabled on config.

                    TerminalNode item = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID].shopSelectionNode;

                    Plugin.StaticLogger.LogDebug($"Applying discount of {RotationSales[item]} to {item.creatureName}...");

                    return RotationSales.ContainsKey(item) ? item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f)) : totalCostOfItems;
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
                    // TODO: Skip if disabled on config.

                    if (RotationSales.ContainsKey(storeRotation[index]))
                    {
                        Plugin.StaticLogger.LogDebug($"Appending {RotationSales[storeRotation[index]]} to {storeRotation[index].creatureName}...");

                        TerminalNode item = storeRotation[index];
                        _ = sb.Replace(item.itemCost.ToString(), (item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f))).ToString())
                            .Append($"   ({RotationSales[item]}% OFF!)");
                    }
                }
            )).InstructionEnumeration();
        }
    }
}