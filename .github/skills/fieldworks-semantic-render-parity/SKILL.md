---
name: fieldworks-semantic-render-parity
description: Use when capturing or reviewing FieldWorks semantic snapshots, render baselines, layout parity, failure artifacts, XML view definitions, or Avalonia presentation IR.
---

# FieldWorks Semantic Render Parity

## Snapshot Discipline
Semantic snapshots should preserve behaviorally meaningful identity and omit incidental layout noise.

## Include
- Stable node ID and source layout/part identity.
- When a scenario can run through multiple hosts or fallback states, record which route produced the artifact (`Avalonia`, legacy fallback, or blocked state).
- Object/class binding, field/flid binding, editor kind, writing-system metadata, visibility, ghost state, expansion, focus order, localization key, and accessibility identity.
- Unsupported construct diagnostics with enough path context to fix the source layout.

## Exclude Or Normalize
- Pixel bounds, transient generated names, timestamps, machine paths, culture-dependent ordering, and realized-control counts unless the test explicitly owns them.

## Render Evidence
- Pixel/render tests need deterministic fixtures, clear thresholds, and failure artifacts that reviewers can inspect.
- A semantic snapshot is not a substitute for visual/render parity when typography, density, wrapping, or native rendering seams are under review.

## Path 3 Bundle
For migration-quality visual fidelity, prefer a triangulated bundle instead of a single artifact lane:

- semantic snapshot,
- visual evidence for legacy WinForms and Avalonia,
- diff/variance artifact,
- workflow/accessibility evidence,
- one failure summary that classifies the broken lane.

Use the semantic snapshot as the anchor. Visual variance should be interpreted against stable binding/focus/accessibility identity, not in isolation.

Control-level Avalonia visual evidence may come from Avalonia.Headless rendered frames when the scenario is explicitly control-scoped. Desktop workflow/accessibility claims still need live-window evidence.

## Review Red Flags
- A preview-only or lossy route is presented as if it proved product parity.
- Placeholder metadata is presented as real binding or writing-system parity.
- Snapshot tests update large JSON blobs without a small behavioral explanation.
- Cache invalidation tests depend on sleeps or filesystem timestamp luck.

## Handoff
State whether evidence is semantic, visual, accessibility/workflow, or performance parity, and identify remaining unproven axes. When a Path 3 bundle is used, name each artifact and which lane it proves.