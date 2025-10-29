# ManagedVwWindow

## Purpose
Managed view window components. Provides .NET wrappers and management for native view windows, bridging the gap between managed UI code and native view rendering.

## Key Components
- **ManagedVwWindow.csproj** - Managed window wrapper library
- **ManagedVwWindowTests/ManagedVwWindowTests.csproj** - Window component tests

## Technology Stack
- C# .NET with C++/CLI interop
- Windows Forms integration
- Native window handle management

## Dependencies
- Depends on: views (native view layer), Common (UI infrastructure), ManagedVwDrawRootBuffered
- Used by: All applications with view-based UI (xWorks, LexText)

## Build Information
- C# class library with native interop
- Includes test suite
- Build with MSBuild or Visual Studio

## Testing
- Run tests: `dotnet test ManagedVwWindow/ManagedVwWindowTests/ManagedVwWindowTests.csproj`
- Tests cover window management and view integration

## Entry Points
- Provides managed window classes for views
- Bridge between managed UI and native rendering

## Related Folders
- **views/** - Native view layer that ManagedVwWindow wraps
- **ManagedVwDrawRootBuffered/** - Buffered rendering used by windows
- **Common/RootSite/** - Root site components using managed windows
- **xWorks/** - Applications using view windows
- **LexText/** - Uses view windows for text display
