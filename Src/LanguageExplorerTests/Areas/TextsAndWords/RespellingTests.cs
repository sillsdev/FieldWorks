// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Areas.TextsAndWords
{
	[TestFixture]
	public class RespellingTests : MemoryOnlyBackendProviderTestBase
	{
		private const int kObjectListFlid = 89999956;
		private FlexComponentParameters _flexComponentParameters;

		#region Overrides of FdoTestBase

		/// <summary>
		/// Done before each test.
		/// Overriders should call base method.
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			_flexComponentParameters = TestSetupServices.SetupEverything(Cache, false);
		}

		/// <summary>
		/// Done after each test.
		/// </summary>
		public override void TestTearDown()
		{
			try
			{
				while (m_actionHandler.CanUndo())
				{
					Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
				}
				Assert.AreEqual(0, m_actionHandler.UndoableSequenceCount);
				var interestingTextlist = _flexComponentParameters.PropertyTable.GetValue<InterestingTextList>(AreaServices.InterestingTexts);
				if (interestingTextlist != null)
				{
					Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>().RemoveNotification(interestingTextlist);
					_flexComponentParameters.PropertyTable.RemoveProperty(AreaServices.InterestingTexts);
				}
				_flexComponentParameters.PropertyTable.RemoveProperty(FwUtils.cache);
				TestSetupServices.DisposeTrash(_flexComponentParameters);
				_flexComponentParameters = null;
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

		/// <summary>
		/// Test simulating multiple spelling changes in a single segment in a paragraph. Should
		/// be possible to undo this change. This test was written for TE-9243, but then we
		/// realized that it actually worked fine all along. Still a nice test though, don't you
		/// think?
		/// </summary>
		[Test]
		public void CanUndoChangeMultipleOccurrences_InSingleSegment()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are hoping, we are.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out para);
			respellUndoAction.AllChanged = false;
			respellUndoAction.KeepAnalyses = true;
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
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
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out para);
			respellUndoAction.AllChanged = false;
			respellUndoAction.KeepAnalyses = true;
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
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
			var morphs = new[] { "multi", "morphemic" };
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out para, true, morphs);
			Assert.AreEqual(2, para.SegmentsOS[0].AnalysesRS[3].Analysis.MorphBundlesOS.Count, "Should have 2 morph bundles before spelling change.");
			respellUndoAction.AllChanged = true;
			respellUndoAction.KeepAnalyses = true;
			respellUndoAction.CopyAnalyses = true; // in the dialog this is always true?
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
			Assert.AreEqual(0, para.SegmentsOS[0].AnalysesRS[2].Analysis.MorphBundlesOS.Count, "Unexpected morph bundle contents for 'be'");
			Assert.AreEqual(2, para.SegmentsOS[0].AnalysesRS[3].Analysis.MorphBundlesOS.Count, "Wrong morph bundle count for 'multimorphemic'");
			Assert.AreEqual(0, para.SegmentsOS[1].AnalysesRS[2].Analysis.MorphBundlesOS.Count, "Unexpected morph bundle contents for 'are'");
			Assert.AreEqual(2, para.SegmentsOS[1].AnalysesRS[1].Analysis.MorphBundlesOS.Count, "Wrong morph bundle count for 'multimorphemic'");
			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// <summary>
		/// Test simulating multiple spelling changes in multiple segments in a single
		/// paragraph. Should be possible to undo this change. TE-9243.
		/// </summary>
		[Test]
		public void CanUndoChangeMultipleOccurrences_InMultipleSegmentsInPara()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are nice. Hoping is what we do when we want. Therefore, we are nice, aren't we? Yes.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out para);
			respellUndoAction.AllChanged = false;
			respellUndoAction.KeepAnalyses = true;
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// <summary>
		/// Test simulating multiple spelling changes in a single segment in a paragraph. Should
		/// be possible to undo this change.
		/// </summary>
		[Test]
		public void CanUndoChangeMultipleOccurrences_InSingleSegment_Glosses()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are hoping, we are.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out para,  true);
			respellUndoAction.AllChanged = false;
			respellUndoAction.KeepAnalyses = true;
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), para.Contents.Text);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[0] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[2] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[4] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[5] is IWfiGloss);
			Assert.IsTrue(para.SegmentsOS[0].AnalysesRS[8] is IWfiGloss);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		/// <summary>
		/// Test simulating multiple spelling changes in multiple segments in a single
		/// paragraph. Should be possible to undo this change. FWR-3424.
		/// </summary>
		[Test]
		public void CanUndoChangeMultipleOccurrences_InMultipleSegmentsInPara_Glosses()
		{
			IStTxtPara para;
			const string ksParaText = "If we hope we are nice. Hoping is what we do when we want. Therefore, we are nice, aren't we? Yes.";
			const string ksWordToReplace = "we";
			const string ksNewWord = ksWordToReplace + "Q";
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out para, true);
			UndoableUnitOfWorkHelper.Do("Undo Added FT", "Redo Added FT", m_actionHandler, () =>
			{
				var i = 0;
				foreach (var seg in para.SegmentsOS)
				{
					seg.FreeTranslation.SetAnalysisDefaultWritingSystem("Segment " + (i++) + " FT");
				}
			});
			respellUndoAction.AllChanged = false;
			respellUndoAction.KeepAnalyses = true;
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
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

		/// <summary>
		/// Test simulating single spelling change in a single segment in a paragraph. Should
		/// be possible to undo this change.
		/// </summary>
		[Test]
		public void CanUndoChangeSingleOccurrence_InSingleSegment()
		{
			IStTxtPara paragraph;
			const string ksParaText = "If we hope we are hoping, we are.";
			const string ksWordToReplace = "hope";
			const string ksNewWord = ksWordToReplace + "ful";
			var respellUndoAction = SetUpParaAndRespellUndoAction(ksParaText, ksWordToReplace, ksNewWord, out paragraph);
			respellUndoAction.AllChanged = true;
			respellUndoAction.CopyAnalyses = false;
			respellUndoAction.KeepAnalyses = false;
			respellUndoAction.PreserveCase = true;
			respellUndoAction.UpdateLexicalEntries = true;
			respellUndoAction.DoIt(_flexComponentParameters.Publisher);
			Assert.AreEqual(ksParaText.Replace(ksWordToReplace, ksNewWord), paragraph.Contents.Text);
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.IsTrue(m_actionHandler.CanUndo());
		}

		private RespellUndoAction SetUpParaAndRespellUndoAction(string paragraphText, string wordToReplace, string newWord, out IStTxtPara paragraph, bool createGlosses = false, string[] morphsToCreate = null)
		{
			var paraFragments = new List<IParaFragment>();
			// Can't use "paragraph" in this method, so use "paragraphSurrogate" until the very end.
			IStTxtPara paragraphSurrogate = null;
			IStText stText = null;
			UndoableUnitOfWorkHelper.Do("Undo create text", "Redo create text", m_actionHandler, () =>
			{
				var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = stText;
				paragraphSurrogate = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				stText.ParagraphsOS.Add(paragraphSurrogate);
				paragraphSurrogate.Contents = TsStringUtils.MakeString(paragraphText, Cache.DefaultVernWs);
				foreach (var seg in paragraphSurrogate.SegmentsOS)
				{
					LcmTestHelper.CreateAnalyses(seg, paragraphSurrogate.Contents, seg.BeginOffset, seg.EndOffset, createGlosses);
					var thisSegParaFrags = new List<IParaFragment>(); //GetParaFragmentsInSegmentForWord(seg, wordToReplace);
					IAnalysis analysis = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetMatchingWordform(Cache.DefaultVernWs, wordToReplace);
					var ichStart = 0;
					int ichWe;
					while ((ichWe = seg.BaselineText.Text.IndexOf(wordToReplace, ichStart)) >= 0)
					{
						ichStart = ichWe + wordToReplace.Length;
						ichWe += seg.BeginOffset;
						thisSegParaFrags.Add(new ParaFragment(seg, ichWe, ichWe + wordToReplace.Length, analysis));
					}
					if (morphsToCreate != null)
					{
						var morphFact = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
						// IWfiWordform, IWfiAnalysis, and IWfiGloss objects will have already been created.
						// Still need to add WfiMorphBundles as per morphsToCreate.
						foreach (IWfiWordform wordform in thisSegParaFrags.Select(x => x.Analysis))
						{
							var wfiAnalysis = wordform.AnalysesOC.First();
							if (wfiAnalysis.MorphBundlesOS.Count != 0)
							{
								continue;
							}
							foreach (var morpheme in morphsToCreate)
							{
								var bundle = morphFact.Create();
								wfiAnalysis.MorphBundlesOS.Add(bundle);
								bundle.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(morpheme, Cache.DefaultVernWs);
							}
						}
					}
					paraFragments.AddRange(thisSegParaFrags);
				}
			});
			var rsda = new RespellingSda(Cache, Cache.ServiceLocator);
			var dummyTextList = MockRepository.GenerateStub<InterestingTextList>(_flexComponentParameters.PropertyTable, Cache.ServiceLocator.GetInstance<ITextRepository>(),
			Cache.ServiceLocator.GetInstance<IStTextRepository>());
			dummyTextList.Stub(t1 => t1.InterestingTexts).Return(new[] { stText });
			ReflectionHelper.SetField(rsda, "m_interestingTexts", dummyTextList);
			rsda.InitializeFlexComponent(_flexComponentParameters);
			rsda.SetOccurrences(0, paraFragments);
			var publisher = new ObjectListPublisher(rsda, kObjectListFlid);
			var xmlCache = MockRepository.GenerateStub<XMLViewsDataCache>(publisher, true, new Dictionary<int, int>());
			xmlCache.Stub(c => c.get_IntProp(paragraphSurrogate.Hvo, CmObjectTags.kflidClass)).Return(StTxtParaTags.kClassId);
			xmlCache.Stub(c => c.VecProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int[]>(publisher.VecProp));
			xmlCache.MetaDataCache = new RespellingMdc(Cache.GetManagedMetaDataCache());
			xmlCache.Stub(c => c.get_ObjectProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int>(publisher.get_ObjectProp));
			xmlCache.Stub(c => c.get_IntProp(Arg<int>.Is.Anything, Arg<int>.Is.Anything)).Do(new Func<int, int, int>(publisher.get_IntProp));
			var respellUndoaction = new RespellUndoAction(xmlCache, Cache, wordToReplace, newWord);
			foreach (var hvoFake in rsda.VecProp(0, ConcDecorator.kflidConcOccurrences))
			{
				respellUndoaction.AddOccurrence(hvoFake);
			}
			paragraph = paragraphSurrogate;
			return respellUndoaction;
		}
	}
}