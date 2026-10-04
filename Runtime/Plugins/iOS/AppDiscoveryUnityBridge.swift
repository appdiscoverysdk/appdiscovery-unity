import Foundation
import UIKit
import AppDiscoverySDK

// UnitySendMessage is provided by the Unity runtime (C symbol). A private alias
// keeps this file independent of how Unity exposes its headers to Swift.
@_silgen_name("UnitySendMessage")
private func appDiscoveryUnitySendMessage(
    _ obj: UnsafePointer<CChar>?,
    _ method: UnsafePointer<CChar>?,
    _ msg: UnsafePointer<CChar>?
)

/// iOS bridge between the Unity C# layer and the AppDiscovery iOS SDK.
/// The offerwall host is a required setting: there is no default.
private enum Bridge {
    static let receiverObject = "AppDiscoveryCallbackReceiver"

    static var host = ""
    static var trackerHost = ""
    static var appId = ""
    static var sdkKey = ""
    static var playerId = ""

    // Pending rewards reach the app twice (through onReward and through the sync
    // callback). Keys delivered through onReward are remembered so the callback
    // can skip them.
    static var recentRewardKeys = Set<String>()

    static func send(_ method: String, _ message: String) {
        receiverObject.withCString { objPtr in
            method.withCString { methodPtr in
                message.withCString { msgPtr in
                    appDiscoveryUnitySendMessage(objPtr, methodPtr, msgPtr)
                }
            }
        }
    }

    static func string(_ pointer: UnsafePointer<CChar>?) -> String {
        guard let pointer = pointer else { return "" }
        return String(cString: pointer).trimmingCharacters(in: .whitespacesAndNewlines)
    }

    static func firstNonEmpty(_ preferred: String, _ fallback: String) -> String {
        return preferred.isEmpty ? fallback : preferred
    }

    struct Config {
        let host: String
        let trackerHost: String?
        let appId: String
        let sdkKey: String
        let playerId: String
    }

    static func resolve(
        _ hostArg: String, _ trackerArg: String, _ appArg: String, _ keyArg: String, _ userArg: String
    ) -> Config {
        // The remembered tracker host belongs to the remembered host only.
        let tracker = trackerArg.isEmpty ? (hostArg.isEmpty ? trackerHost : "") : trackerArg
        return Config(
            host: firstNonEmpty(hostArg, host),
            trackerHost: tracker.isEmpty ? nil : tracker,
            appId: firstNonEmpty(appArg, appId),
            sdkKey: firstNonEmpty(keyArg, sdkKey),
            playerId: firstNonEmpty(userArg, playerId)
        )
    }

    static func rewardKey(_ reward: [String: Any?]) -> String {
        func text(_ key: String) -> String? {
            if let outer = reward[key], let inner = outer {
                return "\(inner)"
            }
            return nil
        }
        let txid = text("txid") ?? text("tx_id") ?? "nil"
        return "\(txid)|\(text("status") ?? "nil")"
    }

    static func deliver(_ reward: [String: Any?]) {
        var sanitized: [String: Any] = [:]
        for (key, value) in reward {
            if let value = value {
                sanitized[key] = value
            }
        }
        do {
            let data = try JSONSerialization.data(withJSONObject: sanitized, options: [])
            send("OnRewardReceivedInternal", String(data: data, encoding: .utf8) ?? "{}")
        } catch {
            send("OnRewardReceivedInternal", "{}")
        }
    }

    static func topViewController() -> UIViewController? {
        let root = UIApplication.shared.connectedScenes
            .compactMap { $0 as? UIWindowScene }
            .flatMap { $0.windows }
            .first(where: { $0.isKeyWindow })?.rootViewController
        return top(of: root)
    }

    static func top(of viewController: UIViewController?) -> UIViewController? {
        if let nav = viewController as? UINavigationController {
            return top(of: nav.visibleViewController) ?? nav
        }
        if let tab = viewController as? UITabBarController {
            return top(of: tab.selectedViewController) ?? tab
        }
        if let presented = viewController?.presentedViewController {
            return top(of: presented)
        }
        return viewController
    }
}

@_cdecl("AppDiscovery_InitSDK")
public func AppDiscovery_InitSDK(
    _ host: UnsafePointer<CChar>?,
    _ trackerHost: UnsafePointer<CChar>?,
    _ appId: UnsafePointer<CChar>?,
    _ sdkKey: UnsafePointer<CChar>?,
    _ playerId: UnsafePointer<CChar>?
) {
    Bridge.host = Bridge.string(host)
    Bridge.trackerHost = Bridge.string(trackerHost)
    Bridge.appId = Bridge.string(appId)
    Bridge.sdkKey = Bridge.string(sdkKey)
    Bridge.playerId = Bridge.string(playerId)
}

@_cdecl("AppDiscovery_SetUserId")
public func AppDiscovery_SetUserId(_ playerId: UnsafePointer<CChar>?) {
    Bridge.playerId = Bridge.string(playerId)
}

@_cdecl("AppDiscovery_ShowOfferwall")
public func AppDiscovery_ShowOfferwall(
    _ host: UnsafePointer<CChar>?,
    _ trackerHost: UnsafePointer<CChar>?,
    _ appId: UnsafePointer<CChar>?,
    _ sdkKey: UnsafePointer<CChar>?,
    _ playerId: UnsafePointer<CChar>?
) {
    let hostArg = Bridge.string(host)
    let trackerArg = Bridge.string(trackerHost)
    let appArg = Bridge.string(appId)
    let keyArg = Bridge.string(sdkKey)
    let userArg = Bridge.string(playerId)

    DispatchQueue.main.async {
        guard let topVC = Bridge.topViewController() else {
            Bridge.send("OnOfferwallError", "Unable to find a view controller to present the offerwall")
            return
        }

        let config = Bridge.resolve(hostArg, trackerArg, appArg, keyArg, userArg)
        if config.appId.isEmpty || config.sdkKey.isEmpty {
            Bridge.send("OnOfferwallError", "Cannot show offerwall: appId and sdkKey must not be empty")
            return
        }

        do {
            let offerwall = try AppDiscovery.create(
                host: config.host,
                appId: config.appId,
                sdkKey: config.sdkKey,
                playerId: config.playerId,
                trackerHost: config.trackerHost
            )

            offerwall.onReward = { reward in
                Bridge.recentRewardKeys.insert(Bridge.rewardKey(reward))
                Bridge.deliver(reward)
            }

            offerwall.onClose = {
                Bridge.send("OnOfferwallClosedInternal", "")
            }

            offerwall.launch(viewController: topVC)
            Bridge.send("OnOfferwallOpenedInternal", "")
        } catch {
            Bridge.send("OnOfferwallError", error.localizedDescription)
        }
    }
}

@_cdecl("AppDiscovery_SyncPendingRewards")
public func AppDiscovery_SyncPendingRewards(
    _ host: UnsafePointer<CChar>?,
    _ trackerHost: UnsafePointer<CChar>?,
    _ appId: UnsafePointer<CChar>?,
    _ sdkKey: UnsafePointer<CChar>?,
    _ playerId: UnsafePointer<CChar>?
) {
    let config = Bridge.resolve(
        Bridge.string(host), Bridge.string(trackerHost), Bridge.string(appId),
        Bridge.string(sdkKey), Bridge.string(playerId)
    )

    do {
        try AppDiscovery.syncPendingRewards(
            host: config.host,
            appId: config.appId,
            sdkKey: config.sdkKey,
            playerId: config.playerId,
            trackerHost: config.trackerHost
        ) { rewards in
            for reward in rewards {
                if Bridge.recentRewardKeys.remove(Bridge.rewardKey(reward)) == nil {
                    Bridge.deliver(reward)
                }
            }
            Bridge.recentRewardKeys.removeAll()
        }
    } catch {
        Bridge.send("OnOfferwallError", error.localizedDescription)
    }
}
