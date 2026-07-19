using System.Collections.Generic;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Lantern.Unity.Editor
{
    /// <summary>
    /// A single <c>{ key, value }</c> pair in the shape Lantern's write API expects. Fields are
    /// public and named to match the JSON body (<c>entries:[{ key, value }]</c>) so
    /// <see cref="UnityEngine.JsonUtility"/> serializes them directly — no hand-rolled escaping.
    /// </summary>
    [System.Serializable]
    internal class LanternEntry
    {
        public string key;
        public string value;

        public LanternEntry(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>The entries to push for one locale — one <c>PUT …/translations</c> call.</summary>
    internal struct LocalePush
    {
        public string Locale;
        public List<LanternEntry> Entries;
    }

    /// <summary>
    /// Reads a Unity Localization <see cref="StringTableCollection"/> into per-locale entry lists,
    /// the reverse of <see cref="LanternImporter"/>. Pure — it performs no network I/O; the caller
    /// pushes the result via <see cref="LanternClient.PushTranslations"/>.
    /// </summary>
    internal static class LanternExporter
    {
        /// <summary>
        /// Walks the collection's shared keys against each locale's String Table, producing one
        /// <see cref="LocalePush"/> per table.
        /// </summary>
        /// <param name="includeEmpty">
        /// When <c>false</c> (the default in the UI), entries whose localized value is null/empty are
        /// omitted, so a blank Unity cell never overwrites a value that exists on the Lantern web app
        /// and empty keys aren't created. When <c>true</c>, blanks are pushed as empty strings.
        /// </param>
        public static List<LocalePush> Read(StringTableCollection collection, bool includeEmpty)
        {
            var result = new List<LocalePush>();
            if (collection == null) return result;

            var shared = collection.SharedData;
            foreach (var table in collection.StringTables)
            {
                if (table == null) continue;

                var push = new LocalePush
                {
                    Locale = table.LocaleIdentifier.Code,
                    Entries = new List<LanternEntry>(),
                };

                foreach (var sharedEntry in shared.Entries)
                {
                    if (sharedEntry == null || string.IsNullOrEmpty(sharedEntry.Key)) continue;

                    var entry = table.GetEntry(sharedEntry.Id);
                    var value = entry != null ? entry.Value : null;
                    if (!includeEmpty && string.IsNullOrEmpty(value)) continue;

                    push.Entries.Add(new LanternEntry(sharedEntry.Key, value ?? string.Empty));
                }

                result.Add(push);
            }

            return result;
        }
    }
}
