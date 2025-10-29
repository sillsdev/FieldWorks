---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# CacheLight

## Purpose
Lightweight caching services providing efficient in-memory data access for FieldWorks components.
Implements MetaDataCache for model metadata and RealDataCache for runtime object caching.
Designed to optimize data access patterns by reducing database queries and providing fast lookup
of frequently accessed linguistic data objects. Critical for application performance.

## Key Components
### Key Classes
- **MetaDataCache**
- **RealCacheLoader**
- **RealDataCache**
- **RealDataCacheBase**
- **RealDataCacheIVwCacheDaTests**
- **RealDataCacheISilDataAccessTests**
- **MetaDataCacheInitializationTests**
- **MetaDataCacheBase**
- **MetaDataCacheFieldAccessTests**
- **MetaDataCacheClassAccessTests**

### Key Interfaces
- **IRealDataCache**

## Technology Stack
- C# .NET
- In-memory caching strategies
- Data access optimization

## Dependencies
- Depends on: Common utilities, data model interfaces
- Used by: Core data access layers, LCM (Language and Culture Model)

## Build Information
- C# class library project
- Contains comprehensive unit tests
- Build with MSBuild or Visual Studio

## Entry Points
- Provides caching services and interfaces
- Integrated into data access pipelines

## Related Folders
- **Cellar/** - Core data model that benefits from CacheLight services
- **Common/** - Provides utility infrastructure used by CacheLight
- **DbExtend/** - Database extensions that may use caching

## Code Evidence
*Analysis based on scanning 8 source files*

- **Classes found**: 12 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.CacheLight, SIL.FieldWorks.CacheLightTests

## Interfaces and Data Models

- **IRealDataCache** (interface)
  - Path: `RealDataCache.cs`
  - Public interface definition

- **MetaDataCache** (class)
  - Path: `MetaDataCache.cs`
  - Public class implementation

- **MetaDataCacheBase** (class)
  - Path: `CacheLightTests/MetaDataCacheTests.cs`
  - Public class implementation

- **RealCacheLoader** (class)
  - Path: `RealCacheLoader.cs`
  - Public class implementation

- **RealDataCache** (class)
  - Path: `RealDataCache.cs`
  - Public class implementation

- **RealDataCacheBase** (class)
  - Path: `CacheLightTests/RealDataCacheTests.cs`
  - Public class implementation

- **Resources** (class)
  - Path: `CacheLightTests/Properties/Resources.Designer.cs`
  - Public class implementation

## References

- **Project files**: CacheLight.csproj, CacheLightTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, MetaDataCache.cs, MetaDataCacheTests.cs, RealCacheLoader.cs, RealDataCache.cs, RealDataCacheTests.cs, Resources.Designer.cs, TsMultiString.cs, TsStringfactory.cs
- **XML data/config**: TestModel.xml
- **Source file count**: 9 files
- **Data file count**: 3 files
