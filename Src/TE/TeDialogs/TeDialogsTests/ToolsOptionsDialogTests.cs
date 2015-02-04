// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ToolsOptionsDialogTests.cs
// Responsibility: TE Team

using System;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using XCore;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region DummyApp
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyApp : FwApp
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyApp() : base(null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main window
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use, if needed (can be null).</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this m_cache.</param>
		/// <param name="wndCopyFrom">Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>
		/// New instance of main window if successfull; otherwise <c>null</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(IProgress progressDlg, bool fNewCache,
			Form wndCopyFrom, bool fOpeningNewProject)
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SampleDatabase
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProductExecutableFile
		{
			get { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ApplicationName
		{
			get { return "Dummy app"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a hook for initializing the cache in application-specific ways.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialization was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool InitCacheForApp(IThreadedProgress progressDlg)
		{
			throw new NotImplementedException();
		}
	}
	#endregion

	#region ToolsOptionsDlgDummy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A dialog window for testing ToolsOptions dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ToolsOptionsDlgDummy : ToolsOptionsDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolsOptionsDlgDummy"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ToolsOptionsDlgDummy(IApp app, IHelpTopicProvider helpTopicProvider, WritingSystemManager wsManager) :
			base(app, helpTopicProvider, wsManager)
		{
		}

		/// <summary></summary>
		public CheckBox CheckPromptEmptyParas
		{
			get
			{
				CheckDisposed();
				return m_chkPromptEmptyParas;
			}
		}
		/// <summary></summary>
		public CheckBox CheckMarkerlessFootnoteIcons
		{
			get
			{
				CheckDisposed();
				return m_chkMarkerlessFootnoteIcons;
			}
		}
		/// <summary></summary>
		public CheckBox CheckSynchFootnoteScroll
		{
			get
			{
				CheckDisposed();
				return m_chkSynchFootnoteScroll;
			}
		}
		/// <summary></summary>
		public Button OKButton
		{
			get
			{
				CheckDisposed();
				return btnOK;
			}
		}
		/// <summary></summary>
		public RadioButton RadioBasic
		{
			get
			{
				CheckDisposed();
				return rdoBasicStyles;
			}
		}
		/// <summary></summary>
		public RadioButton RadioAll
		{
			get
			{
				CheckDisposed();
				return rdoAllStyles;
			}
		}
		/// <summary></summary>
		public RadioButton RadioCustom
		{
			get
			{
				CheckDisposed();
				return rdoCustomList;
			}
		}
		/// <summary></summary>
		public CheckBox CheckUserDefined
		{
			get
			{
				CheckDisposed();
				return chkShowUserDefined;
			}
		}
		/// <summary></summary>
		public ComboBox ComboStyleLevel
		{
			get
			{
				CheckDisposed();
				return cboStyleLevel;
			}
		}
		/// <summary></summary>
		public void ClickOK()
		{
			CheckDisposed();

			btnOK_Click(null, null);
		}
	}
	#endregion

	/// <summary>
	/// Summary description for ToolsOptionsDialogTests.
	/// </summary>
	[TestFixture]
	public class ToolsOptionsDialogTests: BaseTest
	{
		private DummyApp m_app;
		private WritingSystemManager m_wsManager;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_app = new DummyApp();
			m_wsManager = new WritingSystemManager();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tears the down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_app.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the draft view options portion of the tools dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DraftViewOptionsTest()
		{
			// set the registry settings before creating the dialog
			Options.ShowMarkerlessIconsSetting = true;
			Options.FootnoteSynchronousScrollingSetting = false;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// create a dialog and make sure that the dialog is initialized properly
			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				Assert.IsTrue(dlg.CheckMarkerlessFootnoteIcons.Checked);
				Assert.IsFalse(dlg.CheckSynchFootnoteScroll.Checked);
				Assert.IsTrue(dlg.CheckPromptEmptyParas.Checked);
				//Assert.IsFalse(dlg.CheckShowStyles.Checked);

				// set the registry settings before creating the dialog again
				Options.ShowMarkerlessIconsSetting = false;
				Options.FootnoteSynchronousScrollingSetting = true;
				Options.ShowEmptyParagraphPromptsSetting = false;
			}

			// check the new dialog values
			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				Assert.IsFalse(dlg.CheckMarkerlessFootnoteIcons.Checked);
				Assert.IsTrue(dlg.CheckSynchFootnoteScroll.Checked);
				Assert.IsFalse(dlg.CheckPromptEmptyParas.Checked);
				//Assert.IsTrue(dlg.CheckShowStyles.Checked);

				// set the items in the dialog and then click OK to make sure they get saved correctly
				dlg.CheckMarkerlessFootnoteIcons.Checked = true;
				dlg.CheckSynchFootnoteScroll.Checked = true;
				dlg.CheckPromptEmptyParas.Checked = false;
				//dlg.CheckShowStyles.Checked = false;
				dlg.ClickOK();

				Assert.IsTrue(Options.ShowMarkerlessIconsSetting);
				Assert.IsTrue(Options.FootnoteSynchronousScrollingSetting);
				Assert.IsFalse(Options.ShowEmptyParagraphPromptsSetting);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the style options portion of the tools/options dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StyleOptionsTest()
		{
			// Make sure all of the settings are the default values in the registry
			Options.ShowTheseStylesSetting = Options.ShowTheseStyles.All;
			Options.ShowStyleLevelSetting = Options.StyleLevel.Basic;
			Options.ShowUserDefinedStylesSetting = true;

			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				// Make sure that all of the settings were initialized correctly.
				Assert.IsTrue(dlg.RadioAll.Checked, "All radio button should be checked");
				Assert.AreEqual(0, dlg.ComboStyleLevel.SelectedIndex);
				Assert.IsTrue(dlg.CheckUserDefined.Checked,
					"Show user defined styles checkbox should be checked");

				// Now, set a different set of values to the registry and see if the dialog gets them correctly
				Options.ShowTheseStylesSetting = Options.ShowTheseStyles.Basic;
				Options.ShowStyleLevelSetting = Options.StyleLevel.Expert;
				Options.ShowUserDefinedStylesSetting = false;
			}

			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				// Make sure that all of the settings were initialized correctly.
				Assert.IsTrue(dlg.RadioBasic.Checked, "Basic radio button should be checked");
				Assert.AreEqual(3, dlg.ComboStyleLevel.SelectedIndex);
				Assert.IsFalse(dlg.CheckUserDefined.Checked,
					"Show user defined styles checkbox should NOT be checked");

				// change the values in the window and then make sure they get saved correctly.
				dlg.RadioCustom.Checked = true;
				dlg.ComboStyleLevel.SelectedIndex = 2;
				dlg.CheckUserDefined.Checked = true;
				dlg.ClickOK();

				// check the registry values to make sure they got set correctly.
				Assert.AreEqual(Options.ShowTheseStyles.Custom, Options.ShowTheseStylesSetting);
				Assert.AreEqual(Options.StyleLevel.Advanced, Options.ShowStyleLevelSetting);
				Assert.IsTrue(Options.ShowUserDefinedStylesSetting,
					"Show User defined styles checkbox was not saved to the registry correctly");
			}
		}
	}
}
