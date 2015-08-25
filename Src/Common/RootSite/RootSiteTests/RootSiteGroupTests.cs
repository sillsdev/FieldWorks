// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RootSiteGroupTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using NMock;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Summary description for RootSiteGroupTests.
	/// </summary>
	[TestFixture]
	public class RootSiteGroupTests : BaseTest
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

			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			{
				using (DummyBasicView stylePane = new DummyBasicView(),
					draftPane = new DummyBasicView(),
					btPane = new DummyBasicView())
				{
					stylePane.InitializeFlexComponent(propertyTable, publisher, subscriber);
					draftPane.InitializeFlexComponent(propertyTable, publisher, subscriber);
					btPane.InitializeFlexComponent(propertyTable, publisher, subscriber);
					using (RootSiteGroup group = new RootSiteGroup())
					{
						PrepareView(stylePane, 50, 300, (IVwRootBox)rootBox.MockInstance);
						PrepareView(draftPane, 150, 300, (IVwRootBox)rootBox.MockInstance);
						PrepareView(btPane, 150, 300, (IVwRootBox)rootBox.MockInstance);

						group.AddToSyncGroup(stylePane);
						group.AddToSyncGroup(draftPane);
						group.AddToSyncGroup(btPane);
						group.ScrollingController = btPane;
						group.Controls.AddRange(new Control[] { stylePane, draftPane, btPane });

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
