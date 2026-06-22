# Stage 8 Review — Notebook, Lists, Dictionary-config UI, Remaining Tools

> Reviewer pass against the live branch `010-advanced-entry-view-phase-1-2`.
> Master plan: `openspec/changes/avalonia-migration-roadmap/complete-migration-program.md` §6 Stage 8
> (lines 285–288), §4 table (line 130), §5 graph (lines 159, 180–182). Repo state read from
> `Src/xWorks/` (DictionaryConfiguration* family, DictionaryDetailsView/, RecordEditView/BrowseView/DocView),
> `Src/Common/Controls/XMLViews/BulkEditBar.cs`, `Src/FwCoreDlgs/IUtility.cs`+`UtilityDlg.cs`,
> `DistFiles/Language Explorer/Configuration/{Notebook,Lists}/`, `UtilityCatalogInclude.xml`,
> and the lexical-edit audit set (`native-views-audit.md` §8.6, `coverage-map.md`, `region-manifest.md`).

## 1. Scope assessment

**Verdict: this is a grab-bag, and it should be split.** Stage 8 as written (plan §6, lines 285–288)
bundles four things that have almost nothing in common architecturally, pattern-wise, dependency-wise,
or in lead level:

1. **Notebook + Lists + bulk-edit** — pure region/composer + shared-grid surfaces. These reuse the
   exact Stage 4 detail pattern and the Stage 3 browse/grid control. They are the *natural* mid-level
   Track-II follow-on to Stage 6.
2. **Dictionary-configuration UI** (the `DictionaryConfigurationDlg` family + `DictionaryDetailsView/`) —
   a hand-authored WinForms **dialog tree**, not an XML-view-driven surface. Per decision §11.3 these
   must use **CommunityToolkit.Mvvm + compiled bindings, NOT the region/composer pattern**. This is
   Stage 5 idiom, not Stage 6/8 idiom.
3. **Dictionary config *preview* wiring** — the live preview pane is a **Gecko/XULRunner browser**
   (`DictionaryConfigurationDlg.cs:10` `using Gecko;`, hard-fails at `:45` if `m_preview.NativeBrowser`
   is not a `GeckoWebBrowser`, and does DOM highlighting over `GeckoElement` at `:187`/`:208`/`:228`).
   Replacing this is **Stage 10's** entire charter (plan §6 line 307; §10 maps it to Stage 10).
4. **"Remaining utilities/tools + sweep for stragglers"** — an open-ended census/long-tail bucket
   spanning ~23 registered `IUtility` implementations (`UtilityCatalogInclude.xml`) and the whole
   non-migrated WinForms surface population.

These four have different leads (mid for 1, junior for 2 dialogs, senior for 3 preview, mixed for 4),
different patterns (region vs MVVM vs browser-replacement), and different dependencies (Stage 3/4 vs
Stage 5 vs Stage 9/10). Folding them into one "mid" epic violates the plan's own §8 staffing model and
the decision-§11.3 pattern split. **Recommend splitting Stage 8 into 8a (Notebook/Lists/bulk-edit,
mid, region pattern) and 8b (Dictionary-config dialogs, junior/mid, MVVM pattern, preview deferred to
Stage 10)**, and **hoisting the straggler-census out of Stage 8 entirely** (see §5).

## 2. Feasibility (repo-grounded)

### Notebook / Lists / bulk-edit — high reuse, genuinely a Stage-4/Stage-6 clone

- **Notebook** is three tools (`DistFiles/Language Explorer/Configuration/Notebook/{Edit,Browse,Document}/
  toolConfiguration.xml`) over the `RnGenericRec` model, built on the *exact* generic hosts Stage 4 owns:
  `notebookEdit` = `RecordBrowseView` (left) + `RecordEditView` (right, DataTree detail);
  `notebookBrowse` = `RecordBrowseView`; `notebookDocument` = `XmlDocView`. There are **no
  Notebook-specific custom slice classes** in the search — it rides the shared DetailControls stack.
  `native-views-audit.md` §8.6 line 237 already routes `notebookEdit` as **explicit legacy fallback**
  through `RecordEditView` (covered by `RecordEditViewSwitchTests`, task 2.11). So the detail half is a
  near-mechanical Stage-4-pattern application; the browse half rides Stage 3; the **document half is the
  catch** (see Stage 9/10 interaction below).
- **Lists** is ~25–29 possibility-list editor tools (`Lists/Edit/toolConfiguration.xml`, etc.), *all*
  `RecordEditView` detail over `CmPossibility`/subclasses, several with hierarchical tree-bar handlers
  (`PossibilityTreeBarHandler`, `SemanticDomainRdeTreeBarHandler`). `native-views-audit.md` §8.6 line 238
  already routes `posEdit`/`domainTypeEdit` as explicit legacy fallback. Feasibility is high **because the
  detail surface is the same composer** — but the **tree-bar / record-list sidebar** (hierarchical
  possibility navigation) is a real sub-surface that the lexical-edit exemplar did *not* exercise, and
  the unbounded-tree case maps to `architecture-patterns.md` §4's "owned flattened virtualized list with
  expander/indent" — i.e. it **depends on Stage 3** for the tree control. Custom-list create/delete
  dialogs (`Src/xWorks/CustomListDlg.cs`, `DeleteCustomList.cs`) are Stage-5-style dialogs.
- **Bulk edit** is the real engineering item in this group. `Src/Common/Controls/XMLViews/BulkEditBar.cs`
  is a large 6-tab WinForms surface (List Choice, Bulk Copy, Click Copy, Process/Transduce, Find/Replace,
  Other) bolted onto `BrowseViewer` when `bulkEdit="true"` (e.g. `Words/BulkEdit/toolConfiguration.xml`,
  `Lexicon/ReversalEntriesBulkEdit/toolConfiguration.xml`). It is **not** a detail surface and **not** a
  simple browse; it is editable-grid-plus-operation-UI and is **squarely Stage-3-dependent** (the shared
  editable virtualized grid). It also has custom column editors (`BulkReversalEntryPosEditor` in
  `Src/LexText/Lexicon/LexEdDll/ReversalEntryBulkEdit.cs`) that need the plugin-registry treatment.
  Bulk-edit is the highest-risk item in 8a and should be sized as its own issue, gated on Stage 3.

### Dictionary-configuration dialogs — MVVM, sizeable, mechanically tractable

- The family is **~43 .cs files** in `Src/xWorks/`: the main `DictionaryConfigurationDlg.cs` (+Designer),
  `DictionaryConfigurationTreeControl.cs` (WinForms `UserControl`), the **15-file `DictionaryDetailsView/`
  folder** (`DetailsView`, `ListOptionsView`, `SenseOptionsView`, `GroupingOptionsView`,
  `PictureOptionsView`, `ButtonOverPanel`, `LabelOverPanel`, each `.cs`+`.Designer.cs`), plus the
  Manager/Import/Rename dialogs (`DictionaryConfigurationManagerDlg`, `DictionaryConfigurationImportDlg`,
  `DictionaryConfigurationNodeRenameDlg`). Controllers (`DictionaryConfigurationController`,
  `DictionaryDetailsController`, the `IDictionary*View` interfaces) are **already MVP-style** — view
  interfaces + controllers — which makes the MVVM port unusually clean: the controllers largely become
  view-models. This is the most MVVM-ready dialog cluster in the repo and is a **good junior+Claude
  reservoir** under the Stage-5 idiom, *not* a region-composer job.
- `DictionaryConfigurationMigrators/` (4 files: `IDictionaryConfigurationMigrator`, `PreHistoricMigrator`,
  `FirstAlphaMigrator`, `FirstBetaMigrator`) and `DictionaryConfigurationMigrator.cs` are **pure
  data/format logic, no UI** — they migrate `.fwdictconfig`/`ConfigurableDictionaryNode` trees and are
  framework-agnostic. They carry over unchanged; do not put them in any UI epic.

### Preview + XHTML generation — this is Stage 10, not Stage 8

- The preview pipeline is Gecko all the way down: `XhtmlDocView.cs` and `GeneratedHtmlViewer.cs` host
  `GeckoWebBrowser` (via `Src/XCore/HtmlControl.cs`); XHTML/CSS are generated by
  `ConfiguredLcmGenerator.cs`, `LcmXhtmlGenerator.cs`, `CssGenerator.cs`; PDF export references
  `GeckofxHtmlToPdf`. `GeckoWebBrowser`/`GeckofxHtmlToPdf` are on the **forbidden-symbol list**
  (`parity-evidence.md` §4) and removing Gecko is **Stage 10's defining deliverable**. Stage 8 *cannot*
  ship the dictionary-config dialog "at parity" while its central preview pane is a forbidden Gecko
  browser — either the dialog ships with preview still hosted in a coexisting WinForms/Gecko island
  (acceptable during coexistence, but then it is *not* a clean Avalonia surface), or it blocks on
  Stage 10. **This is the single biggest cross-stage conflict in the stage.**

## 3. Best practices

1. **Honor decision §11.3 explicitly in the epic text.** Dictionary-config dialogs are hand-authored UI
   with no XML view-definition to compile — forcing them through the IR/composer is the exact misfit the
   decision warns against. Write 8b as "MVVM + compiled bindings, reuse `FwMultiWsTextField`/`FwOptionPicker`
   inside the dialog where WS-aware fields appear," matching Stage 5.
2. **Reuse the existing MVP seam.** The `IDictionary*View` + `*Controller` split is already a clean
   logic/UI boundary. The runbook for 8b should say "controller → view-model, view interface → compiled
   bindings," which is lower-risk than greenfield MVVM and a good teaching example for juniors.
3. **Census custom slices and bulk-edit column editors first** (`migration-checklist.md` Phase 1). Lists
   tree-bar handlers and bulk-edit custom editors (`BulkReversalEntryPosEditor`) must be registered in
   `RegionEditorPlugins.cs` with burn-down tests before the surfaces claim parity (`architecture-patterns.md`
   §5). Notebook itself appears plugin-free, which is a point in its favor.
4. **Keep the four parity lanes honest per surface.** Each Notebook/Lists tool and each dialog needs its
   own Path-3 bundle (`parity-evidence.md` §1); do not let "Notebook ≈ Lexicon detail" justify skipping
   semantic/visual snapshots — the WS-heavy possibility lists and the document view have their own
   typography/density behavior.
5. **Preview parity is a Stage-10 lane, not a Stage-8 checkbox.** Print/preview fidelity, page paging
   (`LcmXhtmlGenerator` EntriesPerPage), and DOM highlight-on-select are Stage-10 deliverables; 8b's exit
   gate must not assert preview parity.

## 4. Interactions & dependencies

- **Stage 3 (shared editable grid/tree) — hard blocker for the heaviest parts.** Bulk-edit *is* an
  editable grid; the Lists hierarchical record-list/tree-bar is an unbounded tree. Both are exactly the
  control Stage 3 owns. Plan §4 already lists Stage 8 `Depends on 4,5` but **omits Stage 3** — that is a
  dependency-graph gap (see §5). Notebook/Lists *detail* and the dictionary dialogs can proceed without
  Stage 3; bulk-edit and the Lists tree-bar cannot.
- **Stage 4 (exemplar) — the detail half clones it.** Notebook Edit and the 25+ Lists editors are the
  cleanest possible reuse of the Stage-4 composer/region pattern; they are the validation that the
  exemplar generalizes. They also inherit Stage 4's open exemplar-debt (the dual-projector unification,
  18.11) — if that is not resolved, Notebook/Lists clone a 2524-line composer.
- **Stage 5 (dialogs) — the dictionary-config dialogs *are* Stage-5-shaped work.** Plan §4 says Stage 8
  `Depends on 5`, but the dependency is really *pattern reuse*, not sequencing: 8b should be staffed and
  run as additional Stage-5 streams. Custom-list/Import/Rename dialogs likewise.
- **Stage 9 (managed document engine) — the document/preview surfaces depend on it, and the plan does not
  say so.** `notebookDocument` (`XmlDocView`) and `XhtmlRecordDocView`/`RecordDocView` are
  document-rendering surfaces. Multi-paragraph/structured document rendering is Stage 9's charter
  (plan §6 line 292). Plan §4 omits the Stage-9 dependency for Stage 8's document views.
- **Stage 10 (Gecko/Graphite removal + dictionary-preview) — direct overlap.** Plan §10 line 307
  explicitly names "dictionary-preview replacement" as Stage 10. So the *preview* half of Stage 8 line 287
  ("config preview wiring") **belongs to Stage 10**, while the *configuration dialog* half stays in
  Stage 8. The plan currently double-books the preview across Stage 8 and Stage 10. This must be
  disambiguated or the two epics collide.

## 5. Recommended plan changes

1. **Split Stage 8 into 8a and 8b.** 8a = Notebook + Lists + bulk-edit (mid, region/composer + Stage-3
   grid/tree). 8b = Dictionary-configuration dialogs (junior/mid, MVVM + compiled bindings, Stage-5 idiom,
   reusing the existing MVP controllers). This matches §8 staffing and decision §11.3.
2. **Move "config preview wiring" out of Stage 8 into Stage 10.** Restate Stage 8 line 287 as
   "Dictionary-configuration *dialogs* (`DictionaryConfigurationDlg` family); the Gecko preview pane and
   its XHTML/CSS/PDF pipeline are migrated by Stage 10." Add an explicit cross-link so 8b's exit gate
   excludes preview parity and instead permits the preview to remain a coexisting island until Stage 10.
3. **Add Stage 3 and Stage 9 to Stage 8's dependency row** (plan §4 line 130 currently says only `4,5`).
   Bulk-edit + Lists tree-bar block on Stage 3; the document/preview views block on Stage 9/10. Update the
   §5 mermaid graph edges accordingly (`S3 --> S8`, `S9 --> S8` for the document/preview slice).
4. **Hoist the "sweep for stragglers via the surface registry" out of Stage 8 into Stage 1 (or a
   standalone census issue).** Today there is **no app-wide surface registry** — only the
   *lexical-edit-scoped* assets (`LexicalEditSurfaceSelectionService.cs`, and the audit docs
   `native-views-audit.md` §8.6 / `coverage-map.md` / `view-inventory.md` / `region-manifest.md`, all
   scoped to the entry surface). Plan §6 Stage 2 already charters "generalize → an app-wide surface
   registry/switch." The *census that feeds that registry* (the ~200+ Form/UserControl population, the 23
   `IUtility` entries, the dialog inventory) is a foundation/Stage-1 deliverable, not a tail task buried in
   a mid-level surface epic. Stage 8 should *consume* the registry to claim "remaining areas done," not be
   the place the census first happens. Recommend: a single living "surface census" artifact under
   `openspec/changes/avalonia-migration-roadmap/` (sibling to the reviews), owned by Stage 1/2, that every
   surface stage burns down against.
5. **Reclassify the migrators and XHTML generators as non-UI.** `DictionaryConfigurationMigrators/` and
   `DictionaryConfigurationMigrator.cs` are framework-agnostic data logic — carry over unchanged, not in a
   UI epic. The XHTML/CSS generators (`ConfiguredLcmGenerator`, `LcmXhtmlGenerator`, `CssGenerator`) are
   the *content* side of the preview and move with Stage 10's preview replacement, not with the dialog.
6. **Size bulk-edit as its own gated issue.** It is the only true engineering item in 8a (editable grid +
   6 operation tabs + custom column editors), explicitly blocked on Stage 3 and the plugin registry.

## 6. Open questions / risks

- **Preview double-booking (Stage 8 vs Stage 10) is unresolved in the plan.** Until §6 line 287 and
  line 307 are reconciled, two epics claim the dictionary preview. Highest-impact ambiguity here.
- **Can the dictionary-config dialog ship "at parity" with a coexisting Gecko preview island?** During
  coexistence the rule is WinForms owns modality (`architecture-patterns.md` §7); a Gecko preview hosted
  beside an Avalonia dialog body may be acceptable transitionally, but then the surface is *not* a clean
  Avalonia surface and `EngineIsolationAuditTests` would flag `GeckoWebBrowser` if it leaks into the
  migrated assembly. Where exactly does the boundary sit? This needs an explicit decision.
- **No app-wide surface registry exists yet** — "sweep for stragglers via the surface registry" presumes
  an asset that is still lexical-edit-scoped. If Stage 2 has not produced it by the time Stage 8 runs, the
  straggler sweep has nothing to sweep against and silently becomes an unbounded discovery task.
- **Lists tree-bar / hierarchical possibility navigation is unexercised by the exemplar.** Semantic-domain
  and other hierarchical lists need the Stage-3 unbounded-tree control plus the `*TreeBarHandler` behavior;
  the lexical-edit slice did not prove this path. Medium risk it is under-sized.
- **Bulk-edit "Process/Transduce" and "Find/Replace" tabs embed regex/transform UI** that may have its own
  hidden dialog/chooser dependencies; census needed before sizing.
- **Notebook Document and `XhtmlRecordDocView` are document surfaces** that quietly depend on Stage 9; if
  Stage 8 is read as "Notebook done," it inherits Stage 9 scope it cannot satisfy.

## 7. Confidence

**High** on the architectural split argument and the Gecko-preview finding — these are confirmed from
source: `DictionaryConfigurationDlg.cs` hard-requires `GeckoWebBrowser` (lines 10/45/187/208/228), the
tool configurations name the exact generic hosts, `BulkEditBar.cs` is a confirmed `BrowseViewer`-bolted
surface, and `native-views-audit.md` §8.6 already routes Notebook/Lists as explicit legacy fallback.
**High** that no app-wide surface registry exists yet (only the lexical-edit-scoped service + audit docs).
**Medium** on bulk-edit and Lists-tree-bar sizing — I read these from tool configs, file structure, and
the audit, not a line-by-line read of `BulkEditBar.cs` internals or the tree-bar handlers. **Medium** on
the precise §11.3-vs-Gecko boundary for the dialog (whether a coexisting preview island passes the
isolation audit) — that needs a deliberate program decision, not just a repo read.
