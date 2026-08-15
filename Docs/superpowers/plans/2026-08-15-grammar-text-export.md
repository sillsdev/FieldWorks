# Grammar and Text Export for AI Analysis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a new Export-dialog option, "Export Grammar and Texts for AI Analysis," that writes the project's HC grammar (`HCGrammar.xml`) and every user-selected text (as `.flextext` files) into one chosen folder.

**Architecture:** A new `FxtTypes.kftGrammarTextsAI` entry in the existing `ExportDialog` (xWorks) drives a WinForms text-picker dialog and a folder browser, then a background task that (a) calls the existing `HCLoader`/`XmlLanguageWriter` pipeline (via a new `ParserCore` project reference — no cycle) and (b) publishes a new pub/sub event answered by a new globally-registered listener living in `ITextDll` (where `InterlinVc`/`InterlinearExporter` already live), because `ITextDll` already depends on `xWorks` and a reverse `ProjectReference` would be a build-breaking cycle.

**Tech Stack:** C# / .NET Framework 4.8, WinForms, NUnit, `SIL.LCModel`, `SIL.Machine.Morphology.HermitCrab`, the existing XCore `Mediator`/`Publisher`/`Subscriber` system.

**Design doc:** `Docs/superpowers/specs/2026-08-15-grammar-text-export-design.md` (read this first for full rationale — this plan implements it task-by-task).

---

## Task 1: Shared event constant and request DTO (`FwUtils`)

**Files:**
- Modify: `Src\Common\FwUtils\EventConstants.cs`
- Create: `Src\Common\FwUtils\ExportTextsAsFlexTextRequest.cs`
- Test: `Src\Common\FwUtils\FwUtilsTests\ExportTextsAsFlexTextRequestTests.cs`

- [ ] **Step 1: Add the new event constant**

In `Src\Common\FwUtils\EventConstants.cs`, insert alphabetically (after `DictionaryConfigured`, before `FilterListChanged`):

```csharp
		public const string ExportTextsAsFlexText = "ExportTextsAsFlexText";
```

- [ ] **Step 2: Write the failing test for the request DTO**

Create `Src\Common\FwUtils\FwUtilsTests\ExportTextsAsFlexTextRequestTests.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class ExportTextsAsFlexTextRequestTests
	{
		[Test]
		public void Constructor_SetsTextsAndFolder_LeavesHandledFalseAndFailuresEmpty()
		{
			var texts = new List<IStText>();
			var request = new ExportTextsAsFlexTextRequest(texts, @"C:\some\folder");

			Assert.That(request.TextsToExport, Is.SameAs(texts));
			Assert.That(request.OutputFolder, Is.EqualTo(@"C:\some\folder"));
			Assert.That(request.Handled, Is.False);
			Assert.That(request.Failures, Is.Empty);
		}

		[Test]
		public void Failures_CanBeAppendedByASubscriber()
		{
			var request = new ExportTextsAsFlexTextRequest(new List<IStText>(), @"C:\folder");

			request.Failures.Add("Some Text: disk full");
			request.Handled = true;

			Assert.That(request.Failures, Is.EqualTo(new[] { "Some Text: disk full" }));
			Assert.That(request.Handled, Is.True);
		}
	}
}
```

- [ ] **Step 3: Run the test to confirm it fails to compile (type doesn't exist yet)**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~FwUtilsTests.ExportTextsAsFlexTextRequestTests"`
Expected: build failure — `ExportTextsAsFlexTextRequest` does not exist.

- [ ] **Step 4: Create the request DTO**

Create `Src\Common\FwUtils\ExportTextsAsFlexTextRequest.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Published with EventConstants.ExportTextsAsFlexText. A globally-registered listener
	/// (FlexTextAIExportListener, in ITextDll) answers this synchronously: it writes one
	/// .flextext file per text in TextsToExport into OutputFolder, sets Handled to true,
	/// and appends a "<name>: <message>" entry to Failures for any text it could not export.
	/// </summary>
	public sealed class ExportTextsAsFlexTextRequest
	{
		public ExportTextsAsFlexTextRequest(IEnumerable<IStText> textsToExport, string outputFolder)
		{
			TextsToExport = textsToExport;
			OutputFolder = outputFolder;
		}

		public IEnumerable<IStText> TextsToExport { get; }

		public string OutputFolder { get; }

		/// <summary>Set true by the subscriber that handled this request.</summary>
		public bool Handled { get; set; }

		/// <summary>One entry per text that failed to export, formatted "<name>: <message>".</summary>
		public List<string> Failures { get; } = new List<string>();
	}
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~FwUtilsTests.ExportTextsAsFlexTextRequestTests"`
Expected: PASS (2 tests)

- [ ] **Step 6: Commit**

```powershell
git add Src/Common/FwUtils/EventConstants.cs Src/Common/FwUtils/ExportTextsAsFlexTextRequest.cs Src/Common/FwUtils/FwUtilsTests/ExportTextsAsFlexTextRequestTests.cs
git commit -m "feat: add ExportTextsAsFlexText event and request DTO"
```

---

## Task 2: Export-template descriptor

**Files:**
- Create: `DistFiles\Language Explorer\Export Templates\GrammarAndTextsForAI.xml`

- [ ] **Step 1: Create the template file**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<template type="grammarTextsAI">
	<FxtDocumentDescription dataLabel="Grammar and Texts" formatLabel="XML for AI Analysis"
		defaultExtension="" filter="">Export Grammar and Texts for AI Analysis</FxtDocumentDescription>
</template>
```

- [ ] **Step 2: Commit**

```powershell
git add "DistFiles/Language Explorer/Export Templates/GrammarAndTextsForAI.xml"
git commit -m "feat: add export-template descriptor for grammar+text AI export"
```

(This file has no automated test on its own — Task 7's `ExportDialogTests` addition exercises `ConfigureItem` reading it.)

---

## Task 3: Word/analysis counting and filename helpers (`xWorks`)

**Files:**
- Create: `Src\xWorks\GrammarTextsAIExportHelpers.cs`
- Test: `Src\xWorks\xWorksTests\GrammarTextsAIExportHelpersTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Src\xWorks\xWorksTests\GrammarTextsAIExportHelpersTests.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class GrammarTextsAIExportHelpersTests
	{
		private LcmCache m_cache;

		[SetUp]
		public void CreateMockCache()
		{
			m_cache = LcmCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(BackendProviderType.kMemoryOnly, null), "en", "fr", "en", new DummyLcmUI(),
				FwDirectoryFinder.LcmDirectories, new LcmSettings());
		}

		[TearDown]
		public void DestroyMockCache()
		{
			m_cache.Dispose();
			m_cache = null;
		}

		// Qualified as SIL.LCModel.IText, not bare IText: this test project also sees the
		// ITextDll assembly, whose root namespace is SIL.FieldWorks.IText -- since this
		// file's own namespace (SIL.FieldWorks.XWorks) nests under SIL.FieldWorks, C#'s
		// enclosing-namespace lookup finds that sibling namespace before considering the
		// `using SIL.LCModel;` import, so bare `IText` is CS0118 ("is a namespace").
		private SIL.LCModel.IText MakeTextWithOneParagraph(string vernacularWord, out IStTxtPara para)
		{
			SIL.LCModel.IText text = null;
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				text = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				m_cache.LangProject.Texts.Add(text);
				var stText = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = stText;
				var newPara = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				stText.ParagraphsOS.Add(newPara);
				newPara.Contents = TsStringUtils.MakeString(vernacularWord, m_cache.DefaultVernWs);
			});
			para = (IStTxtPara)text.ContentsOA[0];
			return text;
		}

		[Test]
		public void CountWordsAndAnalyses_UnanalyzedParagraph_CountsWordsButNoAnalyses()
		{
			IStTxtPara para;
			var text = MakeTextWithOneParagraph("bonjour tout le monde", out para);
			// ParagraphParser.Parse creates segments/wordform occurrences, so it must run
			// inside a UnitOfWork just like any other LCM object creation.
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				using (var pp = new ParagraphParser(m_cache))
					pp.Parse(para);
			});

			var counts = GrammarTextsAIExportHelpers.CountWordsAndAnalyses(text.ContentsOA);

			Assert.That(counts.Words, Is.EqualTo(4));
			Assert.That(counts.Analyses, Is.EqualTo(0));
		}

		[Test]
		public void CountWordsAndAnalyses_OneWordGivenARealAnalysis_CountsThatWordAsAnalyzed()
		{
			IStTxtPara para;
			var text = MakeTextWithOneParagraph("bonjour", out para);
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				using (var pp = new ParagraphParser(m_cache))
					pp.Parse(para);
			});
			var segment = para.SegmentsOS[0];
			var wordform = (IWfiWordform)segment.AnalysesRS[0];
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				var analysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordform.AnalysesOC.Add(analysis);
				segment.AnalysesRS[0] = analysis;
			});

			var counts = GrammarTextsAIExportHelpers.CountWordsAndAnalyses(text.ContentsOA);

			Assert.That(counts.Words, Is.EqualTo(1));
			Assert.That(counts.Analyses, Is.EqualTo(1));
		}

		[Test]
		public void GetTextDisplayName_OwnedByAnIText_ReturnsTextName()
		{
			IStTxtPara para;
			var text = MakeTextWithOneParagraph("hello", out para);
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				text.Name.SetAnalysisDefaultWritingSystem("My Test Text");
			});

			var name = GrammarTextsAIExportHelpers.GetTextDisplayName(text.ContentsOA);

			Assert.That(name, Is.EqualTo("My Test Text"));
		}

		[Test]
		public void MakeSafeFileName_StripsInvalidCharactersAndDedupes()
		{
			var used = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "HCGrammar" };

			var first = GrammarTextsAIExportHelpers.MakeSafeFileName("Story: Part 1?", used);
			var second = GrammarTextsAIExportHelpers.MakeSafeFileName("Story: Part 1?", used);

			Assert.That(first, Is.EqualTo("Story_ Part 1_"));
			Assert.That(second, Is.EqualTo("Story_ Part 1_ (2)"));
			Assert.That(used, Does.Contain(first));
			Assert.That(used, Does.Contain(second));
		}
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.GrammarTextsAIExportHelpersTests"`
Expected: build failure — `GrammarTextsAIExportHelpers` does not exist.

- [ ] **Step 3: Implement the helper class**

Create `Src\xWorks\GrammarTextsAIExportHelpers.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>Word/analysis counts for one text, for the AI-export text picker.</summary>
	public struct WordAnalysisCounts
	{
		public WordAnalysisCounts(int words, int analyses)
		{
			Words = words;
			Analyses = analyses;
		}

		/// <summary>Every word-token occurrence, whether analyzed or not.</summary>
		public int Words { get; }

		/// <summary>Word-token occurrences that have an IWfiAnalysis/IWfiGloss attached.</summary>
		public int Analyses { get; }
	}

	/// <summary>
	/// Helpers shared by the grammar+texts-for-AI export: counting words/analyses per text
	/// for the picker dialog, deriving a display name for a text, and sanitizing text titles
	/// into safe, unique file names.
	/// </summary>
	public static class GrammarTextsAIExportHelpers
	{
		/// <summary>
		/// Counts word-token occurrences (Words) and the subset of those that have a real
		/// IWfiAnalysis/IWfiGloss attached (Analyses), across every paragraph of stText.
		/// </summary>
		public static WordAnalysisCounts CountWordsAndAnalyses(IStText stText)
		{
			var words = 0;
			var analyses = 0;
			for (var i = 0; i < stText.ParagraphsOS.Count; ++i)
			{
				var para = (IStTxtPara)stText.ParagraphsOS[i];
				foreach (var analysis in para.Analyses)
				{
					if (!analysis.HasWordform)
						continue;
					words++;
					if (!(analysis is IWfiWordform))
						analyses++;
				}
			}
			return new WordAnalysisCounts(words, analyses);
		}

		/// <summary>
		/// The display name for a text: the owning IText's Name if there is one (the normal
		/// case for interlinear texts), otherwise the IStText's own short name (covers
		/// Scripture sections, which are not owned by an IText).
		/// </summary>
		public static string GetTextDisplayName(IStText stText)
		{
			if (stText.Owner is IText text)
				return text.Name.BestAnalysisVernacularAlternative.Text;
			return stText.ShortNameTSS.Text;
		}

		private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

		/// <summary>
		/// Replaces characters that are invalid in a file name with '_', then appends
		/// " (2)", " (3)", etc. if the result collides (case-insensitively) with a name
		/// already in usedNames. Adds the returned name to usedNames before returning it.
		/// </summary>
		public static string MakeSafeFileName(string rawName, HashSet<string> usedNames)
		{
			var sanitized = new StringBuilder(rawName.Length);
			foreach (var ch in rawName)
				sanitized.Append(InvalidFileNameChars.Contains(ch) ? '_' : ch);
			var baseName = sanitized.ToString();

			var candidate = baseName;
			var suffix = 2;
			while (usedNames.Contains(candidate))
			{
				candidate = $"{baseName} ({suffix})";
				suffix++;
			}
			usedNames.Add(candidate);
			return candidate;
		}
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.GrammarTextsAIExportHelpersTests"`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```powershell
git add Src/xWorks/GrammarTextsAIExportHelpers.cs Src/xWorks/xWorksTests/GrammarTextsAIExportHelpersTests.cs
git commit -m "feat: add word/analysis counting and filename helpers for AI export"
```

---

## Task 4: HC-grammar load logger and `ParserCore` project reference

**Files:**
- Modify: `Src\xWorks\xWorks.csproj`
- Create: `Src\xWorks\GrammarExportLoadLogger.cs`
- Test: `Src\xWorks\xWorksTests\GrammarExportLoadLoggerTests.cs`

- [ ] **Step 1: Add the ProjectReference**

In `Src\xWorks\xWorks.csproj`, in the `<ItemGroup>` containing `ProjectReference`s, insert alphabetically (after `../LexText/LexTextControls/LexTextControls.csproj`, before `../Utilities/Reporting/Reporting.csproj`):

```xml
    <ProjectReference Include="../LexText/ParserCore/ParserCore.csproj" />
```

- [ ] **Step 2: Write the failing test**

Create `Src\xWorks\xWorksTests\GrammarExportLoadLoggerTests.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class GrammarExportLoadLoggerTests
	{
		[Test]
		public void InvalidPhoneme_AddsAMessage_DoesNotThrow()
		{
			var messages = new List<string>();
			var logger = new GrammarExportLoadLogger(messages);

			Assert.DoesNotThrow(() => logger.InvalidPhoneme(null));

			Assert.That(messages, Has.Count.EqualTo(1));
		}

		[Test]
		public void InvalidStrata_AddsTheReasonToTheMessage()
		{
			var messages = new List<string>();
			var logger = new GrammarExportLoadLogger(messages);

			logger.InvalidStrata("Stratum1", "circular dependency");

			Assert.That(messages[0], Does.Contain("circular dependency"));
		}
	}
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.GrammarExportLoadLoggerTests"`
Expected: build failure — `GrammarExportLoadLogger` does not exist.

- [ ] **Step 4: Implement the logger**

Create `Src\xWorks\GrammarExportLoadLogger.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using SIL.LCModel;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Collects HCLoader's per-item load warnings into a plain message list instead of
	/// surfacing them modally mid-export. HCLoader already skips the offending item and
	/// keeps going for every one of these, so none of them abort the grammar export.
	/// </summary>
	public class GrammarExportLoadLogger : IHCLoadErrorLogger
	{
		private readonly List<string> m_messages;

		public GrammarExportLoadLogger(List<string> messages)
		{
			m_messages = messages;
		}

		public void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
		{
			m_messages.Add($"Invalid shape '{str}' at position {errorPos}.");
		}

		public void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
		{
			m_messages.Add(isInvalidLhs
				? "Invalid affix process: left-hand side is invalid."
				: "Invalid affix process: right-hand side is invalid.");
		}

		public void InvalidPhoneme(IPhPhoneme phoneme)
		{
			m_messages.Add("Invalid phoneme definition.");
		}

		public void DuplicateGrapheme(IPhPhoneme phoneme)
		{
			m_messages.Add("Duplicate grapheme in a phoneme definition.");
		}

		public void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
		{
			m_messages.Add($"Invalid environment: {reason}");
		}

		public void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
		{
			m_messages.Add($"Invalid reduplication form: {reason}");
		}

		public void InvalidRewriteRule(IPhRegularRule prule, string reason)
		{
			m_messages.Add($"Invalid rewrite rule: {reason}");
		}

		public void InvalidStrata(string strata, string reason)
		{
			m_messages.Add($"Invalid strata '{strata}': {reason}");
		}

		public void OutOfScopeSlot(IMoInflAffixSlot slot, IMoInflAffixTemplate template, string reason)
		{
			m_messages.Add($"Out-of-scope affix slot: {reason}");
		}

		public void UnmatchedReduplicationIndexedClass(IMoForm form, string reason, string environment)
		{
			m_messages.Add($"Unmatched reduplication indexed class: {reason}");
		}
	}
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.GrammarExportLoadLoggerTests"`
Expected: PASS (2 tests)

- [ ] **Step 6: Rebuild to confirm the new ProjectReference resolves cleanly**

Run: `.\build.ps1`
Expected: build succeeds with no new warnings/errors from `xWorks.csproj` or `ParserCore.csproj`.

- [ ] **Step 7: Commit**

```powershell
git add Src/xWorks/xWorks.csproj Src/xWorks/GrammarExportLoadLogger.cs Src/xWorks/xWorksTests/GrammarExportLoadLoggerTests.cs
git commit -m "feat: reference ParserCore from xWorks and add HC load logger"
```

---

## Task 5: New localized strings

**Files:**
- Modify: `Src\xWorks\xWorksStrings.resx`
- Modify: `Src\xWorks\xWorksStrings.Designer.cs`

- [ ] **Step 1: Add resx entries**

`xWorksStrings.resx` isn't sorted alphabetically overall (it's grouped
historically) — insert this whole block right after the existing
`ksLIFTFolderNotEmpty` entry, next to the other export-folder-picker strings:

```xml
  <data name="ksChooseGrammarTextsAIExportFolder" xml:space="preserve">
    <value>Choose or create a folder for FLEx to put the HC grammar file and one .flextext file per selected text into.</value>
  </data>
  <data name="ksChooseGrammarTextsAIExportFolderTitle" xml:space="preserve">
    <value>Choose where to save the grammar and texts for AI analysis</value>
  </data>
  <data name="ksGrammarTextsAIExportSummary" xml:space="preserve">
    <value>The export finished, but with some issues:{0}{0}{1}</value>
  </data>
  <data name="ksNoTextsSelectedForAIExport" xml:space="preserve">
    <value>Select at least one text to export, or click Cancel.</value>
  </data>
  <data name="ksSelectTextsForAIExportTitle" xml:space="preserve">
    <value>Select Texts to Export</value>
  </data>
  <data name="ksAIExportColumnText" xml:space="preserve">
    <value>Text</value>
  </data>
  <data name="ksAIExportColumnWords" xml:space="preserve">
    <value>Words</value>
  </data>
  <data name="ksAIExportColumnAnalyses" xml:space="preserve">
    <value>Analyses</value>
  </data>
```

- [ ] **Step 2: Add matching Designer.cs properties**

`xWorksStrings.Designer.cs` IS sorted alphabetically by property name, unlike
the resx — so these 8 properties land in 4 separate locations, each next to
its alphabetical neighbor (same generated pattern as `ksChooseLIFTExportFolder`):
`ksAIExportColumn*` before `ksAbbreviation`; `ksChooseGrammarTextsAIExportFolder(Title)`
before `ksChooseLIFTExportFolder`; `ksGrammarTextsAIExportSummary` after
`ksGeneratingStyleInfo`; `ksNoTextsSelectedForAIExport` after `ksNoExtendedNoteType`;
`ksSelectTextsForAIExportTitle` after `ksSelectedEntryNotInDict`.

```csharp
        /// <summary>
        ///   Looks up a localized string similar to Text.
        /// </summary>
        internal static string ksAIExportColumnText {
            get {
                return ResourceManager.GetString("ksAIExportColumnText", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Words.
        /// </summary>
        internal static string ksAIExportColumnWords {
            get {
                return ResourceManager.GetString("ksAIExportColumnWords", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Analyses.
        /// </summary>
        internal static string ksAIExportColumnAnalyses {
            get {
                return ResourceManager.GetString("ksAIExportColumnAnalyses", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Choose or create a folder for FLEx to put the HC grammar file and one .flextext file per selected text into..
        /// </summary>
        internal static string ksChooseGrammarTextsAIExportFolder {
            get {
                return ResourceManager.GetString("ksChooseGrammarTextsAIExportFolder", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Choose where to save the grammar and texts for AI analysis.
        /// </summary>
        internal static string ksChooseGrammarTextsAIExportFolderTitle {
            get {
                return ResourceManager.GetString("ksChooseGrammarTextsAIExportFolderTitle", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The export finished, but with some issues:{0}{0}{1}.
        /// </summary>
        internal static string ksGrammarTextsAIExportSummary {
            get {
                return ResourceManager.GetString("ksGrammarTextsAIExportSummary", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Select at least one text to export, or click Cancel..
        /// </summary>
        internal static string ksNoTextsSelectedForAIExport {
            get {
                return ResourceManager.GetString("ksNoTextsSelectedForAIExport", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Select Texts to Export.
        /// </summary>
        internal static string ksSelectTextsForAIExportTitle {
            get {
                return ResourceManager.GetString("ksSelectTextsForAIExportTitle", resourceCulture);
            }
        }
```

- [ ] **Step 3: Rebuild to confirm the resx/Designer pair is consistent**

Run: `.\build.ps1`
Expected: build succeeds (a resx/Designer.cs mismatch would otherwise still compile but `ResourceManager.GetString` would return null at runtime — there's no compile-time check, so also grep-verify the two files have matching `ks...` names, see Step 4).

- [ ] **Step 4: Verify the resx and Designer.cs entries match**

Run: `Select-String -Path Src\xWorks\xWorksStrings.resx -Pattern '<data name="ks(AIExportColumn(Text|Words|Analyses)|ChooseGrammarTextsAIExportFolder(Title)?|GrammarTextsAIExportSummary|NoTextsSelectedForAIExport|SelectTextsForAIExportTitle)"'`
Expected: 8 matches (one per new string), and a parallel search with `-Pattern 'internal static string ks(AIExportColumn|ChooseGrammarTextsAIExportFolder|GrammarTextsAIExportSummary|NoTextsSelectedForAIExport|SelectTextsForAIExportTitle)'` on the Designer.cs file also returns 8 matches.

- [ ] **Step 5: Commit**

```powershell
git add Src/xWorks/xWorksStrings.resx Src/xWorks/xWorksStrings.Designer.cs
git commit -m "feat: add localized strings for the grammar+texts AI export"
```

---

## Task 6: WinForms text-selection dialog

**Files:**
- Create: `Src\xWorks\GrammarAndTextsAIExportSelectionDlg.cs`
- Create: `Src\xWorks\GrammarAndTextsAIExportSelectionDlg.Designer.cs`
- Test: `Src\xWorks\xWorksTests\GrammarAndTextsAIExportSelectionDlgTests.cs`

This is a plain WinForms `Form` (per the "use WinForms for the dialog box" directive): a `ListView` with checkboxes and three columns (Text / Words / Analyses), OK/Cancel buttons, and a persisted last-used selection.

- [ ] **Step 1: Write the failing tests for the persistence/selection logic**

These tests exercise the dialog's public surface without showing it modally (`ShowDialog` isn't called in tests — the picker logic is tested directly).

Create `Src\xWorks\xWorksTests\GrammarAndTextsAIExportSelectionDlgTests.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class GrammarAndTextsAIExportSelectionDlgTests
	{
		private LcmCache m_cache;

		[SetUp]
		public void CreateMockCache()
		{
			m_cache = LcmCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(BackendProviderType.kMemoryOnly, null), "en", "fr", "en", new DummyLcmUI(),
				FwDirectoryFinder.LcmDirectories, new LcmSettings());
		}

		[TearDown]
		public void DestroyMockCache()
		{
			m_cache.Dispose();
			m_cache = null;
		}

		// See Task 3's SIL.LCModel.IText note: this test project also sees ITextDll.
		private IStText MakeText(string title)
		{
			SIL.LCModel.IText text = null;
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				text = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				m_cache.LangProject.Texts.Add(text);
				text.ContentsOA = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.Name.SetAnalysisDefaultWritingSystem(title);
			});
			return text.ContentsOA;
		}

		[Test]
		public void ApplyPreviousSelection_OnlyChecksTextsThatWereSelectedBefore()
		{
			var textA = MakeText("Text A");
			var textB = MakeText("Text B");
			using (var dlg = new GrammarAndTextsAIExportSelectionDlg(m_cache, new[] { textA, textB }))
			{
				dlg.ApplyPreviousSelection(new HashSet<string> { textA.Guid.ToString() });

				var selected = dlg.SelectedTexts.ToList();
				Assert.That(selected, Has.Count.EqualTo(1));
				Assert.That(selected[0], Is.SameAs(textA));
			}
		}

		[Test]
		public void ApplyPreviousSelection_WithNoPriorSelection_ChecksEveryText()
		{
			var textA = MakeText("Text A");
			var textB = MakeText("Text B");
			using (var dlg = new GrammarAndTextsAIExportSelectionDlg(m_cache, new[] { textA, textB }))
			{
				dlg.ApplyPreviousSelection(null);

				Assert.That(dlg.SelectedTexts.ToList(), Has.Count.EqualTo(2));
			}
		}
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.GrammarAndTextsAIExportSelectionDlgTests"`
Expected: build failure — `GrammarAndTextsAIExportSelectionDlg` does not exist.

- [ ] **Step 3: Implement the dialog's designer partial**

Create `Src\xWorks\GrammarAndTextsAIExportSelectionDlg.Designer.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
namespace SIL.FieldWorks.XWorks
{
	partial class GrammarAndTextsAIExportSelectionDlg
	{
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.ListView m_textListView;
		private System.Windows.Forms.ColumnHeader m_columnText;
		private System.Windows.Forms.ColumnHeader m_columnWords;
		private System.Windows.Forms.ColumnHeader m_columnAnalyses;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
				components.Dispose();
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.m_textListView = new System.Windows.Forms.ListView();
			this.m_columnText = new System.Windows.Forms.ColumnHeader();
			this.m_columnWords = new System.Windows.Forms.ColumnHeader();
			this.m_columnAnalyses = new System.Windows.Forms.ColumnHeader();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_textListView
			//
			this.m_textListView.CheckBoxes = true;
			this.m_textListView.View = System.Windows.Forms.View.Details;
			this.m_textListView.FullRowSelect = true;
			this.m_textListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
				this.m_columnText, this.m_columnWords, this.m_columnAnalyses});
			this.m_textListView.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_textListView.Height = 340;
			this.m_columnText.Text = xWorksStrings.ksAIExportColumnText;
			this.m_columnText.Width = 260;
			this.m_columnWords.Text = xWorksStrings.ksAIExportColumnWords;
			this.m_columnWords.Width = 80;
			this.m_columnWords.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.m_columnAnalyses.Text = xWorksStrings.ksAIExportColumnAnalyses;
			this.m_columnAnalyses.Width = 80;
			this.m_columnAnalyses.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// m_btnOk
			//
			this.m_btnOk.Text = xWorksStrings.ksOK;
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.Location = new System.Drawing.Point(320, 350);
			this.m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
			//
			// m_btnCancel
			//
			this.m_btnCancel.Text = xWorksStrings.ksCancel;
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Location = new System.Drawing.Point(405, 350);
			//
			// GrammarAndTextsAIExportSelectionDlg
			//
			this.AcceptButton = this.m_btnOk;
			this.CancelButton = this.m_btnCancel;
			this.ClientSize = new System.Drawing.Size(500, 390);
			this.Controls.Add(this.m_textListView);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_btnCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = xWorksStrings.ksSelectTextsForAIExportTitle;
			this.ResumeLayout(false);
		}
	}
}
```

- [ ] **Step 4: Implement the dialog's code-behind**

Create `Src\xWorks\GrammarAndTextsAIExportSelectionDlg.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Lets the user pick which project texts to include in a grammar+texts-for-AI export,
	/// showing a Words and an Analyses count per text (see GrammarTextsAIExportHelpers).
	/// </summary>
	public partial class GrammarAndTextsAIExportSelectionDlg : Form
	{
		private readonly List<IStText> m_texts;

		public GrammarAndTextsAIExportSelectionDlg(LcmCache cache, IEnumerable<IStText> texts)
		{
			InitializeComponent();
			m_texts = texts.ToList();
			foreach (var stText in m_texts)
			{
				var counts = GrammarTextsAIExportHelpers.CountWordsAndAnalyses(stText);
				var name = GrammarTextsAIExportHelpers.GetTextDisplayName(stText);
				var item = new ListViewItem(new[] { name, counts.Words.ToString(), counts.Analyses.ToString() })
				{
					Tag = stText,
					Checked = true
				};
				m_textListView.Items.Add(item);
			}
		}

		/// <summary>
		/// Checks only the texts whose Guid string is in previousSelectionGuids, leaving
		/// every other row unchecked. If previousSelectionGuids is null (first use), every
		/// row stays checked (the default set in the constructor).
		/// </summary>
		public void ApplyPreviousSelection(HashSet<string> previousSelectionGuids)
		{
			if (previousSelectionGuids == null)
				return;
			foreach (ListViewItem item in m_textListView.Items)
			{
				var stText = (IStText)item.Tag;
				item.Checked = previousSelectionGuids.Contains(stText.Guid.ToString());
			}
		}

		public IEnumerable<IStText> SelectedTexts =>
			m_textListView.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (IStText)i.Tag);

		private void m_btnOk_Click(object sender, System.EventArgs e)
		{
			if (!SelectedTexts.Any())
			{
				MessageBox.Show(this, xWorksStrings.ksNoTextsSelectedForAIExport);
				DialogResult = DialogResult.None;
			}
		}
	}
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.GrammarAndTextsAIExportSelectionDlgTests"`
Expected: PASS (2 tests)

- [ ] **Step 6: Commit**

```powershell
git add Src/xWorks/GrammarAndTextsAIExportSelectionDlg.cs Src/xWorks/GrammarAndTextsAIExportSelectionDlg.Designer.cs Src/xWorks/xWorksTests/GrammarAndTextsAIExportSelectionDlgTests.cs
git commit -m "feat: add WinForms text-selection dialog for AI export"
```

**pr-preflight correction:** the code above hardcoded `"OK"`/`"Cancel"` as literal
button text, which the review policy's localization check catches (every
sibling dialog pulls these from a resx, e.g. `ExportSemanticDomainsDlg`'s
`resources.ApplyResources(this.m_okButton, "m_okButton")`). Fixed by adding
`ksOK`/`ksCancel` to `xWorksStrings.resx`/`.Designer.cs` (Task 5's shared
strings file, not a new per-form resx) and using `xWorksStrings.ksOK`/
`xWorksStrings.ksCancel` here instead.

---

## Task 7: Wire the new export type into `ExportDialog`

**Files:**
- Modify: `Src\xWorks\ExportDialog.cs`
- Test: `Src\xWorks\xWorksTests\ExportDialogTests.cs`

- [ ] **Step 1: Add the new `FxtTypes` enum value**

In `Src\xWorks\ExportDialog.cs`, in the `FxtTypes` enum (around line 78-93), add after `kftPhonology`:

```csharp
			kftPhonology,
			kftGrammarTextsAI
```

- [ ] **Step 2: Map the new `type` attribute in `ConfigureItem`**

In the `switch (sType)` block inside `ConfigureItem` (around line 1433-1477), add after the `case "phonology":` block:

```csharp
				case "grammarTextsAI":
					ft.m_ft = FxtTypes.kftGrammarTextsAI;
					break;
```

- [ ] **Step 3: Write the failing test for the picker-then-folder flow's supporting method**

Rather than driving the full modal `btnExport_Click` UI (which needs a message loop), Steps 3-6 test the new task method (`ExportGrammarAndTextsForAI`) directly, the same way `ExportDialogTests.ExportSemanticDomains` already calls `exportDlg.ExportSemanticDomains(...)` directly. This needs more fixture setup than a first guess suggests, because `HCLoader` (called inside `ExportGrammarAndTextsForAI`) has real structural preconditions beyond "the cache exists" — a blank `LcmCache` from `CreateCacheWithNewBlankLangProj` satisfies none of them. Discovered by running the test and fixing each crash in turn:

1. `MorphologicalDataOA.ParserParameters` must be a valid XML fragment (`XElement.Parse` on it or it throws).
2. `PhonologicalDataOA.PhonemeSetsOS` must have at least one phoneme set (indexed directly at `[0]`).
3. That phoneme set must have morph (`+`) and word (`#`) boundary markers (`HCLoaderTests.AddBdry`'s pattern) — `LoadCharacterDefinitionTable` looks one up by representation and throws `KeyNotFoundException` otherwise.
4. `ExportGrammarAndTextsForAI` calls `m_propertyTable.GetWindow()` to publish the FLExText request, so the test needs a real (if minimal) `Mediator`/`PropertyTable`, not just `SetCache` — add a parallel `internal void SetPropertyTable(PropertyTable propertyTable)` setter (Step 5b) for this.

Add to `Src\xWorks\xWorksTests\ExportDialogTests.cs` — a private helper plus the test itself (anywhere after the class's existing `#region` helpers), and add `using SIL.LCModel.Core.Text;` and `using XCore;` to the file's usings if not already present:

```csharp
		private void AddBoundaryMarker(Guid guid, string strRep, IPhPhonemeSet phonemeSet)
		{
			var bdry = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerFactory>().Create(guid, phonemeSet);
			var tss = TsStringUtils.MakeString(strRep, m_cache.DefaultAnalWs);
			bdry.Name.set_String(m_cache.DefaultAnalWs, tss);
			var code = m_cache.ServiceLocator.GetInstance<IPhCodeFactory>().Create();
			bdry.CodesOS.Add(code);
			code.Representation.set_String(m_cache.DefaultAnalWs, tss);
		}

		[Test]
		public void ExportGrammarAndTextsForAI_NoFlexTextListenerRegistered_RecordsFailureAndStillWritesGrammar()
		{
			// HCLoader requires MorphologicalDataOA.ParserParameters to be a valid XML
			// fragment and a phoneme set with morph/word boundary markers;
			// CreateCacheWithNewBlankLangProj leaves all of that empty.
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				m_cache.LanguageProject.MorphologicalDataOA.ParserParameters =
					"<ParserParameters><ActiveParser>HC</ActiveParser><HC/></ParserParameters>";
				var phonemeSet = m_cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create();
				m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(phonemeSet);
				AddBoundaryMarker(LangProjectTags.kguidPhRuleMorphBdry, "+", phonemeSet);
				AddBoundaryMarker(LangProjectTags.kguidPhRuleWordBdry, "#", phonemeSet);
			});
			var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempFolder);
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			{
				try
				{
					using (var exportDlg = new ExportDialog())
					{
						exportDlg.SetCache(m_cache);
						exportDlg.SetPropertyTable(propertyTable);
						exportDlg.SetSelectedTextsForAIExport(new List<IStText>());

						var messages = (List<string>)exportDlg.ExportGrammarAndTextsForAI(new DummyProgressDlg(),
							new object[] { Path.Combine(tempFolder, "HCGrammar.xml") });

						Assert.That(File.Exists(Path.Combine(tempFolder, "HCGrammar.xml")), Is.True);
						Assert.That(messages, Has.Count.EqualTo(1));
						Assert.That(messages[0], Does.Contain("FLExText export service was not available"));
					}
				}
				finally
				{
					Directory.Delete(tempFolder, true);
				}
			}
		}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.ExportDialogTests.ExportGrammarAndTextsForAI_NoFlexTextListenerRegistered_RecordsFailureAndStillWritesGrammar"`
Expected: build failure — `SetSelectedTextsForAIExport`/`SetPropertyTable`/`ExportGrammarAndTextsForAI` do not exist yet.

- [ ] **Step 5: Add the fields, the picker/folder-browser flow, the `DoExport` dispatch, and the task method**

In `Src\xWorks\ExportDialog.cs`:

5a. Add a new field near the other picker-result fields (`m_translationWritingSystems`, `m_translatedLists`, `m_allQuestions`, around line 534-536):

```csharp
		private List<IStText> m_selectedTextsForAIExport;
```

5b. Add two internal setters used by the test above and by the button-click flow, right after `SetCache` (around line 1700 — `SetCache` already exists as a "for testing" setter; add `SetPropertyTable` immediately after it) and right after `PrepareForExport()` (around line 750):

```csharp
		/// <summary>
		/// for testing
		/// </summary>
		internal void SetPropertyTable(PropertyTable propertyTable)
		{
			m_propertyTable = propertyTable;
		}
```

```csharp
		/// <summary>Used by the text-selection dialog step and by tests.</summary>
		internal void SetSelectedTextsForAIExport(List<IStText> texts)
		{
			m_selectedTextsForAIExport = texts;
		}
```

5c. In `btnExport_Click`, change this line (around line 550):

```csharp
				bool fLiftExport = m_exportItems[0].SubItems[2].Text == "lift";
```

to:

```csharp
				bool fLiftExport = m_exportItems[0].SubItems[2].Text == "lift";
				bool fGrammarTextsAIExport = m_rgFxtTypes.Count > 0
					&& m_rgFxtTypes[FxtIndex((string)m_exportItems[0].Tag)].m_ft == FxtTypes.kftGrammarTextsAI;
```

Then find the lone `else` line that separates the LIFT branch from the generic-FXT branch (around line 595 — the `else` immediately before `FxtType ft;`):

```csharp
				}
				else
				{
					FxtType ft;
```

and change it to insert a new branch between them:

```csharp
				}
				else if (fGrammarTextsAIExport)
				{
					var textList = InterestingTextsDecorator.GetInterestingTextList(m_mediator, m_propertyTable, m_cache.ServiceLocator).InterestingTexts;
					using (var textDlg = new GrammarAndTextsAIExportSelectionDlg(m_cache, textList))
					{
						var previousSelection = m_propertyTable.GetStringProperty("GrammarTextsAIExportSelection", null);
						if (previousSelection != null)
							textDlg.ApplyPreviousSelection(new HashSet<string>(previousSelection.Split(',')));
						if (textDlg.ShowDialog(this) != DialogResult.OK)
							return;
						m_selectedTextsForAIExport = textDlg.SelectedTexts.ToList();
						m_propertyTable.SetProperty("GrammarTextsAIExportSelection",
							string.Join(",", m_selectedTextsForAIExport.Select(t => t.Guid.ToString())), true);
						m_propertyTable.SetPropertyPersistence("GrammarTextsAIExportSelection", true);
					}
					using (var dlg = new FolderBrowserDialogAdapter())
					{
						dlg.Description = xWorksStrings.ksChooseGrammarTextsAIExportFolder;
						dlg.ShowNewFolderButton = true;
						dlg.RootFolder = Environment.SpecialFolder.Desktop;
						dlg.SelectedPath = m_propertyTable.GetStringProperty("ExportDir",
							Environment.GetFolderPath(Environment.SpecialFolder.Personal));
						if (dlg.ShowDialog(this) != DialogResult.OK)
							return;
						sDirectory = dlg.SelectedPath;
					}
					sFileName = Path.Combine(sDirectory, "HCGrammar.xml");
				}
```

(`GrammarAndTextsAIExportSelectionDlg` and `InterestingTextsDecorator` are both in the `SIL.FieldWorks.XWorks` namespace already `using`d by this file; add `using System.Collections.Generic;` and `System.Linq;` if not already present — both already are, per the file's existing usings.)

5d. In `DoExport(string outPath, bool fLiftOutput)`'s `switch (ft.m_ft)` (around line 825-853), add after the `kftPhonology` case. `RunTask` blocks the UI thread until the background task finishes (see `ProgressDialogWithTask.RunTask`), so it's safe to show the summary `MessageBox` right after it returns — but NOT from inside the task method itself, which runs on a background `BackgroundWorker` thread where cross-thread `MessageBox.Show(this, ...)` is unsafe (and, in a headless test run with no user to click it, hangs forever):

```csharp
						case FxtTypes.kftGrammarTextsAI:
						{
							progressDlg.Minimum = 0;
							progressDlg.Maximum = m_selectedTextsForAIExport.Count + 1;
							progressDlg.AllowCancel = true;
							var aiExportMessages = (List<string>)progressDlg.RunTask(true, ExportGrammarAndTextsForAI, outPath);
							if (aiExportMessages.Count > 0)
							{
								MessageBox.Show(this, string.Format(xWorksStrings.ksGrammarTextsAIExportSummary,
									Environment.NewLine, string.Join(Environment.NewLine, aiExportMessages)));
							}
							break;
						}
```

5e. Add the task method itself, near `ExportPhonology` (around line 1094). It returns the combined warnings/failures list instead of showing them itself, for exactly the threading reason above:

```csharp
		/// <summary>
		/// Writes the HC grammar (HCGrammar.xml, at outPath) and one .flextext file per
		/// selected text (in the same folder) for the "Export Grammar and Texts for AI
		/// Analysis" option. An unhandled exception from the HC-grammar step aborts the
		/// whole export -- HCLoader already catches per-item linguistic problems internally
		/// and routes them to the logger, so anything that escapes indicates a real bug, not
		/// messy grammar data. Per-text failures are independent and merely skip that text.
		/// Returns the combined list of HC-load warnings and per-text failures (empty if
		/// none) -- this runs on the background task thread, so the caller (on the UI
		/// thread, after RunTask returns) is responsible for showing them, if any.
		/// </summary>
		internal object ExportGrammarAndTextsForAI(IThreadedProgress progress, object[] args)
		{
			var outPath = (string)args[0];
			var outFolder = Path.GetDirectoryName(outPath);
			var loadMessages = new List<string>();
			var logger = new GrammarExportLoadLogger(loadMessages);
			var language = HCLoader.Load(m_cache, logger);
			XmlLanguageWriter.Save(language, outPath);
			progress.Step(1);

			var texts = m_selectedTextsForAIExport ?? new List<IStText>();
			var request = new ExportTextsAsFlexTextRequest(texts, outFolder);
			Publisher.Publish(new PublisherParameterObject(EventConstants.ExportTextsAsFlexText, request, m_propertyTable.GetWindow()));
			if (!request.Handled)
				request.Failures.Add("FLExText export service was not available.");
			foreach (var text in texts)
				progress.Step(1);

			return loadMessages.Concat(request.Failures).ToList();
		}
```

Note `outPath` here is the full `HCGrammar.xml` path (that's what `sFileName` was set to in Step 5c), not the folder — `outFolder` is derived from it for the FLExText request. No `try`/`catch` around the `HCLoader.Load`/`XmlLanguageWriter.Save` calls: letting an exception propagate is exactly what makes it surface as a `WorkerThreadException` through `RunTask` and get caught by `btnExport_Click`'s existing generic `catch (WorkerThreadException e)` handler — the same path every other export type's failures already go through — so the "abort the whole export" behavior falls out of the existing infrastructure for free.

Add these `using`s at the top of `ExportDialog.cs` if not already present: `using SIL.FieldWorks.WordWorks.Parser;` and `using SIL.Machine.Morphology.HermitCrab;`.

- [ ] **Step 6: Run the test to verify it passes**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests.ExportDialogTests.ExportGrammarAndTextsForAI_NoFlexTextListenerRegistered_RecordsFailureAndStillWritesGrammar"`
Expected: PASS — `HCGrammar.xml` exists in the temp folder, and the returned message list contains exactly one entry ("FLExText export service was not available.") since no listener is registered in this unit test.

- [ ] **Step 7: Run the full xWorksTests suite to check for regressions**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~xWorksTests"`
Expected: all tests PASS, including the pre-existing `ExportDialogTests` and the new ones from Tasks 3, 4, 6.

- [ ] **Step 8: Commit**

```powershell
git add Src/xWorks/ExportDialog.cs Src/xWorks/xWorksTests/ExportDialogTests.cs
git commit -m "feat: wire the grammar+texts AI export into ExportDialog"
```

---

## Task 8: `FlexTextAIExportListener` (`ITextDll`)

**Files:**
- Create: `Src\LexText\Interlinear\FlexTextAIExportListener.cs`
- Test: `Src\LexText\Interlinear\ITextDllTests\FlexTextAIExportListenerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Src\LexText\Interlinear\ITextDllTests\FlexTextAIExportListenerTests.cs`. Unlike Tasks 3/6/7 (a bare `LcmCache` in xWorksTests, with no ambient undo task, so object creation there is wrapped in `UndoableUnitOfWorkHelper.Do`), `InterlinearTestBase`'s fixture already runs each test inside an ambient undo task — wrapping object creation in another `UndoableUnitOfWorkHelper.Do` here throws `InvalidOperationException: Nested tasks are not supported.` So this test creates objects directly, matching the pattern already used by `ComplexConcPatternModelTests.MakeText` in this same project:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class FlexTextAIExportListenerTests : InterlinearTestBase
	{
		[Test]
		public void ExportTextsAsFlexText_OneText_WritesOneFlexTextFileAndMarksHandled()
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.Texts.Add(text);
			text.ContentsOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.Name.SetAnalysisDefaultWritingSystem("Listener Test Text");
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ContentsOA.ParagraphsOS.Add(para);
			para.Contents = TsStringUtils.MakeString("hello world", Cache.DefaultVernWs);

			var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempFolder);
			try
			{
				var listener = new FlexTextAIExportListener();
				var request = new ExportTextsAsFlexTextRequest(new[] { text.ContentsOA }, tempFolder);

				listener.ExportTextsAsFlexTextForTests(Cache, request);

				Assert.That(request.Handled, Is.True);
				Assert.That(request.Failures, Is.Empty);
				Assert.That(File.Exists(Path.Combine(tempFolder, "Listener Test Text.flextext")), Is.True);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}
	}
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~ITextDllTests.FlexTextAIExportListenerTests"`
Expected: build failure — `FlexTextAIExportListener` does not exist.

- [ ] **Step 3: Implement the listener**

Create `Src\LexText\Interlinear\FlexTextAIExportListener.cs`:

```csharp
// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.IO;
using System.Text;
using System.Xml;
using SIL.LCModel;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.FieldWorks.XWorks;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Answers EventConstants.ExportTextsAsFlexText for the grammar+texts-for-AI export
	/// (SIL.FieldWorks.XWorks.ExportDialog). Lives here, rather than in xWorks where the
	/// export dialog itself lives, because InterlinVc/InterlinearExporter live in this
	/// project (ITextDll), which already references xWorks -- a reference in the other
	/// direction would be a build-breaking cycle. Registered globally in Main.xml's
	/// &lt;listeners&gt; section, so it answers regardless of which area is active.
	/// </summary>
	public class FlexTextAIExportListener : IxCoreColleague, IDisposable
	{
		private LcmCache m_cache;
		private PropertyTable m_propertyTable;
		private bool m_isDisposed;

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_propertyTable = propertyTable;
			m_cache = propertyTable.GetValue<LcmCache>("cache");
			mediator.AddColleague(this);
			Subscriber.Subscribe(EventConstants.ExportTextsAsFlexText, OnExportTextsAsFlexText, m_propertyTable.GetWindow());
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall => false;

		public int Priority => (int)ColleaguePriority.Medium;

		private void OnExportTextsAsFlexText(object parameterObj)
		{
			if (!(parameterObj is ExportTextsAsFlexTextRequest request))
				return;
			ExportTextsAsFlexTextForTests(m_cache, request);
		}

		/// <summary>
		/// The actual export logic, factored out so tests can call it without going
		/// through the Publisher/Subscriber pipeline.
		/// </summary>
		internal void ExportTextsAsFlexTextForTests(LcmCache cache, ExportTextsAsFlexTextRequest request)
		{
			request.Handled = true;
			var usedNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HCGrammar" };
			foreach (var stText in request.TextsToExport)
			{
				var name = GrammarTextsAIExportHelpers.GetTextDisplayName(stText);
				try
				{
					var fileName = GrammarTextsAIExportHelpers.MakeSafeFileName(name, usedNames);
					var filePath = Path.Combine(request.OutputFolder, fileName + ".flextext");
					var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
					using (var vc = new InterlinVc(cache))
					using (var writer = XmlWriter.Create(filePath, settings))
					{
						vc.LineChoices = InterlinLineChoices.DefaultChoices(cache.LangProject, cache.DefaultVernWs, cache.DefaultAnalWs);
						var exporter = InterlinearExporter.Create("xml", cache, writer, stText, vc.LineChoices, vc);
						exporter.WriteBeginDocument();
						exporter.ExportDisplay();
						exporter.WriteEndDocument();
					}
				}
				catch (Exception e)
				{
					request.Failures.Add($"{name}: {e.Message}");
				}
			}
		}

		public void Dispose()
		{
			if (m_isDisposed)
				return;
			Subscriber.Unsubscribe(EventConstants.ExportTextsAsFlexText, OnExportTextsAsFlexText);
			m_isDisposed = true;
		}
	}
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `.\test.ps1 -TestFilter "FullyQualifiedName~ITextDllTests.FlexTextAIExportListenerTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add Src/LexText/Interlinear/FlexTextAIExportListener.cs Src/LexText/Interlinear/ITextDllTests/FlexTextAIExportListenerTests.cs
git commit -m "feat: add FlexTextAIExportListener to answer the AI-export text request"
```

---

## Task 9: Register the listener in `Main.xml`

**Files:**
- Modify: `DistFiles\Language Explorer\Configuration\Main.xml`

- [ ] **Step 1: Add the listener entry**

In `DistFiles\Language Explorer\Configuration\Main.xml`, in the `<listeners>` section (around line 912-933), insert alphabetically by class name after the `FLExBridgeListener` line:

```xml
		<listener assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.FlexTextAIExportListener"/>
```

- [ ] **Step 2: Rebuild and smoke-check the configuration loads**

Run: `.\build.ps1`
Expected: build succeeds. There is no automated test that loads `Main.xml`'s listener list end-to-end in this codebase's test suite (the sibling `AreaListener`/`FLExBridgeListener` entries aren't unit-tested this way either) — verify manually per Task 10.

- [ ] **Step 3: Commit**

```powershell
git add "DistFiles/Language Explorer/Configuration/Main.xml"
git commit -m "feat: register FlexTextAIExportListener as a global listener"
```

---

## Task 10: Manual verification

**Files:** none (verification only)

- [ ] **Step 1: Run the full test suite**

Run: `.\test.ps1`
Expected: all tests PASS (no regressions in `xWorksTests`, `ITextDllTests`, `FwUtilsTests`, or elsewhere).

- [ ] **Step 2: Launch FieldWorks and open a project with at least one analyzed text**

Use the `fieldworks-winapp` skill to launch the app and open a project such as the standard `TestLangProj` sample (or any project with existing texts).

- [ ] **Step 3: Verify the export option appears and produces the right files**

- Open File > Export from the Lexicon area; confirm "Grammar and Texts" / "XML for AI Analysis" appears in the list with the description "Export Grammar and Texts for AI Analysis".
- Select it, click Export; confirm the text-picker dialog lists every text with plausible Words/Analyses counts, and that unchecking a text and re-opening the dialog on a later export remembers the unchecked state.
- Confirm the folder browser appears next, and after picking a folder, the folder ends up containing `HCGrammar.xml` plus one `.flextext` file per checked text, named after each text's title, with no `Texts\` subfolder.
- Repeat from the Grammar area's Export dialog and confirm the same option is available there too.

- [ ] **Step 4: Take a screenshot of the populated Export dialog and the resulting output folder for the PR**

Use the `smart-screenshot-capture` skill if available, or the `winforms-mcp` tools directly, to capture evidence for the PR description.
