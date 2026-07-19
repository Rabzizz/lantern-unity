# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] — 2026-07-19

### Added

- **Push Translations** editor window (`Window ▸ Lantern ▸ Push Translations`) — the reverse of
  Pull. Reads a String Table Collection and pushes each locale back to Lantern via the write API
  (`PUT …/translations`, one call per locale). Requires a **write-scoped** `lk_` token
  (`translation:edit`; `key:create` to add keys, `language:manage` for a new locale).
- **Create keys missing from Lantern** toggle (maps to the write API's `createMissingKeys`).
  Pushing is an **upsert keyed by the dotted key path**, so existing keys are reused and
  **never duplicated** — pushing a collection whose keys already exist in Lantern just updates
  their values.
- **Push empty values** toggle (off by default) so a blank Unity cell doesn't overwrite a value
  that exists on the Lantern web app; empty entries are skipped unless you opt in.
- A confirm dialog before pushing that states the last-writer-wins direction, and a per-locale
  `{ updated, created, skipped }` summary afterwards.
- Write-path error mapping: a `403` on push is surfaced as a missing-scope / read-only-token
  error (not a slug mismatch), including the server's message.

## [0.1.0] — 2026-07-18

### Added

- Initial release.
- **Pull Translations** editor window (`Window ▸ Lantern ▸ Pull Translations`) that fetches a
  Lantern project's translations from the read API and imports them into a Unity Localization
  String Table Collection in one click.
- Per-machine connection settings (base URL, project slug, API token) stored in `EditorPrefs`;
  the token never enters the build.
- Explicit CSV column mapping built from Lantern's header (`key` + locale codes), so no reliance
  on Unity's default naming.
- Optional creation of missing Locales and String Tables during a pull.
- Quick Start sample and integration docs.
