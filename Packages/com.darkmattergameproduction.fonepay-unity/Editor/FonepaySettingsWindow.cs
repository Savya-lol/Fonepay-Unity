using System.IO;
using UnityEditor;
using UnityEngine;

namespace Darkmatter.Fonepay.Editor
{
    public sealed class FonepaySettingsWindow : EditorWindow
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string ConfigAssetPath = "Assets/Resources/FonepayConfig.asset";

        private FonepayConfigSO _config;
        private SerializedObject _serialized;
        private SerializedProperty _envProp, _merchantProp, _userProp;

        private string _password;
        private string _secretKey;
        private bool _showSecrets;
        private Vector2 _scroll;

        [MenuItem("Tools/Darkmatter/Fonepay/Settings")]
        public static void Open()
        {
            var w = GetWindow<FonepaySettingsWindow>("Fonepay");
            w.minSize = new Vector2(420, 360);
            w.Show();
        }

        private void OnEnable()
        {
            LoadOrPrepare();
            _password  = FonepaySecretsStore.GetPassword();
            _secretKey = FonepaySecretsStore.GetSecretKey();
        }

        private void LoadOrPrepare()
        {
            _config = AssetDatabase.LoadAssetAtPath<FonepayConfigSO>(ConfigAssetPath);
            if (_config != null)
            {
                _serialized = new SerializedObject(_config);
                _envProp      = _serialized.FindProperty("environment");
                _merchantProp = _serialized.FindProperty("merchantCode");
                _userProp     = _serialized.FindProperty("username");
            }
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Fonepay Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Non-secret fields saved to Resources/FonepayConfig.asset (commit safe).\n" +
                "Password & Secret Key saved to EditorPrefs (per-machine, never in repo).\n" +
                "On player build, secrets baked into a temp Resources asset, removed after build.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("No FonepayConfig asset. Click below to create.", MessageType.Warning);
                if (GUILayout.Button("Create Config Asset", GUILayout.Height(28)))
                    CreateConfigAsset();
                EditorGUILayout.EndScrollView();
                return;
            }

            _serialized.Update();
            EditorGUILayout.PropertyField(_envProp);
            EditorGUILayout.PropertyField(_merchantProp);
            EditorGUILayout.PropertyField(_userProp);
            _serialized.ApplyModifiedProperties();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Secrets (EditorPrefs)", EditorStyles.boldLabel);

            _showSecrets = EditorGUILayout.ToggleLeft("Show secrets", _showSecrets);
            _password  = DrawSecret("Password",   _password);
            _secretKey = DrawSecret("Secret Key", _secretKey);

            EditorGUILayout.Space(12);
            DrawStatus();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", GUILayout.Height(28)))
                    Save();
                if (GUILayout.Button("Clear Secrets", GUILayout.Height(28)))
                    ClearSecrets();
            }

            EditorGUILayout.EndScrollView();
        }

        private string DrawSecret(string label, string value)
        {
            return _showSecrets
                ? EditorGUILayout.TextField(label, value ?? string.Empty)
                : EditorGUILayout.PasswordField(label, value ?? string.Empty);
        }

        private void DrawStatus()
        {
            var ok = !string.IsNullOrEmpty(_merchantProp.stringValue)
                  && !string.IsNullOrEmpty(_userProp.stringValue)
                  && !string.IsNullOrEmpty(_password)
                  && !string.IsNullOrEmpty(_secretKey);

            EditorGUILayout.HelpBox(
                ok ? "All required fields set." : "Missing fields — fill all values then Save.",
                ok ? MessageType.Info : MessageType.Warning);
        }

        private void Save()
        {
            _serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();

            FonepaySecretsStore.SetPassword(_password);
            FonepaySecretsStore.SetSecretKey(_secretKey);

            FonepayConfig.Invalidate();
            ShowNotification(new GUIContent("Saved"));
        }

        private void ClearSecrets()
        {
            if (!EditorUtility.DisplayDialog("Clear Fonepay secrets",
                    "Remove password and secret key from EditorPrefs on this machine?",
                    "Clear", "Cancel"))
                return;
            FonepaySecretsStore.Clear();
            _password = _secretKey = string.Empty;
            FonepayConfig.Invalidate();
        }

        private void CreateConfigAsset()
        {
            if (!Directory.Exists(ResourcesDir))
                Directory.CreateDirectory(ResourcesDir);
            var so = ScriptableObject.CreateInstance<FonepayConfigSO>();
            AssetDatabase.CreateAsset(so, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadOrPrepare();
        }
    }
}
