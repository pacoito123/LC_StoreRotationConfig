using HarmonyLib;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using Unity.Netcode;
using StoreRotationConfig.Networking;

namespace StoreRotationConfig.Patches
{
    /// <summary>
    ///     Patches for creating and spawning Networker instance.
    /// </summary>
    internal static class NetworkingInitPatches
    {
        public static StoreRotationNetworker? NetworkerPrefab
        {
            get
            {
                if (field == null)
                {
                    GameObject networkerContainer = new("StoreRotationNetworker")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    try
                    {
                        byte[] hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().GetName().Name));
                        networkerContainer.AddComponent<NetworkObject>().GlobalObjectIdHash = BitConverter.ToUInt32(hash, 0);
                    }
                    catch (Exception e)
                    {
                        Plugin.Logger.LogError($"Could not override default networker hash: {e}");
                    }

                    field = networkerContainer.AddComponent<StoreRotationNetworker>();
                }

                return field;
            }
            private set;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void StartOfRoundAwake_Prefix(StartOfRound __instance)
        {
            if (!__instance.NetworkManager.IsHost)
            {
                return;
            }

            if (NetworkerPrefab == null)
            {
                Plugin.Logger.LogError("Networker prefab is missing and could not be created; mod won't be able to do anything!");

                return;
            }

            GameObject prefabInstance = UnityEngine.Object.Instantiate(NetworkerPrefab.gameObject);
            prefabInstance.hideFlags = HideFlags.None;

            if (prefabInstance.TryGetComponent(out NetworkObject prefabNetworkObject))
            {
                prefabNetworkObject.Spawn(true);
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void GameNetworkManagerStart_Prefix(GameNetworkManager __instance)
        {
            if (!__instance.TryGetComponent(out NetworkManager networkManager))
            {
                return;
            }

            if (NetworkerPrefab == null)
            {
                Plugin.Logger.LogError("Networker prefab is missing and could not be created; mod won't be able to do anything!");

                return;
            }

            if (networkManager.NetworkConfig?.Prefabs.Contains(NetworkerPrefab.gameObject) == true)
            {
                networkManager.AddNetworkPrefab(NetworkerPrefab.gameObject);
            }
        }
    }
}