---
name: dialog-update
description: "Keep a FieldWorks dialog's WinForms (old) and Avalonia (new) implementations in sync whenever either is changed. Use whenever you add, edit, or review a control, field, button, validation rule, apply-order step, or string in a dialog that exists in BOTH a WinForms form (e.g. LexOptionsDlg) and its Avalonia replacement (e.g. OptionsDialogView + AvaloniaOptionsDialogLauncher) — even for a one-line change. Also use before claiming a migrated dialog is at parity, and when deciding whether a difference between the two is an approved divergence."
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
| Tools → Options | `Src/LexText/LexTextControls/LexOptionsDlg.cs` (+ `.Designer.cs`, `.resx`) | `Src/Common/FwAvaloniaDialogs/OptionsDialogView.axaml(.cs)` + `OptionsDialogViewModel.cs` + `OptionsState.cs`; edge: `Src/LexText/LexTextControls/AvaloniaOptionsDialogLauncher.cs` |
| Manage Individual Features | (opened from `LexOptionsDlg.m_manageFeaturesButton_Click`) | `LexicalEditFeatureManagerDialog` (`Src/Common/FwAvaloniaDialogs/`) |

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
   other — e.g. the "Manage Individual Features" selector existed only in
   WinForms. The New-mode user simply can't reach the feature.
3. **Behavioral divergence.** One applies live, the other prompts a restart; one
   validates, the other doesn't; different apply order → different side effects
   (e.g. writing-system change before vs after plugin install).
4. **Visibility/enable drift.** WinForms shows the button only in New mode; the
   Avalonia copy shows it always (or never). Mirror the gate
   (`UpdateManageFeaturesButtonVisibility` ↔ `ManageFeaturesVisible`).
5. **String/localization drift.** Wording, mnemonics, or the `.resx`/XLIFF key
   updated on one side only → inconsistent UI and broken translation memory.
   Both sides must carry the same seed English (see `fieldworks-localization-review`).
6. **State DTO / persisted-key mismatch.** The DTO field, the settings property,
   and the `PropertyTable` broadcast key must all agree
   (`UIModeDisabledTools` ↔ `LexicalEditSurfaceResolver.UIModeDisabledToolsPropertyName`).
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
   - `LexicalEditFeatureCatalog` is the single catalog behind both the WinForms
     button and the Avalonia dialog — extend it, not two lists.
   - Apply/normalize/gate helpers should be shared or mirrored with a pointer
     comment (`NormalizeUiMode`, `ParseDisabledTools`/`SerializeDisabledTools`).
3. **Mirror the apply order.** The launcher's `Apply()` is explicitly written to
   follow `LexOptionsDlg.m_btnOK_Click`'s order. Keep that ordering and cite the
   WinForms method in a comment when you add a step.
4. **Cross-reference in comments.** Each side names its counterpart
   (`// parity with LexOptionsDlg.m_manageFeaturesButton`). A grep for the
   partner symbol should always find the other side.
5. **Parity tests, not just binding tests.** Assert the *behavior* both dialogs
   promise: field persists on OK, control hidden in Legacy/shown in New,
   validation blocks OK, disabled-tools round-trips. Put the DTO/launcher apply
   under test (see `AvaloniaOptionsDialogLauncherTests`, `LexOptionsDlgTests`,
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
- [ ] Same seed strings + `.resx`/XLIFF keys on both (localization lane).
- [ ] DTO field ↔ settings property ↔ `PropertyTable` key all agree.
- [ ] Parity tests assert the behavior (persist / gate / validate), not just a
      binding.
- [ ] Any difference is an explicitly approved, documented divergence — else
      it's removed.
- [ ] Driven live in New mode; Legacy still routes to the WinForms form.

## Related skills

- `fieldworks-winforms-to-avalonia-migration` — the full surface-migration playbook.
- `fieldworks-ui-wiring-review` — which host is active / how a setting reaches a screen.
- `fieldworks-localization-review` — string + `.resx`/XLIFF parity.
- `fieldworks-avalonia-ui` — the Avalonia view/VM/host patterns themselves.
