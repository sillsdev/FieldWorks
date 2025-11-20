# Investigation Plan: SimpleRootSite & Drawing Pipeline

## 1. Objectives
- Document why `SimpleRootSite` is instantiated directly today and whether COM activation is still required anywhere.
- Trace the managed drawing pipeline (SimpleRootSite → IVwDrawRootBuffered → managed/native drawers) to explain the negative-size view reports.
- Summarize current managed/native interop boundaries, registration-free COM expectations, and recent interface changes.
- Identify 32-bit vs 64-bit considerations or overflow risks that could explain coordinate underflow.

## 2. Scope & Constraints
- Read-only analysis only (no code modifications beyond temporary instrumentation proposals).
- Reference instruction files: `.github/instructions/csharp.instructions.md`, `managed.instructions.md`, `common.instructions.md`, and `managedvwdrawrootbuffered.instructions.md` for style/architecture ground truth.
- Focus on `Src/Common/SimpleRootSite`, `Src/ManagedVwDrawRootBuffered`, `Src/views/VwRootBox.cpp`, build/spec docs under `specs/003-convergence-regfree-com-coverage`, and `Build/RegFree.targets`.

## 3. Workstreams & Tasks
1. **SimpleRootSite Instantiation Audit**
   - Search for `new SimpleRootSite` / subclass creation sites; note any COM activation remnants.
   - Summarize expectations in `SimpleRootSite.MakeRoot` comments and related specs.
2. **Negative Rectangle Diagnostics**
   - Map `rcSrc`, `rcDst`, `rcpDraw`, scroll offset, and orientation manager math in `SimpleRootSite`.
   - Compare managed `VwDrawRootBuffered.DrawTheRoot` logic against native `VwRootBox.cpp` to list existing guards and missing checks.
   - Propose targeted instrumentation/logging points (without committing code yet).
3. **Interop & RegFree Story**
   - Document how COM interfaces are defined (`ViewsInterfaces`), how managed implementations get registered (RegFree manifests, `Build/RegFree.targets`), and where they are instantiated (direct `new` vs COM).
   - Capture references from `REGFREE_BEST_PRACTICES.md` and audit reports.
4. **Recent Interface Changes**
   - Review surrounding comments/history in `SimpleRootSite.MakeRoot`, specs, and PR notes to explain the shift toward direct instantiation and AnyCPU/x64 considerations.
5. **32-bit vs 64-bit Analysis**
   - Inspect structure definitions (`Rect`, `IntPtr`) and DPI/scroll math for overflow risks.
   - Outline verification steps (e.g., log rectangle edges, ensure bitmap sizes stay positive).

## 4. Deliverables
- Consolidated markdown summary (sections per workstream) citing file paths + line ranges.
- List of open questions (e.g., remaining COM clients, instrumentation strategy, repro steps for negative rectangles).
- Recommendations for next debugging steps (e.g., run FieldWorks with logging build, capture `rcDst` when width/height ≤ 0).

## 5. Timeline & Dependencies
- **Day 1**: Complete file/code review for workstreams 1–3.
- **Day 2**: Finish 32/64-bit review, synthesize findings, draft report.
- **Prerequisites**: Ensure `Output/Debug/FieldWorks.exe` run logs are available for reference; coordinate with whoever ran `.\build.ps1` last for reproducibility.
