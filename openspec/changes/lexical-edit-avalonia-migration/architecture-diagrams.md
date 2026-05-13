# Lexical Edit Avalonia Migration Architecture Diagrams

These diagrams summarize the current WinForms architecture, the migration seams, the testing strategy, the first optional Avalonia slices, the table/full Lexical Edit path, and the final default architecture after Graphite and native viewing/rendering are removed from migrated regions.

Legend used across diagrams:

- Red dashed nodes are decommissioning targets for completed Avalonia regions.
- Green nodes are Avalonia or future managed UI pieces.
- Blue nodes are dependency-inverted service contracts.
- Yellow nodes are validation and test layers.
- Purple nodes are model or canonical data contracts.

## 1. Current WinForms Architecture and MVC Pressure

The current stack mixes model access, controller behavior, view creation, refresh policy, and native rendering inside the same path. This is why it is hard to test in isolation and why wrapping it in Avalonia would preserve the wrong boundary.

```mermaid
flowchart TB
  User["User input<br/>keyboard, mouse, menus"]:::actor
  Mediator["xCore Mediator<br/>PropertyTable<br/>command routing"]:::controller
  RecordEdit["RecordEditView<br/>screen host"]:::mixed
  DataTree["DataTree<br/>refresh, focus, layout,<br/>slice ownership"]:::mixed
  SliceFactory["SliceFactory<br/>XML interpretation<br/>editor selection"]:::mixed
  Slices["Slices and launchers<br/>WinForms controls<br/>business decisions"]:::mixed
  XMLViews["XMLViews browse/table views<br/>view definitions plus rendering"]:::mixed
  RootSite["RootSite / SimpleRootSite<br/>managed/native bridge"]:::decom
  NativeViews["Native Views C++<br/>layout, measurement,<br/>selection, hit testing,<br/>editing"]:::decom
  Graphite["Graphite engine<br/>Graphite feature settings"]:::decom
  Gecko["Gecko/XWebBrowser/PDF<br/>Graphite-enabled preview/export"]:::decom
  XMLParts["XML Parts/Layout<br/>customer overrides<br/>ghosts, choosers, visibility"]:::model
  LCModel["LCModel<br/>lexicon data<br/>transactions"]:::model
  WS["Writing systems<br/>fonts, script metadata,<br/>legacy Graphite flags"]:::model
  Tests["Hard-to-isolate tests<br/>UIA can drive shell;<br/>owner-drawn content is opaque"]:::test

  User --> Mediator --> RecordEdit --> DataTree
  DataTree --> SliceFactory --> Slices
  SliceFactory --> XMLParts
  Slices --> LCModel
  Slices --> RootSite --> NativeViews
  XMLViews --> RootSite
  NativeViews --> Graphite
  DataTree --> WS
  NativeViews --> WS
  Gecko --> Graphite
  Gecko --> WS
  DataTree -. MVC violation: view owns refresh and control policy .-> LCModel
  SliceFactory -. MVC violation: view factory owns editor decisions .-> LCModel
  Slices -. MVC violation: launchers mix UI and business rules .-> LCModel
  Tests -. brittle / broad .-> DataTree

  classDef actor fill:#f8fafc,stroke:#64748b,color:#0f172a;
  classDef controller fill:#e0f2fe,stroke:#0284c7,color:#082f49;
  classDef mixed fill:#fff7ed,stroke:#f97316,color:#431407;
  classDef model fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef test fill:#fef9c3,stroke:#ca8a04,color:#422006;
  classDef decom fill:#fee2e2,stroke:#b91c1c,stroke-width:2px,stroke-dasharray: 6 4,color:#450a0a;
```

## 2. Dependency Inversion Path and Better MVC

The first architectural move is not Avalonia. It is extracting narrow ports around refresh, view definitions, editor selection, chooser behavior, writing-system text, diagnostics, and retained linguistics services. Legacy WinForms becomes one adapter. Avalonia becomes another adapter later.

```mermaid
flowchart LR
  subgraph Model["Model and canonical contracts"]
    LCModel["LCModel<br/>data and transactions"]:::model
    TypedIR["Typed view definition<br/>Presentation IR<br/>stable node identity"]:::model
    XMLImport["XML import adapter<br/>transitional compatibility"]:::adapter
  end

  subgraph Ports["Dependency-inverted ports"]
    Refresh["IRefreshCoordinator"]:::port
    Editors["IEditorRegistry"]:::port
    Choosers["IChooserService"]:::port
    Text["IWritingSystemTextService<br/>OpenType/HarfBuzz only"]:::port
    Context["ILexicalContext<br/>cache, mediator, property table"]:::port
    Linguistics["ILinguisticsServiceGateway<br/>XAmple, spelling, parsers"]:::port
    Capture["IRenderParityCapture"]:::port
  end

  subgraph LegacyAdapters["Legacy adapters during migration"]
    LegacyHost["RecordEditView/DataTree adapter"]:::legacy
    LegacySlices["Legacy slice adapter"]:::legacy
    LegacyViews["Native Views baseline adapter"]:::decom
  end

  subgraph FutureAdapters["Future adapters"]
    AvaloniaHost["Avalonia screen host"]:::future
    AvaloniaEditors["FieldWorks-owned Avalonia editors"]:::future
    TableTree["Avalonia table/tree renderer"]:::future
  end

  XMLParts["XML Parts/Layout"]:::legacy --> XMLImport --> TypedIR
  LCModel --> TypedIR
  TypedIR --> Editors
  TypedIR --> Capture
  Refresh --> LegacyHost
  Editors --> LegacySlices
  Capture --> LegacyViews
  Editors --> AvaloniaEditors
  Refresh --> AvaloniaHost
  Text --> AvaloniaEditors
  Choosers --> AvaloniaEditors
  Context --> AvaloniaHost
  Linguistics --> AvaloniaEditors
  AvaloniaHost --> TableTree

  classDef model fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef port fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef adapter fill:#e0f2fe,stroke:#0284c7,color:#082f49;
  classDef future fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef legacy fill:#fff7ed,stroke:#f97316,color:#431407;
  classDef decom fill:#fee2e2,stroke:#b91c1c,stroke-width:2px,stroke-dasharray: 6 4,color:#450a0a;
```

## 3. Testing and Validation Map

Tests are layered around the seam being proven. Deep behavior moves to unit and integration tests. UI automation stays narrow. Render verification captures both semantic and visual evidence. Avalonia.Headless covers new controls without booting the full application.

```mermaid
flowchart TB
  Requirements["Migration requirements<br/>density, interaction, fonts,<br/>Graphite-free default, no native viewing"]:::model

  Unit["Unit tests<br/>refresh state<br/>launcher logic<br/>editor registry"]:::test
  Integration["Integration tests<br/>XML import to typed IR<br/>LCModel transactions<br/>cache invalidation"]:::test
  Semantic["Semantic parity snapshots<br/>fields, labels, bindings,<br/>ghosts, focus, accessibility"]:::test
  LegacyUIA["UIA2 legacy smoke<br/>menus, dialogs, chooser launch,<br/>table header reachability"]:::test
  Render["Render comparison<br/>near-pixel evidence<br/>timing buckets<br/>failure bundles"]:::test
  Headless["Avalonia.Headless<br/>input, focus, popups,<br/>control behavior"]:::test
  NativeAudit["Native viewing seam audit<br/>no RootSite / IVwEnv / Views<br/>inside completed region"]:::test
  GraphiteAudit["Graphite-free audit<br/>no graphite2, GraphiteEngine,<br/>Gecko Graphite, feature strings"]:::test

  Seam1["Refactor seams"]:::port
  Seam2["Typed IR and XML import"]:::port
  Slice1["First optional Avalonia slices"]:::future
  Slice2["Tables and Lexical Edit regions"]:::future
  Default["Default Avalonia readiness"]:::future

  Requirements --> Unit --> Seam1
  Requirements --> Integration --> Seam2
  Requirements --> Semantic --> Seam2
  Requirements --> LegacyUIA --> Seam1
  Requirements --> Render --> Slice2
  Requirements --> Headless --> Slice1
  Requirements --> NativeAudit --> Default
  Requirements --> GraphiteAudit --> Default
  Seam1 --> Slice1 --> Slice2 --> Default
  Seam2 --> Slice1

  classDef model fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef port fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef future fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef test fill:#fef9c3,stroke:#ca8a04,color:#422006;
```

## 4. First Optional Avalonia Slices: Hover, Popup, Simple Editors

The first slices should be optional and low-blast-radius. They use the same ports that legacy code uses, but the rendered surface is Avalonia-owned and can run in the Preview Host or headless tests.

```mermaid
flowchart LR
  subgraph LegacyShell["Existing WinForms shell remains default"]
    RecordEdit["RecordEditView/DataTree"]:::legacy
    EditorRegistry["Editor registry seam"]:::port
    LegacySlice["Legacy slice fallback"]:::legacy
  end

  subgraph OptionalAvalonia["Optional first Avalonia slice"]
    PreviewHost["Preview Host or feature flag"]:::future
    SimpleEditor["Simple text/scalar editor"]:::future
    Hover["Hover card / popup chooser"]:::future
    Headless["Avalonia.Headless tests"]:::test
  end

  subgraph Contracts["Shared contracts"]
    IR["Typed IR node"]:::model
    Text["Writing-system text service<br/>OpenType/HarfBuzz"]:::port
    Chooser["Chooser service"]:::port
    Linguistics["Linguistics service gateway<br/>spelling/XAmple allowed"]:::port
  end

  Decom["Not allowed in this slice<br/>RootSite, IVwEnv, native Views,<br/>Graphite"]:::decom

  RecordEdit --> EditorRegistry
  EditorRegistry --> LegacySlice
  EditorRegistry --> PreviewHost
  PreviewHost --> SimpleEditor
  PreviewHost --> Hover
  IR --> SimpleEditor
  Text --> SimpleEditor
  Chooser --> Hover
  Linguistics --> SimpleEditor
  Headless --> SimpleEditor
  Headless --> Hover
  SimpleEditor -. must not call .-> Decom
  Hover -. must not call .-> Decom

  classDef model fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef port fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef future fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef legacy fill:#fff7ed,stroke:#f97316,color:#431407;
  classDef test fill:#fef9c3,stroke:#ca8a04,color:#422006;
  classDef decom fill:#fee2e2,stroke:#b91c1c,stroke-width:2px,stroke-dasharray: 6 4,color:#450a0a;
```

## 5. Lexical Edit and Table Views Slice

Table views and full Lexical Edit regions are meaningfully different from the first hover/simple-editor slices. They need virtualization, stable row/node identity, selection and scrolling services, table/tree templates, and stronger parity gates.

```mermaid
flowchart TB
  subgraph Inputs["Canonical inputs"]
    LCModel["LCModel data"]:::model
    XMLImport["XML import<br/>transition only"]:::adapter
    IR["Typed view definition / IR<br/>sections, fields, tables,<br/>tree nodes, editor descriptors"]:::model
  end

  subgraph AvaloniaRegion["Migrated table or Lexical Edit region"]
    Host["Avalonia region host"]:::future
    Virtualizer["Virtualized table/tree coordinator"]:::future
    Rows["Dense row/node templates<br/>multiple writing-system alternatives"]:::future
    Editors["Editor registry<br/>cell and field editors"]:::future
    Selection["Managed selection, focus,<br/>scroll, hit-test metadata"]:::future
  end

  subgraph Services["Ports and services"]
    Refresh["Refresh coordinator"]:::port
    Text["Writing-system text service<br/>OpenType/HarfBuzz"]:::port
    Chooser["Chooser and popup services"]:::port
    Linguistics["Custom linguistics services<br/>XAmple/spelling/parsers"]:::port
    Diagnostics["Diagnostics and parity capture"]:::port
  end

  subgraph Gates["Completion gates"]
    Semantic["Semantic parity"]:::test
    Render["Render/timing evidence"]:::test
    NativeAudit["No native viewing/rendering/editor path"]:::test
    GraphiteAudit["Graphite-free default path"]:::test
  end

  Decommissioned["Decommission for this region<br/>DataTree slices, XMLViews runtime,<br/>RootSite/IVwEnv/Native Views,<br/>Graphite render engine"]:::decom

  LCModel --> IR
  XMLImport --> IR
  IR --> Host --> Virtualizer --> Rows
  Virtualizer --> Selection
  Rows --> Editors
  Refresh --> Host
  Text --> Rows
  Chooser --> Editors
  Linguistics --> Editors
  Diagnostics --> Semantic
  Host --> Semantic
  Host --> Render
  Host --> NativeAudit
  Host --> GraphiteAudit
  Host -. must not call .-> Decommissioned

  classDef model fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef adapter fill:#e0f2fe,stroke:#0284c7,color:#082f49;
  classDef port fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef future fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef test fill:#fef9c3,stroke:#ca8a04,color:#422006;
  classDef decom fill:#fee2e2,stroke:#b91c1c,stroke-width:2px,stroke-dasharray: 6 4,color:#450a0a;
```

## 6. Final Default Architecture After Avalonia and Graphite Decommissioning

In the final default path, MVC is explicit: LCModel and canonical view definitions are model/data contracts; presenters and services coordinate commands, refresh, transactions, and diagnostics; Avalonia controls own display and input. Graphite and native viewing/rendering are outside the default path. Retained native linguistics engines are service dependencies, not UI dependencies.

```mermaid
flowchart TB
  subgraph ModelLayer["Model and canonical definitions"]
    LCModel["LCModel<br/>lexicon data and transactions"]:::model
    Canonical["Canonical typed view definitions<br/>post-XML runtime contract"]:::model
    ProjectSettings["Project settings<br/>writing systems, fonts,<br/>OpenType/HarfBuzz features"]:::model
  end

  subgraph ControllerLayer["Controller / presenter layer"]
    Presenter["Lexical Edit presenters<br/>commands, refresh, selection policy"]:::controller
    EditorRegistry["Editor registry"]:::port
    Transaction["Transaction and validation services"]:::port
    Diagnostics["Diagnostics and parity hooks"]:::port
  end

  subgraph ViewLayer["Avalonia view layer"]
    Shell["Avalonia Lexical Edit shell"]:::future
    Detail["Dense detail editors"]:::future
    Tables["Virtualized table/tree views"]:::future
    Popups["Choosers, flyouts, hover cards"]:::future
  end

  subgraph ServiceLayer["FieldWorks services"]
    Text["Writing-system text service<br/>OpenType/HarfBuzz only"]:::port
    Linguistics["Custom linguistics gateway<br/>XAmple, spelling, parsers,<br/>Encoding Converters, ICU"]:::port
    BrowserPdf["Non-Graphite browser/PDF strategy"]:::port
  end

  subgraph Decommissioned["Removed from default Avalonia path"]
    DataTree["DataTree/Slice runtime"]:::decom
    XMLRuntime["Runtime XML Parts/Layout"]:::decom
    NativeViews["RootSite, IVwEnv,<br/>ManagedVwWindow, Native Views"]:::decom
    Graphite["graphite2, GraphiteEngine,<br/>Graphite feature settings"]:::decom
    Gecko["Gecko Graphite rendering<br/>GeckofxHtmlToPdf assumptions"]:::decom
  end

  LCModel --> Presenter
  Canonical --> Presenter
  ProjectSettings --> Text
  Presenter --> Shell
  Presenter --> EditorRegistry
  Presenter --> Transaction
  Shell --> Detail
  Shell --> Tables
  Shell --> Popups
  EditorRegistry --> Detail
  EditorRegistry --> Tables
  Text --> Detail
  Text --> Tables
  Linguistics --> Presenter
  BrowserPdf --> Presenter
  Diagnostics --> Presenter
  Shell -. default path excludes .-> Decommissioned

  classDef model fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef controller fill:#e0f2fe,stroke:#0284c7,color:#082f49;
  classDef port fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef future fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef decom fill:#fee2e2,stroke:#b91c1c,stroke-width:2px,stroke-dasharray: 6 4,color:#450a0a;
```