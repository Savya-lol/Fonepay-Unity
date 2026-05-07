using UnityEngine;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// Transient ScriptableObject containing baked secrets for player builds.
    /// Created by FonepayBuildSecretsInjector before build, deleted after build completes.
    /// Never commit this asset — added to .gitignore by the settings window.
    /// </summary>
    public class FonepayBakedSecrets : ScriptableObject
    {
        public string password;
        public string secretKey;
    }
}
