// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FilterBookDialogTests.cs
// Responsibility:
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
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	#region DummyFilterBookDialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exposes FilterBookDialog stuff for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFilterBookDialog : FilterBookDialog
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilterBookDialog"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoList">A list of books to check as an array of hvos</param>
		/// -----------------------------------------------------------------------------------
		public DummyFilterBookDialog(FdoCache cache, int[] hvoList): base(cache, hvoList)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DontShow()
		{
			CheckDisposed();

			CreateHandle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the OK button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button OKButton
		{
			get
			{
				CheckDisposed();
				return m_btnOK;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select a book in the list
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="state"></param>
		/// ------------------------------------------------------------------------------------
		public void SelectBook(int hvo, TriStateTreeView.CheckState state)
		{
			CheckDisposed();

			m_treeScripture.CheckNodeByTag(hvo, state);
		}
	}
	#endregion

	/// <summary>
	/// Tests for FilterBookDialog
	/// </summary>
	[TestFixture]
	public class FilterBookDialogTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private IScrBook m_genesis;
		private IScrBook m_exodus;
		private IScrBook m_leviticus;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			//m_dialog.Close();
			//m_dialog = null;
			m_genesis = null;
			m_exodus = null;
			m_leviticus = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_leviticus = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the OK button is disabled if no books are selected to show.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OkButtonDisabledIfNothingSelected()
		{
			CheckDisposed();

			using (DummyFilterBookDialog dialog = new DummyFilterBookDialog(Cache, new int[] { }))
			{
				dialog.DontShow();
				Assert.IsFalse(dialog.OKButton.Enabled);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the OK button is enabled if books are in the filter
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OkButtonEnabledIfSomethingSelected()
		{
			CheckDisposed();

			using (DummyFilterBookDialog dialog = new DummyFilterBookDialog(Cache, new int[] { m_genesis.Hvo }))
			{
				dialog.DontShow();
				Assert.IsTrue(dialog.OKButton.Enabled);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that OK button gets enabled when something gets selected
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OkButtonGetsEnabledIfSelected()
		{
			CheckDisposed();

			using (DummyFilterBookDialog dialog = new DummyFilterBookDialog(Cache, new int[] { }))
			{
				dialog.DontShow();
				dialog.SelectBook(m_genesis.Hvo, TriStateTreeView.CheckState.Checked);

				Assert.IsTrue(dialog.OKButton.Enabled);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the OK button gets disabled if all books are removed from filter
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OkButtonGetsDisabledIfNothingSelected()
		{
			CheckDisposed();

			using (DummyFilterBookDialog dialog = new DummyFilterBookDialog(Cache, new int[] { m_genesis.Hvo }))
			{
				dialog.DontShow();
				dialog.SelectBook(m_genesis.Hvo, TriStateTreeView.CheckState.Unchecked);

				Assert.IsFalse(dialog.OKButton.Enabled);
			}
		}
	}
}
