# Graphite engine (`GraphiteEngineClass`)

| | |
|---|---|
| **Key files** | `Src/views/lib/GraphiteEngine.cpp`, `GraphiteEngine.h` |
| **Area** | Native-render |
| **Type** | native-render |
| **Primitive** | n/a |
| **State** | legacy — removal accepted (doc+notify Awami Nastaliq) |
| **Phase** | 2 |
| **Census stage** | 10B (path) / 13 (delete) |
| **JIRA** | LT-XXXXX |

## What it is
The native Graphite smart-font shaping engine used by the Views renderer for complex-script layout.

## Notes / gotchas
- Removal accepted: documented decommission with a heads-up to affected scripts (notify Awami Nastaliq), not a reimplementation.
- The replacement render path (HarfBuzz/platform shaping) is evaluated at Stage 10B; native code deleted at Stage 13.
- Registration-free COM constraint applies while it still ships.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
