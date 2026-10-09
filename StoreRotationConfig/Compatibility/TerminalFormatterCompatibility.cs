using HarmonyLib;
using MrovLib.ContentType;
using StoreRotationConfig.Networking;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TerminalFormatter;
using TerminalFormatter.Nodes;

namespace StoreRotationConfig.Compatibility
{
    internal static class TerminalFormatterCompatibility
    {
        /// <summary>
        ///     Whether <c>TerminalFormatter</c> is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(MyPluginInfo.PLUGIN_GUID);

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        /// <summary>
        ///     Inserts a call to 'TerminalFormatterCompatibility.ApplyUnlockableDiscount()' to apply discounts for rotating items.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         -> ApplyUnlockableDiscount(thing, ref nameWithDiscount, ref price);
        ///         table.AddRow($"{nameWithDiscount.PadRight(Settings.itemNameWidth)}");
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(Store), nameof(Store.GetNodeText))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GetNodeText_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo thingNameGetter = typeof(BuyableThing).GetProperty(nameof(BuyableThing.Name)).GetGetMethod();
            MethodInfo thingPriceGetter = typeof(BuyableThing).GetProperty(nameof(BuyableThing.Price)).GetGetMethod();
            MethodInfo stringFormatInfo = typeof(string).GetMethod(nameof(string.Format), [typeof(string), typeof(object)]);
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: false,
                new(OpCodes.Ldloc_S), // V_15 (thing)
                new(OpCodes.Callvirt, thingNameGetter),
                new(OpCodes.Stloc_S), // V_16 (nameWithDiscount)
                new(OpCodes.Ldstr, "${0}"),
                new(OpCodes.Ldloc_S), // V_15 (thing)
                new(OpCodes.Callvirt, thingPriceGetter),
                new(OpCodes.Box, typeof(int)),
                new(OpCodes.Call, stringFormatInfo),
                new(OpCodes.Stloc_S)); // V_17 (price)

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match Store 'price' formatting.");

                return instructions;
            }

            CodeInstruction loadThing = codeMatcher.Instruction; // Ldfld.s V_15 (thing)
            object nameWithDiscount = codeMatcher.Advance(2).Operand; // V_16 (nameWithDiscount)
            object price = codeMatcher.Advance(6).Operand; // V_17 (price)

            FieldInfo itemNameWidthInfo = typeof(Settings).GetField(nameof(Settings.itemNameWidth), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo padRightInfo = typeof(string).GetMethod(nameof(string.PadRight), [typeof(int)]);
            _ = codeMatcher.MatchForward(useEnd: false,
                new(OpCodes.Ldloc_1),
                new(OpCodes.Ldc_I4_3),
                new(OpCodes.Newarr, typeof(object)),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldloc_S), // V_16 (nameWithDiscount)
                new(OpCodes.Ldsfld, itemNameWidthInfo),
                new(OpCodes.Callvirt, padRightInfo),
                new(OpCodes.Dup));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match Store 'nameWithDiscount' padding.");

                return instructions;
            }

            MethodInfo appendDiscountTagInfo = typeof(TerminalFormatterCompatibility).GetMethod(nameof(AppendDiscountTag), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.SetAndAdvance(loadThing.opcode, loadThing.operand) // Ldfld.s V_15 (thing)
            .Insert(
                new(OpCodes.Ldloca_S, nameWithDiscount),
                new(OpCodes.Ldloca_S, price),
                new(OpCodes.Call, appendDiscountTagInfo),
                new(OpCodes.Ldloc_1))
            .InstructionEnumeration();
        }

        /// <summary>
        ///     Append discount tag to an item being displayed in the <c>Terminal</c> store, if it has one.
        /// </summary>
        /// <param name="thing">Item being displayed.</param>
        /// <param name="nameWithDiscount">Name of the item being displayed with discount included, as a ref parameter.</param>
        /// <param name="price">Price of the item being displayed, as a ref parameter.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void AppendDiscountTag(BuyableThing thing, ref string nameWithDiscount, ref string price)
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
                // Set discount value to display, if there is a discount.
                string discountPercent = $" -{unlockableDiscount}%";

                // Format discount like the regular store.
                nameWithDiscount = (nameWithDiscount.Length + discountPercent.Length > Settings.itemNameWidth)
                    ? $"{nameWithDiscount[..(Settings.itemNameWidth - 4 - discountPercent.Length)]}... {discountPercent}"
                    : $"{nameWithDiscount.PadRight(Settings.itemNameWidth - discountPercent.Length)}{discountPercent}".PadRight(Settings.itemNameWidth);

                // Set discounted price to display, if there is a discount.
                price = $"${entry.GetDiscountedPrice(unlockableNode)}";
            }
        }
    }
}