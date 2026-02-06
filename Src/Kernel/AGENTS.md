---
last-reviewed: 2025-10-31
last-reviewed-tree: c8147e4135449a80e746c376e1cf2012eec0bd4845459fff1a1cd3e89825bf9b
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - interop--contracts
  - referenced-by
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

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

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- CellarConstants enum: Contract for class and field identifiers

### Referenced By

- [COM Registration Patterns](../../openspec/specs/architecture/interop/com-contracts.md#com-registration-patterns) — Registration-free COM contracts overview

## Threading & Performance
- Constants: Compile-time; zero runtime overhead

## Config & Feature Flags
No configuration. Constants generated from MasterLCModel.xml at build time.

## Build Information
- Project file: Kernel.vcxproj (builds FwKernel.dll - proxy/stub DLL)

## Interfaces and Data Models
kflidCmObject_.

## Entry Points
Header files included by all FieldWorks native C++ projects. FwKernel.dll loaded by COM for marshaling.

## Test Index
No dedicated test project. Constants verified via consuming components.

## Usage Hints
- Include: #include "CellarConstants.h" (generated from .vm.h template)

## Related Folders
- Generic/: Low-level utilities used with Kernel constants

## References
See `.cache/copilot/diff-plan.json` for file details.
