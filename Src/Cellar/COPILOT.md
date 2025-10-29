# Cellar

## Purpose
Core data model and persistence layer (also known as LCM - FieldWorks Language and Culture Model). This is the foundational data model for FieldWorks, handling XML serialization and data object management.

## Key Components
- **FwXml.cpp/h** - XML processing and serialization for FieldWorks data
- **FwXmlString.cpp** - String handling in XML contexts

## Technology Stack
- C++ native code
- XML processing and serialization
- Data model persistence

## Dependencies
- Depends on: Kernel (low-level infrastructure), Generic (utilities)
- Used by: All data-driven components across FieldWorks (FdoUi, LexText, xWorks)

## Build Information
- Native C++ library
- Built as part of the larger FieldWorks solution
- Critical foundational component

## Entry Points
- Provides data model base classes and XML serialization
- Core persistence layer for all FieldWorks data

## Related Folders
- **DbExtend/** - Extends database schema and data model capabilities
- **CacheLight/** - Provides caching for data model objects
- **FdoUi/** - UI components for FieldWorks Data Objects built on Cellar
- **LCMBrowser/** - Browser tool for exploring the LCM data model
- **Common/FieldWorks/** - Contains higher-level data access built on Cellar
