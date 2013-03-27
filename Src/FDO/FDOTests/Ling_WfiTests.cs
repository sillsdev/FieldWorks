using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests of stuff in Overrides_LingWfi.cs
	/// </summary>
	[TestFixture]
	public class Ling_WfiTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Tests the indicated method.
		/// </summary>
		[Test]
		public void Wordform_ConflictCount()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			Assert.AreEqual(0, wf.ConflictCount);
			var human = Cache.LangProject.DefaultUserAgent;
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			wa.SetAgentOpinion(human, Opinions.approves);
			Assert.AreEqual(0, wf.ConflictCount, "it's not a conflict if not all agents have an opinion");
			var parser = Cache.LangProject.DefaultParserAgent;
			wa.SetAgentOpinion(parser, Opinions.approves);
			Assert.AreEqual(0, wf.ConflictCount, "it's not a conflict if all agents agree");
			wa.SetAgentOpinion(parser, Opinions.disapproves);
			Assert.AreEqual(1, wf.ConflictCount, "it's a conflict if two agents disagree");

			var newAgent = Cache.ServiceLocator.GetInstance<ICmAgentFactory>().Create();
			Cache.LangProject.AnalyzingAgentsOC.Add(newAgent);

			var wa2 = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa2);
			wa2.SetAgentOpinion(parser, Opinions.approves);
			wa2.SetAgentOpinion(human, Opinions.approves);
			wa2.SetAgentOpinion(newAgent, Opinions.approves);
			Assert.AreEqual(1, wf.ConflictCount, "the second WA is not in conflict");

			wa2.SetAgentOpinion(human, Opinions.disapproves);
			Assert.AreEqual(2, wf.ConflictCount, "two approvals out of three is a conflict");

			wa2.SetAgentOpinion(parser, Opinions.disapproves);
			Assert.AreEqual(2, wf.ConflictCount, "one approval out of three is a conflict");

			wa2.SetAgentOpinion(newAgent, Opinions.disapproves);
			Assert.AreEqual(1, wf.ConflictCount, "all disapproving is not a conflict");
		}

		/// <summary>
		/// Tests the indicated method.
		/// </summary>
		[Test]
		public void Wordform_TextGenres()
		{
			var wf = MakeAWordform();
			Assert.AreEqual(0, wf.TextGenres.Count());

			// Connect the wordform to a text, but still no genres because the text doesn't have any.
			var text = MakeText("Here are some wordforms");
			MakeFirstSegReferenceAnalysis(text, wf);
			Assert.AreEqual(0, wf.TextGenres.Count());

			// Give the text a genre, now we should get something.
			var genre1 = MakeGenre();
			text.GenresRC.Add(genre1);
			Assert.AreEqual(genre1, wf.TextGenres.ToArray()[0]);

			// One way to get another is to give the text more.
			var genre2 = MakeGenre();
			text.GenresRC.Add(genre2);
			VerifyPossList(new [] {genre1, genre2}, wf.TextGenres);

			// Another is to give it an analysis that is used by another text.
			var text2 = MakeText("here is some more junk");
			var genre3 = MakeGenre();
			text2.GenresRC.Add(genre2); // just to check a duplicate does not show up in the enumeration.
			text2.GenresRC.Add(genre3);
			var wa = MakeAnalysis(wf);
			MakeFirstSegReferenceAnalysis(text2, wa);
			VerifyPossList(new[] { genre1, genre2, genre3 }, wf.TextGenres);

			// And yet another is if a WfiGloss is used...
			var text3 = MakeText("here is some more junk");
			var genre4 = MakeGenre();
			text3.GenresRC.Add(genre4);
			var wg = MakeGloss(wa);
			MakeFirstSegReferenceAnalysis(text3, wg);
			VerifyPossList(new[] { genre1, genre2, genre3, genre4 }, wf.TextGenres);

			// And just check that removing one removes it...this is really more testing maintenance of OccurrencesInTexts.
			wa.MeaningsOC.Remove(wg);
			VerifyPossList(new[] { genre1, genre2, genre3 }, wf.TextGenres);
		}

		private IWfiGloss MakeGloss(IWfiAnalysis wa)
		{
			var wg = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wg);
			return wg;
		}

		/// <summary>
		/// Tests that we update the occurrences list properly (on Undo and Redo, too)
		/// </summary>
		[Test]
		public void UpdatingOccurrencesList()
		{
			var wf = MakeAWordform();
			var form = Cache.TsStrFactory.MakeString("abdThisIsUnlikelyxyz", Cache.DefaultVernWs);
			wf.Form.VernacularDefaultWritingSystem = form;
			var wordformRepos = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			IWfiWordform retval;
			Assert.That(wordformRepos.TryGetObject(form, false, out retval), Is.True);
			Assert.That(retval, Is.EqualTo(wf));
			m_actionHandler.EndUndoTask();
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => ((ICmObjectInternal)wf).DeleteObject());
			Assert.That(wordformRepos.TryGetObject(form, false, out retval), Is.False);
			m_actionHandler.Undo();
			Assert.That(wordformRepos.TryGetObject(form, false, out retval), Is.True);
			Assert.That(retval, Is.EqualTo(wf));
		}

		private IWfiAnalysis MakeAnalysis(IWfiWordform wf)
		{
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			return wa;
		}

		private IWfiWordform MakeAWordform()
		{
			return Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
		}

		private ICmPossibility MakeGenre()
		{
			if (Cache.LangProject.GenreListOA == null)
				Cache.LangProject.GenreListOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var result = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.GenreListOA.PossibilitiesOS.Add(result);
			return result;
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

		private void MakeFirstSegReferenceAnalysis(IText text, IAnalysis analysis)
		{
			var seg = ((IStTxtPara) text.ContentsOA.ParagraphsOS[0]).SegmentsOS[0];
			seg.AnalysesRS.Add(analysis);
		}

		private void RemoveReferenceAnalysisFromFirstSeg(IText text, IWfiAnalysis analysis)
		{
			var seg = ((IStTxtPara)text.ContentsOA.ParagraphsOS[0]).SegmentsOS[0];
			seg.AnalysesRS.Remove(analysis);
		}

		private void VerifyPossList(ICmPossibility[] expected, IEnumerable<ICmPossibility> actual)
		{
			var actualSet = new HashSet<ICmPossibility>(actual);
			var expectedSet = new HashSet<ICmPossibility>(expected);
			Assert.AreEqual(expectedSet.Count, actualSet.Count, "wrong number of possibilities");
			Assert.AreEqual(actualSet.Count, actual.Count(), "duplicates returned!");
			expectedSet.IntersectWith(actualSet);
			Assert.AreEqual(expectedSet.Count, actualSet.Count, "not the same possibilities");
		}

		/// <summary>
		/// Tests the indicated method.
		/// </summary>
		[Test]
		public void Wordform_AttestedPos()
		{
			var wf = MakeAWordform();
			Assert.AreEqual(0, wf.AttestedPos.Count());

			// Connect the WfiAnalyis to a text, but still no AttestedPos because the analysis doesn't have one.
			var wa = MakeAnalysis(wf);
			var text = MakeText("Here are some wordforms");
			MakeFirstSegReferenceAnalysis(text, wa);
			Assert.AreEqual(0, wf.AttestedPos.Count());

			// Once it has a POS, it should be attested.
			var pos1 = MakePartOfSpeech();
			wa.CategoryRA = pos1;
			VerifyPartOfSpeechList(new[] { pos1 }, wf.AttestedPos);

			// But, not if it doesn't occur
			RemoveReferenceAnalysisFromFirstSeg(text, wa);
			Assert.AreEqual(0, wf.AttestedPos.Count());

			// An occurrence of a gloss counts, however
			var wg = MakeGloss(wa);
			MakeFirstSegReferenceAnalysis(text, wg);
			VerifyPartOfSpeechList(new[] { pos1 }, wf.AttestedPos);

			// Check we can find more than one, and based on a different segment.
			var wa2 = MakeAnalysis(wf);
			var text2 = MakeText("Some words");
			var pos2 = MakePartOfSpeech();
			wa2.CategoryRA = pos2;
			MakeFirstSegReferenceAnalysis(text2, wa2);
			VerifyPartOfSpeechList(new[] { pos1, pos2 }, wf.AttestedPos);

			// Make sure it doesn't look at other wordforms in the same segment by mistake (as it did once, FWR-3360)
			var wf2 = MakeAWordform();
			var wa2_1 = MakeAnalysis(wf2);
			var seg = ((IStTxtPara)text.ContentsOA.ParagraphsOS[0]).SegmentsOS[0];
			seg.AnalysesRS.Insert(0, wa2_1);
			var pos3 = MakePartOfSpeech();
			wa2_1.CategoryRA = pos3;
			VerifyPartOfSpeechList(new[] { pos1, pos2 }, wf.AttestedPos);
		}

		void VerifyPartOfSpeechList(ICmPossibility[] expected, IEnumerable<IPartOfSpeech> actual)
		{
			VerifyPossList(expected, actual.Cast<ICmPossibility>());
		}

		IPartOfSpeech MakePartOfSpeech()
		{
			if (Cache.LangProject.PartsOfSpeechOA == null)
				Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var result = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(result);
			return result;
		}

		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void Wordform_IsComplete()
		{
			var wf = MakeAWordform();
			Assert.IsFalse(wf.IsComplete);

			IPartOfSpeech pos;
			ICmAgent human;
			IWfiGloss wg;
			IWfiAnalysis wa = MakeCompleteAnalysis(wf, out pos, out human, out wg);
			Assert.IsTrue(wf.IsComplete);

			// But if the analysis is incomplete, so is the wordform. Making the analysis occur (directly) is one way to make this so.
			var text = MakeText("some words");
			MakeFirstSegReferenceAnalysis(text, wa);
			Assert.IsFalse(wf.IsComplete);

			// Just having one complete one does not make the wordform complete.
			MakeCompleteAnalysis(wf, out pos, out human, out wg);
			Assert.IsFalse(wf.IsComplete);

			// But if all of them are, it is.
			RemoveReferenceAnalysisFromFirstSeg(text, wa);
			Assert.IsTrue(wf.IsComplete);
		}
		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void Analysis_IsComplete()
		{
			// Make a fully complete analysis, then try altering one thing at a time.
			var wf = MakeAWordform();
			IPartOfSpeech pos;
			ICmAgent human;
			IWfiGloss wg;
			IWfiAnalysis wa = MakeCompleteAnalysis(wf, out pos, out human, out wg);
			Assert.IsTrue(wa.IsComplete);

			// - gloss is incomplete
			wg.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.EmptyString(Cache.DefaultAnalWs);
			Assert.IsFalse(wa.IsComplete);
			// - no gloss
			wa.MeaningsOC.Remove(wg);
			Assert.IsFalse(wa.IsComplete);
			// - multiple glosses, one incomplete
			wg = MakeCompleteGloss(wa);
			var wg2 = MakeGloss(wa);
			Assert.IsFalse(wa.IsComplete);
			wa.MeaningsOC.Remove(wg2);
			// - restore completeness, as far as glosses are concerned, and verify
			Assert.IsTrue(wa.IsComplete);

			// - bundles are incomplete
			wa.MorphBundlesOS[0].SenseRA = null;
			Assert.IsFalse(wa.IsComplete);
			// - no bundles
			wa.MorphBundlesOS.RemoveAt(0);
			Assert.IsFalse(wa.IsComplete);
			// - multiple bundles, not all complete
			MakeCompleteBundle(wa);
			MakeBundle(wa);
			Assert.IsFalse(wa.IsComplete);
			// - restore bundles to complete and verify
			wa.MorphBundlesOS.RemoveAt(1);
			Assert.IsTrue(wa.IsComplete);

			// - Category missing
			wa.CategoryRA = null;
			Assert.IsFalse(wa.IsComplete);
			wa.CategoryRA = pos;
			Assert.IsTrue(wa.IsComplete);

			// - Conflicting parser analysis
			var parser = Cache.LangProject.DefaultParserAgent;
			wa.SetAgentOpinion(parser, Opinions.disapproves);
			Assert.IsFalse(wa.IsComplete);
			// - But two opinions agreeing is OK.
			wa.SetAgentOpinion(parser, Opinions.approves);
			Assert.IsTrue(wa.IsComplete);
			// - however the human must have an opinion
			wa.SetAgentOpinion(human, Opinions.noopinion);
			Assert.IsFalse(wa.IsComplete);
			wa.SetAgentOpinion(human, Opinions.approves);

			// - It occurs.
			var text = MakeText("some words");
			MakeFirstSegReferenceAnalysis(text, wa);
			Assert.IsFalse(wa.IsComplete);
			RemoveReferenceAnalysisFromFirstSeg(text, wa);

			// But it's OK if one of its glosses occurs.
			MakeFirstSegReferenceAnalysis(text, wg);
			Assert.IsTrue(wa.IsComplete);
		}

		private IWfiAnalysis MakeCompleteAnalysis(IWfiWordform wf, out IPartOfSpeech pos, out ICmAgent human, out IWfiGloss wg)
		{
			var wa = MakeAnalysis(wf);
			var bundle = MakeCompleteBundle(wa);
			wg = MakeCompleteGloss(wa);
			pos = MakePartOfSpeech();
			wa.CategoryRA = pos;
			human = Cache.LangProject.DefaultUserAgent;
			wa.SetAgentOpinion(human, Opinions.approves);
			Assert.IsTrue(wa.IsComplete);
			return wa;
		}

		private IWfiGloss MakeCompleteGloss(IWfiAnalysis wa)
		{
			var wg = MakeGloss(wa);
			wg.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("a gloss", Cache.DefaultAnalWs);
			return wg;
		}
		private IWfiMorphBundle MakeCompleteBundle(IWfiAnalysis wa)
		{
			var bundle = MakeBundle(wa);
			bundle.SenseRA = MakeCompleteSense();
			bundle.MsaRA = bundle.SenseRA.MorphoSyntaxAnalysisRA;
			bundle.MorphRA = ((ILexEntry) bundle.SenseRA.Owner).LexemeFormOA;
			return bundle;
		}

		private ILexSense MakeCompleteSense()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexSense sense = MakeCompleteSense(entry);
			IMoStemAllomorph morph = MakeStemMorphWithFormDefaultAlternatives(entry, "dummy form", "dummy form");
			return sense;
		}

		private ILexSense MakeCompleteSense(ILexEntry entry)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			GiveSenseGloss(sense);
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;
			return sense;
		}

		private IMoStemAllomorph MakeStemMorphWithFormDefaultAlternatives(ILexEntry entryOwner, string formVern, string formAnalysis)
		{
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entryOwner.LexemeFormOA = morph;
			if (formVern != null)
				morph.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(formVern, Cache.DefaultVernWs);
			if (formAnalysis != null)
				morph.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(formAnalysis, Cache.DefaultAnalWs);
			return morph;
		}

		private void GiveSenseGloss(ILexSense sense)
		{
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("a sense gloss",
				Cache.DefaultAnalWs);
		}

		private IWfiMorphBundle MakeBundle(IWfiAnalysis wa)
		{
			var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(bundle);
			return bundle;
		}

		/// <summary>
		/// Test the indicated method.
		/// </summary>
		[Test]
		public void MorphBundle_IsComplete()
		{
			var wf = MakeAWordform();
			var wa = MakeAnalysis(wf);
			var bundle = MakeCompleteBundle(wa);
			Assert.IsTrue(bundle.IsComplete);

			// Missing parts
			var sense = bundle.SenseRA;
			bundle.SenseRA = null;
			Assert.IsFalse(bundle.IsComplete);
			bundle.SenseRA = sense;

			var msa = bundle.MsaRA;
			bundle.MsaRA = null;
			Assert.IsFalse(bundle.IsComplete);
			bundle.MsaRA = msa;

			var morph = bundle.MorphRA;
			bundle.MorphRA = null;
			Assert.IsFalse(bundle.IsComplete);
			bundle.MorphRA = morph;
			Assert.IsTrue(bundle.IsComplete); // paranoia, make sure all the restores worked

			// Incomplete parts
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.EmptyString(Cache.DefaultAnalWs);
			Assert.IsFalse(bundle.IsComplete);
			GiveSenseGloss(sense);

			// Test the case where the default analysis WS is set, but another is not.
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(Cache.LangProject.DefaultVernacularWritingSystem);
			Assert.IsFalse(bundle.IsComplete);
			sense.Gloss.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("vern gloss",
				Cache.DefaultVernWs);
			Assert.IsTrue(bundle.IsComplete);
			Cache.LangProject.CurrentAnalysisWritingSystems.Remove(Cache.LangProject.DefaultVernacularWritingSystem);

			// Incomplete MoForm
			morph.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.EmptyString(Cache.DefaultVernWs);
			Assert.IsFalse(bundle.IsComplete);
			morph.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("vern form",
				Cache.DefaultVernWs);
			// false if another WS is missing. (Slightly greedy here, this amounts to a test of MoForm.IsComplete.)
			Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.LangProject.DefaultAnalysisWritingSystem);
			morph.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.EmptyString(Cache.DefaultAnalWs);
			Assert.IsFalse(bundle.IsComplete);
			morph.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("anal form",
				Cache.DefaultAnalWs);
			Assert.IsTrue(bundle.IsComplete);
			Cache.LangProject.CurrentVernacularWritingSystems.Remove(Cache.LangProject.DefaultAnalysisWritingSystem);

			// Incomplete MSA: test for this if we ever define a non-trivial IsComplete for MSA.

		}

		/// <summary>
		/// Check that Updating MorphRA also updates the Form
		/// </summary>
		[Test]
		public void MorphBundle_UpdateMorphRA()
		{
			var wf = MakeAWordform();
			var wa = MakeAnalysis(wf);
			var bundle = MakeBundle(wa);

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemAllomorph morph1 = MakeStemMorphWithFormDefaultAlternatives(entry, "formVern", null);
			bundle.MorphRA = morph1;

			Assert.AreEqual("formVern", bundle.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(null, bundle.Form.AnalysisDefaultWritingSystem.Text);

			// test that the bundle form doesn't change with morph form changes.
			morph1.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("formVernChanged",
																					   Cache.DefaultVernWs);
			Assert.AreEqual("formVern", bundle.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(null, bundle.Form.AnalysisDefaultWritingSystem.Text);

			// save the form information off so we can try to access it with invalid data later
			var wmbForm = bundle.Form;
			var morphRA = bundle.MorphRA;

			// test that we maintain the Form values if the MorphRA is unlinked
			bundle.MorphRA = null;
			Assert.AreEqual("formVern", bundle.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(null, bundle.Form.AnalysisDefaultWritingSystem.Text);

			// make sure saved off form accessor still matches expectations
			Assert.AreEqual("formVern", wmbForm.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(null, wmbForm.AnalysisDefaultWritingSystem.Text);

			// test that we can set new values
			IMoStemAllomorph morph2 = MakeStemMorphWithFormDefaultAlternatives(entry, null, "formAnalysis");
			bundle.MorphRA = morph2;
			Assert.AreEqual(null, bundle.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("formAnalysis", bundle.Form.AnalysisDefaultWritingSystem.Text);

			// make sure saved off form accessor still matches expectations
			Assert.AreEqual(null, wmbForm.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("formAnalysis", wmbForm.AnalysisDefaultWritingSystem.Text);


			IMoStemAllomorph morphEmptyStrings = MakeStemMorphWithFormDefaultAlternatives(entry, "", "");
			bundle.MorphRA = morphEmptyStrings;
			Assert.AreEqual(null, bundle.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(null, bundle.Form.AnalysisDefaultWritingSystem.Text);

			// test XML output for empty strings.
			//var xml = ((ICmObjectInternal)bundle).ToXmlString();
			//Assert.IsNotNull(xml);
		}

		/// <summary>
		/// Tests the method of the given name (also DeletionText).
		/// </summary>
		[Test]
		public void CanDelete()
		{
			var wf = MakeAWordform();
			wf.Form.set_String(Cache.DefaultVernWs, MakeVernString("Hello"));
			Assert.That(wf.DeletionTextTSS.Text, Is.EqualTo("Hello"));
			Assert.That(wf.CanDelete, Is.True);
			//var wa = MakeAnalysis(wf);
			//var mb = MakeBundle(wa);
			var text = MakeText("Hello");
			var para = (IStTxtPara) text.ContentsOA.ParagraphsOS[0];
			var seg = para.SegmentsOS[0];
			seg.AnalysesRS.Add(wf);
			Assert.That(wf.DeletionTextTSS.Text, Is.EqualTo("Hello"));
			Assert.That(wf.CanDelete, Is.False);
			para.SegmentsOS.RemoveAt(0);
			Assert.That(wf.CanDelete, Is.True);
			var wordset = MakeSet(new[] {wf}, "Words");
			Assert.That(wf.CanDelete, Is.True, "a wordset should not prevent deletion");
			Assert.That(wf.DeletionTextTSS.Text, Is.EqualTo("Hello: a member of the word set(s) called \"Words\""));
			((ICmObjectInternal)wf).DeleteObject();
			Assert.That(wordset.CasesRC, Is.Empty);
		}

		private ITsString MakeVernString(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultVernWs);
		}
		private ITsString MakeAnalysisString(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultAnalWs);
		}

		IWfiWordSet MakeSet(IWfiWordform[] wordforms, string name)
		{
			if (Cache.LangProject.MorphologicalDataOA == null)
				Cache.LangProject.MorphologicalDataOA = Cache.ServiceLocator.GetInstance<IMoMorphDataFactory>().Create();
			var wordset = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordset);
			wordset.Name.set_String(Cache.DefaultAnalWs, MakeAnalysisString(name));
			foreach (var wf in wordforms)
				wordset.CasesRC.Add(wf);
			return wordset;
		}
	}
}
