# Ghost Lex Ref Launcher (`GhostLexRefLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.LexEd.GhostLexRefLauncher` (`Src/LexText/Lexicon/GhostLexRefSlice.cs`) |
| **Area** | Lexicon |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog |
| **JIRA** | LT-XXXXX |

## What it is
Owned-control launcher for a ghost (empty-placeholder) lexical-reference slice; creates the relation on first use.

## Notes / gotchas
- Subclass of ButtonLauncher; defined in GhostLexRefSlice.cs (line 50). State=coexist (UIMode-gated). Lexical-relations slice family.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

