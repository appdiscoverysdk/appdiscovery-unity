using System;
using UnityEngine;

namespace AppDiscovery.Internal
{
    /// <summary>
    /// Android bridge using JNI. Calls com.appdiscoverysdk.unity.AppDiscoveryUnityBridge,
    /// which wraps the AppDiscovery Android SDK.
    /// </summary>
    internal class AppDiscoveryAndroid : IAppDiscoveryPlatform
    {
        private const string BRIDGE_CLASS_NAME = "com.appdiscoverysdk.unity.AppDiscoveryUnityBridge";
        private const string UNITY_PLAYER_CLASS_NAME = "com.unity3d.player.UnityPlayer";

        public void InitSDK(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridgeClass = new AndroidJavaClass(BRIDGE_CLASS_NAME))
                {
                    bridgeClass.CallStatic("initSDK", host, trackerHost, appId, sdkKey, playerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Android JNI InitSDK failed: {ex.Message}");
            }
#endif
        }

        public void SetUserId(string playerId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridgeClass = new AndroidJavaClass(BRIDGE_CLASS_NAME))
                {
                    bridgeClass.CallStatic("setUserId", playerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Android JNI SetUserId failed: {ex.Message}");
            }
#endif
        }

        public void ShowOfferwall(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass(UNITY_PLAYER_CLASS_NAME))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var bridgeClass = new AndroidJavaClass(BRIDGE_CLASS_NAME))
                {
                    bridgeClass.CallStatic("showOfferwall", currentActivity, host, trackerHost, appId, sdkKey, playerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Android JNI ShowOfferwall failed: {ex.Message}");
            }
#endif
        }

        public void SyncPendingRewards(string host, string trackerHost, string appId, string sdkKey, string playerId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridgeClass = new AndroidJavaClass(BRIDGE_CLASS_NAME))
                {
                    bridgeClass.CallStatic("syncPendingRewards", host, trackerHost, appId, sdkKey, playerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Android JNI SyncPendingRewards failed: {ex.Message}");
            }
#endif
        }
    }
}
