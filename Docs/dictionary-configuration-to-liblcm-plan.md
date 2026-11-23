# Plan: Move the Dictionary Configuration Model, Core Generator, XHTML/CSS Writers, and Word Writer into liblcm

> **Temporary document.** This plan is kept in the repo while the project runs. It is deleted when Phase 2 finishes (2.E).

## Preamble: how content and style stay aligned

The core generator does not write any output format and does not decide any styling. For each entry, it walks the dictionary configuration tree, reads the LCM data each enabled node asks for, and applies the configuration's filtering (enabled nodes, list options, publications, sense numbering, shared nodes). Two pluggable writers produce the actual output:

- **The content writer (`ILcmContentGenerator`)** produces the structure: one element (or run, or fragment) per configuration node the engine visits, nested the way the configuration tree is nested.
- **The styles writer (`ILcmStylesGenerator`)** produces the styling rules for those nodes.

**Alignment comes from the class name.** At each node, the engine first calls `StylesGenerator.AddStyles(nodeList)`, passing the path of configuration nodes from the entry's root down to the current node.

- The styles writer generates (or reuses) the rules for that node *in that context* and returns a class name that is unique to that path.
- The engine passes that exact name to the content writer, which stamps it onto the element it emits.

So structure and style line up by construction. The styles writer issues every name, and the content writer only uses the names it is given. Because names are keyed by path, a node that appears in several contexts (for example, a shared Senses node used by both main entries and subentries) can be styled differently in each one.

Base names come from a single node-naming policy (`GetClassAttributeForConfig` today; `ConfigurationNodeNaming` after this move). The styles writer adds suffixes where it needs uniqueness. Two other inputs:

- Model-wide styles are produced once per run by `AddGlobalStyles`.
- Writing-system-specific run styling goes through the content writer's `SetRunStyle`.

FieldWorks has two pairs of writers today:

- **XHTML + CSS:** `LcmXhtmlGenerator` writes `class` attributes, and `CssGenerator` writes selectors built from the same names.
- **Word:** `LcmWordGenerator` implements both interfaces, so the names become paragraph and character style names.

Both pairs move to liblcm with the engine, into the new `SIL.LCModel.DictionaryConfiguration` assembly. The JSON writer, which reuses CSS for Webonary, stays in FieldWorks. Any future format supplies its own pair against the same contract.

## Baseline

| Repo | Ref | Commit |
|---|---|---|
| FieldWorks | `origin/main` | `412ee26b8` (2026-11-05) |
| liblcm | `origin/master` | `da7140f0` |

Line numbers below refer to these commits. Paths are relative to `Src/xWorks/` unless shown otherwise.

## Goal

Move the dictionary configuration model, its parsing and migration, the core configured-output engine (`ConfiguredLcmGenerator`), and the XHTML, CSS, and Word writers from FieldWorks into a new liblcm project, `SIL.LCModel.DictionaryConfiguration`. Lexbox will be the first consumer outside FieldWorks. Its initial target is XHTML and CSS for the future Webonary 2.0, and it wants to offer Word (.docx) output later. Using the liblcm engine and writers means its output follows the user's dictionary configuration.

FieldWorks keeps the JSON writer, the FW-specific document drivers (preview, paging, export progress), and all UI. Where FieldWorks-specific types have crept into the code that moves, they are extracted first.

**Scope rule:** the plan does only what the move requires, plus what the export needs to run on Linux in Lexbox. It does not otherwise enhance the XHTML or Word export. The approved exceptions are listed where they occur (0.E, Word media copying). Other improvements go to Follow-ups.

## Scope

### Moves to liblcm (new project `SIL.LCModel.DictionaryConfiguration`)

- **Model:** `DictionaryConfigurationModel`, `ConfigurableDictionaryNode`, `DictionaryNodeOptions` and its subclasses.
- **Parsing and load-time synchronization with the project.** Today this lives in `DictionaryConfigurationController` statics; see 0.H.
- **Migration:** `FirstAlphaMigrator`, `FirstBetaMigrator`, `IDictionaryConfigurationMigrator` (refactored), and the pure parts of `DictionaryConfigurationMigrator`: `VersionCurrent`, `PerformActionOnNodes`, `BuildPathStringFromNode`, `NodePathSeparator`, `GetConfigsNeedingMigration`, `ConfigFilesInDir`, the file-name constants, and `SetWritingSystemForReversalModel`.
- **Engine:** `ConfiguredLcmGenerator`, `GeneratorSettings`, `ConfigFragment`, `ILcmContentGenerator`, `ILcmStylesGenerator`, `IFragmentWriter`, `IFragment`.
- **Style resolution:** `ExportStyleInfo` and a new `DictionaryStyleResolver` that turns a project style into a format-neutral resolved style (0.L).
- **XHTML and CSS writers (0.M):**
  - `LcmXhtmlGenerator`'s `ILcmContentGenerator` implementation, the XHTML template generation used by Webonary (`GenerateXHTMLTemplatesForConfigurationModel`), and a new clerk-free `XhtmlDocumentWriter` that holds the document skeleton (opening and closing HTML, stylesheet links, writing-system metadata, letter-heading markup)
  - `CssGenerator`'s `ILcmStylesGenerator` implementation and rule generation, `GenerateLetterHeaderCss`, and the custom-CSS lookup (`CopyCustomCssAndGetPath`)
  - `SpecialCharacterHandling` (today in the dialog's `DictionaryDetailsView` folder)
  - a dependency on `SIL.ExCSS` 2.1.0, retargeted to `netstandard2.0;net462` (0.N)
- **Word writer (0.O-1 to 0.O-3):**
  - `LcmWordGenerator`'s `ILcmContentGenerator` and `ILcmStylesGenerator` implementation, `WordStylesGenerator`, and `WordStyleCollection`
  - `SavePublishedDocx`, with the same shape, minus its FW types (0.O-3)
  - image sizing rewritten without WPF (0.O-1)
  - a dependency on `DocumentFormat.OpenXml` 2.20.0 (has `net46`, `netstandard2.0`, and `net6.0` builds)
- **Publication filtering:** `DictionaryPublicationDecorator`, without its PropertyTable/clerk methods.
- **Letter headings:** the lead-character logic from `Src/Common/Controls/XMLViews/ConfiguredExport.cs`, `GetSortWordForLetterHead`, and a collator helper equivalent to `FwUtils.GetCollatorForWs`.
- **`ReversalIndexServices`** (creates and removes reversal-index config files). It is pure LCM and it is configuration-file maintenance.
- **Shipped defaults:** `DistFiles/Language Explorer/DefaultConfigurations/**` (Dictionary: Root, Lexeme, Hybrid; ReversalIndex: AllReversalIndexes; Classified Dictionary: SemanticDomainSenses).
- **Schema:** `DistFiles/Language Explorer/Configuration/DictionaryConfiguration.xsd`.

### Stays in FieldWorks

- **Writers:** `LcmJsonGenerator`.
- **Word export caller code (0.O-3):** `DictionaryExportService` maps the clerk to a sort kind and supplies the media resolver and scheduler. It maps `DocxExportStage` to the localized `xWorksStrings` progress messages.
- **FW document drivers for XHTML (0.M):** `SavePreviewHtmlWithStyles`, `SavePublishedHtmlWithStyles`, `GenerateEntryHtmlWithStyles`, `GenerateNextFewEntries`, page buttons and page ranges, the current-entry marker, preview paths, and progress messages. Also the preview-only CSS (`GenerateCssForPageButtons`, `GenerateCssForSelectedEntry`).
- **Config UI:** `DictionaryConfigurationDlg`, `DictionaryConfigurationController`, the Manager and Import dialogs and controllers, `DictionaryDetailsController` with `DictionaryDetailsView/*`, and `DictionaryConfigurationTreeControl`.
- **Orchestration:** `DictionaryConfigurationListener`, the `DictionaryConfigurationMigrator` orchestration shell (runs at project open, creates custom CSS), and `PreHistoricMigrator`.
- **Export and publishing:** `DictionaryExportService`, Webonary upload (`UploadToWebonary*`, `WebonaryClient`), `XhtmlDocView` and preview, and `WavConverter`.

## Decisions made in the design session

1. **The XHTML, CSS, and Word writers move; JSON stays.** All export writers move together so that the engine, the style resolver (0.L), and every writer that translates its output live and are tested in one place, and interface changes never cross repositories. Webonary 2.0 (XHTML and CSS) is Lexbox's initial target; Lexbox wants to offer Word in the future, and consolidating now keeps that path open. FW-specific document code (preview, paging, progress) is split off and stays.
2. **Decompose `PropertyTable` and `RecordClerk`; do not abstract them.** `GeneratorSettings` gets explicit typed inputs.
3. **Entry selection and ordering belong to the caller,** through an entry-source interface. liblcm supplies a clerk-free default; FieldWorks supplies a clerk adapter.
4. **Static functions, no static data.** The engine stays a set of static functions over (settings, model, decorator, object). All mutable static state is removed.
5. **Shipped default configurations move into liblcm.** They are embedded resources behind `IDefaultConfigurationSource`. FieldWorks extracts them at build time for its installer and reads the extracted files at runtime, as it does today (2.B).
6. **Public API surface:**
   - **Group A, the members any generator needs, become public.**
   - **Group B, the members the config dialog needs, use option 3:** read-only queries become public; mutation stays internal except for explicit public mutation methods on the model and node types.
   - **Everything else stays internal.** Test-only internals stay internal because those tests move to liblcm.
7. **The core behavioral tests are split.** Traversal, filter, publication, XHTML, CSS, and Word tests move to `SIL.LCModel.DictionaryConfiguration.Tests` and run against the real writers. Tests for the JSON writer, preview, and paging stay in FieldWorks. The test project is published as a package, and its public `DictionaryConfigurationTestHelpers` class serves FW's remaining tests.
8. **Everything goes into one new project and assembly, `SIL.LCModel.DictionaryConfiguration`, in `LCM.sln`.** That covers the model, engine, migrators, default configurations, and all three writers. `SIL.LCModel` is unchanged, so FLExBridge and LfMerge take no new dependencies. Namespaces: `SIL.LCModel.DictionaryConfiguration` for the model, `.Generation` for the engine, `.Generation.Xhtml` for XHTML and CSS, and `.Generation.Word` for Word. All of this code compiles against liblcm's public API today, so the new assembly needs no `InternalsVisibleTo` from `SIL.LCModel`. The project sets `<Nullable>disable</Nullable>`.
9. **Phasing:** finish every extraction inside FieldWorks first (Phase 0), then move the code (Phase 1), then switch FieldWorks over (Phase 2). Phase 0 and Phase 2 land on FW `main`. No FW release depends on intermediate work. Phase 0 may land as several larger commits or PRs, and each intermediate build is integration-tested with the 0.P smoke list.
10. **One media-resolver hook** replaces the engine's file copying. The resolver is set on `GeneratorSettings`, so it applies to one run: a host passes a URL resolver for XHTML and a local-path resolver for Word.
11. **Style resolution moves to liblcm as a format-neutral model.** A resolver turns a project style, writing system, and node path into resolved paragraph and character properties. Writers only translate that result into their format. The moved CSS and Word writers are its first consumers, and both are tested in liblcm.
12. **`SIL.ExCSS` stays the CSS object model and becomes a dependency of the new project.** It is retargeted rather than replaced. Its repo moves to sillsdev so CI can publish it (0.N). ExCSS types stay out of the public API.
13. **`DocumentFormat.OpenXml` 2.20.0 becomes a dependency of the new project.** No OpenXml types appear in the public API.
14. **The new package is versioned and released with liblcm** (same GitVersion, same release). A writer fix ships in the next liblcm beta followed by an FW `SilLcmVersion` bump. A hotfix to a released FW needs a liblcm patch release from the version it pins. The workflow is documented in 2.E. Word development has settled down, so the extra release step for Word fixes is acceptable.

## Target architecture

```
liblcm — SIL.LCModel (unchanged)
        ▲
liblcm — SIL.LCModel.DictionaryConfiguration (new project and package; depends on SIL.ExCSS, DocumentFormat.OpenXml)
  SIL.LCModel.DictionaryConfiguration
    DictionaryConfigurationModel, ConfigurableDictionaryNode, DictionaryNodeOptions*
    load + sync-with-project (custom fields, types, styles, writing systems, publications)
    FirstAlphaMigrator, FirstBetaMigrator, version constants, node path helpers
    IDefaultConfigurationSource (+ embedded-resource implementation), DictionaryConfiguration.xsd
    ReversalIndexServices
    DictionaryPublicationDecorator (publication filtering only)
  SIL.LCModel.DictionaryConfiguration.Generation
    ConfiguredLcmGenerator (static functions), GeneratorSettings (per-run context)
    ILcmContentGenerator, ILcmStylesGenerator, IFragmentWriter, IFragment
    IDictionaryEntrySource + DefaultDictionaryEntrySource
    LetterHeadService (lead char, sort word by LetterHeadSortKind)
    IDictionaryMediaResolver + default absolute-path/URI resolver
    IGenerationScheduler (parallel strategy) + default
    ConfigurationNodeNaming (CSS class-name policy shared by content + styles)
    DictionaryStyleResolver → ResolvedStyle (format-neutral style values), ExportStyleInfo
  SIL.LCModel.DictionaryConfiguration.Generation.Xhtml
    LcmXhtmlGenerator : ILcmContentGenerator, XhtmlDocumentWriter, XHTML templates
    CssGenerator : ILcmStylesGenerator (uses SIL.ExCSS internally), SpecialCharacterHandling
  SIL.LCModel.DictionaryConfiguration.Generation.Word
    LcmWordGenerator : ILcmContentGenerator, ILcmStylesGenerator (uses DocumentFormat.OpenXml)
    LcmWordGenerator.SavePublishedDocx, DocxExportStage
    WordStylesGenerator, WordStyleCollection, image size reader
        ▲ one-way dependency
FieldWorks — xWorks
  ClerkDictionaryEntrySource : IDictionaryEntrySource
  FwExportMediaResolver : IDictionaryMediaResolver (relative paths, copy, dedupe, WavToMp3)
  FwStaGenerationScheduler : IGenerationScheduler (STA threads for COM)
  LcmJsonGenerator
  FwXhtmlDocuments (preview, paging, export drivers, preview-only CSS)
  Word export call in DictionaryExportService (sort kind, resolver, stage → progress text) → SavePublishedDocx
  DictionaryExportService, Webonary upload, config dialogs, PreHistoricMigrator
```

## Notes for Lexbox implementers

These follow from the design rather than from any single phase.

- **Lexbox uses the moved XHTML and CSS writers.** It creates a `LcmXhtmlGenerator` and `CssGenerator` pair, builds `GeneratorSettings`, and calls `ConfiguredLcmGenerator.GenerateContentForEntry` per entry. `XhtmlDocumentWriter` assembles a document the way FW's export does.
- **Lexbox references the `SIL.LCModel.DictionaryConfiguration` package**, versioned with `SIL.LCModel`.
- **Set `IsWebExport = true`** on `GeneratorSettings`. Otherwise the XHTML contains FW's preview-only `nodeId`, `sourceGuid`, and `sourceField` attributes.
- **Word (.docx) is available for later** through `LcmWordGenerator.SavePublishedDocx`, and matches FW's Word export. Word embeds pictures, so a Word run needs a media resolver that returns local file paths, not URLs. The whole document is built in memory, so memory use grows with dictionary size and pictures.
- **File names are case-sensitive on Linux.** On Windows, a `LinkedFiles` path that differs from the disk only in case still resolves. Lexbox's media resolver has to handle such mismatches.
- **Line endings follow the platform.** The writers use `Environment.NewLine`, so XHTML and CSS produced on Linux has `\n` where FW's has `\r\n`.
- **The caller still owns everything above the document skeleton:** entry selection and order (`IDictionaryEntrySource`), paging, splitting out reversal indexes, media URLs (`IDictionaryMediaResolver`), and site metadata.
- **Writers are used for one run.** The engine generates entries in parallel, so `AddStyles` and the content calls arrive from worker threads. Create a new writer pair for each export, and read the CSS (`GetStylesString`) after generation finishes. After 0.O-2 the same holds for the Word writer; today its state is process-wide.
- **The project must not change during a generation run.** Generation threads read the `LcmCache` concurrently, so a server must not let sync or edit jobs write to the project while a run is in progress.
- **The output matches FieldWorks, including its legacy CSS.** That includes the class names, the `flex-line-height` property kept for Pathway, and the bidirectional shim written for GeckoFx. Changing any of it changes FW output too.
- **User CSS overrides need further design.** FieldWorks users can keep `*Overrides*` CSS files (synced by Send/Receive) written against FW's generated class names. Because the class names now come from the shared writer, those files would apply to Lexbox output, and `CopyCustomCssAndGetPath` finds them. Whether Lexbox should honor them, and how users edit them there, needs its own design; one option is a separate Lexbox UX for specific style overrides. Only a handful of users rely on this.
- **Writing another output format.** A new writer pair reads punctuation and layout settings from the configuration node passed to every writer call, either as `config` or as `nodeList` (the path from the root; the last item is the current node):
  - `ConfigurableDictionaryNode.Before`, `.After`, and `.Between`. The `first` / `firstItem` flags on the collection and sense calls tell the writer when `Between` applies.
  - `ConfigurableDictionaryNode.Style` and `.StyleType`.
  - `DictionaryNodeOptions`, by subtype:
    - `DictionaryNodeSenseOptions`: `BeforeNumber`, `AfterNumber`, `NumberStyle`, `DisplayEachSenseInAParagraph`, `DisplayFirstSenseInline`
    - `IParaOption.DisplayEachInAParagraph` (lists, grouping, notes)
    - `DictionaryNodeWritingSystemOptions.DisplayWritingSystemAbbreviations` (the engine reads it and passes it to `GenerateWsPrefixWithString`)

  Style values come from `DictionaryStyleResolver` (0.L); the writer translates `ResolvedStyle` into its format.

---

## Phase 0: Extraction inside FieldWorks

**Rule for the whole phase:** no liblcm changes. All work lands on FieldWorks `main`, protected by the existing xWorks test suite. Behavior stays the same unless an item says otherwise. No FW release depends on this work. Items may be grouped into larger PRs, and each intermediate build is integration-tested with the 0.P smoke list.

**Exit criterion:** every file on the "moves" list compiles in the scratch project, which does not depend on FW.

**The scratch project.** Moving files still compile inside `xWorks`, which references every FW assembly, so a leftover FW dependency would go unnoticed until Phase 1. Add a throwaway project, for example `Build/DictionaryConfigurationScratch/DictionaryConfigurationScratch.csproj`, created in the first Phase 0 PR:
- It owns no code. It includes each moving file by path (`<Compile Include="..\..\Src\xWorks\ConfiguredLcmGenerator.cs" />`) and gains entries as items make files ready.
- It targets `netstandard2.0;net462;net8.0`, matching the new liblcm project.
- It references only the `SIL.LCModel` package FW pins (which brings `SIL.LCModel.Core`, `.Utils`, `icu.net`, and `SIL.Core`), `SIL.ExCSS` 2.1.0 (0.N), and `DocumentFormat.OpenXml` 2.20.0. It never references FW projects.
- A matching scratch test project compiles `DictionaryConfigurationTestHelpers` (0.M) the same way.
- Both build in CI on every Phase 0 PR. In Phase 1 the scratch project becomes the template for `SIL.LCModel.DictionaryConfiguration.csproj`. Both are deleted in 2.A, in the same change that deletes the moved code.

The order of items inside the phase matters only where a dependency is noted.

### 0.A Remove static mutable state

| Static | Location | Action |
|---|---|---|
| `_configNodeToTypeMap` | `ConfiguredLcmGenerator.cs:1206` | Dead (never read or written). Delete. |
| `s_reportedNodes` | `ConfiguredLcmGenerator.cs:852` (`#if DEBUG`) | Delete, or make `ShowConfigDebugInfo` stateless. |
| `AssemblyFile` / `AssemblyMap` | `ConfiguredLcmGenerator.cs:67,72,96,1305,1452-1456` | Replace with a type resolver on `GeneratorSettings` (`Func<string, Type>` or a small interface). The production default resolves `"SIL.LCModel.DomainImpl." + name` from the LCM assembly. Tests pass a resolver for their fake model. This also removes the unlocked write to `AssemblyMap` from generation threads. About 40 test sites set `AssemblyFile = "xWorksTests"` today (`ConfiguredLcmGeneratorTests`, `CssGeneratorTests`, `UploadToWebonaryControllerTests`, plus the XHTML suite); change them all to the resolver. |
| `DictionaryConfigurationModel.NoteInParaStyles` | `DictionaryConfigurationModel.cs:104` | Make it `static readonly IReadOnlyCollection<string>`. |
| `lock (settings.PropertyTable)` | `ConfiguredLcmGenerator.cs:1134` | Replace with a lock object owned by the settings. (The code moves with the media resolver in 0.E.) |

### 0.B Decompose `PropertyTable` and `RecordClerk` out of the contract

**`GeneratorSettings`** (`ConfiguredLcmGenerator.cs:3547-3600`):
- Remove `ReadOnlyPropertyTable PropertyTable` and both constructors that take a `PropertyTable`.
- Add `LcmStyleSheet StyleSheet` (the class is already in liblcm, `DomainServices/LcmStyleSheet.cs`), the type resolver from 0.A, the media resolver (0.E), the scheduler (0.F), and the entry-source sort kind (0.C).
- Make `ContentGenerator` and `StylesGenerator` required constructor arguments. Remove the defaults `new LcmXhtmlGenerator()` / `new CssGenerator()` (`:3549-3550`).

**Interfaces:**
- `ILcmStylesGenerator.AddGlobalStyles(model, ReadOnlyPropertyTable)` and `Init(ReadOnlyPropertyTable)` (`:3610-3612`): drop the property table and read the stylesheet and cache from the settings.
- `ILcmContentGenerator.SetRunStyle(..., ReadOnlyPropertyTable, ...)` (`ILcmContentGenerator.cs:38`): drop the property table.
- `ILcmContentGenerator.StartEntry(..., RecordClerk clerk)` (`ILcmContentGenerator.cs:52`): drop the clerk. The only use is the JSON writer's letter head, which switches to `LetterHeadService` with the settings' sort kind.

**Callers:**
- `IsEntryStyleRtl(ReadOnlyPropertyTable, model)` (`:99-103`) becomes `IsEntryStyleRtl(LcmStyleSheet, model)`. `ExportStyleInfo` (FW `Common/Framework`) moves with the engine (0.L).
- The `settings.PropertyTable.GetValue<RecordClerk>("ActiveClerk")` in `GenerateContentForEntry` (`:424`) goes away; see 0.C.
- Every `GetValue<LcmCache>("cache")` in `CssGenerator`, `LcmXhtmlGenerator`, `LcmJsonGenerator`, and `LcmWordGenerator` becomes `settings.Cache`.
- `FontHeightAdjuster.StyleSheetFromPropertyTable` calls in the writers become `settings.StyleSheet`.

**FW-only context stays in FW code**, not in the settings. The XHTML preview's `clerk.CurrentObjectHvo` paging lookup (`LcmXhtmlGenerator.cs:465`) and `EntriesToAddCount` belong to the FW document drivers split off in 0.M and to `XhtmlDocView`.

**Add an FW factory** (for example `FwGeneratorSettings.Create(propertyTable, cache, contentGenerator, stylesGenerator, ...)`) that builds settings from the PropertyTable, so FW call sites change once.

### 0.C Entry source, sort kind, and decorator split (depends on 0.B)

- **Define `IDictionaryEntrySource`.** It returns a snapshot: ordered HVOs, the `DictionaryPublicationDecorator` that matches them, and a `LetterHeadSortKind` (Headword, LexemeForm, CitationForm, ReversalForm). The snapshot exists so callers can no longer pair a decorator with the wrong flid: FW decorates the RecordList's virtual list, while a clerk-free caller decorates `DomainDataByFlid` over the real `LexDb.Entries` flid.
- **Operations:** dictionary entries for a publication; reversal entries for an index and a publication; classified-dictionary domains for a publication (with a show-empties flag).
- **FW implementation, `ClerkDictionaryEntrySource`:** wraps what `DictionaryExportService.GetDictionaryFilteredAndSortedEntries`, `GetReversalFilteredAndSortedEntries`, and `GetClassifiedDictionaryFilteredAndSortedDomains` do today, including `FilterAndSortList` and the virtual flid. It maps `clerk.SortName` to the sort kind using the rules in `GetSortWordForLetterHead` (`ConfiguredLcmGenerator.cs:210-240`).
- **Decorator, `DictionaryPublicationDecorator`:**
  - Move `GetEntriesToPublish(PropertyTable, ...)` (`:411-450`) out into the FW entry source. It is the only place that touches `DictionaryConfigurationListener.GetDictionaryConfigurationBaseType`, `ReversalIndexEntryUi.GetObjectGuidIfValid` (the decorator's only FdoUi use), `"ShowFailingItems-lexiconClassifiedDictionary"`, and `ActiveClerk`.
  - Keep `GetSortedAndFilteredReversalEntries` and `FlattenDomains`. They are pure LCM.
  - Remove `using SIL.FieldWorks.FdoUi` and `using XCore`.
- **Move `ConfiguredLcmGenerator.CurrentDecorator` and `GetDecorator`** (`:3469-3495`), which take a PropertyTable and a clerk, to the FW side (`DictionaryExportService` or the entry source).
- **Replace `xWorksStrings.AllEntriesPublication`** (`:359`, `:3471`, `:3482`) with a constant. The resx value is the untranslated sentinel `$$all_entries$$`.
- **`GetSortWordForLetterHead(entry, RecordClerk)`** becomes `GetSortWordForLetterHead(entry, LetterHeadSortKind)`.

### 0.D Letter-head extraction

- **New class `LetterHeadService`** (FW-internal for now, in a file that will move). It holds:
  - `ConfiguredExport.GetLeadChar` (`Src/Common/Controls/XMLViews/ConfiguredExport.cs:648` onward), plus the `CollationLevel` enum, `GetLetterLengthAt`, and the digraph, character-equivalence, and ignorable-character map building it depends on. FW's XHTML paging code (`LcmXhtmlGenerator.cs:493`, `:496`) then calls `LetterHeadService.GetLetterLengthAt`.
  - `GetSortWordForLetterHead`
  - `ConfiguredLcmGenerator.GenerateLetterHeaderIfNeeded` (`:117-152`)
  - a collator factory equivalent to `FwUtils.GetCollatorForWs` (`Src/Common/FwUtils/FwUtils.cs:250`; it is ICU locale only)
- **`ConfiguredExport`** (the old XMLViews export) calls the new class instead of owning the logic.
- **Update callers:** `LcmJsonGenerator.StartEntry` and `GenerateLetterHeaders`, `LcmWordGenerator`, `LcmXhtmlGenerator`, and `ConfiguredLcmGenerator`.

### 0.E Media resolver

- **Define `IDictionaryMediaResolver`:** `(file path or ICmFile, MediaKind {Picture, Audio, Video}, owner) → src string`.
- **The engine keeps:** locating the file in LCM (`LinkedFilesRootDir`, `AbsoluteInternalPath`, `InternalPath`) and the `\` → `/` fix-up (`:657`), which uses `Platform.IsWindows` from `SIL.PlatformUtilities`. That is allowed.
- **The engine loses:**
  - `GenerateSrcAttributeFromFilePath` and `GenerateSrcAttributeForMediaFromFilePath` (`:1021-1058`) are reduced to locating the file plus one resolver call
  - `CopyFileSafely` (`:1122-1178`)
  - `MakeSafeFilePath` and `Common.FwUtils.Unicode.CheckForNonAsciiCharacters` (`:1180-1190`)
  - `WavConverter.WavToMp3`
  - the settings flags `UseRelativePaths`, `UseUri`, `CopyFiles`, and `ExportPath`
  - the media use of `IsWebExport` (the WAV-to-MP3 decision, `:1126`)
- **`IsWebExport` and `IsXhtmlExport` stay on `GeneratorSettings` unchanged.** `LcmXhtmlGenerator.WriteNodeId` (`:569`) uses them to leave out the preview-only `nodeId`, `sourceGuid`, and `sourceField` attributes.
- **FW implementation, `FwExportMediaResolver`:** carries today's relative-path, copy, dedupe, and MP3-conversion behavior, owns the lock from 0.A, and is configured with the flags the export code passes today.
- **Word export stops copying media (approved behavior change).** Today `SavePublishedDocx` sets `copyFiles = true` with the .docx folder as the export path (`LcmWordGenerator.cs:79-81`). So pictures, and audio converted to MP3, are copied next to the .docx even though pictures are embedded and Word ignores audio. FW's Word export uses a resolver that returns the original file's absolute path. FW's release notes say the loose media files no longer appear.
- **liblcm default (added in Phase 1):** returns the absolute path or a `file://` URI.
- **Callers:** the preview driver's direct use of `CopyFileSafely` for the current-entry marker image (`LcmXhtmlGenerator.cs:157`) moves to the FW resolver.

### 0.F Parallelism and STA

- **Problem:** `SpawnEntryGenerationThreadsAndWait` (`:158-200`) forces `SetApartmentState(ApartmentState.STA)` (`:192`). That throws `PlatformNotSupportedException` on .NET 8/Linux, and the STA requirement exists only because of FW COM.
- **Introduce `IGenerationScheduler`** (run a list of actions with progress and cancel). The default implementation uses plain worker threads or `Parallel.ForEach`. FW provides `FwStaGenerationScheduler` with today's STA code.
- **Pass the scheduler through `GeneratorSettings`.** The writers (`LcmXhtmlGenerator`, `LcmJsonGenerator`, `LcmWordGenerator`) call it instead of the static.

### 0.G Remove concrete-writer references from the engine

- **Class naming:** `CssGenerator.GetClassAttributeForConfig` / `GetClassAttributeForCollectionItem` are called by the engine (`:464-466`, `:2389`, `:2581-2583`). Move that naming policy into a `ConfigurationNodeNaming` helper in a file that will move. `CssGenerator` and the template generation in `UploadToWebonaryController.GenerateConfigurationTemplates` call the helper.
- **Word NFC special case:** `bldr is LcmWordGenerator.DocFragment wordFragment` → `NormalizeText` (`:435-439`). Replace it with a hook on `IFragment` (for example `NormalizeToNfc()`) or on `ILcmContentGenerator`, and move the NFC logic into each writer.
- **Generator defaults:** removing `LcmXhtmlGenerator`/`CssGenerator` as defaults is done in 0.B.
- **Table-cell alignment:** `HorizontalAlign` comes from `System.Web.UI.WebControls`, which exists only on .NET Framework. The engine uses it when it turns USFM table markup into table output (`:3341-3353`), and it is a parameter of `ILcmContentGenerator.AddTableCell` (`ILcmContentGenerator.cs:48`). Add a `TableCellAlignment { NotSet, Left, Center, Right }` enum in a moving file and use it in the engine, the interface, and the three writers (`LcmXhtmlGenerator.cs:851`, `LcmJsonGenerator.cs:242`, `LcmWordGenerator.cs:1528` and `:1626`). Then remove the `System.Web.UI.WebControls` using from all four files.
- **Dead usings:** delete `ExCSS`, `System.Configuration`, `System.Runtime.Remoting.Messaging` (.NET Framework only), `SIL.FieldWorks.Filters`, and `UnitType = ExCSS.UnitType` from `ConfiguredLcmGenerator.cs`. None of them is used.

### 0.H Move model sync and editing out of the dialog controller; apply option 3

**Sync functions.** `DictionaryConfigurationModel.Load` (`DictionaryConfigurationModel.cs:200-236`) calls public statics on `DictionaryConfigurationController`, and so do the migrators. Move these into the model types and make them **internal** unless FW UI code calls them directly; those become public methods on the model.

| Controller static | Called from moving code | Destination |
|---|---|---|
| `GetAllPublications`, `FilterInvalidPublicationsFromModel` | `DictionaryConfigurationModel.cs:206-208` | Model |
| `GetCurrentWritingSystems` | `DictionaryConfigurationModel.cs:213` | Model or `DictionaryNodeWritingSystemOptions` |
| `MergeCustomFieldsIntoDictionaryModel` | `DictionaryConfigurationModel.cs:228`, `FirstBetaMigrator.cs:97` | Model |
| `MergeTypesIntoDictionaryModel` | `DictionaryConfigurationModel.cs:230` | Model (uses the GUID constants from 0.I) |
| `EnsureValidStylesInModel`, `EnsureValidNumberingStylesInModel` | `DictionaryConfigurationModel.cs:232-234` | Model |
| `UpdateWritingSystemInModel` (+ `UpdateWsOptions`) | `DictionaryConfigurationModel.cs:236` | Model |
| `LinkReferencedNode` | `DictionaryConfigurationModel.cs:334` | Model (public method) |
| `ShareNodeAsReference` | `FirstAlphaMigrator.cs:123-132` | Model (public method) |
| `GetDictionaryConfigurationModels` | `DictionaryConfigurationModel.cs:361` | Model loader, taking `IDefaultConfigurationSource` + project dir |
| `GetLookupClassForCustomFieldParent`, `BuildCustomFieldMap`, `GetCustomFieldsForType` | used by the custom-field merge | Same place as the merge |

- **`ShareNodeAsReference` UI extraction:** today it calls `MessageBoxUtils.Show(xWorksStrings.InadvisableToShare, ...)`. Change it to return (or report through an optional callback) an "inadvisable share" warning. The controller shows the message box; the migrators log the warning.
- **Stays in the controller (UI only):** `CanReorder`, `EnableNodeAndDescendants`/`DisableNodeAndDescendants`, `LoadAvailableWsList` (returns `ListViewItem`), `GetWsDefaultName`, `SetConfigureHomographParameters` (PropertyTable), `GetDictionaryConfigurationLabels`. Check each of them against the exit-criterion compile.
- **`DictionaryConfigurationModel.GetReversalConfigurationModel(ws, cache, PropertyTable)`** (`:355`): split it into a liblcm overload that takes directories (or the default source plus the project dir) and an FW wrapper that resolves the directories from the PropertyTable.
- **Directory and type constants:** move `DictConfigDirName`, `RevIndexConfigDirName`, `ClassifiedDictConfigDirName`, and `DictionaryType`/`ReversalType`/`ClassifiedType` from `DictionaryConfigurationListener` to a constants class in a moving file. The listener keeps aliases or references the new location. Replace `FwDirectoryFinder.DefaultConfigurations` in `DictionaryConfigurationModel.cs:177`, `DictionaryConfigurationUtils.cs:30`, and `FirstBetaMigrator.cs:69-70` with `IDefaultConfigurationSource`. In Phase 0 this is a directory-backed implementation over `FwDirectoryFinder.DefaultConfigurations`.

**Option 3 visibility (applied now, so Phase 1 is a pure move):**
- **Public read-only:** the `ReferencedNode` getter, `IsMasterParent`, `IsSubordinateParent`, `TryGetMasterParent`. The `ReferencedNode` setter and `Parent` wiring stay internal.
- **Public mutation methods on the model and node:** `LinkReferencedNode`, `ShareNodeAsReference`, `DeepCloneUnderParent`, `DeepCloneUnderSameParent`, `SpecifyParentsAndReferences`, `PublicationsInXml`. Plus any merge or sync method the controller calls directly (`MergeCustomFieldsIntoDictionaryModel` is called by the controller at `:1366`).
- **Controller writes to `ReferencedNode`** (`DictionaryConfigurationController.cs:865`, `:921`) disappear because those methods move to the model.

### 0.I Small invasions

- **`XmlViewsUtils.GetGuidForUnspecified{Variant,ComplexForm,ExtendedNote}Type`** (`Src/Common/Controls/XMLViews/XmlViewsUtils.cs:1262` onward): these "fake" GUIDs are persisted in config files and used by the engine (`:2691-2707`) and by `MergeTypesIntoDictionaryModel`. Move the constants into a moving file; `XmlViewsUtils` references them.
- **Strings:** `xWorksStrings.InvalidRootConfigurationNode` (`:1314`, exception text) and `InvalidUSFM_TextAfterTR` (`:3332`, written into the dictionary output by `GenerateError`) move into a new `DictionaryConfigurationStrings.resx` that moves with the code. FW's existing liblcm localization (`Build/Localize.targets:94-102`, `crowdin.json` `Localizations/LCM/**`) picks up any `src/**/*.resx` in liblcm, so the strings stay localized for FW. Lexbox gets English. See 2.D for the checks.
- **`SIL.Reporting` and `SIL.Code` usings** in the engine come from `SIL.Core`, which liblcm already references. Confirm during the exit-criterion compile.
- **`Common.FwUtils.Unicode.CheckForNonAsciiCharacters`** and `FileUtils.IsFilePathValid` (`:1182-1187`) leave with `CopyFileSafely` (0.E).
- **`IThreadedProgress`:** confirm that the engine's use resolves to the liblcm type, not an FW one.

### 0.J Migrator refactor

- **`IDictionaryConfigurationMigrator.MigrateIfNeeded(SimpleLogger, PropertyTable, appVersion)`** becomes `MigrateIfNeeded(LcmCache, ISimpleLogger, IDefaultConfigurationSource, projectConfigDir, appVersion)`. Move `ISimpleLogger` with it if it is FW-owned today; otherwise use liblcm's logging abstraction.
- **`FirstAlphaMigrator` / `FirstBetaMigrator`:** drop `PropertyTable`, `FwDirectoryFinder`, and the listener constants (0.H). Keep their model transforms unchanged.
- **The `DictionaryConfigurationMigrator` orchestration** (`DictionaryConfigurationMigrator.cs:36-90`: Mediator, PropertyTable, custom-CSS creation, `PreHistoricMigrator`) stays in FW. It calls the refactored migrators. Split its pure members (listed in Scope) into a file that will move.
- **Pre-8.3 configs:** the liblcm loader must report "this project's configuration predates 8.3 and must be opened in FieldWorks once" when no `.fwdictconfig` exists but old `.fwlayout` files do. It must not try a PreHistoric migration.
- **New in-memory path:** loading a config below `VersionCurrent` (26) through the liblcm loader runs the Alpha/Beta steps in memory **without writing the file**. FW's orchestration keeps its write-back behavior.

### 0.K `ReversalIndexServices` and `DictionaryConfigurationUtils`

- **`ReversalIndexServices.CreateOrRemoveReversalIndexConfigurationFiles`** is pure LCM plus file I/O. Switch its `defaultConfigDir` parameter to `IDefaultConfigurationSource`.
- **`DictionaryConfigurationUtils`:** `GatherBuiltInAndUserConfigurations(cache, configObjectName)` moves (after the 0.H directory fix). `SetReversalIndexGuidBasedOnReversalIndexConfiguration(PropertyTable, ...)` and `RemoveAllReversalChoiceFromList(UIListDisplayProperties)` stay in FW. Split the file.

### 0.L Resolved style model (depends on 0.B, 0.G)

**Problem:** two writers each turn a project style into output with the same steps, and the copies have drifted. Lexbox would need a third copy.
- `CssGenerator.GenerateCssStyleFromLcmStyleSheet` + `AddFontInfoCss` (`CssGenerator.cs:1236-1815`)
- `WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet` + `AddFontInfoWordStyles` (`WordStylesGenerator.cs:164-665`)

Known drift: Word disables borders and falls back to the default analysis writing system's font for Normal; CSS emits `flex-line-height` and `page-break-*`.

**Steps:**
1. **Move `ExportStyleInfo`** (`Src/Common/Framework/ExportStyleInfo.cs`, 136 lines, depends only on SIL.LCModel) into a moving file. Its other FW users (`XhtmlHelper`, `FlexStylesXmlAccessor`, `XhtmlDocView`) keep using it from there.
2. **Add `DictionaryStyleResolver.Resolve(styleSheet, styleName, wsId, nodeList)` returning `ResolvedStyle`,** with values in points:
   - **Paragraph:** paragraph or character style, alignment, borders (widths and color), first-line or hanging indent, leading indent with ancestor paragraph indents applied (today's `CalculateParagraphIndentsFromAncestors` / `CalculateMarginLeft`), trailing indent, space before and after, line spacing (relative, at-least, or exact), keep together, keep with next, direction.
   - **Character:** font family after the writing-system override and the `<default font>` / Normal fallback, size, bold, italic, foreground and background color, superscript/subscript, underline type and color, font features.
   - **Bullets and numbering as data:** custom bullet text, bullet symbol, or numbering scheme. `BulletSymbolsCollection` and `NumberingStylesCollection` become neutral tables; CSS escapes and counter names stay in `CssGenerator`.
   - **First-sense variant:** the alternate margin used when the parent sense node displays each sense in a paragraph (today's `calculateFirstSenseStyle`).
3. **Rewrite `CssGenerator` and `WordStylesGenerator`** to translate `ResolvedStyle`. Format-specific choices stay in the writers: Word's disabled borders, CSS `flex-line-height`, `page-break-*`, and CSS counters. Decide per difference whether the Normal-style font fallback belongs in the resolver (both writers) or stays Word-only, using output diffs.
4. **Remove the model mutation** at `CssGenerator.cs:1393`, where style generation sets `node.CSSClassNameOverride` for numbered lists. That property is persisted configuration and is written from worker threads. `CssGenerator` computes the counter name through `ConfigurationNodeNaming` without writing to the node.

**Done when:** `CssGeneratorTests` (131 tests) and `LcmWordGeneratorTests` (33 tests) pass, new resolver unit tests cover each property group, and CSS and Word output for a sample project is unchanged apart from any reconciliation agreed in step 3.

### 0.M Prepare the XHTML and CSS writers to move (depends on 0.B–0.E, 0.G, 0.L)

**Split `LcmXhtmlGenerator.cs` (1,259 lines).**
- **Moves:** the `ILcmContentGenerator` implementation (`:542-1155`) and `GenerateXHTMLTemplatesForConfigurationModel` (`:1156-1259`).
- **New moving class `XhtmlDocumentWriter`:** `GenerateOpeningHtml`, `CreateLinkElement`, `GenerateWritingSystemsMetadata`, `GenerateClosingHtml`, and the letter-heading markup from `GenerateLetterHeaderIfNeeded` (`:169-196`, switched to `LetterHeadService` and the sort kind from 0.C). It takes the stylesheet hrefs from the caller instead of probing the file system for the custom CSS.
- **Stays in FW, in a new class (for example `FwXhtmlDocuments`):** `SavePreviewHtmlWithStyles`, `SavePublishedHtmlWithStyles`, `GenerateEntryHtmlWithStyles`, `GenerateNextFewEntries`, `GenerateAdjustedPageButtons` and the other page-button and page-range code (`:319-540`), `GetPreferredPreviewPath`, `IsExport`, `CurrentEntryMarker`, and `EntriesPerPage`. These hold the clerk (`RecordClerk.IsClerkSortingByHeadword`, `CurrentObjectHvo`), `FwDirectoryFinder.FlexFolder`, `xWorksStrings` progress messages, `DictionaryConfigurationListener.GetProjectConfigurationDirectory`, and `XhtmlDocView` paths. They call the moving writer and `XhtmlDocumentWriter`.
- **Re-point callers:** `XhtmlDocView` (`:410`, `:443`, `:881`, `:1708`, `:1715`), `XhtmlRecordDocView` (`:194`), `DictionaryExportService` (`:154`), and `DictionaryConfigurationController` (`:273`) call the FW class.

**Split `CssGenerator.cs` (2,006 lines).**
- **Moves:** the `ILcmStylesGenerator` implementation, rule generation, `GenerateCssFromConfiguration`, `GenerateLetterHeaderCss`, `CopyCustomCssAndGetPath` / `CopyCustomCssToTempFolder` (pure after the 0.H directory-constant move; they use liblcm's `LcmFileHelper`), and `CssExtensions`.
- **Stays in FW:** `GenerateCssForPageButtons` (`:1829`) and `GenerateCssForSelectedEntry` (`:1873`), used only by the preview.
- **Move `SpecialCharacterHandling`** (`DictionaryDetailsView/SpecialCharacterHandling.cs`) into a moving file. The dialog keeps using it from there.
- **Remove the `FwCoreDlgControls` using.** No type from it is used (`FontInfo.kDefaultFontSize` already resolves to liblcm).

**Keep ExCSS out of the public API.**
- `LcmJsonGenerator.cs:195` calls `CssGenerator.GenerateCssStyleFromLcmStyleSheet`, which returns an ExCSS `StyleDeclaration`. Replace that with a public method that returns the declaration as a CSS string (built from `ResolvedStyle`, 0.L).
- `CssGenerator.GetNodePath` (used by the Word writer at `LcmWordGenerator.cs:1006` and six other sites) joins `ConfigurationNodeNaming` (0.G), so the Word writer no longer references the CSS writer.
- `GetClassAttributeForConfig` callers (`DictionaryConfigurationController.cs:901`, `DictionaryConfigurationDlg.cs:236`, `UploadToWebonaryController.cs:591`, `WordStyleCollection.cs:282`, `:658`, `LcmWordGenerator.cs:2031`) call `ConfigurationNodeNaming`.

**Writer state.**
- `CssGenerator` keeps all state per instance (`_styleDictionary`, `_uniqueNodeNames`, `_styleSheet`). Confirm that every member reached from `AddStyles` is safe under parallel entry generation, and lock `_uniqueNodeNames` the same way as `_styleDictionary` if it is not.
- The lazily filled static `BulletSymbolsCollection` and `NumberingStylesCollection` (`:47-48`) are replaced by the neutral tables from 0.L.

**Tests.**
- **Setup:** `CssGeneratorTests` and the `ConfiguredXHTMLGenerator*Tests` set up `MockFwXApp` / `MockFwXWindow` to get a stylesheet into a PropertyTable. Change their setup to build `GeneratorSettings` with an `LcmStyleSheet` directly (0.B), so they can move in Phase 1 without FW test infrastructure.
- **Test data paths:** replace `FwDirectoryFinder.SourceDirectory + "xWorks/xWorksTests/TestData/"` and `FwDirectoryFinder.CodeDirectory` with paths relative to the test assembly.
- **Line endings:** the expectations hard-code `\r\n` (`ConfiguredXHTMLGeneratorTests` 141 places, `CssGeneratorTests` 78, `…ReversalTests` 25, `ConfiguredLcmGeneratorTests` 6), while the writers use `Environment.NewLine`. Normalize line endings in the assertions, or build expectations with `Environment.NewLine`, so the tests pass on Linux. Do not change the writers.
- **Shared helpers:** eight FW fixtures that stay call internal helpers in fixtures that move. Examples: `CssGeneratorTests.PopulateFieldsForTesting` (about 68 calls) and `ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry`, `GetWsOptionsForLanguages`, `CreateInterestingConfigurationModel`, `AddSenseToEntry`, `SetPublishAsMinorEntry`, `CreateVariantForm`, and `GetFullyEnabledListOptions`. The callers are `LcmJsonGeneratorTests`, `DictionaryConfigurationControllerTests`, `PreHistoricMigratorTests`, `DictionaryExportServiceTests`, `DictionaryDetailsControllerTests`, `DictionaryConfigurationImportControllerTests`, `UploadToWebonaryControllerTests`, and `ConfiguredLcmUsfmGeneratorTests`. Move these helpers into a public static class `DictionaryConfigurationTestHelpers` with no FW dependencies, compiled by the scratch test project.
- **Driver tests:** split out the tests that cover the FW drivers (paging, preview, current-entry CSS) into their own fixture.

### 0.N Retarget `SIL.ExCSS` (no dependencies; start early)

- **Problem:** `SIL.ExCSS` 2.0.2, the version FW pins (`Directory.Packages.props:116`), ships only `net40`, `net45`, and `net451` assets. liblcm's `netstandard2.0` target cannot reference it, and `net8.0` would load it only through the .NET Framework compatibility fallback.
- **Where 2.0.2 came from:** the fork `github.com/jasonleenaylor/ExCSS` (the package's `projectUrl`; code last changed in 2014, `2a0270f`) plus open PR #1, "Create SIL.ExCSS nuget package" (Eberhard Beilharz, 2020-07-31, from `ermshiperete/ExCSS`). The PR changes only packaging: the nuspec id, URLs, and pack command. It was never merged, so the recipe for the published package is not in the repo. On nuget.org, `SIL.ExCSS` is owned by `sil-lsdev`. There is no `sillsdev/ExCSS` repo yet.
- **Steps:**
  1. Merge PR #1 into the fork.
  2. Transfer the repo to sillsdev (GitHub redirects the old URL).
  3. Convert `ExCSS.csproj` to an SDK project targeting `netstandard2.0;net462`, keeping `ExCSS.snk` signing. Drop `net40`/`net45`/`net451`, which nothing uses. Replace the nuspec and `nuget.exe` packing with `dotnet pack` and `SIL.ExCSS` metadata in the csproj.
  4. Add a GitHub Actions workflow that builds, packs, and pushes to nuget.org with the org secret `SILLSDEV_PUBLISH_NUGET_ORG`, the same way libpalaso (`build.yml`) and chorus (`ci+cd.yml`) do.
  5. Compare the new `net462` DLL with the 2.0.2 DLL (ILSpy diff), then publish 2.1.0.
  6. Bump FW's pin. The other FW users (`xWorks`, `ITextDll`) take the same version.
- **Done when:** FW builds against 2.1.0, and CSS output for a sample project is byte-identical.
- **Rejected alternatives:** moving to upstream ExCSS 4 or writing our own CSS builder. Both mean rewriting every rule-building call in `CssGenerator`.

### 0.O Prepare the Word writer to move

Split into three items. 0.O-1 and 0.O-2 have no dependencies and hold the riskiest work, so start them first. Phase 0 still has one exit check.

#### 0.O-1 Image sizing without WPF (no dependencies; start early)

- **Problem:** `CreateImage` (`LcmWordGenerator.cs:2170`) loads each picture into a WPF `BitmapImage` (`System.Windows.Media.Imaging`, from `PresentationCore`/`WindowsBase`) only to read pixel size and DPI. WPF does not exist on `netstandard2.0`, `net8.0`, or Linux.
- **Fix:** replace it with a small header reader that returns pixel size and DPI, defaulting to 96 DPI when the file has none. Avoid a new image library dependency.
- **Formats:** the formats Windows' built-in image codecs read today: PNG, JPEG, GIF, BMP, TIFF, and ICO.
- **Errors:** if the reader cannot read a file, throw, as `BitmapImage` does today (the export fails).
- **TODO comment:** keep `ImagePartType.Jpeg` for every picture. Today `AddImage` calls `AddImagePartToPackage(srcAttribute)` (`:1808`) without a type, so the default (`:2156`) applies. Add a `// TODO` at that call saying that the reader already knows the real format, and that passing it would correct the content type for non-JPEG pictures (see Follow-ups).
- **Done when:** for a set of test images in each format, the reader returns the same size and DPI as `BitmapImage`, compared side by side while WPF is still available. Word output is unchanged.

#### 0.O-2 Word export state per instance (no dependencies; start early)

- `LcmWordGenerator` keeps per-export state in statics: `s_styleCollection` (`:43`), `IsBidi` (`:50`), `MasterFragment` (`:51`), and the locks `_collectionLock` / `_masterFragmentLock` (`:44-45`). Two Word exports in one process share them.
- Move all of it onto the `LcmWordGenerator` instance (one per export).
- `WordStyleCollection.cs:238` reads `LcmWordGenerator.IsWritingSystemRightToLeft`; keep that a pure static helper.
- **Done when:** `LcmWordGeneratorTests` (33 tests) pass and Word output is unchanged.

#### 0.O-3 Remove FW types from `SavePublishedDocx` (depends on 0.B, 0.C, 0.D, 0.E, 0.F, 0.L, 0.O-2)

`SavePublishedDocx` (`:64-336`) keeps its shape and moves with the writer. Only the FW types change:

| Today | After |
|---|---|
| `RecordClerk clerk` (letter-heading sort word only) | `LetterHeadSortKind sortKind` (0.C) |
| `PropertyTable propertyTable` (cache, stylesheet, settings) | `LcmCache cache`, `LcmStyleSheet styleSheet`, `IDictionaryMediaResolver mediaResolver` (0.E), `IGenerationScheduler scheduler` (0.F) |
| `progress.Message = xWorksStrings.…` (`:111`, `:116`, `:245`) | optional `Action<DocxExportStage> onStage`, with stages `GeneratingEntries`, `ArrangingEntries`, `GeneratingStyles` |
| `FwUtils.GetCollatorForWs` (`:118`) | the 0.D collator factory |
| `FontHeightAdjuster.StyleSheetFromPropertyTable` (`:120`) | `styleSheet` |
| `ConfiguredLcmGenerator.SpawnEntryGenerationThreadsAndWait` (`:112`) | `scheduler` |

- **Unchanged:** `entryHvos`, `publicationDecorator`, `configuration`, `filePath`, `progress` (`IThreadedProgress`, for per-entry progress), and the internal `MemoryStream`.
- **FW caller:** `DictionaryExportService.cs:112`, `:124`, `:142` map the clerk to a sort kind, pass FW's Word media resolver (0.E) and `FwStaGenerationScheduler`, and map `onStage` to the three `xWorksStrings` messages.

#### Smaller items

- `WordStylesGenerator` and `WordStyleCollection` take `ReadOnlyPropertyTable` (`WordStylesGenerator.cs:137`, `:165`, `:180`, `:201`, `:987`; `WordStyleCollection.cs:17`, `:36`); switch to `settings.StyleSheet` and `settings.Cache` (0.B).
- `WordStylesGenerator.cs:3` `using ExCSS` is unused; remove it.
- The engine's `LcmWordGenerator.DocFragment` NFC special case (0.G) still becomes a hook, even though both now live in liblcm: the engine should not know about writer types.
- `LcmWordGeneratorTests` set up `MockFwXApp` / `MockFwXWindow` (`:76-78`) and point `Cache.ProjectId.Path` at FW's `TestData` (`:155`). Change them as in 0.M: build `GeneratorSettings` directly, use test-relative paths, and normalize line endings (37 hard-coded `\r\n`).

### 0.P Phase 0 exit check

1. **Scratch compile:** the scratch project and scratch test project (see the start of Phase 0) build for `netstandard2.0`, `net462`, and `net8.0`. They include every moving file: `ILcmContentGenerator.cs`, the new interfaces and services, the moving parts of the XHTML, CSS, and Word writers, `DictionaryConfigurationStrings.resx`, and `DictionaryConfigurationTestHelpers`. Zero FW references.
2. **Tests:** the full xWorks and XMLViews test suites pass unchanged in behavior. Expected edits are signature churn, the type-resolver swap, test-relative paths, and line-ending normalization.
3. **Manual smoke tests:** dictionary preview, XHTML export, Word export (expect no loose media files next to the .docx, per 0.E), Webonary upload (JSON), the configuration dialog (share, unshare, clone, custom fields), the import and manager dialogs, and opening a project that has v<26 configs. The same list integration-tests intermediate Phase 0 builds.

---

## Phase 1: Move into liblcm

**Target:** liblcm `master`, released as a beta package.

### 1.A Code

- **Create the project** `src/SIL.LCModel.DictionaryConfiguration/SIL.LCModel.DictionaryConfiguration.csproj` in `LCM.sln`, starting from the scratch project:
  - targets `netstandard2.0;net462;net8.0`
  - references the `SIL.LCModel` project, `SIL.ExCSS` 2.1.0, and `DocumentFormat.OpenXml` 2.20.0 (which brings `System.IO.Packaging` on `netstandard2.0`)
  - sets `<Nullable>disable</Nullable>`, matching how the code compiles in FW today
  - `InternalsVisibleTo("SIL.LCModel.DictionaryConfiguration.Tests")`
- **Copy the files cleaned in Phase 0** into the project, under `DictionaryConfiguration/`, `Generation/`, `Generation/Xhtml/`, and `Generation/Word/`. Change namespaces to `SIL.LCModel.DictionaryConfiguration[.Generation[.Xhtml|.Word]]`.
- **Copy, don't `git mv`.** The repos have no shared history. Each moved file's header comment records its FW origin path and commit (`412ee26b8` or the Phase 0 tip).
- **Package downgrades:** liblcm treats `NU1605` as an error (`Directory.Build.props:19`). The scratch compile will have shown any downgrade first; if one appears, pin the version in the new project.
- **The default type resolver** resolves `"SIL.LCModel.DomainImpl." + name` from `typeof(CmObject).Assembly` (`SIL.LCModel`).
- **Visibility:** apply the API decisions. Public: group A, option 3 read-only queries, and the explicit mutation methods. Internal: everything else. The test-only internals (`AreThereEnabledSubsensesWithNumberingStyle`, `GenerateContentForFieldByReflection`, `IsCollectionType`, `ShouldThisSenseBeNumbered`, `LookupComplexEntryType`) stay internal and are reached through the `InternalsVisibleTo` above.
- **Group A members** become public:
  - engine and model: `GenerateContentForEntry`, `BuildNodeList`, `GetWsForEntryType`, `IsEntryStyleRtl`, `GetPropertyTypeForConfigurationNode` + `PropertyType`, `GetTypeForConfigurationNode`, `IsBlockProperty`, `IsCollectionNode`, `IsFactoredReference`, `GetCustomFieldFlid`, `GetClassNameForCustomFieldParent`, node `IsHeadWord`/`IsMainEntry`, model `IsReversal`/`IsClassified`/`IsHybrid`, `NoteInParaStyles`, `LoudSpeaker`/`MovieCamera`, `IsListItemSelectedForExport` (used by `DictionaryExportService.IsGenerated`)
  - services: `ConfigurationNodeNaming`, `LetterHeadService` (including `GetLetterLengthAt`), `DictionaryStyleResolver` + `ResolvedStyle`, `ExportStyleInfo`, `TableCellAlignment`
  - `Xhtml` namespace: `LcmXhtmlGenerator`, `CssGenerator` (interface members, `GetStylesString`, the CSS-string style method from 0.M, `GenerateLetterHeaderCss`, `CopyCustomCssAndGetPath`), `XhtmlDocumentWriter`, `GenerateXHTMLTemplatesForConfigurationModel`, and `SpecialCharacterHandling`
  - `Word` namespace: `LcmWordGenerator` (including `SavePublishedDocx`) and `DocxExportStage`
- **Phase 0 FW-only items must not be present:** `CopyFileSafely`, `SpawnEntryGenerationThreadsAndWait` (STA), `EntriesToAddCount`, the FW document drivers and preview CSS from 0.M, and the `xWorksStrings` progress messages from 0.O-3.

### 1.B New liblcm-only pieces

- **`DefaultDictionaryEntrySource`:**
  - dictionary entries: all `ILexEntry` passing the publication decorator over `cache.DomainDataByFlid`, ordered by headword (or lexeme or citation form) with the headword writing system's collation
  - reversal entries: ordered by reversal form
  - classified dictionary: through `FlattenDomains`
  - returns the same snapshot type as the FW adapter
- **Default `IDictionaryMediaResolver`:** absolute path or `file://` URI.
- **Default `IGenerationScheduler`:** no STA.
- **`EmbeddedDefaultConfigurationSource`:** reads the `.fwdictconfig` resources by group (Dictionary, ReversalIndex, Classified Dictionary).
- **`DirectoryDefaultConfigurationSource`:** for callers with the files on disk (FW at runtime, 2.B; tests).

### 1.C Resources

- **Configs:** add `DefaultConfigurations/**` (all `.fwdictconfig` files plus `Readme.md` as documentation, not a resource) and `DictionaryConfiguration.xsd` under `src/SIL.LCModel.DictionaryConfiguration/Resources/` as `EmbeddedResource` with stable logical names.
- **Strings:** `DictionaryConfigurationStrings.resx` (0.I) moves into the project. FW's localization build collects it with the other liblcm resx files (`Build/Localize.targets:97`).

### 1.D Tests (`tests/SIL.LCModel.DictionaryConfiguration.Tests/`)

- **New test project** in `LCM.sln`, targeting `net462;net8.0` like `SIL.LCModel.Tests`, and **published as a package** the same way `SIL.LCModel.Tests` is. FW's remaining tests reference it (2.C). It contains the public `DictionaryConfigurationTestHelpers` class from 0.M.
- **Move with their subjects:** `ConfiguredLcmGeneratorTests`, `CssGeneratorTests` (minus preview CSS), `LcmWordGeneratorTests`, `ConfigurableDictionaryNodeTests`, `DictionaryConfigurationModelTests`, `DictionaryNodeOptionsTests`, `DictionaryPublicationDecoratorTests`, `FirstAlphaMigratorTests`, `FirstBetaMigratorTests`, `DictConfigModelExtTests`, `ReversalIndexServicesTests`, and the moving parts of `DictionaryConfigurationMigratorTests` and `DictionaryConfigurationUtilsTests`.
- **Test fixtures:** the tests use the real `LcmXhtmlGenerator`, `CssGenerator`, and `LcmWordGenerator`, so no stand-in writers are needed. Bring:
  - the fake `DomainImpl` model classes that xWorksTests currently provides through `AssemblyFile = "xWorksTests"`, now supplied through the type resolver
  - the helpers from `ConfiguredXHTMLGeneratorTestHelpers.cs` that the ported tests need
  - the test data from `Src/xWorks/xWorksTests/TestData/`: `AudioFiles/abu2.wav`, `ImageFiles/test_auth_copy_license.jpg`, `ConfigurationSettings/Dictionary/ProjectDictionaryOverrides.css`, and `ConfigurationSettings/ReversalIndex/ProjectReversalOverrides.css`, copied to the output and found through the test-relative paths from 0.M
- **Move the XHTML suites** (`ConfiguredXHTMLGeneratorTests.cs` 527 KB, `…ReversalTests.cs` 41 KB, `…ClassifiedTests.cs` 26 KB) almost whole, including XHTML markup, letter-head markup, and media markup produced through the default media resolver.
  - **Stay in FW:** paging and preview (the driver fixture split out in 0.M), current-entry and page-button CSS, and media tests that assert on FW's copy, relative-path, or MP3 behavior (`FwExportMediaResolver`).
  - Track the split as a checklist by test name.
- **Linux:** liblcm CI's existing `windows-latest` / `ubuntu-22.04` matrix (`.github/workflows/ci-cd.yml:24`) runs the whole moved suite on Linux. That proves the removal of STA, `System.Web`, and WPF, and the line-ending fixes from 0.M and 0.O.
- **New liblcm tests:**
  - `DefaultDictionaryEntrySource` ordering, including homographs and ignorable characters
  - `LetterHeadService` against the cases from the existing letter-head tests
  - `DictionaryStyleResolver`: move the resolver tests added in 0.L
  - `EmbeddedDefaultConfigurationSource` (all default files load, deserialize, are at `VersionCurrent`, and validate against the XSD)
  - in-memory migration without write-back
  - pre-8.3 detection
  - `XhtmlDocumentWriter`: a full document for a small project, with and without letter headings
  - `SavePublishedDocx`: a full .docx for a small project; open it with OpenXml validation (`OpenXmlValidator`)
  - the image size reader from 0.O-1, using test images in each supported format
- **Sort parity:** add a test, or a documented manual check, that compares FW's clerk ordering with `DefaultDictionaryEntrySource` on a sample project. It cannot run in liblcm CI because it needs FW, so put it in FW in Phase 2 (2.D).

### 1.E Package and release

- **Packages:** `SIL.LCModel.DictionaryConfiguration` and `SIL.LCModel.DictionaryConfiguration.Tests` are versioned and published with the other liblcm packages (Decision 14). Confirm liblcm's pack step includes both new projects.
- **Release notes:** a CHANGELOG entry listing the new packages, namespaces, every public type and member they add, and their `SIL.ExCSS` and `DocumentFormat.OpenXml` dependencies.
- **Mark the API as new and still evolving** in the changelog and XML docs. Lexbox's needs may still reshape `IDictionaryEntrySource` and the media resolver.
- **Open GitHub issues on `sillsdev/liblcm`** for each item in Follow-ups.
- **Publish a beta.** FW Phase 2 develops against it locally through `LocalDevPackages` until it is merged.

---

## Phase 2: FieldWorks switches over

**Target:** FieldWorks `main`, as one switch-over change, after the liblcm beta passes FW's tests through `LocalDevPackages`. Nothing goes to `release/9.3.x`.

### 2.A Consume the package

- **Version bump:** raise `SilLcmVersion` to the Phase 1 release. Add `PackageVersion` entries for `SIL.LCModel.DictionaryConfiguration` and `SIL.LCModel.DictionaryConfiguration.Tests` (same version) to `Directory.Packages.props`, and reference them from xWorks and xWorksTests (and any other project the build shows needs them).
- **Delete moved code:** remove every file that moved, and the moved parts of the split files (`DictionaryConfigurationMigrator`, `DictionaryConfigurationUtils`, `ConfiguredExport`, `XmlViewsUtils` constants, `FwUtils.GetCollatorForWs` if nothing else uses it). `LcmXhtmlGenerator.cs`, `CssGenerator.cs`, `SpecialCharacterHandling.cs`, `LcmWordGenerator.cs`, `WordStylesGenerator.cs`, and `WordStyleCollection.cs` leave FW; the FW drivers and preview CSS from 0.M remain. Drop xWorks' direct `DocumentFormat.OpenXml` and `SIL.ExCSS` references if nothing else in xWorks uses them (`ITextDll` keeps `SIL.ExCSS`).
- **Delete the scratch project and scratch test project** and their CI job in the same change. Their job ends when FW consumes the package.
- **Namespaces:** add `using SIL.LCModel.DictionaryConfiguration[.Generation[.Xhtml|.Word]]` throughout xWorks, xWorksTests, XMLViews, `Src/LexText/Lexicon` (`ReversalListener`, `ReversalEntryBulkEdit`), and any other consumers (find them by building).
- **Implementations:** `ClerkDictionaryEntrySource`, `FwExportMediaResolver`, `FwStaGenerationScheduler`, and the JSON writer now implement the liblcm interfaces. Preview, export, Word export, and Webonary use the liblcm writers.

### 2.B Default configurations

- **Remove** `DistFiles/Language Explorer/DefaultConfigurations/**` and `DistFiles/Language Explorer/Configuration/DictionaryConfiguration.xsd` from source control.
- **Add a build step** that extracts them from the `SIL.LCModel.DictionaryConfiguration` embedded resources into `DistFiles/...` before installer harvesting, so the WiX 3 PatchableInstaller flow and FW's own code still find the files on disk.
- **Gitignore** the generated paths.
- **FW's runtime source is `DirectoryDefaultConfigurationSource` over `FwDirectoryFinder.DefaultConfigurations`.** This keeps today's behavior: an edit to an installed default file still takes effect. In a normal install the files on disk are the extracted copies of the embedded resources.

### 2.C Visibility fallout

- **Fix FW code that used internals not made public.** The group-A and option 3 lists should cover all production uses found on `412ee26b8`. Any remaining break is a missed item: decide case by case whether to make it public or move it to FW.
- **FW tests that stay** switch from `CssGeneratorTests.PopulateFieldsForTesting` and `ConfiguredXHTMLGeneratorTests.*` to `DictionaryConfigurationTestHelpers` from the test package. Other uses of now-internal members in the fixtures that stay need review one by one: the XHTML driver and preview tests split out in 0.M, `XhtmlDocViewTests`, `LcmJsonGeneratorTests`, `ConfiguredLcmUsfmGeneratorTests`, `DictionaryConfigurationControllerTests`, `DictionaryConfigurationImportControllerTests`, `DictionaryDetailsControllerTests`, `UploadToWebonaryControllerTests`, `DictionaryExportServiceTests`, `PreHistoricMigratorTests`, and `DictionaryConfigurationMigratorTests`. `InternalsVisibleTo` to FW assemblies is not allowed.

### 2.D Tests and checks remaining in FW

- **Tests that stay:** JSON, the XHTML drivers, preview and paging, the Word export caller in `DictionaryExportService`, FW media-resolver behavior, plus controller, dialog, export-service, and Webonary tests. Remove the parts that were ported.
- **Add the sort-parity test** (FW clerk order vs. `DefaultDictionaryEntrySource`). Known differences are documented, not failures.
- **Localization of the moved strings** (0.I):
  1. `LocalizeFieldWorks` builds `SIL.LCModel.DictionaryConfiguration.resources.dll` for each locale, not only the existing liblcm satellites.
  2. The WiX 3 harvesting includes that satellite in each locale folder.
  3. On Crowdin, the two strings appear under the new `Localizations/LCM/**` path. Pre-translate them from translation memory so existing translations of `InvalidUSFM_TextAfterTR` and `InvalidRootConfigurationNode` carry over.
- **Full regression smoke:** repeat the 0.P smoke list against the package build.
- **Release note:** FW's Word export no longer copies media files next to the .docx (0.E).

### 2.E Cleanup

- **Update** `Src/xWorks` docs and AGENTS.md / developer notes that point to the moved files.
- **Delete this plan** (`Docs/dictionary-configuration-to-liblcm-plan.md`) as the last change of the project.
- **Document the fix workflow** for the moved code (Decision 14):
  - fix in liblcm and test in FW through `LocalDevPackages`
  - ship in the next liblcm beta, then bump FW's `SilLcmVersion`
  - for a hotfix to a released FW, cut a liblcm patch release from the version that FW release pins

---

## Review items (marked during the design session)

1. **Sort provider and entry ordering for Lexbox: needs further consideration on the Lexbox side.** `DefaultDictionaryEntrySource` gives headword, lexeme, or citation ordering by writing-system collation without FW RecordList filters. Lexbox has to decide whether that is the order Webonary 2.0 wants, whether user filters or sort columns from the project matter, and how closely it must match FW output. The FW parity test (2.D) measures the gap.

## Follow-ups (out of scope; GitHub issues opened in 1.E)

1. **Nullable annotations for `SIL.LCModel.DictionaryConfiguration`.** The project starts with `<Nullable>disable</Nullable>`. Annotate the code and enable nullable checks.
2. **Correct image part types in Word export.** Every picture is stored as `ImagePartType.Jpeg`. The image size reader from 0.O-1 already detects the format; pass it to `AddImagePartToPackage`. Link to the `// TODO` added in 0.O-1.

## Appendix: FW-specific dependencies found in moving code (`412ee26b8`)

| Dependency | Where | Resolved by |
|---|---|---|
| `XCore.PropertyTable` / `ReadOnlyPropertyTable` | `GeneratorSettings`, `ILcmStylesGenerator`, `ILcmContentGenerator.SetRunStyle`, `IsEntryStyleRtl`, decorator, model, migrators, utils | 0.B, 0.C, 0.H, 0.J, 0.K |
| `RecordClerk` / `"ActiveClerk"` | `ConfiguredLcmGenerator.cs:424`, `StartEntry`, `GetSortWordForLetterHead`, `CurrentDecorator`/`GetDecorator`, decorator `:441` | 0.B, 0.C |
| `FontHeightAdjuster` (Widgets), `ExportStyleInfo` (Framework) | `ConfiguredLcmGenerator.cs:103` | 0.B, 0.L |
| `ConfiguredExport.GetLeadChar`, `XmlViewsUtils` GUIDs (Controls) | `:124-132`, `:2691-2707` | 0.D, 0.I |
| `FwUtils.GetCollatorForWs`, `Unicode`, `WavConverter`, `FwDirectoryFinder` | engine, model, migrators, utils | 0.D, 0.E, 0.H |
| `CssGenerator`, `LcmXhtmlGenerator`, `LcmWordGenerator.DocFragment` | `:435`, `:464-466`, `:2389`, `:2581-2583`, `:3549-3550` | 0.G, 0.B |
| `System.Web.UI.WebControls.HorizontalAlign` (.NET Framework only) | engine `:3341-3353`, `ILcmContentGenerator.AddTableCell`, three writers | 0.G |
| `SIL.ExCSS` 2.0.2 (.NET Framework assets only) | `CssGenerator`, `LcmXhtmlGenerator.SetRunStyle` | 0.N, 0.M |
| Clerk, `FwDirectoryFinder`, `xWorksStrings`, `DictionaryConfigurationListener`, `XhtmlDocView` paths | `LcmXhtmlGenerator.cs:47-540` (document drivers) | 0.M (stay in FW) |
| `PropertyTable`, `FontHeightAdjuster` | `CssGenerator` (most methods), `LcmXhtmlGenerator.SetRunStyle` | 0.B |
| `SpecialCharacterHandling` (`DictionaryDetailsView`) | `CssGenerator.cs:885`, `:963`, `:984` | 0.M |
| WPF `BitmapImage` (`PresentationCore`, `WindowsBase`) | `LcmWordGenerator.cs:2170-2186` | 0.O-1 |
| Process-wide static export state | `LcmWordGenerator.cs:43-51` | 0.O-2 |
| Clerk, `xWorksStrings`, `FwUtils.GetCollatorForWs`, `FontHeightAdjuster` | `LcmWordGenerator.cs:64-336` (`SavePublishedDocx`) | 0.O-3 (progress text and clerk mapping stay in FW) |
| `PropertyTable`, `FontHeightAdjuster` | `WordStylesGenerator`, `WordStyleCollection` | 0.B, 0.O |
| `System.Web.UI.WebControls` (`HorizontalAlign`) | `LcmWordGenerator.cs:1528`, `:1626` | 0.G |
| `copyFiles = true` media side effect | `LcmWordGenerator.cs:79-81` | 0.E (Word stops copying) |
| `ConfiguredExport.GetLetterLengthAt` | `GetLeadChar`; FW paging `LcmXhtmlGenerator.cs:493`, `:496` | 0.D |
| Hard-coded `\r\n` in test expectations | XHTML, CSS, Word, and engine test fixtures | 0.M, 0.O |
| `DictionaryConfigurationController` statics | model `:206-236`, `:334`, `:361`; migrators | 0.H |
| `DictionaryConfigurationListener` constants and directories | model, migrators, decorator | 0.H, 0.C |
| `ReversalIndexEntryUi` (FdoUi) | decorator `:424` | 0.C |
| `MessageBoxUtils` / `xWorksStrings` | `ShareNodeAsReference`; engine `:359`, `:1314`, `:3332` | 0.H, 0.C, 0.I |
| STA threads | `:192` | 0.F |
| Static mutable state | `:67`, `:72`, `:852`, `:1206`; model `:104` | 0.A |
| Dead usings (`ExCSS`, `System.Configuration`, `System.Runtime.Remoting.Messaging`, `SIL.FieldWorks.Filters`) | engine | 0.G |
