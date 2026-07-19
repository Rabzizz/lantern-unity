# Lantern for Unity Localization

Pull translations from a [Lantern](https://lantern.abyss-inn.ch) project **straight into
Unity's official [Localization](https://docs.unity3d.com/Packages/com.unity.localization@latest)
String Table Collections** — one click, no manual CSV files, no bespoke runtime.

Lantern stays the source of truth for your strings; this package is the editor bridge that keeps
your Unity Localization tables in sync. At runtime you use Unity Localization exactly as normal
(`LocalizedString`, `LocalizeStringEvent`, smart strings, TMP) — this package adds nothing to
your build.

**Both directions:** **Pull** brings Lantern → Unity; **Push** sends Unity edits → Lantern. Pull
needs only a read-only token; push needs a write-scoped one.

## Requirements

- Unity **2020.3** or newer.
- The **Localization** package (`com.unity.localization` ≥ 1.5.0). It is a **dependency of this
  package**, so Package Manager installs it automatically — you don't have to add it yourself.

## Install

Package Manager ▸ **＋** ▸ **Add package from git URL…** and paste:

```
https://github.com/Rabzizz/lantern-unity.git#v0.2.0
```

(Or add `"ch.abyss-inn.lantern.unity": "https://github.com/Rabzizz/lantern-unity.git#v0.2.0"`
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

## Push back to Lantern

For teams that edit strings in Unity, **Window ▸ Lantern ▸ Push Translations** sends a String
Table Collection back the other way.

1. Fill in the same **Base URL** / **Project slug**, but use a **write-scoped** `lk_` token — one
   with at least **`translation:edit`** (add **`key:create`** to create keys that don't exist in
   Lantern yet, and **`language:manage`** to auto-create a locale). A **read-only token is
   rejected** with a clear message.
2. Pick the **String Table Collection** to push.
3. Toggles:
   - **Create keys missing from Lantern** *(on)* — keys in your collection that Lantern doesn't
     have yet are created. Off, they're skipped.
   - **Push empty values** *(off)* — by default a blank Unity cell is **not** sent, so it can't
     wipe a value that exists on the Lantern web app. Turn it on to push blanks too.
4. Click **Push to Lantern**, confirm the summary, and each locale is pushed in one call. You get
   a per-run `updated / created / skipped` count.

**Direction & safety.** Push is **last-writer-wins with no merge** — it overwrites newer web edits
for the keys you push (and a web edit later overwrites yours). Pushing is an **upsert keyed by the
dotted key path**, so keys that already exist in Lantern are **updated in place, never
duplicated**. Push is always **manual** — nothing is sent automatically as you edit.

## Token security

- The token is stored in **`EditorPrefs` on your machine only** — it is never written into an
  asset, a scene, or the built player.
- Create it on the project's **API** tab: a **read-only** token is enough to **pull**; a
  **write-scoped** token (`translation:edit`, plus `key:create` / `language:manage` as needed) is
  required to **push**. Grant only the scopes that machine needs. Rotate by revoking in Lantern
  and creating a new one.
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

The push walks the collection's shared keys against each locale's String Table and calls the
write API **once per locale**:

```
PUT https://lantern.abyss-inn.ch/api/v1/projects/<slug>/translations
Authorization: Bearer lk_…
{ "locale": "fr", "entries": [{ "key": "home.title", "value": "Accueil" }], "createMissingKeys": true }
```

That endpoint is a server-side **upsert**: it resolves each key by its dotted path, updates the
value if the key exists and (with `createMissingKeys` + `key:create`) creates it if not — so a
push can't create duplicate keys. It returns `{ updated, created, skipped }`, which the window
sums across locales.

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
- **403 Forbidden** — on a **pull**, the token doesn't belong to that project slug; on a **push**,
  the token is read-only or lacks the write scope. Push needs `translation:edit` (plus
  `key:create` / `language:manage` for new keys / locales).
- **404 Not Found** — no project with that slug at that base URL.
- **Pull "Skipped" locales** — those locales exist in Lantern but not in your Unity project, and
  *Create missing locales* was off. Turn it on, or add the Locales under
  *Edit ▸ Project Settings ▸ Localization*.
- **Push "skipped" count** — entries Lantern didn't write: unknown keys when *Create keys missing
  from Lantern* is off, or (with it on) a token without `key:create`. Empty Unity cells are
  skipped client-side unless *Push empty values* is on.
- **"Nothing to push"** — the collection has no non-empty entries for any locale. Enter values, or
  tick *Push empty values*.

## License

MIT — see [LICENSE.md](LICENSE.md).
