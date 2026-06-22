# Graphite Transition Support

## Why

The lexical-edit migration plan currently treats Graphite as a hard blocker: "Avalonia SHALL never
support Graphite", decommissioning "starts with the migration", and Avalonia cannot become the
default Lexical Edit screen until Graphite is retired from the default path. That posture is wrong
for FieldWorks' users: Graphite-rendered writing systems are concentrated in exactly the minority-
language projects FieldWorks exists to serve, several SIL fonts are Graphite-only (e.g. Awami
Nastaliq), and forcing font migration as a precondition of the UI migration couples two risks that
should be sequenced separately.

This change replaces the hard block with a supported transition: **Graphite remains fully supported
on legacy WinForms/native-Views surfaces through the first half of the WinForms decommissioning**,
Avalonia surfaces render Graphite-enabled writing systems with OpenType shaping plus an **actionable
warning** (never a silent rendering change and never a hard block), and Graphite support sunsets at
a defined mid-decommissioning milestone with migration tooling — not at migration start.

It also extracts Graphite ownership out of `lexical-edit-avalonia-migration` (section 5 of its
tasks, `graphite-decommissioning.md`, and the blocking half of the `lexical-edit-font-decommissioning`
spec delta) into this dedicated change, so font policy stops gating UI-region completion.

## What Changes

- Establish the Graphite support policy for the coexistence period: supported on legacy surfaces,
  warned-and-OpenType-shaped on Avalonia surfaces, sunset at the mid-decommissioning milestone (M2),
  removed with WinForms (M3).
- Replace the "Graphite retirement blocks Avalonia default" gate with a "warning + diagnostics
  coverage" gate: Avalonia may become a default surface while Graphite projects exist, provided the
  per-writing-system warning, classification, and legacy-mode affordance are in place.
- Define the three-tier writing-system/font classification (dual-engine font, dual-engine with
  Graphite feature strings, Graphite-only font) that drives warning severity and migration tooling.
- Define the warning UX contract: per writing system, actionable (switch-to-legacy affordance plus
  font-replacement guidance), shown at most once per project session, never silently suppressed for
  Graphite-only fonts.
- Keep the native-engine boundary unchanged: no path loads `GraphiteEngineClass`/native Views
  Graphite shaping inside an Avalonia surface; the region-manifest forbidden-symbol audit is
  unaffected.
- Define the sunset milestones, the user-facing deprecation sequence, and the font-migration
  tooling that must ship before M2 enforcement.
- Supersede the blocking requirements in `lexical-edit-avalonia-migration`'s
  `lexical-edit-font-decommissioning` spec delta and re-home its section-5 tasks here.

## Non-goals

- Implementing Graphite shaping inside Avalonia (Path B in `design.md` is a recorded contingency
  with explicit pivot triggers, not planned work).
- Hosting legacy native-Views editors inside Avalonia surfaces for Graphite fields (Path C is
  rejected; it violates the active-host contract).
- Changing Gecko/browser/PDF Graphite behavior — export/preview paths keep their own classification
  under the lexical-edit change (tasks 5.6/5.7 there) and are unaffected by this policy until then.
- Removing any Graphite code, settings storage (`IsGraphiteEnabled`, `DefaultFontFeatures`), or
  shipped fonts before M2.

## Capabilities

### New Capabilities

- `graphite-transition-support`: Graphite support policy during WinForms decommissioning —
  legacy-surface support, Avalonia warning behavior, writing-system/font classification, sunset
  milestones, and migration tooling requirements.

### Modified Capabilities

- `lexical-edit-font-decommissioning` (delta carried by `lexical-edit-avalonia-migration`): the
  "decommissioning starts with the migration" and "default screen is blocked by Graphite
  dependency" requirements are superseded by this change's warning-based requirements. The
  inventory, OpenType feature migration, Gecko/PDF classification, and native-dependency
  classification requirements remain in the lexical-edit change.

## Impact

- Managed code: `Src/Common/FwAvalonia/` (warning surface + WS capability diagnostics),
  `Src/xWorks/` (per-host warning wiring, legacy-mode affordance), `Src/FwCoreDlgs/`
  (writing-system setup messaging), font classification service (new, location TBD with 6.x text
  foundation).
- Native code: none removed or added. `Src/views/lib/GraphiteEngine.*` remains in service for
  legacy surfaces until M3.
- Settings/data: `IsGraphiteEnabled` and `DefaultFontFeatures` remain authoritative storage;
  classification reads them, never rewrites them without explicit user action.
- Docs: supersedes `lexical-edit-avalonia-migration/graphite-decommissioning.md` (banner added);
  re-scopes lexical-edit tasks section 5; adjusts roadmap Gate 2 wording from "Graphite-free
  default" to "Graphite-warned default with no native Graphite engine on the Avalonia path".
