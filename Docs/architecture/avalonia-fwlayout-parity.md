# Avalonia `.fwlayout` Parity

## Intention

Avalonia detail views use the same project `.fwlayout` files as WinForms and
must reproduce WinForms layout selection, fallback, mutation, persistence, and
transient behavior. WinForms is the default behavioral contract. A difference
is a defect unless it has been explicitly approved and recorded in the
`Divergences` section of this document.

This work does not convert Avalonia's pre-alpha `.viewoverride.json` files.
Existing JSON overrides are ignored. They are not imported or automatically
deleted.

This plan covers `.fwlayout` selection, command targeting, mutation,
persistence, and related transient writing-system state. It does not claim that
Avalonia already implements every WinForms editor or layout construct. Such
gaps remain defects to implement and must stay visible as unsupported rows,
with their blocking facts recorded in named TODOs. They are not divergences.
Content that WinForms itself omits (an unresolved part ref, part content
`DataTree.ProcessSubpartNode` does not recognize) is omitted the same way and
reported only through import diagnostics; an unsupported row there would be a
divergence.

## Persistent layout identity

An `.fwlayout` layout is identified by all four attributes used by the WinForms
layout inventory:

| Attribute | Meaning |
| --- | --- |
| `class` | Value of `layout/@class` on the selected XML layout; it can name a base class after fallback. |
| `type` | Value of `layout/@type` on the selected XML layout, normally `detail`. |
| `name` | Value of `layout/@name` on the selected XML layout; it can be `default` after fallback. |
| `choiceGuid` | Value of `layout/@choiceGuid` on the selected XML layout, or absence of that attribute. |

The values are matched case-insensitively, as they are by the WinForms layout
inventory. An absent `choiceGuid` is distinct from a present-empty `choiceGuid`
and from any other present value. Code that clones or persists a layout preserves
the selected XML key spelling so a case-sensitive physical-file replacement does
not append a duplicate.

Layout selection has two identities. The requested identity contains the
object's concrete class, requested layout name (or `default`), `detail` type,
and GUID obtained through `layoutChoiceField`. The resolved identity contains
the four attributes of the XML layout that WinForms actually selected after
fallback. Composed fields and commands carry the resolved identity. A command
never repeats layout fallback; it targets that already-resolved layout or fails
closed.

The caller path is also retained when a field is composed because the same part
can appear through different callers. It is relative to the resolved layout
that WinForms treats as the persistence root after promoting the layout beyond
the final `sublayout`. Each path segment contains the XML element name and its
zero-based ordinal among same-named element siblings. It locates the XML node
to mutate, but it is not a fifth layout-inventory key.

Runtime occurrence identity also carries the ordered four-field identities of
every selected layout crossed through object or sequence descent. Persistence
remains rooted in the outer layout, while this chain prevents a command or
transient state from matching the same caller path under a different nested
choice or fallback result. A `sublayout` resets both the persistence root and
the runtime layout chain.

Object HVO and field name are runtime safety checks. They prevent a command
from reaching a stale or ambiguous row, but they are not persistent layout
identity. Avalonia-generated stable IDs are likewise runtime identities and
must not become `.fwlayout` persistence keys.

## Plan

1. Rebase PR #1111 onto `origin/main`. Preserve the current
   `XCoreMenuBridge` interceptor contract and disabled-item normalization, and
   preserve the `RecordEditView.ResolveShownRecord` refresh fix.
   `OnDetailMenuRequested` renders the native menu only, with no WinForms
   adapter-menu fallback: when native menu construction throws, the error is
   logged and no menu is shown; when the menu ids resolve to no items, no menu
   is shown and nothing is logged. In both cases the post-menu refresh still
   runs (see Divergences). Replace conflicting JSON-backed persistence and
   Show-all code with the parity behavior defined here.
2. Carry `class`, `type`, `name`, and optional `choiceGuid` through layout
   loading, view-definition models, composed fields, command identities, menu
   bindings, and nested object or sequence composition. Retain the caller path
   for the exact composed occurrence.
3. Load the same shipped inventories as the legacy `Inventory`:
   `DistFiles/Parts` (`StandardParts.xml`, `GeneratedParts.xml`,
   `Standard.fwlayout`, `Generated.fwlayout`) and then
   `Language Explorer/Configuration/Parts`, with the later hand-authored files
   replacing same-id parts and same-key layouts. Without the first directory
   `CmObject-Detail-HeavySummary`, the generated `default` layouts, and the
   `autoCustom` part do not exist and every sense subtree vanishes.
4. Match WinForms layout selection and fallback order exactly. For the
   requested name, try the concrete class and then each base class with the
   requested choice. For an `RnGenericRec`, a missing choice-specific layout is
   first cloned from the no-choice layout of the class currently being tried.
   If the named search reaches `CmObject` without a match, WinForms changes the
   name to `default` and resets the class variable to the concrete class, but
   immediately advances to that class's base before the next lookup; it does
   not check the concrete class's default. Thus the default search begins at the
   concrete class's base. Throw if that search also reaches `CmObject` without a
   match. Apply this algorithm to root layouts, nested `sublayout` elements,
   and nested object and sequence layouts.
5. Match WinForms handling of a new Notebook record type. If an
   `RnGenericRec` choice layout does not exist, clone the no-choice layout, add
   the requested `choiceGuid`, add it to the layout inventory, and persist it
   to the project `.fwlayout` file before composing against it.
6. Resolve every persistent layout command to the exact hidden WinForms slice
   and use the existing WinForms mutation path. Missing or ambiguous targets
   fail closed and are logged instead of changing a nearby layout occurrence.
7. Remove the obsolete Avalonia JSON override path. Do not add conversion,
   deletion, or dual-write behavior.
8. Port and extend tests around the shared layout inventory, production command
   route, and project `.fwlayout` files. Cover root and nested choice layouts,
   repeated callers, new Notebook record types, fallback order, hidden-slice
   targeting, field-to-field current-state transitions, record and Type
   changes, and missing or ambiguous command targets.

## Behavior mapping

| Avalonia action | WinForms behavior to reuse |
| --- | --- |
| Hide or show a field | Update the `visibility` attribute on the caller part through the matching `Slice`. |
| Move a field | Change physical sibling order through `Slice.MoveField`. |
| Configure visible writing systems | Update `visibleWritingSystems` through `MultiStringSlice`, retaining its pronunciation-writing-system side effects. |
| Show all writing systems temporarily | Use WinForms transient state. Do not write the layout file. Reload configured writing systems when the target slice changes from current to not current. |
| Sense or record item header | WinForms renders the HeavySummary SummarySlice as the item's header. Avalonia synthesizes the item header from LexSenseOutline + ShortName and folds a HeavySummary that directly follows it into that header (no repeated row, children at the item's depth). |

For nested edits, WinForms promotes the selected layout after the final
`sublayout` to the persistence root. Avalonia must therefore keep the selected
choice variant and caller path intact so the existing override machinery writes
the intended nested layout rather than a same-named neighbor.

## Acceptance criteria

- Given the same project, tool, object, and `.fwlayout` files, Avalonia and
  WinForms resolve the same four-field layout identity at the root and at every
  nested layout boundary.
- Choice and `sublayout` resolution does not omit or duplicate any field that
  Avalonia supports from the selected layout. Constructs WinForms renders but
  Avalonia does not yet support remain visibly represented and tracked rather
  than being silently dropped; constructs WinForms omits are omitted.
- Every persistent Avalonia layout command changes the same XML node and uses
  the same legacy writer that the equivalent WinForms command uses.
- Moving current selection from the target field to another slice reloads the
  configured writing systems and ends temporary Show-all. Recomposition after
  a Type or record change cannot revive the old transient reveal.
- A new Notebook record type creates and persists the same choice-specific
  layout that WinForms creates.
- No command falls back from an exact choice or caller occurrence to a broader
  target merely to make the command succeed.
- Old `.viewoverride.json` files have no effect and require no migration.

## Retirement

After FieldWorks fully switches to Avalonia and no supported WinForms path
depends on `.fwlayout`, retire `.fwlayout` as the project layout-customization
format. Replace it with an Avalonia-owned JSON format and migrate the project
customizations that must survive the cutover. That future migration must define
the JSON schema, conversion and rollback strategy, validation, and removal of
the legacy layout inventory, hidden WinForms slices, and related command bridge.

Until that migration is designed, implemented, and validated, `.fwlayout`
remains the sole source of truth. The obsolete pre-alpha `.viewoverride.json`
format described above is not the future format and must not constrain its
design.

## Divergences

- **Behavior**: a detail-row menu request whose Avalonia-native menu
  construction throws (`XCoreMenuBridge` conversion or the host interceptor).
  **WinForms**: the legacy `DataTree` shows the xCore menu through the
  WinForms adapter `ContextMenuStrip` (`XWindow.ShowContextMenu` ->
  `MenuAdapter`), and the pre-parity Avalonia host fell back to that same
  adapter menu, so a usable menu still appeared.
  **Avalonia**: `RecordEditView.OnDetailMenuRequested` logs the
  error and shows no menu; the pending Show-all reveal still ends and the
  detail view still recomposes. **Why accepted**: the adapter menu bypasses
  the `XCoreMenuBridge` interceptor, so its commands would skip the
  exact-slice re-targeting and the post-command Avalonia recompose and leave
  the view stale. Such a failure is a defect to fix, made visible through the
  log rather than masked by a second rendering.
