// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportWizardTests.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Resources;
using System.Diagnostics;
using System.IO;
using System.CodeDom.Compiler;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Win32;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.ScrImportComponents
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for ImportWizard
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportWizardTests : BaseTest
	{
		#region DummyImportWizard
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Extended ImportWizard class for testing
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public class DummyImportWizard : ImportWizard
		{
			private string m_styleSelection = string.Empty;
			private string m_wsSelection = string.Empty;
			private int m_RadioboxSelection = 0;
			private bool m_ShouldAccept = true;
			private List<string> m_filesAdded = new List<string>();
			private Form m_Dialog;
			private bool m_fCheckBackTrans = false;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyImportWizard"/> class.
			/// </summary>
			/// <param name="cache">The database cache.</param>
			/// <param name="scr">The Scripture object.</param>
			/// <param name="styleSheet">The style sheet.</param>
			/// ------------------------------------------------------------------------------------
			public DummyImportWizard(FdoCache cache, Scripture scr,
				FwStyleSheet styleSheet) :
				base("LANG. PROJ. TEST NAME.", scr, styleSheet, cache)
			{
				m_lvCurrentMappingList = lvScrMappings;
				m_btnCurrentModifyButton = m_btnModifyScrMapping;
				m_btnCurrentDeleteButton = m_btnDeleteScrMapping;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Checks the Back Translation check box
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public bool BackTranslation
			{
				set
				{
					CheckDisposed();
					m_fCheckBackTrans = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Sets the return value for use in DisplayMappingsDialog()
			/// If true, the Mapping Dialog should simulate the OK button. If false, the Cancel
			/// button.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public bool ShouldAccept
			{
				set
				{
					CheckDisposed();
					m_ShouldAccept = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Sets the styles value for use in DisplayMappingsDialog()
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public string StyleSelection
			{
				set
				{
					CheckDisposed();
					m_styleSelection = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public string WsSelection
			{
				set
				{
					CheckDisposed();
					m_wsSelection = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public int RadioboxSelection
			{
				set
				{
					CheckDisposed();
					m_RadioboxSelection = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public Form MappingDialog
			{
				get
				{
					CheckDisposed();
					return m_Dialog;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public Button ModifyButton
			{
				get
				{
					CheckDisposed();
					return m_btnModifyScrMapping;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Simulates the user clicking the Back button
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void BackButtonPerformClick()
			{
				CheckDisposed();

				m_btnBack_Click(null, null);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Simulates the user clicking the Next button
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void NextButtonPerformClick()
			{
				CheckDisposed();

				m_btnNext_Click(null, null);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Exposes the Enabled state of the Next button (read-only)
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public bool NextButtonEnabled
			{
				get
				{
					CheckDisposed();
					return m_btnNext.Enabled;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public RadioButton OtherButton
			{
				get
				{
					CheckDisposed();
					return rbOther;
				}
				set
				{
					CheckDisposed();
					rbOther = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Import settings for the wizard
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public ScrImportSet ScrImportSet
			{
				get
				{
					CheckDisposed();
					return m_settings;
				}
				set
				{
					CheckDisposed();
					m_settings = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public RadioButton Paratext6
			{
				get
				{
					CheckDisposed();
					return rbParatext6;
				}
				set
				{
					CheckDisposed();
					rbParatext6 = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public new ProjectTypes ProjectType
			{
				get
				{
					CheckDisposed();
					return m_projectType;
				}
				set
				{
					CheckDisposed();
					m_projectType = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public new ISCScriptureText ScriptureText
			{
				get
				{
					CheckDisposed();
					return m_ScriptureText;
				}
				set
				{
					CheckDisposed();
					m_ScriptureText = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public List<PTProject> PTLangProj
			{
				get
				{
					CheckDisposed();
					return m_PTLangProj;
				}
				set
				{
					CheckDisposed();
					m_PTLangProj = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public FwOverrideComboBox CboPTLangProj
			{
				get
				{
					CheckDisposed();
					return cboPTLangProj;
				}
				set
				{
					CheckDisposed();
					cboPTLangProj = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public FwOverrideComboBox CboPTBackTrans
			{
				get
				{
					CheckDisposed();
					return cboPTBackTrans;
				}
				set
				{
					CheckDisposed();
					cboPTBackTrans = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public FwOverrideComboBox CboPTTransNotes
			{
				get
				{
					CheckDisposed();
					return cboPTTransNotes;
				}
				set
				{
					CheckDisposed();
					cboPTTransNotes = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public FwListView Mappings
			{
				get
				{
					CheckDisposed();
					return lvScrMappings;
				}
				set
				{
					CheckDisposed();
					lvScrMappings = value;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public ListView FileListBuilderCurrentListView
			{
				get
				{
					CheckDisposed();

					FieldInfo currentListView = sfFileListBuilder.GetType().GetField("m_currentListView",
						BindingFlags.Instance |	BindingFlags.Public |
						BindingFlags.NonPublic);
					return (ListView)currentListView.GetValue(sfFileListBuilder);
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the SFFileListBuilder
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public SFFileListBuilder FileListBuilder
			{
				get
				{
					CheckDisposed();
					return sfFileListBuilder;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Use this to simulate the user adding files via the SfFileListBuilder.
			/// </summary>
			/// <param name="files">Array of filenames to add</param>
			/// ------------------------------------------------------------------------------------
			public void AddFiles(string[] files)
			{
				CheckDisposed();

				// Use reflection to call AddFilesToProjectAndListView.
				MethodInfo addFiles = sfFileListBuilder.GetType().GetMethod("AddFilesToProjectAndListView",
					BindingFlags.Instance |	BindingFlags.Public |
					BindingFlags.NonPublic);

				addFiles.Invoke(sfFileListBuilder, new object[] { files });
				m_filesAdded.AddRange(files);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Displays and operates the ModifyMapping dialog box, using member variables to make
			/// selections and click buttons.
			/// </summary>
			/// <param name="dlg">The dialog to show</param>
			/// <remarks>
			/// Allows us to bypass the problem of having to test a modal dialog box
			/// </remarks>
			/// ------------------------------------------------------------------------------------
			public override void DisplayMappingDialog(Form dlg)
			{
				CheckDisposed();

				m_Dialog = dlg;
				dlg.Show();

				ModifyMapping dialog = (ModifyMapping)dlg;

				ListBox stylesList = dialog.mappingDetailsCtrl.lbStyles;

				Assert.IsTrue(stylesList.Items.Count > 0, "No Styles Found in dialog.");

				if (m_styleSelection != string.Empty)
				{
					stylesList.SelectedIndex =
						stylesList.FindStringExact(m_styleSelection);
				}

				if (m_projectType != ProjectTypes.Paratext)
				{
					if (m_wsSelection != string.Empty)
					{
						ComboBox writingCombo = dialog.WritingCombobox;
						writingCombo.SelectedIndex = writingCombo.FindStringExact(
							m_wsSelection);
					}

					dialog.BackTranslation.Checked = m_fCheckBackTrans;

					switch (m_RadioboxSelection)
					{
						case -1: break;
						case 0:	dialog.Scripture.PerformClick(); break;
						case 1: dialog.Footnotes.PerformClick(); break;
						case 2:	dialog.Notes.PerformClick(); break;
						default:
							throw new Exception("Radio Button " + m_RadioboxSelection + " not found!");
					}
				}

				Application.DoEvents();

				if (m_ShouldAccept)
					dialog.AcceptButton.PerformClick();
				else
					dialog.CancelButton.PerformClick();

				Application.DoEvents();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Simulates the finish button being poked
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void SimulateFinish()
			{
				CheckDisposed();

				m_settings.SaveSettings();
			}
		}
		#endregion

		private const string kDummyAnalLegacyMapping = "HackedLegacyMappingForAnalysis";
		private const string kDummyVernLegacyMapping = "HackedLegacyMappingForVernacular";
		private const string kGermanLegacyMapping = "DE-TestMapping";

		#region data members
		private DummyImportWizard m_DummyImportWizard;
		private FdoCache m_cache;
		private FwStyleSheet m_styleSheet;
		private IScripture m_Scripture;
		private ScrImportSet m_settings;
		private RegistryData m_regData;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
		}

		/// <summary>
		/// Correct way to deal with FixtureTearDown for class that derive from BaseTest.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_regData != null)
					m_regData.RestoreRegistryData();

				Unpacker.RemoveParatextTestProjects();
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_cache = FdoCache.Create("TestLangProj");

			if (!m_cache.DatabaseAccessor.IsTransactionOpen())
				m_cache.DatabaseAccessor.BeginTrans();

			m_cache.BeginUndoTask("Undo ScrImportSetTest", "Redo ScrImportSetTest");

			m_Scripture = m_cache.LangProject.TranslatedScriptureOA;
			m_settings = new ScrImportSet();
			m_Scripture.DefaultImportSettings = m_settings;

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(m_cache, m_Scripture.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			foreach (ILgWritingSystem ws in m_cache.LanguageEncodings)
			{
				if (ws.ICULocale != null && ws.ICULocale.ToLower() == "de")
					ws.LegacyMapping = kGermanLegacyMapping;
				if (ws.ICULocale == m_cache.LangProject.DefaultAnalysisWritingSystemICULocale)
					ws.LegacyMapping = kDummyAnalLegacyMapping;
				if (ws.ICULocale == m_cache.LangProject.DefaultVernacularWritingSystemICULocale)
					ws.LegacyMapping = kDummyVernLegacyMapping;
			}

			m_DummyImportWizard = new DummyImportWizard(m_cache, (Scripture)m_Scripture, m_styleSheet);

			m_DummyImportWizard.Show();
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			if (m_DummyImportWizard != null)
			{
				if (m_DummyImportWizard.MappingDialog != null)
					m_DummyImportWizard.MappingDialog.Close();

				m_DummyImportWizard.Close();
				m_DummyImportWizard = null;
			}
			m_settings = null;

			m_cache.ActionHandlerAccessor.EndOuterUndoTask();

			while (m_cache.Undo());

			if (m_cache.DatabaseAccessor.IsTransactionOpen())
				m_cache.DatabaseAccessor.RollbackTrans();

			m_cache.Dispose();
			m_Scripture = null;
			m_cache = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the wizard throws an exception if m_DummyImportWizard.ScrImportSet
		/// property isn't set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void ExceptionWhenNoSettings()
		{
			CheckDisposed();
			m_DummyImportWizard.ScrImportSet = null;

			m_DummyImportWizard.Show();
			Application.DoEvents();

			// Move to Project type step.
			m_DummyImportWizard.NextButtonPerformClick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the Paratext 6 project combo. boxes in the wizard get the correct data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyParatextProjectStep()
		{
			CheckDisposed();
			// Move to Project type step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Choose paratext project type.
			m_DummyImportWizard.Paratext6.Checked = true;
			Application.DoEvents();

			// Move to choosing paratext projects.
			m_DummyImportWizard.NextButtonPerformClick();

			foreach (ImportWizard.PTProject ptProj in m_DummyImportWizard.PTLangProj)
			{
				Assert.IsTrue(ptProj.ShortName == "KAM" ||
					ptProj.ShortName == "TEV");
				Assert.IsTrue(ptProj.LongName == "Kamwe" ||
					ptProj.LongName == "PREDISTRIBUTION Today's English Version (USFM)");
			}

			Assert.AreEqual("(none)", (m_DummyImportWizard.CboPTBackTrans.Items[0]).ToString());

			for (int i = 1; i < 3; i++)
			{
				Assert.IsTrue(
					m_DummyImportWizard.CboPTBackTrans.Items[i].ToString() == "Kamwe" ||
					m_DummyImportWizard.CboPTBackTrans.Items[i].ToString() ==
					"PREDISTRIBUTION Today's English Version (USFM)");
			}

			Assert.AreEqual("(none)", (m_DummyImportWizard.CboPTTransNotes.Items[0]).ToString());

			for (int i = 1; i < 3; i++)
			{
				Assert.IsTrue(
					m_DummyImportWizard.CboPTTransNotes.Items[i].ToString() == "Kamwe" ||
					m_DummyImportWizard.CboPTTransNotes.Items[i].ToString() ==
					"PREDISTRIBUTION Today's English Version (USFM)");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the project location step for the "other" project type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyOtherProjectStep()
		{
			CheckDisposed();
			// Move to Project type step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Choose other project type.
			m_DummyImportWizard.OtherButton.Checked = true;

			// Move to choosing scripture files.
			m_DummyImportWizard.NextButtonPerformClick();

			// We should have no files in the list, therefore the next button should be disabled
			Assert.AreEqual(0, m_DummyImportWizard.FileListBuilder.ScriptureFiles.Count);
			Assert.AreEqual(false, m_DummyImportWizard.NextButtonEnabled);

			// Add our test file to the file list builder.
			m_DummyImportWizard.AddFiles(new string[] {Unpacker.PtProjectTestFolder + "SOTest.sfm"});

			// we should have one line with the file name and the books Tit and Jud
			Assert.AreEqual(1, m_DummyImportWizard.FileListBuilderCurrentListView.Items.Count);
			Assert.AreEqual(true, m_DummyImportWizard.NextButtonEnabled);

			Assert.AreEqual((Unpacker.PtProjectTestFolder + "SOTest.sfm").ToLower(),
				m_DummyImportWizard.FileListBuilderCurrentListView.Items[0].Text.ToLower());

			Assert.AreEqual("TIT, JUD",
				m_DummyImportWizard.FileListBuilderCurrentListView.Items[0].SubItems[1].Text);

			// Move to next step so we can test the back button.
			m_DummyImportWizard.NextButtonPerformClick();
			m_DummyImportWizard.BackButtonPerformClick();

//			Assert.AreEqual(1, m_DummyImportWizard.FileListBuilderCurrentListView.Items.Count);
			Assert.AreEqual(true, m_DummyImportWizard.NextButtonEnabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate that the mappings step gets the correct data for the mappings list view
		/// when Paratext 6 projects are being used.
		/// </summary>
		/// <remarks>
		/// 1) Test is currently failing because notes domain is not being correctly assigned
		/// when marker is assigned to style with annotation context.
		/// 2) Need to remove mappings when a previously specified Paratext project is reset
		/// back to nothing (or changed to a different project). Test is not currently coded
		/// for this.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyMappingsFromParatext()
		{
			CheckDisposed();
			// Move to Project type step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Choose paratext project type.
			m_DummyImportWizard.Paratext6.Checked = true;

			// Move to project location step.
			m_DummyImportWizard.NextButtonPerformClick();

			while (m_DummyImportWizard.CboPTLangProj.Text != "PREDISTRIBUTION Today's English Version (USFM)")
				m_DummyImportWizard.CboPTLangProj.SelectedIndex++;

			// Move to the mappings step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Make sure there are markers in the mappings list match number from
			// paratext project and that they are in sorted order (\id should be omitted).
			int cMappingsTEV = ((ScrMappingList)m_DummyImportWizard.ScrImportSet.Mappings(MappingSet.Main)).Count - 1;
			Assert.AreEqual(cMappingsTEV, m_DummyImportWizard.Mappings.Items.Count);
			Assert.AreEqual(@"\add", m_DummyImportWizard.Mappings.Items[0].Text);
			Assert.AreEqual("Supplied", m_DummyImportWizard.Mappings.Items[0].SubItems[1].Text);
			Assert.AreEqual("Scripture", m_DummyImportWizard.Mappings.Items[0].SubItems[2].Text);
			Assert.AreEqual(@"\xt", m_DummyImportWizard.Mappings.Items[cMappingsTEV - 1].Text);

			// Make sure the markers in the list are tagged correctly.
			int cDefault = 0;
			int cNote = 0;
			foreach (ListViewItem lvi in m_DummyImportWizard.Mappings.Items)
			{
				if (MarkerDomain.Default == ((ImportMappingInfo)lvi.Tag).Domain)
					cDefault++;
				else if (MarkerDomain.Note == ((ImportMappingInfo)lvi.Tag).Domain)
					cNote++;
				else
					Assert.Fail("Unexpected domain type in mappings");
			}

			Assert.AreEqual(cMappingsTEV - 1, cDefault);
			Assert.AreEqual(1, cNote);

			// Move back to the project location step.
			m_DummyImportWizard.BackButtonPerformClick();

			m_DummyImportWizard.CboPTLangProj.SelectedIndex = 0;
			while (m_DummyImportWizard.CboPTLangProj.Text != "Kamwe")
				m_DummyImportWizard.CboPTLangProj.SelectedIndex++;

			// Move to the mappings step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Make sure number of markers in the mappings list matches Paratext project,
			// (\id excluded)
			int cMappingsKamwe = ((ScrMappingList)m_DummyImportWizard.ScrImportSet.Mappings(MappingSet.Main)).Count - 1;
			Assert.AreEqual(cMappingsKamwe, m_DummyImportWizard.Mappings.Items.Count);

			// Move back to the project location step.
			m_DummyImportWizard.BackButtonPerformClick();

			m_DummyImportWizard.CboPTLangProj.SelectedIndex = 0;
			while (m_DummyImportWizard.CboPTLangProj.Text != "Kamwe")
				m_DummyImportWizard.CboPTLangProj.SelectedIndex++;

			m_DummyImportWizard.CboPTBackTrans.SelectedIndex = 0;
			while (m_DummyImportWizard.CboPTBackTrans.Text != "PREDISTRIBUTION Today's English Version (USFM)")
				m_DummyImportWizard.CboPTBackTrans.SelectedIndex++;

			// Move to the mappings step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Make sure there are right number of markers in the mappings list
			// (\id should be omitted).
			int cMappings = ((ScrMappingList)m_DummyImportWizard.ScrImportSet.Mappings(MappingSet.Main)).Count - 1;
			Assert.AreEqual(cMappings, m_DummyImportWizard.Mappings.Items.Count);
			Assert.IsTrue(cMappings > cMappingsKamwe);
			Assert.IsTrue(cMappings > cMappingsTEV);
			Assert.AreEqual(@"\add", m_DummyImportWizard.Mappings.Items[0].Text);
			Assert.AreEqual("Supplied", m_DummyImportWizard.Mappings.Items[0].SubItems[1].Text);
			Assert.AreEqual("Back Translation", m_DummyImportWizard.Mappings.Items[0].SubItems[2].Text);
			Assert.AreEqual(@"\b", m_DummyImportWizard.Mappings.Items[1].Text);
			Assert.AreEqual("Stanza Break", m_DummyImportWizard.Mappings.Items[1].SubItems[1].Text);
			Assert.AreEqual("Scripture", m_DummyImportWizard.Mappings.Items[1].SubItems[2].Text);
			Assert.AreEqual(@"\xt", m_DummyImportWizard.Mappings.Items[cMappings - 1].Text);
			Assert.AreEqual("Back Translation", m_DummyImportWizard.Mappings.Items[cMappings - 1].SubItems[2].Text);

			// Make sure the markers in the list have appropriate domains.
			Assert.AreEqual(MarkerDomain.BackTrans,
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[0].Tag).Domain);
			Assert.AreEqual(MarkerDomain.Default,
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[1].Tag).Domain);
			Assert.AreEqual(MarkerDomain.BackTrans,
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[cMappings - 1].Tag).Domain);

			// Move back to the project location step.
			m_DummyImportWizard.BackButtonPerformClick();

			// Select no back translation.
			m_DummyImportWizard.CboPTBackTrans.SelectedIndex = 0;

			// Verify the language project is still correct.
			Assert.AreEqual("Kamwe", m_DummyImportWizard.CboPTLangProj.Text);

			// Move to the mappings step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Make sure all the back translation items are now removed by verifying the
			// markers in the mappings list and that they are in sorted order and
			// that their type is 'Scripture' (\id should be omitted).
			Assert.AreEqual(cMappingsKamwe, m_DummyImportWizard.Mappings.Items.Count);
			Assert.AreEqual(@"\b", m_DummyImportWizard.Mappings.Items[0].Text);
			Assert.AreEqual("Stanza Break", m_DummyImportWizard.Mappings.Items[0].SubItems[1].Text);
			Assert.AreEqual("What should this say",
				m_DummyImportWizard.Mappings.Items[0].SubItems[2].Text);
			Assert.AreEqual(@"\x", m_DummyImportWizard.Mappings.Items[cMappingsKamwe - 1].Text);

			// Make sure the markers in the list are tagged as scripture projects. Also make
			// sure the \id marker is not in the list.
			cDefault = 0;
			cNote = 0;
			foreach (ListViewItem lvi in m_DummyImportWizard.Mappings.Items)
			{
				if (MarkerDomain.Default == ((ImportMappingInfo)lvi.Tag).Domain)
					cDefault++;
				else if (MarkerDomain.Note == ((ImportMappingInfo)lvi.Tag).Domain)
					cNote++;
				Assert.IsTrue(@"\id" != lvi.Text, "Marker '\\id' should not be in list.");
			}
			Assert.AreEqual(cMappingsKamwe - 1, cDefault);
			Assert.AreEqual(1, cNote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate that the mappings step gets the correct data for the mappings list view
		/// when using SF files.
		/// </summary>
		/// <remarks>Fixes we need from DanH to make this test work:
		/// 1) Return the TE style name from the ECObject
		/// 2) Standard markers should default to appropriate TE style names.
		/// 3) Standard markers should default to appropriate domain and encoding
		/// 4) Non-standard markers should default to Scripture domain and vernacular enc.
		/// 5) Need to consider removing mappings when files are removed from the list, but
		/// we need to figure out how to do this in a way that balances performance with
		/// reliability. (It's probably better to have an occasional hangover marker than
		/// kill the user with a complete rescan of all files every time one is removed.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyMappingsFromOtherSF()
		{
			CheckDisposed();
			// Move to Project type step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Choose other project type.
			m_DummyImportWizard.OtherButton.Checked = true;

			// Move to project location step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Add our test file to the file list builder.
			m_DummyImportWizard.AddFiles(new string[] {Unpacker.PtProjectTestFolder + "SOTest.sfm"});

			// Move to the mappings step.
			m_DummyImportWizard.NextButtonPerformClick();

			// Make sure there are 13 markers in the mappings list and that they are in sorted
			// order (\id should not be in the list).
			Assert.AreEqual(13,
				m_DummyImportWizard.Mappings.Items.Count);
			Assert.AreEqual(@"\c",
				m_DummyImportWizard.Mappings.Items[0].Text.ToLower());
			Assert.AreEqual("chapter number",
				m_DummyImportWizard.Mappings.Items[0].SubItems[1].Text.ToLower());
			Assert.AreEqual("Scripture",
				m_DummyImportWizard.Mappings.Items[0].SubItems[2].Text);

			Assert.AreEqual(@"\e",
				m_DummyImportWizard.Mappings.Items[1].Text);
			Assert.AreEqual(string.Empty,
				m_DummyImportWizard.Mappings.Items[1].SubItems[1].Text);
			Assert.AreEqual("Scripture",
				m_DummyImportWizard.Mappings.Items[1].SubItems[2].Text);
			Assert.AreEqual(@"\v",
				m_DummyImportWizard.Mappings.Items[12].Text);
			Assert.AreEqual("verse number",
				m_DummyImportWizard.Mappings.Items[12].SubItems[1].Text.ToLower());
			Assert.AreEqual("Scripture",
				m_DummyImportWizard.Mappings.Items[12].SubItems[2].Text);

			// ENHANCE TeTeam: We need more thorough tests of ImportMappingInfo properties
			// for domains other than vernacular.
			foreach (ListViewItem lvi in m_DummyImportWizard.Mappings.Items)
			{
				ImportMappingInfo mapping = (ImportMappingInfo)lvi.Tag;
				Assert.AreEqual("Scripture", lvi.SubItems[2].Text);
				Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
				Assert.IsNull(mapping.IcuLocale);

				// Make sure the \id isn't included in the list of markers since that's expected
				// to be the marker that begins a book and thus, it shouldn't be allowed to be
				// mapped to anything else.
				Assert.IsTrue(@"\id" != lvi.Text, "Marker '\\id' should not be in list.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ModifyParatextMappingsTest()
		{
			CheckDisposed();
			// Test 1: test the cancel button

			// Move to Mappings step for the TEV scripture project.
			m_DummyImportWizard.NextButtonPerformClick();
			m_DummyImportWizard.Paratext6.Checked = true;
			m_DummyImportWizard.NextButtonPerformClick();

			// Choose TEV for the scripture project and Kamwe as the back translation project.
			m_DummyImportWizard.CboPTLangProj.SelectedItem =
				"PREDISTRIBUTION Today's English Version (USFM)";
			m_DummyImportWizard.CboPTBackTrans.SelectedItem = "Kamwe";

			// Move to Mappings step.
			m_DummyImportWizard.NextButtonPerformClick();

			// modify dialog
			m_DummyImportWizard.ShouldAccept = false;

			Assert.IsTrue(m_DummyImportWizard.Mappings.Items.Count > 0, "No mappings found");
			string original = m_DummyImportWizard.Mappings.Items[0].SubItems[1].Text;
			m_DummyImportWizard.ModifyButton.PerformClick();

			Application.DoEvents();
			Assert.IsFalse(m_DummyImportWizard.MappingDialog.Visible,
				"Dialog didn't close properly.");
			Assert.AreEqual(original, m_DummyImportWizard.Mappings.Items[0].SubItems[1].Text,
				"Cancel button doesn't work! ");

			// Test 2: test that the right dialog box gets created and that stuff is changed
			// modify dialog
			m_DummyImportWizard.ShouldAccept = true;

			// Choose only item 3 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[3].Selected = true;

			// Choose the "Intro Table Row" style
			m_DummyImportWizard.StyleSelection = "Intro Table Row";

			// Open modify dialog
			m_DummyImportWizard.ModifyButton.PerformClick();
			Application.DoEvents();
			Assert.IsTrue(m_DummyImportWizard.MappingDialog is ModifyMapping,
				"MappingDialog is of the wrong type");
			Assert.IsFalse(m_DummyImportWizard.MappingDialog.Visible,
				"Dialog didn't close properly.");

			// Make sure "Intro Table Row" was chosen in the dialog.
			Assert.AreEqual(
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.m_styleListHelper.SelectedStyleName,
				"Intro Table Row");

			// Check the ListView Item's contents.
			Assert.AreEqual("Intro Table Row",
				m_DummyImportWizard.Mappings.Items[3].SubItems[1].Text,
				"Value not correctly changed: test 2");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("Intro Table Row",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).StyleName,
				"Value not correctly changed: test 2");

			// Choose only item 10 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[10].Selected = true;

			// Choose the "Speech Line2" style
			m_DummyImportWizard.StyleSelection = "Speech Line2";

			// Open modify mappings dialog.
			m_DummyImportWizard.ModifyButton.PerformClick();
			Application.DoEvents();
			Assert.IsTrue(
				m_DummyImportWizard.MappingDialog is ModifyMapping,
				"MappingDialog is of the wrong type");
			Assert.IsFalse(m_DummyImportWizard.MappingDialog.Visible,
				"Dialog didn't close properly.");

			// Make sure "Speech Line2" was chosen in the dialog.
			Assert.AreEqual(
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.m_styleListHelper.SelectedStyleName,
				"Speech Line2");

			// Check the ListView Item's contents.
			Assert.AreEqual("Speech Line2",
				m_DummyImportWizard.Mappings.Items[10].SubItems[1].Text,
				"Value not correctly changed: test 2");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("Speech Line2",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[10].Tag).StyleName,
				"Value not correctly changed: test 2");

			// Save changes to database
			m_DummyImportWizard.SimulateFinish();
			m_DummyImportWizard.Close();

			m_DummyImportWizard = new DummyImportWizard(m_cache, (Scripture)m_Scripture, m_styleSheet);

			m_DummyImportWizard.Show();
			Application.DoEvents();

			// Move to Mappings step for the TEV scripture project.
			m_DummyImportWizard.NextButtonPerformClick();
			Assert.IsTrue(m_DummyImportWizard.Paratext6.Checked, "Paratext 6 should be selected");
			m_DummyImportWizard.NextButtonPerformClick();
			Application.DoEvents();
			Assert.AreEqual(
				"PREDISTRIBUTION Today's English Version (USFM)",
				m_DummyImportWizard.CboPTLangProj.Text,
				"Settings not loaded from database.");
			Assert.AreEqual("Kamwe", m_DummyImportWizard.CboPTBackTrans.Text,
				"Settings not loaded from database.");
			Assert.AreEqual("(none)", m_DummyImportWizard.CboPTTransNotes.Text,
				"Settings not loaded from database.");

			m_DummyImportWizard.NextButtonPerformClick();

			// Check the ListView Item's contents after having read settings from DB.
			Assert.AreEqual("Speech Line2",
				m_DummyImportWizard.Mappings.Items[10].SubItems[1].Text,
				"Value wasn't saved in DB");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("Speech Line2",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[10].Tag).StyleName,
				"Value wasn't saved in DB");

			// Check the ListView Item's contents after having read settings from DB.
			Assert.AreEqual("Intro Table Row",
				m_DummyImportWizard.Mappings.Items[3].SubItems[1].Text,
				"Value wasn't saved in DB");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("Intro Table Row",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).StyleName,
				"Value wasn't saved in DB");

			m_DummyImportWizard.StyleSelection = string.Empty;
			m_DummyImportWizard.WsSelection = string.Empty;
			m_DummyImportWizard.RadioboxSelection = -1;
			m_DummyImportWizard.ShouldAccept = true;

			// Choose only item 10 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[10].Selected = true;

			m_DummyImportWizard.ModifyButton.PerformClick();

			// Make sure "Speech Line2" was chosen in the dialog.
			Assert.AreEqual("Speech Line2",
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.m_styleListHelper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ModifyOtherMappingsTest()
		{
			CheckDisposed();
			m_DummyImportWizard.Activate();
			// Test 1: test the cancel button
			// Move to Project type step.
			m_DummyImportWizard.NextButtonPerformClick();
			// Choose other project type.
			m_DummyImportWizard.OtherButton.Checked = true;
			// Move to project location step.
			m_DummyImportWizard.NextButtonPerformClick();
			// Add our test file to the file list builder.
			m_DummyImportWizard.AddFiles(new string[] {Unpacker.PtProjectTestFolder + "SOTest.sfm"});
			// Move to the mappings step.
			Application.DoEvents();
			m_DummyImportWizard.NextButtonPerformClick();
			// modify dialog
			m_DummyImportWizard.ShouldAccept = false;
			Application.DoEvents();

			// Choose only item 11 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[10].Selected = true;

			Assert.IsTrue(m_DummyImportWizard.Mappings.Items.Count > 0, "No mappings found");
			string original = m_DummyImportWizard.Mappings.Items[10].SubItems[1].Text;
			Assert.IsTrue(m_DummyImportWizard.ModifyButton.Enabled,
				"Modify button not enabled.");
			m_DummyImportWizard.ModifyButton.PerformClick();

			Application.DoEvents();
			Assert.IsFalse(m_DummyImportWizard.MappingDialog.Visible,
				"Dialog didn't close properly.");
			Assert.AreEqual(original, m_DummyImportWizard.Mappings.Items[10].SubItems[1].Text,
				"Cancel button doesn't work!");

			// Test 2: test that the right dialog box gets created and that stuff is changed
			// modify dialog
			m_DummyImportWizard.ShouldAccept = true;

			// Choose only item 3 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[3].Selected = true;

			// Choose the "List Item1" style, French writing system, and vernacular domain
			m_DummyImportWizard.StyleSelection = "List Item1";
			m_DummyImportWizard.WsSelection = "French";
			m_DummyImportWizard.RadioboxSelection = 0;

			// Open modify dialog
			m_DummyImportWizard.ModifyButton.PerformClick();
			Application.DoEvents();
			Assert.AreEqual(typeof(ModifyMapping),
				m_DummyImportWizard.MappingDialog.GetType(),
				"MappingDialog is of the wrong type");
			m_DummyImportWizard.MappingDialog.CancelButton.PerformClick();
			Assert.IsFalse(m_DummyImportWizard.MappingDialog.Visible,
				"Dialog didn't close properly.");

			// Make sure "List Item1" was chosen in the dialog.
			Assert.AreEqual("List Item1",
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.m_styleListHelper.SelectedStyleName);

			// Check the ListView Item's contents.
			Assert.AreEqual("List Item1",
				m_DummyImportWizard.Mappings.Items[3].SubItems[1].Text,
				"Value not correctly changed: test 2");

			Assert.AreEqual("scripture, french",
				m_DummyImportWizard.Mappings.Items[3].SubItems[2].Text.ToLower(),
				"Value not correctly changed: test 2");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("List Item1",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).StyleName,
				"Value not correctly changed: test 2");

			Assert.AreEqual(MarkerDomain.Default,
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).Domain,
				"Value not correctly changed: test 2");

//			// Check to make sure the data encoding was changed
//			Assert.AreEqual(kDummyVernLegacyMapping,
//				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).DataEncoding,
//				"Value not correctly changed: test 2");

			// Choose only item 10 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[10].Selected = true;

			// Choose the "Speech Line2" style, German writing system, and back translation domain
			m_DummyImportWizard.StyleSelection = "Speech Line2";
			m_DummyImportWizard.WsSelection = "German";
			m_DummyImportWizard.BackTranslation = true;

			m_DummyImportWizard.ModifyButton.PerformClick();
			Application.DoEvents();
			Assert.IsTrue(m_DummyImportWizard.MappingDialog is ModifyMapping,
				"MappingDialog is of the wrong type");
			Assert.IsFalse(m_DummyImportWizard.MappingDialog.Visible,
				"Dialog didn't close properly.");

			// Make sure "Speech Line2" was chosen in the dialog.
			Assert.AreEqual("Speech Line2",
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.m_styleListHelper.SelectedStyleName);

			// Check the ListView Item's contents.
			Assert.AreEqual("Speech Line2",
				m_DummyImportWizard.Mappings.Items[10].SubItems[1].Text,
				"Value not correctly changed: test 2");

			Assert.AreEqual("back translation, german",
				m_DummyImportWizard.Mappings.Items[10].SubItems[2].Text.ToLower(),
				"Value not correctly changed: test 2");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("Speech Line2",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[10].Tag).StyleName,
				"Value not correctly changed: test 2");

			Assert.AreEqual(MarkerDomain.BackTrans,
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[10].Tag).Domain,
				"Value not correctly changed: test 2");

			// Check to make sure the data encoding was changed
			Assert.AreEqual("de",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[10].Tag).IcuLocale,
				"Value not correctly changed: test 2");

			// Save changes to database
			m_DummyImportWizard.SimulateFinish();

			// Close and re-open dialog to force all it's internal data to be reinitialized.
			m_DummyImportWizard.Close();
			m_DummyImportWizard = new DummyImportWizard(m_cache, (Scripture)m_Scripture, m_styleSheet);
			m_DummyImportWizard.Show();
			Application.DoEvents();

			// Move to Mappings step for the TEV scripture project.
			m_DummyImportWizard.NextButtonPerformClick();
			m_DummyImportWizard.OtherButton.Checked = true;
			m_DummyImportWizard.NextButtonPerformClick();

			// We should be on the wizard step where files can be added to the
			// list of files to import. Check that the list has one file name.
			Assert.AreEqual((Unpacker.PtProjectTestFolder + "SOTest.sfm").ToLower(),
				((string)m_DummyImportWizard.FileListBuilder.ScriptureFiles[0]).ToLower(),
				"Value wasn't saved in DB");

			// Before getting to the mappings step of the wizard, get the main title mapping
			// directly from the dialog's import settings. Then verify that its domain and
			// ICU locale properties are set properly. This mapping shouldn't
			// get changed by going to the modify mappings dialog.
			ImportMappingInfo mapping;
			mapping = m_DummyImportWizard.ScrImportSet.MappingForMarker(@"\mt", MappingSet.Main);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain, @"Domain for \mt is incorrect");
			Assert.IsNull(mapping.IcuLocale);

			// Move to the mappings step of the wizard.
			m_DummyImportWizard.NextButtonPerformClick();

			// Check the mapping ListView Item's contents.
			Assert.AreEqual("List Item1",
				m_DummyImportWizard.Mappings.Items[3].SubItems[1].Text,
				"Value not correctly changed: test 2");

			Assert.AreEqual("scripture, french",
				m_DummyImportWizard.Mappings.Items[3].SubItems[2].Text.ToLower(),
				"Value not correctly changed: test 2");

			// Check the content of the ECMappings object pointed to by the ListView Item.
			Assert.AreEqual("List Item1",
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).StyleName,
				"Value not correctly changed: test 2");

			Assert.AreEqual(MarkerDomain.Default,
				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).Domain,
				"Value not correctly changed: test 2");

//			// Check to make sure the data encoding was changed
//			Assert.AreEqual(kDummyVernLegacyMapping,
//				((ImportMappingInfo)m_DummyImportWizard.Mappings.Items[3].Tag).DataEncoding,
//				"Value not correctly changed: test 2");

			// Check the ListView Item's contents.
			Assert.AreEqual("Speech Line2",
				m_DummyImportWizard.Mappings.Items[10].SubItems[1].Text,
				"Value not correctly changed: test 2");

			Assert.AreEqual("back translation, german",
				m_DummyImportWizard.Mappings.Items[10].SubItems[2].Text.ToLower(),
				"Value not correctly changed: test 2");

			// Check the content of the ImportMappingInfo object pointed to by the ListView Item.
			ImportMappingInfo info = (ImportMappingInfo)m_DummyImportWizard.Mappings.Items[10].Tag;
			Assert.AreEqual("Speech Line2", info.StyleName, "StyleName value not correctly changed: test 2");
			Assert.AreEqual(MarkerDomain.BackTrans, info.Domain, "Domain value not correctly changed: test 2");
			Assert.AreEqual("de", info.IcuLocale, "ICU locale value not correctly changed: test 2");

			m_DummyImportWizard.StyleSelection = string.Empty;
			m_DummyImportWizard.WsSelection = string.Empty;
			m_DummyImportWizard.RadioboxSelection = -1;
			m_DummyImportWizard.ShouldAccept = true;

			// Choose only item 10 in mappings ListView.
			m_DummyImportWizard.Mappings.SelectedItems.Clear();
			m_DummyImportWizard.Mappings.Items[10].Selected = true;

			m_DummyImportWizard.ModifyButton.PerformClick();

			// Make sure "Speech Line2" was chosen in the dialog.
			Assert.AreEqual("Speech Line2",
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.m_styleListHelper.SelectedStyleName);

			Assert.AreEqual("de",
				((ModifyMapping)m_DummyImportWizard.MappingDialog).mappingDetailsCtrl.WritingSystem);

			Assert.IsTrue(
				((ModifyMapping)m_DummyImportWizard.MappingDialog).Scripture.Checked);

			Assert.IsFalse(
				((ModifyMapping)m_DummyImportWizard.MappingDialog).Footnotes.Checked);

			Assert.IsFalse(
				((ModifyMapping)m_DummyImportWizard.MappingDialog).Notes.Checked);
		}
	}
}
