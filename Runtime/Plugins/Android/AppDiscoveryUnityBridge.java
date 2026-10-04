package com.appdiscoverysdk.unity;

import android.app.Activity;
import android.util.Log;

import com.appdiscoverysdk.AppDiscovery;
import com.appdiscoverysdk.Offerwall;
import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import kotlin.Unit;

/**
 * Android bridge between the Unity C# layer and the AppDiscovery Android SDK.
 * The offerwall host is a required setting: there is no default.
 */
public class AppDiscoveryUnityBridge {
    private static final String TAG = "AppDiscoveryUnity";
    private static final String RECEIVER_OBJECT = "AppDiscoveryCallbackReceiver";

    private static String activeHost = "";
    private static String activeTrackerHost = "";
    private static String activeAppId = "";
    private static String activeSdkKey = "";
    private static String activePlayerId = "";

    // Pending rewards reach the app twice (through onReward and through the sync
    // callback). Keys delivered through onReward are remembered so the callback
    // can skip them.
    private static final Set<String> recentRewardKeys = new HashSet<String>();

    public static void initSDK(String host, String trackerHost, String appId, String sdkKey, String playerId) {
        activeHost = clean(host);
        activeTrackerHost = clean(trackerHost);
        activeAppId = clean(appId);
        activeSdkKey = clean(sdkKey);
        activePlayerId = clean(playerId);
        Log.d(TAG, "Initialized (host=" + activeHost + ", appId=" + activeAppId + ")");
    }

    public static void setUserId(String playerId) {
        activePlayerId = clean(playerId);
    }

    public static void showOfferwall(final Activity activity, String host, String trackerHost,
                                     String appId, String sdkKey, String playerId) {
        if (activity == null) {
            Log.e(TAG, "Cannot show offerwall: Activity is null");
            send("OnOfferwallError", "Activity is null");
            return;
        }

        final String hostArg = clean(host);
        final String h = !hostArg.isEmpty() ? hostArg : activeHost;
        // The remembered tracker host belongs to the remembered host only.
        final String tracker = !clean(trackerHost).isEmpty() ? clean(trackerHost) : (hostArg.isEmpty() ? activeTrackerHost : "");
        final String app = !clean(appId).isEmpty() ? clean(appId) : activeAppId;
        final String key = !clean(sdkKey).isEmpty() ? clean(sdkKey) : activeSdkKey;
        final String user = !clean(playerId).isEmpty() ? clean(playerId) : activePlayerId;

        if (app.isEmpty() || key.isEmpty()) {
            String err = "Cannot show offerwall: appId and sdkKey must not be empty";
            Log.e(TAG, err);
            send("OnOfferwallError", err);
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    Offerwall offerwall = AppDiscovery.INSTANCE.create(h, app, key, user, tracker.isEmpty() ? null : tracker);

                    offerwall.setOnReward(reward -> {
                        synchronized (recentRewardKeys) {
                            recentRewardKeys.add(rewardKey(reward));
                        }
                        deliverReward(reward);
                        return Unit.INSTANCE;
                    });

                    offerwall.setOnClose(() -> {
                        send("OnOfferwallClosedInternal", "");
                        return Unit.INSTANCE;
                    });

                    offerwall.launch(activity);
                    send("OnOfferwallOpenedInternal", "");
                } catch (IllegalArgumentException e) {
                    Log.e(TAG, "Invalid configuration: " + e.getMessage());
                    send("OnOfferwallError", e.getMessage() != null ? e.getMessage() : "Invalid configuration");
                } catch (Throwable t) {
                    String msg = t.getMessage() != null ? t.getMessage() : "Unknown error launching offerwall";
                    Log.e(TAG, "Exception launching offerwall: " + msg, t);
                    send("OnOfferwallError", msg);
                }
            }
        });
    }

    public static void syncPendingRewards(String host, String trackerHost, String appId, String sdkKey, String playerId) {
        final String hostArg = clean(host);
        final String h = !hostArg.isEmpty() ? hostArg : activeHost;
        final String tracker = !clean(trackerHost).isEmpty() ? clean(trackerHost) : (hostArg.isEmpty() ? activeTrackerHost : "");
        final String app = !clean(appId).isEmpty() ? clean(appId) : activeAppId;
        final String key = !clean(sdkKey).isEmpty() ? clean(sdkKey) : activeSdkKey;
        final String user = !clean(playerId).isEmpty() ? clean(playerId) : activePlayerId;

        try {
            AppDiscovery.INSTANCE.syncPendingRewards(h, app, key, user, tracker.isEmpty() ? null : tracker, rewards -> {
                for (Map<String, ?> reward : rewards) {
                    boolean alreadyDelivered;
                    synchronized (recentRewardKeys) {
                        alreadyDelivered = recentRewardKeys.remove(rewardKey(reward));
                    }
                    if (!alreadyDelivered) {
                        deliverReward(reward);
                    }
                }
                synchronized (recentRewardKeys) {
                    recentRewardKeys.clear();
                }
                return Unit.INSTANCE;
            });
        } catch (IllegalArgumentException e) {
            send("OnOfferwallError", e.getMessage() != null ? e.getMessage() : "Invalid configuration");
        } catch (Throwable t) {
            send("OnOfferwallError", t.getMessage() != null ? t.getMessage() : "Unknown error syncing rewards");
        }
    }

    private static void deliverReward(Map<String, ?> reward) {
        try {
            JSONObject json = new JSONObject(reward != null ? reward : new HashMap<String, Object>());
            send("OnRewardReceivedInternal", json.toString());
        } catch (Exception ex) {
            Log.e(TAG, "Error serializing reward callback JSON: " + ex.getMessage());
            send("OnRewardReceivedInternal", "{}");
        }
    }

    private static String rewardKey(Map<String, ?> reward) {
        Object txid = reward != null ? reward.get("txid") : null;
        if (txid == null && reward != null) txid = reward.get("tx_id");
        Object status = reward != null ? reward.get("status") : null;
        return txid + "|" + status;
    }

    private static void send(String method, String message) {
        UnityPlayer.UnitySendMessage(RECEIVER_OBJECT, method, message != null ? message : "");
    }

    private static String clean(String value) {
        return value == null ? "" : value.trim();
    }
}
