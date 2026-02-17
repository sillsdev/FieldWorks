---
last-reviewed: 2025-10-31
last-reviewed-tree: 8ca32c9179ae611e3b86361c36a4e081c7bf39be31ec2a0aa462db5ffd3659e6
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

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
Not thread-safe; use separate instance per thread. Compare() via ICU culturally correct. get_SortKeyVariant() for efficient repeated comparisons.

## Config & Feature Flags
Locale via Open(bstrLocale) using BCP 47 format. LgCollatingOptions parameter (not fully implemented). Dispose pattern via Close().

## Build Information
C# library (net48). Build via `msbuild ManagedLgIcuCollator.csproj`. Output: ManagedLgIcuCollator.dll. COM-visible.

## Interfaces and Data Models
ILgCollatingEngine implementation. Methods: Open(), Close(), Compare(), get_SortKeyVariant(), CompareVariant(). Sort keys (byte[]) for efficient comparisons. COM GUID: e771361c-ff54-4120-9525-98a0b7a9accf.

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
ManagedLgIcuCollatorTests project. Tests collator initialization, Compare() for various locales, sort key generation, locale fallback, null handling, dispose pattern.

## Usage Hints
Open(locale) with BCP 47 codes. Compare() for single comparisons. get_SortKeyVariant() for sorting large lists efficiently. Always Close() to release ICU resources. LgCollatingOptions not currently applied.

## Related Folders
- **Common/ViewsInterfaces/**: ILgCollatingEngine interface
- **LexText/Lexicon/**: Lexicon sorting
- **xWorks/**: Browse views

## References
2 C# files (~180 lines). Key: LgIcuCollator.cs. COM GUID: e771361c-ff54-4120-9525-98a0b7a9accf. See `.cache/copilot/diff-plan.json` for file listings.
