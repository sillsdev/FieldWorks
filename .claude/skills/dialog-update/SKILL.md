---
name: dialog-update
description: "Keep a FieldWorks dialog's WinForms (old) and Avalonia (new) implementations in sync whenever either is changed. Use whenever you add, edit, or review a control, field, button, validation rule, apply-order step, or string in a dialog that exists in BOTH a WinForms form (e.g. LexOptionsDlg) and its Avalonia replacement (e.g. LexOptionsDlgView + AvaloniaOptionsDialogLauncher) — even for a one-line change. Also use before claiming a migrated dialog is at parity, and when deciding whether a difference between the two is an approved divergence."
---

# Dialog Update — Keep Old (WinForms) and New (Avalonia) In Sync

During the WinForms → Avalonia coexistence, many dialogs exist **twice**: the
legacy WinForms form and the migrated Avalonia view. Both ship, and which one
runs is chosen at launch (usually by `UIMode`). A change to one that is not
mirrored in the other is a **divergence bug**, not a style choice.

## Non-negotiable rule

**NEVER diverge the two UIs without explicit product-owner approval.** This
includes behavior, controls, layout, wording, validation, apply order, and
which settings persist. "The new one is nicer this way" is not approval. If you
believe a divergence is warranted, stop and ask; do not encode it and move on.

Approved divergences must be recorded (see *Divergence register* below) with a
one-line reason and the approver — otherwise the next person reads it as a bug
and "fixes" it, thrashing the code.

## The dialog pairs (start here)

| Concern | WinForms (old) | Avalonia (new) |
|---|---|---|
| Tools → Options | `Src/LexText/LexTextControls/LexOptionsDlg.cs` (+ `.Designer.cs`, `.resx`) | `Src/Common/FwAvaloniaDialogs/LexOptionsDlgView.axaml(.cs)` + `LexOptionsDlgViewModel.cs` + `LexOptionsDlgState.cs`; edge: `Src/LexText/LexTextControls/Avalonia/AvaloniaOptionsDialogLauncher.cs` |
| Insert Entry | `Src/LexText/LexTextControls/InsertEntryDlg.cs` | `Src/Common/FwAvaloniaDialogs/InsertEntryDlgView.axaml.cs` + `InsertEntryDlgViewModel.cs` |
| Add New Sense | `Src/LexText/LexTextControls/AddNewSenseDlg.cs` | `Src/Common/FwAvaloniaDialogs/AddNewSenseDlgView.axaml.cs` + `AddNewSenseDlgViewModel.cs` |
| MSA Creator | `Src/LexText/LexTextControls/MsaCreatorDlg.cs` | `Src/Common/FwAvaloniaDialogs/MsaCreatorDlgView.axaml.cs` + `MsaCreatorDlgViewModel.cs` |
| Phonological Feature Chooser | `Src/LexText/LexTextControls/PhonologicalFeatureChooserDlg.cs` | `Src/Common/FwAvaloniaDialogs/FeatureChooserDialogView.axaml.cs` + `FeatureChooserDialogViewModel.cs` |
| Entry Go (jump to entry) | `Src/LexText/LexTextControls/EntryGoDlg.cs` | `Src/Common/FwAvaloniaDialogs/EntryGoDialogView.axaml.cs` + `EntryGoDialogViewModel.cs` |
| Possibility/list chooser (FilterBar "choose") | `Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs` (+ `Src/Common/Controls/DetailControls/SimpleListChooser.cs`) | `Src/Common/FwAvaloniaDialogs/ChooserDialogView.axaml.cs` + `ChooserDialogViewModel.cs`; edge: a product launcher lands with the ReallySimpleListChooser migration |
| Create feature / add feature value | `Src/LexText/LexTextControls/MasterInflectionFeatureListDlg.cs` / `MasterPhonologicalFeatureListDlg.cs` | `Src/Common/FwAvaloniaDialogs/CreateFeatureDialogView.axaml.cs` + `CreateFeatureDialogViewModel.cs`; edge: `Src/LexText/LexTextControls/Avalonia/LcmCreateFeatureLauncher.cs` |

Symbols in the rules below use Tools → Options as the worked example.

The Avalonia side splits into three layers — keep the split when you edit:
- **View (`*.axaml`)** — controls + bindings only. No LCModel, no WinForms.
- **ViewModel (`*ViewModel.cs`)** — edits a plain **state DTO**, exposes
  commands. LCModel-free and WinForms-free.
- **Launcher/edge (`Avalonia*Launcher.cs` in LexText)** — the only place that
  touches `PropertyTable`/`FwApplicationSettings`/LCModel; builds the state,
  applies it on OK, and supplies callbacks (e.g. showing a nested dialog).

The WinForms form does all of this in one class. So "add a field" means one
edit on the WinForms side and typically **four** on the Avalonia side (view,
view-model, state DTO, launcher build + apply). Missing any one silently drops
the field.

## What fails when one side is updated but not the other

Concrete failure modes seen in this codebase — check for each when you touch a
paired dialog:

1. **Setting silently not saved.** A new field added to WinForms `OK`/apply but
   not to the launcher's `Apply()` (or vice versa): the user edits it, closes on
   OK, nothing persists. (This is exactly the "checked everything, X'd out,
   still Legacy" class of bug.)
2. **Control missing entirely.** A button/checkbox added to one view and not the
   other — the user on the missing side simply can't reach the feature. When
   the absence is deliberate, it must be an explicit, recorded divergence, not
   silence.
3. **Behavioral divergence.** One applies live, the other prompts a restart; one
   validates, the other doesn't; different apply order → different side effects
   (e.g. writing-system change before vs after plugin install).
4. **Visibility/enable drift.** One side gates a control's visibility or enabled
   state on a condition (UI mode, platform, settings state) that the other side
   doesn't mirror. Check that both sides gate on the same condition, not just
   that both sides have a similarly named control.
5. **String/localization drift.** Wording, mnemonics, or the `.resx`/XLIFF key
   updated on one side only → inconsistent UI and broken translation memory.
   Both sides must carry the same seed English (see `fieldworks-localization-review`).
6. **State DTO / persisted-key mismatch.** The DTO field, the settings property,
   and the `PropertyTable` broadcast key must all agree
   (e.g., in the Options pair: `UIModeDisabledTools` ↔
   `UIFrameworkResolver.UIModeDisabledToolsPropertyName`).
   A rename on one side leaves the other writing a dead key.
7. **Test blind spot.** Headless Avalonia tests pass while the WinForms form (or
   the live modal-host input path) is broken, because the tests exercise
   bindings, not the real host. Green tests ≠ parity.
8. **Divergence comment rot.** A "sanctioned divergence" note that was never
   actually approved (or is now stale) misleads the next migrator into
   preserving a bug. Treat undocumented-approver notes as suspect.

## Concrete ways to keep them in sync

Do these, in order, on any paired-dialog change:

1. **Edit both sides in the same commit.** Never land a one-sided change. If the
   other side is out of scope, stop and say so explicitly.
2. **Share the source of truth, don't copy it.** Prefer one list/rule both
   consume over two hand-maintained copies:
   - e.g., in the Options pair: `LexiconFeatureCatalog` is the single catalog
     that `UIFrameworkRegistry.DefaultSupportedTools` is built from — extend
     it, not a second list.
   - Apply/normalize/gate helpers should be shared or mirrored with a pointer
     comment (e.g., in the Options pair: `NormalizeUiMode`,
     `ParseDisabledTools`/`SerializeDisabledTools`).
3. **Mirror the apply order.** Keep the two sides' apply/OK ordering identical
   and cite the counterpart method in a comment when you add a step. Worked
   example: the Options launcher's `Apply()` is explicitly written to follow
   `LexOptionsDlg.m_btnOK_Click`'s order.
4. **Cross-reference in comments.** Each side names its counterpart
   (e.g. `// parity with WinForms LexOptionsDlg m_uiModeBetaWarning`). A grep
   for the partner symbol should always find the other side.
5. **Parity tests, not just binding tests.** Assert the *behavior* both dialogs
   promise: field persists on OK, control hidden in Legacy/shown in New,
   validation blocks OK, disabled-tools round-trips. Put the DTO/launcher apply
   under test (see e.g. `AvaloniaOptionsDialogLauncherTests`, `LexOptionsDlgTests`,
   `OptionsDialogTests`).
6. **Record approved divergences** in the launcher/class doc as a
   `KNOWN GAP`/`APPROVED DIVERGENCE` block with the reason **and the approver**.
   No approver ⇒ it's a bug to fix, not a divergence to keep.
7. **Verify in the real host, both modes.** Headless tests can't see the
   WinForms-hosted-Avalonia input path. Before claiming done, drive the live
   dialog in New mode (and confirm Legacy still uses the WinForms form).

## Pre-commit checklist for a paired-dialog change

- [ ] Both implementations edited (WinForms form; Avalonia view + view-model +
      state DTO + launcher build & apply).
- [ ] Same controls, visibility gates, validation, and apply order on both.
- [ ] Same seed strings + `.resx`/XLIFF keys on both (localization strategy).
- [ ] DTO field ↔ settings property ↔ `PropertyTable` key all agree.
- [ ] Parity tests assert the behavior (persist / gate / validate), not just a
      binding.
- [ ] Any difference is an explicitly approved, documented divergence — else
      it's removed.
- [ ] Driven live in New mode; Legacy still routes to the WinForms form.

## Related skills

- `fieldworks-winforms-to-avalonia-migration` — the full migration playbook.
- `fieldworks-ui-wiring-review` — which host is active / how a setting reaches a screen.
- `fieldworks-localization-review` — string + `.resx`/XLIFF parity.
- `fieldworks-avalonia-ui` — the Avalonia View/ViewModel/host patterns themselves.
