# Migration Proposal B: Combined .NET 8 + Avalonia Migration

**Timeline**: 12-18 months  
**Risk Level**: **MEDIUM-HIGH**  
**Team Size**: 5-7 developers  
**Philosophy**: Migrate to .NET 8 while simultaneously moving to Avalonia for cross-platform support

## Executive Summary

This proposal combines .NET 8 migration with Avalonia UI migration based on the detailed plan in **MIGRATION-PLAN-0.md**. It recognizes that many risks (designer issues, XCore framework, custom controls) need to be addressed anyway, so we should solve them once by moving to Avalonia rather than fixing for WinForms then migrating again. This achieves cross-platform support while modernizing the codebase.

## Strategic Approach

### Core Principles
1. **One-time migration** - Fix problems once with modern solutions
2. **Cross-platform first** - Target Windows, Linux, macOS from start
3. **Incremental delivery** - Migrate screen-by-screen (per MIGRATION-PLAN-0.md)
4. **Modern patterns** - MVVM, reactive programming, Avalonia best practices
5. **Remove legacy complexity** - Eliminate Graphite, GeckoFX, old patterns

### Risk Mitigation Strategy

Uses **modern replacement** approaches:

- **Risk #1 (Views P/Invoke)**: Approach #3 - Replace with Avalonia text rendering (parallel with Approach #1 as backup)
- **Risk #2 (WinForms Designer)**: Approach #3 - Accelerate Avalonia Migration
- **Risk #3 (XCore)**: Approach #3 - Replace with MVVM (per MIGRATION-PLAN-0.md P5.1-P5.3)
- **Risk #4 (Resources)**: Approach #3 - Modernize (remove Graphite resources)
- **Risk #5 (Database)**: Approach #1 - Systematic Validation (same as Proposal A)

### Alignment with MIGRATION-PLAN-0.md

This proposal implements the detailed Avalonia migration plan:
- **Phase 0**: P0.1-P0.2 Avalonia foundations
- **Bucket 1**: P1.1-P1.4 Docking/workspace
- **Bucket 2**: P2.1-P2.6 Grids and hierarchical data  
- **Bucket 3**: P3.1-P3.4 Property editors
- **Bucket 4**: P4.1-P4.4 Embedded web panes (remove GeckoFX)
- **Bucket 5**: P5.1-P5.3 Menus, toolbars, commands

## Phase-by-Phase Plan

### Phase 0: Foundation (Months 1-3)

**Goals**: Set up both .NET 8 and Avalonia infrastructure

**Activities**:
1. **.NET 8 Setup** (Month 1):
   - Development environment
   - PoC with 3 simple projects (CacheLight, InstallValidator, GenerateHCConfig)
   - Build infrastructure

2. **Avalonia Foundation** (Month 2-3, from MIGRATION-PLAN-0.md P0):
   - **P0.1**: Create Avalonia solution, app host, theme (3-5 PD)
   - **P0.2**: Choose libraries (Dock.Avalonia, FluentAvalonia, DataGrid, PropertyGrid, WebView) (1-2 PD)
   - Set up MVVM framework (ReactiveUI or CommunityToolkit.Mvvm)
   - Configure DI, logging, theming

3. **Risk #5 - Database** (Month 2-3):
   - Same as Proposal A: LCModel validation
   - This is parallel to UI work, doesn't depend on it

4. **Views Engine Investigation** (Month 3):
   - **Risk #1, Approach #3**: Feasibility study for replacing Views with Avalonia
   - Build PoC for complex text scenarios
   - Decision point: continue or fall back to P/Invoke migration

**Deliverables**:
- .NET 8 environment working
- Avalonia application shell running
- LCModel validated for .NET 8
- Views replacement feasibility determined

**Effort**: 3 months (with parallel workstreams)

---

### Phase 1: Core Libraries + App Shell (Months 3-6)

**Goals**: Migrate core libraries and create Avalonia shell

**Activities**:
1. **Core Libraries** (same as Proposal A):
   - 11 pure managed libraries to .NET 8
   
2. **Docking/Workspace** (MIGRATION-PLAN-0.md Bucket 1):
   - **P1.1**: Avalonia shell with Dock.Avalonia (5-8 PD)
   - **P1.2**: Port ObjectBrowser host (4-6 PD)
   - **P1.3**: Port LCMBrowser ModelWnd (4-6 PD)
   - **P1.4**: Remove WinForms docking (2-3 PD)
   - **Total**: ~15-23 PD (3-4.5 weeks)

3. **Views Decision Implementation**:
   - If replacing Views: Begin Avalonia text rendering
   - If keeping Views: Implement P/Invoke compatibility layer (Risk #1, Approach #1)

**Deliverables**:
- Core libraries on .NET 8
- Avalonia shell with docking
- 2 windows migrated to Avalonia
- Views path determined and started

**Effort**: 3 months

---

### Phase 2: Grids and Data Views (Months 6-9)

**Goals**: Migrate grid-heavy screens, eliminate DataGridView wrappers

**Activities**:
1. **Grids** (MIGRATION-PLAN-0.md Bucket 2):
   - **P2.1**: Avalonia DataGrid patterns, TsString cell template (5-8 PD)
   - **P2.2**: WebonaryLogViewer (3-5 PD)
   - **P2.3**: InspectorGrid (6-9 PD)
   - **P2.4**: CharContext grids (4-6 PD)
   - **P2.5**: BrowseViewer (8-12 PD)
   - **P2.6**: Retire WinForms DataGridView wrappers (2-3 PD)
   - **Total**: ~28-43 PD (5.5-8.5 weeks)

2. **Resource Modernization** (Risk #4, Approach #3):
   - Remove Graphite-related resources
   - Modernize resource loading
   - Optimize large resource files

**Deliverables**:
- Major grids on Avalonia
- DataGridView wrappers removed
- Resources modernized
- Graphite resources eliminated

**Effort**: 3 months

---

### Phase 3: Property Editors + Web Panes (Months 9-12)

**Goals**: Migrate complex dialogs and remove GeckoFX

**Activities**:
1. **Property Editors** (MIGRATION-PLAN-0.md Bucket 3):
   - **P3.1**: Avalonia.PropertyGrid foundation (4-6 PD)
   - **P3.2**: InspectorWnd property panel (5-8 PD)
   - **P3.3**: Styles dialog (10-15 PD)
   - **P3.4**: Collection editors (3-5 PD)
   - **Total**: ~22-34 PD (4.5-7 weeks)

2. **Web Panes** (MIGRATION-PLAN-0.md Bucket 4):
   - **P4.1**: Avalonia.WebView foundation (5-8 PD)
   - **P4.2**: XhtmlDocView, XhtmlRecordDocView (8-12 PD)
   - **P4.3**: Dialog/Preview web panes (6-10 PD)
   - **P4.4**: **Remove GeckoFX/XULRunner** (2-3 PD) - User's goal!
   - **Total**: ~21-33 PD (4-6.5 weeks)

**Deliverables**:
- Complex dialogs on Avalonia
- GeckoFX completely removed
- Web views working with modern WebView2
- Property editing modernized

**Effort**: 3 months

---

### Phase 4: Commands + Remaining Screens (Months 12-15)

**Goals**: Complete migration, remove XCore

**Activities**:
1. **Menus/Commands** (MIGRATION-PLAN-0.md Bucket 5):
   - **P5.1**: FluentAvalonia menus, MVVM commands (6-10 PD)
   - **P5.2**: Replace TMInterface (8-12 PD)
   - **P5.3**: Contextual menus (4-6 PD)
   - **Total**: ~18-28 PD (3.5-5.5 weeks)
   - **Implements Risk #3, Approach #3**

2. Migrate remaining screens
3. Remove WinForms completely
4. Remove XCore framework

**Deliverables**:
- All screens on Avalonia
- MVVM command infrastructure
- XCore removed
- WinForms removed

**Effort**: 3 months

---

### Phase 5: Stabilization + Cross-Platform (Months 15-18)

**Goals**: Polish, test on all platforms, production release

**Activities**:
1. Cross-platform testing (Windows, Linux, macOS)
2. Performance optimization
3. Accessibility implementation
4. Beta testing program
5. Documentation and training

**Deliverables**:
- Production release for Windows
- Linux/macOS beta releases
- Performance acceptable on all platforms
- Documentation complete

**Effort**: 3 months

## Workstream Coordination

**Parallel Workstreams** (to achieve 12-18 month timeline):

### Workstream A: Core & Data (2 devs)
- Months 1-6: Core libraries and database
- Months 7-18: Support other workstreams with data integration

### Workstream B: Views & Rendering (2 devs)
- Months 1-3: Views feasibility study
- Months 4-18: Either Views P/Invoke or Avalonia text rendering

### Workstream C: UI Migration (3-4 devs)
- Months 1-3: Avalonia foundation
- Months 4-6: Shell and docking
- Months 7-9: Grids
- Months 10-12: Dialogs and web
- Months 13-15: Commands and completion
- Months 16-18: Polish

## Resource Requirements

### Team
- **5-7 developers**:
  - 2 senior with native interop experience (Workstream A+B)
  - 2-3 with Avalonia/XAML experience (Workstream C)
  - 1-2 full-stack (flexible between workstreams)
- **1-2 QA engineers** (dedicated testing)
- **1 DevOps engineer** (CI/CD for multiple platforms)
- **1 UX designer** (part-time, Avalonia UI consistency)

### Skills Required
- .NET 8 and .NET Framework
- Avalonia and XAML
- MVVM patterns (ReactiveUI or CommunityToolkit.Mvvm)
- P/Invoke and native interop
- C++ (for Views engine work)
- Cross-platform development
- Database and ORM
- Localization

## Success Criteria

1. ✅ All functionality on .NET 8
2. ✅ All UI on Avalonia
3. ✅ **Cross-platform**: Windows, Linux, macOS
4. ✅ GeckoFX removed
5. ✅ Graphite removed (if applicable)
6. ✅ WinForms removed
7. ✅ XCore removed (replaced with MVVM)
8. ✅ Modern, maintainable codebase
9. ✅ Performance acceptable on all platforms
10. ✅ All tests passing

## Advantages

1. **Cross-platform** - Windows, Linux, macOS support
2. **Modern UI** - Avalonia is actively maintained, modern patterns
3. **One migration** - Don't fix WinForms then migrate again
4. **Remove legacy** - GeckoFX, Graphite, XCore, custom controls gone
5. **Strategic** - Positions FieldWorks for next decade
6. **Better maintainability** - Modern patterns, MVVM, cleaner code
7. **Addresses user's goals** - Removes 15-year-old complexity

## Disadvantages

1. **Higher risk** - More moving parts
2. **Longer timeline** - 12-18 months vs. 6-9 months
3. **Larger team needed** - 5-7 developers
4. **Learning curve** - Team must learn Avalonia
5. **More coordination** - Multiple parallel workstreams
6. **User disruption** - UI changes may require user retraining

## When to Choose This Proposal

Choose Proposal B if:
- **Cross-platform is priority** - Need Linux/macOS support
- **Long-term vision** - Want modern, maintainable codebase
- **Team capacity** - Have 5-7 developers available
- **User's goals align** - Remove old complexity (GeckoFX, Graphite)
- **Timeline acceptable** - Can invest 12-18 months
- **Strategic decision** - Want to position for future
- **One migration preferred** - Don't want to migrate UI twice

## Relationship to Other Proposals

- **vs. Proposal A**: More ambitious, longer, but strategic
- **vs. Proposal C**: More structured, follows MIGRATION-PLAN-0.md

## Critical Success Factors

1. **Executive commitment** - 12-18 month timeline requires sustained support
2. **Team skill mix** - Need Avalonia expertise early
3. **Parallel workstreams** - Requires good coordination
4. **User communication** - UI changes need preparation
5. **Incremental delivery** - Must show progress regularly

## References

- **MIGRATION-PLAN-0.md**: Detailed Avalonia migration plan (implements this extensively)
- **MIGRATION-RISK-1**: Views engine (uses Approach #3 or #1)
- **MIGRATION-RISK-2**: WinForms designer (uses Approach #3)
- **MIGRATION-RISK-3**: XCore (uses Approach #3, implements P5.1-P5.3)
- **MIGRATION-RISK-4**: Resources (uses Approach #3, removes Graphite)
- **MIGRATION-RISK-5**: Database (uses Approach #1)
- **Src/DOTNET_MIGRATION.md**: Overall strategy
