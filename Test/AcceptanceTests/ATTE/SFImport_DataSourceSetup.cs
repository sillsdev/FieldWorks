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
// File: SFImport_DataSourceSetup.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using NUnit.Framework;
//using SIL.FieldWorks.AcceptanceTests.Framework;
using SIL.Utils;
//using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Test.ProjectUnpacker;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// <summary>
	/// Test the SF Import - Data Source Setup story (SFI02)
	/// </summary>
	[TestFixture]
	public class SFImport_DataSourceSetup: TeTestsBase
	{
		private AccessibilityHelper m_dialog;

		SIL.FieldWorks.Test.ProjectUnpacker.RegistryData m_saveRegistryData;

		/// <summary>If this registry key exists, Paratext is (probably) installed</summary>
		private const string kPTSettingsRegKey = @"SOFTWARE\ScrChecks\1.0\Settings_Directory";

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SFImport_DataSourceSetup"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SFImport_DataSourceSetup(): base(true)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare Paratext test folder
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureInit()
		{
			Unpacker.UnPackParatextTestProjects();
			base.FixtureInit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove Paratext test folder
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureShutdown()
		{
			base.FixtureShutdown();
			Unpacker.RemoveParatextTestProjects();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts TE and goes to the Import dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Init()
		{
			base.Init();

			Thread.Sleep(500);

			// Select "File/Import/Standard Format" to bring up the import dialog
			m_app.SendKeys("%F");
			m_app.SendKeys("{DOWN 8}");
			m_app.SendKeys("{ENTER}");
			m_app.SendKeys("{ENTER}");
			Application.DoEvents();

			AccessibilityHelper importDlg =
				m_app.MainAccessibilityHelper.Parent.FindDirectChild("Import Standard Format",
				AccessibleRole.None);

			// select the "Import" button which is the default button
			if (importDlg != null)
				m_app.SendKeys("{ENTER}");

			Application.DoEvents();

			m_dialog = m_app.MainAccessibilityHelper.Parent.FindDirectChild(
				"Import Standard Format", AccessibleRole.None);

			m_saveRegistryData = Unpacker.PrepareRegistryForPTData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exits the import dialog and TE
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Shutdown()
		{
			if (m_saveRegistryData != null)
				m_saveRegistryData.RestoreRegistryData();

			if (m_dialog!= null)
			{
				Win32.SetForegroundWindow(m_dialog.HWnd);
				m_app.SendKeys("{ESC}");
				Application.DoEvents();
			}
			base.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the overview step is properly displayed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverviewPage()
		{
			Assert.IsNotNull(m_dialog, "Didn't get an Import dialog");

			AccessibilityHelper overviewWnd = m_dialog.FindChild("Overview",
				AccessibleRole.Window);

			// make sure the step that contains the overview is visible
			AccessibilityHelper step = overviewWnd.Parent;
			Assert.IsTrue((step.States & AccessibleStates.Invisible) != AccessibleStates.Invisible,
				"Overview step is not visible");

			// make sure it displays the language project name
			bool fFoundLangProjName = false;
			foreach (AccessibilityHelper child in step)
			{
				if ((child.States & AccessibleStates.Invisible) == AccessibleStates.Invisible)
					continue;

				if (child.Value.IndexOf("DEB-Debug") > -1)
					fFoundLangProjName = true;
			}

			Assert.IsTrue(fFoundLangProjName, "Can't see Language Project name on dialog");

			// make sure there are no other controls in the step except text boxes
			AccessibleRole[] roles =
				new AccessibleRole[] { AccessibleRole.Text,
										AccessibleRole.Client, AccessibleRole.Window,
										AccessibleRole.StaticText};
			Array.Sort(roles);

			Assert.IsTrue(CheckControls(step, roles), "Overview page contains unexpected controls");


		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the controls on the step and see if there are non-excaptable controls on it,
		/// i.e. for the overview page return false if there are any check boxes.
		/// </summary>
		/// <param name="parent">The element to start with</param>
		/// <param name="acceptableRoles">Sorted array of acceptable controls</param>
		/// <returns><c>true</c> if all visible controls are acceptable, otherwise
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool CheckControls(AccessibilityHelper parent, AccessibleRole[] acceptableRoles)
		{
			bool fRet = true;
			foreach (AccessibilityHelper child in parent)
			{
				if ((child.States & AccessibleStates.Invisible) == AccessibleStates.Invisible)
					continue;

				if (Array.BinarySearch(acceptableRoles, child.Role) < 0)
					return false;

				if (child.IsRealAccessibleObject)
					fRet = CheckControls(child, acceptableRoles);

				if (!fRet)
					return fRet;
			}

			return fRet;

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the data format page
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFormatPage()
		{
			Assert.IsNotNull(m_dialog, "Didn't get an Import dialog");

			m_app.SendKeys("%N");
			Application.DoEvents();

			AccessibilityHelper step2 = m_dialog.FindChild("Step 2", AccessibleRole.None);
			Assert.IsNotNull(step2, "Can't find Step 2");

			// first test with Paratext "installed"
			int nRequiredRbs = 0;
			CheckRadioButtons(step2, ref nRequiredRbs);
			Assert.AreEqual(2, nRequiredRbs, "Unexpected number of radio buttons");

			// then without
			//TODO DavidO: Unsure of these changes; need to fix and reenable this test
			//PrepareParatextProjectTestData.RestoreRegistry();
			//PrepareParatextProjectTestData.TestFolder = null;
			//PrepareParatextProjectTestData.PrepareRegistryForTestData();
			m_saveRegistryData.RestoreRegistryData();
			//Unpacker.TEStyleFileTestFolder = null;
			m_saveRegistryData = Unpacker.PrepareRegistryForPTData();

			// go back to overview page and then go again to second step
			m_app.SendKeys("%B");
			m_app.SendKeys("%N");
			Application.DoEvents();

			nRequiredRbs = 0;
			CheckRadioButtons(step2, ref nRequiredRbs);
			Assert.AreEqual(2, nRequiredRbs, "Unexpected number of radio buttons");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the radio buttons. We should have exactly two: Paratext and Other
		/// </summary>
		/// <param name="parent">The element to start with</param>
		/// <param name="nRbsFound">Number of radio buttons found.</param>
		/// <remarks>On return <paramref name="nRbsFound"/> should be 2. If there are additional
		/// radio buttons on the dialog, <paramref name="nRbsFound"/> will return a negative
		/// number.</remarks>
		/// ------------------------------------------------------------------------------------
		protected void CheckRadioButtons(AccessibilityHelper parent, ref int nRbsFound)
		{
			foreach (AccessibilityHelper child in parent)
			{
				if ((child.States & AccessibleStates.Invisible) == AccessibleStates.Invisible)
					continue;

				if (child.Role == AccessibleRole.RadioButton)
				{
					if (child.Name == "Paratext")
					{
						// paratext radio button has to be default if paratext is installed
						RegistryKey regKey = Registry.LocalMachine.OpenSubKey(kPTSettingsRegKey);
						if (regKey != null)
							Assert.AreEqual(AccessibleStates.Checked, (child.States & AccessibleStates.Checked),
								"Paratext is installed, but Paratext radio button "
								+ "is not the default.");

						nRbsFound++;
					}
					else if (child.Name == "Other")
					{
						// Other radio button has to be default if Paratext isn't installed
						RegistryKey regKey = Registry.LocalMachine.OpenSubKey(kPTSettingsRegKey);
						if (regKey == null)
							Assert.AreEqual(AccessibleStates.Checked, (child.States & AccessibleStates.Checked),
								"Paratext is not installed, but Other radio button is not the default.");

						nRbsFound++;
					}
					else
						nRbsFound = -99;
				}

				if (child.IsRealAccessibleObject)
					CheckRadioButtons(child, ref nRbsFound);
			}
		}

	}
}
