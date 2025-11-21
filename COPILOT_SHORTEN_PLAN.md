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
