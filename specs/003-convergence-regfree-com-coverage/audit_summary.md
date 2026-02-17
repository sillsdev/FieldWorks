# COM Usage Audit Summary

## Overview
This document summarizes the findings of the COM usage audit performed on the FieldWorks codebase. The goal was to identify which managed executables require Registration-Free COM manifests to function correctly without global COM registration.

## Methodology
The audit was performed using `scripts/regfree/audit_com_usage.py`. This tool scans C# project files and source code for:
- `[DllImport("ole32.dll")]`
- `[ComImport]` attributes
- References to `FwKernel` or `Views` namespaces
- Project references to `ViewsInterfaces`
- Package references to `SIL.LCModel` (which implies COM usage)
- Transitive dependencies were also analyzed.

## Findings

The following executables were identified as using COM and requiring RegFree manifests:

| Executable | Priority | COM Usage Indicators | Notes |
| :--- | :--- | :--- | :--- |
| **FieldWorks.exe** | P0 | High | Main application. Heavy COM usage via Views, FwKernel, and LCModel. |
| **LCMBrowser.exe** | P1 | High | Developer tool. Heavy COM usage similar to FieldWorks.exe. |
| **UnicodeCharEditor.exe** | P1 | Medium | Utility. Uses Views and FwKernel. |
| **FxtExe.exe** | P2 | High | FXT tool. Heavy COM usage. |
| **MigrateSqlDbs.exe** | P2 | Medium | Database migration tool. Uses Views and FwKernel. |

The following executables were **NOT** found to use COM directly or transitively in a way that requires a manifest (based on current heuristics):

- `ComManifestTestHost.exe` (Test harness)
- `FixFwData.exe` (Utility)
- `ConverterConsole.exe` (Legacy/Utility)
- `Converter.exe` (Legacy/Utility)
- `ConvertSFM.exe` (Utility)
- `SfmStats.exe` (Utility)

## Recommendations

1.  **FieldWorks.exe**: This is the highest priority. It already has a manifest, but it needs to be verified and potentially updated to be fully RegFree compliant for all dependencies.
2.  **LCMBrowser.exe**: We have already started work on this (verified manifest generation). It serves as a good pilot.
3.  **UnicodeCharEditor.exe**: Should be the next target after LCMBrowser.
4.  **FxtExe.exe** & **MigrateSqlDbs.exe**: Schedule for subsequent updates.

## Next Steps
- Continue with the plan to enable RegFree COM for `LCMBrowser.exe` and verify it runs without registration.
- Apply the same pattern to `FieldWorks.exe` and others.
