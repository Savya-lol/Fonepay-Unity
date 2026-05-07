using UnityEditor;
using UnityEngine;

namespace Darkmatter.Fonepay.Editor
{
    /// <summary>
    /// Per-project secret storage backed by EditorPrefs.
    /// Keys are namespaced with project path hash so different machines/projects never collide.
    /// EditorPrefs lives in OS user store (Keychain on macOS, registry on Windows) — not in repo.
    /// </summary>
    public static class FonepaySecretsStore
    {
        private const string PrefixBase = "Darkmatter.Fonepay.";

        private static string ProjectKey =>
            PrefixBase + Application.dataPath.GetHashCode().ToString("X") + ".";

        public static string PasswordKey  => ProjectKey + "Password";
        public static string SecretKeyKey => ProjectKey + "SecretKey";

        public static string GetPassword()  => EditorPrefs.GetString(PasswordKey,  string.Empty);
        public static string GetSecretKey() => EditorPrefs.GetString(SecretKeyKey, string.Empty);

        public static void SetPassword(string v)  => EditorPrefs.SetString(PasswordKey,  v ?? string.Empty);
        public static void SetSecretKey(string v) => EditorPrefs.SetString(SecretKeyKey, v ?? string.Empty);

        public static void Clear()
        {
            EditorPrefs.DeleteKey(PasswordKey);
            EditorPrefs.DeleteKey(SecretKeyKey);
        }

        public static bool HasAll() =>
            !string.IsNullOrEmpty(GetPassword()) && !string.IsNullOrEmpty(GetSecretKey());

        [InitializeOnLoadMethod]
        private static void RegisterRuntimeProvider()
        {
            FonepayConfig.EditorSecretsProvider = () => (GetPassword(), GetSecretKey());
        }
    }
}
