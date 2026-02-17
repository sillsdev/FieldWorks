---
last-reviewed: 2025-10-31
last-reviewed-tree: 93bb87ed6933f01166c6f42fd1084b9fa82b40f80f614eda2f53a3c1c4cacaf3
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
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

## Dependencies
- Upstream: Windows API
- Downstream: Uses assertions and warnings

## Interop & Contracts
- **Pfn_Assert typedef**: Function pointer for custom assertion handlers

## Threading & Performance
- **Global state**: Uses global variables (g_pfnAssert, g_crefWarnings, etc.)

## Config & Feature Flags
- **g_crefWarnings**: Counter controlling warning display (≥0 enables, <0 disables)

## Build Information
- **Project file**: DebugProcs.vcxproj, DebugProcs.mak

## Interfaces and Data Models
SetAssertProc, ShowAssertMessageBox, WarnProc, DbgSetReportHook, DefAssertProc, DefWarnProc.

## Entry Points
- **DebugProcsInit**: Initialize debug subsystem (called during DLL load)

## Test Index
No test project identified. Tested via assertions and warnings in FieldWorks codebase during development.

## Usage Hints
- Use SetAssertProc to customize assertion behavior for testing

## Related Folders
- **Generic/**: Low-level utilities used alongside DebugProcs

## References
See `.cache/copilot/diff-plan.json` for file details.
