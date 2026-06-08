---
name: fieldworks-semantic-render-parity
description: Use when capturing or reviewing FieldWorks semantic snapshots, render baselines, layout parity, failure artifacts, XML view definitions, or Avalonia presentation IR.
---

# FieldWorks Semantic Render Parity

## Snapshot Discipline
Semantic snapshots should preserve behaviorally meaningful identity and omit incidental layout noise.

## Include
- Stable node ID and source layout/part identity.
- Object/class binding, field/flid binding, editor kind, writing-system metadata, visibility, ghost state, expansion, focus order, localization key, and accessibility identity.
- Unsupported construct diagnostics with enough path context to fix the source layout.

## Exclude Or Normalize
- Pixel bounds, transient generated names, timestamps, machine paths, culture-dependent ordering, and realized-control counts unless the test explicitly owns them.

## Render Evidence
- Pixel/render tests need deterministic fixtures, clear thresholds, and failure artifacts that reviewers can inspect.
- A semantic snapshot is not a substitute for visual/render parity when typography, density, wrapping, or native rendering seams are under review.

## Review Red Flags
- Placeholder metadata is presented as real binding or writing-system parity.
- Snapshot tests update large JSON blobs without a small behavioral explanation.
- Cache invalidation tests depend on sleeps or filesystem timestamp luck.

## Handoff
State whether evidence is semantic, visual, accessibility, or performance parity, and identify remaining unproven axes.