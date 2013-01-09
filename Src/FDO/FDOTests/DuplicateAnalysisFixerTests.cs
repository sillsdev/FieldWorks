using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests the DuplicateWordformFixer
	/// </summary>
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Unit test - m_progress gets disposed in TestTearDown()")]
	public class DuplicateAnalysisFixerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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
		/// This tries to cover all the cases of analyses that should NOT be merged, except the unused fields cases,
		/// which are only there for paranoia.
		/// </summary>
		[Test]
		public void DifferentAnalyses_AreNotMerged()
		{
			var wf = MakeWordform("bank");
			var wa1 = MakeAnalysis(wf);
			var entryBank = MakeEntry("bank", "side of river");
			MakeBundle(wa1, entryBank.SensesOS[0]);

			var wa2 = MakeAnalysis(wf);
			var entryKick = MakeEntry("bank", "place for money");
			MakeBundle(wa2, entryKick.SensesOS[0]);

			var wa3 = MakeAnalysis(wf);
			MakeBundle(wa3, entryBank.SensesOS[0]);
			var verb = MakePartOfSpeech("verb");
			wa3.CategoryRA = verb;

			var wa4 = MakeAnalysis(wf);
			MakeBundle(wa4, "bank"); // analysis only to morpheme level
			var wa5 = MakeAnalysis(wf);
			MakeBundle(wa5, "bank0"); // analysis only to morpheme level

			var wa6 = MakeAnalysis(wf);
			var bundle6 = MakeBundle(wa6, "bank");
			bundle6.MsaRA = MakeMsa(entryBank);

			var wa7 = MakeAnalysis(wf);
			var bundle7 = MakeBundle(wa7, "bank0");
			bundle7.MorphRA = MorphServices.MakeMorph(entryBank, TsStringUtils.MakeTss("bank", Cache.DefaultVernWs));

			var wa8 = MakeAnalysis(wf);
			MakeBundle(wa8, entryBank.SensesOS[0]);
			MakeBundle(wa8, entryBank.SensesOS[0]); // makes sure it handles different number of bundles


			WfiWordformServices.MergeDuplicateAnalyses(Cache, m_progress);

			// We could try for something stronger; the basic idea is that it should change nothing.
			// But the kind of changes it makes is deleting objects, so this should cover it.
			Assert.That(wa1.IsValidObject);
			Assert.That(wa2.IsValidObject);
			Assert.That(wa3.IsValidObject);
			Assert.That(wa4.IsValidObject);
			Assert.That(wa5.IsValidObject);
			Assert.That(wa6.IsValidObject);
			Assert.That(wa7.IsValidObject);
			Assert.That(wa8.IsValidObject);
		}

		/// <summary>What it says</summary>
		[Test]
		public void IdenticalTrivialAnalyses_AreMerged()
		{
			var wf = MakeWordform("bank");
			var wa1 = MakeAnalysis(wf);
			var wa2 = MakeAnalysis(wf);

			WfiWordformServices.MergeDuplicateAnalyses(Cache, m_progress);

			Assert.That(!wa1.IsValidObject || !wa2.IsValidObject); // one should be deleted
			Assert.That(wa1.IsValidObject || wa2.IsValidObject); // and one should survive!
		}

		/// <summary>
		/// This one covers lots of cases. Two analyses are deleted and merged into a third, one is left alone.
		/// A duplicate gloss is merged. References to the duplicate analyses and glosses are fixed.
		/// A non-duplicate WfiGloss is NOT merged. Rather greedy, but hard to break up without a lot of duplication.
		/// </summary>
		[Test]
		public void ComplexMerge_SucceedsAndFixesRefs()
		{
			var wf = MakeWordform("bank");
			var wa1 = MakeAnalysis(wf);
			var entryBankRiver = MakeEntry("bank", "side of river");
			MakeBundle(wa1, entryBankRiver.SensesOS[0]);

			var wa2 = MakeAnalysis(wf);
			var entryBankMoney = MakeEntry("bank", "place for money");
			MakeBundle(wa2, entryBankMoney.SensesOS[0]);

			var wadup1 = MakeAnalysis(wf);
			MakeBundle(wadup1, entryBankMoney.SensesOS[0]);

			var wadup2 = MakeAnalysis(wf);
			MakeBundle(wadup2, entryBankMoney.SensesOS[0]);

			var wgMoney = MakeGloss(wa2, "money");
			var wgPlaceMoney = MakeGloss(wadup1, "place for money");
			var wgdup1 = MakeGloss(wa2, "money");
			var wgdup2 = MakeGloss(wadup1, "money");
			var wgdup3 = MakeGloss(wadup2, "money");

			var text = MakeText("");
			var seg = MakeSegment(text.ContentsOA, "bank bank bank bank bank bank bank bank");
			seg.AnalysesRS.Add(wa1);
			seg.AnalysesRS.Add(wa2);
			seg.AnalysesRS.Add(wadup1);
			seg.AnalysesRS.Add(wadup2);
			seg.AnalysesRS.Add(wgMoney);
			seg.AnalysesRS.Add(wgPlaceMoney);
			seg.AnalysesRS.Add(wgdup1);
			seg.AnalysesRS.Add(wgdup2);
			seg.AnalysesRS.Add(wgdup3);

			WfiWordformServices.MergeDuplicateAnalyses(Cache, m_progress);

			// Theoretically, wa3 or wa4 could legitimately be the survivor. But in fact it is now wa1.
			var survivor = wa2;
			Assert.That(survivor.IsValidObject);
			Assert.That(wa2.IsValidObject);
			Assert.That(wadup1.IsValidObject, Is.False);
			Assert.That(wadup2.IsValidObject, Is.False);

			var survivingGloss = wgMoney; // Enhance: any other duplicate surviving is OK.
			Assert.That(survivingGloss.IsValidObject);
			Assert.That(wgPlaceMoney.IsValidObject);
			Assert.That(wgPlaceMoney.Owner, Is.EqualTo(survivor));
			Assert.That(wgdup1.IsValidObject, Is.False);
			Assert.That(wgdup2.IsValidObject, Is.False);
			Assert.That(wgdup3.IsValidObject, Is.False);

			Assert.That(seg.AnalysesRS[0], Is.EqualTo(wa1));
			Assert.That(seg.AnalysesRS[1], Is.EqualTo(survivor));
			Assert.That(seg.AnalysesRS[2], Is.EqualTo(survivor));
			Assert.That(seg.AnalysesRS[3], Is.EqualTo(survivor));
			Assert.That(seg.AnalysesRS[4], Is.EqualTo(survivingGloss));
			Assert.That(seg.AnalysesRS[5], Is.EqualTo(wgPlaceMoney));
			Assert.That(seg.AnalysesRS[6], Is.EqualTo(survivingGloss));
			Assert.That(seg.AnalysesRS[7], Is.EqualTo(survivingGloss));
			Assert.That(seg.AnalysesRS[8], Is.EqualTo(survivingGloss));


		}

		private IWfiGloss MakeGloss(IWfiAnalysis wa, string form)
		{
			var wg = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wg);
			wg.Form.SetAnalysisDefaultWritingSystem(form);
			return wg;
		}

		private IText MakeText(string contents)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);
			var seg = Cache.ServiceLocator.GetInstance<ISegmentFactory>().Create();
			para.SegmentsOS.Add(seg);
			return text;
		}

		/// <summary>
		/// Add a segment of text to the paragraph and return the resulting segment.
		/// Note that this depends on the code that automatically reparses the paragraph,
		/// so the strings added must really produce segments.
		/// </summary>
		private ISegment MakeSegment(IStText text, string contents)
		{
			var para = (IStTxtPara)text.ParagraphsOS[0];
			int length = para.Contents.Length;
			if (length == 0)
				para.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);
			else
			{
				var bldr = para.Contents.GetBldr();
				bldr.Replace(length, length, " " + contents, null);
				para.Contents = bldr.GetString();
			}
			var seg = para.SegmentsOS[para.SegmentsOS.Count - 1];
			return seg;
		}

		private IMoMorphSynAnalysis MakeMsa(ILexEntry entry)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			return msa;
		}

		private IWfiWordform MakeWordform(string form)
		{
			var result = m_wfiFactory.Create();
			result.Form.VernacularDefaultWritingSystem = MakeVernString(form);
			return result;
		}

		private ITsString MakeVernString(string text)
		{
			return Cache.TsStrFactory.MakeString(text, Cache.DefaultVernWs);
		}

		private IWfiAnalysis MakeAnalysis(IWfiWordform wf)
		{
			// Enhance JohnT: could make at least one bundle, or a gloss...but given the implementation this doesn't add much.
			var result = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(result);
			return result;
		}

		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = null;
			entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return entry;
		}

		private IWfiMorphBundle MakeBundle(IWfiAnalysis wa, ILexSense sense)
		{
			var mb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(mb);
			mb.SenseRA = sense;
			return mb;
		}

		private IWfiMorphBundle MakeBundle(IWfiAnalysis wa, string form)
		{
			var mb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(mb);
			mb.Form.SetVernacularDefaultWritingSystem(form);
			return mb;
		}

		private IPartOfSpeech MakePartOfSpeech(string name)
		{
			var partOfSpeech = MakePartOfSpeech();
			partOfSpeech.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(name, Cache.DefaultAnalWs);
			return partOfSpeech;
		}

		private IPartOfSpeech MakePartOfSpeech()
		{
			var partOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(partOfSpeech);
			return partOfSpeech;
		}
	}
}
