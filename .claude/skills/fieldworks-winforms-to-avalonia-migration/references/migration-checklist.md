# Per-Region Migration Checklist

Copy this checklist into your working notes (or the OpenSpec change tasks)
at the start of a migration and keep it updated. It is the per-region
definition of done. Items map to the workflow phases in SKILL.md.

## Phase 1 — Inventory and scope

- [ ] Legacy surface identified: entry points, layouts/parts, custom slice
      classes, dialogs, choosers, command/listener wiring
- [ ] Custom slice class census taken and compared against the plugin
      registry (`Src/xWorks/RegionEditorPlugins.cs`) — list of missing
      plugins recorded
- [ ] Coverage map drafted (behavior × test status: covered / deferred
      with owner / blocked by named seam)
- [ ] Branch scope reviewed with `fieldworks-migration-scope-review`
      (branch-only diff, split triggers checked)

## Phase 2 — Characterize before refactor

- [ ] Semantic baseline captured for the legacy surface (bindings, labels,
      editor kinds, visibility, ghost state, focus order, WS metadata,
      accessibility identity)
- [ ] Legacy timing baseline measured and committed
- [ ] Legacy UIA smoke coverage exists for launcher/chooser reachability
- [ ] All characterization tests run via `./test.ps1` (not branch-only lanes)

## Phase 3 — Seams

- [ ] Existing seams reused from `Src/Common/FwAvalonia/Seams/ISeams.cs`
- [ ] Any new seam added to `references/seam-catalog.md` with purpose,
      rules, and pivot trigger
- [ ] No region code reaches directly into PropertyTable/mediator/LCModel
      outside a seam

## Phase 4 — Controls

- [ ] Control choices follow architecture-patterns.md §4 (owned controls;
      bounded TreeView ceiling respected)
- [ ] Any deviation justified by a fired pivot trigger, recorded in
      seam-catalog.md §3

## Phase 5 — Region composition

- [ ] Composer walks compiled IR; region model keyed by StableId
- [ ] Custom classes resolve plugin → companion strip → explicit
      unsupported row (no silent fallback)
- [ ] Custom-field placeholders expand from LCModel metadata at compile
      time; ghost rows are runtime state only
- [ ] Stable AutomationIds derived from StableId
      (`{StableId}`, `{StableId}.Label`, `{StableId}.{WsAbbrev}`)

## Phase 6 — Host wiring

- [ ] Every affected host has an explicit `HostUiBehavior` (supported /
      explicit legacy fallback / blocked)
- [ ] Full wiring path traced: setting source → persisted state →
      PropertyTable key → broadcast → listener → host reload → focus and
      command routing → save/`PrepareToGoAway()` → fallback
- [ ] Active-host contract holds: no hidden legacy DataTree/menu/renderer
      driven while Avalonia is active
- [ ] Reviewed with `fieldworks-ui-wiring-review`

## Phase 7 — Parity evidence

- [ ] Path 3 bundle produced per scenario (see parity-evidence.md §1)
- [ ] Semantic, visual, workflow, and performance lanes each prove their
      own axis; no lane substitutes for another
- [ ] Performance within budget (≤ legacy total × 1.2, or accepted delta
      recorded)
- [ ] 100% and 150% DPI captured

## Phase 8 — Localization

- [ ] Field labels resolve through the StringTable lane via the IR's
      `LocalizationKey`
- [ ] Product messages in `FwAvaloniaStrings.resx` with translator comments
- [ ] New csprojs carry `<RootNamespace>` (Crowdin satellite build)
- [ ] AutomationIds nonlocalized; automation Names localized
- [ ] Reviewed with `fieldworks-localization-review`

## Phase 9 — Retirement and gates

- [ ] Forbidden-symbol audit passes (`EngineIsolationAuditTests.cs`);
      new forbidden symbols added to the audit and parity-evidence.md §4
- [ ] Active-host contract tests pass
      (`RecordEditViewActiveHostContractTests.cs`)
- [ ] `./build.ps1` and `./test.ps1` pass; `openspec validate <change>
      --strict` passes when an OpenSpec change is attached
- [ ] Legacy code scheduled for removal is listed explicitly (what, when,
      behind which gate)

## Phase 10 — Retrospective (updates this skill set)

- [ ] New patterns/gotchas/pivots recorded per the protocol in
      `references/lessons-learned.md`
- [ ] New plugins added to the canonical examples in
      architecture-patterns.md §5
- [ ] Stale file pointers in any fieldworks-* skill fixed
- [ ] Skill edits included in the same PR as the migration
