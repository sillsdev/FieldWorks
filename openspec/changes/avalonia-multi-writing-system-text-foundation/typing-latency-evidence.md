# Typing Latency Harness (Task 8.3)

## Harness

- Test: `FwAvaloniaTests.TypingLatencyHarnessTests.TypingHarness_MeetsPerKeystrokeThresholds`
- Command:
  - `./test.ps1 -Filter "TypingLatencyHarnessTests|RtlEditingTests|RegionModelTests|FullEntryRegionComposerTests"`
- Scope: measures per-keystroke latency for the rich-text edit path plus bidi caret mapping logic in managed code.

## Thresholds

- 100% DPI (`dpiScale=1.0`): <= 6.0 ms/key
- 150% DPI (`dpiScale=1.5`): <= 8.0 ms/key

## Latest Run

- Command:
  - `./test.ps1 -TestProject Src/Common/FwAvalonia/FwAvaloniaTests/FwAvaloniaTests.csproj -TestFilter "TypingLatencyHarnessTests|RtlEditingTests|RegionModelTests"`
- Observed:
  - 100% DPI: 0.010 ms/key over 500 edits
  - 150% DPI: 0.008 ms/key over 500 edits
- Result:
  - Both thresholds passed.
