using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TerminalFormatter.Nodes;
using static StoreRotationConfig.Patches.TerminalItemSalesPatches;

namespace StoreRotationConfig.Compatibility
{
    /// <summary>
    ///     Class handling compatibility with 'TerminalFormatter'.
    /// </summary>
    [HarmonyPatch]
    internal static class TerminalFormatterCompatibility
    {
        /// <summary>
        ///     Whether 'TerminalFormatter' is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("TerminalFormatter");

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        /// <summary>
        ///     Whether patches for 'TerminalFormatter' compatibility have already been applied or not.
        /// </summary>
        public static bool Patched { get; internal set; } = false;

        /// <summary>
        ///     Replaces parameter with delegate call to display item discounts and their modified prices.
        /// </summary>
        ///     ... (TerminalFormatter.Nodes.Store:286)
        ///     consoleTable.AddRow(new object[]
        ///     {
        ///         text7,
        ///         string.Format("${0}",
        ///             // terminalNode2.itemCost),
        ///             -> call(terminalNode2)),
        ///         ""
        ///     });
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(Store), nameof(Store.GetNodeText))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GetNodeTextTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "[DECORATIONS]"))
            .MatchForward(false,
                new(OpCodes.Ldfld, AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.itemCost))),
                new(OpCodes.Box, operand: typeof(int)))
            .SetInstructionAndAdvance(Transpilers.EmitDelegate((TerminalNode item) =>
                {
                    // Return full cost if 'salesChance' is disabled OR the 'RotationSales' dictionary doesn't contain a discount for the item about to be displayed.
                    if (Plugin.Settings.SALE_CHANCE == 0 || (RotationSales != null && !RotationSales.ContainsKey(item)))
                    {
                        return $"{item.itemCost}";
                    }

                    Plugin.StaticLogger.LogDebug($"Appending {RotationSales[item]} to {item.creatureName}...");

                    // Obtain discounted item price.
                    int discountedPrice = item.itemCost - (int)(item.itemCost * (RotationSales[item] / 100f));

                    // Return string containing the discounted price and discount amount to display in the store page. 
                    return $"{discountedPrice} ({RotationSales[item]}% OFF!)";
                }))
            .SetOperandAndAdvance(typeof(string))
            .InstructionEnumeration();
        }
    }
}