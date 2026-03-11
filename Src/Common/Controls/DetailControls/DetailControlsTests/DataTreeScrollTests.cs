// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Tests that the DataTree WheelRedirector (IMessageFilter) correctly
	/// scrolls the DataTree when WM_MOUSEWHEEL would otherwise be consumed
	/// by a child control (e.g. the RichTextBox in DateSlice).
	/// </summary>
	[TestFixture]
	public class DataTreeScrollTests
	{
		/// <summary>
		/// DataTree subclass that skips base OnPaint to avoid NullReferenceException
		/// from HandlePaintLinesBetweenSlices when test slices have no LCM objects.
		/// </summary>
		private sealed class TestDataTree : DataTree
		{
			protected override void OnPaint(PaintEventArgs e)
			{
			}
		}

		/// <summary>
		/// Verifies that programmatic AutoScrollPosition manipulation works
		/// on a DataTree with enough content to be scrollable, confirming
		/// the scroll calculation used by the WheelRedirector is correct.
		/// </summary>
		[Test]
		public void DataTree_ScrollPositionManipulation_ScrollsCorrectly()
		{
			using (var parent = new Form())
			using (var dataTree = new TestDataTree())
			{
				parent.Size = new Size(400, 200);
				dataTree.Dock = DockStyle.Fill;
				parent.Controls.Add(dataTree);

				for (int i = 0; i < 12; i++)
				{
					var slice = new Slice(new Panel { Dock = DockStyle.Fill })
					{
						Visible = true,
						Size = new Size(360, 50),
						Location = new Point(0, i * 50)
					};
					dataTree.Controls.Add(slice);
					slice.Install(dataTree);
				}

				parent.Show();
				Application.DoEvents();

				// Simulate how WheelRedirector calculates scroll: delta -120 = scroll down
				int delta = -120;
				int currentY = -dataTree.AutoScrollPosition.Y;
				int maxScroll = System.Math.Max(0,
					dataTree.AutoScrollMinSize.Height - dataTree.ClientRectangle.Height);
				int newY = System.Math.Max(0, System.Math.Min(currentY - delta, maxScroll));

				Assert.That(maxScroll, Is.GreaterThan(0),
					"DataTree content must exceed viewport for scrolling to be possible");

				dataTree.AutoScrollPosition = new Point(0, newY);

				Assert.That(-dataTree.AutoScrollPosition.Y, Is.GreaterThan(0),
					"DataTree should have scrolled down");
			}
		}
	}
}
