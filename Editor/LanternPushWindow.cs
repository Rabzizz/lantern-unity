using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace Lantern.Unity.Editor
{
    /// <summary>
    /// Editor window (<c>Window ▸ Lantern ▸ Push Translations</c>) that pushes a String Table
    /// Collection's entries back to a Lantern project — the reverse of
    /// <see cref="LanternPullWindow"/>. Each locale's entries go up in one <c>PUT …/translations</c>
    /// call (an upsert), so existing keys are reused, never duplicated. Needs a <b>write-scoped</b>
    /// <c>lk_</c> token; read-only tokens are rejected with a clear message.
    /// </summary>
    public class LanternPushWindow : EditorWindow
    {
        private StringTableCollection _collection;
        private bool _createMissingKeys = true;
        private bool _includeEmpty;
        private bool _busy;
        private string _status;
        private MessageType _statusType = MessageType.None;

        [MenuItem("Window/Lantern/Push Translations")]
        public static void Open()
        {
            var window = GetWindow<LanternPushWindow>();
            window.titleContent = new GUIContent("Lantern Push");
            window.minSize = new Vector2(380, 360);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity Localization → Lantern", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Push a String Table Collection back to Lantern. Last-writer-wins: this overwrites " +
                "newer edits made on the Lantern web app for the keys you push. Needs a write-scoped " +
                "lk_ token (translation:edit; key:create to add keys, language:manage for a new locale).",
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
                _createMissingKeys = EditorGUILayout.ToggleLeft(
                    "Create keys missing from Lantern", _createMissingKeys);
                _includeEmpty = EditorGUILayout.ToggleLeft(
                    "Push empty values (overwrites Lantern with blanks)", _includeEmpty);

                EditorGUILayout.Space();
                if (GUILayout.Button(_busy ? "Pushing…" : "Push to Lantern", GUILayout.Height(30)))
                    Push();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_status, _statusType);
            }
        }

        private void Push()
        {
            if (_collection == null)
            {
                SetStatus("Pick a String Table Collection to push.", MessageType.Error);
                return;
            }

            var pushes = LanternExporter.Read(_collection, _includeEmpty);
            pushes.RemoveAll(p => p.Entries.Count == 0 || string.IsNullOrWhiteSpace(p.Locale));
            if (pushes.Count == 0)
            {
                SetStatus(
                    "Nothing to push — the collection has no non-empty entries. " +
                    "Tick \"Push empty values\" to send blanks too.",
                    MessageType.Warning);
                return;
            }

            var totalEntries = 0;
            foreach (var p in pushes) totalEntries += p.Entries.Count;

            var slug = LanternSettings.ProjectSlug;
            var confirmed = EditorUtility.DisplayDialog(
                "Push to Lantern",
                $"Push {totalEntries} value(s) across {pushes.Count} locale(s) to “{slug}”?\n\n" +
                "Last-writer-wins: this overwrites any newer edits made on the Lantern web app for " +
                "these keys. Existing keys are reused (never duplicated); keys missing from Lantern " +
                "are " + (_createMissingKeys ? "created." : "skipped."),
                "Push", "Cancel");
            if (!confirmed) return;

            _busy = true;
            SetStatus("Pushing to Lantern…", MessageType.Info);
            PushNext(pushes, 0, new LanternClient.PushResult(), new List<string>());
        }

        /// <summary>
        /// Drives the per-locale pushes sequentially: one <c>PUT</c> at a time, aggregating the
        /// counts and stopping on the first hard error (auth/scope/network) so a read-only token or
        /// a bad slug fails loudly instead of half-applying.
        /// </summary>
        private void PushNext(List<LocalePush> pushes, int index, LanternClient.PushResult totals, List<string> done)
        {
            if (index >= pushes.Count)
            {
                _busy = false;
                var message = $"Pushed {done.Count} locale(s): {totals.Updated} updated, " +
                              $"{totals.Created} created, {totals.Skipped} skipped.";
                if (done.Count > 0) message += "\nLocales: " + string.Join(", ", done) + ".";
                SetStatus(message, MessageType.Info);
                return;
            }

            var push = pushes[index];
            SetStatus($"Pushing {push.Locale} ({index + 1}/{pushes.Count})…", MessageType.Info);

            LanternClient.PushTranslations(
                LanternSettings.BaseUrl, LanternSettings.ProjectSlug, LanternSettings.Token,
                push.Locale, push.Entries.ToArray(), _createMissingKeys,
                (result, error) =>
                {
                    if (error != null || result == null)
                    {
                        _busy = false;
                        var prefix = done.Count > 0
                            ? $"Pushed {string.Join(", ", done)}, then failed on {push.Locale}: "
                            : "Push failed: ";
                        SetStatus(prefix + error, MessageType.Error);
                        return;
                    }

                    totals.Updated += result.Value.Updated;
                    totals.Created += result.Value.Created;
                    totals.Skipped += result.Value.Skipped;
                    done.Add(push.Locale);
                    PushNext(pushes, index + 1, totals, done);
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
