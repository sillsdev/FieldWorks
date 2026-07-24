---
name: fieldworks-localization-review
description: "Review or change FieldWorks user-facing strings: LocalizationManager/XLIFF and legacy .resx resources, localization keys, the StringTable strategy for XML-configuration labels, Crowdin-facing assets, and localization-sensitive automation metadata. Use whenever a change adds or edits any user-visible text in WinForms or Avalonia, adds a new UI project, touches resource files, or claims localization parity — even for a single new label or error message."
---

# FieldWorks Localization Review

## Use This For

- Product-facing text in WinForms, Avalonia, settings UI, dialogs,
  validation messages, fallback or unsupported-surface text, and promoted
  preview paths.
- `.resx` additions or changes, localization key flow, and Crowdin-sensitive
  resource updates.
- Automation metadata where `Name`, tooltip, or label is localized but
  stable `AutomationId` must remain nonlocalized.

## The Localization Strategies

1. **Field labels** (text originating in XML configuration: layout/part
   labels, browse column headings, XCore menu labels) come from layout data
   and resolve through the StringTable strategy
   (`XmlUtils.GetLocalizedAttributeValue`, `strings-{locale}.xml`) at
   render time. The view-definition IR carries a `LocalizationKey` per
   node; never bake English label text into the IR or region model.
2. **FieldWorks-owned UI text** — WinForms *and* Avalonia forms, controls,
   and dialogs (Save, Cancel, validation errors, unsupported-row text,
   dialog labels, accessible names) — lives in the owning project's
   `.resx`; translations ship as Crowdin-built satellite assemblies. This
   matches the official Avalonia localization guidance
   (https://docs.avaloniaui.net/docs/app-development/localizing). For the
   Avalonia projects, `FwAvaloniaStrings`/`FwAvaloniaDialogsStrings` are
   thin accessors resolving via `ResourceManager` over
   `FwAvaloniaStrings.resx`/`FwAvaloniaDialogsStrings.resx`; a missing
   entry falls back to the string id so drift is visible, and
   `AvaloniaLocalizationTests` pins every accessor property against the
   neutral resx. No live UI-language switching is needed: FLEx already
   shows a restart-required prompt on UI-language change (LexOptionsDlg),
   matching the Avalonia doc's caveat that x:Static/resx does not
   live-update.
3. **L10NSharp/XLIFF** is only for UI supplied by Palaso, FlexBridge, or
   Chorus (or a FieldWorks form hosting an L10NSharp widget). Never add
   L10NSharp usage for FieldWorks-owned strings, and never borrow
   `Palaso`/`Chorus` catalog ids (e.g. "Common.Help") for FieldWorks-owned
   text — put such strings in the project resx with FwAvalonia-owned keys.

**Current source of truth.** For FieldWorks-owned Avalonia UI text, the
neutral `.resx` is the authoritative English source; the accessor classes
(`FwAvaloniaStrings.cs`, `FwAvaloniaDialogsStrings.cs`) are thin
`ResourceManager` wrappers over it, not an independent string source.
`FwAvaloniaLocalization`/`FwAvaloniaLocalizationBootstrap` were deleted, and
`FieldWorks.InitializeLocalizationManager` no longer registers FwAvalonia
namespaces.

## Required Checks

- Product-facing user-visible strings live in the established localization
  mechanism; preview-only hardcoded text stays clearly preview-only.
- New UI mode labels, fallback or unsupported messages, validation errors,
  and diagnostics are localized before a product path is exposed.
- Stable `AutomationId` and other selectors remain nonlocalized; localized
  names, tooltips, and labels may vary by locale.
- Resource keys and neutral-resx English entries stay aligned with existing
  Crowdin and repo conventions (same seed English as any WinForms twin so
  Crowdin translation memory matches).
- New SDK-style csprojs declare `<RootNamespace>` explicitly — it keeps
  satellite resource names stable, and the Crowdin satellite-assembly build
  (`Build/Src/FwBuildTasks/Localization/ProjectLocalizer.cs`) fails
  without it.
- Resx satellites need no runtime bootstrap; only hosts exercising genuine
  Palaso/FlexBridge/Chorus-supplied UI need an L10NSharp
  LocalizationManager, and those prove their bootstrap or their
  intentional English fallback.
- If localization parity is claimed, tests or evidence cover the localized
  path and confirm selectors do not depend on localized text. English on
  the Avalonia surface where legacy shows translations is a parity
  failure, not cosmetics.

## Review Red Flags

- Hardcoded English text in product C#, XAML, or product-facing
  preview-promotion paths.
- Field labels rendered raw from the IR without StringTable resolution.
- New L10NSharp usage — or a borrowed `Palaso`/`Chorus` catalog id — for a
  FieldWorks-owned string; those belong in the project resx with
  FieldWorks-owned keys.
- Tests or automation selectors depending on localized labels when stable
  IDs exist or are required.
- A product route reusing preview-only placeholder text.
- Localization claims without resource updates or without identifying
  remaining hardcoded strings.

## Handoff

List the resx keys, accessor properties, StringTable entries, or XLIFF ids
(Palaso/FlexBridge/Chorus UI only) touched,
remaining hardcoded product strings, automation identity strategy, and
whether localized behavior has executable evidence or is still pending.

## Keep This Skill Current

When a new localization strategy, Crowdin constraint, or resource convention
appears (or a gap like the `<RootNamespace>` one is found), record it here
in the same PR; route durable lessons through
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
