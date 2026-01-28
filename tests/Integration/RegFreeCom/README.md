# RegFree COM Integration Validation

This folder centralizes clean-VM and developer-machine validation notes for every executable touched by the RegFree COM coverage project.

## Test Environments

1. **Clean VM (Hyper-V)**
   - Snapshot: `regfree-clean`
   - Requirements: Windows 11 x64, no FieldWorks installed, PowerShell Direct enabled
   - Launch via `scripts/regfree/run-in-vm.ps1 -VmName <name> -ExecutablePath <exe>`
2. **Developer Machine**
   - Fully provisioned FieldWorks dev box with COM components registered
   - Used to confirm manifests do not regress legacy behavior

## Evidence Files

| File                          | Purpose                                                     |
| ----------------------------- | ----------------------------------------------------------- |
| `user-tools-vm.md`            | LCMBrowser + UnicodeCharEditor clean-VM runbook/results     |
| `user-tools-dev.md`           | Developer-machine results for user-facing tools             |
| `user-tools-i18n.md`          | Complex-script coverage evidence                            |
| `migration-utilities-vm.md`   | Clean-VM results for MigrateSqlDbs/FixFwData/FxtExe         |
| `migration-utilities-dev.md`  | Developer-machine / complex script validation for utilities |
| `supporting-utilities-dev.md` | Coverage for the lower-priority utilities                   |
| `installer-validation.md`     | Installer smoke test transcript                             |
| `build-smoke.md`              | Traversal build verification notes                          |

> Each markdown file must link back to artifacts stored in `specs/003-convergence-regfree-com-coverage/artifacts/` so reviewers can trace evidence.
