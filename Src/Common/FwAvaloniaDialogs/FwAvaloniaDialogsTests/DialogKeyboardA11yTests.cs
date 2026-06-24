// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// Keyboard-accessibility parity for the kept dialog spine (A11Y-02 tab order, A11Y-03 initial focus,
	/// review 2026-06-23). Legacy WinForms dialogs opened with focus in the first field and tabbed
	/// fields-before-buttons; the Avalonia kit must match. These run on a realized headless surface and
	/// assert the deterministic selection contract of <see cref="AvaloniaDialogHost.FocusInitialControl"/>
	/// plus the per-view TabIndex that pushes the button strip last — independent of the WinForms-hosted
	/// modal delivery path (that bridge is covered by the desktop UIA lane, A11Y-04).
	/// </summary>
	[TestFixture]
	public class DialogKeyboardA11yTests
	{
		private static IReadOnlyList<RegionChoiceOption> Candidates() => new List<RegionChoiceOption>
		{
			new RegionChoiceOption("g-noun", "Noun", 0),
			new RegionChoiceOption("g-verb", "Verb", 0),
			new RegionChoiceOption("g-adj", "Adjective", 0)
		};

		private static void Pump(Window window, Control view)
		{
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
		}

		private static Button FindButton(Control root, string automationId)
			=> root.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == automationId);

		// --- A11Y-03: FocusInitialControl contract (synthetic, no dialog coupling) ---

		[AvaloniaTest]
		public void FocusInitialControl_PrefersTextInput_OverCommandButton()
		{
			// Mirrors the kit layout: a bottom button strip (declared first, TabIndex=1) over content with a
			// text field. Initial focus must land in the field, never on the command button.
			var field = new TextBox();
			var okButton = new Button { TabIndex = 1 };
			var strip = new StackPanel { TabIndex = 1 };
			strip.Children.Add(okButton);
			var content = new StackPanel();
			content.Children.Add(field);
			var root = new DockPanel();
			root.Children.Add(strip);    // declared first (docks bottom in real views)
			root.Children.Add(content);  // the fill content with the input
			var window = new Window { Content = root, Width = 300, Height = 200 };
			Pump(window, root);

			var focused = AvaloniaDialogHost.FocusInitialControl(root);

			Assert.That(focused, Is.SameAs(field), "initial focus should land on the first text input, not the button");
		}

		// --- A11Y-03: picker-driven dialogs must NOT auto-focus OK ---

		[AvaloniaTest]
		public void FocusInitialControl_PickerDialog_DoesNotFocusOkButton()
		{
			// The flat Chooser's FwOptionPicker is intentionally Focusable=false (handles keys directly).
			// Whatever FocusInitialControl picks, it must NEVER be a command button (OK/Cancel) — otherwise
			// Enter would accept the dialog the instant it opened.
			var vm = new ChooserDialogViewModel(new ChooserDialogInput { Candidates = Candidates() });
			var view = new ChooserDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 360, Height = 420 };
			Pump(window, view);

			var focused = AvaloniaDialogHost.FocusInitialControl(view);
			var ok = FindButton(view, "Chooser.Ok");
			var cancel = FindButton(view, "Chooser.Cancel");

			Assert.That(focused, Is.Not.SameAs(ok), "must not auto-focus OK on a picker-driven dialog");
			Assert.That(focused, Is.Not.SameAs(cancel), "must not auto-focus Cancel on a picker-driven dialog");
			Assert.That(focused, Is.Not.InstanceOf<Button>(), "initial focus must never be a command button");
		}

		// --- A11Y-02: button strip sorts AFTER content in tab order ---

		[AvaloniaTest]
		public void ButtonStrip_SortsAfterContent_InTabOrder()
		{
			var vm = new ChooserDialogViewModel(new ChooserDialogInput { Candidates = Candidates() });
			var view = new ChooserDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 360, Height = 420 };
			Pump(window, view);

			// The bottom button strip is declared first (so DockPanel docks it to the bottom) but carries
			// TabIndex=1 so it is visited AFTER the default-0 content in tab order (A11Y-02).
			var ok = FindButton(view, "Chooser.Ok");
			var strip = ok.GetVisualAncestors().OfType<StackPanel>().First();
			Assert.That(strip.TabIndex, Is.EqualTo(1),
				"the button strip must carry TabIndex=1 so the fields tab before the OK/Cancel strip");
		}

		// --- A11Y-02: NATIVE Avalonia Tab traversal actually honors the container TabIndex ---
		// (Asserting the attribute is set is not enough — this exercises KeyboardNavigationHandler, the
		// real Tab engine, to prove the container-level TabIndex reorders traversal in Avalonia's model.)

		[AvaloniaTest]
		public void NativeTabTraversal_FirstStop_IsContentNotButtonStrip()
		{
			// Mirrors the kit layout: a button strip declared FIRST but TabIndex=1, over content with a field.
			var field = new TextBox();
			var okButton = new Button { TabIndex = 1 };
			var strip = new StackPanel { TabIndex = 1 };
			strip.Children.Add(okButton);
			var content = new StackPanel();
			content.Children.Add(field);
			var root = new DockPanel();
			root.Children.Add(strip);    // declared first (docks bottom in real views)
			root.Children.Add(content);
			var window = new Window { Content = root, Width = 300, Height = 200 };
			Pump(window, root);

			var first = KeyboardNavigationHandler.GetNext(root, NavigationDirection.Next);

			Assert.That(first, Is.SameAs(field),
				"native Tab traversal must reach the content field before the TabIndex=1 button strip");

			// NOTE (verified 2026-06-23): this proves container-level TabIndex reorders Avalonia's NATIVE Tab
			// engine when the content has a tab stop — so InsertEntry/EntryGo (text fields) tab fields-first.
			// Picker-driven dialogs (Chooser/Options) are a separate case: their FwOptionPicker is
			// Focusable=false (handles keys directly), so they have NO tabbable content and Tab necessarily
			// begins on the button strip — inherent to the picker design and pre-existing, not a regression
			// introduced or removable by this TabIndex change.
		}
	}
}
