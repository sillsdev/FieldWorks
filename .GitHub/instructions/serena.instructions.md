---
applyTo: "**/*"
name: "serena.instructions"
description: "When and how to use Serena MCP tools for symbolic code navigation and editing"
---

# Serena MCP Integration

## Purpose & Scope
Guide GitHub Copilot on when to use Serena's symbolic tools versus built-in tools for code exploration and editing in FieldWorks.

## When to Use Serena Tools

### Use Serena for Symbol-Level Operations
Prefer Serena tools (`mcp_oraios_serena_*`) when:

| Task | Serena Tool | Why |
|------|-------------|-----|
| Find all usages of a class/method | `find_referencing_symbols` | Semantic accuracy across 110+ projects |
| Understand class structure | `get_symbols_overview` | Quick view without reading entire file |
| Rename a symbol safely | `rename_symbol` | Updates all references automatically |
| Replace entire method/class | `replace_symbol_body` | Preserves surrounding code structure |
| Add code before/after a symbol | `insert_before_symbol`, `insert_after_symbol` | Precise placement without line counting |
| Search by symbol name | `find_symbol` | Works across C# and C++ boundaries |

### Use Built-in Tools for These Tasks
Prefer VS Code/Copilot built-in tools when:
- Reading entire files (use `read_file`)
- Simple text search (use `grep_search`)
- File/directory listing (use `list_dir`)
- Running terminal commands (use `run_in_terminal`)
- Making small line-based edits (use `replace_string_in_file`)

## Decision Flowchart

```
Need to understand code?
├─ Entire file contents → read_file
├─ File structure/symbols → mcp_oraios_serena_get_symbols_overview
├─ Find symbol by name → mcp_oraios_serena_find_symbol
└─ Find who calls X → mcp_oraios_serena_find_referencing_symbols

Need to edit code?
├─ Replace whole method/class → mcp_oraios_serena_replace_symbol_body
├─ Add new method to class → mcp_oraios_serena_insert_after_symbol
├─ Small inline change → replace_string_in_file
└─ Rename across codebase → mcp_oraios_serena_rename_symbol
```

## Serena Memories

Read project memories for context before complex tasks:
```
mcp_oraios_serena_list_memories  → See available memories
mcp_oraios_serena_read_memory("architecture")  → Build phases, subsystems
mcp_oraios_serena_read_memory("common_issues")  → Troubleshooting guide
```

## Key Advantages in FieldWorks

1. **Cross-language awareness**: Serena indexes both C# and C++ (via language servers)
2. **Large codebase navigation**: 110+ projects—symbol search is faster than grep
3. **Refactoring safety**: `find_referencing_symbols` catches usages across project boundaries
4. **Native/managed boundary**: Find where C# calls C++/CLI and vice versa

## Examples

### Find all usages of a class
```
# Instead of: grep_search for "LcmCache"
# Use: Serena's semantic search
mcp_oraios_serena_find_symbol(name_path="LcmCache", include_body=false)
mcp_oraios_serena_find_referencing_symbols(relative_path="Src/...", name_path="LcmCache")
```

### Add a new method to a class
```
# Instead of: read entire file, find line number, replace_string_in_file
# Use: Serena's symbolic insertion
mcp_oraios_serena_find_symbol(name_path="MyClass/LastMethod", include_body=false)
mcp_oraios_serena_insert_after_symbol(name_path="MyClass/LastMethod", content="public void NewMethod() { ... }")
```

### Understand file structure before editing
```
# Instead of: read_file (potentially 1000+ lines)
# Use: Serena's overview
mcp_oraios_serena_get_symbols_overview(relative_path="Src/LexText/LexTextApp.cs")
# Then read only the specific symbol you need
mcp_oraios_serena_find_symbol(name_path="LexTextApp/Initialize", include_body=true)
```

## Activation

Serena tools are available via `activate_symbol_management_tools`, `activate_file_search_and_listing_tools`, etc. The tools activate automatically when you call them through the `mcp_oraios_serena_*` prefix.
