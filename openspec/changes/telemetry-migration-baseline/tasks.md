## 1. Usage enrichment — `UiFramework` property (`Src/Common/FieldWorks/FieldWorks.cs`)

- [x] 1.1 Call `Analytics.SetApplicationProperty("UiFramework", "WinForms")` once at startup, right after `Analytics` construction (design.md D1).
- [ ] 1.2 **GAP — not implemented.** No automated test verifies that events tracked after startup carry `UiFramework=WinForms`: `SetApplicationProperty`'s effect is internal to the `DesktopAnalytics` package (it tags outgoing Mixpanel payloads, which this codebase has no visibility into after the fact) and requires a live `Analytics` singleton to exercise at all. Verifiable only by manual/Mixpanel-side inspection (§4.3).

## 2. Session baseline (`Src/Common/FieldWorks/FieldWorks.cs`)

- [x] 2.1 Generate a `SessionId` (`Guid.NewGuid()`) once at startup and store it for the process lifetime.
- [x] 2.2 Track a session-start event immediately after the `Analytics` instance is constructed, carrying `SessionId`.
- [x] 2.3 Add the `Interlocked.CompareExchange`-guarded single-terminal-event flag (design.md D2).
- [x] 2.4 Track a `clean`-outcome session-end event in `Main`'s `finally`, including duration since session-start.
- [x] 2.5 Track a `crashed`-outcome session-end event from `HandleUnhandledException`/`HandleTopLevelError`, including duration.
- [ ] 2.6 **GAP — not implemented.** Unit/integration tests for session-start/end tracking. `FieldWorks.Main`'s startup/shutdown sequence is not currently structured for isolated unit testing (it constructs a real `Analytics` singleton, which can only happen once per process). Recommended follow-up: extract the session-lifecycle logic into a small, independently-testable class, or cover via manual verification (§4.3) until then.

## 3. Usage enrichment — `SwitchToTool` dwell time (`Src/LexText/LexTextDll/AreaListener.cs`)

- [x] 3.1 Add a field tracking the last tool-activation timestamp, updated on every tool change regardless of the once-per-day throttle.
- [x] 3.2 When the throttled `SwitchToTool` event does fire, include a `duration` property computed from the stored timestamp (design.md D3), without changing the existing `area`/`tool` properties.
- [x] 3.3 Add a final dwell-time record at application shutdown for whatever tool is currently active.
- [ ] 3.4 **GAP — not implemented.** No unit test covers the dwell-time behavior itself (repeated same-day revisits still throttle but carry a duration; a delayed switch reports that delay; shutdown while a tool is active produces one final record). Recommended follow-up: extract the dwell-time computation (`m_lastToolActivationUtc` diff) into a small pure function that can be unit-tested directly — the logic is simple and low-risk but currently unverified by an automated test.

## 4. Verification

- [x] 4.1 Run `.\build.ps1` for the full managed build (no native changes expected; only managed projects relinked).
- [x] 4.2 Run `.\test.ps1` for the affected test projects and confirm zero regressions vs. `main`: `FwUtilsTests` 375/375, `LexTextDllTests` 3/3, `FieldWorksTests` 34/34 (1 pre-existing skip).
- [ ] 4.3 **MANUAL — not performed.** In a real FieldWorks launch with `OkToPingBasicUsageData` enabled, confirm session-start/end and `SwitchToTool` dwell events appear in Mixpanel carrying the `UiFramework=WinForms` property, and that the Tools > Options > Privacy checkbox still gates all telemetry. Not covered by the automated suite.

## 5. Offline durability (external — `SIL.DesktopAnalytics` library)

Offline durability is provided by the `SIL.DesktopAnalytics` library's durable `MixpanelClient` (on-disk spool, retry/backoff, real per-event delivery ack, `$insert_id` dedup, consent purge), **not** by any FieldWorks-side code (design.md D4). Local iteration against an unreleased build is supported via `Build/Manage-LocalLibraries.ps1 -DesktopAnalytics` (see `Docs/architecture/local-library-debugging.md`).

- [ ] 5.1 **EXTERNAL (DesktopAnalytics.net repo).** Release the durable-Mixpanel `SIL.DesktopAnalytics` version. Smoke-tested directly (without a FieldWorks launch) via `scripts/Test-AnalyticsOfflineDrain.ps1` (per-process firewall block, accumulate-then-drain across processes) and `scripts/Test-AnalyticsOfflineSandbox.ps1` (Windows Sandbox, no guest NIC — born-offline, never falsely acked). Both PASS as of 2026-07-09 against the live Mixpanel endpoint (PR sillsdev/DesktopAnalytics.net#43).
- [ ] 5.2 Once released, bump `SilDesktopAnalyticsVersion` in `Build/SilVersions.props` so FieldWorks inherits durable offline delivery with no other code change (diff the 4.0.0 → new API surface first; verify CPM transitive pinning for the library's new DiskQueue/Polly dependencies).
