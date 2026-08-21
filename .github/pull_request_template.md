## Quick Summary
<!-- Briefly describe what changed and why. Link issues if applicable (e.g., Fixes #1234). For large, multi-faceted PRs, use at most 6 bullets or short items. If more would be needed, include: "This quick summary does not capture all meaningful changes from this PR - please review the full summary carefully." -->

## CI-ready checklist

- [ ] Commit messages follow [`.github/commit-guidelines.md`](https://github.com/sillsdev/FieldWorks/blob/main/.github/commit-guidelines.md).
- [ ] As much as possible, the change is unit tested.
- [ ] Builds & tests pass locally (or I've run the CI-style build via `build.ps1`, `test.ps1`, or MSBuild).
- [ ] If this is core-developer AI-assisted work, I followed `Docs/workflows/ai-pr-workflow.md` and ran `pr-preflight` or the equivalent branch-readiness review before requesting review.
- [ ] For any `Src/**` folders touched, corresponding `AGENTS.md` files are updated or explicitly confirmed still accurate.
- [ ] I have considered all comments from an AI code reviewer (such as [Devin]https://app.devin.ai/review/sillsdev/FieldWorks/pull/####)

## Notes for reviewers (optional)
<!-- Risks, roll-out, docs/tests touched, special validation steps, etc. -->

