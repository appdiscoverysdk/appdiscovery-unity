using UnityEngine;
using AppDiscovery;
using AppDiscovery.Models;

/// <summary>
/// Minimal example: initialize the SDK with the host of your offerwall, listen for
/// rewards and show the offerwall.
/// </summary>
public class AppDiscoveryExample : MonoBehaviour
{
    [Header("From your dashboard")]
    [Tooltip("Host of YOUR offerwall. Required, there is no default.")]
    [SerializeField] private string host = "offers.example.com";
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string sdkKey = "YOUR_SDK_KEY";

    [Header("Player")]
    [SerializeField] private string playerId = "player_123";

    private void OnEnable()
    {
        AppDiscoverySDK.OnRewardReceived += HandleReward;
        AppDiscoverySDK.OnOfferwallClosed += HandleClosed;
        AppDiscoverySDK.OnOfferwallError += HandleError;
    }

    private void OnDisable()
    {
        AppDiscoverySDK.OnRewardReceived -= HandleReward;
        AppDiscoverySDK.OnOfferwallClosed -= HandleClosed;
        AppDiscoverySDK.OnOfferwallError -= HandleError;
    }

    private void Start()
    {
        // Throws an ArgumentException when the host is missing or invalid.
        AppDiscoverySDK.Initialize(host, appId, sdkKey, playerId);

        // Rewards earned while the app was closed arrive through OnRewardReceived.
        AppDiscoverySDK.SyncPendingRewards();
    }

    /// <summary>Hook this up to a UI button.</summary>
    public void ShowOfferwall()
    {
        AppDiscoverySDK.ShowOfferwall();
    }

    private void HandleReward(AppDiscoveryReward reward)
    {
        // Use the transaction id to avoid crediting twice.
        Debug.Log($"Reward {reward.Amount} (txid {reward.TxId}, {reward.Status})");
    }

    private void HandleClosed() => Debug.Log("Offerwall closed");

    private void HandleError(string message) => Debug.LogError($"Offerwall error: {message}");
}
