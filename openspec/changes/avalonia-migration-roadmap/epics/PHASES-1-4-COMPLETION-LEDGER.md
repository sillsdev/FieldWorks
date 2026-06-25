# Phases 1–4 — completion ledger (in-session)

> **Close-out update (2026-06-16).** Two items previously bucketed as blocked/unbuilt have since
> **landed and are integrated on this branch**, so the buckets below are partly stale (corrected
> inline):
> - **Stage 3 editable virtualized table (3a/3b/3c + AutomationPeers)** — built and consolidated
>   (`LexicalBrowseView` + capability interfaces + peer, `ClerkBrowseRowSource`/`ClerkBrowseEditContext`,
>   `LexicalBrowseHostControl`, `RecordBrowseView` wiring behind `ResolveBrowse`/`UIMode=New`). No longer
>   "Bucket D / not a session."
> - **Dialog-MVVM kit (1.2)** — the net48 compiled-bindings spike is done and verified
>   (`FwAvaloniaDialogs` builds green through `build.ps1`; first dialog Tools→Options; dialog suite 9/9).
>   No longer "unbuilt — top item."
>
> A multi-layer close-out review this session (architecture, dead/POC/naming, test coverage,
> completeness) found: **no dead/POC/spike code in production assemblies**, naming sound, OpenSpec
> `--strict` valid for both changes, **no overclaims**. Cleanups applied off the review: reparented
> `LexicalBrowseHostControl` onto the shared `AvaloniaRegionHostControl` base (removed duplicate Avalonia
> bootstrap + restored directional-key interop) and unified the surface-resolve precedence
> (`ResolveFromPreference`). Remaining xWorks browse-class test coverage and the full native+managed
> traversal are tracked below.

> Honest definition-of-done for the "finish phases 1–4" pass, under the user's directive
> **"do everything feasible, skip blocked."** Buckets every item as **verified-done this session**,
> **already built (pre-existing)**, **feasible-remaining (tracked follow-up)**, or **blocked
> (out-of-session by nature)**. Phases 1–4 are **spec-complete** (finished epic docs
> `stage-01..04-*.md`); this ledger tracks the *code*.

## A. Verified done this session (built + tests green)
| Item | Stage | Evidence |
| --- | --- | --- |
| Resolver tool-gating contract tests — unregistered tool never silently resolves to Avalonia (incl. override/New cases); null = "no tool gate" documented | 2.2 | `FwAvaloniaTests/LexicalEditSurfaceResolverTests.cs`; `.\test.ps1 -SkipNative -TestProject FwAvaloniaTests` → **20/20 passed** |
| Owned-control AutomationId convention made executable (stable id stamp + localized Name + per-WS suffix) | 1.5 | `FwAvaloniaTests/OwnedControlAutomationConventionTests.cs` → **2/2 passed** (headless `[AvaloniaTest]`) |
| Confirmed the in-progress viewing-replacement unit compiles + passes | 1/4 | `RegionViewingServices.cs` + `RegionViewingServiceReplacementTests.cs` green in baseline run |
| **View-definition override differ** — base vs customized IR → sparse StableId-keyed patch with the full representable op set (**setVisibility, setLabel, reorderChildren, hideNode, addNode** with parent+index); only changed binding/editor/kind remains a diagnostic, never a silent drop | 4.3 / task 9.2 (core) | `ViewDefinition/ViewDefinitionOverrideDiffer.cs` + `FwAvaloniaTests/ViewDefinitionOverrideDifferTests.cs` → **8/8 passed** |
| **Override JSON wire format** — deterministic canonical serialization of the sparse patch (mirrors `ViewDefinitionJsonSerializer`: Newtonsoft, ordered keys, defaults omitted, formatVersion header) + lossless round-trip of all op kinds incl. addNode + audit diagnostics; wrong-version guard | 4.3 / task 9.2 (wire format) | `ViewDefinition/ViewDefinitionOverrideJsonSerializer.cs` + `FwAvaloniaTests/ViewDefinitionOverrideJsonSerializerTests.cs` → **6/6 passed** |
| **Override applier (load side)** — applies a sparse patch to a shipped model to produce the customized model; inverse of the differ (`Apply(base, Diff(base, custom)) == custom` proven for representable changes); stale patch targets reported as diagnostics, not fatal | 4.3 / task 9.2 (apply) | `ViewDefinition/ViewDefinitionOverrideApplier.cs` + `FwAvaloniaTests/ViewDefinitionOverrideApplierTests.cs` → **5/5 passed** |
| **`.fwlayout` migrator core** — imports a shipped layout + a project's customized copy via the real `XmlLayoutImporter` and diffs them into a sparse patch (+ `MigrateLayoutToJson`); the framework-neutral core of the legacy-override → sparse-patch migration | 4.3 / task 9.2 (driver) | `ViewDefinition/ViewDefinitionOverrideMigrator.cs` + `FwAvaloniaTests/ViewDefinitionOverrideMigratorTests.cs` → **3/3 passed** |
| **`.fwlayout` file-I/O driver** — reads a project's whole-copy override file from disk, diffs vs the shipped layout, writes the canonical JSON patch file (shipped-layout + parts injected, so only the `Inventory` lookup is left to XCore) | 4.3 / task 9.2 (file driver) | `ViewDefinition/ViewDefinitionOverrideFileMigrator.cs` + `FwAvaloniaTests/ViewDefinitionOverrideFileMigratorTests.cs` → **3/3 passed** (override suite **28/28**, verified with temp files) |
| **Runtime-XML-disable loader (9.4)** — a gated/migrated surface loads committed canonical JSON instead of runtime XML, with the XML compile retained as the audit/fallback lane; missing/invalid JSON falls back with an explicit diagnostic, never silent | 4.4 / task 9.4 | `ViewDefinition/ViewDefinitionLoader.cs` + `FwAvaloniaTests/ViewDefinitionLoaderTests.cs` → **4/4 passed** (whole ViewDefinition area **42/42**) |
| **`duplicateNode` op** — completes the design's full representable override op set (setVisibility/setLabel/reorderChildren/hideNode/addNode/duplicateNode); leaf copy-with-new-id; subtree/missing source → diagnostics | 4.3 / task 9.2 (op set) | applier + serializer + tests → override suite **25/25 passed** |
| **App-wide surface registry (2.2)** — generalizes the resolver's hardcoded supported-tool list into an injectable registry; tools opt in by registration; unregistered tools never resolve to Avalonia; resolver refactored with zero regression | 2.2 | `LexicalEditSurfaceRegistry.cs` + resolver overloads + tests → LexicalEditSurface suite **35/35 passed** |
| **Surface census (2.2)** — the living UI-surface inventory + migration-state tracker that Stage 8's straggler sweep reconciles against | 2.2 | `epics/SURFACE-CENSUS.md` (doc artifact) |
| **Dual-projector unify (18.11)** — extracted `RegionStructureProjector` owning the section-header construction + child-indent rule; **both** the thin `LexicalEditRegionMapper` (FwAvalonia) and the full `FullEntryRegionComposer` (xWorks) now route through it (editor→kind already shared via `EditorKindMap`). Behavior-preserving by construction; verified on both paths | 4.5 / task 18.11 | `Region/RegionStructureProjector.cs` + `RegionStructureProjectorTests.cs`; FwAvalonia region **91/91** + xWorks composer region **137/137** passed |
| **Host generalization (2.1)** — extracted the reusable `AvaloniaRegionHostControl` base (Avalonia bootstrap, companion strip, WinForms/Avalonia directional-key interop, focus-safe content swap, context menus, message/clear) out of `LexicalEditHostControl`, which now adds only the lexical-edit `ShowRegion` + splitter memory; public API preserved (RecordEditView unchanged) | 2.1 | `AvaloniaRegionHostControl.cs` + slimmed `LexicalEditHostControl.cs`; build-verified across the solution + host/surface/region **134/134** + xWorks host-contract/switch **10/10** (interop test re-pointed at the base) |
| **9.2 `.fwlayout` Inventory glue** — `LexicalEditOverrideMigration` (xWorks): bridges the live `Inventory` (parts root + shipped layout node) to the tested migration core. Resolved the earlier "baseline" concern by keeping the shipped layout a **caller-provided** param (the adapter doesn't decide the baseline) — a clean composition of tested parts | 4.3 / task 9.2 (xWorks adapter) | `Src/xWorks/LexicalEditOverrideMigration.cs` + `LexicalEditOverrideMigrationTests.cs`; XElement core **2/2** + Inventory overload build-verified by the xWorks build. (Only the caller's choice of *pristine* shipped-layout source needs a real-project smoke test.) |

> Finding (no code change needed): the resolver's **safety property already held** — non-null
> unrecognized tools already return WinForms. The gap was *test coverage*, now closed. Changing the
> `null`→permissive behavior would have been a regression (existing `Resolve()` tests depend on it), not a fix.

## B. Already built (pre-existing — counted toward "finished")
- Region pipeline, owned controls, surface switch, host bridge, **JSON view-definition serializer**
  (`ViewDefinitionJsonSerializer.cs`), engine-isolation audit, Path-3 evidence harness,
  multi-WS text foundation. 90%+ of `lexical-edit-avalonia-migration/tasks.md` is `[x]`.

## C. Feasible-remaining

> **Progress (2026-06-15):** the cleanly-verifiable, framework-neutral portion of bucket C is now
> **DONE & verified** — the entire **9.2 override system** (differ + serializer + applier + `.fwlayout`
> migrate core + full op set incl. addNode/duplicateNode), the **9.4 loader**, and **2.2** (surface
> registry + census). What remains is **heavier integration that cannot be cleanly verified in the
> headless FwAvalonia test lane in-session** (cross-project xWorks builds, WinForms/realized-UI coupling,
> or central build-file changes) — listed below with the specific reason. Each is a focused effort, not
> a refusal: point me at one and I'll do it with proper verification.

| Item | Stage/task | Why not done in-session |
| --- | --- | --- |
| Unify the dual projector into one shared structural projector — **DONE & verified** (`RegionStructureProjector`; both mapper + composer route through it; 91/91 + 137/137) | 4.5 / task 18.11 | **Done** — header construction + indent rule unified; editor→kind already shared; product path behavior-preserved |
| XML→typed override migrator — **DONE & verified**: differ + JSON wire format + applier + full op set + `.fwlayout` file-I/O driver (override suite 28/28) **+ xWorks `Inventory` adapter** (`LexicalEditOverrideMigration`, core 2/2 + build-verified). Only the caller's choice of *pristine* shipped-layout source needs a real-project smoke test | 4.3 / task 9.2 | **Done** end-to-end (logic verified; one caller-side baseline-source smoke test remains) |
| Runtime-XML-disable for the gated surface — **DELIVERED & verified** (`ViewDefinitionLoader`, 4/4): gated surface prefers committed JSON, XML retained as audit/fallback with explicit diagnostics | 4.4 / task 9.4 | **Done** (framework-neutral core); only the XCore gate-source + JSON-file-location wrapper remains |
| End-to-end IME compose/cancel/commit wiring → **moved to bucket D (discovered verification blocker)** | 4.6 / task 18.10 | The `RegionImeCompositionState` machine is already unit-tested; *wiring it to replace the working native TextBox IME* needs real IME input on a realized desktop surface — see bucket D |
| Region/IR scaffolding generator + dialog MVVM kit → **moved to bucket D (documented architectural decision)** | 1.1 / 1.2 | `FwAvalonia.csproj` lines 14-16 deliberately keep the project XAML-free — see bucket D |
| **Host generalization (2.1) — DONE & verified** (`AvaloniaRegionHostControl`); surface registry + census (2.2) **DONE** | 2.1 / 2.2 | **Done** — reusable host base extracted, 134/134 |

## D. Blocked — needs a verification environment / decision not present in this session
| Item | Stage | Reason |
| --- | --- | --- |
| Editable virtualized **table** (3a/3b/3c) + custom AutomationPeers | 3 | **DONE & integrated (2026-06-16)** — built over a separate effort and consolidated onto this branch: `LexicalBrowseView` (sort/selection/keyboard/density, in-cell edit, checkbox, filter, multi-sort, bulk-edit) + `BrowseTableAutomationPeer`/`BrowseRowAutomationPeer`, product-wired via `ClerkBrowseRowSource`/`ClerkBrowseEditContext`/`LexicalBrowseHostControl`/`RecordBrowseView` behind `ResolveBrowse`. Remaining: desktop UIA2/FlaUI evidence (3.3), xWorks browse-class unit tests, the 10k real-DPI perf budget (2.7), and live FLEx verification. |
| 150% DPI parity (mixed-mode coexistence) | 4.2 / task 7.7 (manifest §5.4) | **DEFERRED by decision 2026-06-15** to post-100%-conversion. During coexistence, DPI issues are mitigated by the **WinForms fallback** (users revert to WinForms-only); full 150% DPI parity is validated once the app is fully Avalonia (no mixed-mode DPI surface to compare). Not a coexistence/Stage-4 gate. |
| scroll/expand/typing-latency budgets | 4.2 / task 7.7 (manifest §5.4) | Perf-measurement items needing a named-machine baseline; partial open-time budgets already measured. Not blocking the Stage-4 *enable-able* gate. |
| **18.10 IME custom composition wiring — WILL NOT BUILD (conscious decision 2026-06-15)** | 4.6 / task 18.10 | **Policy: "do not build unless there is no other way."** Text input rides the **stock Avalonia `TextBox`** (TSF/IBus) + **libpalaso per-WS keyboard activation incl. Keyman** — the platform + Keyman do the input-method work, so ordinary IME/Keyman typing already works without custom code. `RegionImeCompositionState` stays **forward foundation, consciously un-wired**. The historical custom IME (`VwTextStore`/IBus handler) existed only because the native Views surface was non-standard; a standard control offloads input to the OS + Keyman. Build explicit composition control **only if** the standard path is *demonstrated* insufficient for a specific scenario (verified on a real desktop with the relevant Keyman/IME). Not a gate; not a blocker — a deliberate non-goal. |
| **1.1/1.2 dialog MVVM / XAML kit** | 1.1 / 1.2 | **DECISION MADE 2026-06-15: adopt MVVM (Avalonia XAML + CommunityToolkit.Mvvm + compiled bindings) in a dedicated XAML-enabled project (`FwAvaloniaDialogs`), keeping the foundation pure-C#.** No longer decision-blocked. Remaining is implementation: the one-time MSBuild/XAML-compiler integration spike on net48 (Stage 1.2 first task) + the scaffolding generator — ready to build, not blocked. |
| Full `./build.ps1` + `./test.ps1` native+managed traversal | task 18.12 | Heavy full-graph run; partial targeted runs done this session. |

## Verdict (updated 2026-06-15 — blockers resolved via decisions)

The foundation/exemplar work of phases 1–4 is **substantially complete and verified**, and the former
blockers are now **cleared by decision or in progress** — none remain as hard "can't proceed" blockers:

- **XAML/dialog-MVVM** → **decided** (MVVM; dedicated `FwAvaloniaDialogs` project). Unblocked.
- **150% DPI** → **deferred by decision** to post-100%-conversion; WinForms fallback covers coexistence. Not a gate.
- **18.10 IME** → **conscious non-goal** ("do not build unless there is no other way"); standard TextBox + Keyman path. Not a gate.
- **9.2 Inventory baseline** → **reframed + built** (caller-provides-baseline); only a caller-side smoke test remains.
- **Stage 3 editable grid** → **landed & integrated (2026-06-16)** — the consolidated browse table is on this branch (see the close-out update at top and Bucket D). Remaining is verification/evidence, not the build.

**Remaining to call phases 1–4 done (ready work + verification, not blockers):**
1. **Dialog MVVM kit follow-ons** — the net48 XAML-compiler/MSBuild spike + `FwAvaloniaDialogs` + first dialog are **done & verified** (dialog suite 9/9). Remaining: the host-modal WinForms wrapper for coexistence, the localization lane, and the dialog scaffolding generator (Stage 1.2 acceptance; gates Stage 5).
2. **Stage 4 manifest finalization** — flip region-manifest §6 rows once Stage 3 lands the entry tables (DPI lane excluded by decision §11.5).
3. **Full `./build.ps1` + `./test.ps1` traversal** (18.12) as the final 1–4 verification (this session ran targeted suites, all green per-component).
4. Minor setup: dual-run CI matrix, modernized-Fluent theme baseline, the 9.2 caller-side pristine-baseline smoke test.

## Close-out review follow-ups (2026-06-16)

Findings from the multi-layer close-out review, with what was done and what is deliberately deferred.

**Done this review:** reparented `LexicalBrowseHostControl` onto `AvaloniaRegionHostControl` (removed
duplicate Avalonia bootstrap; restored directional-key interop — fixes a latent arrow-key-swallow bug
when the table has focus); unified the surface-resolve precedence (`ResolveFromPreference`, shared by
`Resolve`/`ResolveBrowse`); browse gate now covers `lexiconEdit` + `lexiconBrowse`; row density pinned
to `FwAvaloniaDensity` tokens on the integrated view; added `ClerkBrowseEditContextTests` (row-switch =
separate undo steps; non-LexEntry/invalid = no-op); renamed the `SenseTreeSpike` fixture; reconciled the
stale ledger/epic claims + test counts above.

**Tracked follow-ups (not close-out blockers):**
- **xWorks browse test coverage** — `ClerkBrowseEditContext` is now unit-tested. `ClerkBrowseRowSource`
  (filter index-map / `HvoAt` remap / sort-clears-filter) and the `RecordBrowseView` selection-mirror
  echo-guard are **build-verified only**: both are coupled to a live `BrowseViewer`/clerk, so they are
  desktop/integration-lane tests, not headless. Add when the desktop UIA2/FlaUI lane (3.3) is set up.
- **M3 — `IRegionEditContext` is monolithic** (text/rich/option/reference/validate in one interface);
  the browse contexts stub the reference verbs. Consider splitting into capability interfaces (mirroring
  the browse `IBrowseSortSource`/`IBrowseFilterSource` pattern) **before Stage 5** adds chooser/dialog
  edit surfaces. Watch item, not a defect.
- **M4 — `FullEntryRegionComposer` (~2,500 lines)** routes shared rules through `RegionStructureProjector`
  + `EditorKindMap` but keeps all per-field-kind walkers in one class. Extract a per-kind dispatch table
  before adding the next object type in Stage 5+. Watch item.
- **Filter perf** — `ClerkBrowseRowSource.RebuildFilter` scans the whole clerk list (per-cell finder
  eval) per keystroke; fine for typical lists, but fold into the 10k real-DPI scroll/filter budget (2.7).
