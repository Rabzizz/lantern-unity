# Lantern for Unity Localization

This package bridges [Lantern](https://lantern.abyss-inn.ch) — a self-hosted localization
management platform — and Unity's official **Localization** package. It pulls a Lantern
project's translations and imports them into a String Table Collection, so your Unity project
stays in sync with Lantern without any manual file juggling.

It is **editor-only**: nothing ships in your build. At runtime you rely entirely on Unity
Localization.

## Concepts

| Lantern | Unity Localization |
| --- | --- |
| Project | A String Table Collection (you choose which one) |
| Locale (e.g. `fr`) | A `Locale` + one String Table in the collection |
| Key (e.g. `home.title`) | A table entry Key |
| Translation value | The localized string for that Locale |

## The pull

`Window ▸ Lantern ▸ Pull Translations` performs:

1. `GET {baseUrl}/api/v1/projects/{slug}/translations?format=csv` with a Bearer `lk_` token.
2. Parse the CSV header (`key,<locale>,<locale>…`).
3. For each locale column, ensure a `Locale` and a String Table exist (created on demand when
   *Create missing locales & tables* is on), then map that column to the locale.
4. `Csv.ImportInto(reader, collection, mappings)` — an in-memory import, no temp files.

Existing keys are updated to Lantern's current values; keys absent from Lantern are left in
place (the import does not delete entries).

## Settings & security

Connection settings live in `EditorPrefs`, scoped per project. The API token is stored on the
local machine only and is never serialized into an asset or the build. Prefer a **read-only**
token; rotate it by revoking in Lantern and issuing a new one.

## Scope

- **v0.1.0:** read-only (Lantern → Unity).
- **Planned:** an optional push (Unity → Lantern) via Lantern's write API for teams that edit in
  Unity.

See the top-level `README.md` for install and step-by-step usage, and the **Quick Start** sample
for a minimal runtime example.
