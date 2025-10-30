---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# CacheLight

## Purpose
Lightweight caching services used by core components. Provides efficient in-memory caching mechanisms for FieldWorks data access patterns.

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
