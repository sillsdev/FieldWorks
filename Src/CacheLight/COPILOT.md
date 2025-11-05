---
last-reviewed: 2025-10-31
last-reviewed-tree: bfd5c5b458798cefffa58e0522131c73d7590dc3c36de80ff9159ff667cf240e
status: draft
---

# CacheLight COPILOT summary

## Purpose
Provides lightweight, in-memory caching implementation for FieldWorks data access without requiring a full database connection. Includes MetaDataCache for model metadata (classes, fields, property types from XML model definitions) and RealDataCache for runtime object caching with support for ISilDataAccess and IVwCacheDa interfaces. Designed for testing scenarios, data import/export operations, and lightweight data access where full LCM is unnecessary.

## Architecture
C# class library (.NET Framework 4.6.2) with two primary cache implementations. MetaDataCache loads model definitions from XML files and provides IFwMetaDataCache interface. RealDataCache provides ISilDataAccess and IVwCacheDa interfaces for storing and retrieving object properties in memory using dictionaries keyed by HVO (object ID) and field ID combinations. Includes test project (CacheLightTests) with comprehensive unit tests.

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
- C# .NET Framework 4.6.2 (target framework: net462)
- System.Xml for XML model parsing
- System.Collections.Generic for dictionary-based caching
- System.Runtime.InteropServices for Marshal operations (COM interop support)
- NUnit for unit tests (CacheLightTests project)

## Dependencies

### Upstream (consumes)
- **SIL.LCModel.Core**: Core data model interfaces (IFwMetaDataCache, ISilDataAccess, CellarPropertyType)
- **SIL.LCModel.Utils**: Utility classes and interfaces
- **ViewsInterfaces**: View interfaces (IVwCacheDa, ITsString, ITsMultiString)
- **XMLUtils**: XML processing utilities
- **System.Xml**: XML parsing for model loading

### Downstream (consumed by)
- **CacheLightTests**: Comprehensive unit test project for CacheLight
- **Common/SimpleRootSite/SimpleRootSiteTests**: Uses CacheLight for testing
- Test scenarios requiring lightweight data access without full LCM database

## Interop & Contracts
Implements COM-compatible interfaces (ISilDataAccess, IVwCacheDa, IFwMetaDataCache) to support interop with native FieldWorks components. Uses Marshal operations for cross-boundary calls. RealDataCache implements IDisposable for proper cleanup.

## Threading & Performance
Single-threaded design; not thread-safe. All caches use Dictionary<TKey, TValue> for O(1) average-case lookups. Performance optimized for testing and lightweight data access; not designed for large-scale production data. MetaDataCache caches all class IDs (m_clids) to avoid repeated MDC queries. CheckWithMDC flag can be disabled for faster property access without metadata validation.

## Config & Feature Flags
- **RealDataCache.CheckWithMDC** (bool): When true, validates property access against metadata cache; disable for performance in trusted scenarios
- No external configuration files; behavior controlled by code and constructor parameters

## Build Information
- C# class library project: CacheLight.csproj (.NET Framework 4.6.2)
- Test project: CacheLightTests/CacheLightTests.csproj
- Output: CacheLight.dll, CacheLightTests.dll (to Output/Debug or Output/Release)
- Build via top-level FieldWorks.sln or: `msbuild CacheLight.csproj /p:Configuration=Debug`
- Run tests: `dotnet test CacheLightTests/CacheLightTests.csproj` or via Visual Studio Test Explorer
- Documentation: Debug builds produce CacheLight.xml documentation file

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
- **MetaDataCache.CreateMetaDataCache()**: Factory method to create and initialize metadata cache from XML model
- **RealDataCache constructor**: Creates empty in-memory cache; populate via property setters or RealCacheLoader
- **RealCacheLoader.LoadCache()**: Populates cache from XML data file
- Used in test projects via dependency injection or direct instantiation

## Test Index
- **Test project**: CacheLightTests (CacheLightTests.csproj)
- **Test files**: MetaDataCacheTests.cs (MetaDataCacheInitializationTests, MetaDataCacheFieldAccessTests, MetaDataCacheClassAccessTests), RealDataCacheTests.cs (RealDataCacheIVwCacheDaTests, RealDataCacheISilDataAccessTests)
- **Test data**: TestModel.xml (model definition), TestModel.xsd (schema)
- **Run tests**: `dotnet test CacheLightTests/CacheLightTests.csproj` or Visual Studio Test Explorer
- **Coverage**: Unit tests for metadata loading, property access, cache operations

## Usage Hints
- Use MetaDataCache.CreateMetaDataCache() to load model from XML file
- Use RealDataCache for in-memory object storage during testing or lightweight data operations
- Disable CheckWithMDC in RealDataCache for faster property access when metadata validation is unnecessary
- Use RealCacheLoader to populate RealDataCache from XML data files
- Use TsStringfactory.MakeString() to create formatted text (ITsString) for testing
- Check CacheLightTests for usage examples and patterns

## Related Folders
- **Common/ViewsInterfaces/**: Defines ITsString, IVwCacheDa interfaces implemented by CacheLight
- **Common/SimpleRootSite/**: Uses CacheLight in tests for lightweight data access
- **Utilities/XMLUtils/**: Provides XML utilities used by CacheLight

## References
- **Project files**: CacheLight.csproj (net462), CacheLightTests/CacheLightTests.csproj
- **Target frameworks**: .NET Framework 4.6.2 (net462)
- **Key dependencies**: SIL.LCModel.Core, SIL.LCModel.Utils, ViewsInterfaces, XMLUtils
- **Key C# files**: MetaDataCache.cs (990 lines), RealCacheLoader.cs (480 lines), RealDataCache.cs (2135 lines), TsMultiString.cs (65 lines), TsStringfactory.cs (176 lines), AssemblyInfo.cs (6 lines)
- **Test files**: CacheLightTests/MetaDataCacheTests.cs, CacheLightTests/RealDataCacheTests.cs
- **Data contracts**: CacheLightTests/TestModel.xml (model definition), CacheLightTests/TestModel.xsd (schema), CacheLightTests/Properties/Resources.resx
- **Total lines of code**: 3852 (main library), plus test code
- **Output**: Output/Debug/CacheLight.dll, Output/Debug/CacheLight.xml (documentation)
- **Namespace**: SIL.FieldWorks.CacheLight