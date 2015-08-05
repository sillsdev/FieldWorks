// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RespellingTests.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	[TestFixture]
	public class RespellingTests : MemoryOnlyBackendProviderTestBase
	{
		private const int kObjectListFlid = 89999956;
		private Mediator m_mediator;
		private IPropertyTable m_propertyTable;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.LanguageProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			});
		}

		#region Overrides of FdoTestBase

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Done before each test.
		/// Overriders should call base method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_propertyTable = PropertyTableFactory.CreatePropertyTable(new MockPublisher());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Done after each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			while (m_actionHandler.CanUndo())
				Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(0, m_actionHandler.UndoableSequenceCount);

			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}

			base.TestTearDown();
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test simulating multiple spelling changes in a single segment in a paragraph. Should
		/// be possible to undo this change. This test was written for TE-9243, but then we
		/// realized that it actually worked fine all along. Still a nice test though, don't you
		/// think?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanUndoChangeMultipleOccurrences_InSingleSegment()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are hoping, we are.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";

			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction(ksParaText,
				ksWordToReplace, ksNewWord, false, out para);

			respellUndoaction.AllChanged = false;
			respellUndoaction.KeepAnalyses = true;
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		[Test]
		public void CanRespellShortenWord()
		{
			IStTxtPara para;
			const string ksParaText = "somelongwords must be short somelongwords. somelongwords are. somelongwords aren't. somelongwords somelongwords";
			const string ksWordToReplace = "somelongwords";
			const string ksNewWord = "s";

			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction(ksParaText,
				ksWordToReplace, ksNewWord, false, out para);

			respellUndoaction.AllChanged = false;
			respellUndoaction.KeepAnalyses = true;
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		[Test]
		public void CanRespellMultiMorphemicWordAndKeepUsages()
		{
			IStTxtPara para;
			const string ksParaText = "somelongwords must be multimorphemic. somelongwords multimorphemic are.";
			const string ksWordToReplace = "multimorphemic";
			const string ksNewWord = "massivemorphemic";
			var morphs = new [] { "multi", "morphemic" };

			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction_MultiMorphemic(ksParaText,
				ksWordToReplace, ksNewWord, morphs, out para);

			Assert.AreEqual(2, para.SegmentsOS[0].AnalysesRS[3].Analysis.MorphBundlesOS.Count,
				"Should have 2 morph bundles before spelling change.");

			respellUndoaction.AllChanged = true;
			respellUndoaction.KeepAnalyses = true;
			respellUndoaction.CopyAnalyses = true; // in the dialog this is always true?
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(0, para.SegmentsOS[0].AnalysesRS[2].Analysis.MorphBundlesOS.Count,
				"Unexpected morph bundle contents for 'be'");
			Assert.AreEqual(2, para.SegmentsOS[0].AnalysesRS[3].Analysis.MorphBundlesOS.Count,
				"Wrong morph bundle count for 'multimorphemic'");
			Assert.AreEqual(0, para.SegmentsOS[1].AnalysesRS[2].Analysis.MorphBundlesOS.Count,
				"Unexpected morph bundle contents for 'are'");
			Assert.AreEqual(2, para.SegmentsOS[1].AnalysesRS[1].Analysis.MorphBundlesOS.Count,
				"Wrong morph bundle count for 'multimorphemic'");
			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test simulating multiple spelling changes in multiple segments in a single
		/// paragraph. Should be possible to undo this change. TE-9243.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanUndoChangeMultipleOccurrences_InMultipleSegmentsInPara()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are nice. Hoping is what we do when we want. Therefore, we are nice, aren't we? Yes.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";
			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction(ksParaText,
				ksWordToReplace, ksNewWord, false, out para);

			respellUndoaction.AllChanged = false;
			respellUndoaction.KeepAnalyses = true;
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test simulating multiple spelling changes in a single segment in a paragraph. Should
		/// be possible to undo this change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanUndoChangeMultipleOccurrences_InSingleSegment_Glosses()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are hoping, we are.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";

			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction(ksParaText,
				ksWordToReplace, ksNewWord, true, out para);

			respellUndoaction.AllChanged = false;
			respellUndoaction.KeepAnalyses = true;
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[0] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[2] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[4] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[5] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[8] is IWfiGloss);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test simulating multiple spelling changes in multiple segments in a single
		/// paragraph. Should be possible to undo this change. FWR-3424.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanUndoChangeMultipleOccurrences_InMultipleSegmentsInPara_Glosses()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are nice. Hoping is what we do when we want. Therefore, we are nice, aren't we? Yes.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";
			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction(ksParaText,
				ksWordToReplace, ksNewWord, true, out para);

			UndoableUnitOfWorkHelper.Do("Undo Added BT", "Redo Added BT", m_actionHandler, () =>
			{
				int i = 0;
				foreach (ISegment seg in para.SegmentsOS)
					seg.FreeTranslation.SetAnalysisDefaultWritingSystem("Segment " + (i++) + " FT");
			});

			respellUndoaction.AllChanged = false;
			respellUndoaction.KeepAnalyses = true;
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[0] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[2] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[4] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[5] is IWfiGloss);
			Assert.AreEqual("Segment 0 FT", para.SegmentsOS[0].FreeTranslation.AnalysisDefaultWritingSystem.Text);

			Assert.IsTrue(para.SegmentsOS[1].AnalysesRS[0] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[1].AnalysesRS[1] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[1].AnalysesRS[2] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[1].AnalysesRS[4] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[1].AnalysesRS[5] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[1].AnalysesRS[7] is IWfiGloss);
			Assert.AreEqual("Segment 1 FT", para.SegmentsOS[1].FreeTranslation.AnalysisDefaultWritingSystem.Text);

			Assert.IsTrue(para.SegmentsOS[2].AnalysesRS[0] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[2].AnalysesRS[3] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[2].AnalysesRS[4] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[2].AnalysesRS[6] is IWfiGloss);
			Assert.AreEqual("Segment 2 FT", para.SegmentsOS[2].FreeTranslation.AnalysisDefaultWritingSystem.Text);

			Assert.IsTrue(para.SegmentsOS[3].AnalysesRS[0] is IWfiGloss);
			Assert.AreEqual("Segment 3 FT", para.SegmentsOS[3].FreeTranslation.AnalysisDefaultWritingSystem.Text);

			Assert.AreEqual(3, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test simulating single spelling change in a single segment in a paragraph. Should
		/// be possible to undo this change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanUndoChangeSingleOccurrence_InSingleSegment()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are hoping, we are.";
			const string ksWordToReplace = "hope";
			const string ksNewWord = ksWordToReplace + "ful";

			RespellUndoAction respellUndoaction = SetUpParaAndRespellUndoAction(ksParaText,
				ksWordToReplace, ksNewWord, false, StTxtParaTags.kClassId, out para);

			respellUndoaction.AllChanged = true;
			respellUndoaction.CopyAnalyses = false;
			respellUndoaction.KeepAnalyses = false;
			respellUndoaction.PreserveCase = true;
			respellUndoaction.UpdateLexicalEntries = true;

			Mediator mediator = MockRepository.GenerateStub<Mediator>();
			respellUndoaction.DoIt(mediator);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up para and respell undo action.
		/// </summary>
		/// <param name="sParaText">The paragraph text.</param>
		/// <param name="sWordToReplace">The word to replace.</param>
		/// <param name="sNewWord">The new word.</param>
		/// <param name="fCreateGlosses">if set to <c>true</c> in addition to wordforms, glosses
		/// are created for each word in the text and analyses are hooked up to those glosses
		/// instead of just the wordforms.</param>
		/// <param name="para">The para.</param>
		/// <returns>The RespellUndoAction that is actually the workhorse for changing multiple
		/// occurrences of a word</returns>
		/// ------------------------------------------------------------------------------------
		private RespellUndoAction SetUpParaAndRespellUndoAction(string sParaText,
			string sWordToReplace, string sNewWord, bool fCreateGlosses, out IStTxtPara para)
		{
			return SetUpParaAndRespellUndoAction(sParaText, sWordToReplace, sNewWord,
				fCreateGlosses, ScrTxtParaTags.kClassId, out para);
		}

		private RespellUndoAction SetUpParaAndRespellUndoAction(string sParaText,
			string sWordToReplace, string sNewWord, bool fCreateGlosses, int clidPara,
			out IStTxtPara para)
		{
			List<IParaFragment> paraFrags = new List<IParaFragment>();
			IStTxtPara paraT = null;
			IStText stText = null;
			UndoableUnitOfWorkHelper.Do("Undo create book", "Redo create book", m_actionHandler, () =>
			{
				var lp = Cache.LanguageProject;
				if (clidPara == ScrTxtParaTags.kClassId)
				{
					IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1, out stText);
					paraT = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(stText, "Monkey");
					paraT.Contents = TsStringUtils.MakeTss(sParaText, Cache.DefaultVernWs);
					object owner = ReflectionHelper.CreateObject("FDO.dll", "SIL.FieldWorks.FDO.Infrastructure.Impl.CmObjectId", BindingFlags.NonPublic,
						new object[] { book.Guid });
					ReflectionHelper.SetField(stText, "m_owner", owner);
				}
				else
				{
					var proj = Cache.LangProject;
					var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
					stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
					text.ContentsOA = stText;
					paraT = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
					stText.ParagraphsOS.Add(paraT);
					paraT.Contents = TsStringUtils.MakeTss(sParaText, Cache.DefaultVernWs);
				}
				foreach (ISegment seg in paraT.SegmentsOS)
				{
					FdoTestHelper.CreateAnalyses(seg, paraT.Contents, seg.BeginOffset, seg.EndOffset, fCreateGlosses);
					paraFrags.AddRange(GetParaFragmentsInSegmentForWord(seg, sWordToReplace));
				}
			});

			var rsda = new RespellingSda((ISilDataAccessManaged)Cache.MainCacheAccessor, null, Cache.ServiceLocator);
			InterestingTextList dummyTextList = MockRepository.GenerateStub<InterestingTextList>(m_mediator, m_propertyTable, Cache.ServiceLocator.GetInstance<ITextRepository>(),
			Cache.ServiceLocator.GetInstance<IStTextRepository>());
			if (clidPara == ScrTxtParaTags.kClassId)
				dummyTextList.Stub(tl => tl.InterestingTexts).Return(new IStText[0]);
			else
				dummyTextList.Stub(t1 => t1.InterestingTexts).Return(new IStText[1] { stText });
			ReflectionHelper.SetField(rsda, "m_interestingTexts", dummyTextList);
			rsda.SetCache(Cache);
			rsda.SetOccurrences(0, paraFrags);
			ObjectListPublisher publisher = new ObjectListPublisher(rsda, kObjectListFlid);
			XMLViewsDataCache xmlCache = MockRepository.GenerateStub<XMLViewsDataCache>(publisher, true, new Dictionary<int, int>());

			xmlCache.Stub(c => c.get_IntProp(paraT.Hvo, CmObjectTags.kflidClass)).Return(ScrTxtParaTags.kClassId);
			xmlCache.Stub(c => c.VecProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int[]>(publisher.VecProp));
			xmlCache.MetaDataCache = new RespellingMdc((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor);
			xmlCache.Stub(c => c.get_ObjectProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int>(publisher.get_ObjectProp));
			xmlCache.Stub(c => c.get_IntProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int>(publisher.get_IntProp));

			var respellUndoaction = new RespellUndoAction(xmlCache, Cache, Cache.DefaultVernWs, sWordToReplace, sNewWord);
			foreach (int hvoFake in rsda.VecProp(0, ConcDecorator.kflidConcOccurrences))
				respellUndoaction.AddOccurrence(hvoFake);

			para = paraT;
			return respellUndoaction;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up para and respell undo action.
		/// </summary>
		/// <param name="sParaText">The paragraph text.</param>
		/// <param name="sWordToReplace">The word to replace.</param>
		/// <param name="sNewWord">The new word.</param>
		/// <param name="morphs">Array of substrings from sNewWord to build WfiMorphBundles for</param>
		/// <param name="para">The para.</param>
		/// <returns>The RespellUndoAction that is actually the workhorse for changing multiple
		/// occurrences of a word</returns>
		/// ------------------------------------------------------------------------------------
		private RespellUndoAction SetUpParaAndRespellUndoAction_MultiMorphemic(string sParaText,
			string sWordToReplace, string sNewWord, string[] morphs, out IStTxtPara para)
		{
			return SetUpParaAndRespellUndoAction_MultiMorphemic(sParaText, sWordToReplace, sNewWord,
				morphs, ScrTxtParaTags.kClassId, out para);
		}

		private RespellUndoAction SetUpParaAndRespellUndoAction_MultiMorphemic(string sParaText,
			string sWordToReplace, string sNewWord, string[] morphsToCreate, int clidPara,
			out IStTxtPara para)
		{
			List<IParaFragment> paraFrags = new List<IParaFragment>();
			IStTxtPara paraT = null;
			IStText stText = null;
			UndoableUnitOfWorkHelper.Do("Undo create book", "Redo create book", m_actionHandler, () =>
			{
				var lp = Cache.LanguageProject;
				if (clidPara == ScrTxtParaTags.kClassId)
				{
					IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1, out stText);
					paraT = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(stText, "Monkey");
					paraT.Contents = TsStringUtils.MakeTss(sParaText, Cache.DefaultVernWs);
					object owner = ReflectionHelper.CreateObject("FDO.dll", "SIL.FieldWorks.FDO.Infrastructure.Impl.CmObjectId", BindingFlags.NonPublic,
						new object[] { book.Guid });
					ReflectionHelper.SetField(stText, "m_owner", owner);
				}
				else
				{
					var proj = Cache.LangProject;
					var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
					stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
					text.ContentsOA = stText;
					paraT = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
					stText.ParagraphsOS.Add(paraT);
					paraT.Contents = TsStringUtils.MakeTss(sParaText, Cache.DefaultVernWs);
				}
				foreach (ISegment seg in paraT.SegmentsOS)
				{
					FdoTestHelper.CreateAnalyses(seg, paraT.Contents, seg.BeginOffset, seg.EndOffset, true);
					var thisSegParaFrags = GetParaFragmentsInSegmentForWord(seg, sWordToReplace);
					SetMultimorphemicAnalyses(thisSegParaFrags, morphsToCreate);
					paraFrags.AddRange(thisSegParaFrags);
				}
			});

			var rsda = new RespellingSda((ISilDataAccessManaged)Cache.MainCacheAccessor, null, Cache.ServiceLocator);
			InterestingTextList dummyTextList = MockRepository.GenerateStub<InterestingTextList>(m_mediator, m_propertyTable, Cache.ServiceLocator.GetInstance<ITextRepository>(),
			Cache.ServiceLocator.GetInstance<IStTextRepository>());
			if (clidPara == ScrTxtParaTags.kClassId)
				dummyTextList.Stub(tl => tl.InterestingTexts).Return(new IStText[0]);
			else
				dummyTextList.Stub(t1 => t1.InterestingTexts).Return(new IStText[1] { stText });
			ReflectionHelper.SetField(rsda, "m_interestingTexts", dummyTextList);
			rsda.SetCache(Cache);
			rsda.SetOccurrences(0, paraFrags);
			ObjectListPublisher publisher = new ObjectListPublisher(rsda, kObjectListFlid);
			XMLViewsDataCache xmlCache = MockRepository.GenerateStub<XMLViewsDataCache>(publisher, true, new Dictionary<int, int>());

			xmlCache.Stub(c => c.get_IntProp(paraT.Hvo, CmObjectTags.kflidClass)).Return(ScrTxtParaTags.kClassId);
			xmlCache.Stub(c => c.VecProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int[]>(publisher.VecProp));
			xmlCache.MetaDataCache = new RespellingMdc((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor);
			xmlCache.Stub(c => c.get_ObjectProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int>(publisher.get_ObjectProp));
			xmlCache.Stub(c => c.get_IntProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int>(publisher.get_IntProp));

			var respellUndoaction = new RespellUndoAction(xmlCache, Cache, Cache.DefaultVernWs, sWordToReplace, sNewWord);
			foreach (int hvoFake in rsda.VecProp(0, ConcDecorator.kflidConcOccurrences))
				respellUndoaction.AddOccurrence(hvoFake);

			para = paraT;
			return respellUndoaction;
		}

		private void SetMultimorphemicAnalyses(IEnumerable<IParaFragment> thisSegParaFrags, string[] morphsToCreate)
		{
			var morphFact = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
			var tssFact = Cache.TsStrFactory;
			// IWfiWordform, IWfiAnalysis, and IWfiGloss objects will have already been created.
			// Still need to add WfiMorphBundles as per morphsToCreate.
			foreach (IWfiWordform wordform in thisSegParaFrags.Select(x => x.Analysis))
			{
				var analysis = wordform.AnalysesOC.First();
				if (analysis.MorphBundlesOS.Count != 0)
					continue;
				foreach (var morpheme in morphsToCreate)
				{
					var bundle = morphFact.Create();
					analysis.MorphBundlesOS.Add(bundle);
					bundle.Form.VernacularDefaultWritingSystem = tssFact.MakeString(morpheme, Cache.DefaultVernWs);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerates the para fragments for all occurrences of the given word in the given
		/// segment.
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="word">The word to find.</param>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<IParaFragment> GetParaFragmentsInSegmentForWord(ISegment seg, string word)
		{
			IAnalysis analysis = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetMatchingWordform(Cache.DefaultVernWs, word);
			int ichStart = 0;
			int ichWe;
			while ((ichWe = seg.BaselineText.Text.IndexOf(word, ichStart)) >= 0)
			{
				ichStart = ichWe + word.Length;
				ichWe += seg.BeginOffset;
				yield return new ParaFragment(seg, ichWe, ichWe + word.Length, analysis);
			}
		}
	}

#if WANTTESTPORT
	[TestFixture]
	public class RespellingTests : InDatabaseFdoTestBase
	{
		Text m_text;
		StText m_stText;
		StTxtPara m_para1;
		StTxtPara m_para2;
		IWfiWordform m_wfAxx; // wordform for 'axx'
		List<int> m_axxOccurrences; // CBAs that have m_wfAxx as their instanceOf
		List<int> m_para1Occurrences; // sorted by BeginOffset
		List<int> m_para2Occurrences;
		ICmBaseAnnotation m_cbaAxx;
		ICmBaseAnnotation m_cba2;

		// stuff related to wordform analyses
		WfiAnalysis m_wfaAxe; // Monomorphemic analysis of axx as {axe, chopper}
		WfiAnalysis m_wfaCut; // Monomorphemic analysis of axx as cut
		WfiGloss m_wgAxe; // one of the glosses of m_wfaAxe
		WfiGloss m_wgChopper; // another.
		WfiGloss m_wgCut; // word glos of m_wfaCut.
		WfiAnalysis m_wfaCutIt; // Multimorpheme analysis, ax/cut -x/it
		WfiAnalysis m_wfaNotRude;	// Multimorpheme analysis, a-/not xx/rude
		int m_cAnalyses; // count of analyses made on old wordform.


		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			CreateTestData();
		}
		protected void CreateTestData()
		{
			// Create required virtual properties
			XmlDocument doc = new XmlDocument();
			// Subset of Flex virtuals required for parsing paragraphs etc.
			doc.LoadXml(
			"<virtuals>"
				+"<virtual modelclass=\"StTxtPara\" virtualfield=\"Segments\">"
				+"<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"OccurrencesInTexts\" destinationClass=\"CmBaseAnnotation\">"
				+"<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"HumanApprovedAnalyses\" computeeverytime=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOSequencePropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"HumanNoOpinionParses\" computeeverytime=\"true\" requiresRealParserGeneratedData=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOSequencePropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"HumanDisapprovedParses\" computeeverytime=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOSequencePropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"FullConcordanceCount\" depends=\"OccurrencesInTexts\" computeeverytime=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"UserCount\" bulkLoadMethod=\"LoadAllUserCounts\" computeeverytime=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"ParserCount\" bulkLoadMethod=\"LoadAllParserCounts\" computeeverytime=\"true\" requiresRealParserGeneratedData=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WfiWordform\" virtualfield=\"ConflictCount\" computeeverytime=\"true\" requiresRealParserGeneratedData=\"true\">"
				+"<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
				+"</virtual>"
				+"<virtual modelclass=\"WordformInventory\" virtualfield=\"ConcordanceWords\" destinationClass=\"WfiWordform\">"
				+"<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.ConcordanceWordsVirtualHandler\"/>"
				+"</virtual>"
			+"</virtuals>");
			BaseVirtualHandler.InstallVirtuals(doc.DocumentElement, Cache);

			m_text = new Text();
			// Cache.LangProject.TextsOC.Add(m_text);
			string para1 = "Axx simplexx testxx withxx axx lotxx ofxx wordsxx endingxx inxx xx";
			string para2 = "axx sentencexx axx havingxx axx lotxx ofxx axx";
			m_para1 = new StTxtPara();
			m_stText = new StText();
			m_text.ContentsOA = m_stText;
			m_para1 = MakePara(para1);
			m_para2 = MakePara(para2);
			m_wfAxx = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "axx", Cache.DefaultVernWs, true));
			// Make one real annotation, which also serves to link the Axx to this.
			m_cbaAxx = CmBaseAnnotation.CreateUnownedCba(Cache);
			m_cbaAxx.InstanceOfRA = m_wfAxx;
			m_cbaAxx.BeginObjectRA = m_para1;
			m_cbaAxx.BeginOffset = 0;
			m_cbaAxx.EndOffset = 3;
			m_cbaAxx.Flid = (int)StTxtPara.StTxtParaTags.kflidContents;
			m_cbaAxx.AnnotationTypeRA = CmAnnotationDefn.Twfic(Cache);

			// Make another real annotation, which should get updated during Apply.
			IWfiWordform wf2 = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "lotxx", Cache.DefaultVernWs, true));
			m_cba2 = CmBaseAnnotation.CreateUnownedCba(Cache);
			m_cba2.InstanceOfRA = wf2;
			m_cba2.BeginObjectRA = m_para2;
			m_cba2.BeginOffset = "axx sentencexx axx havingxx axx ".Length;
			m_cba2.EndOffset = m_cba2.BeginOffset + "lotxx".Length;
			m_cba2.AnnotationTypeRA = CmAnnotationDefn.Twfic(Cache);
			m_cba2.Flid = (int)StTxtPara.StTxtParaTags.kflidContents;

			ParagraphParser.ConcordTexts(Cache, new int[] { m_stText.Hvo }, new NullProgressState());
			m_axxOccurrences = m_wfAxx.ConcordanceIds;
			m_para1Occurrences = OccurrencesInPara(m_para1.Hvo, m_axxOccurrences);
			m_para2Occurrences = OccurrencesInPara(m_para2.Hvo, m_axxOccurrences);

			// to improve test isolation, be sure to null things not always initialized.
			m_wfaAxe = m_wfaCut = m_wfaCutIt = m_wfaNotRude = null;
			m_cAnalyses = 0;
		}

		private StTxtPara MakePara(string para1)
		{
			StTxtPara result = new StTxtPara();
			m_stText.ParagraphsOS.Append(result);
			result.Contents.UnderlyingTsString = Cache.MakeVernTss(para1);
			return result;
		}

		const int kflidInstanceOf = (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf;
		const int kflidBeginObject = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject;
		const int kflidBeginOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset;
		const int kflidEndOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset;

		private List<int> OccurrencesInPara(int hvoPara, List<int> allOccurrences)
		{
			List<int> result = allOccurrences.FindAll(
				delegate(int hvoCba) { return Cache.GetObjProperty(hvoCba, kflidBeginObject) == hvoPara; });
			result.Sort(
				delegate(int left, int right)
				{ return Cache.GetIntProperty(left, kflidBeginOffset).CompareTo(
					Cache.GetIntProperty(right, kflidBeginOffset)); });
			return result;
		}

		const int tagPrecedingContext = -2001;
		const int tagPreview = -2002;
		const int tagAdjustedBegin = -2003;
		const int tagAdjustedEnd = -2004;
		const int tagEnabled = -2005;

		[Test]
		public void Previews()
		{
			Assert.AreEqual(6, m_axxOccurrences.Count);
			int ich2ndOcc = Cache.GetIntProperty(m_para2Occurrences[1], kflidBeginOffset);
			int ich3rdOcc = Cache.GetIntProperty(m_para2Occurrences[2], kflidBeginOffset);
			RespellUndoAction action = new RespellUndoAction(Cache, "axx", "ayyy");
			action.AddOccurrence(m_para2Occurrences[1]);
			action.AddOccurrence(m_para2Occurrences[2]);
			action.SetupPreviews(tagPrecedingContext, tagPreview, tagAdjustedBegin, tagAdjustedEnd, tagEnabled, m_axxOccurrences);
			Assert.AreEqual(m_para1.Contents.Text, Cache.GetTsStringProperty(m_para1Occurrences[1], tagPrecedingContext).Text,
				"Unselected occurrences should have unchanged previews");
			Assert.AreEqual(0, Cache.GetIntProperty(m_para1Occurrences[0], tagAdjustedBegin),
				"Unselected occurrences should still have adjustedBegin set");
			Assert.AreEqual(3, Cache.GetIntProperty(m_para1Occurrences[0], tagAdjustedEnd),
				"Unselected occurrences should still have adjustedEnd set");
			AssertTextProp(m_para1Occurrences[0], tagPrecedingContext, 0, RespellUndoAction.SecondaryTextProp, -1, "prop should not be set on unchanged occurrence");

			Assert.AreEqual("axx sentencexx axx", Cache.GetTsStringProperty(m_para2Occurrences[1], tagPrecedingContext).Text,
				"First occurrence should have only part of para 2");
			Assert.AreEqual(ich2ndOcc, Cache.GetIntProperty(m_para2Occurrences[1], tagAdjustedBegin),
				"First occurrence in para has no begin adjustment");
			Assert.AreEqual(ich2ndOcc + 3, Cache.GetIntProperty(m_para2Occurrences[1], tagAdjustedEnd),
				"First occurrence in para has no end adjustment");
			Assert.AreEqual("ayyy havingxx ayyy lotxx ofxx axx", Cache.GetTsStringProperty(m_para2Occurrences[1], tagPreview).Text,
				"First occurrence should have correct following context");
			AssertTextProp(m_para2Occurrences[1], tagPrecedingContext, 0, RespellUndoAction.SecondaryTextProp, -1, "prop should not be set on unchanged occurrence- 2");
			AssertTextProp(m_para2Occurrences[1], tagPrecedingContext, 5, RespellUndoAction.SecondaryTextProp, -1, "prop should not be set on other words");
			AssertTextProp(m_para2Occurrences[1], tagPreview, "ayyy havingxx ".Length, RespellUndoAction.SecondaryTextProp,
				RespellUndoAction.SecondaryTextVal, "prop should be set on changed occurrence in Preview");
			AssertTextProp(m_para2Occurrences[1], tagPreview, "ayyy havingxx ".Length - 1, RespellUndoAction.SecondaryTextProp,
				-1, "prop should not be set on other text in Preview");
			AssertTextProp(m_para2Occurrences[1], tagPreview, "ayyy havingxx ".Length + 4, RespellUndoAction.SecondaryTextProp,
				-1, "prop should not be set on other text in Preview");
			AssertTextProp(m_para2Occurrences[1], tagPreview, "ayyy havingxx ayyy lotxx ofxx a".Length, RespellUndoAction.SecondaryTextProp,
				-1, "prop should not be set on unchanged occurrence in Preview");
			AssertTextProp(m_para2Occurrences[1], tagPreview, 0, (int)FwTextPropType.ktptBold,
				(int)FwTextToggleVal.kttvForceOn, "bold should be set at start of preview");
			AssertTextProp(m_para2Occurrences[1], tagPreview, 4, (int)FwTextPropType.ktptBold,
				-1, "bold should not be set except on changed word");
			// no longer action responsibility. Assert.IsTrue(Cache.GetIntProperty(m_para2Occurrences[1], tagEnabled) != 0);

			Assert.AreEqual("axx sentencexx ayyy havingxx axx", Cache.GetTsStringProperty(m_para2Occurrences[2], tagPrecedingContext).Text,
				"Second occurrence should have more of para 2 with first occurrence corrected");
			Assert.AreEqual(ich3rdOcc + 1, Cache.GetIntProperty(m_para2Occurrences[2], tagAdjustedBegin),
				"Second occurrence in para has begin adjustment");
			Assert.AreEqual(ich3rdOcc + 1 + 3, Cache.GetIntProperty(m_para2Occurrences[2], tagAdjustedEnd),
				"Second occurrence in para has end adjustment");
			Assert.AreEqual("ayyy lotxx ofxx axx", Cache.GetTsStringProperty(m_para2Occurrences[2], tagPreview).Text,
				"Second occurrence should have correct following context");
			AssertTextProp(m_para2Occurrences[2], tagPrecedingContext, 0, RespellUndoAction.SecondaryTextProp, -1, "prop should not be set on unchanged occurrence- 3");
			AssertTextProp(m_para2Occurrences[2], tagPrecedingContext, "axx sentencexx a".Length, RespellUndoAction.SecondaryTextProp,
				RespellUndoAction.SecondaryTextVal, "prop should be set on changed occurrence in preceding context");
			AssertTextProp(m_para2Occurrences[2], tagPrecedingContext, "axx sentencexx".Length, RespellUndoAction.SecondaryTextProp,
				-1, "prop should not be set on other text in preceding context");
			AssertTextProp(m_para2Occurrences[2], tagPrecedingContext, "axx sentencexx ayyy".Length, RespellUndoAction.SecondaryTextProp,
				-1, "prop should not be set on other text in preceding context - 2");
			AssertTextProp(m_para2Occurrences[2], tagPreview, 0, (int)FwTextPropType.ktptBold,
				(int)FwTextToggleVal.kttvForceOn, "bold should be set at start of preview - 2");
			AssertTextProp(m_para2Occurrences[2], tagPreview, 4, (int)FwTextPropType.ktptBold,
				-1, "bold should not be set except on changed word - 2");

			Assert.AreEqual("axx sentencexx ayyy havingxx ayyy lotxx ofxx axx", Cache.GetTsStringProperty(m_para2Occurrences[3], tagPrecedingContext).Text,
				"Unselected occurrences should have full-length preview");
			Assert.AreEqual("axx sentencexx axx havingxx axx lotxx ofxx ".Length + 2, Cache.GetIntProperty(m_para2Occurrences[3], tagAdjustedBegin),
				"Unselected occurrences after changed ones should have adjusted begin");
			Assert.AreEqual("axx sentencexx axx havingxx axx lotxx ofxx ".Length + 2 + 3, Cache.GetIntProperty(m_para2Occurrences[3], tagAdjustedEnd),
				"Unselected occurrences after changed ones should have adjustedEnd set");

			//-----------------------------------------------------------------------------------
			// This is rather a 'greedy' test, but tests on the real database are expensive.
			// Now we want to try changing the status of an occurrence to see whether it updates correctly.
			action.UpdatePreview(m_para2Occurrences[0], true);
			Assert.AreEqual("axx", Cache.GetTsStringProperty(m_para2Occurrences[0], tagPrecedingContext).Text,
				"Newly selected item at start of sentence has null preceding context");
			Assert.AreEqual("ayyy sentencexx ayyy havingxx ayyy lotxx ofxx axx", Cache.GetTsStringProperty(m_para2Occurrences[0], tagPreview).Text,
				"After select at start occ(0) should have correct preview");
			AssertTextProp(m_para2Occurrences[0], tagPreview, 0, (int)FwTextPropType.ktptBold,
				(int)FwTextToggleVal.kttvForceOn, "After select at start occ(0) bold should be set at start of preview");
			AssertTextProp(m_para2Occurrences[0], tagPreview, 4, (int)FwTextPropType.ktptBold,
				-1, "After select at start occ(0) bold should not be set except on changed word");

			Assert.AreEqual("ayyy sentencexx axx", Cache.GetTsStringProperty(m_para2Occurrences[1], tagPrecedingContext).Text,
				"After select at start occ(1) should have new preceding context.");
			Assert.AreEqual(ich2ndOcc + 1, Cache.GetIntProperty(m_para2Occurrences[1], tagAdjustedBegin),
				"After select at start occ(1) should have changed begin adjustment");
			Assert.AreEqual(ich2ndOcc + 4, Cache.GetIntProperty(m_para2Occurrences[1], tagAdjustedEnd),
				"After select at start occ(1) should have changed end adjustment");
			Assert.AreEqual("ayyy havingxx ayyy lotxx ofxx axx", Cache.GetTsStringProperty(m_para2Occurrences[1], tagPreview).Text,
				"After select at start occ(1) should have correct following context");
			AssertTextProp(m_para2Occurrences[1], tagPrecedingContext, 0, RespellUndoAction.SecondaryTextProp,
				RespellUndoAction.SecondaryTextVal, "after select at start prop should be set on initial (new) occurrence");
			AssertTextProp(m_para2Occurrences[1], tagPrecedingContext, 5, RespellUndoAction.SecondaryTextProp, -1,
				"after select at start prop should not be set on other words");
			AssertTextProp(m_para2Occurrences[1], tagPreview, "ayyy havingxx ".Length, RespellUndoAction.SecondaryTextProp,
				RespellUndoAction.SecondaryTextVal, "after select at start prop should be set on changed occurrence in Preview");
			AssertTextProp(m_para2Occurrences[1], tagPreview, "ayyy havingxx ".Length - 1, RespellUndoAction.SecondaryTextProp,
				-1, "after select at start prop should not be set on other text in Preview");
			// no longer action responsibilty. Assert.IsTrue(Cache.GetIntProperty(m_para2Occurrences[1], tagEnabled) != 0);
			Assert.AreEqual(ich3rdOcc + 2, Cache.GetIntProperty(m_para2Occurrences[2], tagAdjustedBegin),
				"After one change occ(2) should have appropriate begin adjustment");
			Assert.AreEqual(ich3rdOcc + 2 + 3, Cache.GetIntProperty(m_para2Occurrences[2], tagAdjustedEnd),
				"After one change occ(2) should have appropriate end adjustment");

			//------------------------------------------------------------------------
			// And now try turning one off.
			action.UpdatePreview(m_para2Occurrences[1], false);
			Assert.AreEqual("ayyy sentencexx axx havingxx ayyy lotxx ofxx axx", Cache.GetTsStringProperty(m_para2Occurrences[1], tagPrecedingContext).Text,
				"Turned-off occurrence should have full-length preview");
			Assert.AreEqual("ayyy sentencexx ".Length, Cache.GetIntProperty(m_para2Occurrences[1], tagAdjustedBegin),
				"Turned-off occurrence should still have adjusted begin");
			Assert.AreEqual("ayyy sentencexx axx havingxx axx", Cache.GetTsStringProperty(m_para2Occurrences[2], tagPrecedingContext).Text,
				"After two changes occ(2) should have appropriate preceding context");
			Assert.AreEqual(ich3rdOcc + 1, Cache.GetIntProperty(m_para2Occurrences[2], tagAdjustedBegin),
				"After two changes occ(2) should have appropriate begin adjustment");
			Assert.AreEqual(ich3rdOcc + 1 + 3, Cache.GetIntProperty(m_para2Occurrences[2], tagAdjustedEnd),
				"After two changes occ(2) should have appropriate end adjustment");

		}

		/// <summary>
		/// Assert that the specified object has the specified string property, and that the string
		/// has the specified text property at the specified location.
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="flid"></param>
		/// <param name="ich"></param>
		/// <param name="tpt"></param>
		/// <param name="val"></param>
		void AssertTextProp(int hvoObj, int flid, int ich, int tpt, int val, string message)
		{
			ITsString tss = Cache.GetTsStringProperty(hvoObj, flid);
			Assert.IsTrue(tss.Length > ich, "String is too short (" + message + ")");
			ITsTextProps props = tss.get_PropertiesAt(ich);
			int valActual, var;
			valActual = props.GetIntPropValues(tpt, out var);
			Assert.AreEqual(val, valActual, "String has wrong property value (" + message + ")");
		}
		[Test]
		public void PreserveCase()
		{
			Assert.AreEqual(6, m_axxOccurrences.Count);
			int ich2ndOcc = Cache.GetIntProperty(m_para1Occurrences[1], kflidBeginOffset);
			RespellUndoAction action = new RespellUndoAction(Cache, "axx", "ayyy");
			action.AddOccurrence(m_para1Occurrences[0]);
			action.AddOccurrence(m_para1Occurrences[1]);
			action.SetupPreviews(tagPrecedingContext, tagPreview, tagAdjustedBegin, tagAdjustedEnd, tagEnabled, m_axxOccurrences);
			Assert.AreEqual("Axx", Cache.GetTsStringProperty(m_para1Occurrences[0], tagPrecedingContext).Text,
				"Old value at start without preserve case");
			Assert.AreEqual("ayyy simplexx testxx withxx axx", Cache.GetTsStringProperty(m_para1Occurrences[1], tagPrecedingContext).Text,
				"Preceding context without preserve case has LC");
			action.PreserveCase = true;
			action.UpdatePreviews();
			Assert.AreEqual("Axx", Cache.GetTsStringProperty(m_para1Occurrences[0], tagPrecedingContext).Text,
				"Old value at start with preserver case");
			Assert.AreEqual("Ayyy simplexx testxx withxx axx", Cache.GetTsStringProperty(m_para1Occurrences[1], tagPrecedingContext).Text,
				"Preceding context with preserve case has UC");
		}

		/// <summary>
		/// A basic test of Apply, with two out of six occurrences changed.
		/// </summary>
		[Test]
		public void ApplyTwo()
		{
			RespellUndoAction action = new RespellUndoAction(Cache, "axx", "ayyy");
			action.AddOccurrence(m_para2Occurrences[1]);
			action.AddOccurrence(m_para2Occurrences[2]);
			action.DoIt();
			VerifyDoneStateApplyTwo();
			Assert.IsTrue(m_fdoCache.CanUndo, "undo should be possible after respelling");
			UndoResult ures;
			m_fdoCache.Undo(out ures);
			VerifyStartingState();
			m_fdoCache.Redo(out ures);
			VerifyDoneStateApplyTwo();
		}

		/// <summary>
		/// Verify everything any test cares about concerning the initial state.
		/// Used after various Undo operations to verify success.
		/// </summary>
		private void VerifyStartingState()
		{
			string text = m_para1.Contents.Text;
			Assert.AreEqual(text, "Axx simplexx testxx withxx axx lotxx ofxx wordsxx endingxx inxx xx", "para 1 changes should be undone");
			text = m_para2.Contents.Text;
			Assert.AreEqual(text, "axx sentencexx axx havingxx axx lotxx ofxx axx", "para 2 changes should be undone");
			VerifyTwfic(m_cba2.Hvo, "axx sentencexx axx havingxx axx ".Length, "axx sentencexx axx havingxx axx lotxx".Length,
				"following Twfic");
			VerifyTwfic(m_para1Occurrences[0], 0, "Axx".Length,
				"first para 1 Twfic changed");
			VerifyTwfic(m_para1Occurrences[1], "Axx simplexx testxx withxx ".Length, "Axx simplexx testxx withxx axx".Length,
				"first para 1 Twfic changed");
			VerifyTwfic(m_para2Occurrences[0], 0, "axx".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[1], "axx sentencexx ".Length, "axx sentencexx axx".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[2], "axx sentencexx axx havingxx ".Length, "axx sentencexx axx havingxx axx".Length,
				"second Twfic changed");
			VerifyTwfic(m_para2Occurrences[3], "axx sentencexx axx havingxx axx lotxx ofxx ".Length, "axx sentencexx axx havingxx axx lotxx ofxx axx".Length,
				"final (unchanged) Twfic");
			IWfiWordform wf = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "ayyy", Cache.DefaultVernWs, false));
			//the wordform becomes real, and that is not undoable.
			//Assert.IsTrue(wf.IsDummyObject, "should have deleted the WF");
			Assert.AreEqual(0, Cache.GetVectorSize(wf.Hvo, (int)WfiWordform.WfiWordformTags.kflidAnalyses),
				"when undone ayyy should have no analyses");

			IWfiWordform wfOld = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "axx", Cache.DefaultVernWs, false));
			Assert.AreEqual((int)SpellingStatusStates.undecided, wf.SpellingStatus);

			if (m_wfaAxe != null)
			{
				Assert.AreEqual("axx", m_wfaAxe.MorphBundlesOS[0].MorphRA.Form.VernacularDefaultWritingSystem,
					"lexicon should be restored(axe)");
				Assert.AreEqual("axx", m_wfaCut.MorphBundlesOS[0].MorphRA.Form.VernacularDefaultWritingSystem,
					"lexicon should be restored(cut)");
			}

			Assert.AreEqual(m_cAnalyses, wfOld.AnalysesOC.Count, "original analyes restored");
		}

		/// <summary>
		/// Verify the expected state of the world after doing (or redoing) changing the spelling of
		/// the middle two occurrences in paragraph 2.
		/// </summary>
		/// <param name="ich2ndOcc"></param>
		/// <param name="ich3rdOcc"></param>
		private void VerifyDoneStateApplyTwo()
		{
			string text = m_para2.Contents.Text;
			Assert.AreEqual(text, "axx sentencexx ayyy havingxx ayyy lotxx ofxx axx", "expected text changes should occur");
			VerifyTwfic(m_cba2.Hvo, "axx sentencexx ayyy havingxx ayyy ".Length, "axx sentencexx ayyy havingxx ayyy lotxx".Length,
				"following Twfic");
			VerifyTwfic(m_para2Occurrences[0], 0, "axx".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[1], "axx sentencexx ".Length, "axx sentencexx ayyy".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[2], "axx sentencexx ayyy havingxx ".Length, "axx sentencexx ayyy havingxx ayyy".Length,
				"second Twfic changed");
			VerifyTwfic(m_para2Occurrences[3], "axx sentencexx ayyy havingxx ayyy lotxx ofxx ".Length, "axx sentencexx ayyy havingxx ayyy lotxx ofxx axx".Length,
				"final (unchanged) Twfic");
			IWfiWordform wf = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "ayyy", Cache.DefaultVernWs, false));
			Assert.IsFalse(wf.IsDummyObject, "should have a real WF to hold spelling status");
			Assert.AreEqual((int)SpellingStatusStates.correct, wf.SpellingStatus);
		}

		private void VerifyTwfic(int cba, int begin, int end, string message)
		{
			Assert.AreEqual(begin, m_fdoCache.GetIntProperty(cba, kflidBeginOffset), message + " beginOffset");
			Assert.AreEqual(end, m_fdoCache.GetIntProperty(cba, kflidEndOffset), message + " endOffset");
		}

		/// <summary>
		/// Change two out of six occurrences and also copy analysis information.
		/// </summary>
		[Test]
		public void ApplyTwoAndCopyAnalyses()
		{
			MakeMonoAnalyses();
			MakeMultiAnalyses();
			// Makes one more analysis, which (having no human approval) should NOT get copied.
			m_wfAxx.AnalysesOC.Add(new WfiAnalysis());
			m_cAnalyses++;

			RespellUndoAction action = new RespellUndoAction(Cache, "axx", "ayyy");
			action.AddOccurrence(m_para2Occurrences[1]);
			action.AddOccurrence(m_para2Occurrences[2]);
			action.CopyAnalyses = true;
			action.DoIt();
			VerifyDoneStateApplyTwoAndCopyAnalyses();
			Assert.IsTrue(m_fdoCache.CanUndo, "undo should be possible after respelling");
			UndoResult ures;
			m_fdoCache.Undo(out ures);
			VerifyStartingState();
			m_fdoCache.Redo(out ures);
			VerifyDoneStateApplyTwoAndCopyAnalyses();
		}

		private void VerifyDoneStateApplyTwoAndCopyAnalyses()
		{
			VerifyDoneStateApplyTwo();
			IWfiWordform wf = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "ayyy", Cache.DefaultVernWs, false));
			VerifyAnalysis(wf, "axe", 0, "axx", "axe", "first mono");
			VerifyAnalysis(wf, "chopper", -1, null, null, "second gloss");
			VerifyAnalysis(wf, "cut", 0, "axx", "cut", "second mono");
			VerifyAnalysis(wf, "cut.it", 0, "ax", "cut", "first morph cut.it");
			VerifyAnalysis(wf, "cut.it", 1, "x", "it", "2nd morph cut.it");
			VerifyAnalysis(wf, "not.rude", 0, "a", "not", "first morph not.rude");
			VerifyAnalysis(wf, "not.rude", 1, "xx", "rude", "2nd morph not.rude");
		}

		// Verify something about analysis i of the given wordform, specifically, that it has an analysis at iAnalysis
		// which has a word gloss (at index iGloss) equal to wgloss,
		// and a morph bundle at iMorph with the specified form and gloss.
		// iGloss or iMorph may be -1 to suppress.
		private void VerifyAnalysis(IWfiWordform wf, string wgloss, int iMorph, string form, string mgloss,
			string message)
		{
			foreach (WfiAnalysis analysis in wf.AnalysesOC)
			{
				foreach (IWfiGloss wg in analysis.MeaningsOC)
				{
					if (wgloss == wg.Form.AnalysisDefaultWritingSystem)
					{
						if (iMorph >= 0)
						{
							IWfiMorphBundle bundle = analysis.MorphBundlesOS[iMorph];
							Assert.AreEqual(mgloss, bundle.SenseRA.Gloss.AnalysisDefaultWritingSystem, message + " morph gloss");
							Assert.AreEqual(form, bundle.MorphRA.Form.VernacularDefaultWritingSystem, message + " morph form");
						}
						return; // found what we want, mustn't hit the Fail below!
					}
				}
			}
			Assert.Fail(message + " word gloss not found");
		}

		/// <summary>
		/// Make (two) monomorphemic analyses on our favorite wordform, connected to two entries, one with two glosses.
		/// </summary>
		private void MakeMonoAnalyses()
		{
			string formLexEntry = "axx";
			ITsString tssLexEntryForm = Cache.MakeVernTss(formLexEntry);
			int clsidForm;
			ILexEntry entry = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"axe", null);
			ILexSense senseAxe = entry.SensesOS[0];
			IMoForm form = entry.LexemeFormOA;

			m_wfaAxe = new WfiAnalysis();
			m_wfAxx.AnalysesOC.Add(m_wfaAxe);
			IWfiMorphBundle bundle = m_wfaAxe.MorphBundlesOS.Append(new WfiMorphBundle());
			bundle.MorphRA = form;
			bundle.SenseRA = senseAxe;

			m_wgAxe = new WfiGloss();
			m_wfaAxe.MeaningsOC.Add(m_wgAxe);
			m_wgAxe.Form.AnalysisDefaultWritingSystem = "axe";

			m_wgChopper = new WfiGloss();
			m_wfaAxe.MeaningsOC.Add(m_wgChopper);
			m_wgChopper.Form.AnalysisDefaultWritingSystem = "chopper";
			m_wfaAxe.SetAgentOpinion(m_fdoCache.LangProject.DefaultUserAgent, Opinions.approves);

			ILexEntry entryCut = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"cut", null);
			m_wfaCut = new WfiAnalysis();
			m_wfAxx.AnalysesOC.Add(m_wfaCut);
			bundle = m_wfaCut.MorphBundlesOS.Append(new WfiMorphBundle());
			bundle.MorphRA = entryCut.LexemeFormOA;
			bundle.SenseRA = entryCut.SensesOS[0];

			m_wgCut = new WfiGloss();
			m_wfaCut.MeaningsOC.Add(m_wgCut);
			m_wgCut.Form.AnalysisDefaultWritingSystem = "cut";
			m_wfaCut.SetAgentOpinion(m_fdoCache.LangProject.DefaultUserAgent, Opinions.approves);

			m_cAnalyses += 2;
		}
		/// <summary>
		/// Make (two) multimorphemic analyses on our favorite wordform, connected to four entries.
		/// </summary>
		private void MakeMultiAnalyses()
		{
			m_wfaCutIt = Make2BundleAnalysis("ax", "-x", "cut", "it");
			m_wfaNotRude = Make2BundleAnalysis("a-", "xx", "not", "rude");
		}

		private WfiAnalysis Make2BundleAnalysis(string form1, string form2, string gloss1, string gloss2)
		{
			WfiAnalysis result;
			ILexEntry entry1 = MakeEntry(form1, gloss1);
			ILexEntry entry2 = MakeEntry(form2, gloss2);

			result = new WfiAnalysis();
			m_wfAxx.AnalysesOC.Add(result);
			IWfiMorphBundle bundle = result.MorphBundlesOS.Append(new WfiMorphBundle());
			bundle.MorphRA = entry1.LexemeFormOA;
			bundle.SenseRA = entry1.SensesOS[0];
			bundle = result.MorphBundlesOS.Append(new WfiMorphBundle());
			bundle.MorphRA = entry2.LexemeFormOA;
			bundle.SenseRA = entry2.SensesOS[0];

			WfiGloss gloss = new WfiGloss();
			result.MeaningsOC.Add(gloss);
			gloss.Form.AnalysisDefaultWritingSystem = gloss1 + "." + gloss2;
			result.SetAgentOpinion(m_fdoCache.LangProject.DefaultUserAgent, Opinions.approves);

			m_cAnalyses++;

			return result;
		}

		private ILexEntry MakeEntry(string form, string gloss)
		{
			ITsString tssLexEntryForm = Cache.MakeVernTss(form);
			string form1 = form;
			int clsidForm;
			return LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref form1, out clsidForm), tssLexEntryForm,
					gloss, null);
		}

		private void AddAllOccurrences(RespellUndoAction action, List<int> occurrences)
		{
			foreach (int hvo in occurrences)
				action.AddOccurrence(hvo);
		}

		/// <summary>
		/// Change all six occurrences and also update the lexicon (monomorphemic).
		/// This test is also important for confirming that preserve case works, and that it works
		/// to reduce rather than increasing the length of the word, and that when all are changed,
		/// multimorphemic analyses are deleted by default.
		/// </summary>
		[Test]
		public void ApplyAllAndUpdateLexicon()
		{
			MakeMonoAnalyses();
			MakeMultiAnalyses();
			RespellUndoAction action = new RespellUndoAction(Cache, "axx", "ay");
			AddAllOccurrences(action, m_para1Occurrences);
			AddAllOccurrences(action, m_para2Occurrences);
			action.AllChanged = true;
			action.UpdateLexicalEntries = true;
			action.PreserveCase = true;
			action.DoIt();
			VerifyDoneStateApplyAllAndUpdateLexicon();
			Assert.IsTrue(m_fdoCache.CanUndo, "undo should be possible after respelling");
			UndoResult ures;
			m_fdoCache.Undo(out ures);
			VerifyStartingState();
			m_fdoCache.Redo(out ures);
			VerifyDoneStateApplyAllAndUpdateLexicon();
		}

		private void VerifyDoneStateApplyAllAndUpdateLexicon()
		{
			string text = m_para1.Contents.Text;
			Assert.AreEqual(text, "Ay simplexx testxx withxx ay lotxx ofxx wordsxx endingxx inxx xx", "expected text changes para 1");
			text = m_para2.Contents.Text;
			Assert.AreEqual(text, "ay sentencexx ay havingxx ay lotxx ofxx ay", "expected text changes para 2");
			VerifyTwfic(m_cba2.Hvo, "ay sentencexx ay havingxx ay ".Length, "ay sentencexx ay havingxx ay lotxx".Length,
				"following Twfic");
			VerifyTwfic(m_para1Occurrences[0], 0, "Ay".Length,
				"first para 1 Twfic changed");
			VerifyTwfic(m_para1Occurrences[1], "Ay simplexx testxx withxx ".Length, "Ay simplexx testxx withxx ay".Length,
				"first para 1 Twfic changed");
			VerifyTwfic(m_para2Occurrences[0], 0, "ay".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[1], "ay sentencexx ".Length, "ay sentencexx ay".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[2], "ay sentencexx ay havingxx ".Length, "ay sentencexx ay havingxx ay".Length,
				"second Twfic changed");
			VerifyTwfic(m_para2Occurrences[3], "ay sentencexx ay havingxx ay lotxx ofxx ".Length, "ay sentencexx ay havingxx ay lotxx ofxx ay".Length,
				"final (unchanged) Twfic");
			IWfiWordform wf = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "ay", Cache.DefaultVernWs, false));
			Assert.IsFalse(wf.IsDummyObject, "should have a real WF to hold spelling status");
			Assert.AreEqual((int)SpellingStatusStates.correct, wf.SpellingStatus);

			IWfiWordform wfOld = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "axx", Cache.DefaultVernWs, false));
			Assert.IsFalse(wfOld.IsDummyObject, "should have a real WF to hold old spelling status");
			Assert.AreEqual((int)SpellingStatusStates.incorrect, wfOld.SpellingStatus);

			Assert.AreEqual("ay", m_wfaAxe.MorphBundlesOS[0].MorphRA.Form.VernacularDefaultWritingSystem, "lexicon should be updated(axe)");
			Assert.AreEqual("ay", m_wfaCut.MorphBundlesOS[0].MorphRA.Form.VernacularDefaultWritingSystem, "lexicon should be updated(cut)");

			Assert.AreEqual(0, wfOld.AnalysesOC.Count, "old wordform has no analyses");
			Assert.AreEqual(2, wf.AnalysesOC.Count, "two analyses survived");
			foreach (WfiAnalysis wa in wf.AnalysesOC)
				Assert.AreEqual(1, wa.MorphBundlesOS.Count, "only monomorphemic analyses survived");
		}

		/// <summary>
		/// Change all six occurrences and also keep the analyses (but don't update lexicon).
		/// This test is also important for confirming that case is not preserved when that option is off,
		/// and that it works to not change the length of the word.
		/// </summary>
		[Test]
		public void ApplyAllAndKeepAnalyses()
		{
			MakeMonoAnalyses();
			MakeMultiAnalyses();
			RespellUndoAction action = new RespellUndoAction(Cache, "axx", "byy");
			AddAllOccurrences(action, m_para1Occurrences);
			AddAllOccurrences(action, m_para2Occurrences);
			action.AllChanged = true;
			action.KeepAnalyses = true;
			action.DoIt();
			VerifyDoneStateApplyAllAndKeepAnalyses();
			Assert.IsTrue(m_fdoCache.CanUndo, "undo should be possible after respelling");
			UndoResult ures;
			m_fdoCache.Undo(out ures);
			VerifyStartingState();
			m_fdoCache.Redo(out ures);
			VerifyDoneStateApplyAllAndKeepAnalyses();
		}

		private void VerifyDoneStateApplyAllAndKeepAnalyses()
		{
			string text = m_para1.Contents.Text;
			Assert.AreEqual(text, "byy simplexx testxx withxx byy lotxx ofxx wordsxx endingxx inxx xx", "expected text changes para 1");
			text = m_para2.Contents.Text;
			Assert.AreEqual(text, "byy sentencexx byy havingxx byy lotxx ofxx byy", "expected text changes para 2");
			VerifyTwfic(m_cba2.Hvo, "byy sentencexx byy havingxx byy ".Length, "byy sentencexx byy havingxx byy lotxx".Length,
				"following Twfic");
			VerifyTwfic(m_para1Occurrences[0], 0, "byy".Length,
				"first para 1 Twfic changed");
			VerifyTwfic(m_para1Occurrences[1], "byy simplexx testxx withxx ".Length, "byy simplexx testxx withxx byy".Length,
				"first para 1 Twfic changed");
			VerifyTwfic(m_para2Occurrences[0], 0, "byy".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[1], "byy sentencexx ".Length, "byy sentencexx byy".Length,
				"first Twfic changed");
			VerifyTwfic(m_para2Occurrences[2], "byy sentencexx byy havingxx ".Length, "byy sentencexx byy havingxx byy".Length,
				"second Twfic changed");
			VerifyTwfic(m_para2Occurrences[3], "byy sentencexx byy havingxx byy lotxx ofxx ".Length, "byy sentencexx byy havingxx byy lotxx ofxx byy".Length,
				"final (unchanged) Twfic");
			IWfiWordform wf = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "byy", Cache.DefaultVernWs, false));
			Assert.IsFalse(wf.IsDummyObject, "should have a real WF to hold spelling status");
			Assert.AreEqual((int)SpellingStatusStates.correct, wf.SpellingStatus);

			IWfiWordform wfOld = WfiWordform.CreateFromDBObject(Cache,
				WfiWordform.FindOrCreateWordform(Cache, "axx", Cache.DefaultVernWs, false));
			Assert.IsFalse(wfOld.IsDummyObject, "should have a real WF to hold old spelling status");
			Assert.AreEqual((int)SpellingStatusStates.incorrect, wfOld.SpellingStatus);

			Assert.AreEqual("axx", m_wfaAxe.MorphBundlesOS[0].MorphRA.Form.VernacularDefaultWritingSystem, "lexicon should not be updated(axe)");
			Assert.AreEqual("axx", m_wfaCut.MorphBundlesOS[0].MorphRA.Form.VernacularDefaultWritingSystem, "lexicon should not be updated(cut)");

			Assert.AreEqual(0, wfOld.AnalysesOC.Count, "old wordform has no analyses");
			Assert.AreEqual(4, wf.AnalysesOC.Count, "all analyses survived");
		}
	}
#endif
}
