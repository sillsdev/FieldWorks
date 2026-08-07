## ADDED Requirements

### Requirement: UiFramework application property
The application SHALL set a `DesktopAnalytics` application property named `UiFramework` with the constant value `WinForms` once at startup, via the existing `Analytics.SetApplicationProperty` mechanism, so it is attached to all subsequently tracked events and exception reports.

#### Scenario: Any event tracked after startup
- **WHEN** any `Analytics.Track` or `Analytics.ReportException` call occurs after startup has completed
- **THEN** the resulting Mixpanel event includes a `UiFramework` property with value `WinForms`

### Requirement: No dependency on UI-mode concepts
The `UiFramework` property SHALL be set to a fixed constant and SHALL NOT read from, branch on, or otherwise depend on any UI-mode or Avalonia-specific concept.

#### Scenario: Built without any Avalonia/UIMode code present
- **WHEN** the application is built and run on a codebase that contains no `UIMode`, `LexicalEditSurfaceResolver`, or other Avalonia-branch-only code
- **THEN** the `UiFramework` property still resolves to a valid constant value with no compile-time or run-time dependency on such code

### Requirement: SwitchToTool dwell-time enrichment
The existing `SwitchToTool` event SHALL include a duration property representing the time spent in the previously active tool before the switch (or before application exit).

#### Scenario: User switches tools after working in one
- **WHEN** a user works in tool A for a period of time and then switches to tool B
- **THEN** the `SwitchToTool` event recorded for that switch includes a duration reflecting the time spent in tool A

### Requirement: Preserve existing throttle and properties
The dwell-time enrichment SHALL NOT change the existing `area`/`tool` properties on `SwitchToTool`, nor change its existing once-per-tool-per-day throttle behavior.

#### Scenario: Same tool revisited multiple times in one day
- **WHEN** a user switches into the same tool more than once within the same day
- **THEN** `SwitchToTool` still fires at most once for that (tool, day) pair as before, now additionally carrying a duration value

### Requirement: Final dwell time captured at shutdown
Dwell time accumulated in the tool active at the moment the application exits SHALL be captured and tracked rather than silently discarded because no further tool switch occurs.

#### Scenario: User exits while a tool is active
- **WHEN** the user exits the application while a tool is the currently active one
- **THEN** a dwell-time record reflecting that tool's final active period is tracked as part of shutdown
