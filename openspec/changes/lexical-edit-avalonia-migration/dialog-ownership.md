# Dialog Ownership and Modality Across the Interop Boundary (Task 3.16)

Rules for dialogs and popups while WinForms and Avalonia surfaces coexist in one process and one
window (Avalonia 11.x hosted via `WinFormsAvaloniaControlHost`). One UI thread, one message loop:
WinForms modality (`ShowDialog`) blocks the shared loop, which is what makes the rules below workable.

## Rules

1. **WinForms modal dialogs launched from an active Avalonia surface** (choosers, message boxes,
   options): always pass an owner — the hosting WinForms top-level form (`Control.FindForm()` of the
   `LexicalEditHostControl`/host), never `null` and never an Avalonia handle. This guarantees
   correct z-order and minimize/restore grouping. Modality is process-wide on the shared loop, so
   the Avalonia surface is implicitly blocked — no extra disable/enable dance.
2. **Focus return**: the launcher records the focused Avalonia control before `ShowDialog` and
   restores focus to it (via the host's focus API) after the dialog closes. Cross-boundary Tab is
   unreliable on 11.x (AvaloniaUI/Avalonia#12025); explicit restore-after-dialog is the supported
   pattern, never "let focus find its way back".
3. **Avalonia flyouts/popups inside the WinForms-hosted surface**: prefer flyouts attached to the
   triggering control (the shared chooser pattern). Account for the known 11.x popup-DPI quirk on
   mixed-DPI monitors: test popup placement at 100%/150%, and prefer `Flyout` over free `Popup`
   windows — flyouts position in-surface and avoid the worst of it.
4. **No Avalonia modal windows during coexistence.** Avalonia `Window.ShowDialog` against a WinForms
   owner is not a supported combination on 11.x in this app; anything needing modality uses a
   WinForms dialog (rule 1) until the shell phase.
5. **Message boxes** from Avalonia-side code route through the existing FieldWorks message adapter
   (`SetMessageBoxAdapter` test seam), same as legacy code — keeps tests headless-safe.

## Explicitly unsupported during coexistence

- Avalonia-owned modal windows (rule 4).
- WinForms modeless tool windows owned by an Avalonia surface (no owner handle to give them) —
  modeless tools stay owned by the main window.
- Cross-boundary Tab order between WinForms siblings and the Avalonia surface (own the focus
  *inside* the surface; coarse hosting is the standing constraint).

## Verification

- Covered now: focus-return contract at the seam level (`IHostSurface` focus API,
  `HostSurfaceContractTests`); popup focus return inside the surface (`LexicalEditPreviewTests`).
- Still open (needs a realized-window UIA run, not headless): chooser-launch-from-Avalonia smoke —
  launch a WinForms chooser from the Avalonia surface, assert owner is the host form, dismiss,
  assert focus returns to the launching Avalonia control. Lands with the first real chooser
  integration (6.3), driven through the `WinFormsUiaSmokeTests` harness.
