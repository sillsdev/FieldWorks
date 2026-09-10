# Applying the pitch

Order:

1. `git rm` the RESEARCH / NOT-TAKEN / PROCESS / STALE files, **after** the
   developer has confirmed the triage.
2. Commit the alignment edits and the deletions together, with a message
   saying the reasoning moved to the PR.
3. Push.
4. Update the PR body -- pitch and accordions, one write.

Write the body to a file first. Never pass it inline.

```powershell
gh pr edit <n> --body-file body.md
```

`gh pr edit` needs `read:org` on the token and will fail with a GraphQL scope
error without it. Go straight to REST when it does:

```powershell
gh api -X PATCH repos/<owner>/<repo>/pulls/<n> -F body=@body.md --jq '.body|length'
```

The body is sticky: editing is in place and the PR URL never changes, so
nothing needs marker-matching or an existence check. That is the main reason
the record lives here rather than in a comment.

## Folding in an old provenance comment

If an earlier run left a separate comment, move its content into the
accordions and delete it, so there is exactly one record:

```powershell
gh api -X DELETE repos/<owner>/<repo>/issues/comments/<id>
```

Deleting is destructive and public -- confirm first, and only once the content
is verifiably in the published body. Anything in the tree pointing at that
comment now dangles:

```
git grep -n "provenance comment\|issuecomment" -- openspec/ Docs/ Src/
```

A wrapped line defeats a naive two-word grep. Search for each word.

## Before finishing

- [ ] The pitch zone is 200-400 words and fits one screen. Count, do not estimate.
- [ ] It opens with `**Start here:**` and closes with a `Next:` line.
- [ ] Every `<details>` is closed -- count `<details>` against `</details>`.
- [ ] Whole body under 65,536 characters.
- [ ] No content lives in a PR comment; the description is the only record.
- [ ] Nothing in the tree references a comment that was deleted.
- [ ] No deleted file's content was lost -- each is in an accordion, or was
      deliberately dropped as STALE.
- [ ] Every name in the body resolves in the current tree. Accordions rot the
      same way the pitch does, and a late rename sweep strands names the
      earlier reasoning used.
- [ ] Every count was recounted.
- [ ] The pitch does not repeat what an accordion already says.
- [ ] Working notes are gitignored (`Docs/migration/working/`), not merged.
- [ ] The pre-send check in `.claude/references/compact-style.md` passes.

Do not mark this complete on unverified claims. If a claim could not be
checked, say so in the report rather than asserting it.
