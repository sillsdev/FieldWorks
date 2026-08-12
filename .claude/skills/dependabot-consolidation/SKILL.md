---
name: dependabot-consolidation
description: "Combine several open Dependabot pull requests into a single branch and PR: enumerate the open Dependabot PRs, cherry-pick them onto fresh main, rewrite commit messages to satisfy .github/commit-guidelines.md, verify locally, push, open the combined PR, wait for CI, and close the superseded PRs once it is green. Use when asked to combine, consolidate, batch, roll up, or squash together existing Dependabot PRs."
argument-hint: "Optional PR numbers to combine, e.g. 1035 1036 1037"
---

# Dependabot Consolidation

Turn several open Dependabot pull requests into one reviewable pull request, so
the repository pays for one CI cycle instead of one per bump.

Every command below is `git` or `gh` except the optional build step, which is
Windows only because it uses `robocopy` and `build.ps1`.

## When this is the wrong tool

If the complaint is that Dependabot opens too many PRs *every month*, the fix is
the `groups:` blocks in `.github/dependabot.yml`, not this skill. Today those
blocks put `github-actions` and `nuget` in separate groups and leave
`github-actions` major bumps ungrouped, so a quiet month still produces three
PRs. Say so, and offer that as separate work.

This skill only combines pull requests that already exist.

## Scope

- In scope: open pull requests authored by `app/dependabot` against one base.
- Out of scope: human-authored dependency PRs, even when named explicitly.
- Out of scope: editing `.github/dependabot.yml`.

## Three gates

The run stops and asks three times. Do not collapse them.

1. **Plan** - after enumerating, before any branch or worktree exists.
2. **Push** - after all local work and checks, before anything leaves the machine.
3. **Close** - after CI goes green, before closing the superseded PRs.

Anything that deviates from the approved plan stops and reports rather than
improvising: a dropped PR, a different base, a conflict, a red CI.

## Step 1 - Enumerate

```bash
gh pr list --author "app/dependabot" --state open --limit 100 --json number,title,baseRefName,headRefName
```

Then:

- If zero or one PR comes back, there is nothing to combine. Report and stop.
- If the PRs target more than one base, stop and ask which base to use. Do not
  pick one silently.

Fetch once, so every later inspection is local and free. Dependabot branches
live in this repository (it never forks), so a plain fetch is enough - no
`refs/pull/N/head` plumbing:

```bash
git fetch origin --prune
```

Flag major bumps. They are visible in the commit trailer, not the title. Read
the whole range, not `-1` - a PR Dependabot has force-pushed can carry more than
one commit, and `-1` would silently miss the others' trailers:

```bash
git log --format=%B origin/<base>..origin/<headRefName>
```

A body line reading `update-type: version-update:semver-major` marks a major.
`.github/dependabot.yml` ignores nuget majors but lets `github-actions` majors
through, so a major in the batch is normal, not an error - it just needs to be
visible in the plan.

Count the dependencies by counting `dependency-name:` lines across all the
trailers in that same range. That count goes in the PR title.

## Step 2 - Gate 1: the plan

Present the plan and get approval before creating anything:

```text
Plan:
  Combine #1035, #1036, #1037 -> chore/dependabot-combined-20260812
  Title: Chore: Bump 15 dependencies from 3 dependabot PRs

  #1035  actions-minor group (2)      minor/patch
  #1036  actions/setup-dotnet 5->6    MAJOR
  #1037  nuget-minor group (12)       minor/patch

  Local:  cherry-pick, reword, gitlint + whitespace, build (nuget in batch)
  Push:   origin chore/dependabot-combined-20260812
  Open:   PR against main, label 'dependencies'
  Then:   wait for CI; if green, ask before closing #1035 #1036 #1037

Proceed?
```

Do not describe two PRs touching the same file as a conflict. File overlap is
not conflict - two bumps in the same workflow file 150 lines apart apply
cleanly. Conflicts are discovered by attempting the cherry-picks, in step 3.

Branch name defaults to `chore/dependabot-combined-<yyyyMMdd>`. The `chore/`
prefix is in live use on the remote but is missing from the table in
`Docs/workflows/pull-request-workflow.md`; it matches the `Chore:` commit prefix
that `.github/dependabot.yml` already sets.

## Step 3 - Construct the branch

Work in a dedicated worktree. `.gitignore` reserves `.claude/worktrees/*` for
exactly this. The user's checkout is never touched, HEAD never moves under them,
and the build lock is per-worktree so a build running elsewhere is unaffected.

```bash
git worktree add --no-track <mainRepo>/.claude/worktrees/dependabot-combined-<yyyyMMdd> -b <branch> origin/<base>
```

`--no-track` matters: without it the new branch tracks `origin/<base>`, so
`git status` reports it as ahead of main and a stray `git pull` would rebase it.

Use a flat directory name, not the branch name - the branch contains a slash,
which would nest the worktree under `.claude/worktrees/chore/` and leave an
empty directory behind on removal. Give the absolute path under the main
repository root, since the command may be run from inside another worktree.

Branching off current `origin/<base>` is what makes staleness a non-issue -
Dependabot PRs are often weeks old and their existing CI results are worthless.

For each PR in ascending number order, for each of its commits in order:

```bash
git -C <wt> log --reverse --format=%H origin/<base>..origin/<headRefName>
```
```bash
git -C <wt> log -1 --format=%B <sha>
```
```bash
git -C <wt> cherry-pick <sha>
```
```bash
git -C <wt> commit --amend -F <msgfile>
```

`--amend` preserves the original author and author date, so Dependabot stays the
author with no `--author` or `--date` juggling. Write `<msgfile>` with the Write
tool rather than a shell heredoc - these bodies contain backticks and quotes
that break under PowerShell 5.1 quoting.

### Rewriting the message

Dependabot bodies fail the CI commit-message check on B1, body line length 80,
and the violation is narrowly the markdown link lines.

Dependabot writes at least three different body formats, so do not pattern-match
on one of them. Read the actual body first:

- **nuget group** - `Bumps <name> from A to B` prose lines, no links. Already
  compliant. Change nothing and skip the amend entirely.
- **actions group** - a `Bumps the <group> with N updates: [a](url) and [b](url).`
  summary followed by ``Updates `name` from A to B`` lines and link bullets.
- **single dependency** - one `Bumps [name](url) from A to B.` line plus link
  bullets, and that line is the only prose naming the dependency.

Apply these in order, and never delete a line whose information is not preserved
somewhere else in the message:

1. Delete every `- [Release notes](url)` / `- [Changelog](url)` /
   `- [Commits](url)` bullet. Pure noise, always long.
2. In the surviving lines, strip markdown link syntax: `[text](url)` becomes
   `text`. This alone brings the single-dependency form under 80 and normalizes
   it to the same shape nuget already uses.
3. Delete a line that is still over 80 only when it is a group summary whose
   contents are restated by the per-dependency lines below it. If a still-long
   line is the only place a dependency is named, stop and ask rather than
   dropping it.
4. Keep every ``Updates `name` from A to B`` and `Bumps <name> from A to B` line.
5. Keep the `---` separator, the whole `updated-dependencies:` trailer, the
   closing `...`, and the `Signed-off-by:` line. All are under 80, and the
   trailer is machine-readable metadata worth preserving.
6. Collapse runs of blank lines to one.

If a body already satisfies every rule, leave it byte-identical and do not amend
that commit. Rewriting a compliant message is churn with a chance of loss.

Then assert, before committing: subject is 72 characters or fewer, no trailing
punctuation, second line blank, every body line 80 characters or fewer, no hard
tabs, no trailing whitespace. If an assertion fails, stop - do not push
something the commit-message check will reject.

Result for #1035, verbatim. The trailing `...` is Dependabot's YAML terminator,
not an elision - keep it:

```text
Chore: Bump the actions-minor group with 2 updates

Updates `softprops/action-gh-release` from 3.0.1 to 3.0.2
Updates `lycheeverse/lychee-action` from 2.8.0 to 2.9.0

---
updated-dependencies:
- dependency-name: softprops/action-gh-release
  dependency-version: 3.0.2
  dependency-type: direct:production
  update-type: version-update:semver-patch
  dependency-group: actions-minor
- dependency-name: lycheeverse/lychee-action
  dependency-version: 2.9.0
  dependency-type: direct:production
  update-type: version-update:semver-minor
  dependency-group: actions-minor
...

Signed-off-by: dependabot[bot] <support@github.com>
```

### On conflict

Any conflict aborts the whole run:

```bash
git -C <wt> cherry-pick --abort
```
```bash
git worktree remove --force <wt> && git branch -D <branch>
```

Report the PR that conflicted, the conflicting paths, and the exact retry
command with that PR excluded, so the retry is one command rather than a
re-diagnosis.

## Step 4 - Verify locally

Always, in the worktree:

```bash
git -C <wt> log --check --pretty=format:"---% h% s" origin/<base>..
```
```bash
gitlint --ignore body-is-missing --commits origin/<base>..
```

`gitlint` is the exact tool CI runs, so there are no rules to reimplement and
drift. If it is not on PATH, report that, point at the documented install in
`.github/commit-guidelines.md`, and fall back to reading
`git log --format=%B origin/<base>..` and checking T1, T3, B1, B4, hard tabs,
and trailing whitespace directly. Do not pip install anything without asking.

When falling back, a check that cannot execute is a failure, not a pass. Git
Bash on Windows rejects `grep -P` outside UTF-8 locales, so a tab check written
that way exits non-zero and silently reports nothing wrong. Prefer
`grep -q "$(printf '\t')"`, and confirm each check actually ran before reporting
the result.

### Check for deliberate pins - do this before anything else

Dependabot cannot tell a dependency from a pin. `Directory.Packages.props` has a
`Transitive Pins` block whose entries exist specifically to hold a version, each
documented by a comment above it, and Dependabot will happily bump them and
leave the comment behind saying the opposite.

This is not hypothetical. PR #1000 held `Microsoft.Extensions.DependencyModel`
at 9.0.16 after reproducing a `TypeInitializationException` on
`Icu.NativeMethods` in the .NET Framework test host, and wrote the reason into
the file. The next monthly batch proposed bumping it again.

For every changed `PackageVersion` / `PackageReference` / version property line,
read the comment block immediately above it:

```bash
git -C <wt> diff -U6 origin/<base>..HEAD -- Directory.Packages.props Build/Src/Directory.Packages.props Build/SilVersions.props
```

Stop and ask if a nearby comment does any of these:

- names a specific version (`Pin to 9.0.17`, `stays at 9.0.16`)
- uses the words pin, pinned, stays, hold, do not bump, intentionally
- cites a PR or issue explaining why the current version was chosen
- gives a retest procedure

Two distinct outcomes, and do not conflate them:

- **The comment forbids the bump.** Drop that single line from the pick, keeping
  the rest of the batch, and say so in the plan and the PR body. Do not restore
  it silently and do not argue from a green CI - the pin's own retest procedure
  may require a full `test.ps1`, while CI runs with
  `TestCategory!=LongRunning&TestCategory!=ByHand&TestCategory!=SmokeTest&TestCategory!=DesktopRequired`.
  A green CI does not clear a pin whose repro lives outside that filter.
- **The comment merely names the old version.** The bump is fine, but the
  comment is now false. Update the comment in the same commit.

A recurring pin belongs in the `ignore:` block of `.github/dependabot.yml` so it
stops being proposed every month. That is a config change outside this skill -
recommend it, do not make it.

### Build, only when the batch touches packages

If any picked commit touches `*.props`, `*.json`, or another package manifest,
build. A `nuget-minor` group matches `patterns: ["*"]`, so it can carry
`Microsoft.Build.Utilities.Core`, which `FwBuildTasks` itself compiles against,
or a test-only package like NUnit that a product-only build would miss.

A fresh worktree has no `Output/`, so seed it first. A nuget bump cannot
invalidate native binaries - it only touches managed-side manifests - so native
output copied from any recent build of the same configuration stays valid:

```bash
robocopy <mainRepo>\Output\<Config> <wt>\Output\<Config> /E /NFL /NDL /NJH /NJS /R:1 /W:1
```

`robocopy` returns 1 when it successfully copies files. Treat 0 through 7 as
success and only 8 or higher as failure, or a working seed will be reported as
a broken one.

`build.ps1` needs `vswhere.exe` on PATH, and it is not there by default in a
worktree. Prepend the installer directory in the same invocation, since shell
state does not persist between calls:

```bash
$env:PATH = "$env:PATH;C:\Program Files (x86)\Microsoft Visual Studio\Installer"
```

Then, from the worktree:

```bash
.\build.ps1 -BuildTests -SkipNative -StartedBy agent
```

`-BuildTests` compiles test projects without running them. Running the suite
duplicates what CI will do on the PR. If the main checkout has no built output
to seed from, run `.\build.ps1 -BuildTests` and accept the native build.

With the seed in place this is cheap - about 90 seconds for product plus test
projects. Do not talk the user out of it on cost grounds; the expensive path is
only the unseeded native build.

For an actions-only batch, skip this entirely. A local build says nothing about
a change to workflow YAML.

The local build is a smoke test, not a stand-in for CI. If it prints
`Local library packages detected in ...`, a developer package folder is
shadowing upstream NuGet packages during restore, so what resolved locally may
not be what CI resolves. This bites hardest on the SIL packages, which are the
most likely to have local overrides and are routinely in the nuget group.

Do not just warn about it - check. Confirm the bumped versions are the ones that
actually restored:

```bash
find <wt> -name project.assets.json | xargs grep -ohE '"SIL\.(Core|LCModel)/[0-9][^"]*"' | sort -u
```

Compare against the versions in the picked commits. If they disagree, a local
package shadowed the bump, the build proved nothing about it, and the report
must say so.

## Step 5 - Gate 2: push and open

After approval:

```bash
git -C <wt> push -u origin <branch>
```
```bash
gh pr create --base <base> --head <branch> --title "..." --body-file <file> --label dependencies
```

Open it ready, not draft - the `pull_request` trigger has no `types:` filter, so
a draft runs CI anyway and buys only an extra step.

Write a purpose-built body rather than `.github/pull_request_template.md`, with
`--body-file` so quoting is never an issue. Five sections, in this order:

1. **Opening** - what it combines and the reason: one CI cycle instead of N.
2. **How it was built** - original Dependabot commits cherry-picked onto current
   `main`, author and date preserved; which bodies were trimmed and why, and
   which were already compliant and left byte-identical.
3. **Includes table** - one row per source PR, with the actual old -> new
   versions, not just a count.
4. **Major callout** - name any major bump in prose as the one change worth a
   close look. Do not leave it as a parenthetical in a table cell.
5. **Validation** - what was verified locally, itemized and specific, and what
   was not. Do not claim a local build that did not run. If the batch included
   packages, state that the resolved versions were confirmed against
   `project.assets.json` rather than shadowed by local dev packages.

Then a reviewer note explaining why the source PRs look red, because a reviewer
who checks them will otherwise assume the bumps are broken:

```markdown
Note for reviewers: the three source PRs all show a red `Build Debug and run
tests`, but the failing step is `Verify Codecov upload succeeded` - Dependabot
PRs do not receive `secrets.CODECOV_TOKEN`, so they cannot go green regardless
of the bumps. Their tests passed. This PR runs from a repository-owned branch
and should not hit that.
```

## Step 6 - Wait for CI

```bash
gh pr checks <new> --watch --interval 30
```

Run it in the background so the turn is not blocked and the user can interject.
It exits non-zero when any check fails, which is a verdict, not a tool error -
read the output, not just the exit code. A full cycle here is roughly 13 minutes.

**If CI is red:** report and stop. Name the failing job, step, and log URL, and
say whether it reads as a dependency failure or as infrastructure. Leave the
combined PR and every original open and untouched. Do not close anything, do not
guess which of the bumps caused it, do not re-run.

Note for triage: a Dependabot PR in this repository can never go green on its
own. `CI.yml` passes `secrets.CODECOV_TOKEN` to the coverage upload, Dependabot
PRs do not receive repository secrets, and `Verify Codecov upload succeeded`
fails even when every test passes. The combined PR is pushed from a
repository-owned branch, so that specific failure should not appear on it - if
it does, it is infrastructure, not a bump.

## Step 7 - Gate 3: close the superseded PRs

Only after CI is green, and only after asking:

```bash
gh pr close <N> --comment "Superseded by #<new>."
```

One call per original. Closing before green would be closing on a promise;
closing after green loses nothing, and `gh pr reopen <N>` remains available.

Then remove the worktree:

```bash
git worktree remove <wt>
```

## Final report

- The combined PR number and URL.
- Which PRs went in, and any that were excluded and why.
- What was verified locally and what was left to CI.
- The CI verdict.
- Which originals were closed.
