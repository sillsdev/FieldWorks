# Comprehensive Commit-by-Commit Analysis

**Total Commits**: 93
**Base**: 8e508dab484fafafb641298ed9071f03070f7c8b
**Head**: 3fb3b608c

================================================================================


## Commit 1: bf82f8dd6
**"Migrate all the .csproj files to SDK format"**

- **Author**: Jason Naylor
- **Date**: 2025-09-26
- **Stats**:  10 files changed, 749 insertions(+), 161 deletions(-)

**Commit Message**:
```
- Created convertToSDK script in Build folder
- Updated mkall.targets RestoreNuGet to use dotnet restore
- Update mkall.targets to use dotnet restore instead of old NuGet restore
- Update build scripts to use RestorePackages target
```


**C# Source Files**: 1
  - Build/Src/FwBuildTasks/CollectTargets.cs

**Python Scripts**: 1
  - Build/convertToSDK.py

**Build Files**: 2
  - Build/NuGet.targets
  - Build/mkall.targets

**Scripts**: 1
  - Build/build.bat

**Category**: 🎨 Formatting only

---


## Commit 2: f1995dac9
**"Implement and execute improved convertToSDK.py"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-09-29
- **Stats**:  116 files changed, 4577 insertions(+), 25726 deletions(-)

**Commit Message**:
```
* Use mkall.targets-based NuGet detection
* Fix test package references causing build failures
* Add PrivateAssets to test packages to exclude transitive deps

  SDK-style PackageReferences automatically include transitive
  dependencies. The SIL.LCModel.*.Tests packages depend on
  TestHelper, which causes NU1102 errors. Adding PrivateAssets="All"
  prevents transitive dependencies from flowing to consuming
  projects

Co-authored-by: jasonleenaylor <2295227+jasonleenaylor@users.noreply.github.com>
```

**Project Files Changed**: 115
  - (Too many to list - 115 files)

**Category**: 📦 Mass SDK Conversion

---


## Commit 3: 21eb57718
**"Update package versions to fix conflicts and use wildcards"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-09-30
- **Stats**:  89 files changed, 321 insertions(+), 321 deletions(-)

**Commit Message**:
```
- Remove icu.net 3.0.0-beta.297 references to avoid version
  downgrade conflicts (SIL.LCModel.Core uses 3.0.0-*)
- Update all SIL.LCModel.* packages from 11.0.0-beta0136 to
  11.* wildcard to automatically use latest version 11 releases
- Resolves NU1605 version downgrade warnings
- Enables automatic TestHelper fix in new LCM packages
- Fix LCM package wildcards to match beta versions

Co-authored-by: jasonleenaylor <2295227+jasonleenaylor@users.noreply.github.com>
```

**Project Files Changed**: 89
  - (Too many to list - 89 files)

**Category**: 📦 Mass SDK Conversion

---


## Commit 4: bfd1b3846
**"Convert DesktopAnalytics and IPCFramework to PackageReferences"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-09-30
- **Stats**:  8 files changed, 10 insertions(+), 11 deletions(-)

**Commit Message**:
```
Converted regular References to PackageReferences for NuGet packages:
- SIL.DesktopAnalytics (version 4.0.0) in 6 projects
- SIL.FLExBridge.IPCFramework (version 1.1.1-beta0001) in FwUtils
- Updated package versions to resolve NU1605 downgrade errors:
- Moq: 4.17.2 → 4.20.70 in XMLViewsTests.csproj
- TagLibSharp: 2.2.0 → 2.3.0 in xWorks.csproj

Co-authored-by: jasonleenaylor <2295227+jasonleenaylor@users.noreply.github.com>
```

**Project Files Changed**: 8
  - Src/Common/Controls/FwControls/FwControls.csproj
  - Src/Common/Controls/XMLViews/XMLViewsTests/XMLViewsTests.csproj
  - Src/Common/FieldWorks/FieldWorks.csproj
  - Src/Common/FwUtils/FwUtils.csproj
  - Src/LexText/Interlinear/ITextDll.csproj
  - Src/LexText/LexTextControls/LexTextControls.csproj
  - Src/LexText/LexTextDll/LexTextDll.csproj
  - Src/xWorks/xWorks.csproj

**Category**: 📦 Package Updates

---


## Commit 5: eb4dc7a45
**"Fix bare References and update convertToSDK.py script"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-09-30
- **Stats**:  12 files changed, 107 insertions(+), 34 deletions(-)

**Commit Message**:
```
* Fixed bare Reference elements in FieldWorks.csproj and
  XMLViews.csproj that should have been PackageReferences:
- Geckofx60.32/64 packages (provide Geckofx-Core, Geckofx-Winforms)
- SharpZipLib (provides ICSharpCode.SharpZipLib)
- SIL.ParatextShared (provides ParatextShared)
- FwControls.csproj: ParatextShared → SIL.ParatextShared
- ITextDll.csproj: Geckofx, SharpZipLib, ParatextShared → packages
- FwParatextLexiconPlugin.csproj: Paratext.LexicalContracts → ParatextData
- ScriptureUtilsTests.csproj: ParatextShared → SIL.ParatextShared
- Paratext8Plugin.csproj: Paratext.LexicalContracts → removed (provided by ParatextData)
- FwParatextLexiconPluginTests.csproj: Paratext.LexicalContracts* → ParatextData
- ParatextImportTests.csproj: ParatextShared → SIL.ParatextShared

Co-authored-by: jasonleenaylor <2295227+jasonleenaylor@users.noreply.github.com>
```

**Project Files Changed**: 10
  - Src/Common/Controls/FwControls/FwControls.csproj
  - Src/Common/Controls/XMLViews/XMLViews.csproj
  - Src/Common/FieldWorks/FieldWorks.csproj
  - Src/Common/ScriptureUtils/ScriptureUtils.csproj
  - Src/Common/ScriptureUtils/ScriptureUtilsTests/ScriptureUtilsTests.csproj
  - Src/FwParatextLexiconPlugin/FwParatextLexiconPlugin.csproj
  - Src/FwParatextLexiconPlugin/FwParatextLexiconPluginTests/FwParatextLexiconPluginTests.csproj
  - Src/LexText/Interlinear/ITextDll.csproj
  - Src/Paratext8Plugin/Paratext8Plugin.csproj
  - Src/ParatextImport/ParatextImportTests/ParatextImportTests.csproj

**Python Scripts**: 1
  - Build/convertToSDK.py

**Build Files**: 1
  - Directory.Build.props

**Category**: 🤖 Automation Script

---


## Commit 6: 186e452cb
**"Fix Geckofx version and DotNetZip warnings"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-09-30
- **Stats**:  7 files changed, 11 insertions(+), 11 deletions(-)

**Commit Message**:
```
Updated Geckofx60.32/64 from 60.0.50/51 to 60.0.52 (only
version available on NuGet). This resolves NU1603 warnings
about missing package version 60.0.51.

Updated SharpZipLib in ITextDll.csproj from 1.3.3 to 1.4.0
to avoid downgrade warning (SIL.LCModel requires >= 1.4.0).

Suppressed DotNetZip NU1903 security warning in xWorks.csproj
and xWorksTests.csproj (already suppressed globally in
Directory.Build.props, but some projects need local suppression).

All 115 projects now restore successfully without errors.

Co-authored-by: jasonleenaylor <2295227+jasonleenaylor@users.noreply.github.com>
```

**Project Files Changed**: 7
  - Src/Common/Controls/XMLViews/XMLViews.csproj
  - Src/Common/FieldWorks/FieldWorks.csproj
  - Src/LexText/Interlinear/ITextDll.csproj
  - Src/LexText/LexTextControls/LexTextControls.csproj
  - Src/LexText/LexTextDll/LexTextDll.csproj
  - Src/xWorks/xWorks.csproj
  - Src/xWorks/xWorksTests/xWorksTests.csproj

**Category**: 🔄 General Changes

---


## Commit 7: 053900d3b
**"Fix post .csproj conversion build issues"**

- **Author**: Jason Naylor
- **Date**: 2025-10-02
- **Stats**:  54 files changed, 1836 insertions(+), 1565 deletions(-)

**Commit Message**:
```
* Add excludes for test subdirectories
* Fix several references that should have been PackageReferences
* Fix Resource ambiguity
* Add c++ projects to the solution
```

**Project Files Changed**: 44
  - (Too many to list - 44 files)

**C# Source Files**: 3
  - Src/Common/SimpleRootSite/EditingHelper.cs
  - Src/Common/SimpleRootSite/IbusRootSiteEventHandler.cs
  - Src/Common/SimpleRootSite/Properties/Resources.Designer.cs

**Build Files**: 5
  - Directory.Build.props
  - Lib/Directory.Build.targets
  - Src/Common/ViewsInterfaces/BuildInclude.targets
  - Src/Directory.Build.props
  - Src/Directory.Build.targets

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 8: c4a995f48
**"Delete some obsolete files and clean-up converted .csproj"**

- **Author**: Jason Naylor
- **Date**: 2025-10-03
- **Stats**:  121 files changed, 855 insertions(+), 9442 deletions(-)

**Commit Message**:
```
* Fix more encoding converter and geckofx refs
* Delete obsolete projects
* Delete obsoleted test fixture
```

**Project Files Changed**: 27
  - (Too many to list - 27 files)

**C# Source Files**: 65

**Scripts**: 2
  - Bin/nmock/src/build.bat
  - Bin/nmock/src/ccnet/ccnet-nmock.bat

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 9: 3d8ddad97
**"Copilot assisted NUnit3 to NUnit4 migration"**

- **Author**: Jason Naylor
- **Date**: 2025-10-06
- **Stats**:  110 files changed, 7627 insertions(+), 8590 deletions(-)

**Commit Message**:
```
* Also removed some obsolete tests and clean up some incomplete
  reference conversions
```

**Project Files Changed**: 25
  - (Too many to list - 25 files)

**C# Source Files**: 84

**Category**: 🧪 Test Framework Migration (NUnit 3→4)

---


## Commit 10: 8476c6e42
**"Update palaso dependencies and remove GeckoFx 32bit"**

- **Author**: Jason Naylor
- **Date**: 2025-10-08
- **Stats**:  86 files changed, 307 insertions(+), 267 deletions(-)

**Commit Message**:
```
* The conditional 32/64 bit dependency was causing issues
  and wasn't necessary since we aren't shipping 32 bit anymore
```

**Project Files Changed**: 86
  - (Too many to list - 86 files)

**Category**: 📦 Mass SDK Conversion

---


## Commit 11: 0f963d400
**"Fix broken test projects by adding needed external dependencies"**

- **Author**: Jason Naylor
- **Date**: 2025-10-09
- **Stats**:  57 files changed, 387 insertions(+), 102 deletions(-)

**Commit Message**:
```
* Mark as test projects and include test adapter
* Add .config file and DependencyModel package if needed
* Add AssemblyInfoForTests.cs link if needed
* Also fix issues caused by a stricter compiler in net48
```

**Project Files Changed**: 49
  - (Too many to list - 49 files)

**C# Source Files**: 6
  - Src/Common/Controls/XMLViews/XMLViewsTests/XmlBrowseViewBaseVcTests.cs
  - Src/LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.cs
  - Src/Utilities/FixFwDataDll/ErrorFixer.cs
  - Src/Utilities/FixFwDataDll/FixFwDataStrings.Designer.cs
  - Src/Utilities/FixFwDataDll/FwData.cs
  - Src/Utilities/FixFwDataDll/WriteAllObjectsUtility.cs

**Category**: 📦 Package Updates

---


## Commit 12: 16c8b63e8
**"Update FieldWorks.cs to use latest dependencies"**

- **Author**: Jason Naylor
- **Date**: 2025-11-04
- **Stats**:  2 files changed, 15 insertions(+), 6 deletions(-)

**Commit Message**:
```
* Update L10nSharp calls
* Specify the LCModel BackupProjectSettings
* Add CommonAsssemblyInfo.cs link lost in conversion
* Set Deterministic builds to false for now (evaluate later)
```

**Project Files Changed**: 1
  - Src/Common/FieldWorks/FieldWorks.csproj

**C# Source Files**: 1
  - Src/Common/FieldWorks/FieldWorks.cs

**Category**: 📦 Package Updates

---


## Commit 13: c09c0c947
**"Spec kit and AI docs, tasks and instructions"**

- **Author**: John Lambert
- **Date**: 2025-11-04
- **Stats**:  131 files changed, 17780 insertions(+), 108 deletions(-)

**Commit Message**:
```
Refine AI onboarding and workflows:
* Update copilot-instructions.md with agentic workflow links and
clearer pointers to src-catalog and per-folder guidance (COPILOT.md).
* Tune native and installer instructions for mixed C++/CLI, WiX, and build
nuances (interop, versioning, upgrade behavior, build gotchas).

Spec kit improvements:
* Refresh spec.md and plan.md to align with the
feature-spec and bugfix agent workflows and FieldWorks conventions.
Inner-loop productivity:
* Extend tasks.json with quick checks for whitespace and commit
message linting to mirror CI and shorten feedback loops.

CI hardening for docs and future agent flows:
* Add lint-docs.yml to verify COPILOT.md presence per
Src/<Folder> and ensure folders are referenced in .github/src-catalog.md.
* Add agent-analysis-stub.yml (disabled-by-default) to
document how we will run prompts/test-failure analysis in CI later.

Locally run CI checks in Powershell
* Refactor scripts and add whitespace fixing algorithm
* Add system to keep track of changes needed to be reflected in
  COPILOT.md files.

git prune task
```


**Documentation Files**: 102
  - .github/chatmodes/installer-engineer.chatmode.md
  - .github/chatmodes/managed-engineer.chatmode.md
  - .github/chatmodes/native-engineer.chatmode.md
  - .github/chatmodes/technical-writer.chatmode.md
  - .github/commit-guidelines.md
  - .github/context/codebase.context.md
  - .github/copilot-framework-tasks.md
  - .github/copilot-instructions.md
  - .github/instructions/build.instructions.md
  - .github/instructions/installer.instructions.md
  - .github/instructions/managed.instructions.md
  - .github/instructions/native.instructions.md
  - .github/instructions/testing.instructions.md
  - .github/memory.md
  - .github/option3-plan.md
  - .github/prompts/bugfix.prompt.md
  - .github/prompts/revise-instructions.prompt.md
  - .github/prompts/feature-spec.prompt.md
  - .github/prompts/speckit.analyze.prompt.md
  - .github/prompts/speckit.checklist.prompt.md
  - .github/prompts/speckit.clarify.prompt.md
  - .github/prompts/speckit.constitution.prompt.md
  - .github/prompts/speckit.implement.prompt.md
  - .github/prompts/speckit.plan.prompt.md
  - .github/prompts/speckit.specify.prompt.md
  - .github/prompts/speckit.tasks.prompt.md
  - .github/prompts/test-failure-debug.prompt.md
  - .github/pull_request_template.md
  - .github/recipes/add-dialog-xworks.md
  - .github/recipes/extend-cellar-schema.md
  - .github/spec-templates/plan.md
  - .github/spec-templates/spec.md
  - .github/src-catalog.md
  - .github/update-copilot-summaries.md
  - .specify/memory/constitution.md
  - .specify/templates/agent-file-template.md
  - .specify/templates/checklist-template.md
  - .specify/templates/plan-template.md
  - .specify/templates/spec-template.md
  - .specify/templates/tasks-template.md
  - Src/AppCore/COPILOT.md
  - Src/CacheLight/COPILOT.md
  - Src/Cellar/COPILOT.md
  - Src/Common/COPILOT.md
  - Src/Common/Controls/COPILOT.md
  - Src/Common/FieldWorks/COPILOT.md
  - Src/Common/Filters/COPILOT.md
  - Src/Common/Framework/COPILOT.md
  - Src/Common/FwUtils/COPILOT.md
  - Src/Common/RootSite/COPILOT.md
  - Src/Common/ScriptureUtils/COPILOT.md
  - Src/Common/SimpleRootSite/COPILOT.md
  - Src/Common/UIAdapterInterfaces/COPILOT.md
  - Src/Common/ViewsInterfaces/COPILOT.md
  - Src/DbExtend/COPILOT.md
  - Src/DebugProcs/COPILOT.md
  - Src/DocConvert/COPILOT.md
  - Src/FXT/COPILOT.md
  - Src/FdoUi/COPILOT.md
  - Src/FwCoreDlgs/COPILOT.md
  - Src/FwParatextLexiconPlugin/COPILOT.md
  - Src/FwResources/COPILOT.md
  - Src/GenerateHCConfig/COPILOT.md
  - Src/Generic/COPILOT.md
  - Src/InstallValidator/COPILOT.md
  - Src/Kernel/COPILOT.md
  - Src/LCMBrowser/COPILOT.md
  - Src/LexText/COPILOT.md
  - Src/LexText/Discourse/COPILOT.md
  - Src/LexText/FlexPathwayPlugin/COPILOT.md
  - Src/LexText/Interlinear/COPILOT.md
  - Src/LexText/LexTextControls/COPILOT.md
  - Src/LexText/LexTextDll/COPILOT.md
  - Src/LexText/LexTextExe/COPILOT.md
  - Src/LexText/Lexicon/COPILOT.md
  - Src/LexText/Morphology/COPILOT.md
  - Src/LexText/ParserCore/COPILOT.md
  - Src/LexText/ParserUI/COPILOT.md
  - Src/ManagedLgIcuCollator/COPILOT.md
  - Src/ManagedVwDrawRootBuffered/COPILOT.md
  - Src/ManagedVwWindow/COPILOT.md
  - Src/MigrateSqlDbs/COPILOT.md
  - Src/Paratext8Plugin/COPILOT.md
  - Src/ParatextImport/COPILOT.md
  - Src/ProjectUnpacker/COPILOT.md
  - Src/Transforms/COPILOT.md
  - Src/UnicodeCharEditor/COPILOT.md
  - Src/Utilities/COPILOT.md
  - Src/Utilities/FixFwData/COPILOT.md
  - Src/Utilities/FixFwDataDll/COPILOT.md
  - Src/Utilities/MessageBoxExLib/COPILOT.md
  - Src/Utilities/Reporting/COPILOT.md
  - Src/Utilities/SfmStats/COPILOT.md
  - Src/Utilities/SfmToXml/COPILOT.md
  - Src/Utilities/XMLUtils/COPILOT.md
  - Src/XCore/COPILOT.md
  - Src/XCore/FlexUIAdapter/COPILOT.md
  - Src/XCore/SilSidePane/COPILOT.md
  - Src/XCore/xCoreInterfaces/COPILOT.md
  - Src/XCore/xCoreTests/COPILOT.md
  - Src/views/COPILOT.md
  - Src/xWorks/COPILOT.md

**Python Scripts**: 5
  - .github/check_copilot_docs.py
  - .github/copilot_tree_hash.py
  - .github/detect_copilot_needed.py
  - .github/fill_copilot_frontmatter.py
  - .github/scaffold_copilot_markdown.py

**Scripts**: 15
  - .specify/scripts/powershell/check-prerequisites.ps1
  - .specify/scripts/powershell/common.ps1
  - .specify/scripts/powershell/create-new-feature.ps1
  - .specify/scripts/powershell/setup-plan.ps1
  - .specify/scripts/powershell/update-agent-context.ps1
  - Build/Agent/GitHelpers.ps1
  - Build/Agent/check-and-fix-whitespace.ps1
  - Build/Agent/check-and-fix-whitespace.sh
  - Build/Agent/check-whitespace.ps1
  - Build/Agent/check-whitespace.sh
  - Build/Agent/commit-messages.ps1
  - Build/Agent/commit-messages.sh
  - Build/Agent/fix-whitespace.ps1
  - Build/Agent/fix-whitespace.sh
  - Build/Agent/lib_git.sh

**Category**: 📚 Documentation Only

---


## Commit 14: ba9d11d64
**"Ai updates"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  18 files changed, 1709 insertions(+), 484 deletions(-)

**Project Files Changed**: 8
  - Lib/src/ObjectBrowser/ObjectBrowser.csproj
  - Lib/src/ScrChecks/ScrChecksTests/ScrChecksTests.csproj
  - Src/Common/Controls/DetailControls/DetailControls.csproj
  - Src/Common/Controls/FwControls/FwControlsTests/FwControlsTests.csproj
  - Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj
  - Src/LexText/Interlinear/ITextDll.csproj
  - Src/LexText/Morphology/MorphologyEditorDll.csproj
  - Src/LexText/ParserUI/ParserUI.csproj

**C# Source Files**: 3
  - Src/GenerateHCConfig/NullThreadedProgress.cs
  - Src/LexText/Morphology/MGA/AssemblyInfo.cs
  - Src/xWorks/xWorksTests/InterestingTextsTests.cs

**Documentation Files**: 4
  - .github/MIGRATION_ANALYSIS.md
  - .github/copilot-instructions.md
  - .github/update-copilot-summaries.md
  - MIGRATION_FIXES_SUMMARY.md

**Build Files**: 1
  - Build/mkall.targets

**Scripts**: 2
  - clean-rebuild.sh
  - rebuild-after-migration.sh

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 15: 5e63fdab5
**"more updates"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  6 files changed, 318 insertions(+), 128 deletions(-)

**Project Files Changed**: 3
  - Lib/src/ObjectBrowser/ObjectBrowser.csproj
  - Src/LexText/Morphology/MorphologyEditorDll.csproj
  - Src/LexText/ParserUI/ParserUI.csproj

**C# Source Files**: 1
  - Src/xWorks/xWorksTests/InterestingTextsTests.cs

**Documentation Files**: 2
  - .github/MIGRATION_ANALYSIS.md
  - MIGRATION_FIXES_SUMMARY.md

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 16: 811d8081a
**"closer to building"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  61 files changed, 1774 insertions(+), 1003 deletions(-)

**Project Files Changed**: 2
  - Lib/src/ObjectBrowser/ObjectBrowser.csproj
  - Src/LexText/Morphology/MorphologyEditorDll.csproj

**C# Source Files**: 5
  - Lib/src/ObjectBrowser/ClassPropertySelector.cs
  - Lib/src/ScrChecks/ScrChecksTests/ChapterVerseTests.cs
  - Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckUnitTest.cs
  - Src/GenerateHCConfig/NullThreadedProgress.cs
  - Src/xWorks/xWorksTests/InterestingTextsTests.cs

**Documentation Files**: 51
  - .github/context/codebase.context.md
  - .github/copilot-instructions.md
  - .github/instructions/build.instructions.md
  - .github/instructions/managed.instructions.md
  - .github/instructions/testing.instructions.md
  - .github/update-copilot-summaries.md
  - Src/AppCore/COPILOT.md
  - Src/CacheLight/COPILOT.md
  - Src/Cellar/COPILOT.md
  - Src/Common/COPILOT.md
  - Src/Common/Controls/COPILOT.md
  - Src/Common/FieldWorks/COPILOT.md
  - Src/Common/Filters/COPILOT.md
  - Src/Common/Framework/COPILOT.md
  - Src/Common/FwUtils/COPILOT.md
  - Src/Common/RootSite/COPILOT.md
  - Src/Common/ScriptureUtils/COPILOT.md
  - Src/Common/SimpleRootSite/COPILOT.md
  - Src/Common/UIAdapterInterfaces/COPILOT.md
  - Src/Common/ViewsInterfaces/COPILOT.md
  - Src/DebugProcs/COPILOT.md
  - Src/FXT/COPILOT.md
  - Src/FdoUi/COPILOT.md
  - Src/FwCoreDlgs/COPILOT.md
  - Src/FwParatextLexiconPlugin/COPILOT.md
  - Src/FwResources/COPILOT.md
  - Src/GenerateHCConfig/COPILOT.md
  - Src/InstallValidator/COPILOT.md
  - Src/Kernel/COPILOT.md
  - Src/LCMBrowser/COPILOT.md
  - Src/LexText/COPILOT.md
  - Src/LexText/Discourse/COPILOT.md
  - Src/LexText/FlexPathwayPlugin/COPILOT.md
  - Src/LexText/Interlinear/COPILOT.md
  - Src/LexText/LexTextControls/COPILOT.md
  - Src/LexText/LexTextDll/COPILOT.md
  - Src/LexText/LexTextExe/COPILOT.md
  - Src/LexText/Lexicon/COPILOT.md
  - Src/LexText/Morphology/COPILOT.md
  - Src/LexText/ParserCore/COPILOT.md
  - Src/LexText/ParserUI/COPILOT.md
  - Src/ManagedLgIcuCollator/COPILOT.md
  - Src/ManagedVwDrawRootBuffered/COPILOT.md
  - Src/ManagedVwWindow/COPILOT.md
  - Src/MigrateSqlDbs/COPILOT.md
  - Src/Paratext8Plugin/COPILOT.md
  - Src/ParatextImport/COPILOT.md
  - Src/ProjectUnpacker/COPILOT.md
  - Src/UnicodeCharEditor/COPILOT.md
  - Src/Utilities/COPILOT.md
  - Src/XCore/COPILOT.md

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 17: 9e3edcfef
**"NUnit Conversions"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  17 files changed, 643 insertions(+), 508 deletions(-)

**Project Files Changed**: 1
  - Build/Src/FwBuildTasks/FwBuildTasks.csproj

**C# Source Files**: 13
  - Lib/src/ObjectBrowser/ClassPropertySelector.Designer.cs
  - Lib/src/ScrChecks/ScrChecksTests/CapitalizationCheckSilUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/CapitalizationCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/ChapterVerseTests.cs
  - Lib/src/ScrChecks/ScrChecksTests/CharactersCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/MatchedPairsCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/MixedCapitalizationCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/PunctuationCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/QuotationCheckSilUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/QuotationCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckTests.cs
  - Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckUnitTest.cs
  - Lib/src/ScrChecks/ScrChecksTests/ScrChecksTestBase.cs

**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🧪 Test Framework Migration (NUnit 3→4)

---


## Commit 18: 1dda05293
**"NUnit 4 migration complete"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  7 files changed, 277 insertions(+), 267 deletions(-)


**C# Source Files**: 6
  - Build/Src/FwBuildTasks/FwBuildTasksTests/GoldEticToXliffTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeListsTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/PoToXmlTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/XmlToPoTests.cs
  - Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckUnitTest.cs

**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🧪 Test Framework Migration (NUnit 3→4)

---


## Commit 19: a2a0cf92b
**"and formatting"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  2 files changed, 16 insertions(+), 8 deletions(-)


**C# Source Files**: 1
  - Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckUnitTest.cs

**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🎨 Formatting only

---


## Commit 20: 2f0e4ba2d
**"Next round of build fixes (AI)"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  6 files changed, 146 insertions(+), 6 deletions(-)

**Project Files Changed**: 1
  - Src/LexText/Morphology/MorphologyEditorDll.csproj

**C# Source Files**: 5
  - Lib/src/ObjectBrowser/ClassPropertySelector.Designer.cs
  - Lib/src/ObjectBrowser/ClassPropertySelector.cs
  - Lib/src/ObjectBrowser/FDOHelpers.cs
  - Lib/src/ObjectBrowser/Program.cs
  - Src/xWorks/xWorksTests/InterestingTextsTests.cs

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 21: 60f01c9fa
**"Checkpoint from VS Code for coding agent session"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  5 files changed, 214 insertions(+), 92 deletions(-)

**Project Files Changed**: 1
  - Lib/src/ObjectBrowser/ObjectBrowser.csproj

**C# Source Files**: 4
  - Src/GenerateHCConfig/NullThreadedProgress.cs
  - Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs
  - Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs
  - Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs

**Category**: 💾 Checkpoint/Save point

---


## Commit 22: 29b5158da
**"Automated RhinoMocks to Moq conversion"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  15 files changed, 283 insertions(+), 117 deletions(-)

**Commit Message**:
```
- Created Python script for automated pattern conversion
- Replaced RhinoMocks package with Moq 4.20.70 in all 6 projects
- Converted common patterns: GenerateStub/Mock, .Stub/.Expect, .Return/.Returns
- Converted Arg<T>.Is.Anything to It.IsAny<T>()
- Updated using statements from Rhino.Mocks to Moq

Manual fixes still needed for complex patterns:
- out parameter handling (.OutRef, Arg<T>.Out().Dummy)
- Arg<T>.Is.Equal conversions
- GetArgumentsForCallsMadeOn verifications

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```

**Project Files Changed**: 6
  - Src/Common/Framework/FrameworkTests/FrameworkTests.csproj
  - Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj
  - Src/FwCoreDlgs/FwCoreDlgsTests/FwCoreDlgsTests.csproj
  - Src/LexText/Interlinear/ITextDllTests/ITextDllTests.csproj
  - Src/LexText/Morphology/MorphologyEditorDllTests/MorphologyEditorDllTests.csproj
  - Src/ParatextImport/ParatextImportTests/ParatextImportTests.csproj

**C# Source Files**: 8
  - Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs
  - Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs
  - Src/Common/RootSite/RootSiteTests/RootSiteGroupTests.cs
  - Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs
  - Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs
  - Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs
  - Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs
  - Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs

**Python Scripts**: 1
  - convert_rhinomocks_to_moq.py

**Category**: 🧪 Test Framework Migration (RhinoMocks→Moq)

---


## Commit 23: 9567ca24e
**"Manual fixes for Mock<T>.Object patterns"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  3 files changed, 40 insertions(+), 29 deletions(-)

**Commit Message**:
```
- Fixed MoreRootSiteTests.cs out parameter handling
- Fixed RootSiteGroupTests.cs Mock.Object casts
- Refactored FwEditingHelperTests.cs to use Mock<T> fields with properties
- Fixed MakeMockSelection to return .Object properly

Remaining work:
- FwEditingHelperTests: Convert GetArgumentsForCallsMadeOn() to Moq Verify/Callback
- FwEditingHelperTests: Convert Arg<T>.Is.Equal patterns
- InterlinDocForAnalysisTests: Handle complex out parameter setups
- Build verification after all conversions complete

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**C# Source Files**: 3
  - Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs
  - Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs
  - Src/Common/RootSite/RootSiteTests/RootSiteGroupTests.cs

**Category**: 🐛 Bug Fixes

---


## Commit 24: 1d4de1aa6
**"Complete RhinoMocks to Moq migration documentation"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  2 files changed, 210 insertions(+), 12 deletions(-)

**Commit Message**:
```
- Added comprehensive migration summary document
- Documented all conversion patterns (automated and manual)
- Listed completed files (4/8 fully converted)
- Detailed remaining work with code examples
- Estimated 4-6 hours remaining for completion

Major accomplishments:
- 6/6 projects converted to Moq 4.20.70
- Python automation script for common patterns
- 4/8 test files fully working
- 2/8 test files nearly complete
- 2/8 test files need out parameter work

Next developer can follow RHINOMOCKS_TO_MOQ_MIGRATION.md for remaining conversions.

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**C# Source Files**: 1
  - Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs

**Documentation Files**: 1
  - RHINOMOCKS_TO_MOQ_MIGRATION.md

**Category**: 🧪 Test Framework Migration (RhinoMocks→Moq)

---


## Commit 25: 26975a780
**"Use NUnit 4"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  5 files changed, 195 insertions(+), 98 deletions(-)

**Project Files Changed**: 3
  - Build/Src/FwBuildTasks/FwBuildTasks.csproj
  - Lib/src/ScrChecks/ScrChecksTests/ScrChecksTests.csproj
  - Src/ParatextImport/ParatextImportTests/ParatextImportTests.csproj

**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🧪 Test Framework Migration (NUnit 3→4)

---


## Commit 26: 1ebe7b917
**"Complete RhinoMocks to Moq conversion"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  4 files changed, 226 insertions(+), 174 deletions(-)

**Commit Message**:
```
Fixed all remaining conversion issues:
- Converted GetArgumentsForCallsMadeOn to Moq Callback pattern (11 tests)
- Fixed Arg<T>.Is.Equal to specific parameter values
- Fixed .Return( to .Returns(
- Refactored helper methods to accept Mock<T> parameters
- Fixed out parameter handling in InterlinDocForAnalysisTests
- Fixed mock variable declarations (Mock<T> vs .Object)

All 8 test files now fully converted to Moq 4.20.70:
- MoreRootSiteTests.cs ✓
- RootSiteGroupTests.cs ✓
- FwEditingHelperTests.cs ✓ (completed 11 complex test conversions)
- RespellingTests.cs ✓
- ComboHandlerTests.cs ✓
- GlossToolLoadsGuessContentsTests.cs ✓
- InterlinDocForAnalysisTests.cs ✓ (fixed out parameters)
- FwWritingSystemSetupModelTests.cs ✓

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**C# Source Files**: 4
  - Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs
  - Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs
  - Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs
  - Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs

**Category**: 🧪 Test Framework Migration (RhinoMocks→Moq)

---


## Commit 27: a7cca23d8
**"Update migration documentation to reflect completion"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  1 file changed, 62 insertions(+), 92 deletions(-)

**Commit Message**:
```
Updated RHINOMOCKS_TO_MOQ_MIGRATION.md with:
- Mark all 8 test files as complete
- Document all conversion patterns used
- Remove "Remaining Work" section
- Add "Status: COMPLETE" marker

All RhinoMocks to Moq conversion work is now complete.

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - RHINOMOCKS_TO_MOQ_MIGRATION.md

**Category**: 📚 Documentation Only

---


## Commit 28: 0be56a4b7
**"Merge commit 'a7cca23d84542a5e28d1a63a718ec5c1b62ad03d' into spec-kit"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  16 files changed, 674 insertions(+), 277 deletions(-)

**Project Files Changed**: 1
  - Src/ParatextImport/ParatextImportTests/ParatextImportTests.csproj

**Category**: 🔄 General Changes

---


## Commit 29: 5a5cfc4ea
**"Initial plan"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**: No stats


**Category**: 🔄 General Changes

---


## Commit 30: 0793034c4
**"Enhanced convert_nunit.py with comprehensive Assert converters"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  1 file changed, 319 insertions(+), 6 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🤖 Automation Script

---


## Commit 31: 9c700de0c
**"Convert all NUnit 3 assertions to NUnit 4 in Src directory"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  156 files changed, 6179 insertions(+), 6665 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**C# Source Files**: 156

**Category**: 🧪 Test Framework Migration (NUnit 3→4)

---


## Commit 32: b0ac9bae1
**"Add comprehensive NUnit 4 conversion summary documentation"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  1 file changed, 193 insertions(+)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - NUNIT4_CONVERSION_SUMMARY.md

**Category**: 🧪 Test Framework Migration (NUnit 3→4)

---


## Commit 33: 68a9f05e8
**"Add help message to conversion script"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-05
- **Stats**:  1 file changed, 30 insertions(+)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🔄 General Changes

---


## Commit 34: cce597f91
**"more conversion"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  10 files changed, 993 insertions(+), 314 deletions(-)


**C# Source Files**: 9
  - Build/Src/FwBuildTasks/FwBuildTasksTests/ClouseauTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/GoldEticToXliffTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeListsTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/PoToXmlTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/WxsToWxiTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/XliffToGoldEticTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/XmlToPoTests.cs
  - Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs

**Python Scripts**: 1
  - convert_nunit.py

**Category**: 🔄 General Changes

---


## Commit 35: 55c8c2577
**"fix: resolve build errors after SDK-style migration and test framework upgrades"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  10 files changed, 116 insertions(+), 110 deletions(-)

**Commit Message**:
```
Comprehensive fixes for compilation errors introduced during SDK-style project
migration and upgrades to NUnit 4 and Moq 4.20.70.

## NUnit 4 Migration Fixes

### Fixed .Within() constraint usage (NUnit 4 compatibility)
- NUnit 4's .Within() only works with numeric constraints (EqualNumericConstraint)
- Converted 19 instances where .Within(message) was incorrectly used on string
  assertions in XmlToPoTests.cs and PoToXmlTests.cs
- Changed pattern from .Within(message) to message as third parameter in Assert.That

Files modified:
- Build/Src/FwBuildTasks/FwBuildTasksTests/XmlToPoTests.cs (12 fixes)
- Build/Src/FwBuildTasks/FwBuildTasksTests/PoToXmlTests.cs (7 fixes)

## Moq 4.20.70 Migration Fixes

### Upgraded Moq package
- Updated from Moq 4.17.2 to 4.20.70 for better out parameter support
- Build/nuget-common/packages.config

### Fixed Moq .Object property access
- Added missing .Object property calls to extract mocked instances from Mock<T>
- Affected files:
  - Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs (3 fixes)
  - Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs (2 fixes)

### Converted RhinoMocks to Moq
- FwWritingSystemSetupModelTests.cs: Converted 4 test methods
  - Replaced .Expect().WhenCalled() with .Setup()
  - Replaced .AssertWasCalled() with .Verify()
  - Changed MockRepository.GenerateStub to new Mock<T>().Object

- ComboHandlerTests.cs: Converted RhinoMocks patterns
  - Replaced .Stub() with .Setup()
  - Replaced MockRepository.GenerateStub() with new Mock<T>()
  - Added .Object property access (10 instances)

- RespellingTests.cs: Converted complex mock setups
  - Changed MockRepository.GenerateStub<XMLViewsDataCache>() to new Mock<>()
  - Changed MockRepository.GenerateStub<InterestingTextList>() to new Mock<>()
  - Converted .Stub().Do() patterns to .Setup().Returns() with lambda functions

### Fixed complex out parameter mocking
- MoreRootSiteTests.cs: PropInfo method with 5 out parameters
  - Created PropInfoDelegate type matching IVwSelection.PropInfo signature
  - Used .Callback() with delegate instead of direct out parameter assignment
  - Moq 4.20.70 requires delegate approach for void methods with multiple out params

## SDK-Style Project Fixes

### Fixed MorphologyEditorDll duplicate assembly attributes
- Added GenerateTargetFrameworkAttribute=false to MorphologyEditorDll.csproj
- SDK-style projects auto-generate TargetFrameworkAttribute unless explicitly disabled
- Changed GenerateAssemblyInfo from true to false
- Restored NuGet packages after project modification

### Fixed missing interface member
- GenerateHCConfig: Added missing IProgress.Canceling event to NullThreadedProgress.cs
- Added #pragma warning disable CS0067 for unused event warning

## Build Verification
- All compilation errors resolved (0 errors)
- Build completes successfully
- Only minor warnings remain (GeckoFX DLL references, not blocking)

Fixes compilation errors from origin/chore/migrateToSdkCsproj branch.
```

**Project Files Changed**: 1
  - Src/LexText/Morphology/MorphologyEditorDll.csproj

**C# Source Files**: 8
  - Build/Src/FwBuildTasks/FwBuildTasksTests/PoToXmlTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/XmlToPoTests.cs
  - Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs
  - Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs
  - Src/GenerateHCConfig/NullThreadedProgress.cs
  - Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs
  - Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs
  - Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 36: e2c851059
**"Formatting"**

- **Author**: John Lambert
- **Date**: 2025-11-05
- **Stats**:  7 files changed, 3761 insertions(+), 1034 deletions(-)


**C# Source Files**: 7
  - Build/Src/FwBuildTasks/FwBuildTasksTests/PoToXmlTests.cs
  - Build/Src/FwBuildTasks/FwBuildTasksTests/XmlToPoTests.cs
  - Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs
  - Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs
  - Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs
  - Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs
  - Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs

**Category**: 🔄 General Changes

---


## Commit 37: 4f1c0d8d6
**"Plan out 64 bit, non-registry COM handling"**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  11 files changed, 628 insertions(+), 2 deletions(-)


**Documentation Files**: 11
  - .github/copilot-instructions.md
  - Docs/64bit-regfree-migration.md
  - specs/001-64bit-regfree-com/checklists/requirements.md
  - specs/001-64bit-regfree-com/contracts/manifest-schema.md
  - specs/001-64bit-regfree-com/contracts/msbuild-regfree-contract.md
  - specs/001-64bit-regfree-com/data-model.md
  - specs/001-64bit-regfree-com/plan.md
  - specs/001-64bit-regfree-com/quickstart.md
  - specs/001-64bit-regfree-com/research.md
  - specs/001-64bit-regfree-com/spec.md
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 🔧 64-bit Only Migration

---


## Commit 38: 63f218897
**"Small fixes"**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  2 files changed, 52 insertions(+), 45 deletions(-)


**Documentation Files**: 2
  - Docs/64bit-regfree-migration.md
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 📚 Documentation Only

---


## Commit 39: f7078f199
**"Checkpoint from VS Code for coding agent session"**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  2 files changed, 5 insertions(+), 1 deletion(-)


**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Build Files**: 1
  - Directory.Build.props

**Category**: 💾 Checkpoint/Save point

---


## Commit 40: 1c13e12b6
**"format"**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  1 file changed, 11 insertions(+), 12 deletions(-)


**Build Files**: 1
  - Directory.Build.props

**Category**: 🎨 Formatting only

---


## Commit 41: 223ac32ec
**"Complete T002: Remove Win32/x86/AnyCPU solution platforms, keep x64 only"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  3 files changed, 917 insertions(+), 2266 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 🔧 64-bit Only Migration

---


## Commit 42: b61e13e3c
**"Complete T003: Remove Win32 configurations from all native VCXPROJ files"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  8 files changed, 1040 insertions(+), 1598 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Native Projects**: 7
  - Src/DebugProcs/DebugProcs.vcxproj
  - Src/Generic/Generic.vcxproj
  - Src/Generic/Test/TestGeneric.vcxproj
  - Src/Kernel/Kernel.vcxproj
  - Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleCOMWrapper.vcxproj
  - Src/views/Test/TestViews.vcxproj
  - Src/views/views.vcxproj

**Category**: 🔧 64-bit Only Migration

---


## Commit 43: ada4974ac
**"Complete T004-T005: Verify x64 enforcement in CI and audit build scripts"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  2 files changed, 4 insertions(+), 2 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 🔧 64-bit Only Migration

---


## Commit 44: 2f3a9a6a7
**"Complete T006 and Phase 1: Document build instructions in quickstart.md"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  2 files changed, 100 insertions(+), 13 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 2
  - specs/001-64bit-regfree-com/quickstart.md
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 📚 Documentation Only

---


## Commit 45: 1c2bca84e
**"Complete Phase 2 (T007-T010): Wire up reg-free manifest generation"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  4 files changed, 15 insertions(+), 5 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```

**Project Files Changed**: 2
  - Src/Common/FieldWorks/FieldWorks.csproj
  - Src/LexText/LexTextExe/LexTextExe.csproj

**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Build Files**: 1
  - Src/LexText/LexTextExe/BuildInclude.targets

**Category**: 🔐 Registration-Free COM

---


## Commit 46: 1b54eacde
**"Complete T011-T012: Remove x86 PropertyGroups from core EXE projects"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  3 files changed, 2 insertions(+), 29 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```

**Project Files Changed**: 2
  - Src/Common/FieldWorks/FieldWorks.csproj
  - Src/LexText/LexTextExe/LexTextExe.csproj

**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 🔧 64-bit Only Migration

---


## Commit 47: 2bb6d8b05
**"Complete T022-T023: Update CI for x64-only and manifest artifact upload"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  2 files changed, 13 insertions(+), 3 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Category**: 🔧 64-bit Only Migration

---


## Commit 48: 2131239d4
**"Complete T025-T027: Create ComManifestTestHost for registration-free COM tests"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-06
- **Stats**:  4 files changed, 107 insertions(+), 3 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```

**Project Files Changed**: 1
  - Src/Utilities/ComManifestTestHost/ComManifestTestHost.csproj

**C# Source Files**: 1
  - Src/Utilities/ComManifestTestHost/Program.cs

**Documentation Files**: 1
  - specs/001-64bit-regfree-com/tasks.md

**Build Files**: 1
  - Src/Utilities/ComManifestTestHost/BuildInclude.targets

**Category**: 🔐 Registration-Free COM

---


## Commit 49: bd99fc3e0
**"Closer to a build..."**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  20 files changed, 1377 insertions(+), 981 deletions(-)

**Project Files Changed**: 4
  - Build/Src/NUnitReport/NUnitReport.csproj
  - Lib/src/Converter/ConvertConsole/ConverterConsole.csproj
  - Lib/src/Converter/Converter/Converter.csproj
  - Src/Common/FieldWorks/FieldWorks.csproj

**Documentation Files**: 4
  - Docs/64bit-regfree-migration.md
  - ReadMe.md
  - specs/001-64bit-regfree-com/quickstart.md
  - specs/001-64bit-regfree-com/tasks.md

**Build Files**: 4
  - Build/Installer.targets
  - Build/RegFree.targets
  - Src/Common/FieldWorks/BuildInclude.targets
  - Src/LexText/LexTextExe/BuildInclude.targets

**Native Projects**: 3
  - Src/Generic/Generic.vcxproj
  - Src/Kernel/Kernel.vcxproj
  - Src/views/views.vcxproj

**Category**: 🔧 Native C++ Changes

---


## Commit 50: 154ae71c4
**"More fixes"**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  2 files changed, 10 insertions(+), 5 deletions(-)

**Project Files Changed**: 2
  - Src/LexText/LexTextExe/LexTextExe.csproj
  - Src/LexText/Morphology/MorphologyEditorDll.csproj

**Category**: 🔄 General Changes

---


## Commit 51: 67227eccd
**"Move FwBuildTasks to BuildTools."**

- **Author**: John Lambert
- **Date**: 2025-11-06
- **Stats**:  8 files changed, 624 insertions(+), 468 deletions(-)

**Project Files Changed**: 1
  - Build/Src/FwBuildTasks/FwBuildTasks.csproj

**Documentation Files**: 1
  - specs/001-64bit-regfree-com/quickstart.md

**Build Files**: 5
  - Build/FwBuildTasks.targets
  - Build/Localize.targets
  - Build/RegFree.targets
  - Build/SetupInclude.targets
  - Build/Windows.targets

**Category**: 🔄 General Changes

---


## Commit 52: 0b1f6adc5
**"Debug from VSCode"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  132 files changed, 709 insertions(+), 1173 deletions(-)

**Project Files Changed**: 110
  - (Too many to list - 110 files)

**Documentation Files**: 21
  - Src/Common/FwUtils/COPILOT.md
  - Src/Common/RootSite/COPILOT.md
  - Src/Common/ScriptureUtils/COPILOT.md
  - Src/Common/SimpleRootSite/COPILOT.md
  - Src/Common/UIAdapterInterfaces/COPILOT.md
  - Src/Common/ViewsInterfaces/COPILOT.md
  - Src/FdoUi/COPILOT.md
  - Src/FwCoreDlgs/COPILOT.md
  - Src/FwParatextLexiconPlugin/COPILOT.md
  - Src/FwResources/COPILOT.md
  - Src/GenerateHCConfig/COPILOT.md
  - Src/InstallValidator/COPILOT.md
  - Src/LCMBrowser/COPILOT.md
  - Src/LexText/Discourse/COPILOT.md
  - Src/LexText/FlexPathwayPlugin/COPILOT.md
  - Src/LexText/Interlinear/COPILOT.md
  - Src/LexText/LexTextControls/COPILOT.md
  - Src/LexText/LexTextDll/COPILOT.md
  - Src/LexText/LexTextExe/COPILOT.md
  - Src/LexText/Lexicon/COPILOT.md
  - Src/LexText/Morphology/COPILOT.md

**Category**: 📦 Mass SDK Conversion

---


## Commit 53: 9c559029d
**"Add icons.  Remove x86 build info"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  109 files changed, 619 insertions(+), 1522 deletions(-)

**Project Files Changed**: 103
  - (Too many to list - 103 files)

**Python Scripts**: 2
  - tools/include_icons_in_projects.py
  - tools/remove_x86_property_groups.py

**Build Files**: 1
  - Build/mkall.targets

**Category**: 📦 Mass SDK Conversion

---


## Commit 54: bb638fed5
**"Force everything to x64."**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  112 files changed, 687 insertions(+), 245 deletions(-)

**Project Files Changed**: 111
  - (Too many to list - 111 files)

**Python Scripts**: 1
  - tools/enforce_x64_platform.py

**Category**: 📦 Mass SDK Conversion

---


## Commit 55: c723f584e
**"Moving from the build files..."**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  7 files changed, 607 insertions(+), 582 deletions(-)


**Documentation Files**: 2
  - Docs/64bit-regfree-migration.md
  - specs/001-64bit-regfree-com/quickstart.md

**Build Files**: 2
  - Build/SetupInclude.targets
  - Build/mkall.targets

**Category**: 📚 Documentation Only

---


## Commit 56: c6b9f4a91
**"All net48"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  1 file changed, 1 insertion(+), 1 deletion(-)

**Project Files Changed**: 1
  - Build/Src/FwBuildTasks/FwBuildTasks.csproj

**Category**: 🔄 General Changes

---


## Commit 57: 9d14e03af
**"It now builds with FieldWorks.proj!"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  10 files changed, 50 insertions(+), 136 deletions(-)

**Project Files Changed**: 1
  - Build/Src/FwBuildTasks/FwBuildTasks.csproj

**C# Source Files**: 2
  - Build/Src/FwBuildTasks/CollectTargets.cs
  - Build/Src/FwBuildTasks/DownloadFilesFromTeamCity.cs

**Build Files**: 6
  - Build/FwBuildTasks.targets
  - Build/Localize.targets
  - Build/RegFree.targets
  - Build/SetupInclude.targets
  - Build/Windows.targets
  - Build/mkall.targets

**Category**: 🔨 Build Fixes + Code Changes

---


## Commit 58: 0e5567297
**"Minor updates"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  52 files changed, 411 insertions(+), 247 deletions(-)


**C# Source Files**: 1
  - Build/Src/FwBuildTasks/CollectTargets.cs

**Documentation Files**: 51
  - Src/CacheLight/COPILOT.md
  - Src/Common/FieldWorks/COPILOT.md
  - Src/Common/Filters/COPILOT.md
  - Src/Common/Framework/COPILOT.md
  - Src/Common/FwUtils/COPILOT.md
  - Src/Common/RootSite/COPILOT.md
  - Src/Common/ScriptureUtils/COPILOT.md
  - Src/Common/SimpleRootSite/COPILOT.md
  - Src/Common/UIAdapterInterfaces/COPILOT.md
  - Src/Common/ViewsInterfaces/COPILOT.md
  - Src/FXT/COPILOT.md
  - Src/FdoUi/COPILOT.md
  - Src/FwCoreDlgs/COPILOT.md
  - Src/FwParatextLexiconPlugin/COPILOT.md
  - Src/FwResources/COPILOT.md
  - Src/GenerateHCConfig/COPILOT.md
  - Src/InstallValidator/COPILOT.md
  - Src/LCMBrowser/COPILOT.md
  - Src/LexText/Discourse/COPILOT.md
  - Src/LexText/FlexPathwayPlugin/COPILOT.md
  - Src/LexText/Interlinear/COPILOT.md
  - Src/LexText/LexTextControls/COPILOT.md
  - Src/LexText/LexTextDll/COPILOT.md
  - Src/LexText/LexTextExe/COPILOT.md
  - Src/LexText/Lexicon/COPILOT.md
  - Src/LexText/Morphology/COPILOT.md
  - Src/LexText/ParserCore/COPILOT.md
  - Src/LexText/ParserUI/COPILOT.md
  - Src/ManagedLgIcuCollator/COPILOT.md
  - Src/ManagedVwDrawRootBuffered/COPILOT.md
  - Src/ManagedVwWindow/COPILOT.md
  - Src/MigrateSqlDbs/COPILOT.md
  - Src/Paratext8Plugin/COPILOT.md
  - Src/ParatextImport/COPILOT.md
  - Src/ProjectUnpacker/COPILOT.md
  - Src/UnicodeCharEditor/COPILOT.md
  - Src/Utilities/COPILOT.md
  - Src/Utilities/FixFwData/COPILOT.md
  - Src/Utilities/FixFwDataDll/COPILOT.md
  - Src/Utilities/MessageBoxExLib/COPILOT.md
  - Src/Utilities/Reporting/COPILOT.md
  - Src/Utilities/SfmStats/COPILOT.md
  - Src/Utilities/SfmToXml/COPILOT.md
  - Src/Utilities/XMLUtils/COPILOT.md
  - Src/XCore/COPILOT.md
  - Src/XCore/FlexUIAdapter/COPILOT.md
  - Src/XCore/SilSidePane/COPILOT.md
  - Src/XCore/xCoreInterfaces/COPILOT.md
  - Src/XCore/xCoreTests/COPILOT.md
  - Src/views/COPILOT.md
  - Src/xWorks/COPILOT.md

**Category**: 🔄 General Changes

---


## Commit 59: ab685b911
**"Fix warnings"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  3 files changed, 3 insertions(+)


**Native Projects**: 3
  - Src/Generic/Generic.vcxproj
  - Src/Kernel/Kernel.vcxproj
  - Src/views/views.vcxproj

**Category**: 🔧 Native C++ Changes

---


## Commit 60: b6e069eef
**"Getting closer to a clean build"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  11 files changed, 528 insertions(+), 355 deletions(-)


**Documentation Files**: 1
  - .github/copilot-instructions.md

**Build Files**: 2
  - Build/SetupInclude.targets
  - Build/mkall.targets

**Native Projects**: 1
  - Lib/src/unit++/VS/unit++.vcxproj

**Scripts**: 2
  - build.ps1
  - build.sh

**Category**: 📚 Documentation Only

---


## Commit 61: efcc3ed54
**"One error at a time"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  2 files changed, 25 insertions(+), 20 deletions(-)


**Build Files**: 1
  - Build/mkall.targets

**Scripts**: 1
  - build.ps1

**Category**: ⚙️ Build Infrastructure

---


## Commit 62: 61610e12c
**"Checkpoint from VS Code for coding agent session"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  3 files changed, 38 insertions(+), 5 deletions(-)


**Build Files**: 2
  - Build/Windows.targets
  - Build/mkall.targets

**Scripts**: 1
  - build.ps1

**Category**: 💾 Checkpoint/Save point

---


## Commit 63: eec3f0ad7
**"formatting"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  1 file changed, 4 insertions(+), 2 deletions(-)


**Build Files**: 1
  - Build/mkall.targets

**Category**: 🎨 Formatting only

---


## Commit 64: 0b13207c5
**"Fix arch not being set properly"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  2 files changed, 2 insertions(+), 6 deletions(-)


**Build Files**: 1
  - Build/mkall.targets

**Scripts**: 1
  - build.ps1

**Category**: ⚙️ Build Infrastructure

---


## Commit 65: 4f4bd556c
**"Checkpoint from VS Code for coding agent session"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  8 files changed, 565 insertions(+), 112 deletions(-)


**C# Source Files**: 1
  - Src/Common/ViewsInterfaces/VwPropertyStoreManaged.cs

**Documentation Files**: 1
  - .github/instructions/build.instructions.md

**Build Files**: 4
  - Build/Localize.targets
  - Build/mkall.targets
  - Directory.Build.props
  - Src/Common/ViewsInterfaces/BuildInclude.targets

**Scripts**: 1
  - build.ps1

**Category**: 💾 Checkpoint/Save point

---


## Commit 66: bb0fc10b2
**"Add traversal support to build.sh (to be simplified)"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-07
- **Stats**:  1 file changed, 58 insertions(+), 11 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Scripts**: 1
  - build.sh

**Category**: 🏗️ Build System - Traversal SDK

---


## Commit 67: 86d541630
**"Complete MSBuild Traversal SDK migration - sunset legacy build"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-07
- **Stats**:  9 files changed, 166 insertions(+), 155 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 2
  - .github/instructions/build.instructions.md
  - ReadMe.md

**Scripts**: 2
  - build.ps1
  - build.sh

**Category**: 🏗️ Build System - Traversal SDK

---


## Commit 68: 48c920c6e
**"Fully modernize build system - remove all legacy paths"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-07
- **Stats**:  6 files changed, 276 insertions(+), 38 deletions(-)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 2
  - .github/instructions/build.instructions.md
  - Docs/traversal-sdk-migration.md

**Build Files**: 1
  - Build/Installer.targets

**Scripts**: 1
  - build.ps1

**Category**: 🗑️ Legacy Removal

---


## Commit 69: 0efcc7153
**"Add comprehensive implementation summary document"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-07
- **Stats**:  1 file changed, 306 insertions(+)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - TRAVERSAL_SDK_IMPLEMENTATION.md

**Category**: 📚 Documentation Only

---


## Commit 70: 7144ad8a3
**"Put back teamcity downloads back in (Gecko, Args, ExCss)"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  4 files changed, 111 insertions(+), 3 deletions(-)


**C# Source Files**: 1
  - Build/Src/FwBuildTasks/DownloadFilesFromTeamCity.cs

**Build Files**: 3
  - Build/FwBuildTasks.targets
  - Build/Localize.targets
  - Build/mkall.targets

**Category**: 🔄 General Changes

---


## Commit 71: 7463c6b79
**"Initial plan"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**: No stats


**Category**: 🔄 General Changes

---


## Commit 72: 57df3c789
**"Aggressively modernize build system - remove all legacy SDK code"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  5 files changed, 75 insertions(+), 336 deletions(-)

**Commit Message**:
```
- Removed 210 lines from Build/mkall.targets (1243 → 1033 lines)
- Deleted legacy targets: mkall, remakefw*, allCsharp, allCpp, test targets
- Removed ALL PDB download/copy logic (SDK handles this automatically)
- Removed symbol package downloads (snupkg, NoSymbols logic)
- Deleted agent-build-fw.sh (legacy headless build script)
- Cleaned up Build/FieldWorks.proj (removed legacy target references)
- Updated documentation to reflect modern build system

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 2
  - TRAVERSAL_SDK_IMPLEMENTATION.md
  - specs/001-64bit-regfree-com/quickstart.md

**Build Files**: 1
  - Build/mkall.targets

**Scripts**: 1
  - agent-build-fw.sh

**Category**: 🗑️ Legacy Removal

---


## Commit 73: 1aec44046
**"Remove 30 legacy build files - aggressive modernization phase 2"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  32 files changed, 4 insertions(+), 617 deletions(-)

**Commit Message**:
```
- Deleted 29 legacy batch/cmd files from Bin/ (mkall.bat, RemakeFw.bat, mk*.bat, wrapper.cmd, etc.)
- Deleted Build/native.proj (optional wrapper, not used)
- Updated documentation and removed stale references
- All legacy pre-MSBuild build entry points removed

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - TRAVERSAL_SDK_IMPLEMENTATION.md

**Scripts**: 26
  - Bin/Mktstw.bat
  - Bin/RemakeFw.bat
  - Bin/_EnsureRoot.bat
  - Bin/mkGenLib-tst.bat
  - Bin/mkGenLib.bat
  - Bin/mkaft.bat
  - Bin/mkall-tst.bat
  - Bin/mkdp.bat
  - Bin/mkecob.bat
  - Bin/mkfwk-tst.bat
  - Bin/mkfwk.bat
  - Bin/mkgrc.bat
  - Bin/mkgre.bat
  - Bin/mkhv.bat
  - Bin/mkhw.bat
  - Bin/mkhwt.bat
  - Bin/mkhwv.bat
  - Bin/mkhwx.bat
  - Bin/mklg-tst.bat
  - Bin/mklg.bat
  - Bin/mklgt.bat
  - Bin/mktlbs.bat
  - Bin/mktsth.bat
  - Bin/mktv.bat
  - Bin/mkvw-tst.bat
  - Bin/mkvw.bat

**Category**: 🗑️ Legacy Removal

---


## Commit 74: fadf0b25d
**"Remove 6 more legacy tool binaries from Bin/"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  6 files changed, 45 deletions(-)

**Commit Message**:
```
- Deleted ReadKey.exe, WriteKey.exe, WriteKey.exe.manifest
- Deleted md5sums.exe, mkdir.exe, BCopy.exe
- None of these pre-built tools were referenced in modern build
- Can be replaced by PowerShell cmdlets or dotnet tools if needed

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Category**: 🗑️ Legacy Removal

---


## Commit 75: ea7f9daae
**"Add comprehensive legacy removal summary documentation"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  1 file changed, 207 insertions(+)

**Commit Message**:
```
- Created LEGACY_REMOVAL_SUMMARY.md documenting all changes
- Lists all 36 removed files with rationale
- Documents 210 lines removed from mkall.targets
- Explains why removals were safe
- Identifies future modernization opportunities
- Provides testing recommendations

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - LEGACY_REMOVAL_SUMMARY.md

**Category**: 🗑️ Legacy Removal

---


## Commit 76: a36093f97
**"small fix"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  1 file changed, 14 insertions(+), 8 deletions(-)


**Build Files**: 1
  - Build/Localize.targets

**Category**: 🔄 General Changes

---


## Commit 77: 56d938dac
**"Fix"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  1 file changed, 12 insertions(+), 21 deletions(-)


**Build Files**: 1
  - Build/mkall.targets

**Category**: 🔄 General Changes

---


## Commit 78: 328a4d820
**"Revert "Remove 6 more legacy tool binaries from Bin/""**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  6 files changed, 45 insertions(+)

**Commit Message**:
```
This reverts commit fadf0b25dc2df85aedbdab5818fa2c0f1ba43f3d.
```


**Category**: 🗑️ Legacy Removal

---


## Commit 79: 958690246
**"Initial plan"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**: No stats


**Category**: 🔄 General Changes

---


## Commit 80: 0231aca36
**"Add DLL modernization plan document to repository root"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  1 file changed, 330 insertions(+)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - DLL_MODERNIZATION_PLAN.md

**Category**: 📚 Documentation Only

---


## Commit 81: 864480ec9
**"Phase 1: Add icu.net and mixpanel-csharp PackageReferences to key projects"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  8 files changed, 9 insertions(+)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```

**Project Files Changed**: 8
  - Src/Common/Controls/FwControls/FwControls.csproj
  - Src/Common/Controls/Widgets/Widgets.csproj
  - Src/Common/Controls/XMLViews/XMLViews.csproj
  - Src/Common/FieldWorks/FieldWorks.csproj
  - Src/Common/Filters/Filters.csproj
  - Src/Common/Framework/Framework.csproj
  - Src/Common/FwUtils/FwUtils.csproj
  - Src/Common/RootSite/RootSite.csproj

**Category**: 📦 Package Updates

---


## Commit 82: 75f77a11f
**"Add Python scripts for efficient PackageReference management"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  4 files changed, 1273 insertions(+)

**Commit Message**:
```
- add_package_reference.py: Add packages to projects with glob support
- remove_package_reference.py: Remove packages from projects
- find_projects_using_namespace.py: Find projects using/not using namespaces
- Supports cleanup of unused references and batch operations

Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - ADD_PACKAGE_REFERENCE_README.md

**Python Scripts**: 3
  - add_package_reference.py
  - find_projects_using_namespace.py
  - remove_package_reference.py

**Category**: 📚 Documentation Only

---


## Commit 83: e1efb3065
**"Add quick start guide for PackageReference management scripts"**

- **Author**: copilot-swe-agent[bot]
- **Date**: 2025-11-08
- **Stats**:  1 file changed, 308 insertions(+)

**Commit Message**:
```
Co-authored-by: johnml1135 <13733556+johnml1135@users.noreply.github.com>
```


**Documentation Files**: 1
  - PACKAGE_MANAGEMENT_QUICKSTART.md

**Category**: 📚 Documentation Only

---


## Commit 84: f039d7d69
**"Updated packages."**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  20 files changed, 190 insertions(+), 345 deletions(-)

**Project Files Changed**: 17
  - (Too many to list - 17 files)

**Build Files**: 2
  - Build/Localize.targets
  - Build/mkall.targets

**Scripts**: 1
  - build.sh

**Category**: 📦 Package Updates

---


## Commit 85: 552a064a8
**"All SDK format now"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  16 files changed, 537 insertions(+), 357 deletions(-)

**Project Files Changed**: 1
  - Build/Src/NativeBuild/NativeBuild.csproj

**Documentation Files**: 6
  - .github/instructions/build.instructions.md
  - Docs/traversal-sdk-migration.md
  - LEGACY_REMOVAL_SUMMARY.md
  - NON_SDK_ELIMINATION.md
  - TRAVERSAL_SDK_IMPLEMENTATION.md
  - specs/001-64bit-regfree-com/quickstart.md

**Build Files**: 2
  - Build/mkall.targets
  - Src/Common/ViewsInterfaces/BuildInclude.targets

**Scripts**: 2
  - build.ps1
  - build.sh

**Category**: ⚙️ Build Infrastructure

---


## Commit 86: 6319f01fa
**"Non-sdk native builds"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  2 files changed, 12 insertions(+), 27 deletions(-)

**Project Files Changed**: 1
  - Build/Src/NativeBuild/NativeBuild.csproj

**Category**: 🔄 General Changes

---


## Commit 87: 940bd65bf
**"More fixes"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  5 files changed, 61 insertions(+), 29 deletions(-)

**Project Files Changed**: 1
  - Build/Src/NativeBuild/NativeBuild.csproj

**Build Files**: 3
  - Build/SetupInclude.targets
  - Build/Windows.targets
  - Build/mkall.targets

**Category**: 🔄 General Changes

---


## Commit 88: 189cd3662
**"Closer to building"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  3 files changed, 10 insertions(+), 11 deletions(-)

**Project Files Changed**: 1
  - Build/Src/FwBuildTasks/FwBuildTasks.csproj

**Build Files**: 2
  - Build/FwBuildTasks.targets
  - Build/mkall.targets

**Category**: 🔄 General Changes

---


## Commit 89: 0fd887b15
**"Use powershell"**

- **Author**: John Lambert
- **Date**: 2025-11-07
- **Stats**:  4 files changed, 229 insertions(+), 26 deletions(-)


**C# Source Files**: 1
  - Build/Src/FwBuildTasks/Make.cs

**Documentation Files**: 1
  - .github/BUILD_REQUIREMENTS.md

**Scripts**: 2
  - build.ps1
  - build.sh

**Category**: ⚙️ Build Infrastructure

---


## Commit 90: 717cc23ec
**"Fix RegFree manifest generation failure in SDK-style projects"**

- **Author**: John Lambert
- **Date**: 2025-11-08
- **Stats**:  13 files changed, 643 insertions(+), 399 deletions(-)

**Commit Message**:
```
Problem:
--------
The build was failing with "Failed to load and parse the manifest. The
system cannot find the file specified" when trying to embed manifests
into FieldWorks.exe. This occurred because SDK-style projects have
different default behaviors for OutDir vs OutputPath, causing the RegFree
task and mt.exe to look for files in different locations.

Root Cause:
-----------
1. OutputPath was correctly set to "Output\Debug\" in Src/Directory.Build.props
2. OutDir was NOT explicitly set, defaulting to SDK behavior (bin\x64\Debug\net48\)
3. RegFree task created manifests at $(Executable).manifest (using OutDir)
4. AttachManifest target's mt.exe ran with WorkingDirectory=$(OutDir)
5. Result: manifest and executable were in different directories

Solution:
---------
1. **Src/Directory.Build.props**: Explicitly set OutDir=$(OutputPath) to
   ensure both properties point to the same location (Output\Debug\)

2. **Build/RegFree.targets**:
   - Removed WorkingDirectory from mt.exe Exec command
   - Simplified to use full paths from $(Executable) property
   - Removed unnecessary Inputs/Outputs from CreateManifest target

3. **Build/FwBuildTasks.targets**: Added Runtime="CLR4" Architecture="x64"
   to RegFree UsingTask to force 64-bit MSBuild host (prevents BadImageFormat
   errors when loading 64-bit native DLLs)

4. **Build/Src/FwBuildTasks/RegFree.cs**:
   - Removed obsolete registry redirection code (RegHelper usage)
   - Simplified to process type libraries directly
   - Read COM metadata from HKCR when available, use defaults otherwise
   - Code formatting improvements for readability

5. **Build/Src/FwBuildTasks/RegFreeCreator.cs**:
   - Fixed manifest architecture values (win64/amd64 for x64 builds)
   - Improved ProcessClasses/ProcessInterfaces to read from actual registry
   - Better error handling and logging
   - Code formatting improvements

6. **Build/mkall.targets**:
   - Added stub manifest creation for FwKernel.X and Views.X
   - Hard-coded Platform="x64" (removing unreliable $(Platform) variable)
   - Added validation to fail fast if manifests not generated
   - Fixed LCM artifact paths (LcmBuildTasksDir, LcmModelArtifactsDir)

7. **Directory.Build.props**: Added PreferredToolArchitecture=x64 to ensure
   64-bit MSBuild host is used throughout the build

Additional Changes:
-------------------
- Build/Src/NativeBuild/NativeBuild.csproj: Set fwrt early for NuGet restore,
  added PackageReference for SIL.LCModel.Core
- Build/SetupInclude.targets: Path computation fixes for LCM artifacts
- Build/Windows.targets: Added FindXsltcPath task for XSLT compilation
- Build/Src/FwBuildTasks/RegHelper.cs: Cleanup of unused registry code
- Src/views/Views.mak: Commented out BUILD_REGSVR (no longer needed)
- Src/Common/FieldWorks/FieldWorks.exe.manifest: Regenerated with correct
  architecture values

Verification:
-------------
- msbuild Src\Common\FieldWorks\FieldWorks.csproj /t:Clean,Build succeeds
- Output\Debug\FieldWorks.exe.manifest created with correct COM entries
- Manifest successfully embedded in FieldWorks.exe
- All manifest files have proper win64/amd64 architecture attributes

This fix eliminates the Access Violation crashes and "file not found"
errors that were blocking the 64-bit registration-free COM migration.
```

**Project Files Changed**: 1
  - Build/Src/NativeBuild/NativeBuild.csproj

**C# Source Files**: 3
  - Build/Src/FwBuildTasks/RegFree.cs
  - Build/Src/FwBuildTasks/RegFreeCreator.cs
  - Build/Src/FwBuildTasks/RegHelper.cs

**Build Files**: 7
  - Build/FwBuildTasks.targets
  - Build/RegFree.targets
  - Build/SetupInclude.targets
  - Build/Windows.targets
  - Build/mkall.targets
  - Directory.Build.props
  - Src/Directory.Build.props

**Category**: 🔐 Registration-Free COM

---


## Commit 91: 53e2b69a1
**"It builds!"**

- **Author**: John Lambert
- **Date**: 2025-11-08
- **Stats**:  3 files changed, 214 insertions(+), 30 deletions(-)


**C# Source Files**: 3
  - Src/FXT/FxtExe/ConsoleLcmUI.cs
  - Src/FXT/FxtExe/NullThreadedProgress.cs
  - Src/FXT/FxtExe/main.cs

**Category**: 🔄 General Changes

---


## Commit 92: c4b4c55fe
**"Checkpoint from VS Code for coding agent session"**

- **Author**: John Lambert
- **Date**: 2025-11-08
- **Stats**:  7 files changed, 27 insertions(+), 22 deletions(-)

**Project Files Changed**: 2
  - Build/Src/NUnitReport/NUnitReport.csproj
  - Src/InstallValidator/InstallValidatorTests/InstallValidatorTests.csproj

**C# Source Files**: 1
  - Build/Src/FwBuildTasks/RegFreeCreator.cs

**Build Files**: 1
  - Build/SetupInclude.targets

**Category**: 💾 Checkpoint/Save point

---


## Commit 93: 3fb3b608c
**"formatting"**

- **Author**: John Lambert
- **Date**: 2025-11-08
- **Stats**:  1 file changed, 12 insertions(+), 10 deletions(-)


**C# Source Files**: 1
  - Build/Src/FwBuildTasks/RegFreeCreator.cs

**Category**: 🎨 Formatting only

---
