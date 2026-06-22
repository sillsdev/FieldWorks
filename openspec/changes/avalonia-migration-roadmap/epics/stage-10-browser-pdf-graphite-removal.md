# Stage 10 — Browser/PDF & dictionary-preview replacement; Graphite (managed-path) removal (Epic draft)

> JIRA-ready draft derived from `complete-migration-program.md` §4 Stage 10 + Post-review callout,
> §7 Definition of Done, §10 JIRA structure/labels, §11.1 resolved decision, and
> `reviews/stage-10-browser-pdf-graphite-removal.md` + `reviews/00-cross-comparison-synthesis.md`
> §3/§4/§6/§7. Per synthesis §10, epics follow the §6 sub-epic map, not the one-row-per-stage table.

---

## Epic

**Summary:** Stage 10 — Retire native-bound rendering: replace Gecko/XULRunner dictionary preview
and PDF export with managed Avalonia/WebView2, and remove Graphite from the managed/Avalonia
rendering path.

**Type:** Epic (parent). Decomposes into child epics **10A** (Gecko/PDF/preview) and **10B**
(Graphite classify + managed-path removal), each with its own gate.

**Labels:** `track-surfaces`, `lead-senior`, `parity-blocked-by:rendering-engine`.

**Description:**
Stage 10 bundles two genuinely distinct workstreams that share the theme "retire native-bound
rendering" but have different blast radius, gates, and critical-path positions, so they are split
into two child epics under this parent (synthesis §3, review §1):

- **10A — Browser/PDF/dictionary-preview replacement (Gecko/XULRunner retirement).** A
  surface-migration + packaging problem. The dictionary preview/doc-view content is generated as
  managed XHTML/CSS (`Src/xWorks/LcmXhtmlGenerator.cs`, `ConfiguredLcmGenerator.cs`,
  `DictionaryExportService.cs`, `CssGenerator.cs`); **Gecko only *displays* it**, so the
  replacement is "render this HTML string in a managed view," not "rebuild the dictionary
  renderer." The dominant cost is the **process-wide XULRunner bootstrap** in
  `Src/Common/FieldWorks/FieldWorks.cs:164-192` (hard `ApplicationException` if the Firefox folder
  is missing) and the shutdown double-free hack at `:399-409` — these, not the leaf browser
  controls, are the true "Gecko removed" gate. PDF export is a single call site
  (`Src/xWorks/XhtmlDocView.cs:849-925`, invoking the renamed `FieldWorksPdfMaker.exe`) →
  `CoreWebView2.PrintToPdfAsync`.
- **10B — Graphite removal (managed/Avalonia path only).** A rendering-core + data-policy problem
  gated on Stage 9 proving HarfBuzz/managed shaping covers the formerly-Graphite scripts. The single
  selection point is `Src/Common/SimpleRootSite/RenderEngineFactory.cs:107-126`
  (`GetRenderingEngine`). **Native `GraphiteEngineClass` deletion is Stage 13, not here** — the
  `RenderEngineFactory` Graphite branch is shared by **legacy** WinForms+Views surfaces, so deleting
  it at Stage 10 breaks legacy rendering mid-program (review §6 Q1; synthesis §3/§7). 10B only removes
  Graphite from the managed/Avalonia path, runs the **G0–G3 classifier** as pre-removal evidence, and
  discharges the document/notify obligation from resolved §11.1. **`DefaultFontFeatures` is kept**
  (LCModel-owned, reused for OpenType export at `CssGenerator.cs:1562` and
  `WordStylesGenerator.cs:479,488` — do not delete).

**Acceptance criteria:**
- Both child epics (10A, 10B) closed against their independent gates.
- Per-region §7 Definition of Done satisfied for every migrated surface (semantic + visual +
  workflow + performance baselines, 100% + 150% DPI, `EngineIsolationAuditTests` green, retrospective
  folded into the skill set in the same PR).
- `EngineIsolationAuditTests` (`Src/Common/FwAvalonia/FwAvaloniaTests/EngineIsolationAuditTests.cs`)
  pass for the Avalonia path; Gecko/`GeckofxHtmlToPdf` and `GraphiteEngineClass` are absent from the
  Avalonia rendering path.
- `./build.ps1` + `./test.ps1` green.
- Superseded banners added to all four `graphite-transition-support` files (see Notes).

**Dependencies:**
- **Stage 9 (hard gate for 10B):** HarfBuzz coverage proof + the Stage 9.0 G0–G3 fixture-scan evidence.
- **Stage 12 (Avalonia 12) — 10A ordering tension:** the clean managed preview control
  (`Avalonia.Controls.WebView`) ships in Av12; see Notes / open questions.
- **Stages 6/7/8 (per-surface Gecko consumers):** the DOM-interaction surfaces land *with their
  owning surface stage*; 10A owns startup/shutdown decoupling, the PDF path, the shared
  `HtmlControl` wrapper, and packaging/installer.
- **liblcm/LCModel owners (10B):** deprecation coordination for `IsGraphiteEnabled` /
  `DefaultFontFeatures` (external repo cadence).
- **Stage 13:** native `GraphiteEngine` deletion + RegFree/build cleanup + Gecko harvest removal land
  there, not here.

**Rough size:** Large (parent). Two senior-led child epics; cross-cutting sweep across Stages 6/7/8
plus shell startup, a rendering-core change, and packaging/installer work.

---

## Sub-epics / stories

### 10A — Gecko / PDF / dictionary-preview replacement

**Summary:** Decouple the XULRunner bootstrap, replace the Gecko-bound preview/doc views with a
managed Avalonia/WebView2 HTML renderer, and replace PDF export with `CoreWebView2.PrintToPdfAsync`.

**Type:** Epic (child of Stage 10).

**Description:**
The preview is managed XHTML/CSS — Gecko only displays it — so the architecturally favorable case is
"render this HTML." **First task: make XULRunner init lazy/conditional** in `FieldWorks.cs` so
"Gecko absent" stops being a hard process-fail; this is the keystone that lets the rest of 10A proceed
incrementally. Gecko-bound display call sites: `XhtmlRecordDocView.cs:67`
(`new XWebBrowser(BrowserType.GeckoFx)`, the Lexical Edit preview pane, on by default),
`XhtmlDocView.cs:59` (Dictionary/Reversal/Classified doc views), `DictionaryConfigurationDlg.cs:44-49`
(configure-dictionary live preview, with `GeckoElement` DOM walking at `:228`). DOM-interaction
surfaces (config highlight; interlinear config via `AutoJSContext`/`GeckoInputElement`; parser trace
`WebPageInteractor.cs`) are the real engineering cost and are re-expressed as WebView2
`ExecuteScriptAsync`/message-channel interop or reworked so interaction happens in Avalonia — these
pieces land with their owning surface stages (6/7/8). PDF: single call site → `PrintToPdfAsync`,
collapsing the PDF-maker exe, the binding-redirect patch (`Build/PackageRestore.targets:477-520`), and
the ICE30 suppression (`Build/Installer.legacy.targets`) into in-box managed code.

**Acceptance criteria:**
- XULRunner bootstrap is lazy/optional; the app no longer hard-fails when the Firefox/XULRunner folder
  is absent; the shutdown double-free hack is removed.
- Dictionary preview + doc views render via the managed Avalonia HTML renderer with semantic/visual
  parity (§7 DoD, 100% + 150% DPI).
- PDF export uses `CoreWebView2.PrintToPdfAsync`; `FieldWorksPdfMaker.exe`/`GeckofxHtmlToPdf` harvest,
  binding-redirect patch, and ICE30 suppression are removed from the build/installer.
- Print parity (large-dictionary >10k-entry path and the small-print `Window.Print()` fallback) is met.
- No archived/CVE-bearing HTML→PDF lib (wkhtmltopdf/DinkToPdf) and no bundled-Chromium path
  (Puppeteer/Playwright) is introduced; QuestPDF rejected (not an HTML renderer).
- `EngineIsolationAuditTests` confirm Gecko symbols / `GeckofxHtmlToPdf` absent from the Avalonia path.

**Dependencies:** Stage 12 (Av12 WebView — see Notes; interim WebView2-direct on the 11.x line is the
alternative); per-surface DOM-interop pieces gated with Stages 6/7/8. Final Gecko harvest removal →
Stage 13.

**Labels:** `track-surfaces`, `lead-senior`, `parity-blocked-by:html-renderer`.

**Rough size:** Large. Startup decoupling + per-surface DOM-interop rewrites + packaging/installer
changes; cross-cutting across Stages 6/7/8 + shell startup.

---

### 10B — Graphite classify + managed-path removal

**Summary:** Run the G0–G3 coverage classifier as pre-removal evidence, then remove Graphite engine
selection from the managed/Avalonia rendering path (always returning Uniscribe/OpenType), keeping
`DefaultFontFeatures` for OpenType export. Native engine deletion is deferred to Stage 13.

**Type:** Epic (child of Stage 10).

**Description:**
Gated on Stage 9 proving HarfBuzz coverage. Graphite is well-isolated at the decision point: the single
selection branch is `RenderEngineFactory.cs:107-126` — on the managed/Avalonia path this becomes a
one-branch change that always returns the OpenType engine. The managed COM factory
(`Src/Common/ViewsInterfaces/Views.cs:2605-2767`, `GraphiteEngineClass`), the native engine
(`Src/views/lib/GraphiteEngine.{h,cpp}`, `GraphiteSegment.{h,cpp}`, `Src/views/views.vcxproj`,
`Lib/src/graphite2/`, `Build/Windows.targets`, `Build/RegFree.targets:50-51`), and WS-setup UI
(`DefaultFontsControl.cs`, `FontFeaturesButton.cs:421`, `FwWritingSystemSetupModel.cs:514-523`,
`GraphiteFontFeatures.cs`) are **not deleted here** — that wholesale native decommission + RegFree/build
cleanup lands in **Stage 13** because the shared `RenderEngineFactory` branch is still used by legacy
WinForms+Views surfaces during coexistence (review §6 Q1; synthesis §3/§7). **Keep
`DefaultFontFeatures`**: it is LCModel-owned, not Graphite-specific, and drives `CssGenerator.cs:1562` /
`WordStylesGenerator.cs:479,488` for OpenType export — only the Graphite *engine selection* and the
`GraphiteFontFeatures.ConvertFontFeatureCodesToIds` ID-conversion path retire. `IsGraphiteEnabled` is
read-as-false pending an LCModel-owner deprecation (coordinate with liblcm). Per resolved §11.1, the
G0–G3 scan is salvaged from `graphite-transition-support` as pre-removal *impact evidence* (which
projects break when Graphite is gone), feeding the document/notify obligation — see the dedicated story
below.

**Acceptance criteria:**
- G0–G3 classifier / fixture-LDML scan run; output enumerates the exact dropped-script list + affected
  projects as dropped-script evidence (not a go/no-go gate — removal proceeds per resolved §11.1).
- Graphite engine selection removed from the managed/Avalonia `RenderEngineFactory` path; the OpenType
  (Uniscribe/HarfBuzz) engine is always returned on that path.
- `GraphiteFontFeatures.ConvertFontFeatureCodesToIds` ID-conversion path retired.
- `DefaultFontFeatures` retained and verified still functioning for OpenType/Word-styles export.
- `IsGraphiteEnabled` treated as false on the managed path; **no native deletion**, no RegFree/build
  changes, no WS-setup UI removal in this epic (all deferred to Stage 13).
- `EngineIsolationAuditTests` confirm `GraphiteEngineClass` absent from the Avalonia path (audit
  graduates to whole-codebase absence only after Stage 13).
- Stage 9 HarfBuzz coverage proof is the entry gate; document/notify story (below) closed.

**Dependencies:** **Stage 9** (hard gate — HarfBuzz coverage proof + G0–G3 evidence); liblcm/LCModel
owners (property deprecation); **Stage 13** consumes the native deletion + RegFree/build cleanup.

**Labels:** `track-surfaces`, `lead-senior`, `parity-blocked-by:harfbuzz-coverage`.

**Rough size:** Medium. The managed-path change is small (one-branch); the weight is the classifier
evidence run, the keep/retire boundary discipline, and LCModel coordination.

---

### 10B-1 — Dropped-script documentation & user notification

**Summary:** Using the G0–G3 scan output, document the exact dropped (Graphite-only) scripts/projects
and notify affected users with migration guidance before removal ships.

**Type:** Story (under 10B).

**Description:**
Discharges the obligation incurred by resolved decision §11.1 ("accept the loss, document + notify").
HarfBuzz covers the large majority of formerly-Graphite scripts, **but Awami Nastaliq (Urdu/Arabic
Nastaliq) is Graphite-only with no OpenType/HarfBuzz path** and SIL has no OpenType replacement planned —
these are minority-language FieldWorks users. The Stage 9.0 LDML G0–G3 scan enumerates the exact dropped
scripts + affected projects; this story turns that into (a) published documentation of the loss and
(b) user notification with migration guidance. Salvaged from `graphite-transition-support`'s
font-replacement-policy + outreach tasks (its outreach obligation is *greater*, not lesser, under full
removal). The settings-preservation rule survives: read stored WS settings, never silently rewrite;
migrate only on explicit user action with undo.

**Acceptance criteria:**
- Dropped-script list (incl. Awami Nastaliq / Graphite-only) and affected-project inventory documented
  from the G0–G3 scan output.
- User-facing migration guidance published; affected users notified **before** removal ships.
- Settings-preservation rule honored (no silent rewrite of stored WS Graphite settings).
- Product-owner sign-off recorded on the accepted loss for Graphite-only projects.

**Dependencies:** Stage 9.0 G0–G3 scan output (source evidence); 10B managed-path removal (this story
must close before removal ships per §11.1).

**Labels:** `track-surfaces`, `lead-senior`, `user-comms`, `parity-blocked-by:harfbuzz-coverage`.

**Rough size:** Small–Medium. Non-engineering-heavy but a hard release gate; product-owner + outreach
coordination.

---

## Notes / open questions

- **S10 ↔ S12 WebView ordering tension (10A).** The clean managed preview/browser control,
  `Avalonia.Controls.WebView`, ships with **Avalonia 12 (Stage 12)** — yet the §5 dependency graph shows
  `S10 → S12`, implying 10 precedes 12. Resolve one of two ways: (a) 10A uses an **interim
  WebView2-direct integration** on the Avalonia 11.x line, or (b) 10A's managed-preview piece is
  **sequenced with/after Stage 12**. Either way the §5 mermaid edges need updating — 10A's preview work
  is not strictly before 12. (Review §4; synthesis §6/§7.)
- **Av12 WebView reintroduces a native runtime (Windows).** "Remove Gecko" trades XULRunner for the
  WebView2 runtime (OS-serviced, cross-platform, not bundled). Acceptable but not "zero native" —
  acknowledge rather than imply pure managed rendering. (Review §6 Q3.)
- **Native Graphite deletion deferred to Stage 13.** The `RenderEngineFactory` Graphite branch is shared
  by legacy WinForms+Views surfaces, so the native `GraphiteEngine`/`GraphiteSegment` deletion,
  `views.vcxproj`/`graphite2` build removal, RegFree manifest cleanup, and WS-setup UI removal land with
  the native Views decommission in **Stage 13** — deleting at Stage 10 would break legacy rendering
  mid-program. 10B removes Graphite only from the managed/Avalonia path. (Review §6 Q1; synthesis §3/§7.)
- **DOM-interaction interop strategy (10A).** Config-highlight, interlinear config, and parser-trace each
  use a different Gecko DOM-interop pattern; a per-surface decision (WebView2 `ExecuteScriptAsync` vs
  rework-in-Avalonia) is needed and is owned by the respective surface stage (6/7/8). (Review §6 Q4.)
- **`graphite-transition-support` superseded — already done.** Per synthesis §8 and review §5, superseded
  banners are added to all four files (`proposal.md`, `design.md`, `tasks.md`, spec delta); its core
  premise (keep Graphite, warn on Avalonia, sunset at M2) is reversed by "remove, not warn." Cross-refs
  in `lexical-edit-avalonia-migration` (§5 re-homing + `graphite-decommissioning.md` banner) point at
  Stage 10B. The G0–G3 classifier, font-outreach obligation, and settings-preservation rule are salvaged
  into 10B (above). *Banner work noted as already complete; tracked here for traceability only.*
- **liblcm coordination (10B).** `IsGraphiteEnabled` / `DefaultFontFeatures` are LCModel-owned (external);
  property deprecation depends on the liblcm repo + its release cadence. (Review §6 Q5.)
