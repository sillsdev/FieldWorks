---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# ScriptureUtils COPILOT summary

## Purpose
Scripture-specific utilities and Paratext integration support. Provides ParatextHelper for Paratext project access, PT7ScrTextWrapper for Paratext 7 text handling, and Paratext7Provider for data exchange between FieldWorks and Paratext.

## Architecture
C# class library (.NET Framework 4.6.2) with Paratext integration components.

## Key Components
- **ParatextHelper**: Paratext project utilities
- **PT7ScrTextWrapper**: Paratext 7 text wrapper
- **Paratext7Provider**: Data provider for Paratext integration
- Scripture-specific utilities

## Technology Stack
- C# .NET Framework 4.6.2
- Paratext API integration

## Dependencies
### Upstream
- Paratext libraries
- SIL.LCModel (scripture data)

### Downstream
- Scripture editing components
- Interlinear tools

## Build Information
- **Project**: ScriptureUtils.csproj (net462, Library)
- **Test project**: ScriptureUtilsTests
- **Build**: Via FW.sln

## References
- **Namespace**: SIL.FieldWorks.Common.ScriptureUtils
