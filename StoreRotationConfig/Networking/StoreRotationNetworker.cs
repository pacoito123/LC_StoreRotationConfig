using System.Collections.Generic;
using Unity.Netcode;

namespace StoreRotationConfig.Networking
{
    /// <summary>
    ///     Network handler for syncing store rotation across clients.
    /// </summary>
    public sealed class StoreRotationNetworker : NetworkBehaviour
    {
        /// <summary>
        ///     Cached <c>StoreRotationNetworker</c> instance.
        /// </summary>
        public static StoreRotationNetworker? Instance { get; private set; }

        /// <summary>
        ///     Synced list of items present in the store rotation.
        /// </summary>
        /// <remarks>Should match one-to-one with the actual <c>Terminal.ShipDecorSelection</c> list.</remarks>
        public NetworkList<StoreRotationEntry>? StoreRotation { get; private set; }

        private void Awake()
        {
            Instance = this;

            // Initialize NetworkList holding items in rotation.
            StoreRotation = new();
        }

        /// <summary>
        ///     Subscribe to list changes to update the actual <c>Terminal.ShipDecorSelection</c> list.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            StoreRotation?.OnListChanged += OnListChanged;

            if (Plugin.Terminal == null)
            {
                Plugin.Logger.LogError("Could not find Terminal instance! Store rotation doesn't exist...");

                return;
            }

            if (!IsHost && StoreRotation != null)
            {
                Plugin.Terminal.ShipDecorSelection ??= [];
                Plugin.Terminal.ShipDecorSelection.AddRange([.. StoreRotation]);

                return;
            }

            if (Plugin.Settings == null)
            {
                Plugin.Logger.LogError("Configuration could not be loaded or is missing! Store rotation won't work...");

                return;
            }

            // Parse whitelisted and blacklisted items from config file.
            Plugin.Settings.RefreshConfigLists();
        }

        /// <summary>
        ///     Handle transferring changes between synced store rotation list and the actual <c>Terminal.ShipDecorSelection</c> list.
        /// </summary>
        /// <param name="changeEvent">Event information about changes done to the synced store rotation list.</param>
        private static void OnListChanged(NetworkListEvent<StoreRotationEntry> changeEvent)
        {
            if (Plugin.Terminal == null)
            {
                Plugin.Logger.LogError("Could not find Terminal instance! Store rotation doesn't exist...");

                return;
            }

            List<TerminalNode> shipDecorSelection = Plugin.Terminal.ShipDecorSelection ??= [];
            TerminalNode? item = changeEvent.Value;

            switch (changeEvent.Type)
            {
                case NetworkListEvent<StoreRotationEntry>.EventType.Add:
                    if (item != null)
                    {
                        shipDecorSelection.Add(item);
                    }
                    break;
                case NetworkListEvent<StoreRotationEntry>.EventType.Insert:
                    if (item != null && changeEvent.Index >= 0 && changeEvent.Index < shipDecorSelection.Count)
                    {
                        shipDecorSelection.Insert(changeEvent.Index, item);
                    }
                    break;
                case NetworkListEvent<StoreRotationEntry>.EventType.Remove:
                    if (item != null)
                    {
                        _ = shipDecorSelection.Remove(item);
                    }
                    break;
                case NetworkListEvent<StoreRotationEntry>.EventType.RemoveAt:
                    if (changeEvent.Index >= 0 && changeEvent.Index < shipDecorSelection.Count)
                    {
                        shipDecorSelection.RemoveAt(changeEvent.Index);
                    }
                    break;
                case NetworkListEvent<StoreRotationEntry>.EventType.Clear:
                    shipDecorSelection.Clear();
                    break;
                case NetworkListEvent<StoreRotationEntry>.EventType.Value:
                    if (item != null && changeEvent.Index >= 0 && changeEvent.Index < shipDecorSelection.Count)
                    {
                        shipDecorSelection[changeEvent.Index] = item;
                    }
                    break;
                case NetworkListEvent<StoreRotationEntry>.EventType.Full:
                default:
                    break;
            }
        }
    }
}