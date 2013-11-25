// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PageTests.cs
// Responsibility: Lothers
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Drawing;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PageTests : BaseTest
	{
		private class DummyDivisionMgr : DivisionLayoutMgr
		{
			private bool m_fMainStreamRightToLeft;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyDivisionMgr"/> class.
			/// </summary>
			/// <param name="stream">The stream.</param>
			/// <param name="fMainStreamRightToLeft">if set to <c>true</c> if the main stream in
			/// the test is right-to-left; otherwise <c>false</c>.</param>
			/// --------------------------------------------------------------------------------
			public DummyDivisionMgr(IVwLayoutStream stream, bool fMainStreamRightToLeft): base(null, null, 0)
			{
				m_mainLayoutStream = stream;
				m_fMainStreamRightToLeft = fMainStreamRightToLeft;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether the main stream is right to left.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected override bool MainStreamIsRightToLeft
			{
				get
				{
					return m_fMainStreamRightToLeft;
				}
			}
		}

		private class DummyPage : Page
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyPage"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyPage(PublicationControl pub)
				: base(pub, 0, 0, 1, 0, 0)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the get last element.
			/// </summary>
			/// <param name="division">The division.</param>
			/// <param name="xd">The xd.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public PageElement CallGetLastElement(DivisionLayoutMgr division, out int xd)
			{
				return GetLastElement(division, out xd);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLastElement when there is only one element on the page.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetLastElement_OneElement()
		{
			IVwLayoutStream stream = VwLayoutStreamClass.Create();
			using (DummyDivisionMgr division = new DummyDivisionMgr(stream, false))
			{
				using (DummyPublication dummyPub = new DummyPublication(null, division, DateTime.Now))
				{
					using (DummyPage page = new DummyPage(dummyPub))
					{
						PageElement element = new PageElement(division, stream, false,
							new Rectangle(720, 1440, (int)(6.5 * 720), (int)(9 * 1440)),
							0, true, 1, 1, 0, 9 * 1440, 0, false);
						page.PageElements.Add(element);

						int xd;
						PageElement lastElement = page.CallGetLastElement(division, out xd);
						Assert.AreEqual(element.LocationOnPage.Right, xd);
						Debug.WriteLine("GetLastElement_OneElement xd: " + xd);

						Assert.AreEqual(element, lastElement);
					}
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLastElement when there are two elements with a left-to-right main stream.
		/// Even though the left-most element goes below the right-most element, the right-most
		/// element should be returned as the last element on the page.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetLastElement_TwoElementsLtoR()
		{
			IVwLayoutStream stream = VwLayoutStreamClass.Create();
			using (DummyDivisionMgr division = new DummyDivisionMgr(stream, false))
			{
				using (DummyPublication dummyPub = new DummyPublication(null, division, DateTime.Now))
				{
					using (DummyPage page = new DummyPage(dummyPub))
					{
						PageElement leftColumnElement = new PageElement(division, stream, false,
							new Rectangle(0, 1440, (int)(3 * 720), (int)(9 * 1440)),
							0, true, 1, 2, 0, 9 * 1440, 0, false);
						PageElement rightColumnElement = new PageElement(division, stream, false,
							new Rectangle((int)(3 * 720 + 360), 1440, (int)(3 * 720), (int)(5 * 1440)),
							0, true, 2, 2, 0, 5 * 1440, 0, false);

						page.PageElements.Add(leftColumnElement);
						page.PageElements.Add(rightColumnElement);

						int xd;
						PageElement lastElement = page.CallGetLastElement(division, out xd);
						Debug.WriteLine("GetLastElement_TwoElementsLtoR xd: " + xd);

						Assert.AreEqual(rightColumnElement, lastElement,
							"The right-most column should be the last element in a left-to-right writing system");
						Assert.AreEqual(rightColumnElement.LocationOnPage.Right, xd);
					}
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLastElement when there are three elements with a left-to-right main stream.
		/// Even though the left-most element goes below the right-most element, the right-most
		/// element should be returned as the last element on the page. The page in this test also
		/// begins with a page element that spans across the page beyond the second column.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetLastElement_ThreeElementsLtoR()
		{
			IVwLayoutStream stream = VwLayoutStreamClass.Create();
			using (DummyDivisionMgr division = new DummyDivisionMgr(stream, false))
			{
				using (DummyPublication dummyPub = new DummyPublication(null, division, DateTime.Now))
				using (DummyPage page = new DummyPage(dummyPub))
				{
					//				PageElement topElement = new PageElement(division, stream, false,
					//					new Rectangle(720, 0, (int)(9 * 720), 1440),
					//					0, true, 1, 1, 0, 9 * 1440, 0, false);
									PageElement leftColumnElement = new PageElement(division, stream, false,
										new Rectangle(0, 1440, (int)(3 * 720), (int)(9 * 1440)),
										0, true, 1, 2, 0, 9 * 1440, 0, false);
									PageElement rightColumnElement = new PageElement(division, stream, false,
										new Rectangle(3 * 720 + 360, 1440, (int)(3 * 720), (int)(5 * 1440)),
										0, true, 2, 2, 0, 5 * 1440, 0, false);

									page.PageElements.Add(leftColumnElement);
									page.PageElements.Add(rightColumnElement);

									int xd;
									PageElement lastElement = page.CallGetLastElement(division, out xd);
									Debug.WriteLine("GetLastElement_TwoElementsLtoR xd: " + xd);

									Assert.AreEqual(rightColumnElement, lastElement,
										"The right-most column should be the last element in a left-to-right writing system");
									Assert.AreEqual(rightColumnElement.LocationOnPage.Right, xd);
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLastElement when there are two elements with a right-to-left main stream.
		/// Even though the right-most element goes below the left-most element, the left-most
		/// element should be returned as the last element on the page.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetLastElement_TwoElementsRtoL()
		{
			IVwLayoutStream stream = VwLayoutStreamClass.Create();
			using (DummyDivisionMgr division = new DummyDivisionMgr(stream, true))
			{
				using (DummyPublication dummyPub = new DummyPublication(null, division, DateTime.Now))
				using (DummyPage page = new DummyPage(dummyPub))
				{
					PageElement leftColumnElement = new PageElement(division, stream, false,
						new Rectangle(0, 1440, (int)(3 * 720), (int)(9 * 1440)),
						0, true, 1, 2, 0, 9 * 1440, 0, true);
					PageElement rightColumnElement = new PageElement(division, stream, false,
						new Rectangle(3 * 720 + 360, 1440, (int)(3 * 720), (int)(5 * 1440)),
						0, true, 2, 2, 0, 5 * 1440, 0, true);

					page.PageElements.Add(leftColumnElement);
					page.PageElements.Add(rightColumnElement);

					int xd;
					PageElement lastElement = page.CallGetLastElement(division, out xd);
					Debug.WriteLine("GetLastElement_TwoElementsRtoL xd: " + xd);

					Assert.AreEqual(leftColumnElement, lastElement,
						"The left-most column should be the last element in a right-to-left writing system");
					Assert.AreEqual(0, xd);
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLastElement when there are two elements with a right-to-left main stream.
		/// The left-most element should be returned as the last element on the page.
		/// This test is a more-standard layout in that the first (right) column is taller than
		/// the second (left) column.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetLastElement_TwoElementsRtoLStandard()
		{
			IVwLayoutStream stream = VwLayoutStreamClass.Create();
			using (DummyDivisionMgr division = new DummyDivisionMgr(stream, true))
			using (DummyPublication dummyPub = new DummyPublication(null, division, DateTime.Now))
			using (DummyPage page = new DummyPage(dummyPub))
			{

				PageElement leftColumnElement = new PageElement(division, stream, false,
					new Rectangle(0, 1440, (int)(3 * 720), (int)(5 * 1440)),
					0, true, 1, 2, 0, 5 * 1440, 0, true);
				PageElement rightColumnElement = new PageElement(division, stream, false,
					new Rectangle(3 * 720 + 360, 1440, (int)(3 * 720), (int)(9 * 1440)),
					0, true, 2, 2, 0, 9 * 1440, 0, true);

				page.PageElements.Add(leftColumnElement);
				page.PageElements.Add(rightColumnElement);

				int xd;
				PageElement lastElement = page.CallGetLastElement(division, out xd);
				Debug.WriteLine("GetLastElement_TwoElementsRtoLStandard xd: " + xd);

				Assert.AreEqual(leftColumnElement, lastElement,
					"The left-most column should be the last element in a right-to-left writing system");
				Assert.AreEqual(0, xd);
			}
		}
	}
}
