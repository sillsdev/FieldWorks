# Updating COPILOT.md summaries

Each folder under `Src/` has a `COPILOT.md` file that documents its purpose, components, public interfaces, and data models. These files are essential for understanding the codebase and for keeping AI guidance accurate and actionable.

## Frontmatter (required)
Add minimal frontmatter to each `COPILOT.md` so review hygiene are clear:

```yaml
---
last-reviewed: YYYY-MM-DD          # update when materially reviewed/verified
last-reviewed-tree: <tree-hash>    # deterministic git tree hash for the folder (excludes COPILOT.md)
status: draft|verified             # draft until an SME validates accuracy
---
```

If the tree hash cannot be computed yet (e.g., pending migration), use a placeholder and mark it clearly:

```yaml
---
last-reviewed: 2025-10-29
last-reviewed-tree: FIXME(set-tree-hash)
status: draft
---
```

Optional frontmatter fields (use sparingly):
- related-folders: ["Src/Common/Controls", "Src/XCore"]
- tags: [interop, threading, xml]

## Accuracy principles

- Never speculate. If the code or assets do not confirm a fact, capture the gap with an explicit `FIXME(<topic>): ...` instead of inventing an answer.
- When a folder contains only trivial stubs or archived artifacts, state that plainly and list the actual files under `## References` so readers can verify the claim.
- Prefer grounded references to specific files, symbols, or project entries over generic phrases like "utilities" or "helpers".
- Remove a `FIXME` only after verifying the information against source files, project metadata, or other authoritative artifacts—never by replacing it with a guess.

## When to update COPILOT.md files

**Update threshold (substantive change gates)**: COPILOT.md files should be updated when there are **substantive changes** to a folder:

- Architectural changes (layering, boundaries)
- New or removed public interfaces (classes, methods, exported functions, COM interfaces)
- XML/XSL/XAML/data model changes (new elements/attributes, schema changes, transforms)
- New major components, subprojects, or entry points
- Purpose/scope changes for the folder
- Dependency changes (project refs, COM/PInvoke/interop, NuGet/system libs)
- Build or test infrastructure changes (targets, defines, runners, props/targets)
- Significant behavior changes surfaced in public APIs or data contracts
- Threading model or concurrency patterns that affect consumers; notable performance characteristics

- Interop/signature changes: P/Invoke signatures, COM GUIDs/Interfaces, marshaling behavior
- Configuration or feature flag matrices that change behavior for users or integrators
- Packaging/deployment footprint that changes public locations or required runtime assets

**Do NOT update for**:
- Minor code changes within existing files with no public API or data contract change
- Bug fixes that don't change architecture
- Refactoring that preserves interfaces and structure
- Comment or documentation updates within source files
- Pure formatting/style-only changes or localization string churn without semantic impact

## Canonical COPILOT.md skeleton (required headings)

Use this skeleton for all `Src/**/COPILOT.md` files. Keep sections concise, but prefer concrete, verifiable details.

> Keep the sections in this exact order as per `.github\scaffold_copilot_markdown.py`. Remove scaffold leftovers such as `## Auto Summary`, duplicated `## References`, or any headings that are not part of this template.

```markdown
---
last-reviewed: YYYY-MM-DD
last-reviewed-tree: <tree-hash>
status: draft|verified
---

# <FolderName> COPILOT summary

## Purpose
Short 1–2 sentences followed by an optional short paragraph grounded in code/data.

## Architecture
High-level components and how they interact; diagram optional (textual is fine).

## Key Components
Bulleted list of major classes/modules/files with brief roles.

## Technology Stack
Languages, frameworks, target frameworks, notable defines.

## Dependencies
What this folder depends on and what depends on it.

### Upstream (consumes)
List key upstream libraries/folders; cite project references.

### Downstream (consumed by)
List primary known consumers; link to their COPILOT.md where possible.

## Interop & Contracts
COM/PInvoke/C++/CLI boundaries, marshaling, binary/layout contracts.

## Threading & Performance
Threading model, synchronization, known hot paths, perf considerations.

## Config & Feature Flags
List config sources, feature flags, and effects at a glance.

## Build Information
Prefer FieldWorks.sln or agent-build-fw.sh; note any special targets/props.

## Interfaces and Data Models
Public interfaces and XML/XAML/XSLT contracts (see template in this guide).

## Entry Points
How this code is invoked/used (apps, services, tasks, COM entrypoints).

## Test Index
Where tests live and how to run them; key fixtures/data sets.

## Usage Hints
Common tasks, extension points, examples; link to samples if any.

## Related Folders
Cross-links to peer folders with short rationale.

## References
Authoritative file lists: project files, key sources/headers, data artifacts.
```

## Systematic validation approach (scan everything)

When performing comprehensive COPILOT.md updates (e.g., repository-wide validation), use this systematic process:

### Pass 1 – Comprehension
- Read the existing `COPILOT.md` alongside every file in the folder (and immediate subfolders) to build a grounded understanding of the main inputs, outputs, and classes.
- Capture a draft Purpose/Architecture summary tied to explicit files. Whenever information is missing, add `FIXME(<topic>)` markers instead of guessing.

### Pass 2 – Contracts, dependencies, and edge cases
- Inspect project files, includes/usings, and interop hooks to document upstream/downstream dependencies, configuration knobs, XML/data contracts, and boundary conditions.
- Replace placeholder dependency terms with the concrete library/header (e.g., `Include/xmlparse.h`, `Lib/DataWriterRgb.lib`).
- Note any unusual behaviors (binary parsing, marshaling, threading) with references to the relevant files.

### Pass 3 – Synthesis and cleanup
- Re-architect the `COPILOT.md` narrative: ensure required sections exist in canonical order, trim duplication, and resolve as many `FIXME` markers as possible with verified evidence.
- If unresolved `FIXME` entries remain, explain the blocker inline so the next reviewer knows what is missing.
- Perform a final scan of the folder to confirm every significant file/class is reflected in the doc and dependencies align with project metadata.

Add extra focused passes when warranted (e.g., dedicated performance review). The key is to stay scoped to the folder during each pass—close unrelated files and searches until the current folder is complete.

### 1. Analyze every folder's actual contents (code + XML)
For each `Src/` folder, examine all relevant files:
- Project/build: `.csproj`, `.vcxproj`, `.props`, `.targets`
- Managed/native sources: `.cs`, `.xaml`, `.cpp`, `.cc`, `.c`, `.hpp`, `.h`, `.ixx`, `.def`
- Data and config: `.xml`, `.xsl`, `.xslt`, `.dtd`, `.xsd`, `.resx`, `.config`
- Tests: test projects and data under sibling `*.Tests` or folder-specific test dirs

Notes:
- Include COM interop artifacts (GuidAttribute, ComVisible, registry hints) and C++/CLI bridges.
- Include XSLT transforms, XML data contracts, and serialized model definitions (e.g., attributes or schemas).

### 2. Validate against actual code
-- **Verify project references** by parsing `.csproj`/`.vcxproj` files
- **Check dependencies** match actual build graph
- **Validate file lists** against directory contents
- **Confirm technology stack** from project file target frameworks and file types
- **Test build commands** if documenting standalone builds

### 3. Extract and document Interfaces and Data Models (required)
Create a dedicated section in COPILOT.md:

```markdown
## Interfaces and Data Models

- InterfaceOrTypeName (path: Src/Folder/File.ext)
   - Purpose: what it is for in 1–2 sentences.
   - Inputs: key parameters or inputs (types, key fields) — concise.
   - Outputs: return values, side effects, artifacts produced.
   - Notes: interop boundaries (COM/PInvoke), encoding, threading, error contracts (if notable).

- XmlOrXamlModelOrTransform (path: Src/Folder/Model.xml)
   - Purpose: describe the document/transform role in 1–2 sentences.
   - Shape: key elements/attributes or template names (terse bullets).
   - Consumers: where it’s used (project/class) and at what stage.
```

Keep each entry short but concrete. Prefer 1–2 sentences per interface/model plus 2–4 bullets. List only public/consumed interfaces and persisted data contracts; skip purely private helpers unless they define externally-consumed behavior.

Tip: For architecture, you may include a Mermaid diagram when useful. If diagrams aren’t practical, provide a precise textual description. Examples of text-only diagrams are acceptable.

### 4. Add authoritative references section
Include a **References** section at the end of each COPILOT.md with concrete details:

```markdown
## References
- Project files: ProjectName.csproj, ProjectName.Tests.csproj
- Target frameworks/config: net48, defines, include/lib paths (if notable)
- Key dependencies: SIL.LCModel, SIL.WritingSystems, System.Windows.Forms
- Key C# files: MainClass.cs, ImportantHelper.cs, ConfigManager.cs
- Key C++ files: NativeCore.cpp, Interop.cpp
- Key headers: Interface.h, Types.h
- Data contracts/transforms: schema.xml, layout.xslt, view.xaml, strings.resx
```

This provides Copilot with authoritative, verifiable information about folder contents.

## How to update COPILOT.md files
1. Read the existing `COPILOT.md` file for the folder you're working in.
2. If you notice discrepancies (e.g., missing components, outdated descriptions, incorrect dependencies):
   - Update the `COPILOT.md` file to reflect the current state.
   - Update cross-references in related folders' `COPILOT.md` files if relationships changed.
   - Update `.github/src-catalog.md` with the new concise description.
3. Keep documentation concise but informative:
   - **Purpose**: What the folder is for (1–2 sentences)
   - Then expand to a short, accurate paragraph grounded in code and data (1–4 sentences)
   - **Key Components**: Major files, subprojects, or features (with concrete file names from References)
   - **Technology Stack**: Primary languages and frameworks (validated from project files)
   - **Dependencies**: What it depends on and what uses it (from project references)
   - **Build Information**: Always prefer top-level solution or `agent-build-fw.sh`; avoid per-project builds unless clearly supported. If per-project SDK-style build/test is valid, document exact commands.
   - **Interfaces and Data Models**: Public interfaces and XML/XAML/XSLT contracts with 1–2 sentence summaries and terse I/O notes
   - **Entry Points**: How the code is used or invoked
   - **Threading & Performance**: Concurrency model and any practical perf notes
   - **Config & Feature Flags**: Inputs that materially change behavior
   - **Test Index**: Where tests live and how to run them
   - **Usage Hints**: Common tasks and extension points
   - **Related Folders**: Cross-references to other `Src/` folders (bidirectional)
   - **References**: Authoritative section with concrete file names, project files, and dependencies

4. Resolve `FIXME` markers deliberately:
   - Replace `FIXME(accuracy): ...` with verified information drawn from the inspected files.
   - Replace `FIXME(build): ...` only after running or validating the documented command.
   - Replace `FIXME(components): ...` with the actual file listings or authoritative references.
   - Leave a `FIXME` in place **only** when SME input or future work is required, and annotate why it could not be resolved.

5. Use longer descriptions when they raise accuracy.
   - Prefer brevity, but if a few extra sentences materially improve precision (especially for architecture, interop, or data contracts), include them.
   - Always ground longer text in specific files, types, or project settings cited in References.

## Example scenarios requiring COPILOT.md updates
- Adding a new C# project to a folder → update "Key Components", "Build Information", and "References"
- Discovering a folder depends on another folder not listed → update "Dependencies", "Related Folders", and "References"
- Finding that a folder's description is inaccurate → update "Purpose" section based on actual code analysis
- Adding new test projects → update "Build Information", "Testing", and "References" sections
- Removing deprecated components → update all relevant sections and remove from "References"
- Changing target framework → update "Technology Stack" and "References"

## Automated validation tools (optional)

For repository-wide updates, use the helper scripts in this folder to systematically analyze and maintain all COPILOT.md files.

Quick starts (run from repo root):

```powershell
# Ensure required frontmatter fields across all Src/**/COPILOT.md
python .github/fill_copilot_frontmatter.py --status draft --ref HEAD

# Validate structure, headings, and references for changed COPILOT.md only; fail build on errors
python .github/check_copilot_docs.py --only-changed --fail --verbose

# Detect which folders changed and need COPILOT.md updates (CI-friendly)
# Tip: Base is optional; script defaults to diffing against origin/HEAD when omitted
python .github/detect_copilot_needed.py --strict

# Scaffold COPILOT.md for changed folders (frontmatter, section ordering, auto references)
# Tip: Base is optional; script defaults to diffing against origin/HEAD when omitted
python .github/scaffold_copilot_markdown.py
```

You can also script your own analysis. Example snippet for parsing .csproj references:

```python
# Example: Analyze project file for dependencies
import xml.etree.ElementTree as ET

def analyze_csproj(path):
    tree = ET.parse(path)
    root = tree.getroot()

    # Extract project references, target framework, etc.
    references = []
    for elem in root.iter():
        tag = elem.tag.split('}')[-1]
        if tag == 'ProjectReference':
            ref = elem.get('Include', '')
            references.append(os.path.basename(ref).replace('.csproj', ''))

    return references
```

This ensures **every file** is validated against actual code and data contracts, achieving >90% accuracy.

Additional automation ideas:
- Headings presence check: ensure every required heading from the skeleton exists (simple regex scan per file).
- Cross-link check: if A lists B under Downstream, ensure B’s Related Folders includes A.
- Link integrity: a non-blocking link-check workflow (`.github/workflows/link-check.yml`) is included in this repo; keep it green.

CI tip: You may add a non-blocking docs validation job that runs `python .github/check_copilot_docs.py` and posts results as annotations. Keep it advisory until coverage is consistent.

Recommended CI wiring:
- Run `detect_copilot_needed.py` on pull_request with `--base origin/${{ github.base_ref }}` and `--strict` to fail when code changes under `Src/**` don’t include a corresponding COPILOT.md update.
- Optionally run `check_copilot_docs.py --only-changed --base origin/${{ github.base_ref }} --fail` to ensure updated COPILOT.md files still meet the skeleton (prevents unintentional section deletions) without flagging unrelated folders.
- Use `scaffold_copilot_markdown.py` in automation to normalize section ordering when COPILOT docs change.

VS Code integration:
- Use the tasks under `.vscode/tasks.json`:
   - COPILOT: Detect updates needed
   - COPILOT: Propose updates for changed folders
   - COPILOT: Validate COPILOT docs (changed only)
   - COPILOT: Update flow (detect → propose → validate)

Note: Local VS Code tasks resolve the default base branch dynamically via `Build/Agent/GitHelpers.ps1` (Get-DefaultBranchRef) to avoid hardcoded branch names.

Agentic flow prompt:
- See `.github/prompts/copilot-docs-update.prompt.md` to drive the full detect → propose → validate sequence with clear success criteria and step-by-step instructions.

## Quality gates for COPILOT.md updates

Before committing COPILOT.md changes, verify:

✓ **Frontmatter present**: All files have last-reviewed, and status fields
✓ **Last reviewed tree**: `last-reviewed-tree` present and matches the folder's git tree hash (excludes COPILOT.md)
✓ **References section**: Includes concrete file names and data artifacts from actual directory listing
✓ **Dependencies verified**: Project references match those in .csproj/.vcxproj files
✓ **Build commands tested**: If documenting standalone builds, verify they work
✓ **Cross-references bidirectional**: If A references B, B should reference A
✓ **Required headings present**: Matches the canonical skeleton
✓ **Consistency with src-catalog.md**: Short description matches catalog entry
✓ **FIXME markers are purposeful**: Remaining FIXMEs explain why verification is blocked and point to needed evidence; none mask speculation.
✓ **File lists current**: Key files section reflects actual directory contents

Always validate that your code and data changes align with the documented architecture and contracts. If they don't, either adjust your changes or update the documentation to reflect the new reality.
