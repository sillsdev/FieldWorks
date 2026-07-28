# WinForms-Free Lexeme Editor — Decisions and Burn-Down

Status: ACTIVE. The Avalonia Lexicon-Edit region is trimmed to its committed foundation: string
editors, list-choice editors, editable reference vectors, structured text, the read-only literal
row, and ONE native-conversion plugin exemplar. Every other legacy editor renders a labeled
**Unsupported** row — the visible conversion worklist.

## Goal

The lexicon Entry pane (LexEntry/Normal detail and everything it composes) renders and edits with
**zero WinForms controls inside the pane**. Two carve-outs, both sanctioned by existing rules:

1. **Modal dialogs stay WinForms during coexistence** (`dialog-ownership.md`). A dialog migrates in
   its own workstream; the pane itself carries no WinForms.
2. **The host shell** (FwXWindow, panels, the WinForms↔Avalonia interop host) is the shell phase,
   not this lane.

Everything built here is reusable by the next DataTree tools (Notebook, Morphology, Grammar) — the
plugin contract and the composer are keyed by layout vocabulary and LCModel metadata, not by the
lexicon.

## What the region renders

The region composes exactly these row kinds; anything else is an Unsupported row.

- **Text** — the (multi-)writing-system string editor (`FwMultiWsTextField`), single and multi WS,
  with the per-run rich-text seam, character-style / WS-retag / insert-link / delete-object context
  menu, and WS font/RTL/keyboard. A voice/audio WS alternative renders as read-only text (no
  in-pane player).
- **StructuredText** — the editable multi-paragraph StText editor (`FwStructuredTextField`).
- **Chooser** — the atomic reference / list-choice editor (`FwChooserField`).
- **ReferenceVector** — the editable reference-vector editor (`FwReferenceVectorField`), including
  the composer's D2 reference-vector route below.
- **Header / Literal / Unsupported** — structural rows: a section header, the read-only "lit" label,
  and the labeled Unsupported placeholder.
- **Custom** — a plugin-claimed native editor built through `IRegionEditorPlugin`.

## The measured problem

Custom/dynamic slices actually used by the lexeme editor's part files (LexEntryParts.xml +
LexSenseParts.xml census):

| Legacy slice class | What it is | Route now |
|---|---|---|
| `LexEd.ReversalIndexEntrySlice` | reversal index entries | **Plugin (native exemplar)** |
| `LexEd.EntrySequenceReferenceSlice` | components / variant-of / complex-form entry-reference vectors | Composer-absorbed (ReferenceVector, D2) |
| `LexEd.GhostLexRefSlice` | ghost ("type to add") lane for lexical relations | Composer-absorbed (ReferenceVector, D2) |
| `LexEd.LexReferenceMultiSlice` | lexical relations: one slice per relation type | Composer-absorbed (ReferenceVector, D2) |
| `LexEd.MessageSlice` | Chorus Send/Receive notes bar | **Unsupported** (conversion worklist) |
| `DetailControls.AudioVisualSlice` | pronunciation media | **Unsupported** (conversion worklist) |
| MSA/phonological `*DlgLauncherSlice` | feature-structure launchers | **Unsupported** (conversion worklist) |
| `LexEd.LexEntryChangeHandler` | not UI — a change handler | not an editor; excluded |

## Decisions

### D1. One plugin contract for a native editor (`IRegionEditorPlugin`)

A registry in xWorks maps the **legacy layout identity** (the `class` attribute the importer already
carries on the typed node, e.g. `SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice`) to a plugin
that builds an **Avalonia control** for (object, node, edit context). The composer consults the
registry while walking; a claimed node composes as a `RegionFieldKind.Custom` row carrying the
plugin's control factory, and `LexicalEditRegionView` renders that control in-tree at the slice's
real position.

Keyed by legacy class name because the layouts are the contract: keying the registry off the same
attribute means zero layout edits per migration, per-tool reuse for free (Notebook's layouts route
through the identical mechanism), and a measurable burn-down (registry coverage vs. census).

**Resolution order in the composer: plugin registry → Unsupported row.** There is no launcher or
companion-strip fallback. A class graduates by acquiring a plugin: Unsupported → plugin.

`ReversalIndexEntryPlugin` is the sole registered exemplar (`RegisterBuiltins`). It claims
`ReversalIndexEntrySlice` and renders the sense's reversal-entry forms as an editable multi-WS text
field, riding the region's fenced edit session like every other text row.

### D2. Entry-reference vectors ride the ReferenceVector route

`EntrySequenceReferenceSlice`, `GhostLexRefSlice`, and `LexReferenceMultiSlice` are recognized by the
composer's own routing (no plugin) and compose as editable `ReferenceVector` rows — current
entries/senses as items (headword text), remove in-pane, with the add-slot affordance. **Add** uses
type-ahead search over the lexicon (headword prefix match via the entry repository) rather than
materializing the whole lexicon as options — possibility lists enumerate; lexicons search. The
composer keys these off the legacy class identity plus LCModel metadata (entry/sense reference
targets); pinned by `EntryReferenceVectorTests`, `GhostLexRefSliceTests`, and
`LexReferenceMultiSliceTests`.

Deferred within this route (unchanged): the slice's VIRTUAL back-ref fields (ComplexFormEntries,
Subentries, VisibleComplexFormBackRefs, VariantFormEntries) still render read-only — their writes
land on the other entry's `LexEntryRef`.

### D3. Unsupported rows are the conversion worklist

Every legacy editor not covered by D1 or D2 composes as a labeled **Unsupported** row rather than
being silently omitted. That includes the dropped scalar editors (boolean/checkbox, exact-date and
generic-date, closed enum-combo, integer, picture/image, command/button) and every unclaimed custom
slice (the Chorus notes bar, the pronunciation media slice, the MSA/phonological feature-structure
launchers). The visible set of Unsupported rows IS the backlog.

**Converting a slice to native Avalonia** is the forward path and needs no layout edits: write an
`IRegionEditorPlugin` that claims the legacy `class=`, build the Avalonia control in-tree, ride the
region's fenced edit context, and register it in `RegisterBuiltins` — the Unsupported row graduates
to a real editor. `ReversalIndexEntryPlugin` is the worked exemplar. A scalar editor (date, enum,
integer, boolean, picture) graduates the same way, by giving its category a native row again.

### D4. Governance: the burn-down is enforced by tests

`LexemeEditorBurnDownTests` reads the LexEntry/LexSense part-file census and pins each custom slice's
route: `ReversalIndexEntrySlice` is plugin-routed; the three D2 classes are composer-absorbed and are
NOT plugin-claimed; the formerly launcher-routed slices and the Chorus notes bar are unclaimed (so
they render Unsupported). The default registry's builtins are asserted to be **exactly**
`{ ReversalIndexEntrySlice }`. `RegionEditorPluginResolutionOrderTests` (over the memory cache) proves
that an unclaimed custom slice composes as Unsupported and that a plugin claim composes it as a
`RegionFieldKind.Custom` row.

### D5. Explicitly out of scope here

The WinForms host shell, WinForms dialogs themselves, morphology/grammar family-3 editors, and the
native media player. Each rides its own gate; none is needed for the pane itself to be WinForms-free.

## Reuse statement

The registry, the resolution order, the ReferenceVector route, and the native text editor are all
keyed by layout vocabulary and LCModel metadata — none of them know they are in the lexicon. Notebook
and Morphology adopt them by registering plugins, not by re-architecting.
