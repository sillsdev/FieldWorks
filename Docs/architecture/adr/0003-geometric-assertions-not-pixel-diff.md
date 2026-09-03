# Visual verification uses geometric assertions + a small committed screenshot set, not automated pixel-diff

FieldWorks' Avalonia UI has no automated visual regression testing (Percy/Chromatic/
Playwright-screenshot-diff style). Instead: `DialogLayoutAssert.AssertNoCrowding` is a
deterministic, headless geometric tripwire (no sibling overlap, no zero-area or
illegibly-small text, host borders present, children inset from padded containers, dialog
root has window padding, `fwGroupBox` siblings keep their minimum token-defined gap) run
automatically on every dialog snapshot capture, backed by a curated set of representative
screenshots (one per dialog, `Docs/migration/baseline-screenshots/`) that get committed to
the repo and reviewed with the same scrutiny as a code change when updated.

**Why not pixel-diff**: real, current tooling for this (Percy, Chromatic, Playwright) is a
web/DOM-native ecosystem with no mature managed equivalent for Avalonia/WPF desktop apps,
and even mature web tooling needed a dedicated AI-review layer to suppress
anti-aliasing/font/DPI noise — a bespoke desktop pixel-diff pipeline would hit that same
noise with none of the mitigation tooling that took years to build on the web side.
Geometric assertions plus real Skia-rendered, human/AI-reviewed screenshots is close to the
realistic ceiling for this platform today, not a corner cut.

**What this deliberately does NOT catch**: real color/contrast defects (white text on a
white background passes every geometric check — nothing here reads actual rendered
pixels), and anything not present in the specific dialogs/stages captured. The geometric
checks and the screenshot review are known-incomplete by design, which is exactly why a
small set of screenshots is committed rather than only reviewed once and discarded — so a
human reviewer, not just the agent that captured it, gets a chance to actually look.

**Revisit when**: a managed, Avalonia-native pixel-diff tool with the noise-suppression
tooling web-side tools have matures, or FieldWorks' Avalonia UI grows large enough
that "an agent looks at a PNG" stops scaling as the primary visual-quality check.
