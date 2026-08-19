// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexText;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LexTextDllTests
{
	/// <summary>
	/// Covers the grammar half of the AI-analysis export, which the listener runs before
	/// handing the texts to the interlinear exporter.
	/// </summary>
	[TestFixture]
	public class AiAnalysisExportListenerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void Export_WritesTheGrammarAndMarksTheRequestHandled()
		{
			SeedMinimalHCGrammarData();
			var tempFolder = MakeTempFolder();
			try
			{
				var grammarPath = Path.Combine(tempFolder, "HCGrammar.xml");
				var request = new AiAnalysisExportRequest(new List<IStText>(), tempFolder, grammarPath);

				AiAnalysisExportListener.Export(Cache, request);

				Assert.That(request.Handled, Is.True);
				Assert.That(File.Exists(grammarPath), Is.True);
				Assert.That(request.Messages, Is.Empty, string.Join("; ", request.Messages));
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}

		[Test]
		public void Export_UnloadableGrammar_ThrowsAndWritesNothing()
		{
			// Deliberately skips seeding ParserParameters/phonemes/boundary markers, so the
			// grammar cannot be built. That must propagate rather than get swallowed, so the
			// whole export aborts instead of silently omitting the grammar.
			var tempFolder = MakeTempFolder();
			try
			{
				var grammarPath = Path.Combine(tempFolder, "HCGrammar.xml");
				var request = new AiAnalysisExportRequest(new List<IStText>(), tempFolder, grammarPath);

				Assert.Throws<ArgumentNullException>(() => AiAnalysisExportListener.Export(Cache, request));

				Assert.That(File.Exists(grammarPath), Is.False);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}

		/// <summary>
		/// Gives the grammar loader the minimum it needs: ParserParameters as a valid XML fragment
		/// and a phoneme set carrying morph and word boundary markers, none of which the blank
		/// test project provides.
		/// </summary>
		/// <remarks>
		/// Already runs inside an ambient undo task here, so this must not open its own --
		/// LCM disallows nested undo tasks.
		/// </remarks>
		private void SeedMinimalHCGrammarData()
		{
			Cache.LanguageProject.MorphologicalDataOA.ParserParameters =
				"<ParserParameters><ActiveParser>HC</ActiveParser><HC/></ParserParameters>";
			var phonemeSet = Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(phonemeSet);
			AddBoundaryMarker(LangProjectTags.kguidPhRuleMorphBdry, "+", phonemeSet);
			AddBoundaryMarker(LangProjectTags.kguidPhRuleWordBdry, "#", phonemeSet);
		}

		private void AddBoundaryMarker(Guid guid, string strRep, IPhPhonemeSet phonemeSet)
		{
			var bdry = Cache.ServiceLocator.GetInstance<IPhBdryMarkerFactory>().Create(guid, phonemeSet);
			var tss = TsStringUtils.MakeString(strRep, Cache.DefaultAnalWs);
			bdry.Name.set_String(Cache.DefaultAnalWs, tss);
			var code = Cache.ServiceLocator.GetInstance<IPhCodeFactory>().Create();
			bdry.CodesOS.Add(code);
			code.Representation.set_String(Cache.DefaultAnalWs, tss);
		}

		private static string MakeTempFolder()
		{
			var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempFolder);
			return tempFolder;
		}
	}
}
