// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The browse "Filter For…" pattern-setup dialog (the Avalonia counterpart of the legacy FilterBar
	/// SimpleMatchDlg): a spec-only modal that edits a <see cref="FilterForPattern"/>. The VM gates OK on a
	/// non-empty match text and a valid regex (in regex mode), disables the position radios when regex is on,
	/// and snapshots the chosen match style + case option into Result on OK.
	/// </summary>
	[TestFixture]
	public class FilterForDialogTests
	{
		private static (FilterForDialogView view, FilterForDialogViewModel vm) Show(
			FilterForPattern seed = null, string stageName = "FilterFor-01-initial")
		{
			var vm = new FilterForDialogViewModel(seed);
			var view = new FilterForDialogView { DataContext = vm };
			AvaloniaDialogTestHarness.Realize(view, 380, 280, stageName);
			return (view, vm);
		}

		private static void Capture(Control view, string stageName)
			=> AvaloniaDialogTestHarness.Recapture(view, stageName);

		[AvaloniaTest]
		public void EmptyMatchText_BlocksOk()
		{
			// Empty match text == OK-disabled stage.
			var (_, vm) = Show(stageName: "FilterFor-03-invalid-empty");
			Assert.That(vm.IsValid, Is.False, "an empty match text gates OK off");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
		}

		[AvaloniaTest]
		public void NonEmptyMatchText_EnablesOk()
		{
			var (view, vm) = Show();
			vm.MatchText = "cat";
			Capture(view, "FilterFor-02-populated");
			Assert.That(vm.IsValid, Is.True);
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		[AvaloniaTest]
		public void DefaultMatchStyle_IsAnywhere()
		{
			var (_, vm) = Show();
			vm.MatchText = "cat";
			Dispatcher.UIThread.RunJobs();
			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.MatchType, Is.EqualTo(FilterForMatchType.Anywhere));
		}

		[AvaloniaTest]
		public void UseRegex_DisablesThePositionOptions()
		{
			var (_, vm) = Show();
			Assert.That(vm.PositionOptionsEnabled, Is.True);
			vm.UseRegularExpressions = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.PositionOptionsEnabled, Is.False, "regex disables the position radios");
		}

		[AvaloniaTest]
		public void InvalidRegex_BlocksOk_AndSurfacesError()
		{
			var (view, vm) = Show();
			vm.UseRegularExpressions = true;
			vm.MatchText = "("; // unterminated group: invalid regex
			Capture(view, "FilterFor-04-invalid-regex");
			Assert.That(vm.IsValid, Is.False);
			Assert.That(vm.HasInvalidRegex, Is.True);
			Assert.That(vm.ValidationErrors, Is.Not.Empty);
		}

		[AvaloniaTest]
		public void ValidRegex_PassesTheGate_AndSnapshotsRegexType()
		{
			var (_, vm) = Show();
			vm.UseRegularExpressions = true;
			vm.MatchText = "ca+t";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.IsValid, Is.True);
			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.MatchType, Is.EqualTo(FilterForMatchType.Regex));
		}

		[AvaloniaTest]
		public void Ok_SnapshotsTheChosenPositionStyle_AndMatchCase()
		{
			var (_, vm) = Show();
			vm.MatchText = "cat";
			vm.MatchWholeItem = true; // selecting the whole-item radio
			vm.MatchCase = true;
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.Result, Is.Not.Null);
			Assert.That(vm.Result.MatchText, Is.EqualTo("cat"));
			Assert.That(vm.Result.MatchType, Is.EqualTo(FilterForMatchType.WholeItem));
			Assert.That(vm.Result.MatchCase, Is.True);
		}

		[AvaloniaTest]
		public void Seed_RoundTripsTheMatchType()
		{
			var (_, vm) = Show(new FilterForPattern
			{
				MatchText = "x",
				MatchType = FilterForMatchType.AtStart,
				MatchCase = true
			});
			Assert.That(vm.MatchAtStart, Is.True);
			Assert.That(vm.MatchCase, Is.True);
			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.MatchType, Is.EqualTo(FilterForMatchType.AtStart));
		}

		[AvaloniaTest]
		public void Cancel_NeverSnapshotsAResult()
		{
			var (_, vm) = Show();
			vm.MatchText = "cat";
			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
			Assert.That(vm.Result, Is.Null);
		}

		[AvaloniaTest]
		public void View_HostsTheFilterForControls()
		{
			var (view, _) = Show();
			var ids = view.GetVisualDescendants()
				.Select(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c as Avalonia.Controls.Control))
				.Where(id => !string.IsNullOrEmpty(id))
				.ToList();
			Assert.That(ids, Does.Contain("FilterFor.MatchText"));
			Assert.That(ids, Does.Contain("FilterFor.Anywhere"));
			Assert.That(ids, Does.Contain("FilterFor.UseRegex"));
			Assert.That(ids, Does.Contain("FilterFor.MatchCase"));
			Assert.That(ids, Does.Contain("FilterFor.Ok"));
		}

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.FilterForTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FilterForMatchLabel, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FilterForInvalidRegex, Is.Not.Null.And.Not.Empty);
		}
	}
}
