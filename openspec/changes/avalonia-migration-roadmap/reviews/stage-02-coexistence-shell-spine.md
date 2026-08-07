# Stage 2 Review — Coexistence shell spine & host contracts

Reviewer: senior migration review (Claude, Opus 4.8). Date: 2026-06-15.
Scope reviewed: `complete-migration-program.md` §4/§6 Stage 2 + §11 decisions;
architecture-patterns §3/§7; seam-catalog; parity-evidence; the as-built host/seam
code in `Src/Common/FwAvalonia/` and `Src/xWorks/RecordEditView.cs`; the
`fieldworks-avalonia-shell-migration` change.

---

## 1. Scope assessment

Stage 2 bundles six deliverables (generalized host, generalized surface registry,
`IXCoreCommandBridge`, feature-flag/dual-run build, ControlTheme baseline, pulled-forward
shell contract layer). This is **right-sized as one epic but internally heterogeneous**:
five of the six are pure generalizations of code that already exists for Lexical Edit
(`LexicalEditHostControl`, `LexicalEditSurfaceSelectionService`, `IXCoreCommandBridge`
stub in `Seams/ISeams.cs:72`, the `UIMode` flag), while the sixth — pulling forward the
shell contract layer (window/dialog ownership ports) — is genuinely new design surface
imported from `fieldworks-avalonia-shell-migration`.

Findings:

- **The host and surface-switch generalization is mostly a rename + parameterize, not
  net-new work.** `LexicalEditHostControl` is already a thin `WinFormsAvaloniaControlHost`
  wrapper (companion strip, focus memento, keyboard-interop overrides) that is
  region-agnostic except for the `LexicalEditRegionModel`/`LexicalEditRegionView` types in
  its `ShowRegion` signature. Generalizing it means extracting an `IRegionView`/`IRegionModel`
  abstraction and making `ShowRegion` generic — low risk. Scope is correct; effort is
  smaller than its sibling stages.
- **`HostUiBehavior` is already the four-state enum** (`LegacyActive / SupportedAvalonia /
  ExplicitLegacyFallback / Blocked`) the master plan describes — Stage 2's job is to move
  the *tool list* (`LexicalEditSurfaceResolver.SupportedAvaloniaToolNames`, currently a
  hard-coded `{"lexiconEdit","lexiconEditPopup"}` array) into an app-wide registry keyed by
  area/tool id, not to invent the model. Correctly scoped.
- **The ControlTheme baseline is under-specified for the new charter.** §11 decision 4
  ("upgrade the look; modernized Fluent, not legacy mimicry") changed the contract since
  `architecture-patterns.md §12` was written (which still says "measured against legacy
  WinForms baselines" for *density*). Stage 2's exit gate says "theming + AutomationId
  conventions locked." That lock must explicitly reconcile: visual lane = intentional
  restyle allowed; density + semantic + workflow lanes = parity-enforced. This is a real
  decision item, not a copy-forward, and the stage text should call it out.
- **`IXCoreCommandBridge` scope is correctly bounded.** The seam already exists as an
  interface; seam-catalog §1 says "shell-scope wiring happens in the shell phase, not per
  region." Stage 2 should stand up the *bridge implementation over the existing Mediator/
  PropertyTable* for region-local + the minimal shell-scope commands the host needs, and
  explicitly **defer** full shell command routing to Stage 11. The plan says this; keep it.
- **One scope gap:** the master plan lists "global feature-flag plumbing (default WinForms)
  and the dual-run build" but the repo already ships the flag (`UIMode` property,
  `LexicalEditSurfaceResolver`) and a single-CLR in-process dual surface in `RecordEditView`.
  What's *missing* and should be named explicitly in Stage 2 is a **build-level dual-run
  switch** (a way to produce/CI-test both the all-WinForms and flag-on configurations) — the
  current setup is a runtime preference, not a build configuration. Clarify which is meant.

## 2. Feasibility (repo-grounded)

**Feasible, and the riskiest dependency is already retired.** The central feasibility
question — "does the host bridge run on net48?" — is answered yes by the shipping build:
`Src/Common/FwAvalonia/FwAvalonia.csproj:19` targets `net48`; `Directory.Packages.props:185-213`
pins **Avalonia 11.3.17** with the explicit comment that 11.3.x "is the last line that still
ships netstandard2.0 assemblies and therefore loads on net48" and that "Avalonia 12.x dropped"
that support. `Avalonia.Win32.Interoperability 11.3.17` (the `WinFormsAvaloniaControlHost`
carrier) is referenced and in use. So Stage 2 inherits a *proven* net48 + Avalonia-11 host;
it does not have to establish it. This is the single most important de-risking fact and it is
already true.

Confirmed working in-repo today:
- In-process host: `LexicalEditHostControl` embedded in `RecordEditView` via
  `m_lexicalEditSurfaceFactory.Create(LexicalEditSurface.Avalonia)` (RecordEditView.cs:599).
- Single Avalonia init gate + finalizer-safe sync context
  (`LexicalEditHostControl.EnsureAvaloniaInitialized`, `FinalizerSafeSynchronizationContext`).
- Active-host contract enforcement (`Seams/ActiveHostContract.cs`) + audit tests
  (`EngineIsolationAuditTests.cs`, `SurfaceAndHostContractTests.cs`,
  `RecordEditViewActiveHostContractTests.cs`).
- Editing-aware refresh coordination across the boundary
  (`AvaloniaRegionRefreshController`, RecordEditView.cs:611-637) — including the documented
  cross-window undo re-entrancy hazard (`LockRecursionException`) and its deactivate-settle
  mitigation. This is exactly the class of coexistence bug Stage 2 must generalize carefully.

## 3. Best practices (incremental host / strangler spine)

1. **Generalize by extracting an interface seam, not by widening the concrete host.** Keep
   `LexicalEditHostControl`'s mitigations (the `IsInputKey`/`ProcessCmdKey`/`PreviewKeyDown`
   directional-key bypass, the per-host remembered splitter width, the focus memento) as the
   *reference behavior* a generic `RegionHostControl<TModel,TView>` or an `IRegionView`-based
   host preserves. Do not lose these by accident in the rename — they encode hard-won
   coexistence fixes.
2. **Keep WinForms owning all modality through Stage 11** (architecture-patterns §7,
   dialog-ownership.md). Web research confirms the underlying constraint:
   `Application.Current.ApplicationLifetime` is null when Avalonia runs hosted in WinForms, so
   Avalonia modal windows are unsupported on this path; the supported pattern is a WinForms
   `Form` owning embedded Avalonia content. Stage 2's pulled-forward dialog-ownership ports
   must encode "modal = WinForms-owned" as a contract, not aspiration.
3. **Airspace:** because the WinForms-hosted Avalonia control is always topmost, no WinForms
   control or popup can paint over the Avalonia surface. Use Avalonia *flyouts inside* the
   surface (already the rule — `RegionMenuFlyout`), never free popup windows. The generic host
   must expose the flyout path, not a window path.
4. **Tab/focus does not cross the boundary** (Avalonia issue #12025). The host owns focus
   internally; there is no WinForms↔Avalonia Tab order. Keep this as a host-contract test.
5. **Surface registry must default to WinForms and fail safe.** `LexicalEditSurfaceResolver`
   already returns `WinForms` for unknown tools and treats null tool as "supported" only for
   resolution convenience — the app-wide registry should make *unregistered = ExplicitLegacy
   or Blocked*, never silent Avalonia. No silent fallback (architecture-patterns §3).
6. **AutomationId day-one** (master-plan principle 8): the generic host must require an
   AutomationId-deriving convention (from StableId) on every hosted view, enforced at the host
   seam so Stage 5+ juniors cannot skip it.
7. **One global undo stack only.** The cross-window re-entrancy mitigation already in
   RecordEditView is the canonical pattern; the generalized host must carry the undo-guard /
   deactivate-settle hooks, not leave them per-surface.

## 4. Interactions & dependencies

- **Gates Stage 5 (dialogs) and Stage 11 (shell).** Stage 5 needs the generalized host +
  dialog-ownership contract before juniors can place Avalonia dialog *content* inside
  WinForms-owned modal forms. Stage 11 consumes the same window/dialog ownership ports and
  promotes `IXCoreCommandBridge` from region-local to shell-global. **Stage 2 must ship the
  *contract* shapes (ports/interfaces) that Stage 11 will implement** — and explicitly resist
  implementing the shell. The plan already says "pull forward the contract layer ... without
  replacing the shell yet" — good; the review's recommendation is to *name the exact ports*
  (lifetime, main-window, active-window registry, dialog owner, dispatcher, shutdown, modal
  state — per shell-migration design §2) so Stage 2 and Stage 11 share one definition.
- **Relationship to `fieldworks-avalonia-shell-migration`:** that change's design §7 says the
  shell phase *consumes* the lexical-edit seams rather than redefining them, and §2/§3 list the
  ports. Stage 2 is the right place to *extract* those ports while WinForms stays default.
  Recommend Stage 2 own the port extraction and shell-migration design §2 (decision: "neutral
  ports first") be cross-linked as the source of truth; avoid two divergent port definitions.
- **`IXCoreCommandBridge` ↔ `IRecordNavigationContext`:** both bridge to Mediator/
  PropertyTable (ISeams.cs). The nav-context seam already exists and is "coexistence
  infrastructure, not throwaway." Stage 2's command bridge should reuse the same
  PropertyTable-access discipline (never reach into PropertyTable directly from a region —
  seam-catalog §1) and share the bus access with nav-context, not open a second path.
- **Ordering vs Stage 12 (runtime jump):** the **one-CLR constraint is the governing fact**:
  during coexistence the whole process is one CLR on Avalonia 11.x / net48 (master plan §5).
  Stage 2 must therefore write all new host/bridge/theme code **Avalonia-12-ready** (avoid APIs
  removed in 12) but ship on 11.3.17. The ControlTheme pipeline especially is at risk: Fluent
  theming and `ControlTheme`/resource APIs changed between Avalonia 11 and 12 — Stage 2 should
  avoid 11-only theming idioms that Stage 12 must rip out. Add an explicit "Av12-ready" check
  to the theming task.

## 5. Recommended plan changes

1. **Split the Stage 2 epic into two work-streams in the issue breakdown:** (a) *generalization
   of existing assets* (host, surface registry, command-bridge impl, flag/build) — low risk,
   can start immediately; (b) *new contract design* (window/dialog ownership ports + ControlTheme
   baseline decision) — higher design content, needs the shell-migration design as input.
2. **Name the exact ports Stage 2 extracts** (lifetime, main-window, active-window registry,
   dialog owner, UI dispatcher, shutdown, modal state) and declare them the shared source of
   truth for Stage 11. Reuse `IUiScheduler`/`IRegionLifetime` from `ISeams.cs` rather than
   adding new dispatcher/lifetime abstractions (seam-catalog §2; shell-design §3/§7).
3. **Make the ControlTheme baseline an explicit decision item**, reconciling §11 decision 4
   (restyle allowed) with architecture-patterns §12 (density parity enforced): visual lane =
   restyle OK; density/semantic/workflow/perf lanes = parity. Update architecture-patterns §12
   wording in the same PR so it stops saying "mimic legacy."
4. **Disambiguate "dual-run build"**: state whether Stage 2 delivers a build-time configuration
   matrix (CI builds/tests both all-WinForms and flag-on) in addition to the existing runtime
   `UIMode` preference. Recommend yes — CI must exercise both surfaces.
5. **Add a host-contract test suite as a Stage 2 exit artifact**: directional-key bypass,
   no cross-boundary Tab, modal-is-WinForms-owned, flyout-not-window, focus save/restore,
   AutomationId-required-on-hosted-view, single-undo-stack guard. Promote the existing
   `LexicalEditHostControlTests` patterns into the generic suite.
6. **Add an explicit "Avalonia-12-readiness" gate to the theming task** (no Av11-only theming
   APIs), to keep Stage 12's bump small.
7. **Update `LexicalEditSurfaceResolver.SupportsAvaloniaForTool` null-handling** when
   generalizing: a null/unknown tool currently returns "supported" (resolver.cs:72) — the
   app-wide registry should treat unregistered tools as explicit legacy/blocked, not supported.

## 6. Open questions / risks

- **OQ-1 (port ownership):** Do the window/dialog ownership ports live in `Src/Common/FwAvalonia/Seams/`
  (with the other seams) or in a new shell-contracts assembly? Recommend `Seams/` to keep one
  seam home until Stage 11 needs more.
- **OQ-2 (ControlTheme scope):** Is the modernized-Fluent baseline a full app ControlTheme or
  per-control themes layered on `FluentTheme` (currently `FwAvaloniaApp.Initialize` just adds
  `new FluentTheme()`)? Density tokens (`FwAvaloniaDensity.cs`) must compose with it.
- **OQ-3 (build matrix):** Does CI have headroom to build/test both surface configurations, or
  is flag-on the only CI lane with all-WinForms covered by the legacy suite?
- **Risk — theming churn at Stage 12 (Med/Med):** mitigated by the Av12-ready gate (rec. 6).
- **Risk — generalization drops a coexistence fix (Med/High):** the focus/keyboard/undo
  mitigations in `LexicalEditHostControl`/`RecordEditView` are subtle; mitigated by the
  host-contract test suite (rec. 5) capturing them *before* the refactor.
- **Risk — port definitions diverge from shell-migration (Med/Med):** mitigated by declaring
  Stage 2 the single source (rec. 2) and cross-linking the shell-migration design.
- **Risk — surface registry silent-fallback regression (Low/High):** mitigated by rec. 7 +
  existing `ActiveHostContract` audit tests.

## 7. Confidence

**High** on feasibility and the net48/Avalonia-11 foundation (directly verified in the
shipping csproj and packages.props, plus a working in-process host in RecordEditView).
**High** on the coexistence-risk catalog (airspace/focus/modality/threading), corroborated by
Avalonia upstream issues and already mitigated in-repo. **Medium** on the ControlTheme-baseline
and dual-run-build scope, which are under-specified in the master plan and are the two items
most likely to expand. The recommended split + explicit decision items convert those from
hidden scope into named, gateable work.

---

### Sources (external)
- Avalonia WinForms migration guide: https://docs.avaloniaui.net/docs/migration/winforms/
- `WinFormsAvaloniaControlHost` API: https://docs.avaloniaui.net/api/avalonia/win32/interoperability/winformsavaloniacontrolhost
- Modal dialog from host (ApplicationLifetime null): https://github.com/AvaloniaUI/Avalonia/discussions/15977
- Tab/focus across interop boundary: https://github.com/AvaloniaUI/Avalonia/issues/12025
- Hosting Avalonia in WinForms: https://github.com/AvaloniaUI/Avalonia/issues/11454
