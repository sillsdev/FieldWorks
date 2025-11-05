---
last-reviewed: 2025-10-31
last-reviewed-tree: 001efe2bada829eaaf0f9945ca7676da4b443f38a42914c42118cb2430b057c7
status: draft
---

# Filters COPILOT summary

## Purpose
Data filtering and sorting infrastructure for searchable data views throughout FieldWorks. Implements matcher types (IntMatcher, RangeIntMatcher, ExactMatcher, BeginMatcher, RegExpMatcher, DateTimeMatcher, BadSpellingMatcher) and filtering logic (RecordFilter, AndFilter, ProblemAnnotationFilter, FilterBarCellFilter) for narrowing data sets. Provides sorting infrastructure (RecordSorter, FindResultSorter, ManyOnePathSortItem) for organizing filtered results. Essential for browse views, search functionality, filtered list displays, and filter bar UI components in FieldWorks applications.

## Architecture
C# class library (.NET Framework 4.6.2) with filtering and sorting components. RecordFilter base class provides in-memory filtering using IMatcher implementations and IStringFinder interfaces to extract and match values from objects. Filter bar support via FilterBarCellFilter combines matchers with string finders for column-based filtering in browse views. Sorting via RecordSorter with progress reporting (IReportsSortProgress) and IManyOnePathSortItem for complex hierarchical sorts. Test project (FiltersTests) validates matcher behavior, sorting, and persistence.

## Key Components
- **RecordFilter** class (RecordFilter.cs, 2751 lines): Base class for in-memory filters
  - Abstract class with Matches() method for object acceptance testing
  - Subclasses: AndFilter (combines multiple filters), FilterBarCellFilter (filter bar cell), ProblemAnnotationFilter (annotation-specific)
  - FilterChangeEventArgs: Event args for filter add/remove notifications
- **Matcher classes** (RecordFilter.cs): String matching strategies
  - **IMatcher** interface: Contract for text matching (Matches() method)
  - **ExactMatcher**: Exact string match
  - **BeginMatcher**: Match string beginning
  - **EndMatcher**: Match string ending
  - **AnywhereMatcher**: Substring match anywhere
  - **RegExpMatcher**: Regular expression matching
  - **BlankMatcher**: Matches empty/blank strings
  - **NonBlankMatcher**: Matches non-empty strings
  - **InvertMatcher**: Inverts another matcher's result
- **IntMatcher** (IntMatcher.cs, 220 lines): Integer value matching
  - Matches integer properties against target values
  - **RangeIntMatcher**: Matches integers within range (min/max)
  - **NotEqualIntMatcher**: Matches integers not equal to target
- **DateTimeMatcher** (DateTimeMatcher.cs, 309 lines): Date/time matching
  - DateMatchType enum: Before, After, On, Between, NotSet
  - Matches date/time properties with various comparison modes
- **BadSpellingMatcher** (BadSpellingMatcher.cs, 183 lines): Spelling error matcher
  - Identifies strings with spelling errors
  - Integrates with spelling checker services
- **ExactLiteralMatcher** (ExactLiteralMatcher.cs, 136 lines): Literal string exact match
  - Case-sensitive exact matching of literal strings
- **WordFormFilters** (WordFormFilters.cs, 289 lines): Word form specific filters
  - Specialized filters for linguistic word form data
- **IStringFinder** interface (RecordFilter.cs): Extracts strings from objects
  - Used by FilterBarCellFilter to obtain cell values for matching
  - Implementations: OwnMlPropFinder, OwnMonoPropFinder, OneIndirectMlPropFinder, OneIndirectAtomMlPropFinder, MultiIndirectMlPropFinder
- **RecordSorter** class (RecordSorter.cs, 2268 lines): Sorts filtered results
  - Implements IComparer for object comparison
  - Supports multi-level hierarchical sorting
  - IReportsSortProgress interface: Progress callbacks during long sorts
- **FindResultSorter** (FindResultSorter.cs, 75 lines): Sorts search/find results
  - Specialized sorter for search result ordering
- **IManyOnePathSortItem** interface (IManyOnePathSortItem.cs, 29 lines): Multi-path sort item
  - Contract for items with multiple sort paths (e.g., many-to-one relationships)
- **ManyOnePathSortItem** class (ManyOnePathSortItem.cs, 463 lines): Multi-path sort implementation
  - Handles sorting of items with complex hierarchical relationships
- **IntFinder** (IntFinder.cs, 102 lines): Integer property finder
  - Extracts integer values from objects for IntMatcher filtering
- **FiltersStrings** (FiltersStrings.Designer.cs/.resx): Localized string resources
  - UI strings for filter-related messages and labels

## Technology Stack
- C# .NET Framework 4.6.2 (target framework: net462)
- OutputType: Library (class library DLL)
- System.Xml for XML-based filter persistence
- Regular expressions (System.Text.RegularExpressions) for RegExpMatcher
- SIL.LCModel for data access (LcmCache, ICmObject)
- SIL.WritingSystems for writing system support

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Language and Culture Model (LcmCache, ICmObject)
- **SIL.LCModel.Core.Text**: Text handling
- **SIL.LCModel.Core.WritingSystems**: Writing system infrastructure
- **SIL.LCModel.Core.KernelInterfaces**: Core interfaces
- **Common/ViewsInterfaces**: View interfaces (IVwCacheDa)
- **SIL.WritingSystems**: Writing system utilities
- **SIL.Utils**: General utilities
- **System.Xml**: XML parsing for filter persistence

### Downstream (consumed by)
- **xWorks/**: Uses filtering for data tree and browse view searches
- **LexText/**: Uses filtering in lexicon searches and browse views
- Any FieldWorks component requiring data filtering, sorting, or filter bar UI

## Interop & Contracts
- **IMatcher**: Contract for text matching strategies
- **IStringFinder**: Contract for extracting strings from objects for matching
- **IReportsSortProgress**: Contract for reporting sort progress during long operations
- **IManyOnePathSortItem**: Contract for items with multiple sort paths
- Uses marshaling for COM interop scenarios (ICmObject from LCModel)

## Threading & Performance
- Single-threaded in-memory filtering and sorting
- RecordSorter supports progress reporting (IReportsSortProgress) for responsiveness during long sorts
- Filter bar optimization: Limits unique value enumeration to ~30 items to avoid performance issues
- Performance note: All filtering currently done in-memory (see RecordFilter.cs comments for future query-based filtering)

## Config & Feature Flags
- No external configuration files
- Filter definitions can be persisted to/from XML
- Filter bar behavior configurable via XML cell definitions

## Build Information
- **Project file**: Filters.csproj (.NET Framework 4.6.2, OutputType=Library)
- **Test project**: FiltersTests/FiltersTests.csproj
- **Output**: Filters.dll (to Output/Debug or Output/Release)
- **Build**: Via top-level FieldWorks.sln or: `msbuild Filters.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test FiltersTests/FiltersTests.csproj` or Visual Studio Test Explorer

## Interfaces and Data Models

- **IMatcher** (RecordFilter.cs)
  - Purpose: Contract for string matching strategies
  - Inputs: ITsString (formatted text string), int ws (writing system)
  - Outputs: bool (true if matches)
  - Notes: Implemented by ExactMatcher, BeginMatcher, EndMatcher, AnywhereMatcher, RegExpMatcher, etc.

- **IStringFinder** (RecordFilter.cs)
  - Purpose: Extracts strings from objects for matching
  - Inputs: LcmCache cache, int hvo (object ID)
  - Outputs: ITsString (extracted string value)
  - Notes: Used by FilterBarCellFilter to get cell values from XML-defined cell specifications

- **IReportsSortProgress** (RecordFilter.cs)
  - Purpose: Progress reporting during long sort operations
  - Inputs: int nTotal (total items), int nCompleted (completed items)
  - Outputs: void (progress callbacks)
  - Notes: Allows UI to show progress bar and remain responsive

- **IManyOnePathSortItem** (IManyOnePathSortItem.cs)
  - Purpose: Contract for items with multiple sort paths (many-to-one relationships)
  - Inputs: N/A (properties)
  - Outputs: Multiple sort key paths for hierarchical sorting
  - Notes: Enables complex multi-level sorts across object relationships

- **RecordFilter.Matches** (RecordFilter.cs)
  - Purpose: Tests whether object passes filter
  - Inputs: LcmCache cache, int hvo (object ID)
  - Outputs: bool (true if object passes filter)
  - Notes: Abstract method implemented by subclasses (AndFilter, FilterBarCellFilter, etc.)

- **AndFilter** (RecordFilter.cs)
  - Purpose: Combines multiple filters with logical AND
  - Inputs: Array of RecordFilter instances
  - Outputs: bool (true if all filters match)
  - Notes: Used to combine filter bar cell filters or layered filters

- **FilterBarCellFilter** (RecordFilter.cs)
  - Purpose: Filter for one column in filter bar
  - Inputs: IStringFinder (value extractor), IMatcher (matching strategy)
  - Outputs: bool via Matches() method
  - Notes: Combines string finder and matcher for column-based filtering

- **IntMatcher** (IntMatcher.cs)
  - Purpose: Matches integer properties
  - Inputs: int target value
  - Outputs: bool (true if integer matches)
  - Notes: Used with IntFinder for integer property filtering

- **RangeIntMatcher** (IntMatcher.cs)
  - Purpose: Matches integers within range
  - Inputs: int min, int max
  - Outputs: bool (true if value in range)
  - Notes: Inclusive range matching

- **DateTimeMatcher** (DateTimeMatcher.cs)
  - Purpose: Matches date/time properties with various comparison modes
  - Inputs: DateMatchType (Before, After, On, Between, NotSet), DateTime value(s)
  - Outputs: bool (true if date matches criteria)
  - Notes: Supports single date or range comparisons

- **BadSpellingMatcher** (BadSpellingMatcher.cs)
  - Purpose: Identifies strings with spelling errors
  - Inputs: Spelling checker service
  - Outputs: bool (true if spelling errors detected)
  - Notes: Integrates with FieldWorks spelling infrastructure

- **RecordSorter** (RecordSorter.cs)
  - Purpose: Sorts filtered object lists
  - Inputs: List of objects, sort specifications
  - Outputs: Sorted list
  - Notes: Implements IComparer; supports progress reporting via IReportsSortProgress

- **MatchRangePair** struct (RecordFilter.cs)
  - Purpose: Represents text match range (start and end positions)
  - Inputs: int ich Min (start), int ichLim (end)
  - Outputs: Struct with match positions
  - Notes: Used in pattern matching and highlighting

## Entry Points
- Referenced as library in consuming projects for filtering and sorting
- RecordFilter subclasses instantiated for specific filter scenarios
- Matchers instantiated based on user filter bar selections or search criteria
- RecordSorter used to order filtered results before display

## Test Index
- **Test project**: FiltersTests (FiltersTests.csproj)
- **Test files**: DateTimeMatcherTests.cs, FindResultsSorterTests.cs, RangeIntMatcherTests.cs, TestPersistence.cs, WordformFiltersTests.cs
- **Run tests**: `dotnet test FiltersTests/FiltersTests.csproj` or Visual Studio Test Explorer
- **Coverage**: Unit tests for matchers, date/time filtering, sorting, filter persistence

## Usage Hints
- Extend RecordFilter to create custom filters; implement Matches() method
- Use AndFilter to combine multiple filters (e.g., filter bar + base filter)
- Implement IMatcher for custom matching strategies (pattern matching, custom logic)
- Implement IStringFinder to extract values from custom object properties
- Use RecordSorter with IReportsSortProgress for responsive long sorts
- Filter bar: XML cell definitions + StringFinder + Matcher → FilterBarCellFilter
- Check RecordFilter.cs header comments for design rationale and future plans (query-based filtering)

## Related Folders
- **Common/FwUtils/**: Utilities used by filters
- **Common/ViewsInterfaces/**: View interfaces (IVwCacheDa) used by filters
- **xWorks/**: Major consumer using filtering for data tree and browse searches
- **LexText/**: Uses filtering in lexicon searches and browse views

## References
- **Project files**: Filters.csproj (net462, OutputType=Library), FiltersTests/FiltersTests.csproj
- **Target frameworks**: .NET Framework 4.6.2 (net462)
- **Key dependencies**: SIL.LCModel, SIL.LCModel.Core.Text, SIL.LCModel.Core.WritingSystems, SIL.WritingSystems, SIL.Utils
- **Key C# files**: RecordFilter.cs (2751 lines), RecordSorter.cs (2268 lines), ManyOnePathSortItem.cs (463 lines), DateTimeMatcher.cs (309 lines), WordFormFilters.cs (289 lines), IntMatcher.cs (220 lines), BadSpellingMatcher.cs (183 lines), ExactLiteralMatcher.cs (136 lines), IntFinder.cs (102 lines), FindResultSorter.cs (75 lines), IManyOnePathSortItem.cs (29 lines), AssemblyInfo.cs (6 lines)
- **Designer files**: FiltersStrings.Designer.cs (270 lines)
- **Resources**: FiltersStrings.resx (localized strings)
- **Total lines of code**: 7101
- **Output**: Output/Debug/Filters.dll, Output/Release/Filters.dll
- **Namespace**: SIL.FieldWorks.Filters