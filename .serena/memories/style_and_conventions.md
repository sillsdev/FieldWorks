# Style & Conventions

- Managed code follows `.github/instructions/managed.instructions.md`: C#/.NET Framework patterns, respect analyzers, prefer descriptive naming, keep comments concise, and update resource files accordingly. Native C++/C++-CLI work uses `.github/instructions/native.instructions.md`.
- Installer edits obey `.github/instructions/installer.instructions.md`; WiX fragments must remain consistent with RegFree COM packaging requirements.
- Testing guidance exists in `.github/instructions/testing.instructions.md`; unit/integration tests usually live alongside source projects.
- Documentation changes should align with `.github/update-copilot-summaries.md` and the three-pass COPILOT workflow when touching folder docs; maintain `last-reviewed-tree` metadata.
- Formatting relies on `.editorconfig`; avoid trailing whitespace, ensure newline at EOF, and keep comments meaningful but sparse per repo guidance.
- When editing files, prefer ASCII unless non-ASCII is already present, and document complex code blocks with succinct comments if clarity is needed.