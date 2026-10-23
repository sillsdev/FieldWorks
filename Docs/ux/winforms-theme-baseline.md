# WinForms theme baseline

The measured or coded look of the WinForms interface, set beside the Avalonia
theme token that now carries each value. The [UX guide](README.md) states what
each element must achieve; this table records where its look started, so
divergence from WinForms can be measured instead of argued from memory.

How to read and keep it:

- **WinForms value** is what the WinForms code draws, cited by file and line
  (paths under `Src/`). "Measured" means the value was read from a rendered
  baseline rather than from a constant.
- **Avalonia token** names the key in `Src/Common/FwAvaloniaTheme/Tokens/`. A
  "—" means that area isn't converted yet; the converting change fills it in.
- **Match / differs**: a *differs* row must cite a
  [decision register](README.md#decision-register) entry or a guide rule that
  calls for the change. A difference with neither is a bug. *Check* marks a row
  whose comparison still needs confirming.
- Add a row when a conversion introduces a token for an element that WinForms
  already drew.

## Detail views

| Element | WinForms value | Source | Avalonia token | Match / differs |
| --- | --- | --- | --- | --- |
| Label column width | 150 px | `Common/Controls/DetailControls/DataTree.cs:113` | `DataTree.LabelColumnWidth` (150) | Match |
| Label / value splitter width | 5 px | `Common/Controls/DetailControls/Slice.cs:854` | `DataTree.SplitterWidth` (5) | Match |
| Width at which labels switch to abbreviations | 60 px | `Common/Controls/DetailControls/Slice.cs:53` | `DataTree.WsAbbrevWidth` (60) | Check: the token uses this value for the writing-system abbreviation column, a different element |
| Field label color | `SystemColors.ControlDarkDark` (#696969) | `Common/Controls/DetailControls/Slice.cs:1466` | `FwLabelBrush` (#696969) | Match |
| Field label font | Sans serif, 10 pt | `Common/Controls/DetailControls/Slice.cs:80` | `FwSurfaceFontSize` (11) | Check: Avalonia font sizes are in device-independent pixels, so 11 is about 8.25 pt, not 10 pt |
| Writing-system abbreviation color | #404040 (measured) | `Common/Controls/DetailControls/DetailControlsTests/DataTreeRenderTests.DataTreeRender_multiws.verified.png` | `FwWsAbbrevBrush` (#404040) | Match |
| Rule between rows | 1 px, `Color.LightGray` | `Common/Controls/DetailControls/DataTree.cs:1999` | `FwSliceRuleBrush` (LightGray), `DataTree.SliceRuleHeight` (1) | Match |
| Rule above a section header | 3 px, `Color.LightGray` (1 + `HeavyweightRuleThickness` 2) | `Common/Controls/DetailControls/DataTree.cs:199, 2000` | `FwSectionRuleBrush` (LightGray), `DataTree.SectionRuleHeight` (2) | Differs: 2 px, no register entry |
| Space above a section header rule | 10 px (`HeavyweightRuleAboveMargin`) | `Common/Controls/DetailControls/DataTree.cs:201` | `DataTree.SectionRuleMargin` (top 6) | Differs: 6 px, no register entry |
| Expand / collapse box outline | `SystemColors.ControlDark` (#A0A0A0) | `Common/Controls/DetailControls/SliceTreeNode.cs:334` | `FwTreeBoxBorderBrush` (#A0A0A0) | Match |
| Section command link text | `Color.Blue`, sans serif 10 pt, underlined | `Common/Controls/DetailControls/SummaryCommandControl.cs:45, 168` | `SemiColorLink` (through `FwAvaloniaDensity.HotlinkBrush`) | Differs: register, section command look (Provisional, LT-22693) |
| Unavailable section command | `Color.Gray` | `Common/Controls/DetailControls/SummaryCommandControl.cs:169` | `FwDisabledOptionBrush` (Gray) | Match |
| Selected item background | `kclrBackgroundSelRow` (#D7E6FF) | `Common/Controls/XMLViews/XmlBrowseViewBaseVc.cs:894` | `FwSelectedRowBrush` (#D7E6FF) | Match |
| Context menu item height | About 22 px (measured) | — | `DataTree.MenuItemMinHeight` (22) | Match |

## Browse views

| Element | WinForms value | Source | Avalonia token | Match / differs |
| --- | --- | --- | --- | --- |
| Row height | About 17 px (measured) | — | `DataTree.BrowseRowMinHeight` (18) | Check: 1 px taller |
| Tentative / selected row fill | RGB(200, 255, 255) (`kclrTentative`) | `Common/Controls/XMLViews/XmlBrowseViewBaseVc.cs:51` | — | Guide rule *Analysis trust colors* requires a color distinct from the approved-guess color |
| Grid line | — | — | `FwBrowseGridLineBrush` (#DCDCDC) | Check: WinForms source not yet cited |

## Dialogs

| Element | WinForms value | Source | Avalonia token | Match / differs |
| --- | --- | --- | --- | --- |
| OK / Cancel / Help button size | 75 × 23 px | `FwCoreDlgs/FwStylesDlg.resx:516-517, 546-547` | `DialogMinControlHeight` (Semi `SemiHeightControlSmall`, 24) | Differs: 24 px minimum height, per the target-size rule in [*Keyboard and assistive access*](README.md#keyboard-and-assistive-access) |
| Gap between dialog buttons | 6 px | `FwCoreDlgs/FwStylesDlg.resx:513-514, 543-544` | `DialogButtonStripGap` (Semi `SemiSpacingTight`) | Check: Semi step value not yet confirmed |
| Interface font | MS Sans Serif, 8.25 pt | Form designer defaults (for example `FwCoreDlgs/FwStylesDlg.resx`) | `FwSurfaceFontSize` (11 px, about 8.25 pt) | Match |

## Interlinear text

| Element | WinForms value | Source | Avalonia token | Match / differs |
| --- | --- | --- | --- | --- |
| Guess based on the user's approvals | RGB(150, 255, 255) | `LexText/Interlinear/InterlinVc.cs:446` | — | — |
| Several approved analyses could apply | RGB(255, 255, 50) | `LexText/Interlinear/InterlinVc.cs:454` | — | — |
| Machine (parser) guess | RGB(234, 220, 186) | `LexText/Interlinear/InterlinVc.cs:462` | — | — |
| Background of the word being analyzed | RGB(235, 235, 220) | `LexText/Interlinear/SandboxBase.SandboxVc.cs:27` | — | — |
| Border of the word being analyzed | `Color.Blue` | `LexText/Interlinear/SandboxBase.SandboxVc.cs:29` | — | — |
| Editable lines of the word being analyzed | `Color.White` | `LexText/Interlinear/SandboxBase.SandboxVc.cs:30` | — | — |

## Status bar

| Element | WinForms value | Source | Avalonia token | Match / differs |
| --- | --- | --- | --- | --- |
| "Filtered" indicator | `Brushes.Yellow` | `xWorks/RecordClerk.cs:2907` | — | — |
| Parser-development indicator | `Brushes.Tan` | `xWorks/RecordClerk.cs:2930` | — | — |
| "Sorted by" indicator | `Brushes.Lime` | `xWorks/RecordClerk.cs:2960` | — | — |
