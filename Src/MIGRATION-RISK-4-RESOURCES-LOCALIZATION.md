# Migration Risk #4: Resource and Localization Infrastructure

**Risk Level**: ⚠️ **MEDIUM-HIGH**  
**Affected Projects**: FwResources, LexTextDll (LexTextStrings.resx, HelpTopicPaths.resx 215KB), all projects with .resx files  
**Estimated Effort**: 3-4 weeks

## Problem Statement

FieldWorks uses extensive .resx resource files for localization, help topics, and embedded resources. .NET 8 has different resource loading behavior, and the Crowdin integration must continue working. Large resource files (like HelpTopicPaths.resx at 215KB) may have performance implications.

## Specific Technical Risks

1. **Resource file format changes**: .resx files may need regeneration with new ResXResourceWriter.
2. **Designer-generated resource classes**: Auto-generated Designer.cs files may not compile or may generate different code.
3. **Satellite assembly loading**: Localized resource satellite assemblies load differently in .NET 8.
4. **Large resource files**: HelpTopicPaths.resx (215KB) in LexTextDll may have performance implications.
5. **Crowdin integration**: crowdin.json configuration must continue working with .NET 8 build process.
6. **Runtime resource lookup**: Resource manager behavior changed subtly for fallback handling.

## Affected Components

- **FwResources**: Central resource repository
- **LexTextDll**: LexTextStrings.resx, HelpTopicPaths.resx (215KB), ImageHolder.resx
- **All projects**: ~50+ projects with embedded .resx files
- **Crowdin workflow**: Localization sync and build integration
- **Satellite assemblies**: Language-specific resource DLLs

## Impact Assessment

**Impact**: **MEDIUM-HIGH** - Localization broken = unusable for international users. Help system broken = poor user experience. However, doesn't affect core functionality for English users.

**Affected Scenarios**:
- UI text in all supported languages
- Context-sensitive help system
- Embedded images and icons
- Error messages and dialogs
- Localization workflow for translators
- Build process and packaging

---

## Approach #1: Regenerate and Validate All Resources

**Strategy**: Regenerate all .resx files and Designer.cs classes with .NET 8 tools, then systematically test resource loading.

**Steps**:
1. **Inventory and Backup** (Week 1):
   - Inventory all .resx files in solution
   - Document current resource loading patterns
   - Backup existing resources and Designer.cs files
   - Identify problematic or unusual resource usages

2. **Regeneration** (Week 1-2):
   - Regenerate all Designer.cs files with .NET 8 ResXFileCodeGenerator
   - Update project files to use .NET 8 resource targets
   - Fix compilation errors from changed signatures
   - Test resource access in code

3. **Localization Testing** (Week 2-3):
   - Build satellite assemblies for all supported languages
   - Test resource loading in each locale
   - Validate fallback behavior (specific → neutral → invariant)
   - Test resource manager caching

4. **Crowdin Integration** (Week 3):
   - Test Crowdin sync process with .NET 8 projects
   - Update build scripts if needed
   - Validate translation workflow
   - Test satellite assembly generation from Crowdin

5. **Performance and Cleanup** (Week 3-4):
   - Profile large resource file loading (HelpTopicPaths.resx)
   - Optimize if needed (consider lazy loading or splitting)
   - Remove unused resources
   - Document resource best practices

**Pros**:
- Thorough validation of all resources
- Clean migration with modern tools
- Opportunity to optimize and clean up
- High confidence in localization

**Cons**:
- Time-consuming for ~50+ projects
- May uncover many small issues
- Requires testing in all locales
- Some resources may need manual fixes

**Risk Level**: Low-Medium (tedious but safe)

---

## Approach #2: Minimal Changes with Targeted Fixes

**Strategy**: Keep existing .resx files and Designer.cs, only regenerate and fix when issues are discovered.

**Steps**:
1. **Compatibility Testing** (Week 1):
   - Build all projects with .NET 8
   - Run applications in multiple locales
   - Identify resource loading failures
   - Document known issues

2. **Targeted Fixes** (Week 1-2):
   - Regenerate only problematic Designer.cs files
   - Fix resource loading issues as they appear
   - Update resource access patterns if needed
   - Add workarounds for compatibility

3. **Crowdin Validation** (Week 2-3):
   - Test Crowdin workflow minimally
   - Fix only blocking issues
   - Document any workflow changes
   - Ensure satellite assemblies build

4. **Performance Monitoring** (Week 3-4):
   - Monitor resource loading in production
   - Fix performance issues reactively
   - Consider splitting large resources if problems arise

**Pros**:
- Minimal upfront effort
- Focuses on actual problems
- Faster initial migration
- Less disruptive

**Cons**:
- Issues may appear later
- Incomplete coverage
- May miss subtle bugs
- Reactive rather than proactive

**Risk Level**: Medium (unknown issues may lurk)

---

## Approach #3: Modernize Resource Infrastructure

**Strategy**: Use this migration as opportunity to modernize resource management with .NET 8 best practices and potentially address Graphite removal mentioned by user.

**Context**: User mentioned "moving away from Graphite" and that "15-year-old complexity may no longer be needed with modern tooling."

**Steps**:
1. **Assessment** (Week 1):
   - Identify Graphite-related resources
   - Evaluate modern alternatives (e.g., HarfBuzz for text shaping)
   - Assess if resource-heavy features can be simplified
   - Plan for removal of obsolete resources

2. **Resource Consolidation** (Week 1-2):
   - Consolidate duplicate or similar resources
   - Remove obsolete Graphite-related resources
   - Move large resources to on-demand loading
   - Consider JSON/XML for non-string resources

3. **Modern Loading Patterns** (Week 2-3):
   - Implement lazy resource loading for large files
   - Use source generators for resource access (strongly-typed)
   - Consider resource trimming for unused locales
   - Optimize satellite assembly loading

4. **Localization Infrastructure** (Week 3-4):
   - Evaluate modern localization alternatives
   - Keep Crowdin but optimize workflow
   - Consider ResX → JSON for easier editing
   - Document modernized patterns

**Pros**:
- Removes obsolete complexity
- Modern, performant patterns
- Aligns with Graphite removal
- Better developer experience
- Long-term maintainability

**Cons**:
- Highest effort
- Risk of breaking existing workflows
- Team learning curve
- May affect translators' workflow

**Risk Level**: Medium-High (but strategic)

**Note**: This approach specifically addresses the user's mention of removing Graphite complexity and using modern tooling.

---

## Recommended Strategy

**Primary Path**: **Approach #1** (Regenerate and Validate)
- Thorough, low-risk approach
- Catches issues early
- Clean baseline for .NET 8

**Quick Win Hybrid**:
1. **Week 1**: Use Approach #2 to get basic functionality working
2. **Week 2-3**: Systematically regenerate per Approach #1
3. **Week 4**: Evaluate if Approach #3 optimizations are worthwhile

**If Graphite Removal is Priority**: **Approach #3** (Modernize)
- Addresses user's concern about 15-year-old complexity
- Combines with Graphite removal effort
- Higher effort but strategic value

## Graphite Context

Since user mentioned removing Graphite:
- Graphite (SIL's smart font technology) may have resources for:
  - Font feature tables
  - Glyph substitution rules
  - Rendering hints
  - Complex script support
- Modern alternatives:
  - HarfBuzz (better Unicode support, actively maintained)
  - Platform text APIs (CoreText, DirectWrite, Pango)
  - OpenType features (more standard than Graphite)

If Graphite resources are found, **Approach #3** can eliminate them as part of modernization.

## Success Criteria

1. All resources load correctly in .NET 8
2. All supported locales work (satellite assemblies load)
3. Help topics display correctly
4. Embedded images/icons render
5. Crowdin workflow functions
6. No performance regression (resource loading < 100ms startup impact)
7. Resource fallback works (specific → neutral → invariant)
8. Build process generates satellite assemblies correctly

## Crowdin Integration Details

Current setup (from crowdin.json):
- Translation files sync from Crowdin
- Build process incorporates translations
- Satellite assemblies generated per locale

.NET 8 considerations:
- Satellite assembly format unchanged
- Build targets may need updates
- ResX format compatibility maintained
- Test with actual Crowdin sync

## Related Documents

- **Src/DOTNET_MIGRATION.md**: Overall migration strategy
- **crowdin.json**: Localization configuration
- **Src/FwResources/COPILOT.md**: Central resources
- **Src/LexText/LexTextDll/COPILOT.md**: Large resource files (HelpTopicPaths.resx)

## Graphite Removal Synergy

If removing Graphite is planned:
- Combine Approach #3 with Graphite removal
- Remove Graphite-specific resources simultaneously
- Document what's being replaced by modern tooling
- This creates a clean break from legacy complexity

This could reduce overall effort by combining two efforts into one strategic modernization.
