# FieldWorks.targets Compatibility Inventory

_Generated on 2025-12-08_

FieldWorks.proj is now the authoritative traversal build, but certain workflows (warnings-only builds, "allCsharp" buckets, etc.) still shell out to the auto-generated Build/FieldWorks.targets. The file itself dates back to the pre-SDK mkall build and is produced by Build/Src/FwBuildTasks/CollectTargets.cs every time the Setup target runs.

## Why the term "legacy" comes up
- Build/FieldWorks.targets predates the Traversal SDK migration documented in SDK_MIGRATION.md and mirrors the old mkall build graph.
- The projects listed below are not obsolete; they are simply executed through that generated target file whenever callers invoke `allCsharp` or `allCsharpNoTests`.
- Recent tooling (e.g., TEST_WARNINGS_PLAN) still depends on those aggregate targets, which is why a compatibility bridge exists inside FieldWorks.proj.

## Clean architecture alignment & naming
- Traversal now owns the aggregates: `AllManaged` (no tests) and `AllManagedWithTests` live in `FieldWorks.proj`; `allCsharp`/`allCsharpNoTests` map to those targets.
- The generated `Build/FieldWorks.targets` is no longer required for the managed aggregates; it can be retained temporarily for other legacy scenarios, but the primary flows are unified under traversal.
- Layering is simplified: `build.ps1` (environment/bootstrap) ➜ traversal (`FieldWorks.proj` with aggregates) ➜ individual projects.

## Build pipeline evolution
| Stage | Previous behavior | Current behavior | Improvement |
| --- | --- | --- | --- |
| 1. Bootstrap tasks | `build.ps1` copied `FwBuildTasks.dll`; callers invoked `Build/FieldWorks.targets` directly when they needed `allCsharp`, bypassing traversal. | `build.ps1` still bootstraps tasks, but callers stay on traversal and use `AllManagedWithTests`/`allCsharp` directly. | Single entry point; no generated-file hop for managed aggregates.
| 2. Generate aggregate targets | Users had to remember `msbuild Build/FieldWorks.targets /t:allCsharp` (legacy naming). | Users call `msbuild FieldWorks.proj /t:allCsharp` (or `AllManagedWithTests`), which is traversal-native. | Consistent with traversal SDK; honors declared ordering.
| 3. Future migration | No path to retire `Build/FieldWorks.targets`; scripts/docs still referenced it directly. | Managed aggregates are unified; remaining legacy uses of `Build/FieldWorks.targets` can now be audited and removed. | Clear exit path: delete shim after confirming no consumers.

## Inventory overview
- **Total projects routed through Build/FieldWorks.targets: 101**
- Groups correspond to the first path segment under Src/ (or Lib/).

| Component | Projects | Notes |
| --- | --- | --- |
| CacheLight | 2 | e.g., Src/CacheLight/CacheLight.csproj |
| Common | 26 | e.g., Src/Common/Controls/Design/Design.csproj |
| FdoUi | 2 | e.g., Src/FdoUi/FdoUi.csproj |
| FwCoreDlgs | 4 | e.g., Src/FwCoreDlgs/FwCoreDlgs.csproj |
| FwParatextLexiconPlugin | 2 | e.g., Src/FwParatextLexiconPlugin/FwParatextLexiconPlugin.csproj |
| FwResources | 1 | e.g., Src/FwResources/FwResources.csproj |
| FXT | 3 | e.g., Src/FXT/FxtDll/FxtDll.csproj |
| GenerateHCConfig | 1 | e.g., Src/GenerateHCConfig/GenerateHCConfig.csproj |
| InstallValidator | 2 | e.g., Src/InstallValidator/InstallValidator.csproj |
| LCMBrowser | 1 | e.g., Src/LCMBrowser/LCMBrowser.csproj |
| LexText | 22 | e.g., Src/LexText/Discourse/Discourse.csproj |
| ManagedLgIcuCollator | 2 | e.g., Src/ManagedLgIcuCollator/ManagedLgIcuCollator.csproj |
| ManagedVwDrawRootBuffered | 1 | e.g., Src/ManagedVwDrawRootBuffered/ManagedVwDrawRootBuffered.csproj |
| ManagedVwWindow | 2 | e.g., Src/ManagedVwWindow/ManagedVwWindow.csproj |
| MigrateSqlDbs | 1 | e.g., Src/MigrateSqlDbs/MigrateSqlDbs.csproj |
| Paratext8Plugin | 2 | e.g., Src/Paratext8Plugin/Paratext8Plugin.csproj |
| ParatextImport | 2 | e.g., Src/ParatextImport/ParatextImport.csproj |
| ProjectUnpacker | 1 | e.g., Src/ProjectUnpacker/ProjectUnpacker.csproj |
| UnicodeCharEditor | 2 | e.g., Src/UnicodeCharEditor/UnicodeCharEditor.csproj |
| Utilities | 10 | e.g., Src/Utilities/ComManifestTestHost/ComManifestTestHost.csproj |
| XCore | 7 | e.g., Src/XCore/xCore.csproj |
| xWorks | 2 | e.g., Src/xWorks/xWorks.csproj |
| Lib/ScrChecks | 2 | e.g., Lib/src/ScrChecks/ScrChecks.csproj |
| Lib/ObjectBrowser | 1 | e.g., Lib/src/ObjectBrowser/ObjectBrowser.csproj |

## Detailed inventory (ordered by build graph)

### CacheLight (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | CacheLight | Src/CacheLight/CacheLight.csproj |
| Test | CacheLightTests | Src/CacheLight/CacheLightTests/CacheLightTests.csproj |

### Common (26)

| Kind | Project | Path |
| --- | --- | --- |
| Product | Design | Src/Common/Controls/Design/Design.csproj |
| Product | DetailControls | Src/Common/Controls/DetailControls/DetailControls.csproj |
| Test | DetailControlsTests | Src/Common/Controls/DetailControls/DetailControlsTests/DetailControlsTests.csproj |
| Product | FwControls | Src/Common/Controls/FwControls/FwControls.csproj |
| Test | FwControlsTests | Src/Common/Controls/FwControls/FwControlsTests/FwControlsTests.csproj |
| Product | Widgets | Src/Common/Controls/Widgets/Widgets.csproj |
| Test | WidgetsTests | Src/Common/Controls/Widgets/WidgetsTests/WidgetsTests.csproj |
| Product | XMLViews | Src/Common/Controls/XMLViews/XMLViews.csproj |
| Test | XMLViewsTests | Src/Common/Controls/XMLViews/XMLViewsTests/XMLViewsTests.csproj |
| Product | FieldWorks | Src/Common/FieldWorks/FieldWorks.csproj |
| Test | FieldWorksTests | Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.csproj |
| Product | Filters | Src/Common/Filters/Filters.csproj |
| Test | FiltersTests | Src/Common/Filters/FiltersTests/FiltersTests.csproj |
| Product | Framework | Src/Common/Framework/Framework.csproj |
| Test | FrameworkTests | Src/Common/Framework/FrameworkTests/FrameworkTests.csproj |
| Product | FwUtils | Src/Common/FwUtils/FwUtils.csproj |
| Test | FwUtilsTests | Src/Common/FwUtils/FwUtilsTests/FwUtilsTests.csproj |
| Product | RootSite | Src/Common/RootSite/RootSite.csproj |
| Test | RootSiteTests | Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj |
| Product | ScriptureUtils | Src/Common/ScriptureUtils/ScriptureUtils.csproj |
| Test | ScriptureUtilsTests | Src/Common/ScriptureUtils/ScriptureUtilsTests/ScriptureUtilsTests.csproj |
| Product | SimpleRootSite | Src/Common/SimpleRootSite/SimpleRootSite.csproj |
| Test | SimpleRootSiteTests | Src/Common/SimpleRootSite/SimpleRootSiteTests/SimpleRootSiteTests.csproj |
| Product | UIAdapterInterfaces | Src/Common/UIAdapterInterfaces/UIAdapterInterfaces.csproj |
| Product | ViewsInterfaces | Src/Common/ViewsInterfaces/ViewsInterfaces.csproj |
| Test | ViewsInterfacesTests | Src/Common/ViewsInterfaces/ViewsInterfacesTests/ViewsInterfacesTests.csproj |

### FdoUi (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | FdoUi | Src/FdoUi/FdoUi.csproj |
| Test | FdoUiTests | Src/FdoUi/FdoUiTests/FdoUiTests.csproj |

### FwCoreDlgs (4)

| Kind | Project | Path |
| --- | --- | --- |
| Product | FwCoreDlgs | Src/FwCoreDlgs/FwCoreDlgs.csproj |
| Product | FwCoreDlgControls | Src/FwCoreDlgs/FwCoreDlgControls/FwCoreDlgControls.csproj |
| Test | FwCoreDlgControlsTests | Src/FwCoreDlgs/FwCoreDlgControls/FwCoreDlgControlsTests/FwCoreDlgControlsTests.csproj |
| Test | FwCoreDlgsTests | Src/FwCoreDlgs/FwCoreDlgsTests/FwCoreDlgsTests.csproj |

### FwParatextLexiconPlugin (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | FwParatextLexiconPlugin | Src/FwParatextLexiconPlugin/FwParatextLexiconPlugin.csproj |
| Test | FwParatextLexiconPluginTests | Src/FwParatextLexiconPlugin/FwParatextLexiconPluginTests/FwParatextLexiconPluginTests.csproj |

### FwResources (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | FwResources | Src/FwResources/FwResources.csproj |

### FXT (3)

| Kind | Project | Path |
| --- | --- | --- |
| Product | FxtDll | Src/FXT/FxtDll/FxtDll.csproj |
| Test | FxtDllTests | Src/FXT/FxtDll/FxtDllTests/FxtDllTests.csproj |
| Product | FxtExe | Src/FXT/FxtExe/FxtExe.csproj |

### GenerateHCConfig (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | GenerateHCConfig | Src/GenerateHCConfig/GenerateHCConfig.csproj |

### InstallValidator (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | InstallValidator | Src/InstallValidator/InstallValidator.csproj |
| Test | InstallValidatorTests | Src/InstallValidator/InstallValidatorTests/InstallValidatorTests.csproj |

### LCMBrowser (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | LCMBrowser | Src/LCMBrowser/LCMBrowser.csproj |

### LexText (22)

| Kind | Project | Path |
| --- | --- | --- |
| Product | Discourse | Src/LexText/Discourse/Discourse.csproj |
| Test | DiscourseTests | Src/LexText/Discourse/DiscourseTests/DiscourseTests.csproj |
| Product | FlexPathwayPlugin | Src/LexText/FlexPathwayPlugin/FlexPathwayPlugin.csproj |
| Test | FlexPathwayPluginTests | Src/LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.csproj |
| Product | ITextDll | Src/LexText/Interlinear/ITextDll.csproj |
| Test | ITextDllTests | Src/LexText/Interlinear/ITextDllTests/ITextDllTests.csproj |
| Product | LexEdDll | Src/LexText/Lexicon/LexEdDll.csproj |
| Test | LexEdDllTests | Src/LexText/Lexicon/LexEdDllTests/LexEdDllTests.csproj |
| Product | LexTextControls | Src/LexText/LexTextControls/LexTextControls.csproj |
| Test | LexTextControlsTests | Src/LexText/LexTextControls/LexTextControlsTests/LexTextControlsTests.csproj |
| Product | LexTextDll | Src/LexText/LexTextDll/LexTextDll.csproj |
| Test | LexTextDllTests | Src/LexText/LexTextDll/LexTextDllTests/LexTextDllTests.csproj |
| Product | MorphologyEditorDll | Src/LexText/Morphology/MorphologyEditorDll.csproj |
| Product | MGA | Src/LexText/Morphology/MGA/MGA.csproj |
| Test | MGATests | Src/LexText/Morphology/MGA/MGATests/MGATests.csproj |
| Test | MorphologyEditorDllTests | Src/LexText/Morphology/MorphologyEditorDllTests/MorphologyEditorDllTests.csproj |
| Product | ParserCore | Src/LexText/ParserCore/ParserCore.csproj |
| Test | ParserCoreTests | Src/LexText/ParserCore/ParserCoreTests/ParserCoreTests.csproj |
| Product | XAmpleManagedWrapper | Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapper.csproj |
| Test | XAmpleManagedWrapperTests | Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/XAmpleManagedWrapperTests.csproj |
| Product | ParserUI | Src/LexText/ParserUI/ParserUI.csproj |
| Test | ParserUITests | Src/LexText/ParserUI/ParserUITests/ParserUITests.csproj |

### ManagedLgIcuCollator (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ManagedLgIcuCollator | Src/ManagedLgIcuCollator/ManagedLgIcuCollator.csproj |
| Test | ManagedLgIcuCollatorTests | Src/ManagedLgIcuCollator/ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.csproj |

### ManagedVwDrawRootBuffered (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ManagedVwDrawRootBuffered | Src/ManagedVwDrawRootBuffered/ManagedVwDrawRootBuffered.csproj |

### ManagedVwWindow (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ManagedVwWindow | Src/ManagedVwWindow/ManagedVwWindow.csproj |
| Test | ManagedVwWindowTests | Src/ManagedVwWindow/ManagedVwWindowTests/ManagedVwWindowTests.csproj |

### MigrateSqlDbs (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | MigrateSqlDbs | Src/MigrateSqlDbs/MigrateSqlDbs.csproj |

### Paratext8Plugin (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | Paratext8Plugin | Src/Paratext8Plugin/Paratext8Plugin.csproj |
| Test | Paratext8PluginTests | Src/Paratext8Plugin/ParaText8PluginTests/Paratext8PluginTests.csproj |

### ParatextImport (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ParatextImport | Src/ParatextImport/ParatextImport.csproj |
| Test | ParatextImportTests | Src/ParatextImport/ParatextImportTests/ParatextImportTests.csproj |

### ProjectUnpacker (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ProjectUnpacker | Src/ProjectUnpacker/ProjectUnpacker.csproj |

### UnicodeCharEditor (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | UnicodeCharEditor | Src/UnicodeCharEditor/UnicodeCharEditor.csproj |
| Test | UnicodeCharEditorTests | Src/UnicodeCharEditor/UnicodeCharEditorTests/UnicodeCharEditorTests.csproj |

### Utilities (10)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ComManifestTestHost | Src/Utilities/ComManifestTestHost/ComManifestTestHost.csproj |
| Product | FixFwData | Src/Utilities/FixFwData/FixFwData.csproj |
| Product | FixFwDataDll | Src/Utilities/FixFwDataDll/FixFwDataDll.csproj |
| Product | MessageBoxExLib | Src/Utilities/MessageBoxExLib/MessageBoxExLib.csproj |
| Test | MessageBoxExLibTests | Src/Utilities/MessageBoxExLib/MessageBoxExLibTests/MessageBoxExLibTests.csproj |
| Product | Reporting | Src/Utilities/Reporting/Reporting.csproj |
| Product | Sfm2Xml | Src/Utilities/SfmToXml/Sfm2Xml.csproj |
| Test | Sfm2XmlTests | Src/Utilities/SfmToXml/Sfm2XmlTests/Sfm2XmlTests.csproj |
| Product | XMLUtils | Src/Utilities/XMLUtils/XMLUtils.csproj |
| Test | XMLUtilsTests | Src/Utilities/XMLUtils/XMLUtilsTests/XMLUtilsTests.csproj |

### XCore (7)

| Kind | Project | Path |
| --- | --- | --- |
| Product | xCore | Src/XCore/xCore.csproj |
| Product | FlexUIAdapter | Src/XCore/FlexUIAdapter/FlexUIAdapter.csproj |
| Product | SilSidePane | Src/XCore/SilSidePane/SilSidePane.csproj |
| Test | SilSidePaneTests | Src/XCore/SilSidePane/SilSidePaneTests/SilSidePaneTests.csproj |
| Product | xCoreInterfaces | Src/XCore/xCoreInterfaces/xCoreInterfaces.csproj |
| Test | xCoreInterfacesTests | Src/XCore/xCoreInterfaces/xCoreInterfacesTests/xCoreInterfacesTests.csproj |
| Test | xCoreTests | Src/XCore/xCoreTests/xCoreTests.csproj |

### xWorks (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | xWorks | Src/xWorks/xWorks.csproj |
| Test | xWorksTests | Src/xWorks/xWorksTests/xWorksTests.csproj |

### Lib/ScrChecks (2)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ScrChecks | Lib/src/ScrChecks/ScrChecks.csproj |
| Test | ScrChecksTests | Lib/src/ScrChecks/ScrChecksTests/ScrChecksTests.csproj |

### Lib/ObjectBrowser (1)

| Kind | Project | Path |
| --- | --- | --- |
| Product | ObjectBrowser | Lib/src/ObjectBrowser/ObjectBrowser.csproj |

## Modernization plan
1. **Add traversal-native aggregate targets.** Teach FieldWorks.proj (or a dedicated .targets file imported by it) to expose llManaged and llManagedNoTests targets that iterate @(ProjectReference) instead of invoking Build/FieldWorks.targets. This preserves the warning/test workflows without leaving the traversal graph.
2. **Update scripts and docs.** Point TEST_WARNINGS_PLAN, build.ps1, and any other callers to the new traversal targets once they exist. Document the change in SDK_MIGRATION.md and TEST_WARNINGS_PLAN.md so that future developers stop referring to FieldWorks.targets.
3. **Retire the compatibility bridge.** After the callers move over, delete the bridge inside FieldWorks.proj, skip generating Build/FieldWorks.targets during normal builds, and remove the bootstrap copy logic from uild.ps1.
4. **Archive or delete FieldWorks.targets generation.** Once no caller consumes it, we can stop running CollectTargets entirely (or leave a developer-only opt-in target) to simplify Setup and reduce bootstrap time.

> Tracking issue placeholder: create specs/legacy-fieldworks-targets.md to guide the cleanup (this document).