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
// File: ScrImportBackupMessageTests.cs
// Responsibility: TeTeam
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace SIL.FieldWorks.ScrImportComponents
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for ScrImportBackupMessage
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrImportBackupMessageTest : ScrImportBackupMessage
	{
		RegistryKey m_key =
			Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\Translation Editor");

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrImportBackupMessage"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrImportBackupMessageTest()
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
			CheckDisposed();

			Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();

			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the tab order
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TabOrderTest()
		{
			CheckDisposed();

			Assert.AreEqual(0, btnBackup.TabIndex);
			Assert.AreEqual(1, btnImport.TabIndex);
			Assert.AreEqual(2, btnCancel.TabIndex);
			Assert.AreEqual(3, chkDontBugMe.TabIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the return values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReturnValuesTest()
		{
			CheckDisposed();

			btnBackup.PerformClick();
			Assert.AreEqual(System.Windows.Forms.DialogResult.Yes, DialogResult);

			Show();
			btnImport.PerformClick();
			Assert.AreEqual(System.Windows.Forms.DialogResult.No, DialogResult);

			Show();
			btnCancel.PerformClick();
			Assert.AreEqual(System.Windows.Forms.DialogResult.Cancel, DialogResult);

			Show();
		}
	}
}