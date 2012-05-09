// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportWizardTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Paratext;
using Rhino.Mocks;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.FDO.DomainServices;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using Paratext.DerivedTranslation;

namespace SIL.FieldWorks.TE.ImportComponentsTests
{
	#region ImportWizard wrapper class to access protected members
	internal class ImportWizardWrapper : ImportWizard
	{
		#region DummyCharacterMappingSettings class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// This class is used to test the CharacterMappingSettings dialog
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		internal class DummyCharacterMappingSettings : CharacterMappingSettings
		{
			private bool m_MappingInvalidWarningHappened = false;
			/// <summary>
			/// Info to store in the mapping if the simulated user clicks the simulated OK button.
			/// </summary>
			public ImportMappingInfo m_MappingDialogDummyData = null;

			#region Constructor
			/// -------------------------------------------------------------------------------------
			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="mapping">Provides intial values displayed in dialog.</param>
			/// <param name="styleSheet">Provides the character styles user can pick from.</param>
			/// <param name="cache">The DB cache</param>
			/// -------------------------------------------------------------------------------------
			public DummyCharacterMappingSettings(ImportMappingInfo mapping, FwStyleSheet styleSheet,
				FdoCache cache) : base(mapping, styleSheet, cache, false, null, null)
			{
				// No code needed so far.
			}
			#endregion

			#region members of CharacterMappingSettings class exposed by dummy
			/// -------------------------------------------------------------------------------------
			/// <summary>
			/// This exposes the BeginningMarker test box so that test values can be entered
			/// </summary>
			/// -------------------------------------------------------------------------------------
			public TextBox BeginningTextBox
			{
				get
				{
					CheckDisposed();
					return txtBeginningMarker;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This exposes the EndingMarker test box so that test values can be entered
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public TextBox EndingTextBox
			{
				get
				{
					CheckDisposed();
					return txtEndingMarker;
				}
			}
			#endregion

			#region Properties needed for testing
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets indication of whether or not the test simulation of CharacterMappingSettings
			/// dialog caused a Mapping Invalid warning.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public bool MappingInvalidWarningHappened
			{
				get
				{
					CheckDisposed();
					return m_MappingInvalidWarningHappened;
				}
				set
				{
					CheckDisposed();
					m_MappingInvalidWarningHappened = value;
				}
			}
			#endregion

			#region Overrides
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Overridden to prevent display of a message box
			/// </summary>
			/// <param name="msg"></param>
			/// ------------------------------------------------------------------------------------
			protected override void DisplayInvalidMappingWarning(String msg)
			{
				//set a flag
				m_MappingInvalidWarningHappened = true;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Saves the mapping details to the mapping stored in m_mapping.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected override void SaveMapping()
			{
				m_mapping.BeginMarker = txtBeginningMarker.Text;
				m_mapping.EndMarker = txtEndingMarker.Text;
				m_mapping.Domain = m_MappingDialogDummyData.Domain;
				m_mapping.WsId = m_MappingDialogDummyData.WsId;
				m_mapping.IsExcluded = m_MappingDialogDummyData.IsExcluded;
				m_mapping.IsInUse = m_MappingDialogDummyData.IsInUse;
				m_mapping.MappingTarget = m_MappingDialogDummyData.MappingTarget;
				m_mapping.NoteType = m_MappingDialogDummyData.NoteType;
				m_mapping.StyleName = m_MappingDialogDummyData.StyleName;
			}
			#endregion
		}
		#endregion

		#region member variables
		private ImportMappingInfo m_MappingDialogDummyData;
		private bool m_MappingDialogDummyAccept;
		private bool m_fDisposeInlineMappingDialog;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="scr">The Scripture object.</param>
		/// <param name="styleSheet">The styleSheet</param>
		/// ------------------------------------------------------------------------------------
		public ImportWizardWrapper(IScripture scr, FwStyleSheet styleSheet) :
			base("Test", scr, styleSheet, null, null)
		{
			m_lvCurrentMappingList = lvScrMappings;
			m_btnCurrentModifyButton = m_btnModifyScrMapping;
			m_btnCurrentDeleteButton = m_btnDeleteScrMapping;
			m_btnCurrentAddButton = m_btnAddScrMapping;
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_fDisposeInlineMappingDialog && m_inlineMappingDialog != null)
					m_inlineMappingDialog.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to LoadMappingsFromSettings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallLoadMappingsFromSettings()
		{
			CheckDisposed();

			LoadMappingsFromSettings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user clicking the delete button. Can't use perform click because the
		/// button may not be visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClickDeleteMappingButton()
		{
			CheckDisposed();

			if (!m_btnCurrentDeleteButton.Enabled)
				throw new Exception("Delete button not enabled");
			base.btnDelete_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user clicking the add button. Can't use perform click because the
		/// button may not be visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClickAddMappingButton()
		{
			CheckDisposed();

			if (!m_btnCurrentAddButton.Enabled)
				throw new Exception("Add button not enabled");
			base.btnAdd_Click(null, null);
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to Scripture mappings list view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwListView ScrMappings
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
		/// Provides access to the Enabled state of the Modify Scr Mapping button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ModifyScrMappingButtonEnabled
		{
			get
			{
				CheckDisposed();
				return m_btnModifyScrMapping.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to the Enabled state of the Delete Scr Mapping button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DeleteScrMappingButtonEnabled
		{
			get
			{
				CheckDisposed();
				return m_btnDeleteScrMapping.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to the Enabled state of the Modify Note Mapping button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ModifyNoteMappingButtonEnabled
		{
			get
			{
				CheckDisposed();
				return m_btnModifyNoteMapping.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to the Enabled state of the Delete Note Mapping button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DeleteNoteMappingButtonEnabled
		{
			get
			{
				CheckDisposed();
				return m_btnDeleteNoteMapping.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a value indicating whether to show all mappings, or only those in use.
		/// </summary>
		/// <value><c>true</c> to show all mappings; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ShowAllMappings
		{
			set
			{
				CheckDisposed();

				m_fShowAllMappings = value;
				LoadMappingsFromSettings();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the mapping data that will be the retrieved value of the simulated
		/// CharacterMappingSettings dialog when the overridden displayMappingDialog is called.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo MappingDialogDummyData
		{
			get
			{
				CheckDisposed();

				return m_MappingDialogDummyData;
			}
			set
			{
				CheckDisposed();

				m_MappingDialogDummyData = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets indication of whether or not the test simulation of CharacterMappingSettings
		/// dialog caused a Mapping Invalid warning.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MappingInvalidWarningHappened
		{
			get
			{
				CheckDisposed();

				if (m_inlineMappingDialog == null)
					return false; // If the dialog was never created, it couldn't have had any validation errors now, could it?
				return ((DummyCharacterMappingSettings)m_inlineMappingDialog).MappingInvalidWarningHappened;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Ok/Cancel choice for use in CharacterMappingSettings dialog.
		/// If true, the mapping dialog should simulate the OK button. If false, the Cancel
		/// button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MappingDialogDummyAccept
		{
			set
			{
				CheckDisposed();

				m_MappingDialogDummyAccept = value;
			}
		}
		#endregion

		#region overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates (if necessary) and shows the CharacterMappingSettings dialog.
		/// This is a public virtual so that test code can override it.
		/// </summary>
		/// <param name="mapping">Provides intial values displayed in dialog.</param>
		/// ------------------------------------------------------------------------------------
		protected override void DisplayInlineMappingDialog(ImportMappingInfo mapping)
		{
			if (m_inlineMappingDialog == null)
			{
				m_inlineMappingDialog = new DummyCharacterMappingSettings(mapping, m_StyleSheet, m_cache);
				m_fDisposeInlineMappingDialog = true;
				m_inlineMappingDialog.IsDuplicateMapping += new DummyCharacterMappingSettings.IsDuplicateMappingHandler(IsDup);
			}
			else
			{
				m_inlineMappingDialog.InitializeControls(mapping, false);
				// clear warningHappened flag here in our dummy dialog
				((DummyCharacterMappingSettings)m_inlineMappingDialog).MappingInvalidWarningHappened =
					false;
				// This is pretty dumb, but we have to set it to cancel by default for testing purposes
				// because if the test asked for the OK button to be pressed but a validation error
				// occurs, we'll still pretend to close the dialog. If we return OK (as the user
				// requested), the subsequent code will think everything was okay.
				m_inlineMappingDialog.DialogResult = DialogResult.Cancel;
			}

			DummyCharacterMappingSettings mappingDlg = (DummyCharacterMappingSettings)m_inlineMappingDialog;
			if (m_MappingDialogDummyAccept)
			{
				mappingDlg.BeginningTextBox.Text = m_MappingDialogDummyData.BeginMarker;
				mappingDlg.EndingTextBox.Text = m_MappingDialogDummyData.EndMarker;
				mappingDlg.m_MappingDialogDummyData = m_MappingDialogDummyData;

				// fake the ok button being pressed.
				m_inlineMappingDialog.btnOk_Click(null, null);
			}
			else
				m_inlineMappingDialog.DialogResult = DialogResult.Cancel;
		}
		#endregion
	}
	#endregion

	#region class ImportWizardTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the Modify Other Mappings dialog as accessed from the mapping page of the import
	/// wizard
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportWizardTests: ScrInMemoryFdoTestBase
	{
		#region Data members
		private readonly MockParatextHelper m_ptHelper = new MockParatextHelper();
		private IScrImportSet m_settings;
		private ImportWizardWrapper m_importWizard;
		private FwStyleSheet m_styleSheet;

		private RegistryData m_regData;
		#endregion

		#region Test Data creation, test setup, etc.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Squeezes in the mock ParatextHelper in place of the real adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			ParatextHelper.Manager.SetParatextHelperAdapter(m_ptHelper);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			ParatextHelper.Manager.Reset();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates test data and initializes the DummySFFileListBuilder so we can test it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Other);

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the tests
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_importWizard = new ImportWizardWrapper(m_scr, m_styleSheet);
			MethodInfo createHandle = m_importWizard.ScrMappings.GetType().GetMethod("CreateHandle",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			createHandle.Invoke(m_importWizard.ScrMappings, null);
			m_settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			m_ptHelper.Projects.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_settings = null;
			m_importWizard.Dispose();
			m_importWizard = null;
			m_styleSheet = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the mock Paratext proxy to simulate validity of any requested projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupMockParatextProxy()
		{
			IParatextAdapter mockParatextAdapter = MockRepository.GenerateMock<IParatextAdapter>();
			ReflectionHelper.SetField(m_settings, "m_paratextAdapter", mockParatextAdapter);
			mockParatextAdapter.Stub(x => x.LoadProjectMappings(Arg<string>.Is.Anything,
				Arg<ScrMappingList>.Is.Anything, Arg<ImportDomain>.Is.Anything)).Return(true);
		}
		#endregion

		#region Mapping List display tests
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the book marker (\id) doesn't get displayed in the mapping list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoNotDisplayBookmarkerInMappingList()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\p", null, ScrStyleNames.NormalParagraph));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			Assert.AreEqual(1, m_importWizard.ScrMappings.Items.Count);
			Assert.AreEqual(@"\p", m_importWizard.ScrMappings.Items[0].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that all markers except \id, including in-line markers, are displayed in the
		/// mapping list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyMappingListDisplay()
		{
			//fill the list and show the dialog
			// typical backslash mappings
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\xref", null, "Cross-Reference"));

			// in-line mappings
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("emph{", "}", "Emphasized"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|b", "|r", "Bold"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("<I>", "</I>", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("impl{", "}", "Alluded Text"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("f:Indo{", "}", "wsIndonesian"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			// verify that the list was populated and sorted correctly
			Assert.AreEqual(9, m_importWizard.ScrMappings.Items.Count);
			Assert.AreEqual("/itw.../*itw", m_importWizard.ScrMappings.Items[0].Text, "List should be sorted");
			Assert.AreEqual(@"\c", m_importWizard.ScrMappings.Items[1].Text, "List should be sorted");
			Assert.AreEqual(@"\v", m_importWizard.ScrMappings.Items[2].Text, "List should be sorted");
			Assert.AreEqual("f:Indo{...}", m_importWizard.ScrMappings.Items[7].Text, "List should be sorted");
			Assert.AreEqual("impl{...}", m_importWizard.ScrMappings.Items[8].Text, "List should be sorted");

			//verify that only the first item is selected
			Assert.AreEqual(1, m_importWizard.ScrMappings.SelectedItems.Count);
			Assert.AreEqual(0, m_importWizard.ScrMappings.SelectedIndices[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the list of markers displayed changes based on whether or not the user
		/// has selected to view all or only those in use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilterMarkerListBasedOnInUse()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\inUse", null, true,
				MappingTargetType.TEStyle, MarkerDomain.Default, null, null, null, true, ImportDomain.Main));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\notInUse", null, true,
				MappingTargetType.TEStyle, MarkerDomain.Default, null, null, null, false, ImportDomain.Main));
			m_importWizard.CallLoadMappingsFromSettings();

			m_importWizard.ShowAllMappings = true;
			Assert.AreEqual(2, m_importWizard.ScrMappings.Items.Count);

			m_importWizard.ShowAllMappings = false;
			Assert.AreEqual(1, m_importWizard.ScrMappings.Items.Count);
			Assert.AreEqual(@"\inUse",
				((ImportMappingInfo)m_importWizard.ScrMappings.Items[0].Tag).BeginMarker);
		}
		#endregion

		#region Mapping list button state tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Modify and Delete buttons are disabled when no mapping is selected (list
		/// of mappings hasn't been populated)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ModifyAndDeleteButtonsDisabledForNoMappings()
		{
			Assert.IsFalse(m_importWizard.ModifyScrMappingButtonEnabled);
			Assert.IsFalse(m_importWizard.DeleteScrMappingButtonEnabled);
			Assert.IsFalse(m_importWizard.ModifyNoteMappingButtonEnabled);
			Assert.IsFalse(m_importWizard.DeleteNoteMappingButtonEnabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Modify and Delete buttons are disabled for chapter marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ModifyAndDeleteButtonsDisabledForChapterMarker()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, ScrStyleNames.ChapterNumber));
			m_importWizard.CallLoadMappingsFromSettings();
			Assert.IsFalse(m_importWizard.ModifyScrMappingButtonEnabled);
			Assert.IsFalse(m_importWizard.DeleteScrMappingButtonEnabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Modify and Delete buttons are disabled for verse marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ModifyAndDeleteButtonsDisabledForVerseMarker()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, ScrStyleNames.VerseNumber));
			m_importWizard.CallLoadMappingsFromSettings();
			Assert.IsFalse(m_importWizard.ModifyScrMappingButtonEnabled);
			Assert.IsFalse(m_importWizard.DeleteScrMappingButtonEnabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Modify button is enabled for other marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ModifyButtonEnabledForOtherMarker()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\x", null, true,
				MappingTargetType.TEStyle, MarkerDomain.Default, null, null, null, true, ImportDomain.Main));
			m_importWizard.CallLoadMappingsFromSettings();
			Assert.IsTrue(m_importWizard.ModifyScrMappingButtonEnabled);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Delete button is enabled for a marker that is not in use
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteButtonEnabledForMarkerNotInUse()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\x", null, true,
				MappingTargetType.TEStyle, MarkerDomain.Default, null, null, null, false, ImportDomain.Main));
			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;
			Assert.IsTrue(m_importWizard.DeleteScrMappingButtonEnabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Delete button is disabled for a marker that is in use
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteButtonDisabledForMarkerInUse()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\x", null, true,
				MappingTargetType.TEStyle, MarkerDomain.Default, null, null, null, true, ImportDomain.Main));
			m_importWizard.CallLoadMappingsFromSettings();
			Assert.IsFalse(m_importWizard.DeleteScrMappingButtonEnabled);
		}
		#endregion

		#region Checking Paratext Project
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Paratext project combo will not be modified when "none" is
		/// selected as a project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void CheckProjCombo_NoProjSelected()
		{
			m_ptHelper.AddProject("DUMY");

			ReflectionHelper.CallMethod(m_importWizard, "PrepareToGetParatextProjectSettings", null);

			// Set the combobox index to indicate that nothing is selected.
			ComboBox cbo = ReflectionHelper.GetField(m_importWizard, "cboPTBackTrans") as ComboBox;
			if (cbo != null)
				cbo.SelectedIndex = 0;
			// Set the current projName to null (indicating that nothing was loaded).
			string projName = null;

			ReflectionHelper.CallMethod(m_importWizard, "CheckProjectCombo",
				new object[] { projName, cbo as FwOverrideComboBox, ImportDomain.BackTrans });

			// We expect that nothing would be selected (and that the ParatextLoadException
			// would not be thrown).
			Assert.AreEqual(0, cbo.SelectedIndex,
				"The first item in the combobox (none) should have been selected.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Paratext project combo will not be modified when a valid Paratext
		/// project is selected in the combobox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void CheckProjCombo_ValidProjSelected()
		{
			m_ptHelper.AddProject("TEV");
			ReflectionHelper.CallMethod(m_importWizard, "PrepareToGetParatextProjectSettings", null);
			// Set the combobox index to the second Paratext project in the list (TEV).
			ComboBox cbo = ReflectionHelper.GetField(m_importWizard, "cboPTLangProj") as ComboBox;
			int originalIndex = cbo.FindString("TEV");
			cbo.SelectedIndex = originalIndex;
			FwOverrideComboBox projCombo = ReflectionHelper.GetField(
				m_importWizard, "cboPTLangProj") as FwOverrideComboBox;

			string projName = "TEV";

			ReflectionHelper.CallMethod(m_importWizard, "CheckProjectCombo",
				new object[] { projName, projCombo, ImportDomain.Main });

			// We expect that the selected index in the combobox would still be set to 1.
			Assert.AreEqual(originalIndex, projCombo.SelectedIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Paratext project combo will be modified when a Paratext project is
		/// selected in the combobox but it is not valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void CheckProjCombo_InvalidProjSelected()
		{
			// Set the combobox index to the second Paratext project in the list (NSF).
			ComboBox cbo = ReflectionHelper.GetField(m_importWizard, "cboPTLangProj") as ComboBox;
			// We have to manually add the projects because they could not be loaded.
			m_ptHelper.AddProject("NEC");
			m_ptHelper.AddProject("NSF");
			cbo.Items.AddRange(new object[] { m_ptHelper.Projects.Where(x => x.Name == "NEC").Single(),
				m_ptHelper.Projects.Where(x => x.Name == "NSF").Single() });

			cbo.SelectedIndex = 1;

			// Set the current projName to null (indicating that nothing was loaded because
			// there was a problem with the selected Paratext project).
			string projName = null;

			try
			{
				// We expect that a ParatextLoadException would be thrown.
				ReflectionHelper.CallMethod(m_importWizard, "CheckProjectCombo",
					new object[] { projName, cbo as FwOverrideComboBox, ImportDomain.Main });
				Assert.Fail("We expect this call to fail!");
			}
			catch (Exception e)
			{
				// Since we call the method by reflection, the outer exception will be
				// a System.Reflection.TargetInvocationException. We need to check the
				// inner exception to see if it is the correct type.
				Assert.AreEqual(typeof(ParatextLoadException), e.InnerException.GetType());
				Assert.AreEqual(1, cbo.SelectedIndex,
					"The index should still be on the second item in the combo.");
			}
		}
		#endregion

		#region Using associated linguistic and BT projects
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Paratext project combos will be disabled and hard-wired to the
		/// correct associated PT projects if associated projects exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void PrepareToGetParatextProjectSettingss_AssociatedLinguisticAndBtProj()
		{
			// Setup mocked Paratext projects
			SetupMockParatextProxy();

			m_ptHelper.AddProject("ABC", "Whatever");
			m_ptHelper.AddProject("YES", Cache.ProjectId.Handle);
			m_ptHelper.AddProject("ALM", null, "YES", true, false, "000110000000", DerivedTranslationType.Daughter);
			m_ptHelper.AddProject("BTP", null, "YES");
			m_ptHelper.AddProject("XYZ", null, "ABC");
			m_ptHelper.AddProject("MNKY", null, "YES");
			m_ptHelper.AddProject("SOUP", null, "YES", true, false, "000110000000", DerivedTranslationType.Daughter);

			m_settings.ParatextScrProj = "ABC";
			m_settings.ParatextBTProj = "XYZ";
			m_settings.ParatextNotesProj = null;

			Assert.IsTrue(ReflectionHelper.GetBoolResult(m_importWizard, "PrepareToGetParatextProjectSettings"));

			ComboBox cboScr = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTLangProj");
			ComboBox cboBT = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTBackTrans");
			ComboBox cboNotes = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTTransNotes");

			Assert.AreEqual(1, cboScr.Items.Count);
			Assert.AreEqual(0, cboScr.SelectedIndex);
			Assert.AreEqual(2, cboBT.Items.Count);
			Assert.AreEqual(0, cboBT.SelectedIndex);
			Assert.AreEqual(8, cboNotes.Items.Count);
			Assert.AreEqual(0, cboNotes.SelectedIndex);
			Assert.AreEqual("BTP", ((ScrText)cboBT.Items[0]).Name);
			Assert.AreEqual("MNKY", ((ScrText)cboBT.Items[1]).Name);
			Assert.IsFalse(cboScr.Enabled);
			Assert.IsTrue(cboBT.Enabled);
			Assert.IsTrue(cboNotes.Enabled);
			Assert.AreEqual("YES", m_settings.ParatextScrProj);
			Assert.AreEqual("BTP", m_settings.ParatextBTProj);
			Assert.IsNull(m_settings.ParatextNotesProj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Paratext vernacular Scripture project combo will be disabled and
		/// hard-wired to the correct associated PT vernacular project, which has no associated
		/// BT project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void PrepareToGetParatextProjectSettings_AssociatedLingProj_NoBt()
		{
			SetupMockParatextProxy();

			m_ptHelper.AddProject("ABC", "Whatever");
			m_ptHelper.AddProject("YES", Cache.ProjectId.Handle);
			m_ptHelper.AddProject("BTP");
			m_ptHelper.AddProject("XYZ", null, "ABC");

			m_settings.ParatextScrProj = "ABC";
			m_settings.ParatextBTProj = "BTP";
			m_settings.ParatextNotesProj = null;

			Assert.IsTrue(ReflectionHelper.GetBoolResult(m_importWizard, "PrepareToGetParatextProjectSettings"));
			ComboBox cboScr = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTLangProj");
			ComboBox cboBT = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTBackTrans");
			ComboBox cboNotes = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTTransNotes");

			Assert.AreEqual(1, cboScr.Items.Count);
			Assert.AreEqual(0, cboScr.SelectedIndex);
			Assert.AreEqual(5, cboBT.Items.Count);
			Assert.AreEqual(2, cboBT.SelectedIndex);
			Assert.AreEqual(5, cboNotes.Items.Count);
			Assert.AreEqual(0, cboNotes.SelectedIndex);
			Assert.IsFalse(cboScr.Enabled);
			Assert.IsTrue(cboBT.Enabled);
			Assert.IsTrue(cboNotes.Enabled);
			Assert.AreEqual("YES", m_settings.ParatextScrProj);
			Assert.AreEqual("BTP", m_settings.ParatextBTProj);
			Assert.IsNull(m_settings.ParatextNotesProj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Paratext project combos will not be disabled when there is no
		/// associated PT vernacular project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void PrepareToGetParatextProjectSettings_NoAssociatedLingProj()
		{
			SetupMockParatextProxy();

			m_ptHelper.AddProject("ABC", "Whatever");
			m_ptHelper.AddProject("BTP");
			m_ptHelper.AddProject("XYZ", null, "ABC");

			m_settings.ParatextScrProj = "XYZ";
			m_settings.ParatextBTProj = "BTP";
			m_settings.ParatextNotesProj = null;

			Assert.IsTrue(ReflectionHelper.GetBoolResult(m_importWizard, "PrepareToGetParatextProjectSettings"));
			ComboBox cboScr = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTLangProj");
			ComboBox cboBT = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTBackTrans");
			ComboBox cboNotes = (ComboBox)ReflectionHelper.GetField(m_importWizard, "cboPTTransNotes");

			Assert.AreEqual(3, cboScr.Items.Count);
			Assert.AreEqual(2, cboScr.SelectedIndex);
			Assert.AreEqual(4, cboBT.Items.Count);
			Assert.AreEqual(2, cboBT.SelectedIndex);
			Assert.AreEqual(4, cboNotes.Items.Count);
			Assert.AreEqual(0, cboNotes.SelectedIndex);
			Assert.IsTrue(cboScr.Enabled);
			Assert.IsTrue(cboBT.Enabled);
			Assert.IsTrue(cboNotes.Enabled);
			Assert.AreEqual("XYZ", m_settings.ParatextScrProj);
			Assert.AreEqual("BTP", m_settings.ParatextBTProj);
			Assert.IsNull(m_settings.ParatextNotesProj);
		}
		#endregion

		#region Support for multiple mapping tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests support for creating the first set of import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultipleSettings_FirstSet()
		{
			m_scr.ImportSettingsOC.Clear();

			// Now "click" the Other (sfm) import type (and make sure others are not checked).
			// DefineImportSettings is called when one of the import type radio buttons is clicked.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Other);
			IScrImportSet settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			AddImportFiles(settings, ImportDomain.Main, "other", 5);

			// Save these settings as the default.
			m_scr.DefaultImportSettings = settings;

			// We expect that there will only be one set of import settings,
			Assert.AreEqual(1, m_scr.ImportSettingsOC.Count);
			IScrImportSet actualImportSettings = m_scr.FindImportSettings(TypeOfImport.Other);
			// that it will have a type of "Other",
			Assert.AreEqual(TypeOfImport.Other, actualImportSettings.ImportTypeEnum);
			// that it will be named "Default".
			Assert.AreEqual("Default", actualImportSettings.Name.UserDefaultWritingSystem.Text);
			// and that it will have 5 source files correctly named.
			VerifyImportFiles(settings, ImportDomain.Main, "other", 5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests support for creating a second set of import settings and maintaining the
		/// first set that is a different type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultipleSettings_AddPT6Set()
		{
			m_scr.ImportSettingsOC.Clear();

			// "Click" the Other (sfm) import type.
			// DefineImportSettings is called when one of the import type radio buttons is clicked.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Other);
			IScrImportSet settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			AddImportFiles(settings, ImportDomain.Main, "other", 6);
			settings.SaveSettings();

			// Save the import settings for "Other (USFM)" as the default.
			m_scr.DefaultImportSettings = settings;

			// "Click" on Paratext 6.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Paratext6);
			settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");

			// Now save the import settings for "Paratext 6" as the default.
			m_scr.DefaultImportSettings = settings;

			// We expect that there will be two sets of import settings,
			Assert.AreEqual(2, m_scr.ImportSettingsOC.Count);
			IScrImportSet actualImportSettings = m_scr.FindImportSettings(TypeOfImport.Paratext6);
			// that one of the sets will have a type of "Paratext6",
			Assert.IsNotNull(actualImportSettings);
			Assert.AreEqual(TypeOfImport.Paratext6, actualImportSettings.ImportTypeEnum);
			// and that it will be named "Default".
			Assert.AreEqual("Default", actualImportSettings.Name.UserDefaultWritingSystem.Text);

			// We expect that the other settings will be "Other"
			actualImportSettings = m_scr.FindImportSettings(TypeOfImport.Other);
			Assert.IsNotNull(actualImportSettings);
			// that this set will have a type of "Other",
			Assert.AreEqual(TypeOfImport.Other, actualImportSettings.ImportTypeEnum);
			// that it will be named "Other" rather than "Default",
			Assert.AreEqual("Other", actualImportSettings.Name.UserDefaultWritingSystem.Text);
			// and that it will have files named correctly.
			VerifyImportFiles(actualImportSettings, ImportDomain.Main, "other", 6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests support for creating all three kinds of import settings and maintaining all
		/// import sets.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultipleSettings_AllImportTypes()
		{
			m_scr.ImportSettingsOC.Clear();

			// "Click" the Other (sfm) import type.
			// DefineImportSettings is called when one of the import type radio buttons is clicked.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Other);
			IScrImportSet settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			AddImportFiles(settings, ImportDomain.Main, "other", 4);
			settings.SaveSettings();

			// Save the import settings for "Other (USFM)" as the default (i.e. add it to the db).
			m_scr.DefaultImportSettings = settings;

			// "Click" on Paratext 6 and save these import settings as the default.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Paratext6);
			settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			m_scr.DefaultImportSettings = settings;

			// "Click" on Paratext 5 and save these import settings as the default.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Paratext5);
			settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			AddImportFiles(settings, ImportDomain.Main, "paratext5-", 5);
			settings.SaveSettings();
			m_scr.DefaultImportSettings = settings;

			// We expect that there will be three sets of import settings,
			Assert.AreEqual(3, m_scr.ImportSettingsOC.Count);
			IScrImportSet actualImportSettings =
				(IScrImportSet)m_scr.FindImportSettings(TypeOfImport.Other);
			// that one of the sets will have a type of "Other",
			Assert.IsNotNull(actualImportSettings);
			Assert.AreEqual(TypeOfImport.Other, actualImportSettings.ImportTypeEnum);
			// that it will be named "Other".
			Assert.AreEqual("Other", actualImportSettings.Name.UserDefaultWritingSystem.Text);
			// and that it will have four import source files.
			VerifyImportFiles(actualImportSettings, ImportDomain.Main, "other", 4);

			// Another will be for Paratext 6
			actualImportSettings =
				(IScrImportSet)m_scr.FindImportSettings(TypeOfImport.Paratext6);
			Assert.IsNotNull(actualImportSettings);
			Assert.AreEqual(TypeOfImport.Paratext6, actualImportSettings.ImportTypeEnum);
			// and that it will be named "Paratext 6".
			Assert.AreEqual("Paratext6", actualImportSettings.Name.UserDefaultWritingSystem.Text);

			// Another will be for Paratext 5 (as default)
			actualImportSettings =
				(IScrImportSet)m_scr.FindImportSettings(TypeOfImport.Paratext5);
			Assert.IsNotNull(actualImportSettings);
			Assert.AreEqual(TypeOfImport.Paratext5, actualImportSettings.ImportTypeEnum);
			// that it will be named "Default".
			Assert.AreEqual("Default", actualImportSettings.Name.UserDefaultWritingSystem.Text);
			// and that it will have five import source files.
			VerifyImportFiles(actualImportSettings, ImportDomain.Main, "paratext5-", 5);


			// We can "click" the Other (sfm) import type again and it should be the new default.
			// DefineImportSettings is called when one of the import type radio buttons is clicked.
			ReflectionHelper.CallMethod(m_importWizard, "DefineImportSettings", TypeOfImport.Other);
			settings = (IScrImportSet)ReflectionHelper.GetField(m_importWizard, "m_settings");
			// Save the import settings for "Other (USFM)" as the default (i.e. add it to the db).
			m_scr.DefaultImportSettings = settings;

			// We expect that there should be no more than three settings (i.e. no duplicates).
			Assert.AreEqual(3, m_scr.ImportSettingsOC.Count);
			actualImportSettings =
				(IScrImportSet)m_scr.FindImportSettings(TypeOfImport.Other);
			Assert.IsNotNull(actualImportSettings);
			// We expect the type to be "Other",
			Assert.AreEqual(TypeOfImport.Other, actualImportSettings.ImportTypeEnum);
			// that it will be named "Default",
			Assert.AreEqual("Default", actualImportSettings.Name.UserDefaultWritingSystem.Text);
			// and that it will have four import source files.
			VerifyImportFiles(actualImportSettings, ImportDomain.Main, "other", 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a list files for import settings.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// <param name="domain">The import domain.</param>
		/// <param name="strFileName">Name of the file.</param>
		/// <param name="numFiles">The number of files to add to the import list. Sequential
		/// numbers will be added to each file.</param>
		/// ------------------------------------------------------------------------------------
		private void AddImportFiles(IScrImportSet settings, ImportDomain domain, string strFileName,
			int numFiles)
		{
			for (int iFile = 0; iFile < numFiles; iFile++)
				settings.AddFile(@"c:\" + strFileName + (iFile + 1), domain, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the list of import files (that they are named as expected).
		/// </summary>
		/// <param name="settings">The import settings.</param>
		/// <param name="domain">The import domain.</param>
		/// <param name="strFileName">Name of the file.</param>
		/// <param name="numFiles">The number files.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyImportFiles(IScrImportSet settings, ImportDomain domain,
			string strFileName, int numFiles)
		{
			ImportFileSource source = settings.GetImportFiles(domain);
			Assert.AreEqual(numFiles, source.Count);
			int fileNum = 1;
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(@"c:\" + strFileName + fileNum, info.FileName);
				fileNum++;
			}
		}
		#endregion

		#region Delete mapping tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Delete button works correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteMapping()
		{
			//fill the list and show the dialog
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("impl{", "}", "Alluded Text"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("f:Indo{", "}", "wsIndonesian"));

			m_importWizard.CallLoadMappingsFromSettings();

			m_importWizard.ShowAllMappings = true;

			//select only the first list item and attempt to delete it
			while (m_importWizard.ScrMappings.SelectedItems.Count > 0)
				m_importWizard.ScrMappings.SelectedItems[0].Selected = false;
			m_importWizard.ScrMappings.Items[0].Selected = true;
			Assert.AreEqual("/itw.../*itw", m_importWizard.ScrMappings.Items[0].Text);

			m_importWizard.ClickDeleteMappingButton();

			Assert.AreEqual(4, m_importWizard.ScrMappings.Items.Count);
			Assert.AreEqual(@"\c", m_importWizard.ScrMappings.Items[0].Text);
			//make sure the new first item is selected
			Assert.AreEqual(1, m_importWizard.ScrMappings.SelectedItems.Count);
			Assert.AreEqual(0, m_importWizard.ScrMappings.SelectedIndices[0]);

			//select the last two list items and attempt to delete them
			Assert.AreEqual("f:Indo{...}",
				m_importWizard.ScrMappings.Items[m_importWizard.ScrMappings.Items.Count - 2].Text);
			Assert.AreEqual("impl{...}",
				m_importWizard.ScrMappings.Items[m_importWizard.ScrMappings.Items.Count - 1].Text);
			while (m_importWizard.ScrMappings.SelectedItems.Count > 0)
				m_importWizard.ScrMappings.SelectedItems[0].Selected = false;
			m_importWizard.ScrMappings.Items[m_importWizard.ScrMappings.Items.Count - 1].Selected = true;
			m_importWizard.ScrMappings.Items[m_importWizard.ScrMappings.Items.Count - 2].Selected = true;

			m_importWizard.ClickDeleteMappingButton();

			Assert.AreEqual(@"\v",
				m_importWizard.ScrMappings.Items[m_importWizard.ScrMappings.Items.Count - 1].Text);
			Assert.AreEqual(2, m_importWizard.ScrMappings.Items.Count);
			//verify new selection
			Assert.AreEqual(1, m_importWizard.ScrMappings.SelectedItems.Count);
			Assert.AreEqual(m_importWizard.ScrMappings.Items.Count - 1,
				m_importWizard.ScrMappings.SelectedIndices[0]);
			Assert.AreEqual(3, ((ScrMappingList)m_settings.Mappings(MappingSet.Main)).Count);
		}
		#endregion

		#region Add Mapping tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when adding to list of existing
		/// markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_toNonEmptyList()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// add an item using the Add button
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("GodSpeaking{", "}EndSpeaking", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify new mapping in listView.
			Assert.AreEqual(cItemsBeforeInsertion + 1, m_importWizard.ScrMappings.Items.Count);
			// 3 is the sorted location of the insertion.
			ListViewItem insertedItem = m_importWizard.ScrMappings.Items[3];
			Assert.AreEqual("GodSpeaking{...}EndSpeaking", insertedItem.Text);
			ImportMappingInfo mapping = (ImportMappingInfo)insertedItem.Tag;
			Assert.AreEqual("GodSpeaking{", mapping.BeginMarker);
			Assert.AreEqual("}EndSpeaking", mapping.EndMarker);
			Assert.IsNull(mapping.WsId, "ICU Locale should be based on context");
			Assert.AreEqual("Emphasis", mapping.StyleName);
			Assert.AreEqual(true, mapping.IsInline);
			Assert.IsFalse(m_importWizard.MappingInvalidWarningHappened, "A warning message should not occur");
			// make sure the new item is selected
			Assert.AreEqual(1, m_importWizard.ScrMappings.SelectedItems.Count);
			Assert.AreEqual(insertedItem, m_importWizard.ScrMappings.SelectedItems[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an added marker gets saved to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_VerifySaved()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// add an item using the Add button
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo(@"\em", @"\em", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// save the settings and check the count
			m_settings.SaveSettings();
			Assert.AreEqual(4, m_settings.ScriptureMappingsOC.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when user cancels adding to list of
		/// existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_Cancel()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// Start to add an item, but cancel.
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("A{", "}", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = false;
			m_importWizard.ClickAddMappingButton();

			// verify nothing new in listView
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			foreach (ListViewItem item in m_importWizard.ScrMappings.Items)
			{
				Assert.IsTrue(((ImportMappingInfo)(item.Tag)).BeginMarker != "A{");
			}
			Assert.IsFalse(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should not occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add a marker without
		/// a begin marker to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_NoBeginMarker()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with no begin marker
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("", "}p", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we did not add a maker without a begining marker
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			foreach (ListViewItem item in m_importWizard.ScrMappings.Items)
				Assert.IsFalse(((ImportMappingInfo)(item.Tag)).BeginMarker == "");
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add a marker without
		/// an end marker to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_NoEndMarker()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with no end marker
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("->", "", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we did not add a maker without a end marker
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			foreach (ListViewItem item in m_importWizard.ScrMappings.Items)
				Assert.IsFalse(((ImportMappingInfo)(item.Tag)).EndMarker == "");
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when adding a marker with a backslash
		/// to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_BackslashMarker()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with marker starting with backslash
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo(@"\f{", "}p", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we added a marker with a backslash
			Assert.AreEqual(cItemsBeforeInsertion + 1, m_importWizard.ScrMappings.Items.Count);
			ListViewItem item = m_importWizard.ScrMappings.Items[2];
			Assert.AreEqual(@"\f{", ((ImportMappingInfo)(item.Tag)).BeginMarker);
			Assert.IsFalse(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should not occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add a duplicate
		/// marker to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_Duplicate()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add duplicate item using the Add button
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("/itw", "}x", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify that nothing was inserted and the existing item wasn't changed
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			ListViewItem item = m_importWizard.ScrMappings.Items[0];
			Assert.AreEqual("/itw", ((ImportMappingInfo)(item.Tag)).BeginMarker);
			Assert.AreEqual("/*itw", ((ImportMappingInfo)(item.Tag)).EndMarker);
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add a Begin marker
		/// with a space to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_BeginMarkerHasSpace()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with spaces in the begin marker
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("a space", "isnotgood", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we did not add anything
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add an End marker
		/// with a space to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_EndMarkerHasSpace()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with spaces in the end marker
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("aspace", "is not good", "Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we did not add a end marker with a space
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add a Begin marker
		/// with an invalid character to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_BeginMarkerWithInvalidChar()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with a char with a USV greater then U+007F in the begin marker
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("Sneeky" + (char)0x0080 + "chars", "arenotgood",
				"Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we did not add a begin marker with a character
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the Add button works correctly when attempting to add an End marker
		/// with an invalid character to list of existing markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddMapping_EndMarkerWithInvalidChar()
		{
			// create our m_importWizard with a populated list
			//fill the list and show the m_importWizard
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("/itw", "/*itw", "Italic"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, "Chapter Number"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\id", null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, "Verse Number"));

			m_importWizard.CallLoadMappingsFromSettings();
			m_importWizard.ShowAllMappings = true;

			int cItemsBeforeInsertion = m_importWizard.ScrMappings.Items.Count;

			// attempt to add item with a char with a USV greater then U+007F in the end marker
			m_importWizard.MappingDialogDummyData = new ImportMappingInfo("IllegalImmigrant", "Sneeky" + (char)0x0080 + "char",
				"Emphasis");
			m_importWizard.MappingDialogDummyAccept = true;
			m_importWizard.ClickAddMappingButton();

			// verify we did not add anything
			Assert.AreEqual(cItemsBeforeInsertion, m_importWizard.ScrMappings.Items.Count);
			Assert.IsTrue(m_importWizard.MappingInvalidWarningHappened,
				"A warning message should occur");
		}
		#endregion
	}
	#endregion
}
