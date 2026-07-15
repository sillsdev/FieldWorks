// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// The reusable message / confirmation dialog (the Avalonia analog of WinForms MessageBox): each button set
	/// surfaces the right buttons and returns the right <see cref="FwMessageBoxResult"/>, the default-button and
	/// dismiss handling matches WinForms, the VM honors the <see cref="IDialogViewModel"/> close contract, the
	/// button labels are localized, and the message/title render on a realized headless surface.
	/// </summary>
	[TestFixture]
	public class MessageBoxTests
	{
		private static (MessageBoxView view, MessageBoxViewModel vm) Show(
			string message = "Are you sure?",
			FwMessageBoxButtons buttons = FwMessageBoxButtons.Ok,
			FwMessageBoxIcon icon = FwMessageBoxIcon.None,
			string stageName = "MessageBox-01-initial")
		{
			var vm = new MessageBoxViewModel(message, buttons, icon);
			var view = new MessageBoxView { DataContext = vm };
			AvaloniaDialogTestHarness.Realize(view, 360, 160, stageName);
			return (view, vm);
		}

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> AvaloniaDialogTestHarness.FindByAutomationId<T>(root, id);

		private static Button Button(Control root, string id) => FindByAutomationId<Button>(root, id);

		// --- Initial realized stage (the documented 01-initial PNG): the OK-only message box on a realized
		// headless surface. Mirrors the other MessageBox capture calls (Show captures BEFORE asserting). ---

		[AvaloniaTest]
		public void OkOnly_InitialStage_RendersTheMessageAndOkButton()
		{
			var (view, _) = Show("Are you sure?", FwMessageBoxButtons.Ok, stageName: "MessageBox-01-initial");
			Assert.That(Button(view, "MessageBox.Ok").IsVisible, Is.True);
			Assert.That(Button(view, "MessageBox.Cancel").IsVisible, Is.False);
		}

		// --- Button set -> visible buttons + returned result (the VM is what FwMessageBox.Show reads) ---

		[Test]
		public void Ok_ShowsOnlyOk_AndReturnsOk()
		{
			var vm = new MessageBoxViewModel("hi", FwMessageBoxButtons.Ok, FwMessageBoxIcon.None);
			Assert.That(vm.ShowOk, Is.True);
			Assert.That(vm.ShowCancel || vm.ShowYes || vm.ShowNo, Is.False);

			vm.ConfirmOkCommand.Execute(null);

			Assert.That(vm.Result, Is.EqualTo(FwMessageBoxResult.Ok));
			Assert.That(vm.Accepted, Is.True);
		}

		[Test]
		public void OkCancel_OkReturnsOk_CancelReturnsCancel()
		{
			var ok = new MessageBoxViewModel("q", FwMessageBoxButtons.OkCancel, FwMessageBoxIcon.None);
			Assert.That(ok.ShowOk && ok.ShowCancel, Is.True);
			ok.ConfirmOkCommand.Execute(null);
			Assert.That(ok.Result, Is.EqualTo(FwMessageBoxResult.Ok));

			var cancel = new MessageBoxViewModel("q", FwMessageBoxButtons.OkCancel, FwMessageBoxIcon.None);
			cancel.CloseCancelCommand.Execute(null);
			Assert.That(cancel.Result, Is.EqualTo(FwMessageBoxResult.Cancel));
			Assert.That(cancel.Accepted, Is.False);
		}

		[Test]
		public void YesNo_YesReturnsYes_NoReturnsNo()
		{
			var yes = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNo, FwMessageBoxIcon.Question);
			Assert.That(yes.ShowYes && yes.ShowNo, Is.True);
			Assert.That(yes.ShowOk || yes.ShowCancel, Is.False);
			yes.YesCommand.Execute(null);
			Assert.That(yes.Result, Is.EqualTo(FwMessageBoxResult.Yes));
			Assert.That(yes.Accepted, Is.True);

			var no = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNo, FwMessageBoxIcon.Question);
			no.NoCommand.Execute(null);
			Assert.That(no.Result, Is.EqualTo(FwMessageBoxResult.No));
			Assert.That(no.Accepted, Is.False);
		}

		[Test]
		public void YesNoCancel_ShowsAllThree_AndEachReturnsItsResult()
		{
			var vm = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNoCancel, FwMessageBoxIcon.Warning);
			Assert.That(vm.ShowYes && vm.ShowNo && vm.ShowCancel, Is.True);
			Assert.That(vm.ShowOk, Is.False);

			var y = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNoCancel, FwMessageBoxIcon.None);
			y.YesCommand.Execute(null);
			Assert.That(y.Result, Is.EqualTo(FwMessageBoxResult.Yes));

			var n = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNoCancel, FwMessageBoxIcon.None);
			n.NoCommand.Execute(null);
			Assert.That(n.Result, Is.EqualTo(FwMessageBoxResult.No));

			var c = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNoCancel, FwMessageBoxIcon.None);
			c.CloseCancelCommand.Execute(null);
			Assert.That(c.Result, Is.EqualTo(FwMessageBoxResult.Cancel));
		}

		// --- Default / dismiss handling (a closed window matches WinForms' default for the set) ---

		[TestCase(FwMessageBoxButtons.Ok, FwMessageBoxResult.Ok)]
		[TestCase(FwMessageBoxButtons.OkCancel, FwMessageBoxResult.Cancel)]
		[TestCase(FwMessageBoxButtons.YesNo, FwMessageBoxResult.No)]
		[TestCase(FwMessageBoxButtons.YesNoCancel, FwMessageBoxResult.Cancel)]
		public void DismissedWithoutChoice_ReturnsTheSetsDefaultDismissResult(
			FwMessageBoxButtons buttons, FwMessageBoxResult expected)
		{
			// No command executed = window closed without a button (CloseRequested never fired).
			var vm = new MessageBoxViewModel("q", buttons, FwMessageBoxIcon.None);
			Assert.That(vm.Result, Is.EqualTo(expected));
			Assert.That(vm.Accepted, Is.Null, "no choice means Accepted stays null until a button closes it");
		}

		[AvaloniaTest]
		public void DefaultButton_IsAffirmative()
		{
			// OK is the default when present; otherwise Yes is.
			var (okView, _) = Show(buttons: FwMessageBoxButtons.OkCancel, stageName: "MessageBox-02-okcancel");
			Assert.That(Button(okView, "MessageBox.Ok").IsDefault, Is.True);
			Assert.That(Button(okView, "MessageBox.Cancel").IsCancel, Is.True);

			var (yesView, _) = Show(buttons: FwMessageBoxButtons.YesNoCancel,
				stageName: "MessageBox-03-yesnocancel");
			Assert.That(Button(yesView, "MessageBox.Yes").IsDefault, Is.True);
			Assert.That(Button(yesView, "MessageBox.Cancel").IsCancel, Is.True);
		}

		[AvaloniaTest]
		public void OnlyTheRequestedButtonsAreVisible()
		{
			var (view, _) = Show(buttons: FwMessageBoxButtons.YesNo, stageName: "MessageBox-04-yesno");
			Assert.That(Button(view, "MessageBox.Yes").IsVisible, Is.True);
			Assert.That(Button(view, "MessageBox.No").IsVisible, Is.True);
			Assert.That(Button(view, "MessageBox.Ok").IsVisible, Is.False);
			Assert.That(Button(view, "MessageBox.Cancel").IsVisible, Is.False);
		}

		// --- Close contract: the VM signals via IDialogViewModel; WireClose forwards it ---

		[Test]
		public void AffirmativeButton_RaisesCloseRequestedTrue()
		{
			var vm = new MessageBoxViewModel("q", FwMessageBoxButtons.YesNo, FwMessageBoxIcon.None);
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.YesCommand.Execute(null);

			Assert.That(closed, Is.True);
		}

		[Test]
		public void NegativeButton_RaisesCloseRequestedFalse()
		{
			var vm = new MessageBoxViewModel("q", FwMessageBoxButtons.OkCancel, FwMessageBoxIcon.None);
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CloseCancelCommand.Execute(null);

			Assert.That(closed, Is.False);
		}

		[Test]
		public void WireClose_ForwardsCloseSignal_ThenUnsubscribesOnDispose()
		{
			var vm = new MessageBoxViewModel("q", FwMessageBoxButtons.OkCancel, FwMessageBoxIcon.None);
			var calls = 0;
			using (AvaloniaDialogHost.WireClose(vm, _ => calls++))
			{
				vm.ConfirmOkCommand.Execute(null);
				Assert.That(calls, Is.EqualTo(1));
			}
			vm.CloseCancelCommand.Execute(null);
			Assert.That(calls, Is.EqualTo(1), "WireClose must unsubscribe on dispose");
		}

		// --- Localization: labels resolve from the shared accessor and bind into the XAML ---

		[Test]
		public void ButtonLabels_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.Yes, Is.EqualTo("Yes"));
			Assert.That(FwAvaloniaDialogsStrings.No, Is.EqualTo("No"));
			Assert.That(FwAvaloniaDialogsStrings.Ok, Is.EqualTo("OK"));
			Assert.That(FwAvaloniaDialogsStrings.Cancel, Is.EqualTo("Cancel"));
		}

		[AvaloniaTest]
		public void ButtonContent_ComesFromSharedAccessor_NotHardcodedEnglish()
		{
			var (view, _) = Show(buttons: FwMessageBoxButtons.YesNoCancel, stageName: "MessageBox-05-labels");
			Assert.That(Button(view, "MessageBox.Yes").Content, Is.EqualTo(FwAvaloniaDialogsStrings.Yes));
			Assert.That(Button(view, "MessageBox.No").Content, Is.EqualTo(FwAvaloniaDialogsStrings.No));
			Assert.That(Button(view, "MessageBox.Cancel").Content, Is.EqualTo(FwAvaloniaDialogsStrings.Cancel));
		}

		// --- Renders the message + icon ---

		[AvaloniaTest]
		public void Message_RendersInTheBody()
		{
			var (view, _) = Show("Delete this entry permanently?", stageName: "MessageBox-06-message");
			var body = FindByAutomationId<TextBlock>(view, "MessageBox.Message");
			Assert.That(body.Text, Is.EqualTo("Delete this entry permanently?"));
		}

		[AvaloniaTest]
		public void Icon_IsShownWithAccessibleName_WhenRequested_AndHiddenForNone()
		{
			var (withIcon, _) = Show(icon: FwMessageBoxIcon.Warning, stageName: "MessageBox-07-warning-icon");
			var icon = FindByAutomationId<TextBlock>(withIcon, "MessageBox.Icon");
			Assert.That(icon.IsVisible, Is.True);
			Assert.That(AutomationProperties.GetName(icon), Is.EqualTo(FwAvaloniaDialogsStrings.IconWarning));

			var (noIcon, _) = Show(icon: FwMessageBoxIcon.None, stageName: "MessageBox-08-no-icon");
			Assert.That(FindByAutomationId<TextBlock>(noIcon, "MessageBox.Icon").IsVisible, Is.False);
		}
	}
}
