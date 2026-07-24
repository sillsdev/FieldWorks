# Research Notes

These sources informed the FieldWorks WinApp skill structure.

## Agent Skills and progressive disclosure

- Anthropic Engineering, "Equipping agents for the real world with Agent
  Skills" explains that a skill is a directory with `SKILL.md`, and that large
  or scenario-specific content should be split into referenced files so the
  agent loads only the context it needs.
  https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills
- Agent Skills overview describes progressive disclosure as discovery from
  metadata, activation through `SKILL.md`, and execution through optional
  referenced files, scripts, and resources.
  https://agentskills.io/

Applied here: `SKILL.md` stays as a compact trigger and index, while each
navigation path lives in a separate markdown file.

## Page Object / navigation encapsulation

- Playwright's Page Object Model docs recommend encapsulating common operations
  and selectors in one place to make UI automation easier to author and
  maintain.
  https://playwright.dev/docs/pom
- Selenium's Page Object Model docs emphasize clean separation between tests and
  page-specific locators/layout, plus a single repository for operations a page
  offers.
  https://www.selenium.dev/documentation/test_practices/encouraged/page_object_models/
- Martin Fowler's Page Object article says a page object should wrap a page or
  fragment with an application-specific API, hide widget details, model
  significant user-facing elements rather than every HTML/control container, and
  generally leave assertions to tests.
  https://martinfowler.com/bliki/PageObject.html

Applied here: a navigation file owns menu paths, stable automation IDs,
workarounds, expected loaded-state cues, and safe exits for one user-facing
destination. Jira/OpenSpec/test notes own pass/fail assertions.

## How-to documentation shape

- Diataxis how-to guide guidance says practical guides should be task-oriented,
  goal-focused, action-based, named clearly, and not overloaded with reference
  detail.
  https://diataxis.fr/how-to-guides/

Applied here: navigation files are named by user goal and contain just enough
sequence, cues, and safety notes to perform that route.

## Resulting convention

- One reusable navigation destination or workflow per `navigation/*.md` file.
- Shared safety and triggering rules stay in `SKILL.md`.
- Skill maintenance rules stay in `references/how-to-update.md`.
- Research rationale stays in this file so future edits can revisit the design
  without bloating the active route instructions.

## WinForms MCP headless automation

- `fnrhombus/winforms-mcp` supports `npx -y @fnrhombus/winforms-mcp` MCP setup,
  `HEADLESS=true` hidden desktop launches, `TFM=net48`, and
  `TELEMETRY_OPTOUT=true`.
  https://github.com/fnrhombus/winforms-mcp
- Its headless mode creates a hidden Windows desktop, launches processes there,
  routes UIA operations per process, and captures screenshots via `PrintWindow`.
  https://github.com/fnrhombus/winforms-mcp/wiki/Headless-Mode
- Hidden desktop supports UIA-pattern tools such as element discovery, tree
  inspection, invoke/toggle/selection actions, value setting, and screenshots.
  Input-simulation tools such as `send_keys`, drag/drop, and double-click need
  the visible desktop.

Applied here: FieldWorks is currently WinForms, so WinForms MCP is the default
for new live-app automation. WinApp MCP remains the UIA3 fallback and the right
tool for visible desktop/window diagnostics.

**FieldWorks-specific caveat (supersedes the upstream claim above):** empirically, on this
codebase, `winforms-mcp` `HEADLESS=true` does **not** render FieldWorks — the native Views
(C++/COM, GDI) engine requires a display-bound desktop for its first layout/paint, and the
hidden-desktop mode's `CreateDesktop` desktop is never display-bound, with or without a virtual
display driver. See `headless-rendering.md` in this skill for the full investigation and the
supported alternative (visible console launch, or an RDP/second-session workaround).
