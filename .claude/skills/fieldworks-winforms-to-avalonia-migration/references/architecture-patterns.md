# Established Migration Architecture Patterns

Decisions already made by the lexical-edit migration. Each section gives the
decision, why it was made, the canonical code, and gotchas. Provenance for
every decision lives in `openspec/changes/lexical-edit-avalonia-migration/`
(if that change has been archived, look under `openspec/changes/archive/`).

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
11. Localization lanes
12. Density and performance

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
`LayoutImportCoverageTests.cs`, `BrowseAndCanonicalJsonTests.cs`.

**Gotchas.** Compilation must stay deterministic (same source snapshot →
identical IR) because parity snapshots key off it. Track element/attribute
import coverage explicitly; an unimported construct must surface as a
diagnostic node, not vanish.

## 2. Region model + composer (boundary above DataTree)

**Decision.** The migration boundary sits at the region-model layer above
`DataTree`, not inside it. A composer walks the compiled IR the way legacy
DataTree walks layouts and emits a region model (renderable fields keyed by
IR StableId) plus an edit context. DataTree internals are never extracted —
they are deleted at the end of coexistence, so extracting them is throwaway
work.

**Canonical code.** `Src/xWorks/FullEntryRegionComposer.cs` (walks IR,
emits `ComposedEntryRegion`),
`Src/Common/FwAvalonia/Region/LexicalEditRegionModel.cs`,
`LexicalEditRegionMapper.cs`, `IRegionEditContext.cs`,
`LexicalEditRegionView.cs`.
Tests: `RegionModelTests.cs`, `RegionEditingTests.cs`,
`RegionViewingParityTests.cs` in `Src/Common/FwAvalonia/FwAvaloniaTests/`.

**Gotchas.** The region model is presentation data, not LCModel objects —
it is projected from `IRegionValueProvider` style seams so it can be built
and tested off-thread without WinForms or a real project.

## 3. Explicit surface selection per host

**Decision.** Every host that can show legacy or Avalonia UI resolves an
explicit `HostUiBehavior`: supported Avalonia, explicit legacy fallback, or
blocked. No silent fallback. The active host must never drive hidden legacy
DataTree/menu/renderer infrastructure except through approved baseline
adapters.

**Canonical code.**
`Src/Common/FwAvalonia/LexicalEditSurfaceSelectionService.cs`,
`LexicalEditSurfaceResolver.cs`, `LexicalEditSurfaceFactory.cs`,
`Src/Common/FwAvalonia/Seams/ActiveHostContract.cs` (approved-adapter
whitelist).
Tests: `LexicalEditSurfaceResolverTests.cs`,
`SurfaceAndHostContractTests.cs`,
`Src/xWorks/xWorksTests/RecordEditViewActiveHostContractTests.cs`.

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
  row list + shared column header + owned cell layout
  (`Src/Common/FwAvalonia/Region/LexicalBrowseView.cs`).
- Bounded popup trees (≤500 items): stock `TreeView` with an explicit
  item-count ceiling, validated at 100%/150% DPI.
- Unbounded trees: the owned flattened virtualized list with
  expander/indent row chrome.

**Why.** Stock grids fit poorly with nested senses, multi-WS alternatives,
custom choosers, dense rows, and FieldWorks keyboard behavior; owning the
row keeps the UI framework out of domain semantics. TreeDataGrid was
rejected on licensing and editing/automation gaps (see pivot triggers in
`seam-catalog.md` — revisit if those facts change).

**Canonical code.** `Src/Common/FwAvalonia/Region/FwFieldControls.cs`
(`RegionFieldKind`: Text, Chooser, Boolean, Image, Command,
ReferenceVector, Custom), `FwOptionPicker.cs`, `RegionMenuFlyout.cs`,
`HoverReveal.cs`, `RegionFocusMemory.cs`.

## 5. Plugin registry for custom slice classes

**Decision.** Legacy layouts reference custom slice classes by name (for
example `SIL.FieldWorks.XWorks.LexEd.MessageSlice`). A plugin registry maps
those same class identities to factories that build Avalonia controls.
Resolution order: plugin → companion-strip WinForms coexistence →
explicit "unsupported" row. Never silent mis-render. Keying by legacy class
identity means zero layout edits and measurable burn-down (census vs.
registry coverage).

**Canonical code.** `Src/xWorks/RegionEditorPlugins.cs`,
`Src/xWorks/ChorusNotesPlugin.cs`, registry contracts in
`Src/Common/FwAvalonia/Region/LexicalEditRegionModel.cs`.
Tests: `Src/xWorks/xWorksTests/DialogLauncherPluginTests.cs`,
`LexemeEditorBurnDownTests.cs`, `MessagesCompanionLaneTests.cs`.

**When migrating a new surface:** census its custom slice classes first,
check the registry for existing plugins, and add plugins (with tests) for
the rest. Add each new plugin to the burn-down tracking.

## 6. Writing-system behavior (font, RTL, keyboard, multi-WS)

**Decision.** Every text field renders per-writing-system rows: WS
abbreviation gutter + value box, with font family/size, flow direction
(RTL/LTR), and keyboard activation projected from LCModel WS metadata.
Keyboard switches on focus (legacy `EditingHelper.SetKeyboardForWs`
behavior). OpenType features ship via HarfBuzz; native Graphite is never
loaded on the Avalonia path — Graphite-dependent writing systems are
classified and warned, not blocked.

**Canonical code.** `FwMultiWsTextField` in
`Src/Common/FwAvalonia/Region/FwFieldControls.cs`; `RegionWsValue`
(WsAbbrev, FontFamily, FontSize, RightToLeft, WsTag) in
`LexicalEditRegionModel.cs`.
Tests: `TreeSpikeAndRtlTests.cs`, `VisualParityAndDensityTests.cs`.

**Gotchas.** Never assume one font, one direction, or one script per
field. Test mixed-script content at 100% and 150% DPI with real fonts.

## 7. Dialog ownership and modality across the interop boundary

**Decision.** During coexistence there is one UI thread and one message
loop. Rules (provenance:
`openspec/changes/lexical-edit-avalonia-migration/dialog-ownership.md`):

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

**Canonical code.** `Src/Common/FwAvalonia/Seams/ISeams.cs`,
`SeamImplementations.cs`, `RefreshCoordinator.cs`.
Tests: `SeamTests.cs`, `RegionEditingTests.cs`.

**Gotchas.** Two undo stacks produce user-visible data weirdness. Never
disable global undo while a session is dirty — route it. Defer PropChanged
fan-out during multi-field edits until commit/cancel.

## 9. Validation

**Decision.** Validation runs over immutable presentation snapshots, not
live LCModel. Errors are ordered by presentation/focus order (deterministic
for headless tests), skip unmaterialized lazy items, and carry node id,
object/flid, severity, localized message key + args, and accessibility
text. Only severity=Error blocks save; warnings do not. Stale async results
(from older snapshots) are discarded.

**Canonical code.** `IValidationService` in
`Src/Common/FwAvalonia/Seams/ISeams.cs`; composer wiring in
`Src/xWorks/FullEntryRegionComposer.cs`.

## 10. Custom fields and ghost rows

**Decision.** Stored view definitions contain `CustomFieldPlaceholder`
nodes (typed equivalent of legacy `customFields="here"`), expanded from
LCModel metadata at compile time. Custom fields are never baked into stored
definitions (they differ per project). Ghost rows ("type to add"
placeholders) are runtime UI state managed by the composer/model, never
stored layout structure.

**Canonical code.** `ViewDefinitionModel.cs` (placeholder node kind),
composer expansion in `FullEntryRegionComposer.cs`.
Tests: `RegionCustomFieldRenderingTests.cs`.

## 11. Localization lanes

**Decision.** Two lanes:

- **Field labels** resolve through the legacy StringTable lane
  (`XmlUtils.GetLocalizedAttributeValue`, `strings-{locale}.xml`) at render
  time; the IR carries `LocalizationKey` per node, never baked English.
- **Product messages** (Save, Cancel, validation, unsupported-row text)
  live in `Src/Common/FwAvalonia/FwAvaloniaStrings.resx` with translator
  comments.

**Gotchas.** SDK-style csprojs need an explicit `<RootNamespace>` element
or the Crowdin satellite-assembly build
(`Build/Src/FwBuildTasks/Localization/ProjectLocalizer.cs`) fails — verify
when adding any new Avalonia project. English-on-Avalonia where legacy
shows translations is a parity failure, not cosmetics.

## 12. Density and performance

**Decision.** Visual *density* (row spacing, gutters, box heights) is owned
by FieldWorks density constants, measured against legacy WinForms
baselines. Performance budgets are measured, not estimated: capture legacy
init/populate/total timings with the characterization harness, then hold
Avalonia to within 20% of legacy total (or record an explicitly accepted
delta in the region manifest).

> **Density parity ≠ look parity (migration-program decision 2026-06-15).** The
> program is chartered to *upgrade the look*: it adopts a modernized Fluent-based
> theme rather than mimicking legacy WinForms chrome. Keep this distinction sharp —
> *density* (information per screen, alignment, gutters) stays matched to legacy
> baselines and is asserted by the parity lanes; *styling* (colors, control
> templates, focus visuals, corner radii) may intentionally diverge. The visual
> parity lane therefore checks density/layout, not pixel-for-pixel chrome.

**Canonical code.** `Src/Common/FwAvalonia/Poc/PocDensity.cs`;
legacy harness
`Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeRenderTests.cs`;
committed thresholds `DataTreeTimingBaselines.json` (same directory).
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
(`HeadlessStage`, `BrowseTableDriver`, `LexicalEditorDriver`), exemplar
`FwAvaloniaTests/BrowseEditorIntegrationTests.cs`, real-domain
`Src/xWorks/xWorksTests/ClerkRoutedFilterTests.cs`. Provenance + the per-phase
expansion plan:
`openspec/changes/shared-editable-virtualized-table/headless-integration-harness.md`.

**Gotchas.** Never add `[assembly: AvaloniaTestApplication]` to `xWorksTests`
(it changes the host for ~1400 tests) — Avalonia hosting lives only in dedicated
Avalonia-headless assemblies; the full-stack co-host (real clerk → adapter →
view) belongs in a *new* such project. On the restored test base, create domain
objects directly (a nested `NonUndoableUnitOfWorkHelper.Do` throws "Nested tasks
are not supported"). Stand the entries clerk up with the `ConfiguredXHTMLGeneratorTests`/
`RecordListTests` recipe (`MockFwX(App|Window)`, `<recordList owner='LexDb'
property='Entries'/>`, then `ActivateUI` + `SetSuppressingLoadList(false)` +
`ReloadList`) or it stays empty. Pump the dispatcher after every acting verb.
