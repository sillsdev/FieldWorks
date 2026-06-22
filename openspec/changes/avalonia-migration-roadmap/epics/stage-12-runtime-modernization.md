# Stage 12 — Runtime & toolchain modernization (.NET 10 + Avalonia 12) (Epic draft)

> **Status:** JIRA-ready draft. Source-grounded in
> `complete-migration-program.md` (§2 principle 14, §4 row 12, §5 sequencing note, §6 Stage 12
> detail + post-review callout, §9 risk register, §10 labels), `reviews/stage-12-runtime-modernization.md`,
> and `reviews/00-cross-comparison-synthesis.md` (§6 sub-epic map, §7 edges). Planning only — no
> code/behavior change. Create JIRA from this draft; do not split net10/Av12 into separate epics
> (they are coupled by the one-CLR rule), but model the four internal work-streams as gated stories.

---

## Epic — Stage 12: Runtime & toolchain modernization (.NET 10 + Avalonia 12)

**Summary.** Port the surviving managed FieldWorks codebase from .NET Framework 4.8 to .NET 10 and
bump Avalonia 11.3.17 → 12 as one coordinated, whole-process cutover, then land the coupled NUnit
3 → 4 upgrade that Avalonia 12 headless testing requires. This is the chartered "move to modern
tools" jump, sequenced deliberately late so it ports surviving managed code rather than WinForms
that Stage 13 is about to delete.

**Type.** Epic (Track IV — Shell, runtime modernization & cutover). Senior-led.

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:stage-11-shell`, *(not `parallel-safe`
— this is a coordinated whole-process bump, single active stream)*.

**Description.**
FieldWorks is uniformly **.NET Framework 4.8** today: a full sweep of `Src/` found **130 projects,
all `<TargetFramework>net48</TargetFramework>`, zero net8/9/10, and no multi-targeting anywhere**
(`Directory.Build.props` defaults C# `LangVersion` to 8.0, not 7.3). So this is a **single-target
port of the whole managed tree**, not a multi-targeting consolidation — there is no net8 lane to
retarget from, and no dual-TFM bookkeeping to unwind.

The **timing is forced, not merely wise.** `Directory.Packages.props` pins **Avalonia 11.3.17** with
an in-repo comment: 11.3.x still ships `netstandard2.0` assemblies and can load on net48; **Avalonia
12 dropped `netstandard2.0` (net8+ only) and cannot host in-process on net48.** Because the one-CLR
rule means a single in-process `WinFormsAvaloniaControlHost` spans the boundary, the Av11→12 bump is
*physically impossible* until the process leaves net48 — which cannot happen until the WinForms host
(Stage 11) is gone. Hence the late, coordinated, one-CLR shape.

The epic decomposes into four sequenced internal work-streams (synthesis §6):
**12-net10-port** (net48 → .NET 10 across all ~130 projects, leaf-first) → **intermediate green
checkpoint** (net10 + Avalonia 11.3.17 — a legal, testable state because 11.3.x is netstandard2.0;
decouples the two biggest risk sources) → **12-av12-bump** (resolve Av12 breaking changes:
`UseHarfBuzz()`, clipboard `IDataObject`→`IAsyncDataTransfer`, binding/focus/gesture API migration)
→ **12-nunit4** (NUnit 3.14.0 → 4 across ~40 test projects, required by Av12 headless, coupled to the
SIL.TestUtilities NUnit-4 upgrade). Land it behind the green `./build.ps1` / `./test.ps1` / CI gate;
apply `fieldworks-managed-netfx-review` (refresh the skill's stale net48-vs-net8 premise in the same PR).

**Acceptance criteria.**
- Whole process runs on **.NET 10 + Avalonia 12**; `./build.ps1` and `./test.ps1` green on Windows/x64.
- The **intermediate net10 + Avalonia 11.3.17 checkpoint** was reached, built, and tested green before
  the Av12 bump (verified, not skipped).
- All ~130 product + test projects target net10; **no project targets a different TFM or Avalonia major**
  — enforced as a **CI invariant** (one TFM, one Avalonia major), not just a convention.
- NUnit 4 across all ~40 test projects; `ClassicAssert` aliasing per the in-repo TODO
  (`Src/Directory.Build.props`); SIL.TestUtilities on NUnit 4.
- Av12 breaking-change checklist closed: `UseHarfBuzz()` wired after `UseSkia()`; clipboard/drag-drop
  seams migrated to `IAsyncDataTransfer`/`DoDragDropAsync()`; programmatic-binding audit done
  (`FwFieldControls`, `FwMultiWsTextField`); focus/gesture APIs migrated (`FocusManager`,
  `FocusChangedEventArgs`); DevTools package swapped; `Screen`-abstract usage resolved.
- API-compat hazards discharged: BinaryFormatter, `System.Drawing.Common` (Windows-only package),
  `System.Configuration`/app.config model, `AppDomain`/remoting, CodeDom — searched and ported.
- `fieldworks-managed-netfx-review` skill refreshed to the net48→net10 single-target, C#8→latest,
  NUnit 3→4 reality in the same PR.

**Dependencies.**
- **Hard entry gate: "Stage 11 shell retired."** Stage 12 must not start over a still-WinForms shell —
  if Stage 11 slips, the residual-WinForms-on-net10 port surface balloons (and the surviving C++/COM
  interop with it). Also depends on **most of Stages 5–10** (so the port covers surviving code, not
  soon-to-be-deleted WinForms).
- **Enables Stage 13** (final cutover + native decommission + **cross-platform enablement**). net48 is
  Windows-only; .NET 10 is the capability that unblocks Linux/macOS. Stage 12 delivers the *capability*;
  Stage 13 spends the *validation cost* (cross-platform held to Stage 13 by decision §11.2).

**Rough size.** Large epic, senior-led, single coordinated stream (not parallelizable). Effort
**medium-to-high** but materially de-risked by prior stages: Gecko/XULRunner and Graphite (worst
net48-coupling offenders) are already removed (Stage 10), and the native UI/render COM surface is
largely gone (Stage 9 replaced Views; Stage 13 finishes the rest), so surviving interop is mostly
*non-UI* linguistics services (Kernel/Generic/ICU/XAmple) behind service seams. The residual-WinForms
and surviving-COM census (only Stage 11 close can size it) is the chief effort uncertainty.

---

## Sub-epics / stories

### 12-net10-port — Port the surviving managed tree net48 → .NET 10

**Summary.** Retarget all ~130 net48 projects (product + test) to net10, leaf-first, and get the
whole tree compiling and testing green.

**Type.** Story (gating). Senior.

**Description.** Single-target port: the repo is uniformly net48 (verified), so there is no net8 lane
to consolidate. Port leaf-first (Utils/Kernel-wrapper/test-utilities → up the graph to xWorks/LexText/
shell); CPM + central `Directory.Build.props` make the global TFM switch a single chokepoint, but
per-project compat fixes must go leaf-first to keep the tree buildable. Run **`.NET Upgrade Assistant`
for analysis only** (blocker report) — hand-port the fixes across 130 projects under
`TreatWarningsAsErrors`. Pin API-compat hazards as **pre-port discovery tasks**: BinaryFormatter removal,
`System.Drawing.Common` Windows-only package + runtime config, `System.Configuration`/app.config model,
`AppDomain`/remoting, CodeDom, binding-redirect removal. Residual WinForms moves to WinForms-on-.NET 10
(a supported workload, Windows-only). Native-first build order (`FieldWorks.proj` `BuildNativeFirst`)
already enforces the COM/codegen prerequisite; spike the Kernel/Generic/ICU reg-free COM interop early.

**Acceptance criteria.** All ~130 projects target net10 and build green leaf-first; `.NET Upgrade
Assistant` blocker report triaged and discharged; API-compat hazard list closed; reg-free COM /
P/Invoke marshalling verified on net10 (CharSet defaults, etc.); `./build.ps1` green.

**Dependencies.** Entry gate: **Stage 11 shell retired** + most of Stages 5–10. Gates the
intermediate checkpoint and everything downstream in this epic.

**Labels.** `track-shell`, `lead-senior`.

**Rough size.** Large (the mechanical + API-compat bulk of the epic).

---

### 12-green-checkpoint — Intermediate green state: net10 + Avalonia 11.3.17

**Summary.** Reach and verify a legal, testable **net10 + Avalonia 11.3.17** state — build/test green —
*before* touching Avalonia 12, to decouple the two biggest risk sources.

**Type.** Story (gate / milestone). Senior.

**Description.** 11.3.x is netstandard2.0, so it loads on net10 — making net10 + Av11 a valid
intermediate checkpoint. This is the single most useful tactic available: land net10 with Avalonia
unchanged and get fully green, *then* bump Avalonia, instead of debugging the runtime port and the UI-
framework bump simultaneously. **OQ-1:** verify in a spike that 11.3.17 actually loads/restores on net10
before committing to this as a hard gate.

**Acceptance criteria.** Whole solution builds and tests green on net10 + Avalonia 11.3.17 via
`./build.ps1` / `./test.ps1`; spike confirms 11.3.17 loads on net10; this state is a recorded, gated
milestone (not skipped straight to Av12).

**Dependencies.** Gated by **12-net10-port**. Gates **12-av12-bump**.

**Labels.** `track-shell`, `lead-senior`.

**Rough size.** Small-to-medium (a verification/stabilization gate, not new feature work).

---

### 12-av12-bump — Bump Avalonia 11.3.17 → 12 (breaking changes)

**Summary.** Bump Avalonia to 12 from the green net10 checkpoint and resolve the repo-specific
breaking changes, most confined to named seams by the upstream "Av12-delta-localized" discipline.

**Type.** Story (gating). Senior.

**Description.** Av12 minimum is net8 (net10 recommended), so this is only possible from the net10
checkpoint. Repo-specific breaking changes to resolve (Av12 breaking-changes doc):
- **`UseSkia()` must now also call `UseHarfBuzz()`** — aligns with principle 13 (HarfBuzz-only managed
  shaping; Graphite removed in Stage 10). A one-line wiring change, but text shaping silently breaks if
  omitted — must be on the checklist.
- **Clipboard/drag-drop rewrite:** `IDataObject` → `IAsyncDataTransfer`/`DataTransfer`,
  `DoDragDrop()` → `DoDragDropAsync()`. The clipboard/drag-drop **seam signatures in `ISeams.cs` change here.**
- **Binding internals:** `IBinding`/`InstancedBinding` removed → `BindingBase`/`BindingExpressionBase`;
  binding-plugin system removed. Audit owned controls that construct bindings programmatically
  (`FwFieldControls`, `FwMultiWsTextField`). Compiled bindings on by default — aligns with Decision §11.3.
- **Focus/gestures:** `KeyboardNavigationHandler` → `FocusManager`; `Gestures` no longer public;
  `GotFocusEventArgs` → `FocusChangedEventArgs`. Touches the host focus memento / directional-key bypass.
- **Shell-relevant (Stage 11 output):** DevTools package swap, TopLevel interfaces, `Screen` now abstract,
  `TitleBar`/`CaptionButtons` removed. Direct2D1 removed (Skia only) — no impact (repo already on SkiaSharp).

The delta is real but **bounded and mostly seam-localized**, *provided* the Av12-delta-localized
discipline (the Stage 2/11 exit gate confining 11-only-removed APIs to named seams) actually held
through Stages 1–11.

**Acceptance criteria.** Whole solution on Avalonia 12, build/test green; full Av12 breaking-change
checklist closed (see Epic ACs); `UseHarfBuzz()` verified by a text-shaping check.

**Dependencies.** Gated by **12-green-checkpoint**. Theming coupling with the Stage 2 "Av12-ready
theming" gate (upstream control). Runs with / gates **12-nunit4** (Av12 headless needs NUnit 4).

**Labels.** `track-shell`, `lead-senior`.

**Rough size.** Medium (bounded if the delta-localized discipline held; larger if it leaked).

---

### 12-nunit4 — NUnit 3.14.0 → 4 across the test projects

**Summary.** Upgrade NUnit 3 → 4 across ~40 test projects — a breaking API change required by Avalonia
12 headless testing — as its own reviewed work-stream, not riding silently on the Av12 bump PR.

**Type.** Story (gating, coupled). Senior.

**Description.** Avalonia 12 headless test packages require **NUnit 4** (Avalonia docs). The repo pins
**NUnit 3.14.0** (`Directory.Packages.props`) across ~40 test projects, and `Src/Directory.Build.props`
carries the explicit TODO: "When SIL packages upgrade to NUnit 4, update this … and add global using
aliases for `ClassicAssert`." NUnit 3→4 is a breaking change (`Assert.That` model, `ClassicAssert`)
touching every test assembly. Add the global `ClassicAssert` using-aliases per the in-repo TODO.
**OQ-2:** this is **gated on SIL.TestUtilities shipping NUnit 4** — track now as an external dependency;
if it lags Stage 12, the epic is blocked on it (or must carry a temporary NUnit-version split).

**Acceptance criteria.** All ~40 test projects on NUnit 4; `ClassicAssert` aliasing in place;
SIL.TestUtilities on NUnit 4; `./test.ps1` green on net10 + Av12; the in-repo TODO is removed.

**Dependencies.** Coupled to **12-av12-bump** (Av12 headless requirement) and to the external
SIL.TestUtilities NUnit-4 upgrade. Final green gate of the epic.

**Labels.** `track-shell`, `lead-senior`.

**Rough size.** Medium (mechanical but broad — every test assembly).

---

## Notes / open questions

- **Av12 TFM constraint forces the timing (verified).** Avalonia 12 dropped `netstandard2.0`/net48
  (`Directory.Packages.props` comment + Avalonia discussion #18606, ~<4% telemetry rationale). The
  Av11→12 bump is therefore *physically blocked* until the process leaves net48, which can't happen
  until the WinForms host (Stage 11) is gone. The late, coordinated, one-CLR sequencing is **forced, not
  just tidy** — this is the earliest point the bump is even possible. Confidence: **High**.
- **Do NOT split net10 / Av12 into separate epics.** They are coupled by the one-CLR fact: Av12 requires
  net10 (so the bump can't precede the port), and there's no reason to bump Avalonia while still on net48
  (12 won't load). One epic, four sequenced internal stories, single CI gate.
- **"Av12-delta-localized," not "Av12-ready."** Code running on Avalonia 11 / net48 through Stages 1–11
  *cannot* avoid 11-only APIs (clipboard `IDataObject`, programmatic `IBinding`,
  `KeyboardNavigationHandler`/`Gestures`, `GotFocusEventArgs`) — it must use them on 11. The achievable
  posture is **confining 11-only-removed APIs to named seams** (clipboard, drag-drop, binding
  construction, focus/gestures, theming), enforced by a **Stage 2/11 exit gate**. This converts the vague
  "Av12-ready" promise into a testable invariant and is the chief mitigation for the "breaking changes
  ripple late" risk. If the discipline held, 12-av12-bump is small; if it leaked, the churn surfaces here.
- **Stale `fieldworks-managed-netfx-review` premise.** The skill (and the original Stage 12 wording) is
  built around a "net48 vs SDK-style net8 / C#7.3" boundary that **does not match the repo**: it is
  uniformly net48 (130 projects, zero net8/9/10) and defaults C#8. The real boundary at Stage 12 is
  **net48 → net10 (single target), C#8 → latest, NUnit 3 → 4.** Refresh the skill in the same PR (its own
  "Keep This Skill Current" clause requires it).
- **OQ-3 (residual-WinForms size):** how much WinForms genuinely survives into Stage 12 vs. is deleted in
  Stage 13 determines the WinForms-on-net10 port cost and the surviving C++/COM-interop surface. Needs a
  census at Stage 11 close.
- **CI invariant:** assert one TFM (net10) and one Avalonia major (12) across the whole solution after the
  bump — encode the one-CLR rule as a test, not a convention.
- **Verify with repo scripts only** (`./build.ps1`, `./test.ps1`) on *both* the intermediate net10+Av11
  checkpoint and the final net10+Av12 state — never bare `dotnet build`.
