# LexText/Discourse

## Purpose
Discourse analysis features for FieldWorks. Provides tools for analyzing and charting discourse structure in texts, including constituent chart creation and management.

## Key Components
- **Discourse.csproj** - Main discourse analysis library
- **AdvancedMTDialog** - Advanced morphological tagging dialog
- **ConstChartBody.cs** - Constituent chart body implementation
- **ConstChartVc.cs** - Constituent chart view constructor
- **ConstChartRowDecorator.cs** - Chart row decoration
- **ChartLocation.cs** - Chart location tracking

## Technology Stack
- C# .NET WinForms
- Discourse analysis algorithms
- Chart visualization

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), LexText core
- Used by: LexText application discourse analysis features

## Build Information
- C# class library project
- Build via: `dotnet build Discourse.csproj`
- Part of LexText suite

## Entry Points
- Discourse chart creation and editing
- Constituent analysis tools
- Chart visualization components

## Related Folders
- **LexText/Interlinear/** - Works with interlinear text for discourse analysis
- **LexText/LexTextControls/** - UI controls for discourse features
- **Cellar/** - Stores discourse analysis data
