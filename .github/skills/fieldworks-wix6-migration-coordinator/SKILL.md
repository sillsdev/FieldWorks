---
name: fieldworks-wix6-migration-coordinator
description: Use this skill whenever the user mentions FieldWorks, FLEx, WiX installer migration, WiX 3, WiX 6, PatchableInstaller, genericinstaller, Burn, MSI bundles, installer UI, installer patching, or installer diagnostics in the FieldWorks repo. Always use it for broad FieldWorks WiX Toolset migration questions, even if the user only says "wix"; do not use Wix.com website-builder sources.
---

# FieldWorks WiX 6 Migration Coordinator

Use this as the routing and guardrail skill for FieldWorks installer migration work. It keeps broad migration requests from becoming one undifferentiated installer soup.

## First Moves

1. Confirm the repo root by finding `FieldWorks.sln`, `build.ps1`, and `FLExInstaller/`.
2. Read the active repo guidance before edits: `AGENTS.md`, `FLExInstaller/AGENTS.md`, `.github/instructions/installer.instructions.md`, `.github/instructions/navigation.instructions.md`, and `.github/instructions/terminal.instructions.md`.
3. Read the current migration state: `specs/001-wix-v6-migration/spec.md`, `tasks.md`, `quickstart.md`, `wix3-to-wix6-audit.md`, `REMAINING_WIX6_ISSUES.md`, `BUNDLE_UI.md`, `verification-matrix.md`, and `golden-install-checklist.md`.
4. Treat all web research as WiX Toolset research. Avoid Wix.com/site-builder material entirely.

## Pick The Right Specialist

- Use `fieldworks-wix6-build-migration` for build orchestration, toolset selection, MSBuild targets, `.wixproj` changes, prerequisite downloads, CI checks, and moving old PatchableInstaller/genericinstaller build logic into the WiX 6 path.
- Use `fieldworks-wix6-ui` for bundle/MSI GUI problems, blank or misrendered UI, WixStdBA theme assets, MSI internal UI, dual-directory selection, feature-tree dialogs, and UI screenshot/evidence work.
- Use `fieldworks-wix6-upgrade-patching` for installing WiX 6 over WiX 3, single-instance guarantees, ARP duplication, uninstall/repair compatibility, base builds, MSP patches, `.wixpdb` baselines, and component/product-code stability.
- Use `fieldworks-installer-diagnostics` for runtime failures, "double-click does nothing", uninstall hangs, custom-action failures, evidence folders, Burn/MSI log triage, Event Viewer, crash dumps, and snapshot comparisons.

## Non-Negotiable Context

- FieldWorks is in a transition period: WiX 3 remains the default installer path, WiX 6 is opt-in.
- WiX 3 authoring lives in `FLExInstaller/` and uses the restored `PatchableInstaller/` legacy pipeline.
- WiX 6 authoring lives under `FLExInstaller/wix6/` and should not reuse WiX 3 `.wxi` files.
- The genericinstaller submodule should remain removed. Do not reintroduce a submodule checkout as a solution.
- WiX 6 authoring still uses the v4 XML namespace: `http://wixtoolset.org/schemas/v4/wxs`.
- Prefer `./build.ps1` and `./test.ps1` over ad-hoc `msbuild` or direct tool invocations unless debugging build infrastructure.
- Installer changes often cross build targets, WiX XML, custom actions, Burn behavior, registry state, and VM evidence. Use structural navigation for hidden dependencies before editing.

## Safe Work Pattern

1. State which specialist area applies and why.
2. Gather evidence from repo files and current logs before proposing changes.
3. Keep WiX 3 and WiX 6 paths isolated unless the task explicitly concerns compatibility between them.
4. Make the smallest change that addresses the root cause.
5. Validate with the relevant build/test/evidence checklist, or clearly report why validation could not be run.

## Source Credibility

Prefer sources in this order:

1. FieldWorks repo docs, specs, and current code.
2. Official WiX Toolset and FireGiant docs.
3. WiX maintainer posts, WiX GitHub issues/discussions, and WiX source/tests.
4. Stack Overflow/blog posts only as practical hints that must be converted into repo-specific tests.
