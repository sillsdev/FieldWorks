# Mediator / PropertyTable (`Mediator.cs` / `PropertyTable.cs`)

| | |
|---|---|
| **Key files** | `Src/XCore/xCoreInterfaces/Mediator.cs`, `PropertyTable.cs` (+ `ReadOnlyPropertyTable.cs`) |
| **Area** | Framework |
| **Type** | framework |
| **Primitive** | n/a |
| **State** | legacy (bridge seam exists) |
| **Phase** | 2 |
| **Census stage** | 11c (bridge: 2.3) |
| **JIRA** | LT-XXXXX |

## What it is
The XCore message bus (`Mediator`) and global state store (`PropertyTable`) that route commands, colleague notifications, and shared settings across the shell and tools.

## Notes / gotchas
- A Phase-2.3 bridge seam already exists so Avalonia surfaces can read/write the table during coexistence.
- Reimplement/replace incrementally; pervasive dependency — almost every tool colleague subscribes here.
- Removal is late (after consumers migrate); keep the bridge faithful until then.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
