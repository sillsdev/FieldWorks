# Gecko/XULRunner Browser and PDF Audit (Tasks 5.6 / 5.7)

Date: 2026-06-09
Branch: `010-advanced-entry-view-phase-1-2`
Scope: task 5.6 (audit of Gecko/XULRunner preview, print, and PDF paths) and task 5.7
(non-Gecko browser/PDF strategy — **proposal only, owner sign-off required**).

All file:line references below were verified by grep/read on this branch on 2026-06-09.

---

## Inventory (5.6)

### Startup initialization and the Graphite preference

Gecko/XULRunner is initialized unconditionally in `FieldWorks.exe` `Main`, **before any window
opens**, and a missing Firefox folder is a hard startup failure:

- `Src/Common/FieldWorks/FieldWorks.cs:164-192` — `#region Initialize XULRunner`: resolves the
  `Firefox`/`Firefox64` folder (or `XULRUNNER` env var), calls `Xpcom.Initialize(firefoxPath)`.
- `Src/Common/FieldWorks/FieldWorks.cs:175-180` — throws `ApplicationException` ("Cannot find
  Firefox/XulRunner directory") if the folder is absent. **Gecko is currently a hard startup
  dependency of the entire app**, independent of whether any browser surface is ever shown.
- `Src/Common/FieldWorks/FieldWorks.cs:187` — `GeckoPreferences.User["gfx.font_rendering.graphite.enabled"] = true;`
  (the Graphite preference this audit was asked to locate).
- `Src/Common/FieldWorks/FieldWorks.cs:188` — `print.show_print_progress = false`.
- `Src/Common/FieldWorks/FieldWorks.cs:191` — `XWebBrowser.DefaultBrowserType = XWebBrowser.BrowserType.GeckoFx;`
  makes every default-constructed `XWebBrowser` in the app a Gecko browser.
- `Src/Common/FieldWorks/FieldWorks.cs:399-409` — shutdown hack: constructs a throwaway
  `GeckoWebBrowser` (line 407) before `Xpcom.Shutdown()` to avoid a native double-free.

### Consumer inventory

Classification key:
- **(a)** reachable from default Lexical Edit workflows (lexiconEdit tool and its dialogs)
- **(b)** reachable from export/print only
- **(c)** dev/test only (or latent: no shipped configuration reaches it)
- **(d)** reachable from other areas/tools in normal navigation, but outside the Lexical Edit
  migrated-region boundary (added for honesty; the migration boundary does not own these)

| Area | Source (file:line) | Role | Classification |
|---|---|---|---|
| App startup XULRunner init + Graphite pref | `Src/Common/FieldWorks/FieldWorks.cs:164-192` (pref at `:187`, hard fail at `:175-180`) | Process-wide Gecko bootstrap; required before any window | (a) — runs in every workflow, including Lexical Edit |
| App shutdown Gecko hack | `Src/Common/FieldWorks/FieldWorks.cs:399-409` | Throwaway `GeckoWebBrowser` + `Xpcom.Shutdown()` | (a) — every process exit |
| Dictionary preview pane inside Lexicon Edit | `Src/xWorks/XhtmlRecordDocView.cs:67` (`new XWebBrowser(BrowserType.GeckoFx)`); wired by `DistFiles/Language Explorer/Configuration/Lexicon/Edit/toolConfiguration.xml:36-41` including `Dictionary/toolConfiguration.xml:4-6` (`DictionaryPubPreviewControl`); on by default per `Lexicon/areaConfiguration.xml:308` (`Show_DictionaryPubPreview` bool=true) | "Show Dictionary Preview" pane rendered above the entry pane in the lexiconEdit tool | (a) — visible by default in Lexical Edit |
| Lexicon Edit File > Print | `Src/xWorks/XhtmlRecordDocView.cs:135-139` → `XhtmlDocView.PrintPage` at `Src/xWorks/XhtmlDocView.cs:963-967` (`geckoBrowser.Window.Print()`); `defaultPrintPane="DictionaryPubPreview"` at `Lexicon/Edit/toolConfiguration.xml:9` | Printing from Lexical Edit prints the Gecko preview pane via Gecko's print dialog | (b) — print only |
| Dictionary doc view (Lexicon > Dictionary tool) | `Src/xWorks/XhtmlDocView.cs:59` (`new XWebBrowser(BrowserType.GeckoFx)`); tool wiring `Lexicon/Dictionary/toolConfiguration.xml:27`; record view `Lexicon/Dictionary/toolConfiguration.xml:5` | Full configured-dictionary XHTML view, DOM click/right-click handling (`XhtmlDocView.cs:289,315,497`) | (d) — Lexicon area navigation, outside lexiconEdit region |
| Reversal Indexes doc view | `DistFiles/.../Lexicon/ReversalIndices/toolConfiguration.xml:12` → `XhtmlDocView` | Reversal index XHTML doc view | (d) |
| Classified Dictionary view | `DistFiles/.../Lexicon/RDE/toolConfiguration.xml:213` → `XhtmlDocView` | Semantic-domain classified dictionary XHTML view | (d) |
| Dictionary print → PDF (large dictionaries) | `Src/xWorks/XhtmlDocView.cs:849-925`; exe name `:885`, missing-exe error `:900`, invocation `:907` (`FieldWorksPdfMaker.exe "<xhtml>" "<pdf>" --graphite --reduce-memory`), open in OS viewer `:923` (`Process.Start(outputFile)`) | `OnPrint` over `defaultMaxEntriesFWCanPrint` (10000) entries generates a PDF via `GeckofxHtmlToPdf` and hands it to the OS viewer to print | (b) — print only |
| Dictionary print (in-browser path) | `Src/xWorks/XhtmlDocView.cs:927-961` (`GenerateReloadAndPrint`) and `:963-967` (`PrintPage`) | Smaller dictionaries print directly through Gecko `Window.Print()` | (b) — print only |
| Dictionary Configuration dialog preview | `Src/xWorks/DictionaryConfigurationDlg.Designer.cs:55` (`m_preview = new XWebBrowser()`); hard Gecko requirement `Src/xWorks/DictionaryConfigurationDlg.cs:44-49` and `:207-211`; highlight logic over `GeckoElement` `:228` | Tools > Configure > Dictionary preview pane; launched from `Src/xWorks/DictionaryConfigurationListener.cs:220` and from doc-view right-click at `Src/xWorks/XhtmlDocView.cs:630` | (a) — the configure-dictionary dialog is reachable from Lexicon Edit's Tools menu |
| New Entry dialog gloss assistant (MGA) | `Src/LexText/Morphology/MGA/MGAHtmlHelpDialog.cs:19,30` (`GeckoWebBrowser m_browser`); launched from `Src/LexText/LexTextControls/InsertEntryDlg.cs:1832-1836` | EticGlossList HTML help pane in the Morphological Gloss Assistant, opened from the Insert Entry dialog's "Gloss assistant" link (inflectional MSA only) | (a) — reachable from Lexical Edit Insert Entry |
| Configure Interlinear dialog | `Src/LexText/Interlinear/ConfigureInterlinDialog.Designer.cs:28` (`mainBrowser = new XWebBrowser()`); hard Gecko requirement `ConfigureInterlinDialog.cs:74-78`; `AutoJSContext` JS interop `:764,791`; Gecko DOM checkbox manipulation `:785,803` | Interlinear row-configuration preview; launched from `Src/LexText/Interlinear/InterlinDocRootSiteBase.cs:855` and `Src/LexText/Discourse/ConstituentChart.cs:110` | (d) — Texts & Words / Discourse only |
| Grammar Sketch viewer | `Src/xWorks/GeneratedHtmlViewer.cs:206-219` (`InitHtmlControl` → XCore `HtmlControl`); Gecko Find dialog `GeneratedHtmlViewer.cs:1053-1056`; tool wiring `DistFiles/.../Grammar/Edit/toolConfiguration.xml:398` (tool `grammarSketch`) | Generated morphological sketch HTML viewer | (d) — Grammar area only |
| XCore `HtmlControl` (shared Gecko wrapper) | `Src/XCore/HtmlControl.cs:20,38,265` (`m_browser = new GeckoWebBrowser()`) | Reusable Gecko host used by GeneratedHtmlViewer, TryAWordDlg, HtmlViewer, FLExBridge instructions dialog | (d) — classification follows its consumers |
| Parser Try A Word / parser trace | `Src/LexText/ParserUI/TryAWordDlg.cs:56,152-154` (`m_htmlControl = new HtmlControl`); Gecko DOM interop in `Src/LexText/ParserUI/WebPageInteractor.cs:18-59`; launched from `Src/LexText/ParserUI/ParserListener.cs:1176` | Parser results/trace HTML with clickable Gecko elements | (d) — Texts & Words parser menu |
| Send/Receive first-time instructions | `Src/LexText/Lexicon/FLExBridgeFirstSendReceiveInstructionsDlg.Designer.cs:42` (`new XCore.HtmlControl()`) | Static instruction HTML before first Send/Receive | (d) — Send/Receive workflow |
| SFM Import wizard marker help | `Src/LexText/LexTextControls/LexImportWizardMarker.cs:77,485` (`GeckoWebBrowser m_browser`); help file path `:80` (`Language Explorer/Import/Help.htm`) | Embedded help pane in the lexical SFM import wizard | (d) — import workflow |
| XCore `HtmlViewer` content control | `Src/XCore/HtmlViewer.cs:26,59` (`m_htmlControl = new HtmlControl()`) | Generic xCore HTML content control; **no shipped `DistFiles` configuration references it** (verified by grep) | (c) — latent infrastructure |
| `ReallySimpleListChooser` help-browser pane | `Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs:124,933-935,1046-1083` (`m_webBrowser = new XWebBrowser()` at `:1057`) | Optional chooser help pane gated on `chooserInfo helpBrowser="true"`; both shipped configs set it **false** (`DistFiles/.../Parts/CellarParts.xml:237`, `NotebookParts.xml:61`). Note: the non-Mono branch at `:1071-1074` casts `NativeBrowser as WebBrowser` (WinForms/IE), which is null under the app-wide GeckoFx default — a latent NRE if ever enabled on Windows | (c) — latent / config-gated, no shipped consumer |
| Forbidden-symbol enforcement (FwAvalonia) | `Src/Common/FwAvalonia/FwAvaloniaTests/EngineIsolationAuditTests.cs:30,41` and `LexicalEditSurfaceResolverTests.cs:101`; manifest list `openspec/changes/lexical-edit-avalonia-migration/region-manifest.md:96` | Tests asserting `FwAvalonia` references no Gecko/`XWebBrowser`/`GeckofxHtmlToPdf`/`FieldWorksPdfMaker` symbols (task 5.5) | (c) — dev/test enforcement |

### `GeckofxHtmlToPdf` / `FieldWorksPdfMaker` usage and packaging

- **Single runtime call site.** `FieldWorksPdfMaker.exe` is invoked only from
  `Src/xWorks/XhtmlDocView.cs:882-925` (`GeneratePdfToPrint`): the *print* path for dictionaries
  larger than 10000 entries (`:851,861-869`) or after a COM print failure
  (`:952-957` directs users to the env-var/PDF route). The PDF is opened in the OS viewer
  (`Process.Start`, `:923`) — printing itself is already delegated to the OS at that point.
  No File > Export path uses it (verified: no other `FieldWorksPdfMaker`/`GeckofxHtmlToPdf`
  reference under `Src/` outside `XhtmlDocView.cs`, its resources
  `Src/xWorks/xWorksStrings.resx:1100` / `xWorksStrings.Designer.cs:2624-2626`, and the
  FwAvalonia forbidden-symbol tests).
- **Packaging.** `Build/PackageRestore.targets:31-32` pins `GeckofxHtmlToPdfVersion 1.1.0`;
  `:259-273` downloads `GeckofxHtmlToPdf.exe(.config)` + `Args.dll` from the sillsdev GitHub
  release; `:477-491` copies it into the output as **`FieldWorksPdfMaker.exe`** (renamed so users
  can identify it in antivirus quarantine); `:493-520` patches its `Geckofx-Core` binding redirect
  to match `GeckoNugetVersion` (`Build/SilVersions.props:24` = `60.0.56`); `:556-570` copies the
  Geckofx60 NuGet `content/` (the native XULRunner `Firefox64` folder) into the output base.
- **Installer.** `Build/Installer.legacy.targets:670,716` suppress ICE30 because FieldWorks and
  the Encoding Converters merge module both install Geckofx files.
- **Assembly references.** `Geckofx60.64` is referenced by `Src/Common/FieldWorks/FieldWorks.csproj:52`,
  `Src/xWorks/xWorks.csproj:45-46`, `Src/XCore/xCore.csproj:29`, `Src/Common/Controls/XMLViews/XMLViews.csproj:34`,
  `Src/LexText/Interlinear/ITextDll.csproj:36,43` (plus `SIL.Windows.Forms.GeckoBrowserAdapter`),
  `Src/LexText/ParserUI/ParserUI.csproj:31`, `Src/LexText/Morphology/MGA/MGA.csproj:30`, and
  `Src/LexText/LexTextControls/LexTextControls.csproj:36-37`.

---

## Graphite coupling

1. **App-wide rendering preference.** `Src/Common/FieldWorks/FieldWorks.cs:187` turns on
   `gfx.font_rendering.graphite.enabled` for every Gecko surface above. Any Graphite-enabled
   writing system whose font carries Graphite tables is shaped by Gecko's Graphite engine in the
   Lexicon Edit dictionary preview, the Dictionary/Reversal/Classified doc views, the
   configuration-dialog previews, and the interlinear configuration preview.
2. **PDF shaping.** `Src/xWorks/XhtmlDocView.cs:907` passes `--graphite` to
   `FieldWorksPdfMaker.exe`, so the print-to-PDF path also shapes with Graphite (the PDF maker is
   itself Geckofx-based).
3. **Replacement consequence.** Graphite shaping is a Firefox/Gecko-specific capability; Chromium
   (and therefore WebView2) has no Graphite support. Any non-Gecko browser/PDF replacement loses
   Graphite shaping on these surfaces by construction. That is consistent with the
   `graphite-transition-support` change (warn-don't-block, sunset policy), but every replaced
   surface must be covered by that change's classification + warning machinery before the swap.
4. **Boundary enforcement already in place.** The migrated Avalonia region cannot acquire a Gecko
   or Graphite dependency silently: `EngineIsolationAuditTests.cs:30,41` fails the suite if
   `FwAvalonia` references `Gecko*`, `XWebBrowser`, `GeckofxHtmlToPdf`, or `FieldWorksPdfMaker`
   (whole-identifier match), per `region-manifest.md:96`.

---

## Strategy options (5.7)

> **Status: PROPOSAL — recommended, not decided.** This section is a recommendation for owner
> sign-off; no replacement work is started by this document.

| # | Option | Scope | Effort | Risk | Graphite impact | Notes |
|---|---|---|---|---|---|---|
| 1 | **WebView2** (Microsoft.Web.WebView2, WinForms control) | Replace `XWebBrowser`/`GeckoWebBrowser` in the (a)/(b) surfaces: dictionary preview pane, doc views, configuration-dialog previews; replace `GeckofxHtmlToPdf` with `CoreWebView2.PrintToPdfAsync` | Medium-high: ~10 consumer surfaces; the hard parts are the Gecko DOM/JS interop ports (`DictionaryConfigurationDlg.cs:228` GeckoElement walking, `ConfigureInterlinDialog.cs:764-803` `AutoJSContext` + `GeckoInputElement`, `XhtmlDocView.cs:497` DOM right-click menus, `WebPageInteractor.cs:32-59`) to `ExecuteScriptAsync`/WebMessage patterns | Medium: Windows-only (fits the current net48 Windows-only target); requires the Evergreen WebView2 runtime (installer bootstrapper or fixed-version distribution); behavior parity for print CSS must be re-validated | **Loses Graphite shaping** — requires `graphite-transition-support` classification + warnings on each converted surface | The natural end state for Windows; also removes the startup hard dependency once `FieldWorks.cs:164-192` is made lazy |
| 2 | **Keep Gecko as a classified legacy-export/preview boundary** | No code change; all (a)/(b)/(d) consumers stay on Gecko; the Avalonia Lexical Edit region continues to forbid Gecko symbols (already enforced, task 5.5) | Low | Low short-term; long-term: Gecko 60 (2018, security-frozen) keeps shipping; XULRunner stays a hard startup dependency (`FieldWorks.cs:175-180`) | Graphite rendering preserved on legacy surfaces — consistent with `graphite-transition-support` legacy-surface support until M2 | Correct posture for the first migrated slices; insufficient as the end state for an Avalonia *default* |
| 3 | **Print via the OS** (PDF hand-off) | Generalize the existing pattern at `XhtmlDocView.cs:923` (`Process.Start(pdf)`): generate a PDF (engine per option 1 or 2) and let the OS viewer own the print dialog, retiring `Gecko Window.Print()` (`XhtmlDocView.cs:963-967`, `XhtmlRecordDocView.cs:135-139`) | Low (on top of whichever PDF engine is chosen) | Low; removes the flaky in-browser print paths documented at `XhtmlDocView.cs:940-946,952-957` | Follows the PDF engine's shaping | Recommended regardless of engine choice |
| 4 | Avalonia-native preview rendering of typed IR (no embedded browser for the lexiconEdit preview pane) | Replace `XhtmlRecordDocView` preview with an Avalonia render of the dictionary-publication IR | High | High; duplicates the configured-dictionary CSS pipeline | Follows the 6.13 HarfBuzz text foundation (no Graphite) | Long-term option only; out of scope for this change's gates |

### Recommendation (proposed)

- **Now through first slices (M0/M1):** Option 2. Keep every Gecko consumer as a classified
  legacy boundary. The lexiconEdit-reachable surfaces — dictionary preview pane, configure-dictionary
  dialog, MGA help pane, and the print path — remain legacy WinForms surfaces under the
  coexistence model; the forbidden-symbol tests keep the migrated region clean.
- **Before the Avalonia default switch:** decide and validate Option 1 + Option 3 — WebView2 for
  the (a)-classified surfaces (dictionary preview pane, DictionaryConfigurationDlg, MGA help) and
  `PrintToPdfAsync` + OS-viewer printing replacing both `Gecko Window.Print()` and
  `FieldWorksPdfMaker.exe` on the default path; (d)-classified surfaces (interlinear config,
  grammar sketch, parser trace, import wizard, S/R instructions) may follow later or stay legacy
  with their areas.
- **Prerequisite for any Gecko removal:** make the startup init (`FieldWorks.cs:164-192`) lazy or
  optional; today a missing Firefox folder is a hard startup failure, so packaging cannot drop
  XULRunner until that changes.

---

## Decision gates

| Gate | Decision needed by | Owner action | Blocked work if undecided |
|---|---|---|---|
| Browser/PDF strategy sign-off (accept/modify the Option 2 → Option 1+3 recommendation above) | **Before the Avalonia default switch — explicitly NOT before the first migrated slice.** First slices proceed under Option 2 (classified legacy boundary) with no decision required. | Owner (john_lambert@sil.org) approves or amends this proposal; record the decision in this file and flip 5.7 to done | Avalonia-default readiness (task 5.8 validation, 10.5 browser/PDF replacement validation, 7.5 default gate) |
| Graphite-warning coverage for any converted surface | With (not after) the first surface converted off Gecko | Confirm `graphite-transition-support` classification + G0-G3 warning machinery covers the surface | Converting any preview/print/PDF surface to WebView2 |
| Startup-init laziness | Before removing XULRunner from packaging | Schedule a task to make `FieldWorks.cs:164-192` lazy/optional | Dropping the Firefox folder, Geckofx NuGets, or `FieldWorksPdfMaker.exe` from the installer |

Until the first gate is decided, the standing classification is: **all Gecko consumers are
legacy-boundary surfaces outside the migrated Avalonia region; the migrated region's freedom from
them is enforced by `EngineIsolationAuditTests`.**
