# RenderSnapshots Directory

This directory contains approved baseline PNG snapshots and timing data for pixel-perfect render validation.

## Files

### Image Baselines
- `simple.png` - Minimal lexical entry with one sense
- `medium.png` - Entry with 3 senses, multiple definitions
- `complex.png` - Entry with 10+ senses, subsenses, cross-references
- `deep-nested.png` - Entry with deeply nested subsenses (5+ levels)
- `custom-field-heavy.png` - Entry with many custom fields

### Timing Baselines
- `baseline-timing.json` - Cold/warm render timings for all scenarios

### Environment Files
- `*.environment.txt` - Environment metadata (DPI, theme, etc.) for each baseline

## Bootstrap Behavior

The tests support **automatic bootstrapping**:

1. **First run (no baselines)**: Tests create initial baseline images and timing data automatically
2. **Subsequent runs**: Tests compare against stored baselines

This means you don't need to run special "generate" tests - just run the regular tests and they will create baselines if none exist.

```powershell
# Run render benchmark tests (will bootstrap if needed)
.\test.ps1 -TestFilter "TestCategory=RenderBenchmark"
```

## Timing Comparison

Timing baselines use a 20% threshold for regression detection:
- **>20% slower**: ⚠️ REGRESSION
- **>20% faster**: ✅ IMPROVED  
- **Within 20%**: ➡️ STABLE

## Environment Requirements

Snapshots are environment-specific. Each snapshot has a corresponding `.environment.txt` file
documenting the DPI, theme, and other settings used when generating the baseline.

If tests fail due to environment mismatch, either:
1. Match your environment to the baseline settings
2. Regenerate snapshots for your environment (delete the baseline files and rerun tests)

## Notes

- Snapshots and `baseline-timing.json` are committed to source control to enable CI validation
- Use `.environment.txt` files to verify environment compatibility before running tests
- To force regeneration, delete the specific baseline file and rerun the test
