using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Darkmatter.Fonepay.Editor
{
    /// <summary>
    /// Bakes EditorPrefs secrets into a temporary Resources asset before player build,
    /// then deletes the asset after the build (success or fail) so secrets never linger on disk.
    /// </summary>
    internal sealed class FonepayBuildSecretsInjector : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string AssetPath    = "Assets/Resources/FonepayBakedSecrets.asset";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!FonepaySecretsStore.HasAll())
                throw new BuildFailedException(
                    "Fonepay secrets missing. Open Tools > Fonepay > Settings before building.");

            if (!Directory.Exists(ResourcesDir))
                Directory.CreateDirectory(ResourcesDir);

            var baked = ScriptableObject.CreateInstance<FonepayBakedSecrets>();
            baked.password  = FonepaySecretsStore.GetPassword();
            baked.secretKey = FonepaySecretsStore.GetSecretKey();

            AssetDatabase.CreateAsset(baked, AssetPath);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<FonepayBakedSecrets>(AssetPath) != null)
                AssetDatabase.DeleteAsset(AssetPath);
        }
    }
}
