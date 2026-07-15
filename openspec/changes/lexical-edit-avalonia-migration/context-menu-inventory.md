# Lexical Edit Context-Menu Inventory (Section 15 work record, 2026-06-10)

Complete inventory of the legacy right-click logic reachable from the Lexicon Edit lexical entry
detail view, produced by sub-agent sweeps of the shipped configuration (`*.fwlayout` files carry
layout part refs — earlier sweeps that filtered on `*.xml` missed them), the xCore menu engine,
and the command dispatch chain. This is the migration checklist §15 implements against.

## 1. Menu bindings reachable from `LexEntry` detail `Normal`

| Menu id | Bound at (file:line) | Bound via | Shown on |
|---|---|---|---|
| mnuDataTree-Sense (+ -Hotlinks) | LexSense.fwlayout:6 | `<part ref="HeavySummary" menu= hotlinks=>` | every sense header |
| mnuDataTree-Subsenses | LexSense.fwlayout:47 | Senses(SubSenses) part ref | subsenses section |
| mnuDataTree-VariantForms (+ -Hotlinks) | LexEntry.fwlayout:33 | VariantFormsSection part ref | Variants section |
| mnuDataTree-AlternateForms (+ -Hotlinks) | LexEntry.fwlayout:38 | AlternateFormsSection part ref | Allomorphs section |
| mnuDataTree-Help | LexEntry.fwlayout:43,48; many slices | Grammatical/Publication sections, most fields | most rows |
| mnuDataTree-Etymology (+ -Hotlinks) | LexSense.fwlayout:73 (NormalSummary); LexEntryParts.xml:29 | etymology summary/seq | Etymology |
| mnuDataTree-Pronunciation | LexEntryParts.xml:25 | Pronunciations seq | Pronunciation |
| mnuDataTree-AlternateForm | LexEntryParts.xml:119 | AlternateForms seq | individual allomorph |
| mnuDataTree-Allomorph / -AffixProcess | MorphologyParts.xml:66,79,94 | form slices | stem/affix allomorph |
| mnuDataTree-VariantForm (+Context) | LexEntryParts.xml:853; MorphologyParts.xml:297 | variant refs/form | individual variant |
| mnuDataTree-ExtendedNote (+ -Hotlinks) | LexSense.fwlayout:64 | NormalSummary | extended note |
| mnuDataTree-Examples / -Example | LexSenseParts.xml:622 | Examples seq | examples section/item |
| mnuDataTree-CitationFormContext | LexEntryParts.xml:15 | `contextMenu=` (in-string) | citation form value |
| mnuDataTree-LexemeFormContext | MorphologyParts.xml:221 | `contextMenu=` (in-string) | lexeme form value |
| mnuReorderVector | LexEntryParts.xml:824,838,844 | ref-vector slices | complex forms/subentries |
| mnuDataTree-Object | always appended (DTMenuHandler.cs:1744-1745) | — | every slice menu |
| mnuDataTree-MultiStringSlice | appended for multistring slices | — | in-string menus |

**Add-a-sense answer:** `mnuDataTree-Sense` (DataTreeInclude.xml:442) bound on the sense's
`HeavySummary` part ref (LexSense.fwlayout:6) — items `CmdDataTree-Insert-SenseBelow` ("Insert
Sense") and `CmdDataTree-Insert-SubSense` ("Insert Subsense"); also `mnuDataTree-Sense-Hotlinks`
(:461) and `mnuDataTree-Subsenses` (:543). The Senses part ref in LexEntry.fwlayout:32 itself has
NO menu attribute — the binding lives inside the per-sense layout, so synthesized per-sense
headers must inherit the ITEM layout's root binding (15.3).

## 2. mnuDataTree-Sense contents (DataTreeInclude.xml:442-460), in order
Insert Example · Find example sentence... · Insert Extended Note · Insert Sense ·
Insert Subsense · Insert Picture (defaultVisible=false) · — · Show Sense in Concordance · — ·
Move Sense Up · Move Sense Down · Demote · Promote · — · Merge Sense into... ·
Move Sense to a New Entry · Delete this Sense and any Subsenses.
(Other menus' item lists recorded in the agent sweep; all ids resolve in the shipped window
configuration — proven by `Compose_EveryMenuBinding_ResolvesInTheShippedWindowConfiguration`.)

## 3. Command dispatch chain (sense commands)

| Command | Message | Handler | Target resolution |
|---|---|---|---|
| CmdDataTree-Insert-SenseBelow/-SubSense | DataTreeInsert | DTMenuHandler.OnDataTreeInsert (DTMenuHandler.cs:428) → Slice.HandleInsertCommand (Slice.cs:1967) | CurrentSlice(.Object/owner search) |
| CmdDataTree-Delete-Sense | DataTreeDeleteSense | LexEntryMenuHandler.OnDataTreeDeleteSense (:179) → Slice.HandleDeleteCommand | CurrentSlice.Object |
| CmdDataTree-Merge-Sense | DataTreeMerge | DTMenuHandler.OnDataTreeMerge (:1016) | CurrentSlice.Object |
| CmdDataTree-MakeSub-Sense / Promote | DemoteSense/PromoteSense | LexEntryMenuHandler (:192/:279) | CurrentSlice.Object |
| CmdDataTree-MoveUp/Down-Sense | Move{Up,Down}ObjectInSequence | DTMenuHandler (:1142/:1223) | CurrentSlice.Object |
| CmdDataTree-Split-Sense | DataTreeSplit | DTMenuHandler (:1051) | CurrentSlice.Object |

Lexicon Edit menuHandler subclass: `SIL.FieldWorks.XWorks.LexEd.LexEntryMenuHandler`
(LexEdDll.dll), configured in Lexicon\Edit\toolConfiguration.xml:46-48. The SAME DTMenuHandler
infrastructure serves Grammar/Notebook/Lists/Words — changes here migrate those tools for free.

**Hidden-adapter risk found:** `DTMenuHandler.OnDisplayDataTreeInsert` (DTMenuHandler.cs:865)
gates on `m_dataEntryForm.Visible` — with the hidden command-routing adapter tree, Insert
Sense/Subsense render DISABLED even though execution (which never checks visibility) would
succeed. Every other sense command checks only model state. Fix: 15.4.

## 4. xCore menu engine recipe (for the Avalonia renderer)

- Materialize: `new ChoiceGroup(mediator, propertyTable, adapter, List<XmlNode>, null)`
  (ChoiceGroup.cs:266) + `PopulateNow()` (:453) — fully headless, creates no WinForms UI.
- Iterate: ChoiceGroup IS an ArrayList (ChoiceRelatedClass : ArrayList, ChoiceGroup.cs:17);
  members are `SeparatorChoice` (Choice.cs:679, subclass of ChoiceBase — test it FIRST),
  nested `ChoiceGroup` (submenu; PopulateNow it), and `ChoiceBase` (CommandChoice,
  Bool/List/StringPropertyChoice).
- Display state: `choice.GetDisplayProperties()` → `UIItemDisplayProperties` { Text ('_' marks
  the accelerator), Enabled, Visible, Checked } (IUIAdapter.cs:181-237) — drives the mediator
  `Display*` round-trip, i.e. the SAME enable/check logic the WinForms menu shows.
- Execute: `choice.OnClick(sender, EventArgs)` (Choice.cs:143) — CommandChoice invokes the
  command through the mediator; property choices toggle the PropertyTable.
- The WinForms adapter's only routing-relevant extra is the optional TemporaryColleagueParameter
  add (MenuAdapter.cs:287) — the legacy slice path passes null (DTMenuHandler.cs:1746-1749), so
  the Avalonia renderer can skip it.

## 5. Double-menu defect (live app)

Avalonia `TextBox` ships a theme-default ContextFlyout (Cut/Copy/Paste) opened by
`ContextRequested` on right-button RELEASE; the bridge handled only `PointerPressed`, so both
the built-in flyout and the bridged menu appeared. Fix: bridged boxes get `ContextFlyout = null`
plus a handled `ContextRequested` (15.2).
