---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Filters

## Purpose
Data filtering and sorting infrastructure for searchable data views.
Implements various matcher types (IntMatcher, RangeIntMatcher, ExactMatcher, BeginMatcher)
and filtering logic (RecordFilter, ProblemAnnotationFilter) for narrowing and organizing
data sets. Essential for browse views, search functionality, and filtered list displays
throughout FieldWorks applications.

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

## Interfaces and Data Models

- **IManyOnePathSortItem** (interface)
  - Path: `IManyOnePathSortItem.cs`
  - Public interface definition

- **IMatcher** (interface)
  - Path: `RecordFilter.cs`
  - Public interface definition

- **IReportsSortProgress** (interface)
  - Path: `RecordFilter.cs`
  - Public interface definition

- **IStringFinder** (interface)
  - Path: `RecordFilter.cs`
  - Public interface definition

- **AndFilter** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **AnywhereMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **BeginMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **BlankMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **EndMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **ExactMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **FilterBarCellFilter** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **FilterChangeEventArgs** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **IntMatcher** (class)
  - Path: `IntMatcher.cs`
  - Public class implementation

- **InvertMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **MultiIndirectMlPropFinder** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **NonBlankMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **NotEqualIntMatcher** (class)
  - Path: `IntMatcher.cs`
  - Public class implementation

- **OneIndirectAtomMlPropFinder** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **OneIndirectMlPropFinder** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **OwnMlPropFinder** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **OwnMonoPropFinder** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **ProblemAnnotationFilter** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **RangeIntMatcher** (class)
  - Path: `IntMatcher.cs`
  - Public class implementation

- **RegExpMatcher** (class)
  - Path: `RecordFilter.cs`
  - Public class implementation

- **MatchRangePair** (struct)
  - Path: `RecordFilter.cs`

- **DateMatchType** (enum)
  - Path: `DateTimeMatcher.cs`

## References

- **Project files**: Filters.csproj, FiltersTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, DateTimeMatcher.cs, ExactLiteralMatcher.cs, FiltersStrings.Designer.cs, IManyOnePathSortItem.cs, IntFinder.cs, IntMatcher.cs, ManyOnePathSortItem.cs, RecordFilter.cs, RecordSorter.cs
- **Source file count**: 18 files
- **Data file count**: 1 files
