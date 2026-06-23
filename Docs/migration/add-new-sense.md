# Add New Sense (legacy `AddNewSenseDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.AddNewSenseDlg` (`Src/LexText/LexTextControls/AddNewSenseDlg.cs`) |
| **Area / tool** | Interlinear / Sandbox › morpheme/sense combo › "Add new sense…" |
| **Primitive(s)** | owned-control form (FwMultiWsTextField gloss + FwMsaGroupBox grammatical-info) |
| **Canonical reference** | InsertEntryDialog (owned-control form hosting a gloss field + FwMsaGroupBox) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/AddNewSenseDialogView.axaml(.cs)` + `AddNewSenseDialogViewModel.cs` @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user add a new sense to an existing lexical entry from the interlinear Sandbox morpheme
combo: type a gloss (one row per analysis WS) and set the grammatical info (MSA), creating the sense
in one undoable step. Opens from the Sandbox sense/morph combo's "Add new sense" item.

## What it looks like
<!-- CAPTURE: launch legacy FLEx (UIMode=Legacy), open an interlinear text, click a morpheme,
     pick "Add new sense…" from the combo. See .claude/skills/fieldworks-winapp. -->
![Add New Sense – initial](./images/add-new-sense-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Read-only citation form / headword shown at top (from the entry).
- [ ] Editable gloss: one `FwMultiWsTextField` row per analysis WS.
- [ ] Owned `FwMsaGroupBox` for grammatical info: POS choosers, slot picker, inflection-class picker, inflection-feature editor.
- [ ] OK is gated when the gloss is empty (legacy `AddNewSenseDlg_Closing` shows `ksFillInGloss` and cancels OK).
- [ ] No gate on the MSA box (always valid).
- [ ] Help button shown only when a help topic is available.

## Migration gotchas
- WS/RTL: the gloss is multi-WS — each analysis WS gets its own row and must round-trip the right TsString.
- Owned-control hosting: the stub mounts `FwMsaGroupBox` and stages edits into `InMemoryRegionEditContext`.
- The stub header marks this an "MSA-port Stage 5 replacement for the legacy AddNewSenseDlg in New-UI mode".
- Stub markers to honour: `// Stage 3 wires the feature dialogs` and `// §19b Stage 3: wire the inline
  create-feature / add-value affordances (replacing the deferred no-op)` — the inline create-feature/add-value
  affordances are wired through `LcmInflectionFeatureCreateWiring`, verify they re-attach.

## Wiring
- Legacy call site(s): `Src/LexText/Interlinear/SandboxBase.ComboHandlers.cs` — the `using (new AddNewSenseDlg(...))`
  block in the Legacy branch (below line 2495 in the same method).
- The Avalonia path branched on `UIMode=New` here before back-out: `SandboxBase.ComboHandlers.cs:2492` —
  `LcmAddNewSenseDialogLauncher.Show(...)`, inside `if (AvaloniaOptionsDialogLauncher.ShouldUseAvaloniaOptionsDialog(uiMode))`
  (the `UIMode` test is at line 2488). Launcher: `LcmAddNewSenseDialogLauncher`
  (`Src/LexText/LexTextControls/LcmAddNewSenseDialogLauncher.cs`).
- Re-wiring target: this launcher/host should re-enter the Avalonia surface behind `UIMode=New`;
  Legacy keeps `AddNewSenseDlg`.
