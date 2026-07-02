## ADDED Requirements

### Requirement: Consent-gated persistence
The outbox SHALL persist an analytics event or exception report locally only if `OkToPingBasicUsageData` is `true` at the moment the event is generated. It SHALL NOT accumulate events on disk while consent is off.

#### Scenario: Consent disabled at event time
- **WHEN** an event or exception is generated and `OkToPingBasicUsageData` is `false`
- **THEN** the outbox does not write it to local storage and the event is dropped, matching today's behavior for non-consenting users

#### Scenario: Consent enabled at event time
- **WHEN** an event or exception is generated and `OkToPingBasicUsageData` is `true`, but immediate delivery to Mixpanel is not possible
- **THEN** the outbox persists the event to local durable storage for later retry

### Requirement: Durability across restarts
Events written to the outbox SHALL survive an application restart, so an event queued during one process run can still be delivered by a later run.

#### Scenario: App restarts with pending events
- **WHEN** the outbox holds one or more undelivered events at the time the application exits or crashes
- **THEN** those events are still present in the outbox the next time the application starts

### Requirement: Retry and flush on connectivity
The outbox SHALL attempt to deliver persisted events to Mixpanel via the existing `Analytics.Track`/`Analytics.ReportException` calls when a network path is available, without blocking the UI thread.

#### Scenario: Network available on next launch
- **WHEN** the application starts and previously queued events are present, and Mixpanel is reachable
- **THEN** the outbox flushes the queued events in the background without blocking application startup or the UI thread

#### Scenario: Network still unavailable
- **WHEN** a flush attempt is made and Mixpanel is not reachable
- **THEN** the events remain persisted for a later retry and no data is lost

### Requirement: FIFO delivery order
The outbox SHALL flush persisted events in the order they were originally enqueued.

#### Scenario: Multiple queued events flushed
- **WHEN** the outbox holds multiple persisted events accumulated across one or more offline sessions
- **THEN** it flushes them to Mixpanel in the same order they were originally recorded

### Requirement: Bounded growth
The outbox SHALL cap total stored events (by count and/or age) to prevent unbounded local disk growth during extended offline periods.

#### Scenario: Extended offline period exceeds the cap
- **WHEN** the number or age of persisted events exceeds the configured cap
- **THEN** the oldest events are evicted first and the outbox size remains within the cap

### Requirement: Exception-report cap interaction
Exception reports flushed from the outbox SHALL be subject to the same per-process-run cap (currently 10 reports per run, enforced internally by `DesktopAnalytics`) as live exceptions generated during that run.

#### Scenario: Flushing queued exceptions on a fresh run
- **WHEN** the outbox flushes previously queued exception reports during a run that also generates its own live exceptions
- **THEN** the combined total of flushed and live exception reports sent during that run does not exceed the package's existing per-run cap

### Requirement: Non-blocking shutdown
Outbox flush attempts triggered at application shutdown SHALL be time-bounded so they do not materially delay the application from exiting.

#### Scenario: Shutdown with a pending flush in progress
- **WHEN** the application begins shutting down while an outbox flush is in progress
- **THEN** the flush attempt is bounded by a timeout and the application exits without hanging, leaving any undelivered events persisted for the next run
