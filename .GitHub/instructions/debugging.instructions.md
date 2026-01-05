---
applyTo: "**/*"
name: "debugging.instructions"
description: "Runtime debugging and tracing guidance for FieldWorks"
---

# Debugging and Tracing

## Purpose & Scope
- Enable developers to capture actionable runtime diagnostics in Debug builds.
- Cover trace switches, listeners, and crash evidence collection for FieldWorks desktop.

## Quick Start: Enable Trace Log
1) Copy the snippet below into `Output/Debug/FieldWorks.exe.config` (or create it by copying `Src/AppForTests.config`).
2) Ensure `%temp%` is writable; set `AssertUiEnabled=false` to avoid modal assertion dialogs.
3) Restart `FieldWorks.exe`; traces will append to `%temp%/FieldWorks.trace.log`.

```xml
<system.diagnostics>
  <trace autoflush="false">
    <listeners>
      <clear />
      <add name="FwTraceListener"
           type="SIL.LCModel.Utils.EnvVarTraceListener, SIL.LCModel.Utils"
           initializeData="assertuienabled='false' assertexceptionenabled='false' logfilename='%temp%/FieldWorks.trace.log'" />
    </listeners>
  </trace>
  <switches>
    <add name="ShowPendingMsgs" value="3" />
    <add name="XCore.Mediator_InvokeTrace" value="3" />
    <add name="XWorks_Timing" value="3" />
    <add name="XWorks_LinkListener" value="3" />
  </switches>
</system.diagnostics>
```
- Switch levels: 0 Off, 1 Error, 2 Warning, 3 Info, 4 Verbose.
- Log file path is configurable via `logfilename`; environment variable expansion (`%temp%`) is supported by `EnvVarTraceListener`.

## Trace Switch Reference
- `ShowPendingMsgs`: XCore pending message queue tracing.
- `XCore.Mediator_InvokeTrace`: Mediator invoke and job queue tracing.
- `XWorks_Timing`: Timing hooks in xWorks (RecordList, etc.).
- `XWorks_LinkListener`: LinkListener diagnostics.
- Additional Trace.WriteLine hooks exist in Debug-only helpers (`DebugProcs`, etc.).

## Assertions and UI
- `EnvVarTraceListener` honors `AssertUiEnabled` and `AssertExceptionEnabled` environment variables.
- Set `AssertUiEnabled=false` to suppress modal assertion dialogs; exceptions are thrown only if `assertexceptionenabled` remains `true`.

## Dev switch (auto config)
- FieldWorks now supports a swappable diagnostics config via `FieldWorks.Diagnostics.config`.
- Default is quiet. `build.ps1` now enables the dev diagnostics config automatically for Debug builds unless you override `/p:UseDevTraceConfig`. You can also force it via `UseDevTraceConfig=true` or by setting environment variable `FW_TRACE_LOG` before the build; the dev diagnostics file is copied as `FieldWorks.Diagnostics.config` in the output.
- Dev log location: `Output/Debug/FieldWorks.trace.log` (relative to the app folder) so it’s easy to collect alongside binaries.
- Dev config logs to `%temp%/FieldWorks.trace.log` and turns on the core switches above. Edit `Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config` to change log path or switches.

## Crash Evidence
- Check `%LOCALAPPDATA%\CrashDumps` for `FieldWorks.exe.*.dmp` (enable Windows Error Reporting local dumps if missing).
- Windows Event Viewer → Windows Logs → Application contains `.NET Runtime` and `Application Error` entries.

## Proposed Improvements (dev-only)
- Add `Docs/FieldWorks.trace.sample.config` with the snippet above for easy reuse.
- Introduce a dev flag (`UseDevTraceConfig=true` or `FW_TRACE_LOG` env var) that copies the dev diagnostics file next to `FieldWorks.exe` in Debug builds so tracing is on by default for local runs.
- Document standard trace switches in `Docs/logging.md` and keep `EnvVarTraceListener` as the default listener for dev traces.
