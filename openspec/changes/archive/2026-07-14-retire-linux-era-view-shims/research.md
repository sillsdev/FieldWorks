# Research: Linux-Era Views COM Shims

## Summary

`ViewInputManager` and `ManagedVwWindow` are both compatibility shims from the non-Windows/Linux/Mono era. Current Windows/x64 FieldWorks uses native Views code instead:

- Text input and IME/composition use native `VwTextStore`, which implements `IViewInputMgr`.
- RootSite window geometry for selection/page operations uses Win32 `GetClientRect`.

The managed shims are still present as COM-visible types and still influence reg-free manifest generation, build inputs, and solution/test topology. They should be retired together in a dedicated cleanup slice, not mixed into unrelated managed-provider work.

## Current Platform Policy

- `Docs/CONTRIBUTING.md` states: FieldWorks is Windows-only; Linux builds are no longer supported.
- The prior COM-reduction OpenSpec marked the Windows-first policy decision for these shims as complete, while leaving implementation tasks optional/deferred.

## ViewInputManager

### What it is

`ViewInputManager` is a managed C# COM class in `Src/Common/SimpleRootSite/ViewInputManager.cs`:

- `[ComVisible(true)]`
- CLSID `{830BAF1F-6F84-46EF-B63E-3C1BFDF9E83E}`
- Implements `IViewInputMgr`
- Source comment: Linux creates this class; Windows uses unmanaged `VwTextStore`.

Its behavior is mostly no-op, except mouse-down activation of the keyboard through the old IBus/root-site event path.

### Windows runtime evidence

`Src/views/VwRootBox.cpp` creates the input manager like this:

```text
#ifdef ENABLE_TSF
#if defined(WIN32) || defined(WIN64)
  new VwTextStore(this)
  QueryInterface(IID_IViewInputMgr, &m_qvim)
#else
  m_qvim.CreateInstance(CLSID_ViewInputManager)
#endif
#endif
```

So the managed shim is only selected for the non-Windows branch. Windows uses `VwTextStore`.

### Active interface evidence

`IViewInputMgr` itself is still active and should not be removed casually:

- `Src/views/VwTextStore.h` inherits `public IViewInputMgr`.
- `Src/views/VwTextStore.cpp` handles `IID_IViewInputMgr` in `QueryInterface`.
- `Src/views/VwRootBox.h` stores `IViewInputMgrPtr m_qvim` and exposes `InputManager()`.
- Native callers use `Root()->InputManager()` in `VwSelection.cpp` and `VwTextBoxes.cpp`.

Language-service reference lookup found references to `VwRootBox::InputManager()` in native selection/text-box code, but only the definition for the managed `ViewInputManager` class.

## ManagedVwWindow

### What it is

`ManagedVwWindow` is a managed C# COM class in `Src/ManagedVwWindow/ManagedVwWindow.cs`:

- `[ComVisible(true)]`
- CLSID `{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}`
- Implements `IVwWindow`
- Wraps an HWND-like value by calling `Control.FromHandle((IntPtr)value)`
- Returns the WinForms control client rectangle

It exists to let native code query window properties without calling Win32 directly, which mattered for cross-platform support.

### Windows runtime evidence

`Src/views/VwSelection.cpp` has two RootSite geometry paths:

```text
#if defined(WIN32) || defined(WIN64)
  GetClientRect(hwndRootSite, &rcRootSite)
#else
  CoCreate CLSID_VwWindow
  IVwWindow.Window = hwndRootSite
  IVwWindow.GetClientRectangle(...)
#endif
```

So Windows never needs `ManagedVwWindow` for page/selection geometry.

Language-service reference lookup found `ManagedVwWindow` references only in its own unit tests, not product code.

## Build and Manifest Evidence

Both shims still affect build/reg-free COM plumbing:

- `Build/mkall.targets` excludes both managed CLSIDs from native manifests:
  - `{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}` for `ManagedVwWindow`
  - `{830BAF1F-6F84-46EF-B63E-3C1BFDF9E83E}` for `ViewInputManager`
- `Build/RegFree.targets` includes `SimpleRootSite.dll` and `ManagedVwWindow.dll` in managed COM manifest inputs.
- `Src/Common/FieldWorks/BuildInclude.targets` includes `ManagedVwWindow.dll` for FieldWorks.exe manifest generation.
- `Build/Src/FwBuildTasks/FwBuildTasksTests/RegFreeCreatorTests.cs` uses the `ManagedVwWindow` CLSID as test data for excluded CLSID handling.
- `Src/views/Views.idh` still declares `IVwWindow`, `VwWindow`, and `IViewInputMgr`.
- `Src/Common/ViewsInterfaces/Views.cs` contains generated managed declarations for `IVwWindow`, `VwWindow`, and `IViewInputMgr`.

`SimpleRootSite.dll` should not be removed wholesale from managed COM manifest inputs unless a separate audit proves every remaining COM-visible type in that assembly is no longer required. The first cleanup target is the `ViewInputManager` class surface, not the entire assembly.

## Dependency Diagrams

### Windows active path

```text
FieldWorks / RootSite
  -> VwRootBox::Init()
     -> new VwTextStore(this)
     -> QueryInterface(IID_IViewInputMgr)
     -> m_qvim
        -> SetFocus / KillFocus / composition / layout notifications

Selection page sizing
  -> IVwRootSite.get_Hwnd
  -> Win32 GetClientRect
```

### Dormant non-Windows path

```text
VwRootBox::Init()
  -> CoCreate CLSID_ViewInputManager
     -> SimpleRootSite.dll!ViewInputManager
     -> old IBus/root-site keyboard bridge

VwSelection page sizing
  -> CoCreate CLSID_VwWindow
     -> ManagedVwWindow.dll!ManagedVwWindow
     -> Control.FromHandle(...).ClientRectangle
```

### Reg-free packaging today

```text
Build/RegFree.targets + BuildInclude.targets
  -> managed COM assembly scan
     -> FwUtils.dll
     -> SimpleRootSite.dll
     -> ManagedVwWindow.dll

Build/mkall.targets
  -> excludes managed shim CLSIDs from native manifests to avoid duplicate SxS entries
```

## External Docs Consulted

- Microsoft Learn, Text Services Framework: TSF is Windows text input infrastructure for Windows-based computers and COM/C++ programmers. This supports keeping native `VwTextStore` as the Windows path.
  - https://learn.microsoft.com/en-us/windows/win32/tsf/text-services-framework
- Microsoft Learn, Registration-Free COM Interop: reg-free COM uses application/component manifests instead of registry activation metadata. .NET-based COM classes must be public and have a parameterless constructor for registry-free activation.
  - https://learn.microsoft.com/en-us/dotnet/framework/interop/registration-free-com-interop
- Microsoft Learn, Exposing .NET Components to COM: unmanaged clients can activate .NET objects through COM; managed types intended for COM require public COM-facing shape and deployment metadata.
  - https://learn.microsoft.com/en-us/dotnet/framework/interop/exposing-dotnet-components-to-com
- Microsoft Learn, `Control.FromHandle(IntPtr)`: returns the WinForms control associated with an HWND, or null if no control is associated with the handle.
  - https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.fromhandle
- Microsoft Learn, `NativeWindow`: background for WinForms HWND/message wrapping; relevant only as contrast because Windows FieldWorks already uses native Win32/Views paths here.
  - https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.nativewindow

## Conclusion

Both managed shim implementations are Linux-era cruft from a product/runtime perspective. The remaining risk is not normal Windows behavior; it is cleanup correctness:

- keeping `IViewInputMgr` while removing only the managed non-Windows class,
- removing `ManagedVwWindow` without leaving stale `VwWindow` coclass/generated artifacts,
- keeping reg-free manifests accurate,
- proving native `VwTextStore`/`GetClientRect` paths still behave correctly.
