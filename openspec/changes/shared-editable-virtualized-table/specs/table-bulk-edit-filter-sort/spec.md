## ADDED Requirements

### Requirement: Checkbox-select column

The table SHALL support an optional checkbox-select column with per-row check state plus
check-all / uncheck-all, exposed to automation, and SHALL preserve check state under virtualization
(checks survive scrolling rows out of and back into the realized window).

#### Scenario: Check state survives virtualization
- **WHEN** rows are checked and then scrolled out of and back into view
- **THEN** their check state SHALL be preserved

#### Scenario: Check-all and uncheck-all
- **WHEN** the user invokes check-all then uncheck-all
- **THEN** every row's check state SHALL be set then cleared, including de-realized rows

### Requirement: Multi-column filtering and sorting

The table SHALL support multi-column filtering and sorting equivalent to the legacy `FilterBar` /
column sort, applied through the row-source ordering/predicate so virtualization is preserved (the
filtered/sorted list never fully materializes).

#### Scenario: Filter narrows the row set lazily
- **WHEN** a column filter is applied on the 10k-row fixture
- **THEN** the visible row count SHALL reflect the filter and rows SHALL still realize only within the visible window

#### Scenario: Multi-column sort orders rows
- **WHEN** the user sorts by one column and then refines with a second
- **THEN** rows SHALL be ordered by the combined sort keys at parity with the legacy sort

### Requirement: Bulk-edit columns over a managed in-memory model

The table SHALL provide bulk-edit preview-and-apply columns equivalent to `BulkEditBar`'s
operations, backed by a managed in-memory model that replaces the legacy fake-flid
`XMLViewsDataCache` (90000000-range tags) preview/edit-storage mechanism. A census of `BulkEditBar`
features SHALL gate the work so no legacy capability is silently dropped.

#### Scenario: Bulk preview does not mutate the model
- **WHEN** a bulk-edit operation is previewed across selected rows
- **THEN** preview values SHALL be shown from the in-memory model without mutating the LCModel

#### Scenario: Bulk apply commits through the edit session
- **WHEN** a previewed bulk-edit operation is applied
- **THEN** the changes SHALL commit through the edit session as undoable changes across the affected rows

#### Scenario: Feature census gates parity
- **WHEN** the 3c work is evaluated for completion
- **THEN** a recorded census of `BulkEditBar` operations SHALL show each is covered or explicitly deferred with rationale

### Requirement: Bulk operations and filtering meet the 10k-row budget

Bulk operations and filtering SHALL operate at parity on the 10k-row production fixture within the
recorded budget, preserving virtualization throughout.

#### Scenario: Bulk operation on the 10k fixture
- **WHEN** a bulk-edit operation is applied to a large selection on the 10k-row fixture
- **THEN** it SHALL complete within the recorded budget without fully materializing the row list
