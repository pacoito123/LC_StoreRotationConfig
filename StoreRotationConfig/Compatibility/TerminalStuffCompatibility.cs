using HarmonyLib;
using StoreRotationConfig.Networking;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TerminalStuff.StoreTweaks;

namespace StoreRotationConfig.Compatibility
{
    internal static class TerminalStuffCompatibility
    {
        /// <summary>
        ///     Whether <c>TerminalStuff</c> is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(TerminalStuff.Plugin.Id);

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(StoreInfo), nameof(StoreInfo.PriceChecks))]
        [HarmonyPostfix]
        private static void PriceChecks_Postfix(StoreInfo __instance, TerminalNode ___terminalNode)
        {
            if (__instance.Unlockable == null)
            {
                return;
            }

            // Reset sale state for the displayed item.
            __instance.OnSale = false;

            List<TerminalNode>? shipDecorSelection = (Plugin.Terminal != null) ? Plugin.Terminal.ShipDecorSelection : null;

            // Obtain index in the current store rotation for the displayed item.
            int rotationIndex = shipDecorSelection?.IndexOf(___terminalNode) ?? -1;

            if (rotationIndex == -1)
            {
                return;
            }

            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError($"StoreRotationNetworker instance is missing! No discount could be applied to '{___terminalNode.creatureName}'...");

                return;
            }

            // Obtain synced information for the displayed item.
            StoreRotationEntry entry = StoreRotationNetworker.Instance.StoreRotation[rotationIndex];

            // Set discounted price for the displayed item and set sale state, if there is a discount.
            __instance.price = entry.GetDiscountedPrice(___terminalNode);
            __instance.OnSale = entry.UnlockableDiscount > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(StorePlus), nameof(StorePlus.GetSalesPercentage))]
        [HarmonyPostfix]
        private static void GetSalesPercentage_Postfix(ref int __result, StoreInfo item)
        {
            if (item.Unlockable == null)
            {
                return;
            }

            List<TerminalNode>? shipDecorSelection = (Plugin.Terminal != null) ? Plugin.Terminal.ShipDecorSelection : null;

            // Obtain index in the current store rotation for the displayed item.
            int rotationIndex = shipDecorSelection?.IndexOf(item.terminalNode) ?? -1;

            if (rotationIndex == -1)
            {
                return;
            }

            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError($"StoreRotationNetworker instance is missing! No discount could be applied to '{item.terminalNode.creatureName}'...");

                return;
            }

            // Obtain synced information for the displayed item.
            StoreRotationEntry entry = StoreRotationNetworker.Instance.StoreRotation[rotationIndex];

            // Set discount percentage for the displayed item.
            __result = entry.UnlockableDiscount;
        }
    }
}