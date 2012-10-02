// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportWizardTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.TE.ImportComponentsTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class derived from ScrImportSetMessage for test purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScrImportSetMessage : ScrImportSetMessage
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help button on the dummy message box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new Button HelpButton
		{
			get
			{
				CheckDisposed();
				return this.btnHelp;
			}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for IScrImportSet
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrImportSetMessageTest: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		private DummyScrImportSetMessage m_dummyMsgBox;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrImportSetMessage"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrImportSetMessageTest()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_dummyMsgBox = new DummyScrImportSetMessage();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			m_dummyMsgBox.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HelpBtnHiddenTest()
		{
			m_dummyMsgBox.Show();
			Assert.IsFalse(m_dummyMsgBox.HelpButton.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HelpBtnVisibleTest()
		{
			m_dummyMsgBox.HelpURL = "gumby";
			m_dummyMsgBox.HelpTopic = "pokey";
			m_dummyMsgBox.Show();
			Assert.IsTrue(m_dummyMsgBox.HelpButton.Visible);
		}
	}
}
