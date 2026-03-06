# FieldWorks UI Automation Testing Plan

> **Status:** Draft | **Created:** 2026-03-06 | **Author:** @johnml1135

## 1. Executive Summary

Add "snapshot" UI automation testing to FieldWorks: launch the app, navigate menus/views, and verify what's on screen — all running on GitHub Actions `windows-latest` with no physical display. This plan is grounded in three open-source projects that already do exactly this, and in the actual code structure of `sillsdev/FieldWorks`.

### Evidence Base

| Project | What it proves | Key file |
|---|---|---|
| [FlaUI/FlaUI.WebDriver](https://github.com/FlaUI/FlaUI.WebDriver) | Screenshots work on `windows-latest`, zero display setup needed | [ScreenshotTests.cs](https://github.com/FlaUI/FlaUI.WebDriver/blob/3cd26e6f205cf650f4c6e4c84de195effb986173/src/FlaUI.WebDriver.UITests/ScreenshotTests.cs) |
| [LordOfMyatar/Radoub](https://github.com/LordOfMyatar/Radoub) | Direct FlaUI + Avalonia app, isolated settings, focus management | [FlaUITestBase.cs](https://github.com/LordOfMyatar/Radoub/blob/8b54afe78fdaf74b8ab6a48d346a7c4bf1b600ca/Radoub.IntegrationTests/Shared/FlaUITestBase.cs) |
| [anpin/ContextMenuContainer](https://github.com/anpin/ContextMenuContainer) | FlaUI.WebDriver on CI, screenshot-on-error, artifact upload | [main.yml](https://github.com/anpin/ContextMenuContainer/blob/0625ade06dc12f6e2fe08fbb6d108913cbdfa688/.github/workflows/main.yml#L269-L351) |

---

## 2. FieldWorks-Specific Constraints

These are facts from reading the actual codebase:

### 2.1 Application Entry Point

FieldWorks launches via `Src/Common/FieldWorks/FieldWorks.cs` → `Main()`. It accepts command-line args parsed by `FwAppArgs` (in `Src/Common/FwUtils/FwLinkArgs.cs`):

```
FieldWorks.exe -db "Sena 3"         # Open specific project by name
FieldWorks.exe -db "TestLangProj"   # Open test project
FieldWorks.exe -noui                # Headless mode (no UI shown)
FieldWorks.exe -app FLEx            # Specify application
```

Key switches from `FwAppArgs`:
- `-db` / `-proj` → project name (resolves to `ProjectsDirectory/<name>/<name>.fwdata`)
- `-noui` → `NoUserInterface` flag (suppresses UI — we do NOT want this for UI tests)
- `-x` → config file override
- `-appServerMode` → server mode for other applications

### 2.2 Project File Location

`FwDirectoryFinder.ProjectsDirectory` resolves via registry (`HKCU\Software\SIL\FieldWorks\ProjectsDir`) with HKLM fallback, defaulting to `%APPDATA%\SIL\FieldWorks\Projects`. The sample project is:

```csharp
// From Src/Common/Framework/FwApp.cs line ~810
public virtual string SampleDatabase => Path.Combine(
    FwDirectoryFinder.ProjectsDirectory, "Sena 3",
    "Sena 3" + LcmFileHelper.ksFwDataXmlFileExtension);
```

For UI tests, we'll need to either:
- Copy a test `.fwdata` file into the expected projects directory, OR
- Use `DummyFwRegistryHelper` (already used in unit tests — see `FwDirectoryFinderTests.cs`) to redirect the projects directory

### 2.3 Existing CI Pipeline

The current CI (`.github/workflows/CI.yml`) already runs on `windows-latest`:

```yaml
# From .github/workflows/CI.yml
jobs:
  debug_build_and_test:
    runs-on: windows-latest
    steps:
      - run: .\build.ps1 -Configuration Debug -Platform x64 -BuildTests
      - run: .\test.ps1 -Configuration Debug -NoBuild -TestFilter 'TestCategory!=LongRunning&TestCategory!=ByHand&TestCategory!=SmokeTest&TestCategory!=DesktopRequired'
```

Note: Tests tagged `DesktopRequired` are **already excluded** from CI. Our UI tests will need their own workflow or a separate test category so they can be run independently.

### 2.4 Build System

FieldWorks uses SDK-style traversal builds (`build.ps1`), targets `net9.0` (from `SDK_MIGRATION.md`), x64 only, Windows only. Test projects use NUnit (migrating from 3 to 4 — see `scripts/tests/convert_nunit.py`). The test runner is `test.ps1` which calls `dotnet test` with category filters.

### 2.5 UI Technology

Currently WinForms (`System.Windows.Forms.Application.Run()`). Migration to Avalonia is in progress. FlaUI's UIA3 automation works with both. Radoub proves the Avalonia popup menu pattern we'll eventually need.

---

## 3. Technology Choice: FlaUI Direct (No WebDriver)

### What to use

| NuGet Package | Purpose |
|---|---|
| `FlaUI.Core` | Core automation primitives |
| `FlaUI.UIA3` | Windows UI Automation v3 provider |
| `NUnit` 4.x | Test framework (matches existing FW tests) |
| `NUnit3TestAdapter` 5.x | Test adapter for `dotnet test` |
| `Microsoft.NET.Test.Sdk` 17.x | Test host |
| `SixLabors.ImageSharp` | Screenshot comparison (Phase 2+) |

### Why NOT FlaUI.WebDriver/Appium

- Extra process to manage (Radoub proves direct FlaUI works fine)
- HTTP overhead between test and automation layer
- FieldWorks is Windows-only, so the cross-platform benefits of WebDriver are wasted
- Direct FlaUI gives access to `VirtualKeyShort`, `AutomationElement`, `Window` — everything we need

### Why NOT WinAppDriver

- Microsoft deprecated it; no updates since 2021
- Requires a separate server process
- FlaUI.WebDriver is the community successor, but even that adds unnecessary complexity for our case

---

## 4. Project Structure

```
Src/
└── FwUiAutomationTests/
    ├── FwUiAutomationTests.csproj      # net9.0-windows, FlaUI.Core + FlaUI.UIA3
    ├── Infrastructure/
    │   ├── FwTestBase.cs               # Abstract base: app lifecycle, focus, helpers
    │   ├── FwProcessManager.cs         # Launch/kill FieldWorks.exe
    │   ├── FwProjectSetup.cs           # Copy test .fwdata to isolated temp directory
    │   ├── FwScreenshot.cs             # Screenshot capture + comparison utilities
    │   ├── FwMenuDriver.cs             # Menu navigation (desktop-search for popups)
    │   └── TestSteps.cs                # Step-based multi-assertion (from Radoub)
    ├── SmokeTests/
    │   └── ApplicationLaunchTests.cs   # App launches, main window appears, menus exist
    ├── LexiconTests/
    │   └── LexiconEditViewTests.cs     # Navigate to Lexicon Edit, verify headword field
    ├── NavigationTests/
    │   └── AreaSwitchingTests.cs       # Switch between Lexicon/Grammar/Texts areas
    ├── TestData/
    │   └── TestProject.fwdata          # Minimal test project (checked into repo)
    └── Screenshots/
        └── baselines/                  # Baseline screenshots for comparison (Phase 2)
```

### 4.1 Project File

```xml name=FwUiAutomationTests.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlaUI.Core" Version="5.*" />
    <PackageReference Include="FlaUI.UIA3" Version="5.*" />
    <PackageReference Include="NUnit" Version="4.*" />
    <PackageReference Include="NUnit3TestAdapter" Version="5.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <!-- Phase 2: screenshot comparison -->
    <!-- <PackageReference Include="SixLabors.ImageSharp" Version="3.*" /> -->
  </ItemGroup>

  <ItemGroup>
    <!-- Ensure FieldWorks.exe is built before tests -->
    <ProjectReference Include="..\Common\FieldWorks\FieldWorks.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

---

## 5. Core Infrastructure Code

### 5.1 FwTestBase — Application Lifecycle

Modeled directly on Radoub's `FlaUITestBase.cs`, adapted for FieldWorks specifics:

```csharp name=Infrastructure/FwTestBase.cs
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using NUnit.Framework;

namespace SIL.FieldWorks.UiAutomationTests.Infrastructure;

/// <summary>
/// Base class for all FieldWorks UI automation tests.
/// Pattern from: https://github.com/LordOfMyatar/Radoub/blob/main/Radoub.IntegrationTests/Shared/FlaUITestBase.cs
/// </summary>
public abstract class FwTestBase : IDisposable
{
    protected Application? App { get; private set; }
    protected UIA3Automation? Automation { get; private set; }
    protected Window? MainWindow { get; set; }

    private string? _isolatedProjectDir;

    /// <summary>
    /// Path to FieldWorks.exe. Resolved from build output.
    /// </summary>
    protected virtual string FieldWorksExePath =>
        Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "Common", "FieldWorks",
            "bin", "x64", "Debug", "net9.0-windows", "FieldWorks.exe");

    protected virtual TimeSpan DefaultTimeout => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Name of the test project to load (e.g., "Sena 3" or "TestLangProj").
    /// Override in derived classes.
    /// </summary>
    protected virtual string ProjectName => "TestLangProj";

    /// <summary>
    /// Launch FieldWorks with the specified project.
    /// Uses isolated temp directory for project data (from Radoub pattern).
    /// </summary>
    protected void LaunchFieldWorks(string? projectName = null)
    {
        var project = projectName ?? ProjectName;
        Automation = new UIA3Automation();

        // Create isolated project directory (Radoub pattern)
        _isolatedProjectDir = Path.Combine(
            Path.GetTempPath(),
            "FwUiAutomationTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_isolatedProjectDir);

        // Copy test project data to isolated directory
        FwProjectSetup.CopyTestProject(project, _isolatedProjectDir);

        var processInfo = new ProcessStartInfo
        {
            FileName = FieldWorksExePath,
            Arguments = $"-db \"{project}\"",
            UseShellExecute = false
        };

        App = Application.Launch(processInfo);
        MainWindow = App.GetMainWindow(Automation, DefaultTimeout);

        // Aggressive focus (Radoub pattern: double-tap)
        if (MainWindow != null)
        {
            MainWindow.SetForeground();
            Thread.Sleep(50);
            MainWindow.Focus();
            Thread.Sleep(150);
            MainWindow.SetForeground();
            MainWindow.Focus();
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Ensure main window has focus before keyboard input.
    /// From Radoub: FlaUITestBase.cs lines 293-343
    /// </summary>
    protected bool EnsureFocused(int maxRetries = 3)
    {
        if (MainWindow == null) return false;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                MainWindow.SetForeground();
                Thread.Sleep(50);
                MainWindow.Focus();
                Thread.Sleep(100);

                if (!MainWindow.Properties.IsOffscreen.ValueOrDefault)
                    return true;
            }
            catch { /* Window may have closed */ }
            Thread.Sleep(200);
        }
        return false;
    }

    /// <summary>
    /// Click through menu hierarchy. Searches from Desktop for popup menus.
    /// From Radoub: FlaUITestBase.cs lines 219-280
    /// Critical for Avalonia: popup menus are separate top-level windows.
    /// </summary>
    protected void ClickMenu(params string[] menuPath)
    {
        if (menuPath.Length == 0 || MainWindow == null) return;
        const int maxRetries = 5;

        // Find and click top-level menu item
        AutomationElement? menu = null;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            menu = MainWindow.FindFirstDescendant(cf => cf.ByName(menuPath[0]));
            if (menu != null) break;
            Thread.Sleep(300);
            MainWindow = App?.GetMainWindow(Automation!, TimeSpan.FromMilliseconds(500));
        }
        menu?.AsMenuItem().Click();
        Thread.Sleep(300);

        // Click sub-items, searching from Desktop (Avalonia popup pattern)
        for (int i = 1; i < menuPath.Length; i++)
        {
            AutomationElement? item = null;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var desktop = Automation?.GetDesktop();
                item = desktop?.FindFirstDescendant(cf => cf.ByName(menuPath[i]));
                if (item == null)
                    item = MainWindow?.FindFirstDescendant(cf => cf.ByName(menuPath[i]));
                if (item != null) break;
                Thread.Sleep(300);
            }
            item?.AsMenuItem().Click();
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Capture screenshot of current window. Non-black validation only (Phase 1).
    /// From FlaUI.WebDriver: ScreenshotTests.cs
    /// </summary>
    protected byte[] CaptureScreenshot(string? savePath = null)
    {
        var capture = FlaUI.Core.Capturing.Capture.MainScreen();
        var bytes = capture.ToByteArray();

        if (savePath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            File.WriteAllBytes(savePath, bytes);
        }
        return bytes;
    }

    protected void CloseFieldWorks()
    {
        try
        {
            if (App != null && !App.HasExited)
            {
                Thread.Sleep(200);
                try { App.Close(); } catch { }

                var timeout = TimeSpan.FromSeconds(10);
                var start = DateTime.Now;
                while (!App.HasExited && (DateTime.Now - start) < timeout)
                    Thread.Sleep(100);

                if (!App.HasExited)
                    try { App.Kill(); } catch { }
            }
        }
        catch { /* App may have already exited */ }

        try { Automation?.Dispose(); } catch { }
        Thread.Sleep(500);
        App = null;
        Automation = null;
        MainWindow = null;
    }

    public void Dispose()
    {
        // Capture screenshot on failure (ContextMenuContainer pattern)
        if (TestContext.CurrentContext.Result.Outcome.Status ==
            NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            try
            {
                var screenshotDir = Path.Combine(
                    TestContext.CurrentContext.TestDirectory, "Screenshots", "failures");
                var fileName = $"{TestContext.CurrentContext.Test.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                CaptureScreenshot(Path.Combine(screenshotDir, fileName));
            }
            catch { /* Best effort */ }
        }

        CloseFieldWorks();
        CleanupIsolatedProject();
        GC.SuppressFinalize(this);
    }

    private void CleanupIsolatedProject()
    {
        if (string.IsNullOrEmpty(_isolatedProjectDir)) return;
        try
        {
            if (Directory.Exists(_isolatedProjectDir))
                Directory.Delete(_isolatedProjectDir, recursive: true);
        }
        catch { /* Best effort */ }
        _isolatedProjectDir = null;
    }
}
```

### 5.2 TestSteps — Multi-Assertion Diagnostics

From Radoub's `SmokeTests.cs` pattern:

```csharp name=Infrastructure/TestSteps.cs
using NUnit.Framework;

namespace SIL.FieldWorks.UiAutomationTests.Infrastructure;

/// <summary>
/// Step-based test runner that collects all failures instead of stopping at first.
/// Pattern from: https://github.com/LordOfMyatar/Radoub/blob/main/Radoub.IntegrationTests/Parley/SmokeTests.cs
/// </summary>
public class TestSteps
{
    private readonly List<(string Name, bool Passed, string? Error)> _results = new();
    private Action? _onFailure;

    public void Run(string stepName, Func<bool> step)
    {
        try
        {
            var passed = step();
            _results.Add((stepName, passed, passed ? null : "Step returned false"));
        }
        catch (Exception ex)
        {
            _results.Add((stepName, false, ex.Message));
        }
    }

    public void OnFailure(Action action) => _onFailure = action;

    public void AssertAllPassed()
    {
        var failures = _results.Where(r => !r.Passed).ToList();
        if (failures.Any())
        {
            _onFailure?.Invoke();
            var report = string.Join("\n",
                failures.Select(f => $"  FAIL: {f.Name} — {f.Error}"));
            Assert.Fail($"Test steps failed:\n{report}\n\n" +
                $"Passed: {_results.Count - failures.Count}/{_results.Count}");
        }
    }
}
```

---

## 6. Example Tests

### 6.1 Smoke Test — Application Launches

```csharp name=SmokeTests/ApplicationLaunchTests.cs
using NUnit.Framework;
using SIL.FieldWorks.UiAutomationTests.Infrastructure;

namespace SIL.FieldWorks.UiAutomationTests.SmokeTests;

[TestFixture]
[Category("UiAutomation")]
public class ApplicationLaunchTests : FwTestBase
{
    [Test]
    public void FieldWorks_LaunchesAndShowsMainWindow()
    {
        var steps = new TestSteps();
        steps.OnFailure(() => CaptureScreenshot(
            Path.Combine(TestContext.CurrentContext.TestDirectory,
                "Screenshots", "LaunchFailure.png")));

        steps.Run("Application launches", () =>
        {
            LaunchFieldWorks();
            return App != null && MainWindow != null;
        });

        steps.Run("Window title contains project name", () =>
            MainWindow?.Title?.Contains(ProjectName,
                StringComparison.OrdinalIgnoreCase) == true);

        steps.Run("Tools menu exists", () =>
            MainWindow?.FindFirstDescendant(
                cf => cf.ByName("Tools")) != null);

        steps.Run("Screenshot is not empty", () =>
        {
            var screenshot = CaptureScreenshot();
            return screenshot.Length > 1000; // Not a blank/black image
        });

        steps.AssertAllPassed();
    }
}
```

### 6.2 Navigation Test — Area Switching

```csharp name=NavigationTests/AreaSwitchingTests.cs
using NUnit.Framework;
using SIL.FieldWorks.UiAutomationTests.Infrastructure;

namespace SIL.FieldWorks.UiAutomationTests.NavigationTests;

[TestFixture]
[Category("UiAutomation")]
public class AreaSwitchingTests : FwTestBase
{
    [OneTimeSetUp]
    public void LaunchApp() => LaunchFieldWorks();

    [OneTimeTearDown]
    public void CloseApp() => CloseFieldWorks();

    [Test]
    [Order(1)]
    public void CanNavigateToLexiconEdit()
    {
        ClickMenu("View", "Lexicon Edit");
        Thread.Sleep(2000); // Wait for view to load

        // Verify we're in the lexicon area
        var screenshot = CaptureScreenshot(
            Path.Combine(TestContext.CurrentContext.TestDirectory,
                "Screenshots", "LexiconEdit.png"));
        Assert.That(screenshot.Length, Is.GreaterThan(1000),
            "Screenshot should not be empty/black");
    }

    [Test]
    [Order(2)]
    public void CanNavigateToGrammar()
    {
        ClickMenu("View", "Grammar");
        Thread.Sleep(2000);

        var screenshot = CaptureScreenshot(
            Path.Combine(TestContext.CurrentContext.TestDirectory,
                "Screenshots", "Grammar.png"));
        Assert.That(screenshot.Length, Is.GreaterThan(1000));
    }
}
```

---

## 7. CI Integration

### 7.1 New Workflow: `.github/workflows/ui-tests.yml`

```yaml name=.github/workflows/ui-tests.yml
name: UI Automation Tests

on:
  # Don't run on every PR — these are slow
  workflow_dispatch:
  schedule:
    - cron: '0 6 * * 1'  # Weekly Monday 6am UTC

permissions:
  contents: read
  checks: write

concurrency:
  group: ui-tests-${{ github.ref }}
  cancel-in-progress: true

jobs:
  ui_automation_tests:
    name: Run UI Automation Tests
    runs-on: windows-latest
    timeout-minutes: 30
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Build FieldWorks
        shell: powershell
        run: |
          .\build.ps1 -Configuration Debug -Platform x64

      - name: Build UI Test Project
        shell: powershell
        run: |
          dotnet build Src/FwUiAutomationTests/FwUiAutomationTests.csproj -c Debug

      - name: Run UI Automation Tests
        shell: powershell
        run: |
          dotnet test Src/FwUiAutomationTests/FwUiAutomationTests.csproj `
            --no-build -c Debug `
            --logger "trx;LogFileName=ui-test-results.trx" `
            --filter "TestCategory=UiAutomation"

      - name: Upload Screenshots
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: ui-test-screenshots
          path: |
            **/Screenshots/**/*.png
          if-no-files-found: ignore

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: ui-test-results
          path: |
            **/*.trx
          if-no-files-found: warn
```

### 7.2 Integration with Existing CI

The existing `CI.yml` already filters out `DesktopRequired` tests. We use a new category `UiAutomation` and a separate workflow so UI tests don't block regular CI:

```
# Existing test.ps1 filter (won't pick up our tests):
TestCategory!=LongRunning&TestCategory!=ByHand&TestCategory!=SmokeTest&TestCategory!=DesktopRequired

# Our tests use:
[Category("UiAutomation")]
```

---

## 8. Display / Headless Strategy

### 8.1 Primary: No Special Setup Required

**All three reference projects run on `windows-latest` with zero display configuration.** The FlaUI.WebDriver project's `ScreenshotTests.cs` proves screenshots are valid (non-black, correct dimensions) on the stock runner.

Why this works: GitHub's `windows-latest` runner has an active interactive session (Session 1). WinForms apps launched via `Process.Start()` get a window in this session. UIA3 automation can find and interact with these windows.

### 8.2 Fallback: PsExec for Session 1

If tests fail with "no desktop" errors (unlikely based on evidence), use PsExec:

```yaml
- name: Download PsExec
  run: |
    Invoke-WebRequest -Uri "https://download.sysinternals.com/files/PSTools.zip" -OutFile PSTools.zip
    Expand-Archive PSTools.zip -DestinationPath .\PSTools

- name: Run UI Tests in Interactive Session
  run: |
    .\PSTools\PsExec.exe -accepteula -i 1 dotnet test ...
```

### 8.3 Nuclear Option: Virtual Display Driver

[VirtualDrivers/Virtual-Display-Driver](https://github.com/VirtualDrivers/Virtual-Display-Driver) creates virtual monitors. Installable via:

```powershell
winget install --id=VirtualDrivers.Virtual-Display-Driver -e
```

This is **overkill** based on evidence — all three reference repos work without it. Reserve this for scenarios where:
- You need specific resolutions for screenshot comparison
- The runner image changes and removes the interactive session
- You need multi-monitor testing

---

## 9. App Lifecycle Strategy

### 9.1 Launch-per-TestFixture (Recommended)

FieldWorks takes 15-30 seconds to launch and load a project. Launching per-test is too slow. Launching once for all tests risks cross-contamination. The compromise:

```csharp
[TestFixture]
[Category("UiAutomation")]
public class LexiconEditTests : FwTestBase
{
    // Launch ONCE for this fixture
    [OneTimeSetUp]
    public void Launch()
    {
        LaunchFieldWorks("TestLangProj");
        ClickMenu("View", "Lexicon Edit");
        Thread.Sleep(2000);
    }

    // Reset state between tests (if needed)
    [SetUp]
    public void ResetView()
    {
        EnsureFocused();
    }

    [Test] public void HeadwordFieldExists() { /* ... */ }
    [Test] public void CanTypeInHeadwordField() { /* ... */ }

    [OneTimeTearDown]
    public void Close() => CloseFieldWorks();
}
```

This matches the pattern from FlaUI.WebDriver's `WebDriverFixture` (launch server once) and Radoub's per-test-class pattern.

### 9.2 Preventing Test Pollution

From Radoub's isolated settings pattern — each fixture gets its own temp directory:

1. `[OneTimeSetUp]`: Create temp dir → copy test project → set registry/env vars → launch
2. `[SetUp]`: Re-focus window, optionally navigate to known state
3. `[OneTimeTearDown]`: Close app → delete temp dir

---

## 10. Screenshot Strategy

### Phase 1: Capture + Non-Black Validation (Ship This)

From FlaUI.WebDriver's ScreenshotTests.cs:

```csharp
var screenshot = CaptureScreenshot();
Assert.That(screenshot.Length, Is.GreaterThan(1000)); // Not black/empty
```

Screenshots are saved as artifacts on CI for human review.

### Phase 2: Baseline Comparison (Future)

Add `SixLabors.ImageSharp` for pixel-diff comparison:

```csharp
var current = Image.Load(CaptureScreenshot());
var baseline = Image.Load("Screenshots/baselines/LexiconEdit.png");
var diff = ComputePixelDifference(current, baseline);
Assert.That(diff, Is.LessThan(0.05)); // < 5% pixel difference
```

### Phase 3: AI-Assisted Verification (Future)

Feed screenshots to an LLM/vision model:
> "Does this screenshot show a FieldWorks Lexicon Edit view with a headword field visible?"

---

## 11. Implementation Phases

### Phase 1: Foundation (Week 1-2)

- [ ] Create `Src/FwUiAutomationTests/` project
- [ ] Implement `FwTestBase` (app lifecycle, focus, menu navigation)
- [ ] Implement `TestSteps` (multi-assertion)
- [ ] Create minimal test project data (`TestLangProj.fwdata`)
- [ ] Write smoke test: app launches, window appears, menus exist
- [ ] Verify smoke test passes locally

### Phase 2: CI Integration (Week 3)

- [ ] Add `.github/workflows/ui-tests.yml`
- [ ] Verify tests pass on `windows-latest` with no special setup
- [ ] Add screenshot artifact upload
- [ ] Add test result publishing

### Phase 3: Navigation Tests (Week 4)

- [ ] Add area switching tests (Lexicon Edit, Grammar, Texts)
- [ ] Add menu interaction tests
- [ ] Add screenshot capture at each navigation point
- [ ] Implement screenshot-on-failure in `Dispose()`

### Phase 4: Content Verification (Week 5-6)

- [ ] Verify specific UI elements exist after navigation
- [ ] Add tests for project-specific content (entries in Sena 3)
- [ ] Add baseline screenshot comparison (optional)

### Phase 5: Regression Suite (Ongoing)

- [ ] Add tests for bug repro scenarios
- [ ] Add tests for Avalonia-migrated views (using Desktop search pattern)
- [ ] Integrate with PR checks (optional, for critical paths)

---

## 12. Risks and Mitigations

| Risk | Likelihood | Mitigation | Evidence |
|---|---|---|---|
| No display on runner | **Low** | PsExec fallback; VDD nuclear option | 3 repos prove it works stock |
| FieldWorks too slow to launch | **Medium** | Launch-per-fixture, not per-test | Radoub uses same pattern |
| COM registration required | **Medium** | Reg-free COM manifests (already implemented per `SDK_MIGRATION.md`) | FieldWorks already has reg-free COM |
| Focus stolen by other processes | **High on CI** | Aggressive double-tap focus (Radoub pattern) | Radoub's `EnsureFocused()` |
| Flaky element finding | **Medium** | Retry loops with 300ms delays (Radoub pattern) | All 3 reference repos use retries |
| Test project data too large | **Medium** | Create minimal `.fwdata` with only needed entries | `TestLangProj` pattern from existing tests |
| Avalonia popup menus not found | **Medium (future)** | Search from `Automation.GetDesktop()` | Radoub's `ClickMenu()` does this |
| Screenshot comparison too brittle | **Low** | Phase 1: non-black only. Phase 2: % threshold | FlaUI.WebDriver's approach |