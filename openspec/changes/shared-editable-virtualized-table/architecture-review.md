# Architecture review — shared editable virtualized table + product wiring

**Reviewer:** Claude (Opus 4.8) · **Date:** 2026-06-15 · **Branch:** `editable-table`
**Scope:** the owned control (`LexicalBrowseView` + capability interfaces), the WinForms host
(`LexicalBrowseHostControl`), and the xWorks product wiring (`RecordBrowseView`,
`ClerkBrowseRowSource`, `ClerkBrowseEditContext`), as built in this change.

---

## 1. What was built (as-is)

- **Control:** one owned `ListBox`/`VirtualizingStackPanel` table with sortable headers, selection,
  keyboard nav, inline text/chooser editing, checkbox column, filter, multi-sort, bulk-edit
  preview/apply, and a row-synthesizing AutomationPeer. Capabilities are opt-in interfaces on the row
  source (`IBrowseSortSource`, `IBrowseFilterSource`, `IBrowseMultiSortSource`, `IBrowseEditSource`,
  `IBrowseBulkEditSource`).
- **Host:** `LexicalBrowseHostControl` embeds the table in WinForms via `WinFormsAvaloniaControlHost`
  (the same bridge the edit surface uses).
- **Wiring:** `RecordBrowseView` keeps the legacy `BrowseViewer` fully constructed and, when
  `UIMode=New` for `lexiconBrowse`, overlays the Avalonia table on top (legacy hidden but functional),
  forwarding row selection to `Clerk.JumpToRecord`. Cells render via `BrowseViewer.GetRowCellStrings`
  (legacy per-column finders). Editing respects the column `editable` flag and routes through a
  per-row delegating `ClerkBrowseEditContext` → `LexicalEditRegionEditContext` (the proven fenced
  write path).

## 2. Decisions reviewed

### D1 — Additive overlay (keep legacy alive) vs replace
**Verdict: correct for safety, but it is a temporary scaffold, not the end state.**
Keeping `m_browseViewer` constructed and functional underneath means all ~50 of `RecordBrowseView`'s
coupling points (selection, ctrl-tab, message targets, `PrepareToGoAway`, bulk-edit bar) keep working
untouched, and the default path is byte-for-byte unchanged. That is the right call to land safely.
**But** it builds two full browse stacks per open and leaves the Avalonia table as a *display mirror*
rather than the owner of selection/sort/filter. The end state must eventually retire the legacy
viewer for this surface (Stage-3 task 6.2 / Stage 13), at which point the coupling has to move onto
the owned table. **Recommendation:** keep the overlay now; track the coupling-migration as explicit
debt, and do not let other surfaces copy the overlay pattern as if it were the target architecture.

### D2 — Cell text via legacy `IStringFinder`s
**Verdict: good — faithful and low-effort.** Reusing the column finders means the table shows exactly
the legacy text without re-implementing the Views engine. Finders are built once and cached.
**Risk:** finder `Strings()` runs per realized cell; for fast scrolling this is per-row work. It is
bounded by the realized window (≤ ~100 rows), so acceptable, but if a finder is expensive (layout
evaluation) scroll latency could suffer — this is exactly what the 2.7 scroll-budget gate must
measure on production fixtures. **Recommendation:** measure; if a finder is hot, add a small
per-row cell cache keyed by hvo.

### D3 — Editing respects `editable`; reuse the per-entry write path
**Verdict: correct and data-safe.** The lexicon Browse columns are all `editable="false"`, so the
Avalonia table is read-only there — matching legacy exactly. Where a column *is* editable, the write
delegates to `LexicalEditRegionEditContext` (already validated for the Edit view) rather than a new
generic LCModel writer, which minimizes data risk. **Limitation (important):** `LexicalEditRegionEditContext`
only supports `Form`/`Gloss`/`MorphType`, and real browse columns are layout-based (no simple
`field` attribute), so **in practice no current browse column becomes editable.** In-product browse
editing is therefore wired but effectively dormant under shipping configs. See §4.

### D4 — One-way selection (Avalonia → clerk)
**Verdict: adequate for v1, incomplete.** Clicking a row navigates FLEx. But the clerk→Avalonia
direction is not wired, so external navigation (e.g. from a link, or the edit view changing record)
does not move the Avalonia table's highlight. **Recommendation:** wire `clerk current-record changed`
→ `host.SelectRow(index)` with a reentrancy guard (the `IRecordNavigationContext` bridge already
exists and is the right seam).

### D5 — AutomationPeer synthesizes all rows
**Verdict: correct for UIA completeness; watch allocation.** `GetOrCreateChildrenCore` builds one
peer per row (10k objects for a 10k list) on enumeration. UIA clients typically enumerate lazily, but
a naive "give me all children" call allocates the full set. **Recommendation:** if profiling shows
pressure, virtualize peer creation (create on access by index) — the synthesis logic already keys
purely on row index so this is mechanical.

## 3. Cross-cutting gaps (not regressions — unbuilt surface)

- **Sort/filter on the owned table are not connected to the clerk** in product. The table *has*
  `IBrowseSortSource`/`IBrowseFilterSource`, but `ClerkBrowseRowSource` does not implement them, so
  header-sort/filter in the overlaid table do nothing; sorting/filtering still happens through the
  (hidden) legacy filter bar, which the user can't reach. This is the biggest UX gap of the overlay.
- **Bulk-edit / checkbox** likewise not wired to the clerk in product (control-only).
- **No clerk→table selection mirror** (D4).

## 4. Recommended revisions

1. **Make `ClerkBrowseRowSource` implement `IBrowseSortSource`/`IBrowseFilterSource`** delegating to
   `Clerk.OnSorterChanged`/`Clerk.OnChangeFilter`, so the owned headers actually sort/filter. Without
   this the overlay is display-only and the hidden legacy filter bar is unreachable — the single most
   important follow-up for the overlay to be usable. *(Deferred in this change; tracked.)*
2. **Wire clerk→table selection** via `IRecordNavigationContext` with a reentrancy guard (D4).
3. **Re-scope "in-product browse editing" (6.x):** given lexicon Browse is read-only by config and the
   reuse path only covers `Form`/`Gloss`/`MorphType`, accept that the *primary* editing consumer is
   the **Edit view (already wired)** and **bulk-edit columns**, not the lexicon Browse grid. Keep
   `IBrowseEditSource` (correct, dormant) but stop treating "browse cell editing" as the headline 6.x
   deliverable — its real value is bulk-edit (3c) and the Edit view. **This is the key architectural
   correction.**
4. **Track the legacy-coupling migration** (D1) as explicit debt for the retire-legacy stage.
5. **Add a per-row cell cache** only if 2.7 scroll measurement shows finder cost (D2).

## 5. Net

The control layer is sound and well-tested. The product wiring is a **safe, faithful, read-only
overlay** — the right first landing. The honest architectural finding is that **browse-grid cell
editing is largely a non-feature for the lexicon Browse (read-only by design); the editable-table's
editing value lives in the Edit view and bulk-edit**, and the overlay's missing sort/filter/selection
wiring (not editing) is what most limits it today. Revisions 1–2 are the highest-value next steps;
revision 3 is the scope correction this review exists to make.
