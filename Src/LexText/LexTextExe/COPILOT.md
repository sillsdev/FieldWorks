---
last-reviewed: 2025-10-31
last-reviewed-tree: 2c73021cb5a77d5dbf9ea03930cdcec9aed45942bf1ddab43c28ee93e1c6d94d
status: draft
---

# LexTextExe COPILOT summary

## Purpose
Minimal entry point executable for FieldWorks Language Explorer (FLEx). Contains only Main() entry point calling Common/FieldWorks/FieldWorks.StartFwApp() to launch FLEx application. Actual application logic lives in LexTextDll (LexTextApp class) and xWorks (main shell). Icons/resources for FLEx executable branding. Smallest possible entry point (32 lines) delegating to shared FieldWorks infrastructure.

## Architecture
C# Windows executable (WinExe, net462) with single entry point class. LexText.Main() calls FieldWorks.StartFwApp() passing command-line arguments. StartFwApp() handles application instantiation, project selection, window creation. LexTextApp (from LexTextDll) provides application-specific logic. Icons (LT.ico, LT.png variants) for executable branding.

## Key Components
- **LexText** (LexText.cs, 32 lines): Application entry point class
  - Main() entry point: STAThread attribute for Windows Forms threading
  - Calls FieldWorks.StartFwApp(rgArgs): Delegates to shared FW infrastructure
  - Returns 0 on success
  - using statement ensures proper disposal of FW app
- **Icons/Resources**:
  - LT.ico (4.5KB): Windows icon
  - LT.png (1.8KB): 48x48 PNG icon
  - LT64.png (3.3KB): 64x64 PNG icon
  - LT128.png (6.8KB): 128x128 PNG icon
- **AssemblyInfo.cs** (12 lines): Assembly metadata

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: WinExe (Windows executable)
- STAThread (Windows Forms threading model)

## Dependencies

### Upstream (consumes)
- **Common/FieldWorks**: FieldWorks.StartFwApp() (shared FW entry point)
- **LexTextDll**: LexTextApp application class (instantiated by StartFwApp)
- **xWorks**: Main application shell

### Downstream (consumed by)
- **Windows**: Launched by users from Start menu, desktop shortcuts
- **Command line**: Can be invoked with project arguments

## Interop & Contracts
- **FieldWorks.StartFwApp()**: Shared FieldWorks application launcher
  - Inputs: string[] rgArgs (command-line arguments)
  - Outputs: IDisposable application instance
  - Handles: Project selection, application instantiation, window creation
- **Command-line arguments**: Passed through to StartFwApp for project selection

## Threading & Performance
- **STAThread**: Required for Windows Forms COM interop
- **Startup**: StartFwApp() handles splash screen, initialization

## Config & Feature Flags
Configuration handled by StartFwApp() and LexTextApp. No config in entry point.

## Build Information
- **Project file**: LexTextExe.csproj (net462, OutputType=WinExe)
- **Output**: FieldWorks.exe (FLEx executable)
- **Build**: Via top-level FieldWorks.sln or: `msbuild LexTextExe.csproj`
- **Run**: `FieldWorks.exe` (double-click or command line)
- **Icon**: LT.ico embedded in executable

## Interfaces and Data Models

- **LexText.Main()** (LexText.cs)
  - Purpose: Application entry point
  - Inputs: string[] rgArgs (command-line arguments)
  - Outputs: int (exit code, always 0)
  - Notes: STAThread, delegates to FieldWorks.StartFwApp()

- **FieldWorks.StartFwApp()** (from Common/FieldWorks)
  - Purpose: Launch FieldWorks application
  - Inputs: string[] args (command-line arguments for project selection)
  - Outputs: IDisposable application instance
  - Notes: Handles project selection, app instantiation, window creation

## Entry Points
- **FieldWorks.exe**: Main executable (built from LexTextExe project)
- **LexText.Main()**: Entry point method

## Test Index
No dedicated test project (minimal entry point, tested via FLEx integration tests).

## Usage Hints
- **Launch**: Double-click FieldWorks.exe or run from command line
- **Command line**: `FieldWorks.exe` (prompts for project) or `FieldWorks.exe -p "ProjectName"`
- **Minimal code**: 32 lines, all logic in LexTextDll/xWorks/Common
- **Delegation pattern**: Entry point delegates to shared FieldWorks infrastructure
- **Icons**: LT.* files provide branding for executable, taskbar, shortcuts

## Related Folders
- **Common/FieldWorks**: FieldWorks.StartFwApp() (shared entry point)
- **LexTextDll**: LexTextApp application class
- **xWorks**: Main application shell
- **Common/Framework**: FwXApp base class

## References
- **Project file**: LexTextExe.csproj (net462, OutputType=WinExe)
- **Key C# files**: LexText.cs (32 lines), AssemblyInfo.cs (12 lines)
- **Icons**: LT.ico (4.5KB), LT.png (1.8KB), LT64.png (3.3KB), LT128.png (6.8KB)
- **Total lines of code**: 44
- **Output**: FieldWorks.exe (FLEx main executable)
- **Namespace**: SIL.FieldWorks.XWorks.LexText
- **Entry point**: LexText.Main()