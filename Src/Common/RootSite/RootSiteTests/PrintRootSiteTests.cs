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
// File: PrintRootSiteTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	#region DummyPrintRootSite
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Instantiate a print root site for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyPrintRootSite : PrintRootSite
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="totalPages"></param>
		/// <param name="psettings"></param>
		/// ------------------------------------------------------------------------------------
		public DummyPrintRootSite(int totalPages, PrinterSettings psettings) :
			base(totalPages, psettings)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void Advance()
		{
			base.Advance();
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for PrintRootSiteTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PrintRootSiteTests : BaseTest
	{
		PrinterSettings m_pSettings;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PrintRootSiteTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PrintRootSiteTests()
		{
			m_pSettings = new PrinterSettings();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_pSettings = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the simple case of one copy, collation on and the page range is within the
		/// total number of possible pages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CollationTest_OneCopy()
		{
			CheckDisposed();

			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 5;
			m_pSettings.ToPage = 7;
			m_pSettings.Copies = 1;

			DummyPrintRootSite pRootSite = new DummyPrintRootSite(10, m_pSettings);

			Assert.AreEqual(5, pRootSite.NextPageToPrint);
			Assert.IsTrue(pRootSite.HasMorePages);

			pRootSite.Advance();
			Assert.AreEqual(6, pRootSite.NextPageToPrint);
			Assert.IsTrue(pRootSite.HasMorePages);

			pRootSite.Advance();
			Assert.AreEqual(7, pRootSite.NextPageToPrint);
			Assert.IsTrue(pRootSite.HasMorePages);

			pRootSite.Advance();
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the simple case of one copy, collation on and the page range crosses the range
		/// of total available pages to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CollationTest_OneCopy_InvalidRange1()
		{
			CheckDisposed();

			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 3;
			m_pSettings.ToPage = 7;
			m_pSettings.Copies = 1;

			DummyPrintRootSite pRootSite = new DummyPrintRootSite(5, m_pSettings);

			Assert.AreEqual(3, pRootSite.NextPageToPrint);
			Assert.IsTrue(pRootSite.HasMorePages);

			pRootSite.Advance();
			Assert.AreEqual(4, pRootSite.NextPageToPrint);
			Assert.IsTrue(pRootSite.HasMorePages);

			pRootSite.Advance();
			Assert.AreEqual(5, pRootSite.NextPageToPrint);
			Assert.IsTrue(pRootSite.HasMorePages);

			pRootSite.Advance();
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the simple case of one copy, collation on and the page range is outside the
		/// the range of total available pages to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CollationTest_OneCopy_InvalidRange2()
		{
			CheckDisposed();

			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 7;
			m_pSettings.ToPage = 9;
			m_pSettings.Copies = 1;

			DummyPrintRootSite pRootSite = new DummyPrintRootSite(5, m_pSettings);
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case of multiple copies, collation on and the page range crosses the range
		/// of total available pages to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CollationTest_MultipleCopy()
		{
			CheckDisposed();

			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 2;
			m_pSettings.ToPage = 4;
			m_pSettings.Copies = 3;

			DummyPrintRootSite pRootSite = new DummyPrintRootSite(10, m_pSettings);

			// printing handles the multiple copies automatically, so they just need
			// to be printed once.
			int[] ExpectedPages = new int []{2, 3, 4};
			int iteration = 1;

			foreach(int i in ExpectedPages)
			{
				Assert.AreEqual(i, pRootSite.NextPageToPrint,
					"this failed in iteration: " + iteration);
				Assert.IsTrue(pRootSite.HasMorePages,
					"this failed in iteration: " + iteration);
				pRootSite.Advance();
				iteration++;
			}
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case of multiple copies, collation on and the page range crosses the range
		/// of total available pages to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NonCollationTest_MultipleCopy()
		{
			CheckDisposed();

			m_pSettings.Collate = false;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 2;
			m_pSettings.ToPage = 4;
			m_pSettings.Copies = 3;
			int[] ExpectedPages = new int []{2, 2, 2, 3, 3, 3, 4, 4, 4};
			int iteration = 1;

			DummyPrintRootSite pRootSite = new DummyPrintRootSite(10, m_pSettings);

			foreach(int i in ExpectedPages)
			{
				Assert.AreEqual(i, pRootSite.NextPageToPrint,
					"this failed in iteration: " + iteration);
				Assert.IsTrue(pRootSite.HasMorePages,
					"this failed in iteration: " + iteration);
				pRootSite.Advance();
				iteration++;
			}
			Assert.IsFalse(pRootSite.HasMorePages);
		}
	}
}
