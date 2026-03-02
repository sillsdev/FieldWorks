---
applyTo: "**/*"
name: "navigation.instructions"
description: "Navigation policy for agentic code discovery and hidden-dependency tasks"
---

# Navigation policy (FieldWorks)

## Purpose & Scope
Define when agents should use structural navigation (Serena symbol/reference tools) versus lexical search, so hidden dependencies are not missed.

## Core policy
- Use Serena symbol tools before broad file reads for code exploration and edits.
- Treat lexical search (`grep`/keyword search) as a helper, not a complete dependency map.

## Task classification
- **Semantic task**: wording in the issue maps directly to files/symbols (renames, copy updates, obvious feature toggles).
- **Structural task**: changes likely propagate through inheritance, interfaces, factories, DI/composition, build graph, installer topology, or interop boundaries.
- **Hidden-dependency risk**: issue text is narrow but implementation may cross layers (native/managed boundary, COM/reg-free behavior, parser pipelines, installer chains).

## Required workflow
1. If structural or hidden-dependency risk is present, run Serena reference/navigation steps before editing:
   - locate defining symbol(s),
   - enumerate referencing symbols/callers,
   - inspect likely boundary files first.
2. Only then proceed to implementation.

## Veto protocol
- If lexical search returns weak or zero relevant results, do **not** conclude “no dependency.”
- Escalate to structural navigation (symbol references and topology traversal) and continue until likely callers/consumers are inspected.

## Prompt/checklist placement
- Put navigation checklist items at the **end** of long agent prompts to improve compliance in long-context runs.

## References
- `.github/instructions/build.instructions.md`
- `.github/instructions/testing.instructions.md`
- `.github/instructions/security.instructions.md`
