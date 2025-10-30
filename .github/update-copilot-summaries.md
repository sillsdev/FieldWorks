# Updating COPILOT.md summaries

Each folder under `Src/` has a `COPILOT.md` file that documents its purpose, components, and relationships. These files are essential for understanding the codebase and for keeping AI guidance accurate and actionable.

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

**Update threshold**: COPILOT.md files should be updated when there are **substantive changes** to a folder:

- Making significant architectural changes to a folder
- Adding new major components, subprojects, or entry points
- Changing the purpose or scope of a folder
- Discovering discrepancies between documentation and reality
- Adding or removing project dependencies
- Modifying build requirements or test infrastructure

**Do NOT update for**:
- Minor code changes within existing files
- Bug fixes that don't change architecture
- Refactoring that preserves interfaces and structure
- Comment or documentation updates within source files

## Systematic validation approach

When performing comprehensive COPILOT.md updates (e.g., repository-wide validation), use this systematic process:

### 1. Analyze every folder's actual contents
For each `Src/` folder, examine:
- **Project files** (*.csproj, *.vcxproj): Parse for dependencies, references, target frameworks
- **Source files**: List key C#, C++, and header files (excluding tests, generated files)
- **Test projects**: Identify associated test assemblies
- **Subdirectories**: Document major subfolders with their own COPILOT.md files
- **Build configuration**: Check how the folder is built (via solution, standalone, or as part of another project)

### 2. Validate against actual code
- **Verify project references** by parsing .csproj/.vcxproj files
- **Check dependencies** match actual build graph
- **Validate file lists** against directory contents
- **Confirm technology stack** from project file target frameworks and file types
- **Test build commands** if documenting standalone builds

### 3. Add authoritative references section
Include a **References** section at the end of each COPILOT.md with concrete details:

```markdown
## References
- **Project Files**: ProjectName.csproj, ProjectName.Tests.csproj
- **Target Framework**: net48, netstandard2.0
- **Key Dependencies**: SIL.LCModel, SIL.WritingSystems, System.Windows.Forms
- **Key C# Files**: MainClass.cs, ImportantHelper.cs, ConfigManager.cs
- **Key C++ Files**: NativeCore.cpp, Interop.cpp
- **Key Headers**: Interface.h, Types.h
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
   - **Key Components**: Major files, subprojects, or features (with concrete file names from References)
   - **Technology Stack**: Primary languages and frameworks (validated from project files)
   - **Dependencies**: What it depends on and what uses it (from project references)
   - **Build Information**: Always prefer top-level solution or `agent-build-fw.sh`; avoid per-project builds unless clearly supported. If per-project SDK-style build/test is valid, document exact commands.
   - **Entry Points**: How the code is used or invoked
   - **Related Folders**: Cross-references to other `Src/` folders (bidirectional)
   - **References**: Authoritative section with concrete file names, project files, and dependencies

4. Remove FIXME markers once information has been validated:
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

## Automated validation tools

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

This ensures **every file** is validated against actual code, achieving >90% accuracy.

## Quality gates for COPILOT.md updates

Before committing COPILOT.md changes, verify:

✓ **Frontmatter present**: All files have owner, last-reviewed, and status fields  
✓ **References section**: Includes concrete file names from actual directory listing  
✓ **Dependencies verified**: Project references match those in .csproj/.vcxproj files  
✓ **Build commands tested**: If documenting standalone builds, verify they work  
✓ **Cross-references bidirectional**: If A references B, B should reference A  
✓ **Consistency with src-catalog.md**: Short description matches catalog entry  
✓ **No vague FIXME markers**: Only keep FIXMEs for items requiring domain expertise  
✓ **File lists current**: Key files section reflects actual directory contents  

Always validate that your code changes align with the documented architecture. If they don't, either adjust your changes or update the documentation to reflect the new architecture.
