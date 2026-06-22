// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Hover-reveal chrome (UI polish): the chooser's settings gear and the reference vector's
	/// separator bars + "+" launcher start hidden (opacity 0, not hit-testable, but still in
	/// layout and in the UIA tree), fade in while the pointer is over the row (label or editor),
	/// and fade out when it leaves — driven here by REAL headless mouse input. Behavior is pinned
	/// unchanged: the same flyouts open and the same staging calls fire once revealed, and the
	/// row's layout height is identical hidden vs revealed (no reflow).
	/// </summary>
	[TestFixture]
	public class HoverRevealTests
	{
		private static LexicalEditRegionField ChooserField() => new LexicalEditRegionField(
			"LexEntry/x/#0", "Morph Type", "MorphType", null,
			RegionFieldKind.Chooser, EditorClassification.Known, "MorphTypeChooser", null,
			SurfaceRouting.Inherit, null,
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("g1", "stem"),
				new RegionChoiceOption("g2", "suffix")
			},
			"g1");

		private static LexicalEditRegionField VectorField() => new LexicalEditRegionField(
			"LexEntry/x/#1", "Publish Entry In", "PublishIn", null,
			RegionFieldKind.ReferenceVector, EditorClassification.Known, "PublishIn", null,
			SurfaceRouting.Inherit, null,
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("p1", "Main Dictionary"),
				new RegionChoiceOption("p2", "Pocket")
			},
			null, isEditable: true, indent: 0,
			items: new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") });

		private static (LexicalEditRegionView view, FakeRegionEditContext context, Window window) Show()
		{
			var model = new LexicalEditRegionModel("LexEntry", "test",
				new List<LexicalEditRegionField> { ChooserField(), VectorField() },
				new List<ViewDiagnostic>());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 500, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (view, context, window);
		}

		private static T Find<T>(Control view, string automationId) where T : Control
			=> view.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static IReadOnlyList<Control> VectorAffordances(Control view)
			=> ((IHoverAffordanceProvider)Find<FwReferenceVectorField>(view, "PublishIn")).HoverAffordances;

		private static Control Gear(Control view) => Find<Control>(view, "MorphTypeChooser.Settings");

		/// <summary>Moves the headless mouse over the center of a control (window coordinates).</summary>
		private static void MoveMouseOver(Window window, Control control)
		{
			var center = control.TranslatePoint(
				new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window);
			Assert.That(center, Is.Not.Null, "the control must be placed in the window");
			window.MouseMove(center.Value);
			Dispatcher.UIThread.RunJobs();
		}

		private static void MoveMouseFarAway(Window window)
		{
			// The bottom of the 300px window is below both rows: over no hover source.
			window.MouseMove(new Point(490, 290));
			Dispatcher.UIThread.RunJobs();
		}

		private static void ClickAt(Window window, Control control)
		{
			var center = control.TranslatePoint(
				new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window).Value;
			window.MouseDown(center, Avalonia.Input.MouseButton.Left);
			window.MouseUp(center, Avalonia.Input.MouseButton.Left);
			Dispatcher.UIThread.RunJobs();
		}

		/// <summary>Pumps real time through the headless render timer until the opacity fade lands.</summary>
		private static void PumpUntilOpacity(Control control, double expected, string because)
		{
			var sw = Stopwatch.StartNew();
			while (Math.Abs(control.Opacity - expected) > 0.001 && sw.ElapsedMilliseconds < 2000)
			{
				Thread.Sleep(20);
				AvaloniaHeadlessPlatform.ForceRenderTimerTick();
				Dispatcher.UIThread.RunJobs();
			}
			Assert.That(control.Opacity, Is.EqualTo(expected).Within(0.001), because);
		}

		[AvaloniaTest]
		public void Affordances_StartHidden_ByOpacity_StillInLayoutAndUiaTree()
		{
			var (view, _, _) = Show();

			var gear = Gear(view);
			Assert.That(gear, Is.Not.Null, "the gear is always in the tree (UIA/automation)");
			Assert.That(gear.Opacity, Is.EqualTo(0d), "the gear starts hidden by opacity");
			Assert.That(gear.IsHitTestVisible, Is.False);
			Assert.That(gear.IsVisible, Is.True, "hidden by opacity, NOT collapsed — no reflow");
			Assert.That(AutomationProperties.GetName(gear), Does.Contain("Morph Type"),
				"the gear carries a meaningful automation name");

			var affordances = VectorAffordances(view);
			Assert.That(affordances.OfType<Border>().Count(), Is.GreaterThanOrEqualTo(1),
				"separator bars are hover affordances");
			Assert.That(affordances.OfType<Button>().Count(), Is.EqualTo(2),
				"the + launcher AND the settings gear are hover affordances");
			foreach (var affordance in affordances)
			{
				Assert.That(affordance.Opacity, Is.EqualTo(0d));
				Assert.That(affordance.IsHitTestVisible, Is.False);
				Assert.That(affordance.IsVisible, Is.True);
			}

			// The vector's item text is always visible — only the chrome hides.
			var item = Find<TextBlock>(view, "PublishIn.Item.p1");
			Assert.That(item.Opacity, Is.EqualTo(1d));
		}

		[AvaloniaTest]
		public void HoverOverChooser_RevealsGear_AndLeavingHidesItAgain()
		{
			var (view, _, window) = Show();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			var gear = Gear(view);

			MoveMouseOver(window, chooser);
			Assert.That(gear.IsHitTestVisible, Is.True, "hover reveals the gear immediately");
			PumpUntilOpacity(gear, 1d, "the gear fades in on row hover");

			MoveMouseFarAway(window);
			Assert.That(gear.IsHitTestVisible, Is.False, "leaving the row hides the gear");
			PumpUntilOpacity(gear, 0d, "the gear fades back out");
		}

		[AvaloniaTest]
		public void HoverOverRowLabel_AlsoReveals_TheWholeRowIsTheHoverSurface()
		{
			var (view, _, window) = Show();
			var label = Find<TextBlock>(view, "MorphTypeChooser.Label");
			var gear = Gear(view);

			MoveMouseOver(window, label);
			Assert.That(gear.IsHitTestVisible, Is.True, "hovering the row LABEL reveals the editor's gear");
			PumpUntilOpacity(gear, 1d, "label hover fades the gear in");

			var vectorLabel = Find<TextBlock>(view, "PublishIn.Label");
			MoveMouseOver(window, vectorLabel);
			foreach (var affordance in VectorAffordances(view))
				Assert.That(affordance.IsHitTestVisible, Is.True, "vector label hover reveals bars + launcher");
			PumpUntilOpacity(gear, 0d, "moving to the OTHER row hides the chooser's gear again");
		}

		[AvaloniaTest]
		public void HoverOverVectorRow_RevealsBarsAndPlus_AndLeavingHides()
		{
			var (view, _, window) = Show();
			var vector = Find<FwReferenceVectorField>(view, "PublishIn");
			var affordances = VectorAffordances(view);

			MoveMouseOver(window, vector);
			foreach (var affordance in affordances)
				Assert.That(affordance.IsHitTestVisible, Is.True, "row hover reveals bars and the + launcher");
			PumpUntilOpacity(affordances[0], 1d, "the bars fade in");

			MoveMouseFarAway(window);
			foreach (var affordance in affordances)
				Assert.That(affordance.IsHitTestVisible, Is.False, "leaving hides them again");
			PumpUntilOpacity(affordances[0], 0d, "the bars fade out");
		}

		[AvaloniaTest]
		public void KeyboardFocus_OnAnAffordance_Reveals_AndLosingFocusHides()
		{
			var (view, _, window) = Show();
			var addButton = Find<Button>(view, "PublishIn.Add");
			var affordances = VectorAffordances(view);

			addButton.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(addButton.IsFocused, Is.True, "opacity-hidden affordances stay keyboard-focusable");
			foreach (var affordance in affordances)
				Assert.That(affordance.IsHitTestVisible, Is.True, "Tab onto the affordance reveals (accessibility)");
			PumpUntilOpacity(addButton, 1d, "focus fades the launcher in");

			Find<FwChooserField>(view, "MorphTypeChooser").Focus();
			Dispatcher.UIThread.RunJobs();
			foreach (var affordance in affordances)
				Assert.That(affordance.IsHitTestVisible, Is.False, "focus moving away (pointer not over) hides again");
			PumpUntilOpacity(addButton, 0d, "the launcher fades out after focus leaves");
		}

		[AvaloniaTest]
		public void ClickingTheGearAfterReveal_OpensTheSameFlyout_AndSelectionStillStages()
		{
			var (view, context, window) = Show();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			var gear = Gear(view);

			MoveMouseOver(window, chooser);
			Assert.That(gear.IsHitTestVisible, Is.True);

			ClickAt(window, gear);
			var flyout = (Flyout)chooser.Flyout;
			Assert.That(flyout.IsOpen, Is.True, "clicking the revealed gear opens the SAME chooser flyout");

			var options = (ListBox)flyout.Content;
			options.SelectedIndex = 1;
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.OptionEdits, Has.Count.EqualTo(1), "behavior unchanged: selection stages");
			Assert.That(context.OptionEdits[0], Is.EqualTo(("MorphType", "g2")));
			Assert.That(chooser.SelectedKey, Is.EqualTo("g2"));
			Assert.That(chooser.ValueText, Is.EqualTo("suffix"));
		}

		[AvaloniaTest]
		public void ClickingValueText_StillOpensTheFlyout_LikeTheLegacyCombo()
		{
			var (view, _, window) = Show();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");

			// No hover dance needed for the value itself: click anywhere on the button opens.
			ClickAt(window, chooser);
			Assert.That(((Flyout)chooser.Flyout).IsOpen, Is.True,
				"click-anywhere-on-value still opens the chooser");
		}

		[AvaloniaTest]
		public void ClickingTheRevealedPlus_OpensTheAddFlyout_AndAddStillStages()
		{
			var (view, context, window) = Show();
			var vector = Find<FwReferenceVectorField>(view, "PublishIn");
			var addButton = Find<Button>(view, "PublishIn.Add");

			MoveMouseOver(window, vector);
			Assert.That(addButton.IsHitTestVisible, Is.True);

			ClickAt(window, addButton);
			var flyout = (Flyout)addButton.Flyout;
			Assert.That(flyout.IsOpen, Is.True, "the revealed + opens the same add flyout");

			var list = (ListBox)flyout.Content;
			list.SelectedIndex = 1;
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1), "behavior unchanged: add stages");
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("PublishIn", "p2")));
		}

		// Bug "gear icons not showing" (a): the reference vector row gets the SAME hover-revealed
		// gear (the "this value has a supporting list" affordance); clicking it opens the IDENTICAL
		// flyout instance the "+" launcher opens — no new behavior.
		[AvaloniaTest]
		public void VectorRowHover_RevealsTheGear_AndGearClick_OpensTheSameFlyoutAsThePlus()
		{
			var (view, context, window) = Show();
			var vector = Find<FwReferenceVectorField>(view, "PublishIn");
			var gear = Find<Button>(view, "PublishIn.Settings");
			var addButton = Find<Button>(view, "PublishIn.Add");
			Assert.That(gear, Is.Not.Null, "the vector row carries a settings gear");
			Assert.That(gear.Opacity, Is.EqualTo(0d), "the gear starts hidden like the bars/+");
			Assert.That(VectorAffordances(view), Does.Contain(gear), "the gear reveals with the row chrome");

			MoveMouseOver(window, vector);
			Assert.That(gear.IsHitTestVisible, Is.True, "row hover reveals the gear");
			PumpUntilOpacity(gear, 1d, "the gear fades in alongside bars/+");

			Assert.That(gear.Flyout, Is.SameAs(addButton.Flyout),
				"the gear opens the SAME flyout instance as the + launcher");
			ClickAt(window, gear);
			var flyout = (Flyout)gear.Flyout;
			Assert.That(flyout.IsOpen, Is.True, "clicking the revealed gear opens the add/options flyout");

			// Behavior unchanged: selecting from the gear-opened flyout stages like the + path.
			var list = (ListBox)flyout.Content;
			list.SelectedIndex = 1;
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("PublishIn", "p2")));
		}

		private static (FwDialogLauncherField row, Window window, Control focusPark) ShowLauncher(Action launch)
		{
			var row = new FwDialogLauncherField("[NOUN: common]", "Inflection Features", launch);
			// A StackPanel keeps the row at its natural height, so the window bottom is "far away";
			// the TextBox is somewhere to park keyboard focus (a focused affordance stays revealed).
			var focusPark = new TextBox();
			var window = new Window
			{
				Content = new StackPanel { Children = { row, focusPark } },
				Width = 500,
				Height = 300
			};
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (row, window, focusPark);
		}

		// Bug "gear icons not showing" (b): the dialog-launcher row's always-visible "..." became
		// the same hover-revealed gear — same click path into the injected launcher callback.
		[AvaloniaTest]
		public void LauncherGear_HiddenUntilRowHover_AndClickInvokesTheInjectedLauncher()
		{
			var calls = 0;
			var (row, window, focusPark) = ShowLauncher(() => calls++);
			var gear = row.GetVisualDescendants().OfType<Button>().Single();

			Assert.That(gear.Opacity, Is.EqualTo(0d), "the launcher gear starts hidden by opacity");
			Assert.That(gear.IsHitTestVisible, Is.False);
			Assert.That(gear.IsVisible, Is.True, "hidden by opacity, NOT collapsed — no reflow");

			MoveMouseOver(window, row);
			Assert.That(gear.IsHitTestVisible, Is.True, "row hover reveals the gear");
			PumpUntilOpacity(gear, 1d, "the gear fades in");

			ClickAt(window, gear);
			Assert.That(calls, Is.EqualTo(1), "the gear click runs the host's launcher callback");

			// The clicked gear holds keyboard focus, which (by design) keeps it revealed; park
			// focus elsewhere, then leaving the row hides it again.
			focusPark.Focus();
			Dispatcher.UIThread.RunJobs();
			MoveMouseFarAway(window);
			PumpUntilOpacity(gear, 0d, "leaving the row hides the gear again");
		}

		[AvaloniaTest]
		public void LauncherGear_WithoutAService_RevealsDisabled_WithTheExplanatoryTooltip()
		{
			var (row, window, _) = ShowLauncher(null);
			var gear = row.GetVisualDescendants().OfType<Button>().Single();

			Assert.That(gear.IsEnabled, Is.False, "no host dialog service: the gear is disabled");
			Assert.That(ToolTip.GetTip(gear), Is.EqualTo(FwAvaloniaStrings.LauncherUnavailable),
				"the disabled gear explains itself");

			MoveMouseOver(window, row);
			PumpUntilOpacity(gear, 1d, "hover still reveals the (disabled) gear");
			Assert.That(() => row.Launch(), Throws.Nothing, "launching without a callback is a no-op");
		}

		[AvaloniaTest]
		public void RowLayoutHeight_DoesNotChange_BetweenHiddenAndRevealed()
		{
			var (view, _, window) = Show();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			var vector = Find<FwReferenceVectorField>(view, "PublishIn");

			var hiddenChooserHeight = chooser.Bounds.Height;
			var hiddenVectorHeight = vector.Bounds.Height;
			Assert.That(hiddenChooserHeight, Is.GreaterThan(0));
			Assert.That(hiddenVectorHeight, Is.GreaterThan(0));

			MoveMouseOver(window, chooser);
			PumpUntilOpacity(Gear(view), 1d, "revealed");
			MoveMouseOver(window, vector);
			PumpUntilOpacity(VectorAffordances(view)[0], 1d, "revealed");
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(chooser.Bounds.Height, Is.EqualTo(hiddenChooserHeight),
				"revealing the gear must not reflow the chooser row");
			Assert.That(vector.Bounds.Height, Is.EqualTo(hiddenVectorHeight),
				"revealing bars/launcher must not reflow the vector row");
		}
	}
}
