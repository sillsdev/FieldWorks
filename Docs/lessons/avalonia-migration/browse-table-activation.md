# Browse-table implementation and activation

Status: current warning; implementation and activation path rejected
Sources: retired PR #967 and commit `3fe5c0401`; removal commit `bd7d3a5e5`;
PR #964 current base
Human review: PR #964

## Question tested

Could a compiled but dormant Avalonia browse implementation be activated by a
small resolver change after landing broad table, clerk, bulk-edit, and product
wiring in the base?

## Observations

- The historical activation changed eight tool routes and resolver expectations
  despite touching only two files.
- Its premise depended on a large dormant implementation already being present.
  Current base deliberately removed that implementation and its product wiring.
- Browse behavior spans virtualization, selection identity, clerk sorting and
  filtering, configuration persistence, editing, bulk operations, rapid data
  entry, export, accessibility, and lifecycle. These are not activation details.
- Stable selection is domain identity across sort, filter, and realization—not
  a row index or realized-control reference.
- Real clerk behavior and headless workflow tests can establish much of the
  managed contract, but focus, UIA, rendering, and shell behavior still require
  appropriate desktop evidence.

## What failed or was retired

The dormant browse view, host, renderer, row/column adapters, bulk-edit bar,
configuration and dialog code, clerk adapters, product wiring, and their
tests were removed. The two-file activation commit no longer has a view to
activate and directly conflicts with the accepted current-base decision that a
future browse table return as a reachable whole.

## Durable lessons

1. Count affected product routes, not changed files, when assessing activation
   risk.
2. Treat selection identity, bounded realization, edit sessions, clerk state,
   accessibility, and lifecycle as first-class table contracts.
3. Build and prove a reachable vertical slice before adding richer table
   capabilities; activation is the final step.
4. Use real clerk matchers, sorting, field identities, writing systems, and
   external chooser behavior for product claims.
5. Dormant compiled code is neither supported behavior nor a safe substitute
   for an independently reviewable vertical slice.

## Evidence needed next time

- A human-approved first tool route and initial read-only capability boundary.
- Real-clerk data with stable HVO selection through sort, filter, refresh, and
  virtualization.
- Bounded realization and de-realized UIA/selection behavior.
- Incremental evidence for editing, sort/filter, column configuration, bulk
  operations, RDE, copy/paste, context commands, and CSV as each is proposed.
- Undo, cancellation, refresh settlement, handler teardown, keyboard, UIA,
  density, localization, and representative-project evidence before activation.

## Decision boundary

This record constrains scope measurement, identity, virtualization, clerk
integration, evidence, and activation order. A human decides whether to reverse
the removal decision, which tool and capabilities form the first slice, and the
current architecture.

## Do not infer

- Do not cherry-pick or rebase the two-file activation commit.
- Do not assume the removed browse implementation still exists in the base.
- Do not make browse depend on rule or interlinear work because of the old
  stack.
- Do not restore all eight tool routes together.
- Do not infer that an old test inventory authorizes the same design or scope.
