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
// File: CharacterMappingSettingsTest.cs
// Responsibility: BryanW
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.ScrImportComponents
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// CharacterMappingSettingsTest.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	[TestFixture]
	public class CharacterMappingSettingsTest
	{
		#region DummyCharacterMappingSettings class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// This class is used to test the CharacterMappingSettings dialog
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private class DummyCharacterMappingSettings : CharacterMappingSettings
		{
			/// -------------------------------------------------------------------------------------
			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="mapping">Provides intial values displayed in dialog.</param>
			/// <param name="styleSheet">Provides the character styles user can pick from.</param>
			/// <param name="cache">The DB cache</param>
			/// -------------------------------------------------------------------------------------
			public DummyCharacterMappingSettings(ImportMappingInfo mapping, FwStyleSheet styleSheet,
				FdoCache cache): base(mapping, styleSheet, cache, false)
			{
				// No code needed so far.
			}

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

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This exposes the styles list box so that it can be tested that it loads correctly
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public ListBox StyleList
			{
				get
				{
					CheckDisposed();
					return mappingDetailsCtrl.lbStyles;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public StyleListBoxHelper StyleListHelper
			{
				get
				{
					CheckDisposed();
					return mappingDetailsCtrl.m_styleListHelper;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This exposes the writingSystemCombo box to the test
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public ComboBox WritingSystemComboBox
			{
				get
				{
					CheckDisposed();
					return mappingDetailsCtrl.cboWritingSys;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Simulates the user clicking the Footnote domain
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void SelectFootnoteDomain()
			{
				CheckDisposed();

				mappingDetailsCtrl.rbtnFootnotes.Checked = true;
			}
		}
		#endregion

		#region Member variables
		private FdoCache m_cache;
		private IScripture m_Scripture;
		private FwStyleSheet m_styleSheet;
		private ImportMappingInfo m_mapping;
		private DummyCharacterMappingSettings m_dialog;
		#endregion

		#region Constructor, Init, Cleanup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void InitFixture()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up called after all tests are run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void CleanUpFixture()
		{
			FdoCache.RestoreTestLangProj();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_cache = FdoCache.Create("TestLangProj");
			m_Scripture = m_cache.LangProject.TranslatedScriptureOA;
			// Make sure we don't call InstallLanguage during tests.
			m_cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(m_cache, m_Scripture.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			m_mapping = new ImportMappingInfo("emph{", "}", "Emphasis");
			Options.ShowTheseStylesSetting = Options.ShowTheseStyles.All;
			m_dialog = new DummyCharacterMappingSettings(m_mapping, m_styleSheet, m_cache);
			m_dialog.StyleListHelper.MaxStyleLevel = int.MaxValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			// For runtime sake, do FdoCache.RestoreTestLangProj in TestFixtureTearDown unless
			// our individual tests really need a clean database.
			//			FdoCache.RestoreTestLangProj();
			m_dialog.Dispose();
			m_dialog = null;
			m_mapping = null;
			m_styleSheet = null;
			m_Scripture = null;
			m_cache.Dispose();
			m_cache = null;
		}
		#endregion

		#region Tests

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This verifies that the data in the mapping is loaded into the textboxes of the
		/// dialog when it is loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyInitialValues()
		{
			//verify default values in dialog.
			Assert.AreEqual(m_mapping.BeginMarker, m_dialog.BeginningTextBox.Text);
			Assert.AreEqual(m_mapping.EndMarker, m_dialog.EndingTextBox.Text);
			Assert.AreEqual(m_mapping.StyleName, m_dialog.StyleList.Text,
				"Style should be initialized to mapping's style");
			Assert.AreEqual(m_dialog.WritingSystemComboBox.Text, "<Based on Context>",
				"Should show based on context menu item");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This checks to see that the mapping got changed to what is in the text boxes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyMappingOnOk()
		{
			m_dialog.Show();

			m_dialog.BeginningTextBox.Text = "Gumby[";
			m_dialog.EndingTextBox.Text = "]";
			m_dialog.StyleListHelper.SelectedStyleName = "Emphasis";
			m_dialog.WritingSystemComboBox.SelectedIndex =
				m_dialog.WritingSystemComboBox.SelectedIndex + 1;
			string sWsName = m_dialog.WritingSystemComboBox.Text;

			m_dialog.AcceptButton.PerformClick();

			//verify return values
			Assert.AreEqual("Gumby[", m_mapping.BeginMarker,
				"BeginMarker should be set by dialog");
			Assert.AreEqual("]", m_mapping.EndMarker);
			Assert.AreEqual("Emphasis", m_mapping.StyleName,
				"StyleName should be set by dialog");
			Assert.AreEqual(true, m_mapping.IsInline,
				"Should create an inline style"); //optional?
			string sMapWsName = m_cache.LangProject.GetWritingSystemName(m_mapping.IcuLocale);
			Assert.AreEqual(sWsName, sMapWsName, "Writing System should be set by dialog");
			Assert.AreEqual(MarkerDomain.Default, m_mapping.Domain);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This checks to see that the return values in the MappingResult do get changed to
		/// what is in the text boxes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyFootnoteDomainIsSet()
		{
			m_dialog.Show();
			Application.DoEvents();

			m_dialog.BeginningTextBox.Text = "|fr ";
			m_dialog.EndingTextBox.Text = "|fr*";
			m_dialog.SelectFootnoteDomain();
			m_dialog.StyleListHelper.SelectedStyleName = "Note Target Reference";

			m_dialog.AcceptButton.PerformClick();

			//verify return values
			Assert.AreEqual("|fr ", m_mapping.BeginMarker,
				"BeginMarker should be set by dialog");
			Assert.AreEqual("|fr*", m_mapping.EndMarker,
				"EndMarker should be set by dialog");
			Assert.AreEqual("Note Target Reference", m_mapping.StyleName,
				"StyleName should be set by dialog");
			Assert.AreEqual(true, m_mapping.IsInline,
				"Should create an inline style"); //optional?
			Assert.AreEqual(MarkerDomain.Footnote, m_mapping.Domain,
				"Domain should be inferred from footnote style");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This checks to see that the return values in the MappingResult do get changed to
		/// what is in the text boxes and that leading/trailing spaces are preserved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyMappingResultWithSpaces()
		{
			m_dialog.Show();

			m_dialog.BeginningTextBox.Text = " Gumby[ ";
			m_dialog.EndingTextBox.Text = " ] ";
			m_dialog.StyleListHelper.SelectedStyleName = "Emphasis";
			m_dialog.WritingSystemComboBox.SelectedIndex =
				m_dialog.WritingSystemComboBox.SelectedIndex + 1;
			string sWsName = m_dialog.WritingSystemComboBox.Text;

			m_dialog.AcceptButton.PerformClick();

			//verify return values
			Assert.AreEqual(" Gumby[ ", m_mapping.BeginMarker,
				"BeginMarker should be set by dialog");
			Assert.AreEqual(" ] ", m_mapping.EndMarker);
			Assert.AreEqual("Emphasis", m_mapping.StyleName,
				"StyleName should be set by dialog");
			Assert.AreEqual(true, m_mapping.IsInline,
				"Should create an inline style"); //optional?
			string sMapWsName = m_cache.LangProject.GetWritingSystemName(m_mapping.IcuLocale);
			Assert.AreEqual(sWsName, sMapWsName, "Writing System should be set by dialog");
			Assert.AreEqual(MarkerDomain.Default, m_mapping.Domain);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This verifies that the dialog result gets set to cancel when the dialog is canceled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyCancel()
		{
			m_dialog.Show();

			//simulate input editing
			m_dialog.BeginningTextBox.Text = "Gumby[";
			m_dialog.EndingTextBox.Text = "]";
			m_dialog.StyleListHelper.SelectedStyleName = "Alternate Reading";

			m_dialog.CancelButton.PerformClick();

			//verify return values
			Assert.AreEqual(DialogResult.Cancel, m_dialog.DialogResult);
		}
		#endregion
	}
}
