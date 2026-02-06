---
spec-id: architecture/build-deploy/installer
created: 2026-02-05
status: draft
---

# Installer

## Purpose

Describe installer build and packaging patterns for FieldWorks.

## Context

FieldWorks uses WiX 3 (default) and WiX 6 (opt-in) installers. This spec captures shared constraints and references installer-specific AGENTS.md.

## Installer Patterns

- Use build scripts to generate MSI and bundle artifacts.
- WiX 3 is the default pipeline; WiX 6 is opt-in.

### References

- [WiX 3 installer inputs](../../../../FLExInstaller/AGENTS.md#flexinstaller-wix-3-default) — Legacy WiX 3 pipeline
- [WiX 6 installer inputs](../../../../FLExInstaller/wix6/AGENTS.md#flexinstaller-wix-v6) — SDK-style WiX 6 pipeline

## Constraints

- Keep installer builds in sync with traversal build outputs.
- Do not register COM globally; rely on reg-free manifests.

## Anti-patterns

- Running installer builds without a current traversal build.

## Open Questions

- Should installer evidence collection be standardized for PRs?
