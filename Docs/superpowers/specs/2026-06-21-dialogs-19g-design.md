# §19g Remaining Functional Dialogs — Scope & Design

Phase-1 §19g: migrate the remaining FUNCTIONAL dialogs reachable from
lexical-edit/browse to Avalonia behind the New-UI gate, WinForms preserved.
Decisions delegated to the implementer ("do NOT pause for approval"); this doc
records the scope decision and the design so the choices are auditable.

## Architecture (unchanged kit)

Each dialog = **View (`.axaml` UserControl, `fwDialogRoot`, tokens,
AutomationIds) + ViewModel (`DialogViewModelBase`, compiled bindings) +
launcher** (`AvaloniaDialogLauncher<TState,TViewModel,TPayload>` subclass at the
LexText product edge; `AvaloniaDialogHost.ShowModal` hosts it in a WinForms-owned
modal `Form`). View is LCModel-free; model reads/writes live in the launcher in
one UOW. Strings append to `FwAvaloniaDialogsStrings`. Gated at the WinForms call
site by `AvaloniaOptionsDialogLauncher.ShouldUseAvaloniaOptionsDialog(uiMode)`.

## Scope decision (the key call)

| Dialog | Decision | Rationale |
| --- | --- | --- |
| Delete-confirmation | **Shipped fully** | Bounded; FwMessageBox-style + `DeletionTextTSS`/`CanDelete`. Gated at both LexReference delete sites. |
| LexReferenceDetails | **Shipped fully** | Plain name+comment Form. Gated at `EditReferenceDetails`. |
| Special-character insert | **Shipped (net-new core)** | No WinForms truth dialog (OS charmap shellout); built an in-app Avalonia Unicode picker. Legacy shellout preserved + noted. |
| Writing System properties | **Shipped (bounded core)** | name/abbr/font/RTL/sort managed core; full `FwWritingSystemSetupModel` (SLDR/converters/merge/advanced) = PARITY note at the call site. |
| Lex Options tabs | **Already covered** | `LexOptionsDlg`'s 4 tabs (Interface/Plugins/Privacy/Updates) are exactly the migrated `OptionsDialog`; gate already wired. No new code. |
| **Styles dialog** | **PARITY §19g (→ Stage 9)** | `FwStylesDlg(IVwRootSite, …)` is Views-engine-coupled; the dialog-kit hard rule (dialog-conversion.md §0) routes such dialogs to the document engine, NOT this kit. §19c already ships named-style APPLY. Precise note at the call site. |
| Reversal-entry / Occurrence / Import-Export | **PARITY §19g** | `BaseGoDlg`-rooted tool surface / inline morphology-concordance picker / multi-page OS-file wizards — each larger than a bounded slice. Precise notes at the call sites. |

## Testing (T0–T5)

T0 research note: `openspec/changes/lexical-edit-avalonia-migration/dialogs-test-research.md`.
T1 VM/view (headless, per-stage PNGs) in `FwAvaloniaDialogsTests`; T1/T4 real-cache
launcher cores in `LexTextControlsTests`; T5 PNGs READ + six-question review.
