# Splash Screen (`RealSplashScreen`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.RealSplashScreen` (`Src/FwCoreDlgs/RealSplashScreen.cs`) |
| **Area** | App-wide (startup) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form→nearest |
| **JIRA** | LT-XXXXX |

## What it is
The startup splash screen the user sees while FieldWorks loads. Created and managed by `FwSplashScreen` (a non-Form `IThreadedProgress` wrapper) and runs on a separate thread; implements `IProgress`.

## Notes / gotchas
- This is a splash window, not a true modal/modeless dialog — borderless, no user input, runs on its own thread. Migration treatment differs from ordinary dialogs.
- `internal class` (not public). `FwSplashScreen` is the public wrapper/owner and is not itself a Form.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
