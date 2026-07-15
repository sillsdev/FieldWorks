# §19 Visual (PNG) Review Notes — 2026-06-21

Direct image review of the captured Output/Snapshots PNGs (not just existence checks).
Feeds the fix sweeps. Severity: P = product defect, A = headless artifact (fine on real Windows), C = cosmetic, D = documented deferral.

| # | Surface (PNG) | Observation | Class | Action |
|---|---|---|---|---|
| V1 | FieldType-04 / -06, Region-05 | Exact-date `CalendarDatePicker` right edge shows an odd "21" + dot glyph | A | Verify on real Windows; it's the calendar-button icon (Segoe MDL2) falling back in headless Skia. If it also renders wrong in-product, replace the glyph with a text/▾ affordance. |
| V2 | Region-StText-06-rtl-khmer | Arabic (RTL) content is **left-aligned**, not right-aligned; per-WS `FlowDirection` not applied. Text is preserved verbatim (data-safe). | D | Documented "rendering-polish" deferral. Confirm it's tracked under 19c; consider applying `FlowDirection` from the run/paragraph WS as a low-cost win. |
| V3 | MediaSurface-01-integration | Picture empty-state: the "Add a picture…" placeholder text and the "Add a picture…" button overlap/duplicate. | C | Drop the placeholder when the action button is shown (or make the button the only affordance). |
| V4 | FieldType-06-integration | Long labels clip ("Allomorph Statu", "Semantic Domai"). | — (harness) | NOT a product defect — the FieldType **test harness** uses a narrow label column; Region-* surfaces render labels fully. Optionally widen the harness for cleaner snapshots. |

**Generally good:** Region detail surfaces (label/value alignment, row separators, density), the feature-structure editor (tree indent, radio values, expand chevrons, search + create-feature footer), media rows, and browse all read cleanly at WinForms-like density. Khmer complex-script glyphs render (HarfBuzz shaping).
