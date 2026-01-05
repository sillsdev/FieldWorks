---
applyTo: "Src/Common/Filters/**"
name: "filters.instructions"
description: "Auto-generated concise instructions from COPILOT.md for Filters"
---

# Filters (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **RecordFilter** class (RecordFilter.cs, 2751 lines): Base class for in-memory filters
- Abstract class with Matches() method for object acceptance testing
- Subclasses: AndFilter (combines multiple filters), FilterBarCellFilter (filter bar cell), ProblemAnnotationFilter (annotation-specific)
- FilterChangeEventArgs: Event args for filter add/remove notifications
- **Matcher classes** (RecordFilter.cs): String matching strategies
- **IMatcher** interface: Contract for text matching (Matches() method)

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 001efe2bada829eaaf0f9945ca7676da4b443f38a42914c42118cb2430b057c7
status: draft
---

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
  - **EndMatcher**: Match s
