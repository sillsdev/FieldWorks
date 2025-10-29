# Common/SimpleRootSite

## Purpose
Simplified root site implementation for view hosting. Provides a streamlined API for hosting FieldWorks views with common functionality pre-configured.

## Key Components
- **SimpleRootSite.csproj** - Simplified root site library
- **SimpleRootSite.cs** - Main simplified site class
- **AccessibilityWrapper.cs** - Accessibility support
- **ActiveViewHelper.cs** - Active view management
- **DataUpdateMonitor.cs** - Data change monitoring
- **EditingHelper.cs** - Editing support
- **SelectionHelper.cs** - Selection management
- **SelectionRestorer.cs** - Selection restoration
- **VwSelectionArgs.cs** - Selection event arguments
- **IRootSite.cs** - Root site interface

## Technology Stack
- C# .NET
- View hosting infrastructure
- Event-driven architecture

## Dependencies
- Depends on: Common/RootSite, views (native views), Common/ViewsInterfaces
- Used by: Most FieldWorks view-based components

## Build Information
- C# class library project
- Build via: `dotnet build SimpleRootSite.csproj`
- Higher-level abstraction over RootSite

## Entry Points
- SimpleRootSite class for easy view hosting
- Helper classes for common view operations
- Selection and editing support

## Related Folders
- **Common/RootSite/** - Base infrastructure used by SimpleRootSite
- **Common/ViewsInterfaces/** - Interfaces implemented
- **ManagedVwWindow/** - Window components using SimpleRootSite
- **xWorks/** - Uses SimpleRootSite for data views
- **LexText/** - Uses SimpleRootSite for text editing
