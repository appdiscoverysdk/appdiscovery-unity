namespace AppDiscovery.Internal
{
    /// <summary>
    /// Implemented by the platform bridges (Android, iOS, Editor). Hosts arrive
    /// already validated and normalised; <c>trackerHost</c> is empty when it is the offerwall host.
    /// </summary>
    internal interface IAppDiscoveryPlatform
    {
        void InitSDK(string host, string trackerHost, string appId, string sdkKey, string playerId);
        void SetUserId(string playerId);
        void ShowOfferwall(string host, string trackerHost, string appId, string sdkKey, string playerId);
        void SyncPendingRewards(string host, string trackerHost, string appId, string sdkKey, string playerId);
    }
}
