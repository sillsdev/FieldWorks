# Stage 13 Review — Final cutover, native decommission & cross-platform enablement

> Reviewer pass over Stage 13 of `complete-migration-program.md` (§4 row 13, §6 Track IV).
> Grounded in the as-built repo (`Src/views/`, `Src/Common/ViewsInterfaces/`, `Src/CacheLight/`,
> `Src/Common/SimpleRootSite|RootSite`, `Src/Common/Controls/{DetailControls,XMLViews}`,
> `Src/Common/FwAvalonia/`, `FLExInstaller/`) and the `retire-linux-era-view-shims` change.
> Status: planning review only; no code/behavior change proposed here.

## Scope assessment

Stage 13 bundles **five** distinct, high-blast-radius workstreams under one "senior" epic:
(1) flip the global default to Avalonia; (2) delete the WinForms surface layer (shell, dialogs,
DataTree/Slice, SimpleRootSite/RootSite, XMLViews, interop spine); (3) decommission native C++
UI/render (`Src/views/`, `ManagedVwDrawRootBuffered`) and the `IVwRootBox`/`IVwGraphics`/`IVwEnv`
COM surface; (4) cross-platform Linux/macOS build+headless+smoke; (5) final accessibility/
localization/performance gates. **This is too much for one stage and should split.** The five
have different reversibility, different skill profiles, and different failure signatures:

- **Cutover (flip + staged rollback)** is a *runtime config* change — reversible in minutes, no
  code deleted. This is the highest-value, lowest-cost, most-reversible action.
- **Decommission (delete WinForms + native UI)** is *destructive and irreversible-by-design*.
  Deleting code while a freshly-flipped default is still proving itself in the field removes the
  rollback path Stage 13 nominally relies on. Cutover and deletion **must not** be the same step,
  let alone the same epic.
- **Cross-platform enablement** is *additive net-new validation* (CI lanes, OS smoke, packaging
  for two new OSes) that depends on the deletion being *done* but shares no code with it.

**Recommended split into three sequenced epics:**
- **13a — Cutover & bake:** flip default to Avalonia behind the existing `UIMode` setting via a
  staged rollout; WinForms code stays in the tree as the live rollback for a defined bake period.
- **13b — Decommission:** delete WinForms/native UI only *after* 13a's bake gate is green; this is
  the irreversible step and deserves its own epic with its own ordered deletion plan.
- **13c — Cross-platform & final gates:** Linux/macOS build/headless/smoke + accessibility/
  localization/performance, gated on 13b (the managed surface is the only surface left to validate).

Keeping these in one row understates the program's tail risk and hides the fact that the most
dangerous action (deletion) is gated on the success of a different action (the flip) that needs
field bake time, not a green CI run, to be trusted.

## Feasibility (repo-grounded)

The deletion target is **mostly** as the plan describes, but the plan's interface-deletion claim is
**too broad and would break non-UI code as written**. Specifics:

**Safe to delete (pure UI/render), confirmed:**
- `Src/views/` native engine — 44 C++ files (`VwRootBox.cpp/h`, `VwSelection`, `VwTextBoxes`,
  `VwEnv`, `VwLayoutStream`, `VwNotifier`, etc.). It is the **last** native component built
  (`Build/mkall.targets`: `DebugProcs → GenericLib → FwKernel → Views`); nothing non-UI `#include`s
  its headers. It only *consumes* Kernel/Generic, never provides to them. Deletable.
- `Src/ManagedVwDrawRootBuffered/` — referenced **only** by
  `Src/Common/SimpleRootSite/SimpleRootSite.csproj`. Deletable with SimpleRootSite.
- `Src/Common/Controls/DetailControls/` (DataTree/Slice) and `Src/Common/Controls/XMLViews/` — pure
  UI; no domain/LCModel code imports them. Deletable once their UI consumers are gone.
- `Src/Common/SimpleRootSite/` and `Src/Common/RootSite/` — these are the WinForms **hosts of** the
  native engine (`SimpleRootSite` implements `IVwRootSite`, depends on `ViewsInterfaces` +
  `ManagedVwDrawRootBuffered`). Deletable, but **18+ projects still reference them** (xWorks,
  LexEdDll, ITextDll, Discourse, MorphologyEditorDll, FdoUi, Framework, FieldWorks, Widgets…), so
  every Interlinear/Discourse/parser surface must be migrated first (Stages 7/9). This is the long
  pole that gates 13b, not a Stage-13-internal task.

**LOAD-BEARING — the plan's "retire the IVwRootBox/IVwGraphics/IVwEnv COM surface" must be scoped to
the *rendering* interfaces only, NOT the whole `ViewsInterfaces` assembly:**
- `Src/Common/ViewsInterfaces/Views.cs` defines both the UI rendering interfaces (`IVwRootBox`
  ~line 6627, `IVwEnv` ~line 10200, `IVwGraphics`) **and** `IVwCacheDa` (~line 4122), which is a
  **data-access** cache contract, not a render contract.
- `IVwCacheDa` is implemented by `Src/CacheLight/RealDataCache.cs` and referenced by **45+
  projects**. `ISilDataAccess` (the broader data-access interface) is defined in
  `Src/Kernel/FwKernel.idh` (~line 475) — i.e., in Kernel, which Stage 13 keeps.
- **Therefore:** deleting `ViewsInterfaces` wholesale, or "the IVwEnv/IVwRootBox COM surface" without
  surgically separating it from `IVwCacheDa`/data-access, breaks the data cache and ~45 projects.
  The render-interface decommission requires **splitting `ViewsInterfaces`** into a deletable
  render-interface set and a surviving data-access interface set (or relocating `IVwCacheDa` into a
  Kernel-adjacent assembly) before the render half can be removed. The plan does not name this; it
  is real, non-trivial senior work and a hard prerequisite for the COM-surface deletion claim.
- `ITsString`/`TsString` (used in 4000+ places) live in `Src/Kernel/TextServ.idh` — **not** Views —
  so the "keep Kernel/Generic" line correctly preserves them. Good.

**Cutover mechanics already exist and are sound:**
- The flip is a one-property change: `LexicalEditSurfaceResolver` reads `UIMode`
  (`Src/Common/FwUtils/Properties/Settings.Designer.cs` defaults `[DefaultSettingValue("Legacy")]`),
  with `LegacyUIMode="Legacy"`/`NewUIMode="New"`. Flipping the default to `New` *is* the cutover.
  Today only `lexiconEdit`/`lexiconEditPopup` pass `SupportsAvaloniaForTool`; the flip is only safe
  once every tool returns Supported — i.e., the gate "all manifests pass" must be **mechanically
  enforced**, not asserted.
- The active-host contract in `Src/xWorks/RecordEditView.cs` (only one surface instantiated/driven)
  and `EngineIsolationAuditTests.cs` are the right safety rails — see Best practices for extending
  them to a whole-codebase gate.

**Cross-platform:** genuinely blocked until Stage 12 (net48 is Windows-only). The managed-only bet
(no native Views on the Avalonia path) is what makes it *possible* at all. But `Src/views/` and the
WinForms hosts are Windows-only C++/WinForms; any residual reference from a surviving surface will
fail the Linux/macOS build, so 13c is only feasible *after* 13b is genuinely complete.

**Installer:** `FLExInstaller/wix6/` is WiX6/Windows. Gecko is not named literally in the `.wxi`/
`.wxs` files (it is harvested from the build output dir, so removal is an upstream
output/dependency change plus a harvest-exclude, not a manifest edit). Cross-platform packaging
(Linux `.deb`/`.rpm`, macOS bundle) is **net-new** and unscoped — the WiX installer does not port.
This alone argues 13c is its own epic.

## Best practices

- **Separate "flip" from "delete."** Strangler-fig cutover = flip the toggle, *bake*, then remove the
  strangled code in a later change. Deleting in the same step destroys the rollback path. (Fowler
  Strangler Fig; the program's own §2 principle 1.)
- **Staged rollout, not big-bang flip.** Use the `UIMode` setting for a ringed rollout
  (opt-in → default-on-with-easy-revert → remove legacy). Per-tool granularity already exists
  (`SupportsAvaloniaForTool`); flip tool-by-tool, not all-at-once.
- **Mechanical "all manifests pass" gate.** Make the flip blocked by a test that asserts every
  registered tool/surface returns `Supported` and has a green region manifest — never a human
  checklist. Hallucinated-parity is the program's top AI risk (§9).
- **Promote `EngineIsolationAuditTests` from one assembly to a codebase-wide gate at cutover.**
  Today it scans only `FwAvalonia` (via `typeof(LexicalEditRegionView).Assembly` references + a
  regex source scan of `Src/Common/FwAvalonia`). At cutover the audit's value is proving **no
  surviving production assembly** references `SimpleRootSite`/`RootSite`/`XMLViews`/`DetailControls`/
  Views render symbols — extend it to scan all shipping assemblies before 13b deletes anything.
- **Delete in dependency order, leaf-first.** Repo-grounded order:
  consumers' WinForms surfaces → `DetailControls`/`XMLViews` → `RootSite` → `SimpleRootSite` →
  `ManagedVwDrawRootBuffered` → split `ViewsInterfaces` (relocate `IVwCacheDa`) → native `Src/views/`
  → render COM interfaces. Each deletion behind its own build-green commit for bisectable rollback.
- **Rollback window with data on it.** Define the bake duration and the metric that closes it
  (crash-free sessions, parity-incident count) before 13b is allowed to start.

## Interactions & dependencies

- **Depends on ALL prior stages** — correctly the terminal stage. Specifically it cannot start the
  deletion until Stages 7 (Interlinear/Discourse) and 9 (document engine) have removed the last
  `SimpleRootSite`/`IVwRootBox` consumers; the 18+ project references found are the concrete
  gate. If any Track-II/III surface slips, 13b stalls — Stage 13 inherits the entire program's tail.
- **Stage 12 is a hard prerequisite for 13c** (net48 → .NET 10; net48 is Windows-only). The plan's
  ordering (shell → runtime → cutover) is right for this reason. Confirmed.
- **`retire-linux-era-view-shims` is a *narrow prerequisite*, not part of Stage 13.** It removes the
  Linux/Mono-era managed shims (`ViewInputManager`, `ManagedVwWindow`) while **explicitly preserving**
  native `VwTextStore`, `IViewInputMgr`, and `ManagedVwDrawRootBuffered` (its non-goals). It is a
  Views-IDL/interop cleanup that should land *before* the Stage-13 native decommission so the
  `Src/views/` deletion starts from an already-de-shimmed Views surface. Note the irony: that change
  preserves `ManagedVwDrawRootBuffered`, which Stage 13 then deletes — fine, because the consumer
  (`SimpleRootSite`) is gone by then, but the sequencing should be stated.
- **Decision interactions:** Graphite full removal is Stage 10 (not here) — Stage 13 should *verify*
  it is gone, not redo it. The MVVM-dialog decision (Decision 3) means "delete WinForms-only dialogs"
  is gated on Stage 5 having replaced them with MVVM content, not on the region pattern.

## Recommended plan changes (concrete)

1. **Split Stage 13 into 13a (cutover & bake), 13b (decommission), 13c (cross-platform + final
   gates)** with 13b gated on a *field bake* metric from 13a and 13c gated on 13b complete.
2. **Rewrite the COM-surface line** from "retire the `IVwRootBox`/`IVwGraphics`/`IVwEnv` COM surface"
   to "**split `ViewsInterfaces`**: relocate/keep the data-access contracts (`IVwCacheDa`, and any
   non-render interface `CacheLight`/the 45+ consumers use) behind a service seam; delete only the
   render interfaces." Name this as a prerequisite sub-task with the `CacheLight` dependency cited.
3. **Add an explicit, dependency-ordered deletion runbook** (leaf-first order in Best practices) and
   require each deletion to be an independently-green, bisectable commit.
4. **Make "all manifests pass" a mechanical gate** (test asserting every tool returns `Supported` +
   green manifest) that blocks the flip; flip per-tool via `SupportsAvaloniaForTool`, not globally.
5. **Promote `EngineIsolationAuditTests` to a whole-shipping-assembly audit** as the entry gate to
   13b; deletion may not begin until no surviving assembly references the legacy UI/render symbols.
6. **Scope cross-platform packaging explicitly** in 13c: the WiX6 installer is Windows-only and does
   not port; Linux/macOS packaging is net-new work, not a tweak to `FLExInstaller/`.
7. **State the `retire-linux-era-view-shims` ordering** (lands before the `Src/views/` decommission).

## De-risking cross-platform within the deferral decision

The decision to hold all cross-platform OS validation to Stage 13 is *made*; the research warning
(regressions surface late) is real, so de-risk **within** the constraint rather than relitigating it:

- **Run the headless lane on Linux CI continuously from Stage 1** (Avalonia.Headless is
  OS-portable; the program's §9 risk row already commits to this). This catches *logic/binding/
  layout* regressions OS-early even though OS *smoke/UIA* is deferred — it is the single biggest
  cheap de-risk and costs nothing extra given headless tests already exist.
- **Lint for Windows-only APIs as code is written** (path separators, registry, P/Invoke, `\`),
  so the surviving managed code is cross-platform-clean *before* the OS build is ever attempted.
- **Stand up the Linux/macOS *build* (compile-only, no smoke) one stage early (end of 12)** so
  build-break surprises (project SDK, runtime IDs, native-dep resolution) are found before they pile
  onto the terminal stage. This honors "no *validation* cost earlier" while de-risking the build.
- **Budget an explicit integration-and-verification sub-phase in 13c** (§2 principle 11): clean
  cross-platform builds ≠ working software; reserve time for post-green OS smoke debugging.

## Open questions / risks

- **`ViewsInterfaces` split feasibility:** can `IVwCacheDa` be cleanly separated from the render
  interfaces, or do render interfaces reference data-access types (coupling the split)? This decides
  whether the COM-surface deletion is a clean cut or a refactor. Needs a focused audit before 13b.
- **Bake metric undefined:** what closes the 13a rollback window — duration, crash-free rate,
  parity-incident count? Without it, 13b's "safe to delete" trigger is subjective.
- **Tail-risk concentration:** Stage 13 inherits slippage from all of 5–12; if any surface still
  uses `SimpleRootSite`, the whole deletion stalls. The 18+ project reference list is the live
  burn-down to track program-wide, not at Stage 13.
- **Cross-platform packaging is unscoped** (no Linux/macOS installer exists); risk of discovering
  this is a multi-week effort at the very end.
- **HarfBuzz/Graphite coverage** must already be proven (Stages 9/10); if not, the cross-platform
  text-rendering smoke fails late with no Graphite fallback (it was removed). Verify, don't assume.
- **Big-flip blast radius:** even with per-tool granularity, the *default* flip touches every user;
  the ringed rollout and instant `UIMode` revert are the mitigations and must be real, not nominal.

## Confidence

**High** on the feasibility findings — the deletable-vs-load-bearing split is grounded in inspected
paths (`Src/views` build order; `IVwCacheDa` in `ViewsInterfaces` consumed by `CacheLight` + 45
projects; `ITsString` in Kernel; the 18+ `SimpleRootSite` consumers; the `UIMode` flip mechanism;
the audit's single-assembly scope). **High** on the split recommendation — the five workstreams
differ in reversibility and the irreversible one (deletion) is gated on the field success of a
reversible one (the flip), which is the textbook reason to separate them. **Medium** on the exact
`ViewsInterfaces` split mechanics — whether `IVwCacheDa` separates cleanly from the render interfaces
needs the focused audit named in Open questions before the deletion sequence can be committed.
