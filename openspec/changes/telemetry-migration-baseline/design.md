## Context

FieldWorks wraps `SIL.DesktopAnalytics` (Mixpanel client) with a single `Analytics` instance constructed once at startup (`Src/Common/FieldWorks/FieldWorks.cs`) inside a `using` block spanning the app's lifetime, gated by `FwApplicationSettings.Reporting.OkToPingBasicUsageData`. Several files call `Analytics.Track`/`Analytics.ReportException` directly today: `FieldWorks.cs` (exceptions), `AreaListener.cs` (`SwitchToTool`), `TrackingHelper.cs` (import/export/help), `ConcordanceContainer.cs`, `ConfigureInterlinDialog.cs`, and `ObtainProjectMethod.cs`. `DesktopAnalytics` also already exposes an unused `Analytics.SetApplicationProperty(key, value)` mechanism (an "application property" attached to every subsequent event) and an internal per-process cap of 10 exception reports (`MAX_EXCEPTION_REPORTS_PER_RUN`).

Offline durability — delivering events generated while Mixpanel is unreachable — is provided by the `SIL.DesktopAnalytics` library itself, not by FieldWorks (see D4). This change therefore adds no FieldWorks-side persistence, queue, or retry: it instruments session and usage baseline data on top of the existing direct `Analytics` calls.

## Goals / Non-Goals

**Goals:**
- Add session-level baseline metrics: duration and crash-vs-clean outcome (`telemetry-session-baseline`).
- Add a forward-compatible, constant-valued `UiFramework` property and per-tool dwell time (`telemetry-usage-enrichment`).
- Keep the existing consent flag (`OkToPingBasicUsageData`) as the sole authority over whether any of this data is ever sent.

**Non-Goals:**
- Any FieldWorks-side offline queue/retry/persistence for analytics — that lives in the `SIL.DesktopAnalytics` library, not here (D4).
- Any code that reads, branches on, or depends on `UIMode`/Avalonia concepts, none of which exist on this branch.
- Changing the Mixpanel API key handling (pre-existing, out of scope per proposal).

## Decisions

### D1 — Forward-compatible `UiFramework` application property, set once at startup
`Analytics.SetApplicationProperty("UiFramework", "WinForms")` is called once, right after the `Analytics` instance is constructed, so every subsequently tracked event and exception report is segmentable by UI framework. The value is a constant today — this codebase has only one UI framework — and a future Avalonia surface sets a different value on the same property without changing how anything is tracked. No code reads or depends on a `UIMode` value (none exists on this branch).

### D2 — Session identity, correlation, and the single-terminal-event guard
A `SessionId` (`Guid.NewGuid()`) is generated once at startup and stored for the process's lifetime. Session-start is tracked right after the `Analytics` instance is constructed. Session-end is tracked from exactly one of two places — the normal fall-through in `Main`'s `finally` (outcome `clean`), or `HandleUnhandledException`/`HandleTopLevelError` (outcome `crashed`) — guarded by `Interlocked.CompareExchange` on a static flag so whichever path runs first wins and the other is suppressed. Duration is `DateTime.UtcNow` at the reporting point minus the recorded start time. The `finally` placement covers every normal exit (including early `return`s inside the `try`), which placing the call "at the end of" the `using` block would skip.

### D3 — SwitchToTool dwell time via a stored "last activation" timestamp
`AreaListener` already fires `SwitchToTool` on tool changes (throttled to once per tool per day). A new field records `DateTime.UtcNow` every time the active tool changes, regardless of whether the throttle allows an event to fire. When the (already-throttled) `SwitchToTool` event does fire, it includes a `duration` property computed from that stored timestamp — i.e., the dwell time since the tool was last activated, not a running total across a whole day (a deliberate simplification consistent with the existing throttle, not a bug — see Risks). At application shutdown, if a tool is currently active, one additional dwell record is tracked for that final stretch so it isn't silently lost.

### D4 — Offline durability is the `SIL.DesktopAnalytics` library's responsibility, not FieldWorks'
FieldWorks calls `Analytics.Track`/`Analytics.ReportException` directly. Making those calls durable when Mixpanel is unreachable — an on-disk spool, retry/backoff, a real per-event delivery ack (confirmed HTTP 2xx, not merely "didn't throw"), `$insert_id` dedup so at-least-once replay is deduplicated, and consent-revocation purge — is implemented once, in the library's durable `MixpanelClient`, so every consumer benefits and FieldWorks carries no bespoke queueing code. Until a FieldWorks build references that durable library version, offline-generated events are delivered best-effort exactly as they are today; that is an accepted, temporary gap, not something this change works around locally. Local iteration against an unreleased library build is supported via `Build/Manage-LocalLibraries.ps1 -DesktopAnalytics` (see `Docs/architecture/local-library-debugging.md`), and the two `scripts/Test-AnalyticsOffline*.ps1` smoke tests validate that library's durable spool directly, without a FieldWorks launch.

## Risks / Trade-offs

- **[Risk] `SwitchToTool` dwell time reflects only the most recent stretch in a tool, not cumulative time across a whole day** (a consequence of preserving the existing once-per-day throttle) → **Mitigation**: documented here and in the spec; if cumulative dwell time is later wanted, that's a throttle-semantics change outside this change's scope.
- **[Risk] Offline-generated events can still be lost until FieldWorks references the durable `SIL.DesktopAnalytics` build** → **Mitigation**: accepted, temporary; durability is delivered by a library version bump (D4), not FieldWorks-side code, and is no worse than today's behavior in the meantime.

## Open Questions

- Whether `SessionId` should additionally ride along as `Analytics.SetApplicationProperty("SessionId", ...)` so every other event (not just session-start/end) is automatically correlated to a session — not required by the spec. **Decision for this implementation pass: not added**, to keep the enrichment scoped exactly to what the spec requires.
