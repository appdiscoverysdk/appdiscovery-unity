using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AppDiscovery.Internal;
using AppDiscovery.Models;
using UnityEngine;
using Xunit;

namespace AppDiscovery.Tests
{
    internal class FakePlatform : IAppDiscoveryPlatform
    {
        public readonly List<string> Calls = new List<string>();

        public void InitSDK(string host, string trackerHost, string appId, string sdkKey, string playerId)
            => Calls.Add($"init|{host}|{trackerHost}|{appId}|{sdkKey}|{playerId}");

        public void SetUserId(string playerId) => Calls.Add($"user|{playerId}");

        public void ShowOfferwall(string host, string trackerHost, string appId, string sdkKey, string playerId)
            => Calls.Add($"show|{host}|{trackerHost}|{appId}|{sdkKey}|{playerId}");

        public void SyncPendingRewards(string host, string trackerHost, string appId, string sdkKey, string playerId)
            => Calls.Add($"sync|{host}|{trackerHost}|{appId}|{sdkKey}|{playerId}");
    }

    // The SDK keeps static state: run these tests one after the other.
    [Collection("sdk")]
    public class SdkTests
    {
        private readonly FakePlatform platform = new FakePlatform();

        public SdkTests()
        {
            AppDiscoverySDK.ResetForTesting(platform);
            Debug.Clear();
        }

        [Fact]
        public void InitializeRequiresAHost()
        {
            Assert.Throws<System.ArgumentException>(() => AppDiscoverySDK.Initialize("", "app", "key"));
            Assert.Throws<System.ArgumentException>(() => AppDiscoverySDK.Initialize("http://offers.example.com", "app", "key"));
            Assert.Throws<System.ArgumentException>(() => AppDiscoverySDK.Initialize("offers.example.com", "app", "key", "p", "a b"));
            Assert.False(AppDiscoverySDK.IsInitialized());
            Assert.Empty(platform.Calls);
        }

        [Fact]
        public void InitializeStoresTheConfigurationAndPassesTheNormalisedHost()
        {
            AppDiscoverySDK.Initialize("HTTPS://Offers.Example.com/", " app1 ", "key1", " player1 ", "Track.Example.com");

            Assert.True(AppDiscoverySDK.IsInitialized());
            Assert.Equal("offers.example.com", AppDiscoverySDK.GetHost());
            Assert.Equal("track.example.com", AppDiscoverySDK.GetTrackerHost());
            Assert.Equal("app1", AppDiscoverySDK.GetAppId());
            Assert.Equal("key1", AppDiscoverySDK.GetSdkKey());
            Assert.Equal("player1", AppDiscoverySDK.GetPlayerId());
            Assert.Equal("init|offers.example.com|track.example.com|app1|key1|player1", platform.Calls[0]);
        }

        [Fact]
        public void ShowOfferwallUsesTheInitialisedConfiguration()
        {
            AppDiscoverySDK.Initialize("offers.example.com", "app", "key", "p");
            AppDiscoverySDK.ShowOfferwall();
            AppDiscoverySDK.ShowOfferwall("p2");

            Assert.Equal("show|offers.example.com||app|key|p", platform.Calls[1]);
            Assert.Equal("show|offers.example.com||app|key|p2", platform.Calls[2]);
        }

        [Fact]
        public void ShowOfferwallBeforeInitializeIsAnErrorNotAGuess()
        {
            AppDiscoverySDK.ShowOfferwall();
            Assert.Empty(platform.Calls);
            Assert.Single(Debug.Errors);
        }

        [Fact]
        public void ExplicitShowOfferwallValidatesTheHost()
        {
            Assert.Throws<System.ArgumentException>(() => AppDiscoverySDK.ShowOfferwall("", "app", "key", "p"));
            Assert.Empty(platform.Calls);

            AppDiscoverySDK.ShowOfferwall("Offers.Example.com", "app", "key", "p", "track.example.com");
            Assert.Equal("show|offers.example.com|track.example.com|app|key|p", platform.Calls[0]);
        }

        [Fact]
        public void SyncPendingRewardsNeedsInitializeAndAPlayer()
        {
            AppDiscoverySDK.SyncPendingRewards();
            Assert.Empty(platform.Calls);

            AppDiscoverySDK.Initialize("offers.example.com", "app", "key");
            platform.Calls.Clear();
            AppDiscoverySDK.SyncPendingRewards();
            Assert.Empty(platform.Calls);

            AppDiscoverySDK.SetUserId("p");
            platform.Calls.Clear();
            AppDiscoverySDK.SyncPendingRewards();
            Assert.Equal("sync|offers.example.com||app|key|p", platform.Calls[0]);
        }

        [Fact]
        public void SetUserIdUpdatesThePlayer()
        {
            AppDiscoverySDK.SetUserId("  new_player  ");
            Assert.Equal("new_player", AppDiscoverySDK.GetPlayerId());
            Assert.Equal("user|new_player", platform.Calls[0]);
        }

        [Fact]
        public void NativeCallbacksRaiseTheEvents()
        {
            var rewards = new List<AppDiscoveryReward>();
            int opened = 0, closed = 0;
            var errors = new List<string>();
            AppDiscoverySDK.OnRewardReceived += rewards.Add;
            AppDiscoverySDK.OnOfferwallOpened += () => opened++;
            AppDiscoverySDK.OnOfferwallClosed += () => closed++;
            AppDiscoverySDK.OnOfferwallError += errors.Add;

            var receiver = AppDiscoveryCallbackReceiver.Instance;
            receiver.OnOfferwallOpenedInternal("");
            receiver.OnRewardReceivedInternal("{\"amount\": 12.5, \"txid\": \"tx1\", \"status\": \"approved\", \"click_id\": \"c1\", \"nested\": {\"a\": [1, 2]}}");
            receiver.OnOfferwallClosedInternal("");
            receiver.OnOfferwallError("boom");

            Assert.Equal(1, opened);
            Assert.Equal(1, closed);
            Assert.Equal(new[] { "boom" }, errors);
            var reward = Assert.Single(rewards);
            Assert.Equal(12.5, reward.Amount);
            Assert.Equal("tx1", reward.TxId);
            Assert.Equal("approved", reward.Status);
            Assert.Equal("c1", reward.GetString("click_id"));
            Assert.True(reward.RawData.ContainsKey("nested"));
        }

        [Fact]
        public void AThrowingHandlerDoesNotBreakTheReceiver()
        {
            AppDiscoverySDK.OnOfferwallClosed += () => throw new System.InvalidOperationException("handler bug");
            AppDiscoveryCallbackReceiver.Instance.OnOfferwallClosedInternal("");
            Assert.Single(Debug.Errors);
        }

        [Fact]
        public void ReceiverIsASingleton()
        {
            Assert.Same(AppDiscoveryCallbackReceiver.Instance, AppDiscoveryCallbackReceiver.Instance);
            Assert.Equal("AppDiscoveryCallbackReceiver", AppDiscoveryCallbackReceiver.Instance.gameObject.name);
        }

        [Fact]
        public void SdkVersionMatchesPackageJson()
        {
            string json = File.ReadAllText(Path.Combine(System.AppContext.BaseDirectory, "package.json"));
            string version = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
            Assert.Equal(version, AppDiscoverySDK.SDK_VERSION);
        }
    }

    public class RewardAndJsonTests
    {
        [Fact]
        public void RewardFallsBackToPayoutAndAlternativeTxidKey()
        {
            var reward = new AppDiscoveryReward(MiniJsonParser.Deserialize("{\"payout\": \"7.25\", \"tx_id\": \"abc\"}"));
            Assert.Equal(7.25, reward.Amount);
            Assert.Equal("abc", reward.TxId);
            Assert.Equal("approved", reward.Status);
        }

        [Fact]
        public void RewardToleratesMissingData()
        {
            var reward = new AppDiscoveryReward(null);
            Assert.Equal(0.0, reward.Amount);
            Assert.Equal("", reward.TxId);
            Assert.Equal("fallback", reward.GetString("missing", "fallback"));
        }

        [Fact]
        public void JsonParserHandlesObjectsArraysNumbersAndEscapes()
        {
            var dict = MiniJsonParser.Deserialize("{\"s\": \"a\\\"b\\u00e9\", \"n\": -3.5, \"b\": true, \"z\": null, \"l\": [1, \"x\"]}");
            Assert.Equal("a\"bé", dict["s"]);
            Assert.Equal(-3.5, dict["n"]);
            Assert.Equal(true, dict["b"]);
            Assert.Null(dict["z"]);
            Assert.Equal(2, ((List<object>)dict["l"]).Count);
        }

        [Fact]
        public void JsonParserReturnsAnEmptyDictionaryForEmptyOrNonObjectInput()
        {
            Assert.Empty(MiniJsonParser.Deserialize(""));
            Assert.Empty(MiniJsonParser.Deserialize(null));
            Assert.Empty(MiniJsonParser.Deserialize("[1, 2]"));
        }
    }
}
