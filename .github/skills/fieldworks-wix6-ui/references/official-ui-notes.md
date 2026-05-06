# Official WiX UI And Burn Notes

Use these for WiX Toolset, not Wix.com.

## Important Sources

- MsiPackage schema: https://docs.firegiant.com/wix/schema/wxs/msipackage/
- Bundle schema: https://docs.firegiant.com/wix/schema/wxs/bundle/
- Bal extension source/tests: https://github.com/wixtoolset/wix/tree/main/src/ext/Bal
- Built-in BA migration post: https://rseanhall.com/blog/2021/06/06/v4-breaking-changes-ref-builtin-ba/
- Bundle architecture post: https://rseanhall.com/blog/2021/06/08/v4-breaking-changes-bundles-respect-arch/

## Facts To Encode In UI Work

- `bal:DisplayInternalUICondition` controls whether Burn shows the authored MSI UI. It is from the Bal extension namespace.
- `DisplayInternalUI` as a core `MsiPackage` attribute is not the FieldWorks WiX 6 approach.
- When internal MSI UI is shown, it appears on top of the bootstrapper UI; it is not embedded in WixStdBA.
- WixStdBA does not support EmbeddedUI.
- `MsiPackage/@Visible` controls whether the MSI appears in Programs and Features. FieldWorks currently authors `Visible="no"`.
- `MsiPackage/@LogPathVariable` defaults to `WixBundleLog_[PackageId]`; FieldWorks uses `WixBundleLog_AppMsiPackage` explicitly.
- WiX 4+ bundles respect architecture. In an x64-only product, review registry searches, BA payload architecture, and old Win64 assumptions.
- Theme assets must be present in the BA container/output using the exact names referenced by the theme.

## Debugging Hints From WiX Source

- WixStdBA creates its UI on a UI thread and reports theme/window creation failures to Burn logs.
- WixStdBA evaluates `DisplayInternalUICondition` during MSI planning and only sets MSI UI level when the condition is true.
- WixInternalUIBootstrapperApplication is a different BA with different warnings and behavior; do not assume WixStdBA and IUIBA behave the same.
