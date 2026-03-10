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
- Architectural decision still to document with tests: whether `putref_Overlay(...)` is intentionally relayout-only or should sometimes dirty reconstruct state.
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
| `VwRootBox::OnStylesheetChange()` | Stylesheet effects change rendered output and layout | Yes | Sets `m_fNeedsReconstruct = true` and `m_fNeedsLayout = true` | Confirmed correct |
| `VwRootBox::SetRootObjects(HVO* prghvo, IVwViewConstructor** prgpvwvc, int* prgfrag, IVwStylesheet* pss, int chvo)` | Root HVO, VC, fragment, stylesheet, or root count changes | Yes | Now sets `m_fNeedsReconstruct = true` before `Reconstruct()` when already constructed | Confirmed fixed |
| `VwRootBox::SetRootObject(HVO hvo, IVwViewConstructor* pvvc, int frag, IVwStylesheet* pss)` | Single-root wrapper over `SetRootObjects(...)` | Yes | Delegates to `SetRootObjects(...)` | Confirmed correct after fix |
| `VwRootBox::putref_Overlay(IVwOverlay* pvo)` | Overlay affects appearance and can affect layout | Possibly layout-only, depending on whether overlay changes box generation or only appearance/layout metrics | Currently sets `m_fNeedsLayout = true` and calls `LayoutFull()`, but does not set `m_fNeedsReconstruct` | Needs explicit architectural decision and targeted test |
| `VwRootBox::putref_DataAccess(ISilDataAccess* psda)` | Replacing the underlying data access object after construction | Yes if used post-construct | Adds/removes notifications, does not dirty reconstruct state | Potential risk path; likely safe in current usage because callers typically set root object immediately after |
| `VwRootBox::Construct(...)` | Initial construction of box tree | No, this is the rebuild itself | Clears `m_fNeedsReconstruct = false` | Correct |
| `VwRootBox::Reconstruct(...)` | Full rebuild of current root | No, this is the rebuild itself | Clears `m_fNeedsReconstruct = false` at end | Correct |

### Native paths that bypass the dirty flag intentionally

Not every path that bypasses `m_fNeedsReconstruct` is a bug. The native sweep found several intentional bypass categories:

| Path family | Example | Why it bypasses the flag |
|---|---|---|
| Immediate reconstruct after explicit root change | `SetRootObjects(...)` | Sets the flag and immediately reconstructs |
| Immediate relayout after appearance change | `OnStylesheetChange()`, `putref_Overlay(...)` | Treats the change as relayout work instead of deferred rebuild work |
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
| Repeated `SetRootObject()` / `SetRootObjects()` after construction | Confirms the recent native fix with a focused regression test |
| Overlay mutation after construction | Clarifies whether overlay changes are truly layout-only or should dirty reconstruct state |
| Decorator `Refresh()` under PATH-L5 | Validates that a decorator-visible change is not lost when `NeedsReconstruct` stays false |
| Stylesheet-change dirtying / relayout expectations | Verifies the current `OnStylesheetChange()` behavior matches intended architecture |
| Width-change boundary behavior around `m_fNeedsLayout` / `m_dxLastLayoutWidth` | Confirms the split between relayout and reconstruct stays coherent |

### Suggested test targets

If this audit turns into follow-up implementation work, the highest-value new tests are:

1. a focused native or integration test for repeated `SetRootObject()` after a first construct
2. an overlay-mutation test that asserts whether a full reconstruct is or is not expected
3. a decorator test for `DictionaryPublicationDecorator.Refresh()` under the managed fast path
4. a stylesheet-change test that documents whether relayout-only behavior is intentional

## Execution Checklist

### Findings

- [x] Confirm `SetRootObjects(...)` needed to dirty reconstruct state for already-constructed root boxes.
- [x] Confirm `DictionaryPublicationDecorator.Refresh()` was the highest-risk managed refresh path.
- [x] Identify `putref_DataAccess(...)` as a plausible native dirty-flag gap.
- [ ] Resolve whether `putref_Overlay(...)` is intentionally relayout-only.
- [ ] Decide whether any other decorator besides `DictionaryPublicationDecorator` needs immediate code changes.

### Existing Coverage Verified

- [x] Native regression test added for repeated `SetRootObject()` on a constructed view.
- [x] Existing render baseline/timing tests cover the performance intent of PATH-L5 / PATH-R1.
- [x] Existing dictionary decorator tests cover filtering logic and `PropChanged(...)` routing.

### New Tests Planned

- [ ] Add native test that `putref_DataAccess(...)` marks a constructed root box dirty.
- [x] Add managed test that `DictionaryPublicationDecorator.Refresh()` notifies registered listeners after state changes.
- [ ] Add targeted test for overlay mutation behavior after construction.
- [ ] Add targeted test for stylesheet-change behavior after construction.

### Code Cleanups Planned

- [ ] Dirty reconstruct state when `putref_DataAccess(...)` is called on a constructed root box.
- [x] Make `DictionaryPublicationDecorator.Refresh()` participate in the normal refresh/invalidation contract.

### Validation

- [ ] Run native `TestViews` after native test/code changes.
- [x] Run targeted managed tests for `DictionaryPublicationDecoratorTests` after managed test/code changes.

## Systematic Architectural Assessment

### What the current design gets right

- `VwRootBox` is the correct place for the native source of truth.
- `NeedsReconstruct` is the correct kind of signal for the managed fast path.
- `m_fNeedsLayout` and `m_dxLastLayoutWidth` are a good example of a coherent lower-level dirty contract.

### What is still weak

The current reconstruct contract is distributed across three different conventions:

1. native mutation methods set `m_fNeedsReconstruct = true`
2. SDA notifications eventually become `PropChanged(...)`, which sets it
3. decorators are expected to mutate in a way that eventually flows into one of the above

That means the architecture is only as safe as the least careful caller. Missing one path creates silent stale-display risk that is hard to test and hard to notice.

### The correct architecture

The cleanest architecture is:

- `VwRootBox` remains the only authority on whether the current box tree is stale.
- Any operation that invalidates the current box tree must notify `VwRootBox` through one clearly defined mechanism.
- Managed code should consume the signal, not infer it independently.
- Decorators should not rely on unconditional reconstruction as a safety net.

In practice, that means one of two architectures is preferable:

1. **Notification architecture**: every semantic change flows into `PropChanged(...)` or an equivalent native invalidation API.
2. **Explicit refresh contract architecture**: decorators and managed refresh participants explicitly report “I changed visible state” and the root box is dirtied from that contract.

The current system is mostly using architecture 1, with some implicit dependence on convention.

## Three Paths Forward

### Path 1: Stay with the current architecture and close every missing dirty path

This means:

- keep `NeedsReconstruct` and PATH-L5 / PATH-R1 exactly as designed
- audit all native mutation paths and decorator refresh paths
- fix any path that mutates visible state without dirtying reconstruct state

Examples:

- keep the new `SetRootObjects()` fix
- review `putref_Overlay(...)`
- review `putref_DataAccess(...)`
- fix `DictionaryPublicationDecorator.Refresh()` to trigger the right notification path

**Pros**

- Smallest architectural change
- Preserves current performance work with minimal disruption
- Keeps the source of truth in `VwRootBox`
- No new public contract if existing notification paths are sufficient

**Cons**

- Requires careful audit discipline
- Easy to miss future paths
- No compile-time enforcement for decorator authors
- Silent stale-display bugs remain possible if somebody forgets one path later

### Path 2: Add an explicit managed-side “visual state changed” contract for decorators

This means:

- decorator refresh paths explicitly report whether they changed visible state
- managed refresh code uses that signal to request reconstruct state before the guard check
- the contract becomes visible and reviewable, instead of implicit

Possible shapes:

- `Refresh()` returns `bool`
- new interface such as `IReportsVisualRefresh`
- explicit dirty callback from decorator to root site/root box

**Pros**

- Makes decorator refresh semantics explicit
- Easier to review and reason about than “maybe PropChanged happens somewhere underneath”
- Good fit for the actual risky area discovered so far

**Cons**

- New API surface and coordination cost
- If only managed code knows about the change, native PATH-R1 still needs a clean way to learn it
- Risks creating two parallel dirtying systems if not designed carefully

### Path 3: Use a conservative fallback for decorated views or uncertain mutation paths

This means:

- if a root site has a decorator, or if a mutation path is not proven safe, avoid the fast skip and reconstruct conservatively
- treat this as a safety-oriented policy rather than a long-term architecture

**Pros**

- Lowest stale-display risk immediately
- Very simple to implement
- Good temporary safety net while the audit is still in progress

**Cons**

- Gives back part of the performance win exactly where many complex views live
- Masks contract problems instead of fixing them
- Hard to know when it is safe to remove without completing the deeper audit

## Recommendation

Recommend **Path 1 as the target architecture**, with a **temporary Path 3 safety net only if needed for confidence while auditing**.

Reasoning:

- The current design is fundamentally sound if `VwRootBox` remains the single source of truth.
- The recent `SetRootObjects()` fix is evidence that the architecture works once missing dirty paths are repaired.
- Adding a second, parallel dirtying authority in managed code should be avoided unless the decorator audit proves the notification model is too fragile to maintain.

### Concrete next steps

1. Resolve whether `putref_Overlay(...)` is intentionally relayout-only or should sometimes dirty reconstruct state.
2. Decide whether `putref_DataAccess(...)` should dirty reconstruct state when used after construction.
3. Audit every decorator used in complex document views, with `DictionaryPublicationDecorator` first.
4. Add focused regression tests for repeated `SetRootObject()`, overlay mutation, and decorator refresh under PATH-L5.
5. If a decorator changes visible state without a notification path, fix that path instead of weakening the guard globally.
6. Only if the decorator audit finds repeated breakage should the codebase adopt an explicit decorator refresh contract.

## Bottom Line

The correct architectural answer is not to weaken `NeedsReconstruct`. It is to make the dirty-state contract complete.

The branch has already shown one missing path in `SetRootObjects()`. The remaining work is to finish the same audit mindset across decorators and any other native mutation APIs that can change what the current box tree should display.
