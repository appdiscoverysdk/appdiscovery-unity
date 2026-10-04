using System;
using UnityEngine;
using AppDiscovery.Internal;
using AppDiscovery.Models;

namespace AppDiscovery
{
    /// <summary>
    /// AppDiscovery offerwall SDK for Unity.
    ///
    /// Every integration points the SDK at its own offerwall, so the host is a
    /// required setting and there is no default.
    /// </summary>
    public static class AppDiscoverySDK
    {
        public const string SDK_VERSION = "1.0.0";

        private static string _host = "";
        private static string _trackerHost = "";
        private static string _appId = "";
        private static string _sdkKey = "";
        private static string _playerId = "";
        private static bool _isInitialized = false;

        // Replaceable in tests.
        internal static IAppDiscoveryPlatform Platform = CreatePlatform();

        // --- C# events ---

        /// <summary>Triggered when the offerwall is displayed to the user.</summary>
        public static event Action OnOfferwallOpened;

        /// <summary>Triggered when the user dismisses the offerwall.</summary>
        public static event Action OnOfferwallClosed;

        /// <summary>Triggered when the user earns a reward (also for rewards found by <see cref="SyncPendingRewards"/>).</summary>
        public static event Action<AppDiscoveryReward> OnRewardReceived;

        /// <summary>Triggered if an error occurs while preparing or showing the offerwall.</summary>
        public static event Action<string> OnOfferwallError;

        private static IAppDiscoveryPlatform CreatePlatform()
        {
#if UNITY_EDITOR
            return new AppDiscoveryEditor();
#elif UNITY_ANDROID
            return new AppDiscoveryAndroid();
#elif UNITY_IOS
            return new AppDiscoveryIOS();
#else
            return new AppDiscoveryEditor();
#endif
        }

        /// <summary>
        /// Initializes the SDK.
        /// </summary>
        /// <param name="host">Host of your offerwall, for example <c>offers.example.com</c>
        /// (an <c>https://</c> URL is accepted). Required, there is no default.</param>
        /// <param name="appId">Your app ID from the dashboard.</param>
        /// <param name="sdkKey">Your SDK key from the dashboard.</param>
        /// <param name="playerId">Unique player identifier (can be set later with <see cref="SetUserId"/>).</param>
        /// <param name="trackerHost">Host of the tracker, when reward sync runs on a different host than the offerwall.</param>
        /// <exception cref="ArgumentException"><paramref name="host"/> (or <paramref name="trackerHost"/>) is missing or not a valid host.</exception>
        public static void Initialize(string host, string appId, string sdkKey, string playerId = "", string trackerHost = null)
        {
            string normalizedHost = HostNormalizer.Normalize(host);
            string normalizedTracker = HostNormalizer.NormalizeOptional(trackerHost, "trackerHost");

            _host = normalizedHost;
            _trackerHost = normalizedTracker ?? "";
            _appId = (appId ?? "").Trim();
            _sdkKey = (sdkKey ?? "").Trim();
            _playerId = (playerId ?? "").Trim();
            _isInitialized = true;

            // Ensure the native callback receiver is mounted in the scene.
            var _ = AppDiscoveryCallbackReceiver.Instance;

            Platform.InitSDK(_host, _trackerHost, _appId, _sdkKey, _playerId);
        }

        /// <summary>
        /// Updates the active player ID. Call this when a user logs in or changes accounts.
        /// </summary>
        public static void SetUserId(string playerId)
        {
            _playerId = (playerId ?? "").Trim();
            Platform.SetUserId(_playerId);
        }

        /// <summary>
        /// Shows the offerwall using the configuration given to <see cref="Initialize"/>.
        /// </summary>
        /// <param name="playerId">Optional override for the player ID.</param>
        public static void ShowOfferwall(string playerId = "")
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AppDiscovery SDK] Error: call AppDiscoverySDK.Initialize() before showing the offerwall.");
                return;
            }

            string user = !string.IsNullOrEmpty(playerId) ? playerId.Trim() : _playerId;
            if (string.IsNullOrEmpty(user))
            {
                Debug.LogWarning("[AppDiscovery SDK] Warning: Player ID is empty. Offers may not attribute properly without a unique user identifier.");
            }

            Platform.ShowOfferwall(_host, _trackerHost, _appId, _sdkKey, user);
        }

        /// <summary>
        /// Shows the offerwall with an explicit configuration.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="host"/> (or <paramref name="trackerHost"/>) is missing or not a valid host.</exception>
        public static void ShowOfferwall(string host, string appId, string sdkKey, string playerId, string trackerHost = null)
        {
            string normalizedHost = HostNormalizer.Normalize(host);
            string normalizedTracker = HostNormalizer.NormalizeOptional(trackerHost, "trackerHost");
            Platform.ShowOfferwall(normalizedHost, normalizedTracker ?? "", (appId ?? "").Trim(), (sdkKey ?? "").Trim(), (playerId ?? "").Trim());
        }

        /// <summary>
        /// Delivers rewards the player earned while the app was closed, without opening the offerwall.
        /// Each reward arrives through <see cref="OnRewardReceived"/>.
        /// </summary>
        public static void SyncPendingRewards()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AppDiscovery SDK] Error: call AppDiscoverySDK.Initialize() before syncing rewards.");
                return;
            }
            if (string.IsNullOrEmpty(_playerId))
            {
                Debug.LogWarning("[AppDiscovery SDK] Warning: Player ID is empty; there is nothing to sync.");
                return;
            }

            Platform.SyncPendingRewards(_host, _trackerHost, _appId, _sdkKey, _playerId);
        }

        // --- Getters ---
        public static string GetHost() => _host;
        public static string GetTrackerHost() => _trackerHost;
        public static string GetAppId() => _appId;
        public static string GetSdkKey() => _sdkKey;
        public static string GetPlayerId() => _playerId;
        public static bool IsInitialized() => _isInitialized;

        // --- Internal dispatchers (called by AppDiscoveryCallbackReceiver) ---
        internal static void DispatchOfferwallOpened()
        {
            try
            {
                OnOfferwallOpened?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Exception in OnOfferwallOpened handler: {ex.Message}");
            }
        }

        internal static void DispatchOfferwallClosed()
        {
            try
            {
                OnOfferwallClosed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Exception in OnOfferwallClosed handler: {ex.Message}");
            }
        }

        internal static void DispatchRewardReceived(AppDiscoveryReward reward)
        {
            try
            {
                OnRewardReceived?.Invoke(reward);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Exception in OnRewardReceived handler: {ex.Message}");
            }
        }

        internal static void DispatchOfferwallError(string error)
        {
            try
            {
                OnOfferwallError?.Invoke(error);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppDiscovery SDK] Exception in OnOfferwallError handler: {ex.Message}");
            }
        }

        /// <summary>Resets the stored configuration and the platform. For tests only.</summary>
        internal static void ResetForTesting(IAppDiscoveryPlatform platform)
        {
            _host = "";
            _trackerHost = "";
            _appId = "";
            _sdkKey = "";
            _playerId = "";
            _isInitialized = false;
            OnOfferwallOpened = null;
            OnOfferwallClosed = null;
            OnRewardReceived = null;
            OnOfferwallError = null;
            Platform = platform;
        }
    }
}
