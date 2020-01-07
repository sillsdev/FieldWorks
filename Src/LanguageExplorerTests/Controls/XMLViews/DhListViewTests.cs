// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Controls.XMLViews
{
	/// <summary />
	[TestFixture]
	public class DhListViewTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary />
		[Test]
		public void Basic()
		{
			using (var view = new DhListView(null))
			{
				Assert.That(view, Is.Not.Null);
			}
		}

		/// <summary />
		[Test]
		public void IsThisColumnChangeAllowable_Callable()
		{
			using (var view = new FakeDhListView(null))
			{
				ReflectionHelper.GetBoolResult(view, "IsThisColumnChangeAllowable", 0, 0, 0);
			}
		}

		/// <summary />
		[Test]
		[TestCase(0, 50, 100, true, false)]
		[TestCase(0, 50, 100, false, true)]
		[TestCase(1, 50, 10, true, false)]
		[TestCase(1, 50, 100, true, true)]
		[TestCase(1, 5, 10, true, true)]
		[TestCase(1, 5, 5, true, true)]
		public  void IsThisColumnChangeAllowable(int columnIndex, int currentWidth, int requestedWidth, bool hasCheckMarkColumn, bool expected)
		{
			bool actual;
			using (var view = new FakeDhListView(null))
			{
				view.m_hasCheckBoxColumn = hasCheckMarkColumn;
				actual = ReflectionHelper.GetBoolResult(view, "IsThisColumnChangeAllowable", columnIndex, currentWidth, requestedWidth);
			}
			Assert.That(actual, Is.EqualTo(expected));
		}

		/// <summary />
		private sealed class FakeDhListView : DhListView
		{
			/// <summary/>
			public bool m_hasCheckBoxColumn = true;

			/// <summary/>
			public override bool HasCheckBoxColumn => m_hasCheckBoxColumn;

			/// <summary/>
			internal FakeDhListView(BrowseViewer bv) : base(bv)
			{
			}
		}
	}
}