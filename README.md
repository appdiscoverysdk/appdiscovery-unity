# AppDiscovery SDK for Unity

A Unity package that adds an offerwall to your Android and iOS game. Your players earn rewards by completing offers, and your game is notified through C# events.

The SDK is configured with the host of your own offerwall (given to you by the network you publish for). There is no default host.

## Requirements

| Requirement | Version |
|---|---|
| Unity | 2021.3 or newer |
| Android minSdk | 21, Java 17 |
| iOS deployment target | 13.0+ |
| [External Dependency Manager for Unity](https://github.com/googlesamples/unity-jar-resolver) (EDM4U) | Android only: resolves the native SDK from JitPack |

## Installation

In Unity open **Window > Package Manager**, click **+ > Add package from git URL...** and enter:

```
https://github.com/appdiscoverysdk/appdiscovery-unity.git#1.0.0
```

or add this line to `Packages/manifest.json`:

```json
"com.appdiscoverysdk.sdk": "https://github.com/appdiscoverysdk/appdiscovery-unity.git#1.0.0"
```

### Android

Install EDM4U (Package Manager, or the `.unitypackage` from its repository). On the next build it resolves the native SDK `com.github.appdiscoverysdk:appdiscovery-android` from JitPack, plus the AndroidX libraries it needs, using `Editor/AppDiscoveryDependencies.xml`. Use **Assets > External Dependency Manager > Android Resolver > Force Resolve** to resolve right away.

Without EDM4U, add the dependency to your custom `mainTemplate.gradle` yourself:

```groovy
implementation 'com.github.appdiscoverysdk:appdiscovery-android:1.0.0'
implementation 'androidx.core:core-ktx:1.10.1'
implementation 'com.google.android.gms:play-services-ads-identifier:18.2.0'
```

and add `maven { url 'https://jitpack.io' }` to the repositories (`settingsTemplate.gradle` on Unity 2022.2+). The permissions (`INTERNET`, `ACCESS_NETWORK_STATE`, `AD_ID`) and the offerwall activity are merged automatically.

### iOS

The native iOS SDK (`AppDiscoverySDK.xcframework`) is bundled with the package as an iOS plugin that Unity links and embeds (Unity 2021.3+). A post-build step sets the Swift version and the `-ObjC` linker flag. Build for iOS and open the generated Xcode project as usual.

## Quick start

```csharp
using UnityEngine;
using AppDiscovery;
using AppDiscovery.Models;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        AppDiscoverySDK.OnRewardReceived += OnReward;

        AppDiscoverySDK.Initialize(
            host: "offers.example.com",   // Host of your offerwall (required)
            appId: "YOUR_APP_ID",
            sdkKey: "YOUR_SDK_KEY",
            playerId: "player_123");      // Unique id of the player in your game

        AppDiscoverySDK.SyncPendingRewards();   // rewards earned while the game was closed
    }

    public void OnShowOfferwallButton() => AppDiscoverySDK.ShowOfferwall();

    void OnReward(AppDiscoveryReward reward)
    {
        // Use the transaction id to avoid crediting twice.
        Debug.Log($"Reward {reward.Amount}, transaction {reward.TxId}");
    }

    void OnDestroy() => AppDiscoverySDK.OnRewardReceived -= OnReward;
}
```

A sample scene script is available from the Package Manager (**Samples > Basic usage**).

## API

### AppDiscoverySDK (namespace `AppDiscovery`)

| Member | Description |
|---|---|
| `Initialize(host, appId, sdkKey, playerId = "", trackerHost = null)` | Stores the configuration. `host` is a plain host name (an `https://` URL is accepted). Throws an `ArgumentException` when it is missing or invalid. `trackerHost` is only needed when reward sync runs on a different host. |
| `SetUserId(playerId)` | Changes the active player, for example after a login. |
| `ShowOfferwall(playerId = "")` | Shows the offerwall with the initialized configuration. |
| `ShowOfferwall(host, appId, sdkKey, playerId, trackerHost = null)` | Shows the offerwall with an explicit configuration. |
| `SyncPendingRewards()` | Delivers rewards earned while the game was closed, through `OnRewardReceived`. |
| `OnRewardReceived`, `OnOfferwallOpened`, `OnOfferwallClosed`, `OnOfferwallError` | Events. |
| `GetHost()`, `GetTrackerHost()`, `GetAppId()`, `GetSdkKey()`, `GetPlayerId()`, `IsInitialized()` | Read the stored configuration. |

### AppDiscoveryReward (namespace `AppDiscovery.Models`)

| Member | Description |
|---|---|
| `Amount` | Reward amount |
| `TxId` | Unique transaction id, use it to avoid crediting twice |
| `Status` | `approved`, `pending`, `reversed` or `rejected` |
| `GetString(key)`, `GetDouble(key)`, `RawData` | Any other parameter sent by the server |

## Editor mode

In the Unity Editor the SDK runs a mock: it logs the calls and simulates opening the offerwall, one reward and closing, so you can test your game loop without a device.

## Troubleshooting

- **`ArgumentException` mentioning `host`:** `host` must be a plain host name such as `offers.example.com` (or an `https://` URL). Cleartext `http://` is rejected.
- **`ClassNotFoundException` for `com.appdiscoverysdk` on Android:** the native SDK was not resolved; run **Android Resolver > Force Resolve** (or add the Gradle dependency above).
- **Xcode reports `No such module 'AppDiscoverySDK'`:** select `AppDiscoverySDK.xcframework` under the package's `Runtime/Plugins/iOS` in the Project window, make sure iOS is enabled and **Add to Embedded Binaries** is ticked, then build again.
- **Zero offers shown:** check that the application id matches the one registered for your `appId`, that `appId`, `sdkKey` and `host` are correct, and that the player id is not empty.

## Development

`Tests~/` holds unit tests for the C# layer that run with `dotnet test` and need no Unity installation.

## License

MIT, see [LICENSE](LICENSE).
