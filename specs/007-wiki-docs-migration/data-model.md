# Data Model: Wiki Documentation Migration

**Feature**: 007-wiki-docs-migration
**Date**: 2025-12-01

## Overview

This feature does not involve database entities or persistent storage. The "data model" describes the documentation artifacts being created and their relationships.

## Documentation Entities

### WikiPage

Represents a page from the source FwDocumentation wiki.

| Attribute | Type | Description |
|-----------|------|-------------|
| `name` | string | Wiki page name (e.g., "Contributing-to-FieldWorks-Development") |
| `url` | string | Full URL to wiki page |
| `category` | enum | Getting Started, Workflow, Coding Standards, Architecture, Linux, Historical |
| `migrationStatus` | enum | ACTIVE, PARTIALLY_OBSOLETE, OBSOLETE, CONFIRMATION_NEEDED |
| `lastUpdated` | date | Last wiki edit date |
| `targetLocation` | string | Path in repo where content will live (or null if not migrating) |

### MigratedDocument

Represents a documentation file created in the repository.

| Attribute | Type | Description |
|-----------|------|-------------|
| `path` | string | Relative path from repo root (e.g., `docs/CONTRIBUTING.md`) |
| `type` | enum | INSTRUCTION_FILE, DOC_FILE, WORKFLOW_DOC |
| `sourcePages` | WikiPage[] | Wiki pages that contributed content |
| `confirmationNeeded` | boolean | True if contains unverified content |
| `lastVerified` | date | Date content was verified against codebase |

### DocumentationCategory

Logical grouping for navigation and organization.

| Attribute | Type | Description |
|-----------|------|-------------|
| `name` | string | Category name (e.g., "Getting Started") |
| `description` | string | Brief description of category |
| `entryPoint` | string | Path to main document in category |
| `documents` | MigratedDocument[] | Documents in this category |

## Directory Structure

```
FieldWorks/
├── .github/
│   └── instructions/           # Copilot-facing code guidance
│       ├── coding-standard.instructions.md  # NEW: From wiki
│       ├── code-review.instructions.md      # NEW: From wiki
│       └── dispose.instructions.md          # NEW: From wiki
│
├── docs/                       # Human-facing documentation (NEW)
│   ├── CONTRIBUTING.md         # Main entry point
│   ├── visual-studio-setup.md  # VS 2022 setup
│   ├── core-developer-setup.md # Core dev onboarding
│   │
│   ├── workflows/              # Development workflows
│   │   ├── pull-request-workflow.md
│   │   └── release-process.md
│   │
│   ├── architecture/           # Technical architecture
│   │   ├── data-migrations.md
│   │   └── dependencies.md
│   │
│   ├── linux/                  # Cross-platform docs
│   │   ├── build-linux.md
│   │   └── vagrant.md
│   │
│   └── images/                 # Documentation images
│       └── (screenshots from wiki)
│
└── ReadMe.md                   # Updated to link to docs/
```

## Migration Status Enum Values

| Status | Description | Action |
|--------|-------------|--------|
| `ACTIVE` | Content is current and verified | Migrate as-is with path updates |
| `PARTIALLY_OBSOLETE` | Some content obsolete | Migrate with updates, remove obsolete |
| `OBSOLETE` | Entire page obsolete | Do not migrate |
| `CONFIRMATION_NEEDED` | Cannot verify against codebase | Migrate with marker |

## Relationships

```
WikiPage (source)
    │
    ├── 1:1 → MigratedDocument (for simple pages)
    │
    └── N:1 → MigratedDocument (for consolidated pages)
                    │
                    └── N:1 → DocumentationCategory
```

## Validation Rules

1. **No broken links**: All internal links must resolve to existing files
2. **No duplicate content**: Each topic covered in exactly one location
3. **Path consistency**: Use relative paths for in-repo references
4. **Marker format**: CONFIRMATION_NEEDED markers use consistent format:
   ```markdown
   > ⚠️ **CONFIRMATION_NEEDED**: [description of what needs verification]
   ```

## Content Transformation Rules

### File Path Updates

| Wiki Pattern | Repo Pattern |
|--------------|--------------|
| `C:\fwrepo\fw\` | Repository root (use relative paths) |
| `$FWROOT\` | Repository root (use relative paths) |
| `build.bat` | `build.ps1` |
| `FW.sln` | `FieldWorks.sln` |

### Link Transformations

| Wiki Link Type | Transformed To |
|----------------|----------------|
| Wiki page link `[[Page Name]]` | Relative markdown link `[Page Name](./page-name.md)` |
| External link | Preserve as-is |
| Image link | `![alt](./images/filename.png)` |

### Code Block Updates

| Wiki Code | Updated Code |
|-----------|--------------|
| `git review` | `git push origin <branch>` + create PR |
| `git start task develop myfeature` | `git checkout -b feature/myfeature` |
| `git finish task` | Merge PR via GitHub UI |
