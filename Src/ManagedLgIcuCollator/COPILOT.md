---
last-reviewed: 2025-10-31
last-reviewed-tree: 7b43a1753527af6dabab02b8b1fed66cfd6083725f4cd079355f933d9ae58e11
status: reviewed
---

# ManagedLgIcuCollator

## Purpose
Managed C# implementation of ILgCollatingEngine for ICU-based collation. Direct port of C++ LgIcuCollator providing locale-aware string comparison and sort key generation. Enables culturally correct alphabetical ordering for multiple writing systems by wrapping Icu.Net Collator with FieldWorks-specific ILgCollatingEngine interface. Used throughout FLEx for sorting lexicon entries, wordforms, and linguistic data according to writing system collation rules.

## Architecture
C# library (net48) with 2 source files (~180 lines total). Single class ManagedLgIcuCollator implementing IL gCollatingEngine COM interface, using Icu.Net library (NuGet) for ICU collation access. Marked [Serializable] and [ComVisible] for COM interop.

## Key Components

### Collation Engine
- **ManagedLgIcuCollator**: Implements ILgCollatingEngine for ICU-based collation. Wraps Icu.Net Collator instance, manages locale initialization via Open(bstrLocale), provides Compare() for string comparison, get_SortKeyVariant() for binary sort key generation, CompareVariant() for sort key comparison. Implements lazy collator creation via EnsureCollator(). Marked with COM GUID e771361c-ff54-4120-9525-98a0b7a9accf for COM interop.
  - Inputs: ILgWritingSystemFactory (for writing system metadata), locale string (e.g., "en-US", "fr-FR")
  - Methods:
    - Open(string bstrLocale): Initializes collator for given locale
    - Close(): Disposes collator
    - Compare(string val1, string val2, LgCollatingOptions): Returns -1/0/1 for val1 < = > val2
    - get_SortKeyVariant(string value, LgCollatingOptions): Returns byte[] sort key
    - CompareVariant(object key1, object key2, LgCollatingOptions): Compares byte[] sort keys
    - get_SortKey(string, LgCollatingOptions): Not implemented (throws)
    - SortKeyRgch(...): Not implemented (throws)
  - Properties: WritingSystemFactory (ILgWritingSystemFactory)
  - Internal: m_collator (Icu.Net Collator), m_stuLocale (locale string), m_qwsf (ILgWritingSystemFactory)
  - Notes: LgCollatingOptions parameter (e.g., IgnoreCase, IgnoreDiacritics) currently not used in implementation

### Sort Key Comparison
- **CompareVariant()**: Byte-by-byte comparison of ICU sort keys. Handles null keys (null < non-null), compares byte arrays element-wise, shorter key considered less if all matching bytes equal. Efficient for repeated comparisons (generate sort key once, compare many times).

### Lazy Initialization
- **EnsureCollator()**: Creates Icu.Net Collator on first use. Converts FieldWorks locale string to ICU Locale, calls Collator.Create(icuLocale, Fallback.FallbackAllowed) allowing locale fallback (e.g., "en-US" falls back to "en" if specific variant unavailable).

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Key libraries**:
  - Icu.Net (NuGet package wrapping ICU C++ libraries for collation)
  - SIL.LCModel.Core (ILgWritingSystemFactory, LgCollatingOptions)
  - Common/ViewsInterfaces (ILgCollatingEngine interface)
  - SIL.LCModel.Utils
- **COM interop**: Marked [ComVisible], [Serializable] with COM GUID for legacy interop
- **Native dependencies**: ICU libraries (accessed via Icu.Net wrapper)

## Dependencies
- **External**: Icu.Net (NuGet package wrapping ICU C++ libraries), SIL.LCModel.Core (ILgWritingSystemFactory, ITsString, ArrayPtr, LgCollatingOptions enum), Common/ViewsInterfaces (ILgCollatingEngine interface), SIL.LCModel.Utils
- **Internal (upstream)**: ViewsInterfaces (ILgCollatingEngine interface contract)
- **Consumed by**: Components needing locale-aware sorting - LexText/Lexicon (lexicon entry lists), LexText/Interlinear (wordform lists), xWorks (browse views with sorted columns), Common/RootSite (Views rendering with sorted data), any UI displaying writing-system-specific alphabetical lists

## Interop & Contracts
- **COM interface**: ILgCollatingEngine from ViewsInterfaces
  - COM GUID: e771361c-ff54-4120-9525-98a0b7a9accf
  - Attributes: [ComVisible(true)], [Serializable], [ClassInterface(ClassInterfaceType.None)]
  - Purpose: Expose collation to COM consumers (legacy C++ Views code)
- **Data contracts**:
  - Open(string bstrLocale): Initialize collator with locale (e.g., "en-US")
  - Compare(string val1, string val2, LgCollatingOptions): Return -1/0/1 for ordering
  - get_SortKeyVariant(string, LgCollatingOptions): Return byte[] sort key
  - CompareVariant(object key1, object key2, LgCollatingOptions): Compare byte[] sort keys
- **ICU wrapper**: Uses Icu.Net Collator class wrapping native ICU C++ libraries
  - Locale fallback: "en-US" → "en" → "root" if specific variant unavailable
- **Options**: LgCollatingOptions enum (IgnoreCase, IgnoreDiacritics, etc.)
  - Note: Currently not fully implemented in this class; passed through but not applied to ICU Collator
- **Sort key format**: Byte arrays generated by ICU for efficient repeated comparisons

## Threading & Performance
- **Thread safety**: Not thread-safe; each thread should use its own ManagedLgIcuCollator instance
  - Icu.Net Collator is not thread-safe
  - No internal synchronization in ManagedLgIcuCollator
- **Performance characteristics**:
  - Compare(): Direct string comparison via ICU, culturally correct but slower than ordinal
  - get_SortKeyVariant(): One-time cost to generate sort key, enables fast repeated comparisons
  - CompareVariant(): Byte-by-byte comparison of pre-generated sort keys, faster than Compare() for multiple comparisons
- **Optimization pattern**: Generate sort keys once, compare many times (e.g., sorting large lists)
- **Lazy initialization**: EnsureCollator() creates Collator on first use, amortizes initialization cost
- **Memory**: Sort keys consume memory (variable size based on string length/locale complexity)
- **No caching**: No internal cache of sort keys; caller responsible for caching if needed

## Config & Feature Flags
- **Locale selection**: Configured via Open(bstrLocale) method
  - Locale string format: BCP 47 (e.g., "en-US", "fr-FR", "zh-CN")
  - Fallback enabled: ICU automatically falls back to less specific locale if exact match unavailable
- **LgCollatingOptions parameter**: Enum for collation options
  - Values: IgnoreCase, IgnoreDiacritics, IgnoreKanaType, IgnoreWidth, etc.
  - Current limitation: Not fully implemented; passed to methods but not applied to ICU Collator
  - Future enhancement: Map LgCollatingOptions to ICU Collator strength/attributes
- **Writing system factory**: ILgWritingSystemFactory provides metadata (not currently used in collation logic)
- **No global state**: Each ManagedLgIcuCollator instance is independent
- **Dispose pattern**: Implements IDisposable; Close() releases ICU Collator resources

## Build Information
- Project type: C# class library (net48)
- Build: `msbuild ManagedLgIcuCollator.csproj` or `dotnet build` (from FieldWorks.sln)
- Output: ManagedLgIcuCollator.dll
- Dependencies: Icu.Net NuGet package, LCModel.Core, ViewsInterfaces
- COM attributes: [ComVisible], [Serializable], [ClassInterface(ClassInterfaceType.None)], GUID for COM registration

## Interfaces and Data Models

### Interfaces
- **ILgCollatingEngine** (path: Src/Common/ViewsInterfaces/)
  - Purpose: Abstract collation interface for locale-aware string comparison
  - Inputs: strings (for comparison), locale (for initialization), LgCollatingOptions (collation behavior)
  - Outputs: int (-1/0/1 for comparison), byte[] (sort keys)
  - Methods: Open(), Close(), Compare(), get_SortKeyVariant(), CompareVariant()
  - Notes: COM-visible interface, consumed by Views and lexicon sorting code

- **IDisposable**
  - Purpose: Resource cleanup pattern
  - Implementation: Close() disposes Icu.Net Collator, releases ICU resources

### Data Models
- **Sort keys** (byte arrays)
  - Purpose: Binary representation for efficient repeated comparisons
  - Shape: Variable-length byte[] generated by ICU
  - Consumers: CompareVariant() for sort key comparison
  - Notes: Shorter keys for simple scripts, longer for complex collation (diacritics, ligatures)

- **LgCollatingOptions** (enum from LCModel.Core)
  - Purpose: Collation behavior flags
  - Values: IgnoreCase, IgnoreDiacritics, IgnoreKanaType, IgnoreWidth
  - Current limitation: Not applied to ICU Collator in this implementation

### COM Contracts
- **GUID**: e771361c-ff54-4120-9525-98a0b7a9accf
- **ClassInterface**: None (interface-only COM exposure)
- **Serializable**: Marked for .NET remoting/serialization (though not typically serialized)

## Entry Points
- **Instantiation**: Direct construction via `new ManagedLgIcuCollator()`
  - Typically created by writing system or sort logic
  - One instance per writing system/locale
- **Initialization**: Call Open(locale) before first use
  - Example: `collator.Open("en-US")` for U.S. English collation
- **Usage pattern**:
  1. Create: `var collator = new ManagedLgIcuCollator()`
  2. Initialize: `collator.Open("fr-FR")`
  3. Compare: `int result = collator.Compare(str1, str2, options)`
  4. Or generate sort keys: `byte[] key = collator.get_SortKeyVariant(str, options)`
  5. Cleanup: `collator.Close()` or `collator.Dispose()`
- **Common consumers**:
  - Lexicon views: Sorting lexicon entries by headword
  - Concordance: Sorting wordforms alphabetically
  - Browse views: Sortable columns in xWorks browse views
  - Writing system UI: Any alphabetically sorted lists for a specific writing system
- **COM access**: Can be instantiated via COM from C++ Views code using GUID

## Test Index
- **Test project**: ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.csproj
- **Test file**: ManagedLgIcuCollatorTests.cs
- **Test coverage**:
  - Collator initialization: Open() with various locales
  - String comparison: Compare() for different locales (en-US, fr-FR, zh-CN)
  - Sort key generation: get_SortKeyVariant() produces non-null byte arrays
  - Sort key comparison: CompareVariant() matches Compare() results
  - Locale fallback: Specific locales fall back to less specific (e.g., "en-US" → "en")
  - Null handling: Null strings and null sort keys handled correctly
  - Dispose pattern: Close() cleans up resources, subsequent calls safe
  - Error cases: Invalid locales, unopened collator
- **Test approach**: Unit tests with known comparison outcomes for various locales
- **Test runners**:
  - Visual Studio Test Explorer
  - `dotnet test` (if SDK-style)
  - Via FieldWorks.sln top-level build
- **Test data**: Inline test strings in various scripts (Latin, Cyrillic, Chinese, etc.)

## Usage Hints
- **Typical usage**:
  ```csharp
  var collator = new ManagedLgIcuCollator();
  collator.Open("en-US");  // Initialize for U.S. English
  int result = collator.Compare("apple", "banana", LgCollatingOptions.None);  // result = -1
  collator.Close();  // Cleanup
  ```
- **Sort key optimization**:
  ```csharp
  // For sorting large lists, generate sort keys once:
  var entries = GetLexiconEntries();
  var sortedEntries = entries
      .Select(e => new { Entry = e, SortKey = collator.get_SortKeyVariant(e.Headword, options) })
      .OrderBy(x => x.SortKey, new SortKeyComparer())
      .Select(x => x.Entry)
      .ToList();
  ```
- **Locale selection**: Use BCP 47 locale codes (en, en-US, fr-FR, zh-CN, etc.)
  - ICU handles fallback automatically
  - "root" locale is universal fallback (Unicode order)
- **Options limitation**: LgCollatingOptions not currently applied; use default ICU collation strength
  - Future enhancement: Map options to ICU Collator attributes
- **Thread safety**: Create one collator per thread, or synchronize access externally
- **Dispose pattern**: Always call Close() or use `using` statement to release ICU resources
- **COM interop**: C++ Views code can create via GUID e771361c-ff54-4120-9525-98a0b7a9accf
- **Common pitfalls**:
  - Forgetting to call Open() before Compare() (will throw)
  - Reusing collator for multiple locales without Close()/Open() cycle
  - Assuming LgCollatingOptions are applied (currently no-op)
  - Not disposing collator (leaks ICU resources)

## Related Folders
- **Common/ViewsInterfaces/**: Defines ILgCollatingEngine interface
- **LexText/Lexicon/**: Uses collation for lexicon entry sorting
- **xWorks/**: Uses collation in browse views with sortable columns
- **Common/RootSite/**: Views rendering may use collation for sorted displays
- **Lib/**: ICU native libraries (accessed via Icu.Net wrapper)

## References
- **Source files**: 2 C# files (~180 lines): LgIcuCollator.cs, ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.cs
- **Project files**: ManagedLgIcuCollator.csproj, ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.csproj
- **Key class**: ManagedLgIcuCollator (implements ILgCollatingEngine, IDisposable)
- **Key interface**: ILgCollatingEngine (from ViewsInterfaces)
- **NuGet dependencies**: Icu.Net (ICU collation wrapper)
- **COM GUID**: e771361c-ff54-4120-9525-98a0b7a9accf
- **Namespace**: SIL.FieldWorks.Language
- **Target framework**: net48

## Auto-Generated Project and File References
- Project files:
  - Src/ManagedLgIcuCollator/ManagedLgIcuCollator.csproj
  - Src/ManagedLgIcuCollator/ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.csproj
- Key C# files:
  - Src/ManagedLgIcuCollator/LgIcuCollator.cs
  - Src/ManagedLgIcuCollator/ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.cs
## Test Information
- Test project: ManagedLgIcuCollatorTests
- Test file: ManagedLgIcuCollatorTests.cs
- Run: `dotnet test` or Test Explorer in Visual Studio
- Test coverage: Collator initialization, Compare() for various locales, sort key generation, sort key comparison, locale fallback behavior, null handling, dispose patterns
