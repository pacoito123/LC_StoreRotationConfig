using System;
using Unity.Netcode;

namespace StoreRotationConfig.Networking
{
    internal struct StoreRotationEntry(int unlockableID) : INetworkSerializable, IEquatable<StoreRotationEntry>
    {
        public int unlockableID = unlockableID;

        // public readonly NetworkObjectReference itemNetworkObject;

        public static implicit operator TerminalNode(StoreRotationEntry entry)
        {
            UnlockableItem? item = entry;

            return item?.shopSelectionNode!;
        }

        public static implicit operator UnlockableItem(StoreRotationEntry entry)
        {
            return StartOfRound.Instance.unlockablesList.unlockables[entry.unlockableID];
        }

        public static implicit operator StoreRotationEntry(TerminalNode node)
        {
            return (node != null) ? new(node.shipUnlockableID) : default;
        }

        public static implicit operator StoreRotationEntry(UnlockableItem item)
        {
            return (item?.shopSelectionNode != null) ? new(item.shopSelectionNode.shipUnlockableID) : default;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref unlockableID);
        }

        public readonly bool Equals(StoreRotationEntry other)
        {
            return unlockableID == other.unlockableID;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is StoreRotationEntry entry && Equals(entry);
        }

        public static bool operator ==(StoreRotationEntry left, StoreRotationEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StoreRotationEntry left, StoreRotationEntry right)
        {
            return !(left == right);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(unlockableID);
        }
    }
}