## ADDED Requirements

### Requirement: Non-view code uses a thin lifetime and dialog seam

Application lifetime, dialog ownership, shutdown requests, and non-view window coordination SHALL use a thin FieldWorks-owned lifetime seam instead of direct Avalonia lifetime calls from non-view layers.

#### Scenario: Presenter requests dialog through seam
- **WHEN** a presenter or non-view service needs to show a dialog, request shutdown, or coordinate owner windows
- **THEN** it SHALL do so through the lifetime and dialog seam rather than directly referencing Avalonia window lifetime APIs

### Requirement: Direct Avalonia lifetime remains allowed at the UI edge

Direct Avalonia lifetime APIs SHALL remain allowed in `Program`, `App`, preview-host startup, headless-test setup, and concrete window or dialog classes.

#### Scenario: App startup uses classic desktop lifetime directly
- **WHEN** the concrete Avalonia application starts or a preview host boots a top-level window
- **THEN** the concrete startup path MAY use Avalonia lifetime APIs directly at that edge

### Requirement: Full region or document lifetime frameworks are deferred

The migration SHALL NOT require a heavy region, document, or workspace lifetime framework up front; such a framework SHALL be introduced only if repeated cross-screen lifetime problems prove a thin seam insufficient.

#### Scenario: Thin lifetime seam remains default until repeated need is proven
- **WHEN** initial migrated screens and shell slices can be coordinated through the thin lifetime seam
- **THEN** the migration SHALL defer a heavier region or document lifetime framework rather than introducing it by default
