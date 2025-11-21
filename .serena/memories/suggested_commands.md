# Suggested Commands

- `.\build.ps1 -Configuration Debug -Platform x64` — canonical traversal build (leverages FieldWorks.proj with MSBuild Traversal SDK).
- `msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m` — direct traversal build (run from a properly initialized developer environment or via `./build.sh`/`.\build.ps1`).
- `msbuild Build\Src\NativeBuild\NativeBuild.csproj` — native-only phase build when reg-free codegen prerequisites are missing.
- `python .github/detect_copilot_needed.py --base <ref>` — determine which COPILOT docs need updates (often wrapped via VS Code task).
- `git log --check` / `git diff --check --cached` — whitespace verification matching CI.
- `gitlint --ignore body-is-missing --commits origin/<base>..` — commit message lint (invoked via `./Build/Agent/commit-messages.ps1`).
- `docker exec fw-agent-<N> powershell -NoProfile -c "msbuild FieldWorks.sln /m /p:Configuration=Debug"` — required when building inside worktree containers (agent paths like `...\worktrees\agent-N`).
- Standard Windows shell helpers: `Get-ChildItem`, `Set-Location`, `Select-String`, `git status` for navigation, search, and SCM checks.