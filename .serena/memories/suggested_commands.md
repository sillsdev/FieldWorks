# Suggested Commands

## Environment Verification
- `.\Build\Agent\Verify-FwDependencies.ps1 -IncludeOptional` — check all build dependencies are available
- `.\Build\Agent\Setup-FwBuildEnv.ps1 -Verify` — configure VS/MSBuild environment variables
- `.\Build\Agent\Setup-Serena.ps1` — verify Serena MCP setup for code intelligence

## Build Commands
- `.\build.ps1 -Configuration Debug -Platform x64` — canonical traversal build (leverages FieldWorks.proj with MSBuild Traversal SDK).