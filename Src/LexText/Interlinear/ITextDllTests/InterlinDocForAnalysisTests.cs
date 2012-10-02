using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using XCore;
using SIL.CoreImpl;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// todo: Probably should move these into FocusBoxControllerTests.cs
	/// </summary>
	[TestFixture]
	public class FocusBoxControllerTests : MemoryOnlyBackendProviderBasicTestBase
	{
		FDO.IText m_text0;
		private IStText m_stText0;
		private IStTxtPara m_para0_0;
		//private TestableInterlinDocForAnalyis m_interlinDoc;
		private TestableFocusBox m_focusBox;
		private MockInterlinDocForAnalyis m_interlinDoc;
		private IList<AnalysisTree> m_analysis_para0_0 = new List<AnalysisTree>();

		/// <summary>
		///
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			// setup default vernacular ws.
			IWritingSystem wsXkal = Cache.ServiceLocator.WritingSystemManager.Set("qaa-x-kal");
			wsXkal.DefaultFontName = "Times New Roman";
			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(wsXkal);
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Insert(0, wsXkal);
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_text0 = textFactory.Create();
			Cache.LangProject.TextsOC.Add(m_text0);
			m_stText0 = stTextFactory.Create();
			m_text0.ContentsOA = m_stText0;
			m_para0_0 = m_stText0.AddNewTextPara(null);
			m_para0_0.Contents = TsStringUtils.MakeTss("Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.", wsXkal.Handle);

			InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(m_stText0, false);
			// paragraph 0_0 simply has wordforms as analyses
			foreach (var occurence in SegmentServices.GetAnalysisOccurrences(m_para0_0))
				if (occurence.HasWordform)
					m_analysis_para0_0.Add(new AnalysisTree(occurence.Analysis));

		}

		public override void TestSetup()
		{
			base.TestSetup();

			m_interlinDoc = new MockInterlinDocForAnalyis(m_stText0);
			m_focusBox = m_interlinDoc.FocusBox as TestableFocusBox;
		}

		public override void TestTearDown()
		{
			while (Cache.ActionHandlerAccessor.CanUndo())
				Cache.ActionHandlerAccessor.Undo();

			m_interlinDoc.Dispose();
			base.TestTearDown();
		}

		/// <summary>
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// </summary>
		[Test]
		public void ApproveAndStayPut_NoChange()
		{
			var ocurrences = SegmentServices.GetAnalysisOccurrences(m_para0_0).ToList();
			m_interlinDoc.SelectOccurrence(ocurrences[0]);
			var initialAnalysisObj = m_focusBox.InitialAnalysis.Analysis;
			// approve same wordform. Should not result in change during approve.
			m_focusBox.NewAnalysisTree.Analysis = ocurrences[0].Analysis;
			var undoRedoText = new MockUndoRedoText("Undo", "Redo");
			m_focusBox.ApproveAndStayPut(undoRedoText);

			// expect no change to the first occurrence.
			Assert.AreEqual(initialAnalysisObj, ocurrences[0].Analysis);
			// expect the focus box to still be on the first occurrence.
			Assert.AreEqual(ocurrences[0], m_focusBox.SelectedOccurrence);

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
			m_focusBox.DoDuringUnitOfWork = () =>
				WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(initialAnalysisTree.Wordform);
			var undoRedoText = new MockUndoRedoText("Undo", "Redo");
			m_focusBox.ApproveAndStayPut(undoRedoText);

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
			var undoRedoText = new MockUndoRedoText("Undo", "Redo");
			m_focusBox.ApproveAndMoveNext(undoRedoText);

			// expect no change to the first occurrence.
			Assert.AreEqual(initialAnalysisTree.Analysis, occurrences[0].Analysis);
			// expect the focus box to be on the next occurrence.
			Assert.AreEqual(occurrences[1], m_focusBox.SelectedOccurrence);

			// nothing to undo.
			Assert.AreEqual(0, Cache.ActionHandlerAccessor.UndoableSequenceCount);
		}

		/// <summary>
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// </summary>
		[Test]
		public void ApproveAndMoveNext_NewWordGloss()
		{
#if WANTPORTWFIC // Undo is not working. The part of the test up to Undo works, but leaves extra things on the Undo stack
			// that mess things up when this test is run first (NUnit) though not when run last (Resharper).
			var xfics = AnnotationServices.GetAnalysisOccurrences(m_para0_0).ToList();
			m_interlinDoc.SelectAnnotation(xfics[0]);
			var initialAnalysisTree_wfic0 = m_focusBox.InitialAnalysis;
			var newAnalysisTree_wfic0 = m_focusBox.NewAnalysisTree;
			m_focusBox.DoDuringUnitOfWork = () =>
				WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(initialAnalysisTree_wfic0.Wordform);
			var undoRedoText = new MockUndoRedoText("Undo", "Redo");
			m_focusBox.ApproveAndMoveNext(undoRedoText);

			// expect change to the first wfic.
			Assert.AreEqual(newAnalysisTree_wfic0.Gloss, xfics[0].Analysis);
			// expect the focus box to be on the next wfic.
			Assert.AreEqual(xfics[1], m_focusBox.SelectedWfic);

			// test undo.
			Assert.AreEqual(1, Cache.ActionHandlerAccessor.UndoableSequenceCount);
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(initialAnalysisTree_wfic0.Object, xfics[0].Analysis);
			// expect the focus box to be back on the first wfic.
			Assert.AreEqual(xfics[0], m_focusBox.SelectedWfic);
#endif
		}

		#region OnAddWordGlossesToFreeTrans tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the OnAddWordGlossesToFreeTrans method for the simple case of plain text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnAddWordGlossesToFreeTrans_Simple()
		{
			ISegment seg = m_para0_0.SegmentsOS[0];
			SetUpMocksForOnAddWordGlossesToFreeTransTest(seg);
			SetUpGlosses(seg, "hope", "this", "works");

			m_interlinDoc.OnAddWordGlossesToFreeTrans(null);

			AssertEx.AreTsStringsEqual(TsStringUtils.MakeTss("hope this works.", Cache.DefaultAnalWs),
				seg.FreeTranslation.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the OnAddWordGlossesToFreeTrans method for the simple case of plain text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnAddWordGlossesToFreeTrans_ORCs()
		{
			ISegment seg = m_para0_0.SegmentsOS[0];
			ITsStrBldr strBldr = m_para0_0.Contents.GetBldr();
			Guid footnoteGuid = Guid.NewGuid();
			TsStringUtils.InsertOrcIntoPara(footnoteGuid, FwObjDataTypes.kodtOwnNameGuidHot,
				strBldr, 7, 7, Cache.DefaultVernWs);
			UndoableUnitOfWorkHelper.Do("undo Add ORC", "redo Add ORC", Cache.ActionHandlerAccessor,
				() =>
				{
					m_para0_0.Contents = strBldr.GetString();
				});

			SetUpMocksForOnAddWordGlossesToFreeTransTest(seg);
			SetUpGlosses(seg, "hope", null, "this", "works");

			m_interlinDoc.OnAddWordGlossesToFreeTrans(null);

			strBldr.Clear();
			strBldr.Replace(0, 0, "hope this works.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			TsStringUtils.InsertOrcIntoPara(footnoteGuid, FwObjDataTypes.kodtNameGuidHot,
				strBldr, 4, 4, Cache.DefaultAnalWs);

			AssertEx.AreTsStringsEqual(strBldr.GetString(), seg.FreeTranslation.AnalysisDefaultWritingSystem);
		}
		#endregion

		#region Helper methods
		private void SetUpMocksForOnAddWordGlossesToFreeTransTest(ISegment seg)
		{
			IVwRootBox rootb = MockRepository.GenerateMock<IVwRootBox>();
			m_interlinDoc.MockedRootBox = rootb;
			IVwSelection vwsel = MockRepository.GenerateMock<IVwSelection>();
			rootb.Stub(x => x.Selection).Return(vwsel);
			rootb.Stub(x => x.DataAccess).Return(Cache.DomainDataByFlid);
			vwsel.Stub(x => x.TextSelInfo(Arg<bool>.Is.Equal(false), out Arg<ITsString>.Out(null).Dummy,
				out Arg<int>.Out(0).Dummy, out Arg<bool>.Out(false).Dummy, out Arg<int>.Out(seg.Hvo).Dummy,
				out Arg<int>.Out(SimpleRootSite.kTagUserPrompt).Dummy, out Arg<int>.Out(Cache.DefaultAnalWs).Dummy));
			vwsel.Stub(x => x.IsValid).Return(true);
			vwsel.Stub(x => x.CLevels(Arg<bool>.Is.Anything)).Return(0);
			vwsel.Stub(x => x.AllSelEndInfo(Arg<bool>.Is.Anything, out Arg<int>.Out(0).Dummy, Arg<int>.Is.Equal(0),
				Arg<ArrayPtr>.Is.Null, out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy,
				out Arg<int>.Out(0).Dummy, out Arg<bool>.Out(true).Dummy, out Arg<ITsTextProps>.Out(null).Dummy));
			m_interlinDoc.CallSetActiveFreeform(seg.Hvo, Cache.DefaultAnalWs);
		}

		private void SetUpGlosses(ISegment seg, params string[] glosses)
		{
			var servloc = Cache.ServiceLocator;
			IWfiAnalysisFactory analFactory = servloc.GetInstance<IWfiAnalysisFactory>();
			IWfiGlossFactory glossFactory = servloc.GetInstance<IWfiGlossFactory>();
			UndoableUnitOfWorkHelper.Do("Undo add glosses", "Redo add glosses", Cache.ActionHandlerAccessor,
				() =>
				{
					for (int i = 0; i < glosses.Length; i++)
					{
						if (glosses[i] == null)
							continue;
						IWfiWordform wfiWordform = (IWfiWordform)seg.AnalysesRS[i];
						IWfiAnalysis analysis = analFactory.Create(wfiWordform, glossFactory);
						IWfiGloss gloss = analysis.MeaningsOC.First();
						seg.AnalysesRS[i] = gloss;
						gloss.Form.SetAnalysisDefaultWritingSystem(glosses[i]);
					}
				});
		}
		#endregion
	}

	class MockInterlinDocForAnalyis : InterlinDocForAnalysis
	{
		private IStText m_testText;
		internal MockInterlinDocForAnalyis(IStText testText)
		{
			Cache = testText.Cache;
			m_hvoRoot = testText.Hvo;
			m_testText = testText;
			m_vc = new InterlinVc(Cache);
			m_vc.RootSite = this;
		}

		protected override FocusBoxController CreateFocusBoxInternal()
		{
			return new TestableFocusBox();
		}

		public override void SelectOccurrence(AnalysisOccurrence target)
		{
			InstallFocusBox();
			FocusBox.SelectOccurrence(target);
		}

		internal override void UpdateGuesses(HashSet<IWfiWordform> wordforms)
		{
			// for now, don't update guesses in these tests.
		}

		internal IVwRootBox MockedRootBox
		{
			set { m_rootb = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For testing purposes, we want to pretend to have focus all the time.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Focused
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls SetActiveFreeform on the view constructor to simulate having an empty free
		/// translation line selected (with the "Press Enter..." prompt).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void CallSetActiveFreeform(int hvoSeg, int ws)
		{
			ReflectionHelper.CallMethod(m_vc, "SetActiveFreeform", hvoSeg,
				SegmentTags.kflidFreeTranslation, ws, 0);
		}
	}

	class TestableFocusBox : FocusBoxController
	{
		internal TestableFocusBox()
		{
			m_mediator = new Mediator();
		}

		internal override IAnalysisControlInternal CreateNewSandbox(AnalysisOccurrence selected)
		{
			var sandbox = new MockSandbox();
			sandbox.CurrentAnalysisTree.Analysis = selected.Analysis;
			sandbox.NewAnalysisTree.Analysis = selected.Analysis;
			return sandbox;
		}

		public override void SelectOccurrence(AnalysisOccurrence selected)
		{
			// when we change wordforms we should create a new Analysis Tree, so we don't
			// overwrite the last state of one we may have saved during the tests.
			if (m_sandbox != null && selected != SelectedOccurrence)
				(m_sandbox as MockSandbox).NewAnalysisTree = new AnalysisTree();
			base.SelectOccurrence(selected);
		}

		/// <summary>
		/// Use to establish a new analysis to be approved.
		/// </summary>
		internal delegate AnalysisTree CreateNewAnalysis();

		internal CreateNewAnalysis DoDuringUnitOfWork { get; set; }

		protected override bool ShouldCreateAnalysisFromSandbox(bool fSaveGuess)
		{
			if (DoDuringUnitOfWork != null)
				return true;
			return base.ShouldCreateAnalysisFromSandbox(fSaveGuess);
		}

		protected override void ApproveAnalysis(bool fSaveGuess)
		{
			if (DoDuringUnitOfWork != null)
				NewAnalysisTree.Analysis = DoDuringUnitOfWork().Analysis;
			base.ApproveAnalysis(fSaveGuess);
		}

		internal AnalysisTree NewAnalysisTree
		{
			get { return (m_sandbox as MockSandbox).NewAnalysisTree; }
		}
	}

	internal class MockUndoRedoText : ICommandUndoRedoText
	{
		internal MockUndoRedoText(string undo, string redo)
		{
			RedoText = redo;
			UndoText = undo;
		}

		#region ICommandUndoRedoText Members

		public string RedoText
		{
			get;
			set;
		}

		public string UndoText
		{
			get;
			set;
		}

		#endregion
	}

	class MockSandbox : UserControl, IAnalysisControlInternal
	{
		internal MockSandbox()
		{
			CurrentAnalysisTree = new AnalysisTree();
			NewAnalysisTree = new AnalysisTree();
		}

		#region IAnalysisControlInternal Members

		bool IAnalysisControlInternal.HasChanged
		{
			get { return CurrentAnalysisTree.Analysis != NewAnalysisTree.Analysis; }
		}

		void IAnalysisControlInternal.MakeDefaultSelection()
		{
		}

		bool IAnalysisControlInternal.RightToLeftWritingSystem
		{
			get { return false; }
		}

		void IAnalysisControlInternal.SwitchWord(AnalysisOccurrence selected)
		{
			CurrentAnalysisTree.Analysis = selected.Analysis;
			NewAnalysisTree.Analysis = selected.Analysis;
		}

		internal AnalysisTree CurrentAnalysisTree { get; set; }
		internal AnalysisTree NewAnalysisTree { get; set; }

		bool IAnalysisControlInternal.ShouldSave(bool fSaveGuess)
		{
			return (this as IAnalysisControlInternal).HasChanged;
		}

		void IAnalysisControlInternal.Undo()
		{
		}

		#endregion

		AnalysisTree IAnalysisControlInternal.GetRealAnalysis(bool fSaveGuess, out IWfiAnalysis obsoleteAna)
		{
			obsoleteAna = null;
			return NewAnalysisTree;
		}

		public int GetLineOfCurrentSelection()
		{
			throw new NotImplementedException();
		}

		public bool SelectOnOrBeyondLine(int startLine, int increment)
		{
			throw new NotImplementedException();
		}

		public void UpdateLineChoices(InterlinLineChoices choices)
		{
			throw new NotImplementedException();
		}

		public int MultipleAnalysisColor
		{
			set { ; }
		}
	}
}
