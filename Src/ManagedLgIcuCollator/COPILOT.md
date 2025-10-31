---
last-reviewed: 2025-10-31
last-verified-commit: 3d625c3
status: reviewed
---

# ManagedLgIcuCollator

## Purpose
Managed C# implementation of ILgCollatingEngine for ICU-based collation. Direct port of C++ LgIcuCollator providing locale-aware string comparison and sort key generation. Enables culturally correct alphabetical ordering for multiple writing systems by wrapping Icu.Net Collator with FieldWorks-specific ILgCollatingEngine interface. Used throughout FLEx for sorting lexicon entries, wordforms, and linguistic data according to writing system collation rules.

## Architecture
C# library (net462) with 2 source files (~180 lines total). Single class ManagedLgIcuCollator implementing IL gCollatingEngine COM interface, using Icu.Net library (NuGet) for ICU collation access. Marked [Serializable] and [ComVisible] for COM interop.

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

## Dependencies
- **External**: Icu.Net (NuGet package wrapping ICU C++ libraries), SIL.LCModel.Core (ILgWritingSystemFactory, ITsString, ArrayPtr, LgCollatingOptions enum), Common/ViewsInterfaces (ILgCollatingEngine interface), SIL.LCModel.Utils
- **Internal (upstream)**: ViewsInterfaces (ILgCollatingEngine interface contract)
- **Consumed by**: Components needing locale-aware sorting - LexText/Lexicon (lexicon entry lists), LexText/Interlinear (wordform lists), xWorks (browse views with sorted columns), Common/RootSite (Views rendering with sorted data), any UI displaying writing-system-specific alphabetical lists

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ManagedLgIcuCollator.csproj` or `dotnet build` (from FW.sln)
- Output: ManagedLgIcuCollator.dll
- Dependencies: Icu.Net NuGet package, LCModel.Core, ViewsInterfaces
- COM attributes: [ComVisible], [Serializable], [ClassInterface(ClassInterfaceType.None)], GUID for COM registration

## Test Information
- Test project: ManagedLgIcuCollatorTests
- Test file: ManagedLgIcuCollatorTests.cs
- Run: `dotnet test` or Test Explorer in Visual Studio
- Test coverage: Collator initialization, Compare() for various locales, sort key generation, sort key comparison, locale fallback behavior, null handling, dispose patterns

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
- **Target framework**: net462
