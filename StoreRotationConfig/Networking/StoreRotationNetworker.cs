using Unity.Netcode;

namespace StoreRotationConfig.Networking
{
    internal sealed class StoreRotationNetworker : NetworkBehaviour
    {
        public static StoreRotationNetworker? Instance { get; private set; }

        public NetworkList<StoreRotationEntry> StoreRotation { get; internal set; }

        private void Awake()
        {
            StoreRotation = new();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (Instance == null)
            {
                Instance = this;
            }

            StoreRotation.OnListChanged += (changeEvent) =>
            {
                if (changeEvent.Type is NetworkListEvent<StoreRotationEntry>.EventType.Add)
                {
                    Plugin.Terminal.ShipDecorSelection.Add(changeEvent.Value);
                }
                else if (changeEvent.Type is NetworkListEvent<StoreRotationEntry>.EventType.Clear)
                {
                    Plugin.Terminal.ShipDecorSelection.Clear();
                }
            };
        }

        public override void OnNetworkDespawn()
        {
            Instance = null;

            base.OnNetworkDespawn();
        }
    }
}