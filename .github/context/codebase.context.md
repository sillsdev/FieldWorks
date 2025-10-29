# High-signal context for FieldWorks agents

Use these entry points to load context efficiently without scanning the entire repo.

- Onboarding: `.github/copilot-instructions.md`
- Src catalog (overview of major folders): `.github/src-catalog.md`
- Component guides: `Src/<Folder>/COPILOT.md` (and subfolder COPILOT.md where present)
- Build system: `Build/FieldWorks.targets`, `Build/FieldWorks.proj`, `agent-build-fw.sh`, `FW.sln`
- Installer: `FLExInstaller/`
- Test data: `TestLangProj/`
- Localization: `crowdin.json`, `DistFiles/CommonLocalizations/`

Tips
- Prefer top-level scripts or FW.sln over ad-hoc project builds
- Respect CI checks (commit messages, whitespace) before pushing
