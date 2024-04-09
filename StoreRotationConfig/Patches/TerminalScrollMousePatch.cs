using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for 'PlayerControllerB.ScrollMouse_performed()' method; replaces vanilla method unless the 'relativeScroll' setting is disabled.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed", [typeof(InputAction.CallbackContext)])]
    internal class TerminalScrollMousePatch
    {
        // Cached terminal instance.
        private static Terminal terminal;

        // Text of the current terminal page.
        private static string currentText = "";

        // Amount to add/subtract from scrollbar value, relative to number of lines in the current terminal page.
        private static float scrollAmount = 0f;

        [HarmonyPriority(Priority.VeryHigh)]
        private static bool Prefix(ref InputAction.CallbackContext context)
        {
            // Obtain local player instance.
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;

            // Check if player has the terminal open, and the 'relativeScroll' setting enabled.
            if (player.inTerminalMenu && Plugin.Settings.RELATIVE_SCROLL.Value)
            {
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
                    scrollAmount = 1f / (numLines * 0.05f);
                }

                // Increment/decrement scrollbar value by relative scroll amount.
                float scrollDirection = context.ReadValue<float>();
                player.terminalScrollVertical.value += scrollDirection * scrollAmount;

                // Return false to stop vanilla method from executing.
                return false;
            }

            // Return true to execute vanilla method.
            return true;
        }
    }
}