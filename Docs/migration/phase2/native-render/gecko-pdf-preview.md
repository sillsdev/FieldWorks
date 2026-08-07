# Gecko / PDF preview (`GeckoWebBrowser` / `GeckofxHtmlToPdf`)

| | |
|---|---|
| **Key files** | `GeckoWebBrowser` / `GeckofxHtmlToPdf` consumers — `Src/xWorks/XhtmlDocView.cs`, `GeneratedHtmlViewer.cs`, `Src/XCore/HtmlControl.cs` |
| **Area** | Native-render |
| **Type** | native-render |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 10A |
| **JIRA** | LT-XXXXX |

## What it is
The embedded Gecko (Geckofx) HTML browser used for dictionary preview and the Gecko-based HTML-to-PDF export path.

## Notes / gotchas
- Decommission target: replace Gecko with a cross-platform HTML render/preview and a non-Gecko PDF export.
- Geckofx is a heavyweight native dependency and a major cross-platform/packaging blocker — removal is a goal, not just a port.
- Multiple consumers (`XhtmlDocView`, `GeneratedHtmlViewer`, `HtmlControl`); migrate them together.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
