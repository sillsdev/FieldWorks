# WinForms-Free Lexeme Editor — Decisions and Burn-Down

Status: ACTIVE (decisions approved; waves landed; 2026-06-12 UX-semantics rework landed —
gear = configure-jump only, options-only `FwOptionPicker` flyouts, compact menu density)
Owner lane: lexical-edit-avalonia-migration, follows B11 (dynamic editors) and the
companion-strip coexistence lane recorded in `xml-retirement-blockers.md`.

## Goal

The lexicon Entry pane (LexEntry/Normal detail and everything it composes) renders and edits
with **zero WinForms controls inside the pane**. Two explicit carve-outs, both sanctioned by
existing rules:

1. **Modal dialogs stay WinForms during coexistence** (the `dialog-ownership.md` rule already
   forbids Avalonia modals while both frameworks run). Launching a legacy dialog from an
   Avalonia button keeps the *pane* WinForms-free; dialogs migrate in their own workstream.
2. **The host shell** (FwXWindow, panels, the WinForms↔Avalonia interop host) is the shell
   phase, not this lane.

Everything built here must be reusable by the next DataTree tools (Notebook, Morphology,
Grammar) — that constraint drove every decision below.

## The measured problem

Custom/dynamic slices actually used by the lexeme editor's part files (LexEntryParts.xml +
LexSenseParts.xml census, 2026-06-11):

| Legacy slice class | Count | What it is |
|---|---|---|
| `LexEd.EntrySequenceReferenceSlice` | 10 | components / variant-of / complex-form entry-reference vectors |
| `LexEd.GhostLexRefSlice` | 2 | ghost ("type to add") lane for lexical relations |
| `LexEd.LexReferenceMultiSlice` | 2 | lexical relations: one slice generated per relation type |
| `LexEd.MessageSlice` | 1 | Chorus Send/Receive notes bar (WinForms `NotesBarView` from the Chorus repo) |
| `LexEd.ReversalIndexEntrySlice` | 1 | reversal index entries (native-Views editing) |
| `DetailControls.AudioVisualSlice` | 1 | pronunciation media |
| `LexEd.LexEntryChangeHandler` | 1 | not UI — a change handler; no migration needed |
| MSA "Grammatical Info. Details" launchers (`MsaInflectionFeatureListDlgLauncherSlice` etc.) | per-sense section | value + "..." button opening a WinForms feature-structure dialog |

The morphology rule-formula/interlinear monsters (family 3 in the B11 census) do **not**
appear in the lexeme editor — they ride gate 6.13/8.x and do not block this goal.

## Decisions

### D1. One plugin contract for every remaining custom editor (`IRegionEditorPlugin`)

A registry in xWorks maps the **legacy layout identity** (the `class` attribute the importer
already carries on the typed node, e.g. `SIL.FieldWorks.XWorks.LexEd.MessageSlice`) to a
plugin that builds an **Avalonia control** for (object, node, edit context). The composer
consults the registry while walking; a claimed node composes as a `RegionFieldKind.Custom`
row carrying the plugin's control factory; `LexicalEditRegionView` renders the factory's
control in-tree at the slice's real position.

Why keyed by legacy class name: the layouts are the contract — keying the registry off the
same attribute means zero layout edits per migration, per-tool reuse for free (Notebook's
layouts route through the identical mechanism), and a measurable burn-down (registry coverage
vs. census).

Resolution order in the composer: **plugin registry → companion strip (WinForms coexistence)
→ unsupported row**. A class graduates: unsupported → companion → plugin. It never moves the
other way (pinned by the governance test, D5).

### D2. Messages: native Avalonia notes bar on LibChorus's UI-free core

`SIL.Chorus.LibChorus` (netstandard2.0) owns the `.ChorusNotes` data model
(`Chorus.notes.AnnotationRepository`, annotations keyed/ref'd by entry guid); only the bar's
pixels live in the WinForms-only `SIL.Chorus.App`. We re-render the bar (`ChorusNotesPlugin` +
an Avalonia notes control) against LibChorus directly and retire the companion strip's only
designated class.

Compatibility contract (pinned by tests): the plugin reads and writes the **same files**
(`Lexicon.fwstub.ChorusNotes`, the same stub-creation behavior as legacy `MessageSlice`) with
the **same ref-URL/key shapes** legacy wrote, so FLExBridge/S&R and the legacy bar see one
notes store. Tests round-trip: a legacy-format annotation file is read by the plugin; a note
added by the plugin is read back through a fresh LibChorus `AnnotationRepository`.

Upstreaming an Avalonia bar to the Chorus repo stays open as an option; building against
LibChorus (not FieldWorks internals) keeps that door open without blocking on cross-repo
coordination.

### D3. Entry-reference vectors ride the existing ReferenceVector lane

`EntrySequenceReferenceSlice` (components/variants/complex forms) becomes an editable
`ReferenceVector` row: current entries/senses as items (headword text), remove in-pane, with
the 6.3 separator-bar/add-slot affordance. **Add** uses type-ahead search over the lexicon
(headword prefix match via the entry repository) rather than materializing the whole lexicon
as options — possibility lists enumerate; lexicons search.

`LexReferenceMultiSlice` (relations) is explicitly **next in this lane**: the multi-slice
generates one row per relation type and needs the relation-type model walk; it reuses the
same row/affordance once that walk exists. Recorded as the lane's follow-up so it cannot
silently fall off the list.

Status (wave 3, 2026-06-12): LANDED. The composer recognizes non-virtual entry/sense-target
reference vectors (signature LexEntry/LexSense, or CmObject under the
`EntrySequenceReferenceSlice` layout identity — ComponentLexemes/PrimaryLexemes) and composes
an editable `ReferenceVector` row whose add slot carries
`LexicalEditRegionField.SearchOptions` (headword/citation/lexeme-form case-insensitive prefix
search over the entry repository, self + already-present excluded, capped at 50);
`FwReferenceVectorField` renders the search flyout; writes ride `sda.Replace` in the fenced
session. The class moved to the burn-down's `LaneAbsorbedClassNames` ("D3 ReferenceVector
lane"). Legacy-coupling findings, pinned by `EntryReferenceVectorTests`: LCModel does NOT
couple ComponentLexemes ADDs, so the setter carries the legacy launcher's coupling
(first-component → PrimaryLexemes when empty; non-derivative → ShowComplexFormsIn, LT-12285
dedup); LCModel DOES clear PrimaryLexemes on remove (no twin needed), and ShowComplexFormsIn
is retained on remove exactly like the legacy slice's plain removal path. Deferred in-lane:
the slice's VIRTUAL back-ref fields (ComplexFormEntries, Subentries,
VisibleComplexFormBackRefs, VariantFormEntries) still render read-only — their writes land on
the other entry's LexEntryRef (the legacy `AddNewObjectsToProperty` overrides) and ride the
D3 follow-up together with the relations walk.

Top-level ghost lexical-reference rows landed in the same lane on 2026-06-12:
`GhostLexRefSlice` now composes as an editable search-backed `ReferenceVector` row instead of
the unsupported placeholder. The empty `Components` row is always available; the hidden
`Variant of` ghost row appears under Show Hidden Fields. On first add, the setter creates the
missing `LexEntryRef` with the legacy semantics (`krtComplexForm` vs `krtVariant`, seeded
unspecified type, complex-form primary-lexeme coupling, one undo step). Pinned by
`GhostLexRefSliceTests`; the class moved from `ExplicitlyDeferredClassNames` to
`LaneAbsorbedClassNames` ("D3 ghost reference-vector lane").

### D4. Dialog launchers: Avalonia row + legacy dialog through a host service

MSA "Grammatical Info. Details" launchers (and any future `*DlgLauncherSlice`) render as an
Avalonia value row plus a launcher button. The button calls an `ILegacyDialogLauncher`
service the host (RecordEditView) injects — the only place allowed to touch WinForms — which
runs the existing dialog and commits through the normal fenced-session path. The pane stays
WinForms-free; the dialog migrates later without touching the pane again.

`AudioVisualSlice` (pronunciation media) takes the same launcher pattern for "play/choose
file" until the media lane (6.12) lands a native player. `ReversalIndexEntrySlice` is
Views-based text editing and explicitly rides gate 6.13 — documented, not forgotten.

Status (wave 4, 2026-06-11): LANDED.

- **Routed:** `MsaInflectionFeatureListDlgLauncherSlice`, `PhonologicalFeatureListDlgLauncherSlice`,
  and `AudioVisualSlice` are claimed in the default registry by `LauncherRegionPlugin`
  (`DialogLauncherPlugins.cs`) and listed in `LexemeEditorBurnDown.LauncherRoutedClassNames`
  ("D4 launcher lane"); AudioVisualSlice graduated out of ExplicitlyDeferred. The MSA/phon
  launchers live in MSA/FsFeatStruc part files beyond the LexEntryParts/LexSenseParts census —
  registered anyway, forward-looking (per-sense "Grammatical Info. Details", Grammar tools).
  Each claimed node renders `FwDialogLauncherField`: the value as read-only text (MSA = the
  feature structure's ShortName, exactly the launcher view's CmAnalObjectVc kfragShortName;
  phon = LongName per its deParams; AudioVisual = the media file's AbsoluteInternalPath) plus
  the legacy "..." button.
- **Seam:** `ILegacyDialogLauncher.LaunchFor(obj, node) → bool changed` plus a
  `RegionEditorServices` parameter object, threaded RecordEditView → `Compose(..., services)` →
  plugin factories via the back-compatible `IServiceAwareRegionEditorPlugin` extension of the
  D1 contract (classic plugins untouched; services default null). Without a host service the
  button renders DISABLED with a tooltip and the value still shows.
- **What the host service really does (not a stub):** `WinFormsLegacyDialogLauncher`
  (RecordEditView, the sanctioned carve-out) settles any open fenced session first (a legacy
  dialog opens its own UOW; doing that under the fence's write lock would throw), then: MSA →
  creates `MsaInflectionFeatureListDlg` reflectively through `DynamicLoader`
  ("LexTextControls.dll" — xWorks cannot reference it; same load lane as the layouts), resolves
  (fs, flid) exactly like the legacy slice's Install, calls the same SetDlgInfo overloads and
  title/prompt/link strings HandleChooser uses, and ShowDialogs over the host form; OK means the
  dialog committed in its own UOW; Yes posts the FollowLink posEdit jump (the legacy LT-7167
  fallback — the sibling-VectorReferenceLauncher scan needs a DataTree and is intentionally not
  replicated). Phonological → `PhonologicalFeatureChooserDlg`, same recipe (HandleJump on Yes).
  AudioVisual → plays the file like `AudioVisualLauncher.HandleChooser` (SoundPlayer for
  RIFF/WAVE, OS default app otherwise); returns false (no data change).
- **Refresh assumption VERIFIED:** the dialogs commit through their own UOW, which raises
  PropChanged; `AvaloniaRegionRefreshController` subscribes to the real bus and its IsRelevant
  walk covers any object owned by the displayed entry (MSAs and their feature structures are),
  so the region recomposes after the dialog closes (scheduled via the host's UI-thread queue) —
  the launcher never refreshes explicitly.
- **Remaining in-lane:** the dialog launch itself is WinForms-modal and verified by code-path
  parity + the seam tests (fake launcher), not by automated UI tests; a live-FLEx pass over a
  sense's "Grammatical Info. Details" section is the outstanding manual check. The MSA detail
  sections are not yet reached by the composer's walk (the sense layout binds `MsaCombo`, not
  the MoStemMsa Normal layout), and the pronunciation `MediaFiles` part ref does not resolve
  (no `LexPronunciation-Detail-MediaFiles` part ships) — when those walks land, the launcher
  rows light up with zero further plugin work. Choose/replace media file (vs play) rides the
  media lane (6.12) like the native player.

### Post-wave gap fixes (user-reported, 2026-06-11): LANDED
### UX-semantics rework (user direction, 2026-06-11/12): LANDED

- **Gear = CONFIGURE, never choose:** the gear on a chooser/reference-vector row DIRECTLY
  dispatches the list-editor jump (`RegionGearChrome` → `RegionLinkRequest` →
  `RecordEditView.OnRegionLinkRequested` → settle + mediator `FollowLink` with
  `FwLinkArgs(tool, Guid.Empty)`); no flyout, no context menu ever opens from a gear. The
  gear renders ONLY when a list-edit target resolves: (a) the node's imported
  `<chooserInfo><chooserLink type="goto">` wins (B7's import lane — `ViewNode.ChooserLinks`,
  canonical-JSON `chooserLinks` block; all 95 shipped links are goto; first goto wins when
  several ride one row); (b) else the composer DERIVES the tool from the row's possibility
  list, mirroring the legacy generic jump lane: `LinkListener.FollowActiveLink`
  (Src/xWorks/LinkListener.cs:507-517) publishes `GetToolForList`, answered by
  `AreaListener.GetToolForList` (Src/LexText/LexTextDll/AreaListener.cs:388-418) walking the
  lists-area tools' clerks (`Lists/areaConfiguration.xml` recordList owner=/property= ↔
  `Lists/Edit/toolConfiguration.xml` tools); the composer mirrors that clerk table statically
  (`FullEntryRegionComposer.ListEditorToolByOwnerField`, keyed (owner class, owning field) —
  so morph type/semantic domains/usages/status gears work too) and derives ownerless custom
  lists as Name-without-spaces + "Edit" (`AreaListener.GetCustomListToolName`). A list with
  no resolvable editor tool → NO gear on that row. chooserInfo's other facets
  (title/text/guicontrol FlatList) remain the measured B7 remainder.
- **"+" and single-select chooser click = OPTIONS ONLY:** option flyouts carry zero link
  items (`RegionLinkChrome` deleted). Every option dropdown is the ONE compact filterable
  picker `FwOptionPicker` (Region/FwOptionPicker.cs): a selection-filter panel (light
  border), filter TextBox on top (auto-focused on open, ksSearchPrompt watermark) over a
  VIRTUALIZED list capped at `PocDensity.OptionListMaxHeight` (320), item padding pinned to
  the legacy WinForms menu density (`PocDensity.OptionItemPadding`, (6,2)), hierarchy via the
  existing Depth indent. Typing filters live (case-insensitive contains; the search-backed
  vector forwards the query to the D3 `SearchOptions` delegate); Down/Up move, Enter commits
  the highlighted option (first match by default), Esc closes, click commits. Staging is
  unchanged (`TrySetOption` / `TryAddReferenceItem`).
- **Lexeme Form gear: REVERTED.** Gears never open menus, so the text-row slice-menu gear
  (the earlier gap fix) is gone; the slice menu (`menu="mnuDataTree-LexemeForm"` etc.) stays
  on right-click only — the label/value right-click lanes are unchanged.
- **Context-menu density:** `RegionMenuFlyout` items pin the explicit compact legacy padding
  (`PocDensity.MenuItemPadding`/`MenuItemMinHeight`), not the Fluent defaults; long menus
  keep the presenter's scrolling.

### D5. Governance: the burn-down is enforced by tests, not intentions

- The companion-strip designated set may only **shrink** (pinned: a test asserts its exact
  contents; growing it requires consciously editing the test with justification).
- The plugin registry's coverage of the lexeme-editor census above is pinned: a test asserts
  which classes are plugin-routed / launcher-routed / explicitly deferred (with the gate
  they ride). A new custom slice appearing in the lexeme-editor layouts fails the test until
  it is classified.
- `xml-retirement-blockers.md` B11 row references this document as the lexeme-editor lane.

### D6. Explicitly out of scope here

Rich TsString editing (gate 6.13), `ReversalIndexEntrySlice` (rides 6.13), the WinForms host
shell, WinForms dialogs themselves, morphology/grammar family-3 editors, native media player
(6.12). Each is listed where it rides; none are needed for the pane itself to be
WinForms-free apart from 6.13's text-fidelity caveat, which is tracked as the program-wide
long pole.

## Sequence (each wave lands green before the next)

1. **Wave 1 — plugin contract + governance:** `IRegionEditorPlugin`, registry, composer/view
   routing (`RegionFieldKind.Custom` + control factory), resolution-order tests, burn-down
   governance tests.
2. **Wave 2 — Messages:** `ChorusNotesPlugin` + Avalonia notes control on LibChorus; file/ref
   compatibility tests; companion designated set shrinks to empty.
3. **Wave 3 — entry-reference vectors:** `EntrySequenceReferenceSlice` → ReferenceVector with
   type-ahead entry search; composer/edit-context tests over the memory cache.
4. **Wave 4 — dialog launchers:** `ILegacyDialogLauncher` seam + MSA launcher plugin (+
   AudioVisual via the same pattern); seam-level tests with a fake launcher.

## Reuse statement

The registry, the resolution order, the launcher seam, the notes control, and the type-ahead
reference editor are all keyed by layout vocabulary and LCModel metadata — none of them know
they are in the lexicon. Notebook (RnGenericRec has its own custom slices) and Morphology
adopt them by registering plugins, not by re-architecting.
