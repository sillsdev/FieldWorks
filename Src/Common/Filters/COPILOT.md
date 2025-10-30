---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Filters

## Purpose
Data filtering functionality for FieldWorks. Provides filter matchers and sorting capabilities for searching and displaying filtered data sets.

## Key Components
### Key Classes
- **IntMatcher**
- **RangeIntMatcher**
- **NotEqualIntMatcher**
- **FilterChangeEventArgs**
- **RecordFilter**
- **ProblemAnnotationFilter**
- **BaseMatcher**
- **SimpleStringMatcher**
- **ExactMatcher**
- **BeginMatcher**

### Key Interfaces
- **IMatcher**
- **IStringFinder**
- **IReportsSortProgress**
- **IManyOnePathSortItem**

## Technology Stack
- C# .NET
- Filter pattern implementation
- Text matching and sorting algorithms

## Dependencies
- Depends on: Common/FwUtils (utilities)
- Used by: xWorks, LexText (search and filter features)

## Build Information
- C# class library project
- Build via: `dotnet build Filters.csproj`
- Includes test project

## Entry Points
- Filter matchers for data filtering
- Sorting infrastructure for result display

## Related Folders
- **Common/FwUtils/** - Utilities used by filters
- **xWorks/** - Uses filtering for data tree and searches
- **LexText/** - Uses filtering in lexicon searches

## Code Evidence
*Analysis based on scanning 17 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 4 public interfaces
- **Namespaces**: SIL.FieldWorks.Filters
