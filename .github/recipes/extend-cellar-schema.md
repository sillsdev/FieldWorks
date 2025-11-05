# Recipe: Extend Cellar/LCM schema

## When to use
Add new fields or types to the core data model.

## Steps
1) Read `Src/Cellar/COPILOT.md` and related data model docs
2) Define schema changes and review for compatibility
3) Update codegen or model bindings if applicable
4) Migrate test data in `TestLangProj/` as needed
5) Add tests for load/save and migrations
6) Update COPILOT.md and src-catalog if scope changed

## Checks
- [ ] Backward-compatible changes or clear migration path
- [ ] Tests cover upgrade/downgrade paths
- [ ] Performance impact considered
