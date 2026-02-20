# RegFree COM Tooling

This folder holds the automation that drives the RegFree COM coverage work described in `specs/003-convergence-regfree-com-coverage/`. The tooling is split into three primary flows:

1. **Audit** – `audit_com_usage.py` enumerates every executable defined in `project_map.json`, scans the associated source tree for COM indicators (interop attributes, FwKernel/Views dependencies, etc.), and emits evidence (`.csv`, `.json`, `.log`) into `specs/003-convergence-regfree-com-coverage/artifacts/`.
2. **Manifest Wiring** – `add_regfree_manifest.py` reads the metadata entries flagged as requiring RegFree support and updates the corresponding `.csproj` files plus `BuildInclude.targets` to import `Build/RegFree.targets` and set `<EnableRegFreeCom>true</EnableRegFreeCom>`.
3. **Validation** – `validate_regfree_manifests.py` and `run-in-vm.ps1` parse the generated manifests, verify all COM classes/typelibs are present, assemble payload folders, and launch each executable on both the clean VM checkpoint and a developer workstation.

Supporting assets:
- `common.py` exposes strongly-typed helpers for reading `project_map.json`, inferring output paths, and formatting artifact names.
- `project_map.json` is the single source of truth for executable IDs, project paths, and output paths.
- `artifacts/README.md` documents every file that the scripts write so that evidence can be audited later.

> **Workflow**: run `audit_com_usage.py` ➜ review/edit `project_map.json` ➜ run `add_regfree_manifest.py` ➜ rebuild the affected projects ➜ run `validate_regfree_manifests.py` ➜ invoke `run-in-vm.ps1` for VM validation.

## Manifest Generation

In addition to the above, the following scripts are used to generate the actual manifest XML content by extracting COM metadata from the C++ source code:

- `extract_com_guids.py`: Scans C++ source files (`FwKernel_GUIDs.cpp`, `Views_GUIDs.cpp`, `ViewsExtra_GUIDs.cpp`) to extract COM GUIDs (CLSID, IID). It also scans for `GenericFactory` instantiations to extract ProgIDs and ThreadingModels.
- `com_guids.json`: The output of `extract_com_guids.py`. Contains a mapping of COM classes/interfaces to their GUIDs, DLLs, ProgIDs, and ThreadingModels.
- `generate_manifest.py`: Reads `com_guids.json` and generates the manifest file.
- `FieldWorks.regfree.manifest`: The generated manifest file.

**Usage**:
1. Run `python extract_com_guids.py` to update `com_guids.json`.
2. Run `python generate_manifest.py` to generate `FieldWorks.regfree.manifest`.
