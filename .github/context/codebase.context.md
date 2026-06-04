# High-signal context for FieldWorks agents

Use these entry points to load context efficiently without scanning the entire repo.

- Shared language and glossary: `CONTEXT.md`
- Onboarding (canonical anchor): `.github/AGENTS.md`
- Src catalog (overview of major folders): `.github/src-catalog.md`
- AI review workflow: `grill-with-docs` for planning language, `.github/instructions/review-analyzer.instructions.md` for review policy, `pr-preflight` for branch/PR readiness, and `respond-to-review-comments` for reviewer feedback.
- Specialist review agents: `.github/agents/fieldworks.csharp-expert.agent.md`, `.github/agents/fieldworks.winforms-expert.agent.md`, `.github/agents/fieldworks.cpp-expert.agent.md`, and `.github/agents/fieldworks.avalonia-expert.agent.md`.
- Component guides: `Src/<Folder>/AGENTS.md` (and subfolder AGENTS.md where present)
- Build system: `build.ps1`, `FieldWorks.proj`, `FieldWorks.sln`, `Build/InstallerBuild.proj`
- Installer: `FLExInstaller/`
- Testing: `test.ps1`
- Test data: `TestLangProj/`
- Localization: `crowdin.json`, `DistFiles/CommonLocalizations/`
- Documentation discipline: `Docs/agent-docs-refresh.md` (detect → plan workflow, agent doc skeleton)

Tips
- Prefer top-level scripts or FieldWorks.sln over ad-hoc project builds
- Respect CI checks (commit messages, whitespace) before pushing
- Prefer FieldWorks-specific agents over generic language/framework agents when reviewing this repo
