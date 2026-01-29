// Copyright (c) 2005-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Moq;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Summary description for RootSiteGroupTests.
	/// </summary>
	[TestFixture]
	public class RootSiteGroupTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="rootSite"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="rootb"></param>
		/// ------------------------------------------------------------------------------------
		private void PrepareView(DummyBasicView rootSite, int width, int height,
			IVwRootBox rootb)
		{
			rootSite.Visible = false;
			rootSite.Width = width;
			rootSite.Height = height;
			rootSite.SetRootBox(rootb);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the adjusting the scroll range
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange()
		{
			var rootBoxMock = new Mock<IVwRootBox>();
			// This was taken out because it doesn't seem like the views code does this
			// anymore. It just calls AdjustScrollRange for the original view that changed.
			// Done as a part of TE-3576
			// result for bt pane
			rootBoxMock.Setup(r => r.Height).Returns(1100);
			rootBoxMock.Setup(r => r.Width).Returns(100);

			using (DummyBasicView stylePane = new DummyBasicView(),
				draftPane = new DummyBasicView(),
				btPane = new DummyBasicView())
			{
				using (RootSiteGroup group = new RootSiteGroup())
				{
					PrepareView(stylePane, 50, 300, rootBoxMock.Object);
					PrepareView(draftPane, 150, 300, rootBoxMock.Object);
					PrepareView(btPane, 150, 300, rootBoxMock.Object);

					group.AddToSyncGroup(stylePane);
					group.AddToSyncGroup(draftPane);
					group.AddToSyncGroup(btPane);
					group.ScrollingController = btPane;
					group.Controls.AddRange(new Control[] { stylePane, draftPane, btPane } );

					btPane.ScrollMinSize = new Size(100, 1000);
					btPane.ScrollPosition = new Point(0, 700);

					// now call AdjustScrollRange on each of the panes.
					// This simulates what the views code does.
					// This was taken out because it doesn't seem like the views code does this
					// anymore. It just calls AdjustScrollRange for the original view that changed.
					// Done as a part of TE-3576
					btPane.AdjustScrollRange(null, 0, 0, 100, 500);

					Assert.That(btPane.ScrollMinSize.Height, Is.EqualTo(1108), "Wrong ScrollMinSize");
					Assert.That(-btPane.ScrollPosition.Y, Is.EqualTo(800), "Wrong scroll position");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that CastAsIVwRootSite returns null (and doesn't crash) when the
		/// group is empty (i.e., no rootsites have been added yet).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCastAsIVwRootSiteWhenGroupIsEmpty()
		{
			using (RootSiteGroup group = new RootSiteGroup())
			{
				Assert.That(group.CastAsIVwRootSite(), Is.Null);
			}
		}
	}
}
