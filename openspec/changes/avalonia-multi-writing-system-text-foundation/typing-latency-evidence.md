# Typing Latency Harness (Task 8.3)

## Harness

- Test: `FwAvaloniaTests.TypingLatencyHarnessTests.TypingHarness_MeetsPerKeystrokeThresholds`
- Command:
  - `./test.ps1 -Filter "TypingLatencyHarnessTests|RtlEditingTests|RegionModelTests|FullEntryRegionComposerTests"`
- Scope: measures per-keystroke latency of the **managed model layer only** — the rich-text run-edit path (`RegionRichTextEditAlgorithms`) plus bidi caret mapping. It does **not** exercise realized Avalonia text layout, glyph shaping, rendering, or the OS IME, so it is a model-layer floor, not end-to-end typing latency.

## Thresholds

- 100% DPI (`dpiScale=1.0`): <= 6.0 ms/key
- 150% DPI (`dpiScale=1.5`): <= 8.0 ms/key

## Latest Run (as of 2026-06-15)

- Command:
  - `./test.ps1 -TestProject Src/Common/FwAvalonia/FwAvaloniaTests/FwAvaloniaTests.csproj -TestFilter "TypingLatencyHarnessTests|RtlEditingTests|RegionModelTests"`
- Observed:
  - 100% DPI: 0.010 ms/key over 500 edits
  - 150% DPI: 0.008 ms/key over 500 edits
- Result:
  - Both thresholds passed.
- Caveat: the observed ~0.01 ms/key is ~600× under the threshold precisely because it measures the
  model layer in isolation. Treat it as proof the managed edit path adds negligible cost — NOT as an
  end-to-end typing-latency parity claim. Realized-surface typing latency (layout + shaping + render +
  IME) at 100%/150% DPI rides the realized-window evidence lane (task 8.2) and the lexical-edit
  performance lanes (`region-manifest.md` §5.4 — scroll/typing still open).
