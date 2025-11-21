---
last-reviewed: 2025-10-31
last-reviewed-tree: 93bb87ed6933f01166c6f42fd1084b9fa82b40f80f614eda2f53a3c1c4cacaf3
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/DebugProcs. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# DebugProcs COPILOT summary

## Purpose
Developer diagnostics and debugging utilities for troubleshooting FieldWorks native C++ code issues. Provides customizable assertion handling (DefAssertProc, SetAssertProc), warning system (WarnProc, DefWarnProc), debug report hooks (_DBG_REPORT_HOOK), and message box control for assertions. Enables developer-friendly debugging with configurable assertion behavior, debug output redirection, and controlled warning/error reporting. Critical infrastructure for diagnosing issues during development and testing.

## Architecture
C++ native DLL (DebugProcs.dll) with debug utility functions (660 lines total). Single-file implementation (DebugProcs.cpp) with header (DebugProcs.h). Provides init/exit lifecycle (DebugProcsInit, DebugProcsExit), assertion customization hooks, and debug output facilities. Cross-platform support (Windows and Linux/Unix via conditional compilation).

## Key Components
- **DefAssertProc** function: Default assertion handler
  - Calls SilAssert for assertion handling
  - Outputs assertion info to debug output
- **DefWarnProc** function: Default warning handler
  - Formats warning message with file, line, module
  - Outputs to debug output via OutputDebugString
- **SetAssertProc** function: Customize assertion handling
  - Takes Pfn_Assert function pointer
  - Returns previous assertion handler
  - Enables custom assertion behavior
- **WarnProc** function: Warning entry point
  - Checks g_crefWarnings counter (enable/disable warnings)
  - Calls configured warning handler (g_pfnWarn)
  - fCritical flag bypasses warning suppression
- **SilAssert** function: Core assertion implementation
  - Handles assertion display and debugging break
- **ShowAssertMessageBox** function: Control message box display
  - Enable/disable assertion message boxes
  - Sets g_fShowMessageBox flag
- **DbgSetReportHook** function: Set debug report hook
  - Redirects debug output to custom handler
  - Returns previous hook
- **DebugProcsInit** function: Initialize debug subsystem
  - Lifecycle management
- **DebugProcsExit** function: Cleanup debug subsystem
  - Lifecycle management
- **Global state**:
  - g_pfnAssert: Current assertion handler
  - g_pfnWarn: Current warning handler
  - g_crefWarnings: Warning enable/disable counter
  - g_crefAsserts: Assert enable/disable counter
  - g_crefMemory: Memory tracking counter
  - g_fShowMessageBox: Message box enable flag
  - g_ReportHook: Custom report hook

## Technology Stack
- C++ native code
- Windows API (OutputDebugString, GetModuleFileName, MessageBox)
- Linux/Unix support via conditional compilation (COM.h, Hacks.h)
- APIENTRY, WINAPI calling conventions
- DLL exports (__declspec(dllexport))

## Dependencies

### Upstream (consumes)
- **Windows.h**: Windows API
- **CrtDbg.h**: C Runtime debug support (Windows)
- **COM.h, Hacks.h, MessageBox.h**: Linux/Unix equivalents
- **stdio.h, assert.h, signal.h**: Standard C libraries

### Downstream (consumed by)
- **All FieldWorks native C++ code**: Uses assertions and warnings
- **Developer diagnostics**: Custom assertion handlers during debugging
- **Test infrastructure**: Controls assertion behavior in tests

## Interop & Contracts
- **Pfn_Assert typedef**: Function pointer for custom assertion handlers
  - Signature: void (__stdcall *)(const char* expr, const char* file, int line, HMODULE hmod)
- **_DBG_REPORT_HOOK typedef**: Function pointer for debug report hooks
  - Signature: void (__stdcall *)(int, char*)
- **DLL exports**: Functions exported for external use
- **Cross-platform**: Conditional compilation for Windows/Linux

## Threading & Performance
- **Global state**: Uses global variables (g_pfnAssert, g_crefWarnings, etc.)
- **Thread safety**: No explicit synchronization; assumes single-threaded or careful coordination
- **Performance**: Minimal overhead in release builds; debug builds have assertion/warning overhead

## Config & Feature Flags
- **g_crefWarnings**: Counter controlling warning display (≥0 enables, <0 disables)
- **g_crefAsserts**: Counter controlling assertion checking
- **g_fShowMessageBox**: Boolean controlling assertion message boxes
- **g_crefDisableNewAfter**: Memory allocation tracking threshold

## Build Information
- **Project file**: DebugProcs.vcxproj, DebugProcs.mak
- **Output**: DebugProcs.dll (native DLL)
- **Build**: Via top-level FieldWorks.sln or: `msbuild DebugProcs.vcxproj /p:Configuration=Debug`
- **Platform**: Windows (primary), Linux/Unix (conditional support)

## Interfaces and Data Models

- **SetAssertProc** (DebugProcs.h/cpp)
  - Purpose: Customize assertion handler for debugging or testing
  - Inputs: Pfn_Assert pfnAssert (function pointer to custom handler)
  - Outputs: Pfn_Assert (previous assertion handler)
  - Notes: Enables test frameworks to suppress assertions or redirect them

- **ShowAssertMessageBox** (DebugProcs.h/cpp)
  - Purpose: Enable/disable assertion message boxes
  - Inputs: int fShowMessageBox (boolean: 0=disable, non-zero=enable)
  - Outputs: void (sets g_fShowMessageBox)
  - Notes: Useful for automated testing where message boxes would block

- **WarnProc** (DebugProcs.cpp)
  - Purpose: Emit developer warning with file/line context
  - Inputs: const char* pszExp (warning message), const char* pszFile (source file), int nLine (line number), bool fCritical (bypass suppression), HMODULE hmod (module handle)
  - Outputs: void (outputs to debug console via handler)
  - Notes: Check g_crefWarnings; fCritical=true forces output

- **DbgSetReportHook** (DebugProcs.h/cpp)
  - Purpose: Redirect debug output to custom handler
  - Inputs: _DBG_REPORT_HOOK hook (function pointer)
  - Outputs: _DBG_REPORT_HOOK (previous hook)
  - Notes: Enables logging, filtering, or redirecting debug messages

- **DefAssertProc** (DebugProcs.cpp)
  - Purpose: Default assertion handler
  - Inputs: const char* pszExp (assertion expression), const char* pszFile (source file), int nLine (line number), HMODULE hmod (module handle)
  - Outputs: void (breaks into debugger or shows message box)
  - Notes: Calls SilAssert; can be replaced via SetAssertProc

- **DefWarnProc** (DebugProcs.cpp)
  - Purpose: Default warning handler
  - Inputs: const char* pszExp (warning message), const char* pszFile (source file), int nLine (line number), HMODULE hmod (module handle)
  - Outputs: void (formats and outputs to debug console)
  - Notes: Includes module name in output; uses OutputDebugString

## Entry Points
- **DebugProcsInit**: Initialize debug subsystem (called during DLL load)
- **DebugProcsExit**: Cleanup debug subsystem (called during DLL unload)
- Assertions and warnings called from FieldWorks C++ code via macros

## Test Index
No test project identified. Tested via assertions and warnings in FieldWorks codebase during development.

## Usage Hints
- Use SetAssertProc to customize assertion behavior for testing
- Use ShowAssertMessageBox(0) to suppress message boxes in automated tests
- g_crefWarnings counter controls warning display (decrement to enable, increment to disable)
- DbgSetReportHook to redirect debug output to logs
- Critical warnings (fCritical=true) always output regardless of g_crefWarnings
- Replace default handlers for custom debugging workflows

## Related Folders
- **Generic/**: Low-level utilities used alongside DebugProcs
- **Kernel/**: Core infrastructure using debug utilities
- All FieldWorks native C++ projects use DebugProcs

## References
- **Key C++ files**: DebugProcs.cpp (635 lines), DebugProcs.h (25 lines)
- **Project files**: DebugProcs.vcxproj, DebugProcs.mak
- **Total lines of code**: 660
- **Output**: DebugProcs.dll
- **Platform**: Windows (primary), Linux/Unix (conditional)