# Migration Proposal A: Conservative .NET 8-Only Migration

**Timeline**: 6-9 months  
**Risk Level**: **LOW**  
**Team Size**: 3-4 developers  
**Philosophy**: Migrate to .NET 8 with minimal changes, defer UI modernization

## Executive Summary

This proposal focuses exclusively on migrating FieldWorks to .NET 8 while keeping WinForms UI intact. It minimizes risk by making only necessary changes and deferring the Avalonia migration to a future phase. This approach is appropriate if cross-platform support is not immediately required and the primary goal is to get onto a supported .NET runtime.

## Strategic Approach

### Core Principles
1. **Minimum viable changes** - Only change what's required for .NET 8
2. **Keep WinForms** - Leverage .NET 8's Windows-only WinForms support
3. **Risk mitigation** - Extensive testing at each phase
4. **Incremental delivery** - Small, tested changes
5. **Backward compatibility** - Maintain .NET Framework builds during transition

### Risk Mitigation Strategy

Uses the **safest approach** from each risk analysis:

- **Risk #1 (Native Views P/Invoke)**: Approach #1 - Incremental Validation with Compatibility Layer
- **Risk #2 (WinForms Designer)**: Approach #2 - Bypass Designer with Code-First (for problem areas)
- **Risk #3 (XCore Framework)**: Approach #2 - Minimal Changes with Runtime Reflection
- **Risk #4 (Resources)**: Approach #1 - Regenerate and Validate All Resources
- **Risk #5 (Database)**: Approach #1 - Systematic Validation with Parallel Testing

## Phase-by-Phase Plan

### Phase 0: Foundation (Months 1-2)

**Goals**: Set up .NET 8 environment, validate tooling, create baseline

**Activities**:
1. Set up .NET 8 development environment for all devs
2. Convert 2-3 simple projects as proof-of-concept:
   - `CacheLight` (Very Low complexity)
   - `InstallValidator` (Very Low complexity)
   - `GenerateHCConfig` (Very Low complexity)
3. Establish build infrastructure for .NET 8
4. Create testing infrastructure
5. Document migration patterns

**Deliverables**:
- .NET 8 development environment guide
- 3 migrated projects with tests passing
- Migration playbook
- CI/CD pipeline for .NET 8 builds

**Effort**: 1-2 months (parallel with planning)

---

### Phase 1: Core Libraries (Months 2-4)

**Goals**: Migrate pure managed libraries without UI dependencies

**Risk Mitigations**:
- **Risk #5**: Database layer validation (4 weeks)
- **Risk #4**: Resource regeneration for core libraries (2 weeks)

**Activities**:
1. Migrate pure managed libraries (11 projects):
   - ManagedVwDrawRootBuffered
   - ManagedLgIcuCollator
   - Common/ViewsInterfaces
   - Common/ScriptureUtils
   - LexText/ParserCore
   - Utilities/FixFwData
   - Utilities/SfmStats
   - ProjectUnpacker
   - (and 3 more)

2. **Risk #5 focus**: LCModel data access migration
   - Update to Microsoft.Data.SqlClient
   - Parallel testing .NET Framework vs .NET 8
   - Validate all migration scripts
   - XML serialization testing

3. Create P/Invoke compatibility layer (Risk #1)
4. Test extensively before moving to UI layers

**Deliverables**:
- 11 pure managed libraries on .NET 8
- LCModel validated for .NET 8
- P/Invoke compatibility layer
- Comprehensive test suite passing

**Effort**: 2 months

---

### Phase 2: Views Engine P/Invoke (Months 4-6)

**Goals**: Migrate the critical native Views boundary

**Risk Mitigation**:
- **Risk #1**: Full P/Invoke validation (6 weeks)

**Activities**:
1. Implement P/Invoke compatibility layer (from Risk #1, Approach #1):
   - Audit all P/Invoke declarations
   - Document COM interfaces
   - Create dual codepaths for .NET Framework and .NET 8
   
2. Migrate Views boundary:
   - Update ViewsInterfaces to COMWrappers pattern
   - Convert P/Invoke to LibraryImport source generators
   - Fix delegate marshaling
   - Update struct layouts

3. Extensive testing:
   - Complex writing systems (Hebrew, Arabic, Thai, Chinese)
   - IME scenarios
   - Text selection and editing
   - Performance validation

**Deliverables**:
- Views engine working on .NET 8
- All text display functional
- Complex scripts validated
- Performance acceptable

**Effort**: 2 months

---

### Phase 3: UI Framework and Resources (Months 6-8)

**Goals**: Migrate UI framework components keeping WinForms

**Risk Mitigations**:
- **Risk #2**: Designer issues (6-8 weeks)
- **Risk #4**: Resource infrastructure (4 weeks)
- **Risk #3**: XCore compatibility (4 weeks)

**Activities**:
1. **Risk #4**: Regenerate all resources
   - Regenerate Designer.cs for all .resx files
   - Test localization in all supported languages
   - Validate Crowdin integration
   - Test large resource files

2. **Risk #3**: Update XCore for .NET 8
   - Minimal reflection fixes
   - Update assembly loading
   - Fix configuration system
   - Test plugin discovery

3. **Risk #2**: Address designer issues
   - Audit all custom controls
   - Fix critical designer issues
   - Use code-first for problematic controls
   - Document workarounds

4. Migrate UI libraries:
   - Common/FwUtils
   - Common/Framework
   - Common/Controls
   - Common/RootSite
   - FwCoreDlgs
   - (and remaining UI libraries)

**Deliverables**:
- All UI libraries on .NET 8
- Resources and localization working
- XCore command routing functional
- Designer issues documented with workarounds

**Effort**: 2-3 months

---

### Phase 4: Applications (Months 8-9)

**Goals**: Migrate main application entry points and integration

**Activities**:
1. Migrate application shells:
   - LexTextExe
   - xWorks
   - Other executables

2. End-to-end integration testing
3. Performance testing and optimization
4. User acceptance testing
5. Documentation updates

**Deliverables**:
- Complete FieldWorks application on .NET 8
- All tests passing
- Performance validated
- Documentation complete

**Effort**: 1-2 months

---

### Phase 5: Stabilization (Months 9-10)

**Goals**: Beta testing, bug fixes, production readiness

**Activities**:
1. Internal beta testing (2-4 weeks)
2. External beta program
3. Bug fixing
4. Performance optimization
5. Deployment preparation

**Deliverables**:
- Production-ready .NET 8 build
- Beta feedback addressed
- Deployment documentation
- Training materials

**Effort**: 1-2 months

## Resource Requirements

### Team
- 3-4 developers (at least 2 senior with native interop experience)
- 1 QA engineer (dedicated testing)
- 1 DevOps engineer (part-time for CI/CD)

### Skills Required
- .NET Framework and .NET 8 expertise
- P/Invoke and COM interop
- WinForms development
- C++ knowledge for Views engine
- Database and SQL expertise
- Localization/i18n experience

### Infrastructure
- .NET 8 SDK and tooling
- Updated CI/CD pipelines
- Test automation infrastructure
- Beta testing environment

## Success Criteria

1. ✅ All projects build on .NET 8
2. ✅ All existing tests pass
3. ✅ All text display and editing works
4. ✅ All supported languages work
5. ✅ Database operations validated
6. ✅ No data corruption
7. ✅ Performance within 10% of .NET Framework
8. ✅ No regression in functionality
9. ✅ Windows deployment successful

## Advantages

1. **Low risk** - Conservative approach with extensive testing
2. **Clear path** - Well-defined phases with minimal unknowns
3. **Backward compatible** - Can maintain .NET Framework builds
4. **Focused** - Single goal (get to .NET 8)
5. **Proven patterns** - Uses well-established migration techniques
6. **Windows-only acceptable** - Leverages .NET 8 WinForms support

## Disadvantages

1. **No cross-platform** - Still Windows-only after migration
2. **Designer limitations** - Some WinForms designer issues remain
3. **Technical debt** - Legacy patterns carried forward
4. **Future work needed** - Avalonia migration still required for cross-platform
5. **WinForms is legacy** - Microsoft's focus is on modern UI frameworks

## When to Choose This Proposal

Choose Proposal A if:
- **Primary goal is .NET 8 support** - Not cross-platform
- **Risk aversion** - Need safest path
- **Timeline is flexible** - Can accept 9-10 months
- **Windows-only acceptable** - Don't need Linux/macOS immediately
- **Resource constrained** - Smaller team or budget
- **Stability critical** - Can't afford UI disruption

## Relationship to Other Proposals

- **vs. Proposal B**: Less ambitious, lower risk, no Avalonia
- **vs. Proposal C**: Slower, more conservative, tested at each step

## Next Steps if Approved

1. Allocate team (Weeks 1-2)
2. Set up .NET 8 environment (Weeks 1-2)
3. Begin Phase 0 PoC projects (Weeks 2-4)
4. Establish testing infrastructure (Weeks 2-6)
5. Start Phase 1 core libraries (Month 2)

## References

- **MIGRATION-RISK-1**: Views P/Invoke (uses Approach #1)
- **MIGRATION-RISK-2**: WinForms Designer (uses Approach #2)
- **MIGRATION-RISK-3**: XCore Framework (uses Approach #2)
- **MIGRATION-RISK-4**: Resources (uses Approach #1)
- **MIGRATION-RISK-5**: Database (uses Approach #1)
- **Src/DOTNET_MIGRATION.md**: Overall strategy
