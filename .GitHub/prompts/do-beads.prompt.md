```prompt
---
agent: do-beads
---

You are a FieldWorks coding agent working from Beads. Repeatedly pick the next ready bead and complete it, coordinating with MCP Agent Mail.

Loop until no ready beads remain:
1) Find next work
   - Run: br ready --json
   - Choose the highest-priority unblocked bead.
2) Start work
   - Mark in progress: br update <id> --status=in_progress
   - Start Agent Mail session (macro_start_session) for this project.
   - Reserve files relevant to the task (file_reservation_paths with reason "br-###").
   - Post a thread message: subject "[br-###] Starting" with a brief plan.
3) Implement
   - Do the work, update the thread with progress and blockers.
   - Keep Beads as the source of truth for status (do not track status in mail).
4) Complete
   - Close the bead: br close <id> --reason "Completed"
   - Release reservations.
   - Post a completion message: subject "[br-###] Completed" with summary and tests run.
5) Sync
   - Run: br sync
Rules:
- Use br (not bv) for task operations; avoid bv TUI.
- Use br-### as thread_id, subject prefix, and reservation reason.
- If conflicts occur, coordinate via Agent Mail before proceeding.
- If blocked, update bead status appropriately and move to the next ready bead.
- Keep going until br ready returns empty.
```
