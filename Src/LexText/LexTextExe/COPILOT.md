# LexText/LexTextExe

## Purpose
Main executable for the LexText (FLEx) lexicon and dictionary application. Provides the entry point for launching the FieldWorks Language Explorer (FLEx) application.

## Key Components
- **LexTextExe.csproj** - Executable project
- **LexText.cs** - Main entry point
- Application icons (LT.ico, LT.png, various sizes)

## Technology Stack
- C# .NET WinForms
- Application executable
- XCore framework integration

## Dependencies
- Depends on: LexText/LexTextDll (core logic), XCore (framework), all dependencies
- Used by: End users launching FLEx application

## Build Information
- C# Windows application executable
- Build via: `dotnet build LexTextExe.csproj`
- Produces LexText.exe (FLEx application)

## Entry Points
- Main() method - application entry point
- Initializes XCore framework and loads LexTextDll

## Related Folders
- **LexText/LexTextDll/** - Core application logic loaded by this executable
- **XCore/** - Application framework
- **xWorks/** - Shared application infrastructure
