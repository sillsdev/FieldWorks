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
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Hover-reveal affordances (UI polish): the chooser's configure gear and the reference vector's
	/// separator bars + "+" launcher start hidden (opacity 0, not hit-testable, but still in
	/// layout and in the UIA tree), fade in while the pointer is over the row (label or editor),
	/// and fade out when it leaves — driven here by REAL headless mouse input. Gear semantics:
	/// the gear renders only because these rows RESOLVE a list-editor target (chooserLinks), and
	/// clicking it dispatches the jump directly — no flyout. The "+"/value click opens the
	/// options picker, and staging still fires from it.
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
			"g1",
			chooserLinks: new List<RegionChooserLink>
			{
				new RegionChooserLink("Edit the Morpheme Types list", "morphTypeEdit")
			});

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
			items: new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") },
			chooserLinks: new List<RegionChooserLink>
			{
				new RegionChooserLink("Edit the Publications list", "publicationsEdit")
			});

		private static (LexicalEditRegionView view, FakeRegionEditContext context, Window window,
			List<RegionLinkRequest> linkRequests) Show()
		{
			var model = new LexicalEditRegionModel("LexEntry", "test",
				new List<LexicalEditRegionField> { ChooserField(), VectorField() },
				new List<ViewDiagnostic>());
			var context = new FakeRegionEditContext();
			var linkRequests = new List<RegionLinkRequest>();
			var view = new LexicalEditRegionView(model, context, linkRequested: linkRequests.Add);
			var window = new Window { Content = view, Width = 500, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (view, context, window, linkRequests);
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
			var (view, _, _, _) = Show();

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

			// The vector's item text is always visible — only the affordances hide.
			var item = Find<TextBlock>(view, "PublishIn.Item.p1");
			Assert.That(item.Opacity, Is.EqualTo(1d));
		}

		[AvaloniaTest]
		public void HoverOverChooser_RevealsGear_AndLeavingHidesItAgain()
		{
			var (view, _, window, _) = Show();
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
			var (view, _, window, _) = Show();
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
			var (view, _, window, _) = Show();
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
			var (view, _, window, _) = Show();
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

		// GEAR = CONFIGURE: clicking the revealed gear DIRECTLY dispatches the list-editor jump
		// (RegionLinkRequest with the row's resolved tool) — no flyout, no menu, no staging.
		[AvaloniaTest]
		public void ClickingTheGearAfterReveal_DispatchesTheListEditorJump_NoFlyoutOpens()
		{
			var (view, context, window, linkRequests) = Show();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			var gear = Gear(view);

			MoveMouseOver(window, chooser);
			Assert.That(gear.IsHitTestVisible, Is.True);

			ClickAt(window, gear);

			Assert.That(linkRequests, Has.Count.EqualTo(1),
				"the gear click raises RegionLinkRequest directly");
			Assert.That(linkRequests[0].Link.Tool, Is.EqualTo("morphTypeEdit"),
				"the resolved list-editor tool rides the request");
			Assert.That(((Flyout)chooser.Flyout).IsOpen, Is.False,
				"NO flyout opens from the gear — it configures, it does not choose");
			Assert.That(context.OptionEdits, Is.Empty, "a configure jump is not an edit");
		}

		[AvaloniaTest]
		public void ClickingValueText_OpensTheOptionPicker_LikeTheLegacyCombo()
		{
			var (view, _, window, linkRequests) = Show();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");

			// No hover dance needed for the value itself: click anywhere on the button opens.
			ClickAt(window, chooser);
			Assert.That(((Flyout)chooser.Flyout).IsOpen, Is.True,
				"click-anywhere-on-value still opens the chooser");
			Assert.That(((Flyout)chooser.Flyout).Content, Is.TypeOf<FwOptionPicker>(),
				"the options render in the one compact filterable picker");
			Assert.That(linkRequests, Is.Empty, "the value click never dispatches the jump");
		}

		[AvaloniaTest]
		public void ClickingTheRevealedPlus_OpensThePickerFlyout_AndCommitStillStages()
		{
			var (view, context, window, _) = Show();
			var vector = Find<FwReferenceVectorField>(view, "PublishIn");
			var addButton = Find<Button>(view, "PublishIn.Add");

			MoveMouseOver(window, vector);
			Assert.That(addButton.IsHitTestVisible, Is.True);

			ClickAt(window, addButton);
			var flyout = (Flyout)addButton.Flyout;
			Assert.That(flyout.IsOpen, Is.True, "the revealed + opens the add picker");

			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1;
			picker.CommitHighlighted(); // multi-select: checks the row
			picker.CommitChecked();     // Add: commits the checked set
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1), "behavior unchanged: add stages");
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("PublishIn", "p2")));
		}

		// GEAR = CONFIGURE on the vector row too: the hover-revealed gear dispatches the resolved
		// list-editor jump directly; only the "+" opens the add picker.
		[AvaloniaTest]
		public void VectorRowHover_RevealsTheGear_AndGearClick_DispatchesTheJumpDirectly()
		{
			var (view, context, window, linkRequests) = Show();
			var vector = Find<FwReferenceVectorField>(view, "PublishIn");
			var gear = Find<Button>(view, "PublishIn.Settings");
			var addButton = Find<Button>(view, "PublishIn.Add");
			Assert.That(gear, Is.Not.Null, "a resolved list-editor target draws the gear");
			Assert.That(gear.Opacity, Is.EqualTo(0d), "the gear starts hidden like the bars/+");
			Assert.That(VectorAffordances(view), Does.Contain(gear), "the gear reveals with the row affordances");
			Assert.That(gear.Flyout, Is.Null, "the gear carries NO flyout — it dispatches directly");

			MoveMouseOver(window, vector);
			Assert.That(gear.IsHitTestVisible, Is.True, "row hover reveals the gear");
			PumpUntilOpacity(gear, 1d, "the gear fades in alongside bars/+");

			ClickAt(window, gear);

			Assert.That(linkRequests, Has.Count.EqualTo(1), "the gear click raises the host jump");
			Assert.That(linkRequests[0].Link.Tool, Is.EqualTo("publicationsEdit"));
			Assert.That(((Flyout)addButton.Flyout).IsOpen, Is.False, "the add picker stays closed");
			Assert.That(context.ReferenceAdds, Is.Empty, "a configure jump never stages");
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

		// Idempotence regression: controls attach their own affordances in their constructors and
		// the region view attaches AGAIN to widen the hover surface to the row. Before the merge
		// fix each Attach stacked an independent handler set with its own watched list, and the
		// LAST registration could hide the affordance while the pointer was still over a source
		// only an EARLIER registration watched (correctness depended on the superset attaching
		// last). Attaching the SUBSET last here proves the registrations merge.
		[AvaloniaTest]
		public void Attach_MergesRepeatedRegistrations_RegardlessOfOrder()
		{
			var rowSurface = new Border { Width = 200, Height = 40, Background = Brushes.Transparent };
			var editor = new Border { Width = 200, Height = 40, Background = Brushes.Transparent };
			var gear = new Button { Content = "*", Width = 20, Height = 20 };
			var focusPark = new TextBox();
			var window = new Window
			{
				Content = new StackPanel { Children = { rowSurface, editor, gear, focusPark } },
				Width = 500,
				Height = 300
			};
			HoverReveal.Attach(new Control[] { rowSurface, editor }, new Control[] { gear }); // superset first
			HoverReveal.Attach(new Control[] { editor }, new Control[] { gear });             // subset last
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();

			// Hover the source only the FIRST registration named...
			MoveMouseOver(window, rowSurface);
			Assert.That(gear.IsHitTestVisible, Is.True, "a first-registration source still reveals");

			// ...then a focus blip on the gear forces a LostFocus re-evaluation of the hover
			// state. The merged registration still sees the pointer over rowSurface; stacked
			// registrations fought, and the last one hid the gear (it never watched rowSurface).
			gear.Focus();
			Dispatcher.UIThread.RunJobs();
			focusPark.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(gear.IsHitTestVisible, Is.True,
				"one merged watched list decides the state — not whichever Attach ran last");

			MoveMouseFarAway(window);
			Assert.That(gear.IsHitTestVisible, Is.False, "leaving every merged source still hides");
		}

		[AvaloniaTest]
		public void RowLayoutHeight_DoesNotChange_BetweenHiddenAndRevealed()
		{
			var (view, _, window, _) = Show();
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
