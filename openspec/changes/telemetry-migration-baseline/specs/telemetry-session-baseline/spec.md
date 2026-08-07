## ADDED Requirements

### Requirement: Session-start event
The application SHALL track a session-start event once per process launch, gated by the existing `OkToPingBasicUsageData` consent flag, carrying a session identifier unique to that launch.

#### Scenario: App launches with consent enabled
- **WHEN** the application finishes startup and `OkToPingBasicUsageData` is `true`
- **THEN** a session-start event is tracked, carrying a session identifier not reused by any other launch

### Requirement: Session-end event on clean shutdown
The application SHALL track a session-end event with a `clean` outcome when it exits through its normal shutdown path.

#### Scenario: User closes the app normally
- **WHEN** the application shuts down through its normal exit path with no unhandled exception
- **THEN** a session-end event is tracked with outcome `clean`, correlated to the session-start event for that launch

### Requirement: Session-end classification on crash
The application SHALL track a session-end event with a `crashed` outcome when termination occurs via the global unhandled-exception handling path, distinguishable from a clean shutdown.

#### Scenario: Unhandled exception terminates the app
- **WHEN** `HandleUnhandledException` or `HandleTopLevelError` handles an exception that results in the application terminating
- **THEN** a session-end event is tracked with outcome `crashed`, correlated to the session-start event for that launch

### Requirement: Session duration
The session-end event SHALL include the elapsed wall-clock duration since the corresponding session-start event.

#### Scenario: Session lasts a measurable duration
- **WHEN** a session-end event is tracked
- **THEN** it carries a duration value reflecting the elapsed time since that session's session-start event

### Requirement: Session correlation identifier
Session-start and session-end events belonging to the same process run SHALL share a common session identifier, and that identifier SHALL NOT be reused across different launches.

#### Scenario: Multiple launches produce distinct sessions
- **WHEN** the application is launched, exited, and launched again
- **THEN** each launch's session-start and session-end events share one identifier, and the two launches' identifiers differ

### Requirement: Single terminal event per session
At most one session-end event SHALL be recorded per session-start event; a crash-path session-end SHALL prevent a duplicate clean-path session-end from also firing for the same session.

#### Scenario: Crash path already recorded the session end
- **WHEN** the crash-handling path has already tracked a `crashed` session-end event for the current session
- **THEN** no additional `clean` session-end event is tracked for that same session
