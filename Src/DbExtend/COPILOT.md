---
last-reviewed: 2025-10-31
last-reviewed-tree: 4449cc39e6af5c0398802ed39fc79f9aa35da59d3efb1ae3fddbb2af71dd14af
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# DbExtend COPILOT summary

## Purpose
SQL Server extended stored procedure for pattern matching operations. Provides xp_IsMatch extended stored procedure enabling SQL Server to perform pattern matching against nvarchar/ntext fields using custom matching logic. Extends SQL Server functionality with specialized text pattern matching not available in standard SQL Server string functions.

## Architecture
C++ native DLL implementing SQL Server extended stored procedure API. Single file (xp_IsMatch.cpp, 238 lines) containing xp_IsMatch stored procedure and FindMatch helper function. Compiled as DLL loaded by SQL Server for extended procedure calls.

## Key Components
- **xp_IsMatch** function: Extended stored procedure entry point
  - Takes 3 parameters: Pattern (nvarchar), String (nvarchar/ntext), Result (bit output)
  - Performs pattern matching: does String match Pattern?
  - Returns result via output parameter
  - Uses srv_* functions for SQL Server ODS (Open Data Services) API
- **FindMatch** function: Pattern matching implementation
  - Takes wchar_t* pattern and string
  - Returns bool indicating match
  - Core pattern matching logic
- **__GetXpVersion** function: ODS version reporting
  - Required by SQL Server 7.0+ for extended stored procedures
  - Returns ODS_VERSION constant
- **printError** function: Error reporting helper
  - Sends error messages back to SQL Server client
  - Uses srv_sendmsg for error communication

## Technology Stack
- C++ native code

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- SQL Server ODS API: srv_* functions for extended stored procedures

## Threading & Performance
- SQL Server threading: Called on SQL Server worker threads

## Config & Feature Flags
No configuration. Behavior determined by pattern and string inputs.

## Build Information
- No project file: Built as part of larger solution or manually

## Interfaces and Data Models
xp_IsMatch, FindMatch, __GetXpVersion.

## Entry Points
- xp_IsMatch: Called from SQL Server T-SQL as extended stored procedure

## Test Index
No test project identified. Testing via SQL Server T-SQL calls to xp_IsMatch.

## Usage Hints
- Register DLL with SQL Server: `sp_addextendedproc 'xp_IsMatch', 'path\to\dll'`

## Related Folders
- Kernel/: May reference this for database operations

## References
See `.cache/copilot/diff-plan.json` for file details.
