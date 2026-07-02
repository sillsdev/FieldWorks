## Context

FieldWorks wraps `SIL.DesktopAnalytics` v4.0.0 (Mixpanel client) with a single `Analytics` instance constructed once at startup (`Src/Common/FieldWorks/FieldWorks.cs`) inside a `using` block spanning the app's lifetime, gated by `FwApplicationSettings.Reporting.OkToPingBasicUsageData`. Six files call `Analytics.Track`/`Analytics.ReportException` directly today: `FieldWorks.cs` (exceptions), `AreaListener.cs` (`SwitchToTool`), `TrackingHelper.cs` (import/export/help), `ConcordanceContainer.cs`, `ConfigureInterlinDialog.cs`, and `ObtainProjectMethod.cs`. The package itself has no local persistence: a `Track`/`ReportException` call that can't reach Mixpanel is silently dropped. `DesktopAnalytics` also already exposes an unused `Analytics.SetApplicationProperty(key, value)` mechanism (an "application property" attached to every subsequent event) and, per decompilation, an internal `AllowTracking` gate that mirrors the consent flag and a per-process cap of 10 exception reports (`MAX_EXCEPTION_REPORTS_PER_RUN`).

FieldWorks explicitly supports multiple simultaneous `FieldWorks.exe` processes (one project can be open per process; see the `CreateRemoteRequestListener` comment in `FieldWorks.cs` noting a cross-process mutex was considered and rejected in favor of a listener). Any new local persistence shared across processes must not assume single-process exclusivity.

## Goals / Non-Goals

**Goals:**
- Make analytics delivery durable across restarts and offline periods (`telemetry-outbox`).
- Add session-level baseline metrics: duration and crash-vs-clean outcome (`telemetry-session-baseline`).
- Add a forward-compatible, constant-valued `UiFramework` property and per-tool dwell time (`telemetry-usage-enrichment`).
- Keep the existing consent flag (`OkToPingBasicUsageData`) as the sole authority over whether any of this data is ever queued or sent.

**Non-Goals:**
- Building a general-purpose local message queue framework for reuse elsewhere in FieldWorks — this is scoped to analytics delivery only.
- Cross-process locking/mutex infrastructure — the design avoids needing it (see Decisions).
- Any code that reads, branches on, or depends on `UIMode`/Avalonia concepts, none of which exist on this branch.
- Changing the Mixpanel API key handling (pre-existing, out of scope per proposal).

## Decisions

### D1 — One file per queued event, not one shared queue file
Each queued event (a `Track` call's event name + properties, or a `ReportException` call's exception data) is written as its own small JSON file in an outbox directory, named `{UtcTicks:D19}_{Guid:N}.json`. FIFO order falls out of a plain filename sort — no separate index/manifest file is needed, and there is nothing to corrupt by a torn write to a shared file.

*Alternative considered*: a single append-only JSONL file. Rejected — it requires either a lock file or careful append semantics to be safe across the multi-process scenario above, and a torn write (crash mid-append) risks corrupting the tail record.

### D2 — Storage location: `FwDirectoryFinder.UserAppDataFolder("FieldWorks")`
This resolves to `%LocalAppData%\SIL\FieldWorks\` today (`Environment.SpecialFolder.LocalApplicationData` + company/app name), matching the one existing precedent for this exact helper (`XCore/XMessageBoxExManager.cs`'s `DialogResponses.xml`, which also does `Directory.CreateDirectory(path)` before first write). The outbox lives in an `Analytics\Outbox\` subdirectory of that path. This is per-user, consistent with the consent flag being a per-user setting, and is not the `.NET` `user.config` settings store (that's a distinct, unrelated mechanism already used for `OkToPingBasicUsageData` itself).

### D3 — All telemetry funnels through one new facade; outbox is the only thing that calls `Analytics.Track`/`ReportException`
A new `AnalyticsOutbox` class (`Src/Common/FwUtils/AnalyticsOutbox.cs`) exposes `Track(eventName, properties)` and `ReportException(exception, moreProperties)` with signatures matching the package's own methods. Every existing call site (`FieldWorks.cs`, `AreaListener.cs`, `TrackingHelper.cs`, `ConcordanceContainer.cs`, `ConfigureInterlinDialog.cs`, `ObtainProjectMethod.cs`) is migrated to call `AnalyticsOutbox` instead of `Analytics` directly. `AnalyticsOutbox.Track`/`ReportException`:
1. Check consent (see D7). If not consented, do nothing — no file is written.
2. Serialize the call to a new outbox file (D1).
3. Fire a best-effort, non-blocking attempt to flush just that new file immediately (so the common case — online — still delivers with negligible added latency).

This makes "durable by default" the only code path; there is no separate "send immediately, fall back to queue on failure" branch to keep in sync.

*Alternative considered*: keep direct `Analytics.Track` calls for the common case and only route through a queue on a caught delivery failure. Rejected — `Analytics.Track` is fire-and-forget internally (an async call whose result isn't awaited by the caller), so callers have no reliable failure signal to branch on in the first place; funneling everything through the outbox sidesteps that entirely.

### D4 — Cross-process claim via atomic file rename, not a lock manager
Before sending a queued file (whether it's the one just written or one found during a startup sweep), the flush routine attempts to rename it in place to a `.inflight` suffix. `File.Move` is atomic on the same volume and fails if the source is already gone or the destination exists — so a successful rename is a cheap, sufficient claim that no other process is (or already has) handled this file. A failed rename means another process claimed it first; skip and move on. `RetryUtility.Retry` (SIL.Core) wraps only the transient-IO case (e.g. a momentary AV/indexer lock), not the claim race itself, which the rename semantics already resolve.

### D5 — Delivery success = delete the claimed file
After a successful `Analytics.Track`/`ReportException` call (the real package method, called only from inside the flush routine), the `.inflight` file is deleted. If the underlying send throws, the `.inflight` file is renamed back to its original name so a later sweep retries it. This gives at-least-once delivery, not exactly-once: a crash between a successful send and the delete would cause a duplicate on the next sweep. Accepted — see Risks.

### D6 — Bounded growth: cap by count and age, evict oldest first
The outbox enforces a cap (default: 2,000 files or 30 days of age, whichever is hit first) during every sweep, deleting the oldest excess files without attempting to send them. Exact numbers are tunable constants, not a hard requirement of the spec.

### D7 — Consent is re-checked at flush time, not just enqueue time
If a user revokes consent after an event was queued but before it's flushed, the flush routine re-checks consent for each file just before claiming it and deletes (without sending) any file found while consent is currently off. This fails closed on privacy: once consent is off, nothing already queued goes out, even though it was consented-to at write time. `AnalyticsOutbox` consults `Analytics.AllowTracking` if it proves to be accessible (verify at implementation time — see Open Questions); otherwise it falls back to reading `FwApplicationSettings.Reporting.OkToPingBasicUsageData` directly, understanding that path won't reflect the `FEEDBACK` environment-variable override applied once at `Analytics` construction.

### D8 — Queued exceptions replay as `Track`, not `ReportException`
Replaying a queued exception through the real `Analytics.ReportException` at flush time would count against that run's `MAX_EXCEPTION_REPORTS_PER_RUN = 10` cap, potentially starving budget meant for exceptions actually happening in the current run. Instead, `AnalyticsOutbox` replays queued exception records via `Analytics.Track("QueuedExceptionReport", properties)`, reconstructing the same property shape (`Message`, `Stack Trace`, plus whatever `moreProperties` were captured) that `ReportException` would have sent, so the Mixpanel-side data stays structurally comparable without consuming the live run's cap.

### D9 — Flush triggers: enqueue-time, startup sweep, and a bounded shutdown sweep
Three flush triggers, no persistent background poller:
1. Immediately after any `AnalyticsOutbox.Track`/`ReportException` call (fire-and-forget, covers the common "online" case with minimal latency).
2. A full directory sweep on startup, after the `Analytics` instance and consent state are known — this is what delivers everything accumulated while offline.
3. A best-effort, time-bounded (≤2s) sweep at shutdown, so events generated late in a session don't have to wait for the next launch. Consistent with the existing package's own `ShutDown()` pattern (up to 7.5s polling for in-flight tasks).

*Alternative considered*: a network-change-event listener or a periodic timer while running. Rejected as unnecessary complexity — FieldWorks sessions are typically short enough, and startup/shutdown sweeps cover the realistic offline-then-online transition (user closes laptop, travels, reopens FieldWorks later).

### D10 — Session identity, correlation, and the single-terminal-event guard
A `SessionId` (`Guid.NewGuid()`) is generated once at startup and stored for the process's lifetime. Session-start is tracked (via `AnalyticsOutbox.Track`) right after the `Analytics` instance is constructed. Session-end is tracked from exactly one of two places — the normal fall-through at the end of the `using (new Analytics(...))` block (outcome `clean`), or `HandleUnhandledException`/`HandleTopLevelError` (outcome `crashed`) — guarded by `Interlocked.CompareExchange` on a static flag so whichever path runs first wins and the other is suppressed. Duration is `DateTime.UtcNow` at the reporting point minus the recorded start time.

### D11 — SwitchToTool dwell time via a stored "last activation" timestamp
`AreaListener` already has the property-changed handler that fires `SwitchToTool` on tool changes (throttled to once per tool per day). A new field records `DateTime.UtcNow` every time the active tool changes, regardless of whether the throttle allows an event to fire. When the (already-throttled) `SwitchToTool` event does fire, it includes a `duration` property computed from that stored timestamp — i.e., the dwell time since the tool was last activated, not a running total across a whole day (see Risks: this is a deliberate simplification consistent with the existing once-per-day throttle, not a bug). At application shutdown, if a tool is currently active, one additional dwell record is tracked for that final stretch (via `AnalyticsOutbox.Track`, same event shape) so it isn't silently lost.

## Risks / Trade-offs

- **[Risk] At-least-once delivery can double-count events** if a process crashes between a successful Mixpanel send and the local file delete → **Mitigation**: accepted trade-off; strictly better than today's silent-loss behavior, and Mixpanel-side usage analysis (adoption trends, crash rates) tolerates small duplicate noise far better than missing data.
- **[Risk] Six existing call sites must be migrated to the new facade in one mechanical pass** → **Mitigation**: `AnalyticsOutbox.Track`/`ReportException` match `Analytics.Track`/`ReportException`'s signatures exactly, so the migration is a pure substitution; existing tests around those call sites (where they exist) continue to apply.
- **[Risk] Cross-process sweep introduces a new race not present today** (two processes both attempt the same file) → **Mitigation**: the atomic-rename claim (D4) resolves the race to "one wins, one skips," never a crash or corruption.
- **[Risk] `SwitchToTool` dwell time reflects only the most recent stretch in a tool, not cumulative time across a whole day** (a consequence of preserving the existing once-per-day throttle, per spec) → **Mitigation**: documented here and in the spec; if cumulative dwell time is later wanted, that's a throttle-semantics change outside this proposal's scope.
- **[Risk] `Analytics.AllowTracking` may not be a public member** → **Mitigation**: fallback to `FwApplicationSettings.Reporting.OkToPingBasicUsageData` is specified in D7; either way, consent is re-checked at flush time.
- **[Risk] Outbox directory could grow unexpectedly large if Mixpanel is unreachable for a very long time** → **Mitigation**: D6's count/age cap bounds worst-case disk usage to a small, fixed footprint.

## Migration Plan

No user-facing or data-model migration: the outbox directory is new local state with no prior version to migrate from. Rollback (reverting this change) is safe — any leftover `Analytics\Outbox\` directory is inert and can be deleted or ignored; it is never read by anything except the code introduced here. Deployment is a normal FieldWorks release; no installer changes are needed since the directory is created on first use via `Directory.CreateDirectory`, matching the existing `XMessageBoxExManager` precedent.

## Open Questions

- Whether `Analytics.AllowTracking` (found via decompilation of `DesktopAnalytics.dll` v4.0.0) is accessible outside the package, or whether `AnalyticsOutbox` must rely solely on `FwApplicationSettings.Reporting.OkToPingBasicUsageData` — verify during implementation (task-level, not blocking).
- Exact `RobustFile`/`RobustIO` (SIL.Core.Desktop) method names available for the write/delete/rename operations in D1/D4/D5 — verify against the currently installed `SIL.Core.Desktop` version; fall back to plain `System.IO` calls wrapped in `RetryUtility.Retry` if a needed method isn't present.
- Whether `SessionId` should additionally ride along as an `Analytics.SetApplicationProperty("SessionId", ...)` so every other event (not just session-start/end) is automatically correlated to a session — not required by the spec as written; left as an implementation-time judgment call since it doesn't change any requirement's observable behavior.
- Final tunable values for D6's cap (2,000 files / 30 days are illustrative defaults) — open to adjustment without a spec change.
