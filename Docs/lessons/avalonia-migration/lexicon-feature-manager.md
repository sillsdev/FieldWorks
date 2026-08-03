# Lexicon feature-manager dialog

Status: retired implementation; the per-tool opt-out setting it edited survives
Sources: PR #964; commit `6e84cd48d`; removed `LexiconFeatureManagerDialog*`,
`LexiconFeatureManagerState`, the four `FeatureManager*` strings and the
`ManageIndividualFeatures` button string
Human review: PR #964

## Question tested

Should a New-mode-only utility with no WinForms counterpart -- a checkbox list
for disabling individual Avalonia surfaces, reached from Tools > Options --
ship alongside a coexisting dialog pair?

## Observations

- The dialog managed a feature catalog reached from Options. It was unrelated
  to the Lexicon Edit detail view and to the InsertEntry dialog family, so it
  was not a conversion of anything; it was a new utility on the new path only.
- Having no legacy counterpart made it a permanent parity divergence. The
  paired-dialog guidance had to carry an explicit "deliberate absence" row
  describing it, purely so the asymmetry did not read as an oversight. That
  documentation burden was ongoing, not one-off.
- The catalog itself is genuinely shared and stayed:
  `EditSurfaceRegistry.DefaultSupportedTools` is built from
  `LexiconFeatureCatalog.ToolNames`, so it remains the single list of tools
  that ship with a working Avalonia surface. Only the checkbox-list rendering
  was removed.
- The setting the dialog edited also stayed. `UIModeDisabledTools` is the
  per-tool opt-out `EditSurfaceResolver` still honours, seeded into the
  PropertyTable by the shell and read when a surface is resolved. The Options
  dialog now carries it through untouched instead of offering an editor for it.
- Unwinding the entry point took five coordinated edits: the button in the view,
  the view-model's command and its visibility gate, the state object's callback,
  and the launcher method. Missing any one would have left a dead affordance --
  a control that appears and does nothing, or a callback nothing invokes.

## What failed or was retired

The dialog set that rendered the catalog as a checkbox list was removed together
with its Options entry point, rather than shipping as a stray New-mode-only
utility. The catalog, the descriptor type, and the `UIModeDisabledTools` setting
all remain; the setting has no user-facing editor.

## Durable lessons

1. A feature with no legacy counterpart creates a parity divergence that must be
   documented for as long as both paths coexist. Weigh that standing cost before
   building one, not after.
2. Separate the data from its editor. The catalog and the setting survived the
   dialog's removal precisely because neither depended on it.
3. Unwind an entry point end to end -- control, command, visibility gate, state
   callback, launcher method -- or the removal leaves a dead affordance.
4. A utility unrelated to the surface being migrated does not belong in that
   migration, however convenient the host dialog is.
5. Retarget comments that point at deleted code at the surviving concept, or the
   next reader chases a name that no longer exists.

## Evidence needed next time

- A human-approved product decision that per-tool opt-out needs a user-facing
  editor at all, rather than remaining a settings-level value.
- If it does: which dialog owns it, and whether the WinForms Options dialog
  gains the same control so the pair stays symmetric.
- Confirmation that the catalog remains the single source for both the shipped
  tool list and any editor over it.

## Decision boundary

This record constrains where an Options-reached utility belongs and what
unwinding an entry point requires. A human decides whether per-tool opt-out
gets an editor, and on which path.

## Do not infer

- Do not restore the dialog set from history.
- Do not infer that `UIModeDisabledTools` was removed; it is still honoured.
- Do not infer that `LexiconFeatureCatalog` is unused; the shipped tool list is
  built from it.
- Do not add a New-mode-only affordance to the Options dialog on the strength of
  this record.
