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
- SQL Server ODS (Open Data Services) API
- Extended stored procedure framework
- Unicode text handling (wchar_t*)

## Dependencies

### Upstream (consumes)
- **main.h**: Header with ODS API definitions
- **SQL Server ODS library**: srv_* functions (srv_rpcparams, srv_paraminfo, srv_sendmsg, srv_senddone)
- Windows platform (ULONG, RETCODE, BYTE types)

### Downstream (consumed by)
- **SQL Server**: Loads DLL and calls xp_IsMatch from T-SQL
- **FieldWorks database queries**: Uses xp_IsMatch for pattern matching in queries
- Database layer requiring custom pattern matching

## Interop & Contracts
- **SQL Server ODS API**: srv_* functions for extended stored procedures
- **xp_IsMatch signature**: (nvarchar pattern, nvarchar/ntext string, bit output) → RETCODE
- **DLL exports**: __GetXpVersion, xp_IsMatch
- **extern "C"**: C linkage for SQL Server interop

## Threading & Performance
- **SQL Server threading**: Called on SQL Server worker threads
- **Single invocation**: Each call processes one pattern/string pair
- **Memory allocation**: Dynamic allocation for Unicode strings (malloc/free)
- **Performance**: Pattern matching performance depends on FindMatch implementation

## Config & Feature Flags
No configuration. Behavior determined by pattern and string inputs.

## Build Information
- **No project file**: Built as part of larger solution or manually
- **Output**: DLL file loaded by SQL Server
- **Build**: C++ compiler targeting Windows DLL
- **Deploy**: Register with SQL Server via sp_addextendedproc

## Interfaces and Data Models

- **xp_IsMatch** (xp_IsMatch.cpp)
  - Purpose: SQL Server extended stored procedure for pattern matching
  - Inputs: Parameter 1: nvarchar/ntext pattern, Parameter 2: nvarchar/ntext string to match, Parameter 3: bit output for result
  - Outputs: RETCODE (XP_NOERROR or XP_ERROR), Result parameter set to 1 (match) or 0 (no match)
  - Notes: Validates 3 parameters, extracts Unicode strings, calls FindMatch, returns result

- **FindMatch** (xp_IsMatch.cpp)
  - Purpose: Core pattern matching logic
  - Inputs: wchar_t* pszPattern (pattern string), wchar_t* pszString (string to match)
  - Outputs: bool (true if match, false otherwise)
  - Notes: Implementation details in source; custom pattern matching algorithm

- **__GetXpVersion** (xp_IsMatch.cpp)
  - Purpose: Reports ODS version to SQL Server
  - Inputs: None
  - Outputs: ULONG (ODS_VERSION constant)
  - Notes: Required by SQL Server 7.0+ extended stored procedure spec

- **SQL Server ODS API Functions Used**:
  - srv_rpcparams(): Get parameter count
  - srv_paraminfo(): Get parameter info (type, length, value)
  - srv_paramsetoutput(): Set output parameter value
  - srv_sendmsg(): Send error/info messages
  - srv_senddone(): Complete result set

## Entry Points
- **xp_IsMatch**: Called from SQL Server T-SQL as extended stored procedure
- **__GetXpVersion**: Called by SQL Server during DLL initialization

## Test Index
No test project identified. Testing via SQL Server T-SQL calls to xp_IsMatch.

## Usage Hints
- Register DLL with SQL Server: `sp_addextendedproc 'xp_IsMatch', 'path\to\dll'`
- Call from T-SQL: `DECLARE @result bit; EXEC xp_IsMatch N'pattern', N'string', @result OUTPUT`
- Pattern matching syntax determined by FindMatch implementation
- Handles Unicode text (nvarchar, ntext)
- Error handling via SQL Server error messages

## Related Folders
- **Kernel/**: May reference this for database operations
- Database access layers in FieldWorks

## References
- **Key C++ files**: xp_IsMatch.cpp (238 lines)
- **Total lines of code**: 238
- **Output**: DLL loaded by SQL Server
- **ODS API**: SQL Server Open Data Services for extended stored procedures
