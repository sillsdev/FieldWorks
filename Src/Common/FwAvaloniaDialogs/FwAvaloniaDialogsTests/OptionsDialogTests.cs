// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The Tools → Options dialog: the four real tabs bind to an <see cref="OptionsState"/> (the product
	/// settings bus) via compiled bindings, edits write back into the state on OK, the generated
	/// commands close via <see cref="IDialogViewModel"/>, and the Updates tab hides off-Windows. Runtime
	/// proof on a realized headless surface (compiled XAML on net48 + source-generated commands).
	/// </summary>
	[TestFixture]
	public class OptionsDialogTests
	{
		private static OptionsState SampleState() => new OptionsState
		{
			AvailableUiLanguages = new[] { new NamedOption("en", "English"), new NamedOption("fr", "Français") },
			UiLanguage = "en", OriginalUiLanguage = "en",
			UiMode = "Legacy", OriginalUiMode = "Legacy",
			AutoOpenLastProject = false,
			OkToPingBasicUsageData = false,
			UpdatesTabVisible = true, AutoUpdate = true, UpdateChannel = "Stable",
			AvailableChannels = new[]
			{
				new NamedOption("Stable", "Stable", "The recommended channel."),
				new NamedOption("Beta", "Beta", "Early access.")
			},
			PluginsAvailable = true,
			Plugins = new List<PluginOption> { new PluginOption("Concorder", "A concordance tool", false) }
		};

		private static (OptionsDialogView view, OptionsDialogViewModel vm) Show(
			OptionsState state = null, string stageName = "Options-01-initial")
		{
			var vm = new OptionsDialogViewModel(state ?? SampleState());
			var view = new OptionsDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 480, Height = 380 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			// Capture the realized stage BEFORE asserting, so the PNG exists for visual review even if the assert fails.
			// The view is already hosted in `window`; snapshot that window (capturing the view again would re-parent it).
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		// Re-pump the realized surface and snapshot a later interaction stage (edited values, applied mode, etc.).
		// Snapshots the view's hosting window (the view already has a visual parent).
		private static void Capture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == id);

		[AvaloniaTest]
		public void CompiledBinding_PropagatesBothDirections()
		{
			var (view, vm) = Show();
			var checkBox = FindByAutomationId<CheckBox>(view, "Options.General.AutoOpenLastProject");

			Assert.That(checkBox.IsChecked, Is.False);
			vm.AutoOpenLastProject = true;
			Capture(view, "Options-02-checkbox-toggled");
			Assert.That(checkBox.IsChecked, Is.True, "compiled binding must propagate VM -> control");

			checkBox.IsChecked = false;
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.AutoOpenLastProject, Is.False, "compiled binding must propagate control -> VM");
		}

		[AvaloniaTest]
		public void Seeds_FromState()
		{
			var (_, vm) = Show();
			Assert.That(vm.SelectedUiLanguage.Code, Is.EqualTo("en"));
			Assert.That(vm.SelectedUiMode.Code, Is.EqualTo("Legacy"));
			Assert.That(vm.SelectedUpdateChannel.Code, Is.EqualTo("Stable"));
			Assert.That(vm.CanApplyMode, Is.False, "no UI-mode change yet");
		}

		[AvaloniaTest]
		public void ApplyMode_AppliesLive_WithoutClosing_AndClearsPending()
		{
			var state = SampleState();
			string applied = null;
			state.ApplyUiModeLive = mode => applied = mode;
			var (view, vm) = Show(state);

			vm.SelectedUiMode = vm.UiModes.First(o => o.Code == "New");
			Capture(view, "Options-03-mode-pending-apply");
			Assert.That(vm.CanApplyMode, Is.True);

			vm.ApplyModeCommand.Execute(null);

			Assert.That(applied, Is.EqualTo("New"), "Apply switches the mode live via the product callback");
			Assert.That(vm.Accepted, Is.Null, "Apply does not close the dialog");
			Assert.That(vm.CanApplyMode, Is.False, "after Apply the mode is no longer pending");
		}

		[AvaloniaTest]
		public void Ok_WritesEditedValuesBackIntoTheState()
		{
			var state = SampleState();
			var (_, vm) = Show(state);

			vm.SelectedUiMode = vm.UiModes.First(o => o.Code == "New");
			Assert.That(vm.CanApplyMode, Is.True, "changing the UI mode enables Apply");
			vm.AutoOpenLastProject = true;
			vm.OkToPingBasicUsageData = true;
			vm.SelectedUpdateChannel = vm.UpdateChannels.First(o => o.Code == "Beta");

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(state.UiMode, Is.EqualTo("New"));
			Assert.That(state.AutoOpenLastProject, Is.True);
			Assert.That(state.OkToPingBasicUsageData, Is.True);
			Assert.That(state.UpdateChannel, Is.EqualTo("Beta"));
		}

		[AvaloniaTest]
		public void UpdatesTab_HiddenWhenNotAvailable()
		{
			var state = SampleState();
			state.UpdatesTabVisible = false;
			var (view, _) = Show(state, "Options-04-updates-tab-hidden");
			var updatesTab = view.GetVisualDescendants().OfType<TabItem>()
				.First(t => Equals(t.Header, FwAvaloniaDialogsStrings.UpdatesTab));
			Assert.That(updatesTab.IsVisible, Is.False, "the Updates tab hides off-Windows / when unavailable");
		}

		[AvaloniaTest]
		public void Plugins_ShareTheStateList_SoToggleWritesThrough()
		{
			var state = SampleState();
			var (_, vm) = Show(state);
			Assert.That(vm.Plugins, Is.SameAs(state.Plugins),
				"the VM edits the state's plugin list in place (checkbox two-way binding writes Installed)");
			vm.Plugins[0].Installed = true;
			Assert.That(state.Plugins[0].Installed, Is.True);
			Assert.That(state.Plugins[0].WasInstalled, Is.False, "the original state is preserved for the install/uninstall diff");
		}

		// --- Host-modal contract: the VM signals close via IDialogViewModel; AvaloniaDialogHost.WireClose forwards it. ---

		[Test]
		public void OkCommand_RaisesCloseRequestedTrue_AndSetsAccepted()
		{
			var vm = new OptionsDialogViewModel(SampleState());
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}

		[Test]
		public void CancelCommand_RaisesCloseRequestedFalse()
		{
			var vm = new OptionsDialogViewModel(SampleState());
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
		}

		[Test]
		public void WireClose_ForwardsCloseSignal_ThenUnsubscribesOnDispose()
		{
			var vm = new OptionsDialogViewModel(SampleState());
			var calls = 0;
			var last = false;

			using (AvaloniaDialogHost.WireClose(vm, accepted => { calls++; last = accepted; }))
			{
				vm.OkCommand.Execute(null);
				Assert.That(calls, Is.EqualTo(1));
				Assert.That(last, Is.True);
			}

			vm.CancelCommand.Execute(null);
			Assert.That(calls, Is.EqualTo(1), "WireClose must unsubscribe on dispose");
		}

		// --- Localization: strings resolve from the shared accessor and bind into the XAML. ---

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.GeneralTab, Is.EqualTo("General"));
			Assert.That(FwAvaloniaDialogsStrings.Ok, Is.EqualTo("OK"));
			Assert.That(FwAvaloniaDialogsStrings.PluginsTab, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.UiModeNew, Is.Not.Null.And.Not.Empty);
		}

		[AvaloniaTest]
		public void View_BindsLocalizedStrings_NotHardcodedEnglish()
		{
			var (view, _) = Show();
			var ok = FindByAutomationId<Button>(view, "Options.Ok");
			Assert.That(ok.Content, Is.EqualTo(FwAvaloniaDialogsStrings.Ok),
				"the OK button text must come from the shared localization accessor, not a literal");
		}
	}
}
