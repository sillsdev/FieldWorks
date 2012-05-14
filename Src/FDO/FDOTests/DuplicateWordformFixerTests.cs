using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests the DuplicateWordformFixer
	/// </summary>
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_progress gets disposed in TestTearDown()")]
	public class DuplicateWordformFixerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IWfiWordformFactory m_wfiFactory;
		private ProgressBar m_progress;

		/// <summary>
		/// Set up some common data
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();
			m_wfiFactory = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
			m_progress = new ProgressBar();
		}

		/// <summary/>
		public override void TestTearDown()
		{
			m_progress.Dispose();
			m_progress = null;
			base.TestTearDown();
		}

		/// <summary>
		/// Test the simplest possible case.
		/// </summary>
		[Test]
		public void WordformsWithNothingSpecialMerged()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			// One but only one should survive.
			Survivor(wf1, wf2, wf3);
		}

		/// <summary>
		/// Analyses of all the wordforms should be preserved.
		/// </summary>
		[Test]
		public void AnalysesPreserved()
		{
			var wf1 = MakeWordform("wordXX");
			var wa1 = MakeAnalysis(wf1);
			var wf2 = MakeWordform("wordXX");
			var wa2 = MakeAnalysis(wf2);
			var wf3 = MakeWordform("wordXX");
			var wa3 = MakeAnalysis(wf3);
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			var wf = Survivor(wf1, wf2, wf3);
			Assert.That(wa1.Owner, Is.EqualTo(wf));
			Assert.That(wa2.Owner, Is.EqualTo(wf));
			Assert.That(wa3.Owner, Is.EqualTo(wf));
		}

		/// <summary>
		/// Should ignore wordform with null vernacular.
		/// </summary>
		[Test]
		public void CopesWithNullVernacular()
		{
			var wf1 = MakeWordform("");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			var wf = Survivor(wf2, wf3);
			Assert.That(wf1.IsValidObject, Is.True);
		}

		/// <summary>
		/// References to the deleted wordforms should be switched
		/// </summary>
		[Test]
		public void ReferencesToDeletedWordformsShouldSwitch()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			var seg = Cache.ServiceLocator.GetInstance<ISegmentFactory>().Create();
			para.SegmentsOS.Add(seg);
			seg.AnalysesRS.Add(wf1);
			seg.AnalysesRS.Add(wf2);
			seg.AnalysesRS.Add(wf3);
			seg.AnalysesRS.Add(wf2);
			seg.AnalysesRS.Add(wf3);

			var wordset = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordset);
			wordset.CasesRC.Add(wf2);

			ICmPossibilityList list = Cache.LangProject.KeyTermsList;
			var term = Cache.ServiceLocator.GetInstance<IChkTermFactory>().Create();
			list.PossibilitiesOS.Add(term);
			var rendering = Cache.ServiceLocator.GetInstance<IChkRenderingFactory>().Create();
			term.RenderingsOC.Add(rendering);
			rendering.SurfaceFormRA = wf2;
			var chkRef = Cache.ServiceLocator.GetInstance<IChkRefFactory>().Create();
			term.OccurrencesOS.Add(chkRef);
			chkRef.RenderingRA = wf2;

			WfiWordformServices.FixDuplicates(Cache, m_progress);
			// One but only one should survive.
			var wf = Survivor(wf1, wf2, wf3);
			Assert.That(seg.AnalysesRS.Count, Is.EqualTo(5));
			Assert.That(seg.AnalysesRS[0], Is.EqualTo(wf));
			Assert.That(seg.AnalysesRS[1], Is.EqualTo(wf));
			Assert.That(seg.AnalysesRS[2], Is.EqualTo(wf));
			Assert.That(seg.AnalysesRS[3], Is.EqualTo(wf));
			Assert.That(seg.AnalysesRS[4], Is.EqualTo(wf));

			// for several tests to prove anything, the wordform that survives must NOT be the
			// one that was there already.
			Assert.That(wf, Is.Not.EqualTo(wf2));

			Assert.That(wordset.CasesRC, Has.Member(wf));
			Assert.That(rendering.SurfaceFormRA, Is.EqualTo(wf));
			Assert.That(chkRef.RenderingRA, Is.EqualTo(wf));
		}

		/// <summary>
		/// Test the simplest possible case.
		/// </summary>
		[Test]
		public void SpellingCorrectWins()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			wf2.SpellingStatus = (int)SpellingStatusStates.correct;
			wf3.SpellingStatus = (int)SpellingStatusStates.incorrect;
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			// One but only one should survive.
			var wf = Survivor(wf1, wf2, wf3);
			Assert.That(wf, Is.Not.EqualTo(wf2)); // otherwise it doesn't prove much
			Assert.That(wf.SpellingStatus, Is.EqualTo((int)SpellingStatusStates.correct));
		}

		/// <summary>
		/// Test the simplest possible case.
		/// </summary>
		[Test]
		public void SpellingInCorrectBeatsUnknown()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			wf2.SpellingStatus = (int)SpellingStatusStates.incorrect;
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			// One but only one should survive.
			var wf = Survivor(wf1, wf2, wf3);
			Assert.That(wf, Is.Not.EqualTo(wf2)); // otherwise it doesn't prove much
			Assert.That(wf.SpellingStatus, Is.EqualTo((int)SpellingStatusStates.incorrect));
		}

		/// <summary>
		/// Test the simplest possible case.
		/// </summary>
		[Test]
		public void ChecksumIsReset()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			wf1.Checksum = 5;
			wf2.Checksum = 6;
			wf3.Checksum = 7;
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			var wf = Survivor(wf1, wf2, wf3);
			Assert.That(wf, Is.Not.EqualTo(wf2)); // otherwise it doesn't prove much
			Assert.That(wf.Checksum, Is.EqualTo(0));
		}

		/// <summary>
		/// If the deleted one has writing systems set for the form that the surviving one lacks, they should be copied over.
		/// </summary>
		[Test]
		public void ExtraWritingSystemsAreCopied()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordXX");
			int wsSpn = Cache.WritingSystemFactory.GetWsFromStr("es");
			wf2.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("Spanish", wsSpn));
			WfiWordformServices.FixDuplicates(Cache, m_progress);
			var wf = Survivor(wf1, wf2, wf3);
			Assert.That(wf, Is.Not.EqualTo(wf2)); // otherwise it doesn't prove much
			Assert.That(wf.Form.get_String(wsSpn).Text, Is.EqualTo("Spanish"));
		}

		/// <summary>
		/// Duplicates that don't match on some other alternative are listed in the results.
		/// </summary>
		[Test]
		public void InconsistentAlternativesPreventMergeAndAreReported()
		{
			var wf1 = MakeWordform("wordXX");
			var wf2 = MakeWordform("wordXX");
			var wf3 = MakeWordform("wordZZ");
			var wf4 = MakeWordform("wordZZ");
			var wf5 = MakeWordform("wordZZ");
			int wsSpn = Cache.WritingSystemFactory.GetWsFromStr("es");
			wf1.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("Spanish", wsSpn));
			wf2.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("SpanishOther", wsSpn));
			wf3.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("Spanish", wsSpn));
			wf4.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("SpanishOther", wsSpn));
			wf5.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("SpanishYetAnother", wsSpn));
			var failureList = WfiWordformServices.FixDuplicates(Cache, m_progress);

			// None should be deleted.
			Assert.That(wf1.IsValidObject && wf2.IsValidObject && wf3.IsValidObject && wf4.IsValidObject && wf5.IsValidObject);
			Assert.That(failureList, Is.EqualTo("wordXX wordZZ"));
		}

		IWfiWordform MakeWordform(string form)
		{
			var result = m_wfiFactory.Create();
			result.Form.VernacularDefaultWritingSystem = MakeVernString(form);
			return result;
		}

		ITsString MakeVernString(string text)
		{
			return Cache.TsStrFactory.MakeString(text, Cache.DefaultVernWs);
		}

		IWfiAnalysis MakeAnalysis(IWfiWordform wf)
		{
			// Enhance JohnT: could make at least one bundle, or a gloss...but given the implementation this doesn't add much.
			var result = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(result);
			return result;
		}

		IWfiWordform Survivor(params IWfiWordform[] inputs)
		{
			IWfiWordform result = null;
			foreach (var wf in inputs)
			{
				if (wf.IsValidObject)
				{
					Assert.That(result, Is.Null, "only one wordform should have survived");
					result = wf;
				}
			}
			Assert.That(result, Is.Not.Null, "One wordform should have survived");
			return result;
		}


	}
}
