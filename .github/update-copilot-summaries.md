# Updating COPILOT.md summaries

Each folder under `Src/` has a `COPILOT.md` file that documents its purpose, components, public interfaces, and data models. These files are essential for understanding the codebase and for keeping AI guidance accurate and actionable.

## Frontmatter (required)
Add minimal frontmatter to each `COPILOT.md` so ownership and review hygiene are clear:

```yaml
---
owner: <GitHub-team-or-username>   # e.g., @sillsdev/fieldworks-core
last-reviewed: YYYY-MM-DD          # update when materially reviewed/verified
status: draft|verified             # draft until an SME validates accuracy
---
```

If ownership is not known at the time of edit, use a placeholder and mark it clearly:

```yaml
---
owner: FIXME(set-owner)
last-reviewed: 2025-10-29
status: draft
---
```

## When to update COPILOT.md files

**Update threshold (substantive change gates)**: COPILOT.md files should be updated when there are **substantive changes** to a folder:

- Architectural changes (layering, boundaries, ownership)
- New or removed public interfaces (classes, methods, exported functions, COM interfaces)
- XML/XSL/XAML/data model changes (new elements/attributes, schema changes, transforms)
- New major components, subprojects, or entry points
- Purpose/scope changes for the folder
- Dependency changes (project refs, COM/PInvoke/interop, NuGet/system libs)
- Build or test infrastructure changes (targets, defines, runners, props/targets)
- Significant behavior changes surfaced in public APIs or data contracts

**Do NOT update for**:
-- Minor code changes within existing files with no public API or data contract change
- Bug fixes that don't change architecture
- Refactoring that preserves interfaces and structure
- Comment or documentation updates within source files

## Systematic validation approach (scan everything)

When performing comprehensive COPILOT.md updates (e.g., repository-wide validation), use this systematic process:

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
   - **Related Folders**: Cross-references to other `Src/` folders (bidirectional)
   - **References**: Authoritative section with concrete file names, project files, and dependencies

4. Remove FIXME markers once information has been validated (and prefer including file paths inline for context when helpful to Copilot):
   - Replace `FIXME(accuracy): ...` with verified information from code analysis
   - Replace `FIXME(build): ...` with tested build commands
   - Replace `FIXME(components): ...` with actual file listings
   - Only keep FIXME markers for items requiring domain expert knowledge

## Example scenarios requiring COPILOT.md updates
- Adding a new C# project to a folder → update "Key Components", "Build Information", and "References"
- Discovering a folder depends on another folder not listed → update "Dependencies", "Related Folders", and "References"
- Finding that a folder's description is inaccurate → update "Purpose" section based on actual code analysis
- Adding new test projects → update "Build Information", "Testing", and "References" sections
- Removing deprecated components → update all relevant sections and remove from "References"
- Changing target framework → update "Technology Stack" and "References"

## Automated validation tools (optional)

For repository-wide updates, use Python scripts to systematically analyze all folders:

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

## Quality gates for COPILOT.md updates

Before committing COPILOT.md changes, verify:

✓ **Frontmatter present**: All files have owner, last-reviewed, and status fields
✓ **References section**: Includes concrete file names and data artifacts from actual directory listing
✓ **Dependencies verified**: Project references match those in .csproj/.vcxproj files
✓ **Build commands tested**: If documenting standalone builds, verify they work
✓ **Cross-references bidirectional**: If A references B, B should reference A
✓ **Consistency with src-catalog.md**: Short description matches catalog entry
✓ **No vague FIXME markers**: Only keep FIXMEs for items requiring domain expertise (and track owners)
✓ **File lists current**: Key files section reflects actual directory contents

Always validate that your code and data changes align with the documented architecture and contracts. If they don't, either adjust your changes or update the documentation to reflect the new reality.
