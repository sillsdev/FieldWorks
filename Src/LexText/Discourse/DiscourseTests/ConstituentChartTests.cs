// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2013-01-30 ConstituentChartTests.cs

using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.Windows.Forms.Widgets;

namespace SIL.FieldWorks.Discourse
{

	/// <summary/>
	public class FakeConstituentChart : ConstituentChart
	{
		/// <summary/>
		public FakeConstituentChart(LcmCache cache) : base(cache)
		{
		}

		/// <summary/>
		public int m_SetHeaderColAndButtonWidths_callCount = 0;

		/// <summary>
		/// Just keep track of calls.
		/// </summary>
		protected override void SetHeaderColAndButtonWidths()
		{
			m_SetHeaderColAndButtonWidths_callCount++;
		}
	}

	/// <summary/>
	[TestFixture]
	public class ConstituentChartTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary/>
		[Test]
		public void Basic()
		{
			using(var chart = new ConstituentChart(Cache))
			{
			}
		}

		/// <summary/>
		[Test]
		public void LayoutIgnoredDuringColumnResizing()
		{
			using (var chart = new FakeConstituentChart(Cache))
			{
				ChartHeaderView headerView = ReflectionHelper.GetField(chart, "m_headerMainCols") as ChartHeaderView;
				for (int i = 0; i < 5; i++)
				{
					headerView.Controls.Add(new HeaderLabel());
				}

				// Reset count
				chart.m_SetHeaderColAndButtonWidths_callCount = 0;
				for (int i = 1; i < headerView.Controls.Count; i++)
				{
					var header = headerView[i];
					header.Cursor = Cursors.VSplit;
					ReflectionHelper.CallMethod(headerView, "OnColumnMouseDown", header, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
					ReflectionHelper.CallMethod(headerView, "OnColumnMouseMove", header, new MouseEventArgs(MouseButtons.Left, 0, 5, 0, 0));

				}
				Assert.That(chart.m_SetHeaderColAndButtonWidths_callCount, Is.EqualTo(0), "Layout event during column resizing should not have been called.");
			}
		}
	}
}
