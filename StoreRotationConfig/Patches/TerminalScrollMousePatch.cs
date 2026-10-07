using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'PlayerControllerB.ScrollMouse_performed()' method; overrides vanilla scroll amount if the 'relativeScroll' setting is enabled.
    /// </summary>
    internal static class TerminalScrollMousePatch
    {
        // Text shown in the current terminal page, to determine if scroll amount needs to be updated.
        public static string currentText = string.Empty;

        // Amount to add/subtract from the terminal scrollbar, relative to the number of lines in the current terminal page.
        private static float scrollAmount = 1 / 3.0f;

        /// <summary>
        ///     Handles mouse scrolling while the terminal is open.
        /// </summary>
        /// <param name="scrollbar">Scrollbar instance used by the terminal.</param>
        /// <param name="scrollDirection">Direction to move the scrollbar, determined by the mouse wheel input.</param>
        private static void ScrollMouse_performed(Scrollbar scrollbar, float scrollDirection)
        {
            // Perform vanilla scroll if the 'relativeScroll' setting is disabled.
            if (Plugin.Settings?.RELATIVE_SCROLL.Value != true)
            {
                // Increment scrollbar value by vanilla scroll amount (a third of the page).
                scrollbar.value += scrollDirection / 3.0f;

                return;
            }

            // Check if text currently shown in the terminal has changed, to avoid calculating the scroll amount more than once.
            if (Plugin.Terminal != null && !string.Equals(Plugin.Terminal.currentText, currentText, System.StringComparison.Ordinal))
            {
                // Cache text currently shown in the terminal.
                currentText = Plugin.Terminal.currentText;

                // Calculate relative scroll amount using the number of lines in the current terminal page.
                int numLines = currentText.Split('\n').Length;
                scrollAmount = Plugin.Settings.LINES_TO_SCROLL.Value / (float)numLines;

                Plugin.Logger.LogDebug($"Setting terminal scroll amount to '{scrollAmount}'!");
            }

            // Increment terminal scrollbar value by the relative scroll amount, in the direction given by the mouse wheel input.
            scrollbar.value += scrollDirection * scrollAmount;
        }

        /// <summary>
        ///     Inserts a call to 'TerminalScrollMousePatch.ScrollMouse_performed()', followed by a return instruction.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         float num = context.ReadValue();
        /// 
        ///         -> TerminalScrollMousePatch.ScrollMouse_performed(this.terminalScrollVertical, num);
        ///         -> return;
        /// 
        ///         this.terminalScrollVertical.value += num / 3f;
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ScrollMouse_performed), typeof(InputAction.CallbackContext))]
        private static IEnumerable<CodeInstruction> ScrollMousePerformed_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo terminalScrollVerticalInfo = typeof(PlayerControllerB).GetField(nameof(PlayerControllerB.terminalScrollVertical), BindingFlags.Instance | BindingFlags.Public);
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, terminalScrollVerticalInfo));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match Player 'terminalScrollVertical' field.");

                return instructions;
            }

            MethodInfo patchScrollMousePerformedInfo = typeof(TerminalScrollMousePatch).GetMethod(nameof(ScrollMouse_performed), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, terminalScrollVerticalInfo),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, patchScrollMousePerformedInfo),
                new(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }
}