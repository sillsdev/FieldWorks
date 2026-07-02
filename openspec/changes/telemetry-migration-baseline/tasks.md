## 1. Outbox core (managed, `Src/Common/FwUtils/`)

- [ ] 1.1 Verify `Analytics.AllowTracking` accessibility and exact `RobustFile`/`RobustIO`/`RetryUtility` method signatures against the installed `SIL.Core.Desktop`/`SIL.DesktopAnalytics` package versions (resolves design.md's two Open Questions); note the resolution in a code comment at the call site.
- [ ] 1.2 Add `Src/Common/FwUtils/AnalyticsOutbox.cs`: static class with `Track(string eventName, Dictionary<string,string> properties)` and `ReportException(Exception exception, Dictionary<string,string> moreProperties)`, matching `Analytics`'s method shapes (design.md D3).
- [ ] 1.3 Implement consent check (design.md D7) shared by both methods, with the `AllowTracking`-or-fallback-to-`OkToPingBasicUsageData` logic resolved in 1.1.
- [ ] 1.4 Implement outbox file write: serialize event kind, name, properties, and queued-at timestamp to `{UtcTicks:D19}_{Guid:N}.json` under `FwDirectoryFinder.UserAppDataFolder("FieldWorks")\Analytics\Outbox\`, creating the directory if absent (design.md D1, D2).
- [ ] 1.5 Implement the atomic-rename claim (`.inflight` suffix) and post-send delete/rollback logic (design.md D4, D5).
- [ ] 1.6 Implement the bounded-growth sweep: delete oldest files beyond the count/age cap before attempting delivery (design.md D6).
- [ ] 1.7 Implement queued-exception replay via `Analytics.Track("QueuedExceptionReport", ...)` instead of `Analytics.ReportException` (design.md D8).
- [ ] 1.8 Wire the three flush triggers: fire-and-forget flush of the just-written file, full startup sweep, and a bounded (≤2s) shutdown sweep (design.md D9).
- [ ] 1.9 Unit tests (new `Src/Common/FwUtils/FwUtilsTests/AnalyticsOutboxTests.cs`, run via `.\test.ps1`): consent-off write is a no-op; a written file survives a simulated restart (re-instantiate the outbox against the same directory); FIFO ordering of multiple queued files; cap eviction deletes oldest first; a claimed-but-failed send restores the original filename; a claim race (two attempts on the same file) results in exactly one winner.

## 2. Migrate existing call sites to `AnalyticsOutbox`

- [ ] 2.1 `Src/Common/FieldWorks/FieldWorks.cs`: replace the three `Analytics.ReportException` calls with `AnalyticsOutbox.ReportException`.
- [ ] 2.2 `Src/LexText/LexTextDll/AreaListener.cs`: replace the `Analytics.Track("SwitchToTool", ...)` call with `AnalyticsOutbox.Track` (coordinate with section 4's dwell-time change to avoid two separate edits to the same call).
- [ ] 2.3 `Src/Common/FwUtils/TrackingHelper.cs`: replace all three `Analytics.Track` calls (`TrackImport`/`TrackExport`/`TrackHelpRequest`) with `AnalyticsOutbox.Track`.
- [ ] 2.4 `Src/LexText/Interlinear/ConcordanceContainer.cs` and `Src/LexText/Interlinear/ConfigureInterlinDialog.cs`: replace their `Analytics.Track` calls with `AnalyticsOutbox.Track`.
- [ ] 2.5 `Src/Common/Controls/FwControls/ObtainProjectMethod.cs`: replace its `Analytics.Track` call with `AnalyticsOutbox.Track`.
- [ ] 2.6 Repo-wide grep for `Analytics.Track(` and `Analytics.ReportException(` outside `AnalyticsOutbox.cs` itself to confirm no call site was missed.

## 3. Session baseline (`Src/Common/FieldWorks/FieldWorks.cs`)

- [ ] 3.1 Generate a `SessionId` (`Guid.NewGuid()`) once at startup and store it for the process lifetime.
- [ ] 3.2 Track a session-start event via `AnalyticsOutbox.Track` immediately after the `Analytics` instance is constructed, carrying `SessionId`.
- [ ] 3.3 Add the `Interlocked.CompareExchange`-guarded single-terminal-event flag (design.md D10).
- [ ] 3.4 Track a `clean`-outcome session-end event at the normal fall-through point of the `using (new Analytics(...))` block, including duration since session-start.
- [ ] 3.5 Track a `crashed`-outcome session-end event from `HandleUnhandledException`/`HandleTopLevelError`, including duration since session-start.
- [ ] 3.6 Unit/integration tests: clean shutdown produces exactly one `clean` session-end; a simulated unhandled exception produces exactly one `crashed` session-end and suppresses the clean-path event; duration reflects elapsed time in both cases.

## 4. Usage enrichment (`UiFramework` property + `SwitchToTool` dwell time)

- [ ] 4.1 Call `Analytics.SetApplicationProperty("UiFramework", "WinForms")` once at startup in `FieldWorks.cs`, right after `Analytics` construction.
- [ ] 4.2 Test that any event tracked after startup carries `UiFramework=WinForms` (verifies the property attaches automatically without touching individual `Track` call sites).
- [ ] 4.3 In `AreaListener.cs`, add a field tracking the last tool-activation timestamp, updated on every tool change regardless of the once-per-day throttle.
- [ ] 4.4 When the throttled `SwitchToTool` event does fire, include a `duration` property computed from the stored timestamp (design.md D11), without changing the existing `area`/`tool` properties.
- [ ] 4.5 Add a final dwell-time record at application shutdown for whatever tool is currently active, using the same `AnalyticsOutbox.Track` call shape.
- [ ] 4.6 Unit tests (extend `Src/LexText/LexTextDll` test project or nearest existing `AreaListener` test fixture): repeated same-day tool revisits still throttle to one event but carry a duration; a switch after a measurable delay reports a duration reflecting that delay; shutdown while a tool is active produces one final dwell record.

## 5. Full verification

- [ ] 5.1 Run `.\build.ps1` for the full managed build (no native changes expected; confirm no native rebuild is triggered).
- [ ] 5.2 Run `.\test.ps1` for the affected test projects (`FwUtilsTests`, `LexTextDll` tests, and any interlinear/common-controls tests touched by call-site migration) and confirm zero regressions.
- [ ] 5.3 Manually exercise the outbox end-to-end: disconnect network, use FieldWorks to trigger a few tracked events, confirm files accumulate under `%LocalAppData%\SIL\FieldWorks\Analytics\Outbox\`, reconnect, relaunch, and confirm the directory empties (via logging or a debugger breakpoint, since there's no UI surface for this).
- [ ] 5.4 Confirm the existing Tools > Options > Privacy checkbox (`OkToPingBasicUsageData`) still gates all telemetry, including newly-queued-but-not-yet-flushed events, per design.md D7.
