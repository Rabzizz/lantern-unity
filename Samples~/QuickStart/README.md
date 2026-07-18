# Quick Start

A minimal end-to-end example of using translations pulled from Lantern.

## 1. Set up Unity Localization (once)

If you haven't already: **Edit ▸ Project Settings ▸ Localization** → **Create** a
`Localization Settings` asset. You don't need to add Locales by hand — the pull can create
them for you.

## 2. Create a String Table Collection

**Window ▸ Asset Management ▸ Localization Tables** → **New Table Collection** →
**String Table Collection**. Give it a name (e.g. `UI`). It can start empty.

## 3. Pull from Lantern

**Window ▸ Lantern ▸ Pull Translations**:

- **Base URL** — `https://lantern.abyss-inn.ch` (default)
- **Project slug** — your Lantern project slug
- **API token** — an `lk_` token from the project's **API** tab (read-only is fine)
- **String Table Collection** — the `UI` collection you just made
- Leave **Create missing locales & tables** ticked

Click **Pull**. Your collection now holds one String Table per locale, filled from Lantern.

## 4. Read a value at runtime

Add `LanternQuickStartExample` to any GameObject, set **Table Collection Name** to `UI` and
**Key** to a key from your project (e.g. `home.title`), then press **Play**. The value for the
active locale is logged to the Console.

In real UI you'd typically use a **`LocalizeStringEvent`** component on a Text/TMP object, or a
`LocalizedString` field — this sample just shows the underlying `StringDatabase` call.
