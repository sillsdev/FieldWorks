# Criteria for Identifying Obsolete Projects in FieldWorks Source Tree

This document defines criteria for determining whether a project in the FieldWorks repository is obsolete and should be considered for removal or archival.

## Primary Criteria

### 1. **No Active Development or Maintenance**
- **No substantive commits** in the last 3-5 years (excluding mass refactorings like csproj migrations)
- **No bug fixes** or feature additions targeting the project specifically
- **No open issues** or pull requests related to the project
- Evidence in git history that the project hasn't been actively worked on

### 2. **Not Referenced by Active Code**
- **No project references**: No other active projects reference this project in their `.csproj` files
- **No code imports**: No active source files import namespaces or types from this project
- **Not in build system**: Project is excluded from the main build targets (e.g., commented out or conditionally excluded in `CollectTargets.cs` or similar build scripts)
- Only referenced by test projects that are themselves obsolete

### 3. **Platform or Technology Obsolescence**
- **Targets deprecated platforms**: Built for technologies no longer supported (e.g., SQL Server migration for versions no longer in use)
- **Uses obsolete frameworks**: Depends on libraries or frameworks that have been replaced or deprecated
- **Legacy migration tools**: Tools designed for one-time migrations that have already been completed
- **Old data formats**: Handles file formats or data structures no longer in use

### 4. **Functionality Replaced or Superseded**
- **Duplicate functionality**: Another project provides the same capabilities
- **Feature removed**: The feature this project supported has been removed from the product
- **Replaced by external library**: Functionality now provided by a NuGet package or external dependency
- Evidence in documentation or comments that functionality has moved elsewhere

### 5. **Marked as Experimental or Incomplete**
- **Explicit markers**: Comments in build files indicating "experimental" or "not built by default"
- **Incomplete implementation**: Substantial TODO comments or unimplemented features
- **Prototype status**: Documentation or code comments indicating it was a prototype or proof-of-concept
- Never reached production-ready status

## Secondary Criteria

### 6. **Limited or Empty Codebase**
- **Very few files**: Contains only a handful of source files (e.g., < 5-10 files)
- **Minimal functionality**: Code is trivial or could be easily incorporated elsewhere
- **Stub implementations**: Most methods return `E_NOTIMPL` or similar not-implemented indicators
- **No tests**: No associated test projects or test coverage

### 7. **Documentation Indicators**
- **Marked as deprecated**: README or comments explicitly state the project is deprecated
- **No documentation**: Lacks any meaningful documentation or user guides
- **Historical references only**: Only mentioned in old migration guides or historical documentation
- **"Old" in path name**: Located in directories like "OldMigrationScripts" or similar

### 8. **Build and Dependency Issues**
- **Build failures**: Consistently fails to build in current environment
- **Broken dependencies**: Depends on assemblies or tools no longer available
- **Platform-specific obsolescence**: Built for platforms no longer supported (e.g., Windows-only when product is cross-platform)
- **Cannot be migrated**: Cannot be converted to modern project format (SDK-style csproj)

### 9. **Resource Overhead**
- **Maintenance burden**: Requires special handling in build scripts or CI/CD
- **Security vulnerabilities**: Contains or depends on code with known security issues
- **Technical debt**: Would require significant work to bring up to current standards
- **Build time impact**: Significantly increases build times with no value

## Special Considerations

### Projects to Review Carefully

Even if they meet some criteria, these should be carefully evaluated:

- **Shared utilities**: Projects that provide common functionality, even if lightly used
- **Interface definitions**: Projects defining interfaces used across the codebase
- **Build tools**: Projects that are part of the build infrastructure
- **Test helpers**: Shared test utilities used by multiple test projects

### Migration vs. Deletion

Consider whether to:
- **Archive**: Move to a separate archive location with documentation
- **Delete**: Remove entirely if no historical value
- **Consolidate**: Merge useful parts into other active projects
- **Document**: Add clear deprecation notices before removal

## Evaluation Process

For each potentially obsolete project, document:

1. **Last meaningful modification date**: When was it last actively developed?
2. **Current references**: What (if anything) still uses it?
3. **Original purpose**: What was it designed to do?
4. **Current status**: Is that purpose still relevant?
5. **Replacement**: If functionality is needed, what provides it now?
6. **Risk assessment**: What breaks if we remove it?

## Examples from FieldWorks Codebase

Based on initial review, example categories:

### Likely Obsolete
- **MSSQLMigration/OldMigrationScripts**: Historical migration scripts for database versions no longer supported
- **ProjectUnpacker**: Test utility excluded from normal builds (marked in CollectTargets.cs)
- **FxtExe**: Experimental project excluded from normal builds

### Requires Investigation  
- **CacheLight**: Referenced by some test projects; need to verify if still actively used
- **nmock**: External library bundled in Bin folder; may be replaced by modern mocking frameworks

### Active Projects
- **FwBuildTasks**: Core build infrastructure
- **LexText/**: Main application components
- **xWorks**: Main UI framework

## Decision Matrix

| Criteria Met | Action |
|-------------|--------|
| 5+ Primary + 2+ Secondary | Strong candidate for removal |
| 3-4 Primary + 1+ Secondary | Investigate for possible removal |
| 2-3 Primary | Monitor; document status |
| 0-1 Primary | Keep unless specific issues |

## Documentation Requirements

Before removing any project:
1. Create issue documenting the decision
2. Update any related documentation
3. Add entry to changelog/migration guide
4. Verify removal in test environment
5. Get team approval for significant projects
