# Running FieldWorks invisibly (headless rendering) — what works and what doesn't

Empirically established 2026-06-22 on this machine. FieldWorks' main editing surface is drawn by the
native **Views (C++/COM, GDI)** engine, which requires a **display-bound desktop** to complete its first
layout/paint. Plain WinForms apps don't need this; FieldWorks does. This shapes every "invisible" option.

## Confirmed facts
- **Visible (console desktop) works.** Launch on the console desktop (which is bound to the physical
  display), then `winforms_attach_to_process` → full window + UIA tree + PrintWindow capture. Proven with
  Sena 3 (Grammar tool screenshot + complete element tree).
- **winforms-mcp `HEADLESS=true` does NOT render FieldWorks** — with OR without a virtual display. That
  mode runs the app on a `CreateDesktop` desktop, which is **not the active/input desktop and is not bound
  to any display**. The Views engine therefore never gets a display context; the window stays blank (empty
  UIA tree, no title, flat ~460 MB memory). A virtual monitor serves the *console* desktop, not a created
  one, so VDD does not change this. (This is why winforms-mcp headless still worked for plain WinForms apps
  in other projects but not for FieldWorks.)
- **The bad-arg trap is separate:** launch with `-db "<project>"` ONLY (no `-app`); an unknown arg pops an
  invisible modal usage dialog with the same blank signature.

## VDD 25.x: plugging a virtual monitor needs the GUI (tested)
The driver can be installed (device `ROOT\DISPLAY\0000`, Status OK) yet have **0 monitors plugged**. In
VDD 25.7.23, `vdd_settings.xml` `<monitors><count>` is a ceiling, NOT auto-plug: restarting the device
(`scripts/Enable-VirtualMonitor.ps1`, self-elevating disable/enable) did NOT add a monitor. The release
ships only `devcon.exe` + the Electron `VDD Control.exe` — there is **no CLI to plug a monitor**; you plug
one through the VDD Control app's UI. The winforms MCP can't drive Electron, so this is a one-time manual
step (or pick a CLI-pluggable virtual display instead — e.g. `usbmmidd`, which auto-creates a monitor with
`deviceinstaller64 enableidd 1`, no GUI/persistent process — better suited to automation).
Sequence once a monitor exists: position it off the physical screen → launch FieldWorks on the console →
move its window onto the virtual monitor → `winforms_attach_to_process` → capture.

## The Virtual Display Driver (VDD) — what it's actually for here
Installed via `scripts/Install-VirtualDisplayDriver.ps1` (source: VirtualDrivers/Virtual-Display-Driver).
It does NOT fix winforms-mcp headless. Its value is enabling the **off-screen-monitor** invisible path
below: it adds a real monitor to the **console** desktop that you position OUTSIDE your physical screen,
so a window placed there renders (console is display-bound) yet is invisible to you. The VDD device can be
present but report 0 active monitors until enabled/positioned via VDD.Control or
`C:\VirtualDisplayDriver\vdd_settings.xml`.

## Chrome Remote Desktop (CRD) caveat — important on this dev machine
If you reach the machine via **Chrome Remote Desktop**, CRD **mirrors ALL monitors** of the console
session. So the off-screen-virtual-monitor trick does NOT hide anything — the VDD monitor (and any window
on it) shows up in CRD, and plugging it can also reshuffle/shrink your CRD resolution. Net: **VDD-for-
invisibility is a dead end while you're on CRD.** Unplug it with `Enable-VirtualMonitor.ps1 -Disable`.
Under CRD the realistic choices are: capture **visibly** in the CRD session (simplest; it appears in your
CRD window briefly), or run FieldWorks in a **separate user session** CRD isn't displaying (RDP loopback —
heavier). winforms-mcp HEADLESS's CreateDesktop desktop is NOT shown by CRD (so it'd be invisible) but
FieldWorks still won't render there. The VDD off-screen path only helps a LOCAL (non-CRD) operator.

## Invisible options (ranked)
1. **Console desktop + off-screen virtual monitor (uses VDD).** Enable a VDD monitor and arrange it at
   coordinates not overlapping the physical display; launch FieldWorks on the console desktop, move its
   window onto that monitor, then attach + capture. Renders correctly (display-bound) and you don't see it.
   Setup: enable/position the VDD monitor once; add window-move-to-virtual-monitor logic to the launch.
2. **RDP / second session.** Run FieldWorks inside an RDP-to-localhost session (a real display-bound
   session) and disconnect it; nothing shows on your physical console. Robust isolation; heaviest setup
   (loopback RDP, keep-session-alive, force software rendering to avoid the RDP black-screen).
3. **Accept visible.** Launch on the console (proven), optionally park the window off-screen; brief on-
   screen appearance; zero extra setup. This is the default the skill ships (`.mcp.json` HEADLESS=false).

## Recommendation
For routine parity capture, **visible (option 3)** is simplest and proven. If invisible is required, pursue
**option 1** (VDD is already installed for it) — enable an off-screen virtual monitor and add window
placement to the launch flow. winforms-mcp `HEADLESS=true` is NOT a route to invisible FieldWorks.
