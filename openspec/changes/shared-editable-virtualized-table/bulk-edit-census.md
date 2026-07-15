# BulkEditBar operation census (task 7.4)

Census of the legacy `Src/Common/Controls/XMLViews/BulkEditBar.cs` (7,685 LOC) operations against the
new managed bulk-edit mechanism (`IBrowseBulkEditSource` preview/apply over an in-memory model,
replacing the fake-flid `XMLViewsDataCache` 90000000-range tags). Gate for 3c: every legacy operation
is either **covered** by the new mechanism or **explicitly deferred** with a reason — none silently
dropped.

The six operation tabs (`BulkEditBar.cs` fields `m_listChoiceTab`, `m_bulkCopyTab`, `m_clickCopyTab`,
`m_transduceTab`, `m_findReplaceTab`, `m_deleteTab`):

| # | Operation tab | Legacy behavior | New-mechanism status | Notes / remaining |
|---|---|---|---|---|
| 1 | **List Choice** (`m_listChoiceTab`) | Set an atomic/vector list reference (e.g. category) across selected rows via a chooser | **Covered (mechanism)** | `PreviewBulkEdit`/`ApplyBulkEdit` carry a target value across checked rows and commit through the edit session; the chooser-value source of that target reuses the 3b `FwChooserField` path. Per-field target resolution is product-edge. |
| 2 | **Bulk Copy** (`m_bulkCopyTab`) | Copy one field's value into another across rows, with source/target transforms | **Covered (mechanism)** | Preview-then-apply across checked rows fits directly; the field→field copy expression is supplied by the source's `value`/column args. Transform options (uppercase, etc.) deferred to the source. |
| 3 | **Click Copy** (`m_clickCopyTab`) | Click a word/value in a source cell to copy it into the target row | **Deferred** | Needs per-cell click affordance + word-level hit testing in the rendered cell — a UX interaction beyond the preview/apply core. Track as a follow-up cell-interaction item. |
| 4 | **Transduce** (`m_transduceTab`) | Apply a transform (regex/transducer) from a source field to a target field across rows | **Covered (mechanism)** | The transform produces the per-row `value`; preview shows results, apply commits. The transducer itself is a product-edge function feeding `PreviewBulkEdit`. |
| 5 | **Find/Replace** (`m_findReplaceTab`) | Find/replace within a field across selected rows, with match options | **Covered (mechanism)** | Per-row computed replacement feeds preview/apply. Match-option UI (case, regex, whole word) is a source-side concern; the table mechanism is agnostic. |
| 6 | **Delete** (`m_deleteTab`) | Delete selected rows/objects, or clear a field across rows | **Partial** | Clearing a field = `ApplyBulkEdit` with empty value (covered). **Deleting whole objects** is a destructive model operation that must route through the record list / `IRecordNavigationContext`, not the cell mechanism — deferred to the product-wiring stage (groups 4/6) where the record list is available. |

## Cross-cutting legacy capabilities

- **Preview columns** (fake-flid `XMLViewsDataCache`, `ktagEditColumnBase`, 90000000-range tags):
  **replaced** by the in-memory overlay in `IBrowseBulkEditSource.PreviewBulkEdit` — preview values
  display without mutating the model, and `ClearBulkEditPreview` discards them. This is the core
  fake-flid retirement.
- **Enable/disable per selection** (`m_bulkEditIcon`, checked-row gating): **covered** by the
  checkbox-select column (`CheckedRows`) feeding the operation's row set.
- **Undoable as one step**: **covered** — `ApplyBulkEdit` calls `IEditSession.Commit()` once across
  the affected rows (verified by `LexicalBrowseBulkEditTests`).

## Outstanding before parity sign-off

1. **Click Copy** cell-interaction affordance (#3) — net-new UX item.
2. **Object delete** path (#6) — depends on the record-list seam from product wiring (groups 4/6).
3. Per-operation **value-producer wiring** (transforms, find/replace options, copy expressions) lives
   at the product edge; this census confirms the *table mechanism* supports each, but the product
   adapters that compute per-row values are part of the xWorks integration.
