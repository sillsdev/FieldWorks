# Installer Log Triage

## Burn Bundle Log

Read in phase order:

1. Header/version/command line.
2. Detect phase: `Detected package`, related bundle detection, registry searches, prerequisite state.
3. Plan phase: `Planned package`, requested action, cache strategy, dependency registration, feature states.
4. Cache/download phase: URL, payload hash/signature, cache path.
5. Apply phase: `Applying execute package`, package arguments, package-specific log path.
6. Completion: package result, restart state, final HRESULT.

Useful strings:

- `WixBundleLog`
- `WixBundleLog_AppMsiPackage`
- `Detected package:`
- `Planned package:`
- `Applying execute package:`
- `Error 0x`
- `restart:`
- `Will not uninstall package`
- `found dependents`

## MSI Verbose Log

Read around the first real failure, not the final summary only.

Useful strings:

- `Return value 3`
- `CustomAction`
- `Action start`
- `Action ended`
- `APPFOLDER`
- `DATAFOLDER`
- `WIX_UPGRADE_DETECTED`
- `FindRelatedProducts`
- `RemoveExistingProducts`
- `InstallValidate`
- `PATCH`
- `PATCHNEWSUMMARYSUBJECT`

When `Return value 3` appears, walk upward to find the custom action or standard action that failed, then inspect properties and command line immediately before it.

## Custom Actions

Map WiX action ID to DLL entry:

- `CheckApplicationPath` -> `CheckAppPath`
- `VerifyDataPath` -> `VerifyDataDirPath`
- `CloseApplications` -> `ClosePrompt`
- `DeleteRegistryVersionNumber` -> `DeleteVersionNumberFromRegistry`

Common causes:

- Required session properties not initialized before UI sequence.
- Deferred action needs `CustomActionData` but reads session properties directly.
- Missing `CustomActions.CA.dll` or runtime config in the Binary payload.
- Process-close prompt waiting for user action.

## Symptom Shortcuts

- Double-click does nothing: run with `/log`; check Burn condition failures, BA/theme load failure, Event Viewer, crash dumps.
- Bundle waits after detect: interactive bundle may be waiting for user action. Use `/passive` or full UI intentionally.
- MSI UI missing from bundle: inspect `bal:DisplayInternalUICondition`, UI level, and package log.
- Uninstall hangs: find whether Burn, MSI, files-in-use, CloseApplications, or ARP invocation is waiting.
- Duplicate ARP entries: inventory bundle vs MSI registrations and check package visibility/ARP properties.
