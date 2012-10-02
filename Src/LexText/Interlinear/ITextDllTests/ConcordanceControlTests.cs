using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Scripture;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.XWorks;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	internal class MockConcordanceControl : ConcordanceControl
	{
		internal void SelectLineOption(ConcordanceLines lineOption)
		{
			SetConcordanceLine(lineOption);
		}

		internal string SearchText
		{
			get { return m_tbSearchText.Text; }
			set { m_tbSearchText.Text = value; }
		}

		internal string WritingSystem
		{
			get { return m_cbWritingSystem.Text;  }
			set
			{
				// make sure we're sync'd to the latest writing systems
				// before setting a value.
				SyncWritingSystemComboToSelectedLine();
				m_cbWritingSystem.Text = value;
			}
		}

		internal bool MatchCase
		{
			get { return m_chkMatchCase.Checked; }
			set { m_chkMatchCase.Checked = value; }
		}

		/// <summary>
		/// count how many times we've cleared our list
		/// </summary>
		int m_cClearMatches = 0;
		protected internal override void LoadMatches(bool fLoadVirtualProperty)
		{
			if (!fLoadVirtualProperty)
				m_cClearMatches++;
			base.LoadMatches(fLoadVirtualProperty);
		}

		/// <summary>
		/// count how many times we've actually searched for new results.
		/// </summary>
		int m_cSearchForMatches = 0;
		protected internal override List<int> SearchForMatches()
		{
			m_cSearchForMatches++;
			return base.SearchForMatches();
		}

		internal List<int> Search()
		{
			this.LoadMatches(true);
			return Results();
		}

		internal List<int> Results()
		{
			// modelclass="WordformInventory" virtualfield="MatchingConcordanceItems"
			int vtagMatchingConcordanceItems = BaseVirtualHandler.GetInstalledHandlerTag(m_cache,
				"WordformInventory", "MatchingConcordanceItems");
			int[] hvoAnnotationResults = m_cache.GetVectorProperty(m_cache.LangProject.WordformInventoryOAHvo,
				vtagMatchingConcordanceItems, true);
			return new List<int>(hvoAnnotationResults);
		}

		internal OccurrencesOfSelectedUnit Clerk
		{
			get { return base.m_clerk; }
		}

		internal int SearchForMatchesCount
		{
			get { return m_cSearchForMatches; }
		}

		internal int ClearMatchesCount
		{
			get { return m_cClearMatches; }
		}

		internal void ResetCounters()
		{
			m_cSearchForMatches = 0;
			m_cClearMatches = 0;
		}
	}

	public class InterlinearFwXWindowTestBase : InterlinearTestBase
	{
		protected MockFwXWindow m_window = null;
		protected bool m_fSkipInitializeNewWindow = false;
		protected int m_setupUndoCount = 0;

		protected ScrBook m_newBook1;
		protected FDO.IText m_text1;
		protected IWfiWordform m_wfXXXXsecrecyZZZ = null;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();
			InitializeNewWindow();
			// we don't want to initialize the window again after FixtureSetup.
			m_fSkipInitializeNewWindow = true;
			// Force IsTEInstalled for testing.
			MiscUtils.IsTEInstalled = true;
			SetupTexts();
		}

		void SetupTexts()
		{
			// First make a regular text.
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - SetupTexts()", "ConcordanceControlTests - SetupTexts()"))
			{
				m_text1 = Cache.LangProject.TextsOC.Add(new Text());
				m_text1.ContentsOA = new StText();
				StTxtPara para0 = new StTxtPara();
				StTxtPara para1 = new StTxtPara();
				m_text1.ContentsOA.ParagraphsOS.Append(para0);
				m_text1.ContentsOA.ParagraphsOS.Append(para1);
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				//           1         2         3         4         5         6
				// 0123456789012345678901234567890123456789012345678901234567890123456789
				// XXXXsecrecyZZZ; XXXsentenceZZZ!!
				// XXXlocoZZZ, XXXsegmentZZZ?? ZZZamazingXXX wonderfulXXXzzzcounselor!!
				para0.Contents.UnderlyingTsString = tsf.MakeString("XXXXsecrecyZZZ; XXXsentenceZZZ!!", Cache.DefaultVernWs);
				para1.Contents.UnderlyingTsString = tsf.MakeString("XXXlocoZZZ, XXXsegmentZZZ?? ZZZamazingXXX wonderfulXXXzzzcounselor!!", Cache.DefaultVernWs);

				// add scripture
				m_newBook1 = new ScrBook();
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.Append(m_newBook1);
				m_newBook1.TitleOA = new StText();
				m_newBook1.TitleOA.ParagraphsOS.Append(new StTxtPara());
				(m_newBook1.TitleOA.ParagraphsOS[0] as StTxtPara).Contents.UnderlyingTsString = tsf.MakeString("XXXnewBook1zzz.Title", Cache.DefaultVernWs);
				IScrSection newSection1_0 = m_newBook1.SectionsOS.Append(new ScrSection());
				newSection1_0.ContentOA = new StText();
				StTxtPara paraSection1_0 = new StTxtPara();
				(newSection1_0.ContentOA as StText).ParagraphsOS.Append(paraSection1_0);
				paraSection1_0.Contents.UnderlyingTsString = tsf.MakeString("ZZZnewBook1.Section0.Introduction1XXX", Cache.DefaultVernWs);
				IScrSection newSection1_1 = m_newBook1.SectionsOS.Append(new ScrSection());
				newSection1_1.ContentOA = new StText();
				StTxtPara paraSection1_1 = new StTxtPara();
				(newSection1_1.ContentOA as StText).ParagraphsOS.Append(paraSection1_1);
				paraSection1_1.Contents.UnderlyingTsString = tsf.MakeString("XXXnewBook1.Section1.1:1-1:20ZZZ", Cache.DefaultVernWs);
				// section.VerseRefEnd = book.CanonicalNum * 1000000 + 1 * 1000 + (introSection ? 0 : 1);
				newSection1_1.VerseRefEnd = 70 * 1000000 + 1 * 1000 + 1;


				// setup some basic analyses for the texts.
				string formLexEntry = "XXXlexEntry1";
				ITsString tssLexEntryForm = StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
				int clsidForm;
				StringUtils.ReassignTss(ref tssLexEntryForm, StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs));
				ILexEntry lexEntry1_Entry = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"XXXlexEntry1.sense1", null);
				ILexSense lexEntry1_Sense1 = lexEntry1_Entry.SensesOS[0];
				ILexSense lexEntry1_Sense2 = LexSense.CreateSense(lexEntry1_Entry, null, "XXXlexEntry1.sense2");
				ParagraphAnnotator tapara0 = new ParagraphAnnotator(para0);
				ParagraphAnnotator tapara1 = new ParagraphAnnotator(para1);
				ParagraphAnnotator taSection1_0 = new ParagraphAnnotator(paraSection1_0);
				ParagraphAnnotator taSection1_1 = new ParagraphAnnotator(paraSection1_1);

				// currently setup mono-morphemic search
				ArrayList morphForms = new ArrayList();
				formLexEntry = "XXXXsecrecyZZZ";
				StringUtils.ReassignTss(ref tssLexEntryForm, StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs));
				ILexEntry lexEntry2_Entry = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"XXXlexEntry2.sense1", null);
				morphForms.Add(lexEntry2_Entry.LexemeFormOA);
				IWfiAnalysis wfiAnalysis = tapara0.BreakIntoMorphs(0, 0, morphForms);
				m_wfXXXXsecrecyZZZ = (wfiAnalysis as WfiAnalysis).Owner as IWfiWordform;
				ILexSense lexEntry2_Sense1 = lexEntry2_Entry.SensesOS[0];
				tapara0.SetMorphSense(0, 0, 0, lexEntry2_Sense1);

				formLexEntry = "XXXsegmentZZZ";
				StringUtils.ReassignTss(ref tssLexEntryForm, StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs));
				ILexEntry lexEntry3_Entry = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"XXXlexEntry3.sense1", null);
				morphForms[0] = lexEntry3_Entry.LexemeFormOA;
				tapara1.BreakIntoMorphs(0, 2, morphForms);
				ILexSense lexEntry3_Sense1 = lexEntry3_Entry.SensesOS[0];
				tapara1.SetMorphSense(0, 2, 0, lexEntry3_Sense1);

				formLexEntry = "ZZZamazingXXX";
				StringUtils.ReassignTss(ref tssLexEntryForm, StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs));
				ILexEntry lexEntry4_Entry = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"XXXlexEntry4.sense1", null);
				morphForms[0] = lexEntry4_Entry.LexemeFormOA;
				tapara1.BreakIntoMorphs(1, 0, morphForms);
				ILexSense lexEntry4_Sense1 = lexEntry4_Entry.SensesOS[0];
				tapara1.SetMorphSense(1, 0, 0, lexEntry4_Sense1);

				//XXXlocoZZZ
				morphForms[0] = "XXXlocoZZZ";
				tapara1.BreakIntoMorphs(0, 0, morphForms);
				tapara1.SetMorphSense(0, 0, 0, lexEntry1_Sense2);

				formLexEntry = paraSection1_0.Contents.Text;
				StringUtils.ReassignTss(ref tssLexEntryForm, StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs));
				ILexEntry lexEntry5_Entry = LexEntry.CreateEntry(Cache,
					MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
					"XXXlexEntry5.sense1", null);
				morphForms[0] = lexEntry5_Entry.LexemeFormOA;
				taSection1_0.BreakIntoMorphs(0, 0, morphForms);
				ILexSense lexEntry5_Sense1 = lexEntry5_Entry.SensesOS[0];
				taSection1_0.SetMorphSense(0, 0, 0, lexEntry5_Sense1);

				morphForms[0] = paraSection1_1.Contents.Text;
				taSection1_1.BreakIntoMorphs(0, 0, morphForms);		// won't match on LexEntry
				taSection1_1.SetMorphSense(0, 0, 0, lexEntry1_Sense2);	// will match on LexGloss

				string gloss;
				tapara0.SetDefaultWordGloss(0, 0, out gloss);
				tapara1.SetDefaultWordGloss(0, 2, out gloss);
				tapara1.SetDefaultWordGloss(1, 0, out gloss);
				taSection1_1.SetDefaultWordGloss(0, 0, out gloss);

				StTxtPara.TwficInfo infoCba0_0_0 = new StTxtPara.TwficInfo(Cache, tapara0.GetSegmentForm(0, 0));
				StTxtPara.TwficInfo infoCba1_0_2 = new StTxtPara.TwficInfo(Cache, tapara1.GetSegmentForm(0, 2));
				StTxtPara.TwficInfo infoCbaScr_0_0 = new StTxtPara.TwficInfo(Cache, taSection1_1.GetSegmentForm(0, 0));

				int segDefn_literalTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
				int segDefn_freeTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
				int segDefn_note = Cache.GetIdFromGuid(LangProject.kguidAnnNote);
				BaseFreeformAdder ffAdder = new BaseFreeformAdder(Cache);
				ICmIndirectAnnotation freeTrans0 = ffAdder.AddFreeformAnnotation(infoCba0_0_0.SegmentHvo, segDefn_freeTranslation);
				freeTrans0.Comment.SetAlternative("Para0.Segment0: XXXFreeform translation.", Cache.DefaultAnalWs);
				ICmIndirectAnnotation literalTrans0 = ffAdder.AddFreeformAnnotation(infoCba0_0_0.SegmentHvo, segDefn_literalTranslation);
				literalTrans0.Comment.SetAlternative("Para0.Segment0: XXXLiteral translation.", Cache.DefaultAnalWs);
				ICmIndirectAnnotation note0 = ffAdder.AddFreeformAnnotation(infoCba0_0_0.SegmentHvo, segDefn_note);
				note0.Comment.SetAlternative("Para0.Segment0: XXXNote.", Cache.DefaultAnalWs);

				ICmIndirectAnnotation freeTrans1 = ffAdder.AddFreeformAnnotation(infoCba1_0_2.SegmentHvo, segDefn_freeTranslation);
				freeTrans1.Comment.SetAlternative("Para1.Segment0: XXXFreeform translation.", Cache.DefaultAnalWs);
				ICmIndirectAnnotation literalTrans1 = ffAdder.AddFreeformAnnotation(infoCba1_0_2.SegmentHvo, segDefn_literalTranslation);
				literalTrans1.Comment.SetAlternative("Para1.Segment0: XXXLiteral translation.", Cache.DefaultAnalWs);
				ICmIndirectAnnotation note1 = ffAdder.AddFreeformAnnotation(infoCba1_0_2.SegmentHvo, segDefn_note);
				note1.Comment.SetAlternative("Para1.Segment0: XXXNote.", Cache.DefaultAnalWs);

				// Scripture
				ICmIndirectAnnotation freeTransScr1 = ffAdder.AddFreeformAnnotation(infoCbaScr_0_0.SegmentHvo, segDefn_freeTranslation);
				freeTransScr1.Comment.SetAlternative("Scr1.Para0.Segment0: XXXFreeform translation.", Cache.DefaultAnalWs);
				ICmIndirectAnnotation literalTransScr1 = ffAdder.AddFreeformAnnotation(infoCbaScr_0_0.SegmentHvo, segDefn_literalTranslation);
				literalTransScr1.Comment.SetAlternative("Scr1.Para0.Segment0: XXXLiteral translation.", Cache.DefaultAnalWs);
				ICmIndirectAnnotation noteScr1 = ffAdder.AddFreeformAnnotation(infoCbaScr_0_0.SegmentHvo, segDefn_note);
				noteScr1.Comment.SetAlternative("Scr1.Para0.Segment0: XXXNote.", Cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// Note: Dispose() is the TestFixtureTearDown in BaseTest class.
		///
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Undo everything.
				UndoEverythingPossible();

				// Dispose managed resources here.
				if (m_window != null)
				{
					// delete property table settings.
					m_window.PropertyTable.RemoveLocalAndGlobalSettings();
					m_window.Dispose();
				}
				if (m_locallyOwnedMediator && m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_newBook1 = null;
			m_window = null;
			m_setupUndoCount = 0;

			base.Dispose(disposing);
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			// make sure we're setup to reparse the text.
			(m_text1.ContentsOA as StText).LastParsedTimestamp = 0;
			int initialUndoCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;
			// base.Initialize(); // don't UndoEverything.
			InitializeWindowAndToolControls();
			m_setupUndoCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;
		}

		/// <summary>
		/// We need to re-initialize our PropertyTable for each test.
		/// </summary>
		protected virtual void InitializeWindowAndToolControls()
		{
			if (!m_fSkipInitializeNewWindow)
				InitializeNewWindow();
			else
				m_fSkipInitializeNewWindow = false;
		}

		protected virtual void InitializeNewWindow()
		{
			m_window = new MockFwXWindow();
			m_window.Init(Cache);
			// delete property table settings.
			m_window.PropertyTable.RemoveLocalAndGlobalSettings();
			ReinitializeWindow();
			ControlAssemblyReplacements();
			m_window.LoadUI(DirectoryFinder.GetFWCodeFile(@"Language Explorer\Configuration\Main.xml"));
		}

		protected virtual void ControlAssemblyReplacements()
		{
		}

		private void ReinitializeWindow()
		{
			m_mediator = m_window.Mediator;
			InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." });
			m_window.InstalledVirtualHandlers = m_installedVirtualHandlers;
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			// Don't call base.Exit() since this will undo the data we set up in FixtureSetup
			//base.Exit();
			// delete property table settings.
			m_window.PropertyTable.RemoveLocalAndGlobalSettings();

			// reset our property table.
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
			}
			m_window = null;
			UndoLeftOverActions();
			m_mediator = null;
		}

		void UndoLeftOverActions()
		{
			// We don't want anything to try to take prop changes during a tear down.
			// NOTE: IgnorePropChanged doesn't seem to work in Undo, so dispose the main window first.
			using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressAll))
			{
				if (Cache.ActionHandlerAccessor.UndoableSequenceCount > m_setupUndoCount)
				{
					UndoResult ures;
					Debug.WriteLine(String.Format("Expected our undo stack count {0} to match the Setup value {1}.",
						Cache.ActionHandlerAccessor.UndoableSequenceCount, m_setupUndoCount));
					bool fNeedRefresh = false;
					while (Cache.CanUndo && Cache.ActionHandlerAccessor.UndoableSequenceCount > m_setupUndoCount)
					{
						Debug.WriteLine(Cache.ActionHandlerAccessor.GetUndoText());
						Cache.Undo(out ures);
						if(ures == UndoResult.kuresRefresh)
							fNeedRefresh = true;
					}
					if (fNeedRefresh)
						Cache.ClearAllData();
				}
				if (Cache.ActionHandlerAccessor.UndoableSequenceCount < m_setupUndoCount)
				{
					Debug.Fail(String.Format("We undid more than we needed! We expected {0}, but we now have {1} undo actions.",
						m_setupUndoCount, Cache.ActionHandlerAccessor.GetUndoText()));
				}
				// If we don't handle PropChanges, we may need to invalidate our FormToIdTable due to resurrected/deleted wordforms
				// during undo.
				(Cache.LangProject.WordformInventoryOA as WordformInventory).ResetAllWordformOccurrences();
				// for some reason Cache.Undo doesn't seem to be reverting everything in the cache. :(
				//Cache.ClearAllData();
			}
		}

		protected virtual void MasterRefresh()
		{
			m_mediator.SendMessage("MasterRefresh", m_window);
			ReinitializeWindow();
			m_window.ProcessPendingItems();
		}


		internal class InterlinMasterHelper : IDisposable
		{
			InterlinMaster m_interlinMaster = null;
			InterlinearFwXWindowTestBase m_tests = null;
			internal InterlinMasterHelper(InterlinearFwXWindowTestBase tests)
			{
				m_tests = tests;
			}

			internal void DisposeControls()
			{
				CurrentInterlinMasterControl.Dispose();
			}

			internal void SwitchTab(InterlinMaster.TabPageSelection tabSelection)
			{
				InterlinMaster interlinMaster = CurrentInterlinMasterControl;
				TabControl interlinTabs = interlinMaster.Controls.Find("m_tabCtrl", false)[0] as TabControl;
				interlinTabs.SelectedIndex = (int)tabSelection;
			}

			internal InterlinMaster CurrentInterlinMasterControl
			{
				get
				{
					if (m_interlinMaster == null || m_interlinMaster.IsDisposed)
					{
						m_interlinMaster = CurrentToolWindow.FindControl("InterlinMaster") as InterlinMaster;
					}
					return m_interlinMaster;
				}
			}

			internal InterlinDocChild CurrentInterlinDoc
			{
				get { return CurrentInterlinMasterControl.Controls.Find("m_idcPane", true)[0] as InterlinDocChild; }
			}

			internal RawTextPane RawTextPane
			{
				get { return CurrentInterlinMasterControl.Controls.Find("m_rtPane", true)[0] as RawTextPane; }
			}

			/// <summary>
			/// Issues OnKeyDown in RawTextPane
			/// </summary>
			/// <param name="key"></param>
			internal void OnKeyDownAndKeyPress(Keys key)
			{
				RawTextPane rtp = RawTextPane;
				rtp.HandleKeyDownAndKeyPress(key);
				CurrentToolWindow.ProcessPendingItems();
			}

			/// <summary>
			/// Sets cursor (Insertion Point) in RawTextPane
			/// </summary>
			/// <param name="para"></param>
			/// <param name="ichMin"></param>
			internal void SetCursor(StTxtPara para, int ichMin)
			{
				RawTextPane rtp = RawTextPane;
				rtp.MakeTextSelectionAndScrollToView(ichMin, ichMin, 0, para.IndexInOwner);
			}

			/// <summary>
			/// Get selection info from RawTextPane
			/// </summary>
			internal TextSelInfo CurrentSelectionInfo
			{
				get
				{
					RawTextPane rtp = RawTextPane;
					return new TextSelInfo(rtp.RootBox.Selection);
				}
			}

			MockFwXWindow CurrentToolWindow
			{
				get { return m_tests.m_window; }
			}

			#region IDisposable Members

			public void Dispose()
			{
				DisposeControls();
			}

			#endregion
		}
	}

	/// <summary>
	/// </summary>
	[TestFixture]
	public class ConcordanceControlTests : InterlinearFwXWindowTestBase
	{
		MockConcordanceControl m_concordanceControl = null;
		XmlNode m_concordanceToolNode = null;
		//XmlNode m_configurationNode = null;
		InterlinearTextsVirtualHandler m_concordanceTextsVh;


		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// Note: Dispose() is the TestFixtureTearDown in BaseTest class.
		///
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_concordanceControl != null)
					m_concordanceControl.Dispose();
			}

			base.Dispose(disposing);

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_concordanceControl = null;
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			// clear any scripture id selections stored in the PropertyTable
			IVwVirtualHandler vh;
			if (Cache.TryGetVirtualHandler(LangProject.InterlinearTextsFlid(Cache), out vh))
			{
				InterlinearTextsVirtualHandler pvh = vh as InterlinearTextsVirtualHandler;
				pvh.UpdateList(new int[0]);
			}

			base.Initialize();
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			m_concordanceControl.Dispose();
			m_concordanceControl = null;
			m_concordanceTextsVh = null;
			base.Exit();
		}

		/// <summary>
		/// We need to re-initialize our PropertyTable for each test.
		/// </summary>
		protected override void InitializeWindowAndToolControls()
		{
			base.InitializeWindowAndToolControls();

			m_concordanceTextsVh = BaseVirtualHandler.GetInstalledHandler(m_fdoCache,
				"LangProject", "InterlinearTexts") as InterlinearTextsVirtualHandler;
			ClearScriptureFilter();
			SwitchToConcordanceTool();
		}

		protected override void MasterRefresh()
		{
			base.MasterRefresh();
			ReinitializeActiveConcordanceTool();
		}

		private void SwitchToConcordanceTool()
		{
			m_concordanceToolNode = m_window.ActivateTool("concordance");

			ReinitializeActiveConcordanceTool();
		}

		private void ReinitializeActiveConcordanceTool()
		{
			Control control = m_window.FindControl("concordanceControl");
			m_concordanceControl = control as MockConcordanceControl;
		}

		protected override void ControlAssemblyReplacements()
		{
			ControlAssemblyReplacement replacement = new ControlAssemblyReplacement();
			replacement.m_toolName = "concordance";
			replacement.m_controlName = "concordanceControl";
			replacement.m_targetAssembly = "ITextDll.dll";
			replacement.m_targetControlClass = "SIL.FieldWorks.IText.ConcordanceControl";
			replacement.m_newAssembly = "ITextDllTests.dll";
			replacement.m_newControlClass = "SIL.FieldWorks.IText.MockConcordanceControl";
			m_window.AddReplacement(replacement);
		}

		private void ClearScriptureFilter()
		{
			m_concordanceTextsVh.NeedToReloadSettings = true;
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[Ignore("For some reason we can't match on overlapping matches.")]
		public void GetOverlappingOccurrences()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kBaseline);
			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			// Search within words
			m_concordanceControl.SearchText = "XXX";	// should get all occurrences of "XXX" ("XXXX" = 2 occurrences).
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(7, results.Count);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void BaselineSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kBaseline);
			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			// Search within words
			// should get all occurrences of "XXX" ("XXXX" = 2 occurrences)
			// but there seems to be a bug within the pattern matching.
			m_concordanceControl.SearchText = "XXX";
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(6, results.Count);

			// Search for Punctuation
			m_concordanceControl.SearchText = "!";	// should get all occurrences of "!" ("!!" = 2 occurrences).
			results = m_concordanceControl.Search();
			Assert.AreEqual(4, results.Count);

			m_concordanceControl.SearchText = "!!";	// should get all occurrences of "!" ("!!" = 2 occurrences).
			results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);

			// Search for words across punctuation in a segment.
			m_concordanceControl.SearchText = "XXXlocoZZZ, XXXsegmentZZZ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(1, results.Count);

			// Search for words across segment boundaries.
			m_concordanceControl.SearchText = "XXXsegmentZZZ?? ZZZamazingXXX";
			results = m_concordanceControl.Search();
			Assert.AreEqual(1, results.Count);

			m_concordanceControl.SearchText = "YYY";	// no occurrences
			results = m_concordanceControl.Search();
			Assert.AreEqual(0, results.Count);

			// Try searching with Regular Expression.
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.UseRegExp;
			//           1         2         3         4         5         6
			// 0123456789012345678901234567890123456789012345678901234567890123456789
			// XXXXsecrecyZZZ; XXXsentenceZZZ!!
			// XXXlocoZZZ, XXXsegmentZZZ?? ZZZamazingXXX wonderfulXXXzzzcounselor!!

			// XXXXsecrecyZZZ; XXXsentenceZZZ
			// XXXlocoZZZ, XXXsegmentZZZ?? ZZZ
			m_concordanceControl.SearchText = @"XXX.+ZZZ";	//  should one span in each paragraph.
			results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);
			ICmBaseAnnotation cba0 = CmBaseAnnotation.CreateFromDBObject(Cache, results[0]);
			ICmBaseAnnotation cba1 = CmBaseAnnotation.CreateFromDBObject(Cache, results[1]);

			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[0].Hvo, cba0.BeginObjectRAHvo);
			Assert.AreEqual(0, cba0.BeginOffset);
			Assert.AreEqual(30, cba0.EndOffset);
			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[1].Hvo, cba1.BeginObjectRAHvo);
			Assert.AreEqual(0, cba1.BeginOffset);
			Assert.AreEqual(31, cba1.EndOffset);

			// XXXXsecrecyZZZ
			// XXXsentenceZZZ
			// XXXlocoZZZ
			// XXXsegmentZZZ
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.UseRegExp;
			m_concordanceControl.SearchText = @"XXX\w+ZZZ";	//
			results = m_concordanceControl.Search();
			Assert.AreEqual(4, results.Count);
			cba0 = CmBaseAnnotation.CreateFromDBObject(Cache, results[0]);
			cba1 = CmBaseAnnotation.CreateFromDBObject(Cache, results[1]);
			ICmBaseAnnotation cba2 = CmBaseAnnotation.CreateFromDBObject(Cache, results[2]);
			ICmBaseAnnotation cba3 = CmBaseAnnotation.CreateFromDBObject(Cache, results[3]);

			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[0].Hvo, cba0.BeginObjectRAHvo);
			Assert.AreEqual(0, cba0.BeginOffset);
			Assert.AreEqual(14, cba0.EndOffset);

			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[0].Hvo, cba1.BeginObjectRAHvo);
			Assert.AreEqual(16, cba1.BeginOffset);
			Assert.AreEqual(30, cba1.EndOffset);

			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[1].Hvo, cba2.BeginObjectRAHvo);
			Assert.AreEqual(0, cba2.BeginOffset);
			Assert.AreEqual(10, cba2.EndOffset);

			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[1].Hvo, cba3.BeginObjectRAHvo);
			Assert.AreEqual(12, cba3.BeginOffset);
			Assert.AreEqual(25, cba3.EndOffset);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void WordSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
			m_concordanceControl.WritingSystem = "Kalaba";
			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXX";	// should get all our twfics.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(6, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXXse";
			results = m_concordanceControl.Search();
			Assert.AreEqual(3, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.AtStart;
			m_concordanceControl.SearchText = "XXXse";
			results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "ZZZ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(5, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.AtEnd;
			m_concordanceControl.SearchText = "ZZZ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(4, results.Count);

			// Do a concordance in an alternate writing system
			// add an alternate word form to an existing wordform
			ILgWritingSystem lgWsGerman = AddToCurrentWritingSystems("de");
			m_wfXXXXsecrecyZZZ.Form.SetAlternative("QQQalternativeForm", lgWsGerman.Hvo);

			m_concordanceControl.WritingSystem = "German";
			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "QQQ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(1, results.Count);

			m_concordanceControl.WritingSystem = "Kalaba";
			m_concordanceControl.MatchCase = false;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "ZZZ";	// should get all our twfics.
			results = m_concordanceControl.Search();
			Assert.AreEqual(6, results.Count);

			// Add scripture to concordance.
			// see if we can get scripture ids
			IncludeAllScripture();
			// IncludeAllScripture() should result in a new results from the previous search but including scripture ids.
			//m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			//m_concordanceControl.SearchText = "ZZZ";	// should get all our twfics.
			//results = m_concordanceControl.Search();
			results = m_concordanceControl.Results();
			Assert.AreEqual(9, results.Count);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "ZZZ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(7, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.AtEnd;
			m_concordanceControl.SearchText = "ZZZ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(5, results.Count);

			m_concordanceControl.SearchText = "XXX";	// should get all our twfics, including scripture.
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			results = m_concordanceControl.Search();
			Assert.AreEqual(9, results.Count);
		}

		private ILgWritingSystem AddToCurrentWritingSystems(string icuLocale)
		{
			ILgWritingSystem wsVernAlt = null;
			foreach (LgWritingSystem ws in Cache.LanguageEncodings)
			{
				if (LanguageDefinition.SameLocale(ws.ICULocale, icuLocale))
				{
					wsVernAlt = ws;
					break;
				}
			}
			Cache.LangProject.CurVernWssRS.Append(wsVernAlt);
			return wsVernAlt;
		}

		/// <summary>
		/// This should also result in new search results based on the current state of the ConcordanceControl.
		/// </summary>
		private void IncludeAllScripture()
		{
			List<int> hvoScriptureIds = new List<int>(new int[]{m_newBook1.TitleOAHvo,
				m_newBook1.SectionsOS[0].ContentOAHvo, m_newBook1.SectionsOS[1].ContentOAHvo});
			m_concordanceTextsVh.UpdateList(hvoScriptureIds.ToArray());
		}

		[Test]
		public void MorphemesSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kMorphemes);

			m_concordanceControl.MatchCase = false;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXXS";
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXXS";
			results = m_concordanceControl.Search();
			Assert.AreEqual(0, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXX";	// should get all twfics with morpheme analyses.
			results = m_concordanceControl.Search();
			Assert.AreEqual(4, results.Count);

			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.AtEnd;
			m_concordanceControl.SearchText = "ZZZ";
			results = m_concordanceControl.Search();
			Assert.AreEqual(3, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			//m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.AtEnd;
			//m_concordanceControl.SearchText = "ZZZ";
			//results = m_concordanceControl.Search();
			results = m_concordanceControl.Results();
			Assert.AreEqual(4, results.Count);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXX";	// should get all twfics with morpheme analyses.
			results = m_concordanceControl.Search();
			Assert.AreEqual(6, results.Count);


			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXXNEW";
			results = m_concordanceControl.Search();
			Assert.AreEqual(0, results.Count);

			m_concordanceControl.MatchCase = false;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "XXXNEW";
			results = m_concordanceControl.Search();
			Assert.AreEqual(1, results.Count);
		}

		[Test]
		public void LexEntrySearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kLexEntry);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXX";	// should get all twfics with matching lexEntry analyses.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(3, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			//m_concordanceControl.SearchText = "XXX";	// should get all twfics with matching lexEntry analyses.
			//results = m_concordanceControl.Search();
			results = m_concordanceControl.Results();
			Assert.AreEqual(4, results.Count);

		}

		[Test]
		public void LexGlossSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kLexGloss);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = ".sense";	// should get all twfics with matching lexGloss analyses.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(4, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			results = m_concordanceControl.Results();
			Assert.AreEqual(6, results.Count);

		}

		[Test]
		public void WordGlossSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWordGloss);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXX";	// should get all twfics with matching wordGloss analyses.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(3, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			results = m_concordanceControl.Results();
			Assert.AreEqual(4, results.Count);

		}

		[Test]
		public void FreeTranslationSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kFreeTranslation);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXXF";	// should get all segments with matching FreeTranlation comments.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			results = m_concordanceControl.Results();
			Assert.AreEqual(3, results.Count);

		}

		[Test]
		public void LiteralTranslationSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kLiteralTranslation);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXXL";	// should get all segments with matching Literal Translation comments.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			results = m_concordanceControl.Results();
			Assert.AreEqual(3, results.Count);
		}

		[Test]
		public void NoteSearch()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kNote);

			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXXN";	// should get all segments with matching Note comments.
			List<int> results = m_concordanceControl.Search();
			Assert.AreEqual(2, results.Count);

			// try the same tests with scripture included.
			IncludeAllScripture();
			results = m_concordanceControl.Results();
			Assert.AreEqual(3, results.Count);
		}

		/// <summary>
		/// LT-6741 documents the scenario where a new search results in adding one item to the old search (one other item).
		/// </summary>
		[Test]
		[Ignore("ConcordanceControl no longer tries to convert annotations or their wfiwordforms. We could try this on Word List Concordance")]
		public void LT6741_ReloadListWhenSearchResultsInOneItem()
		{
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
			m_concordanceControl.SearchText = "ZZZamazingXXX";	// should get one result.
			List<int> results = m_concordanceControl.Search(); // this should parse the paragraphs creating dummy twfics.
			m_window.ProcessPendingItems();
			Assert.AreEqual(1, results.Count);

			// now do a search resulting in one item.
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - LT6741_ReloadListWhenSearchResultsInOneItem",
				"ConcordanceControlTests - LT6741_ReloadListWhenSearchResultsInOneItem"))
			{
				m_concordanceControl.SearchText = "wonderfulXXXzzzcounselor";	// should get one result.
				results = m_concordanceControl.Search(); // this should parse the paragraphs creating dummy twfics.
				Assert.AreEqual(1, results.Count);
				Assert.IsTrue(Cache.IsDummyObject(results[0]),
					"we're expecting the last annotation (to 'wonderfulXXXzzzcounselor') to be virtual until we select it.");
				// this should result in selecting the dummy value, which will get converted to real.
				m_window.ProcessPendingItems();
				results = m_concordanceControl.Results();
				Assert.IsTrue(!Cache.IsDummyObject(results[0]),
					"we're expecting the last annotation (to 'wonderfulXXXzzzcounselor') to become real because it is selected.");
				// now compare this result set against the number of items in our browse view.
				Assert.AreEqual(1, m_concordanceControl.Clerk.ListSize, "Our browse view results should match our propery list size");
			}
			Cache.Undo();
			m_concordanceControl.SearchText = "wonderfulXXXzzzcounselor";	// should get one result.
			results = m_concordanceControl.Search(); // this should parse the paragraphs creating dummy twfics.
			Assert.AreEqual(1, results.Count);
			Assert.IsTrue(Cache.IsDummyObject(results[0]),
				"we're expecting the last annotation (to 'wonderfulXXXzzzcounselor') to be virtual until we select it.");
		}

		/// <summary>
		/// Test consistency in conversion of dummy objects that can have references in multiple virtual properties.
		/// </summary>
		[Test]
		[Ignore("ConcordanceControl no longer tries to convert annotations or their wfiwordforms. We could try this on Word List Concordance")]
		public void ConvertDummyToReal_RecordListDependencies()
		{
			CheckDisposed();
			List<int> results;
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - ConvertDummyToReal_RecordListDependencies",
				"ConcordanceControlTests - ConvertDummyToReal_RecordListDependencies"))
			{
				// setup a record clerk/list based upon a virtual property that depends upon a virtual property
				// that contains some of the same items found in the clerk's list.
				m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
				m_concordanceControl.MatchCase = true;
				m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
				m_concordanceControl.SearchText = "XXX";	// should get all our twfics.
				results = m_concordanceControl.Search(); // this should parse the paragraphs creating dummy twfics.
				m_window.ProcessPendingItems();
				Assert.AreEqual(6, results.Count);

				// jump to last index and verify we have changed.
				int lastIndex = results.Count - 1;
				Assert.AreEqual(0, m_concordanceControl.Clerk.CurrentIndex);
				// make sure that the item we're jumping to is a virtual object
				Assert.IsTrue(Cache.IsDummyObject(results[lastIndex]),
					"we're expecting the last annotation (to 'wonderfulXXXzzzcounselor') to be virtual until we select it.");
				// get the owner/owning flid information for the results annotation pointint to a WfiWordform.
				CmBaseAnnotation cbaDummy = CmBaseAnnotation.CreateFromDBObject(Cache, results[lastIndex], false) as CmBaseAnnotation;
				// first make sure the annotation is a twfic
				Assert.AreEqual(CmAnnotationDefn.Twfic(Cache).Hvo, cbaDummy.AnnotationTypeRAHvo,
					"Expected the dummy cba to be a twfic.");
				Assert.AreEqual(WfiWordform.OccurrencesFlid(Cache), cbaDummy.OwningFlid,
					"Expected the dummy twfic to be owned by WfiWordform.OccurrencesFlid");
				WfiWordform dummyWfiWordform = WfiWordform.CreateFromDBObject(Cache, cbaDummy.InstanceOfRAHvo, false) as WfiWordform;
				List<int> occurrencesOrig = dummyWfiWordform.OccurrencesInTexts;
				// find our occurrence
				int iCbaDummy = occurrencesOrig.IndexOf(cbaDummy.Hvo);
				Assert.IsTrue(iCbaDummy >= 0, "Expected our dummy cba to be in our WfiWordform.Occurrences");

				// Convert the dummy twfic to a real one.
				CmBaseAnnotation cbaReal = CmObject.ConvertDummyToReal(Cache, cbaDummy.Hvo) as CmBaseAnnotation;

				// see if our wordform Occurrences got updated.
				WfiWordform wfiWordformReal = WfiWordform.CreateFromDBObject(Cache, cbaReal.InstanceOfRAHvo) as WfiWordform;
				List<int> occurrencesUpdated = wfiWordformReal.OccurrencesInTexts;
				Assert.AreEqual(occurrencesOrig.Count, occurrencesUpdated.Count,
					"The size of WfiWordform.Occurrences should not have changed.");
				int iCbaReal = occurrencesUpdated.IndexOf(cbaReal.Hvo);
				Assert.AreEqual(iCbaDummy, iCbaReal, "Expected our real cba to replace the dummy one in WfiWordform.Occurrences");
				iCbaDummy = occurrencesUpdated.IndexOf(cbaDummy.Hvo);
				Assert.IsTrue(iCbaDummy < 0, "Expected our dummy cba to be deleted from WfiWordform.Occurrences");

				// make sure our record clerk's virtual property also got updated.
				List<int> resultsUpdated = m_concordanceControl.Results();
				Assert.AreEqual(results.Count, resultsUpdated.Count, "The size of our matching items results shouldn't change.");
				Assert.AreEqual(resultsUpdated[lastIndex], cbaReal.Hvo, "new matching items result should match our new real cba");
				int iCbaResultsOld = resultsUpdated.IndexOf(cbaDummy.Hvo);
				Assert.IsTrue(iCbaResultsOld < 0, "Expected our dummy cba to be removed from our matching items.");
			}
			Cache.Undo();
		}

		/// <summary>
		/// Test the interaction between making and breaking phrases and the concordance tool.
		/// </summary>
		[Test]
		//[Ignore("FWC-16 - this test causes NUnit to hang. Need to investigate further.")]
		public void MakeAndBreakPhrases()
		{
			using (InterlinMasterHelper imh = new InterlinMasterHelper(this))
			{
				// 1. Start with a search in the basic text.
				m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
				m_concordanceControl.MatchCase = true;
				m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;
				m_concordanceControl.SearchText = "XXX";	// should get all our twfics.
				List<int> results = m_concordanceControl.Search(); // this should parse the paragraphs creating dummy twfics.
				m_window.ProcessPendingItems();
				Assert.AreEqual(6, results.Count);
				Assert.AreEqual(results.Count, m_concordanceControl.Clerk.ListSize);

				// jump to ZZZamazingXXX
				m_concordanceControl.Clerk.JumpToIndex(4);
				m_window.ProcessPendingItems();
				StTxtPara.TwficInfo currentTwficInfo = new StTxtPara.TwficInfo(Cache, m_concordanceControl.Clerk.CurrentObject.Hvo);
				WfiWordform wf = WfiWordform.CreateFromDBObject(Cache, currentTwficInfo.HvoWfiWordform) as WfiWordform;
				Assert.AreEqual("ZZZamazingXXX", wf.Form.VernacularDefaultWritingSystem);

				// switch to interlinear tab, and make sure we're selecting "ZZZamazingXXX"
				imh.SwitchTab(InterlinMaster.TabPageSelection.Interlinearizer);
				m_window.ProcessPendingItems();
				// nothing should have changed
				results = m_concordanceControl.Results(); // the merge should not have refreshed our list (yet)
				Assert.AreEqual(6, results.Count);
				Assert.AreEqual(results.Count, m_concordanceControl.Clerk.ListSize);

				currentTwficInfo = new StTxtPara.TwficInfo(Cache, imh.CurrentInterlinDoc.HvoAnnotation);
				wf = WfiWordform.CreateFromDBObject(Cache, currentTwficInfo.HvoWfiWordform) as WfiWordform;
				Assert.AreEqual("ZZZamazingXXX", wf.Form.VernacularDefaultWritingSystem);

				// 2. Make a basic phrase
				// XXXlocoZZZ, XXXsegmentZZZ?? [ZZZamazingXXX wonderfulXXXzzzcounselor]
				m_window.InvokeCommand("CmdMakePhrase");
				currentTwficInfo = new StTxtPara.TwficInfo(Cache, imh.CurrentInterlinDoc.HvoAnnotation);
				wf = WfiWordform.CreateFromDBObject(Cache, currentTwficInfo.HvoWfiWordform) as WfiWordform;
				Assert.AreEqual("ZZZamazingXXX wonderfulXXXzzzcounselor", wf.Form.VernacularDefaultWritingSystem);

				results = m_concordanceControl.Results(); // CmBaseAnnotation.DeleteUnderlyingObject() removed itself from our cache.
				Assert.AreEqual(6, m_concordanceControl.Clerk.ListSize); // but we haven't refreshed our list yet.
				results = m_concordanceControl.Search(); // manually get the new results
				m_window.ProcessPendingItems();
				Assert.AreEqual(5, results.Count);
				// now compare this result set against the number of items in our browse view.
				Assert.AreEqual(results.Count, m_concordanceControl.Clerk.ListSize, "Our browse view results should match our propery list size");

				// 3. Break the phrase
				// XXXlocoZZZ, XXXsegmentZZZ?? \ZZZamazingXXX\ \wonderfulXXXzzzcounselor\
				m_window.InvokeCommand("CmdBreakPhrase");
				currentTwficInfo = new StTxtPara.TwficInfo(Cache, imh.CurrentInterlinDoc.HvoAnnotation);
				wf = WfiWordform.CreateFromDBObject(Cache, currentTwficInfo.HvoWfiWordform) as WfiWordform;
				Assert.AreEqual("ZZZamazingXXX", wf.Form.VernacularDefaultWritingSystem);
				results = m_concordanceControl.Results(); // CmBaseAnnotation.DeleteUnderlyingObject() removed phrase from our cache.
				Assert.AreEqual(4, results.Count);
				results = m_concordanceControl.Search(); // the break should result in new search results.
				m_window.ProcessPendingItems();
				Assert.AreEqual(6, results.Count);
				// now compare this result set against the number of items in our browse view.
				Assert.AreEqual(results.Count, m_concordanceControl.Clerk.ListSize, "Our browse view results should match our propery list size");
			}
		}

		[Test]
		[Ignore("Complete this test")]
		public void LT7343_ReloadingScriptureIds()
		{
			m_window.ActivateTool("interlinearEdit");
			RecordClerk clerk = m_window.ActiveClerk;
			Assert.AreEqual(1, clerk.ListSize);
			IncludeAllScripture();
			Assert.AreEqual(72, clerk.ListSize);
		}

		[Test]
		[Ignore("ConcordanceControl no longer tries to convert annotations or their wfiwordforms. We could try this on Word List Concordance")]
		public void LT6712_ClerkJumpToIndex()
		{
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - ClerkJumpToIndex_LT6712",
				"ConcordanceControlTests - ClerkJumpToIndex_LT6712"))
			{
				// First verify that we expect the browse view to convert selected dummy annotations to real ones.
				// <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.RecordBrowseView"/>
				// <parameters id="wordOccurrenceList" editable="false" clerk="OccurrencesOfSelectedUnit" convertDummiesSelected="true" filterBar="false" altTitleId="Concordance-Matches">
				XmlNode assemblynode = m_concordanceToolNode.SelectSingleNode(".//dynamicloaderinfo[@class='SIL.FieldWorks.XWorks.RecordBrowseView']");
				XmlNode browseViewParameters = assemblynode.ParentNode.SelectSingleNode("parameters");
				Assert.IsTrue(XmlUtils.GetOptionalBooleanAttributeValue(browseViewParameters, "convertDummiesSelected", false),
					"We expected our browse view to be configured to convert annotations to real ones upon selection");

				m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
				m_concordanceControl.MatchCase = true;
				m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

				m_concordanceControl.SearchText = "XXX";	// should get all our twfics.
				List<int> results = m_concordanceControl.Search();
				m_window.ProcessPendingItems();
				Assert.AreEqual(6, results.Count);

				// jump to last index and verify we have changed.
				int lastIndex = results.Count - 1;
				Assert.AreEqual(0, m_concordanceControl.Clerk.CurrentIndex);
				// make sure that the item we're jumping to is a virtual object
				Assert.IsTrue(Cache.IsDummyObject(results[lastIndex]),
					"we're expecting the last annotation (to 'wonderfulXXXzzzcounselor') to be virtual until we select it.");
				m_concordanceControl.Clerk.JumpToIndex(lastIndex);
				m_window.ProcessPendingItems();
				Assert.AreEqual(lastIndex, m_concordanceControl.Clerk.CurrentIndex);
				// make sure the item on the clerk has become real.
				Assert.IsTrue(!Cache.IsDummyObject(m_concordanceControl.Clerk.CurrentObject.Hvo),
					"the selected annotation should have been converted to a real annotation.");
				// we could compare the contents of the results[lastIndex] and Clerk.CurrentObject just to make sure
				// we are looking at the expected annotation.
			}
			Cache.Undo();

		}

		/// <summary>
		/// Try to convert a dummy twfic in Concordance Control, that gets handled by ConcordanceWordsRecordList
		/// without reloading Concordance Control during the conversion.
		/// </summary>
		[Test]
		[Ignore("ConcordanceControl no longer tries to convert annotations or their wfiwordforms.")]
		public void MultipleRecordLists_PropChange()
		{
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - MultipleRecordLists_PropChange",
				"ConcordanceControlTests - MultipleRecordLists_PropChange"))
			{
				// First verify that we expect the browse view to convert selected dummy annotations to real ones.
				// <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.RecordBrowseView"/>
				// <parameters id="wordOccurrenceList" editable="false" clerk="OccurrencesOfSelectedUnit" convertDummiesSelected="true" filterBar="false" altTitleId="Concordance-Matches">
				XmlNode assemblynode = m_concordanceToolNode.SelectSingleNode(".//dynamicloaderinfo[@class='SIL.FieldWorks.XWorks.RecordBrowseView']");
				XmlNode browseViewParameters = assemblynode.ParentNode.SelectSingleNode("parameters");
				Assert.IsTrue(XmlUtils.GetOptionalBooleanAttributeValue(browseViewParameters, "convertDummiesSelected", false),
					"We expected our browse view to be configured to convert annotations to real ones upon selection");

				m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
				m_concordanceControl.MatchCase = true;
				m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

				m_concordanceControl.SearchText = "XXX";	// should get all our twfics.
				List<int> results = m_concordanceControl.Search();
				m_window.ProcessPendingItems();
				Assert.AreEqual(6, results.Count);

				// switch tools and come back, without modifying anything.
				// this should trigger a reload & clear matches since
				// we currently invalidate our wordform id list when building wordListConcordance.
				m_window.ActivateTool("wordListConcordance");
				m_window.ProcessPendingItems();
				SwitchToConcordanceTool();
				results = m_concordanceControl.Results();
				Assert.AreEqual(1, m_concordanceControl.SearchForMatchesCount);
				Assert.AreEqual(1, m_concordanceControl.ClearMatchesCount);
				Assert.AreEqual(6, results.Count);
				m_concordanceControl.ResetCounters();

				// jump to last index and verify we have changed.
				int lastIndex = results.Count - 1;
				Assert.AreEqual(0, m_concordanceControl.Clerk.CurrentIndex);
				// make sure that the item we're jumping to is a virtual object
				Assert.IsTrue(Cache.IsDummyObject(results[lastIndex]),
					"we're expecting the last annotation (to 'wonderfulXXXzzzcounselor') to be virtual until we select it.");
				m_concordanceControl.Clerk.JumpToIndex(lastIndex);
				m_window.ProcessPendingItems();
				// just because we visited wordListConcordance, doesn't mean we have to reload ConcordanceControl
				// when ConcordancWordformsRecordList handles a twfic dummy conversion
				Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
				Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
				Assert.AreEqual(6, m_concordanceControl.Results().Count);
				m_concordanceControl.ResetCounters();

				Assert.AreEqual(lastIndex, m_concordanceControl.Clerk.CurrentIndex);
				// make sure the item on the clerk has become real.
				Assert.IsTrue(!Cache.IsDummyObject(m_concordanceControl.Clerk.CurrentObject.Hvo),
					"the selected annotation should have been converted to a real annotation.");
				// we could compare the contents of the results[lastIndex] and Clerk.CurrentObject just to make sure
				// we are looking at the expected annotation.
			}
			Cache.Undo();

		}

		[Test]
		public void LT7202_CrashInWordsAfterEditingBaselineText()
		{
			// Switch to Texts
			m_window.ActivateTool("interlinearEdit");
			RecordClerk clerk = m_window.ActiveClerk;
			int hvoFirstSelectedText = clerk.CurrentObject.Hvo;

			// switch to baseline tab.
			m_window.InvokeCommand("CmdInsertText");
			int hvoNewText = clerk.CurrentObject.Hvo;
			Assert.AreNotEqual(hvoFirstSelectedText, hvoNewText, "clerk current object should have changed after inserting new text.");
			// should be the text in the active clerk.
			StText stText = clerk.CurrentObject as StText;
			// Assert in new text.
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - LT7202", "ConcordanceControlTests - LT7202"))
			{
				// verify we are in a blank text.
				Assert.AreEqual(null, (stText.ParagraphsOS[0] as StTxtPara).Contents.Text, "new text should be empty");
				// paste in a new paragraph
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				(stText.ParagraphsOS[0] as StTxtPara).Contents.UnderlyingTsString = tsf.MakeString("aVVV boyVVV", Cache.DefaultVernWs);
			}
			// switch to InterlinearTab.
			InterlinMaster interlinMaster = m_window.FindControl("InterlinMaster") as InterlinMaster;
			TabControl interlinTabs = interlinMaster.Controls.Find("m_tabCtrl", false)[0] as TabControl;
			interlinTabs.SelectedIndex = (int)InterlinMaster.TabPageSelection.Interlinearizer;
			InterlinDocChild idc = interlinMaster.Controls.Find("m_idcPane", true)[0] as InterlinDocChild;
			Assert.IsTrue(idc.HvoAnnotation != 0, "Expected to have selected nonzero annotation");
			StTxtPara.TwficInfo currentTwficInfo = new StTxtPara.TwficInfo(Cache, idc.HvoAnnotation);
			Assert.IsTrue(currentTwficInfo.IsObjectValid() &&
				currentTwficInfo.Object.InstanceOfRA.ClassID == WfiWordform.kClassId,
				"Expected to have selected a wfiwordform annotation");
			Assert.AreEqual("aVVV", (currentTwficInfo.Object.InstanceOfRA as WfiWordform).Form.VernacularDefaultWritingSystem,
				"Expected to have selected annotation for 'aVVV'");
			// switch to wordListConcordance
			m_window.ActivateTool("wordListConcordance");
			// validate we haven't invalidated the "aVVV" wordform or its annotation (yet)
			Assert.IsTrue(currentTwficInfo.IsObjectValid() &&
				currentTwficInfo.Object.InstanceOfRA.ClassID == WfiWordform.kClassId &&
				currentTwficInfo.Object.IsValidObject(),
				"Expected to have a valid wfiwordform");
			// jump to the "aVVV" wordform.
			clerk = m_window.ActiveClerk;
			clerk.JumpToRecord(currentTwficInfo.Object.InstanceOfRAHvo);
			m_window.ProcessPendingItems();
			// validate we jumped to the right wordform.
			Assert.AreEqual("aVVV", (clerk.CurrentObject as WfiWordform).Form.VernacularDefaultWritingSystem,
				"Expected to have selected wordform 'aVVV'");
			// verify our OccurrencesOfWordform clerk sync'd properly.
			RecordClerk occurrencesClerk = RecordClerk.FindClerk(Mediator, "OccurrencesOfSelectedWordform");
			Assert.AreEqual(clerk.CurrentObject.Hvo, occurrencesClerk.OwningObject.Hvo,
				"OccurrencesOfSelectedWordform should be sync'd to the current object of the active clerk.");
			Assert.AreEqual(1, occurrencesClerk.ListSize);
			Assert.IsTrue(occurrencesClerk.CurrentObject != null);
			currentTwficInfo = new StTxtPara.TwficInfo(Cache, occurrencesClerk.CurrentObject.Hvo);
			Assert.IsTrue(currentTwficInfo.IsObjectValid() &&
				currentTwficInfo.Object.InstanceOfRA.ClassID == WfiWordform.kClassId,
				"Expected to have selected a wfiwordform annotation");
			Assert.AreEqual("aVVV", (currentTwficInfo.Object.InstanceOfRA as WfiWordform).Form.VernacularDefaultWritingSystem,
				"Expected to have selected annotation for 'aVVV'");

			interlinMaster = m_window.FindControl("InterlinMaster") as InterlinMaster;
			// switch to InterlinearTab
			interlinTabs = interlinMaster.Controls.Find("m_tabCtrl", false)[0] as TabControl;
			interlinTabs.SelectedIndex = (int)InterlinMaster.TabPageSelection.Interlinearizer;
			idc = interlinMaster.Controls.Find("m_idcPane", true)[0] as InterlinDocChild;
			// Verify we've selected the annotation for 'aVVV'
			currentTwficInfo = new StTxtPara.TwficInfo(Cache, idc.HvoAnnotation);
			Assert.IsTrue(currentTwficInfo.IsObjectValid() &&
				currentTwficInfo.Object.InstanceOfRA.ClassID == WfiWordform.kClassId,
				"Expected to have selected a wfiwordform annotation");
			Assert.AreEqual("aVVV", (currentTwficInfo.Object.InstanceOfRA as WfiWordform).Form.VernacularDefaultWritingSystem,
				"Expected to have selected annotation for 'aVVV'");

			// switch back to interlinearEdit
			m_window.ActivateTool("interlinearEdit");
			clerk = m_window.ActiveClerk;
			interlinMaster = m_window.FindControl("InterlinMaster") as InterlinMaster;
			interlinTabs = interlinMaster.Controls.Find("m_tabCtrl", false)[0] as TabControl;
			interlinTabs.SelectedIndex = (int)InterlinMaster.TabPageSelection.RawText;
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - LT7202", "ConcordanceControlTests - LT7202"))
			{
				// remove the first word
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				(stText.ParagraphsOS[0] as StTxtPara).Contents.UnderlyingTsString = tsf.MakeString("boyVVV", Cache.DefaultVernWs);
			}
			// parse the text again
			interlinTabs.SelectedIndex = (int)InterlinMaster.TabPageSelection.Interlinearizer;
			idc = interlinMaster.Controls.Find("m_idcPane", true)[0] as InterlinDocChild;
			currentTwficInfo = new StTxtPara.TwficInfo(Cache, idc.HvoAnnotation);
			Assert.IsTrue(currentTwficInfo.IsObjectValid() &&
				currentTwficInfo.Object.InstanceOfRA.ClassID == WfiWordform.kClassId,
				"Expected to have selected a wfiwordform annotation");
			Assert.AreEqual("boyVVV", (currentTwficInfo.Object.InstanceOfRA as WfiWordform).Form.VernacularDefaultWritingSystem,
				"Expected to have selected annotation for 'boyVVV'");
			// switch back to Word List Concordance
			// this is where it was crashing.
			m_window.ActivateTool("wordListConcordance");
			// validate we're still on the 'aVVV' wordform.
			clerk = m_window.ActiveClerk;
			Assert.AreEqual("aVVV", (clerk.CurrentObject as WfiWordform).Form.VernacularDefaultWritingSystem,
				"Expected to have selected wordform 'aVVV'");
			occurrencesClerk = RecordClerk.FindClerk(Mediator, "OccurrencesOfSelectedWordform");
			Assert.AreEqual(clerk.CurrentObject.Hvo, occurrencesClerk.OwningObject.Hvo,
				"OccurrencesOfSelectedWordform should be sync'd to the current object of the active clerk.");
			// should have cleared our occurrences list for this wordform.
			Assert.AreEqual(0, occurrencesClerk.ListSize, "Should have cleared the OccurrencesOfSelectedWordform for 'aVVV'");
		}

		/// <summary>
		/// Note: this is more of an InterlinDocView test than a ConcordanceControl test.
		/// </summary>
		[Test]
		public void LT7777_EnsureAllSegmentsHaveFreeformAnnotation()
		{
			Set<int> allAnalWsIds = new Set<int>(Cache.LangProject.AnalysisWssRC.HvoArray);
			int tagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(Cache);
			int segDefn_literalTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
			int segDefn_freeTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			int segDefn_note = Cache.GetIdFromGuid(LangProject.kguidAnnNote);

			// Switch to first text
			m_window.ActivateTool("interlinearEdit");
			RecordClerk clerk = m_window.ActiveClerk;
			clerk.JumpToRecord(m_text1.ContentsOA.Hvo);
			m_window.ProcessPendingItems();

			// switch to baseline tab.
			int hvoSelectedText = clerk.CurrentObject.Hvo;
			Assert.AreEqual(m_text1.ContentsOA.Hvo, hvoSelectedText, "clerk current object should have changed to m_text1");

			// should be the text in the active clerk.
			StText stText = clerk.CurrentObject as StText;
			InterlinMaster interlinMaster = m_window.FindControl("InterlinMaster") as InterlinMaster;
			using (InterlinMasterHelper imh = new InterlinMasterHelper(this))
			{
				// switch to RawText (Edit Pane)
				imh.SwitchTab(InterlinMaster.TabPageSelection.RawText);
				//// Insert new segments in a new paragraph.
				//IStTxtPara newPara;
				//using (new UndoRedoTaskHelper(Cache, "EnsureAllSegmentsHaveFreeformAnnotation", "EnsureAllSegmentsHaveFreeformAnnotation"))
				//{
				//    // insert a new paragraph with two new sentences.
				//    // paste in a new paragraph
				//    ITsStrFactory tsf = TsStrFactoryClass.Create();
				//    newPara = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as IStTxtPara;
				//    newPara.Contents.UnderlyingTsString = tsf.MakeString("XXXXsentenceOneZZZ; XXXsentenceTwoZZZ!!", Cache.DefaultVernWs);
				//}
				// first verify that paragraph 1 has only one real segment.
				List<int> segs1 = (m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara).Segments;
				Assert.AreEqual(2, segs1.Count, "expect paragraph 1 to have two segments");
				Assert.IsFalse(Cache.IsDummyObject(segs1[0]), "expect paragraph 1 seg 0 to be real");
				Assert.IsFalse(Cache.IsDummyObject(segs1[1]), "expect paragraph 1 seg 1 to be real");
				// Verify paragraph 1 segment 1 has only one freeform translation.
				StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segs1), allAnalWsIds);
				int[] segFF1_0 = Cache.GetVectorProperty(segs1[0], tagSegFF, true);
				Assert.AreEqual(3, segFF1_0.Length, "expected paragraph 1 seg 0 to have freeform annotations");
				Assert.AreEqual(1, FreeformTypeCount(segFF1_0, segDefn_freeTranslation));
				int[] segFF1_1 = Cache.GetVectorProperty(segs1[1], tagSegFF, true);
				Assert.AreEqual(0, segFF1_1.Length, "expected paragraph 1 seg 1 to have no freeform annotations");

				// now switch to interlinear tab and make sure we added new freeform translations
				imh.SwitchTab(InterlinMaster.TabPageSelection.Interlinearizer);

				// Verify paragraph 1 segment 1 still has only one freeform translation.
				segs1 = (m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara).Segments;
				Assert.AreEqual(2, segs1.Count, "expect paragraph 1 to have two segments");
				// Verify paragraph 1 segment 1 has only one freeform translation.
				StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segs1), allAnalWsIds);
				segFF1_0 = Cache.GetVectorProperty(segs1[0], tagSegFF, true);
				Assert.AreEqual(3, segFF1_0.Length, "expected paragraph 1 seg 0 to have freeform annotations");
				Assert.AreEqual(1, FreeformTypeCount(segFF1_0, segDefn_freeTranslation));

				// make sure our last segment has a free translation now.
				segFF1_1 = Cache.GetVectorProperty(segs1[1], tagSegFF, true);
				Assert.AreEqual(1, segFF1_1.Length, "expected paragraph 1 seg 1 to have a freeform annotation");
				Assert.AreEqual(1, FreeformTypeCount(segFF1_1, segDefn_freeTranslation));
			}
		}

		/// <summary>
		/// return the number of freeform annotations of the given type
		/// </summary>
		/// <param name="hvoSegFFs"></param>
		/// <param name="hvoTargetTypeFF"></param>
		/// <returns></returns>
		int FreeformTypeCount(int[] hvoSegFFs, int hvoTargetTypeFF)
		{
			int cOfTargetType = 0;
			IDictionary<int, int> typeToCount = new Dictionary<int, int>();
			foreach (int hvoSegFF in hvoSegFFs)
			{
				int hvoTypeFF = Cache.GetObjProperty(hvoSegFF, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
				if (!typeToCount.ContainsKey(hvoTypeFF))
					typeToCount.Add(hvoTypeFF, 1);
				else
					typeToCount[hvoTypeFF]++;
			}
			if (typeToCount.TryGetValue(hvoTargetTypeFF, out cOfTargetType))
				return cOfTargetType;
			else
				return 0;
		}

		[Test]
		//[Ignore("FWC-16 - this test causes NUnit to hang. Need to investigate further.")]
		public void LT6967_LimitingAutoSearch()
		{
			// first initialization should not load or clear matches.
			Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);

			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kBaseline);
			m_concordanceControl.MatchCase = true;
			m_concordanceControl.SearchOption = MockConcordanceControl.ConcordanceSearchOption.Anywhere;

			m_concordanceControl.SearchText = "XXX";	// should get all our twfics.
			List<int> results = m_concordanceControl.Search();
			m_window.ProcessPendingItems();
			Assert.AreEqual(6, results.Count);
			Assert.AreEqual(1, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			m_concordanceControl.ResetCounters();

			// Do a refresh
			// the list should reload with the same results.
			this.MasterRefresh();
			Assert.AreEqual(1, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			Assert.AreEqual(6, m_concordanceControl.Results().Count);
			m_concordanceControl.ResetCounters();

			// switch tools and come back, without modifying anything.
			// The record list should reload, but not the virtual property.
			m_window.ActivateTool("wordListConcordance");
			m_window.ProcessPendingItems();
			SwitchToConcordanceTool();
			Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			Assert.AreEqual(6, m_concordanceControl.Results().Count);
			m_concordanceControl.ResetCounters();

			// switch tools, and modify the text.
			// the record list should clear, expecting the user to click 'Search' to do the search again.
			/// interlinearEdit
			m_window.ActivateTool("interlinearEdit");
			m_window.ProcessPendingItems();
			IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - LT6967_LimitingAutoSearch.ModifyParagraph",
				"ConcordanceControlTests - LT6967_LimitingAutoSearch.ModifyParagraph"))
			{
				// does this issue a prop change?
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				para1.Contents.UnderlyingTsString = tsf.MakeString(para1.Contents.Text + " XXXinsertedTextZZZ", Cache.DefaultVernWs);
			}
			SwitchToConcordanceTool();
			Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(1, m_concordanceControl.ClearMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.Results().Count);
			m_concordanceControl.ResetCounters();

			// Do a refresh
			// we should still have no results, even though we have text in the search line.
			this.MasterRefresh();
			Assert.AreEqual("XXX", m_concordanceControl.SearchText);
			Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.Results().Count);
			m_concordanceControl.ResetCounters();

			// Redo our search.
			m_concordanceControl.SelectLineOption(ConcordanceControl.ConcordanceLines.kWord);
			results = m_concordanceControl.Search();
			m_window.ProcessPendingItems();
			Assert.AreEqual(7, results.Count);
			Assert.AreEqual(1, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			m_concordanceControl.ResetCounters();

			using (new UndoRedoTaskHelper(Cache, "ConcordanceControlTests - LT6967_LimitingAutoSearch.MakeAndBreakPhrases",
				"ConcordanceControlTests - LT6967_LimitingAutoSearch.MakeAndBreakPhrases"))
			{
				ParagraphAnnotator ta = new ParagraphAnnotator(m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara);
				// 2. Make a basic phrase
				// XXXlocoZZZ, XXXsegmentZZZ?? [ZZZamazingXXX wonderfulXXXzzzcounselor]
				ta.MergeAdjacentAnnotations(1, 0);

				results = m_concordanceControl.Results(); // the merge should delete an annotation.
				Assert.AreEqual(6, results.Count);
				Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
				Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
				Assert.AreEqual(7, m_concordanceControl.Clerk.ListSize);	// we haven't refreshed our browse view yet.
				m_concordanceControl.ResetCounters();
				results = m_concordanceControl.Search(); // the merge should result in new search results.
				m_window.ProcessPendingItems();
				Assert.AreEqual(6, results.Count);
				Assert.AreEqual(1, m_concordanceControl.SearchForMatchesCount);
				Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
				Assert.AreEqual(6, m_concordanceControl.Clerk.ListSize);	// we haven't refreshed our browse view yet.
				m_concordanceControl.ResetCounters();
			}

			Cache.Undo();	// Undo MakePhrase
			// Test that the Undo resulted in the old results set.
			m_window.ProcessPendingItems();
			results = m_concordanceControl.Results(); // the undo should delete our phrase annotation.
			// results count will be zero if Undo cleared our virtual property
			//Assert.AreEqual(6, results.Count);
			Assert.AreEqual(0, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			Assert.AreEqual(6, m_concordanceControl.Clerk.ListSize);	// we haven't refreshed our browse view yet.
			m_concordanceControl.ResetCounters();
			results = m_concordanceControl.Search();
			Assert.AreEqual(1, m_concordanceControl.SearchForMatchesCount);
			Assert.AreEqual(0, m_concordanceControl.ClearMatchesCount);
			Assert.AreEqual(7, results.Count);
			Assert.AreEqual(7, m_concordanceControl.Clerk.ListSize);	// we haven't refreshed our browse view yet.
			m_concordanceControl.ResetCounters();
		}
	}

	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class WordListConcordanceTests : InterlinearFwXWindowTestBase
	{
		//XmlNode m_configurationNode = null;
		InterlinearTextsVirtualHandler m_concordanceTextsVh;
		RecordClerk m_occurrencesList;
		RecordClerk m_wordList;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			m_concordanceTextsVh = null;
			base.Exit();
		}

		/// <summary>
		/// We need to re-initialize our PropertyTable for each test.
		/// </summary>
		protected override void InitializeWindowAndToolControls()
		{
			base.InitializeWindowAndToolControls();

			m_concordanceTextsVh = BaseVirtualHandler.GetInstalledHandler(m_fdoCache,
				"LangProject", "InterlinearTexts") as InterlinearTextsVirtualHandler;
			ClearScriptureFilter();
			SwitchToWordListConcordanceTool();
		}

		private void SwitchToWordListConcordanceTool()
		{
			m_window.ActivateTool("wordListConcordance");
			m_occurrencesList = RecordClerk.FindClerk(Mediator, "OccurrencesOfSelectedWordform");
			m_wordList = RecordClerk.FindClerk(Mediator, "concordanceWords");
		}

		private void ClearScriptureFilter()
		{
			m_concordanceTextsVh.NeedToReloadSettings = true;
		}

		private void JumpToWord(string form)
		{
			int hvoWf = Cache.LangProject.WordformInventoryOA.GetWordformId(form, Cache.DefaultVernWs);
			if (Cache.IsDummyObject(hvoWf))
			{
				ICmObject realObj = CmObject.ConvertDummyToReal(Cache, hvoWf);
				hvoWf = realObj.Hvo;
			}
			m_wordList.JumpToRecord(hvoWf);
			m_window.ProcessPendingItems();
		}

		/// <summary>
		/// Make sure editing a word selected with one occurrence, does not change text.
		/// 1) Switch to wordform "XXXlocoZZZ" with one occurrence.
		/// 2) Delete a character at the end of that occurrence resulting in "XXXlocoZZ"
		/// 3) Make sure the same baseline is still active, even though the occurrences of "XXXlocoZZZ" has been altered.
		/// </summary>
		[Test]
		public void SelectWordWithOneOccurrenceAndEditOccurrenceInText()
		{
			JumpToWord("XXXlocoZZZ");
			int indexWordBeforeEdit = m_wordList.CurrentIndex;
			int hvoWordBeforeEdit = m_wordList.CurrentObject.Hvo;
			using (InterlinMasterHelper imh = new InterlinMasterHelper(this))
			{
				imh.SwitchTab(InterlinMaster.TabPageSelection.RawText);
				RawTextPane rtp = imh.RawTextPane;
				IStText stTextBeforeEdit = rtp.RootObject;
				// paragraph1: XXXlocoZZZ, XXXsegmentZZZ?? ZZZamazingXXX wonderfulXXXzzzcounselor!!
				StTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;
				imh.SetCursor(para1, "XXXlocoZZZ".Length);
				// save the cursor location
				TextSelInfo tsiBeforeEdit = imh.CurrentSelectionInfo;
				imh.OnKeyDownAndKeyPress(Keys.Back);
				ValidateExpectedPlaceInText(indexWordBeforeEdit, tsiBeforeEdit.IchAnchor - 1, hvoWordBeforeEdit, imh, rtp, stTextBeforeEdit, tsiBeforeEdit);
				imh.SetCursor(para1, "XXXlocoZZ, XXXsegmentZZ".Length);
				tsiBeforeEdit = imh.CurrentSelectionInfo;
				imh.OnKeyDownAndKeyPress(Keys.Delete);	// delete last letter in "XXXsegmentZZZ"
				ValidateExpectedPlaceInText(indexWordBeforeEdit, tsiBeforeEdit.IchAnchor, hvoWordBeforeEdit, imh, rtp, stTextBeforeEdit, tsiBeforeEdit);
			}
		}

		private void ValidateExpectedPlaceInText(int indexWordBeforeEdit, int ichExpected, int hvoWordBeforeEdit, InterlinMasterHelper imh, RawTextPane rtp, IStText stTextBeforeEdit, TextSelInfo tsiBeforeEdit)
		{
			IStText stTextAfterEdit = rtp.RootObject;
			TextSelInfo tsiAfterEdit = imh.CurrentSelectionInfo;
			Assert.AreEqual(stTextBeforeEdit, stTextAfterEdit, "We expect to be in the same text.");
			// make sure we're in the same text
			Assert.AreEqual(tsiBeforeEdit.HvoAnchor, tsiAfterEdit.HvoAnchor, "We expect to be in the same paragraph");
			// make sure the cursor is in the expected location
			Assert.AreEqual(ichExpected, tsiAfterEdit.IchAnchor, "Expected cursor somewhere else.");
			// Make sure we haven't changed word in the wordform record list.
			int indexWordAfterEdit = m_wordList.CurrentIndex;
			int hvoWordAfterEdit = m_wordList.CurrentObject.Hvo;
			Assert.AreEqual(indexWordBeforeEdit, indexWordAfterEdit, "word index shouldn't have changed");
			Assert.AreEqual(hvoWordBeforeEdit, hvoWordAfterEdit, "word hvo shouldn't have changed");
		}

		/// <summary>
		/// Making (and breaking) a new phrase should keep the focus box in the same place.
		/// </summary>
		[Test]
		public void MakeAndBreakPhraseAndKeepFocusBoxInPlace()
		{
			JumpToWord("wonderfulXXXzzzcounselor");
			int indexWordBeforeEdit = m_wordList.CurrentIndex;
			int hvoWordBeforeEdit = m_wordList.CurrentObject.Hvo;
			using (InterlinMasterHelper imh = new InterlinMasterHelper(this))
			{
				imh.SwitchTab(InterlinMaster.TabPageSelection.Interlinearizer);
				InterlinDocChild idc = imh.CurrentInterlinDoc;
				// paragraph1: XXXlocoZZZ, XXXsegmentZZZ?? ZZZamazingXXX wonderfulXXXzzzcounselor!!
				StTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;
				int hvoAnnBeforeEdit = para1.SegmentForm(1, 0);
				StTxtPara.TwficInfo twficBeforeEdit1_0 = new StTxtPara.TwficInfo(Cache, hvoAnnBeforeEdit);
				twficBeforeEdit1_0.CaptureObjectInfo();
				// select a "ZZZamazingXXX" so we can make a phrase with "wonderfulXXXzzzcounselor"
				IStText stTextBeforeEdit = idc.RawStText;
				idc.SelectAnnotation(hvoAnnBeforeEdit);
				// XXXlocoZZZ, XXXsegmentZZZ?? [ZZZamazingXXX wonderfulXXXzzzcounselor]
				m_window.InvokeCommand("CmdMakePhrase");
				ValidateFocusBoxState(indexWordBeforeEdit, idc, twficBeforeEdit1_0, stTextBeforeEdit);
				StTxtPara.TwficInfo twficAfterMakePhrase1_0 = new StTxtPara.TwficInfo(Cache, idc.HvoAnnotation);
				twficAfterMakePhrase1_0.CaptureObjectInfo();
				m_window.InvokeCommand("CmdBreakPhrase");
				// worderfulXXXzzzcounselor will have been inserted back before the current index, so advance one
				ValidateFocusBoxState(indexWordBeforeEdit + 1, idc, twficAfterMakePhrase1_0, stTextBeforeEdit);
				StTxtPara.TwficInfo twficAfterBreakPhrase1_0 = new StTxtPara.TwficInfo(Cache, idc.HvoAnnotation);
				twficAfterBreakPhrase1_0.CaptureObjectInfo();
				idc.OnUndo(null);
				ValidateFocusBoxState(indexWordBeforeEdit, idc, twficAfterBreakPhrase1_0, stTextBeforeEdit);
			}
		}

		private void ValidateFocusBoxState(int indexWordExpected, InterlinDocChild idc, StTxtPara.TwficInfo twficBeforeEdit1_0, IStText stTextBeforeEdit)
		{
			IStText stTextAfterEdit = idc.RawStText;
			Assert.AreEqual(stTextBeforeEdit, stTextAfterEdit, "We expect to be in the same text.");
			int hvoAnnAfterEdit = idc.HvoAnnotation;
			ICmBaseAnnotation cbaAfterEdit = new CmBaseAnnotation(Cache, hvoAnnAfterEdit);
			Assert.IsFalse(twficBeforeEdit1_0.IsCapturedObjectInfoValid());
			Assert.AreEqual(twficBeforeEdit1_0.BeginOffset, cbaAfterEdit.BeginOffset, "FocusBox should be at the same location.");
			Assert.AreNotEqual(twficBeforeEdit1_0.EndOffset, cbaAfterEdit.EndOffset, "FocusBox should now be on phrase annotation.");

			// FocusBox should be on phrase, not somewhere else.
			Assert.IsNotNull(idc.IsFocusBoxInstalled, "Focus Box should still be installed.");
			Assert.AreEqual(idc.ExistingFocusBox.InterlinWordControl.HvoAnnotation, hvoAnnAfterEdit, "FocusBox should be on current annotation.");

			// Make sure we haven't changed word in the wordform record list.
			int indexWordAfterEdit = m_wordList.CurrentIndex;
			int hvoWordAfterEdit = m_wordList.CurrentObject.Hvo;
			Assert.AreEqual(indexWordExpected, indexWordAfterEdit, "Word index mismatch");
		}


	}
}
