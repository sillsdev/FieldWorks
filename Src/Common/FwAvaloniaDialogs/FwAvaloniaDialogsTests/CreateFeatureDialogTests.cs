// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The minimal "Create New Feature" / "Create New Feature Value" name-entry dialog (Phase-1 §19b Stage 3): the
	/// LCModel-free collector behind the inline create affordances of the feature editor (the Avalonia replacement for
	/// the MasterInflectionFeatureListDlg / MasterPhonologicalFeatureListDlg blank-create link). OK is gated on a
	/// non-empty name; the same VM serves the feature + value flows via its labels. Runtime proof on a realized
	/// headless surface, with a per-stage PNG.
	/// </summary>
	[TestFixture]
	public class CreateFeatureDialogTests
	{
		private static (CreateFeatureDialogView view, CreateFeatureDialogViewModel vm) Show(
			CreateFeatureDialogViewModel vm, string stageName)
		{
			var view = new CreateFeatureDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 340, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		[AvaloniaTest]
		public void FeatureDialog_OkGatedOnNonEmptyName()
		{
			var (view, vm) = Show(CreateFeatureDialogViewModel.ForFeature(), "CreateFeature-01-empty");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "OK is disabled until a name is entered");
			Assert.That(vm.IsValid, Is.False);

			vm.Name = "Case";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "OK enables once a name is present");
			Assert.That(vm.ChosenName, Is.EqualTo("Case"));
		}

		[AvaloniaTest]
		public void FeatureDialog_TrimsNameAndAbbreviation()
		{
			var vm = CreateFeatureDialogViewModel.ForFeature();
			vm.Name = "  Gender  ";
			vm.Abbreviation = "  gen  ";
			Assert.That(vm.ChosenName, Is.EqualTo("Gender"));
			Assert.That(vm.ChosenAbbreviation, Is.EqualTo("gen"));
		}

		[AvaloniaTest]
		public void ValueDialog_UsesValueCaptions_AndGates()
		{
			var (view, vm) = Show(CreateFeatureDialogViewModel.ForValue(), "CreateValue-01-empty");
			Assert.That(vm.Title, Is.EqualTo(FwAvaloniaDialogsStrings.CreateValueTitle));
			Assert.That(vm.NameLabel, Is.EqualTo(FwAvaloniaDialogsStrings.CreateValueNameLabel));
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);

			vm.Name = "future";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		[AvaloniaTest]
		public void Ok_ClosesAccepted_WhenNamed()
		{
			var vm = CreateFeatureDialogViewModel.ForFeature();
			vm.Name = "Number";
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}

		[AvaloniaTest]
		public void Cancel_ClosesWithoutAccepting()
		{
			var vm = CreateFeatureDialogViewModel.ForFeature();
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
		}
	}
}
