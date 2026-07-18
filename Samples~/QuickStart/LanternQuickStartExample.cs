using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Lantern.Unity.Samples
{
    /// <summary>
    /// Minimal runtime example: reads one key from a String Table Collection that was populated by
    /// the Lantern pull window (<c>Window ▸ Lantern ▸ Pull Translations</c>). Attach it to a
    /// GameObject, set the collection name + key, and check the Console on Play.
    /// </summary>
    public class LanternQuickStartExample : MonoBehaviour
    {
        [Tooltip("The name of the String Table Collection you pulled into, e.g. \"UI\".")]
        public string tableCollectionName = "UI";

        [Tooltip("A key that exists in your Lantern project, e.g. \"home.title\".")]
        public string key = "home.title";

        private async void Start()
        {
            // Wait for the Localization system (locale selection, table loading) to be ready.
            await LocalizationSettings.InitializationOperation.Task;

            var value = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync(tableCollectionName, key).Task;

            Debug.Log($"[Lantern] {key} = {value}");
        }
    }
}
