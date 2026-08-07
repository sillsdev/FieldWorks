# Stage 2 — Coexistence shell spine & host contracts (Epic — **finished spec**)

> Status: **implementation-ready**. Grounded in `reviews/stage-02-coexistence-shell-spine.md`, the
> shipping host code, and `Directory.Packages.props` (Avalonia **11.3.17**, last netstandard2.0 line).

## Epic
- **Summary:** Generalize the proven lexical-edit host bridge + surface switch into an app-wide coexistence
  spine (host, surface registry + census, command bridge, ownership ports, theming) so any surface can run
  Avalonia-in-WinForms behind the flag.
- **Type:** Epic · **Labels:** `track-foundation`, `lead-senior` · **Size:** M
- **Description:** Five of six deliverables are **generalizations of existing code**, not net-new: the host
  (`LexicalEditHostControl` + `WinFormsAvaloniaControlHost`, in production via `RecordEditView`), the
  four-state `HostUiBehavior`, the `UIMode` flag, and `IXCoreCommandBridge` (in `Seams/ISeams.cs`) all
  exist. The genuinely new design surface is the **window/dialog ownership ports** pulled forward from
  `fieldworks-avalonia-shell-migration`.
- **Acceptance criteria:** any Avalonia view is hostable in the WinForms shell behind the flag; the surface
  registry resolves every host to Supported/ExplicitLegacyFallback/Blocked with **no silent Avalonia**;
  ownership ports are the single source of truth shared with Stage 11; theming/host code is Av12-delta-localized.
- **Dependencies:** consumes nothing hard; **gates Stage 5 (dialogs) and Stage 11 (shell)**. The riskiest
  dependency (net48 + Avalonia 11 host) is **already retired** — it ships.

## Sub-epics / stories

### 2.1 Generalize the region host  · Story · S
- Extract a reusable region host from `LexicalEditHostControl` (largely extract-interface + parameterize
  `ShowRegion`). **Acceptance:** a second (toy) surface hosts through the same control with no LexicalEdit coupling.

### 2.2 App-wide surface registry + living surface census  · Story · M  *(owns the Stage 1/2 double-booking)*
- Generalize `LexicalEditSurfaceSelectionService`/`LexicalEditSurfaceResolver` into an app-wide registry;
  **fix the null/unregistered-tool path to default to legacy/blocked, never silent Avalonia.**
- Produce a **living surface-census artifact** (the asset Stage 8's straggler-sweep presumes) enumerating
  every WinForms surface + its migration state.
- **Acceptance:** registry + census are the single inventory the program tracks burn-down against.

### 2.3 Shell-scope command/state bridge  · Story · S
- Promote `IXCoreCommandBridge`/`IRecordNavigationContext` to shell scope (interfaces exist; this is wiring).
- **Acceptance:** a shell-scope command routes through the bridge in a headless test.

### 2.4 Window/dialog ownership ports (single source of truth)  · Story · M  *(genuinely new)*
- Name and extract the ports: app-lifetime, main-window, active-window registry, dialog owner, dispatcher,
  shutdown, modal state — reusing `IUiScheduler`/`IRegionLifetime`, not redefining. Declare them shared
  with Stage 11. Add a **host-contract test suite** capturing the existing focus/keyboard/undo mitigations
  *before* any refactor.
- **Acceptance:** Stage 11 consumes these verbatim; contract tests lock the coexistence behavior.

### 2.5 Modernized-Fluent ControlTheme baseline + theming pipeline  · Story · M
- Stand up the modernized Fluent theme (per decision §11.4 — upgrade the look, keep density). Add an
  **Av12-readiness gate** (theming APIs change 11→12) to keep Stage 12 small.
- **Acceptance:** owned controls render under the baseline theme at 100%/150% DPI within density budget.

### 2.6 Dual-run CI matrix  · Story · S
- Make "dual-run" a **CI build matrix** that builds/tests both surfaces, not just the `UIMode` runtime preference.

## Notes / open questions
- Port definitions must not diverge from `fieldworks-avalonia-shell-migration` design §2/§7 — make Stage 2
  the source of truth and cross-link.
- All Stage-2 code is written **Av12-delta-localized** (confine 11-only APIs to named seams).
