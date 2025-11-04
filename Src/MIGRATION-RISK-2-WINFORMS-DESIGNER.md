# Migration Risk #2: WinForms Designer and Custom Controls

**Risk Level**: ⚠️ **HIGH**  
**Affected Projects**: LexTextDll (high complexity), Common/Controls, Common/RootSite, FdoUi, xWorks, all UI projects (37 total)  
**Estimated Effort**: 6-8 weeks

## Problem Statement

FieldWorks has extensive custom WinForms controls with design-time support. .NET 8 introduces the out-of-process designer architecture, which breaks many traditional design-time extensibility patterns. The designer is critical for developer productivity and for controls that rely on design-time-generated code.

## Specific Technical Risks

1. **Out-of-process designer**: Custom control designers, type converters, and UI type editors require migration to work with the new architecture.
2. **Designer serialization**: Complex property serialization in designer-generated code may fail or produce incorrect code.
3. **Data binding engine changes**: .NET 8's new MVVM-oriented binding engine isn't fully compatible with legacy WinForms binding scenarios.
4. **DataGridView customization**: Heavily customized DataGridView derivatives (custom cell editors, dynamic columns) may have rendering or editing issues.
5. **High DPI behavior**: Designer DPI awareness changed; `<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>` may be needed.
6. **Resource file issues**: .resx files with embedded designer code may not deserialize correctly.

## Affected Components

- **LexTextDll** (955 lines in LexTextApp.cs): RestoreDefaultsDlg with designer, ImageHolder resources
- **Common/RootSite**: CollectorEnv classes bridge managed and native rendering
- **Common/Controls**: Shared UI controls library with XML-based views, custom designers
- **FwCoreDlgs**: Custom dialogs with complex designer code
- **DataGridView derivatives**: FwTextBoxControl, FwTextBoxColumn, SilButtonCell (from MIGRATION-PLAN-0.md)

## Impact Assessment

**Impact**: **HIGH** - Developer productivity severely impacted if designers don't work. Runtime issues possible for controls that rely on design-time-generated code.

**Affected Scenarios**:
- Visual Studio designer experience for all WinForms forms
- Custom control property editing in designer
- Data binding configuration
- Resource management and localization
- Complex grid layouts and custom cell types

---

## Approach #1: Fix Designer Compatibility Issues

**Strategy**: Update all custom controls, designers, and type converters to work with .NET 8's out-of-process designer while keeping WinForms.

**Steps**:
1. **Audit Phase** (Week 1-2):
   - Inventory all custom control designers in Common/Controls
   - List all TypeConverters and UITypeEditors
   - Document all design-time attributes and services used
   - Test each control in .NET 8 designer to identify failures

2. **Fix Custom Designers** (Week 2-4):
   - Update ControlDesigner implementations for out-of-process architecture
   - Rewrite TypeConverters to avoid design-time-only APIs
   - Update UITypeEditors to work in separate process
   - Add `<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>` if needed

3. **Fix Designer Serialization** (Week 4-5):
   - Update complex property serialization code
   - Fix .resx resource deserialization issues
   - Test designer code generation for all controls
   - Manual fixes for controls that can't be auto-fixed

4. **Data Binding Migration** (Week 5-6):
   - Audit all data binding scenarios
   - Update to use IBindableComponent where needed
   - Test with new binding engine
   - Document workarounds for incompatible scenarios

5. **Validation** (Week 6-8):
   - Test all 37 WinForms projects in designer
   - Create design-time test harness
   - Document known issues and workarounds
   - Train team on designer quirks

**Pros**:
- Maintains WinForms development workflow
- Incremental fixes can be done per-control
- No runtime changes required
- Developers can continue using familiar tools

**Cons**:
- May not fix all designer issues (some may be unfixable)
- Significant effort for each custom control
- Technical debt as WinForms is legacy
- Will need to redo work when migrating to Avalonia

**Risk Level**: Medium-High (some issues may be unfixable)

---

## Approach #2: Bypass Designer with Code-First Development

**Strategy**: Accept that designer may not work perfectly and shift to code-first development for problematic controls.

**Steps**:
1. **Categorize Controls** (Week 1):
   - Identify controls where designer works in .NET 8
   - Identify controls where designer fails
   - Assess impact of designer failure for each control

2. **Code-First Patterns** (Week 1-3):
   - Create code-first initialization patterns for complex controls
   - Move designer-generated code to manual initialization
   - Create helper methods and factories for common patterns
   - Document migration path from designer to code-first

3. **Selective Designer Use** (Week 3-5):
   - Keep designer for simple controls and layouts
   - Use code-first for complex custom controls
   - Create templates and examples for common scenarios
   - Update team documentation and training

4. **Tooling and Productivity** (Week 5-7):
   - Create code snippets for common control setups
   - Build preview tool for forms without designer
   - Create UI testing helpers for manual testing
   - Optimize developer workflow

5. **Migration** (Week 7-8):
   - Migrate problem forms to code-first approach
   - Update project documentation
   - Train team on new workflow

**Pros**:
- Avoids fighting against designer limitations
- More maintainable code without designer cruft
- Better for version control (no designer .resx changes)
- Easier to test programmatically
- Works for both .NET 8 WinForms and future Avalonia

**Cons**:
- Loss of visual design-time experience
- Steeper learning curve for developers
- Slower initial development for new forms
- May need custom tooling for productivity

**Risk Level**: Medium (developer productivity impact)

---

## Approach #3: Accelerate Avalonia Migration (from MIGRATION-PLAN-0.md)

**Strategy**: Given that WinForms is legacy and Avalonia migration is planned, accelerate the Avalonia migration to avoid investing in WinForms designer fixes.

**Context**: MIGRATION-PLAN-0.md provides detailed migration plan to Avalonia with specific patches:
- P1.1: Avalonia app shell with Dock.Avalonia
- P2.1: Avalonia DataGrid patterns
- P3.1: Avalonia.PropertyGrid
- Systematic replacement of WinForms screens

**Steps**:
1. **Prioritization** (Week 1):
   - Review MIGRATION-PLAN-0.md patches
   - Identify highest-impact screens to migrate first
   - Focus on screens with worst designer issues
   - Defer screens where designer works OK

2. **Accelerated Avalonia Setup** (Week 1-2):
   - Implement P0.1-P0.2 from MIGRATION-PLAN-0.md (Avalonia foundation)
   - Set up Avalonia project structure
   - Configure MVVM framework
   - Create base styles and themes

3. **Priority Screen Migration** (Week 2-6):
   - Migrate screens with worst designer problems first
   - Follow patch sequence from MIGRATION-PLAN-0.md:
     - Start with simple grids (P2.2: WebonaryLogViewer)
     - Move to inspectors (P3.2: InspectorWnd)
     - Migrate dialogs with complex designers
   - Use Avalonia's XAML designer (works better than WinForms)

4. **Hybrid Coexistence** (Week 6-8):
   - Keep working WinForms screens on .NET 8
   - Migrate problematic screens to Avalonia
   - Create interop layer for WinForms ↔ Avalonia communication
   - Document which screens are in which framework

**Pros**:
- Solves designer problems permanently
- Aligns with long-term strategy
- Modern development experience with Avalonia XAML
- Cross-platform benefits
- Better designer experience than .NET 8 WinForms

**Cons**:
- Higher upfront effort (but was planned anyway)
- Team learning curve for Avalonia
- Hybrid maintenance during transition
- May delay .NET 8 migration timeline
- Requires coordination across many screens

**Risk Level**: High (but strategic)

**Note**: This approach references MIGRATION-PLAN-0.md extensively. The patch-by-patch backlog provides clear guidance for systematic migration.

---

## Recommended Strategy

**Primary Path**: **Approach #2** (Bypass Designer with Code-First)
- Quick wins for problem areas
- Maintains .NET 8 WinForms migration timeline
- Skills transfer to Avalonia (code-first mindset)
- Lower risk than trying to fix unfixable designer issues

**Parallel Investigation**: **Approach #3** (Accelerate Avalonia)
- Begin P0 foundation work from MIGRATION-PLAN-0.md
- Migrate 2-3 problematic screens as PoC
- Assess if team can handle parallel migration
- If successful, expand scope

**Fallback**: **Approach #1** (Fix Designer)
- Only for critical controls where designer is essential
- Focus effort on most-used controls
- Accept that some controls won't work

## Hybrid Recommendation

1. **Weeks 1-2**: Audit and categorize (all approaches need this)
2. **Weeks 3-4**: Fix critical simple controls (Approach #1)
3. **Weeks 3-4**: Start Avalonia foundation (Approach #3)
4. **Weeks 5-6**: Code-first for complex controls (Approach #2)
5. **Weeks 7-8**: Migrate 1-2 screens to Avalonia PoC (Approach #3)

This hybrid allows quick progress on .NET 8 while setting up long-term success.

## Success Criteria

1. All forms can be edited (via designer or code)
2. No runtime errors from designer-generated code
3. Developer productivity acceptable (measured by team feedback)
4. Clear documentation for code-first approach
5. 80%+ of controls work in designer OR have code-first alternative
6. Path forward clear for remaining issues

## Related Documents

- **Src/DOTNET_MIGRATION.md**: Overall migration strategy
- **Src/MIGRATION-PLAN-0.md**: Avalonia migration plan (critical for Approach #3)
- **Src/Common/Controls/COPILOT.md**: Custom controls documentation
- **Src/LexText/LexTextDll/COPILOT.md**: High-complexity project affected
