# Dialog-MVVM spike — review of the approach & scalability to more dialogs

> Spike branch: `dialog-mvvm-spike`. Purpose: prove the Stage 1.2 decision (hand-authored Avalonia
> dialogs use XAML + CommunityToolkit.Mvvm + compiled bindings, in a dedicated XAML-enabled project) is
> buildable and verifiable on the FieldWorks net48 toolchain, and assess how cheaply more dialogs can be
> created from it. First real dialog: **Tools → Options** (4 tabs, checkboxes, combos).

## 1. Verdict

**The approach is sound and the toolchain works.** The first XAML/MVVM dialog in the repo builds green
through the real `build.ps1`, and the foundation is a clean, repeatable template for the ~200-dialog
Stage-5 reservoir. The cost per new dialog is low and well-suited to mixed-experience devs + AI, with
two small, one-time foundation pieces still to add (a host-modal wrapper for coexistence, and the
localization lane). Details below.

## 2. What was built (the spike artifacts)

- **`Src/Common/FwAvaloniaDialogs/`** — the dedicated XAML-enabled project (net48, `EnableDefaultAvaloniaItems`,
  `AvaloniaUseCompiledBindingsByDefault`, references CommunityToolkit.Mvvm + the foundation `FwAvalonia`).
  - `OptionsDialogView.axaml` / `.axaml.cs` — XAML `UserControl`: `TabControl` (4 tabs) + `CheckBox`es +
    `ComboBox`es + OK/Cancel, **compiled-bound** (`x:DataType` + compiled bindings), stable `AutomationId`s.
  - `OptionsDialogViewModel.cs` — CommunityToolkit.Mvvm view-model: `[ObservableProperty]` settings,
    `[RelayCommand]` Ok/Cancel, an `Accepted` result.
- **`Src/Common/FwAvaloniaDialogs/FwAvaloniaDialogsTests/`** — headless runtime tests (compiled-binding
  both-directions, command firing, 4-tab load).
- Build wiring: `CommunityToolkit.Mvvm` pinned in `Directory.Packages.props`; the project + test added to
  `build.ps1`'s net48 Avalonia loop; both excluded from the main `FieldWorks.proj` sln-restore traversal.

## 3. Validation

- **Build / compile — PROVEN.** `build.ps1` completed **exit 0** and the Avalonia loop produced
  `Output\Debug\FwAvaloniaDialogs.dll`. So Avalonia XAML compilation + CommunityToolkit.Mvvm source
  generators + compiled bindings all build on **net48** through the customized FieldWorks build.
- **Runtime (headless) — PROVEN, 4/4 passed** (`test.ps1 -TestProject FwAvaloniaDialogsTests`, exit 0):
  compiled bindings propagate **both directions** (VM→control and control→VM); the generated
  `OkCommand`/`CancelCommand` are bound and fire (`Accepted` true/false); the compiled XAML loads all four
  tabs. So the dialog-authoring toolchain is validated **end to end** — XAML compiles *and* the MVVM
  bindings/commands work on a realized headless surface, on net48.

## 4. The integration finding that matters (and how it was solved)

The one real wrinkle was **not** the XAML compiler itself but the repo's build wiring:

- FieldWorks restores packages via **`dotnet restore FieldWorks.sln`** (solution-scoped), but the managed
  build is a **glob traversal** (`FieldWorks.proj` includes `Src\**\*.csproj`). A new project that's in the
  glob but **not in the `.sln`** has no `project.assets.json` → `NETSDK1004`.
- **Fix used:** exclude the dialog project (and its test) from the `FieldWorks.proj` traversal, and build
  them in **`build.ps1`'s dedicated net48 Avalonia loop**, which runs each Avalonia project through its own
  `MSBuild /t:Restore;Build` — *isolated from the main traversal's ILRepack/manifest steps.* This is why
  the "XAML compiler vs. customized MSBuild" worry didn't bite: the Avalonia projects already build on a
  separate, plain-MSBuild path.
- **Clean follow-up (not blocking):** add `FwAvaloniaDialogs` (+ test) to `FieldWorks.sln` so they also
  restore/build via the main path and open in Visual Studio — then the traversal exclusions can be removed.
  Until then, the Avalonia loop builds + restores them correctly.

## 5. Ability to create more dialogs from this foundation — assessment

**High. The pattern is small, repeatable, and largely mechanical.** A new dialog is three artifacts:

1. `XyzDialogView.axaml` — declarative layout (tabs/groups/fields/buttons), `x:DataType` set, `AutomationId`s.
2. `XyzDialogViewModel.cs` — `ObservableObject` with `[ObservableProperty]` state + `[RelayCommand]` actions.
3. `XyzDialogTests.cs` — headless: bindings + commands.

What makes scaling cheap:
- **Compiled bindings catch binding mistakes at *build time*** — the single biggest de-risker for AI- and
  junior-authored dialogs (a wrong property name fails the build, not silently at runtime).
- **Source generators remove boilerplate** — observable properties + commands are one attribute each.
- **Owned WS-aware controls are reusable inside XAML** — `FwMultiWsTextField`/`FwOptionPicker` from the
  foundation drop into dialogs wherever a writing-system field or a "select from a list" surface appears,
  so dialogs don't re-implement linguistic input.
- **View-models are unit-testable headlessly** — dialog logic gets real regression coverage without UI,
  which is exactly the parity evidence the migration program requires.

Turn-key foundation — **the host-modal wrapper is now DELIVERED**:
- **`AvaloniaDialogHost.ShowModal(owner, dialogBody, viewModel, title)`** (FwAvalonia) shows any dialog
  `UserControl` inside a **WinForms-owned modal `Form`** during coexistence (per `dialog-ownership.md` — no
  Avalonia `Window.ShowDialog`), returns the accepted result, and restores owner focus. The view-model
  implements **`IDialogViewModel`** (raises `CloseRequested(bool)`); OK/Cancel close the window with no
  windowing code in the VM. Avalonia init funnels through the single shared `FwAvaloniaRuntime.EnsureInitialized()`.
  So a new dialog really is **"view + VM + `ShowModal`."** Verified by the `WireClose` + VM-close-contract
  tests (7/7); the modal `ShowDialog` itself is desktop-verified (it blocks the WinForms loop, not headless).
- Still to add (one-time): **a scaffolding generator** (Stage 1.1) that emits the three files + a red test.
- **The localization lane**: the spike hardcodes English (flagged in the XAML). Real dialogs route strings
  through `FwAvaloniaStrings.resx` / the StringTable lane and add localized `AutomationProperties.Name`.

Suitability by author: **simple settings/confirmation dialogs → junior + AI** (mechanical, build-checked);
**wizards / WS-setup / project-properties → mid**; **Views-coupled dialogs (Find/Replace, Styles) → not
here** — they belong with the document-engine work (Stage 9), not the MVVM dialog kit.

## 6. Gaps / remaining before this is production-ready (not blockers to the *spike*)

| Gap | Why / where |
| --- | --- |
| ~~Host-modal wrapper~~ — **DONE** | `AvaloniaDialogHost.ShowModal` + `IDialogViewModel` + shared `FwAvaloniaRuntime` init (FwAvalonia). Verified 7/7. |
| ~~`FwAvaloniaDialogs` in `FieldWorks.sln`~~ — **DONE** | Added via `dotnet sln add`; restores + opens in VS. Kept on the Avalonia-loop build path by design. |
| Localization | Spike strings are hardcoded English; move to `.resx`/StringTable + add localized automation `Name`. |
| Real PropertyTable wiring (for Options specifically) | The Options VM is self-contained; binding it to the app-settings bus is the actual Stage-5 migration of this dialog. |
| Modernized-Fluent theme baseline | Apply the chosen Fluent look (decision §11.4) so dialogs match the upgraded chrome. |

## 7. Recommendation

Adopt this as the **Stage-5 dialog template**. Next concrete steps, in order: (1) build the reusable
**host-modal wrapper**; (2) add the project to `FieldWorks.sln`; (3) wire the **localization lane** + a
scaffolding generator. After that, the ~200-dialog reservoir is genuine parallel hand-off work — start
with Tier-A (small, Views-free) dialogs, keep Find/Replace + Styles out (Stage 9), and let compiled
bindings + headless VM tests be the per-dialog definition-of-done.
