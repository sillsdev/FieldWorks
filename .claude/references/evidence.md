# Screenshots and visual evidence

Shared reference. Pointed at by `.claude/skills/pr-pitch/SKILL.md`,
`.claude/skills/jira-issue/SKILL.md` and
`.claude/skills/fieldworks-avalonia-ui/SKILL.md`.

Three stages, and skipping the middle one is the usual failure:
**capture, curate, publish.**

## The rule that outranks the rest

**The test is the evidence. The screenshot is the courtesy.**

A headless capture proves nothing a reviewer can re-run. The assertion does.
So a PR carries the test name *and* the picture, and the picture never carries
a claim the test does not.

The corollary: **a control-level headless capture is not a screenshot of the
product.** If nothing writes the operation at runtime yet, the image shows a
renderer, not a feature. Say which it is, in the caption, every time. That
distinction evaporates the moment an image is pasted without one.

## Capture

| Surface | Who captures | How |
| --- | --- | --- |
| Avalonia | The agent, automated | Headless Skia. See `fieldworks-avalonia-ui/references/visual-snapshot-testing.md` |
| WinForms, live project | The developer | Real scenarios need real data, and real data needs permission |
| WinForms, throwaway project | Either | `fieldworks-winapp/navigation/screenshot-evidence.md`, MCP-driven |

Captures land in `Output/ManualEvidence/<TICKET>/NN-name.png`. That directory
is gitignored, which is correct -- captures are working output, not artifacts.

## Curate

An uncaptured curation step is why evidence reads as decoration. Three things:

**Trim to content.** Renderer captures are mostly background. A 520x180 capture
with content in the top quarter reads as an empty box at thumbnail size, which
is the size it is first seen at in both GitHub and Jira. Crop to the content
bounds plus about 8 pixels.

**Caption every image.** What to look at, not what it is. "Before" is not a
caption; "Before -- the Lexeme field lists both seh and pt" is.

**Label provenance in the caption.** One of: headless control-level capture,
live FLEx desktop, or mockup. Never leave it to be inferred.

Name files `NN-state-subject.png` so they sort into reading order:
`01-before-writing-systems.png`, `02-after-writing-systems.png`.

## Publish -- GitHub

Probe for a route in this order and say which one was used.

**1. `gh --attach`, once it ships.** Native upload on six commands (issue and
PR create, edit, comment), tracked by `github/roadmap#1324`. It uses the
ordinary `gh` token, so no cookie and no committed file. Constraints: write
access required, **Actions tokens excluded** so CI cannot use it, nine file
types, images under 10 MB. Detect it rather than assuming a version:

```powershell
if ((gh pr comment --help 2>&1 | Out-String) -match '--attach') { "native upload available" }
```

**2. `gh image`** (`drogers0/gh-image`, MIT). Drives the web UI's own upload
flow and returns a real `user-attachments` URL. It needs a GitHub **session
cookie**, not the `gh` token: `--token`, `GH_SESSION_TOKEN`, or extraction
from a browser cookie store. Chrome 127 and later encrypt cookies in a way
that defeats extraction on Windows, so a Chrome-only machine will report
`session token is empty`.

- Check availability with `gh image check-token`, which prints a username.
- **Never run `gh image extract-token` in an agent session.** It prints a
  full-account credential to stdout, and stdout becomes conversation context.
- A `user_session` cookie grants complete account access and bypasses 2FA. If
  a developer chooses this route, they set `GH_SESSION_TOKEN` in their own
  shell before starting the session -- never pasted into a prompt.

**3. Orphan `evidence` branch.** Always available, no credential, and the only
route in CI. The branch is created once, never merged, and never appears in a
diff:

```powershell
git switch --orphan evidence
```

Reference the file by a **sha-pinned** raw URL so a merged PR's images can
never change under it:

```markdown
![Before -- Lexeme lists seh and pt (headless capture)](https://raw.githubusercontent.com/sillsdev/FieldWorks/<sha>/LT-22691d/01-before.png)
```

`sillsdev/FieldWorks` is public, so these render for everyone with no auth.
The branch must be protected: deleting it breaks every image in every PR that
ever referenced it.

## Publish -- Jira

Jira takes native attachments, which is better than a URL there because they
outlive any branch:

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-skills/scripts")
from jira_attachments import jira_add_attachment
print(jira_add_attachment("LT-22715", ["01-before.png", "02-after.png"]))
'@
```

Then reference them from the description or comment with `!01-before.png!`,
or `!01-before.png|thumbnail!` to keep a long description scannable. Images
belong in the analysis comment unless the picture *is* the bug report.

## Permission

**Hard stop, every time, before anything leaves the machine:**

> Do you have permission to post this?

A screenshot of a live project is a data disclosure exactly as a sample
project is: vernacular text, speaker names, unpublished lexical data,
community-owned material. Jira attachments are visible to everyone with
project access, and a GitHub attachment on a public repo is public.

- Never publish a capture the agent found on disk without being told to.
- Agent-captured WinForms evidence comes from a throwaway test project only.
- If permission is unclear, describe the image instead and say in the ticket
  that a capture exists but was not attached, so nobody re-asks.

## Checklist

- [ ] The claim the image supports is also pinned by a test, or the image is
      labelled as the only evidence.
- [ ] Trimmed to content.
- [ ] Captioned with what to look at.
- [ ] Provenance named: headless, live, or mockup.
- [ ] Permission asked and answered before upload.
- [ ] The publish route used is stated, and any URL is sha-pinned.
