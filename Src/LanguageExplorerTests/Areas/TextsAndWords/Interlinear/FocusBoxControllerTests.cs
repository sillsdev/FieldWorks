// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	public class FocusBoxControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private IText m_text0;
		private IStText m_stText0;
		private IStTxtPara m_para0_0;
		private TestableFocusBox m_focusBox;
		private MockInterlinDocForAnalysis m_interlinDoc;
		private IList<AnalysisTree> m_analysis_para0_0 = new List<AnalysisTree>();
		private FlexComponentParameters _flexComponentParameters;

		/// <summary />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			// setup default vernacular ws.
			var wsXkal = Cache.ServiceLocator.WritingSystemManager.Set("qaa-x-kal");
			wsXkal.DefaultFont = new FontDefinition("Times New Roman");
			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(wsXkal);
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Insert(0, wsXkal);
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_text0 = textFactory.Create();
			m_stText0 = stTextFactory.Create();
			m_text0.ContentsOA = m_stText0;
			m_para0_0 = m_stText0.AddNewTextPara(null);
			m_para0_0.Contents = TsStringUtils.MakeString("Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.", wsXkal.Handle);

			m_stText0.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(false);
			// paragraph 0_0 simply has wordforms as analyses
			foreach (var occurence in SegmentServices.GetAnalysisOccurrences(m_para0_0))
			{
				if (occurence.HasWordform)
				{
					m_analysis_para0_0.Add(new AnalysisTree(occurence.Analysis));
				}
			}
		}

		public override void TestSetup()
		{
			base.TestSetup();
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			m_interlinDoc = new MockInterlinDocForAnalysis(m_stText0);
			m_interlinDoc.InitializeFlexComponent(_flexComponentParameters);
			m_focusBox = m_interlinDoc.FocusBox as TestableFocusBox;
		}

		public override void TestTearDown()
		{
			try
			{
				while (Cache.ActionHandlerAccessor.CanUndo())
				{
					Cache.ActionHandlerAccessor.Undo();
				}
				m_interlinDoc.Dispose();
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

		/// <summary>
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// </summary>
		[Test]
		public void ApproveAndStayPut_NoChange()
		{
			var occurrences = SegmentServices.GetAnalysisOccurrences(m_para0_0).ToList();
			m_interlinDoc.SelectOccurrence(occurrences[0]);
			var initialAnalysisObj = m_focusBox.InitialAnalysis.Analysis;
			// approve same wordform. Should not result in change during approve.
			m_focusBox.NewAnalysisTree.Analysis = occurrences[0].Analysis;
			m_focusBox.ApproveAndStayPut("Do Something");

			// expect no change to the first occurrence.
			Assert.AreEqual(initialAnalysisObj, occurrences[0].Analysis);
			// expect the focus box to still be on the first occurrence.
			Assert.AreEqual(occurrences[0], m_focusBox.SelectedOccurrence);

			// nothing to undo.
			Assert.AreEqual(0, Cache.ActionHandlerAccessor.UndoableSequenceCount);
		}

		/// <summary>
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// </summary>
		[Test]
		public void ApproveAndStayPut_NewWordGloss()
		{
			var occurrences = SegmentServices.GetAnalysisOccurrences(m_para0_0).ToList();
			m_interlinDoc.SelectOccurrence(occurrences[0]);
			// create a new analysis.
			var initialAnalysisTree = m_focusBox.InitialAnalysis;
			m_focusBox.DoDuringUnitOfWork = () => WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(initialAnalysisTree.Wordform);
			m_focusBox.ApproveAndStayPut("Do Something");

			// expect change to the first occurrence.
			Assert.AreEqual(m_focusBox.NewAnalysisTree.Gloss, occurrences[0].Analysis);
			// expect the focus box to still be on the first occurrence.
			Assert.AreEqual(occurrences[0], m_focusBox.SelectedOccurrence);

			// test undo.
			Assert.AreEqual(1, Cache.ActionHandlerAccessor.UndoableSequenceCount);
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(initialAnalysisTree.Analysis, occurrences[0].Analysis);
			// expect the focus box to still be on the first occurrence.
			Assert.AreEqual(occurrences[0], m_focusBox.SelectedOccurrence);
		}

		/// <summary>
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// </summary>
		[Test]
		public void ApproveAndMoveNext_NoChange()
		{
			var occurrences = SegmentServices.GetAnalysisOccurrences(m_para0_0).ToList();
			m_interlinDoc.SelectOccurrence(occurrences[0]);
			var initialAnalysisTree = m_focusBox.InitialAnalysis;
			m_focusBox.ApproveAndMoveNext("Do Something");

			// expect no change to the first occurrence.
			Assert.AreEqual(initialAnalysisTree.Analysis, occurrences[0].Analysis);
			// expect the focus box to be on the next occurrence.
			Assert.AreEqual(occurrences[1], m_focusBox.SelectedOccurrence);

			// nothing to undo.
			Assert.AreEqual(0, Cache.ActionHandlerAccessor.UndoableSequenceCount);
		}

		#region OnAddWordGlossesToFreeTrans tests

		/// <summary>
		/// Tests the OnAddWordGlossesToFreeTrans method for the simple case of plain text.
		/// </summary>
		[Test]
		public void OnAddWordGlossesToFreeTrans_Simple()
		{
			var seg = m_para0_0.SegmentsOS[0];
			SetUpMocksForOnAddWordGlossesToFreeTransTest(seg);
			SetUpGlosses(seg, "hope", "this", "works");

			m_interlinDoc.OnAddWordGlossesToFreeTrans_TESTS_ONLY();

			AssertEx.AreTsStringsEqual(TsStringUtils.MakeString("hope this works.", Cache.DefaultAnalWs), seg.FreeTranslation.AnalysisDefaultWritingSystem);
		}

		/// <summary>
		/// Tests the OnAddWordGlossesToFreeTrans method for the simple case of plain text.
		/// </summary>
		[Test]
		public void OnAddWordGlossesToFreeTrans_ORCs()
		{
			var seg = m_para0_0.SegmentsOS[0];
			var strBldr = m_para0_0.Contents.GetBldr();
			var footnoteGuid = Guid.NewGuid();
			TsStringUtils.InsertOrcIntoPara(footnoteGuid, FwObjDataTypes.kodtOwnNameGuidHot, strBldr, 7, 7, Cache.DefaultVernWs);
			UndoableUnitOfWorkHelper.Do("undo Add ORC", "redo Add ORC", Cache.ActionHandlerAccessor, () =>
			{
				m_para0_0.Contents = strBldr.GetString();
			});

			SetUpMocksForOnAddWordGlossesToFreeTransTest(seg);
			SetUpGlosses(seg, "hope", null, "this", "works");

			m_interlinDoc.OnAddWordGlossesToFreeTrans_TESTS_ONLY();

			strBldr.Clear();
			strBldr.Replace(0, 0, "hope this works.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			TsStringUtils.InsertOrcIntoPara(footnoteGuid, FwObjDataTypes.kodtNameGuidHot, strBldr, 4, 4, Cache.DefaultAnalWs);

			AssertEx.AreTsStringsEqual(strBldr.GetString(), seg.FreeTranslation.AnalysisDefaultWritingSystem);
		}
		#endregion

		#region Helper methods
		private void SetUpMocksForOnAddWordGlossesToFreeTransTest(ISegment seg)
		{
			var rootb = MockRepository.GenerateMock<IVwRootBox>();
			m_interlinDoc.MockedRootBox = rootb;
			var vwsel = MockRepository.GenerateMock<IVwSelection>();
			rootb.Stub(x => x.Selection).Return(vwsel);
			rootb.Stub(x => x.DataAccess).Return(Cache.DomainDataByFlid);
			vwsel.Stub(x => x.TextSelInfo(Arg<bool>.Is.Equal(false), out Arg<ITsString>.Out(null).Dummy, out Arg<int>.Out(0).Dummy, out Arg<bool>.Out(false).Dummy, out Arg<int>.Out(seg.Hvo).Dummy,
				out Arg<int>.Out(SimpleRootSite.kTagUserPrompt).Dummy, out Arg<int>.Out(Cache.DefaultAnalWs).Dummy));
			vwsel.Stub(x => x.IsValid).Return(true);
			vwsel.Stub(x => x.CLevels(Arg<bool>.Is.Anything)).Return(0);
			vwsel.Stub(x => x.AllSelEndInfo(Arg<bool>.Is.Anything, out Arg<int>.Out(0).Dummy, Arg<int>.Is.Equal(0), Arg<ArrayPtr>.Is.Null, out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy,
				out Arg<int>.Out(0).Dummy, out Arg<bool>.Out(true).Dummy, out Arg<ITsTextProps>.Out(null).Dummy));
			m_interlinDoc.CallSetActiveFreeform(seg.Hvo, Cache.DefaultAnalWs);
		}

		private void SetUpGlosses(ISegment seg, params string[] glosses)
		{
			var servloc = Cache.ServiceLocator;
			var analFactory = servloc.GetInstance<IWfiAnalysisFactory>();
			var glossFactory = servloc.GetInstance<IWfiGlossFactory>();
			UndoableUnitOfWorkHelper.Do("Undo add glosses", "Redo add glosses", Cache.ActionHandlerAccessor, () =>
			{
				for (var i = 0; i < glosses.Length; i++)
				{
					if (glosses[i] == null)
					{
						continue;
					}
					var wfiWordform = (IWfiWordform)seg.AnalysesRS[i];
					var analysis = analFactory.Create(wfiWordform, glossFactory);
					var gloss = analysis.MeaningsOC.First();
					seg.AnalysesRS[i] = gloss;
					gloss.Form.SetAnalysisDefaultWritingSystem(glosses[i]);
				}
			});
		}
		#endregion
	}
}