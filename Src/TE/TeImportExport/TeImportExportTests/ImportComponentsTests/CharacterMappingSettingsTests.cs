// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CharacterMappingSettingsTest.cs
// Responsibility: BryanW
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE.ImportComponentsTests
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// CharacterMappingSettingsTest.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	[TestFixture]
	public class CharacterMappingSettingsTest : ScrInMemoryFdoTestBase
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
				FdoCache cache) : base(mapping, styleSheet, cache, false, null, null)
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
		private IScripture m_Scripture;
		private FwStyleSheet m_styleSheet;
		private ImportMappingInfo m_mapping;
		private DummyCharacterMappingSettings m_dialog;
		#endregion

		#region Constructor, Init, Cleanup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_Scripture = Cache.LangProject.TranslatedScriptureOA;

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_Scripture.Hvo, ScriptureTags.kflidStyles);

			m_mapping = new ImportMappingInfo("emph{", "}", "Emphasis");
			Options.ShowTheseStylesSetting = Options.ShowTheseStyles.All;
			m_dialog = new DummyCharacterMappingSettings(m_mapping, m_styleSheet, Cache);
			m_dialog.StyleListHelper.MaxStyleLevel = int.MaxValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_dialog.Dispose();
			m_dialog = null;
			m_mapping = null;
			m_styleSheet = null;
			m_Scripture = null;
			base.TestTearDown();
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
			string sMapWsName = Cache.ServiceLocator.WritingSystemManager.Get(m_mapping.WsId).DisplayLabel;
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
			string sMapWsName = Cache.ServiceLocator.WritingSystemManager.Get(m_mapping.WsId).DisplayLabel;
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
