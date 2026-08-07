# Stage 12 Review — Runtime & toolchain modernization (.NET 10 + Avalonia 12)

Reviewer: senior migration review (Claude, Opus 4.8). Date: 2026-06-15.
Scope reviewed: `complete-migration-program.md` §2 principle 14, §4 table row 12, §5
sequencing note, §6 Stage 12 detail, §9 risk register, §11 decisions; the as-built
toolchain (`Directory.Build.props`, `Src/Directory.Build.props`, `Directory.Packages.props`,
`FieldWorks.proj`, `build.ps1`); the actual TFM landscape across `Src/`; the
`fieldworks-managed-netfx-review` skill; Stage 1 and Stage 2 reviews; external research on
Avalonia 12 supported TFMs / breaking changes and .NET Framework → .NET 10 migration tooling.

---

## 1. Scope assessment

Stage 12 bundles four deliverables: (a) port surviving managed code net48 → .NET 10;
(b) bump Avalonia 11.3.x → 12; (c) coordinated whole-process bump behind green build/test/CI
with test-project TFM retargeting; (d) declare cross-platform prerequisite satisfied. The
**late sequencing is correct**, and the bundling is **defensible but should be explicitly
ordered internally**, not treated as one atomic flip.

Findings:

- **The late placement is well-justified and survives scrutiny.** Principle 14 / §5 are right:
  the runtime jump should port surviving managed code, not WinForms that Stage 13 deletes. The
  one-CLR constraint (one process, one runtime, one Avalonia major) makes a *coordinated* bump
  mandatory — you cannot trickle net48→net10 per project while a single in-process
  `WinFormsAvaloniaControlHost` spans the boundary. So "late + coordinated" is the only coherent
  shape. Confirmed correct.

- **(a) and (b) are genuinely coupled by the one-CLR fact and should NOT be split into
  separate stages — but they are two distinct work-streams inside the one epic.** Avalonia 12
  *requires* net8+ (drops net48 — see §2), so the Avalonia bump is *impossible* until the net10
  port lands; conversely there is no reason to bump Avalonia while still on net48 (12 won't
  load). They must ship in the same coordinated cutover. The right structure is **one epic, two
  sequenced internal work-streams**: WS-1 net48→net10 port (gates first), WS-2 Avalonia 11→12
  bump (depends on WS-1), with a single CI gate flipping both. Recommend the plan name this
  internal ordering explicitly rather than implying simultaneity.

- **The plan's test-project framing is stale and should be corrected.** Stage 12 text says
  "retarget `net48`/`net8` multi-targeting in test projects," and the
  `fieldworks-managed-netfx-review` skill is built around a "net48 / C#7.3 vs SDK-style net8
  boundary." **Neither matches the repo today.** A full sweep of `Src/` found **130 projects, all
  `<TargetFramework>net48</TargetFramework>`, zero net8/net9/net10, and no multi-targeting
  anywhere** (`grep -rhoiE "<TargetFrameworks?>" --include="*.csproj" Src/` → only `net48`).
  `Directory.Build.props:62` even defaults C# `LangVersion` to **8.0**, not 7.3 (so the skill's
  "C#7.3" red-flag list is also stale). There is no net8 lane to "retarget from"; Stage 12 is a
  **net48 → net10 port of the whole managed tree**, not a multi-targeting consolidation. This is
  a larger and simpler-shaped job than the plan's wording implies (no dual-TFM bookkeeping to
  unwind — but also no partial-net8 head start to build on).

- **A hidden, sizeable sub-deliverable is missing from the scope: NUnit 3 → 4.** Avalonia 12's
  headless test packages require **NUnit 4** (Avalonia docs: "Headless … NUnit upgraded to v4").
  The repo pins **NUnit 3.14.0** (`Directory.Packages.props:174`) across ~40 test projects, and
  `Src/Directory.Build.props:32-34` carries the explicit note "When SIL packages upgrade to NUnit
  4, update this … and add global using aliases for `ClassicAssert`." NUnit 3→4 is a breaking
  API change (`Assert.That` model, `ClassicAssert`) touching every test assembly, **coupled to**
  both the Avalonia 12 bump and the SIL.TestUtilities upgrade. This belongs explicitly in Stage
  12 scope as WS-3, gated by the SIL package family moving to NUnit 4.

- **Residual-WinForms-on-net10 scope is correct but should state the dependency on Stage 13
  ordering.** The plan says residual WinForms moves to WinForms-on-.NET 10 (Windows-only). That is
  feasible (WinForms is a supported net10 workload), but the *amount* of residual WinForms depends
  on how much of Stages 11/13 has landed. If Stage 12 runs strictly before Stage 13's deletions,
  the surviving WinForms surface — and the C++/COM interop it carries — is still non-trivial.

## 2. Feasibility (repo-grounded + web)

**The single governing fact is verified in-repo and is the gate on timing: Avalonia 12 cannot
run on net48, so the Avalonia bump *forces* the .NET 10 port first.**

- `Directory.Packages.props:183-213` pins **Avalonia 11.3.17** with the explicit comment:
  *"Pinned to 11.3.x because it still ships netstandard2.0 assemblies and can therefore load on
  .NET Framework 4.8. Avalonia 12.x dropped netstandard2.0 (net8+ only) and cannot host in-process
  on net48."* `Src/Common/FwAvalonia/FwAvalonia.csproj` and `FwAvaloniaTests` both target
  **net48**. So during coexistence the whole process is correctly net48 + Avalonia 11.3.17.
- External confirmation: Avalonia 12 **drops netstandard2.0 and .NET Framework; minimum is .NET
  8, recommended .NET 10** (Avalonia 12 breaking-changes doc; discussion #18606 "Dropping support
  for .NET Framework 4.x and netstandard2.0 in 12.0"). This *validates the program's whole timing
  premise*: the Av11→12 bump is **physically blocked** until the process leaves net48. Stage 12's
  late placement is not just tidy — it is the earliest point the bump is even possible, given the
  one-CLR rule and the decision to keep WinForms hosting in-process until Stage 11.

- **net48 → net10 effort for FieldWorks is moderate-to-high but de-risked by prior stages:**
  - *C++ interop / registration-free COM:* The native Views/Kernel/Generic engine is `.vcxproj`
    (`Src/views/views.vcxproj`, `Src/Kernel/Kernel.vcxproj`, etc.) consumed via reg-free COM.
    .NET 10 fully supports COM interop and reg-free COM activation contexts on Windows, and
    `FieldWorks.proj` already builds native-first (`BuildNativeFirst` target, line 49). The
    interop *surface* is unchanged by the runtime; the risk is in P/Invoke/marshalling defaults
    (e.g. `CharSet`, `BinaryFormatter` removal, `System.Drawing.Common` becoming Windows-only
    package) rather than the COM model itself. **Crucially, by Stage 12 the native *UI/render*
    COM surface is largely gone** (Stage 9 replaced Views; Stage 13 decommissions the rest), so
    the interop that survives the port is mostly the *non-UI* linguistics services
    (Kernel/Generic/ICU/XAmple) behind service seams — a much smaller, more stable interop set.
  - *Dependencies already removed by Stage 12:* Gecko/XULRunner and Graphite are gone (Stages 10,
    principle 13) — these were among the worst net48-coupling offenders (native, x86-ish, old
    interop). Their prior removal materially shrinks the port.
  - *Mechanical bulk:* 130 SDK-style-ish net48 projects retargeted to net10. `.NET Upgrade
    Assistant` automates the TFM bump and flags blockers, but its value here is limited because
    the projects are already SDK-style and CPM-managed; the real work is API-compat
    (`System.Web`, `System.Drawing`, `AppDomain`/remoting, BinaryFormatter, config-system
    differences, `System.Configuration` app.config model) and binding-redirect removal.
  - *Build/CI:* `build.ps1` / `FieldWorks.proj` are MSBuild-Traversal and TFM-agnostic; the bump
    is a props change plus per-project fixes, not a build-system rewrite. `Directory.Build.props`
    already centralizes `LangVersion`, `RuntimeIdentifiers`, x64 — a good single chokepoint.

- **Avalonia 11.3.17 → 12 breaking changes that hit this repo specifically** (Avalonia 12
  breaking-changes doc):
  - *Compiled bindings on by default* — aligns with Decision 3 (dialogs use `x:CompileBindings`);
    low friction, mostly positive.
  - *`UseSkia()` must now also call `UseHarfBuzz()`* — directly relevant: the program's managed
    shaping (principle 13, HarfBuzz-only) must wire `UseHarfBuzz()` explicitly at the Av12 bump.
  - *Clipboard/drag-drop rewrite:* `IDataObject` → `IAsyncDataTransfer`/`DataTransfer`,
    `DoDragDrop()` → `DoDragDropAsync()`. The repo has clipboard/drag-drop seams
    (`ISeams.cs`) — these seam signatures will change at Av12.
  - *Binding internals:* `IBinding`/`InstancedBinding` removed → `BindingBase`/
    `BindingExpressionBase`; binding-plugin system removed. Owned controls that construct bindings
    programmatically (`FwFieldControls`, `FwMultiWsTextField`) need an audit.
  - *Focus/gestures:* `KeyboardNavigationHandler` → `FocusManager`; `Gestures` no longer public;
    `GotFocusEventArgs` → `FocusChangedEventArgs`. The host's focus memento / directional-key
    bypass (`LexicalEditHostControl`) touches this area.
  - *DevTools, TopLevel interfaces, `Screen` now abstract, `TitleBar`/`CaptionButtons` removed*
    — shell-relevant (Stage 11 output), low impact on field surfaces.
  - *Direct2D1 removed (Skia only):* repo already uses Skia (`SkiaSharp 2.88.9`); no impact.
  - *Headless test runner: NUnit 3 → 4* — see §1; the largest mechanical Avalonia-coupled cost.

  Net: the Av11→12 delta is real but **bounded and mostly seam-localized**, *provided* the
  "Av12-ready" discipline (principle 14, Stage 2 rec. 6) was actually honored through Stages 1-11.

## 3. Best practices (large .NET Framework → modern .NET + major UI-framework bump)

1. **Bump the runtime first, the UI framework second, in one coordinated cutover.** Get the whole
   tree compiling/testing green on net10 *while still on Avalonia 11.3.17 if it loaded on
   net10* — but note 11.3.x is netstandard2.0, so it *does* load on net10, giving a valuable
   intermediate checkpoint: **net10 + Avalonia 11** is a legal, testable state. Use it. Land
   net10 with Avalonia unchanged, get green, *then* bump Avalonia 12. This decomposes the two
   biggest risk sources instead of debugging them simultaneously. (This intermediate state is the
   single most useful tactic available and the plan should mandate it.)
2. **Leaf-first port order.** Standard guidance (MS WinForms migration docs, Upgrade Assistant):
   port no-dependency projects first (Utils/Kernel-wrapper/test-utilities), then up the graph to
   xWorks/LexText/shell. CPM + the central `Directory.Build.props` make a global TFM switch
   trivial, but per-project compat fixes must still go leaf-first to keep the tree buildable.
3. **Use `.NET Upgrade Assistant` for *analysis*, hand-port the fixes.** Run its analyzer to
   produce the blocker report (BinaryFormatter, `System.Web`, remoting, config) but don't trust
   its automated edits across 130 projects under `TreatWarningsAsErrors=true`; treat the report as
   a checklist, port deliberately.
4. **Pin the API-compat hazards as explicit tasks:** BinaryFormatter removal (also an Avalonia 12
   clipboard change), `System.Drawing.Common` Windows-only package + runtime config,
   `System.Configuration`/app.config model differences, `AppDomain`/remoting usage, `CodeDom`,
   any `BinaryFormatter`-based caching/serialization. Search the tree for each before the port.
5. **Keep the one-CLR / one-Avalonia-major invariant as a CI assertion**, not a convention: a
   build-time check that no project targets a different TFM or Avalonia major than the rest.
6. **Treat NUnit 3→4 as its own reviewed work-stream** (global `ClassicAssert` aliasing per the
   in-repo TODO), gated on SIL.TestUtilities shipping NUnit 4; do not let it ride silently on the
   Avalonia bump PR.
7. **Apply `fieldworks-managed-netfx-review` — but update the skill first.** The skill's central
   premise (net48 *coexisting with* SDK-style net8) is now historically inaccurate for this repo;
   at Stage 12 the relevant boundary is **net48 → net10 (single target), C#8→latest, NUnit
   3→4**. Refresh the skill in the same PR (its own "Keep This Skill Current" clause requires it).
8. **Verify with repo scripts only** (`./build.ps1`, `./test.ps1`) on both the intermediate
   net10+Av11 checkpoint and the final net10+Av12 state — never bare `dotnet build`.

## 4. Interactions & dependencies

- **Depends on Stage 11 (shell) and most of 5-10 — correctly stated.** The value of "port
  surviving code, not deleted WinForms" is only realized if the WinForms shell (11) and surfaces
  (5-8) are gone first. If Stage 11 slips, Stage 12 either slips with it or wastes effort porting
  WinForms that Stage 13 deletes. **Hard gate: Stage 12 must not start until Stage 11 has retired
  the WinForms shell**, else the residual-WinForms-on-net10 surface balloons.
- **Gates Stage 13 (cross-platform) — correctly the prerequisite.** net48 is Windows-only; net10
  is the thing that unblocks Linux/macOS. The plan's claim that cross-platform is *held* to Stage
  13 by decision (not by capability) is consistent: Stage 12 delivers the *capability*, Stage 13
  spends the validation cost. Good separation.
- **The "new code is Av12-ready through Stages 1-11" claim is plausible but partially unverifiable
  and is the chief sequencing risk.** Stages 1-11 build on **Avalonia 11.3.17** (the only version
  that loads on net48). "Av12-ready" can only mean *avoiding APIs Avalonia 12 removes* — and
  several removed-in-12 APIs are exactly what coexistence code must use on 11: `IDataObject`
  clipboard/drag-drop, programmatic `IBinding`, `KeyboardNavigationHandler`/`Gestures`,
  `GotFocusEventArgs`. **You cannot fully avoid 11-era APIs while running on 11**; the realistic
  posture is "minimize and centralize behind seams so the Av12 delta is localized," not "write
  code that needs no Av12 changes." Recommend the plan downgrade the claim from "Av12-ready
  (no changes needed)" to "**Av12-delta-localized** (11-only APIs confined to named seams:
  clipboard, drag-drop, binding construction, focus/gestures, theming)" and add a Stage-2/Stage-11
  exit check that those APIs appear *only* inside those seams. This converts a vague promise into a
  testable invariant and is the best mitigation for "breaking changes ripple late" (§9 risk).
- **Theming coupling with Stage 2 (already flagged in Stage 2 review rec. 6):** Fluent/ControlTheme
  resource APIs changed in 12; the Stage 2 "Av12-ready theming" gate is the upstream control. If
  that gate held, Stage 12 theming churn is small; if not, it surfaces here.
- **HarfBuzz coupling:** Av12 requires explicit `UseHarfBuzz()` after `UseSkia()`. Since principle
  13 already mandates HarfBuzz-only shaping (Graphite removed in Stage 10), this is a one-line
  wiring change that *aligns* with the program rather than fighting it — but it must be on the
  Stage 12 checklist or text shaping silently breaks at the bump.

## 5. Recommended plan changes

1. **Re-word Stage 12's test-project line.** Replace "retarget `net48`/`net8` multi-targeting in
   test projects" with the accurate task: "**retarget all ~130 net48 projects (product + test) to
   net10; there is no existing net8 lane**." State that the repo is uniformly net48 today
   (verified) so this is a single-target port, not a multi-targeting consolidation.
2. **Make the internal ordering explicit: WS-1 net10 port → (intermediate green net10+Av11
   checkpoint) → WS-2 Avalonia 12 bump → WS-3 NUnit 3→4.** Keep it one epic; sequence the
   work-streams; mandate the net10+Av11 intermediate checkpoint as a defined gate.
3. **Add NUnit 3→4 as a named Stage 12 deliverable**, coupled to the SIL.TestUtilities NUnit-4
   upgrade and the Avalonia 12 headless requirement; reference the existing in-repo TODO
   (`Src/Directory.Build.props:32-34`).
4. **Add an explicit Avalonia-12 breaking-change checklist** to the stage: `UseHarfBuzz()` wiring,
   clipboard/drag-drop seam rewrite (`IDataObject`→`IAsyncDataTransfer`), binding-construction
   audit, focus/gesture API migration, DevTools package swap, `Screen`-abstract usage. Cite the
   Avalonia 12 breaking-changes doc.
5. **Downgrade the "Av12-ready" principle wording to "Av12-delta-localized"** (§2 principle 14,
   §5, §9 risk row) and add an upstream exit-gate in Stages 2/11 that confines 11-only-removed
   APIs to named seams. This is the real mitigation for the late-ripple risk.
6. **Add a hard "Stage 11 shell retired" entry gate** to Stage 12 so the residual-WinForms-on-
   net10 port surface stays small; if Stage 11 slips, Stage 12 slips with it (do not start the
   port over a still-WinForms shell).
7. **Refresh `fieldworks-managed-netfx-review` in the same PR** to reflect the net48→net10 single
   target, C#8→latest, and NUnit 3→4 reality (its current net48-vs-net8 framing is stale).
8. **Add a CI invariant** asserting one TFM (net10) and one Avalonia major (12) across the whole
   solution after the bump — encodes the one-CLR rule as a test.
9. **Pin the API-compat hazard list as pre-port discovery tasks** (BinaryFormatter, System.Drawing
   Windows-only, System.Configuration, AppDomain/remoting); run `.NET Upgrade Assistant` analysis
   for the blocker report but hand-port the fixes.

## 6. Open questions / risks

- **OQ-1 (intermediate checkpoint):** Will the program adopt the **net10 + Avalonia 11.3.17**
  intermediate green checkpoint (legal because 11.3.x is netstandard2.0)? Strongly recommended —
  it decouples the two biggest risk sources. *Verify* 11.3.17 actually restores/loads on net10 in
  a spike before committing to it as a gate.
- **OQ-2 (NUnit 4 timing):** Is SIL.TestUtilities on NUnit 4 by the time Stage 12 runs? If not,
  Stage 12 is blocked on an upstream dependency for the Avalonia-12 headless requirement, or must
  carry a temporary NUnit-version split. Track this as an external dependency now.
- **OQ-3 (residual WinForms size):** How much WinForms genuinely survives into Stage 12 vs. is
  deleted in Stage 13? This determines the WinForms-on-net10 port cost and the surviving
  C++/COM-interop surface. Needs a census at Stage 11 close.
- **Risk — net48→net10 API-compat surprises (Med/High):** BinaryFormatter, System.Drawing,
  config, remoting. *Mitigation:* pre-port discovery tasks (rec. 9) + leaf-first port +
  intermediate checkpoint.
- **Risk — Av12 breaking changes ripple late (Med/Med, but under-mitigated as written):** the
  "Av12-ready" claim is partially unachievable on Av11. *Mitigation:* rec. 5 (delta-localized +
  seam-confinement exit gate upstream).
- **Risk — NUnit 3→4 churn across ~40 test projects (Med/Med):** hidden in current scope.
  *Mitigation:* rec. 3, dedicated work-stream, `ClassicAssert` aliasing.
- **Risk — Stage 11 slip drags the WinForms surface into the port (Med/High):** *Mitigation:*
  rec. 6 hard entry gate.
- **Risk — C++/CLI / reg-free COM marshalling regressions on net10 (Low/Med):** interop model is
  preserved on net10; surviving interop is mostly non-UI linguistics services by Stage 12.
  *Mitigation:* native-first build already enforced; spike the Kernel/Generic/ICU interop early.

## 7. Confidence

**High** on the governing constraint and timing: Avalonia 12 drops net48 (verified in
`Directory.Packages.props` comment + Avalonia 12 docs/discussion #18606), so the Av11→12 bump is
*physically blocked* until the net10 port — the late, coordinated, one-CLR sequencing is not just
reasonable, it is forced. **High** that the plan's "net48/net8 multi-targeting in test projects"
wording is stale: the repo is uniformly net48 (130 projects, zero net8/9/10, verified). **High**
that NUnit 3→4 is a missing, coupled sub-deliverable. **Medium** on net48→net10 *effort* (depends
on the residual-WinForms and surviving-COM census, which only Stage 11 close can size) and on
whether the "Av12-ready" discipline actually held through Stages 1-11 — both are the chief
remaining uncertainties, and both are convertible to named gates by the recommendations above.
**Recommendation: keep Stage 12 as one late epic; do NOT split into separate stages (the one-CLR
rule couples them), but sequence three internal work-streams (net10 port → Av12 bump → NUnit 4)
with a net10+Av11 intermediate checkpoint, and fix the stale test-TFM wording.**

---

### Sources (external)
- Avalonia 12 breaking changes (TFMs, removed APIs, NUnit 4, UseHarfBuzz, clipboard/drag-drop, focus): https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- Avalonia 12 release / supported platforms (net8 min, net10 recommended): https://avaloniaui.net/blog/avalonia-12 ; https://docs.avaloniaui.net/docs/supported-platforms
- "Dropping support for .NET Framework 4.x and netstandard2.0 in 12.0" (rationale, <4% telemetry): https://github.com/AvaloniaUI/Avalonia/discussions/18606
- TreeDataGrid v12 breaking changes (API stability refactor): https://docs.avaloniaui.net/controls/data-display/structured-data/treedatagrid/breaking-changes-v12
- MS: Upgrade a .NET Framework WinForms app to .NET (incremental, leaf-first): https://learn.microsoft.com/en-us/dotnet/desktop/winforms/migration/
- .NET Upgrade Assistant (analysis/blocker report for net48→modern): https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview

### Sources (in-repo, verified)
- Avalonia 11.3.17 pin + net48/netstandard2.0 rationale: `Directory.Packages.props:183-213`
- Uniform net48 (no net8) + C#8 default: `grep` over `Src/**/*.csproj` (130× net48, 0× net8/9/10); `Directory.Build.props:62`
- NUnit 3.14.0 pin + "upgrade to NUnit 4" TODO: `Directory.Packages.props:174`; `Src/Directory.Build.props:32-34`
- FwAvalonia/FwAvaloniaTests target net48: `Src/Common/FwAvalonia/FwAvalonia.csproj`, `FwAvaloniaTests/*.csproj`
- Native-first build order (COM/codegen prerequisite): `FieldWorks.proj:49` (`BuildNativeFirst`)
- Native UI/render engine still .vcxproj: `Src/views/views.vcxproj`, `Src/Kernel/Kernel.vcxproj`
