# Lantern for Unity Localization

Pull translations from a [Lantern](https://lantern.abyss-inn.ch) project **straight into
Unity's official [Localization](https://docs.unity3d.com/Packages/com.unity.localization@latest)
String Table Collections** — one click, no manual CSV files, no bespoke runtime.

Lantern stays the source of truth for your strings; this package is the editor bridge that keeps
your Unity Localization tables in sync. At runtime you use Unity Localization exactly as normal
(`LocalizedString`, `LocalizeStringEvent`, smart strings, TMP) — this package adds nothing to
your build.

> **Read-only for now.** v0.1.0 pulls Lantern → Unity. Pushing edits back from Unity to Lantern
> is planned as a follow-up.

## Requirements

- Unity **2020.3** or newer.
- The **Localization** package (`com.unity.localization` ≥ 1.5.0). It is a **dependency of this
  package**, so Package Manager installs it automatically — you don't have to add it yourself.

## Install

Package Manager ▸ **＋** ▸ **Add package from git URL…** and paste:

```
https://github.com/Rabzizz/lantern-unity.git#v0.1.0
```

(Or add `"ch.abyss-inn.lantern.unity": "https://github.com/Rabzizz/lantern-unity.git#v0.1.0"`
to your `Packages/manifest.json`.) Pin a tag as shown so upgrades are deliberate.

## Use

1. **Window ▸ Lantern ▸ Pull Translations.**
2. Fill in:
   - **Base URL** — `https://lantern.abyss-inn.ch` (default).
   - **Project slug** — your Lantern project's slug.
   - **API token** — an `lk_` token from the project's **API** tab. **Read-only is enough.**
   - **String Table Collection** — the collection to import into (create one via
     *Window ▸ Asset Management ▸ Localization Tables* if you don't have one yet).
   - **Create missing locales & tables** — when on (default), any locale in Lantern that isn't
     yet in your project is created for you.
3. Click **Pull**. Each Lantern locale becomes/updates a String Table in the collection; existing
   keys are overwritten with the latest values, and keys not present in Lantern are left alone.

Re-pull whenever translations change in Lantern.

## Token security

- The token is stored in **`EditorPrefs` on your machine only** — it is never written into an
  asset, a scene, or the built player.
- Use a **read-only** token (create it on the project's **API** tab). Rotate by revoking it in
  Lantern and creating a new one.
- Because the token is per-machine, each teammate enters their own; nothing token-related is
  committed to your repo.

## How it works

The pull calls the Lantern read API:

```
GET https://lantern.abyss-inn.ch/api/v1/projects/<slug>/translations?format=csv
Authorization: Bearer lk_…
```

Lantern returns a CSV with a `key` column plus one column per locale. The package feeds that
CSV directly into Unity Localization's importer
(`UnityEditor.Localization.Plugins.CSV.Csv.ImportInto`) with **explicit column mappings** built
from the header, so the exact names Lantern uses (a lowercase `key` column and bare locale codes)
are matched regardless of Unity's default naming. No temporary files are written.

## Prefer no package? (CSV import by hand)

You don't strictly need this package — Unity Localization can import Lantern's CSV directly:

1. Save the read-API response to a file:
   `curl -H "Authorization: Bearer lk_…" "https://lantern.abyss-inn.ch/api/v1/projects/<slug>/translations?format=csv" > lantern.csv`
2. On your String Table Collection, add the **Comma Separated Values (CSV)** extension, point it
   at `lantern.csv`, and **Import**.

This package just automates that round-trip into one button and handles the header/locale mapping
for you.

## Troubleshooting

- **401 Unauthorized** — the token is missing/invalid.
- **403 Forbidden** — the token doesn't belong to that project slug.
- **404 Not Found** — no project with that slug at that base URL.
- **"Skipped" locales** — those locales exist in Lantern but not in your Unity project, and
  *Create missing locales* was off. Turn it on, or add the Locales under
  *Edit ▸ Project Settings ▸ Localization*.

## License

MIT — see [LICENSE.md](LICENSE.md).
