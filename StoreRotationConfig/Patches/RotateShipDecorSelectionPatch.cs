using HarmonyLib;
using StoreRotationConfig.Networking;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patch for <c>Terminal.RotateShipDecorSelection()</c> method; overrides vanilla method, but should functionally be the same.
    /// </summary>
    internal static class RotateShipDecorSelectionPatch
    {
        /// <summary>
        ///     Fills <c>Terminal.ShipDecorSelection</c> list with items, reading from the host's config file.
        /// </summary>
        /// <param name="random">Seeded <c>Random</c> instance used for generating a new store rotation.</param>
        private static void RotateShipDecorSelection(Random random)
        {
            if (StoreRotationNetworker.Instance == null || StoreRotationNetworker.Instance.StoreRotation == null)
            {
                Plugin.Logger.LogError("StoreRotationNetworker instance is missing! Store rotation won't work...");

                return;
            }

            if (!StoreRotationNetworker.Instance.IsHost)
            {
                return;
            }

            if (Plugin.Settings == null)
            {
                Plugin.Logger.LogError("Configuration could not be loaded or is missing! Store rotation won't work...");

                return;
            }

            // Obtain values from the config file.
            int maxItems = Math.Abs(Plugin.Settings.MAX_ITEMS.Value),
                minItems = Math.Abs(Plugin.Settings.MIN_ITEMS.Value);
            bool stockAll = Plugin.Settings.STOCK_ALL.Value,
                sortItems = Plugin.Settings.SORT_ITEMS.Value,
                removePurchased = Plugin.Settings.REMOVE_PURCHASED.Value;
            HashSet<UnlockableItem> whitelistedItems = Plugin.Settings.WhitelistedItems,
                blacklistedItems = Plugin.Settings.BlacklistedItems;
            // ...

            Plugin.Logger.LogInfo("Rotating store...");

            // Clear previous store rotation.
            StoreRotationNetworker.Instance.StoreRotation.Clear();

            // Use 'minItems' for 'maxItems', if the former is greater than the latter.
            if (minItems > maxItems && !stockAll)
            {
                Plugin.Logger.LogWarning("Value for 'minItems' is larger than 'maxItems', using it instead...");

                maxItems = minItems;
            }

            // Obtain a random number of items using the map seed, or use a fixed number if 'minItems' and 'maxItems' are equal.
            int numItems = (minItems != maxItems && !stockAll) ? random.Next(minItems, maxItems + 1) : maxItems;

            // Obtain list of possible items for this rotation.
            List<UnlockableItem> possibleItems = [.. StartOfRound.Instance.unlockablesList.unlockables];
            _ = possibleItems.RemoveAll(item => item == null || item.shopSelectionNode == null || item.alwaysInStock // Remove invalid items and ship upgrades.
                || whitelistedItems.Contains(item) || blacklistedItems.Contains(item) // Remove both whitelisted and blacklisted items.
                || (removePurchased && item.hasBeenUnlockedByPlayer)); // Remove purchased items, if configured to do so.

            // Set number of items to the total number of possible items, if set to stock all.
            if (stockAll)
            {
                numItems = possibleItems.Count;
            }

            // Create list that'll become the next store rotation.
            List<UnlockableItem> storeRotation = [.. whitelistedItems]; // Add whitelisted items.
            _ = storeRotation.RemoveAll(item => item == null || item.shopSelectionNode == null || item.alwaysInStock // Remove invalid items and ship upgrades.
                || (removePurchased && item.hasBeenUnlockedByPlayer)); // Remove purchased items, if configured to do so.

            // Iterate for every item to add to the store rotation, exiting early if there are no more items in the list of possible items.
            for (int i = 0; i < numItems && possibleItems.Count != 0; i++)
            {
                // Obtain a random item from the list of possible items.
                int index = random.Next(0, possibleItems.Count);

                // Add random item to the current rotation, and remove it from the list of possible items.
                storeRotation.Add(possibleItems[index]);
                possibleItems.RemoveAt(index);
            }

            // Sort store rotation list alphabetically, if set to sort items.
            if (sortItems && storeRotation.Count > 1)
            {
                storeRotation.Sort(static (x, y) => string.Compare(x.shopSelectionNode.creatureName, y.shopSelectionNode.creatureName, StringComparison.Ordinal));
            }

            // Obtain sales for the current rotation, if any are rolled.
            int[]? possibleSales = TerminalItemSalesPatches.RollSales(storeRotation.Count);

            // Fill synced store rotation list with the current rotation.
            for (int i = 0; i < storeRotation.Count; i++)
            {
                UnlockableItem item = storeRotation[i];

                int unlockableID = item.shopSelectionNode.shipUnlockableID,
                    unlockablePrice = item.shopSelectionNode.itemCost,
                    unlockableDiscount = possibleSales?[i] ?? 0; // Obtain discount to apply from list of sales.
                bool isSuit = item.unlockableType == 0 && item.suitMaterial != null;

                // Add new entry to synced store rotation list.
                StoreRotationNetworker.Instance.StoreRotation.Add(new(unlockableID, unlockablePrice, unlockableDiscount, isSuit));
            }
        }

        /// <summary>
        ///     Inserts a call to 'RotateShipDecorSelectionPatch.RotateShipDecorSelection()', followed by a return instruction.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         Random random = new Random(StartOfRound.Instance.randomMapSeed + 65);
        /// 
        ///         -> RotateShipDecorSelection(random);
        ///         -> return;
        /// 
        ///         this.ShipDecorSelection.Clear();
        ///     </code>
        /// </remarks>
        /// <param name="instructions">Iterator with original IL instructions.</param>
        /// <returns>Iterator with modified IL instructions.</returns>
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RotateShipDecorSelection_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo shipDecorSelectionInfo = typeof(Terminal).GetField(nameof(Terminal.ShipDecorSelection), BindingFlags.Instance | BindingFlags.Public);
            MethodInfo listClearInfo = typeof(List<TerminalNode>).GetMethod(nameof(List<>.Clear), BindingFlags.Instance | BindingFlags.Public);
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, shipDecorSelectionInfo),
                new(OpCodes.Callvirt, listClearInfo));

            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not match Terminal 'ShipDecorSelection' list clearing.");

                return instructions;
            }

            MethodInfo rotateShipDecorSelectionPatchInfo = typeof(RotateShipDecorSelectionPatch).GetMethod(nameof(RotateShipDecorSelection), BindingFlags.Static | BindingFlags.NonPublic);
            return codeMatcher.Insert(
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, rotateShipDecorSelectionPatchInfo),
                new(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }
}