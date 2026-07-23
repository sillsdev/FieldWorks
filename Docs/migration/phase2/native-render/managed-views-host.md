# Managed Views host (`SimpleRootSite` / `RootSite`)

| | |
|---|---|
| **Key files** | `Src/Common/SimpleRootSite/SimpleRootSite.cs`, `Src/Common/RootSite/RootSite.cs` |
| **Area** | Native-render |
| **Type** | native-render |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 9/13 |
| **JIRA** | LT-XXXXX |

## What it is
The managed WinForms control that hosts the native Views engine: pumps paint/keyboard/mouse/IME into `VwRootBox` and surfaces selection back to .NET.

## Notes / gotchas
- Decommission target paired with the Views engine — replaced at Stage 9, deleted at Stage 13.
- The WinForms `Control` boundary where native rendering meets managed UI; the Avalonia render host supersedes it.
- Registration-free COM bridge to native Views — keep activation via manifest only.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
