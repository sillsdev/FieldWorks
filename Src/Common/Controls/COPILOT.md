# Common/Controls

## Purpose
Shared UI controls library providing reusable widgets and XML-based view components used throughout FieldWorks applications.

## Key Components
- **Design/** - Design-time support for controls
- **DetailControls/** - Detailed view controls for data display
- **FwControls/** - Core FieldWorks control implementations
- **Widgets/** - Reusable UI widget components
- **XMLViews/** - XML-driven view rendering system

## Technology Stack
- C# .NET WinForms
- Custom control development
- XML-driven UI configuration

## Dependencies
- Depends on: Common/Framework, Common/ViewsInterfaces
- Used by: xWorks, LexText, FwCoreDlgs (UI-heavy applications)

## Build Information
- Part of Common solution
- Contains multiple control libraries
- Build with parent Common project or solution-wide

## Entry Points
- Provides reusable controls for application UIs
- XML view system for declarative UI definition

## Related Folders
- **Common/Framework/** - Application framework using these controls
- **Common/ViewsInterfaces/** - Interfaces implemented by controls
- **xWorks/** - Major consumer of Common controls
- **FwCoreDlgs/** - Dialog system using Common controls
