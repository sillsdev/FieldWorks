# Updating COPILOT.md summaries

Each folder under `Src/` has a `COPILOT.md` file that documents its purpose, components, and relationships. These files are essential for understanding the codebase and for keeping AI guidance accurate and actionable.

## Frontmatter (required)
Add minimal frontmatter to each `COPILOT.md` so ownership and review hygiene are clear:

```yaml
---
owner: <GitHub-team-or-username>   # e.g., @sillsdev/fieldworks-core
last-reviewed: YYYY-MM-DD          # update when materially reviewed/verified
status: draft|verified             # draft until an SME validates accuracy
---
```

If ownership is not known at the time of edit, use a placeholder and mark it clearly:

```yaml
---
owner: FIXME(set-owner)
last-reviewed: 2025-10-29
status: draft
---
```

## When to update COPILOT.md files
- When making significant architectural changes to a folder
- When adding new major components or subprojects
- When changing the purpose or scope of a folder
- When discovering discrepancies between documentation and reality

## How to update COPILOT.md files
1. Read the existing `COPILOT.md` file for the folder you're working in.
2. If you notice discrepancies (e.g., missing components, outdated descriptions, incorrect dependencies):
   - Update the `COPILOT.md` file to reflect the current state.
   - Update cross-references in related folders' `COPILOT.md` files if relationships changed.
   - Update `.github/src-catalog.md` with the new concise description.
3. Keep documentation concise but informative:
   - Purpose: What the folder is for (1–2 sentences)
   - Key Components: Major files, subprojects, or features
   - Technology Stack: Primary languages and frameworks
   - Dependencies: What it depends on and what uses it
   - Build Information: Always prefer top-level solution or `agent-build-fw.sh`; avoid per-project builds unless clearly supported. If per-project SDK-style build/test is valid, document exact commands.
   - Entry Points: How the code is used or invoked
   - Related Folders: Cross-references to other `Src/` folders

4. Add a short "Review Notes (FIXME)" section for anything needing SME validation. Use clear markers like `FIXME(accuracy): ...` or `FIXME(build): ...` so reviewers can find and resolve them quickly.

## Example scenarios requiring COPILOT.md updates
- Adding a new C# project to a folder → update "Key Components" and "Build Information"
- Discovering a folder depends on another folder not listed → update "Dependencies" and "Related Folders"
- Finding that a folder's description is inaccurate → update "Purpose" section
- Adding new test projects → update "Build Information" and "Testing" sections

Always validate that your code changes align with the documented architecture. If they don't, either adjust your changes or update the documentation to reflect the new architecture.
