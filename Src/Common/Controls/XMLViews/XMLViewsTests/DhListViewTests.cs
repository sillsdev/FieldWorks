// Copyright (c) 2013, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2013-04-12 DhListViewTests.cs

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace XMLViewsTests
{
	/// <summary/>
	[TestFixture]
	public class DhListViewTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary/>
		[SetUp]
		public void SetUp()
		{
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
		}

		/// <summary/>
		public class FakeDhListView : DhListView
		{
			/// <summary/>
			public bool m_hasCheckBoxColumn = true;

			/// <summary/>
			public override bool HasCheckBoxColumn
			{
				get
				{
					return m_hasCheckBoxColumn;
				}
			}

			/// <summary/>
			public FakeDhListView(BrowseViewer bv) : base(bv)
			{
			}
		}

		/// <summary/>
		[Test]
		public void Basic()
		{
			using (var view = new DhListView(null))
			{
				Assert.That(view, Is.Not.Null);
			}
		}

		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_Callable()
		{
			using (var view = new FakeDhListView(null))
			{
				ReflectionHelper.GetBoolResult(view, "IsThisColumnChangeAllowable", new object[] {0, 0, 0});
			}
		}

		/// <summary>
		/// Helper for unit tests
		/// </summary>
		static void IsThisColumnChangeAllowable_Helper(int columnIndex, int currentWidth, int requestedWidth, bool hasCheckMarkColumn, bool expected)
		{
			bool actual;
			using (var view = new FakeDhListView(null))
			{
				view.m_hasCheckBoxColumn = hasCheckMarkColumn;
				actual = ReflectionHelper.GetBoolResult(view, "IsThisColumnChangeAllowable", new object[] { columnIndex, currentWidth, requestedWidth });
			}
			Assert.That(actual, Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_HavingCheckMarkColumn_MeansItIsImmutableAndChangeRejected()
		{
			int column = 0;
			int currentWidth = 50;
			int requestedWidth = 100;
			bool hasCheckMarkColumn = true;
			bool expected = false;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}

		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_NotHavingCheckMarkColumn_SoColumn0ShouldBeMutable()
		{
			int column = 0;
			int currentWidth = 50;
			int requestedWidth = 100;
			bool hasCheckMarkColumn = false;
			bool expected = true;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}

		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_MutableColumn_Allowed()
		{
			int column = 1;
			int currentWidth = 50;
			int requestedWidth = 100;
			bool hasCheckMarkColumn = true;
			bool expected = true;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}


		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_TooSmallWidth_Rejected()
		{
			int column = 1;
			int currentWidth = 50;
			int requestedWidth = 10;
			bool hasCheckMarkColumn = true;
			bool expected = false;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}


		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_BigEnoughWidth_Acceptable()
		{
			int column = 1;
			int currentWidth = 50;
			int requestedWidth = 100;
			bool hasCheckMarkColumn = true;
			bool expected = true;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}

		/// <summary/>
		[Test]
		public void IsThisColumnChangeAllowable_TooSmallWidthButBiggerThanCurrent_Allowed()
		{
			int column = 1;
			int currentWidth = 5;
			int requestedWidth = 10;
			bool hasCheckMarkColumn = true;
			bool expected = true;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}

		/// <summary>Equal to current is okay too. Otherwise the column is difficult to
		/// grow in size if starts off too small.</summary>
		[Test]
		public void IsThisColumnChangeAllowable_TooSmallWidthButEqualToCurrent_Allowed()
		{
			int column = 1;
			int currentWidth = 5;
			int requestedWidth = 5;
			bool hasCheckMarkColumn = true;
			bool expected = true;

			IsThisColumnChangeAllowable_Helper(column, currentWidth, requestedWidth, hasCheckMarkColumn, expected);
		}
	}
}