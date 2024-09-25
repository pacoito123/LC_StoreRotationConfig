using System.Collections.Generic;

namespace StoreRotationConfig.Api
{
    /// <summary>
    ///     Helper methods for interacting with the sales system for the rotating store.
    /// </summary>
    public static class RotationItemsAPI
    {
        /// <summary>
        ///     Cached list of every purchasable, non-persistent item available in the store.
        /// </summary>
        public static List<UnlockableItem> AllItems
        {
            get => _allItems ??= new(StartOfRound.Instance.unlockablesList.unlockables.Count + 1);
            private set => _allItems = value;
        }
        private static List<UnlockableItem>? _allItems;

        /// <summary>
        ///     Cached list of items to always add to the rotating store.
        /// </summary>
        public static List<UnlockableItem> PermanentItems
        {
            get => _permanentItems ??= new(StartOfRound.Instance.unlockablesList.unlockables.Count + 1);
            private set => _permanentItems = value;
        }
        private static List<UnlockableItem>? _permanentItems;

        /// <summary>
        ///     Add an item to the 'AllItems' list.
        /// </summary>
        /// <param name="item">'UnlockableItem' instance of the store item to add.</param>
        public static void RegisterItem(UnlockableItem? item)
        {
            if (item == null || AllItems.Contains(item))
            {
                return;
            }

            AllItems.Add(item);
        }

        /// <summary>
        ///     Remove an item from the 'AllItems' list.
        /// </summary>
        /// <param name="item">'UnlockableItem' instance of the store item to remove.</param>
        /// <returns>Whether or not the item was successfully removed.</returns>
        public static bool UnregisterItem(UnlockableItem? item)
        {
            return item != null && AllItems.Remove(item);
        }

        /// <summary>
        ///     Add an item to the 'PermanentItems' list.
        /// </summary>
        /// <param name="item">'UnlockableItem' instance of the store item to add as a permanent item.</param>
        public static void AddPermanentItem(UnlockableItem? item)
        {
            if (item == null || PermanentItems.Contains(item))
            {
                return;
            }

            PermanentItems.Add(item);
        }

        /// <summary>
        ///     Remove an item from the 'PermanentItems' list.
        /// </summary>
        /// <param name="item">'UnlockableItem' instance of the store item to remove as a permanent item.</param>
        /// <returns>Whether or not the item was successfully removed.</returns>
        public static bool RemovePermanentItem(UnlockableItem? item)
        {
            return item != null && PermanentItems.Remove(item);
        }
    }
}