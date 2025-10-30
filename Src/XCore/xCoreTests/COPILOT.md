---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XCore/xCoreTests

## Purpose
Test suite for XCore framework. Provides comprehensive tests for XCore framework functionality including XML configuration, inventory management, and command handling.

## Key Components
- **xCoreTests.csproj** - XCore test project
- **IncludeXmlTests.cs** - XML include/merge testing
- **InventoryTests.cs** - Component inventory tests
- Test data files (CreateOverrideTestData.xml, etc.)
- Inventory test file sets

## Technology Stack
- C# .NET with NUnit or similar
- Unit testing framework
- XML test data

## Dependencies
- Depends on: XCore (framework being tested), XCore/xCoreInterfaces
- Used by: Build and CI systems for validation

## Build Information
- C# test project
- Build via: `dotnet build xCoreTests.csproj`
- Run tests: `dotnet test xCoreTests.csproj`

## Testing
- Tests cover XCore framework functionality
- XML configuration and inventory management
- Command and choice handling

## Entry Points
- Test fixtures for XCore components
- Validation of framework behavior

## Related Folders
- **XCore/** - Framework being tested
- **XCore/xCoreInterfaces/** - Interfaces being tested
- **XCore/FlexUIAdapter/** - May have related tests
