<!-- Copy this file to Docs/migration/<screen-kebab-name>.md and fill every section.
     One file per WinForms screen/dialog that is deferred to a JIRA ticket.
     The PNG(s) are captured from LIVE legacy FLEx (UIMode=Legacy) via the
     fieldworks-winapp / winforms-mcp path — see .claude/skills/fieldworks-winapp. -->

# <Screen name> (legacy `<WinFormsClassName>`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.…<WinFormsClassName>` (`Src/…/<File>.cs`) |
| **Area / tool** | e.g. Lexicon › Browse filter bar |
| **Primitive(s)** | plain-form / TABLE / TREE / MULTI-SELECTOR / TABS / owned-control |
| **Canonical reference** | the kept canonical screen to copy (e.g. ChooserDialog for tree+multi-select) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/<X>View.axaml(.cs)` @ git `<sha>` (recover from history) |
| **JIRA** | LT-XXXXX |

## What it is
One or two sentences: what the user does with this screen and when it opens.

## What it looks like
![<screen> – initial](./images/<screen>-01.png)
<!-- Add more PNGs for important states: filled, error, multi-select, etc. -->

## Behaviour to preserve (parity checklist)
- [ ] …each interactive behaviour the legacy screen has
- [ ] validation / OK-gating rules
- [ ] keyboard / focus / accessibility expectations

## Migration gotchas
- WS/RTL, owned-control hosting, undo-fencing, layout-choice resolution, etc.
- Anything the backed-out stub got wrong or left as `// PARITY`.

## Wiring
- Legacy call site(s): `<File>.cs:<line>`
- The Avalonia path branched on `UIMode=New` here before back-out: `<File>.cs:<line>`
- Re-wiring target: this launcher/host should re-enter the Avalonia surface behind the flag.
