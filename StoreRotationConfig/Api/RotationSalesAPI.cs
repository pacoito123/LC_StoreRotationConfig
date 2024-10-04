using System;
using System.Collections.Generic;

namespace StoreRotationConfig.Api
{
    /// <summary>
    ///     Helper methods for interacting with the sales system for the rotating store.
    /// </summary>
    public static class RotationSalesAPI
    {
        /// <summary>
        ///     Cached dictionary of discount values to apply, using the respective items' nodes as keys.
        /// </summary>
        private static Dictionary<TerminalNode, int>? RotationSales { get; set; }

        /// <summary>
        ///     Check if a rotating store item has a discount assigned.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the store item to check.</param>
        /// <returns>Whether or not the item is on sale.</returns>
        public static bool IsOnSale(TerminalNode? item)
        {
            return IsOnSale(item, out _);
        }

        /// <summary>
        ///     Check if a rotating store item has a discount assigned, and obtain it as an out value.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the store item to check.</param>
        /// <param name="discount">Out value of the discount to apply, or '0' if there isn't one.</param>
        /// <returns>Whether or not the item is on sale.</returns>
        public static bool IsOnSale(TerminalNode? item, out int discount)
        {
            discount = 0;

            return item != null && RotationSales != null && RotationSales.TryGetValue(item, out discount);
        }

        /// <summary>
        ///     Obtain a rotating store item's discount value.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the discounted store item.</param>
        /// <returns>The discount value of the rotating item, or '0' if there isn't one.</returns>
        public static int GetDiscount(TerminalNode? item)
        {
            _ = IsOnSale(item, out int discount);

            return discount;
        }

        /// <summary>
        ///     Obtain a rotating store item's discounted price.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the discounted store item.</param>
        /// <returns>The price of the rotating item after its discount is applied, or its full cost if the item is not on sale.</returns>
        public static int GetDiscountedPrice(TerminalNode? item)
        {
            return GetDiscountedPrice(item, out _);
        }

        /// <summary>
        ///     Obtain a rotating store item's discounted price, as well as its discount as an out value.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the discounted store item.</param>
        /// <param name="discount">Out value of the discount to apply, or '0' if there isn't one.</param>
        /// <returns>The price of the rotating item after its discount is applied, or its full cost if the item is not on sale.</returns>
        public static int GetDiscountedPrice(TerminalNode? item, out int discount)
        {
            discount = 0;

            return item != null ? (IsOnSale(item, out discount) ? (item.itemCost - (int)(item.itemCost * (discount / 100f))) : item.itemCost) : 0;
        }

        /// <summary>
        ///     Obtain a string containing a discounted item's price and its sale tag, or its full cost if the item is not on sale.
        /// </summary>
        /// <param name="item">The item about to be displayed in the terminal store page.</param>
        /// <returns>A formatted string to display in the terminal store page. </returns>
        public static string GetTerminalString(TerminalNode? item)
        {
            return $"{GetDiscountedPrice(item, out int discount)}" + ((discount > 0) ? $"   ({discount}% OFF!)" : "");
        }

        /// <summary>
        ///     Add a discount to a rotating store item.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the store item to add a discount to.</param>
        /// <param name="discount">The discount to apply, within the range '(0, 100]'.</param>
        /// <returns>Whether or not the discount was successfully added to the 'RotationSales' dictionary.</returns>
        public static bool AddItemDiscount(TerminalNode? item, int discount)
        {
            return item != null && RotationSales != null && RotationSales.TryAdd(item, Math.Clamp(discount, 1, 100));
        }

        /// <summary>
        ///     Remove a rotating store item's discount if it has one.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the discounted store item.</param>
        /// <returns>Whether or not the discount was successfully removed.</returns>
        public static bool RemoveItemDiscount(TerminalNode? item)
        {
            return RemoveItemDiscount(item, out _);
        }

        /// <summary>
        ///     Remove a rotating store item's discount if it has one, and obtain the removed discount as an out value.
        /// </summary>
        /// <param name="item">'TerminalNode' instance of the discounted store item.</param>
        /// <param name="discount">Out value of the discount removed, or '0' if there wasn't one.</param>
        /// <returns>Whether or not the discount was successfully removed.</returns>
        public static bool RemoveItemDiscount(TerminalNode? item, out int discount)
        {
            discount = 0;

            return item != null && RotationSales != null && RotationSales.Count > 0
                && RotationSales.Remove(item, out discount);
        }

        /// <summary>
        ///     Initialize new 'RotationSales' dictionary, with its initial capacity set to however many items are to be on sale.
        /// </summary>
        /// <param name="itemsOnSale">The number of rotating store items about to go on sale, with a minimum of '1'.</param>
        public static void ResetSales(int itemsOnSale)
        {
            RotationSales = new((itemsOnSale > 0) ? itemsOnSale : 1);
        }

        /// <summary>
        ///     Obtain the number of items on sale for the current rotation.
        /// </summary>
        /// <returns>The number of rotating items on sale.</returns>
        public static int CountSales()
        {
            return RotationSales?.Count ?? 0;
        }
    }
}