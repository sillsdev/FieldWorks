# Data Model: 64-bit only + Registration-free COM

Created: 2025-11-06 | Branch: 001-64bit-regfree-com

## Entities

- Executable
  - Description: User-facing or test host process that may activate COM.
  - Attributes: Name, OutputPath, HasManifest (bool)
  - Relationships: Includes Manifest; Co-locates Native COM DLLs

- Manifest (Registration-free)
  - Description: XML assembly manifest enabling COM activation without registry.
  - Attributes: FileName, File entries, comClass entries, typelib entries, proxy/stub entries
  - Relationships: References Native COM DLLs

- Native COM DLL
  - Description: Native library exporting COM classes and type libraries.
  - Attributes: Name, ContainsTypeLib (bool), CLSIDs, IIDs
  - Relationships: Discovered and referenced by Manifest; loaded by Executable

## Validation Rules

- An Executable that activates COM MUST have a Manifest generated during build.
- All Native COM DLLs required for primary flows MUST be present next to the Executable (or manifest codebase referenced) and represented in Manifest `<file>` entries.
- Manifests SHOULD include `<comInterfaceExternalProxyStub>` where required by interfaces.

## State Transitions (Build-time)

- Project Build → Manifest Generated → Artifact Packaged → Runtime Activation (No registry)
