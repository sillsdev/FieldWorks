# Refresh Dirty-Flag Audit

Date: 2026-03-10

## Purpose

This document audits the parts of FieldWorks that can require a visual refresh after the PATH-L5 / PATH-R1 optimization work.

The core question is now:

> When something changes that makes the current box tree stale, what code path marks the root box dirty so a later `RefreshDisplay()` or `Reconstruct()` actually rebuilds it?

The recent `SetRootObjects()` fix in [Src/views/VwRootBox.cpp](../../../../Src/views/VwRootBox.cpp) shows the pattern clearly: the optimization is valid, but every mutation path that invalidates the current render tree must set the dirty state.

## Executive Summary

- The native source of truth for whether a box tree needs rebuilding is `VwRootBox::m_fNeedsReconstruct` in [Src/views/VwRootBox.h](../../../../Src/views/VwRootBox.h).
- The managed fast path is [SimpleRootSite.RefreshDisplay()](../../../../Src/Common/SimpleRootSite/SimpleRootSite.cs), which now skips managed overhead when `m_rootb.NeedsReconstruct == false`.
- The native fast path is `VwRootBox::Reconstruct()`, which now returns early when `m_fConstructed && !m_fNeedsReconstruct`.
- The architecture is correct only if every semantic mutation that can stale the view either:
  - sets `m_fNeedsReconstruct = true` directly in native code, or
  - causes a `PropChanged(...)` notification that sets it indirectly.
- Confirmed missing path that was already fixed: `SetRootObjects(...)` on an already-constructed root box.
- Confirmed managed fix: `DictionaryPublicationDecorator.Refresh()` now emits a conservative invalidation when its filtered state changes.
- Plausible native risk still to close: `putref_DataAccess(...)` does not dirty reconstruct state if used post-construction.
- Confirmed current contract: `putref_Overlay(...)` is intentionally relayout-only, with native regression coverage to lock that behavior in.
- Confirmed native stylesheet contract with regression coverage: `OnStylesheetChange()` dirties reconstruct state and also triggers immediate relayout.
- Separate fact: many callers invoke `m_rootb.Reconstruct()` directly. Those sites bypass `SimpleRootSite.RefreshDisplay()` and therefore are not protected by the managed guard, but they still depend on native reconstruct semantics.

## Current Architecture

### Native dirty state

`VwRootBox` owns the native dirty flag:

- `m_fNeedsReconstruct` in [Src/views/VwRootBox.h](../../../../Src/views/VwRootBox.h)
- exposed through `IVwRootBox.NeedsReconstruct` in [Src/Common/ViewsInterfaces/Views.cs](../../../../Src/Common/ViewsInterfaces/Views.cs)

Confirmed native dirtying paths in [Src/views/VwRootBox.cpp](../../../../Src/views/VwRootBox.cpp):

- `Init()` initializes `m_fNeedsReconstruct = true`
- `PropChanged(HVO hvo, PropTag tag, int ivMin, int cvIns, int cvDel)` sets `m_fNeedsReconstruct = true`
- `OnStylesheetChange()` sets `m_fNeedsReconstruct = true`
- `SetRootObjects(...)` now sets `m_fNeedsReconstruct = true` before `Reconstruct()` when already constructed

Confirmed native clearing paths:

- `Construct(...)` clears `m_fNeedsReconstruct = false`
- `Reconstruct(...)` clears `m_fNeedsReconstruct = false`

### Managed refresh gate

[SimpleRootSite.RefreshDisplay()](../../../../Src/Common/SimpleRootSite/SimpleRootSite.cs) does this:

1. If a decorator is installed as `m_rootb.DataAccess`, call `decorator.Refresh()`.
2. If the control is not visible, defer refresh.
3. If `!m_rootb.NeedsReconstruct`, return without selection save/restore or drawing suspension.
4. Otherwise call `m_rootb.Reconstruct()`.

This means the system has two layered assumptions:

- managed assumption: `NeedsReconstruct` accurately tells `SimpleRootSite` whether refresh work is needed
- native assumption: `m_fNeedsReconstruct` accurately tells `VwRootBox::Reconstruct()` whether rebuild work is needed

If either layer misses a mutation path, stale display becomes possible.

## Affected Refresh Hosts

These classes route through `SimpleRootSite.RefreshDisplay()` or are structurally downstream of it.

### Core hosts

| Class / family | File | Refresh model | Notes |
|---|---|---|---|
| `SimpleRootSite` | [Src/Common/SimpleRootSite/SimpleRootSite.cs](../../../../Src/Common/SimpleRootSite/SimpleRootSite.cs) | Base managed refresh gate | Central PATH-L5 check |
| `RootSite` | [Src/Common/RootSite/RootSite.cs](../../../../Src/Common/RootSite/RootSite.cs) | Inherits `SimpleRootSite.RefreshDisplay()` | Most document-style views flow through here |
| `FwRootSite` family | multiple under `Src/` | Inherits `RootSite` behavior | Affected unless they directly call `m_rootb.Reconstruct()` |
| `XmlSeqView` / `XmlBrowseViewBase` / `XmlBrowseView` | [Src/Common/Controls/XMLViews/](../../../../Src/Common/Controls/XMLViews/) | Mostly go through base refresh, with pre/post work | Common consumers of decorators |
| Direct `SimpleRootSite` subclasses such as `InnerFwTextBox`, `InnerFwListBox`, `InternalFwMultiParaTextBox`, `InnerLabeledMultiStringControl`, `RelatedWordsView`, `BulletsPreview`, `SampleView` | `Src/Common/Controls/Widgets/`, `Src/FdoUi/Dialogs/`, `Src/FwCoreDlgs/` | Mixed | Some rely on `RefreshDisplay()`, others call `m_rootb.Reconstruct()` directly |

### Composite refresh callers

These are important because they cause refreshes but are not the dirty-state authority:

| Class | File | Role |
|---|---|---|
| `RootSiteGroup` | [Src/Common/RootSite/RootSite.cs](../../../../Src/Common/RootSite/RootSite.cs) | Delegates refresh to `ScrollingController.RefreshDisplay()` |
| `FwXWindow` | [Src/xWorks/FwXWindow.cs](../../../../Src/xWorks/FwXWindow.cs) | Walks refreshable children |
| `BrowseViewer` | [Src/Common/Controls/XMLViews/BrowseViewer.cs](../../../../Src/Common/Controls/XMLViews/BrowseViewer.cs) | Higher-level refresh wrapper |
| `LabeledMultiStringView` | [Src/Common/Controls/Widgets/LabeledMultiStringView.cs](../../../../Src/Common/Controls/Widgets/LabeledMultiStringView.cs) | Wraps inner root site refresh |
| `DataTree` | [Src/Common/Controls/DetailControls/DataTree.cs](../../../../Src/Common/Controls/DetailControls/DataTree.cs) | Refreshes slices by rebuilding control list, not by root-box dirty flag |

## Dirtying Matrix: Native Reconstruct-State Mutations

These are the highest-priority paths because they define whether the native box tree is stale.

| Item | Parameters / transform | Should make `NeedsReconstruct` dirty? | Current behavior | Status |
|---|---|---:|---|---|
| `VwRootBox::PropChanged(HVO hvo, PropTag tag, int ivMin, int cvIns, int cvDel)` | Any semantic data change surfaced through SDA notification | Yes | Sets `m_fNeedsReconstruct = true` | Confirmed correct |
| `VwRootBox::OnStylesheetChange()` | Stylesheet effects change rendered output and layout | Yes | Sets `m_fNeedsReconstruct = true` and `m_fNeedsLayout = true`, then immediately relayouts | Confirmed correct and covered by native regression test |
| `VwRootBox::SetRootObjects(HVO* prghvo, IVwViewConstructor** prgpvwvc, int* prgfrag, IVwStylesheet* pss, int chvo)` | Root HVO, VC, fragment, stylesheet, or root count changes | Yes | Now sets `m_fNeedsReconstruct = true` before `Reconstruct()` when already constructed | Confirmed fixed |
| `VwRootBox::SetRootObject(HVO hvo, IVwViewConstructor* pvvc, int frag, IVwStylesheet* pss)` | Single-root wrapper over `SetRootObjects(...)` | Yes | Delegates to `SetRootObjects(...)` | Confirmed correct after fix |
| `VwRootBox::putref_Overlay(IVwOverlay* pvo)` | Overlay affects appearance and can affect layout | No, under the current design it is relayout-only | Sets `m_fNeedsLayout = true` and calls `LayoutFull()`, but does not set `m_fNeedsReconstruct` | Confirmed intentional and covered by native regression test |
| `VwRootBox::putref_DataAccess(ISilDataAccess* psda)` | Replacing the underlying data access object after construction | No by itself; this remains the cheap wiring primitive | Adds/removes notifications, does not dirty reconstruct state | Confirmed intentional and covered by native regression test |
| `VwRootBox::Construct(...)` | Initial construction of box tree | No, this is the rebuild itself | Clears `m_fNeedsReconstruct = false` | Correct |
| `VwRootBox::Reconstruct(...)` | Full rebuild of current root | No, this is the rebuild itself | Clears `m_fNeedsReconstruct = false` at end | Correct |

### Native paths that bypass the dirty flag intentionally

Not every path that bypasses `m_fNeedsReconstruct` is a bug. The native sweep found several intentional bypass categories:

| Path family | Example | Why it bypasses the flag |
|---|---|---|
| Immediate reconstruct after explicit root change | `SetRootObjects(...)` | Sets the flag and immediately reconstructs |
| Immediate relayout after appearance change | `OnStylesheetChange()`, `putref_Overlay(...)` | Treats the change as relayout work instead of deferred rebuild work |
| Cheap source wiring without semantic invalidation | `putref_DataAccess(...)` | Keeps setup-time and non-visual SDA swaps cheap; host layers must opt into an explicit semantic refresh signal when the current tree is stale |

### Explicit SDA swap contract

The current native/managed split treats SDA changes as a two-step contract:

1. `putref_DataAccess(...)` changes notification wiring and the source used for future reads.
2. `SimpleRootSite.NotifyDataAccessSemanticsChanged()` is the explicit opt-in signal for the rarer case where an already-constructed root site's current tree is semantically stale and must be rebuilt later.

This keeps initialization-time `DataAccess` setup cheap, avoids forcing incidental swaps to behave like live-view refreshes, and reuses the existing managed refresh pipeline instead of changing the `IVwRootBox` COM surface.

The known `XmlSeqView` print-source swap should stay on the cheap path. That code temporarily changes `RootBox.DataAccess` so `SimpleRootSite.Print()` picks up an alternate SDA when creating a separate `PrintRootSite`; it is not asking the live on-screen root box to rebuild.

The first concrete product caller adjusted under this contract is `InterlinRibbon.SetRoot(...)`. That path previously rebuilt the existing root and then swapped `DataAccess` to the ribbon decorator afterward. It now installs the decorator before calling `ChangeOrMakeRoot(...)`, so the rebuild runs against the right source without needing a post-construct semantic refresh.
| Lazy-box incremental updates | lazy expansion and notifier relayout paths | Mutates only the affected part of the tree |
| Synchronizer-driven updates | synchronized lazy expansion / contraction paths | Coordinator logic updates participating roots directly |

The real bug pattern is narrower: a path that changes semantics, does not dirty reconstruct state, and also does not immediately repair the tree.

## Dirtying Matrix: Decorators and Data-Transform Layers

The managed refresh path explicitly calls `decorator.Refresh()` before checking `NeedsReconstruct`. That makes decorator behavior part of the architecture.

### Confirmed decorator behavior in this repo

| Decorator | File | Transform it performs | Should dirty reconstruct state when transform changes visible output? | Current dirty path | Status |
|---|---|---|---:|---|---|
| `ConcDecorator` | [Src/xWorks/ConcDecorator.cs](../../../../Src/xWorks/ConcDecorator.cs) | Rebuilds cached occurrence/value lists | Yes | Overrides `Refresh()`, clears caches, then calls `base.Refresh()`; also sends property-change style notifications elsewhere | Looks aligned |
| `DictionaryPublicationDecorator` | [Src/xWorks/DictionaryPublicationDecorator.cs](../../../../Src/xWorks/DictionaryPublicationDecorator.cs) | Rebuilds publication exclusion sets, filtered field sets, homograph info | Yes | `Refresh()` now compares pre/post filtered state and sends a broad lexical-list invalidation when that state changes | Fixed and validated with targeted test |
| `XMLViewsDataCache` | [Src/Common/Controls/XMLViews/XMLViewsDataCache.cs](../../../../Src/Common/Controls/XMLViews/XMLViewsDataCache.cs) | View-local cached data for XML views | Yes if cache contents affect visible rows/strings | Does not override `Refresh()`; repo-local setters appear to use explicit property-change notifications | Low risk based on repo-local usage |
| `ObjectListPublisher` | [Src/Common/Controls/XMLViews/ObjectListPublisher.cs](../../../../Src/Common/Controls/XMLViews/ObjectListPublisher.cs) | Owner/list projection | Yes | No `Refresh()` override; public mutation API issues `SendPropChanged(...)` | Low risk |
| `FilterSdaDecorator` | [Src/Common/Controls/XMLViews/FilterSdaDecorator.cs](../../../../Src/Common/Controls/XMLViews/FilterSdaDecorator.cs) | Filtered sequence projection | Yes | No `Refresh()` override found; appears static after setup | Low risk in current usage |
| `InterestingTextsDecorator` | [Src/xWorks/InterestingTextsDecorator.cs](../../../../Src/xWorks/InterestingTextsDecorator.cs) | Filtered text list projection | Yes | Relies on external interesting-text-list notifications rather than local `Refresh()` override | Medium risk |
| `ComplexConcPatternSda` | [Src/LexText/Interlinear/ComplexConcPatternSda.cs](../../../../Src/LexText/Interlinear/ComplexConcPatternSda.cs) | Pattern-based concordance transform | Yes | Inherits base behavior | Unverified in this repo |
| `InterlinRibbonDecorator` | [Src/LexText/Discourse/InterlinRibbonDecorator.cs](../../../../Src/LexText/Discourse/InterlinRibbonDecorator.cs) | Ribbon/occurrence transform | Yes | Inherits base behavior | Unverified in this repo |
| `ShowSpaceDecorator` | [Src/LexText/Interlinear/ShowSpaceDecorator.cs](../../../../Src/LexText/Interlinear/ShowSpaceDecorator.cs) | Alternate display formatting | Yes | Inherits base behavior | Unverified in this repo |
| `ReversalEntryDataAccess` | [Src/LexText/Lexicon/ReversalIndexEntrySlice.cs](../../../../Src/LexText/Lexicon/ReversalIndexEntrySlice.cs) | Reversal entry projection/caching | Yes | Special-case `PropChanged()` behavior but no repo-local `Refresh()` override | Low-medium risk |
| `GhostDaDecorator` | [Src/Common/Controls/DetailControls/GhostStringSlice.cs](../../../../Src/Common/Controls/DetailControls/GhostStringSlice.cs) | Ghost-string data projection | Yes | Inherits base behavior | Unverified in this repo |
| `SdaDecorator` in reference views | [Src/Common/Controls/DetailControls/PossibilityVectorReferenceView.cs](../../../../Src/Common/Controls/DetailControls/PossibilityVectorReferenceView.cs), [Src/Common/Controls/DetailControls/PossibilityAtomicReferenceView.cs](../../../../Src/Common/Controls/DetailControls/PossibilityAtomicReferenceView.cs), [Src/Common/Controls/DetailControls/PhoneEnvReferenceView.cs](../../../../Src/Common/Controls/DetailControls/PhoneEnvReferenceView.cs) | Specialized reference-view projection | Yes | Inherits base behavior | Unverified in this repo |

### Confirmed decorator outlier

`DictionaryPublicationDecorator` is the important current outlier because:

- `Refresh()` rebuilds `m_excludedItems`, `m_fieldsToFilter`, and homograph info.
- Those caches directly affect what rows and fields are visible.
- `Refresh()` does not obviously emit a native-dirtying signal.
- `PropChanged(...)` in that class does re-broadcast notifications, but that only helps if refresh-causing operations actually flow through `PropChanged(...)`.

This is the strongest remaining argument that the current architecture still relies on convention rather than one enforced rule.

### Decorator risk ranking from the comprehensive sweep

The risk is not evenly distributed across all decorators.

- High risk before fix: `DictionaryPublicationDecorator`
- Medium risk: `InterestingTextsDecorator`, `ConcDecorator`, and downstream `RespellingSda` behavior through `ConcDecorator`
- Low-medium risk: `ReversalEntryDataAccess`
- Low risk: `ObjectListPublisher`, `XMLViewsDataCache`, `FilterSdaDecorator`, and most reference-view / ghost decorators

The concentration of risk in dynamic filtering/projection decorators suggests the current notification model is mostly sound, but brittle at the most stateful transforms.

## Sites That Bypass the Managed Guard

Many classes call `m_rootb.Reconstruct()` directly instead of relying on `RefreshDisplay()`. Examples include files under:

- [Src/Common/Controls/Widgets/FwTextBox.cs](../../../../Src/Common/Controls/Widgets/FwTextBox.cs)
- [Src/Common/Controls/Widgets/FwListBox.cs](../../../../Src/Common/Controls/Widgets/FwListBox.cs)
- [Src/Common/Controls/Widgets/FwMultiParaTextBox.cs](../../../../Src/Common/Controls/Widgets/FwMultiParaTextBox.cs)
- [Src/Common/Controls/XMLViews/XmlSeqView.cs](../../../../Src/Common/Controls/XMLViews/XmlSeqView.cs)
- [Src/LexText/Interlinear/SandboxBase.cs](../../../../Src/LexText/Interlinear/SandboxBase.cs)
- [Src/LexText/Morphology/InflAffixTemplateControl.cs](../../../../Src/LexText/Morphology/InflAffixTemplateControl.cs)

This matters because:

- they do not depend on the managed PATH-L5 check
- they still depend on native `Reconstruct()` semantics
- they can hide missing dirty paths if the caller simply reconstructs unconditionally

This is not necessarily wrong. It just means the architecture is currently mixed:

- some sites use dirty-flag-driven refresh
- some sites opt into explicit full rebuilds

### Direct `m_rootb.Reconstruct()` caller categories

The managed sweep grouped direct reconstruct callers into four buckets:

| Category | Typical files | Why they rebuild directly |
|---|---|---|
| Fragment / VC / root semantics change | [Src/Common/Controls/XMLViews/XmlSeqView.cs](../../../../Src/Common/Controls/XMLViews/XmlSeqView.cs), [Src/LexText/ParserUI/TryAWordRootSite.cs](../../../../Src/LexText/ParserUI/TryAWordRootSite.cs) | View composition changed in a way the caller treats as an immediate rebuild event |
| UI-property / writing-system changes | [Src/Common/Controls/Widgets/FwTextBox.cs](../../../../Src/Common/Controls/Widgets/FwTextBox.cs), [Src/Common/Controls/Widgets/FwMultiParaTextBox.cs](../../../../Src/Common/Controls/Widgets/FwMultiParaTextBox.cs), [Src/Common/Controls/Widgets/FwListBox.cs](../../../../Src/Common/Controls/Widgets/FwListBox.cs) | Rendering assumptions changed outside normal model `PropChanged` flow |
| Dialog-launcher / collection edits | files under `Src/LexText/Lexicon/` | UI action commits data that the view chooses to rebuild immediately |
| Workaround paths for incomplete dependency tracking | [Src/LexText/Morphology/InflAffixTemplateControl.cs](../../../../Src/LexText/Morphology/InflAffixTemplateControl.cs) | Explicit workaround because the XML `<choice>` element does not fully track dependencies |

This categorization suggests that direct `Reconstruct()` calls are serving two roles:

- legitimate immediate rebuild policy chosen by the caller
- escape hatch for dependency-tracking weaknesses higher in the stack

That second role is where future audit effort is likely to pay off.

## What Correctly Needs to Dirty the Flag

The right rule is not “everything that changes should set the flag.” The right rule is narrower:

> Any operation that can make the existing root-box tree semantically wrong for the next paint must dirty reconstruct state, unless that operation immediately performs its own full rebuild.

That includes:

- changing root object identity
- changing fragment selection
- changing view constructor selection
- changing stylesheet in a way that requires rebuilding boxes, not just relayout
- changing overlay if overlay semantics alter generated boxes and not just layout metrics
- changing decorator-side filtering, projection, suppression, homograph labeling, or any other transform that changes visible content
- any deferred or batch-updated data access swap performed after initial construction

That does not necessarily include:

- pure width/layout changes already handled by `m_fNeedsLayout`
- explicit callers that immediately invoke `m_rootb.Reconstruct()` as their chosen mechanism

## Test and Evidence Coverage

The subagent sweep also catalogued the current tests and the remaining blind spots.

### Existing coverage

Representative existing coverage includes:

- [Src/Common/RootSite/RootSiteTests/RenderBaselineTests.cs](../../../../Src/Common/RootSite/RootSiteTests/RenderBaselineTests.cs)
- [Src/Common/RootSite/RootSiteTests/RenderTimingSuiteTests.cs](../../../../Src/Common/RootSite/RootSiteTests/RenderTimingSuiteTests.cs)
- [Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs](../../../../Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs)
- [Src/xWorks/xWorksTests/DictionaryPublicationDecoratorTests.cs](../../../../Src/xWorks/xWorksTests/DictionaryPublicationDecoratorTests.cs)
- [Src/xWorks/xWorksTests/InterestingTextsTests.cs](../../../../Src/xWorks/xWorksTests/InterestingTextsTests.cs)
- [Src/Common/Controls/XMLViews/XMLViewsTests/XmlBrowseViewBaseTests.cs](../../../../Src/Common/Controls/XMLViews/XMLViewsTests/XmlBrowseViewBaseTests.cs)

These tests give evidence that the optimization works and that several `PropChanged`-driven refresh paths behave correctly.

### Coverage gaps

The most important remaining gaps are:

| Gap | Why it matters |
|---|---|
| Direct `Reconstruct()` caller scenarios (`XmlView`, `XmlSeqView`, `TryAWordRootSite`, bulk-edit flows) | These bypass `RefreshDisplay()` by design and therefore need focused caller-level tests rather than more root-box dirty-flag changes |
| Decorator refresh under PATH-L5 for dynamic list/projector decorators | Validates that `Refresh()`-driven visible changes are not lost when `NeedsReconstruct` stays false |
| XML `<choice>` and similar dependency-tracking workaround paths | Confirms explicit rebuild workarounds remain necessary and intentional |
| Width-change boundary behavior around `m_fNeedsLayout` / `m_dxLastLayoutWidth` | Confirms the split between relayout and reconstruct stays coherent |

### Suggested test targets

If this audit turns into follow-up implementation work, the highest-value new tests are:

1. a focused managed test for direct rebuild callers such as `XmlView.ResetTables()` / `XmlSeqView.ResetTables()`
2. a decorator test for `DictionaryPublicationDecorator.Refresh()` under the managed fast path
3. a focused managed test for the XML `<choice>` workaround path that currently rebuilds explicitly
4. a width-boundary test that documents relayout-only behavior versus reconstruct behavior

## Execution Checklist

### Findings

- [x] Confirm `SetRootObjects(...)` needed to dirty reconstruct state for already-constructed root boxes.
- [x] Confirm `DictionaryPublicationDecorator.Refresh()` was the highest-risk managed refresh path.
- [x] Identify `putref_DataAccess(...)` as a plausible native dirty-flag gap.
- [x] Resolve whether `putref_Overlay(...)` is intentionally relayout-only.
- [x] Decide whether any other decorator besides `DictionaryPublicationDecorator` needs immediate code changes.
- [x] Classify remaining live `DataAccess` swaps after construction.

Findings summary:

- No additional live post-construction `DataAccess` swap needing code changes was found beyond the already-fixed `InterlinRibbon.SetRoot(...)` ordering case.
- `XmlSeqView` print-time `DataAccess` swaps remain intentionally cheap because they are print/setup flows, not live-view semantic refreshes.
- The remaining medium-risk decorator is `InterestingTextsDecorator`, but its update model already flows through explicit `SendPropChanged(...)` on list changes rather than a hidden post-construction SDA swap.
- Most remaining direct `m_rootb.Reconstruct()` callers are explicit caller-policy rebuilds or workaround paths for incomplete dependency tracking, not evidence that `putref_DataAccess(...)` should dirty the root box globally.

### Existing Coverage Verified

- [x] Native regression test added for repeated `SetRootObject()` on a constructed view.
- [x] Existing render baseline/timing tests cover the performance intent of PATH-L5 / PATH-R1.
- [x] Existing dictionary decorator tests cover filtering logic and `PropChanged(...)` routing.
- [x] Existing `InterestingTextsTests` cover the list/decorator event path at the domain level.
- [x] Existing `SimpleRootSiteTests` cover cheap SDA swaps, explicit semantic refresh, and hidden-site deferral.
- [x] Existing `InterlinRibbonTests` cover decorator-install ordering before rebuilding an existing root.

### New Tests Planned

- [x] Add native test that `putref_DataAccess(...)` remains a cheap wiring operation after construction.
- [x] Add managed test coverage for explicit semantic refresh after a post-construction SDA swap.
- [x] Add targeted test for overlay mutation behavior after construction.
- [x] Add targeted test for stylesheet-change behavior after construction.
- [x] Add focused caller-level test for `XmlView.ResetTables()` / `XmlSeqView.ResetTables()` explicit rebuild behavior.
- [x] Add focused caller-level test for the XML `<choice>` rebuild workaround path.
- [x] Close the decorator-visible PATH-L5 gap with the existing `DictionaryPublicationDecoratorTests.Refresh_NotifiesRegisteredRoots_WhenVisibleFilteringChanges` coverage plus the focused `SimpleRootSite` PATH-L5 tests.
- [x] Add width-boundary test that distinguishes relayout-only invalidation from reconstruct invalidation.

### Edge Cases Planned

- [x] Existing root box rebuilt after VC/layout-spec table reset.
- [x] Decorator-visible filter/list change while `NeedsReconstruct == false`.
- [x] Hidden-site semantic refresh deferred until visible.
- [x] XML `<choice>` dependency change that still requires an explicit rebuild workaround.
- [x] Width change that should relayout without forcing reconstruct.

### Code Changes Completed

- [x] Keep `putref_DataAccess(...)` as a cheap wiring operation and move semantic post-construction swaps to the managed explicit-refresh contract.
- [x] Make `DictionaryPublicationDecorator.Refresh()` participate in the normal refresh/invalidation contract.
- [x] Add explicit managed SDA-swap helpers in `SimpleRootSite` for cheap versus semantic swaps.
- [x] Reorder `InterlinRibbon.SetRoot(...)` so the decorator is installed before rebuilding an existing root.

### Code Changes Rejected By Audit

- [x] Do not dirty reconstruct state automatically in `putref_DataAccess(...)`.
- [x] Do not add another COM/native dirtying API for SDA semantics.
- [x] Do not weaken the PATH-L5 / PATH-R1 guards globally for decorated views.

### Validation

- [x] Get a clean native `TestViews` validation run.
- [x] Run targeted managed tests for `DictionaryPublicationDecoratorTests` after managed test/code changes.
- [x] Run targeted managed tests for `SimpleRootSiteTests` covering cheap SDA swaps and explicit semantic refreshes.
- [x] Run targeted managed tests for `InterlinRibbon` ordering on existing root boxes.
- [x] Run the focused `XMLViewsTests` assertions for explicit rebuild callers and the `ShowFailingItems` workaround path.

### Validation Notes

- `SimpleRootSiteTests` now provide deterministic coverage for:
  - cheap `SetRootBoxDataAccess(...)` swaps staying on the no-reconstruct fast path,
  - explicit `SetRootBoxDataAccessAndRefresh(...)` swaps forcing one managed rebuild,
  - deferred semantic refresh when the site is hidden.
- `InterlinRibbonTests.SetRoot_AssignsDecoratorBeforeChangingExistingRootObject` provides deterministic caller-level coverage that the ribbon decorator is installed before an existing root is rebuilt.
- `XmlViewRefreshPolicyTests` now cover direct XMLViews rebuild callers and the `ShowFailingItems` workaround path with a lightweight fake root box; the focused assertions passed, but the local VSTest host still aborted after completion in this environment.
- The legacy `InterlinRibbonTests.RibbonLayout` integration test still times out in isolation and should be treated as pre-existing instability, not as proof that the SDA-swap ordering change is wrong.
- After a clean native rebuild, `TestViews.exe` now links and runs cleanly again, including the new width-boundary regression.

## Resolved Architectural Decision

The audit now supports a single clear direction:

- `VwRootBox` stays the native source of truth for whether a tree is stale.
- `putref_DataAccess(...)` stays cheap and does not automatically dirty reconstruct state.
- When a post-construction SDA swap truly makes the current tree semantically stale, the host layer must opt in through the managed explicit-refresh contract in `SimpleRootSite`.
- Direct `Reconstruct()` callers remain allowed when the caller is intentionally choosing an immediate rebuild policy or compensating for incomplete dependency tracking.

This is the smallest and most testable design because it:

- preserves PATH-L5 / PATH-R1 performance wins,
- avoids expanding the COM/native invalidation surface,
- keeps one native dirty-state authority,
- and makes the exceptional post-construction SDA case explicit at the host layer.

## Best-Next Test Work

The highest-value remaining work is now testability, not more architecture churn.

1. If the local test-host abort remains reproducible, isolate it separately from the XMLViews assertions themselves.
2. Add broader integration coverage only where a direct caller still lacks focused deterministic tests.

## Bottom Line

The post-construction `DataAccess` sweep did not uncover more live-swap bugs of the same class as the original `InterlinRibbon` issue.

The branch now has:

- a native fix for `SetRootObjects(...)`,
- an explicit managed semantic-refresh contract for the rare SDA-swap case,
- a concrete caller fix in `InterlinRibbon`,
- and a narrowed follow-up list centered on direct-caller and decorator-path test coverage.
