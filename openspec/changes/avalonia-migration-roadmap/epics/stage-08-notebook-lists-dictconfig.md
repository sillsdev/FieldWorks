# Stage 8 — Notebook, Lists, Dictionary-config UI, remaining tools (Epic draft)

> **JIRA-ready draft.** Created from `complete-migration-program.md` §6 Stage 8 (lines 394–406),
> §7 Definition of Done, §10 JIRA structure; `reviews/stage-08-notebook-lists-dictconfig.md`; and
> `reviews/00-cross-comparison-synthesis.md` §3 (preview double-booking), §6 (sub-epic map row 8),
> §7 (dependency edges `S3 → S8`, `S9 → S8`). Planning only — no code/behavior change.
>
> **Headline from review:** Stage 8 as written is a *grab-bag*. It splits into **8a** (Notebook/Lists/
> bulk-edit — region/composer + shared grid, mid) and **8b** (dictionary-config dialogs — MVVM, Stage-5
> idiom). "Config preview wiring" is **moved out** to Stage 10 (it is Gecko). The "straggler sweep"
> depends on the **living surface-census artifact owned by Stage 2** — Stage 8 *consumes* it, it does
> not create it.

---

## Epic

**Summary:** Migrate the Notebook area, Lists/possibility-list editors, the cross-surface bulk-edit
bar, and the Dictionary-configuration dialog family to Avalonia at parity, and reconcile the remaining
WinForms surface stragglers against the surface census.

**Type:** Epic (Track II — Surfaces). Decomposes into sub-epics **8a**, **8b**, and a
**straggler-reconciliation** story; do not implement as a single mid-level epic.

**Labels:** `track-surfaces`, `lead-mid` (8a) / `lead-junior` + `lead-mid` (8b), `parallel-safe`,
`parity-blocked-by:stage-3-editable-grid` (bulk-edit, Lists tree-bar),
`parity-blocked-by:stage-9-document-engine` (Notebook Document / XhtmlRecordDocView),
`parity-blocked-by:stage-10-preview` (dictionary preview island).

**Description.**
This epic finishes the long tail of Track-II surfaces that follow the Stage-4 exemplar but were not
themselves the exemplar. It contains two architecturally distinct bodies of work plus a reconciliation
task:

- **8a — Notebook / Lists / bulk-edit (region/composer + shared grid).** Notebook is three tools
  (`Notebook/{Edit,Browse,Document}`) over `RnGenericRec`, riding the shared `RecordEditView` /
  `RecordBrowseView` / `XmlDocView` hosts — no Notebook-specific custom slices exist, so the detail
  half is a near-mechanical Stage-4-pattern clone. Lists is ~25–29 possibility-list editors over
  `CmPossibility` subclasses, all `RecordEditView` detail, several with hierarchical tree-bar handlers
  (`PossibilityTreeBarHandler`, `SemanticDomainRdeTreeBarHandler`) — the **hierarchical tree-bar is a
  sub-surface the exemplar never exercised** and depends on the Stage-3 owned virtualized tree.
  **`BulkEditBar` is the real engineering item** — a 6-tab editable-grid-plus-operation surface bolted
  onto `BrowseViewer` (`bulkEdit="true"`), with custom column editors (`BulkReversalEntryPosEditor`)
  that need plugin-registry treatment. It is squarely Stage-3-gated.
- **8b — Dictionary-configuration dialogs (MVVM + compiled bindings).** The `DictionaryConfigurationDlg`
  family (~43 `.cs` files: main dialog, `DictionaryConfigurationTreeControl`, the 15-file
  `DictionaryDetailsView/` folder, Manager/Import/Rename dialogs). Per decision §11.3 these are
  hand-authored UI with **no XML view-definition to compile**, so they use **CommunityToolkit.Mvvm +
  compiled bindings (the Stage-5 idiom), NOT the region/composer pattern**. They are unusually
  MVVM-ready: the existing `IDictionary*View` + `*Controller` MVP split means the controllers largely
  *become* view-models — a good junior+Claude reservoir.
- **Straggler reconciliation.** Burn the remaining non-migrated WinForms surface population
  (~23 registered `IUtility` implementations + residual Forms/UserControls) down against the
  Stage-2-owned **surface census**.

**Out of scope (moved elsewhere):**
- **Dictionary config *preview* wiring → Stage 10.** The preview pane hard-requires `GeckoWebBrowser`
  (`DictionaryConfigurationDlg.cs` lines 10/45/187/208/228); Gecko replacement is Stage 10's defining
  deliverable. Synthesis §3 resolution: preview *rendering replacement* = Stage 10; 8b *consumes* the
  replaced preview.
- **XHTML/CSS/PDF generators reclassified as non-UI.** `ConfiguredLcmGenerator`, `LcmXhtmlGenerator`,
  `CssGenerator` are framework-agnostic content generation; they move with Stage 10's preview work.
- **Dictionary-config *migrators* reclassified as non-UI.** `DictionaryConfigurationMigrators/` (4 files)
  + `DictionaryConfigurationMigrator.cs` are pure `.fwdictconfig` data/format logic — carry over
  unchanged, not in any UI epic.

**Acceptance criteria.**
1. 8a, 8b, and the straggler-reconciliation story are all complete with their own acceptance criteria met.
2. Every migrated surface satisfies the per-surface **Definition of Done (§7)** — its own Path-3 parity
   bundle (semantic + visual + workflow + performance), captured before refactor and matched after,
   at **100% + 150% DPI**; `EngineIsolationAuditTests` + active-host contract tests green;
   `./build.ps1` + `./test.ps1` green; retrospective folded into the skill set in the same PR.
3. No surface claims parity by reasoning "≈ Lexicon detail" — WS-heavy possibility lists and document
   views carry distinct typography/density behavior and need their own snapshots.
4. The dictionary preview is **not** asserted at parity in this epic (it is a Stage-10 lane); any
   coexisting Gecko preview island is explicitly fenced (see Notes).
5. Surface census shows zero unreconciled Stage-8-owned stragglers (each is migrated, deferred to a
   named stage, or marked explicit "unsupported" — never silent fallback).

**Dependencies.** Stage 4 (exemplar — the detail half clones it), Stage 5 (dialog idiom + tooling that
8b reuses), **Stage 3** (editable grid for bulk-edit + tree control for Lists tree-bar),
**Stage 9** (managed document engine for Notebook Document / `XhtmlRecordDocView`),
Stage 10 (preview replacement that 8b consumes), Stage 2 (surface census the straggler sweep burns down).
*Plan §4 row currently lists only `4,5` — Stage 3 and Stage 9 are added per review §4/§5 and synthesis §7.*

**Rough size.** Large (mid-level epic split across two sub-epics + a reconciliation tail). 8a is the
larger and higher-risk body (bulk-edit + ~25–29 Lists editors + Notebook); 8b is sizeable but
mechanically tractable (~43 files, MVP-assisted). Bulk-edit alone is its own gated issue.

---

## Sub-epics / stories

### 8a — Notebook / Lists / bulk-edit  *(region/composer + shared grid; mid)*

**Summary.** Migrate the Notebook area, the Lists/possibility-list editors, and the cross-surface
bulk-edit bar to Avalonia using the Stage-4 region/composer pattern for detail and the Stage-3 shared
editable virtualized grid/tree for browse, bulk-edit, and hierarchical lists.

**Type.** Sub-epic (Track II). `lead-mid`.

**Description.**
- **Notebook (3 tools).** `notebookEdit` (`RecordBrowseView` + `RecordEditView` detail), `notebookBrowse`
  (`RecordBrowseView`), `notebookDocument` (`XmlDocView`) over `RnGenericRec`. No Notebook-specific
  custom slices — rides the shared DetailControls stack. `native-views-audit.md` §8.6 already routes
  `notebookEdit` as explicit legacy fallback through `RecordEditView`. The detail half is a near-mechanical
  Stage-4 clone; the browse half rides Stage 3; **the document half (`notebookDocument`) depends on
  Stage 9** and must not be claimed done before Stage 9 covers it.
- **Lists (~25–29 editors).** All `RecordEditView` detail over `CmPossibility` subclasses. The
  **hierarchical tree-bar / record-list sidebar** (`PossibilityTreeBarHandler`,
  `SemanticDomainRdeTreeBarHandler`) is an unbounded-tree sub-surface the exemplar never exercised —
  maps to `architecture-patterns.md` §4 "owned flattened virtualized list with expander/indent" and
  **depends on Stage 3**. Custom-list create/delete dialogs (`CustomListDlg.cs`, `DeleteCustomList.cs`)
  are Stage-5-style dialogs.
- **Bulk edit (the real engineering item).** `Src/Common/Controls/XMLViews/BulkEditBar.cs` — 6 tabs
  (List Choice, Bulk Copy, Click Copy, Process/Transduce, Find/Replace, Other) bolted onto `BrowseViewer`.
  Editable grid + operation UI → **Stage-3-gated**. Custom column editors (`BulkReversalEntryPosEditor`)
  → plugin-registry with burn-down tests. **Size as its own gated issue.**

**Acceptance criteria (ref §7 DoD).**
- Per-surface (each Notebook tool, each Lists editor, the bulk-edit bar): custom-slice/column-editor
  census taken with plugins registered or explicit "unsupported" rows (DoD #1); seams reused from
  `ISeams.cs` (DoD #3); composer walks compiled IR with stable AutomationIds from StableId (DoD #5);
  explicit `HostUiBehavior` per host, no hidden DataTree/Views (DoD #6); Path-3 bundle per surface,
  perf ≤ legacy × 1.2, **100% + 150% DPI** (DoD #7); localization lanes correct (DoD #8);
  `EngineIsolationAuditTests` + active-host contract + `build`/`test` green (DoD #9); retrospective in
  same PR (DoD #10).
- Bulk-edit gated on Stage 3 (editable cells + bulk/checkbox/filter, i.e. 3b + 3c) and on plugin-registry
  custom column editors.
- Lists hierarchical tree-bar gated on Stage 3 owned tree control.
- `notebookDocument` / document views deferred until Stage 9 covers them — not claimed done in 8a.

**Dependencies.** Stage 4 (detail pattern + open exemplar-debt: dual-projector unification 18.11),
**Stage 3** (3b editable cells, 3c bulk/checkbox/filter, owned tree), **Stage 9** (`notebookDocument`),
Stage 5 (custom-list/create/delete dialog bodies).

**Labels.** `track-surfaces`, `lead-mid`, `parallel-safe`,
`parity-blocked-by:stage-3-editable-grid`, `parity-blocked-by:stage-9-document-engine`.

**Rough size.** Large. Notebook (small, near-mechanical) + Lists (medium, ~25–29 editors + tree-bar) +
bulk-edit (large, own gated issue). Bulk-edit is the highest-risk item in the epic.

---

### 8b — Dictionary-configuration dialogs  *(MVVM + compiled bindings; junior/mid)*

**Summary.** Migrate the `DictionaryConfigurationDlg` dialog family to Avalonia using CommunityToolkit.Mvvm
+ compiled bindings (Stage-5 idiom), reusing the existing `IDictionary*View`/`*Controller` MVP split,
with the preview pane left as a coexisting Stage-10 lane.

**Type.** Sub-epic (Track II / Stage-5-pattern). `lead-junior` + `lead-mid`.

**Description.**
- **~43 `.cs` files in `Src/xWorks/`:** `DictionaryConfigurationDlg.cs` (+Designer),
  `DictionaryConfigurationTreeControl.cs`, the 15-file `DictionaryDetailsView/` folder
  (`DetailsView`, `ListOptionsView`, `SenseOptionsView`, `GroupingOptionsView`, `PictureOptionsView`,
  `ButtonOverPanel`, `LabelOverPanel`, each `.cs` + `.Designer.cs`), and the Manager/Import/Rename
  dialogs (`DictionaryConfigurationManagerDlg`, `DictionaryConfigurationImportDlg`,
  `DictionaryConfigurationNodeRenameDlg`).
- **Per decision §11.3:** hand-authored UI, no XML layout to compile → **MVVM + compiled bindings, NOT
  region/composer.** Reuse the owned WS-aware field controls (`FwMultiWsTextField`, `FwOptionPicker`)
  *inside* the dialogs wherever WS-aware text/chooser fields appear.
- **Reuse the existing MVP seam:** the runbook is "`*Controller` → view-model, `IDictionary*View` →
  compiled bindings" — lower-risk than greenfield MVVM and a good junior teaching example.
- **Tooling comes from Stage 1's MVVM-dialog kit** (CommunityToolkit.Mvvm, compiled bindings, dialog
  scaffolding); code-behind exception allowed so proven owned controls embed without rewrite.
- **Preview is NOT in scope** — the preview pane is Gecko (Stage 10). 8b's exit gate excludes preview
  parity; the preview may remain a coexisting WinForms/Gecko island until Stage 10.

**Acceptance criteria (ref §7 DoD).**
- Each dialog: parity bundle (semantic/visual/workflow/performance) captured and matched at 100% + 150%
  DPI (DoD #7, #2); AutomationIds nonlocalized, Names localized (DoD #8); localization review;
  `build`/`test` green (DoD #9); retrospective in same PR (DoD #10).
- No `new Window().ShowDialog()` — modality stays WinForms-owned via host-wrapped body until Stage 11
  (the Stage-5 host-wrapped-body rule).
- **Preview parity explicitly excluded** from the exit gate; the Gecko preview island is fenced so that
  `EngineIsolationAuditTests` does not flag `GeckoWebBrowser` inside the migrated assembly (see Notes).
- Migrators (`DictionaryConfigurationMigrators/`) and XHTML/CSS generators are *not* part of this
  sub-epic (reclassified non-UI / Stage 10 respectively).

**Dependencies.** Stage 1 (MVVM-dialog kit + scaffolding), Stage 5 (idiom + Tier-A/B streams; staffed as
additional Stage-5 streams), Stage 10 (preview replacement that 8b later consumes), Stage 2 (host-wrapped
modality contract).

**Labels.** `track-surfaces`, `lead-junior`, `lead-mid`, `parallel-safe`,
`parity-blocked-by:stage-10-preview`.

**Rough size.** Medium-large (~43 files), mechanically tractable thanks to the MVP split. Suitable as a
multi-stream junior reservoir under mid supervision.

---

### 8c — Straggler reconciliation against the surface census  *(mid)*

**Summary.** Reconcile the remaining non-migrated WinForms surface population owned by Stage 8 —
~23 registered `IUtility` implementations (`UtilityCatalogInclude.xml`) plus residual Forms/UserControls —
against the Stage-2-owned living surface-census artifact, ensuring none are silently missed.

**Type.** Story (Track II). `lead-mid`.

**Description.**
- **This story consumes, it does not create, the surface census.** Per synthesis §3 and the Stage-2
  post-review, the app-wide surface registry **and** the living surface-census artifact are **Stage 2
  deliverables** — today only the *lexical-edit-scoped* assets exist
  (`LexicalEditSurfaceSelectionService.cs`; `native-views-audit.md` §8.6 / `coverage-map.md` /
  `view-inventory.md` / `region-manifest.md`). Stage 8 burns its slice of that census down.
- **Risk:** if Stage 2 has not produced the census by the time Stage 8 runs, "sweep for stragglers"
  has nothing to sweep against and silently becomes an unbounded discovery task. This story therefore
  **hard-depends on the Stage-2 census existing.**
- Each straggler resolves to exactly one of: migrated here (8a/8b idiom as appropriate), deferred to a
  named owning stage (e.g. document/Views-coupled → Stage 9; preview/browser → Stage 10), or rendered as
  an explicit "unsupported" row — **never silent legacy fallback** (DoD #1).
- Bulk-edit "Process/Transduce" and "Find/Replace" tabs may embed regex/transform UI with hidden
  dialog/chooser dependencies — census these before sizing.

**Acceptance criteria (ref §7 DoD).**
- Surface census shows zero unreconciled Stage-8-owned surfaces; each carries an explicit disposition.
- No silent fallback (DoD #1, DoD #6 — explicit `HostUiBehavior`, full wiring path traced).
- Any surface migrated here meets the full §7 DoD; any deferred surface links to its owning stage/epic.

**Dependencies.** **Stage 2 (surface census — hard prerequisite)**, plus 8a/8b for the idioms applied to
any in-scope stragglers.

**Labels.** `track-surfaces`, `lead-mid`, `parity-blocked-by:stage-2-surface-census`.

**Rough size.** Small-to-medium, but **unbounded if the Stage-2 census is missing** — gate on it.

---

## Notes / open questions

- **Preview double-booking — RESOLVED (synthesis §3).** Stage 8 line 287 ("config preview wiring") and
  Stage 10 line 307 ("dictionary-preview replacement") previously both claimed the preview. Resolution:
  preview *rendering replacement* is **Stage 10 only**; Stage 8 (8b) **consumes** the replaced preview.
  "Config preview wiring" is removed from Stage 8 scope. The XHTML/CSS/PDF generators
  (`ConfiguredLcmGenerator`, `LcmXhtmlGenerator`, `CssGenerator`) move with Stage 10 and are reclassified
  non-UI; the `.fwdictconfig` migrators are reclassified non-UI and carry over unchanged.

- **Open decision — can 8b ship "at parity" with a coexisting Gecko preview island?** The dictionary
  dialog's central preview pane is a `GeckoWebBrowser`, on the forbidden-symbol list
  (`parity-evidence.md` §4). Two readings:
  - *(island)* ship the dialog body in Avalonia while the preview remains a coexisting WinForms/Gecko
    island until Stage 10. Then the surface is **not** a clean Avalonia surface, and the boundary must be
    drawn so the Gecko symbol does **not** leak into the migrated assembly — otherwise
    `EngineIsolationAuditTests` will flag `GeckoWebBrowser`. The exact assembly/host boundary that keeps
    the audit green needs a deliberate program decision.
  - *(block)* block 8b's preview-bearing dialog on Stage 10. Cleaner audit story, later delivery.
  Recommendation in the review: permit the coexisting island during coexistence with an explicit fence,
  and exclude preview parity from 8b's exit gate — **but the precise §11.3-vs-`EngineIsolationAuditTests`
  boundary is still an open program decision, not a repo fact.**

- **Lists hierarchical tree-bar is unexercised by the exemplar** (semantic-domain etc.). Needs the
  Stage-3 unbounded-tree control + `*TreeBarHandler` behavior; medium risk it is under-sized.

- **Notebook Document / `XhtmlRecordDocView` are document surfaces** that quietly depend on Stage 9. If
  Stage 8 is read as "Notebook done," it inherits Stage 9 scope it cannot satisfy — keep these explicitly
  deferred until Stage 9 covers them.

- **No app-wide surface registry/census exists yet** — only lexical-edit-scoped assets. 8c hard-depends
  on the Stage-2 census; surface this as a blocking link in JIRA so the straggler sweep cannot silently
  become unbounded discovery.

- **Dependency-graph corrections to land in the plan:** add `S3 → S8` and `S9 → S8` to the §5 mermaid
  graph and to the §4 dependency row (currently `4,5` only), per review §4/§5 and synthesis §7.
