using HarmonyLib;
using MrovLib.ContentType;
using StoreRotationConfig.Networking;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TerminalUtils.Nodes;

namespace StoreRotationConfig.Compatibility
{
    internal static class TerminalUtilsCompatibility
    {
        /// <summary>
        ///     Whether <c>TerminalUtils</c> is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(TerminalUtils.MyPluginInfo.PLUGIN_GUID);

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        /// <summary>
        ///     Inserts a call to 'TerminalUtilsCompatibility.ApplyUnlockableDiscount()' to apply discounts for rotating items.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         -> ApplyUnlockableDiscount(thing, ref priceWithDiscount);
        ///         table.AddRow($"* {name}", $"{priceWithDiscount}");
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(StoreCatalogue), nameof(StoreCatalogue.GetNodeText))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GetNodeText_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: false,
                new(OpCodes.Ldloc_S), // V_10 (thing)
                new(OpCodes.Castclass, typeof(BuyableItem)),
                new(OpCodes.Stloc_S)); // V_13 (item)

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match StoreCatalogue BuyableItem casting.");

                return instructions;
            }

            CodeInstruction loadThing = codeMatcher.Instruction; // Ldfld.s V_10 (thing)

            MethodInfo concatInfo = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)]);
            _ = codeMatcher.MatchForward(useEnd: true,
                new(OpCodes.Ldloc_1),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Newarr, typeof(object)),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldstr, "* "),
                new(OpCodes.Ldloc_S), // V_11 (name)
                new(OpCodes.Call, concatInfo),
                new(OpCodes.Stelem_Ref),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldloc_S)); // V_12 (priceWithDiscount)

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match StoreCatalogue 'priceWithDiscount' concatenation.");

                return instructions;
            }

            object priceWithDiscount = codeMatcher.Operand; // V_12 (priceWithDiscount)

            MethodInfo applyUnlockableDiscountInfo = typeof(TerminalUtilsCompatibility).GetMethod(nameof(AppendDiscountTag), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.Advance(-11)
            .SetAndAdvance(loadThing.opcode, loadThing.operand) // Ldfld.s V_10 (thing)
            .Insert(
                new(OpCodes.Ldloca_S, priceWithDiscount),
                new(OpCodes.Call, applyUnlockableDiscountInfo),
                new(OpCodes.Ldloc_1))
            .InstructionEnumeration();
        }

        /// <summary>
        ///     Append discount tag to an item being displayed in the <c>Terminal</c> store, if it has one.
        /// </summary>
        /// <param name="thing">Item being displayed.</param>
        /// <param name="priceWithDiscount">Price of the item being displayed, as a ref parameter.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void AppendDiscountTag(BuyableThing thing, ref string priceWithDiscount)
        {
            TerminalNode? unlockableNode = (thing is BuyableUnlockable unlockable) ? unlockable.Unlockable?.shopSelectionNode
                : ((thing is BuyableSuit suit) ? suit.Suit?.shopSelectionNode : null);

            // Return if item is neither an unlockable nor a suit, or has no Terminal node.
            if (unlockableNode == null)
            {
                return;
            }

            List<TerminalNode>? shipDecorSelection = (Plugin.Terminal != null) ? Plugin.Terminal.ShipDecorSelection : null;

            // Obtain index in the current store rotation for the displayed item.
            int rotationIndex = shipDecorSelection?.IndexOf(unlockableNode) ?? -1;

            if (rotationIndex == -1)
            {
                return;
            }

            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError($"StoreRotationNetworker instance is missing! No discount could be applied to '{unlockableNode.creatureName}'...");

                return;
            }

            // Obtain synced information for the displayed item.
            StoreRotationEntry entry = StoreRotationNetworker.Instance.StoreRotation[rotationIndex];

            int unlockableDiscount = entry.UnlockableDiscount;

            if (unlockableDiscount > 0)
            {
                // Set discounted price to display, if there is a discount.
                priceWithDiscount = $"${entry.GetDiscountedPrice(unlockableNode)}  (-{unlockableDiscount}%)";
            }
        }
    }
}