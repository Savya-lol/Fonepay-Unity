using UnityEditor;

namespace Darkmatter.Fonepay.Editor
{
    [InitializeOnLoad]
    internal static class FonepayStartupCheck
    {
        private const string SessionFlag = "Darkmatter.Fonepay.StartupChecked";
        private const string ConfigAssetPath = "Assets/Resources/FonepayConfig.asset";

        static FonepayStartupCheck()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool(SessionFlag, false)) return;
            SessionState.SetBool(SessionFlag, true);

            var so = AssetDatabase.LoadAssetAtPath<FonepayConfigSO>(ConfigAssetPath);
            var hasConfig = so != null
                            && !string.IsNullOrEmpty(so.MerchantCode)
                            && !string.IsNullOrEmpty(so.Username);
            var hasSecrets = FonepaySecretsStore.HasAll();

            if (!hasConfig || !hasSecrets)
                FonepaySettingsWindow.Open();
        }
    }
}
