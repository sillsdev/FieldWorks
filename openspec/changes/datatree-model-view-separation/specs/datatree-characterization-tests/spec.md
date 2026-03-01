## ADDED Requirements

### Requirement: Characterization tests for XML-to-slice mapping

The test suite SHALL verify that `ShowObject` with a given layout name and root object produces the correct ordered list of slices (by label, indent level, and count).

#### Scenario: Simple layout produces expected slices
- **WHEN** `ShowObject` is called with a layout containing two `<part>` references (CitationForm, Bibliography) and the object has data in both fields
- **THEN** exactly two slices are created, with labels matching the part labels in order

#### Scenario: Nested/expanded layout produces header plus children
- **WHEN** `ShowObject` is called with a layout using `<indent>` and a header node
- **THEN** the first slice is a header node and subsequent slices have the correct indent level

### Requirement: Characterization tests for show-hidden-fields toggle

The test suite SHALL verify that the show-hidden-fields mechanism correctly shows or hides `ifData`-empty and `visibility="never"` slices based on the tool-specific property key.

#### Scenario: ifData slices hidden when show-hidden is off
- **WHEN** `ShowObject` is called with `ShowHiddenFields` off and a field has no data
- **THEN** the `ifData` slice for that field is excluded from the slice list

#### Scenario: ifData slices revealed when show-hidden is on
- **WHEN** `ShowObject` is called with `ShowHiddenFields` on for the current tool
- **THEN** the `ifData` slice for an empty field is included in the slice list

#### Scenario: visibility=never slices revealed when show-hidden is on
- **WHEN** `ShowObject` is called with `ShowHiddenFields` on and a part has `visibility="never"`
- **THEN** the slice for that part is included in the slice list

#### Scenario: SliceFilter bypassed when show-hidden is on
- **WHEN** a `SliceFilter` is configured to exclude a slice by ID and `ShowHiddenFields` is on
- **THEN** the filtered slice is included in the slice list despite the filter

### Requirement: Characterization tests for slice reuse during refresh

The test suite SHALL verify that `RefreshList` reuses existing slice instances when the object and layout have not changed.

#### Scenario: Same object refresh reuses slices
- **WHEN** `ShowObject` is called, slice references are captured, and `RefreshList(false)` is called
- **THEN** at least the first slice instance in the new list is the same object reference as before

### Requirement: Characterization tests for PropChanged notification

The test suite SHALL verify that modifying a monitored property triggers a refresh of the slice list.

#### Scenario: Monitored property change triggers refresh
- **WHEN** a property registered via `MonitorProp` is changed in the cache
- **THEN** `RefreshListNeeded` becomes true or the slice list is rebuilt

### Requirement: Characterization tests for focus navigation

The test suite SHALL verify that `GotoNextSlice` and `GotoPreviousSliceBeforeIndex` navigate correctly through the slice list.

#### Scenario: GotoNextSlice advances to next focusable slice
- **WHEN** `GotoNextSlice` is called with the first slice focused
- **THEN** `CurrentSlice` moves to the next non-header, focusable slice

#### Scenario: GotoPreviousSliceBeforeIndex moves backward
- **WHEN** `GotoPreviousSliceBeforeIndex` is called with the last slice's index
- **THEN** `CurrentSlice` moves to the previous focusable slice

### Requirement: Characterization tests for DummyObjectSlice expansion

The test suite SHALL verify that sequences with more than `kInstantSliceMax` (20) items use `DummyObjectSlice` placeholders that expand on demand.

#### Scenario: Large sequence uses dummy slices
- **WHEN** `ShowObject` is called with a sequence property containing 25 items
- **THEN** some slices in the list are `DummyObjectSlice` instances (not real slices)

#### Scenario: FieldAt expands dummy to real slice
- **WHEN** `FieldAt(i)` is called on an index occupied by a `DummyObjectSlice`
- **THEN** the slice at that index becomes a real slice (`IsRealSlice == true`)
