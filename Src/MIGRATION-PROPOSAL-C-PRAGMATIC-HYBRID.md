# Migration Proposal C: Pragmatic Hybrid Approach

**Timeline**: 9-15 months (with decision gates)  
**Risk Level**: **MEDIUM**  
**Team Size**: 4-5 developers  
**Philosophy**: Start conservatively, pivot to modern solutions when validated

## Executive Summary

This proposal takes a pragmatic middle path: begin with conservative .NET 8 migration (Proposal A) but with strategic investments that enable pivoting to Avalonia (Proposal B) if early validation proves successful. It recognizes that migrating 15-year-old complexity deserves a measured approach with clear decision points.

## Strategic Approach

### Core Principles
1. **Validate before committing** - Test modern alternatives early
2. **Parallel paths** - Run pilots alongside main migration
3. **Decision gates** - Explicit go/no-go decisions at key milestones
4. **Incremental value** - Deliver .NET 8 benefits early, modernization later
5. **Risk-balanced** - Conservative main path, innovative side paths

### Three-Track Strategy

**Track 1 (Primary)**: Conservative .NET 8 migration
**Track 2 (Pilot)**: Avalonia proof-of-concept
**Track 3 (Research)**: Technology evaluation (Views replacement, modernization opportunities)

### Risk Mitigation Strategy

**Flexible approach** based on validation results:

- **Risk #1 (Views)**: Start Approach #1 (compatibility layer), pilot Approach #3 (Avalonia replacement)
- **Risk #2 (Designer)**: Start Approach #2 (code-first), pilot Approach #3 (Avalonia)
- **Risk #3 (XCore)**: Start Approach #2 (minimal changes), evaluate Approach #3 (MVVM) in pilot
- **Risk #4 (Resources)**: Approach #1 (regenerate), with Graphite removal (Approach #3) if applicable
- **Risk #5 (Database)**: Approach #1 (systematic validation) - no alternatives

## Phase-by-Phase Plan

### Phase 0: Foundation + Pilot Setup (Months 1-3)

**Primary Track**: .NET 8 Foundation
- Set up .NET 8 environment
- PoC with 3 simple projects
- Establish testing infrastructure

**Pilot Track**: Avalonia Proof-of-Concept
- **Week 1-2**: Avalonia environment setup (P0.1-P0.2 from MIGRATION-PLAN-0.md)
- **Week 3-4**: Implement one simple screen in Avalonia (e.g., AboutDialog)
- **Week 5-6**: Implement one complex screen (e.g., WebonaryLogViewer grid from P2.2)
- **Week 7-8**: **Decision Gate #1**: Evaluate pilot results

**Research Track**: Technology Evaluation
- Views engine replacement feasibility (PoC with Avalonia text)
- Graphite removal assessment
- Modern alternatives for legacy components

**Decision Gate #1** (End Month 3):
- **Pilot Success**: Proceed with combined migration (pivot to Proposal B)
- **Pilot Mixed**: Continue both tracks, expand pilot
- **Pilot Failure**: Focus on conservative migration (pivot to Proposal A)

**Deliverables**:
- .NET 8 environment ready
- 3 projects migrated
- Avalonia pilot completed with metrics
- Decision on path forward

---

### Phase 1A: Conservative Path (Months 4-6)
*If Decision Gate #1 → Conservative or Mixed*

**Activities**:
1. Migrate core libraries (11 pure managed projects)
2. Begin Views P/Invoke compatibility layer (Risk #1, Approach #1)
3. Database validation (Risk #5)
4. Resource regeneration (Risk #4)

**Deliverables**:
- Core libraries on .NET 8
- P/Invoke layer started
- Database validated
- Resources regenerated

### Phase 1B: Combined Path (Months 4-6)
*If Decision Gate #1 → Combined Migration*

**Activities**:
1. Migrate core libraries (11 projects)
2. Start Avalonia shell (P1.1 from MIGRATION-PLAN-0.md)
3. Database validation (Risk #5)
4. Begin Views replacement investigation

**Deliverables**:
- Core libraries on .NET 8
- Avalonia shell working
- Database validated
- Views replacement path determined

---

### Phase 2: Main Migration (Months 6-9)

**Conservative Path** (continuing from 1A):
1. Complete Views P/Invoke migration
2. Migrate UI libraries with WinForms
3. Fix designer issues with code-first approach
4. Update XCore minimally

**Combined Path** (continuing from 1B):
1. Migrate docking/workspace to Avalonia (P1.x)
2. Begin grid migrations (P2.x)
3. Continue Views work (either P/Invoke or Avalonia)

**Decision Gate #2** (End Month 9):
- Evaluate progress, team velocity, user feedback
- Determine if on track or need adjustment

---

### Phase 3: Completion or Expansion (Months 9-12)

**Conservative Path**:
1. Migrate applications
2. Integration testing
3. Stabilization
4. **Ship .NET 8 Windows version**

**Combined Path**:
1. Complete grid migrations (P2.x)
2. Migrate property editors (P3.x)
3. Replace web panes/remove GeckoFX (P4.x)
4. Continue toward full Avalonia

**Decision Gate #3** (End Month 12):
- **Conservative**: Prepare production release
- **Combined**: Evaluate if continuing to Avalonia completion or shipping hybrid

---

### Phase 4: Optional Extended Migration (Months 12-15)
*Only if Combined Path and continuing*

1. Complete command migration (P5.x)
2. Migrate remaining screens
3. Cross-platform testing
4. **Ship cross-platform version**

## Resource Requirements

### Team Structure

**Core Team** (constant):
- 2 senior developers (database, native interop)
- 1 QA engineer

**Flexible Team** (adjusts based on path):
- 1-2 developers (WinForms or Avalonia, depending on path)
- 0-1 UX designer (if Avalonia path chosen)

**Total**: 4-5 developers + 1 QA

### Skills Required

**Essential** (for both paths):
- .NET Framework and .NET 8
- P/Invoke and native interop
- Database and ORM
- Testing and validation

**Conditional** (if Avalonia path):
- Avalonia and XAML
- MVVM patterns
- Cross-platform development

## Decision Gates in Detail

### Decision Gate #1 (Month 3): Choose Path

**Evaluate**:
- Avalonia pilot success (functionality, performance, developer experience)
- Team capability with Avalonia
- Timeline and budget constraints
- User feedback on pilot screens

**Options**:
1. **Proceed Conservative**: Focus on .NET 8 only (→ Proposal A trajectory)
2. **Proceed Combined**: Full Avalonia migration (→ Proposal B trajectory)  
3. **Continue Pilot**: Expand Avalonia pilot, keep options open

**Criteria for Combined Path**:
- Pilot screens work well
- Performance acceptable
- Team comfortable with Avalonia
- Stakeholders approve timeline extension
- Budget supports larger scope

### Decision Gate #2 (Month 9): Validate Progress

**Evaluate**:
- On schedule for chosen path?
- Quality acceptable?
- Team velocity sustainable?
- Budget on track?

**Options**:
1. **Continue as planned**
2. **Scale back** (Combined → Conservative)
3. **Adjust timeline or resources**

### Decision Gate #3 (Month 12): Ship or Continue

**If Conservative Path**:
- Ship .NET 8 Windows version
- Plan future Avalonia migration separately

**If Combined Path**:
- **Option A**: Ship hybrid (some Avalonia, some WinForms)
- **Option B**: Continue to complete migration (3 more months)

## Success Criteria by Path

### Conservative Path Success
1. All projects on .NET 8
2. Windows functionality complete
3. Performance acceptable
4. Ship in 9-12 months

### Combined Path Success
1. All above, plus:
2. Significant portion on Avalonia
3. GeckoFX removed
4. Cross-platform partial or complete
5. Ship in 12-15 months

## Advantages

1. **Risk-balanced** - Conservative main path with innovative pilots
2. **Flexible** - Can adapt based on results
3. **Early value** - Delivers .NET 8 benefits quickly
4. **Measured investment** - Don't over-commit to unproven approach
5. **Decision points** - Clear go/no-go gates
6. **Learning opportunity** - Team learns Avalonia without full commitment initially
7. **Stakeholder confidence** - Shows progress while exploring options

## Disadvantages

1. **Coordination complexity** - Managing parallel tracks
2. **Potential pivot costs** - Some work may be discarded if changing paths
3. **Resource allocation** - Splitting team between tracks
4. **Decision overhead** - Gates require meetings and evaluation
5. **Uncertainty** - Timeline depends on gate decisions
6. **Hybrid complexity** - If shipping partial Avalonia, need to maintain both

## When to Choose This Proposal

Choose Proposal C if:
- **Uncertain about best path** - Want to validate before committing
- **Risk-averse but open to innovation** - Want safety net
- **Team capacity limited** - 4-5 developers, not 3-4 or 5-7
- **Stakeholder buy-in needed** - Early results help secure support
- **Flexible timeline** - Can be 9-15 months depending on path
- **Learning organization** - Want to build Avalonia skills gradually
- **Pragmatic culture** - Value measured progress over big bets

## Recommended Path (Our Suggestion)

We recommend **Proposal C** because:

1. **Reality check**: Avalonia is unproven for FieldWorks' complexity
2. **Risk management**: Doesn't bet everything on unvalidated approach
3. **Incremental value**: Can ship .NET 8 even if Avalonia struggles
4. **Team development**: Builds skills without high-pressure commitment
5. **Stakeholder confidence**: Shows tangible progress early
6. **Addresses user's vision**: Still investigates removing old complexity
7. **Balanced**: Not too conservative (Proposal A) or too ambitious (Proposal B)

## Implementation Notes

### Pilot Project Selection

**For Avalonia pilot, choose**:
1. **Simple screen**: AboutDialog, SettingsDialog (test basics)
2. **Grid screen**: WebonaryLogViewer (test P2.1 pattern from MIGRATION-PLAN-0.md)
3. **Complex screen**: One property editor or inspector panel

**Avoid in pilot**:
- Heavy native interop (Views engine)
- Core data access
- Critical path functionality

### Metrics for Decision Gates

**Gate #1 (Avalonia Pilot)**:
- ✅ Pilot screens functionally complete
- ✅ Performance within 20% of WinForms
- ✅ Team: 3/5 developers comfortable with Avalonia
- ✅ User feedback positive
- ✅ No blocking technical issues found

**Gate #2 (Progress)**:
- ✅ On schedule (±2 weeks)
- ✅ On budget (±10%)
- ✅ Quality metrics acceptable
- ✅ No major blockers

**Gate #3 (Ship or Continue)**:
- ✅ All .NET 8 migration complete
- ✅ Partial Avalonia working if on combined path
- ✅ Stakeholder approval for timeline
- ✅ Quality acceptable for release

## Timeline Scenarios

**Best Case** (Combined path, smooth execution):
- 12 months to cross-platform release

**Expected Case** (Conservative path after Gate #1):
- 9-10 months to .NET 8 Windows release
- Future Avalonia migration planned separately

**Challenged Case** (Issues found, scaled back):
- 12 months to .NET 8 Windows release
- No Avalonia, technical debt remains

## References

- **MIGRATION-PLAN-0.md**: Avalonia migration details (used for pilot and combined path)
- **MIGRATION-RISK-1 through 5**: All risk analyses with multiple approaches
- **MIGRATION-PROPOSAL-A**: Conservative baseline (fallback option)
- **MIGRATION-PROPOSAL-B**: Combined approach (aspirational path)
- **Src/DOTNET_MIGRATION.md**: Overall strategy
