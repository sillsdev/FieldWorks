---
name: fieldworks-localization-review
description: Use when reviewing or changing FieldWorks user-facing strings, `.resx` resources, localization keys, Crowdin-facing assets, or localization-sensitive automation metadata.
---

# FieldWorks Localization Review

## Use This For
- Product-facing text in WinForms, Avalonia, settings UI, dialogs, validation messages, fallback or unsupported-surface text, and promoted preview paths.
- `.resx` additions or changes, localization key flow, and Crowdin-sensitive resource updates.
- Automation metadata where `Name`, tooltip, or label is localized but stable `AutomationId` must remain nonlocalized.

## Required Checks
- Product-facing user-visible strings live in `.resx` or the established localization mechanism; preview-only hardcoded text must stay clearly preview-only.
- New UI mode labels, fallback or unsupported messages, validation errors, and diagnostics are localized before a product path is exposed.
- Stable `AutomationId` and other selectors remain nonlocalized; localized names, tooltips, and labels may vary by locale.
- Resource keys and files align with existing Crowdin and repo localization conventions.
- If localization parity is claimed, tests or evidence cover the localized path and confirm selectors do not depend on localized text.

## Review Red Flags
- Hardcoded English text in product C#, XAML, or product-facing preview-promotion paths.
- Tests or automation selectors depend on localized labels when stable IDs exist or are required.
- A product route reuses preview-only placeholder text.
- Localization claims are made without resource updates or without identifying remaining hardcoded strings.

## Handoff
List the resource files or keys touched, remaining hardcoded product strings, automation identity strategy, and whether localized behavior has executable evidence or is still pending.