# Contracts: WiX 3.14 Installer Upgrade

This feature does not define API contracts. It modifies CI workflows and documentation only.

## Workflow Contracts (Reference)

The installer workflows expose the following inputs and outputs:

### base-installer-cd.yml Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `fw_ref` | No | Current branch | Commit-ish for main repository |
| `helps_ref` | No | `develop` | Commit-ish for FwHelps |
| `installer_ref` | No | `master` | Commit-ish for genericinstaller |
| `localizations_ref` | No | `develop` | Commit-ish for FwLocalizations |
| `lcm_ref` | No | `master` | Commit-ish for liblcm |
| `make_release` | No | `false` | Whether to upload to S3 and create GitHub Release |

### patch-installer-cd.yml Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `fw_ref` | No | Current branch | Commit-ish for main repository |
| `helps_ref` | No | `develop` | Commit-ish for FwHelps |
| `installer_ref` | No | `master` | Commit-ish for genericinstaller |
| `localizations_ref` | No | `develop` | Commit-ish for FwLocalizations |
| `lcm_ref` | No | `master` | Commit-ish for liblcm |
| `base_release` | No | `build-1188` | GitHub release with base build artifacts |
| `base_build_number` | No | `1188` | Base build number for version comparison |
| `make_release` | No | `true` | Whether to upload to S3 |

These contracts are unchanged by this feature.
