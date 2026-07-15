# Canonical Post-XML View-Definition Authoring/Storage Format (Task 9.1)

> **Status: PROPOSAL FOR OWNER REVIEW — not decided.** Date: 2026-06-09.
> This answers design.md Open Question 1 ("C# builders, JSON/YAML, resources, database-backed
> project settings, or a hybrid?") with a layered recommendation grounded in the current
> codebase. Nothing here changes runtime behavior until tasks 9.2–9.4 implement it behind the
> existing gates.

## 1. What "canonical format" must replace

Today's stack, as actually built:

- **Shipped definitions:** `DistFiles/Language Explorer/Configuration/Parts/*.fwlayout` +
  `*Parts.xml`, loaded by `Inventory`/`LayoutCache`
  (`Src/Common/Controls/XMLViews/LayoutCache.cs`) into merged XML node trees keyed by
  `layout = {class, type, name, choiceGuid}` and `part = {id}`.
- **Customer/user overrides:** `Inventory.PersistOverrideElement` (`Src/XCore/Inventory.cs`)
  writes a **whole copy of the customized `<layout>` element** into a per-project file under
  the project ConfigurationSettings folder (e.g. `LexEntry.fwlayout`), stamped with
  `LayoutCache.LayoutVersionNumber` (currently **27**). On load, version-mismatched overrides
  are dropped or migrated by `LayoutMerger` (`Inventory.Merger`). Holding Shift at startup
  deletes overrides (the documented support-recovery path).
- **Typed IR (already built):** `ViewDefinitionModel`/`ViewNode`
  (`Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionModel.cs`) with `StableId`, kind,
  field binding, editor classification, writing system, visibility, expansion,
  `CustomFieldPlaceholder` nodes, optional `LocalizationKey`/`AutomationId`/`SurfaceRouting`,
  per-node diagnostics, and a deterministic `ToSnapshot()` used by parity baselines.
- **Migration tooling seed (already built):** `XmlLayoutImporter` + `DictionaryPartResolver`
  + `ViewDefinitionCompiler` (cache key fingerprint, off-thread compile over immutable
  `ViewDefinitionSourceSnapshot`). Measured coverage over shipped files
  (`layout-import-coverage.md`): 136 detail layouts imported, 70.5% element-occurrence /
  51.1% attribute-occurrence coverage, with a full drop-diagnostic taxonomy. The 594
  jtview/publishing layouts are out of detail-lane scope.

Two structural lessons from the legacy system drive the recommendation:

1. **Whole-element override copies go stale.** Because an override is a full copy of the
   shipped `<layout>`, every shipped layout improvement misses customized projects until
   `LayoutVersionNumber` is bumped and `LayoutMerger` re-merges — a manual, lossy, whole-tree
   merge. The replacement must store overrides as **sparse deltas against stable node
   identity**, not copies.
2. **Custom fields are injected, not authored.** Legacy layouts mark injection points
   (`customFields="here"`, ~161 `<generate>` occurrences in shipped files) and the runtime
   expands them from LCModel metadata. The canonical format must keep custom fields as
   **placeholder + runtime expansion**, never baked into stored definitions, or stored files
   drift from each project's model.

## 2. Options evaluated

### Criteria

(a) customer override story, (b) diffability/versioning, (c) custom-field injection,
(d) localization keys, (e) migration tooling from existing XML, (f) runtime loading,
(g) hand-editability for support staff.

### Option 1: C# builder code (fluent builders compiled into the product)

- (a) **Fails as the only store.** Customers and support staff cannot express per-project
  overrides in code that requires recompilation; a second, data-based format would still be
  needed — so builders cannot be *canonical*.
- (b) Excellent for shipped definitions: PR review, refactoring tools, compile-time checks
  against `ViewNodeKind`/`EditorClassification`.
- (c) Natural (`AddCustomFieldPlaceholder()`).
- (d) Natural (string keys checked by tests against `.resx`).
- (e) Poor: the importer produces *data*; turning data into maintained C# source is codegen
  that humans then own — a one-way, high-friction migration.
- (f) Instant (no parse), but only for shipped definitions.
- (g) None.
- **Precedent in repo:** `LexicalEditRegionBuilder.BuildFirstSliceDefinition()` was exactly
  this, and task 4.10 deliberately *demoted it to a diagnosed fallback* in favor of compiling
  from the layout inventory — evidence that hand-maintained builder code drifts from the
  shipped contract.

### Option 2: JSON

- (a) Strong: sparse override/patch documents are idiomatic; schema-validated.
- (b) Strong: line-diffs well, merge tooling exists, stable when generated deterministically
  (ordered keys, one node per object).
- (c) Expressible as a node kind (`"kind": "customFieldPlaceholder"`), matching the IR.
- (d) Plain fields (`"localizationKey": "..."`), matching `ViewNode.LocalizationKey`.
- (e) **Best of all options:** the importer already produces the typed IR; serializing
  `ViewDefinitionModel` to JSON is a projection, and `ToSnapshot()` equality gives a
  ready-made round-trip parity test.
- (f) Good: fast parse, works on net48 (System.Text.Json package or Newtonsoft.Json, both
  already in the dependency universe); fits `ViewDefinitionSourceSnapshot`/`CompileAsync`/
  cache-key fingerprinting unchanged.
- (g) Adequate: readable, but no comments (mitigate with `"//"`-style description fields or
  JSONC for non-shipped files) and quoting noise.

### Option 3: YAML

- (a)/(b)/(c)/(d) comparable to JSON in expressiveness.
- (g) Better hand-editability (comments, less punctuation) — its only advantage.
- (e)/(f) Worse: adds a parser dependency (YamlDotNet) to net48 `FwAvalonia`, which currently
  has a deliberately minimal reference set enforced by `EngineIsolationAuditTests`; whitespace
  sensitivity and implicit typing (the `no`/`Norway` class of surprises) are real support
  hazards for the exact audience hand-editing files.
- **Verdict:** not worth a new dependency and a second wire format for a comment syntax.

### Option 4: XML, new schema

- (a)/(b)/(c)/(d) all expressible; XML diff/merge is familiar to this codebase.
- (e) Moderate: still requires the same IR projection work as JSON, *plus* a new schema.
- (g) Support staff know XML from `.fwlayout` — but that is also the trap: a new-schema XML
  file is visually indistinguishable from legacy layout XML, inviting copy-paste of legacy
  constructs (the 49%-unhandled attribute vocabulary) into the new store and making "is this
  file legacy or canonical?" a recurring support question. It also undercuts the explicit
  program goal ("retire XML") in a way that is hard to communicate.
- **Verdict:** workable but strictly dominated by JSON except on familiarity.

### Option 5: Database-backed project settings (definitions in the LCModel project DB)

- (a) Overrides would live where project data lives and travel with Send/Receive
  automatically.
- (b) **Fails:** opaque to diff/review; definition changes get entangled with data-model
  migrations; no PR review for shipped definitions at all.
- (e)/(f) Couples view-definition load to cache startup; breaks the off-thread immutable
  snapshot compile model (task 4.6) which exists precisely to avoid live-cache reads.
- (g) **Fails:** support staff lose the two recovery moves they rely on today — inspect the
  override file, or delete it (Shift-at-startup). A DB row is neither inspectable nor safely
  deletable in the field.
- **Verdict:** rejected as a store. (Note: per-list `choiceGuid` layouts are *generated from*
  DB content today; generation stays, but the generated artifact should be a file in the
  override layer, not a DB-resident definition.)

## 3. Recommendation: layered JSON, generated-then-owned, with stable-id patches

**Layer 1 — Shipped definitions: deterministic JSON documents, one per
`{class, type, name}` layout, generated from the existing XML by the importer during
transition, then hand-owned after retirement.**

- During transition (9.2–9.3) the build generates them via
  `XmlLayoutImporter` → `ViewDefinitionModel` → canonical JSON serializer; generated output is
  committed (not build-transient) so every shipped-definition change is a reviewable diff.
- After XML retirement the JSON files become the maintained source of truth; the XML and the
  generator are deleted. A versioned JSON Schema validates them in CI.
- A thin C# builder API is retained **only** for tests and programmatic definition assembly
  (the `LexicalEditRegionBuilder` fallback precedent) — it is an API over the model, not a
  storage format.
- Localization stays out of the definition files: nodes carry `localizationKey` resolving to
  `.resx`/Crowdin (task 6.11), exactly as `ViewNode.LocalizationKey` already models. Literal
  labels are permitted only in customer overrides.

**Layer 2 — Per-project customer overrides: sparse JSON patch documents keyed by
`ViewNode.StableId`,** stored as files in the project ConfigurationSettings folder (same
location family as today's override `.fwlayout` files, so backup/Send-Receive/support habits
carry over).

- Patch operations, deliberately small: `setVisibility`, `setLabel`, `reorderChildren`,
  `duplicateNode` (the legacy copy-with-suffix mechanism), `addNode` (customer-authored field
  arrangements), `hideNode`. Anything not expressible is an explicit migration diagnostic,
  never a silent drop — same policy as task 4.9.
- Each patch file carries a `formatVersion` (successor to `LayoutVersionNumber = 27`) and the
  shipped-definition fingerprint it was authored against; the loader migrates or quarantines
  stale patches with a user-visible diagnostic — replacing `LayoutMerger`'s whole-tree merge
  with per-node, per-operation reconciliation. Patches referencing a `StableId` that no longer
  exists are reported individually instead of invalidating the whole override.
- This directly fixes legacy lesson 1: shipped improvements flow to customized projects
  because the base is never copied.

**Layer 3 — Runtime expansion (not stored):** custom fields (`CustomFieldPlaceholder` +
LCModel metadata), `$ws` writing-system expansion, and per-list `choiceGuid` generated views
are expanded at compile time by `ViewDefinitionCompiler` over the immutable snapshot, exactly
as the legacy runtime expands `<generate>`/`customFields="here"`. Stored files never contain
project-model-derived nodes.

**Explicitly rejected:** YAML (new dependency, second format, whitespace hazards), new-schema
XML (legacy/canonical confusion, undercuts retirement), database-backed definitions (opaque,
breaks snapshot compile and field recovery), C#-builders-as-canonical-store (cannot express
customer overrides; in-repo precedent 4.10 shows drift).

### Why this is the conclusion the codebase points to

1. The typed IR is already the runtime contract (design decision 2); the only open question is
   its at-rest encoding. JSON is the lowest-friction faithful projection of
   `ViewDefinitionModel`, and `ToSnapshot()` equality is a free round-trip oracle.
2. `StableId` already exists on every node and is already the key for semantic baselines
   (task 2.8) — patch-by-stable-id reuses the identity scheme the test infrastructure depends
   on, instead of inventing a second one.
3. The override pain that support staff live with (stale whole-copies, version-bump merges) is
   a *copy-vs-delta* problem, not an *XML-vs-JSON* problem — so the layered answer (base +
   sparse patches) matters more than the syntax choice, and only Options 2–4 can express it;
   JSON expresses it with the fewest new moving parts.

## 4. Migration sequence

1. **Serializer + schema (new, feeds 9.2):** canonical JSON writer/reader for
   `ViewDefinitionModel` with deterministic ordering; round-trip test = snapshot equality
   against the importer's output for every shipped detail layout (the 136 already measured).
2. **Generate shipped JSON (9.2/9.3):** build step runs importer over shipped
   `.fwlayout`/`*Parts.xml`, commits generated JSON, and publishes the audit report
   (extension of `layout-import-coverage.md`). Dual-run gate: XML-import path and JSON-load
   path must produce identical snapshots and identical `ViewDefinitionCacheKey` semantics.
3. **Override migrator (9.2/9.3):** for each project override `.fwlayout`, import both shipped
   and overridden layouts to IR, diff by `StableId`, emit a JSON patch file plus a per-project
   audit report listing every non-representable customization (mandatory fixtures from
   `override-fixtures.md` §2 prove this on real customer overrides).
4. **Gated runtime flip (9.4):** the first migrated surface loads shipped JSON + project
   patches; XML import remains available as an audit/fallback lane behind the same surface
   gates (`region-manifest.md`), with rollback to XML-import resolution as the documented
   recovery switch.
5. **Retirement (9.5):** delete runtime XML resolution for migrated surfaces only after the
   9.5 blocker list (custom fields, ghost items, table views, choosers, TreeView-heavy views)
   is empty for that surface; non-migrated legacy surfaces keep reading XML until they are
   themselves migrated — the canonical format never needs to serve legacy `DataTree`.

## 5. Open questions for the owner

1. **StableId durability contract:** today `StableId` derives from layout paths (task 4.10).
   When shipped definitions are restructured, do we guarantee id stability by hand (review
   rule + CI guard), or ship an id-remap table per `formatVersion` so old patches re-key
   automatically? (Recommendation: both — guard by default, remap table when a restructure is
   unavoidable.)
2. **Send/Receive merge:** do patch files get a Chorus/FLEx Bridge merge handler (field-level
   merge of patch operations), or keep today's effective last-writer-wins for settings files?
   Needs a product decision before customers collaborate on customized views.
3. **Scope boundary with dictionary/reversal configuration:** `<Project>/Configuration/
   Dictionary/*.xml` is a separate, already-versioned configuration system with its own
   migrators. Out of scope here (this design covers Parts/Layout detail+browse definitions),
   but the owner should confirm whether it eventually converges on the same patch model.
4. **Ghost items and `if`/`ifnot` conditionals:** the IR does not yet model ghost metadata or
   conditional visibility (deferred from 4.1; 230 `if` occurrences in shipped files). The
   canonical schema must reserve their representation *before* Layer-1 generation freezes, or
   the generated files will need a breaking `formatVersion` bump early.
5. **Hand-edit ergonomics:** is plain JSON acceptable for support staff, or do we admit JSONC
   (comments) for override files only, keeping shipped files strict? (Recommendation: strict
   JSON everywhere; put human context in a per-file `"description"` field.)
6. **Serializer dependency on net48:** System.Text.Json (package) vs Newtonsoft.Json for
   `FwAvalonia` — pick one and add it to the `EngineIsolationAuditTests` allowed-reference
   set deliberately.
7. **Who owns the JSON Schema versioning policy** (when `formatVersion` bumps, who writes the
   patch migrator) — proposed: same owner as `LayoutVersionNumber` bumps today.
