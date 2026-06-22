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
	/// The delete-confirmation dialog (Phase-1 §19g): the Avalonia replacement for the WinForms
	/// ConfirmDeleteObjectDlg. Shows the affected-object summary (DeletionTextTSS) + an optional orphan/relation
	/// note over a Delete button GATED on CanDelete (with the confirm question hidden when the object cannot be
	/// deleted). Runtime proof on a realized headless surface, with per-stage PNGs.
	/// </summary>
	[TestFixture]
	public class DeleteConfirmationDialogTests
	{
		private static (DeleteConfirmationDialogView view, DeleteConfirmationDialogViewModel vm) Show(
			DeleteConfirmationDialogViewModel vm, string stageName)
		{
			var view = new DeleteConfirmationDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 400, Height = 240 };
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
		public void Renders_Summary_DeletableEnablesDelete()
		{
			var (view, vm) = Show(new DeleteConfirmationDialogViewModel("Entry: dog (noun)", null, true),
				"DeleteConfirmation-01-deletable");
			Assert.That(vm.Summary, Is.EqualTo("Entry: dog (noun)"));
			Assert.That(vm.CanDelete, Is.True);
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "Delete is enabled for a deletable object");
			Assert.That(vm.HasNote, Is.False);
		}

		[AvaloniaTest]
		public void DeleteDisabled_WhenCannotDelete()
		{
			var (view, vm) = Show(new DeleteConfirmationDialogViewModel("Entry: required (system)", null, false),
				"DeleteConfirmation-02-not-deletable");
			Assert.That(vm.CanDelete, Is.False);
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "Delete is disabled when the object cannot be deleted");
			Assert.That(vm.IsValid, Is.False);
		}

		[AvaloniaTest]
		public void Note_ShownWhenPresent()
		{
			var (view, vm) = Show(new DeleteConfirmationDialogViewModel(
					"Relation: Synonyms", "This is the last item; deleting it will remove the whole relation.", true),
				"DeleteConfirmation-03-with-note");
			Assert.That(vm.HasNote, Is.True);
			Assert.That(vm.Note, Does.Contain("whole relation"));
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		[AvaloniaTest]
		public void Ok_ClosesAccepted_WhenDeletable()
		{
			var vm = new DeleteConfirmationDialogViewModel("x", null, true);
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}

		[AvaloniaTest]
		public void Cancel_ClosesWithoutAccepting()
		{
			var vm = new DeleteConfirmationDialogViewModel("x", null, true);
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
		}

		[AvaloniaTest]
		public void NotDeletable_OkExecuteIsNoOp()
		{
			// Even a direct Execute must honor the gate (an undeletable object can never close via Delete).
			var vm = new DeleteConfirmationDialogViewModel("x", null, false);
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.Null, "the gate blocks accept");
			Assert.That(closed, Is.Null);
		}
	}
}
