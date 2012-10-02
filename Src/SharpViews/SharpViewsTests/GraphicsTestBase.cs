using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// A base for various tests that need a VwGraphics.
	/// </summary>
	public class GraphicsTestBase : Test.TestUtils.BaseTest
	{
		internal GraphicsManager m_gm;
		internal Graphics m_graphics;
		[SetUp]
		public void Setup()
		{
			Bitmap bmp = new Bitmap(200, 100);
			m_graphics = Graphics.FromImage(bmp);

			m_gm = new GraphicsManager(null, m_graphics);
		}

		[TearDown]
		public void Teardown()
		{
			m_gm.Dispose();
			m_gm = null;
			m_graphics.Dispose();
		}
	}
}
