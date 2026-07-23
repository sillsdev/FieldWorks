<!-- Lightweight inventory stub — one per WinForms component to migrate.
     Deepen into the full format (Docs/migration/_TEMPLATE.md) when the JIRA ticket is picked up.
     Keep it short: this exists so nothing is missed and every component has a tracked home. -->

# <Component name> (`<LegacyClassName>`)

| | |
|---|---|
| **Legacy class** | `<ns.ClassName>` (`Src/…/<File>.cs`) |
| **Area** | Lexicon / Grammar / Texts&Words / Notebook / Lists / Reversal / Shell / App-wide |
| **Type** | dialog / chooser / launcher / tool-screen / list-editor / browse / detail / shell / native-render |
| **Primitive** | plain-form / TABLE / TREE / MULTI-SELECTOR / TABS / owned-control / n/a |
| **State** | legacy / coexist (behind `UIMode=New`) / migrated / retired |
| **Phase** | 1 / 2 |
| **Canonical reference** | the kept canonical screen to copy when migrating (see Docs/migration/README.md) |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![before](./images/<name>-before.png) | ![after](./images/<name>-after.png) |

Same seeded data in both; attach both to the JIRA ticket. `before` = `fieldworks-winapp` capture
(launch-per-tool script for tool screens; `ScreenshotHarnessTests` harness for dialogs). `after` =
the Avalonia visual test for this surface (`fieldworks-semantic-render-parity` lane), added when the
Avalonia surface is built.

## What it is
<one line — what the user does with it / when it opens. Fill on pickup if unknown.>

## Notes / gotchas
- <Views-coupling, owned-control needs, modality, known PARITY items — as discovered.>

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (and capture legacy PNGs via the
> `fieldworks-winapp` skill) when this ticket is picked up.
