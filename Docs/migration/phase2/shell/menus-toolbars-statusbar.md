# Menus / toolbars / status bar (`FlexUIAdapter` / `Inventory` / `Main.xml`)

| | |
|---|---|
| **Key files** | `Src/XCore/FlexUIAdapter/` (`MenuAdapter.cs`, `BarAdapterBase.cs`), `Src/XCore/Inventory.cs`, `DistFiles/Language Explorer/Configuration/Main.xml` |
| **Area** | Shell |
| **Type** | shell |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 11b/11f |
| **JIRA** | LT-XXXXX |

## What it is
The XML-driven command surface: `Main.xml` declares menus/toolbars/commands, `Inventory` loads and merges the XML, and `FlexUIAdapter` realizes it as WinForms menu/toolbar/status-bar widgets.

## Notes / gotchas
- Reimplement the adapter layer for Avalonia menus/toolbars; keep the `Main.xml`/`Inventory` declarative model so commands route unchanged.
- Command enable/visibility flows through mediator colleague dispatch — preserve during coexistence.
- Adapters deleted at end of coexistence; `Main.xml` config likely survives the shell swap.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
