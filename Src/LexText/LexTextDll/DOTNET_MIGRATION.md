# .NET 8 Migration Assessment: LexTextDll

## Current State
- **Location**: `LexText/LexTextDll`
- **Project Type(s)**: C#
- **Framework**: v4.6.2
- **Complexity**: **High**

## Technology Analysis

### Projects in This Folder

- **LexTextDll** (C#)
  - Framework: v4.6.2
  - Type: Library
  - Style: legacy
  - ⚠️ Uses WinForms

### Key Dependencies

- Common/Framework
- Common/FwUtils
- Common/Controls
- Common/RootSites
- Interlinear/
- LexTextControls/

## Migration Blockers

- ⚠️ WinForms UI (needs Avalonia migration or use community ports)

## Migration Strategy

1. WinForms dependency detected. Options: 1) Migrate to .NET 8 keeping WinForms (supported on Windows), 2) Plan separate Avalonia migration for cross-platform, 3) Use community WinForms ports if cross-platform is urgent.

## Estimated Effort

**Medium-Large** (2-4 weeks)
- Moderate refactoring required
- Some blockers to address
- Thorough testing needed

## Risk Assessment

- **Medium-High**: WinForms to Avalonia migration deferred
- **Medium**: Many dependencies must migrate first

## Prerequisites

Dependencies that must be migrated first:
- Common/Framework
- Common/FwUtils
- Common/Controls
- Common/RootSites
- Interlinear/

## Notes

- Generated: 2025-11-04
- Total projects analyzed: 1
