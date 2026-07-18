using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace Lantern.Unity.Editor
{
    /// <summary>
    /// Editor window (<c>Window ▸ Lantern ▸ Pull Translations</c>) that pulls a Lantern project's
    /// translations and imports them into a chosen String Table Collection in one click.
    /// </summary>
    public class LanternPullWindow : EditorWindow
    {
        private StringTableCollection _collection;
        private bool _createMissingLocales = true;
        private bool _busy;
        private string _status;
        private MessageType _statusType = MessageType.None;

        [MenuItem("Window/Lantern/Pull Translations")]
        public static void Open()
        {
            var window = GetWindow<LanternPullWindow>();
            window.titleContent = new GUIContent("Lantern");
            window.minSize = new Vector2(380, 340);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Lantern → Unity Localization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Pull a Lantern project's translations into a String Table Collection. The API token " +
                "is stored on this machine (EditorPrefs) and never goes into your build.",
                MessageType.None);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_busy))
            {
                LanternSettings.BaseUrl = EditorGUILayout.TextField("Base URL", LanternSettings.BaseUrl);
                LanternSettings.ProjectSlug = EditorGUILayout.TextField("Project slug", LanternSettings.ProjectSlug);
                LanternSettings.Token = EditorGUILayout.PasswordField("API token (lk_…)", LanternSettings.Token);

                EditorGUILayout.Space();
                _collection = (StringTableCollection)EditorGUILayout.ObjectField(
                    "String Table Collection", _collection, typeof(StringTableCollection), false);
                _createMissingLocales = EditorGUILayout.ToggleLeft(
                    "Create missing locales & tables", _createMissingLocales);

                EditorGUILayout.Space();
                if (GUILayout.Button(_busy ? "Pulling…" : "Pull", GUILayout.Height(30)))
                    Pull();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_status, _statusType);
            }
        }

        private void Pull()
        {
            if (_collection == null)
            {
                SetStatus("Pick a String Table Collection to import into.", MessageType.Error);
                return;
            }

            _busy = true;
            SetStatus("Contacting Lantern…", MessageType.Info);

            LanternClient.FetchCsv(
                LanternSettings.BaseUrl, LanternSettings.ProjectSlug, LanternSettings.Token,
                (csv, error) =>
                {
                    _busy = false;

                    if (error != null)
                    {
                        SetStatus("Pull failed: " + error, MessageType.Error);
                        return;
                    }

                    try
                    {
                        var result = LanternImporter.Import(csv, _collection, _createMissingLocales);
                        AssetDatabase.SaveAssets();

                        var message = "Imported " + result.Imported.Count + " locale(s)";
                        if (result.Imported.Count > 0)
                            message += ": " + string.Join(", ", result.Imported);
                        message += ".";

                        if (result.Skipped.Count > 0)
                        {
                            message += "\nSkipped (no matching Locale in the project — add them in " +
                                       "Localization settings, or tick \"Create missing locales\"): " +
                                       string.Join(", ", result.Skipped) + ".";
                            SetStatus(message, MessageType.Warning);
                        }
                        else
                        {
                            SetStatus(message, MessageType.Info);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        SetStatus("Import failed: " + ex.Message, MessageType.Error);
                        Debug.LogException(ex);
                    }
                });
        }

        private void SetStatus(string message, MessageType type)
        {
            _status = message;
            _statusType = type;
            Repaint();
        }
    }
}
