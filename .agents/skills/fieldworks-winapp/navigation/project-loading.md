# Confirm Or Restore A Sample Project

Use this path after launching FieldWorks when a task needs a repeatable project.

## Entry State

- FieldWorks is launched or attached.
- You have an app ID from WinApp MCP.

## Steps

1. Get a snapshot with `mcp_winapp_get_snapshot`.
2. Treat the project as loaded if the snapshot contains a root document such as
   `Root - Sena 3`, or if the desktop title contains the expected project name.
3. For repeatable manual evidence, use the current project if `Sena 3` is
   already loaded.
4. If no project is loaded, restore from the sample backup at the repository
   root:
   - `Sena 3 2018-09-11 1145.fwbackup`
5. Use the File menu and inspect the menu tree for restore/project-management
   commands before clicking.
6. If a file chooser opens, select the backup path and proceed through restore.
7. Re-inspect the app and capture a loaded-project screenshot.

## Expected Signals

- Project loaded: `Root - Sena 3` document in the snapshot.
- Common loaded side pane: `Lexicon Edit`, `Browse`, `Dictionary`,
  `Collect Words`, `Classified Dictionary`, `Bulk Edit Entries`.

## Safety

- Do not restore over an existing loaded project unless the task requires it.
- Prefer evidence-only screenshots over changing project data.

## Exit

Continue to the requested navigation path. If project restore was only a setup
step and the user did not ask to keep the app open, close the launched app at
the end of the task.
