# WinForms → Avalonia screen migration index

This folder documents every FieldWorks WinForms screen in the lexical-edit migration
program: what it is, what it looks like (PNGs from live legacy FLEx), and the gotchas a
teammate needs to migrate it. Each **deferred** screen has a doc here + a JIRA ticket.

> **[INVENTORY.md](./INVENTORY.md) is the master index** — every in-scope component
> (227 and counting), grouped by dialogs / choosers-launchers / tool-screens / list-editors,
> plus Phase-2 shell & native-render under `phase2/`. Stubs use `_STUB_TEMPLATE.md`; deepen to
> `_TEMPLATE.md` on pickup. The detailed deferred-dialog docs below are the worked examples.

**Phase model.** Phase 1 (this program) = high-value features/bugfix-grade migrations behind
the `UIMode=New` flag (default `Legacy`, so nothing ships to default users yet). Phase 2 =
net10 / multiplatform / shell conversion (gated until Phase 1 + tester burn-down complete).

**Canonical-per-primitive.** We keep ONE canonical screen per UI primitive as the reference
implementation teammates copy; everything else is documented here and backed out, to be
re-built/finished under its JIRA ticket.

## Canonical screens (KEPT — copy these)
| Primitive | Canonical screen | Where |
|---|---|---|
| Virtualized editable TABLE | Lexicon Browse/Edit pane | `Src/Common/FwAvalonia/Region/LexicalBrowseView.cs` |
| Composed detail editor (DataTree replacement) | Lexicon Edit entry pane | `Src/xWorks/FullEntryRegionComposer.cs` |
| Tree + multi-selector | ChooserDialog | `Src/Common/FwAvaloniaDialogs/ChooserDialogView.axaml` |
| Tabs | OptionsDialog | `Src/Common/FwAvaloniaDialogs/OptionsDialogView.axaml` |
| Owned-control composite form | InsertEntryDialog | `Src/Common/FwAvaloniaDialogs/InsertEntryDialogView.axaml` |
| Search + list selector | EntryGoDialog | `Src/Common/FwAvaloniaDialogs/EntryGoDialogView.axaml` |

Also kept (same composer, other record types): **notebookEdit**, **posEdit**.

## Split into their own follow-up PRs (XL, isolated)
| Surface | OpenSpec change |
|---|---|
| Words interlinear editor (`Analyses`) | `avalonia-interlinear-editor` |
| Grammar rule family (6 tools) | `avalonia-rule-formula-editor` |

## Deferred screens (DOCUMENTED + backed out → JIRA)
| Screen | Primitive | Doc | JIRA |
|---|---|---|---|
| ConfigureColumns | dual-list reorder | [configure-columns.md](./configure-columns.md) | _TBD_ |
| AddNewSense | owned-control form | [add-new-sense.md](./add-new-sense.md) | _TBD_ |
| MsaCreator | owned-control form | [msa-creator.md](./msa-creator.md) | _TBD_ |
| FeatureChooser | picker | [feature-chooser.md](./feature-chooser.md) | _TBD_ |
| CreateFeature | plain-form | [create-feature.md](./create-feature.md) | _TBD_ |
| DeleteConfirmation | plain-form | [delete-confirmation.md](./delete-confirmation.md) | _TBD_ |
| LexReferenceDetails | plain-form | [lex-reference-details.md](./lex-reference-details.md) | _TBD_ |
| FilterFor | plain-form / radios | [filter-for.md](./filter-for.md) | _TBD_ |
| DateRangeFilter | plain-form + date pickers | [date-range-filter.md](./date-range-filter.md) | _TBD_ |
| FindReplace | plain-form | [find-replace.md](./find-replace.md) | _TBD_ |
| PictureProperties | plain-form + media | [picture-properties.md](./picture-properties.md) | _TBD_ |
| SpecialCharacter | plain-form + list | [special-character.md](./special-character.md) | _TBD_ |
| WritingSystemProperties (core) | plain-form | [writing-system-properties.md](./writing-system-properties.md) | _TBD_ |

> See [`_TEMPLATE.md`](./_TEMPLATE.md) for the per-screen doc format. PNGs go in `./images/`.
