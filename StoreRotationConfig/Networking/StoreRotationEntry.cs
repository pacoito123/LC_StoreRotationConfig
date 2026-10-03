using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace StoreRotationConfig.Networking
{
    /// <summary>
    ///     Represents an item present in the current store rotation.
    /// </summary>
    /// <param name="unlockableID">ID of the item in rotation, or its index in the unlockables list.</param>
    /// <param name="unlockablePrice">Full price of the item in rotation.</param>
    /// <param name="unlockableDiscount">Discount to apply to the item in rotation, if there is one.</param>
    /// <param name="isSuit">Whether the item in rotation is a suit or not.</param>
    [Serializable]
    public struct StoreRotationEntry(int unlockableID, int unlockablePrice, int unlockableDiscount, bool isSuit) : INetworkSerializable, IEquatable<StoreRotationEntry>
    {
        /// <summary>
        ///     ID of the item in rotation, or its index in the unlockables list.
        /// </summary>
        public readonly int UnlockableID => unlockableID;

        /// <summary>
        ///     Full price of the item in rotation.
        /// </summary>
        public readonly int UnlockablePrice => unlockablePrice;

        /// <summary>
        ///     Discount to apply to the item in rotation, if there is one.
        /// </summary>
        public readonly int UnlockableDiscount => unlockableDiscount;

        /// <summary>
        ///     Whether the item in rotation is a suit or not.
        /// </summary>
        public readonly bool IsSuit => isSuit;

        /// <summary>
        ///     Convert given <c>StoreRotationEntry</c> to its corresponding <c>TerminalNode</c>, if it has one.
        /// </summary>
        /// <param name="entry"><c>StoreRotationEntry</c> to convert.</param>
        public static implicit operator TerminalNode?(StoreRotationEntry entry)
        {
            UnlockableItem? item = entry;

            return item?.shopSelectionNode;
        }

        /// <summary>
        ///     Convert given <c>StoreRotationEntry</c> to its corresponding <c>UnlockableItem</c>, if it has one.
        /// </summary>
        /// <param name="entry"><c>StoreRotationEntry</c> to convert.</param>
        public static implicit operator UnlockableItem?(StoreRotationEntry entry)
        {
            int unlockableID = entry.UnlockableID;

            List<UnlockableItem>? unlockables = (StartOfRound.Instance != null && StartOfRound.Instance.unlockablesList != null)
                ? StartOfRound.Instance.unlockablesList.unlockables : null;

            if (unlockableID < 0 || unlockables == null || unlockableID >= unlockables.Count)
            {
                Plugin.Logger.LogError($"Could not obtain item with ID '{unlockableID}' from list of unlockable items!");

                return null;
            }

            return unlockables[unlockableID];
        }

        /// <summary>
        ///     Obtain discounted price to purchase this item.
        /// </summary>
        /// <returns>Item price with discount applied.</returns>
        public readonly int GetDiscountedPrice()
        {
            return GetDiscountedPrice(this);
        }

        /// <summary>
        ///     Obtain discounted price to purchase the given item.
        /// </summary>
        /// <param name="node">Item to purchase.</param>
        /// <returns>Item price with discount applied.</returns>
        public readonly int GetDiscountedPrice(TerminalNode? node)
        {
            return (node != null) ? node.itemCost - (int)(node.itemCost * (unlockableDiscount / 100.0f)) : -1;
        }

        /// <summary>
        ///     Obtain formatted price with discount tag for this item.
        /// </summary>
        /// <returns>Formatted price to display on the <c>Terminal</c>.</returns>
        public readonly string GetTerminalPriceString()
        {
            return GetTerminalPriceString(this);
        }

        /// <summary>
        ///     Obtain formatted price with discount tag for the given item.
        /// </summary>
        /// <param name="node">Item to obtain the price of.</param>
        /// <returns>Formatted price to display on the <c>Terminal</c>.</returns>
        public readonly string GetTerminalPriceString(TerminalNode? node)
        {
            return (node != null) ? ((unlockableDiscount == 0) ? $"{node.itemCost}" : $"{GetDiscountedPrice(node)}   ({unlockableDiscount}% OFF!)") : string.Empty;
        }

        /// <summary>
        ///     Serialize values to send over the network.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref unlockableID);
            serializer.SerializeValue(ref unlockablePrice);
            serializer.SerializeValue(ref unlockableDiscount);
            serializer.SerializeValue(ref isSuit);
        }

        /// <inheritdoc/>
        public readonly bool Equals(StoreRotationEntry other)
        {
            return unlockableID == other.UnlockableID && unlockablePrice == other.UnlockablePrice && unlockableDiscount == other.UnlockableDiscount
                && isSuit == other.IsSuit;
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            return obj is StoreRotationEntry entry && Equals(entry);
        }

        /// <inheritdoc/>
        public static bool operator ==(StoreRotationEntry left, StoreRotationEntry right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(StoreRotationEntry left, StoreRotationEntry right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(unlockableID, unlockablePrice, unlockableDiscount, isSuit);
        }
    }
}