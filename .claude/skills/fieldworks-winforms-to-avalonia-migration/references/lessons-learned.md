# Lessons Ledger and Skill-Update Protocol

The fieldworks-* migration skills only stay useful if every migration
updates them. This file defines (a) the routing table that maps each kind
of discovery to the exact place to record it, and (b) an append-only ledger
of migration retrospectives so future agents can see how the skill set
evolved and why.

## Update protocol — where each discovery goes

Run this at the end of every migration (workflow step 10 in SKILL.md), and
immediately whenever you hit a stale pointer mid-task. Make the edits in
the same PR as the migration.

| You discovered… | Update |
| --- | --- |
| A new architectural pattern, or a refinement of an existing one | `architecture-patterns.md` — add/extend the numbered section (decision, why, canonical code, gotchas); add a row to the SKILL.md quick map if it is load-bearing |
| A new seam contract | `seam-catalog.md` §1/§2 plus its pivot trigger in §3 |
| A pivot trigger fired (decision re-evaluated) | Record the outcome inline in `seam-catalog.md` §3 and summarize in the ledger below |
| A new plugin for a custom slice class | `architecture-patterns.md` §5 canonical-code list; keep the burn-down test list current |
| A new gotcha / failure mode (interop, DPI, fonts, focus, threading, lifetime…) | The Gotchas paragraph of the matching `architecture-patterns.md` section; if it is a review smell, also add a red flag to the most relevant satellite skill |
| A new forbidden legacy symbol | `EngineIsolationAuditTests.cs` (the enforcement) and `parity-evidence.md` §4 (the documentation) — both in the same PR |
| A new evidence lane, artifact type, or evidence-language term | `parity-evidence.md` |
| A new mandatory step in the per-region process | `migration-checklist.md` (and the workflow list in SKILL.md if it is a new phase) |
| A trigger phrase that failed to invoke a skill when it should have | The `description` frontmatter of that skill — add the missing vocabulary; keep descriptions quoted (YAML colons) and third-person |
| A stale file pointer (file moved/renamed, openspec change archived) | Fix the pointer in whichever skill file holds it; prefer pointing at code and tests over change docs |
| Updated performance baselines | `DataTreeTimingBaselines.json` stays the source of truth; update budget notes in `parity-evidence.md` §5 only if the policy (not the numbers) changed |

Rules of thumb:

- **Skills point, references explain, openspec records provenance.** Do
  not paste large doc content into skills; capture the durable decision and
  point at code/tests, citing the openspec doc as provenance.
- **Generalize before writing.** Record the class of problem, not the
  one-off instance. If it only applies to one region, it goes in that
  region's openspec change, not here.
- **Prune as you add.** If a section no longer pays for its tokens
  (pattern superseded, gotcha fixed at the framework level), delete or
  collapse it. Skills are working memory, not an archive — the archive is
  git history and openspec.
- **Keep SKILL.md bodies under ~150 lines** and references one level deep
  from SKILL.md. If a reference outgrows ~300 lines, split it by domain
  and update the pointers.

## Ledger

Append one entry per completed migration (newest first). Keep entries to
~10 lines: link to the change, what was migrated, what was learned, which
skill files changed.

### 2026-06 — Legacy truth-PNG capture toolkit (legacy-screenshot-capture)

- Change: `openspec/changes/legacy-screenshot-capture/`. Produced the legacy WinForms screenshots
  that back the `Docs/migration/` docs, after UIA2 (winforms-mcp) proved unable to navigate FLEx's
  custom-drawn surfaces.
- Built two non-UIA capture routes (details + recipes folded into the `fieldworks-winapp` skill):
  (1) **launch-per-tool** — `FieldWorks.exe "silfw://…&tool=<toolId>"` (guid-less) +
  PrintWindow; `scripts/migration-capture/Capture-LegacyTools.ps1` captured **67/67** tool/list
  screens. (2) **dialog harness** — an `[Explicit]` NUnit fixture
  (`LexTextControlsTests/ScreenshotHarnessTests.cs`) on the in-memory-cache base, constructing the
  legacy dialog (ctor + `SetDlgInfo`) and `DrawToBitmap`-ing it; proven on the feature-chooser dialog.
- Learned: (1) **UIA2 can't see SilSidePane/OutlookBar/tool-lists or Views content, and has no
  coordinate-click** — don't plan a capture campaign around UIA navigation; drive via the supported
  link mechanism / direct construction instead. (2) An empty `guid=` crashes `FwLinkArgs`; omit it —
  `LinkListener` switches tools regardless. (3) A standalone capture exe is the expensive path
  (reg-free COM manifest + cache + Mediator/PropertyTable/CmObjectUi); **piggybacking a test
  project's existing bootstrap** is far cheaper. (4) `System.Drawing.Bitmap` is unavailable in
  pwsh 7 — run capture scripts under Windows PowerShell 5.1. (5) Verify wiring/feasibility
  empirically before scaling — the first "winforms-mcp will do it" plan was wrong.
  (6) The dialog harness renders simple tree/list/feature dialogs headless, but FwTextBox/app-context
  dialogs (InsertEntryDlg / AddNewSenseDlg / GoDlg family) assert/NRE without a real main window +
  stylesheet (the "LcmStyleSheet" PropertyTable fallback was not enough) - capture those live or on
  JIRA pickup. (7) The restored test base already holds an undoable task open - make model writes
  directly, never inside a nested NonUndoableUnitOfWorkHelper ("Nested tasks are not supported").
- Before/after pipeline: each migration doc shows legacy "before" + Avalonia "after" of the SAME
  seeded data; "before" from this harness/script, "after" from the surface's
  fieldworks-semantic-render-parity visual test; both attach to the JIRA ticket (Docs/migration/_TEMPLATE.md).
- Skill files changed: `fieldworks-winapp/SKILL.md` (recipes + UIA limits + before/after + JIRA),
  `fieldworks-semantic-render-parity/SKILL.md` (after-lane role), this ledger.

### 2026-06 — Phase-1 PR landing strategy (canonical-per-primitive, document-then-back-out)

- Change: scratchpad `phase1-pr-prep-manifest.md`; branch `010-advanced-entry-view-phase-1-2`.
- Decided how to land a sprawling derisk branch (~864 files / +140k): keep ONE canonical
  screen per UI primitive (table/detail-editor/tree+multi-select/tabs/owned-form/search-list),
  document every other deferred WinForms screen under `Docs/migration/` (md + live-FLEx PNG +
  parity checklist + gotchas) and file a JIRA, then back the screen out (remove view/VM/tests +
  unwire its call site). XL surfaces in their own openspec changes split to follow-up PRs
  instead of backing out. Now encoded in SKILL.md "Phase-1 Landing Strategy" +
  migration-checklist.md "Phase 0" + `Docs/migration/{README,_TEMPLATE}.md`.
- Learned: (1) **`UIMode` defaults `"Legacy"`** (Settings.Designer.cs) and every Avalonia
  surface gates on it, so "not breaking anything" is structural — back-out is for reviewability,
  not safety; do it aggressively. (2) **Verify wiring from call sites, never from an Explore
  summary** — an Explore sweep falsely flagged FilterFor/DateRange/FindReplace/SpecialChar as
  "unwired spec-only" when three were instantiated in `RecordBrowseView.cs`; only SpecialChar +
  WritingSystemProperties were genuinely unwired. Quote `file:line`. This repeats the standing
  lesson: Explore agents reading excerpts in isolation produce false negatives/positives —
  ground-truth before deleting. (3) The reusable *control* and the canonical *screen* are
  different layers — keep all controls; keep one screen per primitive. (4) `ChooserDialog`
  covers two named primitives (tree + multi-select) — one screen can be canonical for several.
- Skill files changed: `SKILL.md` (Phase-1 Landing Strategy), `migration-checklist.md` (Phase 0),
  `fieldworks-migration-scope-review` (Phase-1 split trigger), `fieldworks-winapp` (Docs/migration
  capture pointer), this ledger.

### 2026-06 — avalonia-rule-formula-editor + avalonia-interlinear-editor (two XL editors)

- Changes: `openspec/changes/avalonia-rule-formula-editor/`, `.../avalonia-interlinear-editor/`.
- Migrated: all 6 Grammar rule tools (`PhonologicalRuleEdit`, `EnvironmentEdit`,
  `compoundRuleAdvancedEdit`, `naturalClassedit`, `phonemeEdit`, `AdhocCoprohibEdit`) — sectioned
  LCModel-free `RuleFormulaModel` DTO, projector in **xWorks** (`RuleFormulaProjector`, NOT
  Morphology — circular), read-only control + edit sink staging via the fenced
  `RegionEditContextBase`; and the Words `Analyses` interlinear morph-bundle editor — NO
  Sandbox/LCModel in the FwAvalonia view (`InterlinearAnalysisModel`), all reads/writes +
  Sandbox-parity MSA-prune in the xWorks plugin (`InterlinearAnalysisProjector`/`WriteBack`).
- Learned: (1) **Projectors/write-back live in xWorks, never in FwAvalonia** (xWorks has both
  LCModel + FwAvalonia refs; Morphology→FwAvalonia would be circular). The view binds a
  projection DTO; the plugin owns every LCModel touch. (2) **`ToFormulaString()`/oracle strings
  are the parity contract** — encode the legacy rendering ("p → [V] / [C] __ #") as a test
  oracle, not free-form. (3) Context sections need the atomic↔`PhSequenceContext` 0→1→2→1→0
  transition (legacy `CreateSeqCtxt`); own the context FIRST, then set `FeatureStructureRA`
  (else NRE). (4) MSA-prune parity: editable only for human-approved analyses (legacy
  `deParams editable="true"`); write-back + prune on the region's shared fenced UOW (one undo
  step). (5) Deferred with `// PARITY`: morph re-segmentation, MoAffixProcess affix-process
  editing, metathesis middle/move, adhoc nested-group recursion — leaf editing ships, recursion defers.
- Skill files changed: this ledger. New plugins added to `architecture-patterns.md` §5 list;
  composer-generalization gotchas in §2 (see next entry).

### 2026-06 — §20 class-general composer + the composer fixes that unblocked many tools

- Change: `openspec/changes/lexical-edit-avalonia-migration/` §20 + §19i fix sweep.
- Generalized the entry composer to any `ICmObject` (`Compose(ICmObject, layout, choiceGuid)`;
  `RegionEditContextBase`/`ComposedRegionEditContext` on `ICmObject`), unblocking notebookEdit /
  posEdit / Lists / the rule tools. Three regression-free composer fixes were the gating work:
- Learned: (1) **Layout choice resolution is 4-key**, not 1 — `LayoutSourceLoader` had collapsed
  11 RnGenericRec variants to Analysis; thread `choiceGuid` + `ResolveLayoutChoiceGuid` and memo
  by it (root blocker for Notebook/Lists edit). (2) **Multi-child `<if Disabled=true>`/`<if
  Disabled=false>` pairs** were imported as only the first child — `DictionaryPartResolver` must
  return `part.Elements()` (all children); each `<if>`→Conditional node. This made
  MoExoCompound's Name/Description/Active/category-pickers compose. (3) **Generic editable
  reference vector + atomic chooser** (`AddGenericReferenceVector`/`AddGenericAtomicChooser` via
  `ReferenceTargetCandidates`) live in the SHARED `WalkOtherField` fallthrough — gate out
  virtual/computed props (`if (flid==0 || _mdc.get_IsVirtual(flid)) return null;`), mirroring
  legacy `VectorReferenceView.cs:440` (`!get_IsVirtual`), so back-refs/derived collections stay
  read-only (no blind `Replace` corruption). Decision: keep the generic global path — legacy
  editing is itself fully metadata-driven (one `AtomicReferenceSlice`/`ReferenceVectorSlice` per
  field type, ZERO per-class allow-list), so narrowing to grammar classes would be an
  anti-pattern. (4) §19i data-loss: a GenDate composer that emitted `ToLongString()` corrupted
  year-granular dates — emit the canonical year-granular form. (5) Shared-composer changes are
  high blast-radius — MEASURE it (lexicon/notebook/back-ref suites, 192 tests) and run full
  `./test.ps1`; the only failures should be the known 38 environmental data-sentinel ones.
- Skill files changed: `architecture-patterns.md` §1/§2 (layout-choice 4-key + multi-child part
  import + generic metadata-driven reference editing + virtual-prop gate), this ledger.

### 2026-06 — §19e remaining detail-editor field types to parity

- Change: `openspec/changes/lexical-edit-avalonia-migration/field-types-test-research.md`.
- Migrated: dedicated `RegionFieldKind` editors for enum closed-combo (closed
  `ComboBox`, rejects free text), integer (numeric `TextBox` that rejects non-numeric
  keystrokes + reject-and-restore), GenDate qualifiers (new `FwGenDateField`: year +
  precision Before/On/About/After + era AD/BC, composing a `GenDate.TryParse` long-string)
  and exact-date calendar picker (`CalendarDatePicker` beside the parse-on-commit text box),
  literal/"lit" (static `TextBlock`), jtview nested-layout recursion (`WalkEmbeddedView`),
  and per-field WS visibility (`visibleWritingSystems`). Touched
  `RegionFieldKind`/`RegionFieldControlFactory`/`EditorKindMap`(no change needed — already
  classified)/`XmlLayoutImporter`/`ViewNode`/`FullEntryRegionComposer` + strings + tests.
- Learned: (1) probe LCModel string grammars from the DLL before composing — `GenDate.ToLongString`
  emits "About AD 1985"/"After 500 BC"/"AD 1990" and `TryParse` rejects "circa"/"about 1990",
  so the structured editor must emit the canonical word order (precision word + AD-prefix /
  year + BC-suffix), NOT free synonyms. (2) Avalonia `CalendarDatePicker.SelectedDate`
  raises `SelectedDateChanged` for the INITIAL programmatic seed (deferred, during headless
  layout) — guard with a remembered last-date value-compare, not a timing flag, so only a
  genuine user pick stages. (3) Changing an editor's returned control shape (date text box →
  text+calendar panel; GenDate → structured editor) breaks every test that cast
  `Build(...)` straight to `TextBox` — search `(TextBox)` / `InstanceOf<TextBox>` on the
  affected kind first and extract via `GetVisualDescendants`. (4) Enum/integer were already
  functionally safe as Chooser/Text in the composer; the §19e value was a DEDICATED kind so
  the rejection is visible at the editor and the dispatch is explicit (no free-form regression).
- Skill files changed: this ledger entry. Architecture/seam patterns unchanged (new kinds
  ride the existing one-switch `RegionFieldControlFactory` dispatch; importer/composer seams
  extended, not restructured).

### 2026-06 — §19d audio (voice WS) + pictures (CmPicture) editable parity

- Change: `openspec/changes/lexical-edit-avalonia-migration/media-pictures-test-research.md`.
- Migrated: pictures from read-only thumbnail to insert/replace/delete + caption/
  description/license/creator metadata, plus the picture ORC into rich text
  (closes §19c's deferral); audio (IsVoice) from a blanket read-only placeholder to
  play/record/clear. New seam methods on `IRegionEditContext`
  (`TryInsertPicture`/`TryReplacePictureFile`/`TryDeletePicture`/`TrySetPictureMetadata`/
  `TryInsertPictureOrc`); a new LCModel-free media seam `IRegionMediaServices`
  (file pick / picture-properties dialog / audio play+record) implemented in xWorks
  as `LcmRegionMediaServices`; picture LCModel writes centralized in
  `RegionPictureEditor`; audio rides the existing text setter (a voice alternative is
  a multistring alt whose text is the filename).
- Learned: (1) audio play/record uses libpalaso `SIL.Media.AudioFactory`/
  `ISimpleAudioSession` (the same device the legacy `ShortSoundFieldControl` uses;
  NAudio underneath, Linux via Alsa) — prefer it over hand-rolled NAudio; cross-platform
  record availability is reported by `ISimpleAudioSession.CanRecord`, gated in the UI.
  (2) `PalasoImage`/ClearShare `Metadata` (`Creator`/`CopyrightNotice`/
  `SaveUpdatedMetadataIfItMakesSense`) live in `SIL.Windows.Forms.ImageToolbox` —
  caption/description are real `ICmPicture` LCModel multistrings (testable on an
  in-memory cache with no file); license/creator are file metadata (best-effort, real
  file only). (3) Adding a method to `IRegionEditContext` breaks ~6 direct implementers
  (incl. test fakes) — net48/C#7.3 has no default interface methods, so budget for
  touching every implementer. (4) Picture insert into rich text is cleanest via
  `ICmPicture.InsertORCAt(tss, ich)` (canonical guid/ORC encoding) then write the TsString
  back through the existing rich-text setter — never hand-encode `ktptObjData`.
- Skill files changed: this ledger entry. Architecture/seam patterns unchanged (the new
  picture/audio methods extend the existing edit-context seam; `IRegionMediaServices` is a
  host-side media seam analogous to the existing dialog-launcher service seam).

### 2026-06 — Entries browse-table rendering cutover + headless integration harness

- Change: `openspec/changes/shared-editable-virtualized-table/`
  (`rendering-cutover-design.md`, `headless-integration-harness.md`).
- Migrated: the lexicon Entries table off the native C++ Views rendering for its
  surface — owned WS-aware cell renderer (`BrowseCellRenderer`), rich-cell value
  source via `RegionRichTextAdapter.FromTsString`, and clerk-routed sort/filter
  (`BrowseViewer.MakeColumnSorter`/`MakeColumnFilter` → `Clerk.OnSorterChanged`/
  `OnChangeFilter`) replacing the lossy string mirror and the client-side filter
  projection. Legacy `BrowseViewer` still constructed underneath (F1); its
  retirement is F2/Stage-13.
- Key lessons now encoded: **headless integration scenario tests are the
  front-and-center verification style** (new architecture-patterns.md §13;
  parity-evidence.md §2a + the "live-verification-only" downgrade;
  migration-checklist.md Phase 7 gate; SKILL.md workflow step 7 + quick map).
  A read-only grid needs **neither the C++ engine** (cell/sort/filter extraction
  runs through the managed `CollectorEnv : IVwEnv`, no `RootBox`) **nor live
  verification** (real `RecordClerk` narrowing is provable headlessly). Two-layer
  harness: surface-workflow drivers in an Avalonia-headless assembly + real-clerk
  layer in `xWorksTests`. Gotchas: never put `[AvaloniaTestApplication]` in
  `xWorksTests` (~1400 tests share the host); the restored test base holds the
  undoable task open (no nested `NonUndoableUnitOfWorkHelper`); `OnChangeFilter`
  takes an (added, removed) delta that `RecordList` composes into its `AndFilter`.
- Skill files changed: `references/architecture-patterns.md` (§13),
  `references/parity-evidence.md` (§2a, §3), `references/migration-checklist.md`
  (Phase 7), `SKILL.md` (quick map + workflow step 7), this ledger.

### 2026-06 — Lexical Edit (full entry view), phases 1–2 (seed entry)

- Change: `openspec/changes/lexical-edit-avalonia-migration/` (plus
  `avalonia-migration-roadmap`, `lexical-edit-avalonia-poc-spike`).
- Migrated: first Avalonia lexical-edit region — typed IR pipeline, region
  composer, owned field controls (`FwMultiWsTextField`, `FwOptionPicker`,
  menus/flyouts), plugin registry, surface selection service, seam
  contracts, Path 3 parity harness.
- Key lessons now encoded: boundary above DataTree (don't extract
  internals); owned dense controls over stock grids; explicit
  unsupported rows over silent fallback; one global undo stack; WinForms
  dialogs own all modality during coexistence; measured (not estimated)
  performance budgets; StringTable + `.resx` dual localization lanes;
  `<RootNamespace>` required for Crowdin satellite builds.
- Skill set restructured (this commit): skills moved from
  `.github/skills/` to `.claude/skills/` per AI_GOVERNANCE no-mirror rule;
  hub skill rewritten with references/ (architecture-patterns, seam-catalog,
  parity-evidence, migration-checklist, this ledger); satellite skill
  descriptions rewritten for triggering; fixed YAML-colon bug that broke
  `fieldworks-ui-wiring-review` triggering.


### 2026-06 - Browse functional remainders (browse 19f)

- Shipped the last browse-table parity items as **view events + a thin
  capability seam on the row source + product-edge routing**, reusing the
  established pattern (view raises -> LexicalBrowseHostControl re-raises ->
  RecordBrowseView owns LCModel/dialogs/commands and routes back). New
  optional seams: IBrowseRowMenuSource (data-row context menu),
  IBrowseRdeSource (Rapid-Data-Entry new-row). No new infra.
- **Reuse over reinvent paid off twice:** header drag-reorder routed through
  the *existing* ApplyConfiguredColumns (the Configure-Columns path), and
  in-cell picture editing needed *no* new editor - the editable cell already
  realizes whatever RegionFieldKind GetEditField returns, and the factory
  already builds Image. Item collapsed to "return an Image-kind field".
- **icu layering gotcha:** icu.net is referenced by **xWorks, not
  FwAvalonia**. So the diacritic/WS-collation matcher (Find/Replace P2) lives
  in ClerkBrowseRowSource.ComputeReplaced (xWorks), keeping the view
  icu-free. Diacritic-insensitive match = NFD-decompose + strip combining
  marks + an index-map to splice the replacement back into the *original*
  string (untouched diacritics survive). WS-collation = primary-strength
  Icu.Collation.Collator.Compare whole-cell equality.
- **Mediator.SendMessage is obsolete-as-error.** Broadcast commands via
  Publisher.Publish(new PublisherParameterObject(key)) instead.
- **Parallel-agent worktree sharing:** field-types (19e) and browse (19f)
  ran in the SAME worktree concurrently. It worked because the file-ownership
  split was clean and both built green together - but only edit the lines in
  shared files (tasks.md, this ledger) that are unambiguously yours, never
  touch the other agent's files, and verify the COMBINED build.
- **Section-sign / em-dash break exact-match Edits.** Anchor Edit old_string
  on ASCII-only lines (or use a Python byte-replace) when nearby code carries
  those chars; hit this several times this session.
