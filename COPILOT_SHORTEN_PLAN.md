# COPILOT.md Shortening Plan

## Goals
1. Reduce duplication across COPILOT.md files
2. Keep organizational folders under 100 lines
3. Keep leaf folders under 200 lines
4. Follow the organizational template for parent folders
5. Remove verbose/redundant sections that add no unique value

## Analysis of Current State

### Organizational Folders (Should use template, <100 lines)
These folders group subfolders and don't contain code directly:
- `Src/Common/` (120 lines) - Parent of 10 subfolders
- `Src/Common/Controls/` (organizational but may have code)
- `Src/LexText/` (230 lines) - Parent of 11 subfolders
- `Src/Utilities/` (245 lines) - Parent of 7 subfolders
- `Src/FXT/` - Need to check
- `Src/Transforms/` (311 lines) - Contains XSLT files, may be leaf
- `Src/DocConvert/` - Need to check

### Longest Files Needing Reduction (>200 lines)
1. `Src/LexText/ParserUI/COPILOT.md` (353 lines)
2. `Src/LexText/ParserCore/COPILOT.md` (342 lines)
3. `Src/Transforms/COPILOT.md` (311 lines)
4. `Src/UnicodeCharEditor/COPILOT.md` (310 lines)
5. `Src/ParatextImport/COPILOT.md` (307 lines)
6. `Src/MigrateSqlDbs/COPILOT.md` (293 lines)
7. `Src/Paratext8Plugin/COPILOT.md` (281 lines)
8. `Src/ProjectUnpacker/COPILOT.md` (258 lines)
9. `Src/Utilities/COPILOT.md` (245 lines)
10. `Src/ManagedLgIcuCollator/COPILOT.md` (242 lines)

## Duplication Patterns Found

### Verbose Sections to Condense/Remove
1. **Redundant subsections** - Many files have both "Upstream" and "Downstream" dependencies with obvious info
2. **Build Information** - Often states the obvious ("build via FieldWorks.sln")
3. **Threading & Performance** - Boilerplate like "Single-threaded" or "Thread-agnostic"
4. **Config & Feature Flags** - Often "No configuration files" which adds no value
5. **Interop & Contracts** - Often just says "No COM boundaries" or repeats obvious COM info
6. **Entry Points** - Often duplicates what's in Key Components
7. **Test Index** - Often just lists test project names already in Key Components
8. **Usage Hints** - Often restates what's obvious from Purpose/Key Components
9. **Related Folders** - Often duplicates Dependencies section
10. **References (auto-generated hints)** - Duplicates earlier file listings

## Strategy

### For Organizational Folders (7 folders)
- Replace with organizational-copilot.template.md structure
- Keep only: Purpose (2-3 sentences), Subfolder Map (table), Related Guidance (links)
- Target: <100 lines total

### For Leaf Folders with Code (54 folders)
- Keep essential sections: Purpose, Key Components, Dependencies
- Remove/condense:
  - Threading & Performance (unless non-trivial)
  - Config & Feature Flags (unless meaningful config exists)
  - Build Information (unless special build requirements)
  - Interop & Contracts (unless significant interop)
  - Test Index (merge into Key Components or remove if obvious)
  - Usage Hints (unless non-obvious patterns)
  - Related Folders (if duplicates Dependencies)
  - References auto-generated section (remove - duplicates earlier content)
  - Entry Points (if duplicates Key Components)
- Target: 150-200 lines for complex components, 100-150 for simpler ones

## Execution Order

### Phase 1: Organizational Folders (7 files)
1. Src/Common/COPILOT.md
2. Src/LexText/COPILOT.md
3. Src/Utilities/COPILOT.md
4. Src/Common/Controls/COPILOT.md (check if truly organizational)
5. Src/FXT/COPILOT.md (check if truly organizational)
6. Src/Transforms/COPILOT.md (check if truly organizational)
7. Src/DocConvert/COPILOT.md (check if truly organizational)

### Phase 2: Longest Leaf Folders (10 files)
Priority order based on line count:
1. Src/LexText/ParserUI/COPILOT.md (353 → target ~200)
2. Src/LexText/ParserCore/COPILOT.md (342 → target ~200)
3. Src/UnicodeCharEditor/COPILOT.md (310 → target ~200)
4. Src/ParatextImport/COPILOT.md (307 → target ~200)
5. Src/MigrateSqlDbs/COPILOT.md (293 → target ~200)
6. Src/Paratext8Plugin/COPILOT.md (281 → target ~200)
7. Src/ProjectUnpacker/COPILOT.md (258 → target ~200)
8. Src/ManagedLgIcuCollator/COPILOT.md (242 → target ~180)
9. Src/xWorks/COPILOT.md (241 → target ~180)
10. Src/Common/Filters/COPILOT.md (238 → target ~180)

### Phase 3: Remaining Files (44 files)
Process systematically, removing duplication and condensing.

## Validation Strategy
- After each file: Run `python .github/check_copilot_docs.py --paths <file>`
- After each phase: Run full validation
- Final: Run `python .github/check_copilot_docs.py --only-changed --fail`

## Success Metrics
- Organizational folders: All <100 lines ✓
- Leaf folders: 90% under 200 lines ✓
- Zero validation errors ✓
- No loss of essential technical information ✓

## Learnings from Implementation

### Schema Requirements
The COPILOT.md validator requires these sections to be present (even if short):
- Purpose
- Architecture
- Key Components
- Technology Stack
- Dependencies
- Interop & Contracts
- Threading & Performance
- Config & Feature Flags
- Build Information
- Interfaces and Data Models
- Entry Points
- Test Index
- Usage Hints
- Related Folders
- References

### Shortening Strategy for Leaf Folders
Instead of removing sections, condense them:
- **Threading & Performance**: "UI thread required" or "Single-threaded" (1 line)
- **Config & Feature Flags**: "No configuration" or list key settings (1-3 lines)
- **Build Information**: Just project name and output (1-2 lines)
- **Entry Points**: List main entry points only (2-5 lines)
- **Test Index**: Just test project name (1 line)
- **Usage Hints**: 2-3 key patterns only
- **Related Folders**: 2-3 most important only
- **References**: Can use "See planner JSON" to avoid file lists
- **Auto-Generated sections**: Remove entirely (duplicates earlier content)

### Phase 1 Results (Organizational Folders)
Successfully reduced 4 organizational parent folders by 75%:
- Src/Common: 117 → 45 lines
- Src/LexText: 230 → 45 lines
- Src/Utilities: 245 → 42 lines
- Src/Common/Controls: 101 → 43 lines

Total savings: 445 lines removed (620 → 175 lines)

## Complete Checklist of All COPILOT.md Files (61 total)

### Organizational Parent Folders (4 files) - ALL COMPLETE ✓
- [x] Src/Common/COPILOT.md (117 → 45 lines, 62% reduction)
- [x] Src/Common/Controls/COPILOT.md (101 → 43 lines, 57% reduction)
- [x] Src/LexText/COPILOT.md (230 → 45 lines, 80% reduction)
- [x] Src/Utilities/COPILOT.md (245 → 42 lines, 83% reduction)

### Leaf Folders (57 files) - ALL COMPLETE ✓

#### AppCore & Infrastructure (3 files)
- [x] Src/AppCore/COPILOT.md (221 → 125 lines, 43% reduction)
- [x] Src/CacheLight/COPILOT.md (183 → 140 lines, 23% reduction)
- [x] Src/Cellar/COPILOT.md

#### Common/ Subfolders (10 files)
- [x] Src/Common/FieldWorks/COPILOT.md (233 → 142 lines, 39% reduction)
- [x] Src/Common/Filters/COPILOT.md (238 → 138 lines, 42% reduction)
- [x] Src/Common/Framework/COPILOT.md (207 → 123 lines, 41% reduction)
- [x] Src/Common/FwUtils/COPILOT.md
- [x] Src/Common/RootSite/COPILOT.md (145 → 98 lines, 32% reduction)
- [x] Src/Common/ScriptureUtils/COPILOT.md (172 → 115 lines, 33% reduction)
- [x] Src/Common/SimpleRootSite/COPILOT.md (202 → 154 lines, 24% reduction)
- [x] Src/Common/UIAdapterInterfaces/COPILOT.md (148 → 96 lines, 35% reduction)
- [x] Src/Common/ViewsInterfaces/COPILOT.md (172 → 133 lines, 23% reduction)

#### Core Components (8 files)
- [x] Src/DbExtend/COPILOT.md
- [x] Src/DebugProcs/COPILOT.md (170 → 90 lines, 47% reduction)
- [x] Src/DocConvert/COPILOT.md
- [x] Src/FXT/COPILOT.md (167 → 98 lines, 41% reduction)
- [x] Src/Generic/COPILOT.md (163 → 107 lines, 34% reduction)
- [x] Src/Kernel/COPILOT.md
- [x] Src/Transforms/COPILOT.md (311 → 151 lines, 51% reduction)
- [x] Src/views/COPILOT.md

#### FDO & UI Components (5 files)
- [x] Src/FdoUi/COPILOT.md (169 → 102 lines, 40% reduction)
- [x] Src/FwCoreDlgs/COPILOT.md (153 → 98 lines, 36% reduction)
- [x] Src/FwParatextLexiconPlugin/COPILOT.md (170 → 115 lines, 32% reduction)
- [x] Src/FwResources/COPILOT.md (151 → 104 lines, 31% reduction)
- [x] Src/GenerateHCConfig/COPILOT.md (145 → 95 lines, 34% reduction)

#### Installation & Browser (2 files)
- [x] Src/InstallValidator/COPILOT.md
- [x] Src/LCMBrowser/COPILOT.md (174 → 124 lines, 29% reduction)

#### LexText/ Subfolders (8 files)
- [x] Src/LexText/Discourse/COPILOT.md (203 → 152 lines, 25% reduction)
- [x] Src/LexText/FlexPathwayPlugin/COPILOT.md
- [x] Src/LexText/Interlinear/COPILOT.md (202 → 144 lines, 29% reduction)
- [x] Src/LexText/LexTextControls/COPILOT.md (206 → 145 lines, 30% reduction)
- [x] Src/LexText/LexTextDll/COPILOT.md (162 → 102 lines, 37% reduction)
- [x] Src/LexText/Lexicon/COPILOT.md (179 → 137 lines, 23% reduction)
- [x] Src/LexText/Morphology/COPILOT.md (178 → 134 lines, 25% reduction)

#### Parser Components (2 files)
- [x] Src/LexText/ParserCore/COPILOT.md (342 → 220 lines, 36% reduction)
- [x] Src/LexText/ParserUI/COPILOT.md (353 → 144 lines, 59% reduction)

#### Managed Wrappers (3 files)
- [x] Src/ManagedLgIcuCollator/COPILOT.md (242 → 123 lines, 49% reduction)
- [x] Src/ManagedVwDrawRootBuffered/COPILOT.md (225 → 97 lines, 57% reduction)
- [x] Src/ManagedVwWindow/COPILOT.md (210 → 93 lines, 56% reduction)

#### Migration & Import (3 files)
- [x] Src/MigrateSqlDbs/COPILOT.md (293 → 157 lines, 46% reduction)
- [x] Src/Paratext8Plugin/COPILOT.md (281 → 167 lines, 41% reduction)
- [x] Src/ParatextImport/COPILOT.md (307 → 178 lines, 42% reduction)

#### Utilities (2 files)
- [x] Src/ProjectUnpacker/COPILOT.md (258 → 104 lines, 60% reduction)
- [x] Src/UnicodeCharEditor/COPILOT.md (310 → 182 lines, 41% reduction)

#### Utilities/ Subfolders (6 files)
- [x] Src/Utilities/FixFwData/COPILOT.md (192 → 95 lines, 51% reduction)
- [x] Src/Utilities/FixFwDataDll/COPILOT.md (236 → 164 lines, 31% reduction)
- [x] Src/Utilities/MessageBoxExLib/COPILOT.md (193 → 108 lines, 44% reduction)
- [x] Src/Utilities/Reporting/COPILOT.md
- [x] Src/Utilities/SfmStats/COPILOT.md
- [x] Src/Utilities/SfmToXml/COPILOT.md (218 → 80 lines, 63% reduction)
- [x] Src/Utilities/XMLUtils/COPILOT.md

#### XCore Components (5 files)
- [x] Src/XCore/COPILOT.md
- [x] Src/XCore/FlexUIAdapter/COPILOT.md
- [x] Src/XCore/SilSidePane/COPILOT.md
- [x] Src/XCore/xCoreInterfaces/COPILOT.md
- [x] Src/XCore/xCoreTests/COPILOT.md

#### xWorks (1 file)
- [x] Src/xWorks/COPILOT.md (241 → 126 lines, 48% reduction)

### Summary
- **Total files**: 61/61 (100% complete)
- **Organizational folders**: 4/4 complete (72% avg reduction)
- **Leaf folders**: 57/57 complete (various reductions, avg ~40-45%)
- **Estimated total lines removed**: ~4,500+ lines across all files
- **All files validated**: 0 failures

### Completion Status
✅ **ALL 61 FILES COMPLETE** - All COPILOT.md files have been systematically reviewed, condensed, and validated per the strategy documented above.

---

## Phase 5: Post-9.3 Code Changes Review

Analysis of git commits between current branch and release/9.3 identified 30 folders with code changes that may need COPILOT.md updates to reflect new functionality, architecture changes, or added components.

### Folders Prioritized by Change Volume (Needs Review)

**High Priority** - Substantial changes (>30 files changed):
- [ ] Src/Common/ (141 files changed) - Extensive updates across multiple subfolders
- [ ] Src/LexText/ (100 files changed) - Major changes requiring detailed review
- [ ] Src/xWorks/ (40 files changed) - Significant application-level changes
- [ ] Src/FwCoreDlgs/ (39 files changed) - Core dialog changes
- [ ] Src/Utilities/ (32 files changed) - Multiple utility subfolder changes

**Medium Priority** - Moderate changes (10-30 files):
- [ ] Src/XCore/ (25 files changed) - Framework changes
- [ ] Src/ParatextImport/ (20 files changed) - Import pipeline updates
- [ ] Src/FXT/ (11 files changed) - Transform tool changes

**Lower Priority** - Focused changes (4-9 files):
- [ ] Src/FwParatextLexiconPlugin/ (8 files changed)
- [ ] Src/views/ (7 files changed)
- [ ] Src/CacheLight/ (6 files changed)
- [ ] Src/UnicodeCharEditor/ (5 files changed)
- [ ] Src/ManagedVwWindow/ (5 files changed)
- [ ] Src/Paratext8Plugin/ (4 files changed)
- [ ] Src/ManagedLgIcuCollator/ (4 files changed)
- [ ] Src/InstallValidator/ (4 files changed)
- [ ] Src/Generic/ (4 files changed)
- [ ] Src/FdoUi/ (4 files changed)

**Minimal Priority** - Small focused changes (1-3 files):
- [ ] Src/ManagedVwDrawRootBuffered/ (3 files changed)
- [ ] Src/LCMBrowser/ (3 files changed)
- [ ] Src/GenerateHCConfig/ (3 files changed)
- [ ] Src/ProjectUnpacker/ (2 files changed)
- [ ] Src/MigrateSqlDbs/ (2 files changed)
- [ ] Src/FwResources/ (2 files changed)
- [ ] Src/Kernel/ (1 file changed)
- [ ] Src/DebugProcs/ (1 file changed)

### Review Process for Each Folder
1. Run detection script: `python .github/detect_copilot_needed.py --strict --folders Src/<Folder>`
2. If impacted, plan updates: `python .github/plan_copilot_updates.py --folders Src/<Folder> --out .cache/copilot/<folder>-plan.json`
3. Review the plan and git log to understand code changes
4. Apply automated updates: `python .github/copilot_apply_updates.py --plan .cache/copilot/<folder>-plan.json`
5. Manually update narrative sections (Purpose, Key Components, Architecture) to reflect new code
6. Validate: `python .github/check_copilot_docs.py --paths Src/<Folder>/COPILOT.md --fail`

### Notes
- Focus on High Priority folders first (Common, LexText, xWorks, FwCoreDlgs, Utilities)
- Many folders may already have accurate COPILOT.md despite code changes (e.g., bug fixes, refactoring)
- Use git log to determine if changes are substantive enough to warrant narrative updates
- The condensing phase (Phases 1-4) is complete; this phase focuses on accuracy/currency of content
