# Tasks: PowerShell MCP Server

**Input**: Design documents from `/specs/009-powershell-mcp-server/`
**Prerequisites**: plan.md (required), spec.md (required); research.md, data-model.md, contracts/ (not present)

Tests are included for critical routing/error handling (per constitution and FR-009). Other tests remain optional.

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create `scripts/mcp/` package skeleton (`__init__.py`, `server.py`, `ps_tools.py`, `config.py`).
- [X] T002 [P] Add Python dependency manifest `scripts/mcp/requirements.txt` with MCP JSON-RPC library and pin versions.
- [X] T003 [P] Stub developer quickstart skeleton in `specs/009-powershell-mcp-server/quickstart.md` (run/start instructions placeholder).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core MCP server plumbing; blocks all user stories.

- [X] T004 Define tool allowlist/denylist, timeouts, and working-directory rules in `scripts/mcp/config.py`.
- [X] T005 Implement dynamic discovery of `scripts/Agent/*.ps1` plus `build.ps1` and `test.ps1` in `scripts/mcp/ps_tools.py`.
- [X] T006 Implement PowerShell invocation with structured outputs (`stdout`, `stderr`, `exitCode`) and non-zero handling in `scripts/mcp/ps_tools.py` with defaults (timeout 120s, output cap 1MB) and config overrides.
- [X] T007 Wire MCP JSON-RPC server to register discovered tools and route executions in `scripts/mcp/server.py`.
- [X] T008 [P] Add unit tests for discovery/argument mapping/error reporting in `tests/mcp/test_ps_tools.py`.
- [X] T009 [P] Add JSON-RPC execution tests (success + non-zero exit propagation) in `tests/mcp/test_server.py`.
- [X] T010 Integrate workspace MCP client config (e.g., `mcp.json` or `.vscode/settings.json`) pointing to the new server.
- [X] T010a Document config overrides (tool allow/deny, timeouts, output cap, working dir, URL/port) in `specs/009-powershell-mcp-server/quickstart.md` and ensure defaults are listed.

**Checkpoint**: Foundation ready—user stories may proceed.

---

## Phase 3: User Story 1 - Safe Terminal Operations (Priority: P1)  MVP

**Goal**: Expose safe read-only git/file commands via MCP to avoid manual terminal use.
**Independent Test**: Invoke `git_search` and `read_file_content` via MCP and receive correct text without protocol errors.

### Implementation
- [X] T011 [US1] Add safe tool definitions (git log/diff/show/status, file read/search) to allowlist in `scripts/mcp/config.py`.
- [X] T012 [P] [US1] Implement argument schemas and execution mapping for Git-Search and Read-FileContent wrappers in `scripts/mcp/ps_tools.py`.
- [X] T013 [P] [US1] Add integration tests for git and file tools in `tests/mcp/test_us1_safe_ops.py`.
- [X] T014 [US1] Document US1 usage in `specs/009-powershell-mcp-server/quickstart.md` (MCP call examples).

**Checkpoint**: US1 independently testable via MCP.

---

## Phase 4: User Story 2 - Build and Test Execution (Priority: P2)

**Goal**: Trigger build/test through MCP while respecting container/worktree routing.
**Independent Test**: Call MCP `build_project` and `run_tests` and verify correct script invocation (container-aware when applicable).

### Implementation
- [X] T015 [US2] Add `build.ps1` and `test.ps1` tool definitions with arguments to `scripts/mcp/config.py`.
- [X] T016 [P] [US2] Ensure invocation preserves wrapper logic (no direct msbuild/test) in `scripts/mcp/ps_tools.py`.
- [X] T017 [P] [US2] Add integration tests for build/test tool routing (dry-run or no-build mode) in `tests/mcp/test_us2_build_test.py`.

**Checkpoint**: US2 independently testable via MCP.

---

## Phase 5: User Story 3 - Copilot Maintenance (Priority: P3)

**Goal**: Run Copilot maintenance commands (detect/plan/apply) via MCP without complex shell chains.
**Independent Test**: Invoke `copilot_detect_updates` via MCP and receive parsed JSON output.

### Implementation
- [X] T018 [US3] Add Copilot maintenance tool definitions (detect/plan/apply/validate) to `scripts/mcp/config.py`.
- [X] T019 [P] [US3] Map parameters and execution for Copilot tools (Python/PowerShell helpers) in `scripts/mcp/ps_tools.py`.
- [X] T020 [P] [US3] Add integration tests for Copilot detection call (mocked or harmless mode) in `tests/mcp/test_us3_copilot.py`.

**Checkpoint**: US3 independently testable via MCP.

---

## Phase N: Polish & Cross-Cutting Concerns

- [X] T021 [P] Finalize MCP client config (`mcp.json` or `.vscode/settings.json`) and document start/stop in `specs/009-powershell-mcp-server/quickstart.md`.
- [X] T022 [P] Update `.github/instructions/terminal.instructions.md` to prefer MCP server tools over raw terminal equivalents.
- [X] T023 [P] Add usage examples and troubleshooting to `specs/009-powershell-mcp-server/quickstart.md` (include structured error sample).
- [X] T024 Run whitespace/formatting checks after code/doc changes.
- [X] T025 Add a quickstart verification checkpoint: start server with defaults, connect via MCP client config, run sample tool (git status) and capture structured output example.

---

## Dependencies & Execution Order

- Setup (Phase 1) → Foundational (Phase 2) → User Stories (3/4/5) → Polish.
- All user stories depend on Phase 2 completion; stories can proceed in priority order (US1 → US2 → US3) or in parallel after Phase 2.
- Tests within each story should be added before or alongside implementation tasks.

## Parallel Execution Examples

- US1: Run T012 and T013 in parallel after T011.
- US2: Run T016 and T017 in parallel after T015.
- US3: Run T019 and T020 in parallel after T018.
- Cross-story: US2 and US3 can proceed in parallel once Phase 2 is done.

## Implementation Strategy

- MVP = US1 completed after Phases 1–2; validate safe terminal operations via MCP before expanding.
- Incremental delivery: add US2, validate build/test routing; add US3, validate Copilot maintenance; finish with Polish.
