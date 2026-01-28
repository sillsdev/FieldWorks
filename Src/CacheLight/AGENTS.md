---
last-reviewed: 2025-11-21
last-reviewed-tree: 895b49e9397474fc7d6d9b82898935d6dceeec75a28198fbba5e2f43f5f73cfa
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# CacheLight COPILOT summary

## Purpose
Provides lightweight, in-memory caching implementation for FieldWorks data access without requiring a full database connection. Includes MetaDataCache for model metadata (classes, fields, property types from XML model definitions) and RealDataCache for runtime object caching with support for ISilDataAccess and IVwCacheDa interfaces. Designed for testing scenarios, data import/export operations, and lightweight data access where full LCM is unnecessary.

## Architecture
C# class library (.NET Framework 4.8.x) with two primary cache implementations. MetaDataCache loads model definitions from XML files and provides IFwMetaDataCache interface. RealDataCache provides ISilDataAccess and IVwCacheDa interfaces for storing and retrieving object properties in memory using dictionaries keyed by HVO (object ID) and field ID combinations. Includes test project (CacheLightTests) with comprehensive unit tests.

## Key Components
- **MetaDataCache** class (MetaDataCache.cs, 990 lines): XML-based metadata cache
  - Implements IFwMetaDataCache for model metadata queries
  - Loads class/field definitions from XML model files
  - `InitXml()`: Parses XML model files into internal dictionaries
  - `CreateMetaDataCache()`: Factory method for creating initialized instances
  - `MetaClassRec`, `MetaFieldRec`: Internal structs storing class and field metadata
  - Dictionaries: m_metaClassRecords (clid→class), m_nameToClid (name→clid), m_metaFieldRecords (flid→field), m_nameToFlid (name→flid)
  - Supports queries for class names, field names, property types, inheritance, signatures
- **RealDataCache** class (RealDataCache.cs, 2135 lines): In-memory object property cache
  - Implements IRealDataCache (combines ISilDataAccess, IVwCacheDa, IStructuredTextDataAccess)
  - Stores objects and properties in typed dictionaries (int, bool, long, string, ITsString, byte[], vector)
  - `HvoFlidKey`, `HvoFlidWSKey`: Composite keys for cache lookups (object ID + field ID + optional writing system)
  - Dictionary caches: m_basicObjectCache, m_extendedKeyCache, m_basicITsStringCache, m_basicByteArrayCache, m_basicStringCache, m_guidCache, m_guidToHvo, m_intCache, m_longCache, m_boolCache, m_vectorCache
  - Supports atomic, sequence, collection, and reference properties
  - `get_*PropCount()`, `get_*Prop()`, `Set*Prop()`: Property accessor methods
  - `CacheStringAlt()`, `CacheStringFields()`: Multi-string (multilingual) property support
  - `MakeNewObject()`: Allocates new HVO for objects
  - `CheckWithMDC`: Optional metadata validation flag
- **RealCacheLoader** class (RealCacheLoader.cs, 480 lines): Populates cache from XML data
  - Loads object data from XML files into RealDataCache
  - Handles various property types (atomic, sequence, collection, reference)
  - Supports TsString (formatted text) and multi-string properties
- **TsStringfactory** class (TsStringfactory.cs, 176 lines): Factory for creating ITsString instances
  - `MakeString()`: Creates ITsString from string and writing system
  - `MakeStringRgch()`: Creates ITsString from character array
  - Minimal ITsStrFactory implementation for testing
- **TsMultiString** class (TsMultiString.cs, 65 lines): Simple multi-string implementation
  - Stores string values per writing system
  - Implements ITsMultiString interface for testing
- **IRealDataCache** interface (RealDataCache.cs): Combined interface for RealDataCache
  - Extends ISilDataAccess, IVwCacheDa, IStructuredTextDataAccess, IDisposable
  - Adds properties: ParaContentsFlid, ParaPropertiesFlid, TextParagraphsFlid (for structured text support)

## Technology Stack
C# .NET Framework 4.8.x, System.Xml for XML parsing, NUnit for tests.

## Dependencies
Consumes: SIL.LCModel.Core (IFwMetaDataCache, ISilDataAccess), ViewsInterfaces (IVwCacheDa, ITsString), XMLUtils. Used by: CacheLightTests, SimpleRootSiteTests, testing scenarios requiring lightweight data access.

## Interop & Contracts
COM-compatible interfaces (ISilDataAccess, IVwCacheDa, IFwMetaDataCache) for native Views interop.

## Threading & Performance
Single-threaded; Dictionary<TKey, TValue> caches for O(1) lookups. CheckWithMDC flag can be disabled for faster property access without metadata validation.

## Config & Feature Flags
RealDataCache.CheckWithMDC (bool): validates property access against metadata; disable for performance in trusted scenarios.

## Build Information
CacheLight.csproj (net48), output: CacheLight.dll. Tests: `dotnet test CacheLightTests/`.

## Interfaces and Data Models

- **IFwMetaDataCache** (implemented by MetaDataCache)
  - Purpose: Provides read-only access to model metadata (classes, fields, property types)
  - Inputs: Class IDs, field IDs, class/field names
  - Outputs: Metadata queries (class names, field types, inheritance, signatures)
  - Notes: Loaded from XML model files via InitXml()

- **IRealDataCache** (implemented by RealDataCache)
  - Purpose: Combined interface for in-memory data cache supporting multiple data access patterns
  - Inputs: HVO (object ID), flid (field ID), ws (writing system), property values
  - Outputs: Cached property values, object data
  - Notes: Extends ISilDataAccess, IVwCacheDa, IStructuredTextDataAccess

- **MetaDataCache.InitXml** (MetaDataCache.cs)
  - Purpose: Parses XML model file to populate metadata cache
  - Inputs: string mainModelPathname (path to XML model file), bool loadRelatedFiles
  - Outputs: Populates internal dictionaries with class/field metadata
  - Notes: Parses &lt;class&gt; and &lt;field&gt; elements; supports inheritance and abstract classes

- **RealDataCache property accessors** (RealDataCache.cs)
  - Purpose: Get/Set properties of various types (int, bool, long, string, ITsString, byte[], vectors)
  - Inputs: HVO (object ID), flid (field ID), ws (writing system for multi-string properties)
  - Outputs: Property values or void (for setters)
  - Notes: Methods follow naming pattern: get_*Prop(), Set*Prop(), Cache*Prop()

- **RealDataCache.MakeNewObject** (RealDataCache.cs)
  - Purpose: Allocates new object ID (HVO) and registers class ID
  - Inputs: int clid (class ID), int hvoOwner (owner object), int flid (owning property)
  - Outputs: int hvo (new object ID)
  - Notes: Increments m_nextHvo; stores object in m_basicObjectCache

- **RealCacheLoader.LoadCache** (RealCacheLoader.cs)
  - Purpose: Populates RealDataCache from XML data file
  - Inputs: RealDataCache cache, string xmlDataPath, MetaDataCache mdc
  - Outputs: void (side effect: populates cache)
  - Notes: Parses &lt;rt&gt; elements (objects) and nested property elements

- **TsStringfactory.MakeString** (TsStringfactory.cs)
  - Purpose: Creates ITsString instance from string and writing system
  - Inputs: string text, int ws (writing system ID)
  - Outputs: ITsString (formatted text object)
  - Notes: Minimal implementation for testing; full implementation in other components

- **XML Model Format** (TestModel.xml in CacheLightTests)
  - Purpose: Defines data model structure (classes, fields, types, inheritance)
  - Shape: &lt;ModelDef&gt; root with &lt;class&gt; and &lt;field&gt; elements
  - Consumers: MetaDataCache.InitXml() parses into m_metaClassRecords and m_metaFieldRecords
  - Notes: Field types use CellarPropertyType enum (OwningAtomic, ReferenceSequence, etc.)

## Entry Points
MetaDataCache.CreateMetaDataCache() factory, RealDataCache constructor, RealCacheLoader.LoadCache().

## Test Index
CacheLightTests project: MetaDataCacheTests.cs, RealDataCacheTests.cs. Run: `dotnet test CacheLightTests/`.

## Usage Hints
Use MetaDataCache.CreateMetaDataCache() for XML model loading, RealDataCache for in-memory testing, RealCacheLoader for XML data population. Disable CheckWithMDC for faster access in trusted scenarios. See CacheLightTests for patterns.

## Related Folders
Common/ViewsInterfaces (ITsString, IVwCacheDa), Common/SimpleRootSite (uses in tests), Utilities/XMLUtils.

## References
CacheLight.csproj (net48), 3.8K lines. Key files: RealDataCache.cs (2.1K), MetaDataCache.cs (990), RealCacheLoader.cs (480). See `.cache/copilot/diff-plan.json` for file inventory.
