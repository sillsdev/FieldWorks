# Stage 13 — Final cutover, native decommission & cross-platform enablement (Epic draft)

> JIRA-ready epic + sub-epic draft for **Stage 13** of the FieldWorks → Avalonia complete
> migration program. Source of truth: `complete-migration-program.md` §6 (Stage 13 + post-review
> callout), §7 (Definition of Done), §10 (JIRA structure/labels), §11.1 (Graphite),
> §11.2 (cross-platform deferral); `reviews/stage-13-cutover-cross-platform.md`;
> `reviews/00-cross-comparison-synthesis.md` §3, §6, §7.
>
> Stage 13 is the **terminal** stage. It splits into three sequenced sub-epics whose defining
> property is **reversibility**: 13a is a reversible runtime-config flip with WinForms kept as the
> live rollback; 13b is the irreversible deletion, gated on 13a's field-bake metric; 13c is
> additive cross-platform validation gated on 13b being genuinely complete. The most dangerous
> action (deletion) must **never** share a stage with the reversible action (the flip) it rolls
> back to.

---

## Epic — Stage 13: Final cutover, native decommission & cross-platform enablement

**Summary.** Flip the global default UI to Avalonia, decommission the WinForms surface layer and
the native C++ Views render engine + render COM surface, and enable Linux/macOS build + headless +
smoke — closing the migration program.

**Type.** Epic.

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:all-prior-stages`.
(Sub-epics carry finer labels; see each story. Per program §10: `track-foundation | track-surfaces
| track-longpole | track-shell`, `lead-junior|mid|senior`, `parallel-safe`,
`parity-blocked-by:<seam>`.)

**Description.**
FieldWorks ships on Windows on .NET Framework 4.8 with two coexisting UI surfaces (WinForms +
Avalonia) selected per-tool by `LexicalEditSurfaceResolver` reading the `UIMode` setting (default
`Legacy`). Stage 13 ends the coexistence: it makes Avalonia the default, removes the strangled
WinForms + native UI/render code once the new default has proven itself in the field, and unlocks
the cross-platform reach the managed-only architecture was chosen to enable.

The stage is decomposed into three sequenced sub-epics so that the **irreversible** deletion (13b)
is gated on a **field-bake metric** from the **reversible** flip (13a), and the **additive**
cross-platform work (13c) is gated on the deletion being genuinely done (any residual Windows-only
reference fails the Linux/macOS build). Two cross-cutting stories are also tracked: the
**ViewsInterfaces split** (a hard prerequisite inside 13b — `ViewsInterfaces.cs` co-defines render
interfaces and the data-access `IVwCacheDa` used by 45+ projects, so only the render half may be
deleted) and the **dropped-script user-notification verification** (closing the Graphite/Awami
Nastaliq obligation from program §11.1).

Scope boundaries:
- **Graphite** is removed from the managed/Avalonia path in Stage 10B; **native
  `GraphiteEngineClass` deletion lands here in 13b** (deleting earlier breaks legacy surfaces still
  using it during coexistence). 13 *verifies and completes* the removal; it does not redo Stage 10's
  managed-path work.
- `retire-linux-era-view-shims` is a **narrow prerequisite that lands before** the `Src/views`
  decommission (it preserves `VwTextStore`/`IViewInputMgr`/`ManagedVwDrawRootBuffered`, which 13b
  later deletes once their consumers are gone).
- Non-UI native/linguistics services (Kernel, Generic, ICU, XAmple, encoding converters, parsers)
  and `ITsString`/`TsString` (in `Src/Kernel`, not Views) are **kept behind service seams**.

**Acceptance criteria.**
1. `UIMode` default is `New`; a mechanical gate asserts every registered tool/surface returns
   `Supported` with a green region manifest (no human checklist) before the default flip.
2. The 13a **bake metric** (duration + crash-free-session / parity-incident thresholds) is defined,
   measured, and green before any deletion begins; WinForms remains a working live rollback through
   13a.
3. `EngineIsolationAuditTests` is promoted from a single-assembly scan to a **whole-shipping-assembly
   audit** proving no surviving production assembly references `SimpleRootSite`/`RootSite`/
   `XMLViews`/`DetailControls`/Views render symbols — and it gates the start of 13b.
4. WinForms surface layer (shell, WinForms-only dialogs, DataTree/Slice, SimpleRootSite/RootSite,
   XMLViews, WinForms↔Avalonia interop spine) and native UI/render (`Src/views`,
   `ManagedVwDrawRootBuffered`, render COM `IVwRootBox`/`IVwGraphics`/`IVwEnv`) are deleted
   **leaf-first**, each deletion an independently build-green, bisectable commit.
5. `ViewsInterfaces` is **split**: data-access (`IVwCacheDa` + non-render contracts the 45+
   consumers/`CacheLight` use) is preserved behind a seam; only render interfaces are deleted.
6. Native `GraphiteEngineClass` is removed; the dropped-script list (from the Stage 9.0 G0–G3 scan)
   is documented and affected users notified with migration guidance **before** removal ships
   (program §11.1 obligation verified).
7. Linux + macOS **build** (compile-only achieved at end of Stage 12), **headless**, and **smoke**
   are green; Linux/macOS packaging exists (net-new; WiX6 does not port).
8. Final cross-cutting gates pass: accessibility (Narrator/NVDA spot-checks), localization parity,
   performance — per program §7 Definition of Done.
9. `./build.ps1` + `./test.ps1` green on Windows throughout; CI matrix green on Windows + Linux
   (+ macOS smoke) at close.

**Dependencies.** Depends on **ALL** prior stages (terminal stage). Concretely, 13b cannot start
deletion until Stages 7 (Interlinear/Discourse) and 9 (document engine) have removed the last
`SimpleRootSite`/`IVwRootBox` consumers (the 18+ project references are the live burn-down). **13c
hard-depends on Stage 12** (.NET 10; net48 is Windows-only). `retire-linux-era-view-shims` lands
before the `Src/views` decommission.

**Rough size.** XL.

---

## Sub-epics / stories

### 13a — Cutover & bake *(senior; reversible)*

**Summary.** Flip the global default to Avalonia via the `UIMode` setting using a staged/ringed
rollout; keep WinForms in the tree as the live rollback for a defined bake period.

**Type.** Sub-epic.

**Description.**
The flip is a one-property change: `LexicalEditSurfaceResolver` reads `UIMode`
(`Src/Common/FwUtils/Properties/Settings.Designer.cs`, default `[DefaultSettingValue("Legacy")]`);
flipping the default to `New` *is* the cutover. Per-tool granularity already exists
(`SupportsAvaloniaForTool`), so the rollout flips tool-by-tool, not all-at-once: opt-in →
default-on-with-easy-revert → (later) remove legacy. WinForms code stays in the tree as a
runtime-revertible rollback throughout; **no code is deleted in 13a.** A mechanical gate — a test
asserting every registered tool returns `Supported` and has a green region manifest — must block the
flip (hallucinated parity is the program's top AI risk). The bake window defines the metric that
authorizes 13b.

**Acceptance criteria.**
- `UIMode` default flipped to `New`; per-tool ringed rollout in place with instant revert.
- Mechanical "all manifests pass" gate (every tool `Supported` + green manifest) blocks the flip; no
  human checklist substitutes for it.
- Bake duration + closing metric (crash-free-session rate / parity-incident count) defined up front
  and measured; bake-green is a published, objective signal that authorizes 13b.
- WinForms remains a working live rollback for the full bake period; default-revert verified.

**Dependencies.** All prior surface/shell stages (every tool must be `Supported`). Reversible — does
not depend on 13b/13c.

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:all-manifests`.

**Rough size.** L.

---

### 13b — Decommission (WinForms + native UI/render) *(senior; irreversible)*

**Summary.** Delete the WinForms surface layer and native C++ Views render engine + render COM
surface in dependency order, **after** 13a's bake metric is green.

**Type.** Sub-epic.

**Description.**
This is the **irreversible** step and gets its own epic with its own ordered deletion runbook. Entry
gate: 13a bake-green **and** the promoted whole-shipping-assembly `EngineIsolationAuditTests` proving
no surviving production assembly references the legacy UI/render symbols. Deletion proceeds
**leaf-first**, each step an independently build-green, bisectable commit:

`consumers' WinForms surfaces → DetailControls/XMLViews → RootSite → SimpleRootSite →
ManagedVwDrawRootBuffered → ViewsInterfaces SPLIT (see story below) → Src/views (native) →
render COM interfaces`.

Repo-grounded notes: `Src/views` (44 C++ files) is the last native component built and is only a
*consumer* of Kernel/Generic — deletable once its hosts are gone. `ManagedVwDrawRootBuffered` is
referenced only by `SimpleRootSite.csproj`. `SimpleRootSite`/`RootSite` are referenced by 18+
projects (xWorks, LexEdDll, ITextDll, Discourse, MorphologyEditorDll, FdoUi, Framework, FieldWorks,
Widgets…) — all must be migrated first (Stages 7/9). **Native `GraphiteEngineClass` deletes here**
(legacy surfaces use it during coexistence; Stage 10B only removed it from the managed path). Keep
Kernel/Generic/ICU/XAmple/converters/parsers and `ITsString` (Kernel).

**Acceptance criteria.**
- Entry gate met: 13a bake-green + whole-assembly engine-isolation audit shows zero legacy UI/render
  references in surviving production assemblies.
- WinForms surface layer + native UI/render deleted in the leaf-first order; each deletion an
  independently build-green, bisectable commit.
- `ViewsInterfaces` split completed (render half deleted, data-access half preserved — see story).
- Native `GraphiteEngineClass` removed; Stage 10's managed-path Graphite removal verified still green.
- Non-UI native/linguistics services and `ITsString` preserved behind seams; `./build.ps1` +
  `./test.ps1` green on Windows after the final deletion.
- `retire-linux-era-view-shims` confirmed landed before the `Src/views` deletion.

**Dependencies.** 13a (bake-green gate). Stages 7 & 9 (last `SimpleRootSite`/`IVwRootBox` consumers
removed). Stage 5 (WinForms-only dialogs replaced by MVVM content before they can be deleted).
`retire-linux-era-view-shims` (lands first).

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:simplerootsite-consumers`.

**Rough size.** XL.

---

### ViewsInterfaces split *(senior; sub-task of 13b — hard prerequisite for render-COM deletion)*

**Summary.** Split `Src/Common/ViewsInterfaces/Views.cs` so the data-access contracts survive and
only the render interfaces are deleted.

**Type.** Story.

**Description.**
The program's original "retire the `IVwRootBox`/`IVwGraphics`/`IVwEnv` COM surface" line is **too
broad as written and would break non-UI code.** `Views.cs` co-defines the render interfaces
(`IVwRootBox` ~6627, `IVwEnv` ~10200, `IVwGraphics`) **and** `IVwCacheDa` (~4122), which is a
**data-access** cache contract implemented by `Src/CacheLight/RealDataCache.cs` and referenced by
**45+ projects**. (`ISilDataAccess` lives in `Src/Kernel/FwKernel.idh`; `ITsString`/`TsString` live
in `Src/Kernel/TextServ.idh` — both correctly preserved by "keep Kernel/Generic.") Deleting
`ViewsInterfaces` wholesale, or the render COM surface without surgically separating it from
`IVwCacheDa`, breaks the data cache and ~45 projects. This story relocates/keeps the data-access
contracts behind a service seam (a Kernel-adjacent assembly) before the render half is removed. It
is the named prerequisite for AC #5 / the render-COM deletion in 13b.

**Acceptance criteria.**
- Focused audit confirms whether render interfaces reference data-access types (decides clean-cut vs.
  refactor); result recorded.
- `IVwCacheDa` (and any non-render contracts `CacheLight`/the 45+ consumers use) relocated/kept
  behind a seam; all consumers compile against the surviving surface.
- Only render interfaces (`IVwRootBox`/`IVwEnv`/`IVwGraphics` family) remain to be deleted by 13b's
  render-COM step; `CacheLight` + the 45 projects build green.

**Dependencies.** Sequenced inside 13b, immediately before the `Src/views`/render-COM deletion.

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:viewsinterfaces-split`.

**Rough size.** L.

---

### 13c — Cross-platform enablement & final gates *(senior; additive)*

**Summary.** Stand up Linux/macOS build + headless + smoke and Linux/macOS packaging, then pass the
final accessibility/localization/performance gates.

**Type.** Sub-epic.

**Description.**
Held to the final stage by decision (program §11.2): no Linux/macOS *validation* cost is incurred
earlier in the program. Unblocked by the managed-only path + .NET 10 (Stage 12; net48 is
Windows-only). 13c is **additive net-new validation** that shares no code with the deletion but
requires it to be done — any residual Windows-only reference from a surviving surface fails the
Linux/macOS build.

De-risking the late deferral **within** the decision (not relitigating it):
- The OS-portable `Avalonia.Headless` lane runs on **Linux CI from Stage 1** (catches
  logic/binding/layout regressions OS-early; OS smoke/UIA stays deferred).
- Windows-only APIs (path separators, registry, P/Invoke, `\`) are linted as code is written so
  surviving managed code is cross-platform-clean before the OS build is attempted.
- A **compile-only Linux/macOS build** is stood up at the **end of Stage 12** so build-break
  surprises (SDK, runtime IDs, native-dep resolution) are found before the terminal stage.
- An explicit integration-and-verification sub-phase is budgeted for post-green OS smoke debugging
  (clean cross-platform builds ≠ working software).

**Packaging is net-new and unscoped today:** `FLExInstaller/wix6/` is WiX6/Windows-only and does not
port; Linux (`.deb`/`.rpm`) and macOS bundle packaging is new work. Gecko removal is an upstream
build-output/harvest-exclude change (Gecko is harvested, not named in the `.wxs`), tied to Stage 10A.

**Acceptance criteria.**
- Linux + macOS build green (built on the compile-only build standing from end of Stage 12).
- Linux + macOS headless lane green; Linux + macOS smoke green (with budgeted integration-debug
  sub-phase).
- Linux/macOS packaging produced (net-new; not a WiX edit); Windows installer unaffected.
- HarfBuzz/managed shaping coverage (Stages 9/10) verified on Linux/macOS text-rendering smoke — no
  Graphite fallback exists.
- Final gates pass: accessibility (Narrator/NVDA spot-checks), localization parity, performance — per
  program §7.

**Dependencies.** **Stage 12 (hard — .NET 10).** 13b complete (no Windows-only/native references
left). Stages 9/10 (text-shaping coverage proven).

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:net10-runtime`.

**Rough size.** L.

---

### Dropped-script user notification — verification *(senior/PM; verification story)*

**Summary.** Verify the program §11.1 obligation is met before native Graphite removal ships: the
exact dropped-script list is documented and affected users are notified with migration guidance.

**Type.** Story.

**Description.**
Decision §11.1 accepts the loss of Graphite-only scripts (notably **Awami Nastaliq**, Urdu/Arabic
Nastaliq — Graphite-only by design, no OpenType/HarfBuzz path, SIL has no OpenType replacement
planned) but incurs a **user-comms obligation**: the program must (a) run the Stage 9.0 LDML G0–G3
coverage scan to enumerate the **exact** dropped-script list + affected projects, (b) **document**
the dropped scripts, and (c) **notify affected users with migration guidance** — all **before** the
removal ships. This is a Stage 10B / Stage 13 deliverable, not optional. Since native
`GraphiteEngineClass` deletion lands in 13b, this verification story gates that deletion: the comms
must be out before the irreversible removal.

**Acceptance criteria.**
- Stage 9.0 G0–G3 scan output (exact dropped-script list + affected projects) is on file and current.
- Dropped-script loss is documented in user-facing release/migration notes.
- Affected users notified with migration guidance, on record, **before** native Graphite removal
  ships in 13b.
- Sign-off recorded that the §11.1 comms obligation is closed; this gates the 13b
  `GraphiteEngineClass` deletion step.

**Dependencies.** Stage 9.0 (G0–G3 scan). Gates the native Graphite deletion in 13b.

**Labels.** `track-shell`, `lead-senior`, `parity-blocked-by:graphite-comms`.

**Rough size.** S.

---

## Notes / open questions

**Reversible-vs-irreversible separation (the structural reason for the split).** 13a (flip) is a
runtime-config change reversible in minutes with no code deleted; 13b (delete) is destructive and
irreversible by design. Deleting code while a freshly-flipped default is still proving itself removes
the very rollback path the cutover relies on — therefore the flip and the deletion are **separate
epics**, and 13b is gated on a **field-bake metric** from 13a, not on a green CI run. 13c is additive
and gated on 13b being genuinely complete.

**Open questions / risks (from the Stage 13 review):**
- **ViewsInterfaces split feasibility (Medium confidence):** can `IVwCacheDa` separate cleanly from
  the render interfaces, or do render interfaces reference data-access types (coupling the split)?
  This decides whether the render-COM deletion is a clean cut or a refactor — needs a focused audit
  **before** 13b commits its deletion sequence.
- **Bake metric undefined until 13a:** duration + crash-free-rate / parity-incident thresholds must
  be set before 13b can claim "safe to delete." Without it, the trigger is subjective.
- **Tail-risk concentration:** Stage 13 inherits slippage from all of Stages 5–12. If any surface
  still uses `SimpleRootSite`/`IVwRootBox`, 13b stalls. The 18+ project reference list is a
  program-wide burn-down to track continuously, not at Stage 13.
- **Big-flip blast radius:** even with per-tool granularity, the *default* flip touches every user;
  the ringed rollout and instant `UIMode` revert must be real, not nominal.
- **HarfBuzz/Graphite coverage must be proven (Stages 9/10), not assumed** — else cross-platform
  text-rendering smoke fails late with no Graphite fallback.

**Cross-platform late-deferral de-risking (within the §11.2 decision, not relitigating it):**
Linux-CI headless from Stage 1 + Windows-only-API linting as code is written + a compile-only
Linux/macOS build standing at the end of Stage 12 + a budgeted integration-debug sub-phase in 13c.
This honors "no *validation* cost earlier" while moving build-break discovery off the terminal stage.
Linux/macOS **packaging remains net-new and unscoped** (WiX6 is Windows-only) — flagged as a possible
multi-week surprise to size early.
