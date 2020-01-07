// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing.Printing;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary />
	[TestFixture]
	public class PrintRootSiteTests
	{
		PrinterSettings m_pSettings;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_pSettings = new PrinterSettings();
		}

		/// <summary>
		/// Test the simple case of one copy, collation on and the page range is within the
		/// total number of possible pages.
		/// </summary>
		[Test]
		public void CollationTest_OneCopy()
		{
			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 5;
			m_pSettings.ToPage = 7;
			m_pSettings.Copies = 1;

			var pRootSite = new DummyPrintRootSite(10, m_pSettings);

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

		/// <summary>
		/// Test the simple case of one copy, collation on and the page range crosses the range
		/// of total available pages to print.
		/// </summary>
		[Test]
		public void CollationTest_OneCopy_InvalidRange1()
		{
			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 3;
			m_pSettings.ToPage = 7;
			m_pSettings.Copies = 1;

			var pRootSite = new DummyPrintRootSite(5, m_pSettings);

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

		/// <summary>
		/// Test the simple case of one copy, collation on and the page range is outside the
		/// the range of total available pages to print.
		/// </summary>
		[Test]
		public void CollationTest_OneCopy_InvalidRange2()
		{
			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 7;
			m_pSettings.ToPage = 9;
			m_pSettings.Copies = 1;

			var pRootSite = new DummyPrintRootSite(5, m_pSettings);
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// <summary>
		/// Test the case of multiple copies, collation on and the page range crosses the range
		/// of total available pages to print.
		/// </summary>
		[Test]
		public void CollationTest_MultipleCopy()
		{
			m_pSettings.Collate = true;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 2;
			m_pSettings.ToPage = 4;
			m_pSettings.Copies = 3;

			var pRootSite = new DummyPrintRootSite(10, m_pSettings);

			// printing handles the multiple copies automatically, so they just need
			// to be printed once.
			var expectedPages = new[] { 2, 3, 4 };
			var iteration = 1;

			foreach (var i in expectedPages)
			{
				Assert.AreEqual(i, pRootSite.NextPageToPrint, "this failed in iteration: " + iteration);
				Assert.IsTrue(pRootSite.HasMorePages, "this failed in iteration: " + iteration);
				pRootSite.Advance();
				iteration++;
			}
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// <summary>
		/// Test the case of multiple copies, collation on and the page range crosses the range
		/// of total available pages to print.
		/// </summary>
		[Test]
		public void NonCollationTest_MultipleCopy()
		{
			m_pSettings.Collate = false;
			m_pSettings.PrintRange = PrintRange.SomePages;
			m_pSettings.FromPage = 2;
			m_pSettings.ToPage = 4;
			m_pSettings.Copies = 3;
			var expectedPages = new[] { 2, 2, 2, 3, 3, 3, 4, 4, 4 };
			var iteration = 1;

			var pRootSite = new DummyPrintRootSite(10, m_pSettings);

			foreach (var i in expectedPages)
			{
				Assert.AreEqual(i, pRootSite.NextPageToPrint, "this failed in iteration: " + iteration);
				Assert.IsTrue(pRootSite.HasMorePages, "this failed in iteration: " + iteration);
				pRootSite.Advance();
				iteration++;
			}
			Assert.IsFalse(pRootSite.HasMorePages);
		}

		/// <summary>
		/// Instantiate a print root site for testing.
		/// </summary>
		private sealed class DummyPrintRootSite : PrintRootSite
		{
			/// <summary />
			public DummyPrintRootSite(int totalPages, PrinterSettings psettings)
				: base(totalPages, psettings)
			{
			}

			/// <summary />
			public new void Advance()
			{
				base.Advance();
			}
		}
	}
}