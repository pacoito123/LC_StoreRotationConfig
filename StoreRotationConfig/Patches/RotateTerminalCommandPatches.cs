/* using HarmonyLib;
using UnityEngine;

namespace StoreRotationConfig.Patches
{
    internal static class RotateTerminalCommandPatches
    {
        public static TerminalKeyword? ConfirmKeyword { get; private set; }
        public static TerminalKeyword? DenyKeyword { get; private set; }
        public static TerminalNode? CancelPurchaseNode { get; private set; }

        public static TerminalKeyword? RotateKeyword { get; private set; }
        public static TerminalNode? RotateBuyNode { get; private set; }
        public static TerminalNode? RotateBuyConfirmNode { get; private set; }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void TerminalAwake_Prefix(Terminal __instance)
        {
            if (RotateKeyword != null)
            {
                return;
            }

            ConfirmKeyword = __instance.terminalNodes.allKeywords[3];
            DenyKeyword = __instance.terminalNodes.allKeywords[4];
            CancelPurchaseNode = __instance.terminalNodes.allKeywords[0].compatibleNouns[0].result.terminalOptions[1].result;

            RotateKeyword = ScriptableObject.CreateInstance<TerminalKeyword>();
            RotateBuyNode = ScriptableObject.CreateInstance<TerminalNode>();
            RotateBuyConfirmNode = ScriptableObject.CreateInstance<TerminalNode>();

            RotateKeyword.name = "RotateKeyword";
            RotateKeyword.word = "rotate"; // TODO: Make configurable?
            RotateKeyword.compatibleNouns = [];
            RotateKeyword.specialKeywordResult = RotateBuyNode;

            RotateBuyNode.name = "RotateBuyNode";
            RotateBuyNode.displayText = "You have requested to rotate the store selection.\nTotal cost: [totalCost].\n\nPlease CONFIRM or DENY.\n\n";
            RotateBuyNode.clearPreviousText = true;
            RotateBuyNode.maxCharactersToType = 15;
            RotateBuyNode.itemCost = 15; // TODO: Make configurable.
            RotateBuyNode.overrideOptions = true;
            RotateBuyNode.terminalOptions = [
                new(ConfirmKeyword, RotateBuyConfirmNode),
                new(DenyKeyword, CancelPurchaseNode)
            ];

            RotateBuyConfirmNode.name = "RotateBuyConfirmNode";
            RotateBuyConfirmNode.displayText = "Store rotated! Your new balance is [playerCredits].\n\n";
            RotateBuyConfirmNode.clearPreviousText = true;
            RotateBuyConfirmNode.maxCharactersToType = 35;
            RotateBuyConfirmNode.itemCost = 15; // TODO: Make configurable.
            RotateBuyConfirmNode.isConfirmationNode = true;
            RotateBuyConfirmNode.playSyncedClip = 0;

            __instance.terminalNodes.allKeywords = [.. __instance.terminalNodes.allKeywords, RotateKeyword];
            __instance.terminalNodes.specialNodes.Add(RotateBuyNode);
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.RunTerminalEvents))]
        [HarmonyPrefix]
        private static void RunTerminalEvents_Prefix(Terminal __instance, TerminalNode node)
        {
            if (node == RotateBuyConfirmNode)
            {
                // __instance.useCreditsCooldown = true;
                __instance.RotateShipDecorSelection(); // TODO: Network, keep track of rotations
            }
            else if (node == RotateBuyNode)
            {
                __instance.totalCostOfItems = node.itemCost;
            }
        }
    }
} */