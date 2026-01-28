# High-signal context for FieldWorks agents

Use these entry points to load context efficiently without scanning the entire repo.

- Onboarding: `.github/AGENTS.md`
- Src catalog (overview of major folders): `.github/src-catalog.md`
- Component guides: `Src/<Folder>/AGENTS.md` (and subfolder AGENTS.md where present)
- Build system: `Build/FieldWorks.targets`, `Build/FieldWorks.proj`, `FieldWorks.sln`
- Installer: `FLExInstaller/`
- Test data: `TestLangProj/`
- Localization: `crowdin.json`, `DistFiles/CommonLocalizations/`
- Documentation discipline: `Docs/agent-docs-refresh.md` (detect → plan workflow, agent doc skeleton)

Tips
- Prefer top-level scripts or FieldWorks.sln over ad-hoc project builds
- Respect CI checks (commit messages, whitespace) before pushing

