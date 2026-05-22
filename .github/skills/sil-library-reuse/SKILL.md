---
name: sil-library-reuse
description: >
  Reuse lower-level SIL libraries before writing custom helpers in FieldWorks.
  Use this whenever a task touches file or directory I/O, locked-file retries,
  paths or URIs, writing systems, keyboards, LCM caches/project IDs/service
  locators, linked files/media/config paths, localization, LIFT/import/export,
  FLExBridge or Chorus sync/merge flows, analytics/telemetry, morphology or
  NLP, scripture/Paratext integration, or SIL test helpers. Even for small
   fixes, check the package families and repo patterns here first. If any
   guidance here proves inaccurate or stale, revalidate against current
   FieldWorks package references, local package metadata, and upstream repos,
   then update this skill so it stays current.
---

# SIL Library Reuse

FieldWorks already depends on SIL packages that solve many of the "small
helper" problems agents are tempted to rebuild. Start here before adding new
filesystem wrappers, cache bootstraps, writing-system plumbing, localization
hooks, bridge calls, or parser code.

## Staying Current

If you find this to be inaccurate, treat that as a maintenance signal, not a
reason to ignore the skill. SIL package versions, upstream repo layouts, and
public API names change over time. Revalidate the claim against the current
FieldWorks package references, installed package metadata, local usage, and
upstream repos, then update this skill in the same change when practical. If
you cannot update it, call out the stale item clearly in your final response.

## Workflow

1. Identify the package family from the touched namespace or project file. If
   the package or version is unclear, check
   [Directory.Packages.props](../../../Directory.Packages.props) and
   [Build/SilVersions.props](../../../Build/SilVersions.props).
2. Search FieldWorks first for the concrete helper or namespace and copy the
   existing local pattern.
3. If local usage is thin or the name is unfamiliar, open the upstream repo
   links below. Do not assume Context7 covers these packages well.
4. Prefer the library helper over custom code when it already handles retries,
   path normalization, cache setup, localization metadata, sync boundaries, or
   corpus parsing.
5. Keep FieldWorks constraints intact: .NET Framework 4.8, C# 7.3, `.resx` for
   user-facing strings, registration-free COM, and repo build/test scripts.

## Good First Searches

- `RobustIO|RobustFile|RetryUtility|Keyboard.Controller|FileLocationUtilities`
- `LcmCache|TestProjectId|SimpleProjectId|ServiceLocator.GetInstance|LcmFileHelper|WritingSystemManager|FileUtils`
- `LocalizationManagerWinforms|L10NExtender`
- `Analytics.Track|Analytics.ReportException|FLExBridgeHelper`
- `Paratext|IParatextHelper|Usfm|Usx|Morpher|SIL.Machine`

## Package Family Map

### `libpalaso`

Packages:
`SIL.Core`, `SIL.Core.Desktop`, `SIL.Archiving`, `SIL.Lexicon`, `SIL.Lift`,
`SIL.Media`, `SIL.Scripture`, `SIL.TestUtilities`, `SIL.Windows.Forms*`,
`SIL.WritingSystems`

Use for:
filesystem resilience, path helpers, retry loops, desktop file helpers,
writing systems, keyboards, LIFT processing, archiving, and test temp-file
helpers.

FieldWorks usage anchors:

- [Src/Common/RootSite/RootSiteTests/RealDataTestsBase.cs](../../../Src/Common/RootSite/RootSiteTests/RealDataTestsBase.cs)
- [Src/xWorks/FwXWindow.cs](../../../Src/xWorks/FwXWindow.cs)
- [Src/Common/SimpleRootSite/EditingHelper.cs](../../../Src/Common/SimpleRootSite/EditingHelper.cs)

Main APIs to look for first:

- `RobustIO.DeleteDirectoryAndContents`
- `RobustIO.MoveDirectory`
- `RobustIO.RequireThatDirectoryExists`
- `RobustFile.Copy`
- `RobustFile.WriteAllBytes`
- `RobustFile.ReplaceByCopyDelete`
- `RetryUtility.Retry`
- `PathHelper.NormalizePath`
- `PathHelper.AreOnSameVolume`
- `PathHelper.StripFilePrefix`
- `FileLocationUtilities.LocateExecutable`
- `FileLocationUtilities.LocateInProgramFiles`
- `PathUtilities.SelectFileInExplorer`
- `PathUtilities.OpenDirectoryInExplorer`
- `WritingSystemDefinition`
- `IWritingSystemRepository`
- `Keyboard.Controller`
- `KeyboardController`
- `LiftSorter.SortLiftFile`
- `LiftPreparer.MigrateLiftFile`
- `WritingSystemsInLiftFileHelper`
- `ArchivingFileSystem`

Upstream repo:

- [libpalaso](https://github.com/sillsdev/libpalaso)
- [SIL.Core/IO](https://github.com/sillsdev/libpalaso/tree/master/SIL.Core/IO)
- [SIL.Core.Desktop/IO](https://github.com/sillsdev/libpalaso/tree/master/SIL.Core.Desktop/IO)
- [SIL.WritingSystems](https://github.com/sillsdev/libpalaso/tree/master/SIL.WritingSystems)
- [SIL.Windows.Forms.Keyboarding](https://github.com/sillsdev/libpalaso/tree/master/SIL.Windows.Forms.Keyboarding)
- [SIL.Windows.Forms.WritingSystems](https://github.com/sillsdev/libpalaso/tree/master/SIL.Windows.Forms.WritingSystems)
- [SIL.Lift](https://github.com/sillsdev/libpalaso/tree/master/SIL.Lift)
- [SIL.Archiving](https://github.com/sillsdev/libpalaso/tree/master/SIL.Archiving)
- [SIL.TestUtilities](https://github.com/sillsdev/libpalaso/tree/master/SIL.TestUtilities)

### `liblcm`

Packages:
`SIL.LCModel`, `SIL.LCModel.Core`, `SIL.LCModel.Utils`,
`SIL.LCModel.FixData`, `SIL.LCModel.Tests`

Use for:
LCM cache creation, project IDs, service location, writing-system managers,
linked/media/config paths, imports, migrations, and repository/factory access.

FieldWorks usage anchors:

- [Src/Common/RootSite/RootSiteTests/RealDataTestsBase.cs](../../../Src/Common/RootSite/RootSiteTests/RealDataTestsBase.cs)
- [Src/xWorks/FwXWindow.cs](../../../Src/xWorks/FwXWindow.cs)
- [Src/xWorks/xWorksTests/ExportDialogTests.cs](../../../Src/xWorks/xWorksTests/ExportDialogTests.cs)

Main APIs to look for first:

- `LcmCache.CreateCacheFromExistingData`
- `LcmCache.CreateCacheWithNewBlankLangProj`
- `ILcmServiceLocator`
- `ServiceLocator.GetInstance<T>()`
- `IProjectIdentifier`
- `SimpleProjectId`
- `TestProjectId`
- `LcmFileHelper.GetConfigSettingsDir`
- `LcmFileHelper.GetDefaultLinkedFilesDir`
- `LcmFileHelper.GetDefaultMediaDir`
- `LcmFileHelper.GetWritingSystemDir`
- `LinkedFilesRelativePathHelper`
- `FileUtils.FileExists`
- `FileUtils.DirectoryExists`
- `FileUtils.EnsureDirectoryExists`
- `FileUtils.StripFilePrefix`
- `FileUtils.ChangePathToPlatform`
- `WritingSystemManager`
- `UnitOfWorkService`

Upstream repo:

- [liblcm](https://github.com/sillsdev/liblcm)
- [src/SIL.LCModel](https://github.com/sillsdev/liblcm/tree/master/src/SIL.LCModel)
- [Infrastructure](https://github.com/sillsdev/liblcm/tree/master/src/SIL.LCModel/Infrastructure)
- [DomainServices](https://github.com/sillsdev/liblcm/tree/master/src/SIL.LCModel/DomainServices)
- [src/SIL.LCModel.Core](https://github.com/sillsdev/liblcm/tree/master/src/SIL.LCModel.Core)
- [src/SIL.LCModel.Utils](https://github.com/sillsdev/liblcm/tree/master/src/SIL.LCModel.Utils)

### `l10nsharp`

Packages:
`L10NSharp`, `L10NSharp.Windows.Forms`

Use for:
runtime WinForms localization, XLIFF-backed string management, control
metadata, and language switching.

FieldWorks usage anchors:

- [Src/Common/FieldWorks/FieldWorks.cs](../../../Src/Common/FieldWorks/FieldWorks.cs)

Main APIs to look for first:

- `LocalizationManagerWinforms.Create`
- `LocalizationManagerWinforms.SetUILanguage`
- `LocalizationManagerWinforms.ReapplyLocalizationsToAllObjects`
- `L10NExtender`
- `ILocalizableComponent`
- `LocalizingInfoWinforms`
- `LocalizationCategory`
- `XliffLocalizationManagerWinforms`

Upstream repo:

- [l10nsharp](https://github.com/sillsdev/l10nsharp)
- [src/L10NSharp](https://github.com/sillsdev/l10nsharp/tree/master/src/L10NSharp)
- [src/L10NSharp.Windows.Forms](https://github.com/sillsdev/l10nsharp/tree/master/src/L10NSharp.Windows.Forms)
- [UIComponents](https://github.com/sillsdev/l10nsharp/tree/master/src/L10NSharp.Windows.Forms/UIComponents)
- [SampleApp](https://github.com/sillsdev/l10nsharp/tree/master/src/SampleApp)

### `desktopanalytics.net`

Packages:
`SIL.DesktopAnalytics`

Use for:
telemetry, consent-aware tracking, exception reporting, and application-level
analytics properties.

FieldWorks usage anchors:

- [Src/Common/FieldWorks/FieldWorks.cs](../../../Src/Common/FieldWorks/FieldWorks.cs)
- [Src/Common/FwUtils/TrackingHelper.cs](../../../Src/Common/FwUtils/TrackingHelper.cs)
- [Src/LexText/LexTextDll/AreaListener.cs](../../../Src/LexText/LexTextDll/AreaListener.cs)

Main APIs to look for first:

- `Analytics.Track`
- `Analytics.ReportException`
- `Analytics.AllowTracking`
- `Analytics.IdentifyUpdate`
- `Analytics.SetApplicationProperty`
- `Analytics.FlushClient`
- `UserInfo.CreateSanitized`

Upstream repo:

- [DesktopAnalytics.net](https://github.com/sillsdev/DesktopAnalytics.net)
- [Analytics.cs](https://github.com/sillsdev/DesktopAnalytics.net/blob/master/src/DesktopAnalytics/Analytics.cs)
- [UserInfo.cs](https://github.com/sillsdev/DesktopAnalytics.net/blob/master/src/DesktopAnalytics/UserInfo.cs)

### `ipcframework` and `chorus`

Packages:
`SIL.FLExBridge.IPCFramework`, `SIL.Chorus.App`, `SIL.Chorus.LibChorus`,
`SIL.Chorus.L10ns`

Use for:
FLExBridge launch/obtain/send-receive flows, IPC contracts, sync/merge
orchestration, file-type handlers, and merge/conflict notes.

FieldWorks usage anchors:

- [Src/Common/FwUtils/FLExBridgeHelper.cs](../../../Src/Common/FwUtils/FLExBridgeHelper.cs)
- [Src/LexText/Lexicon/FLExBridgeListener.cs](../../../Src/LexText/Lexicon/FLExBridgeListener.cs)
- [Src/Common/Controls/FwControls/ObtainProjectMethod.cs](../../../Src/Common/Controls/FwControls/ObtainProjectMethod.cs)

Start with the local wrapper first:

- `FLExBridgeHelper.LaunchFieldworksBridge`
- `FLExBridgeHelper.DoesProjectHaveFlexRepo`
- `FLExBridgeHelper.DoesProjectHaveLiftRepo`
- `FLExBridgeHelper.IsFlexBridgeInstalled`
- `FLExBridgeHelper.FullFieldWorksBridgePath`

Then inspect upstream helpers if the change crosses the boundary:

- `IPCClientFactory.Create`
- `IPCHostFactory.Create`
- `IIPCClient.RemoteCall`
- `IIPCHost.Initialize`
- `Synchronizer`
- `XmlMergeService.Do3WayMerge`
- `MergeOrder`
- `ElementStrategy`
- `IChorusFileTypeHandler`
- `ChorusNotesMergeEventListener`

Upstream repos:

- [ipcframework](https://github.com/sillsdev/ipcframework)
- [IPCFramework/IPCInterfaces.cs](https://github.com/sillsdev/ipcframework/tree/master/IPCFramework/IPCInterfaces.cs)
- [IPCClientFactory.cs](https://github.com/sillsdev/ipcframework/tree/master/IPCFramework/IPCClientFactory.cs)
- [IPCHostFactory.cs](https://github.com/sillsdev/ipcframework/tree/master/IPCFramework/IPCHostFactory.cs)
- [ServerProgram/FLExBridgeHelper.cs](https://github.com/sillsdev/ipcframework/tree/master/ServerProgram/FLExBridgeHelper.cs)
- [ClientProgram/FLExConnectionHelper.cs](https://github.com/sillsdev/ipcframework/tree/master/ClientProgram/FLExConnectionHelper.cs)
- [chorus](https://github.com/sillsdev/chorus)
- [src/LibChorus/sync](https://github.com/sillsdev/chorus/tree/master/src/LibChorus/sync)
- [src/LibChorus/merge/xml/generic](https://github.com/sillsdev/chorus/tree/master/src/LibChorus/merge/xml/generic)
- [src/LibChorus/FileTypeHandlers](https://github.com/sillsdev/chorus/tree/master/src/LibChorus/FileTypeHandlers)
- [src/LibChorus/VcsDrivers/Mercurial](https://github.com/sillsdev/chorus/tree/master/src/LibChorus/VcsDrivers/Mercurial)
- [src/LibChorus/notes](https://github.com/sillsdev/chorus/tree/master/src/LibChorus/notes)

### `machine`

Packages:
`SIL.Machine`, `SIL.Machine.Morphology.HermitCrab`

Use for:
morphology, feature-structure-based NLP, text corpora, USFM/USX parsing, and
alignment. Direct FieldWorks usage is light today; consult this before
inventing parser or analysis code.

Main APIs to look for first:

- `Morpher.ParseWord`
- `Morpher.GenerateWords`
- `XmlLanguageLoader`
- `TraceManager`
- `FeatureStruct`
- `ParatextTextCorpus`
- `UsxFileTextCorpus`
- `UsfmParser`
- `CorporaUtils`

Upstream repo:

- [machine](https://github.com/sillsdev/machine)
- [src/SIL.Machine](https://github.com/sillsdev/machine/tree/master/src/SIL.Machine)
- [src/SIL.Machine.Morphology.HermitCrab](https://github.com/sillsdev/machine/tree/master/src/SIL.Machine.Morphology.HermitCrab)
- [src/SIL.Machine.Translation.Thot](https://github.com/sillsdev/machine/tree/master/src/SIL.Machine.Translation.Thot)

### `Paratext`

Packages:
`SIL.ParatextShared`, `ParatextData`

Use for:
Scripture project access, verse references, plugin integration, USFM tokens,
and Paratext interoperability.

FieldWorks usage anchors:

- [Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs](../../../Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs)
- `Paratext8Plugin` projects and helpers

Important caveat:
`SIL.ParatextShared` source for the legacy package used by FieldWorks is not
publicly available. Use the public docs and demo plugins instead of hunting
for a missing source tree.

Public docs and references:

- [Paratext demo plugins](https://github.com/ubsicap/paratext_demo_plugins)
- [Full API documentation wiki](https://github.com/ubsicap/paratext_demo_plugins/wiki/Full-API-Documentation)
- [SIL.ParatextShared 7.4.0.1](https://www.nuget.org/packages/SIL.ParatextShared/7.4.0.1)

Useful API names in the public docs:

- `IProject`
- `IReadOnlyProject`
- `IVerseRef`
- `IUSFMToken`
- `IUSFMMarkerToken`
- `IScriptureTextSelection`
- `IBiblicalTerm`
- `IProjectNote`

## Biases To Keep

- Prefer `RobustIO` or `RobustFile` over ad hoc delete/retry loops.
- Prefer `LcmCache`, `TestProjectId`, `ServiceLocator.GetInstance<T>()`, and
  `LcmFileHelper` over custom LCM bootstrapping or path math.
- Prefer `LocalizationManagerWinforms` over ad hoc runtime localization state.
- Prefer `FLExBridgeHelper` over direct IPC calls from new UI code.
- Prefer existing analytics calls or wrappers over one-off telemetry clients.
- When a repo search comes up empty, switch from keyword search to
  symbol/reference navigation before deciding the helper does not exist.