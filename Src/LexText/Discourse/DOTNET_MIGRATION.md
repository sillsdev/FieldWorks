# .NET 8 Migration Assessment: Discourse

## Current State
- **Location**: `LexText/Discourse`
- **Project Type(s)**: C#
- **Framework**: v4.6.2
- **Complexity**: **Medium**

## Technology Analysis

### Projects in This Folder

- **Discourse** (C#)
  - Framework: v4.6.2
  - Type: Library
  - Style: legacy
  - ⚠️ Uses WinForms

### Key Dependencies

- Common/FwUtils
- Common/Controls/Widgets

## Migration Blockers

- ⚠️ WinForms UI (needs Avalonia migration or use community ports)

## Migration Strategy

1. WinForms dependency detected. Options: 1) Migrate to .NET 8 keeping WinForms (supported on Windows), 2) Plan separate Avalonia migration for cross-platform, 3) Use community WinForms ports if cross-platform is urgent.

## Estimated Effort

**Medium** (1-2 weeks)
- Project file conversion
- Limited refactoring
- Standard testing

## Risk Assessment

- **Medium-High**: WinForms to Avalonia migration deferred

## Prerequisites

Dependencies that must be migrated first:
- Common/FwUtils
- Common/Controls/Widgets

## Notes

- Generated: 2025-11-04
- Total projects analyzed: 1
