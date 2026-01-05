# RhinoMocks to Moq Migration Plan

## Overview

FieldWorks test projects contain legacy RhinoMocks code that needs migration to Moq. RhinoMocks and NMock are abandoned frameworks (last updated ~2009-2014) while Moq is actively maintained.

## Affected Files (6 total)

| File | .Stub() | MockRepository | GetArgumentsForCallsMadeOn | .Expect() | Complexity |
|------|---------|----------------|---------------------------|-----------|------------|
| `Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs` | 17 | 0 | 12 | 0 | 🔴 HIGH |
| `Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs` | 8 | 4 | 0 | 0 | 🟡 MEDIUM |
| `Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs` | 3 | 0 | 0 | 0 | 🟢 LOW |
| `Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs` | 2 | 1 | 0 | 0 | 🟢 LOW |
| `Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs` | 0 | 1 | 0 | 0 | 🟢 LOW |
| `Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs` | 0 | 0 | 0 | 3 | 🟡 MEDIUM |

## Migration Patterns

### Pattern 1: MockRepository.GenerateStub<T>() → new Mock<T>().Object
```csharp
// Before (RhinoMocks)
var mock = MockRepository.GenerateStub<IMyInterface>();

// After (Moq)
var mock = new Mock<IMyInterface>().Object;
```

### Pattern 2: .Stub().Return() → .Setup().Returns()
```csharp
// Before (RhinoMocks)
mock.Stub(x => x.GetValue()).Return(42);

// After (Moq)
mockObj.Setup(x => x.GetValue()).Returns(42);

// NOTE: In Moq, you call .Setup() on the Mock<T>, not on .Object
```

### Pattern 3: Arg<T>.Is.Equal() → direct value or It.Is<T>()
```csharp
// Before (RhinoMocks)
mock.Stub(x => x.Method(Arg<int>.Is.Equal(5))).Return(true);

// After (Moq) - Moq matches exact values by default
mock.Setup(x => x.Method(5)).Returns(true);
```

### Pattern 4: Arg<T>.Is.Anything → It.IsAny<T>()
```csharp
// Before (RhinoMocks)
mock.Stub(x => x.Method(Arg<string>.Is.Anything)).Return(true);

// After (Moq)
mock.Setup(x => x.Method(It.IsAny<string>())).Returns(true);
```

### Pattern 5: .Expect().WhenCalled().Repeat.Once() → .Setup().Callback().Verifiable()
```csharp
// Before (RhinoMocks)
mock.Expect(x => x.Save()).WhenCalled(a => { /* callback */ }).Repeat.Once();
mock.VerifyAllExpectations();

// After (Moq)
mock.Setup(x => x.Save()).Callback(() => { /* callback */ }).Verifiable();
mock.Verify();
```

### Pattern 6: GetArgumentsForCallsMadeOn → Callback capture (COMPLEX)
```csharp
// Before (RhinoMocks)
var args = selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
var capturedProps = (ITsTextProps)args[0][0];

// After (Moq) - requires restructuring
ITsTextProps capturedProps = null;
mockSelection.Setup(s => s.SetTypingProps(It.IsAny<ITsTextProps>()))
    .Callback<ITsTextProps>(p => capturedProps = p);
// ... run code that calls SetTypingProps ...
Assert.That(capturedProps, Is.Not.Null);
```

## Migration Script

A Python script is available at `scripts/migrate_rhinomocks_to_moq.py` that handles:
- ✅ `MockRepository.GenerateStub<T>()` → `new Mock<T>().Object`
- ✅ `Arg<T>.Is.Equal(value)` → `value`
- ✅ `Arg<T>.Is.Anything` → `It.IsAny<T>()`
- ✅ `.VerifyAllExpectations()` → `.VerifyAll()`
- ⚠️ Flags `GetArgumentsForCallsMadeOn` for manual review

**Usage:**
```powershell
# Dry run (preview changes)
python scripts/migrate_rhinomocks_to_moq.py <file_or_directory> --dry-run

# Apply changes
python scripts/migrate_rhinomocks_to_moq.py <file_or_directory>
```

## Implementation Order

### Phase 1: Simple Files (script + minor manual fixes)
1. `ParatextHelperTests.cs` - 1 pattern
2. `ComboHandlerTests.cs` - 3 patterns
3. `InterlinDocForAnalysisTests.cs` - 6 patterns

### Phase 2: Medium Files (more manual work)
4. `RespellingTests.cs` - 12 patterns
5. `FwWritingSystemSetupModelTests.cs` - 9 patterns (`.Expect()` chains)

### Phase 3: Complex Files (structural refactor)
6. `FwEditingHelperTests.cs` - 29 patterns including 12x `GetArgumentsForCallsMadeOn`
   - Requires changing from storing `.Object` to storing `Mock<T>`
   - All `.Stub()` calls need to target the Mock, not the proxy

## Key Challenges

### FwEditingHelperTests.cs Structural Issue

The file has a **hybrid state** where it mixes Moq and RhinoMocks patterns:

```csharp
// Current broken pattern:
var selHelper = SelectionHelper.s_mockedSelectionHelper = new Mock<SelectionHelper>().Object;
selHelper.Stub(x => x.Method());  // ERROR: .Stub() is RhinoMocks, doesn't exist on .Object

// Required fix - store Mock<T> instead:
var selHelperMock = new Mock<SelectionHelper>();
SelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;
selHelperMock.Setup(x => x.Method());  // Works: .Setup() on Mock<T>
```

## Time Estimate

| Phase | Files | Estimated Time |
|-------|-------|----------------|
| Phase 1 | 3 simple files | 1 hour |
| Phase 2 | 2 medium files | 1.5 hours |
| Phase 3 | 1 complex file | 2-3 hours |
| **Total** | **6 files** | **~5 hours** |

## Verification

After migration, run:
```powershell
.\build.ps1 -Configuration Debug
dotnet test Src/<TestProject>/<TestProject>.csproj
```
