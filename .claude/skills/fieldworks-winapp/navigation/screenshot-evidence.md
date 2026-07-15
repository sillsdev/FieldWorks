# Capture Manual Screenshot Evidence

Use this path when the task needs before/after images, Jira evidence, OpenSpec
evidence, or visual proof that a FieldWorks UI flow works.

For general screenshot quality rules, target inference, sorted filenames,
annotations, and retake criteria, read `../../smart-screenshot-capture/SKILL.md`
alongside this route.

## Entry State

- FieldWorks is launched and foregrounded.
- The target UI state is visible or reachable through another navigation path.

## Steps

1. Create a deterministic evidence folder:
   - transient: `Output/ManualEvidence/<ticket-or-change-id>/`
   - committed OpenSpec evidence:
     `openspec/changes/<change-id>/evidence/manual-winapp/`
2. Confirm the target FieldWorks process, window, dialog, or tab before capture.
3. Capture the initial loaded-project state when it helps reviewers orient
   themselves.
4. Capture the broken/reproduced state from an unfixed build when available.
5. Capture the fixed state from the current build.
6. Inspect each saved image and retake if it is blank, wrong-window, covered,
  unreadable, or missing the intended state.
7. Capture related canonical surfaces when the issue spans more than one UI
   path.
8. Use an image sequence or GIF when the issue involves timing, redraw, focus,
   modal transitions, or steps that a single screenshot cannot communicate.

## MCP Choice

- Prefer `winforms_take_screenshot` for fresh FieldWorks launches; hidden
  desktop screenshots should not steal focus or require foregrounding.
- Prefer passing the FieldWorks `pid` to WinForms MCP screenshots when the
  client exposes a process parameter.
- Prefer `mcp_winapp_take_screenshot_optimized` for visible-desktop fallback
  captures when image size or token budget matters.
- Use `mcp_winapp_take_screenshot` when WinForms MCP is unavailable, when the
  task is explicitly about the visible desktop, or when comparing UIA3 behavior.
- Use `mcp_winapp_annotate_screenshot` when reviewers need specific controls or
  changed regions called out.
- Use `mcp_winapp_screenshot_diff` for before/after comparisons when pixel
  changes are the evidence.

## Naming

Use names that tell the story in sorted order:

- `01-initial-<state>.png`
- `02-before-<screen>.png`
- `03-after-<screen>.png`
- `04-after-<related-screen>.png`
- Path 3 parity bundle: `01-winforms-<scenario>.png`, `02-avalonia-<scenario>.png`, `03-diff-<scenario>.png`
- `sequence-<scenario>-001.png`, `sequence-<scenario>-002.png`, ...

When the task is migration parity, capture matched WinForms and Avalonia framing for the same scenario id and store them under `openspec/changes/<change-id>/evidence/parity/<scenario-id>/` so the visual lane lines up with the semantic snapshot and the workflow/accessibility evidence.

## Expected Signals

- Screenshots should show FieldWorks, not VS Code or another foreground app.
- WinForms MCP screenshots can capture headless FieldWorks windows through
  `PrintWindow`; foreground-window order should not matter for that path.
- If `mcp_winapp_take_screenshot` captures the wrong surface, use the launch
  route's `ALT+ESCAPE` foregrounding workaround and capture again.
- If a true before-state is not available in the current worktree, document why
  and describe how to capture it from an unfixed build.

## Exit

Copy only review-worthy screenshots into committed evidence folders. Leave
scratch captures under `Output/ManualEvidence` unless the user asks to commit
them.
