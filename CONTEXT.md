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
- **`Writing system`**: overloaded — qualify when the distinction matters.
  - **Writing system definition**: `WritingSystemDefinition` (libpalaso / `SIL.WritingSystems`); the base writing system class, identified by a BCP-47 language tag.
  - **Core writing system definition**: `CoreWritingSystemDefinition` (liblcm / `SIL.LCModel.Core.WritingSystems`); extends `WritingSystemDefinition` with a `Handle` and LCM-specific features such as character sets.
  - **Writing system handle**: Integer `Handle` on `CoreWritingSystemDefinition`, assigned by `WritingSystemManager` within a language project. Used for fast text-run lookups; not meaningful outside LCModel.
  - **Writing system tag**: BCP-47 string (`LanguageTag` / `Id`) identifying a writing system in persistence and cross-library code.
  - **Vernacular / analysis writing system**: The role a writing system plays inside a language project (see Core Domain section).
## Core Domain and Product Language
- **Language project**: A user-managed linguistic dataset opened and edited in FieldWorks.
- **Writing system**: A configured language/script/orthography used to store and display text.
- **Vernacular writing system**: A writing system used for source-language data.
- **Analysis writing system**: A writing system used for glosses, definitions, translated labels, and analysis-oriented text.
- **`ITsString` / TsString**: The LCModel-managed text model used for lexicon and Views text, including run-level writing-system and style properties. Cross-framework clipboard and drag/drop interchange serializes it through `TsStringWrapper` XML.
- **Multi-writing-system text**: A text value or field that exposes one or more writing-system alternatives and may also preserve run-level writing-system and style metadata inside a single `ITsString`.
- **IME composition**: The transient input-method editing state before text is committed. Treat composition behavior and committed text behavior as separate test and parity concerns.
- **Lexicon**: The lexical data and editing experience in FLEx.
- **Interlinear text**: Text annotated with multiple aligned linguistic analysis lines.
- **Morphology**: The part of the system and data model concerned with morphemes, rules, and word analysis.
- **Parser**: Morphological analysis tooling such as HermitCrab or XAmple.
- **Paratext integration**: Scripture and lexicon interoperability with Paratext. Implemented across `FwParatextLexiconPlugin`, `ParatextImport` (scripture text import via `ParatextImportManager`/`ParatextSfmImporter`), and `Paratext8Plugin` (bridge to Paratext APIs).
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
- **LcmCache**: Root entry point to LCModel (`SIL.LCModel`); called "the cache" in code and comments. Not a simple data cache — the name is historical. Exposes the language project, writing system factory, action handler, and `ServiceLocator`.
- **Service locator**: `LcmCache.ServiceLocator` (`ILcmServiceLocator`). IoC container for LCModel — the primary way to retrieve repositories, factories, and services.
- **Unit of work**: Groups data-model changes under `IActionHandler`. All LCModel writes must occur inside one. `UndoableUnitOfWorkHelper` (undoable) and `NonUndoableUnitOfWorkHelper` (non-undoable) are in `SIL.LCModel.Infrastructure`; use as a `using` block or via their static `.Do(...)` helpers.
- **Publish/subscribe system**: Messaging system (`IPublisher` / `ISubscriber`, `SIL.FieldWorks.Common.FwUtils`) via `FwUtils.Publisher` and `FwUtils.Subscriber` singletons. Supports exact-name and prefix subscriptions; `PublishAtEndOfAction` defers delivery to end of user action. Event-based problems deserve event-based solutions — avoid state variables for event timing when a deterministic subscribe/unsubscribe solution can be used.
- **Area**: One of the five top-level navigation divisions — Lexicon, Grammar, Words & Texts, Notebook, Lists. Declared in `areaConfiguration.xml` and identified by constants in `AreaConstants`. Each Area has its own sidebar and owns a set of Tools.
- **Tool**: A view or function within an Area, declared in `toolConfiguration.xml`. The active Tool per Area is tracked via `ToolForAreaNamed_<area>`; switching Areas restores the last-used Tool. Navigation is property-driven through the XCore mediator system.
- **Dictionary configuration**: A `.fwdictconfig` XML file (`DictionaryConfigurationModel`) defining which LCModel fields appear in a dictionary view, their order, style, and options. Scoped to one or more Publications.
- **Publication**: A named output target (e.g. a print edition or web view) that dictionary configurations are scoped to. `AllPublications` applies a configuration to all current and future publications.
- **FlexText**: An XML interchange format (`.flextext`) for interlinear texts, representing analyzed words with morpheme, gloss, and translation annotations. Used for import and export of interlinear data; handled in `LexText/Interlinear`.
- **Webonary**: An online dictionary publishing service that FieldWorks integrates with as an export target. Implemented in xWorks (`UploadToWebonaryController`, `WebonaryClient`).
- **Utility**: A user-invoked data maintenance or migration tool (e.g. resetting homographs, removing parser annotations, fixing duplicate analyses). Utilities implement `IUtility` (`FwCoreDlgs`), are registered in `UtilityCatalogInclude.xml` via reflection, and run through `UtilityDlg` (Tools > Utilities menu).
- **DistFiles**: Runtime assets copied into outputs or installers.
- **Region**: In the Avalonia migration, the framework-neutral model of an editing surface: a flattened list of typed fields (`LexicalEditRegionModel`) composed from the view definition and rendered by the Avalonia region views. A region is data; the surface is its rendered form.
- **Surface**: The rendered UI a tool shows for a record — the legacy WinForms `DataTree`/`BrowseViewer` or the Avalonia region host. Selected per tool from the `UIMode` setting via `LexicalEditSurfaceResolver`.
- **Seam**: A framework-neutral interface that lets the Avalonia layer consume product behavior without referencing LCModel or WinForms — either in `FwAvalonia/Seams` (e.g. `IFwClipboard`) or carved into a legacy control (e.g. `IBrowseColumnSource`). Product implementations live at the xWorks edge.
- **Fenced edit session**: One `IEditSession` wrapping one LCModel undo task, opened lazily on the first staged edit and ended exactly once by Commit or Cancel. Prevents orphaned undo tasks; implemented by `LcmRegionEditSession`.
- **Settle**: The single auto-save policy for an open edit session — commit if valid, otherwise roll back and notify. Invoked on navigation, window deactivate, undo, and teardown (`RegionEditContextHolder.Settle`).
- **Parity**: Behavioral/visual equivalence between a legacy WinForms surface or dialog and its Avalonia replacement. Deviations must be recorded as `// PARITY` deferrals in code or approved divergences, never left implicit.
- **Preview vs POC**: In the Avalonia migration, **preview** means a lightweight sample or design-time path that reuses the shared region renderer; **POC** refers only to the retired spike/evidence vocabulary and should not name live runtime code paths.
- **StringTable lane**: The legacy data-driven localization path for XML/layout-owned labels and attributes, resolved through `StringTable.Table.LocalizeAttributeValue(...)` / `XmlUtils.GetLocalizedAttributeValue(...)` against the `strings-<locale>.xml` files.
- **`.resx` lane**: Project-owned compiled resources resolved through `ResourceManager` and shipped as satellite assemblies. This is the current lane for Avalonia product-message or chrome strings in `FwAvalonia` and `FwAvaloniaDialogs`.
- **LocalizationManager / L10NSharp lane**: The XLIFF-backed UI-localization path initialized by the product host (`FieldWorks.InitializeLocalizationManager`). It supports runtime UI-language switching and translator collection mode, but any host or test process that touches it must initialize at least one `LocalizationManager` first.
- **Avalonia chrome strings**: User-visible Avalonia surface text that is not a data-driven field label — buttons, tab headers, tooltips, dialog titles, validation messages, accessible names, and similar shell/UI text.
## Repo-Wide Invariants
- Native C++ builds before managed code generation and managed projects.
- `build.ps1` is the canonical build entry point.
- `test.ps1` is the canonical test entry point.
- Registration-free COM is a core deployment/runtime assumption; do not introduce global COM registration behavior.
- User-visible UI strings belong in `.resx`, not hardcoded source.
- Installer work lives under `FLExInstaller/`.
- Integration tests often depend on deterministic sample data such as `TestLangProj/`.
- Worktree-aware scripts are preferred because concurrent work across git worktrees is supported.
- FLEx/Language Explorer is built with architectural boundaries, new dependencies between existing projects must be justified.
## Key Relationships
- FieldWorks the repository contains the FLEx application, supporting tools, shared libraries, installer authoring, and docs.
- Native build artifacts feed managed code generation through ViewsInterfaces.
- Send/Receive is a workflow; FLEx Bridge is the underlying integration/tooling layer behind that workflow.
- Writing systems are first-class project configuration, with vernacular and analysis writing systems playing different roles.
## Good Naming Pressure
- Prefer established repo names over generic synonyms.
- If a change concerns user data, say **language project**, not just **project**.
- If a change concerns the source tree, say **repo**, **worktree**, or the specific build/test project.
- If a change touches synchronization, distinguish the user concept (**Send/Receive**) from the implementation/tooling concept (**FLEx Bridge**) unless the distinction is intentionally irrelevant.
- If a change touches the managed/native boundary, call out **ViewsInterfaces**, **Views**, **FwKernel**, or **registration-free COM** explicitly.
- Test classes follow `<Subject>Tests` naming. Test methods use plain descriptive English with the structure `MethodUnderTest_ExpectedResult` or `MethodUnderTest_ExpectedResult_WhenCondition` — examples: `GetGuidForJumpToTool_UsesRootObject_WhenNoCurrentSlice`, `GetWheelScrollPixels_UsesSystemWheelSettings`, `TryGetWheelScrollPosition_ReturnsFalse_WhenAlreadyAtTop`. Only nested classes inside test fixtures for mock/helper types.
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
- **Resolve**: Marking a review thread addressed in GitHub. Do this only after the code or reply fully answers the thread.
## Remaining Open Question
- Should this root file stay mixed product-plus-architecture, or should lower-level developer terms move into narrower subtree contexts later?
## ADR Candidates
- No repository-wide ADR location is established yet.
- If a terminology decision becomes hard to reverse or affects naming across many files, record it here first and promote it to a formal ADR only if the repo adopts a dedicated ADR convention.
