## ADDED Requirements

### Requirement: Legacy truth PNGs are captured for the reachable Phase-1 set

The toolkit SHALL produce a legacy-WinForms screenshot for each reachable Phase-1 capture target in
`Docs/migration/INVENTORY.md`, written to the image path that target's doc references, captured from
the Sena 3 language project with `UIMode=Legacy`. It SHALL NOT modify the language project.

#### Scenario: A tool screen is captured by link-driven launch
- **WHEN** the capture script processes a tool target with id `<toolId>`
- **THEN** it SHALL launch `FieldWorks.exe -db "Sena 3"` with a `silfw://…&tool=<toolId>` link, wait
  for the main window, and save a non-blank screenshot to the target's `images/<screen>-NN.png`

#### Scenario: A constructable dialog is captured by the harness
- **WHEN** the harness processes a dialog target registered with a factory
- **THEN** it SHALL open the Sena 3 cache, construct the dialog via its factory with seeded data,
  render it, and save a non-blank screenshot to the target's image path

#### Scenario: An unreachable target is recorded, not silently skipped
- **WHEN** a target is Views/Gecko-coupled, fails to construct headless, or is a non-visual internal
- **THEN** it SHALL be recorded in `capture-ledger.md` with a reason and an `on-pickup` disposition,
  and SHALL NOT be reported as captured

### Requirement: Each surface gets a before/after pair from the same data, attached to its JIRA ticket

Each migration doc SHALL present the legacy "before" PNG and the Avalonia "after" PNG of the SAME
seeded data side by side (`<name>-before.png` / `<name>-after.png` in the doc's `images/`). The
"after" SHALL be produced by the surface's Avalonia visual test (the `fieldworks-semantic-render-parity`
lane), not an ad-hoc screenshot. Both PNGs SHALL be attached to the surface's JIRA ticket.

#### Scenario: Before/after use the same data
- **WHEN** a surface has both a legacy and an Avalonia implementation
- **THEN** the "before" and "after" PNGs SHALL be rendered from the same seeded objects/input so the
  visual comparison is honest, and the doc SHALL show them side by side

#### Scenario: The after-lane is the parity visual test, not a one-off
- **WHEN** the Avalonia "after" PNG is produced
- **THEN** it SHALL come from the surface's render/visual parity test so it doubles as a parity baseline

### Requirement: Navigation and capture use supported, non-destructive paths

The toolkit SHALL drive surfaces through supported code paths — the `FwAppArgs`/`FwLinkArgs` link
mechanism for tools and `new Dlg()` + `SetDlgInfo(...)` construction for dialogs — and SHALL leave
the language project unchanged (open read-only or discard; dialogs closed via Cancel/Escape).

#### Scenario: Capture never commits project changes
- **WHEN** any capture target has been processed
- **THEN** the Sena 3 project data SHALL be unchanged from before the run

### Requirement: The harness is a non-shipping capture utility

The screenshot harness SHALL be non-shipping: either an `[Explicit]` test fixture in an existing
test project or a standalone dev tool. It SHALL NOT be installed, SHALL NOT run in the normal test
suite, and SHALL NOT be referenced by any product code path.

#### Scenario: No product code depends on the harness, and it stays out of the normal suite
- **WHEN** the harness is added to the build
- **THEN** no product (shipping) MSBuild project SHALL reference it, the installer SHALL NOT include
  it, and a default `./test.ps1` run SHALL NOT execute its captures (it runs only under an explicit
  capture filter)
