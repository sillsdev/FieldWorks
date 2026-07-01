---
name: fieldworks-localization-review
description: "Review or change FieldWorks user-facing strings: LocalizationManager/XLIFF and legacy .resx resources, localization keys, the StringTable lane for field labels, Crowdin-facing assets, and localization-sensitive automation metadata. Use whenever a change adds or edits any user-visible text in WinForms or Avalonia, adds a new UI project, touches resource files, or claims localization parity — even for a single new label or error message."
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

## The Two Runtime Lanes (Avalonia surfaces)

1. **Field labels** come from layout data and resolve through the legacy
   StringTable lane (`XmlUtils.GetLocalizedAttributeValue`,
   `strings-{locale}.xml`) at render time. The view-definition IR carries a
   `LocalizationKey` per node; never bake English label text into the IR or
   region model.
2. **Avalonia chrome** (Save, Cancel, validation errors, unsupported-row
   text, dialog labels, accessible names) should resolve through the
   existing LocalizationManager/L10NSharp XLIFF catalog already loaded by
   the product host. Prefer existing `Palaso`/`Chorus` ids only when their
   semantics and markup truly match; otherwise add unique Avalonia-prefixed
   ids to avoid collisions.

**Current source of truth.** For Avalonia chrome, the authoritative runtime
and English-default source is the accessor code (`FwAvaloniaStrings.cs`,
`FwAvaloniaDialogsStrings.cs`) plus the XLIFF ids it calls. Legacy Avalonia
`.resx` files may still exist in the repo, but they are not the runtime lane
and should not be treated as authoritative unless a task explicitly retires
or updates them.

## Required Checks

- Product-facing user-visible strings live in the established localization
  mechanism; preview-only hardcoded text stays clearly preview-only.
- New UI mode labels, fallback or unsupported messages, validation errors,
  and diagnostics are localized before a product path is exposed.
- Stable `AutomationId` and other selectors remain nonlocalized; localized
  names, tooltips, and labels may vary by locale.
- Localization ids and inline English defaults stay aligned with existing
  Crowdin and repo conventions.
- New SDK-style csprojs declare `<RootNamespace>` explicitly — the Crowdin
  satellite-assembly build
  (`Build/Src/FwBuildTasks/Localization/ProjectLocalizer.cs`) fails
  without it if a legacy `.resx` artifact is still being carried.
- Non-product hosts that request Avalonia chrome prove their
  LocalizationManager bootstrap or their intentional English fallback.
- If localization parity is claimed, tests or evidence cover the localized
  path and confirm selectors do not depend on localized text. English on
  the Avalonia surface where legacy shows translations is a parity
  failure, not cosmetics.

## Review Red Flags

- Hardcoded English text in product C#, XAML, or product-facing
  preview-promotion paths.
- Field labels rendered raw from the IR without StringTable resolution.
- Reusing a `Palaso`/`Chorus` id whose semantics or mnemonic markup do not
  actually match the Avalonia surface.
- Tests or automation selectors depending on localized labels when stable
  IDs exist or are required.
- A product route reusing preview-only placeholder text.
- Localization claims without resource updates or without identifying
  remaining hardcoded strings.

## Handoff

List the XLIFF ids, accessor defaults, or legacy resource files touched,
remaining hardcoded product strings, automation identity strategy, and
whether localized behavior has executable evidence or is still pending.

## Keep This Skill Current

When a new localization lane, Crowdin constraint, or resource convention
appears (or a gap like the `<RootNamespace>` one is found), record it here
in the same PR; route durable lessons through
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
