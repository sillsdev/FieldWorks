# OpenSpec Implementation Plan: Federated Specification System

> **Status:** Draft
> **Created:** 2026-01-30
> **Scope:** Establish bidirectional cross-referencing between architectural specs and folder-level AGENTS.md files

---

## 1. Overview

This plan implements a **federated documentation system** where:

- **Spec files** (`openspec/specs/`) contain **general architectural concerns** that span multiple components
- **AGENTS.md files** (67 existing files in `Src/`, `FLExInstaller/`, etc.) contain **folder-specific implementation details**
- **References** link specs to AGENTS.md sections (forward refs)
- **Back-references** link AGENTS.md sections back to the specs that cite them (reverse refs)
- **Validation scripts** ensure all references are bidirectional and resolve correctly

---

## 2. Reference Format Specification

### 2.1 Forward References (Spec → AGENTS.md)

Each section in a spec file that references AGENTS.md content must include a `### References` subsection at the end of that section.

```markdown
## COM Registration Patterns

FieldWorks uses registration-free COM to avoid global registry pollution...

[General architectural discussion here]

### References

- [Kernel COM implementation](../../Src/Kernel/AGENTS.md#interop--contracts) — Native COM server patterns
- [FwCoreDlgs COM usage](../../Src/FwCoreDlgs/AGENTS.md#interop--contracts) — Managed COM client patterns
- [Views rendering contracts](../../Src/Views/AGENTS.md#interop--contracts) — VwRootSite COM interfaces
```

### 2.2 Back-References (AGENTS.md → Spec)

Each section in an AGENTS.md file that is referenced by a spec must include a `### Referenced By` subsection at the end of that section.

```markdown
## Interop & Contracts

This component exposes COM interfaces for native rendering...

[Folder-specific details here]

### Referenced By

- [COM Registration Patterns](../../../openspec/specs/architecture/interop/com-contracts.md#com-registration-patterns) — General COM architecture
- [Native Boundary Patterns](../../../openspec/specs/architecture/interop/native-boundary.md#marshaling) — Cross-boundary marshaling rules
```

### 2.3 Anchor Naming Convention

Markdown heading anchors must follow these rules:

| Rule | Example Heading | Generated Anchor |
|------|-----------------|------------------|
| Lowercase | `## Architecture` | `#architecture` |
| Spaces → hyphens | `## Key Components` | `#key-components` |
| Special chars → hyphens | `## Threading & Performance` | `#threading--performance` |
| Consecutive hyphens collapsed | `## Interop & Contracts` | `#interop--contracts` |

> **Note:** GitHub-flavored markdown collapses `&` to `--`. Scripts must normalize accordingly.

### 2.4 YAML Frontmatter Extensions

AGENTS.md files gain an optional `anchors:` block for explicit anchor declaration:

```yaml
---
last-reviewed: 2025-10-31
status: reviewed
anchors:
  - architecture
  - key-components
  - interop--contracts
  - threading--performance
---
```

This allows scripts to detect anchor drift when headings are renamed.

---

## 3. Separation of Concerns

### 3.1 Decision Criteria

| If the content is... | It belongs in... |
|----------------------|------------------|
| A pattern used by 3+ components | Spec file |
| A constraint that affects architectural decisions | Spec file |
| An implementation detail of one component | AGENTS.md |
| A dependency or build quirk of one project | AGENTS.md |
| A cross-cutting concern (threading, COM, DI) | Spec file |
| How a component applies a cross-cutting concern | AGENTS.md (with back-ref to spec) |

### 3.2 Examples

| Content | Location | Reasoning |
|---------|----------|-----------|
| "FieldWorks uses STA threading for COM" | Spec: `threading-model.md` | Applies to all COM-using components |
| "Views.dll creates its own STA thread pool" | `Src/Views/AGENTS.md` | Implementation-specific |
| "XCore mediator routes commands via Colleague pattern" | Spec: `xcore-mediator.md` | Framework pattern |
| "LexText registers these specific command handlers" | `Src/LexText/AGENTS.md` | Application-specific |

### 3.3 Content Migration Workflow

When content should move from AGENTS.md to spec (or vice versa):

1. Create the content in the target location
2. Add forward/back references
3. In the original location, replace content with a reference
4. Run validation scripts
5. Commit atomically

---

## 4. Spec File Structure

### 4.1 Design Philosophy

The spec directory has two major divisions:

1. **Capability folders** (user-story focused) — What FieldWorks does for users
2. **Architecture folder** (implementation focused) — How FieldWorks is built

Capability specs answer: "What can a user accomplish?"
Architecture specs answer: "How does the system achieve it?"

### 4.2 Directory Layout

```
openspec/specs/
├── README.md                        # Index with status dashboard
│
├── architecture/                    # HOW the system is built
│   ├── README.md                    # Architecture index
│   ├── layers/
│   │   ├── layer-model.md           # UI → Logic → LCM → Persistence
│   │   ├── dependency-graph.md      # Project reference rules
│   │   └── entry-points.md          # Application startup, DI patterns
│   ├── interop/
│   │   ├── com-contracts.md         # RegFree COM, registration patterns
│   │   ├── native-boundary.md       # C++/CLI marshaling patterns
│   │   └── external-apis.md         # Paratext, ICU, external dependencies
│   ├── ui-framework/
│   │   ├── winforms-patterns.md     # WinForms architecture patterns
│   │   ├── xcore-mediator.md        # Command/message routing
│   │   └── views-rendering.md       # VwRootSite, native rendering
│   ├── data-access/
│   │   ├── lcm-patterns.md          # LCModel usage constraints
│   │   └── undo-redo.md             # Unit-of-work, action handler patterns
│   ├── build-deploy/
│   │   ├── build-phases.md          # Traversal SDK, native-first ordering
│   │   ├── installer.md             # WiX 3.14 packaging
│   │   └── localization.md          # Crowdin, resx conventions
│   └── testing/
│       ├── test-strategy.md         # Unit vs integration vs memory tests
│       └── fixtures.md              # Test data, LCM mocking
│
├── configuration/                   # System-wide settings
│   ├── writing-systems.md           # Scripts, keyboards, fonts, ICU sorting
│   ├── lists.md                     # Custom lists, localization categories
│   └── projects.md                  # Project settings, backup, restore
│
├── lexicon/                         # Dictionary management
│   ├── entries/
│   │   ├── structure.md             # Entry, sense, variant hierarchy
│   │   ├── creation.md              # Rapid entry, semantic domains
│   │   └── relations.md             # Lexical relations
│   ├── import/
│   │   ├── sfm.md                   # SFM/MDF import
│   │   └── lift.md                  # LIFT import
│   └── export/
│       ├── dictionary.md            # Print/web dictionary export
│       ├── lift.md                  # LIFT export
│       └── pathway.md               # Pathway publishing
│
├── grammar/                         # Grammatical analysis
│   ├── morphology/
│   │   ├── categories.md            # POS, inflection classes
│   │   ├── affixes.md               # Affixes, clitics, templates
│   │   └── allomorphs.md            # Allomorph conditioning
│   ├── parsing/
│   │   ├── configuration.md         # Parser setup, HC/XAmple
│   │   ├── rules.md                 # Parsing rules and constraints
│   │   └── troubleshooting.md       # Parser debugging
│   └── sketch/
│       └── generation.md            # Grammar sketch output
│
├── texts/                           # Text corpus management
│   ├── interlinear/
│   │   ├── annotation.md            # Word glossing workflow
│   │   ├── baseline.md              # Original text handling
│   │   └── import.md                # Interlinear text import
│   ├── analysis/
│   │   ├── concordance.md           # Occurrence views
│   │   ├── discourse.md             # Discourse analysis
│   │   └── tagging.md               # Genres, metadata
│   └── export/
│       └── formats.md               # Text export options
│
└── integration/                     # External connections
    ├── collaboration/
    │   ├── send-receive.md          # Chorus sync
    │   └── multi-user.md            # Multi-user workflows
    └── external/
        ├── paratext.md              # Scripture integration
        ├── flextools.md             # Python scripting
        └── encoding.md              # TECkit, encoding converters
```

### 4.3 Spec File Template

```markdown
---
spec-id: lexicon/entries/structure
created: 2026-01-30
status: draft
---

# Entry Structure

## Purpose

[One paragraph explaining what this spec covers and why it matters]

## User Stories

- As a lexicographer, I want to create a new dictionary entry so that I can document a word's meaning
- As a linguist, I want to add variant forms so that dialectal differences are captured

## Context

[Background, history, constraints that led to current patterns]

## Behavior

### Creating an Entry

[Description of the behavior]

#### References

- [LexText Entry Creation](path/to/AGENTS.md#section) — Implementation details

## Constraints

[Rules that must be followed]

## Anti-patterns

[What NOT to do, with explanations]

## Open Questions

[Unresolved decisions, tracked for future specs]
```

---

## 5. Validation Scripts

### 5.1 Script Inventory

| Script | Language | Purpose |
|--------|----------|---------|
| `Validate-OpenSpecRefs.ps1` | PowerShell | Verify all refs resolve, back-refs exist, no orphans |
| `Propose-RefFixes.ps1` | PowerShell | Generate PR-ready diffs for broken/missing refs |
| `Sync-AgentsAnchors.ps1` | PowerShell | Update `anchors:` frontmatter from actual headings |
| `Report-RefCoverage.ps1` | PowerShell | Dashboard showing which AGENTS.md are covered by specs |

### 5.2 Validate-OpenSpecRefs.ps1

**Location:** `scripts/openspec/Validate-OpenSpecRefs.ps1`

**Behavior:**

1. Scan all `openspec/specs/**/*.md` for markdown links matching `*/AGENTS.md#*`
2. For each link:
   - Verify the target file exists
   - Verify the anchor exists in the target file
   - Verify the target file has a corresponding `### Referenced By` entry pointing back
3. Scan all `**/AGENTS.md` for `### Referenced By` sections
4. For each back-ref:
   - Verify the target spec file exists
   - Verify the anchor exists in the spec file
   - Verify the spec file has a corresponding `### References` entry
5. Report:
   - Broken forward refs (file missing, anchor missing)
   - Broken back-refs (file missing, anchor missing)
   - Orphaned refs (forward without back, or vice versa)
   - Circular ref chains (warning, not error)

**Exit codes:**
- `0` — All refs valid
- `1` — Broken refs found
- `2` — Orphaned refs found (missing bidirectional pair)

**Example output:**

```
Validating OpenSpec cross-references...

Scanning 15 spec files...
Scanning 67 AGENTS.md files...

ERRORS:
  [BROKEN] openspec/specs/000-baseline/interop/com-contracts.md:45
           -> Src/Kernel/AGENTS.md#interop-contracts
           Anchor not found. Did you mean #interop--contracts?

  [ORPHAN] Src/Views/AGENTS.md:89 #architecture
           Has back-ref to openspec/specs/000-baseline/ui-framework/views-rendering.md
           But that spec has no forward ref to this section.

WARNINGS:
  [CYCLE] com-contracts.md -> Kernel/AGENTS.md -> native-boundary.md -> Kernel/AGENTS.md
          Circular reference chain detected (not necessarily wrong).

Summary: 2 errors, 1 warning
```

### 5.3 Propose-RefFixes.ps1

**Location:** `scripts/openspec/Propose-RefFixes.ps1`

**Behavior:**

1. Run validation to find issues
2. For each issue type, generate a fix:

| Issue | Proposed Fix |
|-------|--------------|
| Anchor typo | Suggest correct anchor based on fuzzy match |
| Missing back-ref | Generate `### Referenced By` block to insert |
| Missing forward-ref | Generate `### References` block to insert |
| Orphaned back-ref | Suggest deletion or spec creation |

3. Output as:
   - Console summary
   - Optional: `--apply` flag to write changes
   - Optional: `--pr` flag to create branch and PR

**Example output:**

```
Proposed fixes for 2 issues:

FIX 1: Anchor correction
  File: openspec/specs/000-baseline/interop/com-contracts.md
  Line: 45
  Old:  [Kernel COM](../../Src/Kernel/AGENTS.md#interop-contracts)
  New:  [Kernel COM](../../Src/Kernel/AGENTS.md#interop--contracts)

FIX 2: Add missing back-ref
  File: Src/Views/AGENTS.md
  After line: 92 (end of ## Architecture section)
  Insert:
    ### Referenced By

    - [Views Rendering Patterns](../../../openspec/specs/000-baseline/ui-framework/views-rendering.md#architecture) — System-wide rendering architecture

Run with --apply to make changes, or --pr to create a pull request.
```

### 5.4 Sync-AgentsAnchors.ps1

**Location:** `scripts/openspec/Sync-AgentsAnchors.ps1`

**Behavior:**

1. Parse each AGENTS.md file
2. Extract all `##` and `###` headings
3. Generate anchor slugs per GitHub rules
4. Compare against `anchors:` in YAML frontmatter
5. Update frontmatter if mismatched

**Options:**
- `--check` — Report mismatches only (for CI)
- `--fix` — Update frontmatter in place
- `--init` — Add `anchors:` block to files that lack it

### 5.5 Report-RefCoverage.ps1

**Location:** `scripts/openspec/Report-RefCoverage.ps1`

**Behavior:**

Generate a coverage report showing which AGENTS.md files are referenced by specs:

```
OpenSpec Reference Coverage Report
==================================

Fully covered (has refs + back-refs):
  ✓ Src/Kernel/AGENTS.md (3 sections referenced)
  ✓ Src/Views/AGENTS.md (2 sections referenced)
  ✓ Src/XCore/xCoreInterfaces/AGENTS.md (1 section referenced)

Partially covered:
  ~ Src/Common/Framework/AGENTS.md (1 of 5 sections referenced)
  ~ Src/LexText/LexTextControls/AGENTS.md (2 of 7 sections referenced)

Not covered:
  ○ Src/Utilities/BasicUtils/AGENTS.md
  ○ Src/Utilities/XMLUtils/AGENTS.md
  ... (15 more)

Summary: 5 covered, 10 partial, 52 uncovered
```

---

## 6. CI Integration

### 6.1 Pull Request Validation

Add to `.github/workflows/` or existing CI:

```yaml
- name: Validate OpenSpec cross-references
  run: |
    ./scripts/openspec/Validate-OpenSpecRefs.ps1
  if: |
    contains(github.event.pull_request.changed_files, 'openspec/') ||
    contains(github.event.pull_request.changed_files, 'AGENTS.md')
```

**Behavior:**
- Runs when any spec or AGENTS.md file changes
- Blocks merge on broken refs
- Warns on orphaned refs (doesn't block)

### 6.2 Nightly Auto-fix PR

```yaml
schedule:
  - cron: '0 6 * * *'  # 6 AM UTC daily

jobs:
  fix-refs:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Propose fixes
        run: ./scripts/openspec/Propose-RefFixes.ps1 --pr
```

**Behavior:**
- Creates a PR if any fixes are needed
- Auto-assigns to documentation maintainers

---

## 7. Implementation Phases

### Phase 1: Foundation (Week 1)

- [ ] Create `scripts/openspec/` directory
- [ ] Implement `Validate-OpenSpecRefs.ps1` (read-only validation)
- [ ] Implement `Sync-AgentsAnchors.ps1 --check` (anchor inventory)
- [ ] Run anchor sync on all 67 AGENTS.md files to establish baseline
- [ ] Document reference format in `openspec/AGENTS.md`

### Phase 2: First Specs (Week 2)

- [ ] Create `openspec/specs/README.md` (master index)
- [ ] Create `openspec/specs/architecture/README.md` (architecture index)
- [ ] Create first architecture spec: `architecture/interop/com-contracts.md`
  - Identify 3-5 AGENTS.md files with COM content
  - Extract general patterns into spec
  - Add forward references
  - Add back-references to AGENTS.md files
- [ ] Create first capability spec: `lexicon/entries/structure.md` (using Option C)
  - Link to `Src/LexText/Lexicon/AGENTS.md`
  - Add user stories
- [ ] Validate with scripts

### Phase 3: Tooling Completion (Week 3)

- [ ] Implement `Propose-RefFixes.ps1`
- [ ] Implement `Report-RefCoverage.ps1`
- [ ] Add CI workflow for PR validation
- [ ] Test with intentionally broken refs

### Phase 4: Spec Expansion (Week 4+)

- [ ] Create remaining architecture specs per directory layout
- [ ] Create capability specs for each chosen grouping (Option A, B, or C)
- [ ] Prioritize by AGENTS.md files with most content
- [ ] Track coverage via `Report-RefCoverage.ps1`
- [ ] Decide final capability folder structure based on experience

### Phase 5: Automation (Week 6+)

- [ ] Nightly auto-fix PR workflow
- [ ] Integration with `detect_copilot_needed.py` (flag AGENTS.md changes that may need spec updates)

---

## 8. Heading Standardization

### 8.1 Current State

The 67 AGENTS.md files use mostly consistent headings but with variations:

| Standard Heading | Variations Found |
|------------------|------------------|
| `## Architecture` | `## Architecture Overview`, `## Design` |
| `## Dependencies` | `## Upstream/Downstream Dependencies` |
| `## Interop & Contracts` | `## Interop`, `## COM Contracts` |
| `## Threading & Performance` | `## Threading`, `## Performance Notes` |

### 8.2 Normalization Approach

**Option A: Strict normalization (one-time cleanup)**
- Run a script to rename all headings to exact standards
- Pros: Clean anchors, predictable refs
- Cons: Large diff, may break existing links

**Option B: Alias mapping (tooling tolerance)**
- Scripts recognize multiple anchor variants as equivalent
- Pros: No disruptive changes
- Cons: Complexity in tooling, eventual drift

**Recommendation:** Option A with a phased rollout:
1. Add `anchors:` frontmatter first (Phase 1)
2. Normalize headings in a dedicated PR
3. Update all refs atomically

---

## 9. Risk Mitigations

| Risk | Mitigation |
|------|------------|
| Anchor drift (headings renamed) | `anchors:` frontmatter + CI check |
| Back-ref sprawl (popular sections) | Group by spec category in `### Referenced By` |
| Developers forget back-refs | `Propose-RefFixes.ps1` catches in nightly PR |
| Circular refs cause confusion | Scripts warn but don't error; document intent |
| Stale back-refs after spec deletion | Bidirectional validation catches orphans |
| Large initial effort | Phased rollout; start with 1 spec, expand |

---

## 10. Success Criteria

| Metric | Target |
|--------|--------|
| Architecture specs created | 12 files covering 6 concern areas |
| Capability specs created | 8-12 files covering core user workflows |
| AGENTS.md coverage | 30+ files have at least one ref |
| Validation pass rate | 100% on main branch |
| Time to detect broken ref | < 30 min (PR CI check) |
| Auto-fix adoption | 80% of fixes accepted without modification |
| Capability folder decision | Finalized by end of Phase 4 |

---

## 11. Open Questions

1. **Should back-refs be sorted?** Alphabetically, by date added, or by importance?

2. **How to handle external refs?** Some AGENTS.md reference SIL.LCModel docs that aren't in this repo.

3. **Version pinning?** Should refs include commit SHA or branch for stability?

4. **Tooling for IDE?** VS Code extension to navigate refs, preview back-refs?

---

## Appendix A: Reference Syntax Quick Reference

### Forward Reference (in spec file)

```markdown
## Section Title

[Content...]

### References

- [Description](relative/path/to/AGENTS.md#anchor-name) — Brief context
```

### Back-Reference (in AGENTS.md)

```markdown
## Section Title

[Content...]

### Referenced By

- [Spec Section Title](relative/path/to/spec.md#anchor-name) — Brief context
```

### Anchor Slug Rules

```
"## Threading & Performance" → #threading--performance
"## Key Components"          → #key-components
"## Architecture"            → #architecture
```

---

## Appendix B: File Checklist for Phase 2

### Architecture Spec: `architecture/interop/com-contracts.md`

Should reference:

- [ ] `Src/Kernel/AGENTS.md#interop--contracts`
- [ ] `Src/FwCoreDlgs/AGENTS.md#interop--contracts`
- [ ] `Src/Views/AGENTS.md#interop--contracts`
- [ ] `Src/Generic/AGENTS.md#interop--contracts`
- [ ] `Src/Common/RootSite/AGENTS.md#interop--contracts`

Each of those files needs `### Referenced By` added to their `## Interop & Contracts` section.

### Capability Spec: `lexicon/entries/structure.md`

Should reference:

- [ ] `Src/LexText/Lexicon/AGENTS.md#architecture`
- [ ] `Src/LexText/LexTextControls/AGENTS.md#key-components`
- [ ] `Src/Common/FwUtils/AGENTS.md#purpose` (if shared utilities used)

Each of those files needs `### Referenced By` added to their respective sections.


