# Changelog

## 1.0.0

- First public release of the AppDiscovery Unity SDK (Android and iOS), UPM package `com.appdiscoverysdk.sdk`, namespace `AppDiscovery`.
- `AppDiscoverySDK.Initialize(host, appId, sdkKey, playerId, trackerHost)`: the offerwall host is a required setting, there is no default. An invalid host throws an `ArgumentException`.
- `AppDiscoverySDK.ShowOfferwall`, `SetUserId` and `SyncPendingRewards`; events `OnRewardReceived`, `OnOfferwallOpened`, `OnOfferwallClosed` and `OnOfferwallError`.
- Editor mock mode simulates the offerwall flow without a device.
- Builds on the AppDiscovery Android SDK 1.0.0 (JitPack, resolved by the External Dependency Manager) and the AppDiscovery iOS SDK 1.0.0 (xcframework, bundled).
