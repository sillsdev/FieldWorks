# FieldWorks Project Overview

- Purpose: FieldWorks (aka FLEx) is SIL International's Windows-focused linguistics and language data management suite. The repository hosts multiple desktop applications, shared libraries, an installer, tooling, and rich documentation.
- Tech stack: predominantly C#/.NET Framework 4.8 managed code, plus native C++/C++-CLI components, WiX installer assets, PowerShell/bash build scripts, and auxiliary Python tooling. Builds rely on MSBuild traversal (`FieldWorks.proj`).
- Structure highlights: Src/ contains applications and libraries (with per-folder AGENTS.md docs). Build/ houses shared targets/scripts, FLExInstaller/ contains WiX artifacts, Include/ + Lib/ host native headers/libs, and Build/Agent scripts support worktree automation. Specs/ and Docs/ provide planning/reference material.
- Key guidelines: Follow `.github/instructions/*.instructions.md` (build, managed, native, installer, testing). Respect `.editorconfig`, update COPILOT metadata when touching folders, and keep documentation in sync with code.
- Tooling environment: development happens on Windows with Visual Studio 2022 workloads (Desktop .NET + C++), WiX 3.14.x.
