using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AppDiscovery.Internal
{
    /// <summary>
    /// iOS bridge using P/Invoke ([DllImport("__Internal")]). The functions are
    /// implemented in AppDiscoveryUnityBridge.swift (Plugins/iOS), which wraps the
    /// AppDiscovery iOS SDK.
    /// </summary>
    internal class AppDiscoveryIOS : IAppDiscoveryPlatform
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AppDiscovery_InitSDK(string host, string trackerHost, string appId, string sdkKey, string playerId);

        [DllImport("__Internal")]
        private static extern void AppDiscovery_SetUserId(string playerId);

        [DllImport("__Internal")]
        private static extern void AppDiscovery_ShowOfferwall(string host, string trackerHost, string appId, string sdkKey, string playerId);

        [DllImport("__Internal")]
        private static extern void AppDiscovery_SyncPendingRewards(string host, string trackerHost, string appId, string sdkKey, string playerId);
#endif

        public void InitSDK(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                AppDiscovery_InitSDK(host ?? "", trackerHost ?? "", appId ?? "", sdkKey ?? "", playerId ?? "");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] iOS InitSDK failed: {ex.Message}");
            }
#endif
        }

        public void SetUserId(string playerId)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                AppDiscovery_SetUserId(playerId ?? "");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] iOS SetUserId failed: {ex.Message}");
            }
#endif
        }

        public void ShowOfferwall(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                AppDiscovery_ShowOfferwall(host ?? "", trackerHost ?? "", appId ?? "", sdkKey ?? "", playerId ?? "");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] iOS ShowOfferwall failed: {ex.Message}");
            }
#endif
        }

        public void SyncPendingRewards(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                AppDiscovery_SyncPendingRewards(host ?? "", trackerHost ?? "", appId ?? "", sdkKey ?? "", playerId ?? "");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] iOS SyncPendingRewards failed: {ex.Message}");
            }
#endif
        }
    }
}
