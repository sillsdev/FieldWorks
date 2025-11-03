# Requirements Checklist

- [ ] Confirm Advanced New Entry command opens the Avalonia-based view without legacy dialog dependencies (FR-001).
- [ ] Validate property grid enforces required LexEntry fields and blocks save when incomplete (FR-002).
- [ ] Verify senses, pronunciations, and variants can be authored before save and persist correctly (FR-003, FR-004).
- [ ] Ensure multi-writing-system input works across property grid fields and summary preview (FR-006).
- [ ] Exercise template creation, application, and override behavior for default profiles (FR-007, FR-008).
- [ ] Capture diagnostics for validation failures and cancelled operations in existing logging channels (FR-009, FR-010).
- [ ] Verify no writes occur during edit; Save performs a single LCModel transaction (FR-011).
- [ ] Confirm Save appears as one undoable operation in the standard undo stack (FR-012).
