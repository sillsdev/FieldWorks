## Context

Task 6.13 in `lexical-edit-avalonia-migration` is the unresolved gate between today's plain-text per-writing-system Avalonia rows and true text-editing parity. The broader migration already has the dependencies this work must reuse: fenced edit sessions, shared undo or refresh behavior, the `TsStringWrapper` clipboard contract, drag/drop payloads, per-writing-system keyboard activation, and the product host routing that keeps legacy UI mode available. What is still missing is the owned text-editor layer that can render and edit managed `ITsString` data with IME and bidirectional behavior, so `MultiStringSlice`, `StringSlice`, and `GhostStringSlice` stop being blockers in `native-views-audit.md`.

This change therefore sits at the boundary between LCModel text, Avalonia text primitives, and lexical-edit coexistence:

- Keep `ITsString` as the domain text model; do not invent a second product text representation.
- Keep the Avalonia path free of native Views or Graphite runtime dependencies.
- Reuse existing seams for edit sessions, clipboard, drag/drop, refresh, scheduler, and lifetime.
- Treat IME, bidi, and typing-latency evidence as first-class gates, not follow-up polish.

## Goals / Non-Goals

**Goals:**
- Define a FieldWorks-owned Avalonia text-editing foundation for managed `ITsString` data.
- Preserve supported run-level writing-system and style metadata across load, edit, save, clipboard, and drag/drop flows.
- Project default font, direction, culture, and keyboard behavior from the language project's writing-system settings while allowing supported run overrides from `ITsString`.
- Prove IME composition, RTL or mixed-direction caret movement, ghost-field realization, coexistence refresh, and shared undo behavior with executable evidence.
- Measure and record typing latency for the new editor at 100% and 150% DPI so downstream region manifests can gate parity on real numbers.

**Non-Goals:**
- Multi-paragraph `StText` editing, paragraph layout, or full document-editor replacement.
- General-purpose formatting commands such as a new bold or italic toolbar; this foundation preserves and round-trips supported run properties rather than inventing a new style-authoring surface.
- Graphite warning or fallback policy, which stays in `graphite-transition-support`.
- Shell-global command routing or focus policy outside the lexical-edit region host.
- Support for embedded-object or ORC editing beyond what current lexicon text fields demonstrably need.

## Decisions

### 1. Keep `ITsString` as the domain model and add a renderer-neutral run adapter

**Decision:** The editor foundation will read LCModel managed `ITsString` values directly and project them into a managed run model that is neutral to Avalonia rendering primitives. The write-back path will rebuild or update `ITsString` through managed LCModel APIs rather than via plain-text intermediates.

**Rationale:** The repo already treats `ITsString` as the product text contract, and the clipboard or drag/drop seam already standardizes on `TsStringWrapper` XML for cross-surface interchange. Reusing that model avoids a second serialization boundary and keeps coexistence with legacy Views editors honest.

**Alternatives considered:**
- Flatten to plain text and reapply metadata heuristically: rejected because it loses supported run boundaries and makes coexistence bugs hard to explain.
- Invent a new Avalonia-only text document model: rejected because it would fork the product text contract before the migration is complete.

### 2. Use a FieldWorks-owned Avalonia text control over Avalonia text primitives

**Decision:** The implementation will use a FieldWorks-owned editor control in `Src/Common/FwAvalonia/Region/` that composes Avalonia text input, layout, and selection primitives behind FieldWorks-specific behavior for writing systems, IME, and ghost realization. It will not depend on native Views controls, and it will not adopt a third-party rich-text editor as the primary architecture.

**Rationale:** The migration skills already treat owned controls as the long-term contract for dense FieldWorks editing behavior. This work needs deeper control of caret, selection, keyboard switching, and run fidelity than a stock `TextBox` alone exposes.

**Alternatives considered:**
- Keep extending the plain-text `FwMultiWsTextField`: rejected because the 6.13 gate is precisely the missing managed rich-text or run-aware editing layer.
- Use a third-party rich-text control: rejected because it adds licensing, maintenance, and product-contract risk before the core behavior is proven.

### 3. Reuse existing coexistence seams instead of adding new ones

**Decision:** Clipboard, drag/drop, edit-session, refresh, undo, scheduler, and lifetime behavior will reuse the seams already established by tasks 3.13, 3.14, 3.15, 6.8, and 6.10.

**Rationale:** The migration already paid the complexity cost to make cross-surface interchange, shared undo, and refresh propagation explicit. A new text foundation should consume those seams, not reopen them.

**Alternatives considered:**
- Add a separate rich-text clipboard or refresh path: rejected because it would create duplicate contracts and mask coexistence bugs.

### 4. Close the string-slice blockers first and defer `StText`

**Decision:** The first implementation scope closes the blockers for `MultiStringSlice`, `StringSlice`, and `GhostStringSlice`. Multi-paragraph `StText` remains a named follow-on because it brings paragraph layout and document-style editing concerns that would dominate this change.

**Rationale:** The single most common Lexical Edit interaction is multi-writing-system string editing, and ghost string realization is directly tied to that path. `StText` is important but materially broader than the 6.13 gate needs for the first reusable foundation.

**Alternatives considered:**
- Pull `StText` into the first pass: rejected because it couples paragraph editing to the core text foundation and delays closure on the dominant slice families.

### 5. Make Unicode cluster behavior, IME, and bidi explicit design targets

**Decision:** The editor will treat grapheme-cluster-safe insertion, deletion, and caret movement as part of the contract, and it will model IME composition as a distinct local editor state. Mixed-direction and RTL behavior will be validated against explicit fixtures rather than inferred from single-direction tests.

**Rationale:** Complex-script and bidi regressions usually appear at caret, deletion, and composition boundaries, not in simple rendering screenshots. If those states are not modeled directly, the migration will rediscover the same bugs later under larger surfaces.

**Alternatives considered:**
- Treat IME and bidi as later polish once plain input works: rejected because 6.13 is already the parity gate for those behaviors.

### 6. Separate automated, realized-window, and performance evidence lanes

**Decision:** Headless tests prove run fidelity, cluster behavior, and control-local state. Coexistence tests prove LCModel save, refresh, undo, clipboard, and drag/drop behavior. Realized-window or manual evidence proves RTL and complex-script editing on actual Windows input stacks. Typing-latency harnesses provide the performance lane.

**Rationale:** No single lane can prove everything the gate claims. The existing skills and parity docs already distinguish semantic, workflow/accessibility, and performance evidence; this change must do the same for text editing.

**Alternatives considered:**
- Use headless tests as proof of IME parity: rejected because the UIA/parity guidance explicitly warns against that shortcut.

## Risks / Trade-offs

- **[Risk] Avalonia text primitives may not expose enough caret or selection behavior on their own.** -> Keep a FieldWorks-owned selection model above the rendering layer and compare it against legacy fixtures before widening scope.
- **[Risk] IME behavior is environment-sensitive and hard to automate fully.** -> Require realized-window manual evidence with fixed fixtures and keep legacy UI mode as the rollback path.
- **[Risk] Shared code may drift across net48 hosts and newer test targets.** -> Keep the implementation in existing Common projects, validate through `./build.ps1` and `./test.ps1`, and avoid net8-only language or API assumptions.
- **[Risk] Typing latency could regress once cross-surface refresh and undo are wired in.** -> Measure both isolated editor latency and integrated commit latency, then commit thresholds to manifest docs before claiming parity.
- **[Risk] Deferring `StText` leaves some native text blockers open.** -> Record the deferred scope explicitly in the lexical-edit manifest and native audit rather than implying full text parity.

## Migration Plan

1. Land the OpenSpec split and update lexical-edit migration to treat the old 6.13 line item as a dependency pointer instead of the full plan.
2. Implement the managed run adapter, owned Avalonia control shell, and write-back path for single- and multi-writing-system string fields under the existing non-default Avalonia route.
3. Wire clipboard, drag/drop, ghost realization, shared undo, and coexistence refresh through the existing seams and add the corresponding headless and xWorks integration tests.
4. Add RTL and complex-script fixtures, using Khmer as the canonical complex-script lane, collect realized-window manual evidence, and record typing-latency budgets at 100% and 150% DPI.
5. Update lexical-edit manifests and blocker docs to record which native Views text blockers this change closes, while leaving `StText` and any unsupported object-content cases explicitly deferred.

**Rollback:** Legacy UI mode remains the shipping fallback throughout this change. No default-switch claim or shell-routing change should depend on partially complete text-foundation work.

## Open Questions

- How much existing run formatting must the first implementation preserve beyond the run properties already present in lexicon fields today?
- Do any migrated lexicon fields require embedded-object or ORC editing in the first parity wave, or can that remain explicitly deferred with `StText`?