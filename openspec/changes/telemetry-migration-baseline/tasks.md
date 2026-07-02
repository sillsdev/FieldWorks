## 1. Outbox core (managed, `Src/Common/FwUtils/`)

- [x] 1.1 Verify `Analytics.AllowTracking` accessibility and exact `RobustFile`/`RobustIO`/`RetryUtility` method signatures against the installed `SIL.Core.Desktop`/`SIL.DesktopAnalytics` package versions (resolves design.md's two Open Questions); note the resolution in a code comment at the call site.
- [x] 1.2 Add `Src/Common/FwUtils/AnalyticsOutbox.cs`: static class with `Track(string eventName, Dictionary<string,string> properties)` and `ReportException(Exception exception, Dictionary<string,string> moreProperties)`, matching `Analytics`'s method shapes (design.md D3).
- [x] 1.3 Implement consent check (design.md D7) shared by both methods, with the `AllowTracking`-or-fallback-to-`OkToPingBasicUsageData` logic resolved in 1.1.
- [x] 1.4 Implement outbox file write: serialize event kind, name, properties, and queued-at timestamp to `{UtcTicks:D19}_{Guid:N}.json` under `FwDirectoryFinder.UserAppDataFolder("FieldWorks")\Analytics\Outbox\`, creating the directory if absent (design.md D1, D2).
- [x] 1.5 Implement the claim (`.inflight` rename + `FileShare.None` exclusive open, per D4/D14 — see below) and post-send delete/rollback logic (design.md D4, D5).
- [x] 1.6 Implement the bounded-growth sweep: delete oldest files beyond the count/age cap before attempting delivery (design.md D6).
- [x] 1.7 Implement queued-exception replay via `Analytics.Track("QueuedExceptionReport", ...)` instead of `Analytics.ReportException` (design.md D8).
- [x] 1.8 Wire the three flush triggers: fire-and-forget flush of the just-written file, full startup sweep, and a bounded (≤2s) shutdown sweep (design.md D9).
- [x] 1.9 Unit tests (`Src/Common/FwUtils/FwUtilsTests/AnalyticsOutboxTests.cs`, 15 tests, run via `.\test.ps1`): consent-off write is a no-op; a queued file is delivered on a later flush once network becomes available (the "survives a simulated restart" scenario); FIFO ordering; cap eviction (count and age) deletes without delivering; a claimed-but-failed send restores the original filename; concurrent flushes deliver each event exactly once.
  - **Found and fixed during implementation, not anticipated in the original design**: `System.IO.File.Move` does not reliably report failure to the "losing" thread when two callers race an identical rename — both can return without throwing even though the OS performs exactly one physical rename (verified with a standalone repro outside this codebase: 198–200/200 trials). The original rename-only claim design (D4) was corrected to add a `FileShare.None` exclusive open as the real single-owner check (D14). Root-caused via `AnalyticsOutboxTests.Flush_ConcurrentFlushCalls_DeliverEachEventExactlyOnce`, which failed intermittently (11–20 deliveries instead of 10) until this fix; now passes deterministically across 5+ repeated runs.
  - Marked the fixture `[NonParallelizable]` (D13): its test seams are static/process-wide (the real `DesktopAnalytics.Analytics` singleton can't be reconfigured per test), and VSTest's NUnit adapter runs fixtures concurrently by default per this repo's `Test.runsettings`.

## 2. Migrate existing call sites to `AnalyticsOutbox`

- [x] 2.1 `Src/Common/FieldWorks/FieldWorks.cs`: replace the three `Analytics.ReportException` calls with `AnalyticsOutbox.ReportException`.
- [x] 2.2 `Src/LexText/LexTextDll/AreaListener.cs`: replace the `Analytics.Track("SwitchToTool", ...)` call with `AnalyticsOutbox.Track` (coordinate with section 4's dwell-time change to avoid two separate edits to the same call).
- [x] 2.3 `Src/Common/FwUtils/TrackingHelper.cs`: replace all three `Analytics.Track` calls (`TrackImport`/`TrackExport`/`TrackHelpRequest`) with `AnalyticsOutbox.Track`.
- [x] 2.4 `Src/LexText/Interlinear/ConcordanceContainer.cs` and `Src/LexText/Interlinear/ConfigureInterlinDialog.cs`: replace their `Analytics.Track` calls with `AnalyticsOutbox.Track`.
- [x] 2.5 `Src/Common/Controls/FwControls/ObtainProjectMethod.cs`: replace its `Analytics.Track` call with `AnalyticsOutbox.Track`.
- [x] 2.6 Repo-wide grep for `Analytics.Track(` and `Analytics.ReportException(` outside `AnalyticsOutbox.cs` itself to confirm no call site was missed. (Zero matches outside `AnalyticsOutbox.cs`.)

## 3. Session baseline (`Src/Common/FieldWorks/FieldWorks.cs`)

- [x] 3.1 Generate a `SessionId` (`Guid.NewGuid()`) once at startup and store it for the process lifetime.
- [x] 3.2 Track a session-start event via `AnalyticsOutbox.Track` immediately after the `Analytics` instance is constructed, carrying `SessionId`.
- [x] 3.3 Add the `Interlocked.CompareExchange`-guarded single-terminal-event flag (design.md D10).
- [x] 3.4 Track a `clean`-outcome session-end event at the normal fall-through point of the `using (new Analytics(...))` block, including duration since session-start.
- [x] 3.5 Track a `crashed`-outcome session-end event from `HandleUnhandledException`/`HandleTopLevelError`, including duration since session-start.
- [ ] 3.6 **GAP — not implemented.** Unit/integration tests for session-start/end tracking. `FieldWorks.cs`'s session logic (`s_sessionId`, `TryReportSessionEnd`, etc.) is exercised only by `AnalyticsOutboxTests`' generic delivery paths, not by a test that drives `FieldWorks`'s own startup/shutdown/crash code paths directly. `FieldWorksTests` has no `InternalsVisibleTo` access to `AnalyticsOutbox`'s test seams, and `FieldWorks.Main`'s startup/shutdown sequence is not currently structured for isolated unit testing (it constructs a real `Analytics` singleton, which can only happen once per process). Recommended follow-up: either extract the session-lifecycle logic into a small, independently-testable class, or cover this via manual verification (see 5.3/5.4) until then.

## 4. Usage enrichment (`UiFramework` property + `SwitchToTool` dwell time)

- [x] 4.1 Call `Analytics.SetApplicationProperty("UiFramework", "WinForms")` once at startup in `FieldWorks.cs`, right after `Analytics` construction.
- [ ] 4.2 **GAP — not implemented.** No automated test verifies that events tracked after startup carry `UiFramework=WinForms`, because `SetApplicationProperty`'s effect is entirely internal to the `DesktopAnalytics` package (it tags outgoing Mixpanel payloads, which this codebase has no visibility into after the fact) and requires a live `Analytics` singleton to exercise at all. Verifiable only by manual/Mixpanel-side inspection (see 5.3/5.4).
- [x] 4.3 In `AreaListener.cs`, add a field tracking the last tool-activation timestamp, updated on every tool change regardless of the once-per-day throttle.
- [x] 4.4 When the throttled `SwitchToTool` event does fire, include a `duration` property computed from the stored timestamp (design.md D11), without changing the existing `area`/`tool` properties.
- [x] 4.5 Add a final dwell-time record at application shutdown for whatever tool is currently active, using the same `AnalyticsOutbox.Track` call shape.
- [ ] 4.6 **GAP — not implemented.** No unit test covers the dwell-time behavior itself (repeated same-day revisits still throttle but carry a duration; a delayed switch reports that delay; shutdown while a tool is active produces one final dwell record). `LexTextDllTests` (where `AreaListenerTests.cs` lives) has no `InternalsVisibleTo` access to `AnalyticsOutbox`'s test seams, so a test there cannot intercept what `AreaListener` actually sends without either (a) adding `InternalsVisibleTo("LexTextDllTests")` to `FwUtils.csproj` and threading the seam through, or (b) extracting the dwell-time computation into a small pure function in `AreaListener.cs` that can be unit-tested without going through `AnalyticsOutbox` at all. Recommended as a fast follow-up — the logic itself (`m_lastToolActivationUtc` diff) is simple and low-risk, but currently unverified by an automated test.

## 5. Full verification

- [x] 5.1 Run `.\build.ps1` for the full managed build (no native changes expected; confirmed no native rebuild was triggered — only managed projects relinked).
- [x] 5.2 Run `.\test.ps1` for the affected test projects and confirm zero regressions: `FwUtilsTests` (390/390 passed, includes the 15 new `AnalyticsOutboxTests`), `LexTextDllTests` (3/3), `FieldWorksTests` (34/34, 1 pre-existing skip), `ITextDllTests` (207/207, 1 pre-existing skip).
- [ ] 5.3 **MANUAL — not performed.** Exercise the outbox end-to-end in a real FieldWorks session: disconnect network, use FieldWorks to trigger a few tracked events, confirm files accumulate under `%LocalAppData%\SIL\FieldWorks\Analytics\Outbox\`, reconnect, relaunch, and confirm the directory empties. Needs a real FieldWorks launch with `OkToPingBasicUsageData` enabled; not covered by the automated suite.
- [ ] 5.4 **MANUAL — not performed.** Confirm the existing Tools > Options > Privacy checkbox (`OkToPingBasicUsageData`) still gates all telemetry, including newly-queued-but-not-yet-flushed events, per design.md D7. Needs a real FieldWorks launch to toggle the setting and observe behavior.
