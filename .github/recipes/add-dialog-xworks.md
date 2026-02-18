# Recipe: Add a new dialog in xWorks

## When to use
Add a user-facing dialog to the primary FieldWorks app.

## Steps
1) Read `Src/xWorks/AGENTS.md` and related UI component guides
2) Define strings in .resx; avoid hardcoded text
3) Implement dialog following existing patterns (base classes, event flow)
4) Wire up command/shortcut and integrate into menus/toolbars
5) Add tests for dialog logic (if applicable)
6) Update AGENTS.md if adding a reusable component

## Checks
- [ ] Localization via .resx
- [ ] Threading correctness
- [ ] Tests added/updated

