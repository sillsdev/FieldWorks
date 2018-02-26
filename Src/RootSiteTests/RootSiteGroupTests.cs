// Copyright (c) 2005-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Rhino.Mocks;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorerTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;

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
			var rootBox = MockRepository.GenerateMock<IVwRootBox>();
			// result for bt pane
			rootBox.Expect(r => r.Height).Return(1100);
			rootBox.Expect(r => r.Width).Return(100);

			IPublisher publisher;
			ISubscriber subscriber;
			using (var propertyTable = TestSetupServices.SetupTestTriumvirate(out publisher, out subscriber))
			using (DummyBasicView stylePane = new DummyBasicView(), draftPane = new DummyBasicView(), btPane = new DummyBasicView())
			{
				var flexComponentParameterObject = new FlexComponentParameters(propertyTable, publisher, subscriber);
				stylePane.InitializeFlexComponent(flexComponentParameterObject);
				draftPane.InitializeFlexComponent(flexComponentParameterObject);
				btPane.InitializeFlexComponent(flexComponentParameterObject);
				using (RootSiteGroup group = new RootSiteGroup())
				{
					PrepareView(stylePane, 50, 300, (IVwRootBox)rootBox);
					PrepareView(draftPane, 150, 300, (IVwRootBox)rootBox);
					PrepareView(btPane, 150, 300, (IVwRootBox)rootBox);

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
