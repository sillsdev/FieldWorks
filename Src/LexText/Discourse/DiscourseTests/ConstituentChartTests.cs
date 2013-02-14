// Copyright (c) 2013, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2013-01-30 ConstituentChartTests.cs

using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Discourse
{

	/// <summary/>
	public class FakeConstituentChart : ConstituentChart
	{
		/// <summary/>
		public FakeConstituentChart(FdoCache cache) : base(cache)
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
				// Reset count
				chart.m_SetHeaderColAndButtonWidths_callCount = 0;
				ReflectionHelper.CallMethod(chart, "m_headerMainCols_ColumnWidthChanging", new object[] { null, null });
				ReflectionHelper.CallMethod(chart, "m_headerMainCols_Layout", new object[] { null, null });
				Assert.That(chart.m_SetHeaderColAndButtonWidths_callCount, Is.EqualTo(0), "Layout event during column resizing should have done nothing.");
			}
		}
	}
}
