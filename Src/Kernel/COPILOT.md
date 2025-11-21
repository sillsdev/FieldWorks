---
last-reviewed: 2025-10-31
last-reviewed-tree: c8147e4135449a80e746c376e1cf2012eec0bd4845459fff1a1cd3e89825bf9b
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Kernel COPILOT summary

## Purpose
Low-level core constants and COM infrastructure for FieldWorks native code. Defines CellarConstants enum (class IDs kclid*, field IDs kflid* for data model), CellarModuleDefns enum (Cellar property types kcpt*), COM GUIDs, and proxy/stub DLL infrastructure. CellarConstants.vm.h is NVelocity template generating constants from MasterLCModel.xml (LcmGenerate build task). CellarBaseConstants.h defines property type enums aligned with C# CellarPropertyType. FwKernel.dll provides COM proxy/stub for marshaling. Critical foundation defining data model identifiers used by all FieldWorks native components.

## Architecture
C++ header files with generated constants and minimal COM infrastructure (121 total lines). CellarConstants.vm.h NVelocity template processed by LcmGenerate MSBuild task generates CellarConstants from XML model. CellarBaseConstants.h static enum definitions. FwKernel_GUIDs.cpp COM GUID definitions. FwKernel.def DLL export definitions. Kernel.vcxproj builds FwKernel.dll (proxy/stub DLL).

## Key Components
- **CellarConstants.vm.h** (NVelocity template, 59 lines): Class and field ID constant generation
  - Processed by LcmGenerate task from MasterLCModel.xml
  - Generates kclid* (class IDs): kclid for each data model class (e.g., kclid LexEntry)
  - Generates kflid* (field IDs): kflid for each property (e.g., kflidLexEntry_CitationForm)
  - CmObject base fields: kflidCmObject_Id, kflidCmObject_Guid, kflidCmObject_Class, kflidCmObject_Owner, kflidCmObject_OwnFlid, kflidCmObject_OwnOrd
  - kflidStartDummyFlids: Threshold for dummy field IDs (1000000000)
  - kwsLim: Writing system limit constant
- **CellarBaseConstants.h** (static header, 37 lines): Property type enum
  - CellarModuleDefns enum: Property type constants
  - kcptBoolean, kcptInteger, kcptNumeric, kcptFloat, kcptTime, kcptGuid, kcptImage, kcptGenDate, kcptBinary
  - kcptString, kcptMultiString, kcptUnicode, kcptMultiUnicode: String types
  - kcptOwningAtom, kcptReferenceAtom: Atomic object references
  - kcptOwningCollection, kcptReferenceCollection: Collection references
  - kcptOwningSequence, kcptReferenceSequence: Sequence references
  - Aligned with C# CellarPropertyType enum (CoreImpl/CellarPropertyType.cs)
- **FwKernel_GUIDs.cpp** (2661 lines): COM GUID definitions
  - GUID constants for COM interfaces and classes
- **FwKernelPs.idl** (631 lines): IDL for proxy/stub
  - Interface definitions for COM marshaling
- **FwKernel.def** (164 lines): DLL export definitions
  - Exports for proxy/stub DLL
- **dlldatax.c** (231 lines): DLL data for proxy/stub

## Technology Stack
- C++ native code
- NVelocity template engine (CellarConstants.vm.h)
- MSBuild LcmGenerate task for code generation
- COM (Component Object Model) proxy/stub infrastructure
- IDL (Interface Definition Language)

## Dependencies

### Upstream (consumes)
- **MasterLCModel.xml**: Data model definition (input to LcmGenerate)
- **Generic/**: Low-level utilities
- **NVelocity**: Template processing (LcmGenerate task)

### Downstream (consumed by)
- **All FieldWorks native C++ code**: Uses kclid*/kflid* constants
- **views/**: Uses class/field IDs for rendering
- **All data access**: Uses CellarConstants for object identification

## Interop & Contracts
- **CellarConstants enum**: Contract for class and field identifiers
  - kclid* prefix: Class IDs
  - kflid* prefix: Field IDs
- **CellarModuleDefns enum**: Property type identifiers
  - kcpt* prefix: Cellar property types
- **COM marshaling**: FwKernel.dll provides proxy/stub for interface marshaling
- **C#/C++ alignment**: CellarBaseConstants.h matches C# CellarPropertyType

## Threading & Performance
- **Constants**: Compile-time; zero runtime overhead
- **Generated code**: No runtime generation; build-time only

## Config & Feature Flags
No configuration. Constants generated from MasterLCModel.xml at build time.

## Build Information
- **Project file**: Kernel.vcxproj (builds FwKernel.dll - proxy/stub DLL)
- **LcmGenerate task**: Processes CellarConstants.vm.h with MasterLCModel.xml
- **Generated output**: CellarConstants.h (constants from template)
- **Build**: Via top-level FieldWorks.sln; LcmGenerate runs during build
- **Output**: FwKernel.dll (COM proxy/stub DLL), generated CellarConstants.h

## Interfaces and Data Models

- **CellarConstants enum** (CellarConstants.vm.h generated)
  - Purpose: Class and field identifier constants for data model
  - Values: kclid* (class IDs), kflid* (field IDs)
  - Notes: Generated from MasterLCModel.xml; used universally in native code

- **CellarModuleDefns enum** (CellarBaseConstants.h)
  - Purpose: Property type identifiers
  - Values: kcpt* constants (kcptBoolean, kcptString, kcptOwningAtom, etc.)
  - Notes: Aligned with C# CellarPropertyType; defines data storage types

- **kflidCmObject_*** constants**:
  - kflidCmObject_Id: Object ID field
  - kflidCmObject_Guid: Object GUID field
  - kflidCmObject_Class: Object class ID field
  - kflidCmObject_Owner: Owner object field
  - kflidCmObject_OwnFlid: Owning field ID
  - kflidCmObject_OwnOrd: Owning ordinal

- **kflidStartDummyFlids = 1000000000**:
  - Purpose: Threshold for dummy field IDs
  - Notes: Fields >= this value are dummies; not an error if not in database

## Entry Points
Header files included by all FieldWorks native C++ projects. FwKernel.dll loaded by COM for marshaling.

## Test Index
No dedicated test project. Constants verified via consuming components.

## Usage Hints
- **Include**: #include "CellarConstants.h" (generated from .vm.h template)
- **Class IDs**: Use kclid* constants (e.g., kclid LexEntry)
- **Field IDs**: Use kflid* constants (e.g., kflidLexEntry_CitationForm)
- **Property types**: Use kcpt* constants from CellarModuleDefns
- **Build-time generation**: LcmGenerate task regenerates constants from XML model
- **Regeneration**: Edit MasterLCModel.xml and rebuild to update constants
- **Alignment**: Keep CellarBaseConstants.h in sync with C# CellarPropertyType

## Related Folders
- **Generic/**: Low-level utilities used with Kernel constants
- **views/**: Uses Kernel constants for rendering
- **LCModel generation**: MasterLCModel.xml source for constant generation

## References
- **Key files**: CellarConstants.vm.h (NVelocity template, 59 lines), CellarBaseConstants.h (37 lines), FwKernel_GUIDs.cpp (2661 lines), FwKernelPs.idl (631 lines), FwKernel.def (164 lines), dlldatax.c (231 lines)
- **Project file**: Kernel.vcxproj (builds FwKernel.dll)
- **Build process**: LcmGenerate MSBuild task processes .vm.h template
- **Total lines**: 121 (source); ~3784 total including generated GUIDs/IDL
- **Output**: FwKernel.dll (COM proxy/stub), generated CellarConstants.h
- **Generated from**: MasterLCModel.xml (data model definition)
