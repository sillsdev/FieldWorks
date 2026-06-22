// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks;
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The "Reference Set Details" dialog (Phase-1 §19g): the LCModel-free collector behind
	/// LexReferenceMultiSlice.EditReferenceDetails (the Avalonia replacement for the WinForms
	/// LexReferenceDetailsDlg). Seeds + round-trips a name + comment over an always-enabled OK/Cancel. Runtime
	/// proof on a realized headless surface, with a per-stage PNG.
	/// </summary>
	[TestFixture]
	public class LexReferenceDetailsDialogTests
	{
		private static (LexReferenceDetailsDialogView view, LexReferenceDetailsDialogViewModel vm) Show(
			LexReferenceDetailsDialogViewModel vm, string stageName)
		{
			var view = new LexReferenceDetailsDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 360, Height = 280 };
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
		public void Seeds_NameAndComment()
		{
			var (view, vm) = Show(new LexReferenceDetailsDialogViewModel("Synonyms", "Words with the same meaning"),
				"LexReferenceDetails-01-seeded");
			Assert.That(vm.ReferenceName, Is.EqualTo("Synonyms"));
			Assert.That(vm.ReferenceComment, Is.EqualTo("Words with the same meaning"));
			Assert.That(vm.ChosenName, Is.EqualTo("Synonyms"));
			Assert.That(vm.ChosenComment, Is.EqualTo("Words with the same meaning"));
		}

		[AvaloniaTest]
		public void Ok_EnabledWithEmptyFields()
		{
			var (view, vm) = Show(new LexReferenceDetailsDialogViewModel(string.Empty, string.Empty),
				"LexReferenceDetails-02-empty");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "OK is never gated (an empty name/note is valid)");
			Assert.That(vm.IsValid, Is.True);
		}

		[AvaloniaTest]
		public void Edits_RoundTrip()
		{
			var vm = new LexReferenceDetailsDialogViewModel("old", "old note");
			vm.ReferenceName = "new name";
			vm.ReferenceComment = "new note";
			Assert.That(vm.ChosenName, Is.EqualTo("new name"));
			Assert.That(vm.ChosenComment, Is.EqualTo("new note"));
		}

		[AvaloniaTest]
		public void Ok_ClosesAccepted()
		{
			var vm = new LexReferenceDetailsDialogViewModel("x", "y");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}

		[AvaloniaTest]
		public void Cancel_ClosesWithoutAccepting()
		{
			var vm = new LexReferenceDetailsDialogViewModel("x", "y");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
		}
	}
}
