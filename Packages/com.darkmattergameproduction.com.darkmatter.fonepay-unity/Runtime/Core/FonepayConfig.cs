using System;
using UnityEngine;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// Static accessor. Returns ready-to-use <see cref="FonepayConfigSO"/> with credentials injected.
    /// Editor: secrets supplied via <see cref="EditorSecretsProvider"/> hook (set by FonepaySecretsStore on editor load).
    /// Builds: secrets read from <see cref="FonepayBakedSecrets"/> resource (written by build preprocessor, removed after build).
    /// </summary>
    public static class FonepayConfig
    {
        private const string ResourcePath = "FonepayConfig";
        private const string BakedSecretsResource = "FonepayBakedSecrets";

        public static Func<(string password, string secretKey)> EditorSecretsProvider;

        private static FonepayConfigSO _cached;

        public static FonepayConfigSO Load()
        {
            if (_cached != null) return _cached;

            var so = Resources.Load<FonepayConfigSO>(ResourcePath);
            if (so == null)
                throw new FonepayError(0,
                    "FonepayConfig asset missing. Open Tools > Fonepay > Settings to create it.",
                    "FonepayConfig.Load");

            string password = null;
            string secretKey = null;

            if (Application.isEditor && EditorSecretsProvider != null)
            {
                var creds = EditorSecretsProvider();
                password = creds.password;
                secretKey = creds.secretKey;
            }
            else
            {
                var baked = Resources.Load<FonepayBakedSecrets>(BakedSecretsResource);
                if (baked != null)
                {
                    password = baked.password;
                    secretKey = baked.secretKey;
                }
            }

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(secretKey))
                throw new FonepayError(0,
                    "Fonepay credentials missing. Open Tools > Fonepay > Settings.",
                    "FonepayConfig.Load");

            so.SetCredentials(password, secretKey);
            _cached = so;
            return so;
        }

        public static void Invalidate() => _cached = null;
    }
}
