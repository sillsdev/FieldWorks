# FieldWorks Context

This file captures the thin layer of shared language that helps humans and agents talk about this repository without re-explaining the same concepts every session.

It is intentionally not a full architecture manual. It should stay biased toward terminology, relationships, invariants, and naming choices that affect planning, navigation, implementation, and reviews.

## Scope

- This is the root context for the whole repository.
- It covers repo-wide language shared across product, code, build, test, and installer work.
- If a subtree develops materially different language, add a more local `CONTEXT.md` there instead of bloating this file.

## Canonical Product Terms

- **FieldWorks**: The preferred default name for the product suite, the repository, and root-level planning language.
- **FLEx**: Legacy shorthand for **FieldWorks Language Explorer**. Use it when matching existing code, folders, UI text, integrations, or historical documentation, not as the default root-level product term.
- **Language Explorer**: The spelled-out legacy product name behind FLEx that still appears in repo paths, UI assets, and integrations.
- **FieldWorks.exe**: The current host executable for the main application shell.

## Disambiguation Rules

- **Treat `project` as requiring a qualifier almost always.** The word is overloaded in this repo.
- **Language project**: A user/data project inside FieldWorks or FLEx.
- **Git repository** or **repo**: The source tree you are editing now.
- **MSBuild project**: A `.csproj`, `.vcxproj`, `.wixproj`, or similar build unit.
- **Installer project**: WiX authoring and packaging work under `FLExInstaller/`.
- **Worktree**: A git worktree for isolated builds and edits.

## Core Domain and Product Language

- **Language project**: A user-managed linguistic dataset opened and edited in FieldWorks.
- **Writing system**: A configured language/script/orthography used to store and display text.
- **Vernacular writing system**: A writing system used for source-language data.
- **Analysis writing system**: A writing system used for glosses, definitions, translated labels, and analysis-oriented text.
- **Lexicon**: The lexical data and editing experience in FLEx.
- **Interlinear text**: Text annotated with multiple aligned linguistic analysis lines.
- **Morphology**: The part of the system and data model concerned with morphemes, rules, and word analysis.
- **Parser**: Morphological analysis tooling such as HermitCrab or XAmple.
- **Paratext integration**: Scripture and lexicon interoperability with Paratext.
- **Send/Receive**: The user-facing synchronization workflow for sharing project data.
- **FLEx Bridge**: The tool/integration layer used by Send/Receive and related LIFT-based exchange workflows.

## Architecture and Codebase Language

- **LCModel**: The underlying managed data model, caches, services, and related packages used for FieldWorks data access.
- **xWorks**: The shared application framework and shell infrastructure that hosts major work areas.
- **LexText**: The FLEx application layer and related lexicon/text-analysis functionality.
- **Common**: Shared infrastructure, controls, dialogs, utilities, and framework code used across the suite.
- **FwKernel** and **Views**: Native rendering and view infrastructure.
- **ViewsInterfaces**: The managed interface layer generated from native IDL and used across the managed/native boundary.
- **Traversal build**: The ordered build driven by `FieldWorks.proj` and invoked via `build.ps1`.
- **DistFiles**: Runtime assets copied into outputs or installers.

## Repo-Wide Invariants

- Native C++ builds before managed code generation and managed projects.
- `build.ps1` is the canonical build entry point.
- `test.ps1` is the canonical test entry point.
- Registration-free COM is a core deployment/runtime assumption; do not introduce global COM registration behavior.
- User-visible UI strings belong in `.resx`, not hardcoded source.
- Installer work lives under `FLExInstaller/`.
- Integration tests often depend on deterministic sample data such as `TestLangProj/`.
- Worktree-aware scripts are preferred because concurrent work across git worktrees is supported.

## Key Relationships

- FieldWorks the repository contains the FLEx application, supporting tools, shared libraries, installer authoring, and docs.
- FLEx/Language Explorer is built on shared infrastructure such as xWorks, Common, LCModel, and the native Views/FwKernel stack.
- Native build artifacts feed managed code generation through ViewsInterfaces.
- Send/Receive is a workflow; FLEx Bridge is the underlying integration/tooling layer behind that workflow.
- Writing systems are first-class project configuration, with vernacular and analysis writing systems playing different roles.

## Good Naming Pressure

- Prefer established repo names over generic synonyms.
- If a change concerns user data, say **language project**, not just **project**.
- If a change concerns the source tree, say **repo**, **worktree**, or the specific build/test project.
- If a change touches synchronization, distinguish the user concept (**Send/Receive**) from the implementation/tooling concept (**FLEx Bridge**) unless the distinction is intentionally irrelevant.
- If a change touches the managed/native boundary, call out **ViewsInterfaces**, **Views**, **FwKernel**, or **registration-free COM** explicitly.

## Review Workflow Language

- **PR preflight**: An interactive branch-readiness workflow before posting or updating a PR. It applies FieldWorks review policy, may use specialist review agents, interviews the author about risks and validation, and writes `.review/summary.md`.
- **Review analyzer**: The FieldWorks review policy in `.github/instructions/review-analyzer.instructions.md`. It defines what to check; it is not the interactive workflow.
- **Specialist review agent**: A focused read-only reviewer such as the FieldWorks C#, WinForms, C++, or Avalonia agent. Specialist output is evidence for the final synthesis, not a replacement for verifying findings against code.
- **Review comment**: Any actionable feedback from Copilot or a human reviewer on a pull request.
- **Review thread**: A GitHub inline conversation anchored to code. Resolve only after the thread is fully addressed and no question remains.
- **Copilot reviewer comment**: Automated review feedback from GitHub Copilot. Evaluate it like external reviewer feedback; do not treat it as authoritative without checking the code.
- **Human reviewer comment**: Feedback from a person. Treat it seriously, but still verify the requested change against FieldWorks conventions and existing behavior.
- **Sensible fix**: A reviewer request that is technically sound, unambiguous, scoped, and compatible with repo rules.
- **Ambiguous feedback**: A reviewer request that lacks enough context, conflicts with another requirement, or would require a product or architecture decision. Ask the user before changing code.
- **Reply**: A response in the review conversation explaining a fix, asking a question, or giving technical reasoning for no code change.
- **Resolve**: Marking a review thread addressed in GitHub. Do this only when the code or reply fully answers the thread.

## Remaining Open Question

- Should this root file stay mixed product-plus-architecture, or should lower-level developer terms move into narrower subtree contexts later?

## ADR Candidates

- No repository-wide ADR location is established yet.
- If a terminology decision becomes hard to reverse or affects naming across many files, record it here first and promote it to a formal ADR only if the repo adopts a dedicated ADR convention.
