using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEngine.Localization;

namespace Lantern.Unity.Editor
{
    /// <summary>The outcome of a Lantern → String Table Collection import.</summary>
    internal struct ImportResult
    {
        /// <summary>Locale codes that were imported into the collection.</summary>
        public List<string> Imported;

        /// <summary>
        /// Locale codes present in the CSV but skipped because no matching <see cref="Locale"/>
        /// exists in the project and "create missing locales" was off.
        /// </summary>
        public List<string> Skipped;
    }

    /// <summary>
    /// Imports Lantern's CSV export into a Unity Localization <see cref="StringTableCollection"/>.
    /// </summary>
    /// <remarks>
    /// Lantern emits a header of <c>key,&lt;locale&gt;,&lt;locale&gt;…</c> using a lowercase
    /// <c>key</c> column and bare locale codes (e.g. <c>en</c>, <c>fr</c>). Unity's default CSV
    /// mapping expects a <c>Key</c> column and matches locale columns by the locale's display name,
    /// so we build <b>explicit</b> column mappings from the actual header instead of relying on the
    /// defaults. <see cref="Csv.ImportInto"/> does not create tables/locales that are missing from
    /// the collection, so we ensure each one exists first.
    /// </remarks>
    internal static class LanternImporter
    {
        public static ImportResult Import(string csv, StringTableCollection collection, bool createMissingLocales)
        {
            var header = ParseHeader(csv);
            var result = new ImportResult { Imported = new List<string>(), Skipped = new List<string>() };
            var mappings = new List<CsvColumns>();

            // First column identifies the key; the rest are locale codes.
            var keyColumn = header.Count > 0 ? header[0] : "key";
            mappings.Add(new KeyIdColumns
            {
                KeyFieldName = keyColumn,
                IncludeId = false,
                IncludeSharedComments = false,
            });

            for (var i = 1; i < header.Count; i++)
            {
                var code = header[i];
                if (string.IsNullOrWhiteSpace(code)) continue;

                var locale = LocalizationEditorSettings.GetLocale(code);
                if (locale == null)
                {
                    if (!createMissingLocales)
                    {
                        result.Skipped.Add(code);
                        continue;
                    }

                    locale = Locale.CreateLocale(new LocaleIdentifier(code));
                    locale.name = code;
                    LocalizationEditorSettings.AddLocale(locale);
                }

                if (collection.GetTable(locale.Identifier) == null)
                    collection.AddNewTable(locale.Identifier);

                mappings.Add(new LocaleColumns
                {
                    LocaleIdentifier = locale.Identifier,
                    FieldName = code,
                    IncludeComments = false,
                });
                result.Imported.Add(code);
            }

            using (var reader = new StringReader(csv))
            {
                Csv.ImportInto(reader, collection, mappings, createUndo: true);
            }

            return result;
        }

        /// <summary>Reads the first CSV line and splits its columns, honouring quoted fields.</summary>
        private static List<string> ParseHeader(string csv)
        {
            var columns = new List<string>();
            if (string.IsNullOrEmpty(csv)) return columns;

            using (var reader = new StringReader(csv))
            {
                var line = reader.ReadLine();
                if (line == null) return columns;

                var field = new StringBuilder();
                var inQuotes = false;
                for (var i = 0; i < line.Length; i++)
                {
                    var c = line[i];
                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                            else inQuotes = false;
                        }
                        else field.Append(c);
                    }
                    else if (c == '"') inQuotes = true;
                    else if (c == ',') { columns.Add(field.ToString().Trim()); field.Clear(); }
                    else field.Append(c);
                }
                columns.Add(field.ToString().Trim());
            }

            return columns;
        }
    }
}
