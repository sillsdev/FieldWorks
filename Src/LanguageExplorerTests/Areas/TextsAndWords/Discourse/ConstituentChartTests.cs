// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using LanguageExplorer.Impls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Areas.TextsAndWords.Discourse
{
	/// <summary />
	[TestFixture]
	public class ConstituentChartTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary />
		[Test]
		public void Basic()
		{
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (new ConstituentChart(Cache, new SharedEventHandlers()))
				{
				}
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}

		/// <summary/>
		[Test]
		public void LayoutIgnoredDuringColumnResizing()
		{
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var chart = new FakeConstituentChart(Cache, new SharedEventHandlers()))
				{
					chart.InitializeFlexComponent(flexComponentParameters);
					var headerView = ReflectionHelper.GetField(chart, "m_headerMainCols") as ChartHeaderView;
					for (var i = 0; i < 5; i++)
					{
						headerView.Controls.Add(new HeaderLabel());
					}
					// Reset count
					chart.SetHeaderColAndButtonWidthsCallCount = 0;
					for (var i = 1; i < headerView.Controls.Count; i++)
					{
						var header = headerView[i];
						header.Cursor = Cursors.VSplit;
						ReflectionHelper.CallMethod(headerView, "OnColumnMouseDown", header, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
						ReflectionHelper.CallMethod(headerView, "OnColumnMouseMove", header, new MouseEventArgs(MouseButtons.Left, 0, 5, 0, 0));

					}
					Assert.That(chart.SetHeaderColAndButtonWidthsCallCount, Is.EqualTo(0), "Layout event during column resizing should not have been called.");
				}
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}

		/// <summary />
		private sealed class FakeConstituentChart : ConstituentChart
		{
			/// <summary />
			public FakeConstituentChart(LcmCache cache, ISharedEventHandlers sharedEventHandlers) : base(cache, sharedEventHandlers)
			{
			}

			/// <summary />
			internal int SetHeaderColAndButtonWidthsCallCount { get; set; }

			/// <summary>
			/// Just keep track of calls.
			/// </summary>
			protected override void SetHeaderColAndButtonWidths()
			{
				SetHeaderColAndButtonWidthsCallCount++;
			}
		}

	}
}
