# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
