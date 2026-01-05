# Feature Specification: PowerShell MCP Server

**Feature Branch**: `009-powershell-mcp-server`
**Created**: 2025-12-08
**Status**: Draft
**Input**: User description: "Create a universal powershell MCP server to utilize the best powershell commands effectively. Also, refine MCP usage and auto-approves so that other commands (such as git, serena, python, docker) are all called from the optimal locations with minimal user requests while ensuring safe usage."

## Clarifications

### Session 2025-12-08

- Q: When a PowerShell script fails, how should the MCP server report this? → A: Structured output that returns `stdout`, `stderr`, and `exitCode` in the tool response even on non-zero exit codes (no MCP protocol error unless the server itself fails).
- Defaults and overrides: Provide sensible defaults (dynamic tool discovery, allowlist/denylist, timeout, output cap) that can be overridden via configuration (e.g., config file fields for tool selection, timeouts, output size).
- Client config location: Use workspace MCP client config (`mcp.json` preferred; `.vscode/settings.json` acceptable) with URL + `/sse` route; document overrides if port/path differ.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Safe Terminal Operations (Priority: P1)

As an AI agent, I want to perform common file and git operations using MCP tools instead of raw terminal commands, so that I don't trigger security warnings for pipes and redirections.

**Why this priority**: This directly addresses the "minimal user requests" and "safe usage" requirements by replacing the most friction-heavy terminal interactions.

**Independent Test**: Can be tested by invoking the new MCP tools for git log, diff, and file reading and verifying they return correct output without user prompts.

**Acceptance Scenarios**:

1.  **Given** a repository with history, **When** the agent calls `git_search` (or similar) via MCP, **Then** it receives the git log/diff output structured or as text.
2.  **Given** a file path, **When** the agent calls `read_file_content` via MCP, **Then** it receives the file content, respecting line limits.

---

### User Story 2 - Build and Test Execution (Priority: P2)

As an AI agent, I want to trigger builds and tests via MCP tools, so that the complex environment setup (containers, worktrees) is abstracted away.

**Why this priority**: Ensures "optimal locations" are used (host vs container) by leveraging the existing wrapper scripts through a reliable interface.

**Independent Test**: Can be tested by triggering a build via MCP and verifying it completes successfully (and uses the container if applicable).

**Acceptance Scenarios**:

1.  **Given** a clean repo, **When** the agent calls `build_project` via MCP, **Then** the `build.ps1` script is executed with appropriate arguments.
2.  **Given** a test project, **When** the agent calls `run_tests` via MCP, **Then** `test.ps1` is executed for that project.

---

### User Story 3 - Copilot Maintenance (Priority: P3)

As an AI agent, I want to run Copilot maintenance tasks (detect, plan, apply) via MCP, so that I can maintain documentation without constructing complex Python/PowerShell command chains.

**Why this priority**: Streamlines the specific "agentic development" tasks mentioned in the prompt.

**Independent Test**: Can be tested by running the detection tool via MCP and verifying it returns the JSON status.

**Acceptance Scenarios**:

1.  **Given** changed files, **When** the agent calls `copilot_detect_updates` via MCP, **Then** it returns the list of folders needing updates.
2.  **Given** maintenance actions (plan/apply/validate), **When** the agent invokes these via MCP with safe/mock flags, **Then** the commands run and return structured results or dry-run summaries without protocol errors.

### Edge Cases

- Discovery finds no matching scripts (empty toolset) → return clear message.
- Script missing or renamed after discovery → return structured error with exitCode/non-zero and `stderr` indicating missing path.
- Long-running or hung script → enforce default timeout; return timeout error with partial `stdout`/`stderr` and exitCode reflecting timeout.
- Excessive output → cap output size with truncation notice; allow override.
- Container/host routing failure (e.g., container not available) → return structured error indicating routing decision and failure reason.
- Malformed arguments or validation failures → return structured error with validation message, not protocol error.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an MCP server implementation that exposes PowerShell scripts as executable tools.
- **FR-002**: The server MUST be implemented in Python (consistent with repo tools).
- **FR-003**: The server MUST expose the following core scripts from `scripts/Agent/`: `Git-Search.ps1`, `Read-FileContent.ps1`, `Invoke-InContainer.ps1`.
- **FR-004**: The server MUST expose root-level scripts: `build.ps1`, `test.ps1`.
- **FR-005**: The server MUST automatically scan `scripts/Agent/*.ps1` and generate tools (Dynamic Scan).
- **FR-006**: The system MUST include updates to `terminal.instructions.md` to explicitly prefer these MCP tools over their raw terminal equivalents.
- **FR-007**: The system MUST be integrated into the workspace configuration (e.g., `mcp.json` or `project.yml`) to be available to the agent.
- **FR-008**: The tools MUST preserve the "optimal location" logic (host vs container) inherent in the underlying PowerShell scripts.
- **FR-009**: The MCP server MUST return structured tool responses for script execution, including `stdout`, `stderr`, and `exitCode`; non-zero exit codes MUST be surfaced in the response body rather than as protocol errors (protocol errors only if the MCP server itself fails).
- **FR-010**: The server MUST provide sensible defaults (dynamic tool discovery, allowlist/denylist, default timeout, output cap) and allow them to be overridden via configuration (e.g., config file values for tool selection, timeout, output cap, working directory).

### Key Entities

- **MCP Tool**: Represents a specific PowerShell script (e.g., `Build`, `GitSearch`) exposed to the agent.
- **Script Wrapper**: The logic that translates MCP tool arguments into PowerShell command-line arguments.

## Assumptions

- The existing PowerShell scripts in `scripts/Agent/` and root are robust and handle their own environment checks (containers, etc.).
- The user has the necessary runtime (Python or Node.js) installed to run the MCP server.
