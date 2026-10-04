using System;
using System.Collections.Generic;
using System.Globalization;

namespace AppDiscovery.Models
{
    /// <summary>
    /// A reward delivered when the user completes an offerwall task.
    /// </summary>
    [Serializable]
    public class AppDiscoveryReward
    {
        /// <summary>
        /// The complete payload received from the native SDK. Any additional parameter
        /// sent by the server (for example click_id or sub1) is available here.
        /// </summary>
        public Dictionary<string, object> RawData { get; private set; }

        public AppDiscoveryReward(Dictionary<string, object> rawData)
        {
            RawData = rawData ?? new Dictionary<string, object>();
        }

        /// <summary>Reward amount (the "amount" field, or "payout"); 0 when absent.</summary>
        public double Amount
        {
            get
            {
                double amount = GetDouble("amount", double.NaN);
                return double.IsNaN(amount) ? GetDouble("payout", 0.0) : amount;
            }
        }

        /// <summary>Unique transaction id ("txid" or "tx_id"). Use it to avoid crediting twice.</summary>
        public string TxId
        {
            get
            {
                string id = GetString("txid");
                return id.Length > 0 ? id : GetString("tx_id");
            }
        }

        /// <summary>Reward status, for example "approved"; defaults to "approved" when absent.</summary>
        public string Status
        {
            get { return GetString("status", "approved"); }
        }

        /// <summary>Retrieves a string value from the payload.</summary>
        public string GetString(string key, string defaultValue = "")
        {
            if (RawData != null && RawData.TryGetValue(key, out var val) && val != null)
            {
                return Convert.ToString(val, CultureInfo.InvariantCulture);
            }
            return defaultValue;
        }

        /// <summary>Retrieves a numeric value from the payload.</summary>
        public double GetDouble(string key, double defaultValue = 0.0)
        {
            if (RawData != null && RawData.TryGetValue(key, out var val) && val != null)
            {
                if (double.TryParse(Convert.ToString(val, CultureInfo.InvariantCulture),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        public override string ToString()
        {
            return $"AppDiscoveryReward(Amount: {Amount}, TxId: {TxId}, Status: {Status}, Data: {RawData?.Count ?? 0} items)";
        }
    }
}
