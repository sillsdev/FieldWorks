# Per-Region Migration Checklist

Copy this checklist into your working notes (or the OpenSpec change tasks)
at the start of a migration and keep it updated. It is the per-region
definition of done. Items map to the workflow phases in SKILL.md.

## Phase 0 — Phase-1 PR landing (only when reducing a derisk branch for merge)

Use this when collapsing a large multi-surface derisk branch into a landable PR (see SKILL.md
"Phase-1 Landing Strategy"). Skip for an ordinary single-region migration.

- [ ] Canonical screen chosen per primitive (table / detail-editor / tree+multi-select / tabs
      / owned-form / search-list); each is fully wired, green, and parity-evidenced
- [ ] Every deferred WinForms screen has `Docs/migration/<screen>.md` (from `_TEMPLATE.md`) with
      a live-FLEx PNG (capture via `fieldworks-winapp`), primitive, parity checklist, gotchas
- [ ] JIRA ticket filed per deferred screen, linked from its doc
- [ ] Each backed-out screen's wiring verified from the product call site (`file:line`) — NOT
      from class names / comments / an Explore summary (those have given false "unwired" results)
- [ ] Backed-out screens: view/VM/tests removed AND call site reverted to the legacy path
- [ ] XL surfaces with their own openspec change split to a follow-up PR (not backed out);
      shared composer infra stays in the spine PR
- [ ] PR body is a manifest: canonical screens + why, deferred screens + doc/JIRA, split-out PRs

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

- [ ] **Headless integration scenarios cover the surface's key workflows**
      (filter → clear, select → detail follows, edit → commit → refresh,
      navigate), driven via the harness (architecture-patterns.md §13) on
      `./test.ps1` — surface layer in an Avalonia-headless assembly, plus the
      real-clerk layer (`xWorksTests`) for domain claims like list narrowing/
      sort/undo. No behavior/workflow claim left to "live verification" that a
      headless scenario could prove (parity-evidence.md §2a / §3)
- [ ] Path 3 bundle produced per scenario (see parity-evidence.md §1)
- [ ] Semantic, visual, workflow, and performance lanes each prove their
      own axis; no lane substitutes for another
- [ ] Performance within budget (≤ legacy total × 1.2, or accepted delta
      recorded)
- [ ] 100% and 150% DPI captured

## Phase 8 — Localization

- [ ] Field labels resolve through the StringTable lane via the IR's
      `LocalizationKey`
- [ ] Avalonia chrome resolves through the existing LocalizationManager
      XLIFF catalog; existing `Palaso`/`Chorus` ids are reused only when
      semantics and markup match, otherwise unique Avalonia-prefixed ids are
      added there
- [ ] The accessor-owned English defaults remain the single Avalonia chrome
      source of truth; no parallel Avalonia-only string source is introduced
- [ ] New csprojs carry `<RootNamespace>` while any legacy Avalonia `.resx`
      artifact remains (Crowdin satellite build)
- [ ] Product, preview-host, and headless-test paths each prove their
      LocalizationManager bootstrap or intentional English fallback
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
