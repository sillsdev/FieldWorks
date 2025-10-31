# Copilot task: Update COPILOT.md for changed folders (detect → propose → validate)

Purpose: Run a reliable 3-step flow to ensure COPILOT.md files are updated whenever code/config changes in `Src/**` are made.

Context: FieldWorks repository. Scripts referenced below exist under `.github/` and are documented in `.github/update-copilot-summaries.md`.

Inputs:
- base_ref: optional git ref to diff against (default behavior compares to the repo default via origin/HEAD)
- status: draft|verified (default: draft)

Success criteria:
- All impacted `Src/<Folder>` with code/config changes either updated their `COPILOT.md` in the same diff, or the proposer script generated updates.
- `check_copilot_docs.py --fail` passes (frontmatter, headings, references best-effort mapping).

Steps:
1) Detect impacted folders
   - Run: `python .github/detect_copilot_needed.py --strict` (or pass `--base origin/<branch>` explicitly)
   - Collect the set of folders reported as missing `COPILOT.md` updates.

2) Propose/prepare updates for those folders
   - Run: `python .github/scaffold_copilot_markdown.py --status <status>` (or pass `--base origin/<branch>` explicitly)
   - This ensures frontmatter and all required headings exist and appends an "References (auto-generated hints)" section.
   - Do not remove human-written content; only append/fix structure.

3) Follow the instructions in update-copilot-summairies.md for the files identified as needing updates.  Focus on the descriptions for accuracy, ALWAYS reading through all the relevant source files.

4) Validate documentation integrity
   - Run: `python .github/check_copilot_docs.py --only-changed --fail --verbose`
   - If failures occur, iterate step 2 or manually edit `Src/<Folder>/COPILOT.md` to address missing headings, placeholders, or reference issues. Re-run until green.

5) Commit and summarize
   - Include a concise summary of impacted folders and changes.
   - Example message: `docs(copilot): update COPILOT.md for <FolderA>, <FolderB>; ensure frontmatter & skeleton; add auto refs`

Notes:
- VS Code tasks are available for convenience:
   - "COPILOT: Detect updates needed"
   - "COPILOT: Propose updates for changed folders"
   - "COPILOT: Validate COPILOT docs (changed only)"
   - "COPILOT: Update flow (detect → propose → validate)"
- In CI, `.github/workflows/copilot-docs-detect.yml` runs the detector and (optionally) the validator in advisory mode.
