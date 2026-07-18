using UnityEditor;
using UnityEngine;

namespace Lantern.Unity.Editor
{
    /// <summary>
    /// Persists Lantern connection settings in <see cref="EditorPrefs"/>. The API token lives on
    /// this machine only — it is never serialized into an asset, a scene, or the build. Keys are
    /// scoped to the current project (by data path) so multiple projects on one machine don't clash.
    /// </summary>
    internal static class LanternSettings
    {
        public const string DefaultBaseUrl = "https://lantern.abyss-inn.ch";

        private static string Prefix => "Lantern." + Application.dataPath.GetHashCode() + ".";

        public static string BaseUrl
        {
            get => EditorPrefs.GetString(Prefix + "baseUrl", DefaultBaseUrl);
            set => EditorPrefs.SetString(
                Prefix + "baseUrl",
                string.IsNullOrWhiteSpace(value) ? DefaultBaseUrl : value.Trim());
        }

        public static string ProjectSlug
        {
            get => EditorPrefs.GetString(Prefix + "slug", string.Empty);
            set => EditorPrefs.SetString(Prefix + "slug", value?.Trim() ?? string.Empty);
        }

        public static string Token
        {
            get => EditorPrefs.GetString(Prefix + "token", string.Empty);
            set => EditorPrefs.SetString(Prefix + "token", value?.Trim() ?? string.Empty);
        }
    }
}
