# Migration Risk #3: XCore Framework and Plugin Architecture

**Risk Level**: ⚠️ **HIGH**  
**Affected Projects**: XCore/, LexTextDll, xWorks, FlexUIAdapter, all application layers  
**Estimated Effort**: 4-6 weeks

## Problem Statement

XCore provides the plugin-based application framework using Mediator pattern, colleague pattern, and extensive use of reflection for command routing. This architecture relies on .NET Framework reflection APIs and dynamic behavior that changed significantly in .NET 8. The framework is central to how FieldWorks applications coordinate between components.

## Specific Technical Risks

1. **Reflection API changes**: XCore uses extensive reflection for command discovery and routing. .NET 8's trim-friendly reflection has different behavior.
2. **Assembly loading**: Plugin discovery via assembly scanning may fail with new assembly loading contexts.
3. **Configuration system**: App.config → appsettings.json transition affects XCore configuration.
4. **Mediator performance**: XCore's reflection-heavy Mediator may perform poorly on .NET 8 without optimization.
5. **IxCoreColleague pattern**: Colleague pattern uses interface-based discovery that may break with .NET 8's linker/trimming.
6. **TMInterface menu/toolbar**: Legacy menu/toolbar adapter uses reflection for command routing.

## Affected Components

- **XCore/Mediator**: Core message routing and command dispatch
- **IxCoreColleague implementations**: LexTextApp, AreaListener (1,050 lines), all colleagues
- **TMInterface**: Legacy menu/toolbar adapter
- **FlexUIAdapter**: Bridges XCore to UI layer
- **Command routing infrastructure**: Throughout xWorks, LexText

## Impact Assessment

**Impact**: **MEDIUM-HIGH** - Core application framework. Failure breaks application structure, command routing, plugin system, but doesn't affect data integrity.

**Affected Scenarios**:
- Application startup and initialization
- Menu and toolbar command routing
- Inter-component communication
- Plugin loading and discovery
- Configuration management
- Event-based coordination between features

---

## Approach #1: Modernize XCore with Source Generators

**Strategy**: Replace reflection-heavy patterns with source generators for command discovery and routing while preserving the Mediator/Colleague architecture.

**Steps**:
1. **Analysis and Design** (Week 1):
   - Audit all reflection usage in XCore
   - Map command routing patterns
   - Design source generator for command discovery
   - Plan API compatibility layer

2. **Source Generator Development** (Week 1-3):
   - Create source generator for IxCoreColleague discovery
   - Generate command routing tables at compile time
   - Create Roslyn analyzer to ensure proper attribute usage
   - Build backward-compatible API surface

3. **Incremental Migration** (Week 3-5):
   - Add attributes to existing colleagues (`[XCoreColleague]`, `[XCoreCommand]`)
   - Update Mediator to use generated routing tables
   - Fall back to reflection for unmarked code
   - Migrate one colleague at a time

4. **Performance Optimization** (Week 5-6):
   - Profile command routing performance
   - Optimize hot paths
   - Remove reflection fallbacks once all colleagues migrated
   - Validate memory usage and GC pressure

**Pros**:
- Modern, performant .NET 8 pattern
- Compile-time validation of command routing
- Better performance than reflection
- Maintains existing architecture concepts
- Clear migration path per-component

**Cons**:
- Significant upfront development effort
- Team needs to learn source generators
- Requires recompilation for changes (vs. runtime discovery)
- May be overkill if moving to Avalonia soon

**Risk Level**: Medium (proven technique, but effort-intensive)

---

## Approach #2: Minimal Changes with Runtime Reflection

**Strategy**: Keep existing reflection-based architecture but update for .NET 8 compatibility with minimal changes.

**Steps**:
1. **Compatibility Audit** (Week 1):
   - Test XCore on .NET 8 to identify breaking changes
   - Document reflection API differences
   - Identify assembly loading issues
   - Test plugin discovery mechanisms

2. **Targeted Fixes** (Week 1-3):
   - Update assembly loading to use AssemblyLoadContext
   - Fix reflection API usage for .NET 8 patterns
   - Add explicit type registration where dynamic discovery fails
   - Update configuration loading (App.config → appsettings.json)

3. **Performance Testing** (Week 3-4):
   - Profile Mediator performance on .NET 8
   - Identify performance bottlenecks
   - Add caching for reflection results
   - Optimize hot paths only

4. **Plugin System Update** (Week 4-6):
   - Update plugin discovery for new assembly loading
   - Test with all existing plugins
   - Document any plugin API changes
   - Create compatibility guide for plugin developers

**Pros**:
- Minimal code changes required
- Preserves existing architecture
- Lower risk of introducing bugs
- Faster to implement
- Team familiar with codebase

**Cons**:
- May have performance issues
- Relies on legacy patterns
- Technical debt continues
- May need rework for Avalonia anyway
- Limited compile-time validation

**Risk Level**: Low-Medium (safe but limited)

---

## Approach #3: Replace XCore with Modern MVVM Framework (Align with Avalonia)

**Strategy**: Given the Avalonia migration (MIGRATION-PLAN-0.md), replace XCore command routing with modern MVVM command infrastructure.

**Context**: MIGRATION-PLAN-0.md describes replacing XCore/TMInterface with FluentAvalonia menus and MVVM commands (ReactiveUI or CommunityToolkit.Mvvm) in patch P5.1-P5.3.

**Steps**:
1. **MVVM Framework Selection** (Week 1):
   - Choose between ReactiveUI, CommunityToolkit.Mvvm, or Prism
   - Design command infrastructure aligned with Avalonia
   - Plan migration strategy for existing commands
   - Define ViewModel patterns

2. **Command Infrastructure** (Week 1-3):
   - Implement ICommand-based command system
   - Create command registry and routing
   - Build bridges from old XCore commands to new system
   - Implement dependency injection for ViewModels

3. **Incremental Migration** (Week 3-5):
   - Migrate high-traffic commands first
   - Convert colleagues to ViewModels gradually
   - Maintain XCore compatibility layer for unmigrated code
   - Update menus and toolbars to use new commands

4. **Avalonia Integration** (Week 5-6):
   - Integrate with FluentAvalonia command surfaces
   - Implement MVVM bindings for UI
   - Remove XCore Mediator once all commands migrated
   - Clean up compatibility layer

**Pros**:
- Aligns with Avalonia migration (P5.1-P5.3 from MIGRATION-PLAN-0.md)
- Modern, standard MVVM patterns
- Better tooling and community support
- Cleaner architecture
- Easier testing with MVVM

**Cons**:
- Large architectural change
- Requires Avalonia migration to be worthwhile
- Team learning curve for MVVM
- Risk of breaking existing command routing
- Long migration timeline

**Risk Level**: High (but strategic if doing Avalonia)

**Note**: This approach directly implements MIGRATION-PLAN-0.md patch P5.1: "Introduce FluentAvalonia menus/command surfaces; define MVVM command layer" (6-10 person-days).

---

## Recommended Strategy

**For .NET 8 Migration Only**: **Approach #2** (Minimal Changes)
- If Avalonia migration is >6 months away
- Quick path to .NET 8 functionality
- Lower risk, preserves working system

**For Combined .NET 8 + Avalonia Migration**: **Approach #3** (Replace with MVVM)
- If Avalonia migration starting within 3-6 months
- One-time effort for long-term benefits
- Aligns with MIGRATION-PLAN-0.md strategy

**For Performance-Critical Scenarios**: **Approach #1** (Source Generators)
- If measurements show XCore is performance bottleneck
- If staying on WinForms long-term
- Team has source generator expertise

## Phased Hybrid Approach

Given the uncertainty around Avalonia timeline, recommend:

**Phase 1** (Weeks 1-2): **Approach #2** - Get XCore working on .NET 8
- Minimal fixes for compatibility
- Unblocks .NET 8 migration
- Low risk

**Phase 2** (Weeks 3-4): **Evaluate Decision Point**
- Measure XCore performance on .NET 8
- Assess Avalonia migration timeline
- Decide between Approach #1 or #3

**Phase 3** (Weeks 5-6): **Implement Chosen Direction**
- If Avalonia soon: Start Approach #3
- If performance issue: Start Approach #1
- If neither: Stop with Approach #2 complete

This allows quick progress while preserving strategic options.

## Success Criteria

1. All commands route correctly on .NET 8
2. Plugin system loads and discovers plugins
3. Menu and toolbar commands work
4. Mediator performance acceptable (< 10% regression)
5. Configuration system works
6. No memory leaks or excessive GC pressure
7. Existing colleagues work without code changes (or minimal changes)

## Comparison with MIGRATION-PLAN-0.md

The Avalonia migration plan (P5.1-P5.3) estimates:
- P5.1: FluentAvalonia menus/MVVM layer: 6-10 person-days
- P5.2: Replace TMInterface: 8-12 person-days  
- P5.3: Contextual menus: 4-6 person-days
- **Total: 18-28 person-days (3.6-5.6 weeks)**

This aligns well with **Approach #3** timeline, suggesting that approach is realistic and well-scoped.

## Related Documents

- **Src/DOTNET_MIGRATION.md**: Overall migration strategy
- **Src/MIGRATION-PLAN-0.md**: Avalonia migration with command system replacement (P5.1-P5.3)
- **Src/XCore/COPILOT.md**: XCore framework documentation
- **Src/LexText/LexTextDll/COPILOT.md**: LexTextApp and AreaListener (major XCore consumers)
