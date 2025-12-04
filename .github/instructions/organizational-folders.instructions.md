````instructions
---
applyTo: "Src/**"
name: "organizational-folders.instructions"
description: "Guidance for top-level folders that only group submodules"
---

# Organizational COPILOT Guidelines

## Purpose & Scope
Keep parent folder COPILOT guides short (<100 lines) by focusing on navigation and workflows rather than repeating each subfolder's content.

## Rules
- Use the shared template (`.github/templates/organizational-copilot.template.md`).
- Provide a subfolder table (Name → key project → link to its COPILOT.md).
- Replace verbose sections (Architecture, Technology, Tests, etc.) with links to subfolder docs.
- Reference planner JSON for project/file listings instead of embedding them.
- After edits, run:
  1. `python .github/plan_copilot_updates.py --folders <Folder>`
  2. `python .github/copilot_apply_updates.py --folders <Folder>`
  3. `python .github/check_copilot_docs.py --only-changed --fail`

## Examples
```markdown
# Src/Common Overview

## Purpose
Organizes Controls/, Framework/, FwUtils/, ...

## Subfolder Map
| Subfolder | Key project | Notes |
| Controls | Controls/Controls.csproj | Controls/COPILOT.md |
```

````