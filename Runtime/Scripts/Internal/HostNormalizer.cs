using System;
using System.Text.RegularExpressions;

namespace AppDiscovery.Internal
{
    /// <summary>
    /// Validates and normalises host settings. Pure C# (no Unity types) so it is
    /// covered by plain unit tests. The native SDKs apply the same rules.
    ///
    /// There are deliberately no default hosts: the integrator supplies the host
    /// of the offerwall web app and, optionally, a separate tracker host.
    /// </summary>
    internal static class HostNormalizer
    {
        private static readonly Regex HostPattern = new Regex(
            "^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?(:[0-9]{1,5})?$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns a bare, lower-case <c>hostname[:port]</c>. Accepts
        /// <c>offers.example.com</c> or <c>https://offers.example.com/</c>. Rejects blank
        /// values, other schemes (cleartext http included), paths, queries and whitespace.
        /// </summary>
        /// <exception cref="ArgumentException">The value is missing or not a valid host.</exception>
        public static string Normalize(string raw, string setting = "host")
        {
            string trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException(setting + " is required", setting);
            }

            string withoutScheme;
            if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                withoutScheme = trimmed.Substring("https://".Length);
            }
            else if (trimmed.Contains("://"))
            {
                throw new ArgumentException(setting + " must be a host name or an https:// URL", setting);
            }
            else
            {
                withoutScheme = trimmed;
            }
            withoutScheme = withoutScheme.TrimEnd('/');

            if (!HostPattern.IsMatch(withoutScheme))
            {
                throw new ArgumentException(setting + " must be a plain host name such as example.com", setting);
            }
            return withoutScheme.ToLowerInvariant();
        }

        /// <summary>Like <see cref="Normalize"/> for an optional setting: blank means "not set" (null).</summary>
        public static string NormalizeOptional(string raw, string setting)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }
            return Normalize(raw, setting);
        }
    }
}
