---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# DbExtend

## Purpose
Database schema extension and runtime customization infrastructure. 
Provides mechanisms for extending the base FieldWorks data model with custom fields, 
properties, and relationships at runtime. Enables users and plugins to add domain-specific 
data structures without modifying core schema definitions.

## Key Components
No major public classes identified.

## Technology Stack
- C++ native code
- Database schema manipulation
- SQL and data model extension APIs

## Dependencies
- Depends on: Cellar (core data model), Kernel
- Used by: Applications that need custom fields or schema extensions

## Build Information
- Native C++ library
- Built as part of the larger solution
- No standalone project file in root (built via includes)

## Entry Points
- Provides schema extension APIs
- Used by applications to add custom fields and data types

## Related Folders
- **Cellar/** - Core data model that DbExtend extends
- **MigrateSqlDbs/** - Database migration tools that work with schema changes
- **FdoUi/** - UI for managing custom fields built on DbExtend

## Code Evidence
*Analysis based on scanning 1 source files*

## References

- **Key C++ files**: xp_IsMatch.cpp
- **Source file count**: 1 files
- **Data file count**: 0 files
