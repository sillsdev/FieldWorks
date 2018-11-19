// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Areas.TextsAndWords
{
	[TestFixture]
	public class RespellingTests : MemoryOnlyBackendProviderTestBase
	{
		private const int kObjectListFlid = 89999956;
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private ISubscriber m_subscriber;

	#region Overrides of FdoTestBase

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
#if RANDYTODO
				// TODO: Get this and tests to not use TranslatedScriptureOA and things in it.
#endif
				Cache.LanguageProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Done before each test.
		/// Overriders should call base method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			TestSetupServices.SetupTestTriumvirate(out m_propertyTable, out m_publisher, out m_subscriber);
			m_propertyTable.SetProperty("cache", Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Done after each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			try
			{
				while (m_actionHandler.CanUndo())
				{
					Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
				}
				Assert.AreEqual(0, m_actionHandler.UndoableSequenceCount);

				if (m_propertyTable != null)
				{
					var interestingTextlist = m_propertyTable.GetValue<InterestingTextList>("InterestingTexts");
					if (interestingTextlist != null)
					{
						Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>().RemoveNotification(interestingTextlist);
						m_propertyTable.RemoveProperty("InterestingTexts");
					}
					m_propertyTable.RemoveProperty(LanguageExplorerConstants.cache);
					m_propertyTable.Dispose();
				}
				m_propertyTable = null;
				m_publisher = null;
				m_subscriber = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
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

			respellUndoaction.DoIt(m_publisher);

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

			respellUndoaction.DoIt(m_publisher);

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

			respellUndoaction.DoIt(m_publisher);

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

			respellUndoaction.DoIt(m_publisher);

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

			respellUndoaction.DoIt(m_publisher);

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

			respellUndoaction.DoIt(m_publisher);

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

#if RANDYTODO
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

			respellUndoaction.DoIt(m_publisher);

			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}
#endif

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
					paraT.Contents = TsStringUtils.MakeString(sParaText, Cache.DefaultVernWs);
					object owner = ReflectionHelper.CreateObject("SIL.LCModel.dll", "SIL.LCModel.Infrastructure.Impl.CmObjectId", BindingFlags.NonPublic,
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
					paraT.Contents = TsStringUtils.MakeString(sParaText, Cache.DefaultVernWs);
				}
				foreach (ISegment seg in paraT.SegmentsOS)
				{
					LcmTestHelper.CreateAnalyses(seg, paraT.Contents, seg.BeginOffset, seg.EndOffset, fCreateGlosses);
					paraFrags.AddRange(GetParaFragmentsInSegmentForWord(seg, sWordToReplace));
				}
			});

			var rsda = new RespellingSda(Cache, Cache.ServiceLocator);
			InterestingTextList dummyTextList = MockRepository.GenerateStub<InterestingTextList>(m_propertyTable, Cache.ServiceLocator.GetInstance<ITextRepository>(),
			Cache.ServiceLocator.GetInstance<IStTextRepository>());
			if (clidPara == ScrTxtParaTags.kClassId)
				dummyTextList.Stub(tl => tl.InterestingTexts).Return(new IStText[0]);
			else
				dummyTextList.Stub(t1 => t1.InterestingTexts).Return(new IStText[1] { stText });
			ReflectionHelper.SetField(rsda, "m_interestingTexts", dummyTextList);
			rsda.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			rsda.SetOccurrences(0, paraFrags);
			ObjectListPublisher publisher = new ObjectListPublisher(rsda, kObjectListFlid);
			XMLViewsDataCache xmlCache = MockRepository.GenerateStub<XMLViewsDataCache>(publisher, true, new Dictionary<int, int>());

			xmlCache.Stub(c => c.get_IntProp(paraT.Hvo, CmObjectTags.kflidClass)).Return(ScrTxtParaTags.kClassId);
			xmlCache.Stub(c => c.VecProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int[]>(publisher.VecProp));
			xmlCache.MetaDataCache = new RespellingMdc(Cache.GetManagedMetaDataCache());
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
					paraT.Contents = TsStringUtils.MakeString(sParaText, Cache.DefaultVernWs);
					object owner = ReflectionHelper.CreateObject("SIL.LCModel.dll", "SIL.LCModel.Infrastructure.Impl.CmObjectId", BindingFlags.NonPublic,
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
					paraT.Contents = TsStringUtils.MakeString(sParaText, Cache.DefaultVernWs);
				}
				foreach (ISegment seg in paraT.SegmentsOS)
				{
					LcmTestHelper.CreateAnalyses(seg, paraT.Contents, seg.BeginOffset, seg.EndOffset, true);
					var thisSegParaFrags = GetParaFragmentsInSegmentForWord(seg, sWordToReplace);
					SetMultimorphemicAnalyses(thisSegParaFrags, morphsToCreate);
					paraFrags.AddRange(thisSegParaFrags);
				}
			});

			var rsda = new RespellingSda(Cache, Cache.ServiceLocator);
			InterestingTextList dummyTextList = MockRepository.GenerateStub<InterestingTextList>(m_propertyTable, Cache.ServiceLocator.GetInstance<ITextRepository>(),
			Cache.ServiceLocator.GetInstance<IStTextRepository>());
			if (clidPara == ScrTxtParaTags.kClassId)
				dummyTextList.Stub(tl => tl.InterestingTexts).Return(new IStText[0]);
			else
				dummyTextList.Stub(t1 => t1.InterestingTexts).Return(new IStText[1] { stText });
			ReflectionHelper.SetField(rsda, "m_interestingTexts", dummyTextList);
			rsda.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			rsda.SetOccurrences(0, paraFrags);
			ObjectListPublisher publisher = new ObjectListPublisher(rsda, kObjectListFlid);
			XMLViewsDataCache xmlCache = MockRepository.GenerateStub<XMLViewsDataCache>(publisher, true, new Dictionary<int, int>());

			xmlCache.Stub(c => c.get_IntProp(paraT.Hvo, CmObjectTags.kflidClass)).Return(ScrTxtParaTags.kClassId);
			xmlCache.Stub(c => c.VecProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int[]>(publisher.VecProp));
			xmlCache.MetaDataCache = new RespellingMdc(Cache.GetManagedMetaDataCache());
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
					bundle.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(morpheme, Cache.DefaultVernWs);
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
}
