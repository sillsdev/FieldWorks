---
applyTo: "Src/ManagedLgIcuCollator/**"
name: "managedlgicucollator.instructions"
description: "Auto-generated concise instructions from COPILOT.md for ManagedLgIcuCollator"
---

# ManagedLgIcuCollator (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **ManagedLgIcuCollator**: Implements ILgCollatingEngine for ICU-based collation. Wraps Icu.Net Collator instance, manages locale initialization via Open(bstrLocale), provides Compare() for string comparison, get_SortKeyVariant() for binary sort key generation, CompareVariant() for sort key comparison. Implements lazy collator creation via EnsureCollator(). Marked with COM GUID e771361c-ff54-4120-9525-98a0b7a9accf for COM interop.
- Inputs: ILgWritingSystemFactory (for writing system metadata), locale string (e.g., "en-US", "fr-FR")
- Methods:
- Open(string bstrLocale): Initializes collator for given locale
- Close(): Disposes collator
- Compare(string val1, string val2, LgCollatingOptions): Returns -1/0/1 for val1 < = > val2

## Example (from summary)

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
  - P
