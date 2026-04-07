// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	internal class DpiLayoutDummyRootSite : SimpleRootSite
	{
		internal int LayoutWidth
		{
			get { return m_dxdLayoutWidth; }
			set { m_dxdLayoutWidth = value; }
		}

		internal Point CurrentDpi
		{
			get { return Dpi; }
			set { Dpi = value; }
		}
	}

	[TestFixture]
	public class DpiLayoutTests
	{
		[Test]
		public void DpiSetter_DoesNotForceLayout_WhenDpiIsUnchanged()
		{
			var site = new DpiLayoutDummyRootSite();
			try
			{
				site.LayoutWidth = 320;
				site.CurrentDpi = new Point(96, 96);

				Assert.That(site.LayoutWidth, Is.EqualTo(320));
			}
			finally
			{
				site.Dispose();
			}
		}

		[Test]
		public void DpiSetter_ForcesLayout_WhenDpiChangesAfterLayout()
		{
			var site = new DpiLayoutDummyRootSite();
			try
			{
				site.LayoutWidth = 320;
				site.CurrentDpi = new Point(144, 144);

				Assert.That(site.LayoutWidth, Is.EqualTo(SimpleRootSite.kForceLayout));
			}
			finally
			{
				site.Dispose();
			}
		}
	}
}