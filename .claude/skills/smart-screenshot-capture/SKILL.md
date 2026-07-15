---
name: smart-screenshot-capture
description: >
  Capture high-quality screenshots with MCP tools. Use when the user asks to
  take a screenshot, show the screen, capture a window, document UI evidence,
  collect before/after images, inspect browser output, or verify a desktop app
  visually with WinForms MCP, WinApp MCP, browser screenshots, image previews,
  annotations, or screenshot diffs.
license: MIT
compatibility: >
  Windows-first adaptation for VS Code agents with WinForms MCP, WinApp MCP,
  browser page tools, and image preview tools. Inspired by brennacodes'
  screenshotr skill, but adapted away from macOS screencapture/sips commands.
metadata:
  author: FieldWorks team
  version: "1.0"
  upstream: https://mcpmarket.com/tools/skills/smart-screenshot-capture
---

# Smart Screenshot Capture

Use this skill to capture clear, reviewable screenshots without making the user
manually crop, upload, or explain the screen. Prefer focused captures of the
target app, window, browser page, or element over whole-desktop images.

## Core Rules

- Infer the target from the conversation when it is obvious, such as the active
  FieldWorks dialog, a named app, a browser page, or a before/after UI state.
- Ask one short question only when the target is ambiguous enough that a wrong
  capture is likely.
- Save screenshots to a deterministic path before reporting them.
- Generate concise kebab-case filenames from the target and state.
- Display or inspect the saved image after capture with `view_image` or the
  appropriate image preview tool.
- Retake the screenshot when it is blank, the wrong app, overlapped by another
  window, too small to read, or missing the state the user asked to document.
- Prefer non-mutating inspection and capture tools. Do not change project data
  just to make a screenshot easier.

## Target Selection

Choose the narrowest useful target:

1. FieldWorks or another WinForms desktop app: capture by process id or window
   handle when the MCP tool supports it.
2. A visible Windows app where WinForms MCP cannot attach: use WinApp MCP and a
   specific `appId` or `windowHandle`.
3. Browser content controlled by the integrated browser: use `screenshot_page`
   for the whole viewport or an element-specific capture.
4. A UI element that needs visual callouts: capture the app window, then use
   annotation tools when available.
5. Whole desktop: use only when the user asks for desktop context or no focused
   capture path exists.

When capturing a desktop app, inspect the UI tree or window list first if there
is any risk of targeting the wrong window.

## Output Paths

For this repository, default to:

- transient evidence: `Output/ManualEvidence/<ticket-or-change-id>/`
- OpenSpec review evidence: `openspec/changes/<change-id>/evidence/manual-winapp/`
- Path 3 parity bundle evidence: `openspec/changes/<change-id>/evidence/parity/<scenario-id>/`
- ad hoc screenshots: `Output/ManualEvidence/screenshots/`

Create the folder if needed. Do not put scratch screenshots in committed
evidence folders unless the user asks for review-ready evidence.

## Naming

Use sorted, descriptive names:

- single capture: `<target>-<state>.png`
- before/after: `01-before-<state>.png`, `02-after-<state>.png`
- Path 3 parity: `01-winforms-<scenario>.png`, `02-avalonia-<scenario>.png`, `03-diff-<scenario>.png`
- sequence: `step-01-<state>.png`, `step-02-<state>.png`
- app tour: `<app>-<window-or-dialog>.png`
- temporary fallback: `screenshot-YYYY-MM-DD-HHMMSS.png`

Keep generated names short, lowercase, and kebab-case. Prefer 2-5 meaningful
words over timestamps unless ordering or uniqueness requires a timestamp.

## Tool Preference

Use the best available tool for the current target:

- FieldWorks/WinForms hidden desktop: `mcp_winforms-mcp_winforms_take_screenshot`.
- WinApp visible desktop: `mcp_winapp_take_screenshot_optimized` when image size
  matters, otherwise `mcp_winapp_take_screenshot`.
- UI callouts: `mcp_winapp_annotate_screenshot`.
- Visual comparison: `mcp_winapp_screenshot_diff`.
- Browser pages: `screenshot_page`.
- Saved image review: `view_image`.

Prefer MCP screenshot tools over shell commands. Do not use the upstream
macOS-specific `screencapture`, `sips`, or AppleScript workflow on Windows.

## Capture Workflow

1. Identify the target and output path.
2. Inspect the app/page/window when needed to confirm the target is visible.
3. Capture to a deterministic filename.
4. Inspect the saved image.
5. Retake or annotate if the first capture does not communicate the requested
   state clearly.
6. Report the saved path and any useful capture metadata, such as dimensions or
   diff percentage.

For before/after work, capture the before state first whenever it is available.
If the before state is unavailable in the current worktree, say so and capture
the fixed or current state with a clear filename.

## Multi-Screenshot Workflows

Use multiple captures when one image cannot tell the story:

- before/after: two captures with matching framing;
- step sequence: one capture per user-visible step;
- app tour: each relevant dialog, pane, or tab;
- redraw/focus/modal timing: a short ordered sequence rather than a single
  screenshot;
- comparison: capture both images, then run a screenshot diff when available.

For migration parity bundles, keep framing, DPI, zoom, and window size matched across WinForms and Avalonia captures whenever density, wrapping, or spacing is under review.

For a Path 3 parity bundle, pair screenshots with the matching semantic snapshot and workflow/accessibility evidence for the same scenario id; a screenshot pair alone is not a full parity claim.

For sequences, keep the same target, window size, and framing across captures
unless the task is specifically about responsive or layout behavior.

## Quality Checklist

Before considering the screenshot done, verify:

- the image exists at the reported path;
- the target app, page, or element is visible;
- text needed for review is readable;
- modal dialogs, focus state, selected tabs, and highlighted controls match the
  requested state;
- no unrelated foreground window covers the target;
- committed evidence contains only review-worthy images.

If a screenshot tool produces a cropped, blank, or wrong-window capture, switch
drivers in this order: focused WinForms capture, WinApp optimized capture,
browser capture, then whole-window or whole-desktop fallback.