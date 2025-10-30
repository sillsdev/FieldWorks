---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common/Filters

## Purpose
Data filtering functionality for FieldWorks. Provides filter matchers and sorting capabilities for searching and displaying filtered data sets.

## Key Components
- **Filters.csproj** - Main filtering library
- **BadSpellingMatcher.cs** - Spell-check filtering
- **DateTimeMatcher.cs** - Date/time filtering
- **ExactLiteralMatcher.cs** - Exact text matching
- **FindResultSorter.cs** - Result sorting infrastructure
- **FiltersTests/** - Comprehensive test suite


## Key Classes/Interfaces
- **RangeIntMatcher**
- **NotEqualIntMatcher**
- **FilterChangeEventArgs**
- **ProblemAnnotationFilter**
- **IMatcher**
- **DateTimeMatcher**
- **DateMatchType**
- **ExactLiteralMatcher**

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

## Testing
- Run tests: `dotnet test Filters/FiltersTests/`
- Tests cover matcher and sorter functionality

## Entry Points
- Filter matchers for data filtering
- Sorting infrastructure for result display

## Related Folders
- **Common/FwUtils/** - Utilities used by filters
- **xWorks/** - Uses filtering for data tree and searches
- **LexText/** - Uses filtering in lexicon searches


## References
- **Project Files**: Filters.csproj
- **Key C# Files**: BadSpellingMatcher.cs, DateTimeMatcher.cs, ExactLiteralMatcher.cs, FindResultSorter.cs, IManyOnePathSortItem.cs, IntFinder.cs, IntMatcher.cs, ManyOnePathSortItem.cs
