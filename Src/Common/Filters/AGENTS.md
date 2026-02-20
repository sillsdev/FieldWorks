---
last-reviewed: 2025-10-31
last-reviewed-tree: 45612dabc22b994a18b408a873f35423d816384b80bad319f017cc946dcbefb9
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - upstream-consumes
  - downstream-consumed-by
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Filters COPILOT summary

## Purpose
Data filtering and sorting infrastructure for searchable data views throughout FieldWorks. Implements matcher types (IntMatcher, RangeIntMatcher, ExactMatcher, BeginMatcher, RegExpMatcher, DateTimeMatcher, BadSpellingMatcher) and filtering logic (RecordFilter, AndFilter, ProblemAnnotationFilter, FilterBarCellFilter) for narrowing data sets. Provides sorting infrastructure (RecordSorter, FindResultSorter, ManyOnePathSortItem) for organizing filtered results. Essential for browse views, search functionality, filtered list displays, and filter bar UI components in FieldWorks applications.

## Architecture
C# class library (.NET Framework 4.8.x) with filtering and sorting components. RecordFilter base class provides in-memory filtering using IMatcher implementations and IStringFinder interfaces to extract and match values from objects. Filter bar support via FilterBarCellFilter combines matchers with string finders for column-based filtering in browse views. Sorting via RecordSorter with progress reporting (IReportsSortProgress) and IManyOnePathSortItem for complex hierarchical sorts. Test project (FiltersTests) validates matcher behavior, sorting, and persistence.

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
- C# .NET Framework 4.8.x (target framework: net48)
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
Single-threaded in-memory filtering/sorting. RecordSorter supports progress reporting for responsiveness.

## Config & Feature Flags
Filter definitions persist to/from XML. Filter bar behavior configurable via XML cell definitions.

## Build Information
C# library (net48). Build via `msbuild Filters.csproj`. Output: Filters.dll.

## Interfaces and Data Models
IMatcher (ExactMatcher, BeginMatcher, RegExpMatcher, etc.), IStringFinder (extract values), RecordFilter base class, AndFilter, FilterBarCellFilter. Matchers: IntMatcher, RangeIntMatcher, DateTimeMatcher, BadSpellingMatcher. RecordSorter for sorting.

## Entry Points
Library for filtering/sorting. RecordFilter subclasses for filter scenarios. Matchers based on filter bar selections. RecordSorter for ordering results.

## Test Index
FiltersTests project. Tests matchers, date/time filtering, sorting, persistence.

## Usage Hints
Extend RecordFilter (implement Matches()). Use AndFilter to combine filters. Implement IMatcher for custom matching. RecordSorter with IReportsSortProgress for long sorts.

## Related Folders
- **xWorks/**: Data tree and browse filtering
- **LexText/**: Lexicon searches

## References
- **Project files**: Filters.csproj (net48, OutputType=Library), FiltersTests/FiltersTests.csproj
- **Target frameworks**: .NET Framework 4.8.x (net48)
- **Key dependencies**: SIL.LCModel, SIL.LCModel.Core.Text, SIL.LCModel.Core.WritingSystems, SIL.WritingSystems, SIL.Utils
- **Key C# files**: RecordFilter.cs (2751 lines), RecordSorter.cs (2268 lines), ManyOnePathSortItem.cs (463 lines), DateTimeMatcher.cs (309 lines), WordFormFilters.cs (289 lines), IntMatcher.cs (220 lines), BadSpellingMatcher.cs (183 lines), ExactLiteralMatcher.cs (136 lines), IntFinder.cs (102 lines), FindResultSorter.cs (75 lines), IManyOnePathSortItem.cs (29 lines), AssemblyInfo.cs (6 lines)
- **Designer files**: FiltersStrings.Designer.cs (270 lines)
- **Resources**: FiltersStrings.resx (localized strings)
- **Total lines of code**: 7101
- **Output**: Output/Debug/Filters.dll, Output/Release/Filters.dll
- **Namespace**: SIL.FieldWorks.Filters
