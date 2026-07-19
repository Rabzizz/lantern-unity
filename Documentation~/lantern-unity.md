# Lantern for Unity Localization

This package bridges [Lantern](https://lantern.abyss-inn.ch) — a self-hosted localization
management platform — and Unity's official **Localization** package. It **pulls** a Lantern
project's translations into a String Table Collection and **pushes** Unity edits back to Lantern,
so the two stay in sync without any manual file juggling.

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

## The push

`Window ▸ Lantern ▸ Push Translations` performs the reverse:

1. Read the chosen `StringTableCollection` into `{ locale → [{ key, value }] }` — the collection's
   **shared keys** matched against each locale's String Table (`LanternExporter`). Empty cells are
   omitted unless *Push empty values* is on.
2. For each locale, `PUT {baseUrl}/api/v1/projects/{slug}/translations` with a Bearer `lk_` token
   and a `{ locale, entries, createMissingKeys }` body (built with `JsonUtility`, so string
   escaping is handled for you). The window pushes one locale per call, sequentially.
3. The endpoint is a **server-side upsert keyed by the dotted key path** (`pushTranslations` in
   Lantern's `editor-core`): it updates the value when the key exists and creates it — when
   `createMissingKeys` and the token's `key:create` scope both allow — when it doesn't. **Keys are
   never duplicated.** Each call returns `{ updated, created, skipped }`, summed across locales.

Push is **last-writer-wins, no merge**, and always manual — a confirm dialog spells out the
direction before anything is sent. A **write-scoped** token is required (`translation:edit`, plus
`key:create` / `language:manage`); a read-only token gets a clear 403 message instead of a slug
mismatch.

## Settings & security

Connection settings live in `EditorPrefs`, scoped per project. The API token is stored on the
local machine only and is never serialized into an asset or the build. A **read-only** token is
enough to pull; **push needs a write-scoped** token. Grant only the scopes that machine needs, and
rotate by revoking in Lantern and issuing a new one.

## Scope

- **v0.1.0:** read-only (Lantern → Unity).
- **v0.2.0:** adds push (Unity → Lantern) via Lantern's write API — manual, last-writer-wins,
  upsert (no duplicate keys).

See the top-level `README.md` for install and step-by-step usage, and the **Quick Start** sample
for a minimal runtime example.
