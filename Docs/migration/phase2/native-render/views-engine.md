# Views engine (`VwRootBox` / `VwSelection` / `VwTextBoxes`)

| | |
|---|---|
| **Key files** | `Src/views/VwRootBox.cpp`, `VwSelection.cpp`, `VwTextBoxes.cpp` (+ `Src/views/`) |
| **Area** | Native-render |
| **Type** | native-render |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 9 (replace) / 13 (delete) |
| **JIRA** | LT-XXXXX |

## What it is
The native C++ rendering/layout/selection engine: `VwRootBox` builds and lays out the view box tree, `VwSelection` models the insertion point/range, `VwTextBoxes` renders styled multi-writing-system text.

## Notes / gotchas
- Decommission target: the Avalonia presentation IR + render path replaces it at Stage 9; native code deleted at Stage 13.
- Registration-free COM — consumed via the manifest; no global COM registration.
- Carries deep correctness (BiDi, IME, selection semantics) — parity is the hard part; do not delete until consumers are off it.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
