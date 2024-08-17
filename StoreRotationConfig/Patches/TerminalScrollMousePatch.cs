using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'PlayerControllerB.ScrollMouse_performed()' method; overrides vanilla scroll amount if the 'relativeScroll' setting is enabled.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed", typeof(InputAction.CallbackContext))]
    internal class TerminalScrollMousePatch
    {
        // Text shown in the current terminal page, to determine if scroll amount needs to be updated.
        public static string CurrentText { get; internal set; } = "";

        // Amount to add/subtract from the terminal scrollbar, relative to the number of lines in the current terminal page.
        private static float scrollAmount = 1 / 3f;

        /// <summary>
        ///     Handles mouse scrolling while the terminal is open.
        /// </summary>
        /// <param name="scrollbar">Scrollbar instance used by the terminal.</param>
        /// <param name="scrollDirection">Direction to move the scrollbar, determined by the mouse wheel input.</param>
        private static void ScrollMouse_performed(Scrollbar scrollbar, float scrollDirection)
        {
            // Perform vanilla scroll if the 'relativeScroll' setting is disabled.
            if (Plugin.Terminal == null || Plugin.Settings == null || !Plugin.Settings.RELATIVE_SCROLL.Value)
            {
                // Increment scrollbar value by vanilla scroll amount (a third of the page).
                scrollbar.value += scrollDirection / 3f;

                return;
            }

            // Check if text currently shown in the terminal has changed, to avoid calculating the scroll amount more than once.
            if (string.CompareOrdinal(Plugin.Terminal.currentText, CurrentText) != 0)
            {
                // Cache text currently shown in the terminal.
                CurrentText = Plugin.Terminal.currentText;

                // Calculate relative scroll amount using the number of lines in the current terminal page.
                int numLines = CurrentText.Count(c => c.Equals('\n')) + 1;
                scrollAmount = Plugin.Settings.LINES_TO_SCROLL.Value / (float)numLines;

                Plugin.StaticLogger?.LogDebug($"Setting terminal scroll amount to '{scrollAmount}'!");
            }

            // Increment terminal scrollbar value by the relative scroll amount, in the direction given by the mouse wheel input.
            scrollbar.value += scrollDirection * scrollAmount;
        }

        /// <summary>
        ///     Inserts a call to 'TerminalScrollMousePatch.ScrollMouse_performed()', followed by a return instruction.
        /// </summary>
        ///     ... (GameNetcodeStuff.PlayerControllerB:1263)
        ///     float num = context.ReadValue();
        ///     
        ///     -> StoreRotationConfig.Patches.TerminalScrollMousePatch.ScrollMouse_performed(this.terminalScrollVertical, num);
        ///     -> return;
        ///     
        ///     this.terminalScrollVertical.value += num / 3f;
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.terminalScrollVertical))))
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.terminalScrollVertical))),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, AccessTools.Method(typeof(TerminalScrollMousePatch), nameof(ScrollMouse_performed))),
                new(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }
}