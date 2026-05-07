using System;
using UnityEngine;

namespace Darkmatter.Fonepay
{
    // <summary>
    // ScriptableObject. Holds non-secret config fields serialised to disk. Secret key and password are injected at runtime only — never serialised.
    // </summary>
    public class FonepayConfigSO : ScriptableObject
    {
        [SerializeField] private FonepayEnvironment environment;
        [SerializeField] private string merchantCode;
        [SerializeField] private string username;

        private string _password;
        private string _secretKey;
        private bool _credentialsSet;

        // ── public read access ────────────────────────────────────────────

        public FonepayEnvironment Environment => environment;
        public string MerchantCode => merchantCode;
        public string Username => username;

        // ── runtime credential injection (called by FonepayPlayModeInjector) ──

        public void SetCredentials(string password, string secretKey)
        {
            _password = password;
            _secretKey = secretKey;
            _credentialsSet = true;
        }

        private const string FonepayLiveEndpoint = "https://merchantapi.fonepay.com/api/";
        private const string FonepayDevEndpoint = "https://dev-merchantapi.fonepay.com/api/";

        // ── used internally by FonepayApiClient and HmacSha512Signer ─────

        internal string GetPassword() => GuardCredentials(_password);
        internal string GetSecretKey() => GuardCredentials(_secretKey);

        // ── URL resolution ────────────────────────────────────────────────

        public string ResolveBaseUrl() => environment == FonepayEnvironment.Live
            ? FonepayLiveEndpoint
            : FonepayDevEndpoint;

        // ── guards ────────────────────────────────────────────────────────

        private string GuardCredentials(string value)
        {
            if (!_credentialsSet)
                throw new FonepayError(0,
                    "Credentials not set. Call SetCredentials() before using FonepayClient.",
                    "FonepayConfig.SetCredentials");
            return value;
        }
    }
}