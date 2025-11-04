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

## Top 5 Highest Risks

After detailed analysis of the codebase and research into .NET 8 migration gotchas, these are the five highest-risk areas requiring special attention:

### Risk #1: Native Views Rendering Engine P/Invoke Compatibility ⚠️ **CRITICAL**

**Affected Projects**: `views/` (66.7K lines native C++), `ManagedVwWindow`, `Common/RootSite`, `Common/SimpleRootSite`, `Common/ViewsInterfaces`

**Description**: The views rendering engine is a sophisticated 66,700-line native C++ codebase that implements box-based layout, complex writing system support, bidirectional text, and accessible UI. All text display in FieldWorks flows through this engine via P/Invoke.

**Specific Risks**:
- **Marshaling changes**: .NET 8 has stricter marshaling rules for P/Invoke. COM interfaces (IVwEnv, IVwGraphics, IVwSelection) must be validated.
- **COM Interop**: The Views engine uses COM extensively. .NET 8's COMWrappers pattern is fundamentally different from classic RCW (Runtime Callable Wrappers).
- **Callback delegates**: VwEnv and VwSelection use callbacks from native to managed code. Delegate lifetime and GC behavior changed in .NET Core/.NET 8.
- **Structure layouts**: Any structs passed across P/Invoke boundaries (VwSelectionInfo, RECT, etc.) must have explicit layouts validated.
- **Text Services Framework (TSF)**: VwTextStore implements TSF for advanced input. TSF behavior may differ on .NET 8.

**Impact**: **HIGH** - Failure here breaks all text display and editing in FieldWorks.

**Mitigation Strategy**:
1. Create comprehensive P/Invoke validation test suite
2. Use LibraryImport source generator for new P/Invoke declarations
3. Audit all COM interface definitions and update to COMWrappers pattern
4. Test complex writing systems (RTL, BiDi, vertical text) extensively
5. Validate TSF input scenarios (IME, complex scripts)
6. Consider creating P/Invoke compatibility layer for gradual migration

**Estimated Effort**: 4-6 weeks for full validation and fixes

---

### Risk #2: WinForms Designer and Custom Controls ⚠️ **HIGH**

**Affected Projects**: LexTextDll (high complexity), Common/Controls, Common/RootSite, FdoUi, xWorks, all UI projects (37 total)

**Description**: FieldWorks has extensive custom WinForms controls with design-time support. .NET 8 introduces the out-of-process designer architecture, which breaks many traditional design-time extensibility patterns.

**Specific Risks**:
- **Out-of-process designer**: Custom control designers, type converters, and UI type editors require migration to work with the new architecture.
- **Designer serialization**: Complex property serialization in designer-generated code may fail or produce incorrect code.
- **Data binding engine changes**: .NET 8's new MVVM-oriented binding engine isn't fully compatible with legacy WinForms binding scenarios.
- **DataGridView customization**: Heavily customized DataGridView derivatives (custom cell editors, dynamic columns) may have rendering or editing issues.
- **High DPI behavior**: Designer DPI awareness changed; `<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>` may be needed.
- **Resource file issues**: .resx files with embedded designer code may not deserialize correctly.

**Discovered in Analysis**:
- **LexTextDll** (955 lines in LexTextApp.cs): Uses RestoreDefaultsDlg with designer, ImageHolder resources, and XCore integration that relies on design-time services.
- **Common/RootSite**: CollectorEnv classes bridge managed and native rendering - designer integration fragile.
- **Common/Controls**: Shared UI controls library with XML-based views - custom designers at risk.

**Impact**: **HIGH** - Developer productivity severely impacted if designers don't work. Runtime issues possible for controls that rely on design-time-generated code.

**Mitigation Strategy**:
1. Audit all custom control designers and update for out-of-process architecture
2. Test all designer scenarios in Visual Studio 2022 after migration
3. Consider manual .resx editing for controls that can't be fixed
4. Create design-time test harness for rapid validation
5. Document designer workarounds for team
6. Evaluate third-party control migration costs vs. replacement

**Estimated Effort**: 6-8 weeks for full designer compatibility

---

### Risk #3: XCore Framework and Plugin Architecture ⚠️ **HIGH**

**Affected Projects**: XCore/, LexTextDll, xWorks, FlexUIAdapter, all application layers

**Description**: XCore provides the plugin-based application framework using Mediator pattern, colleague pattern, and extensive use of reflection for command routing. This architecture relies on .NET Framework reflection APIs and dynamic behavior that changed significantly in .NET 8.

**Specific Risks**:
- **Reflection API changes**: XCore uses extensive reflection for command discovery and routing. .NET 8's trim-friendly reflection has different behavior.
- **Assembly loading**: Plugin discovery via assembly scanning may fail with new assembly loading contexts.
- **Configuration system**: App.config → appsettings.json transition affects XCore configuration.
- **Mediator performance**: XCore's reflection-heavy Mediator may perform poorly on .NET 8 without optimization.
- **IxCoreColleague pattern**: Colleague pattern uses interface-based discovery that may break with .NET 8's linker/trimming.

**Discovered in Analysis**:
- **LexTextDll/LexTextApp.cs**: Implements IxCoreColleague, IApp - core integration point at risk.
- **LexTextDll/AreaListener.cs** (1,050 lines): Complex XCore colleague managing list configuration - relies on Mediator heavily.
- **XCore/FlexUIAdapter**: Bridges XCore to UI layer - architectural boundary at risk.

**Impact**: **MEDIUM-HIGH** - Core application framework. Failure breaks application structure, command routing, plugin system.

**Mitigation Strategy**:
1. Create XCore compatibility tests focusing on reflection scenarios
2. Consider source-generated alternatives for hot paths
3. Audit all IxCoreColleague implementations
4. Test plugin discovery and loading thoroughly
5. Validate Mediator performance with profiling
6. Plan incremental XCore refactoring if performance unacceptable

**Estimated Effort**: 4-6 weeks for XCore migration and validation

---

### Risk #4: Resource and Localization Infrastructure ⚠️ **MEDIUM-HIGH**

**Affected Projects**: FwResources, LexTextDll (LexTextStrings.resx, HelpTopicPaths.resx 215KB), all projects with .resx files

**Description**: FieldWorks uses extensive .resx resource files for localization, help topics, and embedded resources. .NET 8 has different resource loading behavior and Crowdin integration must continue working.

**Specific Risks**:
- **Resource file format changes**: .resx files may need regeneration with new ResXResourceWriter.
- **Designer-generated resource classes**: Auto-generated Designer.cs files may not compile or may generate different code.
- **Satellite assembly loading**: Localized resource satellite assemblies load differently in .NET 8.
- **Large resource files**: HelpTopicPaths.resx (215KB) in LexTextDll may have performance implications.
- **Crowdin integration**: crowdin.json configuration must continue working with .NET 8 build process.
- **Runtime resource lookup**: Resource manager behavior changed subtly for fallback handling.

**Impact**: **MEDIUM-HIGH** - Localization broken = unusable for international users. Help system broken = poor user experience.

**Mitigation Strategy**:
1. Regenerate all .resx Designer.cs files with .NET 8 tools
2. Test resource loading in all supported locales
3. Validate Crowdin sync process with .NET 8 build
4. Test large resource file performance (HelpTopicPaths.resx)
5. Audit satellite assembly packaging and deployment
6. Create resource loading test suite

**Estimated Effort**: 3-4 weeks for resource migration and validation

---

### Risk #5: Database and ORM Layer Compatibility ⚠️ **MEDIUM**

**Affected Projects**: All projects depending on LCModel, MigrateSqlDbs, DbExtend

**Description**: FieldWorks uses LCModel for data access, which includes database migrations, XML persistence, and complex object graphs. .NET 8 has changes to System.Data, SQL Client, and serialization that may affect data access.

**Specific Risks**:
- **SQL Server client**: Microsoft.Data.SqlClient behavior differs from System.Data.SqlClient used in .NET Framework.
- **XML serialization**: XmlSerializer behavior changed for edge cases (nullable references, collections).
- **Binary serialization**: BinaryFormatter is obsolete in .NET 8 - if used anywhere, must be replaced.
- **Connection string handling**: Configuration and connection string management changed.
- **Transaction scope**: Distributed transactions behave differently on .NET 8.
- **Migration scripts**: SQL migration scripts must be validated for .NET 8 execution.

**Impact**: **MEDIUM** - Data corruption risk if not handled properly. Migration failures = blocked users.

**Mitigation Strategy**:
1. Audit all database access code for .NET Framework-specific patterns
2. Update to Microsoft.Data.SqlClient with compatibility testing
3. Test all migration scenarios (old → new data format)
4. Validate XML round-tripping of complex objects
5. Check for any BinaryFormatter usage and eliminate
6. Create data integrity test suite
7. Test with real-world project data

**Estimated Effort**: 3-4 weeks for data access validation

---

### Risk Summary Table

| Risk | Impact | Effort | Priority | Dependencies |
|------|--------|--------|----------|--------------|
| 1. Native Views P/Invoke | Critical | 4-6 weeks | 1 | Blocks all UI migration |
| 2. WinForms Designer | High | 6-8 weeks | 2 | Blocks developer workflow |
| 3. XCore Framework | High | 4-6 weeks | 3 | Blocks application structure |
| 4. Resources/Localization | Medium-High | 3-4 weeks | 4 | Blocks internationalization |
| 5. Database/ORM | Medium | 3-4 weeks | 5 | Blocks data migration |

**Total Risk Mitigation Effort**: 20-28 weeks (~5-7 months) of focused work

These risks should be addressed in priority order during the migration phases. Risk #1 (Views P/Invoke) must be resolved in Phase 2-3 before UI components can migrate. Risks #2-3 should be addressed in Phase 4 during UI framework migration.

### Detailed Risk Analysis Documents

Each risk has been analyzed in depth with 2-3 alternative approaches:

- **[MIGRATION-RISK-1-NATIVE-VIEWS-PINVOKE.md](MIGRATION-RISK-1-NATIVE-VIEWS-PINVOKE.md)**: Native Views rendering engine P/Invoke compatibility
  - Approach #1: Incremental validation with compatibility layer (LOW RISK)
  - Approach #2: Clean break with modernized boundary (HIGH RISK)
  - Approach #3: Replace with Avalonia text rendering (VERY HIGH RISK, strategic)

- **[MIGRATION-RISK-2-WINFORMS-DESIGNER.md](MIGRATION-RISK-2-WINFORMS-DESIGNER.md)**: WinForms designer and custom controls
  - Approach #1: Fix designer compatibility issues (MEDIUM-HIGH RISK)
  - Approach #2: Bypass designer with code-first development (MEDIUM RISK)
  - Approach #3: Accelerate Avalonia migration (HIGH RISK, strategic)

- **[MIGRATION-RISK-3-XCORE-FRAMEWORK.md](MIGRATION-RISK-3-XCORE-FRAMEWORK.md)**: XCore framework and plugin architecture
  - Approach #1: Modernize with source generators (MEDIUM RISK)
  - Approach #2: Minimal changes with runtime reflection (LOW-MEDIUM RISK)
  - Approach #3: Replace with MVVM framework (HIGH RISK, aligns with Avalonia)

- **[MIGRATION-RISK-4-RESOURCES-LOCALIZATION.md](MIGRATION-RISK-4-RESOURCES-LOCALIZATION.md)**: Resource and localization infrastructure
  - Approach #1: Regenerate and validate all resources (LOW-MEDIUM RISK)
  - Approach #2: Minimal changes with targeted fixes (MEDIUM RISK)
  - Approach #3: Modernize resource infrastructure, remove Graphite complexity (MEDIUM-HIGH RISK, strategic)

- **[MIGRATION-RISK-5-DATABASE-ORM.md](MIGRATION-RISK-5-DATABASE-ORM.md)**: Database and ORM layer compatibility
  - Approach #1: Systematic validation with parallel testing (LOW RISK, recommended)
  - Approach #2: Minimal changes with focused testing (MEDIUM RISK)
  - Approach #3: Modernize data access layer (VERY HIGH RISK, deferred)

## Migration Proposals

Based on the risk analyses, three comprehensive migration proposals have been developed:

### Proposal A: Conservative .NET 8-Only Migration
**Timeline**: 6-9 months | **Risk**: LOW | **Team**: 3-4 developers

Focuses exclusively on migrating to .NET 8 while keeping WinForms. Uses the lowest-risk approach from each risk analysis. Appropriate when cross-platform is not immediately required.

**[Read Full Proposal A →](MIGRATION-PROPOSAL-A-CONSERVATIVE-DOTNET8.md)**

**Key Characteristics**:
- Windows-only (WinForms on .NET 8)
- Minimum viable changes
- Extensive testing at each phase
- Fastest path to .NET 8 support
- Defers Avalonia migration to future phase

---

### Proposal B: Combined .NET 8 + Avalonia Migration
**Timeline**: 12-18 months | **Risk**: MEDIUM-HIGH | **Team**: 5-7 developers

Combines .NET 8 migration with Avalonia UI migration based on **MIGRATION-PLAN-0.md**. Addresses most risks by moving to modern solutions rather than fixing legacy patterns. Achieves cross-platform support.

**[Read Full Proposal B →](MIGRATION-PROPOSAL-B-COMBINED-AVALONIA.md)**

**Key Characteristics**:
- Cross-platform (Windows, Linux, macOS)
- Implements MIGRATION-PLAN-0.md systematically
- Removes GeckoFX, Graphite, legacy complexity
- One-time migration (don't fix WinForms then migrate again)
- Modern, maintainable codebase for next decade

**References**:
- **[MIGRATION-PLAN-0.md](MIGRATION-PLAN-0.md)**: Detailed Avalonia migration with patch-by-patch backlog

---

### Proposal C: Pragmatic Hybrid Approach (RECOMMENDED)
**Timeline**: 9-15 months | **Risk**: MEDIUM | **Team**: 4-5 developers

Starts conservatively with .NET 8 migration but runs Avalonia pilot in parallel. Includes decision gates to pivot based on validation results. Balances risk with innovation.

**[Read Full Proposal C →](MIGRATION-PROPOSAL-C-PRAGMATIC-HYBRID.md)**

**Key Characteristics**:
- Three-track strategy (primary, pilot, research)
- Decision gates at Months 3, 9, and 12
- Can deliver .NET 8 quickly or expand to full Avalonia
- Builds team Avalonia skills gradually
- Measured approach with validation before commitment
- **Recommended** for balanced risk/reward

---

### Proposal Comparison

| Aspect | Proposal A | Proposal B | Proposal C |
|--------|-----------|-----------|-----------|
| Timeline | 6-9 months | 12-18 months | 9-15 months |
| Team Size | 3-4 devs | 5-7 devs | 4-5 devs |
| Risk Level | LOW | MEDIUM-HIGH | MEDIUM |
| Cross-Platform | No (Windows) | Yes (Win/Lin/Mac) | Conditional |
| WinForms | Keep | Remove | Conditional |
| Avalonia | Future | Full | Pilot/Optional |
| GeckoFX Removed | No | Yes | Conditional |
| Graphite Removed | No | Yes | Conditional |
| Best For | Quick .NET 8, Windows-only | Strategic modernization | Balanced, risk-averse |

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
