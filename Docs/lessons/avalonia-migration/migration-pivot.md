# Avalonia migration scope and evidence pivot

Status: current principle; retired implementations
Sources: PR #964; commits `537577b39`, `6da2334cd`, and `bd7d3a5e5`;
retired `avalonia-migration-roadmap` and `lexical-edit-avalonia-migration`
change artifacts
Human review: PR #964

## Question tested

How can FieldWorks replace native Views and WinForms UI incrementally
without landing a broad, dormant, weakly evidenced alternate application?

## Observations

- Extracting DataTree internals did not produce a clean migration boundary.
  The useful boundary sits above DataTree in typed view definitions, projected
  detail values, explicit edit contexts, and host selection.
- A large derisk branch accumulated plans, infrastructure, product views,
  and dormant code faster than reviewers could establish which paths were
  real, reachable, and parity-complete.
- Call-site inspection disproved several summaries about whether views were
  wired. Names, comments, task checkboxes, and agent reports were not reliable
  substitutes for following the product route.
- Headless control tests proved useful mechanics but sometimes bypassed the
  real gesture, host, clerk, focus, or dialog route being claimed.
- Current base retained reusable infrastructure and a small set of canonical
  consumers, while deliberately removing unreachable or over-broad views.

## What failed or was retired

The broad migration roadmap, dormant browse implementation, many deferred
screens, and several implementation-specific plans were removed. Their checked
tasks and class layouts do not describe current product capability.

## Durable lessons

1. Characterize product behavior before choosing or extracting a seam.
2. Establish reachability from real call sites and fail closed when a route is
   unsupported.
3. Keep one reviewed consumer per reusable primitive; do not confuse a reusable
   control with permission to ship every screen that once used it.
4. Activation follows end-to-end parity evidence. Compiled or dormant code is
   not supported behavior.
5. Use semantic, workflow, accessibility, visual, lifecycle, and performance
   evidence in proportion to the claim; no single headless layer proves all of
   them.

## Evidence needed next time

- A current call-site and host-routing map.
- Characterization of legacy semantics and undo/refresh behavior.
- Real workflow tests that drive the production route.
- Desktop evidence for focus, UIA, dialogs, rendering, and other shell behavior
  that the headless backend cannot reproduce.
- A current-tree audit proving that proposed infrastructure is not duplicated.

## Decision boundary

These lessons constrain sequencing, evidence, reachability, and scope control.
A human still chooses the product outcome, feature boundary, UX, controls,
implementation architecture, and acceptable deferrals.

## Do not infer

- Do not restore a removed OpenSpec change or task list.
- Do not treat an old checked box as current parity evidence.
- Do not assume a historical type or assembly boundary remains canonical.
- Do not turn the retired phase stack into an autonomous migration roadmap.
- Do not infer that default-off routing makes dormant product code reviewable or
  ready to activate.
