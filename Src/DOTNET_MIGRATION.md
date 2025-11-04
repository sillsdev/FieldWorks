# FieldWorks .NET 8 Migration Strategy

## Executive Summary

This document outlines a comprehensive strategy for migrating FieldWorks from .NET Framework 4.6.2 to .NET 8. The migration has been analyzed across **62 components** and organized into phases that minimize interface boundaries between .NET Framework and .NET 8 code.

### Key Findings

- **Native C++ Projects**: 14 projects require no migration, only P/Invoke validation
- **Pure Managed (No UI)**: 11 projects are good early migration candidates
- **WinForms Projects**: 37 projects use WinForms (can migrate to .NET 8 Windows-only)
- **C++/CLI Projects**: 0 projects need significant refactoring (limited .NET 8 support)
- **High Complexity**: 1 projects require extensive work
- **Medium Complexity**: 36 projects require moderate work
- **Low Complexity**: 25 projects require minimal work

### Migration Philosophy

1. **Incremental Approach**: Migrate in phases, starting with low-dependency components
2. **Keep WinForms Initially**: Migrate to .NET 8 keeping WinForms on Windows (defer Avalonia migration)
3. **Minimize Boundaries**: Group related components to reduce .NET Framework/.NET 8 interfaces
4. **Address C++/CLI Last**: C++/CLI has limited .NET 8 support; consider refactoring to P/Invoke
5. **Validate Native Interop**: Ensure P/Invoke signatures work correctly with .NET 8

## Project Categories

### Category 1: Native C++ (No Migration Needed)
**Count**: 14 projects

These projects are pure native C++ and don't need .NET migration. However, P/Invoke signatures in managed code that call these libraries must be validated with .NET 8.

- `AppCore`
- `Cellar`
- `Common`
- `Common/Controls`
- `DbExtend`
- `DebugProcs`
- `DocConvert`
- `FXT`
- `Generic`
- `Kernel`
- `LexText`
- `Transforms`
- `Utilities`
- `views`

**Action**: Keep as-is, validate P/Invoke from .NET 8.

### Category 2: Pure Managed (No UI)
**Count**: 11 projects

These C# projects have no WinForms/WPF UI dependencies and are excellent candidates for early migration.

- `CacheLight` (Complexity: Very Low)
- `Common/ScriptureUtils` (Complexity: Very Low)
- `Common/ViewsInterfaces` (Complexity: Very Low)
- `GenerateHCConfig` (Complexity: Very Low)
- `InstallValidator` (Complexity: Very Low)
- `LexText/ParserCore` (Complexity: Very Low)
- `ManagedLgIcuCollator` (Complexity: Very Low)
- `ManagedVwDrawRootBuffered` (Complexity: Very Low)
- `ProjectUnpacker` (Complexity: Very Low)
- `Utilities/FixFwData` (Complexity: Very Low)
- `Utilities/SfmStats` (Complexity: Very Low)

**Action**: Migrate to .NET 8 SDK-style projects, update dependencies, test thoroughly.

### Category 3: Managed with WinForms
**Count**: 37 projects

These C# projects use WinForms. .NET 8 supports WinForms on Windows, so we can migrate without moving to Avalonia immediately.

- `Common/FieldWorks` (Complexity: Medium)
- `Common/Filters` (Complexity: Medium)
- `Common/Framework` (Complexity: Medium)
- `Common/FwUtils` (Complexity: Medium)
- `Common/RootSite` (Complexity: Medium)
- `Common/SimpleRootSite` (Complexity: Medium)
- `Common/UIAdapterInterfaces` (Complexity: Medium)
- `FdoUi` (Complexity: Medium)
- `FwCoreDlgs` (Complexity: Medium)
- `FwParatextLexiconPlugin` (Complexity: Medium)
- `FwResources` (Complexity: Medium)
- `LCMBrowser` (Complexity: Medium)
- `LexText/Discourse` (Complexity: Medium)
- `LexText/FlexPathwayPlugin` (Complexity: Medium)
- `LexText/Interlinear` (Complexity: Medium)
- `LexText/LexTextControls` (Complexity: Medium)
- `LexText/LexTextDll` (Complexity: High)
- `LexText/LexTextExe` (Complexity: Medium)
- `LexText/Lexicon` (Complexity: Medium)
- `LexText/Morphology` (Complexity: Medium)
- ... and 17 more

**Action**: Migrate to .NET 8 keeping WinForms (Windows-only). Plan separate Avalonia migration later for cross-platform support.

### Category 4: C++/CLI Projects
**Count**: 0 projects

These projects use C++/CLI, which has limited .NET 8 support. Requires significant refactoring.


**Action**: Consider these options:
1. Refactor to use P/Invoke instead of C++/CLI (preferred)
2. Keep on .NET Framework longer while other components migrate
3. Use .NET Core 3.1/5/6 as intermediate step if needed
4. Evaluate cost/benefit of maintaining C++/CLI vs rewriting

## Migration Phases

Based on dependency analysis, we recommend migrating in 5 phases:


### Phase 1: Foundation Layer
**Projects**: 40

**Low Complexity** (start here):
- `CacheLight`
- `Common/Controls`
- `Common/ScriptureUtils`
- `DbExtend`
- `DebugProcs`
- `DocConvert`
- `Generic`
- `InstallValidator`
- `Kernel`
- `LexText`
- ... and 9 more

**Medium Complexity**:
- `Common/FwUtils`
- `Common/UIAdapterInterfaces`
- `FwResources`
- `LexText/FlexPathwayPlugin`
- `LexText/ParserUI`
- `ManagedVwWindow`
- `MigrateSqlDbs`
- `Paratext8Plugin`
- `ParatextImport`
- `UnicodeCharEditor`
- ... and 11 more


### Phase 2: Foundation Layer
**Projects**: 12

**Low Complexity** (start here):
- `AppCore`
- `Cellar`
- `Common`
- `Common/ViewsInterfaces`
- `FXT`
- `GenerateHCConfig`

**Medium Complexity**:
- `FwParatextLexiconPlugin`
- `LCMBrowser`
- `LexText/Discourse`
- `LexText/LexTextControls`
- `LexText/Lexicon`
- `LexText/Morphology`


### Phase 3: Foundation Layer
**Projects**: 5

**Medium Complexity**:
- `Common/Filters`
- `Common/Framework`
- `Common/RootSite`
- `Common/SimpleRootSite`
- `LexText/Interlinear`


### Phase 4: Foundation Layer
**Projects**: 4

**Medium Complexity**:
- `Common/FieldWorks`
- `FdoUi`
- `FwCoreDlgs`

**High Complexity** (address last in phase):
- `LexText/LexTextDll`


### Phase 5: Foundation Layer
**Projects**: 1

**Medium Complexity**:
- `LexText/LexTextExe`


## Recommended Migration Order

### Step 1: Prepare Foundation (Weeks 1-4)
1. Set up .NET 8 development environment
2. Create proof-of-concept migrations for 2-3 simple projects
3. Establish migration patterns and tooling
4. Validate P/Invoke with native C++ libraries
5. Set up CI/CD for .NET 8 builds

### Step 2: Migrate Core Libraries (Weeks 5-12)
1. Start with pure managed libraries (no UI, low dependencies)
2. Update to .NET 8 SDK-style project files
3. Address API compatibility issues
4. Update NuGet packages
5. Run comprehensive tests
6. Keep both .NET Framework and .NET 8 builds initially

**Suggested starting projects**:
- `CacheLight`
- `InstallValidator`
- `GenerateHCConfig`
- `ProjectUnpacker`
- `ManagedVwDrawRootBuffered`
- `ManagedLgIcuCollator`
- `Common/ViewsInterfaces`
- `Common/ScriptureUtils`
- `LexText/ParserCore`
- `Utilities/FixFwData`

### Step 3: Migrate Utility Libraries (Weeks 13-20)
1. Migrate utility and helper libraries
2. Address dependencies on core libraries
3. Update test projects
4. Validate functionality

### Step 4: Migrate UI Framework Components (Weeks 21-32)
1. Migrate UI framework components keeping WinForms
2. Address WinForms API differences in .NET 8
3. Test UI controls and dialogs thoroughly
4. Plan for gradual Avalonia adoption (future phase)

**Note**: WinForms on .NET 8 is Windows-only. Cross-platform requires Avalonia.

### Step 5: Migrate Application Shells (Weeks 33-40)
1. Migrate main application entry points
2. Update application configuration
3. Address COM interop issues with COMWrappers
4. End-to-end testing

### Step 6: Address C++/CLI Components (Weeks 41-52)
1. Evaluate each C++/CLI project
2. Refactor to P/Invoke where possible
3. Consider keeping some components on .NET Framework temporarily
4. Document long-term strategy for remaining C++/CLI

## Critical Challenges

### Challenge 1: WinForms Windows-Only Limitation
**Impact**: High
**Solution**: 
- Accept Windows-only deployment initially
- Plan separate Avalonia migration for cross-platform support
- Investigate WinForms community ports for Linux/macOS if needed urgently

### Challenge 2: C++/CLI Limited Support
**Impact**: High
**Solution**:
- Audit all C++/CLI usage
- Refactor to P/Invoke patterns
- Consider maintaining hybrid .NET Framework/.NET 8 solution temporarily
- Evaluate cost of rewriting in pure managed or pure native code

### Challenge 3: COM Interop Changes
**Impact**: Medium
**Solution**:
- Update to COMWrappers API (replacement for RCW)
- Use source generators for COM interfaces
- Test thoroughly with native COM components

### Challenge 4: Breaking API Changes
**Impact**: Medium
**Solution**:
- Use .NET Upgrade Assistant for initial analysis
- Apply compatibility packages where appropriate
- Update obsolete APIs
- Test extensively

### Challenge 5: Large Codebase Coordination
**Impact**: High
**Solution**:
- Maintain parallel .NET Framework and .NET 8 builds
- Use multi-targeting where appropriate
- Coordinate team efforts across components
- Establish clear migration milestones

## Testing Strategy

1. **Unit Tests**: Migrate alongside each component
2. **Integration Tests**: Run against both .NET Framework and .NET 8
3. **UI Tests**: Extensive manual and automated testing
4. **Performance Tests**: Compare .NET Framework vs .NET 8 performance
5. **Compatibility Tests**: Validate file format compatibility between versions

## Rollout Strategy

### Phase A: Internal Testing (First 6 months)
- Migrate core libraries
- Build proof of concept applications
- Internal dogfooding

### Phase B: Beta Testing (Months 7-10)
- Migrate main applications
- Limited beta release
- Gather feedback

### Phase C: Production Release (Months 11-12)
- Full migration complete
- Production release of .NET 8 version
- Maintain .NET Framework version for stability

## Success Metrics

1. All non-C++/CLI projects migrated to .NET 8
2. All tests passing on .NET 8
3. Performance equal or better than .NET Framework
4. No blocking issues for users
5. Clear path defined for remaining C++/CLI components

## Resources Required

- **Development Time**: 12-18 months for full migration
- **Team Size**: 3-5 developers dedicated to migration
- **Infrastructure**: CI/CD updates, .NET 8 runtime deployment
- **Training**: Team training on .NET 8 features and migration patterns

## Appendix: Individual Project Assessments

Detailed migration assessments are available in each folder:
- `AppCore/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `CacheLight/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Cellar/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Common/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Common/Controls/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Common/FieldWorks/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/Filters/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/Framework/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/FwUtils/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/RootSite/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/ScriptureUtils/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Common/SimpleRootSite/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/UIAdapterInterfaces/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Common/ViewsInterfaces/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `DbExtend/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `DebugProcs/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `DocConvert/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `FXT/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `FdoUi/DOTNET_MIGRATION.md` (Complexity: Medium)
- `FwCoreDlgs/DOTNET_MIGRATION.md` (Complexity: Medium)
- `FwParatextLexiconPlugin/DOTNET_MIGRATION.md` (Complexity: Medium)
- `FwResources/DOTNET_MIGRATION.md` (Complexity: Medium)
- `GenerateHCConfig/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Generic/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `InstallValidator/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Kernel/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `LCMBrowser/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `LexText/Discourse/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/FlexPathwayPlugin/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/Interlinear/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/LexTextControls/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/LexTextDll/DOTNET_MIGRATION.md` (Complexity: High)
- `LexText/LexTextExe/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/Lexicon/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/Morphology/DOTNET_MIGRATION.md` (Complexity: Medium)
- `LexText/ParserCore/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `LexText/ParserUI/DOTNET_MIGRATION.md` (Complexity: Medium)
- `ManagedLgIcuCollator/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `ManagedVwDrawRootBuffered/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `ManagedVwWindow/DOTNET_MIGRATION.md` (Complexity: Medium)
- `MigrateSqlDbs/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Paratext8Plugin/DOTNET_MIGRATION.md` (Complexity: Medium)
- `ParatextImport/DOTNET_MIGRATION.md` (Complexity: Medium)
- `ProjectUnpacker/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Transforms/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `UnicodeCharEditor/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Utilities/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Utilities/FixFwData/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Utilities/FixFwDataDll/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Utilities/MessageBoxExLib/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Utilities/Reporting/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Utilities/SfmStats/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `Utilities/SfmToXml/DOTNET_MIGRATION.md` (Complexity: Medium)
- `Utilities/XMLUtils/DOTNET_MIGRATION.md` (Complexity: Medium)
- `XCore/DOTNET_MIGRATION.md` (Complexity: Medium)
- `XCore/FlexUIAdapter/DOTNET_MIGRATION.md` (Complexity: Medium)
- `XCore/SilSidePane/DOTNET_MIGRATION.md` (Complexity: Medium)
- `XCore/xCoreInterfaces/DOTNET_MIGRATION.md` (Complexity: Medium)
- `XCore/xCoreTests/DOTNET_MIGRATION.md` (Complexity: Medium)
- `views/DOTNET_MIGRATION.md` (Complexity: Very Low)
- `xWorks/DOTNET_MIGRATION.md` (Complexity: Medium)

## Conclusion

The migration to .NET 8 is feasible but requires significant planning and coordination. The recommended approach:

1. **Start with low-hanging fruit**: Pure managed libraries without UI dependencies
2. **Keep WinForms initially**: Migrate to .NET 8 keeping WinForms, defer Avalonia
3. **Address C++/CLI carefully**: These require the most significant refactoring
4. **Maintain parallel builds**: Keep .NET Framework builds during transition
5. **Test extensively**: Comprehensive testing at each phase

**Estimated timeline**: 12-18 months for full migration, with early phases delivering value incrementally.

**Key decision point**: Determine strategy for C++/CLI components early, as this affects the critical path.
