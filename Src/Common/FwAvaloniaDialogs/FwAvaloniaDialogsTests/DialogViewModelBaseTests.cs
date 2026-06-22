// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The shared <see cref="DialogViewModelBase"/>: a new dialog inherits the close contract, the generated
	/// Ok/Cancel commands, <c>Accepted</c>, and the <c>ApplyChanges</c> ("ApplyTo(state)") convention — so a
	/// dialog is "view + VM + ShowModal", not a copy of the Options plumbing.
	/// </summary>
	[TestFixture]
	public class DialogViewModelBaseTests
	{
		// Plain properties (no [ObservableProperty]) so the test assembly needs no MVVM source generator;
		// the inherited OkCommand/CancelCommand are generated in the base (FwAvaloniaDialogs) assembly.
		private sealed class SampleDialogViewModel : DialogViewModelBase
		{
			public string Value { get; set; } = "initial";
			public int AppliedCount { get; private set; }
			public string AppliedValue { get; private set; }

			protected override void ApplyChanges()
			{
				AppliedCount++;
				AppliedValue = Value;
			}
		}

		[Test]
		public void IsIDialogViewModel()
		{
			Assert.That(new SampleDialogViewModel(), Is.InstanceOf<IDialogViewModel>());
		}

		[Test]
		public void Ok_RunsApplyChanges_SetsAccepted_RaisesCloseTrue()
		{
			var vm = new SampleDialogViewModel { Value = "edited" };
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.OkCommand.Execute(null);

			Assert.That(vm.AppliedCount, Is.EqualTo(1), "OK applies changes exactly once before closing");
			Assert.That(vm.AppliedValue, Is.EqualTo("edited"), "ApplyChanges sees the edited VM state");
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}

		[Test]
		public void Cancel_DoesNotApply_SetsAcceptedFalse_RaisesCloseFalse()
		{
			var vm = new SampleDialogViewModel();
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.AppliedCount, Is.EqualTo(0), "Cancel must not apply changes");
			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
		}

		[Test]
		public void Accepted_NullUntilClosed()
		{
			Assert.That(new SampleDialogViewModel().Accepted, Is.Null);
		}

		[Test]
		public void DefaultApplyChanges_IsNoOp_DoesNotThrow()
		{
			var vm = new TrivialDialogViewModel();
			Assert.DoesNotThrow(() => vm.OkCommand.Execute(null));
			Assert.That(vm.Accepted, Is.True);
		}

		// A dialog that does not override ApplyChanges (e.g. a confirmation dialog).
		private sealed class TrivialDialogViewModel : DialogViewModelBase
		{
		}
	}
}
