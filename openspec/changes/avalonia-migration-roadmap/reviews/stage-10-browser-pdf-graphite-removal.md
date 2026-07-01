# Stage 10 Review — Browser/PDF & Dictionary-Preview Replacement; Graphite Full Removal

Reviewer: Claude (Opus 4.8). Date: 2026-06-15.
Scope under review: master plan §4 (stage table row 10), §6 Stage 10, §11 decision 1, and the
now-superseded `graphite-transition-support` change.

---

## 1. Scope assessment

**Verdict: the stage bundles two genuinely distinct workstreams that should be split into two
epics under one parent.** They share a theme ("retire native-bound rendering") but have
different blast radius, different gates, different risk profiles, and different critical-path
positions:

- **10A — Browser/PDF/dictionary-preview replacement (Gecko/XULRunner retirement).** This is a
  *surface-migration + packaging* problem. It is large (8 referencing projects, ~14 source call
  sites, a hard startup dependency, a renamed PDF exe, installer harvest, binding-redirect
  patching) and it spans areas owned by **other stages** (Stage 7 Interlinear/Discourse, Stage 8
  dictionary-config UI, Stage 6/parser, plus shell startup). It is a **Stage 13 cross-platform
  blocker** in its own right (XULRunner is Windows-only as packaged here).
- **10B — Graphite full removal.** This is a *rendering-core + native-decommission + data-policy*
  problem. It is gated hard on Stage 9 (HarfBuzz coverage proof), touches the native Views engine
  and `RenderEngineFactory`, and reaches into LCModel-owned model properties
  (`IsGraphiteEnabled`, `DefaultFontFeatures`) that this repo does not own.

These only weakly interact (the single real coupling is the Gecko `gfx.font_rendering.graphite`
preference and the `--graphite` flag passed to the PDF maker — both of which simply *disappear*
when Gecko goes away, requiring no Graphite-engine work). Splitting lets 10A proceed on the
Track-II surface cadence while 10B stays blocked behind Stage 9 without holding up browser
retirement. **Recommendation: keep Stage 10 as the parent epic; create child epics 10A (Gecko/PDF)
and 10B (Graphite removal) with independent gates.**

One scope gap in the current write-up: Stage 10's bullet names only `GeckoWebBrowser` and
`GeckofxHtmlToPdf`, but the dominant cost is the **process-wide XULRunner bootstrap** in
`Src/Common/FieldWorks/FieldWorks.cs:164-192` (hard `ApplicationException` if the Firefox folder is
missing) and the **shutdown double-free hack** at `:399-409`. The stage scope text should name the
startup/shutdown coupling explicitly — it is the true gate for "Gecko removed from codebase," not
the leaf browser controls.

---

## 2. Feasibility (repo-grounded + web)

### 2a. How dictionary preview is rendered today

The preview/doc-view pipeline is fully repo-owned and **does not depend on Gecko to produce
content** — only to *display* it:

- XHTML generation: `Src/xWorks/LcmXhtmlGenerator.cs`, `ConfiguredLcmGenerator.cs`,
  `DictionaryExportService.cs`; CSS from `Src/xWorks/CssGenerator.cs`. These are managed and
  HarfBuzz/OpenType-agnostic — they emit HTML + CSS.
- Display controls (the Gecko-bound part):
  - `Src/xWorks/XhtmlRecordDocView.cs:67` — `new XWebBrowser(BrowserType.GeckoFx)`, the **preview
    pane inside Lexical Edit** (on by default).
  - `Src/xWorks/XhtmlDocView.cs:59` — Dictionary/Reversal/Classified doc views.
  - `Src/xWorks/DictionaryConfigurationDlg.cs:44-49` — configure-dictionary live preview (uses
    `GeckoElement` DOM walking at `:228` to highlight config changes).

**This is the architecturally favorable case:** because the content is generated HTML/CSS, the
replacement is "render this HTML string in a managed view," not "rebuild the dictionary renderer."

### 2b. What replaces it in managed Avalonia

Two viable managed targets, both confirmed by web research:

1. **Avalonia 12 built-in `Avalonia.Controls.WebView`** (NuGet `Avalonia.Controls.WebView`, v12.x,
   now open-source and in-box). It wraps the *native* platform engine: WebView2 (Windows),
   WebKit (macOS), WebKitGTK (Linux), with bidirectional JS interop and no bundled Chromium.
   This is the natural fit and is **cross-platform**, which directly unblocks Stage 13. It does,
   however, reintroduce a native dependency per platform (WebView2 runtime on Windows). Crucially
   it **lands with Stage 12 (Avalonia 12)** — so Stage 10A's *managed* preview cannot fully land
   before Stage 12 unless an interim WebView2-direct integration is used.
   (https://www.nuget.org/packages/Avalonia.Controls.WebView/,
   https://docs.avaloniaui.net/docs/app-development/embedding-web-content)
2. **A managed HTML-render-to-Avalonia path** for the *highlight/interaction*-light surfaces
   (the simpler help panes). Heavier, generally not worth it given option 1.

For the **DOM-interaction** surfaces (config-dialog highlighting via `GeckoElement`; interlinear
config via `AutoJSContext`/`GeckoInputElement` in `ConfigureInterlinDialog.cs`; parser trace via
`WebPageInteractor.cs`), the migration is non-trivial: each uses a different Gecko DOM-interop
pattern and must be re-expressed as WebView2 `ExecuteScriptAsync`/message-channel interop, or
(better) reworked so interaction happens in Avalonia rather than in the document DOM. These are
the real engineering cost in 10A and are spread across **Stages 6/7/8**, reinforcing the split.

### 2c. PDF export — well isolated

PDF export is a **single runtime call site**: `Src/xWorks/XhtmlDocView.cs:849-925`
(`GeneratePdfToPrint`), invoking `FieldWorksPdfMaker.exe` (the renamed `GeckofxHtmlToPdf`) with
`"<xhtml>" "<pdf>" --graphite --reduce-memory`, used only for large dictionaries (>10k entries) and
as a print fallback. Smaller prints use Gecko `Window.Print()` (`:963-967`). Packaging:
`Build/PackageRestore.targets:477-520` downloads/renames the exe and patches its binding redirect;
`Build/Installer.legacy.targets` suppresses an ICE30 duplicate-file warning.

Replacement options (web-confirmed):
- If Avalonia WebView2 is adopted, **`CoreWebView2.PrintToPdfAsync`** replaces both the PDF-maker
  exe and `Window.Print()` with one managed API — the cleanest path and it removes the harvested
  exe entirely.
- `wkhtmltopdf`/DinkToPdf are **archived (2023/2024) with unpatched CVEs — do not adopt**
  (https://medium.com/iron-software/whatever-happened-to-wkhtmltopdf-and-dinktopdf-916dc6cdb1cc).
- QuestPDF is a *programmatic* PDF builder, **not** an HTML renderer, and is revenue-license-gated
  above $1M — wrong tool for "render existing dictionary XHTML." Headless Chromium (PuppeteerSharp/
  Playwright) is the only other faithful HTML→PDF path but reintroduces a bundled browser, which
  contradicts the migration's "shed native deps" charter.

**Feasibility conclusion:** 10A is feasible and the content side is favorable; the cost is the
startup decoupling + the per-surface DOM-interop rewrites + packaging/installer changes, and the
clean version of it is naturally an **Avalonia-12 / Stage-12-aligned** piece of work.

### 2d. Graphite removal — feasibility

Graphite is **surprisingly well-isolated at the decision point** but reaches into model + UI + build:

- Single selection point: `Src/Common/SimpleRootSite/RenderEngineFactory.cs:107-126`
  (`GetRenderingEngine`) — `if (ws.IsGraphiteEnabled) { GraphiteEngineClass.Create(); ... if
  (graphiteEngine.FontIsValid) return it; else release and fall through to Uniscribe }`. Removal
  here is a one-branch deletion that always returns the Uniscribe/OpenType engine.
- Managed COM factory: `Src/Common/ViewsInterfaces/Views.cs:2605-2767` (`GraphiteEngineClass`).
- Native engine: `Src/views/lib/GraphiteEngine.{h,cpp}`, `GraphiteSegment.{h,cpp}`; project entries
  in `Src/views/views.vcxproj`; native lib `Lib/src/graphite2/` built by the `graphite2Windows`
  target in `Build/Windows.targets:64,69,220-256`; RegFree manifest entry in
  `Build/RegFree.targets:50-51`. Native deletion is wholesale and self-contained.
- WS-setup UI: `Src/FwCoreDlgs/FwCoreDlgControls/DefaultFontsControl.cs` (Graphite checkbox/groupbox),
  `FontFeaturesButton.cs:421` (`GraphiteEngineClass.Create()` for feature labels),
  `FwWritingSystemSetupModel.cs:514-523`. Feature utility: `Src/Common/FwUtils/GraphiteFontFeatures.cs`.
- **Model properties are LCModel-owned (external):** `IsGraphiteEnabled` and `DefaultFontFeatures`
  live on `CoreWritingSystemDefinition` in SIL.LCModel, not this repo. They cannot be *deleted*
  here; they can only be ignored (treated as always-OpenType). Note `DefaultFontFeatures` is **not
  Graphite-specific** — it also drives `CssGenerator.cs:1562` and `WordStylesGenerator.cs:479,488`
  for export, so it must **survive** as an OpenType feature string. Only the *Graphite engine
  selection* and the *Graphite feature-ID conversion* path retire.

**Is removal safe once Stage 9 lands?** Conditionally yes, and the gate is correctly placed:
removal is safe **only if** the Stage 9 spike proves HarfBuzz covers the scripts Graphite formerly
handled. The repo's own `graphite-transition-support/design.md` flags the hard cases — **Awami
Nastaliq is Graphite-only with no OpenType replacement** today, and dual-engine fonts with
*Graphite feature strings* (tier G2) render differently under OpenType. Removing Graphite entirely
makes *those projects' rendering change on legacy surfaces too* (because `RenderEngineFactory` is
shared by WinForms and any surviving native-Views surface, not just Avalonia). This is the single
biggest correctness risk in the whole stage and is **stronger than the superseded plan assumed** —
see §6.

---

## 3. Best practices

- **Decouple the startup bootstrap first.** Make XULRunner init lazy/conditional before touching
  any consumer, so "Gecko absent" stops being a hard process-fail. This is the keystone that lets
  10A proceed incrementally.
- **Prefer one managed PDF API over a harvested exe.** `CoreWebView2.PrintToPdfAsync` collapses
  three things (PDF-maker exe, binding-redirect patch, ICE30 suppression) into in-box managed code.
- **Do not adopt archived/CVE-bearing HTML→PDF libs** (wkhtmltopdf/DinkToPdf). Avoid reintroducing
  a bundled Chromium (Puppeteer/Playwright) — it fights the charter.
- **Treat `DefaultFontFeatures` as OpenType, never delete it.** Retire only the Graphite *engine
  selection* and the `GraphiteFontFeatures.ConvertFontFeatureCodesToIds` ID-conversion path.
- **Keep the forbidden-symbol audit unchanged** — it already lists `GraphiteEngineClass`,
  `UniscribeEngineClass`, `FwGrEngine`, `GraphiteSegment`, Gecko symbols, `GeckofxHtmlToPdf`
  (`Src/Common/FwAvalonia/FwAvaloniaTests/EngineIsolationAuditTests.cs`). After 10B the audit
  graduates from "not on the Avalonia path" to "absent from the whole codebase."
- **Coordinate the LCModel property deprecation with liblcm owners** rather than mutating in-repo.
- **Measure before sunsetting** (the one transition-support practice worth keeping): run the
  fixture/LDML scan for G2/G3 (Graphite-feature and Graphite-only) prevalence so removal is
  evidence-based, not calendar-based.

---

## 4. Interactions & dependencies

- **Stage 9 (hard gate, correct).** 10B removal is gated on Stage 9 proving HarfBuzz coverage. The
  master plan already states this (§6 Stage 9, §11 decision 1, risk register). Keep it; tighten it
  to require the *fixture-scan evidence*, not just a spike opinion.
- **Stage 12 (Avalonia 12) — under-acknowledged dependency.** The clean managed browser/preview
  replacement (`Avalonia.Controls.WebView`) ships in Avalonia 12. So **10A's preferred
  implementation is Stage-12-aligned**, yet the dependency graph (§5) shows `S10 → S12`, implying
  10 precedes 12. Either (a) 10A uses an interim WebView2-direct integration on the 11.x line, or
  (b) 10A's managed-preview piece is sequenced *with/after* Stage 12. This ordering tension is not
  currently called out and should be.
- **Stage 13 (cross-platform).** XULRunner is the packaged Windows-only blocker; retiring it (10A)
  is a prerequisite for Linux/macOS. Native WebView via Avalonia 12 is cross-platform, so 10A done
  right *advances* Stage 13 rather than just unblocking it.
- **Stage 7 / Stage 8 / Stage 6.** The Gecko consumers are spread across these surfaces
  (Interlinear/Discourse config preview → Stage 7; dictionary-config preview → Stage 8; parser
  trace, MGA help, SFM import help → Stage 6/misc). 10A cannot be a single self-contained senior
  task; it is a **cross-cutting sweep** whose per-surface pieces should land *with each owning
  surface stage*, leaving 10A to own the startup/shutdown decoupling, the PDF path, the shared
  `XCore.HtmlControl` wrapper, and packaging. The plan should say this.
- **Print parity** interacts with Stage 11 shell (print menu/command wiring) and the global undo
  model only loosely; print is essentially read-only rendering.

---

## 5. Recommended plan changes

1. **Split Stage 10 into 10A (Gecko/PDF/preview) and 10B (Graphite removal)** under one parent
   epic, with independent gates (10A: surface + packaging + cross-platform; 10B: Stage-9 HarfBuzz
   evidence + LCModel coordination).
2. **Add the startup/shutdown XULRunner coupling to the stage scope text** and make "lazy/optional
   XULRunner init" the first 10A task — it is the real "Gecko removed" gate.
3. **Note the Stage-12/Avalonia-12 dependency for the managed browser control** explicitly, and
   decide interim-WebView2-on-11.x vs sequence-10A-with-12. Update the §5 mermaid edges accordingly
   (10A's preview work is not strictly before 12).
4. **Reassign the per-surface Gecko consumers to their owning surface stages** (7/8/6); keep 10A
   owning startup, PDF, shared `HtmlControl`, and packaging/installer.
5. **Specify the PDF replacement as `CoreWebView2.PrintToPdfAsync`** (default), explicitly rejecting
   wkhtmltopdf/DinkToPdf (CVE/archived) and QuestPDF (not an HTML renderer).
6. **Clarify 10B scope on model properties:** retire Graphite *engine selection* + ID-conversion;
   **keep** `DefaultFontFeatures` (OpenType) and `IsGraphiteEnabled` (read-as-false) pending an
   LCModel-owner deprecation; flag the liblcm coordination as an explicit dependency.

### graphite-transition-support supersession handling

The master plan (§11 decision 1, §2 principle 13, §6 Stage 10) already declares
`graphite-transition-support` superseded by "remove, not warn." **The change itself is not yet
updated** — it still presents Path A (legacy-harbor + graded warning) as the decision and defines
the G0–G3 classifier, warning UX, sunset milestones, and Path B pivot. Concretely:

- **Archive / add a superseded banner** to `graphite-transition-support/proposal.md`,
  `design.md`, `tasks.md`, and its spec delta. Its core premise (keep Graphite, warn on Avalonia,
  sunset at M2) is now reversed: there is no Avalonia warning tier because there is no Graphite at
  all post-Stage-10.
- **Salvage and re-home three still-valuable pieces into Stage 10B** (do not lose them):
  1. The **G0–G3 classifier / fixture-scan** (tasks 1.3, 1.4, 3.2) — repurposed as
     *pre-removal impact evidence* ("how many projects break when Graphite is gone"), feeding the
     Stage-9 HarfBuzz-coverage gate.
  2. The **font-replacement policy + outreach** (tasks 3.3, 4.3) — still needed; Awami Nastaliq /
     Graphite-only projects now have *no* in-app fidelity path, so the migration/outreach
     obligation is *greater*, not lesser.
  3. The **settings-preservation rule** (read storage, never silently rewrite; migrate only on
     explicit user action with undo) — still correct under full removal.
- **Update the cross-references** in `lexical-edit-avalonia-migration` (its section-5 re-homing and
  `graphite-decommissioning.md` banner) to point at Stage 10B rather than the warning-based change.
- **Reconcile the contradiction in the design's premise:** transition-support assumed Graphite
  removal was *coupled to WinForms deletion at M3* and that **legacy surfaces keep rendering
  Graphite**. Full removal from the codebase means **legacy/native-Views surfaces also lose
  Graphite** (shared `RenderEngineFactory`). The roadmap should state whether full removal waits
  until WinForms/native Views are themselves retired (Stage 13) — otherwise Graphite-only projects
  break on the *legacy* surface mid-program, which transition-support explicitly promised they
  would not. **This is a real sequencing decision the plan has not yet made.** (See Open Questions.)

---

## 6. Open questions / risks

1. **Does full Graphite removal happen at Stage 10, or is it deferred to Stage 13 (with WinForms/
   native-Views decommission)?** Because `RenderEngineFactory` is shared, deleting the Graphite
   branch at Stage 10 degrades *legacy* surfaces too, breaking the transition-support promise that
   legacy keeps rendering Graphite until WinForms goes. Likely resolution: **10B disables the
   Graphite engine selection on the managed/Avalonia path and stops shipping new Graphite enablement
   at Stage 10, but the native `GraphiteEngine` deletion + RegFree/build cleanup lands with the
   native decommission in Stage 13.** The plan should make this explicit. (High impact.)
2. **Awami Nastaliq / Graphite-only fonts with no OpenType replacement** — confirmed by the repo's
   own design doc. After removal these render wrong everywhere. Needs product-owner sign-off and a
   replacement-or-freeze decision per affected project before removal. (High.)
3. **Avalonia 12 WebView2 = a *new* native runtime dependency on Windows.** "Remove Gecko" trades
   XULRunner for the WebView2 runtime. Acceptable (WebView2 is OS-serviced, cross-platform, not
   bundled) but it is not "zero native" — the plan should acknowledge it rather than imply pure
   managed rendering. (Med.)
4. **DOM-interaction surfaces** (config highlight, interlinear config, parser trace) are the real
   10A engineering cost and need a per-surface interop strategy decision (WebView2
   `ExecuteScriptAsync` vs rework-in-Avalonia). (Med.)
5. **LCModel coordination** for `IsGraphiteEnabled`/`DefaultFontFeatures` deprecation is an external
   dependency on the liblcm repo and its release cadence. (Med.)

---

## 7. Confidence

**High** on the repo-grounded inventory (Gecko/PDF call sites, the single `RenderEngineFactory`
decision point, native/build/RegFree footprint, model-property ownership) — these were verified
against the existing `gecko-pdf-audit.md` and read directly.
**Medium-high** on the split recommendation and the salvage list (clear from structure).
**Medium** on the exact sequencing fixes (Stage-10-vs-13 Graphite-deletion timing; Stage-12
WebView dependency) — these are genuine plan decisions the owner must make; I have surfaced the
tension and the most likely resolution but the choice is theirs.
