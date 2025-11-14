---
applyTo: "Src/LexText/ParserCore/**"
name: "parsercore.instructions"
description: "Auto-generated concise instructions from COPILOT.md for ParserCore"
---

# ParserCore (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **ParserScheduler**: Background thread scheduler with priority queue (5 priority levels: ReloadGrammarAndLexicon, TryAWord, High, Medium, Low). Manages ParserWorker thread, queues ParserWork items, tracks queue counts per priority. Supports TryAWordWork, UpdateWordformWork, BulkParseWork.
- Inputs: LcmCache, PropertyTable (XCore configuration)
- Outputs: ParserUpdateEventArgs events for task progress
- Threading: Single background thread, synchronized queue access
- **ParserWorker**: Executes parse tasks on background thread. Creates active parser (HCParser or XAmpleParser based on MorphologicalDataOA.ActiveParser setting), instantiates ParseFiler for result filing, handles TryAWord() test parses and BulkUpdateOfWordforms() batch processing.
- Inputs: LcmCache, taskUpdateHandler, IdleQueue, dataDir

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 47db0e38023bc4ba08f01c91edc6237e54598b77737bc15cad40098f645273a5
status: reviewed
---

# ParserCore

## Purpose
Morphological parser infrastructure supporting both HermitCrab and XAmple parsing engines. Implements background parsing scheduler (ParserScheduler/ParserWorker) with priority queue, manages parse result filing (ParseFiler), provides parser model change detection (ParserModelChangeListener), and wraps both HC (HermitCrab via SIL.Machine) and XAmple (legacy C++ parser via COM/managed wrapper) engines. Enables computer-assisted morphological analysis in FLEx by decomposing words into morphemes based on linguistic rules, phonology, morphotactics, and allomorphy defined in the morphology editor.

## Architecture
C# library (net48) with 34 source files (~9K lines total). Contains 3 subprojects: ParserCore (main library), XAmpleManagedWrapper (C# wrapper for XAmple DLL), XAmpleCOMWrapper (C++/CLI COM wrapper for XAmple). Supports pluggable parser architecture via IParser interface (HCParser for HermitCrab, XAmpleParser for legacy XAmple).

## Key Components

### Parser Infrastructure
- **ParserScheduler**: Background thread scheduler with priority queue (5 priority levels: ReloadGrammarAndLexicon, TryAWord, High, Medium, Low). Manages ParserWorker thread, queues ParserWork items, tracks queue counts per priority. Supports TryAWordWork, UpdateWordformWork, BulkParseWork.
  - Inputs: LcmCache, PropertyTable (XCore configuration)
  - Outputs: ParserUpdateEventArgs events for task progress
  - Threading: Single background thread, synchronized queue access
- **ParserWorker**: Executes parse tasks on background thread. Creates active parser (HCParser or XAmpleParser based on MorphologicalDataOA.ActiveParser setting), instantiates ParseFiler for result filing, handles TryAWord() test parses and BulkUpdateOfWordforms() batch processing.
  - Inputs: LcmCache, taskUpdateHandler, IdleQueue, dataDir
  - Manages: IParser inst
