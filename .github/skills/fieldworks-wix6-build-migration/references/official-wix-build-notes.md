# Official WiX Build And Migration Notes

Use these notes for WiX Toolset, not the Wix website builder.

## Official Sources

- WiX source: https://github.com/wixtoolset/wix
- FireGiant docs: https://docs.firegiant.com/wix/
- v3 to v4 migration WIP: https://docs.firegiant.com/wix/development/wips/4561-migrate-v3-source-code-to-v4/
- MSBuild SDK docs: https://docs.firegiant.com/wix/tools/msbuild/
- Preprocessor docs: https://docs.firegiant.com/wix/tools/preprocessor/
- Extension docs: https://docs.firegiant.com/wix/tools/wixext/

## Migration Facts To Remember

- WiX v6 authoring still uses the v4 XML namespace.
- v3 `candle.exe` and `light.exe` thinking maps to `wix build` or SDK-style MSBuild.
- SDK-style `.wixproj` files can use `ProjectReference`s; WiX creates bind paths and preprocessor variables for referenced projects.
- Extensions are NuGet packages in modern WiX, not only `-ext` paths to globally installed DLLs.
- Useful MSBuild properties include `DefineConstants`, `BindPath`, `InstallerPlatform`, `OutputType`, `VerboseOutput`, `SuppressIces`, `SuppressValidation`, and `*AdditionalOptions`.
- `Product` became `Package`; old `Package` metadata moved/reorganized.
- `Component/@Win64` became `Bitness`; default bitness follows `InstallerPlatform`/`-arch`.
- `RemotePayload` became package-specific payload elements such as `ExePackagePayload`, `MsiPackagePayload`, and `MspPackagePayload`.
- `ExePackage` command attributes became `InstallArguments`, `RepairArguments`, and `UninstallArguments`.
- `DisplayInternalUI` moved out of core Burn package authoring; use Bal `DisplayInternalUICondition` when appropriate.

## Build Quality Rules

- Do not suppress validation to get past real authoring problems.
- Keep warnings visible unless the repo has an intentional suppression with a reason.
- Preserve `.wixpdb` outputs for diagnostics and patch baselines.
- Treat Heat as technical debt. If using generated fragments, guard component/file identity carefully.
