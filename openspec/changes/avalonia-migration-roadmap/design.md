## Context

> **Status note (2026-06-09 — resolved).** Execution diverged from the original sequence. After
> Gate 0, work proceeded directly into the lexical-edit program (sections 2–4 of
> `lexical-edit-avalonia-migration/tasks.md`) and built the region-model path
> (`ViewDefinitionModel`/`LexicalEditRegionModel` through `RecordEditView`) rather than Plan A's
> `DataTreeModel`/`SliceSpec`/`IDataTreeView`. `seam-domain-comparison.md` classifies wiring new
> ports into legacy `DataTree` internals as throwaway work, which undercuts Plan A's premise.
>
> **Resolution (1.13 done 2026-06-09):** `datatree-model-view-separation` is formally superseded as
> a migration gate. `DataTree` is frozen on the legacy side and will be deleted at end of the ~1-year
> coexistence phase; its internal extraction (`DataTreeModel`/`SliceSpec`/`IDataTreeView`) is
> optional legacy maintenance only. Gate 1 is redefined below around the region-model boundary.
> See `datatree-model-view-separation/hybrid-alignment.md` for the superseded banner and historical
> content.

The recommendation from the original plan-comparison analysis (Plan A "datatree-model-view-separation"
vs. Plan B "lexical-edit-avalonia-migration") was **Approach 3 then Approach 2**: a time-boxed
proof-of-concept spike, then the Hybrid (Plan B as the spine). Execution
confirmed that approach but resolved the Phase 1 boundary differently from the original plan: instead
of extracting a model layer from `DataTree`, Phase 1 built a typed IR path (`ViewDefinitionModel` →
`LexicalEditRegionModel`) that bypasses `DataTree` entirely on the Avalonia side.

**Plan A — `datatree-model-view-separation`** (superseded as migration gate): would have split
`DataTree.cs` into `DataTreeModel`/`SliceSpec`/`IDataTreeView`. Refactoring the internals of a class
that will be deleted in ~1 year is throwaway. DataTree stays frozen as the legacy surface.

**Plan B — `lexical-edit-avalonia-migration` (+ `avalonia-end-game`)**: the
end-to-end program. This is the active plan. Phase 1 was executed as sections 3–4 of the
lexical-edit tasks. Phase 7+ / shell is now owned by `avalonia-end-game` (the cutover), which absorbs
and supersedes `fieldworks-avalonia-shell-migration` (2026-06-20).

## Goals / Non-Goals

**Goals:**
- One ordered plan with explicit gates and a clear overlap resolution.
- Start with a small phase-0 spike, then the densest real screen (Lexical Edit via the DataTree region).
- Keep everything behind a default-off flag with WinForms as the safe default during transition.
- Preserve functional fidelity and density; pixel-perfect is explicitly not required.

**Non-Goals:**
- Duplicating the referenced changes' detailed requirements.
- Fixing shell timing before the regional gates are proven.

## Decisions

### 1. Sequence: phase-0 spike → first migrated region → Lexical Edit → Shell

**Decision:** Phase 0 is the entry-point spike; Phase 1 is the first migrated region via the region-model
path (lexical-edit-avalonia-migration sections 3–4); Phases 2–6 are the continued lexical-edit
program; Phase 7+ is the shell, gated on the regional gates.

**Rationale:** Banks cheapest risk reduction first (phase-0 spike), then the typed-IR + surface-seam
foundation that all further Avalonia screens build on, and defers the most expensive work (shell)
until the regional pattern is proven. The `DataTree` internal extraction was originally planned as
Phase 1 but is superseded: bypassing DataTree entirely on the Avalonia path is both simpler and
avoids investing in a class that will be deleted.

### 2. Region-model boundary as the seam

**Decision:** The boundary between legacy and Avalonia is `ViewDefinitionModel` (typed IR compiled
from XML layouts) + `LexicalEditRegionModel` (value-bound region) + `IRegionValueProvider` (seam to
LCModel). `RecordEditView` selects the surface via `LexicalEditSurfaceSelectionService`; the
active-host contract (`ActiveHostContract`) forbids driving hidden legacy `DataTree` infrastructure
when Avalonia is active. `DataTree` remains the complete legacy surface — no internal extraction.

**Rationale:** Avalonia does not need to understand DataTree's mental model (slices, XML configs,
ObjSeqHashMap reuse keys). The typed IR path is standalone, testable without WinForms, and can be
compiled off-thread. DataTree is deleted wholesale at end of the coexistence phase; the seam is at
RecordEditView routing, not inside DataTree.

### 3. Minimal-risk posture throughout

**Decision:** Every phase keeps WinForms as the default, lands behind tests, and is independently
valuable and reversible. No phase deletes native Views or makes Avalonia default until that region's
manifest gates pass.

## Master sequence and gates

```mermaid
flowchart TB
  subgraph P0["Phase 0 — entry-point spike (folded into lexical-edit-avalonia-migration)"]
    direction LR
    A0["Flag + in-proc host bridge<br/>one slice (3 editors)<br/>density/parity evidence"]:::spike
  end
  subgraph P1["Phase 1 — First migrated region (lexical-edit-avalonia-migration §3–4)"]
    direction LR
    A1["Seams → typed IR (ViewDefinitionModel) →<br/>LexicalEditRegionModel + IRegionValueProvider →<br/>RecordEditView routing + active-host contract"]:::region
  end
  subgraph P2["Phases 2–6 — Lexical Edit program (lexical-edit-avalonia-migration)"]
    direction LR
    A2["Seams → typed IR + XML import →<br/>Avalonia editors/tables →<br/>parity + Graphite/native gates"]:::spine
  end
  subgraph P7["Phase 7+ — Shell / cutover (avalonia-end-game, absorbs fieldworks-avalonia-shell-migration)"]
    direction LR
    A7["Shell contracts → Avalonia shell → screen migration →<br/>net48→net10 retarget → kill WinForms →<br/>Win/macOS/Linux cutover"]:::shell
  end

  G0{"Gate 0<br/>host bridge proven +<br/>density acceptable +<br/>flag dual-run works"}:::gate
  G1{"Gate 1<br/>LexicalEditRegionView at semantic parity,<br/>DataTree untouched on legacy path,<br/>active-host contract proven, no native/Graphite"}:::gate
  G2{"Gate 2<br/>Lexical Edit region complete:<br/>parity, native audit clean,<br/>Graphite-warned default<br/>(no native Graphite engine)"}:::gate

  P0 --> G0 --> P1 --> G1 --> P2 --> G2 --> P7

  classDef spike fill:#fef9c3,stroke:#ca8a04,color:#422006;
  classDef region fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef spine fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef shell fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef gate fill:#fee2e2,stroke:#b91c1c,color:#450a0a;
```

### Gate definitions

- **Gate 0 (phase-0 spike → region):** in-process net48 host bridge proven (or fallback recorded); density
  delta acceptable at 100% and 150% DPI; the same build runs either surface behind the flag;
  `spike-evidence.md` gives go.
- **Gate 1 (first region → continued program):** `LexicalEditRegionView` renders
  `LexicalEditRegionModel` built from `ViewDefinitionModel` + `IRegionValueProvider`; the legacy
  `DataTree` is untouched on the legacy path; `RecordEditView` routing selects the appropriate
  surface via `LexicalEditSurfaceSelectionService`; `RecordEditViewActiveHostContractTests` proves no
  hidden DataTree drive under Avalonia mode; semantic + density baseline matches within tolerance; no
  native Views or Graphite on the Avalonia path. **(Passed — lexical-edit-avalonia-migration §3–4
  complete as of 2026-06-09.)**
- **Gate 2 (program → shell):** the Lexical Edit region manifest passes — semantic parity, UIA2 legacy
  baselines, Avalonia.Headless tests, render-comparison evidence, native-viewing audit clean, and no
  native Graphite engine or native Views shaping on the Avalonia path. (Per
  `graphite-transition-support`, 2026-06-09: Graphite *presence* in a project no longer blocks an
  Avalonia default — the gate is per-writing-system classification + warning coverage; Graphite stays
  fully supported on legacy surfaces until the M2 sunset milestone.)

## Vocabulary — as-built (Phase 1)

The original overlap map mapped Plan A vocabulary to Plan B vocabulary. That mapping is superseded.
The vocabulary actually built:

```mermaid
flowchart TD
  subgraph IR["Typed IR (ViewDefinition)"]
    VDM["ViewDefinitionModel\n(layout compile output)"]
    VN["ViewNode\n(IR node: field, kind, ws,\nstableId, automationId,\nSurfaceRouting)"]
    XI["XmlLayoutImporter\n(XML layouts → IR)"]
    VDM --> VN
    XI -->|produces| VDM
  end

  subgraph Region["Region model (FwAvalonia)"]
    LRM["LexicalEditRegionModel\n(value-bound fields)"]
    LRMAP["LexicalEditRegionMapper\n(IR + values → region)"]
    IRVP["IRegionValueProvider\n(seam: LCModel-free)"]
    LRV["LexicalEditRegionView\n(data-driven Avalonia UI)"]
    LRMAP -->|projects| LRM
    IRVP -->|supplies values to| LRMAP
    LRM -->|rendered by| LRV
  end

  subgraph Seam["Surface seam (FwAvalonia + xWorks)"]
    LESS["LexicalEditSurfaceSelectionService\n(UIMode → SurfaceDecision)"]
    AHC["ActiveHostContract\n(forbids hidden DataTree drive)"]
    REV["RecordEditView\n(routes to legacy or Avalonia surface)"]
    LESS -->|informs| REV
    AHC -->|enforced by| REV
  end

  subgraph LCModel["LCModel boundary (xWorks)"]
    LRB["LexicalEditRegionBuilder\n(IRegionValueProvider impl;\nreads ILexEntry)"]
  end

  subgraph Legacy["Legacy (frozen)"]
    DT["DataTree\n(unchanged WinForms surface;\ndeleted at end of coexist phase)"]
  end

  VN -->|walked by| LRMAP
  LRB -->|implements| IRVP
  REV -->|Avalonia path: calls| LRB
  REV -->|Legacy path: calls| DT

  classDef ir fill:#fef9c3,stroke:#ca8a04,color:#422006;
  classDef region fill:#dcfce7,stroke:#16a34a,color:#052e16;
  classDef seam fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
  classDef lcm fill:#f3e8ff,stroke:#7e22ce,color:#3b0764;
  classDef legacy fill:#f1f5f9,stroke:#94a3b8,color:#475569;
  class VDM,VN,XI ir;
  class LRM,LRMAP,IRVP,LRV region;
  class LESS,AHC,REV seam;
  class LRB lcm;
  class DT legacy;
```

## Risk controls

- WinForms stays the default until each region's gate passes; the flag default is WinForms.
- Each phase is independently valuable and reversible; stalling at any phase still leaves value.
- No native Views deletion or Graphite default-path removal until the region manifest proves it.
- The phase-0 spike converts the roadmap's remaining estimates into measured numbers before the region starts.
