# Buffered draw (`VwDrawRootBuffered`)

| | |
|---|---|
| **Key files** | `Src/ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs` |
| **Area** | Native-render |
| **Type** | native-render |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 13 |
| **JIRA** | LT-XXXXX |

## What it is
Managed double-buffered draw helper for the Views engine: renders the root box to an off-screen buffer to avoid flicker on the WinForms host.

## Notes / gotchas
- Pure decommission target — Avalonia compositing supplies its own buffering, so there is no reimplementation; deleted at Stage 13.
- Tied to the GDI/WinForms paint model; obsolete once the native render path is gone.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
