# Migration Risk #1: Native Views Rendering Engine P/Invoke Compatibility

**Risk Level**: ⚠️ **CRITICAL**  
**Affected Projects**: `views/` (66.7K lines native C++), `ManagedVwWindow`, `Common/RootSite`, `Common/SimpleRootSite`, `Common/ViewsInterfaces`  
**Estimated Effort**: 4-6 weeks

## Problem Statement

The views rendering engine is a sophisticated 66,700-line native C++ codebase that implements box-based layout, complex writing system support, bidirectional text, and accessible UI. All text display in FieldWorks flows through this engine via P/Invoke. .NET 8 has stricter marshaling rules, changed COM interop patterns, and different GC behavior that threaten this critical boundary.

## Specific Technical Risks

1. **Marshaling changes**: .NET 8 has stricter marshaling rules for P/Invoke. COM interfaces (IVwEnv, IVwGraphics, IVwSelection) must be validated.
2. **COM Interop**: The Views engine uses COM extensively. .NET 8's COMWrappers pattern is fundamentally different from classic RCW (Runtime Callable Wrappers).
3. **Callback delegates**: VwEnv and VwSelection use callbacks from native to managed code. Delegate lifetime and GC behavior changed in .NET Core/.NET 8.
4. **Structure layouts**: Any structs passed across P/Invoke boundaries (VwSelectionInfo, RECT, etc.) must have explicit layouts validated.
5. **Text Services Framework (TSF)**: VwTextStore implements TSF for advanced input. TSF behavior may differ on .NET 8.

## Impact Assessment

**Impact**: **CRITICAL** - Failure here breaks all text display and editing in FieldWorks. This is the foundation of the application's core functionality.

**Affected User Scenarios**:
- All text display and editing
- Complex writing systems (RTL, BiDi, vertical text)
- IME and advanced input methods
- Text selection and navigation
- Accessibility features

## Approach #1: Incremental Validation with Compatibility Layer

**Strategy**: Create a P/Invoke compatibility layer that abstracts the native Views boundary, allowing gradual migration while maintaining .NET Framework compatibility.

**Steps**:
1. **Audit Phase** (Week 1-2):
   - Inventory all P/Invoke declarations in ViewsInterfaces, ManagedVwWindow, RootSite
   - Document all COM interfaces and their usage patterns
   - Identify all delegate callbacks and their lifetimes
   - Map all struct marshaling scenarios

2. **Compatibility Layer** (Week 2-3):
   - Create `Views.Interop.Compat` namespace with abstraction layer
   - Implement dual codepaths: .NET Framework (classic RCW) and .NET 8 (COMWrappers)
   - Use conditional compilation for framework-specific code
   - Create test harness for both frameworks

3. **Migration Phase** (Week 3-5):
   - Update P/Invoke declarations to use LibraryImport source generator
   - Convert COM interfaces to COMWrappers pattern incrementally
   - Add explicit struct layouts with MarshalAs attributes
   - Update delegate pinning and lifetime management

4. **Validation Phase** (Week 5-6):
   - Run comprehensive test suite on both frameworks
   - Test complex writing systems (Hebrew, Arabic, Thai, Chinese)
   - Validate IME scenarios with multiple input methods
   - Performance testing to ensure no regression

**Pros**:
- Maintains backward compatibility during transition
- Lower risk through incremental changes
- Can roll back individual components if issues arise
- Parallel testing on both frameworks

**Cons**:
- Adds temporary complexity with dual codepaths
- Longer timeline due to incremental approach
- Compatibility layer becomes technical debt

**Risk Level**: Medium (mitigated by incremental approach)

---

## Approach #2: Clean Break with Modernized Native Boundary

**Strategy**: Rewrite the managed-to-native boundary using modern .NET 8 patterns, accepting that backward compatibility is broken but gaining long-term maintainability.

**Steps**:
1. **Design Phase** (Week 1):
   - Design new COM interface patterns using COMWrappers
   - Define source-generated P/Invoke declarations
   - Create architecture for delegate marshaling with SafeHandles

2. **Implementation Phase** (Week 1-4):
   - Rewrite ViewsInterfaces with COMWrappers-based interfaces
   - Convert all P/Invoke to LibraryImport source generators
   - Implement proper delegate lifetime management with GCHandle
   - Update struct definitions with explicit StructLayout and field offsets

3. **Bridge Layer** (Week 4-5):
   - Create temporary bridge that translates old API calls to new patterns
   - Update Common/RootSite and Common/SimpleRootSite to use new APIs
   - Update ManagedVwWindow for new interface patterns

4. **Testing and Validation** (Week 5-6):
   - Comprehensive testing of all text scenarios
   - Stress testing delegate callbacks and GC behavior
   - Performance profiling and optimization
   - Cross-platform validation (Windows, Linux if applicable)

**Pros**:
- Modern, maintainable codebase aligned with .NET 8 best practices
- No technical debt from compatibility layers
- Better performance through source-generated P/Invoke
- Cleaner separation of concerns

**Cons**:
- Higher risk due to "big bang" change
- No fallback to .NET Framework code
- Requires more upfront design effort
- Harder to debug if issues arise

**Risk Level**: High (but acceptable for clean migration)

---

## Approach #3: Replace Views Rendering Engine with Modern Alternative

**Strategy**: Given the move to Avalonia (per MIGRATION-PLAN-0.md), consider replacing the native Views engine with Avalonia's text rendering primitives or a modern text layout library.

**Context**: MIGRATION-PLAN-0.md outlines extensive migration to Avalonia for UI. The Views engine's complexity was built for WinForms. Modern cross-platform frameworks like Avalonia have sophisticated text layout already.

**Steps**:
1. **Feasibility Analysis** (Week 1-2):
   - Evaluate Avalonia's TextBlock, TextBox, and FormattedText capabilities
   - Assess if Avalonia + HarfBuzz + platform text APIs can handle:
     - Complex writing systems (BiDi, vertical text)
     - Writing system switching within paragraphs
     - Custom paragraph layout (interlinear text, glosses)
   - Identify gaps requiring custom rendering
   - Cost/benefit analysis vs. Views engine migration

2. **Proof of Concept** (Week 2-4):
   - Build prototype rendering core FieldWorks text scenarios:
     - Multi-writing-system paragraphs
     - Interlinear text with glosses
     - RTL and BiDi text
     - Custom box layouts
   - Performance testing vs. Views engine
   - Accessibility testing

3. **Decision Point** (Week 4):
   - If PoC successful: Plan full replacement alongside Avalonia migration
   - If PoC fails: Fall back to Approach #1 or #2 for Views engine

4. **Implementation** (Week 4-12+ if proceeding):
   - Implement Avalonia-based text rendering layer
   - Create bridge from existing data model to new rendering
   - Migrate incrementally, starting with simple views
   - Keep Views engine as fallback during transition

**Pros**:
- Aligns with long-term Avalonia migration strategy
- Eliminates 66.7K lines of complex native code
- Modern, cross-platform text rendering
- Leverages Avalonia's maintained text infrastructure
- No P/Invoke complexity to maintain

**Cons**:
- Very high risk and effort (could be 3-6 months)
- May not support all FieldWorks' specialized text layout needs
- Large coordination required with UI migration
- Could block .NET 8 migration if it takes too long
- Requires extensive testing and may have subtle rendering differences

**Risk Level**: Very High (but could be transformational)

**Recommendation**: Do feasibility analysis and PoC immediately. If viable, this becomes the long-term strategy, but run in parallel with Approach #1 as backup.

---

## Recommended Strategy

**Primary Path**: **Approach #1** (Incremental Validation with Compatibility Layer)
- Lower risk, proven technique
- Allows .NET 8 migration to proceed while maintaining stability
- Provides safety net for rollback

**Parallel Investigation**: **Approach #3** (Replace with Avalonia)
- Begin feasibility study immediately
- If promising, pivot to this as long-term solution
- If not viable, Approach #1 already in progress

**Fallback**: **Approach #2** (Clean Break)
- Only if Approach #1 proves too complex or performance is unacceptable
- Better for long-term maintenance but higher risk

## Success Criteria

1. All text displays correctly in .NET 8 (visual regression tests pass)
2. Complex writing systems work (Hebrew, Arabic, Thai, Chinese, etc.)
3. IME input works correctly
4. No performance regression (< 5% slower than .NET Framework)
5. Accessibility features work (screen readers, keyboard navigation)
6. All existing tests pass
7. No memory leaks or GC pressure issues with delegates

## Related Documents

- **Src/DOTNET_MIGRATION.md**: Overall migration strategy
- **Src/MIGRATION-PLAN-0.md**: Avalonia migration plan (relevant for Approach #3)
- **Src/views/COPILOT.md**: Views rendering engine documentation
- **Src/Common/ViewsInterfaces/COPILOT.md**: Views interfaces documentation
