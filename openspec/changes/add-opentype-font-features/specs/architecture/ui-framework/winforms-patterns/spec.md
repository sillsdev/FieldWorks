## ADDED Requirements

### Requirement: Font-feature controls are not Graphite-gated
WinForms Font Features controls SHALL be composed so feature availability depends on selected-font capabilities and explicit disabled states, not on the Graphite checkbox.

#### Scenario: Shared control is reused across canonical font surfaces
- **WHEN** Writing System Properties, Styles, or the shared Font dialog need font-feature selection
- **THEN** they SHALL use the shared Font Features control/provider behavior rather than duplicating Graphite or OpenType checks

#### Scenario: Explicit disabled state still wins
- **WHEN** a caller explicitly disables font-feature selection, such as through an existing always-disable flag
- **THEN** the shared Font Features control SHALL remain disabled even if the selected font has features

### Requirement: Font-feature UI changes are localization-safe
WinForms UI changes for Font Features SHALL keep user-visible strings in `.resx` resources and avoid unnecessary designer churn.

#### Scenario: Labels are renamed through resources
- **WHEN** Graphite-specific labels are made generic
- **THEN** the visible text SHALL be updated through resource files instead of hardcoded strings

#### Scenario: Designer fields remain stable unless necessary
- **WHEN** an existing designer field has a Graphite-specific internal name but only the visible label changes
- **THEN** implementation SHOULD prefer label/resource changes over broad designer renames unless the rename reduces real maintenance risk

### Requirement: Dual-technology fonts expose a clear provider toggle
WinForms font-feature surfaces SHALL expose a clear OpenType-versus-Graphite provider choice when a selected font supports both feature systems, and SHALL default to OpenType.

#### Scenario: OpenType is the default provider
- **WHEN** a Writing System, Styles, or shared Font dialog surface loads a font that exposes both OpenType and Graphite features
- **THEN** the shared Font Features control SHALL default to the OpenType provider

#### Scenario: Provider toggle semantics are consistent across shared surfaces
- **WHEN** a user switches the provider on one canonical font surface
- **THEN** the provider selection rules, labels, and availability semantics SHALL match the other shared font-feature surfaces that use the same control

#### Scenario: Enable Graphite does not replace provider choice
- **WHEN** `Enable Graphite` is toggled for renderer selection behavior
- **THEN** that control SHALL remain separate from the dual-technology feature-provider choice exposed by the Font Features UI
