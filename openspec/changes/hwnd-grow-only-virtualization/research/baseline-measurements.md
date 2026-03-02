# Baseline Measurements

Date: 2026-03-02
Branch: `001-render-speedup`

## Instrumentation added

- USER handle count via `HwndDiagnostics.GetCurrentProcessUserHandleCount()`
- Slice install creation count via `DataTree.SliceInstallCreationCount`
- Render harness timing captures:
  - `UserHandleCountBeforePopulate`
  - `UserHandleCountAfterPopulate`
  - `UserHandleDelta`
  - `SliceInstallCreationCount`

## Automated baseline scenario

- Test: `DataTreeHwndCountTest.ShowObject_RecordsUserHandleDeltaAndInstallCount`
- Layout: `Normal`
- Object: `LexEntry` with 40 senses
- Assertions:
  - slice count > 20
  - install count equals slice count (baseline eager install)
  - USER handle delta >= 0
  - USER handle delta <= slices * 8

## Pending manual baselines

The following scenarios are still pending manual capture in the running app:

- Lexicon Edit (pathological 253-slice entry)
- Grammar Edit (typical category)
- Lists Edit (typical item)

These should be recorded with the same USER-handle instrumentation before and after opening each record.
