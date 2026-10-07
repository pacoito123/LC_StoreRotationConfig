using HarmonyLib;
using StoreRotationConfig.Networking;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for including sale percentages from the store rotation in advertisements, similar to tools.
    /// </summary>
    internal static class UnlockableDisplayAdsPatches
    {
        /// <summary>
        ///     Attempt to get a rotating item's discount value and override bottom text, if it has one for the current rotation.
        /// </summary>
        /// <param name="rotationIndex">Index in the current store rotation of the item being displayed.</param>
        /// <param name="saleText">Bottom text to display in the advertisement, as a ref parameter.</param>
        /// <returns>Whether the item being displayed has a discount available or not.</returns>
        private static bool TryGetDiscount(int rotationIndex, ref string saleText)
        {
            if (Plugin.Settings?.DISPLAY_AD_DISCOUNTS.Value != true)
            {
                return false;
            }

            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError($"StoreRotationNetworker instance is missing! No discount could be applied to item at index '{rotationIndex}'...");

                return false;
            }

            // Obtain synced information for the displayed item.
            StoreRotationEntry entry = StoreRotationNetworker.Instance.StoreRotation[rotationIndex];

            if (entry.UnlockableDiscount > 0)
            {
                // Replace sale text for the current ad, if there is a discount.
                saleText = $"{entry.UnlockableDiscount}% OFF!";

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Inserts a call to 'UnlockableDisplayAdsPatches.TryGetDiscount()' to change bottom text, if the rotating item has a discount.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         if (flag)
        ///         {
        ///             -> _ = UnlockableDisplayAdsPatches.TryGetDiscount(num3, ref text2);
        ///             this.BeginDisplayAd(text, text2);
        ///         }
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <param name="generator">Generator for IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ChooseAdItem))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ChooseAdItem_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo displayAdClientRpcInfo = typeof(HUDManager).GetMethod(nameof(HUDManager.CreateFurnitureAdModelAndDisplayAdClientRpc), BindingFlags.Instance | BindingFlags.Public);
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).End().MatchBack(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_S), // V_12 (indexInShipDecorList)
                new(OpCodes.Call, displayAdClientRpcInfo));

            if (codeMatcher.Advance(1).IsInvalid)
            {
                Plugin.Logger.LogError("Could not match ChooseAdItem 'CreateFurnitureAdModelAndDisplayAdClientRpc()' call.");

                return instructions;
            }

            CodeInstruction loadRotationIndex = codeMatcher.Instruction; // Ldloc.s V_12 (indexInShipDecorList)

            MethodInfo beginDisplayAdInfo = typeof(HUDManager).GetMethod(nameof(HUDManager.BeginDisplayAd), BindingFlags.Instance | BindingFlags.Public);
            _ = codeMatcher.MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_S), // V_5 (itemName)
                new(OpCodes.Ldloc_S), // V_6 (saleText)
                new(OpCodes.Call, beginDisplayAdInfo));

            if (codeMatcher.Advance(2).IsInvalid)
            {
                Plugin.Logger.LogError("Could not match ChooseAdItem 'BeginDisplayAd()' call.");

                return instructions;
            }

            object saleTextLocal = codeMatcher.Operand; // V_6 (saleText)

            MethodInfo tryGetDiscountInfo = typeof(UnlockableDisplayAdsPatches).GetMethod(nameof(TryGetDiscount), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.CreateLabelAt(codeMatcher.Pos + 1, out Label discountTarget)
            .CreateLabel(out Label noDiscountTarget)
            .Insert(
                loadRotationIndex, // Ldloc.s V_12 (indexInShipDecorList)
                new(OpCodes.Ldloca_S, saleTextLocal),
                new(OpCodes.Call, tryGetDiscountInfo),
                new(OpCodes.Pop))
            .InstructionEnumeration();
        }

        /// <summary>
        ///     Inserts a call to 'UnlockableDisplayAdsPatches.TryGetDiscount()' to change bottom text, if the rotating item has a discount.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         BeginDisplayAd(StartOfRound.Instance.unlockablesList.unlockables[terminalScript.ShipDecorSelection[indexInShipDecorList].shipUnlockableID].unlockableName,
        ///             -> UnlockableDisplayAdsPatches.TryGetDiscount(indexInShipDecorList, out string saleText) ? saleText :
        ///             ChooseSaleText());
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <param name="generator">Generator for IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.CreateFurnitureAdModelAndDisplayAdClientRpc))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> CreateFurnitureAdModelAndDisplayAdClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo chooseSaleTextInfo = typeof(HUDManager).GetMethod(nameof(HUDManager.ChooseSaleText), BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo beginDisplayAdInfo = typeof(HUDManager).GetMethod(nameof(HUDManager.BeginDisplayAd), BindingFlags.Instance | BindingFlags.Public);
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).End().MatchBack(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, chooseSaleTextInfo),
                new(OpCodes.Call, beginDisplayAdInfo));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match CreateFurnitureAdModelAndDisplayAdClientRpc 'BeginDisplayAd()' call.");

                return instructions;
            }

            LocalBuilder saleTextLocal = generator.DeclareLocal(typeof(string)); // saleText

            MethodInfo tryGetDiscountInfo = typeof(UnlockableDisplayAdsPatches).GetMethod(nameof(TryGetDiscount), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.CreateLabel(out Label discountTarget)
            .Advance(-2)
            .CreateLabel(out Label noDiscountTarget)
            .Insert(
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldloca_S, saleTextLocal),
                new(OpCodes.Call, tryGetDiscountInfo),
                new(OpCodes.Brfalse_S, noDiscountTarget),
                new(OpCodes.Ldloc_S, saleTextLocal),
                new(OpCodes.Br_S, discountTarget))
            .InstructionEnumeration();
        }
    }
}