// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RootSiteGroupTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;

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
			DynamicMock rootBox = new DynamicMock(typeof(IVwRootBox));
			// This was taken out because it doesn't seem like the views code does this
			// anymore. It just calls AdjustScrollRange for the original view that changed.
			// Done as a part of TE-3576
			//// result for style pane
			//rootBox.ExpectAndReturn("Height", 900);
			//rootBox.ExpectAndReturn("Width", 100);
			//rootBox.ExpectAndReturn("Height", 1000);
			//rootBox.ExpectAndReturn("Width", 100);
			//rootBox.ExpectAndReturn("Height", 1000);
			//rootBox.ExpectAndReturn("Width", 100);
			//// result for draft pane
			//rootBox.ExpectAndReturn("Height", 950);
			//rootBox.ExpectAndReturn("Width", 100);
			//rootBox.ExpectAndReturn("Height", 900);
			//rootBox.ExpectAndReturn("Width", 100);
			//rootBox.ExpectAndReturn("Height", 1000);
			//rootBox.ExpectAndReturn("Width", 100);
			// result for bt pane
			rootBox.ExpectAndReturn("Height", 1100);
			rootBox.ExpectAndReturn("Width", 100);
			rootBox.ExpectAndReturn("Height", 900);
			rootBox.ExpectAndReturn("Width", 100);
			rootBox.ExpectAndReturn("Height", 950);
			rootBox.ExpectAndReturn("Width", 100);

			using (RootSiteGroup group = new RootSiteGroup())
			{
				DummyBasicView stylePane = new DummyBasicView();
				DummyBasicView draftPane = new DummyBasicView();
				DummyBasicView btPane = new DummyBasicView();

				PrepareView(stylePane, 50, 300, (IVwRootBox)rootBox.MockInstance);
				PrepareView(draftPane, 150, 300, (IVwRootBox)rootBox.MockInstance);
				PrepareView(btPane, 150, 300, (IVwRootBox)rootBox.MockInstance);

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
				//stylePane.AdjustScrollRange(null, 0, 0, -100, 500);
				//draftPane.AdjustScrollRange(null, 0, 0, -50, 500);
				btPane.AdjustScrollRange(null, 0, 0, 100, 500);

				Assert.AreEqual(1108, btPane.ScrollMinSize.Height, "Wrong ScrollMinSize");
				Assert.AreEqual(800, -btPane.ScrollPosition.Y, "Wrong scroll position");
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
				Assert.IsNull(group.CastAsIVwRootSite());
			}
		}
	}
}
