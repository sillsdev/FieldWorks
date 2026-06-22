# FieldWorks → Avalonia: Complete Migration Program Plan

> **Purpose.** This is the master, end-to-end plan for migrating **all** of FieldWorks'
> WinForms UI and native C++ UI/rendering code to Avalonia + C#. It extends the existing
> `avalonia-migration-roadmap` (which covers Phase 0 spike → Lexical Edit → shell) to the
> *whole* application, organizes the work into stages sized to become **JIRA epics**, and
> sequences the foundation work that must precede broad hand-off to less-AI-experienced
> developers.
>
> **Status:** planning only. No code/behavior change. JIRA epics/issues will be created
> from this document later, not now.
>
> **Grounding:** built from (a) the frozen architecture in
> `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/`, (b) the as-built
> work in `Src/Common/FwAvalonia/` and `openspec/changes/lexical-edit-avalonia-migration/`,
> (c) a full repo surface inventory, and (d) external research on incremental desktop
> migrations (sources cited in the Appendix).

---

## 1. Executive summary

FieldWorks is a large desktop app: ~16 WinForms UI projects, 200+ Form/UserControl classes,
~200 dialogs, a 73-file DataTree slice framework, a 44-file XMLViews browse framework, and a
native C++ **Views** rendering/editing engine (`Src/views/`, VwRootBox/VwSelection/VwTextBoxes)
consumed through registration-free COM and hosted by `SimpleRootSite`. Conservative sizing:
**~150k LOC of UI/render code, an 18–30 month program.**

The first vertical slice — the **Lexical Edit / Advanced Entry** region — is essentially done
and proves the whole approach: a typed view-definition IR compiled from XML, a region
model + composer, owned dense Avalonia controls, frozen seam contracts, a managed
multi-writing-system text-editing foundation (no native Views), a global surface-selection
switch with explicit per-host behavior, and a triangulated parity-evidence harness
(semantic + visual + workflow + performance). **The architecture is decided; do not reinvent it.**

What remains is (1) **hardening that one-off into a reusable platform** other developers can
drive, (2) **a shared editable virtualized grid/tree** (the single biggest off-the-shelf gap),
(3) **migrating each remaining surface** in parallel vertical slices, (4) **replacing the native
Views engine** for the document/multi-paragraph/interlinear surfaces (the long pole), and
(5) **the shell + final cutover + cross-platform enablement**.

The plan below is **13 stages in 4 tracks**. Tracks I (foundation) and III (native engine) are
senior-led; Track II (surfaces) is built for parallel hand-off; Track IV (shell, runtime
modernization, cutover) lands last.

---

## 2. Guiding principles (research-grounded; some already chosen by the repo)

1. **Strangler Fig, never big-bang.** Keep shipping FieldWorks on Windows; build Avalonia
   surfaces beside the old ones; retire WinForms surface-by-surface behind a feature flag.
   (Avalonia's own WinForms guide, Fowler, Spolsky/Netscape all converge here.)
2. **`WinFormsAvaloniaControlHost` is the coexistence spine.** Host whole Avalonia *views*
   inside the running WinForms shell (already proven as `LexicalEditHostControl`). Windows-only
   during transition is acceptable — FieldWorks targets Windows today.
3. **Decouple logic from the UI framework first.** The framework-free seams, typed IR, and
   region models are reusable by both surfaces and are what make per-screen strangling possible.
4. **Prove behavior before replacing it.** Characterization/golden-master baselines (semantic +
   visual + workflow + performance) gate every surface. This is non-negotiable *because* the work
   is AI-assisted — the dominant AI failure mode is **hallucinated parity**.
5. **Reuse the frozen architecture; don't reinvent.** Typed IR, region/composer, owned dense
   controls, plugin registry, seam catalog, surface-selection switch, Path-3 evidence bundle.
6. **Repo divergence from generic advice — managed rendering, not native interop.** Standard
   advice is "keep the native engine via `NativeControlHost`, rewrite last." FieldWorks instead
   bet on a **managed-only** path (no native Views on the Avalonia surface; managed `ITsString`
   editing already works for fields). Tradeoff: more engine work up front, but it unblocks
   cross-platform and avoids the airspace/transparency limits of native hosting. The long pole is
   therefore *replacing* the Views engine for document surfaces, not hosting it.
7. **Editable virtualized grids are the #1 framework gap.** TreeDataGrid is display-only and
   licensing-blocked; standard DataGrid is slow at scale. Plan an **owned virtualized control**.
8. **`AutomationId` is mandatory, day one, every control** — it is the single prerequisite for
   both accessibility identity and Appium/WinAppDriver automation.
9. **One global undo/redo stack** (LCModel action handler). Never a parallel Avalonia history.
10. **Slice work as vertical, file-creating, independently-owned units.** New surfaces are new
    files (conflict-free); defer host-wiring/config edits to a single integration step; cap
    concurrency at ~4–6 active streams; give dialogs to juniors, engine/shell to seniors.
11. **Budget an explicit integration-and-verification phase per milestone.** Clean merges ≠ working
    software (one practitioner spent ~83% of integration time post-merge even with zero conflicts).
12. **Upgrade the look while keeping the functionality.** This program is also chartered to modernize
    the UX: adopt a modernized **Fluent-based** ControlTheme rather than mimicking the legacy WinForms
    chrome. The hard contract is **functional fidelity + density**, not pixel- or look-for-look parity —
    parity evidence asserts behavior/semantics/density, the visual lane allows an intentional restyle.
13. **Fully managed text — Graphite is being sunset.** No native Graphite (or native Views) shaping on
    any Avalonia surface. Complex-script shaping is HarfBuzz/managed only. This supersedes the earlier
    per-writing-system "classify-and-warn" compromise in `graphite-transition-support`: Graphite is
    *removed*, not warned.
14. **Land .NET 10 + Avalonia 12 before the program closes** (its own stage). Sequenced *late* — after
    the WinForms shell and most WinForms surfaces are gone — so the runtime jump ports surviving managed
    code, not throwaway WinForms (same "don't invest in code being deleted" principle that froze DataTree).

---

## 3. Current state (what is already done — "Stage 0")

This is the proven baseline the program builds on. Source: `lexical-edit-avalonia-migration`
(Phases 0–1 complete, 2–8 mostly complete) and `avalonia-multi-writing-system-text-foundation`
(completed 2026-06-15).

| Asset | Where | Reusable for |
| --- | --- | --- |
| Typed view-definition IR + XML importer + compiler (deterministic, cached, off-thread) | `Src/Common/FwAvalonia/ViewDefinition/` | Every surface driven by XML layouts |
| Region model + composer (boundary **above** DataTree) | `Src/xWorks/FullEntryRegionComposer.cs`, `Src/Common/FwAvalonia/Region/LexicalEditRegionModel.cs` | Detail/tree/browse surfaces |
| Owned dense controls (multi-WS text, chooser flyout, reference vector, dialog launcher) | `Src/Common/FwAvalonia/Region/FwFieldControls.cs`, `FwOptionPicker.cs`, `RegionMenuFlyout.cs` | All field/cell rendering |
| Managed multi-WS `ITsString` editing (fonts, RTL/bidi, IME, grapheme clusters, ghost realization) | `FwMultiWsTextField` | All text fields (no native Views) |
| Frozen seam contracts (edit session, undo, validation, scheduler, lifetime, refresh, navigation, command bridge, clipboard, drag-drop) | `Src/Common/FwAvalonia/Seams/ISeams.cs`, `ActiveHostContract.cs` | Every surface |
| Plugin registry for custom/legacy slice classes | `Src/xWorks/RegionEditorPlugins.cs`, `ChorusNotesPlugin.cs` | Custom slices anywhere |
| Surface-selection switch (explicit Supported / ExplicitLegacyFallback / Blocked per host) | `Src/Common/FwAvalonia/LexicalEditSurfaceSelectionService.cs`, `Src/xWorks/RecordEditView.cs` | Every host |
| In-process WinForms→Avalonia host bridge | `Src/Common/FwAvalonia/LexicalEditHostControl.cs` | Coexistence spine |
| Path-3 parity evidence harness (semantic + visual + workflow + performance), legacy timing baselines | `FwAvaloniaTests/Path3BundleTests.cs`, `DetailControlsTests/DataTreeTimingBaselines.json` | Every parity claim |
| Engine-isolation symbol audit (forbidden WinForms/Views/Graphite/Gecko symbols) | `FwAvaloniaTests/EngineIsolationAuditTests.cs` | Every migrated region |
| Region manifest template (gates, perf budgets, rollback) | `lexical-edit-avalonia-migration/region-manifest.md` | Every surface's definition-of-done |

**Open items still inside Stage 0** (fold into Stage 4): table/browse virtualization (7.x), full
P0/P1 field parity beyond first slice, 150% DPI + typing-latency budgets, JSON view-definition
serialization/retirement (9.x), Path-3 completeness across all scenarios.

---

## 4. The stages at a glance

| # | Stage (→ JIRA epic) | Track | Depends on | Parallel? | Lead level | Exit gate |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Migration platform & developer-enablement kit | I Foundation | Stage 0 | — | Senior | Kit + runbook proven by a junior+Claude migrating a trivial surface end-to-end |
| 2 | Coexistence shell spine & host contracts | I Foundation | 1 | with 3 | Senior | Any Avalonia view hostable in WinForms shell behind the flag; theming + AutomationId conventions locked |
| 3 | Shared editable virtualized grid/tree control | I Foundation | 1 | with 2 | Senior | Owned control passes 10k-row browse + 253-slice tree at 100%/150% DPI within perf budget |
| 4 | Finish Lexical / Advanced Entry surface (exemplar) | II Surfaces | 2,3 | — | Senior | Region manifest fully green; becomes the reference implementation |
| 5 | Global dialogs & choosers (FwCoreDlgs + shared) | II Surfaces | 2 | yes (4–6 streams) | **Junior-friendly** | Each dialog: parity bundle + AutomationIds + localization; WinForms-owned modality honored |
| 6 | Lexicon completion + Grammar/Morphology detail | II Surfaces | 4 | yes | Mid | Detail surfaces at parity via region/composer + plugins |
| 7 | Texts & Words / Interlinear + Discourse | II Surfaces | 3,9 | partial | Senior | Interlinear/concordance/chart parity on managed document engine |
| 8 | Notebook, Lists, Dictionary-config UI, remaining tools | II Surfaces | 4,5 | yes | Mid | Remaining areas at parity |
| 9 | Managed document/text rendering & editing engine (Views replacement) | III Long pole | 1 | with II | Senior | Multi-paragraph/StText + structured editing parity; no native Views on Avalonia path |
| 10 | Browser/PDF & dictionary-preview replacement; **Graphite full removal** | III Long pole | 9 | with II | Senior | Gecko + Graphite **removed** from codebase; fully-managed shaping; preview/print parity |
| 11 | Application shell replacement (window, areas, menus, toolbars, lifetime) | IV Shell | 2, enough of 5–8 | partial | Senior | Avalonia shell hosts migrated screens; `fieldworks-avalonia-shell-migration` gates pass |
| 12 | **Runtime & toolchain modernization (.NET 10 + Avalonia 12)** | IV Shell | 11, most of 5–10 | — | Senior | Whole process on .NET 10 + Avalonia 12; build/test/CI green |
| 13 | Final cutover, native decommission & cross-platform enablement | IV Cutover | all | — | Senior | Avalonia default; WinForms + native Views/COM UI retired; Linux/macOS smoke green |

Cross-cutting concerns (accessibility/UIA, localization, performance, parity evidence) are **not**
separate stages — they are continuous per-surface gates enforced by the harness from Stage 1 on.

---

## 5. Sequencing and parallelism

```mermaid
flowchart TB
  S0["Stage 0 (done)\nLexical-edit foundation + first region"]:::done

  subgraph TI["Track I — Foundation (senior, sequential-ish)"]
    S1["1. Platform & enablement kit"]:::found
    S2["2. Coexistence shell spine + contracts"]:::found
    S3["3. Shared editable virtualized grid/tree"]:::found
  end

  subgraph TII["Track II — Surfaces (parallel hand-off)"]
    S4["4. Finish Lexical/Advanced Entry (exemplar)"]:::surf
    S5["5. Global dialogs & choosers (junior)"]:::surf
    S6["6. Lexicon + Grammar/Morphology"]:::surf
    S7["7. Texts & Words / Interlinear + Discourse"]:::surf
    S8["8. Notebook/Lists/Dict-config/remaining"]:::surf
  end

  subgraph TIII["Track III — Long pole (senior, parallel)"]
    S9["9. Managed document/text engine (Views replacement)"]:::pole
    S10["10. Browser/PDF + Graphite full removal"]:::pole
  end

  subgraph TIV["Track IV — Shell, modernization & cutover"]
    S11["11. Application shell replacement"]:::shell
    S12["12. Runtime modernization (.NET 10 + Avalonia 12)"]:::shell
    S13["13. Final cutover + native decommission + cross-platform"]:::cut
  end

  S0 --> S1 --> S2 --> S4
  S1 --> S3 --> S4
  S1 --> S9 --> S10
  S2 --> S5
  S4 --> S6
  S3 --> S7
  S9 --> S7
  S4 --> S8
  S5 --> S8
  S5 & S6 & S7 & S8 --> S11
  S2 --> S11
  S11 --> S12
  S10 --> S12
  S12 --> S13

  classDef done fill:#e2e8f0,stroke:#64748b,color:#0f172a;
  classDef found fill:#fef9c3,stroke:#ca8a04,color:#422006;
  classDef surf fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef pole fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef shell fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef cut fill:#fee2e2,stroke:#b91c1c,color:#450a0a;
```

**The critical path** is `Stage 1 → 2/3 → 4 → 11 → 12 → 13` for the shell/runtime, and
`Stage 1 → 9 → 10 → 13` for the native engine. Track II surfaces fan out wide and absorb most of the
parallel head-count. **The .NET 10 + Avalonia 12 jump (Stage 12) is deliberately late** — it runs after
the shell and most surfaces are Avalonia so it ports surviving code, not soon-to-be-deleted WinForms.
During coexistence the whole process stays on the current Avalonia 11.x / .NET Framework 4.8 (one CLR
per process); new code is written Avalonia-12-ready (avoiding APIs removed in 12) but the actual bump
ships in Stage 12.
**Hand-off to less-experienced developers begins after Stage 1 completes** (the kit exists) and
**accelerates after Stage 4** (a fully-worked exemplar exists to copy).

---

## 6. Stage detail (JIRA epic → representative sub-tasks)

Each stage is one **epic**. Sub-bullets are representative **issues** (real backlog will be finer).
Every surface-migrating issue inherits the per-region **Definition of Done** in §7.

### Track I — Foundation (must precede broad hand-off)

#### Stage 1 — Migration platform & developer-enablement kit  *(senior)*
Turn the lexical-edit one-off into a reusable, documented platform a mid/junior dev + Claude can drive.
- Extract a reusable **region scaffolding** template (new-surface generator: composer skeleton, region
  model, view, manifest stub, test bundle stub).
- Promote the **Path-3 evidence harness** to a shared test base any surface test can derive from.
- Freeze & document the **seam catalog** and **plugin-registry** onboarding ("how to add a custom slice").
- Author a **"migrate-a-surface" runbook** mapping the 10-step workflow to concrete repo actions; wire it
  to the `fieldworks-winforms-to-avalonia-migration` skill so AI assistants follow it.
- Lock **conventions**: AutomationId derivation from StableId, density tokens, ControlTheme baseline,
  localization lanes (StringTable for labels, `FwAvaloniaStrings.resx` for product strings), `<RootNamespace>`.
- **Validation:** a junior+Claude migrates one trivial surface (e.g. a single simple dialog) end-to-end
  using only the kit + runbook, with a green parity bundle. That is the gate.

#### Stage 2 — Coexistence shell spine & host contracts  *(senior)*
Generalize the host bridge and surface switch beyond Lexical Edit so any surface can coexist.
- Generalize `LexicalEditHostControl` → a reusable `WinFormsAvaloniaControlHost`-based region host.
- Generalize `LexicalEditSurfaceSelectionService` → an app-wide surface registry/switch with explicit
  per-host `Supported/ExplicitLegacyFallback/Blocked` decisions.
- Stand up the **XCore mediator/PropertyTable bridge** seam (`IXCoreCommandBridge`) for shell-scope commands.
- Global **feature-flag** plumbing (default WinForms) and the dual-run build.
- **ControlTheme** baseline matching legacy look + density; theming resource pipeline.
- Pull forward the **contract layer** of `fieldworks-avalonia-shell-migration` (window/dialog ownership
  contracts) without yet replacing the shell.
- Apply `fieldworks-ui-wiring-review`.

#### Stage 3 — Shared editable virtualized grid/tree control  *(senior)*
The identified off-the-shelf gap; build it once, many surfaces depend on it.
- Spike & decide: owned virtualizing list/table over `VirtualizingStackPanel` vs. fully-owned realization
  window (record the fired pivot trigger per `seam-catalog.md` §3 if deviating).
- Implement an **owned virtualized table** (browse/XMLViews) and **owned virtualized tree** (slice/detail,
  flattened with expander/indent) with editing, selection, keyboard, and custom automation peers.
- Prove against **large fixtures**: 10k-row browse, 253-slice detail, at **100% and 150% DPI**, within the
  measured legacy performance budget.
- **Do not** build on TreeDataGrid (display-only, licensing). Record the decision in the manifest.

### Track II — Surface migration (parallel, hand-off-friendly)

#### Stage 4 — Finish the Lexical / Advanced Entry surface (the exemplar)  *(senior)*
Close the open Stage-0 items so this region is 100% green and becomes the copy-me reference.
- Tables/browse in the entry view on the Stage-3 control (lexical-edit tasks 7.x).
- Full **P0/P1 field parity** beyond the first slice (custom fields, rich references, media/pronunciation).
- **150% DPI + scroll/expand/typing-latency** budgets (tasks 2.13, 7.7).
- **JSON view-definition** serialization + override migrator + runtime-XML-disable for the gated surface (9.x).
- Path-3 bundle completeness across all entry scenarios; region manifest fully green.

#### Stage 5 — Global dialogs & choosers  *(junior-friendly, 4–6 parallel streams)*
~200 dialogs; mostly mechanical, file-creating, low merge contention — the main hand-off reservoir.
- `Src/FwCoreDlgs/` first (most reused): Font, Styles, Apply-Style, Writing-System setup, New-Project
  wizard, Project properties, Backup/Restore, Find/Replace, Valid-Characters, Chooser, converters.
- Shared chooser/launcher infrastructure (the `FwOptionPicker`/flyout pattern).
- Domain dialogs across `xWorks`, `LexText`, `FdoUi` as their owning areas migrate.
- **Coexistence rule:** until the shell migrates (Stage 11), anything modal stays a WinForms dialog owned
  by the host form (`dialog-ownership.md`); new Avalonia dialog *content* is fine inside the host.
- Each dialog: parity bundle + AutomationIds + localization review.

#### Stage 6 — Lexicon completion + Grammar/Morphology detail  *(mid)*
Detail-view-heavy areas that reuse region/composer + plugin registry directly.
- Lexicon (`Src/LexText/Lexicon`, `LexTextControls`) remaining slices/launchers (MSA, references, examples).
- Morphology (`Src/LexText/Morphology`): inflection features/classes, phonological environments, categories.
- Grammar detail editors via `FdoUi` editors (POS, inflection, phonological features).
- Custom slice classes → plugin registry with burn-down tracking.

#### Stage 7 — Texts & Words / Interlinear + Discourse  *(senior; depends on Stage 9)*
The most complex non-shell surface; Views-engine-heavy document rendering.
- Interlinear doc + sandbox (`Src/LexText/Interlinear`, ~98 files): word/morpheme breakdown, glossing, POS.
- Concordance + raw-text + statistics views.
- Constituent charts (`Src/LexText/Discourse`): chart body, logic, export.
- Import wizards (SFM, LinguaLinks) — can be split to Stage-5-style hand-off.
- Depends on the managed document engine (Stage 9) and shared tables (Stage 3).

#### Stage 8 — Notebook, Lists, Dictionary-config UI, remaining tools  *(mid)*
- Notebook area, Lists editors, bulk-edit surfaces.
- Dictionary configuration dialogs (`DictionaryConfigurationDlg` family) and config preview wiring.
- Remaining utilities/tools; sweep for stragglers via the surface registry.

### Track III — The long pole (native engine; senior, parallel with Track II)

#### Stage 9 — Managed document/text rendering & editing engine (Views replacement)  *(senior)*
Replace the native C++ Views engine for **document/multi-paragraph/structured** surfaces. (Field-level
multi-WS editing is already managed — `FwMultiWsTextField`.)
- **Spike first** (de-risk the #1 framework gap): mixed bidi (Arabic/Hebrew + Latin), CJK IME, custom
  writing systems, multi-paragraph `StText` editing, selection across structured content. Decide build-vs-extend.
- **Fully managed shaping — no Graphite, no native Views.** Complex-script shaping is HarfBuzz/managed only
  (decision confirmed 2026-06-15). Verify HarfBuzz coverage for the scripts Graphite formerly handled during
  the spike; this is a gating risk for the Graphite sunset.
- Managed text layout/shaping on HarfBuzz/SkiaSharp (`TextLayout`); managed selection/caret model replacing
  `VwSelection`; structured document model replacing `VwRootBox`/box hierarchy for the surfaces that need it.
- Editing path: keystroke/IME/clipboard/undo through the existing seams and one global undo stack.
- Validate against the rich-text/bidi/IME open-issue risks identified in research; keep the bridge coarse if
  any residual native call survives transitionally.
- Forbidden-symbol audit stays green on every migrated surface.

#### Stage 10 — Browser/PDF & dictionary-preview replacement; **Graphite full removal**  *(senior)*
- Replace Gecko/XULRunner-based dictionary preview & PDF export (`GeckoWebBrowser`, `GeckofxHtmlToPdf`).
- Managed print/preview parity.
- **Sunset Graphite entirely** (decision confirmed 2026-06-15): this supersedes
  `graphite-transition-support`'s per-WS "classify-and-warn" compromise. Remove the native Graphite engine
  (`GraphiteEngineClass`) and its references from the codebase, not just from the default path. Gated on
  Stage 9 proving HarfBuzz/managed shaping covers the formerly-Graphite scripts.

### Track IV — Shell, runtime modernization & cutover

> Track IV is ordered **shell → runtime jump → cutover** on purpose: replacing the WinForms shell (11)
> and surfaces first means the .NET 10 + Avalonia 12 jump (12) ports surviving managed code rather than
> WinForms that's about to be deleted.

#### Stage 11 — Application shell replacement  *(senior; = `fieldworks-avalonia-shell-migration` body)*
- Avalonia application lifetime, main-window ownership, multi-window, active-window tracking, shutdown.
- Compile `Language Explorer/Configuration/Main.xml` into typed shell definitions (commands, menus, context
  menus, toolbars, sidebars, status, shortcuts, listeners, screen/tool registrations).
- Avalonia shell composition: navigation (replace `SilSidePane`/OutlookBar), content hosting, record/side
  panes (replace `CollapsingSplitContainer`/`MultiPane`), menus/toolbars/status, diagnostics, accessibility.
- Retire `FlexUIAdapter` default behavior; route mediator/PropertyTable through the typed command bridge.
- Migrate screens area-by-area into the Avalonia main-screen registry.

#### Stage 12 — Runtime & toolchain modernization (.NET 10 + Avalonia 12)  *(senior)*
The chartered "move to modern tools" jump, sequenced late by design.
- Port the surviving managed codebase from **.NET Framework 4.8 → .NET 10** (WinForms-on-net48 host is gone
  or nearly gone by now; any residual WinForms moves to WinForms-on-.NET 10, Windows-only).
- Bump **Avalonia 11.x → 12**; resolve breaking changes flagged during coexistence (new code was written
  Avalonia-12-ready to minimize this).
- One CLR per process: this is a coordinated whole-process bump, not a per-project trickle. Land it behind
  the green build/test/CI gate; retarget `net48`/`net8` multi-targeting in test projects accordingly
  (apply `fieldworks-managed-netfx-review`).
- Prerequisite for cross-platform (net48 is Windows-only; .NET 10 is not).

#### Stage 13 — Final cutover, native decommission & cross-platform enablement  *(senior)*
- Flip the global default to Avalonia after all region/shell manifests pass.
- Delete the WinForms shell, WinForms-only dialogs, DataTree/Slice, SimpleRootSite/RootSite, XMLViews,
  and the WinForms↔Avalonia interop spine.
- Decommission native C++ **UI/render** projects (`Src/views/`, `ManagedVwDrawRootBuffered`) and the
  `IVwRootBox`/`IVwGraphics`/`IVwEnv` COM surface; keep non-UI native/linguistics services (Kernel, Generic,
  ICU, XAmple, encoding converters, parsers) behind service seams.
- Installer/packaging changes; remove Gecko harvest.
- **Cross-platform enablement** (now unblocked by the managed path + .NET 10 from Stage 12): Linux/macOS
  build + headless + smoke. Held to this final stage by decision (2026-06-15) — no cross-platform validation
  cost is incurred earlier in the program.
- Final cross-cutting gates: accessibility (Narrator/NVDA spot-checks), localization parity, performance.

---

## 7. Definition of Done (per surface — applies inside every Track-II/III issue)

Reuse the frozen per-region checklist
(`.claude/skills/fieldworks-winforms-to-avalonia-migration/references/migration-checklist.md`).
Condensed gate: a surface is **migrated** only when —
1. Custom slice census taken; plugins exist or explicit "unsupported" rows render (never silent fallback).
2. Semantic + visual + workflow + performance baselines captured *before* refactor and matched after.
3. Seams reused from `ISeams.cs`; any new seam recorded in `seam-catalog.md` with a pivot trigger.
4. Owned-control choices per `architecture-patterns.md` §4; deviations justified by a fired pivot trigger.
5. Composer walks compiled IR; stable AutomationIds from StableId; ghost rows are runtime-only.
6. Explicit `HostUiBehavior` per host; full wiring path traced; active-host contract holds (no hidden DataTree/Views).
7. Path-3 parity bundle per scenario; perf ≤ legacy × 1.2 or accepted delta recorded; **100% + 150% DPI**.
8. Localization lanes correct; AutomationIds nonlocalized, Names localized.
9. `EngineIsolationAuditTests` + active-host contract tests pass; `./build.ps1` + `./test.ps1` green.
10. Retrospective folds new lessons back into the skill set in the **same** PR.

**Evidence language is enforced:** a checked task whose evidence says *substitute / placeholder /
skipped / future / partial* is a review blocker (`parity-evidence.md`).

---

## 8. Staffing & hand-off model

- **Seniors (with Claude):** Stages 1, 2, 3, 4, 7, 9, 10, 11, 12, 13 — foundation, engine, shell, runtime
  modernization, exemplar.
- **Mid (with Claude):** Stages 6, 8 — detail surfaces that follow the exemplar pattern.
- **Junior (with Claude):** Stage 5 dialog streams — high-volume, mechanical, well-fenced; the runbook +
  exemplar make these safe. Cap at **4–6 parallel streams**; each stream owns whole files.
- **Integration owner:** one senior runs a per-milestone **integration-and-verification** pass (host wiring,
  cross-surface refresh/undo, headless + screen-reader + perf) — clean merges are not "done."
- **Hand-off prerequisites:** Stage 1 (kit + runbook) before any junior work; Stage 4 (worked exemplar)
  before scaling Track II head-count.

---

## 9. Risk register (top risks + mitigation)

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Rich-text / bidi / IME gaps in Avalonia for complex scripts | High | High | Managed text foundation already proven for fields; Stage 9 spike-first on document editing; keep coarse interop fallback if needed |
| Editable virtualized grid at scale | High | High | Stage 3 owns the control; validate on 10k-row/253-slice fixtures at 150% DPI before any dependent surface |
| AI "hallucinated parity" (claims done, isn't) | High | High | Mandatory Path-3 evidence bundle + evidence-language enforcement + integration owner verification |
| Native Views engine replacement underestimated (long pole) | Med | High | Senior-only; spike-first; runs in parallel so it doesn't block dialogs/detail surfaces |
| Merge contention across parallel teams | Med | Med | Vertical file-owning slices; defer wiring to integration step; ~4–6 stream cap |
| Coexistence threading/focus/modality bugs | Med | Med | WinForms owns all modality until Stage 11; `dialog-ownership.md` rules; finalizer-safe sync context |
| Scope drift / mixed PRs | Med | Med | `fieldworks-migration-scope-review`; one surface per PR; skill retrospective in same PR |
| Shell migration timing pulled too early | Low | High | Gate 11 on enough of Stages 5–8; existing roadmap already defers shell |
| HarfBuzz/managed shaping doesn't cover formerly-Graphite scripts | Med | High | Stage 9 spike verifies coverage *before* Stage 10 removes Graphite; removal gated on the spike result |
| .NET 10 / Avalonia 12 breaking changes ripple late | Med | Med | New code written Av12-ready; jump sequenced after WinForms is mostly gone so the port surface is smaller; `fieldworks-managed-netfx-review` |
| Cross-platform regressions surface only at the end | Med | Med | Accepted tradeoff (held to Stage 13 by decision); headless tests are cross-platform-capable from Stage 1 to catch logic regressions early even though OS smoke is deferred |

---

## 10. JIRA structure suggestion

- **1 program/initiative:** "FieldWorks → Avalonia complete migration."
- **13 epics:** one per stage above.
- **Issues under each epic:** one per surface/dialog/control, carrying the §7 Definition of Done as a
  checklist and the region-manifest fields as acceptance criteria.
- **Labels:** `track-foundation | track-surfaces | track-longpole | track-shell`, `lead-junior|mid|senior`,
  `parallel-safe`, `parity-blocked-by:<seam>`.
- **Dependencies:** wire epic links per the §5 graph; mark Stage 1 as blocking all junior issues and
  Stage 4 as the "scale-up" milestone.
- Existing OpenSpec changes map onto epics: `lexical-edit-avalonia-migration` → Stage 4 close-out;
  `avalonia-multi-writing-system-text-foundation` → Stage 0/9; `graphite-transition-support` → Stage 10;
  `fieldworks-avalonia-shell-migration` → Stage 11.

---

## 11. Decisions (resolved 2026-06-15)

1. **Graphite → fully managed only.** Graphite is sunset and *removed* from the codebase (not warned),
   superseding the per-WS compromise in `graphite-transition-support`. Complex-script shaping is
   HarfBuzz/managed. Reflected in Stages 9 and 10; the Stage 9 spike must verify HarfBuzz coverage for
   formerly-Graphite scripts before Stage 10 removes the engine.
2. **Cross-platform held to the final stage (13).** No Linux/macOS validation cost is incurred earlier;
   headless tests stay cross-platform-capable so logic regressions are still caught during the program.
3. **New standalone dialogs/wizards use modern Avalonia MVVM — CommunityToolkit.Mvvm + compiled bindings —
   NOT the region/composer pattern.** *Rationale:* the region/IR/owned-control machinery exists for surfaces
   driven by FieldWorks' **XML view-definitions** (the entry/detail/browse views) — it compiles XML layouts
   into a typed IR and data-binds dense owned controls. **Dialogs and wizards have no XML layout to compile**;
   they are hand-authored UI with bespoke logic, so forcing them through the IR machinery is a misfit and
   pure overhead. Idiomatic MVVM fits them, and aligns with the "migrate to modern tools" charter:
   *CommunityToolkit.Mvvm* gives source-generated observable properties/commands (less boilerplate, gentler
   curve for less-experienced devs), and *compiled bindings* (`x:CompileBindings`) make bindings
   statically checked and refactor-safe — which catches a whole class of AI-introduced binding errors at
   build time. The **owned writing-system-aware field controls** (`FwMultiWsTextField`, `FwOptionPicker`)
   are still reused *inside* dialogs wherever WS-aware text or chooser fields appear. Net rule:
   **region pattern for IR-driven surfaces; MVVM + compiled bindings for dialogs/wizards/shell.**
4. **Upgrade the look; keep the functionality.** Adopt a modernized **Fluent-based** ControlTheme rather
   than mimicking the legacy WinForms chrome. The contract is functional fidelity + density (asserted by
   the semantic/workflow/perf parity lanes); the visual lane permits an intentional restyle. This is the
   chartered UX upgrade, not a regression.

### Remaining open question (spike decides, not blocking epic creation)

- **Document-engine build-vs-extend (Stage 9):** fully-managed rewrite of the document/structured-text
  surfaces vs. extending an existing managed editor. The Stage 9 spike output decides; both paths are
  fully managed (no native Views, no Graphite) per decision 1.

---

## Appendix — external research sources (high-signal)

- Avalonia official **WinForms migration guide** (incremental; `WinFormsAvaloniaControlHost`): https://docs.avaloniaui.net/docs/migration/winforms/
- Avalonia **native interop / NativeControlHost** (airspace limits): https://docs.avaloniaui.net/docs/app-development/native-interop
- Avalonia **XPF** (WPF-compat product — *not* applicable to WinForms origin): https://docs.avaloniaui.net/xpf/welcome
- **TreeDataGrid** (display-only, no editing): https://avaloniaui.net/blog/announcing-the-release-of-treedatagrid
- Avalonia **rich-text editor** (Pro tier; RTL unverified): https://avaloniaui.net/blog/rich-text-editor
- **AvaloniaEdit** (code editor, not rich text): https://github.com/AvaloniaUI/AvaloniaEdit
- Avalonia **headless testing**: https://docs.avaloniaui.net/docs/concepts/headless/  •  **accessibility**: https://docs.avaloniaui.net/docs/app-development/accessibility  •  **Appium UI testing**: https://docs.avaloniaui.net/docs/testing/ui-testing-with-appium  •  **ControlThemes**: https://docs.avaloniaui.net/docs/basics/user-interface/styling/control-themes
- **Strangler Fig** (Fowler): https://martinfowler.com/bliki/StranglerFigApplication.html  •  **Branch by Abstraction**: https://martinfowler.com/bliki/BranchByAbstraction.html
- **Working Effectively with Legacy Code** (seams) summary: https://understandlegacycode.com/blog/key-points-of-working-effectively-with-legacy-code/  •  **Characterization tests**: https://en.wikipedia.org/wiki/Characterization_test
- **Joel Spolsky — never rewrite from scratch**: https://www.joelonsoftware.com/2000/04/06/things-you-should-never-do-part-i/  •  **Netscape 6**: https://en.wikipedia.org/wiki/Netscape_6
- **JetBrains WPF→Avalonia** case: https://avaloniaui.net/success/jetbrains  •  WinForms→Avalonia "near-100% rewrite" maintainer note: https://github.com/AvaloniaUI/Avalonia/discussions/11104  •  **Expert guide to porting** (~9 hrs/view): https://avaloniaui.net/blog/the-expert-guide-to-porting-wpf-applications-to-avalonia
- **C++/CLI interop perf**: https://learn.microsoft.com/en-us/cpp/dotnet/performance-considerations-for-interop-cpp
- **Google LLM migration at scale** (~50% time saved, checkpoints+review): https://getdx.com/research/migrating-code-at-scale-with-llms-at-google/
- **Vertical slicing / parallel dev**: https://medium.com/@kmorpex/vertical-slicing-the-key-to-better-net-projects-991c1c757393  •  zero-conflict architecture: https://dev.to/aviad_rozenhek_cba37e0660/zero-conflict-architecture-the-8020-of-parallel-development-5aok

> Caveat: the richest WinForms→Avalonia case studies are vendor-published (avaloniaui.net) and
> promotional in framing; independent numbers-rich retrospectives are scarce. FieldWorks may become
> one of the more substantial public examples.
