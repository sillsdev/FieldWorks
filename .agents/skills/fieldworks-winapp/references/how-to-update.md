# How To Update This Skill

Update this skill as FieldWorks navigation improves or as WinForms MCP and
WinApp MCP reveal more stable automation patterns.

## Organization Rules

1. Keep `SKILL.md` small. It is the trigger, safety, and table-of-contents
   layer.
2. Put each distinct navigation destination or workflow in its own file under
   `navigation/`.
3. Use one route file when the user goal is one destination, even if it crosses
   several menus.
4. Split a route file when a path has a different entry state, different target
   dialog, different safety profile, or different verification signal.
5. Keep source rationale and maintenance rules in `references/`, not in every
   route file.

## Route File Template

Use this structure for new navigation files:

```markdown
# Navigate To <Destination Or Goal>

Use this path when ...

## Entry State

- ...

## Steps

1. ...

## Stable Elements Observed

- Dialog: `...`
- Button: `...`

## Expected Signals

- ...

## Known Workarounds

- ...

## Exit

- ...
```

## Update Checklist

- Start from a fresh `winforms_get_element_tree` or `mcp_winapp_get_snapshot`.
- Prefer automation IDs and element names over coordinates.
- Record menu path, dialog title, stable automation IDs, and the successful
  tool call pattern.
- Record which MCP driver was verified: WinForms MCP UIA2/headless or WinApp
   MCP UIA3/visible-desktop.
- Record failed tool calls only when the workaround is useful later.
- Include the safe exit path and whether the route mutates project data.
- Keep verification cues separate from route mechanics; route files can say how
  to reach and recognize the state, while tests or Jira/OpenSpec notes decide
  whether the state passes.
- If a route changes, update or remove stale selectors in the same edit.
- If a route becomes long, move repeated control groups into a second route file
  only when another path needs them.
- If a route works with both MCP drivers, document the preferred driver first
   and keep fallback steps shorter.

## When To Add A New Navigation File

Add a new file when you discover a repeatable user goal such as:

- opening a dialog;
- restoring a project;
- reaching a major side-pane area;
- setting up export/import options;
- collecting a standard evidence sequence.

Do not add a file for a one-off button unless it is a stable reusable component
used by multiple paths.
