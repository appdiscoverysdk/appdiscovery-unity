using System;
using System.Collections.Generic;
using UnityEngine;
using AppDiscovery.Models;

namespace AppDiscovery.Internal
{
    /// <summary>
    /// Hidden persistent GameObject that receives callbacks from the native Android and
    /// iOS code via UnitySendMessage and raises the AppDiscoverySDK events.
    /// The object name and the method names are the contract with the native bridges.
    /// </summary>
    [AddComponentMenu("")]
    public class AppDiscoveryCallbackReceiver : MonoBehaviour
    {
        internal const string GAME_OBJECT_NAME = "AppDiscoveryCallbackReceiver";
        private static AppDiscoveryCallbackReceiver _instance;

        public static AppDiscoveryCallbackReceiver Instance
        {
            get
            {
                if (_instance == null)
                {
                    var existing = GameObject.Find(GAME_OBJECT_NAME);
                    if (existing != null)
                    {
                        _instance = existing.GetComponent<AppDiscoveryCallbackReceiver>();
                    }

                    if (_instance == null)
                    {
                        var go = new GameObject(GAME_OBJECT_NAME);
                        _instance = go.AddComponent<AppDiscoveryCallbackReceiver>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Invoked by the native code when the offerwall opens.</summary>
        public void OnOfferwallOpenedInternal(string message)
        {
            AppDiscoverySDK.DispatchOfferwallOpened();
        }

        /// <summary>Invoked by the native code when the offerwall is closed.</summary>
        public void OnOfferwallClosedInternal(string message)
        {
            AppDiscoverySDK.DispatchOfferwallClosed();
        }

        /// <summary>Invoked by the native code when a reward is received (JSON string).</summary>
        public void OnRewardReceivedInternal(string jsonString)
        {
            try
            {
                var dict = MiniJsonParser.Deserialize(jsonString);
                AppDiscoverySDK.DispatchRewardReceived(new AppDiscoveryReward(dict));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AppDiscovery SDK] Failed to parse reward callback JSON: {ex.Message}");
                AppDiscoverySDK.DispatchRewardReceived(new AppDiscoveryReward(new Dictionary<string, object>()));
            }
        }

        /// <summary>Invoked by the native code when an error occurs during launch.</summary>
        public void OnOfferwallError(string errorMessage)
        {
            Debug.LogError($"[AppDiscovery SDK] Native offerwall error: {errorMessage}");
            AppDiscoverySDK.DispatchOfferwallError(errorMessage);
        }
    }
}
