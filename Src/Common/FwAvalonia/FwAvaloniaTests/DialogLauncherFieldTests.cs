// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D4 (wave 4) — the dialog-launcher row control: the field's
	/// current value as read-only text plus the launcher button, drawn as the shared hover-revealed
	/// settings gear (it replaced the legacy always-visible "..."). The button invokes the injected
	/// callback (the host's ILegacyDialogLauncher seam on the xWorks side); without a callback the
	/// button renders disabled with an explanatory tooltip, and the value still shows. Hover-reveal
	/// behavior itself is pinned in <see cref="HoverRevealTests"/>.
	/// </summary>
	[TestFixture]
	public class DialogLauncherFieldTests
	{
		private static (FwDialogLauncherField Row, Window Window) Show(FwDialogLauncherField row)
		{
			var window = new Window { Content = row, Width = 420, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (row, window);
		}

		private static Button LauncherButton(FwDialogLauncherField row)
			=> row.GetVisualDescendants().OfType<Button>().Single();

		[AvaloniaTest]
		public void EmptyRow_WithLauncher_RendersTheGearButton_NoValueText()
		{
			// Initial stage: a launchable field whose value is not set yet — the read-only value area is empty
			// but the (hover-revealed) gear launcher is present and enabled.
			var (row, window) = Show(new FwDialogLauncherField(string.Empty, "Inflection Features", () => { }));

			DialogSnapshot.Capture(window, "DialogLauncherField-01-initial");
			DialogLayoutAssert.AssertNoCrowding(row);

			Assert.That(row.Value, Is.Empty);
			Assert.That(LauncherButton(row).IsEnabled, Is.True, "a host callback makes the launcher available");
		}

		[AvaloniaTest]
		public void RendersTheValueText_AndTheGearLauncherButton()
		{
			var (row, window) = Show(new FwDialogLauncherField("[NOUN: common]", "Inflection Features", () => { }));

			DialogSnapshot.Capture(window, "DialogLauncherField-02-with-value");
			DialogLayoutAssert.AssertNoCrowding(row);

			var value = row.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => t.Text == "[NOUN: common]");
			Assert.That(value, Is.Not.Null, "the field's current value renders as read-only text");
			Assert.That(row.Value, Is.EqualTo("[NOUN: common]"));

			var button = LauncherButton(row);
			Assert.That(button.Content, Is.InstanceOf<Avalonia.Controls.Shapes.Path>(),
				"the launcher draws the shared settings gear (the legacy '...' is gone)");
			Assert.That(button.IsEnabled, Is.True);
			Assert.That(button.IsVisible, Is.True, "hidden by opacity on idle, never collapsed");
		}

		[AvaloniaTest]
		public void ButtonClick_InvokesTheInjectedCallback()
		{
			var calls = 0;
			var (row, _) = Show(new FwDialogLauncherField("value", "Media File", () => calls++));

			LauncherButton(row).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

			Assert.That(calls, Is.EqualTo(1), "the button-click path runs the host's launcher");
		}

		[AvaloniaTest]
		public void WithoutACallback_TheButtonIsDisabled_WithATooltip_AndTheValueStillShows()
		{
			var (row, window) = Show(new FwDialogLauncherField("hello.wav", "Media File", null));

			DialogSnapshot.Capture(window, "DialogLauncherField-03-disabled-no-service");
			DialogLayoutAssert.AssertNoCrowding(row);

			Assert.That(row.CanLaunch, Is.False);
			var button = LauncherButton(row);
			Assert.That(button.IsEnabled, Is.False,
				"no host dialog service: the launcher affordance is visibly unavailable");
			Assert.That(ToolTip.GetTip(button), Is.EqualTo(FwAvaloniaStrings.LauncherUnavailable),
				"the disabled button explains itself");
			Assert.That(row.GetVisualDescendants().OfType<TextBlock>()
				.Any(t => t.Text == "hello.wav"), Is.True, "the value still renders");
			Assert.That(() => row.Launch(), Throws.Nothing, "launching without a callback is a no-op");
		}

		[AvaloniaTest]
		public void NullValue_RendersEmpty_NeverThrows()
		{
			var (row, _) = Show(new FwDialogLauncherField(null, null, () => { }));
			Assert.That(row.Value, Is.Empty);
		}
	}
}
