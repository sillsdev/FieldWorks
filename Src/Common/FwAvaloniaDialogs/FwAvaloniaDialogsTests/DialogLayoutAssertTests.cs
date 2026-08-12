// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// Proves the <see cref="DialogLayoutAssert.AssertNoCrowding"/> tripwire actually FAILS on the defect it
	/// guards (a host border with no frame, and overlapping siblings) — so a future dialog that regresses the
	/// no-border / text-crowding defect is caught — and PASSES on a correctly framed/spaced layout.
	/// </summary>
	[TestFixture]
	public class DialogLayoutAssertTests
	{
		private static Window ShowRoot(Control root, int w = 300, int h = 200)
		{
			var window = new Window { Content = root, Width = w, Height = h };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			root.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return window;
		}

		[AvaloniaTest]
		public void Catches_HostBorderWithNoFrame()
		{
			// A PART_*Host border with NO BorderThickness — exactly the original defect.
			var bad = new StackPanel
			{
				Children =
				{
					new Border
					{
						Name = "PART_LexemeFormHost",
						Child = new TextBlock { Text = "casa" }
						// no BorderThickness => the tripwire must catch it
					}
				}
			};
			ShowRoot(bad);

			Assert.That(() => DialogLayoutAssert.AssertNoCrowding(bad), Throws.InstanceOf<AssertionException>(),
				"a PART_*Host border with no frame must trip the assertion");
		}

		[AvaloniaTest]
		public void Catches_OverlappingSiblings()
		{
			// A Canvas lets us position two text blocks so their bounds intersect — the crowding defect.
			var canvas = new Canvas { Width = 200, Height = 100 };
			var a = new TextBlock { Text = "first", Width = 100, Height = 30 };
			var b = new TextBlock { Text = "second", Width = 100, Height = 30 };
			Canvas.SetLeft(a, 0); Canvas.SetTop(a, 0);
			Canvas.SetLeft(b, 50); Canvas.SetTop(b, 10); // overlaps a
			canvas.Children.Add(a);
			canvas.Children.Add(b);
			ShowRoot(canvas);

			Assert.That(() => DialogLayoutAssert.AssertNoCrowding(canvas), Throws.InstanceOf<AssertionException>(),
				"two overlapping sibling controls must trip the assertion");
		}

		[AvaloniaTest]
		public void Passes_OnAFramedSpacedLayout()
		{
			// A host border WITH a frame + padding, stacked text that does not overlap — the fixed shape.
			var good = new StackPanel
			{
				Spacing = 8,
				Children =
				{
					new TextBlock { Text = "Lexeme form" },
					new Border
					{
						Name = "PART_LexemeFormHost",
						BorderThickness = new Thickness(1),
						BorderBrush = Brushes.Gray,
						Padding = new Thickness(4),
						Child = new TextBox { Text = "casa", MinHeight = 24 }
					},
					new TextBlock { Text = "Gloss" }
				}
			};
			ShowRoot(good);

			Assert.That(() => DialogLayoutAssert.AssertNoCrowding(good), Throws.Nothing,
				"a framed, non-overlapping, padded layout must pass the tripwire");
		}
	}
}
