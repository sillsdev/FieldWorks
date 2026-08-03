# Playbook: Adjust a Converted Layout by Hand

How a developer takes a generated dialog, slice, or owned control and tunes it
to the visual result they want. Companion to `migrate-a-dialog.md` and
`migrate-a-slice-type.md`; use it at their hand-implement and verify steps, or
any time afterwards.

## Set expectations first

**Avalonia has no WYSIWYG designer.** There is no equivalent of the WinForms
designer or WPF+Blend: nothing lets you drag a control, resize with the mouse,
and have the tool write the markup back. No IDE, extension, or third-party
tool provides that today.

What you get instead is a **live previewer**: the markup on one side, the
rendered surface on the other, re-rendering as you type. You see it live and
you edit by typing. That covers most of what the designer gave you; what is
missing is direct manipulation.

If you came from the WinForms designer, the habit to unlearn is coordinates.
You will never write `Location = (10, 62)` or set `Anchor`. You describe
containers and alignment -- "these lines stack with a gap; the button row docks
bottom-right" -- and the layout falls out. Your `Anchor`/`Dock` instincts
transfer; the pixel arithmetic does not.

## Where each visual property lives

The most common source of wasted time is editing the wrong file. A converted
dialog's appearance comes from four places, not one:

| What you want to change | Where it lives |
| --- | --- |
| Window size, resizability, title | The **launcher** C# (`DialogWidth`, `DialogHeight`, `Resizable`, `DialogTitle`). These are **client** pixels. NOT in the `.axaml`. |
| Structure: rows, columns, order, what docks where | The **view** `.axaml` |
| Spacing and margins | Tokens from `DialogTheme.axaml` (`DialogControlGap`, `DialogButtonStripGap`, ...). Never inline a number -- the repo bans margin literals. |
| Font size, control density, field borders | `DialogTheme.axaml` plus `CompactDialogStyles.cs`, which are kept numerically identical on purpose. Change **both**. |
| Which controls appear at all, and their text | The **view-model** (visibility flags, labels from `FwAvaloniaDialogsStrings`) |

Because fonts, spacing, and colours are centralized, "make this text smaller"
is usually a theme question rather than a per-control property. That is the
biggest working difference from the designer, where you would have set the
property on the control and moved on.

## The loop: seeing your change

Three options. Start at the top; each lower one is a fallback.

### 1. Visual Studio previewer -- live, closest to the designer

Requires the **Avalonia for Visual Studio** extension (VS 17.14+; its community
licence covers non-commercial use only -- check the terms for your situation)
or the MIT-licensed **AXAML Viewer** extension, which is free and includes a
previewer.

Two things must be true or the previewer cannot start:

- **The view needs design-time data.** Add these to the root element, plus a
  `<Design.DataContext>` naming the view-model. Both are design-time only:
  `mc:Ignorable="d"` makes the runtime parser skip the `d:` attributes, and
  Avalonia applies `Design.*` only in design mode, so neither displaces what
  the launcher sets.

  ```xml
  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  mc:Ignorable="d" d:DesignWidth="340" d:DesignHeight="200"
  ```

  `Design.DataContext` uses the view-model's parameterless preview
  constructor, so make that constructor carry representative text -- real
  wording and realistic path lengths, so wrapping looks like production.
  Without it the previewer renders an empty box.

- **The previewer needs an Avalonia host.** It boots an executable whose entry
  type exposes `static AppBuilder BuildAvaloniaApp()` or inherits
  `Application`. `FieldWorks.exe` has neither, so if Visual Studio picks it you
  get:

  ```
  Unable to create AppBuilder from type "SIL.FieldWorks.FieldWorks".
  ```

  Fix: set **`FwAvaloniaPreviewHost` as the startup project**. It is a
  `WinExe`, it has `BuildAvaloniaApp()`, and it references the dialog kit.

**Status: unverified on this repo.** The AppBuilder cause above is understood
and fixed, but whether the previewer process itself works against this
solution's net48 target and customized MSBuild has not been confirmed. If it
renders, you have the best loop available. If it does not, use option 2 --
nothing is wasted, because option 2 needs the same startup project.

### 2. Preview host -- verified working, edit/run/look

`FwAvaloniaPreviewHost` is a small Avalonia app that opens one converted
surface at its real client size with the same compact density the runtime
applies. Not live, but seconds per cycle and no FLEx launch.

```bash
Src\Common\FwAvaloniaPreviewHost\bin\Debug\net48\FwAvaloniaPreviewHost.exe --module create-feature
```

The id is a **separate argument** -- `--module <id>`, not `--module=<id>`. The
parser ignores the `=` form silently and falls back to the first registered
surface, so getting a surface you did not ask for means the flag was not
recognized. The window title is `<DisplayName> (Preview)`, which tells you
which module actually loaded. Omit `--module` entirely to get the first
surface on purpose.

**Adding your converted surface** -- one small window class plus one assembly
attribute in `Src/Common/FwAvaloniaPreviewHost/DialogPreviews.cs`:

```csharp
[assembly: FwPreviewModule("my-dialog", "My Dialog", typeof(MyDialogPreviewWindow))]

public sealed class MyDialogPreviewWindow : DialogPreviewWindow
{
    public MyDialogPreviewWindow()
        : base(new MyDialogView { DataContext = new MyDialogViewModel() },
               clientWidth: 340, clientHeight: 200, "MyDialogPreviewWindow")
    {
    }
}
```

Keep the width/height in step with the launcher's `DialogWidth`/`DialogHeight`
so what you see is what ships. `DialogPreviewWindow` applies
`CompactDialogStyles` for you -- without it the preview renders at roomy
Fluent defaults and misleads. The window needs a public parameterless
constructor: the host creates it with `Activator.CreateInstance`.

How it finds your surface: the host's `ModuleCatalog` reflects over
assembly-level `FwPreviewModule` attributes across every assembly in its own
output folder. No host code changes per surface -- registration is the
attribute. That folder is the host's own `bin\`, not the shared `Output\`,
which is why the host project references the dialog kit.

### 3. Snapshot PNGs -- always available

Run the surface's visual test and look at the PNGs in `Output/Snapshots/`:

```bash
./test.ps1 -SkipNative -TestProject FwAvaloniaDialogsTests -TestFilter "FullyQualifiedName~OptionsDialogTests"
```

About a minute per cycle. Note the window size comes from the **test**, not
the launcher, so this shows your layout but not the shipped dialog size.

## Why a converted surface is previewable at all

Worth knowing, because it doubles as a smell test. The preview needs no cache,
no WinForms, and no project data because the kit's boundaries hold:

- the view is a `UserControl`, not a `Window`, so any host can wrap it;
- the view-model is LCModel-free and takes a plain input DTO, so sample data
  is a few assignments;
- the launcher owns the LCModel work, window ownership, and help.

**A surface that cannot be previewed in a few lines has usually broken one of
those rules** -- reached for LCModel in the view-model, or assumed a modal
host. Treat difficulty here as a design signal, not a tooling problem.

## Gotchas that cost real time

- **A `MinWidth`/`MinHeight` on the view root fights the launcher's size.** If
  the root declares `MinHeight="150"` and the launcher asks for a smaller
  client height, the bottom of the button strip clips. Set the size in one
  place.
- **`TextBlock` subclasses miss the dialog font size.** Both
  `CompactDialogStyles` and `DialogTheme.axaml` select `TextBlock` by exact
  type, which does not match subclasses (`SelectableTextBlock`, `AccessText`),
  so such text renders at the Fluent default instead of the dialog size. Use
  `:is(TextBlock)` / `s.Is<TextBlock>()` if you introduce one.
- **The host window is WinForms, not Avalonia.** Title bar, icon, and close
  button belong to the modal host form. You cannot style them from the
  `.axaml`, and neither loop above shows them.
- **Converted dialogs render on the Fluent background, not WinForms grey**, and
  use the Fluent font family. That is kit-wide, not something your dialog did.
  Changing it means editing `DialogTheme.axaml`'s `fwDialogRoot` style, which
  restyles **every** converted dialog -- treat it as its own change, with dark
  mode considered.
- **Tabs, not spaces**, in `.axaml` and `.cs` alike.

## Before you call it done

1. `./build.ps1 -SkipNative` -- compiled bindings mean a mistyped binding is a
   build error, which is your first safety net.
2. Run the surface's tests. `DialogLayoutAssert.AssertNoCrowding` runs inside
   every realized-view test and fails on zero-area text, overlapping siblings,
   an unframed field host, or a root with no padding.
3. Compare against the `-before` captures for the surface.
4. Check both UI modes: the converted surface in New mode, and the legacy
   surface still untouched with the toggle off.
