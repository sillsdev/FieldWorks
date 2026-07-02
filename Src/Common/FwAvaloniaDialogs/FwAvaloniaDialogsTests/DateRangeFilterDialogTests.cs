// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
	/// The browse "Restrict Date…" date-range dialog (the Avalonia counterpart of the legacy FilterBar
	/// SimpleDateMatchDlg): a spec-only modal that edits a <see cref="DateRangeFilterPattern"/>. The VM exposes a
	/// relation picker (on / not on / on or before / on or after / between), a start date (and an end date shown
	/// only for "between"), gates OK on a valid range, and snapshots the chosen relation + date(s) into Result on
	/// OK. Captures a PNG per stage and asserts no layout crowding.
	/// </summary>
	[TestFixture]
	public class DateRangeFilterDialogTests
	{
		private static (DateRangeFilterDialogView view, DateRangeFilterDialogViewModel vm) Show(
			DateRangeFilterPattern seed = null, bool genDate = false, string stageName = "DateRangeFilter-01-initial")
		{
			var vm = new DateRangeFilterDialogViewModel(seed, genDate);
			var view = new DateRangeFilterDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 380, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			// Capture the realized stage BEFORE asserting, so the PNG exists for visual review even if the assert fails.
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		private static void Capture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		// The index of a relation in the VM's MatchTypes list (the ComboBox SelectedIndex the view binds).
		private static int IndexOf(DateRangeFilterDialogViewModel vm, DateRangeMatchType type)
		{
			for (var i = 0; i < vm.MatchTypes.Count; i++)
				if (vm.MatchTypes[i] == type)
					return i;
			return 0;
		}

		[AvaloniaTest]
		public void DefaultRelation_IsOn_AndEndDateHidden()
		{
			var (_, vm) = Show();
			Assert.That(vm.MatchType, Is.EqualTo(DateRangeMatchType.On), "the default relation is On");
			Assert.That(vm.ShowEndDate, Is.False, "the end date is hidden for single-date relations");
			Assert.That(vm.IsValid, Is.True, "a single-date relation with a chosen date is valid");
		}

		[AvaloniaTest]
		public void SelectingBetween_ShowsTheEndDate()
		{
			var (view, vm) = Show();
			vm.SelectedMatchTypeIndex = IndexOf(vm, DateRangeMatchType.Between);
			Capture(view, "DateRangeFilter-02-between");
			Assert.That(vm.MatchType, Is.EqualTo(DateRangeMatchType.Between));
			Assert.That(vm.ShowEndDate, Is.True, "the end date is shown for the between relation");
		}

		[AvaloniaTest]
		public void Between_InvertedRange_BlocksOk_AndSurfacesError()
		{
			var (view, vm) = Show();
			vm.SelectedMatchTypeIndex = IndexOf(vm, DateRangeMatchType.Between);
			vm.StartDate = new DateTime(2020, 6, 1);
			vm.EndDate = new DateTime(2020, 1, 1); // end before start
			Capture(view, "DateRangeFilter-03-invalid");
			Assert.That(vm.IsValid, Is.False, "an inverted range gates OK off");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
			Assert.That(vm.ValidationErrors, Is.Not.Empty);
		}

		[AvaloniaTest]
		public void Between_ValidRange_PassesTheGate()
		{
			var (_, vm) = Show();
			vm.SelectedMatchTypeIndex = IndexOf(vm, DateRangeMatchType.Between);
			vm.StartDate = new DateTime(2020, 1, 1);
			vm.EndDate = new DateTime(2020, 12, 31);
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.IsValid, Is.True);
		}

		[AvaloniaTest]
		public void NoDate_BlocksOk()
		{
			var (_, vm) = Show();
			vm.StartDate = null;
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.IsValid, Is.False, "OK requires a chosen start date");
		}

		[AvaloniaTest]
		public void Ok_SnapshotsRelation_AndInclusiveEndOfDayRange()
		{
			var (_, vm) = Show();
			vm.SelectedMatchTypeIndex = IndexOf(vm, DateRangeMatchType.On);
			vm.StartDate = new DateTime(2021, 3, 4);
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.Result, Is.Not.Null);
			Assert.That(vm.Result.MatchType, Is.EqualTo(DateRangeMatchType.On));
			Assert.That(vm.Result.Start, Is.EqualTo(new DateTime(2021, 3, 4)), "start is the chosen day at midnight");
			// END is the last instant of the SAME day for a single-date relation (the legacy inclusive-range rule).
			Assert.That(vm.Result.End.Date, Is.EqualTo(new DateTime(2021, 3, 4)), "end stays on the chosen day");
			Assert.That(vm.Result.End.TimeOfDay, Is.GreaterThan(TimeSpan.FromHours(23)), "end extends to end-of-day");
		}

		[AvaloniaTest]
		public void Ok_Between_SnapshotsTheChosenEndDay()
		{
			var (_, vm) = Show();
			vm.SelectedMatchTypeIndex = IndexOf(vm, DateRangeMatchType.Between);
			vm.StartDate = new DateTime(2020, 1, 1);
			vm.EndDate = new DateTime(2020, 6, 30);
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);

			Assert.That(vm.Result.MatchType, Is.EqualTo(DateRangeMatchType.Between));
			Assert.That(vm.Result.Start, Is.EqualTo(new DateTime(2020, 1, 1)));
			Assert.That(vm.Result.End.Date, Is.EqualTo(new DateTime(2020, 6, 30)), "the end uses the chosen end day");
		}

		[AvaloniaTest]
		public void GenDateColumn_CarriesHandleGenDateIntoTheResult()
		{
			var (_, vm) = Show(genDate: true);
			vm.StartDate = new DateTime(2010, 5, 5);
			Dispatcher.UIThread.RunJobs();
			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.HandleGenDate, Is.True, "a genDate column carries HandleGenDate into the result");
		}

		[AvaloniaTest]
		public void Seed_RoundTripsTheRelationAndDates()
		{
			var seed = new DateRangeFilterPattern
			{
				MatchType = DateRangeMatchType.Between,
				Start = new DateTime(2015, 2, 1),
				End = new DateTime(2015, 8, 1)
			};
			var (_, vm) = Show(seed);
			Assert.That(vm.MatchType, Is.EqualTo(DateRangeMatchType.Between));
			Assert.That(vm.StartDate?.Date, Is.EqualTo(new DateTime(2015, 2, 1)));
			Assert.That(vm.EndDate?.Date, Is.EqualTo(new DateTime(2015, 8, 1)));
		}

		[AvaloniaTest]
		public void Cancel_NeverSnapshotsAResult()
		{
			var (_, vm) = Show();
			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
			Assert.That(vm.Result, Is.Null);
		}

		[AvaloniaTest]
		public void View_HostsTheDateRangeControls()
		{
			var (view, _) = Show();
			var ids = view.GetVisualDescendants()
				.Select(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c as Avalonia.Controls.Control))
				.Where(id => !string.IsNullOrEmpty(id))
				.ToList();
			Assert.That(ids, Does.Contain("RestrictDate.Relation"));
			Assert.That(ids, Does.Contain("RestrictDate.StartDate"));
			Assert.That(ids, Does.Contain("RestrictDate.Ok"));
		}

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.RestrictDateTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.RestrictDateRelationLabel, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.RestrictDateBetween, Is.Not.Null.And.Not.Empty);
		}
	}
}
