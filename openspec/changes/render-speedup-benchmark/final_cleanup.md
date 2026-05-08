# Final Cleanup Review

This file captures the final review items for the render-speedup branch before the PR is considered clean. The goal is to keep the branch focused on Views rendering speed and render/image assessment, while fixing rebase cruft and tightening explanations where the current diff is ambiguous.

## Required Code Fixes

### `VwRootBox::SetTableColWidths` must force layout dirty

**Files**
- `Src/views/VwRootBox.cpp`

**Issue**
`SetTableColWidths()` changes table geometry and then calls `LayoutFull()`, but the new width/DPI layout guard can skip layout if `m_fNeedsLayout` is false and the available width has not changed.

**Best resolution**
Set `m_fNeedsLayout = true` before calling `LayoutFull()` from `SetTableColWidths()`. Add a focused native regression test if practical; otherwise call out the targeted rationale in the code review notes. This is render-speedup work, not unrelated cleanup, because the new layout guard introduced the stale-layout risk.

### Managed redraw cache must copy from the matching clipped source region

**Files**
- `Src/ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs`
- Native comparison point: `Src/views/VwRootBox.cpp`

**Issue**
`ReDrawLastDraw()` copies from cached source `(0, 0)` for every repaint clip. The native buffered redraw path copies from `(rcp.left, rcp.top)` so a partial repaint gets the matching region from the cached bitmap.

**Best resolution**
Align the managed cached redraw path with the native coordinate semantics. For disabled-view reuse, copy the requested clip from the same coordinates in the cached full-client image, or explicitly change/cache the managed buffer contract so it is impossible to reuse a full-client buffer with a clipped repaint. The safest fix is to mirror native source offsets and add a focused clipped-repaint test or diagnostic proof.

## Scope And Rebase-Cruft Fixes

### DeleteRecord routing should keep the pub/sub direction

**Files**
- `Src/FdoUi/FdoUiCore.cs`
- `Src/Common/Controls/XMLViews/XmlBrowseViewBase.cs`
- `Src/Common/Controls/XMLViews/XmlBrowseRDEView.cs`

**Issue**
The current diff moves DeleteRecord dispatch back toward obsolete `Mediator.SendMessage()` command handling and removes `XmlBrowseRDEView`'s pub/sub subscription. That is not a render-speedup feature. It appears to be rebase cruft from older branch history.

**Best resolution**
Do not revert the broader pub/sub migration direction. Fix the cruft by preserving the newer pub/sub path: deletion initiated by `DeleteUnderlyingObject()` should publish the DeleteRecord event, and the RDE browse view should handle that event through the pub/sub mechanism expected by the current architecture. Reconcile the files carefully rather than blindly reverting chunks, because there may be adjacent compile/interface changes worth keeping. Add or update a focused regression test proving RDE DeleteRecord still reaches the correct handler through pub/sub.

### VS Code task changes should be render-only

**Files**
- `.vscode/tasks.json`

**Issue**
The render baseline task belongs in this PR, but the same diff also changes general task descriptions, removes the worktree colorizer `runOn` behavior, and adds a generic `Build Tests` task. Those are workflow cleanup, not render-speedup or render-assessment work.

**Best resolution**
Keep only `Test: RenderBaselineTests` in this branch. Move the general task cleanup to the non-render cleanup PR, or drop it if it is no longer needed. The render task should remain documented as the quick targeted validation entry point.

### Unused `Verify` package version should be removed or made real

**Files**
- `Directory.Packages.props`

**Issue**
The branch adds a central `Verify` package version, but the implementation uses the custom `RenderSnapshotVerifier` and there is no matching `PackageReference Include="Verify"` in the changed projects.

**Best resolution**
Remove the unused central package version. Only keep it if the branch actually adopts the Verify package through an explicit project reference. For the current custom verifier approach, the central version pin is misleading and should not be in this PR.

### `Views_RenderTiming` switch should be wired or removed

**Files**
- `Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config`
- `Src/views/VwRenderTrace.h`

**Issue**
The dev diagnostics config adds `Views_RenderTiming`, but the native trace helper is compile-time gated by `TRACING_RENDER` and no runtime consumer of this switch was found. As written, the switch looks like it enables tracing but does not.

**Best resolution**
Either wire the switch into a real runtime trace path, or remove the config entry and mark the runtime trace-switch work as deferred. If native tracing remains compile-time only, the docs and task list should not claim that the config switch enables it.

## Documentation And Explanation Fixes

### OpenSpec task paths should match the final project layout

**Files**
- `openspec/changes/render-speedup-benchmark/tasks.md`
- `openspec/changes/render-speedup-benchmark/quickstart.md`

**Issue**
Some migrated task entries still point helper classes at `Src/Common/RootSite/RootSiteTests`, but the final implementation places shared infrastructure under `Src/Common/RenderVerification` and `Src/Common/RenderTestInfrastructure`. The quickstart also still uses the old `.csproj` `-TestProject` form and stale snapshot locations.

**Best resolution**
Update the OpenSpec docs to the actual implementation layout:
- Shared snapshot/benchmark helpers live under `RenderVerification` and `RenderTestInfrastructure`.
- RootSite test entry points remain under `Src/Common/RootSite/RootSiteTests`.
- DataTree render tests and baselines live beside `DetailControlsTests`.
- Targeted command should use the working form: `./test.ps1 -TestProject "RootSiteTests" -TestFilter "FullyQualifiedName~RenderBaselineTests"`.
- Approved snapshots are committed as `*.verified.png` beside the relevant test source, not under the old `TestData/RenderSnapshots` path.

### DataTree render validation should describe its actual coverage

**Files**
- `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeRenderTests.cs`
- `Src/Common/RenderVerification/DataTreeRenderHarness.cs`
- PR description / OpenSpec notes

**Issue**
Some comments describe the DataTree snapshots as full production layout coverage, but the harness strips known-problematic production parts and can continue after `ShowObject()` throws an `ApplicationException`. The tests are still valuable, but their coverage is not literally the full lexeme edit view.

**Best resolution**
Adjust comments and PR/OpenSpec wording to say these are production-like render baselines with documented exclusions. Keep the tests, but explain that they validate the DataTree/Slice render pipeline and selected production-like layouts, not every production part in a fully initialized FLEx shell.

### Render project boundary should be explicit

**Files**
- `Src/Common/RenderVerification/RenderVerification.csproj`
- `Src/Common/RenderTestInfrastructure/RenderTestInfrastructure.csproj`

**Issue**
Most shared verifier/reporting files physically live in `RenderVerification`, while `RenderTestInfrastructure` links many of them. That may be intentional to avoid dependencies, but it is not obvious from the file layout.

**Best resolution**
Either move the linked shared files under the project that owns them, or document the assembly boundary. A good explanation would be: `RenderTestInfrastructure` owns lightweight benchmark/snapshot helpers that tests can reference broadly, while `RenderVerification` owns DataTree/composite capture pieces that need heavier DetailControls dependencies.

### Artifact policy should be crisp

**Files**
- `.gitignore`
- `.gitattributes`
- `Src/Common/RenderVerification/RenderSnapshotVerifier.cs`
- `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTimingBaselineCatalog.cs`

**Issue**
The intended artifact contract is spread across code and repo metadata. Ignoring `*.received.png` and `*.diff.png` is correct for transient snapshot output, but marking ignored `*.received.png` as binary is confusing. `DataTreeTimingBaselines.json` is ignored and missing baselines skip timing threshold checks, so timing thresholds are advisory/local unless the baseline file is committed.

**Best resolution**
Document the contract in the quickstart and keep metadata consistent:
- Commit `*.verified.png` and any intentional `*.verified.json` metadata.
- Ignore transient `*.received.*` and `*.diff.*` artifacts.
- Remove unnecessary binary metadata for ignored received images unless there is a specific staging workflow.
- Decide whether `DataTreeTimingBaselines.json` is local advisory data or a committed guard. If local-only, say so and do not present it as a CI-enforced threshold.

### Generic macro hygiene changes should be explained, not moved

**Files**
- `Src/Generic/GenSmartPtr.h`
- `Src/Generic/UtilCom.h`
- `Src/Generic/UtilTime.h`
- `Src/views/VwRenderTrace.h`

**Issue**
The Generic utility edits look non-render-specific at first glance.

**Best resolution**
Keep them with this PR, but explain the supporting purpose: they fix two-step token-paste macro hygiene so `__LINE__` expands correctly, matching the new render trace timer macro pattern and avoiding duplicate local names when lock/timing macros are used more than once in a scope.

## Suggested Cleanup Order

1. Fix the two concrete render bugs: `SetTableColWidths()` dirty layout and managed cached redraw clip coordinates.
2. Fix DeleteRecord rebase cruft by preserving the pub/sub migration direction and adding a focused regression test.
3. Trim unrelated `.vscode/tasks.json` changes from this PR.
4. Remove unused `Verify` package pin unless the package is actually referenced.
5. Remove or wire `Views_RenderTiming`.
6. Update `tasks.md`, `quickstart.md`, and PR wording to match the final implementation and artifact policy.
7. Add a short explanation for Generic macro hygiene and the render project boundary.

## Expected End State

After cleanup, the branch should read as one coherent change set:
- Views render path avoids redundant reconstruct/layout and caches hot native render resources.
- Managed render buffering remains visually correct for partial repaints.
- Render verification and benchmark infrastructure are documented with current paths and commands.
- Rebase cruft is fixed in favor of the current pub/sub architecture.
- Non-render workflow cleanup stays in the companion cleanup PR.