# Native Views Audit (Tasks 8.1, 8.2, 8.6, 8.7, 8.8)

Date: 2026-06-09

Scope: the migrated Lexical Edit region is the FwAvalonia region-model path
(`Src/Common/FwAvalonia` + `RecordEditView` routing in `Src/xWorks`). The legacy region it replaces
is `DataTree`/`Slice` (`Src/Common/Controls/DetailControls`), which is substantially
RootSite/native-Views-backed. Every row cites a verified file path in this worktree. Rows are
marked **[E]** evidence-based (verified declaration/call site) or **[J]** judgment call (reachability
or phase assignment inferred, not proven by a runtime trace).

Related enforcement already in place: `Src/Common/FwAvalonia/FwAvaloniaTests/EngineIsolationAuditTests.cs`
(tasks 5.5/5.8) forbids the FwAvalonia production assembly from referencing
`ViewsInterfaces`/`RootSite`/`SimpleRootSite`/Graphite/Gecko assemblies and from naming
`IVwRootBox`, `IVwEnv`, `IVwGraphics`, `RootSiteControl`, `ManagedVwWindow`, etc. in source.

Status update (2026-06-15, `avalonia-multi-writing-system-text-foundation`):
- Closed blocker lanes for migrated lexical text rows: managed `ITsString` run projection/write-back, grapheme-cluster-safe editing (combining marks/surrogates/ZWJ), shared TsString clipboard+drag/drop interchange, and coexistence refresh/undo for styled commits.
- `MultiStringSlice`, `StringSlice`, and `GhostStringSlice` now have owned Avalonia editing coverage with focused automated evidence.
- Still deferred: `StTextSlice` multi-paragraph editing.
- Still deferred: rich runs with unsupported object-content payloads remain read-only in the Avalonia lane.

---

## 8.1 Inventory

Native Views/C++ viewing/rendering/editor dependencies reachable from the legacy Lexical Edit
region (`RecordEditView` → `DataTree` → slices), bottom-up.

### 8.1.1 Engine and interop foundation

| Dependency | File evidence | What it provides | Mark |
|---|---|---|---|
| Native Views engine (C++) | `Src/views/VwRootBox.cpp`, `Src/views/VwEnv.cpp`, `Src/views/VwSelection.cpp`, `Src/views/VwSimpleBoxes.cpp`, `Src/views/VwTableBox.cpp`, `Src/views/VwLazyBox.cpp` | Box layout, paragraph layout, selection model, laziness for all RootSite views | [E] |
| Native IME/TSF integration | `Src/views/VwTextStore.cpp` / `VwTextStore.h` | Text Services Framework store: composition, keyboard input into the root box | [E] |
| Native accessibility | `Src/views/VwAccessRoot.cpp` | IAccessible exposure of view boxes | [E] |
| Native render engines | `Src/views/lib/UniscribeEngine.cpp`, `Src/views/lib/UniscribeSegment.cpp`, `Src/views/lib/GraphiteEngine.cpp`, `Src/views/lib/GraphiteSegment.cpp` | Text shaping/segmenting (Uniscribe + Graphite) | [E] |
| Native TsString/props kernel | `Src/views/lib/TsString.cpp`, `Src/views/lib/TsTextProps.cpp`, `Src/views/lib/TextServ.cpp` | Native string/props used by the C++ engine (managed code uses LCModel's managed TsString) | [E] |
| Managed COM interop layer | `Src/Common/ViewsInterfaces/Views.cs` (`IVwRootBox`, `IVwEnv`, `IVwSelection`, `IVwGraphics`, `IVwDrawRootBuffered`, `SetSpellingRepository` at lines 7524/8489/9432) | The managed face of the native engine; everything below consumes it | [E] |
| `ManagedVwWindow` | **Retired.** `Src/ManagedVwWindow` removed by the archived `retire-linux-era-view-shims` change (its `tasks.md` 3.2/3.3 done); only installer-removal records remain (`Build/Installer.legacy.targets:116-119`, CLSID exclusion `Build/mkall.targets:46`) | Linux-era shim; **not** a live dependency of Lexical Edit | [E] |

### 8.1.2 Managed hosting/editor stack

| Dependency | File evidence | What it provides | Mark |
|---|---|---|---|
| `SimpleRootSite` | `Src/Common/SimpleRootSite/SimpleRootSite.cs:39` (`UserControl, IVwRootSite, IRootSite, ...`) | WinForms host of an `IVwRootBox`: paint, scroll, focus, keyboard dispatch | [E] |
| Buffered rendering path | `Src/Common/SimpleRootSite/SimpleRootSite.cs:230` (`IVwDrawRootBuffered m_vdrb`), `:5338` (`VwDrawRootBufferedClass.Create()`) | All on-screen drawing of view content | [E] |
| Editor realization / typing | `Src/Common/SimpleRootSite/EditingHelper.cs:1054-1082` (`CallOnTyping` → `Callbacks.EditedRootBox.OnTyping(...)`) | Keystrokes become document edits inside the native engine | [E] |
| Hit testing / selection | `Src/Common/SimpleRootSite/EditingHelper.cs:1492` (`rootb.MakeSelAt(...)`), `Src/Common/SimpleRootSite/SelectionHelper.cs`, `Src/Common/SimpleRootSite/TextSelInfo.cs` | Mouse→selection, selection persistence/restore metadata | [E] |
| Render-engine selection | `Src/Common/SimpleRootSite/RenderEngineFactory.cs:110-120` (`GraphiteEngineClass.Create()` when `ws.IsGraphiteEnabled`, else Uniscribe) | Per-WS shaping engine choice for every RootSite view | [E] |
| Managed view constructors | `Src/Common/SimpleRootSite/VwBaseVc.cs` (`FwBaseVc` lives beside it) | Base class for the `IVwEnv`-driven display logic each slice view implements | [E] |
| `RootSite` / `RootSiteControl` | `Src/Common/RootSite/RootSite.cs:61`, `Src/Common/RootSite/RootSiteControl.cs:15` | LCModel-aware root site; base of nearly every slice view below | [E] |
| Spell-check squiggle wiring | `Src/Common/RootSite/RootSite.cs:800` (`m_rootb.SetSpellingRepository(SpellingHelper.GetCheckerInstance)`), `Src/Common/RootSite/SpellCheckHelper.cs`, `Src/Common/RootSite/RootSiteEditingHelper.cs:349` | Native engine draws squiggles by querying the managed spell engine | [E] |
| Clipboard rich-text bridge | `Src/Common/SimpleRootSite/TsStringWrapper.cs` | Serialized TsString clipboard format (now also the 3.13 shared seam) | [E] |
| Printing path | `Src/Common/SimpleRootSite/PrintRootSite.cs` | Print layout via the native engine | [E] |

### 8.1.3 Views-backed slice classes in the legacy Lexical Edit region

`Slice` itself is a plain WinForms `UserControl` (`Src/Common/Controls/DetailControls/Slice.cs:46`);
the Views dependency enters through these subclasses and their inner view controls.

| Slice / view class | File evidence | Views usage | Mark |
|---|---|---|---|
| `ViewSlice` (base) | `Src/Common/Controls/DetailControls/ViewSlice.cs:16` | Hosts a `RootSite` control as the slice body | [E] |
| `ViewPropertySlice : ViewSlice` | `Src/Common/Controls/DetailControls/ViewPropertySlice.cs:11` | Base of the property-bound Views slices | [E] |
| `MultiStringSlice : ViewPropertySlice` | `Src/Common/Controls/DetailControls/MultiStringSlice.cs:29,33` (creates `LabeledMultiStringView`) | Multi-WS string editing — the most common Lexical Edit editor (`SliceFactory.cs:85-107` "first, these are the most common slices") | [E] |
| `LabeledMultiStringView` (adapter) | `Src/Common/Controls/Widgets/LabeledMultiStringView.cs:29` (`UserControl, IxCoreColleague`) | WS-labeled wrapper around the inner root site | [E] |
| `InnerLabeledMultiStringView` | `Src/Common/Controls/Widgets/InnerLabeledMultiStringView.cs:26` (`: RootSiteControl`) | The actual native-Views text editor for multistring slices | [E] |
| `InnerLabeledMultiStringControl` | `Src/Common/Controls/Widgets/InnerLabeledMultiStringControl.cs:17` (`: SimpleRootSite`) | Cache-light variant used in dialogs (e.g. insert-entry) | [E] |
| `StringSlice : ViewPropertySlice` | `Src/Common/Controls/DetailControls/StringSlice.cs:24`; inner `StringSliceView : RootSiteControl` at `:388` | Single-WS string editing (`SliceFactory.cs:146-154`) | [E] |
| `StTextSlice : ViewPropertySlice` | `Src/Common/Controls/DetailControls/StTextSlice.cs:29`; inner `StTextView : RootSiteControl` at `:228` | Structured-text editing (`SliceFactory.cs:305`) | [E] |
| `GhostStringSlice : ViewPropertySlice` | `Src/Common/Controls/DetailControls/GhostStringSlice.cs:35,64,77` (`GhostStringSliceVc : FwBaseVc`, `GhostStringSliceView : RootSiteControl`); created in `DataTree.cs:2822` | Ghost (not-yet-created property) editing and become-real editor realization | [E] |
| `SummarySlice : ViewSlice` | `Src/Common/Controls/DetailControls/SummarySlice.cs:28`; `LiteralLabelView : RootSiteControl` at `:669`; `SummaryXmlView : XmlView` at `:800` | Section header rendering uses the Views engine even for labels | [E] |
| `PhoneEnvReferenceSlice : ReferenceSlice` | `Src/Common/Controls/DetailControls/PhoneEnvReferenceSlice.cs:26`; `PhoneEnvReferenceView : RootSiteControl` (`PhoneEnvReferenceView.cs:33`); created in `SliceFactory.cs:301` | Phonological-environment editing with live validation | [E] |
| `ReferenceViewBase : RootSiteControl` | `Src/Common/Controls/DetailControls/ReferenceViewBase.cs:26` | Base of all reference-launcher views | [E] |
| Reference views (atomic/vector) | `Src/Common/Controls/DetailControls/AtomicReferenceView.cs` (5 `IVwRootBox/IVwEnv` hits), `PossibilityAtomicReferenceView.cs`, `VectorReferenceView.cs`, `PossibilityVectorReferenceView.cs` | Display + selection inside `AtomicReferenceSlice`/`ReferenceVectorSlice`/possibility slices (`ReferenceSlice.cs:25`, `AtomicReferenceSlice.cs:19`, `ReferenceVectorSlice.cs:30`) | [E] |
| `AtomicRefTypeAheadSlice` | `Src/Common/Controls/DetailControls/AtomicRefTypeAheadSlice.cs:54` (`AtomicRefTypeAheadView : RootSiteControl`) | Type-ahead reference editing | [E] |
| `AudioVisualSlice : ViewSlice` | `Src/Common/Controls/DetailControls/AudioVisualSlice.cs:33,388` (`AudioVisualView : RootSiteControl`) | Pronunciation media file display | [E] |
| `MediaInfoSlice : ViewSlice` | `Src/Common/Controls/DetailControls/MediaInfoSlice.cs:20,89` (`MediaInfoView : RootSiteControl`) | Media info display | [E] |
| `MultiLevelConc` / `TwoLevelConc` slices | `Src/Common/Controls/DetailControls/MultiLevelConc.cs:291,391`, `TwoLevelConc.cs:343,376` (`ConcView : RootSiteControl`, `ConcSlice : ViewSlice`) | Legacy concordance slices in DetailControls | [E] |
| `GhostReferenceVectorSlice : FieldSlice` | `Src/Common/Controls/DetailControls/GhostReferenceVectorSlice.cs:23` | Ghost variant for vector refs; realizes a Views-backed reference slice on edit | [E] |

### 8.1.4 Custom-editor slices outside DetailControls reachable from Lexical Edit layouts

Loaded via `SliceFactory`'s `custom`/assembly-loaded editor path from lexicon layout XML.

| Class | File evidence | Mark |
|---|---|---|
| `ReversalIndexEntrySliceView : RootSiteControl` | `Src/LexText/Lexicon/ReversalIndexEntrySlice.cs:216` | [E] |
| `MSADlglauncherView : RootSiteControl, IVwNotifyChange` | `Src/LexText/Lexicon/MSADlglauncherView.cs:14` | [E] |
| `MsaInflectionFeatureListDlgLauncherView : RootSiteControl` | `Src/LexText/Lexicon/MsaInflectionFeatureListDlgLauncherView.cs:20` | [E] |
| `PhonologicalFeatureListDlgLauncherView : RootSiteControl` | `Src/LexText/Lexicon/PhonologicalFeatureListDlgLauncherView.cs:13` | [E] |
| `StringRepSliceView : RootSiteControl` (env. string rep) | `Src/LexText/Morphology/PhEnvStrRepresentationSlice.cs:236` | [E] |
| `AnalysisInterlinearRs : RootSite` (Words/Analyses slice) | `Src/LexText/Morphology/AnalysisInterlinearRS.cs:27` | [E] — reachability from *lexiconEdit* layouts vs the Words area is [J] (it serves the Analyses detail view) |

### 8.1.5 Views-backed widgets used by region dialogs/launchers

| Widget | File evidence | Mark |
|---|---|---|
| `FwTextBox` / `InnerFwTextBox : SimpleRootSite` | `Src/Common/Controls/Widgets/FwTextBox.cs:1658` | [E] |
| `FwListBox` / `InnerFwListBox : SimpleRootSite` | `Src/Common/Controls/Widgets/FwListBox.cs:1252` | [E] |
| `FwMultiParaTextBox` / `InternalFwMultiParaTextBox : SimpleRootSite` | `Src/Common/Controls/Widgets/FwMultiParaTextBox.cs:220` | [E] |

---

## 8.2 Classification

Categories: **Baseline/fallback-only** (kept for parity comparison and the explicit legacy UI mode),
**Non-migrated-region-only** (other tools/areas), **Blocker** (the Avalonia region must replace it —
i.e. what gate 6.13's TsString text foundation plus 6.x editors must cover).

Important framing [E]: under the global UI-mode contract (tasks 1.9/3.10), the *entire* legacy
DataTree stack remains shipping as the selectable legacy surface during coexistence. "Blocker"
therefore means "the Avalonia region cannot claim parity until a managed replacement exists", not
"delete now". Deletion is gated by 8.5/8.6 and the legacy-mode sunset.

| Dependency (from 8.1) | Classification | Rationale | Mark |
|---|---|---|---|
| `MultiStringSlice` + `LabeledMultiStringView`/`InnerLabeledMultiStringView` | **Blocker** | The single most common Lexical Edit interaction; exactly what 6.13's multi-WS TsString foundation must replace (read/write, per-WS fonts/keyboards, IME, bidi) | [E] |
| `StringSlice` (`StringSliceView`) | **Blocker** | Single-WS text editing; covered by 6.13 + 6.1/6.2 | [E] |
| `GhostStringSlice` | **Blocker** | Ghost→real editor realization must be reproduced managed (IR ghost metadata + edit session) | [E] |
| `StTextSlice` (`StTextView`) | **Blocker** | Multi-paragraph text editing in entries (e.g. comments); needs the 6.13 foundation plus paragraph support | [E]; priority within lexicon parity is [J] |
| `SummarySlice` (`LiteralLabelView`, `SummaryXmlView`) | **Blocker** | Even header labels render through Views today; Avalonia region renders headers natively (already does in `LexicalEditRegionView`) | [E] |
| `PhoneEnvReferenceSlice`/`PhoneEnvReferenceView` | **Blocker** | Environment editing is part of lexeme-form/allomorph parity | [E]; phase placement (7.4) is [J] |
| `ReferenceViewBase` family (atomic/vector/possibility views + their slices) | **Blocker** | Reference display/selection inside launchers; replaced by Avalonia chooser/launcher controls (6.3) | [E] |
| `AtomicRefTypeAheadSlice` | **Blocker** | Type-ahead reference editing in the region | [E]; usage frequency is [J] |
| Lexicon custom slices (`ReversalIndexEntrySlice`, `MSADlglauncherView`, `MsaInflectionFeatureListDlgLauncherView`, `PhonologicalFeatureListDlgLauncherView`, `StringRepSliceView`) | **Blocker** | Reachable from shipped lexicon detail layouts via the custom-editor path | [E] for class/Views backing; per-layout reachability is [J] (custom editors are layout-driven) |
| `AudioVisualSlice` / `MediaInfoSlice` | **Blocker** (late-phase) | Pronunciation media slices appear in LexEntry layouts | [J] — reachable from Pronunciations; can be deferred behind explicit fallback under 6.12 |
| `MultiLevelConc` / `TwoLevelConc` | **Non-migrated-region-only** | Legacy concordance slices; not part of lexicon edit layouts | [J] — no lexiconEdit layout reference found; treat as dead/other-area code |
| `AnalysisInterlinearRs` | **Non-migrated-region-only** | Serves the Words/Analyses detail view (`Analyses` is an explicit legacy fallback in `RecordEditViewSwitchTests`) | [E] for the fallback routing; [J] for "never reachable from lexiconEdit" |
| `FwTextBox`/`FwListBox`/`FwMultiParaTextBox` widgets | **Blocker where used inside region-owned dialogs/choosers; otherwise shell-phase** | Region chooser/dialog replacements (6.3) must not re-host SimpleRootSite text boxes; app-wide dialog usage is the shell change's problem | [J] split; widget Views backing is [E] |
| `SimpleRootSite`/`RootSite`/`RootSiteControl`, `EditingHelper`, `SelectionHelper`, `RenderEngineFactory`, `IVwDrawRootBuffered` path | **Blocker (as the implied foundation) + Baseline/fallback-only (as shipping code)** | The Avalonia region must own rendering/selection/hit-testing/typing/IME itself (6.13, 8.3); the classes themselves stay for the legacy surface and parity baselines | [E] |
| `ViewsInterfaces` interop + native `Src/views` engine | **Baseline/fallback-only for this region; deletion blocked repo-wide (8.6)** | The migrated region must never load them (enforced by `EngineIsolationAuditTests`); other areas still need them | [E] |
| Spell squiggle wiring (`SetSpellingRepository`) | **Blocker (behavior), service (engine)** | Squiggle drawing/suggestion UI must be re-owned by Avalonia; the spell engine itself is a retained service (8.7/8.8) | [E] |
| `TsStringWrapper` clipboard format | **Retained shared seam** (not a blocker) | Deliberately adopted as the cross-surface clipboard contract (task 3.13) | [E] |
| `PrintRootSite` / printing | **Non-migrated-region-only / later phase** | Region printing flows are not in the current parity scope | [J] |
| `ManagedVwWindow` | **Already retired** | See 8.1.1 | [E] |

---

## 8.3 Region-local viewing/render/editor replacement (managed/Avalonia)

Date: 2026-06-15. With the multi-writing-system text foundation
(`avalonia-multi-writing-system-text-foundation`) landed, the migrated region now provides every
native-Views viewing capability itself, in managed/Avalonia form. This is the **positive** map
(what now provides each capability); `EngineIsolationAuditTests` is the **negative** audit (the
production assembly names/loads no native symbol). Both are asserted in code — the positive side by
`RegionViewingServiceReplacementTests` against the machine-checked `RegionViewingServices`
descriptor (`Src/Common/FwAvalonia/Region/RegionViewingServices.cs`).

| Native capability | Superseded native symbol | Managed/Avalonia owner (in `FwAvalonia`) | Evidence |
|---|---|---|---|
| Text shaping | `IRenderEngine` (Uniscribe/Graphite) | Avalonia text stack (Skia/HarfBuzz) inside `FwMultiWsTextField` | `RegionViewingServiceReplacementTests`; `EngineIsolationAuditTests` (no `IRenderEngine*`) |
| Measurement / layout | `IVwEnv` | Avalonia layout over `LexicalEditRegionView` panels | render/visual suites; no `IVwEnv` named |
| Selection metadata | `IVwSelection` | `RegionBidirectionalTextNavigation` + `RegionSelectionRange` over the run model | `RegionEditingTests` (mixed-direction caret/selection) |
| Hit testing | `IVwRootBox.MakeSelAt` | Avalonia `TextBox` hit test, cluster-normalized by `RegionTextGraphemeClusters` | `RegionEditingTests`; `TreeSpikeAndRtlTests` |
| Scrolling | `SimpleRootSite` auto-scroll | Avalonia `ScrollViewer` wrapper in `LexicalEditRegionView` (task 11.12) | viewing-parity suite (60-row overflow) |
| Rendering | `IVwDrawRootBuffered` | Avalonia renderer (Skia); parity frame captured from it (task 6.9) | `VisualParityCaptureTests` |
| Editor realization | `RootSiteControl` slices | Owned controls over the IR: `FwMultiWsTextField` / `FwChooserField` / `FwReferenceVectorField` / `FwDialogLauncherField` + `RegionRichTextEditAlgorithms` + `RegionImeCompositionState` | `RegionEditingTests`, `RegionModelTests`, foundation suites |

**Deferred (named, not silent)** — recorded in `RegionViewingServices.Deferred` and asserted by the
test:
- **StText multi-paragraph editing** — paragraph/document editing out of the first text wave; `sttext`
  fields render read-only content, full editing stays in the legacy view. Owner: foundation StText
  follow-on.
- **Embedded-object (ORC) rich-run editing** — ORCs are not a structural feature of the default
  lexeme string editors (census: `multistring`(118)/`string`(15) editors are plain; structural
  object content lives in `sttext`(11)). ORC-bearing values render **read-only with an explicit
  affordance** (`FwAvaloniaStrings.EmbeddedObjectReadOnly`), preserve the `TsString` losslessly, and
  stay editable in the legacy view (`RegionViewingServiceReplacementTests`). Revisit if real data
  shows common ORC runs in plain string fields.
- **Context-menu command routing** — see §8.5 (the one remaining region-driven native adapter).

## 8.4 Runtime non-instantiation evidence

Date: 2026-06-09 (refreshed 2026-06-15). The migrated region does not instantiate or call native
Views/C++ viewing/rendering/editor infrastructure at runtime for display or editing:
- `EngineIsolationAuditTests` — assembly references + source symbols (the production Avalonia
  assembly cannot even load native Views).
- `RecordEditViewActiveHostContractTests` — the live product host never initializes/drives the
  hidden legacy `DataTree` under Avalonia mode (a fresh load leaves `m_legacySurfaceInitialized ==
  false`; the empty `DataTree()` ctor realizes no slices/RootSites — `DataTree.cs:573`).
- headless render/editing suites run the full region path with no native Views present.
The single contract-gated exception (command-menu routing) is §8.5.

## 8.5 Removal / disable of region-local native viewing adapters

Date: 2026-06-15. There is **no region-local native viewing/render/editor adapter to physically
delete** — the Avalonia region was built clean (`FwAvalonia` references no native Views, enforced by
`EngineIsolationAuditTests`). "Disable" is therefore achieved structurally, in three layers:

1. **Viewing/render/editor adapter — disabled.** Under Avalonia mode the host never initializes the
   legacy surface, so no slice/RootSite is realized (active-host contract, task 3.10;
   `RecordEditViewActiveHostContractTests`). The `RecordEditView` ctor eagerly allocates an *empty*
   `DataTree` (`new DataTree()`), but that ctor instantiates **no** native Views — it only sets
   WinForms styles and empty collections (`DataTree.cs:573`). Making the field lazy was evaluated and
   **declined**: 67 `m_dataEntryForm` use-sites and a subclass that injects its own `DataTree` through
   the protected ctor (`InterlinearTextsRecordEditView`/`StTextDataTree`, `InfoPane.cs:177`) make it
   high blast-radius for **zero** native-Views benefit, since the empty control carries no adapter
   until `ShowObject` builds slices — which the contract already prevents.
2. **Command-routing adapter — retained, contract-gated, scheduled.** On first right-click the
   Avalonia surface lazily builds a hidden, detached legacy `DataTree` + `DTMenuHandler`
   (`EnsureMenuCommandAdapter`, `RecordEditView.cs:976`) solely to supply the colleague chain +
   `CurrentSlice` context the legacy xCore command handlers need. This **does** realize native Views,
   but only through the active-host contract's explicitly-approved `command-menu-routing` baseline
   adapter (task 3.10/13.4) and never on screen. It is classified as a command/service dependency
   (the viewing-vs-service line of §8.2/§8.7), and its removal is owned by the shell-phase xCore
   command-pipeline migration (`avalonia-command-focus` global phase, task 10.9).
3. **Shared native Views code — retained for non-migrated consumers.** `Src/views`,
   `ViewsInterfaces`, `SimpleRootSite`, `RootSite`, and the `DataTree`/slice stack stay available for
   the explicit legacy UI mode (selectable per task 1.9), the parity baseline, and the
   Grammar/Notebook/Lists/Words/Notebook fallbacks and other areas inventoried in §8.6. Removal is
   repo-wide and gated there, not by this region.

Net: the region's native **viewing/render/editor** seam is decommissioned (disabled by
construction); the only native code the region still drives at runtime is the contract-gated
command-routing adapter, named for shell-phase removal. Enforced by `EngineIsolationAuditTests`,
`RecordEditViewActiveHostContractTests`, and `RegionViewingServiceReplacementTests`.

---

## 8.6 Repo-wide native Views deletion blockers outside Lexical Edit

Even after Lexical Edit migrates, these consumers keep `Src/views`, `ViewsInterfaces`,
`SimpleRootSite`, and `RootSite` alive. Phase column: `lexical-edit 7.x` (this change),
`shell phase` (`fieldworks-avalonia-shell-migration`), or `unplanned` (no openspec change exists;
the roadmap defers it — `openspec/changes/avalonia-migration-roadmap/proposal.md` lines 25-32).
Phase assignments are [J] unless a change document names them.

| Consumer area | What it uses | File evidence | Migration phase |
|---|---|---|---|
| Interlinear text (Texts & Words) | `RootSite`-derived document/analysis views: `InterlinDocRootSiteBase`, `InterlinDocForAnalysis`, `InterlinTaggingChild`, `InterlinPrintChild`, `RawTextPane`, `TitleContentsPane`, `SandboxBase`/`Sandbox` | `Src/LexText/Interlinear/InterlinDocRootSiteBase.cs:28`, `InterlinDocForAnalysis.cs:30`, `InterlinTaggingChild.cs:29`, `InterlinPrintView.cs:17`, `RawTextPane.cs:31`, `TitleContentsPane.cs:22`, `SandboxBase.cs:34`, `Sandbox.cs:21` | unplanned |
| Concordance | Concordance container/control hosting browse (XMLViews) result views and interlinear panes | `Src/LexText/Interlinear/ConcordanceControl.cs:34`, `ConcordanceContainer.cs:23` | unplanned |
| Discourse chart | `ConstChartBody : RootSite`, `InterlinRibbon : InterlinDocRootSiteBase` | `Src/LexText/Discourse/ConstChartBody.cs:22`, `InterlinRibbon.cs:24` | unplanned |
| XMLViews browse/table (all areas) | `XmlBrowseViewBase : RootSite` + `XmlBrowseView`, `XmlBrowseRDEView`, `OneColumnXmlBrowseView`, `XmlView : RootSiteControl`, `XmlSeqView : RootSite` | `Src/Common/Controls/XMLViews/XmlBrowseViewBase.cs:28`, `XmlBrowseView.cs:26`, `XmlBrowseRDEView.cs:31`, `BrowseViewer.cs:1917`, `XmlView.cs:93`, `XmlSeqView.cs:113` | lexical-edit 7.1/7.2 for the lexicon browse pane; other areas' browse views: unplanned |
| xWorks document views | `XmlDocItemView : XmlView` in `RecordDocView` | `Src/xWorks/RecordDocView.cs:236` | unplanned |
| Notebook | `RecordEditView` + `DataTree` slices (notebook detail uses the same DetailControls stack); routed as explicit legacy fallback (`notebookEdit`) under the UI mode | `Src/xWorks/RecordEditView.cs`, fallback coverage in `Src/xWorks/xWorksTests/RecordEditViewSwitchTests.cs` (task 2.11) | unplanned (explicit legacy fallback meanwhile) |
| Grammar / Lists detail | Same DetailControls slice stack via `RecordEditView` (`posEdit`, `domainTypeEdit` fallbacks) | `Src/xWorks/xWorksTests/RecordEditViewSwitchTests.cs` (task 2.11) | unplanned (explicit legacy fallback meanwhile) |
| Grammar morphology tools | `InflAffixTemplateControl : XmlView`, `PatternView : RootSiteControl`, `OneAnalysisSandbox : SandboxBase` | `Src/LexText/Morphology/InflAffixTemplateControl.cs:30`, `Src/LexText/LexTextControls/PatternView.cs:18`, `Src/LexText/Morphology/OneAnalysisSandbox.cs:16` | unplanned |
| Parser UI | `TryAWordRootSite : RootSiteControl`, `TryAWordSandbox : SandboxBase` | `Src/LexText/ParserUI/TryAWordRootSite.cs:24`, `TryAWordSandbox.cs:17` | unplanned |
| FdoUi dialogs | `RelatedWordsView : SimpleRootSite` | `Src/FdoUi/Dialogs/RelatedWords.cs:553` | unplanned |
| Core dialogs / find-replace | `SampleView : SimpleRootSite` (converter tester), `BulletsPreview : SimpleRootSite`, `FwTextBox` in `FwFindReplaceDlg`, `ValidCharactersDlg` | `Src/FwCoreDlgs/ConverterTester.cs:529`, `Src/FwCoreDlgs/FwCoreDlgControls/FwBulletsPreview.cs:30`, `Src/FwCoreDlgs/FwFindReplaceDlg.cs`, `Src/FwCoreDlgs/ValidCharactersDlg.cs` | shell phase |
| Shared Views widgets (app-wide) | `FwTextBox`, `FwListBox`, `FwMultiParaTextBox`, `InnerLabeledMultiStringControl` | `Src/Common/Controls/Widgets/FwTextBox.cs:1658`, `FwListBox.cs:1252`, `FwMultiParaTextBox.cs:220`, `InnerLabeledMultiStringControl.cs:17` | shell phase |
| Framework hosting/printing | `FwRootSite : RootSite`, `PrintRootSite` | `Src/Common/Framework/FwRootSite.cs:26`, `Src/Common/SimpleRootSite/PrintRootSite.cs` | shell phase |
| Filters (spell-check matcher) | `BadSpellingMatcher` consumes `ISpellEngine` but lives in the Views-adjacent filter stack used by browse views | `Src/Common/Filters/BadSpellingMatcher.cs:41` | follows XMLViews browse migration |
| Reg-free COM packaging of the native Views/Kernel COM servers | Build tooling keeps native CLSIDs activatable for everything above | `Build/RegFree.targets`, `Build/mkall.targets:46`, `Src/Common/FieldWorks/BuildInclude.targets`, `Build/Src/FwBuildTasks/RegHelper.cs` | last to go; shrinks as consumers retire |

Bottom line [E+J]: native Views deletion is blocked by (at minimum) Interlinear/Texts & Words,
Discourse, XMLViews browse in every area, xWorks doc views, the DetailControls stack for
Notebook/Grammar/Lists, app-wide Views widgets, and Framework printing. This change only removes
the *Lexical Edit region's* dependence (8.3-8.5); repo-wide deletion needs the shell change plus
currently-unplanned area migrations.

---

## 8.7 Non-viewing native dependencies (custom linguistics / service / tool)

Classification rule applied: a dependency is a retained service/tool unless it owns display,
layout, hit testing, selection, or editor realization. For each row the "owns no viewing behavior"
claim was checked by locating its UI touchpoints.

| Dependency | Kind | File evidence | Owns display/layout/hit-test/selection/editor realization? |
|---|---|---|---|
| XAmple morphological parser (native `xample.dll`) | Custom linguistics engine | `Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleDLLWrapper.cs:37+` (`DllImport("xample.dll")`), `Src/LexText/ParserCore/XAmpleParser.cs:22`, `Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleCOMWrapper.vcxproj` | **No** [E] — returns parse/trace XML (`ParseResult.cs`); display happens in ParserUI (`XAmpleTrace.cs`, `WebPageInteractor.cs`), not in the engine |
| HermitCrab parser | Managed linguistics engine | `Src/LexText/ParserCore/HCParser.cs:22`, `HCLoader.cs` | **No** [E] — fully managed, same `IParser` contract |
| pcpatr / ToneParsFLEx / HCSynthByGloss utilities | External tool wrappers | `Src/Utilities/pcpatrflex/DisambiguateInFLExDB/PCPatrInvoker.cs`, `Src/Utilities/pcpatrflex/ToneParsFLExDll/XAmpleDLLWrapperForTonePars.cs`, `Src/LexText/ParserCore/PatrParserWrapper/`, `Src/Utilities/HCSynthByGloss/HCSynthByGlossLib/Synthesizer.cs` | **No** [E] — they own their *own* WinForms tool UI (`PcPatrFLExForm.cs`, `ToneParsFLExForm.cs`), which is standalone-tool UI, not Views render/editor infrastructure [J on the standalone-tool framing] |
| Encoding converters (ECInterfaces / SilEncConverters40) | Conversion service | `Src/FwCoreDlgs/AddCnvtrDlg.cs:12,16` (`using ECInterfaces; using SilEncConverters40;`), `Src/FwCoreDlgs/CnvtrPropertiesCtrl.cs`, importer consumers `Src/LexText/LexTextControls/LexImportWizard.cs`, `Src/LexText/Interlinear/LinguaLinksImport.cs` | **No** [E] — string-conversion API; the FwCoreDlgs converter dialogs are WinForms hosts around it (the `ConverterTester.SampleView : SimpleRootSite` preview is a *RootSite* dependency, counted in 8.6, not an EncConverters one) |
| ICU (icu.net managed + native data) | Unicode/collation service | `Src/ManagedLgIcuCollator/LgIcuCollator.cs:7-8` (`using Icu; using Icu.Collation;`), `CustomIcu` usage `Src/xWorks/CssGenerator.cs:242`, `Src/FXT/FxtDll/XDumper.cs:164`; native side consumed inside C++ views/lib (`Src/views/lib/LgUnicodeCollater.cpp`) | **No** [E] — normalization/collation/character properties. (Text *shaping* is Uniscribe/Graphite and is classified as viewing in 8.1, not here) |
| Spell-check engine (Hunspell via managed `SpellingHelper`) | Service | `Src/Common/RootSite/SpellCheckHelper.cs:15` (`using SIL.LCModel.Core.SpellChecking;`), `:257` (`SpellingHelper.GetSpellChecker`), `Src/Common/Filters/BadSpellingMatcher.cs:41` | **No for the engine** [E]. The *interop into native Views* (`VwRootBox::SetSpellingRepository`, `Src/views/VwRootBox.cpp:291`, `Src/Kernel/FwKernel.idh:81-87`) exists solely so the native renderer can draw squiggles — that drawing is a viewing behavior owned by Views (8.1/8.2), not by the spell engine |
| Expat / ParserObject (native XML parsing) | Native build-time/legacy utility | `Lib/src/xmlparse/xmlparse.h`, `Lib/src/ParserObject/CExPat.cpp`; referenced by native Views test/build (`Src/views/Test/TestViews.vcxproj`, `Src/views/Views.mak`) | **No** [E] — XML parsing only; retires with the native C++ tree, not with any UI work |
| Reg-free COM tooling | Build/packaging service | `Build/RegFree.targets`, `Build/mkall.targets:46`, `Build/Src/FwBuildTasks/RegHelper.cs`, `Src/Common/FieldWorks/BuildInclude.targets` | **No** [E] — manifests/activation for native COM servers (FwKernel, Views); it serves viewing code but owns none |

Confirmation: none of the above owns display, layout, hit testing, selection, or editor
realization. The only one that *touches* rendering is spell-check, and there the rendering half
(squiggles, suggestion menus) belongs to RootSite/Views (`SpellCheckHelper` builds the menu,
`VwRootBox` draws), so the engine remains a clean service. This confirmation is evidence-based for
the call paths cited; "no other hidden UI ownership" is a [J] negative claim backed by the searches
in this audit, not an exhaustive proof.

---

## 8.8 Service seams for retained linguistics engines

Rule (applies to every row): **the Avalonia region consumes results through these managed
contracts and never hosts, links, or re-implements the engines' UI/render/editor infrastructure.**
The `EngineIsolationAuditTests` assembly/symbol audit is the enforcement backstop; engines below
must stay out of `FwAvalonia`'s reference graph entirely — consumption happens in service layers
(xWorks/LexText) behind seam interfaces.

| Engine | Managed service seam (is / should be) | File evidence | Avalonia consumption rule |
|---|---|---|---|
| XAmple + HermitCrab parsers | `IParser` (engine contract) under `ParserScheduler`, consumed by UI through `ParserConnection` | `Src/LexText/ParserCore/IParser.cs`, `Src/LexText/ParserCore/ParserScheduler.cs:51`, `Src/LexText/ParserUI/ParserConnection.cs:21` | Avalonia surfaces request parses/trace results via `ParserConnection` (or a thin async port over it) and render results with Avalonia controls; never P/Invoke `xample.dll`, never embed the legacy trace HTML host (`WebPageInteractor.cs`) |
| Spell checking | `SpellingHelper` + `ISpellEngine` (managed Hunspell, `SIL.LCModel.Core.SpellChecking`); `IGetSpellChecker` is the *legacy native* seam only | `Src/Common/RootSite/SpellCheckHelper.cs:257`, `Src/Common/RootSite/RootSiteEditingHelper.cs:349`, native-only seam `Src/Common/ViewsInterfaces/Views.cs:7524` | Avalonia text editors query `ISpellEngine` directly for check/suggest/add and draw their own squiggles/menus; they MUST NOT call `SetSpellingRepository` or any `IGetSpellChecker` COM path (that exists only for `VwRootBox`) |
| Encoding converters | `ECInterfaces` (`IEncConverter`/`IEncConverters`) implemented by `SilEncConverters40` | `Src/FwCoreDlgs/AddCnvtrDlg.cs:12,16`, import consumers `Src/LexText/LexTextControls/LexImportWizard.cs` | Avalonia import/export flows call `ECInterfaces` conversions; converter *configuration* UI remains the external EncConverters/WinForms surface until separately rebuilt — Avalonia never embeds it |
| ICU | icu.net managed wrappers (`Icu.*`) and `CustomIcu` (SIL.LCModel.Core) | `Src/ManagedLgIcuCollator/LgIcuCollator.cs:7-8`, `Src/xWorks/CssGenerator.cs:242` | Avalonia uses `Icu`/`CustomIcu` for normalization/collation/character data only; text shaping/layout comes from Avalonia's own HarfBuzz/Skia stack, never from `Src/views/lib` engines |
| pcpatr / ToneParsFLEx / HCSynthByGloss | Process/DLL invoker utilities (`PCPatrInvoker`, `ToneParsInvoker`, `Synthesizer`) in standalone tool projects | `Src/Utilities/pcpatrflex/DisambiguateInFLExDB/PCPatrInvoker.cs`, `Src/Utilities/HCSynthByGloss/HCSynthByGlossLib/Synthesizer.cs` | Out of scope for the migrated region: these are standalone tools with their own UI; FwAvalonia takes no reference to them [J — should-be statement; no current seam needed] |
| Native TsString kernel (for completeness) | Managed TsString (`SIL.LCModel.Core.Text`/`KernelInterfaces`) + the `TsStringWrapper` clipboard contract | `Src/Common/SimpleRootSite/TsStringWrapper.cs`, seam bridge `FwTsStringClipboard` (task 3.13, xWorks) | Avalonia reads/writes TsStrings only through LCModel-managed APIs and the 3.13 clipboard seam; the native `Src/views/lib/TsString.cpp` kernel is never touched from FwAvalonia |

Seam status notes:
- `ParserConnection`, `ISpellEngine`, `ECInterfaces`, and `Icu`/`CustomIcu` are **existing** seams
  in product code today [E]. No new abstraction is required before Avalonia consumes them; what is
  required is keeping the consumption *outside* `FwAvalonia` assembly references (LCModel-free rule,
  enforced by `EngineIsolationAuditTests.ProductionAssembly_ReferencesNoNativeRenderLegacyOrDomainAssemblies`)
  and routing through the region's service interfaces in `Src/Common/FwAvalonia/Seams/ISeams.cs`.
- "Should-be" rows (pcpatr family) are judgment calls [J]; everything else is evidence-based.
