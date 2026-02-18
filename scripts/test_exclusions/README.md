# FieldWorks Test Exclusion Tooling

This package houses the shared helpers that power the audit, conversion,
and validation CLIs described in `specs/004-convergence-test-exclusion-patterns/*`.
Each module intentionally focuses on a single responsibility:

| Module              | Responsibility                                                                                     |
| ------------------- | -------------------------------------------------------------------------------------------------- |
| `models.py`         | Dataclasses that mirror the entities defined in `data-model.md`                                    |
| `msbuild_parser.py` | Minimal XML helpers for reading/writing `<Compile Remove>` and `<None Remove>` entries             |
| `repo_scanner.py`   | Repository discovery utilities that enumerate SDK-style `.csproj` files and their `*Tests` folders |
| `report_writer.py`  | Serializes audit results into JSON + CSV formats under `Output/test-exclusions/`                   |
| `converter.py`      | (Future) Reusable routines for deterministic `.csproj` rewrites                                    |
| `validator.py`      | (Future) Policy enforcement helpers shared by CLI + PowerShell wrapper                             |

## Usage

1. Import the shared models and helpers from this package:
   ```python
   from scripts.test_exclusions import models, repo_scanner
   ```
2. Use `repo_scanner.scan_repository(Path.cwd())` to enumerate SDK-style
   projects along with their test folders and exclusion metadata.
3. Call `msbuild_parser.ensure_explicit_exclusion(...)` when a conversion
   requires inserting the canonical `<ProjectName>Tests/**` pattern.

Modules purposely avoid external dependencies. Standard library types are
used throughout so tests can run anywhere Py3.11 is available.
