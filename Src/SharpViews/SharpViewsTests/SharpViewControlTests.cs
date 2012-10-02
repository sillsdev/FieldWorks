using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Tests of the SharpViews control itself. These are sketchy, because this class is deliberately kept as simple
	/// as possible, since being a real control it is hard to test it without showing it on the screen.
	/// </summary>
	[TestFixture]
	public class SharpViewControlTests : BaseTest
	{
		/// <summary>
		/// Tests the code that sets the autoscroll range based on the size of the root box.
		/// </summary>
		[Test]
		public void ScrollRange()
		{
			var sv = new SharpView();
			sv.Size = new Size(100, 200);
			sv.SetScrollRange(60, 300);
		}
	}
}
