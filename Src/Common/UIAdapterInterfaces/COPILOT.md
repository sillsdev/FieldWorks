---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# UIAdapterInterfaces COPILOT summary

## Purpose
UI adapter pattern interfaces for abstraction and testability. Defines contracts (SIBInterface, TMInterface) that allow UI components to be adapted to different technologies or replaced with test doubles. Enables dependency injection and testing of UI-dependent code.

## Architecture
C# interface library (.NET Framework 4.6.2) with UI adapter contracts.

## Key Components
- **SIBInterface**: Side bar interface contract
- **TMInterface**: Text manager interface contract
- Helper classes for UI adaptation
- Adapter pattern interfaces

## Technology Stack
- C# .NET Framework 4.6.2
- Interface definitions only

## Dependencies
### Upstream
- Minimal (interface definitions)

### Downstream
- UI components implementing adapters
- Test projects using test doubles

## Build Information
- **Project**: UIAdapterInterfaces.csproj (net462, Library)
- **Build**: Via FW.sln

## References
- **Namespace**: SIL.FieldWorks.Common.UIAdapterInterfaces
