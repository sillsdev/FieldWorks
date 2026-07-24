# Running FieldWorks invisibly (headless rendering) — what works and what doesn't

> **Three senses of "headless" — don't conflate them:**
> 1. **Avalonia.Headless** in-process test rendering — works; used for the Avalonia "after" evidence.
> 2. **winforms-mcp `HEADLESS=true`** hidden desktop — does NOT render FieldWorks; do not use (abandoned; history below).
> 3. **Invisible capture generally** — see the RDP option at the end of this doc.

Empirically established 2026-06-22 on this machine. FieldWorks' main editing surface is drawn by the
native **Views (C++/COM, GDI)** engine, which requires a **display-bound desktop** to complete its first
layout/paint. Plain WinForms apps don't need this; FieldWorks does. This shapes every "invisible" option.

## Confirmed facts
- **Visible (console desktop) works.** Launch on the console desktop (which is bound to the physical
  display), then `winforms_attach_to_process` → full window + UIA tree + PrintWindow capture. Proven with
  Sena 3 (Grammar tool screenshot + complete element tree). This is the shipped default (`.mcp.json`
  `HEADLESS=false`).
- **winforms-mcp `HEADLESS=true` does NOT render FieldWorks** — with OR without a virtual display. That
  mode runs the app on a `CreateDesktop` desktop, which is **not the active/input desktop and is not bound
  to any display**. The Views engine therefore never gets a display context; the window stays blank (empty
  UIA tree, no title, flat ~460 MB memory). A virtual monitor serves the *console* desktop, not a created
  one, so a virtual display driver does not change this.
- **The bad-arg trap is separate:** launch with `-db "<project>"` ONLY (no `-app`); an unknown arg pops an
  invisible modal usage dialog with the same blank signature.

## Virtual Display Driver (VDD) — tried and abandoned
A Virtual Display Driver (`VirtualDrivers/Virtual-Display-Driver`, community, SignPath-signed) was
investigated as a way to render FieldWorks on an off-screen monitor (console desktop, invisible to the
operator). **Abandoned — do not re-attempt this path:**
- It never fixed the actual goal: winforms-mcp `HEADLESS=true` still doesn't render FieldWorks with a VDD
  installed (see above — the created desktop isn't display-bound regardless of what monitors exist).
- The only thing VDD could offer — an off-screen monitor on the console desktop — has no CLI to plug a
  monitor in VDD 25.x; it's a one-time manual GUI click in the VDD Control app, so it can't be automated
  end-to-end.
- On this dev machine specifically it's a dead end anyway: the machine is reached via **Chrome Remote
  Desktop**, which mirrors ALL monitors of the console session — so a window on the "off-screen" virtual
  monitor still shows up in the CRD session. There's nothing invisible about it here.

## If invisible capture is required
- **RDP / second session.** Run FieldWorks inside an RDP-to-localhost session (a real display-bound
  session) and disconnect it; nothing shows on your physical console. Robust isolation; heaviest setup
  (loopback RDP, keep-session-alive, force software rendering to avoid the RDP black-screen).
- Otherwise, **accept visible**: launch on the console (proven), optionally park the window off-screen;
  brief on-screen appearance; zero extra setup. This is what the skill actually uses.
