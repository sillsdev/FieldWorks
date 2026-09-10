---
name: dependabot-consolidation
description: "Combine several open Dependabot pull requests into a single branch and PR: enumerate the open Dependabot PRs, cherry-pick them onto fresh main, rewrite commit messages to satisfy .github/commit-guidelines.md, verify locally, push, open the combined PR, wait for CI, and close the superseded PRs once it is green. Use when asked to combine, consolidate, batch, roll up, or squash together existing Dependabot PRs."
argument-hint: "Optional PR numbers to combine, e.g. 1035 1036 1037"
---

# Dependabot Consolidation

Turn several open Dependabot pull requests into one reviewable pull request, so
the repository pays for one CI cycle instead of one per bump. In scope: open
PRs authored by `app/dependabot` against one base. Out of scope: human-authored
dependency PRs and edits to `.github/dependabot.yml` (recommend those, do not
make them).

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

Zero or one PR: nothing to combine, report and stop. More than one base:
stop and ask. Then fetch once - Dependabot branches live in this repository,
so no `refs/pull/N/head` plumbing is needed:

```bash
git fetch origin --prune
```

Read every commit body in each PR's range (`git log --format=%B
origin/<base>..origin/<headRefName>` - never `-1`; a force-pushed PR can carry
several commits). From the `updated-dependencies:` trailers: count
`dependency-name:` lines for the PR title, and flag any
`update-type: version-update:semver-major` in the plan. A major in
`github-actions` is normal - the config only ignores nuget majors.

## Step 2 - Gate 1: the plan

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

Do not present file overlap as conflict - overlapping files usually apply
cleanly, and real conflicts are discovered by the cherry-picks themselves.
Branch name: `chore/dependabot-combined-<yyyyMMdd>` (`chore/` is in live use
on the remote even though `Docs/workflows/pull-request-workflow.md` omits it).

## Step 3 - Construct the branch

Work in a dedicated worktree. `scripts/Worktree-CreateFromBranch.ps1` decides
the location; use a flat directory name, because the branch name contains a
slash. Creating one by hand instead:

```bash
git worktree add --no-track <worktreeRoot>/dependabot-combined-<yyyyMMdd> -b <branch> origin/<base>
```

`--no-track` prevents the branch tracking `origin/<base>` (else `git status`
reads "ahead of main" and a stray `git pull` rebases it). Branching off current
`origin/<base>` makes Dependabot-branch staleness a non-issue.

For each PR in ascending order, for each of its commits in order:
`git cherry-pick <sha>`, then `git commit --amend -F <msgfile>` when the
message needs rewriting. `--amend` preserves the original author and date.
Write `<msgfile>` with the Write tool, never a shell heredoc (PowerShell 5.1
mangles the backticks and quotes these bodies contain).

**On conflict, abort the whole run**: `cherry-pick --abort`, remove the
worktree, delete the branch, and report the conflicting PR, the paths, and the
exact retry command with that PR excluded.

### Rewriting the message

Dependabot bodies fail gitlint's B1 (body line <= 80) on their markdown link
lines, and Dependabot writes at least three body formats - read the actual
body, never pattern-match on one form:

- **nuget group**: `Bumps <name> from A to B` prose lines, no links. Already
  compliant - do not amend at all.
- **actions group**: a `Bumps the <group> with N updates: [a](url)...` summary,
  ``Updates `name` from A to B`` lines, and link bullets.
- **single dependency**: one `Bumps [name](url) from A to B.` line plus link
  bullets - the only prose naming the dependency.

Apply in order, never deleting a line whose information survives nowhere else:

1. Delete every `- [Release notes](url)` / `- [Changelog](url)` /
   `- [Commits](url)` bullet.
2. Strip markdown links in surviving lines: `[text](url)` becomes `text`.
3. Delete a still-long line only if it is a group summary restated by the
   per-dependency lines below; if it is the only place a dependency is named,
   stop and ask.
4. Keep every `Updates ...` / `Bumps ...` dependency line, the `---`
   separator, the whole `updated-dependencies:` trailer with its closing
   `...`, and `Signed-off-by:`.
5. Collapse blank-line runs to one.

Before committing, assert: subject <= 72, no trailing punctuation, blank
second line, body lines <= 80, no hard tabs, no trailing whitespace.

## Step 4 - Verify locally

```bash
git -C <wt> log --check --pretty=format:"---% h% s" origin/<base>..
```
```bash
gitlint --ignore body-is-missing --commits origin/<base>..
```

`gitlint` is the exact tool CI runs. If absent, point at the install in
`.github/commit-guidelines.md` and check the rules directly (do not pip
install without asking). When checking directly, **a check that cannot execute
is a failure, not a pass** - e.g. `grep -P` dies outside UTF-8 locales in Git
Bash and reports nothing; prefer `grep -q "$(printf '\t')"` and confirm each
check ran.

### Check for deliberate pins before accepting any package bump

Dependabot cannot tell a dependency from a pin. Read the comment block above
every changed `PackageVersion`/version-property line
(`git diff -U6 origin/<base>..HEAD -- Directory.Packages.props
Build/Src/Directory.Packages.props Build/SilVersions.props`). Stop on any
comment that names a version, says pin/stays/do-not-bump, cites a PR or issue,
or gives a retest procedure. Then classify:

- **Constraint-driven** (comment cites a range): verify from the depending
  package's `.nuspec` under `packages/`. Unbracketed `version="X"` is a floor -
  bumping above it is always safe (pinning too LOW is what causes NU1109);
  bracketed `[X]`/`[X,Y)` ranges are the only blocking kind and are rare.
  Take a floor-cleared bump; refresh a comment that names a stale number.
- **Empirically-driven** (pinned because a version was observed to break
  something, e.g. `Microsoft.Extensions.DependencyModel` at 9.0.16 from
  PR #1000): no manifest expresses this, so the graph will never block it -
  these belong in dependabot's `ignore:` list. Never clear one on a green CI:
  PR #984 was green at the documented-broken version, so CI is not evidence
  either way. Only the pin's own retest procedure counts, and the procedure
  must first be validated against the known-bad version (a positive control);
  if the known-bad version also passes, the repro is lost - keep the pin,
  report that, and never declare the bump safe. Re-run any suspected failure
  before attributing it to a bump; flaky tests mimic signal.

Two more structural checks, both silent when violated:

- **No property-to-literal rewrites**: ~a third of root entries read
  `Version="$(SilLcmVersion)"` etc. with the value in `Build/SilVersions.props`.
  `git diff ... | grep -E '^[-+].*Version="\$\('` - any hit needs a look.
- **Build/Src layering**: `Build/Src/Directory.Packages.props` imports the root
  and layers `Include` (build-only packages) / `Update` (overrides a root pin),
  so one package can be deliberately at two versions (DependencyModel 9.0.16
  root / 2.1.0 Build/Src; Microsoft.Bcl.AsyncInterfaces 9.0.16 root / 10.0.4
  Build/Src). When a changed package appears in both files, say which sites
  moved. `Build/Src/NativeBuild/NativeBuild.csproj` is the only CPM opt-out; a
  literal version in any other csproj is a violation, not a bump.

Second-order: a batch can bump a pinned package AND the package whose comment
justifies the pin (ParatextData is in Dependabot's scope). Re-derive ranges
from the new `.nuspec`, not the comment.

### Anticipate Paratext integration impact

Paratext loads the ILRepack-merged `FwParatextLexiconPlugin.dll` into its own
net48 process, where FieldWorks' binding redirects do not apply and the
plugin's config carries only dllmaps. External references resolve there at
exact strong-name versions or through a version-ignoring fallback over
Paratext's own directory - so a bump can break or silently downgrade the
plugin inside Paratext while every FieldWorks build and test stays green.
Nothing in CI covers this; the merged plugin first executes inside a real
Paratext.

This skill does not fix Paratext-contract breaks; it anticipates them. Treat a
batch as Paratext-sensitive when it touches any of: `SIL.LCModel*`, `SIL.Core*`
or the other SIL packages internalized by
`Src/FwParatextLexiconPlugin/ILRepack.targets`, `ParatextData`, `SIL.Machine*`,
`icu.net`, `CommonServiceLocator`, or the packages pinned "so the copies
ILRepack internalizes are deterministic"
(`Microsoft.Extensions.DependencyInjection*`, `Microsoft.Bcl.AsyncInterfaces`).
Then:

1. Say in the plan (gate 1) that the batch is Paratext-sensitive and why.
2. After the local build, diff the merged plugin's external references between
   the base build and the branch build - run this against each and compare:

   ```bash
   powershell -Command "[System.Reflection.Assembly]::ReflectionOnlyLoadFrom('<dir>\Output\Debug\FwParatextLexiconPlugin.dll').GetReferencedAssemblies() | Sort-Object Name | ForEach-Object { \"$($_.Name) $($_.Version)\" }"
   ```

3. Report any difference to the user and stop there: an added or
   version-shifted strong-named reference is a change to what Paratext's
   process must resolve. New externals usually mean a package swapped its
   internals (as liblcm's StructureMap-to-DI swap did); the fix - extending
   the ILRepack list or adjusting pins - is deliberate follow-up work with its
   own verification, not part of this run.
4. `ParatextData` bumps deserve a release-notes read even when green:
   `ParatextDataIntegrationTests` self-skips without an installed Paratext,
   so CI coverage of that package is partial.
5. When a batch changed what gets internalized into the plugin, recommend a
   manual smoke of the lexicon plugin inside a real Paratext before release -
   there is no automated coverage of the merged assembly anywhere.

### Build, only when the batch touches packages

If any picked commit touches `*.props`, `*.json`, or another package manifest,
build - the nuget group matches `*`, so it can carry build-critical
(`Microsoft.Build.Utilities.Core` backs FwBuildTasks) or test-only packages.
Seed native artifacts first (a nuget bump cannot invalidate them), noting
robocopy's exit codes 0-7 all mean success:

```bash
robocopy <mainRepo>\Output\<Config> <wt>\Output\<Config> /E /NFL /NDL /NJH /NJS /R:1 /W:1
```

`build.ps1` needs `vswhere.exe`; prepend its directory in the same invocation:

```bash
$env:PATH = "$env:PATH;C:\Program Files (x86)\Microsoft Visual Studio\Installer"
```
```bash
.\build.ps1 -BuildTests -SkipNative -StartedBy agent
```

With the seed this is ~90 seconds, so never skip it on cost grounds; only the
unseeded native fallback is expensive. Afterward confirm the bumped versions
actually restored rather than being shadowed by `LocalDevPackages`:

```bash
find <wt> -name project.assets.json | xargs grep -ohE '"SIL\.(Core|LCModel)/[0-9][^"]*"' | sort -u
```

If resolved versions disagree with the picked commits, the build proved
nothing about the bump - say so. For an actions-only batch skip the build
entirely; it says nothing about workflow YAML. Either way the local build is a
smoke test, not a stand-in for CI.

## Step 5 - Gate 2: push and open

```bash
git -C <wt> push -u origin <branch>
```
```bash
gh pr create --base <base> --head <branch> --title "..." --body-file <file> --label dependencies
```

Open ready, not draft (drafts run CI anyway). Write a purpose-built body with
`--body-file`, not the PR template: what it combines and why (one CI cycle
instead of N); how it was built (original commits cherry-picked onto current
main, authors preserved, which bodies were trimmed and which left
byte-identical); a table of actual old -> new versions per source PR; any
major bump called out in prose; and an itemized what-was-verified /
what-was-not section - never claim a build that did not run. Add the reviewer
note that Dependabot PRs show red `Build Debug and run tests` because they
cannot read `secrets.CODECOV_TOKEN`, while this PR runs from a repo-owned
branch.

## Step 6 - Wait for CI

```bash
gh pr checks <new> --watch --interval 30
```

Run it in the background; it exits non-zero on any failed check - that is a
verdict, not a tool error. A full cycle is roughly 13 minutes.

**If CI is red**: report the failing job, step, and log URL, say whether it
reads as a dependency failure or infrastructure, leave everything open, and
stop. Do not guess which bump caused it and do not re-run.

## Step 7 - Gate 3: close the superseded PRs

Only after CI is green, and only after asking:

```bash
gh pr close <N> --comment "Superseded by #<new>."
```

Then `git worktree remove <wt>`.

## Final report

- Combined PR number and URL; which PRs went in, which were excluded and why.
- What was verified locally and what was left to CI; CI verdict.
- Paratext-sensitivity assessment and contract-test verdict, when applicable.
- Which originals were closed.
