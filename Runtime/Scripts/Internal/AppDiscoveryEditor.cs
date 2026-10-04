using System.Collections;
using UnityEngine;

namespace AppDiscovery.Internal
{
    /// <summary>
    /// Mock implementation for the Unity Editor and unsupported platforms. It prevents
    /// missing-library and JNI errors during desktop testing and simulates the callbacks.
    /// </summary>
    internal class AppDiscoveryEditor : IAppDiscoveryPlatform
    {
        private const string LOG_PREFIX = "<color=#4CAF50>[AppDiscovery SDK (Editor Mock)]</color>";

        public void InitSDK(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
            Debug.Log($"{LOG_PREFIX} Initialized with Host: '{host}', AppId: '{appId}', PlayerId: '{playerId}'.");
        }

        public void SetUserId(string playerId)
        {
            Debug.Log($"{LOG_PREFIX} Player ID updated to: '{playerId}'");
        }

        public void ShowOfferwall(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
            Debug.Log($"{LOG_PREFIX} ShowOfferwall requested for Host: '{host}', AppId: '{appId}', PlayerId: '{playerId}'.");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(sdkKey))
            {
                Debug.LogError($"{LOG_PREFIX} Cannot show the offerwall without a host, an appId and an sdkKey.");
                return;
            }

            // Simulate the user flow with delayed callbacks.
            var receiver = AppDiscoveryCallbackReceiver.Instance;
            if (receiver != null)
            {
                receiver.StartCoroutine(SimulateOfferwallFlow(receiver));
            }
        }

        public void SyncPendingRewards(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
            Debug.Log($"{LOG_PREFIX} SyncPendingRewards requested for Host: '{host}', PlayerId: '{playerId}'. No pending rewards in the Editor.");
        }

        private IEnumerator SimulateOfferwallFlow(AppDiscoveryCallbackReceiver receiver)
        {
            yield return new WaitForSeconds(0.2f);
            receiver.OnOfferwallOpenedInternal("mock_open");
            Debug.Log($"{LOG_PREFIX} Offerwall opened event dispatched.");

            yield return new WaitForSeconds(1.5f);
            string mockRewardJson = "{\"amount\":100,\"currency\":\"Coins\",\"txid\":\"mock_tx_12345\",\"status\":\"approved\"}";
            receiver.OnRewardReceivedInternal(mockRewardJson);
            Debug.Log($"{LOG_PREFIX} Reward event dispatched (100 Coins).");

            yield return new WaitForSeconds(0.5f);
            receiver.OnOfferwallClosedInternal("mock_close");
            Debug.Log($"{LOG_PREFIX} Offerwall closed event dispatched.");
        }
    }
}
