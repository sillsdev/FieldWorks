# FieldWorks Avalonia tokens alias Semi's own semantic layer, not independent values

Semi.Avalonia already ships a two-tier token system of its own: raw color-ramp/spacing
primitives (Layer 1, ~449 keys, no meaning attached) and named semantic roles that alias
them (Layer 2: `SemiColorText0`-`3`, `SemiColorBorder`, `SemiColorBackground0`-`4`,
`SemiColorDanger`, `SemiColorLink`, plus a flat spacing/radius/height scale). FieldWorks'
own product tokens (`FwLabelBrush`, `DataTree.LabelColumnWidth`, `Dialog*`, ...) sit above
that as a third, FieldWorks-owned tier.

**Decision**: default every FieldWorks token to aliasing Semi's Layer 2 role directly,
deleting the FieldWorks-owned key entirely where Semi's role fits with no divergence
(callers resolve `SemiColorDanger` etc. directly). Keep a FieldWorks-owned value only where
a specific, written reason shows Semi's shared role doesn't fit — and that reason must be
checkable against the actual pinned Semi.Avalonia source
(`src/Semi.Avalonia/Tokens/Palette/Light.axaml` at the pinned 11.3.14 tag), not assumed.
For example, `SemiColorBorder` is `SemiGrey9Color` (`#1C1F23`) at **8% `Opacity`**, which
composites to roughly `#EDEDED` over the shared `White` background both Semi's own controls
and FieldWorks use (`SemiColorBackground0` is `White` in Light) — nearly invisible, not "too
heavy." Likewise Semi's `Text0`-`3` are four genuinely distinct `SolidColorBrush` resources,
each with a different `Opacity` baked directly into the brush (`Text1` 0.8, `Text2` 0.62,
`Text3` 0.35 — not a template `Opacity` `Setter`), compositing to roughly `#494C4F`,
`#727477`, and `#B0B1B2` respectively over `White`. The FieldWorks values kept despite these
real (non-identical, non-opaque) Semi roles are pinned instead to values measured directly
from the legacy WinForms baseline — e.g. `FwLabelBrush` (`#696969`) and `FwWsAbbrevBrush`
(`#404040`) are legacy-measured pixel values (`FwAvaloniaDensity.cs`'s doc comments), each
~10-15 RGB units darker than the nearest real Semi text role; `FwSliceRuleBrush`/
`FwSectionRuleBrush` (`LightGray`) match `DataTree.cs`'s own `Color.LightGray` divider pen
exactly; `FwDisabledOptionBrush` (`Gray`, `#808080`) matches `MasterCategoryListDlg.cs`'s
`Color.Gray` for an unavailable option, ~48 RGB units darker (more visible) than Semi's real
composited `SemiColorDisabledText` (`~#B0B1B2`). A handful of other values are pinned to
legacy WinForms pixel-parity on purpose (e.g. `FwSelectedRowBrush`) rather than adopting
Semi's nearest equivalent tone.

**Why this is hard to reverse**: ~218 more dialog conversions will be built against
whichever convention this branch establishes; retrofitting "we independently invented our
own palette" into "we alias the vendor's" after the surface has grown is a much bigger
job than deciding it now, before a second data point exists.

**Consequence**: a future Semi.Avalonia version bump that changes `SemiColorText0`'s exact
hex value flows through FieldWorks automatically for every aliased token, without a manual
FieldWorks re-tune — this was previously not true (every FieldWorks color was chosen
independently by sampling old WinForms screenshots, with no relationship to Semi's palette
at all).

**Enforcement**: `token-hygiene.ps1` fails on a hardcoded color or spacing literal used
anywhere in the scoped Avalonia surface. It does not police the token dictionaries
themselves: a literal on a primitive resource declaration line — `<SolidColorBrush
x:Key="..." Color="#696969"/>` — is exempt, because that literal is the token's definition.
The check requires no justification comment, and does not verify that a token aliases Semi
rather than choosing its own value; those remain conventions this ADR argues for, upheld by
review rather than by the checker. A compile-time-safe token-key generator in `Build/Src/FwBuildTasks` (following
liblcm's `LcmGenerate` custom-MSBuild-Task precedent, not a Roslyn source generator)
additionally turns a typo'd or renamed key into a build error rather than a runtime throw.
