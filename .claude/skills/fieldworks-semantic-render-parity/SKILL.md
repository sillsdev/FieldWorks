---
name: fieldworks-semantic-render-parity
description: "Capture or review FieldWorks parity evidence: semantic snapshots, render/visual baselines, layout parity, failure artifacts, XML view definitions, and the Avalonia presentation IR. Use whenever a task creates or evaluates snapshot tests, screenshot baselines, view-definition compilation output, or any claim that an Avalonia surface matches its WinForms predecessor."
---

# FieldWorks Semantic Render Parity

Shared definitions (Path 3 bundle, evidence types, artifact naming) live in
`fieldworks-winforms-to-avalonia-migration/references/parity-evidence.md`.
This skill covers how to build and review the snapshots themselves.

## Role in the migration before/after pipeline
This skill owns the **"after"** half of the migration-doc before/after pairs (see
`fieldworks-winapp` and `Docs/migration/_TEMPLATE.md`; for where `Docs/migration/`
lives, see the hub skill's "Phase-1 Landing Strategy").
The **before** is the legacy WinForms truth
PNG (captured by the `fieldworks-winapp` launch-per-tool script / dialog harness); the **after** is
the Avalonia surface rendered **from the same seeded data** by its visual test in
`FwAvaloniaDialogsTests`/`FwAvaloniaTests`, saved as `<name>-after.png` in the doc's `images/`. Use
the same render/visual evidence type defined here (not a one-off screenshot) so the "after" doubles as the
parity baseline, and both PNGs attach to the surface's JIRA ticket. When the surfaces should match,
the semantic snapshot — not the side-by-side image — is the authoritative parity check; the images
are the human-facing summary.

## Snapshot Discipline

Semantic snapshots preserve behaviorally meaningful identity and omit
incidental layout noise. The snapshot is the anchor artifact of a parity
bundle: when visual evidence diverges, the snapshot explains whether the
cause is the XML import, slice filtering, editor registry, or rendering.

## Include

- Stable node ID and source layout/part identity.
- Which route produced the artifact (`Avalonia`, legacy fallback, or
  blocked state) when a scenario can run through multiple hosts.
- Object/class binding, field/flid binding, editor kind, writing-system
  metadata, visibility, ghost state, expansion, focus order, localization
  key, and accessibility identity.
- Unsupported construct diagnostics with enough path context to fix the
  source layout.

## Exclude Or Normalize

- Pixel bounds, transient generated names, timestamps, machine paths,
  culture-dependent ordering, and realized-control counts unless the test
  explicitly owns them.

## Canonical Examples

- IR model and snapshot projection:
  `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionModel.cs`
- Snapshot/parity tests:
  `Src/Common/FwAvalonia/FwAvaloniaTests/RegionViewingParityTests.cs`,
  `ViewDefinitionTests.cs`, `BrowseAndCanonicalJsonTests.cs`,
  `Path3BundleTests.cs`
- Import coverage tracking: `LayoutImportCoverageTests.cs` and
  `Src/Common/FwAvalonia/ViewDefinition/LayoutImportCoverage.cs`
- Visual/density evidence: `VisualParityAndDensityTests.cs`

## Render Evidence

- Pixel/render tests need deterministic fixtures, clear thresholds, and
  failure artifacts reviewers can inspect (classified failure summary, not
  a raw diff image).
- A semantic snapshot is not a substitute for visual/render parity when
  typography, density, wrapping, or native rendering seams are under
  review — and vice versa. One evidence type per axis; see
  parity-evidence.md §2.
- Control-level Avalonia visual evidence may come from Avalonia.Headless
  rendered frames when the scenario is explicitly control-scoped; desktop
  workflow/accessibility claims still need live-window evidence.

## Review Red Flags

- A preview-only or lossy route presented as if it proved product parity.
- Placeholder metadata presented as real binding or writing-system parity.
- Snapshot tests updating large JSON blobs without a small behavioral
  explanation of what changed and why.
- Cache invalidation tests that depend on sleeps or filesystem timestamp
  luck.
- A new layout construct silently dropped by the importer instead of
  producing a diagnostic node and a coverage-tracking entry.

## Handoff

State whether evidence is semantic, visual, accessibility/workflow, or
performance parity, and identify remaining unproven axes. When a Path 3
bundle is used, name each artifact and which evidence type it proves.

## Keep This Skill Current

When snapshot fields, normalization rules, or fixture patterns change, or
a new artifact type joins the bundle, update this skill and
parity-evidence.md together in the same PR; record durable lessons via
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
