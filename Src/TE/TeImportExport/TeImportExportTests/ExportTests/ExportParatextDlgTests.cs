// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportParatextDlgTests.cs
// Responsibility: bogle
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportParatextDlgTests: ScrInMemoryFdoTestBase
	{
		#region Data members
		private DummyParatextDialog m_dummyParaDlg;
		private FilteredScrBooks m_bookFilter;
		private IScrBook m_Genesis;
		#endregion

		#region Initalization of tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_Genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();
			m_bookFilter = new FilteredScrBooks(Cache, 1);
			m_bookFilter.UpdateFilter(new int[] { m_Genesis.Hvo });
			m_dummyParaDlg = new DummyParatextDialog(Cache, m_bookFilter);

			//ScrImportSet importSet = new ScrImportSet();
			//Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(importSet);
			//importSet.ParatextScrProj = "xyz";
			//importSet.ParatextBTProj = "xyzBT";
			//Cache.LangProject.TranslatedScriptureOA.DefaultImportSettings = importSet;
			ILgWritingSystem wsVern = ((LgWritingSystem)Cache.LangProject.CurVernWssRS[0]);
			wsVern.Abbr.UserDefaultWritingSystem = "xyz";

			// Initialize in-memory registry settings.
			m_dummyParaDlg.Registry.SetIntValue("ParatextOneDomainExportWhat", 0);
			m_dummyParaDlg.Registry.SetStringValue("ParatextOutputSpec", @"C:\My Paratext Projects");
			m_dummyParaDlg.Registry.SetStringValue("ParatextBTOutputSpec", @"C:\My Paratext Projects");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown the test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Exit()
		{
			base.Exit();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting up the output folder name for Paratext export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetupOutputFolder()
		{
			m_dummyParaDlg.SimulateShowDialog();

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyz", m_dummyParaDlg.DisplayedOutputFolder);
			Assert.AreEqual(string.Empty, m_dummyParaDlg.FileNameSchemeCtrl.Prefix);
			Assert.AreEqual("41MAT", m_dummyParaDlg.FileNameSchemeCtrl.Scheme);
			Assert.AreEqual("xyz", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			// Fails if xyz does not exist.
			//Assert.AreEqual("sfm", m_dummyParaDlg.FileNameSchemeCtrl.Extension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting up the output folder name for Paratext export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetupOutputFolder_ClickOnBackTranslation()
		{
			m_dummyParaDlg.ScriptureChecked = true;

			m_dummyParaDlg.SimulateShowDialog();
			// Now "click" on the BT domain radio button.
			m_dummyParaDlg.SimulateCheckBackTrans();

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.BackTransOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyzBT", m_dummyParaDlg.DisplayedOutputFolder);
			Assert.AreEqual(string.Empty, m_dummyParaDlg.FileNameSchemeCtrl.Prefix);
			Assert.AreEqual("41MAT", m_dummyParaDlg.FileNameSchemeCtrl.Scheme);
			Assert.AreEqual("xyzBT", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			Assert.AreEqual("sfm", m_dummyParaDlg.FileNameSchemeCtrl.Extension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting up the output folder name for Paratext export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetupOutputFolder_ClickOnScripture()
		{
			m_dummyParaDlg.BackTranslationChecked = true;

			m_dummyParaDlg.SimulateShowDialog();

			// Now "click" on the Scripture domain radio button.
			m_dummyParaDlg.SimulateCheckScripture();

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyz", m_dummyParaDlg.DisplayedOutputFolder);
			Assert.AreEqual(string.Empty, m_dummyParaDlg.FileNameSchemeCtrl.Prefix);
			Assert.AreEqual("41MAT", m_dummyParaDlg.FileNameSchemeCtrl.Scheme);
			Assert.AreEqual("xyz", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			// Fails if xyz does not exist.
			//Assert.AreEqual("sfm", m_dummyParaDlg.FileNameSchemeCtrl.Extension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the suffix changes when short name is changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateSuffix()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateEditingShortName("xyza");

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyza", m_dummyParaDlg.DisplayedOutputFolder);
			Assert.AreEqual(string.Empty, m_dummyParaDlg.FileNameSchemeCtrl.Prefix);
			Assert.AreEqual("41MAT", m_dummyParaDlg.FileNameSchemeCtrl.Scheme);
			Assert.AreEqual("xyza", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			// Fails if xyz does not exist.
			//Assert.AreEqual("sfm", m_dummyParaDlg.FileNameSchemeCtrl.Extension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the short name gets appended to the output path if selected folder is
		/// "C:\My Paratext Projects"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BrowseFolder_MyParaProjects()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateBrowseFolder(@"C:\My Paratext Projects");

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyz", m_dummyParaDlg.DisplayedOutputFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the short name gets appended to the output path if selected
		/// folder is not "C:\My Paratext Projects"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BrowseFolder_OtherFolder()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateBrowseFolder(@"C:\My Paratext Projects\SomeProject");

			Assert.AreEqual(@"C:\My Paratext Projects\SomeProject", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\SomeProject\xyz", m_dummyParaDlg.DisplayedOutputFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the short name doesn't get appended to the output path if selected
		/// folder ends with the short name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BrowseFolder_ProjectFolder()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateBrowseFolder(@"C:\My Paratext Projects\xyz");

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyz", m_dummyParaDlg.DisplayedOutputFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the short name gets appended to the output path if selected
		/// folder is not "C:\My Paratext Projects"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BrowseFolder_OtherFolderBT()
		{
			m_dummyParaDlg.SimulateShowDialog();

			// Now "click" on the BT domain radio button.
			m_dummyParaDlg.SimulateCheckBackTrans();

			m_dummyParaDlg.SimulateBrowseFolder(@"C:\My Paratext Projects\SomeProject");

			Assert.AreEqual(@"C:\My Paratext Projects\SomeProject", m_dummyParaDlg.BackTransOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\SomeProject\xyzBT", m_dummyParaDlg.DisplayedOutputFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the output folder for BT is independent from Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BrowseFolder_BTFolderIndependent()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateBrowseFolder(@"C:\My Paratext Projects\SomeProject");

			// Now "click" on the BT domain radio button.
			m_dummyParaDlg.SimulateCheckBackTrans();

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.BackTransOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\SomeProject", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xyzBT", m_dummyParaDlg.DisplayedOutputFolder);
			Assert.AreEqual(string.Empty, m_dummyParaDlg.FileNameSchemeCtrl.Prefix);
			Assert.AreEqual("41MAT", m_dummyParaDlg.FileNameSchemeCtrl.Scheme);
			Assert.AreEqual("xyzBT", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			Assert.AreEqual("sfm", m_dummyParaDlg.FileNameSchemeCtrl.Extension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we strip off padding
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeShortName()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateEditingShortName("xy___");

			Assert.AreEqual(@"C:\My Paratext Projects", m_dummyParaDlg.ScriptureOutputFolder);
			Assert.AreEqual(@"C:\My Paratext Projects\xy_", m_dummyParaDlg.DisplayedOutputFolder);
			Assert.AreEqual(string.Empty, m_dummyParaDlg.FileNameSchemeCtrl.Prefix);
			Assert.AreEqual("41MAT", m_dummyParaDlg.FileNameSchemeCtrl.Scheme);
			Assert.AreEqual("xy_", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			// Fails if xyz does not exist.
			//Assert.AreEqual("sfm", m_dummyParaDlg.FileNameSchemeCtrl.Extension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we maintain/restore the Scripture settings correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeSuffixforBT()
		{
			m_dummyParaDlg.SimulateShowDialog();

			m_dummyParaDlg.SimulateCheckBackTrans();
			m_dummyParaDlg.SimulateEditingSuffix("xy_BT");
			m_dummyParaDlg.SimulateCheckScripture();

			Assert.AreEqual("xyz", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
			Assert.AreEqual("xyz", m_dummyParaDlg.ShortName);
			Assert.AreEqual("xyz", m_dummyParaDlg.ScriptureFileNameSuffix);
			Assert.AreEqual("xy_BT", m_dummyParaDlg.BackTransFileNameSuffix);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we maintain/restore the user modified suffix correctly when we switch
		/// to BT and back.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeSuffix_SwitchKeepsModifiedSuffix()
		{
			m_dummyParaDlg.SimulateShowDialog();

			// Edit the suffix and short name in vernacular
			m_dummyParaDlg.SimulateEditingSuffix("abc");
			m_dummyParaDlg.SimulateEditingShortName("bla");

			// now switch to back translation
			m_dummyParaDlg.SimulateCheckBackTrans();
			// and back
			m_dummyParaDlg.SimulateCheckScripture();

			Assert.AreEqual("bla", m_dummyParaDlg.ShortName);
			Assert.AreEqual("abc", m_dummyParaDlg.ScriptureFileNameSuffix);
			Assert.AreEqual("xyzBT", m_dummyParaDlg.BackTransFileNameSuffix);
			Assert.AreEqual("abc", m_dummyParaDlg.FileNameSchemeCtrl.Suffix);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that selecting an existing Paratext project resets manually edited values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("On our wishlist for TE-5089 - test needs to be finished and code changed")]
		public void SelectProjectResetsValues()
		{
			Unpacker.UnpackTEVTitusWithUnmappedStyle();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_dummyParaDlg.SimulateShowDialog();

				// Edit the suffix and short name in vernacular
				m_dummyParaDlg.SimulateEditingSuffix("abc");
				m_dummyParaDlg.SimulateEditingShortName("bla");

				// Select project from combo box

				// Check that values in dialog get reset to values from Paratext project file
			}
			finally
			{
				regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}
	}

	#region Dummy ExportPtxDialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyParatextDialog : ExportPtxDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyParatextDialog"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filter">The filter.</param>
		/// ------------------------------------------------------------------------------------
		public DummyParatextDialog(FdoCache cache, FilteredScrBooks filter)
			: base(cache, filter, null)
		{
			m_regGroup = new InMemoryRegistryGroup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate loading the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateShowDialog()
		{
			CheckDisposed();

			FileNameSchemeCtrl.InitSchemeComboBx();
			OnLoad(EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the registry group.
		/// </summary>
		/// <value>The registry.</value>
		/// ------------------------------------------------------------------------------------
		public RegistryGroup Registry
		{
			get
			{
				CheckDisposed();
				return m_regGroup;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scripture output folder.
		/// </summary>
		/// <value>The scripture output folder.</value>
		/// ------------------------------------------------------------------------------------
		public string ScriptureOutputFolder
		{
			get
			{
				CheckDisposed();
				return m_OutputFolder;
			}
			set
			{
				CheckDisposed();
				m_OutputFolder = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the back translation output folder.
		/// </summary>
		/// <value>The back trans output folder.</value>
		/// ------------------------------------------------------------------------------------
		public string BackTransOutputFolder
		{
			get
			{
				CheckDisposed();
				return m_BTOutputFolder;
			}
			set
			{
				CheckDisposed();
				m_BTOutputFolder = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the displayed output folder.
		/// </summary>
		/// <value>The displayed output folder.</value>
		/// ------------------------------------------------------------------------------------
		public string DisplayedOutputFolder
		{
			get
			{
				CheckDisposed();
				return txtOutputFolder.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file naming scheme to display in the dialog control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileNameSchemeCtrl FileNameSchemeCtrl
		{
			get
			{
				CheckDisposed();
				return fileNameSchemeCtrl;
			}
			set
			{
				CheckDisposed();
				base.fileNameSchemeCtrl = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the Scripture radio button is checked to
		/// export the Scripture domain.
		/// </summary>
		/// <value><c>true</c> if scripture is checked; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ScriptureChecked
		{
			get
			{
				CheckDisposed();
				return rdoScripture.Checked;
			}
			set
			{
				CheckDisposed();
				rdoScripture.Checked = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the scripture radio button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateCheckScripture()
		{
			CheckDisposed();

			rdoBackTranslation.Checked = false;
			rdoScripture.Checked = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the back translation radio button is checked
		/// to export the back translation domain.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if back translation domain is selected; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool BackTranslationChecked
		{
			get
			{
				CheckDisposed();
				return rdoBackTranslation.Checked;
			}
			set
			{
				CheckDisposed();
				rdoBackTranslation.Checked = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the back translation radio button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateCheckBackTrans()
		{
			CheckDisposed();

			rdoBackTranslation.Checked = true;
			rdoScripture.Checked = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to overwrite a project.
		/// </summary>
		/// <value><c>true</c> if overwriting project; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool OverwriteProject
		{
			get
			{
				CheckDisposed();
				return m_overwriteProject;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default file scheme suffix for Paratext 6.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public new string DefaultSuffix
		{
			get
			{
				CheckDisposed();
				return base.DefaultSuffix;
			}
			set
			{
				CheckDisposed();

				if (ExportScriptureDomain)
					m_fileNameScheme.m_fileSuffix = value;
				else
					m_BTfileNameScheme.m_fileSuffix = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of the noneditable Paratext projects.
		/// </summary>
		/// <value>The non editable projects.</value>
		/// ------------------------------------------------------------------------------------
		public List<string> NonEditableProjects
		{
			get
			{
				CheckDisposed();
				return m_nonEditableP6Projects;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the short name should be appended to the folder.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if appending the short name to the folder; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool AppendShortNameToFolder
		{
			get
			{
				CheckDisposed();
				return m_fAppendShortNameToFolder;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether user has requested export of back translation
		/// data.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public new bool ExportBackTranslationDomain
		{
			get
			{
				CheckDisposed();
				return base.ExportBackTranslationDomain;
			}
			set
			{
				CheckDisposed();
				rdoBackTranslation.Checked = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates editing the short name.
		/// </summary>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		public void SimulateEditingShortName(string newValue)
		{
			CheckDisposed();

			cboShortName.Text = newValue;
			cboShortName_Leave(null, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates editing the file suffix.
		/// </summary>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		public void SimulateEditingSuffix(string newValue)
		{
			CheckDisposed();

			fileNameSchemeCtrl.Suffix = newValue;
			fileNameSchemeCtrl.UserModifiedSuffix = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates browsing to a new output folder.
		/// </summary>
		/// <param name="newFolder">The new folder.</param>
		/// ------------------------------------------------------------------------------------
		public void SimulateBrowseFolder(string newFolder)
		{
			CheckDisposed();

			BaseOutputFolder = newFolder;
			m_fUserModifiedFolder = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name suffix for the Scripture domain.
		/// </summary>
		/// <value>The file name suffix.</value>
		/// ------------------------------------------------------------------------------------
		public string ScriptureFileNameSuffix
		{
			get
			{
				CheckDisposed();
				return m_fileNameScheme.m_fileSuffix;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name suffix for the back translation domain.
		/// </summary>
		/// <value>The file name suffix.</value>
		/// ------------------------------------------------------------------------------------
		public string BackTransFileNameSuffix
		{
			get
			{
				CheckDisposed();
				return m_BTfileNameScheme.m_fileSuffix;
			}
		}
	}
	#endregion
}
