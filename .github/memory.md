# FieldWorks agent memory (curated)

Use this file to capture decisions and pitfalls that help future agent sessions.
Keep it concise and high-value.

- Managed ↔ Native boundaries must be coordinated. Avoid throwing exceptions across the boundary; marshal explicitly.
- UI strings should come from .resx; avoid hardcoded user-visible text (Crowdin is configured).
- Prefer CI-style build scripts for reproducibility; installer builds are slow—run only when needed.
- Integration tests often rely on `TestLangProj/`; keep data deterministic.
- Keep `.editorconfig` and CI checks in mind: trailing whitespace, final newline, commit message format.
