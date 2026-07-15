## ADDED Requirements

### Requirement: Global command and focus behavior uses a FieldWorks-owned bridge

Global command routing, active-target resolution, popup focus return, and shell-level command state SHALL use a FieldWorks-owned command and focus bridge rather than relying solely on direct Avalonia control bindings.

#### Scenario: Global command resolves active target
- **WHEN** a migrated global command such as save, delete, or find is invoked from a menu, toolbar, shortcut, or context menu
- **THEN** the command SHALL resolve the active target through the FieldWorks-owned command and focus bridge

### Requirement: Local Avalonia commands remain allowed inside screens

Migrated screens SHALL be allowed to use direct Avalonia `ICommand`, `KeyBinding`, `HotKey`, CommunityToolkit commands, or similar local command helpers for screen-local behavior when they do not replace the global command and focus bridge.

#### Scenario: Local editor command stays local
- **WHEN** a screen-local editor or popup handles a command that does not require shell-wide target resolution
- **THEN** the screen MAY bind that behavior directly through local Avalonia or MVVM command helpers

### Requirement: Command descriptors separate execution from display state

The shell command model SHALL keep stable command identity, visibility, checked state, enabled state, gestures, and diagnostics separate from the execution mechanism.

#### Scenario: Menu and toolbar share command descriptor
- **WHEN** a command appears in more than one shell surface
- **THEN** those surfaces SHALL be driven from a shared command descriptor rather than duplicating per-surface state logic
