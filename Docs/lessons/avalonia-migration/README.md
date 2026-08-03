# Avalonia migration lessons

These records preserve evidence from FieldWorks migration work without making
retired code or AI-generated plans authoritative. Read the relevant card before
planning a related migration, then verify its observations against the current
tree and the live legacy behavior.

## Capability index

| Capability or problem | Lesson record |
| --- | --- |
| Migration sequencing; boundary above DataTree; canonical exemplars; scope reduction; dormant code; evidence quality | [Migration pivot](migration-pivot.md) |
| Words > Analyses detail; interlinear projection; approval state; sense and MSA changes; pruning; atomic undo; mixed writing systems | [Interlinear analysis](interlinear-analysis.md) |
| Phonological rules; environments; compound rules; natural classes; phonemes; formula parity; context transitions; plugin activation | [Rule-formula editors](rule-formula-editors.md) |
| Browse virtualization; stable selection; clerk sorting/filtering; bulk edit; RDE; accessibility; activation breadth | [Browse-table activation](browse-table-activation.md) |
| Picture editing; properties dialog; dormant view-models; localization pair removal; exchange DTO lifetime | [Avalonia picture editing](avalonia-picture-editing.md) |
| Options-only utilities; features with no WinForms counterpart; parity divergence cost; entry-point unwinding | [Lexicon feature manager](lexicon-feature-manager.md) |

## How to use these records

1. Treat observations as research leads until reconfirmed in the current tree.
2. Treat rejected approaches as warnings, not designs to repair.
3. Promote a lesson into a skill, spec, test, or code contract only with human
   review and current evidence.
4. Create Jira work only after a human approves a product outcome or bounded
   discovery question. A lesson card is context, not backlog authorization.
5. Start approved implementation from the current base. Do not rebase,
   cherry-pick, or copy retired surface code merely because a card cites it.

Historical branches for PRs #965-#967 remain archaeological sources. Their
stacking reflects delivery history, not a dependency between interlinear,
rule-formula, and browse-table work.
