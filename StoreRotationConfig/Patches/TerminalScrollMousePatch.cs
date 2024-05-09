using GameNetcodeStuff;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'PlayerControllerB.ScrollMouse_performed()' method; replaces vanilla method unless the 'relativeScroll' setting is disabled.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed", typeof(InputAction.CallbackContext))]
    internal class TerminalScrollMousePatch
    {
        // Cached terminal instance.
        private static Terminal terminal;

        // Text shown in the current terminal page.
        private static string currentText = "";

        // Amount to add/subtract from scrollbar value, relative to number of lines in the current terminal page.
        private static float scrollAmount = 0f;

        private static bool Prefix(PlayerControllerB __instance, ref InputAction.CallbackContext context)
        {
            // Execute vanilla method if the 'relativeScroll' setting is disabled, or if the player instance does not have the terminal open.
            if (!Plugin.Settings.RELATIVE_SCROLL.Value || !__instance.inTerminalMenu)
            {
                return true;
            }

            // Ensure cached terminal instance exists.
            if (terminal == null)
            {
                terminal = Object.FindObjectOfType<Terminal>();
            }

            // Check if text currently shown in the terminal has changed, to avoid calculating scroll amount more than once.
            if (!currentText.Equals(terminal.currentText))
            {
                // Cache text currently shown in the terminal.
                currentText = terminal.currentText;

                // Calculate relative scroll amount using number of lines in the current terminal page.
                int numLines = currentText.Count(c => c.Equals('\n')) + 1;
                scrollAmount = Plugin.Settings.LINES_TO_SCROLL.Value / (float)numLines;
            }

            // Increment/decrement terminal scrollbar value by relative scroll amount.
            float scrollDirection = context.ReadValue<float>();
            __instance.terminalScrollVertical.value += scrollDirection * scrollAmount;

            // Return false to stop vanilla method from executing.
            return false;
        }
    }
}