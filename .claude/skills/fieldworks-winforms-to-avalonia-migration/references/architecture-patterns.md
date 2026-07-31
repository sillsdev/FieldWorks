# Established Migration Architecture Patterns

Decisions already made by the lexical-edit migration. Each section gives the
decision, why it was made, the canonical code, and gotchas. The durable
contracts are synced to `openspec/specs/`; the full decision record lives in
git history (the `lexical-edit-avalonia-migration` change folder, removed from
the tree) and in PR #964's provenance comment.

Contents:

1. Typed view-definition IR (the long-term contract)
2. Region model + composer (boundary above DataTree)
3. Explicit surface selection per host
4. Owned dense controls (control-selection decisions)
5. Plugin registry for custom slice classes
6. Writing-system behavior (font, RTL, keyboard, multi-WS)
7. Dialog ownership and modality across the interop boundary
8. Undo/redo, edit sessions, and refresh
9. Validation
10. Custom fields and ghost rows
11. Localization strategies
12. Density and performance
13. Headless integration-test harness (scenarios & workflows)

## 1. Typed view-definition IR (the long-term contract)

**Decision.** XML Parts/Layouts are compiled into a typed
`ViewDefinitionModel` (one `ViewNode` per field carrying StableId, editor
kind, writing system, visibility, expansion, custom-field placeholder
metadata, accessibility id, and localization key). Avalonia consumes the IR,
never raw XML. XML is an import format during transition, not the runtime
abstraction; the retirement path is deterministic JSON
(`ViewDefinitionJsonSerializer.cs`) plus customer override patches.

**Why.** Keeps customer layout customizations alive, creates a clean
DI/test boundary, enables off-thread compilation and snapshot-based parity
tests, and gives XML a retirement path.

**Canonical code.** `Src/Common/FwAvalonia/ViewDefinition/` —
`ViewDefinitionModel.cs`, `XmlLayoutImporter.cs`, `ViewDefinitionCompiler.cs`
(caches by immutable source-snapshot fingerprint),
`ViewDefinitionJsonSerializer.cs`, `LayoutImportCoverage.cs`.
Tests: `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionTests.cs`,
`LayoutImportCoverageTests.cs`, `CanonicalJsonTests.cs`.

**Gotchas.** Compilation must stay deterministic (same source snapshot →
identical IR) because parity snapshots key off it. Track element/attribute
import coverage explicitly; an unimported construct must surface as a
diagnostic node, not vanish.

- **Layout choice is a 4-key resolution, not 1.** A class can have many layout
  variants selected by a `layoutChoiceField`/choice-guid (e.g. 11 `RnGenericRec`
  variants). `LayoutSourceLoader` originally collapsed them to one (Analysis),
  which silently broke Notebook/Lists edit composition. Index layouts by choice
  (`IndexLayoutsByChoice`/`SelectLayoutForChoice`), thread the `choiceGuid` through
  the composer (`ResolveLayoutChoiceGuid`), and memo by it. Tests:
  `LayoutChoiceResolutionTests`.
- **Multi-child parts must import every child element.** A part whose body is an
  enable/disable pair — `<if Disabled="true">…</if><if Disabled="false">…</if>` —
  imported as only its first child, so the active-state variant vanished (Name/
  Description/Active on compound rules). `IPartResolver.ResolvePartContents` returns
  `part.Elements()` (all children); the importer makes one node per child, each `<if>`
  a Conditional that `WalkConditional` evaluates. This is shared infra — re-run full
  `./test.ps1` after touching it.

## 2. Detail model + composer (boundary above DataTree)

**Decision.** The migration boundary sits at the region-model layer above
`DataTree`, not inside it. A composer walks the compiled IR the way legacy
DataTree walks layouts and emits a region model (renderable fields keyed by
IR StableId) plus an edit context. DataTree internals are never extracted —
they are deleted at the end of coexistence, so extracting them is throwaway
work.

**Canonical code.** `Src/xWorks/Avalonia/Composer/DetailComposer.cs` (walks IR,
emits `ComposedDetail`),
`Src/Common/FwAvalonia/Detail/DetailModel.cs`,
`DetailModelProjector.cs`, `IDetailEditContext.cs`,
`DataTree.cs`.
Tests: `DetailModelTests.cs`, `DetailEditingTests.cs`,
`DetailViewingParityTests.cs` in `Src/Common/FwAvalonia/FwAvaloniaTests/`.

**Gotchas.** The region model is presentation data, not LCModel objects —
it is projected from `IDetailValueProvider` style seams so it can be built
and tested off-thread without WinForms or a real project.

- **The composer is class-general — compose any `ICmObject`, not just LexEntry.**
  `Compose(ICmObject, layout, choiceGuid)` + `DetailEditContextBase`/
  `ComposedDetailEditContext` on `ICmObject` let notebookEdit / posEdit / Lists / the
  Grammar rule tools all ride one composer. New surfaces opt in by registering their
  tool in `EditSurfaceRegistry`, not by editing the composer.
- **Generic reference editing mirrors legacy's metadata-driven model — keep it
  global, gate on `IsVirtual`.** Editable reference vectors/atomic choosers
  (`AddGenericReferenceVector`/`AddGenericAtomicChooser` via `ReferenceTargetCandidates`)
  live in the shared `WalkOtherField` fallthrough. Legacy editing is itself fully
  metadata-driven — `SliceFactory` reuses ONE `AtomicReferenceSlice`/`ReferenceVectorSlice`
  per field type across ALL classes with ZERO per-class allow-list — so narrowing the
  generic path to specific classes is an anti-pattern. The one required guard:
  `if (flid == 0 || _mdc.get_IsVirtual(flid)) return null;` — exactly the editability gate
  legacy `VectorReferenceView.cs:440` uses (`!get_IsVirtual`), so back-refs and derived
  collections stay read-only and never get a blind `Replace` (data-corruption risk).
- **Shared-composer changes are high blast-radius.** Anything in the composer/importer
  fallthrough touches LIVE lexicon/notebook/pos. Measure the radius (lexicon/notebook/
  back-ref suites) and run full `./test.ps1`; the only expected failures are the known ~38
  environmental data-sentinel ones (main-repo path), not regressions.
- **Probe LCModel string grammars before composing a value editor.** A GenDate editor
  that round-tripped through `ToLongString()` silently corrupted year-granular dates
  (§19i data-loss). Emit the canonical granular form the model's `TryParse` accepts, not a
  display string. Verify against the actual LCModel API, not assumptions.

## 3. Explicit surface selection per host

**Decision.** Every host that can show legacy or Avalonia UI resolves an
explicit `HostUiBehavior`: supported Avalonia, explicit legacy fallback, or
blocked. No silent fallback. The active host must never drive hidden legacy
DataTree/menu/renderer infrastructure except through approved baseline
adapters.

**Canonical code.**
`Src/Common/FwAvalonia/EditSurfaceSelectionService.cs`,
`EditSurfaceResolver.cs`, `EditSurfaceFactory.cs`,
`Src/Common/FwAvalonia/Seams/ActiveHostContract.cs` (approved-adapter
whitelist).
Tests: `EditSurfaceResolverTests.cs`,
`SurfaceAndHostContractTests.cs`,
`Src/xWorks/xWorksTests/Avalonia/Hosting/RecordEditViewActiveHostContractTests.cs`.

**Gotchas.** "Convenience" calls into legacy internals while Avalonia is
visible (for example, to harvest metadata) defeat the boundary — the
contract tests exist to catch exactly that.

## 4. Owned dense controls (control-selection decisions)

**Decision.** Build FieldWorks-owned row/field controls on top of stock
virtualization primitives instead of adopting a stock property grid or
TreeDataGrid:

- Detail view (DataTree replacement): owned slice list over
  `ListBox`/`VirtualizingStackPanel` — flatten in the model, virtualize
  with stock primitives, own the row.
- Browse/table (XMLViews replacement): owned virtualized table — flattened
  row list + shared column header + owned cell layout. (The Phase-1
  implementation was removed from PR #964 in review; the pattern returns
  with the browse-table PR.)
- Bounded popup trees (≤500 items): stock `TreeView` with an explicit
  item-count ceiling, validated at 100%/150% DPI.
- Unbounded trees: the owned flattened virtualized list with
  expander/indent row decorations.

**Why.** Stock grids fit poorly with nested senses, multi-WS alternatives,
custom choosers, dense rows, and FieldWorks keyboard behavior; owning the
row keeps the UI framework out of domain semantics. TreeDataGrid was
rejected on licensing and editing/automation gaps (see pivot triggers in
`seam-catalog.md` — revisit if those facts change).

**Canonical code.** `Src/Common/FwAvalonia/Detail/DetailModel.cs`
(`DetailFieldKind`: Text, Chooser, Unsupported, Header, ReferenceVector,
Custom, StructuredText, Literal); field controls in
`Src/Common/FwAvalonia/Detail/FwFieldControls.cs` (`FwMultiWsTextField`,
`FwChooserField`, `FwReferenceVectorField`, `FwDialogLauncherField`),
`FwOptionChooser.cs`, `DetailMenuFlyout.cs`, `HoverReveal.cs`,
`DetailFocusMemory.cs`. Picture, command, and closed-enum-combo editors have
no dedicated `DetailFieldKind` today — the composer routes them to the
labeled Unsupported row (`DetailComposer.WalkUnsupported`, dispatched from
`EditorKindMap.ClassifyDetailFieldKind`).

## 5. Plugin registry for custom slice classes

**Decision.** Legacy layouts reference custom slice classes by name (for
example `SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice`). A plugin registry maps
those same class identities to factories that build Avalonia controls.
Resolution order is exactly two steps: **plugin → labeled "Unsupported" row.**
There is no launcher or companion-strip fallback. Never silent mis-render: a slice with no
plugin (and not absorbed by a composer route such as the D2 reference-vector route) composes as
a labeled Unsupported row — and the visible set of Unsupported rows IS the conversion worklist.
Keying by legacy class identity means zero layout edits and a measurable burn-down (census vs.
registry coverage).

**Canonical code.** `Src/xWorks/Avalonia/Plugins/SlicePlugins.cs` (`ISlicePlugin`,
`SlicePluginBuildContext`, `SlicePluginRegistry`, `RegisterBuiltins`). The single
registered plugin is `ReversalIndexEntryPlugin` — the native-conversion exemplar. A future PR
converts another Unsupported slice by adding its plugin the same way.
Tests: `Src/xWorks/xWorksTests/Avalonia/Plugins/LexemeEditorInventoryTests.cs` (census + resolution order).

**Converting a custom slice to a native Avalonia editor (worked example —
`ReversalIndexEntryPlugin`).** The forward path needs no layout edits:

1. **Claim the legacy `class=` identity.** Implement `ISlicePlugin.LegacyClassName` with the
   layout's class attribute (e.g. `SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice`) and register
   the plugin in `RegisterBuiltins`. The composer's plugin step now claims that node instead of
   dropping it to Unsupported.
2. **Render in-tree.** `BuildControl(SlicePluginBuildContext)` builds an Avalonia control for
   (target object, typed node, cache) and returns it; the view places it in the value column at the
   slice's real position. Reuse the owned controls where you can — the reversal editor projects the
   sense's reversal forms into a `DetailField` and hands it to `FwMultiWsTextField`.
3. **Ride the fenced edit context.** Route the editor's writes through
   `SlicePluginBuildContext.EditContext` (an `IDetailEditContext`) so plugin edits land as ONE
   undoable step on the region's shared session, exactly like every other row. The reversal editor
   wraps the host context in a small `IDetailEditContext` that routes `TrySetText`/`TrySetRichText`
   to the matching reversal entry's `ReversalForm`, staging on the host's `DetailEditContextBase`.
4. **Graduate the Unsupported row.** With the plugin registered, the slice that previously rendered
   an Unsupported worklist row now renders the native editor — the row leaves the worklist.

**Projectors and write-back belong in xWorks, not FwAvalonia or the domain assembly.**
A plugin's view stays LCModel-free and binds an LCModel-free projection; the projector that reads
LCModel and the write-back that mutates it live in xWorks (which references both LCModel and
FwAvalonia). Putting a projector in `Morphology` would be circular.

**When migrating a new surface:** census its custom slice classes first,
check the registry for existing plugins, and add plugins (with tests) for
the rest. Everything unclaimed renders Unsupported until its plugin lands — that is the burn-down.

## 6. Writing-system behavior (font, RTL, keyboard, multi-WS)

**Decision.** Every text field renders per-writing-system rows: WS
abbreviation gutter + value box, with font family/size, flow direction
(RTL/LTR), and keyboard activation projected from LCModel WS metadata.
Keyboard switches on focus (legacy `EditingHelper.SetKeyboardForWs`
behavior). OpenType features ship via HarfBuzz; native Graphite is never
loaded on the Avalonia path — Graphite-dependent writing systems are
classified and warned, not blocked.

**Canonical code.** `FwMultiWsTextField` in
`Src/Common/FwAvalonia/Detail/FwFieldControls.cs`; `DetailWsValue`
(WsAbbrev, FontFamily, FontSize, RightToLeft, WsTag) in
`DetailModel.cs`.
Tests: `TreeNodeTemplateAndRtlTests.cs`, `VisualParityAndDensityTests.cs`.

**Gotchas.** Never assume one font, one direction, or one script per
field. Test mixed-script content at 100% and 150% DPI with real fonts.

## 7. Dialog ownership and modality across the interop boundary

**Decision.** During coexistence there is one UI thread and one message
loop. Rules (durable contract synced to `openspec/specs/avalonia-lifetime/spec.md`;
implementation: `Src/Common/FwAvalonia/AvaloniaDialogHost.cs`):

- Anything modal is a WinForms dialog, owned by the hosting WinForms
  top-level form (`Control.FindForm()` of the host) — never `null`, never
  an Avalonia handle. Avalonia modal windows are not used (unsupported on
  the 11.x coexistence path).
- Record the focused Avalonia control before `ShowDialog` and restore focus
  explicitly after close.
- Use Avalonia flyouts inside the hosted surface, not free popup windows
  (mixed-DPI positioning).
- No cross-boundary Tab order between WinForms siblings and the Avalonia
  surface; own focus inside the surface.
- No WinForms modeless tool windows owned by an Avalonia surface.

## 8. Undo/redo, edit sessions, and refresh

**Decision.** Edits ride a fenced `IEditSession`
(Active → Saved/Canceled → Disposed) wrapping an LCModel undo task — one
undoable action per save regardless of field count. Transient text undo
stays local to the focused TextBox. Global undo/redo routes through
`IUndoRedoCoordinator` to the LCModel action handler, then refreshes the
region. Cancel rolls back the session and must not create a committed undo
action. Refresh coordination mirrors legacy
`DoNotRefresh`/`RefreshListNeeded` semantics via the refresh-coordinator
seam.

**Canonical code.** `Src/Common/FwAvalonia/Seams/`,
`SeamImplementations.cs`, `RefreshCoordinator.cs`.
Tests: `SeamTests.cs`, `DetailEditingTests.cs`.

**As-built (2026-06-23, ARCH-02).** `IUndoRedoCoordinator` is NOT yet a named
abstraction in `Src/Common/FwAvalonia/Seams/`. For the shipped LexEntry path, global undo/redo is
handled directly by `DetailEditContextHolder.AttachUndoGuard` /
`OnDoingUndoOrRedo` (`DetailEditContextHolder.cs:121-179`), coupled to
`IActionHandlerExtensions` and `System.Windows.Forms.Form.Deactivate`: on a
global undo/redo it settles + cancels the open fenced session to avoid LCModel
UOW write-lock re-entrancy. The fenced `IEditSession` decision above is real
(`DetailEditContextBase`/`DetailEditContextHolder`); only the *coordinator
abstraction* is deferred. Extract `IUndoRedoCoordinator` when a second host
needs it (Phase 2).

**Gotchas.** Two undo stacks produce user-visible data weirdness. Never
disable global undo while a session is dirty — route it. Defer PropChanged
fan-out during multi-field edits until commit/cancel.

## 9. Validation

**Decision (target design).** Validation runs over immutable presentation
snapshots, not live LCModel. Errors are ordered by presentation/focus order
(deterministic for headless tests), skip unmaterialized lazy items, and carry
node id, object/flid, severity, localized message key + args, and
accessibility text. Only severity=Error blocks save; warnings do not. Stale
async results (from older snapshots) are discarded.

**As-built (2026-06-23, ARCH-02).** `IValidationService` does NOT exist yet.
The shipped validation is a `virtual DetailEditContextBase.Validate()`
returning `List<string>` over **live** LCModel
(`DetailEditContextBase.cs:101-128`, e.g.
`entry.LexemeFormOA?.Form?.VernacularDefaultWritingSystem?.Text`) — pluggable
by subclass + per-rule (the CmPossibility Name/Abbreviation rule), but with no
severity model (all messages are Error-equivalent), no node-id/flid metadata,
and no immutable-snapshot determinism. Treat the snapshot-based service above
as the Phase-2 target, not current behavior. Do NOT claim deterministic
snapshot validation until the service exists.

**Canonical code (as-built).** `DetailEditContextBase.Validate()` (virtual) in
`Src/xWorks/Avalonia/DetailEditContextBase.cs`; per-rule validation hooks wired by the
composer in `Src/xWorks/Avalonia/Composer/DetailComposer.cs`. The
`IValidationService` seam in `Src/Common/FwAvalonia/Seams/` is planned,
not present.

## 10. Custom fields and ghost rows

**Decision.** Stored view definitions contain `CustomFieldPlaceholder`
nodes (typed equivalent of legacy `customFields="here"`), expanded from
LCModel metadata at compile time. Custom fields are never baked into stored
definitions (they differ per project). Ghost rows ("type to add"
placeholders) are runtime UI state managed by the composer/model, never
stored layout structure.

**Canonical code.** `ViewDefinitionModel.cs` (placeholder node kind),
composer expansion in `DetailComposer.cs`.
Tests: `DetailCustomFieldRenderingTests.cs`.

## 11. Localization strategies

**Decision.** Three strategies:

- **Field labels** (text originating in XML configuration) resolve through
  the StringTable strategy (`XmlUtils.GetLocalizedAttributeValue`,
  `strings-{locale}.xml`) at render time; the IR carries `LocalizationKey`
  per node, never baked English.
- **FieldWorks-owned UI text** — WinForms and Avalonia alike (Save, Cancel,
  validation, unsupported-row text, dialog labels, accessible names) —
  lives in the owning project's `.resx`, with translations shipped as
  Crowdin-built satellite assemblies, per the Avalonia localization docs
  and repo convention. `FwAvaloniaStrings`/`FwAvaloniaDialogsStrings`
  resolve via `ResourceManager` over the neutral resx (the English source
  of truth); `AvaloniaLocalizationTests` pins accessor and resx together.
- **L10NSharp/XLIFF** only for Palaso/FlexBridge/Chorus-supplied UI; never
  new FieldWorks-owned usage, and never borrowed `Palaso`/`Chorus` ids for
  FieldWorks-owned strings.

*(Decision revised 2026-07-23 in PR #964 review: the earlier
XLIFF-accessor approach for FieldWorks-owned Avalonia strings was reversed
— FieldWorks-owned UI text uses `.resx`, per the Avalonia localization docs
and repo convention; L10NSharp remains only for
Palaso/FlexBridge/Chorus-supplied UI.)*

**Gotchas.** SDK-style csprojs need an explicit `<RootNamespace>` element
for stable satellite resource names — see `fieldworks-localization-review`
for the canonical statement of that rule and the ProjectLocalizer details.
English-on-Avalonia where legacy shows translations is a parity failure,
not cosmetics.

## 12. Density and performance

**Decision.** Visual *density* (row spacing, gutters, box heights) is owned
by FieldWorks density constants, measured against legacy WinForms
baselines. Performance budgets are measured, not estimated: capture legacy
init/populate/total timings with the characterization harness, then hold
Avalonia to within 20% of legacy total (or record an explicitly accepted
delta in the region manifest).

> **Density parity ≠ look parity (migration-program decision 2026-06-15).** The
> program is chartered to *upgrade the look*: it adopts a modernized Fluent-based
> theme rather than mimicking the legacy WinForms look and feel. Keep this
> distinction sharp — *density* (information per screen, alignment, gutters) stays
> matched to legacy baselines and is asserted by the parity evidence types;
> *styling* (colors, control templates, focus visuals, corner radii) may
> intentionally diverge. The visual-parity evidence type therefore checks
> density/layout, not pixel-for-pixel appearance. The density tokens and
> per-surface border/font rules that this parity is measured against live in
> `fieldworks-avalonia-ui/references/style-system.md`, even where styling
> intentionally diverges.

**Canonical code.** `Src/Common/FwAvalonia/FwAvaloniaDensity.cs`;
legacy harness
`Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeRenderTests.cs`;
generated thresholds `DataTreeTimingBaselines.json` (same directory; gitignored,
regenerated per machine, not checked into the repo).
Tests: `VisualParityAndDensityTests.cs`.

**Gotchas.** Validate virtualization against the large fixtures (253-slice
detail, 10k-row browse) before committing a control choice. Include the
150% DPI path — it exposes real layout regressions.

## 13. Headless integration-test harness (scenarios & workflows)

**Decision (2026-06-16).** Avalonia **headless integration tests that walk real
scenarios/workflows are the front-and-center verification style** — preferred
over deferring to "live verification" or unit tests that poke handlers. Build in
**two fidelities** (hosting Avalonia vs. standing up the real domain differ in
cost/risk): a **surface-workflow** layer in an Avalonia-headless assembly
(`FwAvaloniaTests`) — co-host the owned control(s) and drive them through
page-object drivers (filter/clear/select/type/commit), asserting observable
state and round-trips like select→detail and edit→refresh; and a **real-domain**
layer (`xWorksTests`) — a real `RecordClerk` over an in-memory LCModel cache
asserting the real list narrows/reorders/restores, replacing "needs live
verification" for domain claims. A read-only grid needs **neither**: cell/sort/
filter extraction runs through `CollectorEnv : IVwEnv` (managed, SDA-only, no
`RootBox`), so the cutover is seam re-sourcing, not a text-engine rewrite.

**Canonical code.** `Src/Common/FwAvalonia/FwAvaloniaTests/Workflows/HeadlessWorkflowHarness.cs`
(`HeadlessStage`, `DetailEditorDriver`). An earlier revision also carried a browse-table driver
(`BrowseTableDriver`) with its surface-workflow exemplar
(`FwAvaloniaTests/BrowseEditorIntegrationTests.cs`) and a real-domain exemplar
(`Src/xWorks/xWorksTests/ClerkRoutedFilterTests.cs`); both were removed with the browse-table
surface (control-exemplar-map.md §3.6) — do not cite them as present. `DetailEditorDriver` itself
currently has no consuming exemplar test; the next surface that adopts this harness becomes the
exemplar. The requirements this harness satisfies are synced to
`openspec/specs/lexical-edit-parity-automation/spec.md`.

**Gotchas.** Never add `[assembly: AvaloniaTestApplication]` to `xWorksTests`
(it changes the host for ~1400 tests) — Avalonia hosting lives only in dedicated
Avalonia-headless assemblies; the full-stack co-host (real clerk → adapter →
view) belongs in a *new* such project. On the restored test base, create domain
objects directly (a nested `NonUndoableUnitOfWorkHelper.Do` throws "Nested tasks
are not supported"). Stand the entries clerk up with the `ConfiguredXHTMLGeneratorTests`/
`RecordListTests` recipe (`MockFwX(App|Window)`, `<recordList owner='LexDb'
property='Entries'/>`, then `ActivateUI` + `SetSuppressingLoadList(false)` +
`ReloadList`) or it stays empty. Pump the dispatcher after every acting verb.
