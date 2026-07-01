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
	/// The Find/Replace pattern-setup dialog (Find/Replace Phase 1): a spec-only modal that edits a
	/// <see cref="FindReplacePattern"/> for a bulk replace. The VM gates OK on a non-empty find text and a
	/// valid regex (in regex mode), disables+clears the literal-only options when regex is on, and snapshots
	/// the edited pattern into Result on OK. No find engine; no modeless dialog (deferred P2).
	/// </summary>
	[TestFixture]
	public class FindReplaceDialogTests
	{
		private static (FindReplaceDialogView view, FindReplaceDialogViewModel vm) Show(
			FindReplacePattern seed = null, string stageName = "FindReplace-01-initial")
		{
			var vm = new FindReplaceDialogViewModel(seed);
			var view = new FindReplaceDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 400, Height = 300 };
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

		// Re-pump the realized surface and snapshot a later interaction stage (populated, regex, invalid, etc.).
		// Snapshots the view's hosting window (the view already has a visual parent).
		private static void Capture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		// ----- OK gating: empty find blocks OK, typing enables it -----

		[AvaloniaTest]
		public void EmptyFindText_BlocksOk()
		{
			// Empty find text == OK-disabled stage.
			var (_, vm) = Show(stageName: "FindReplace-03-invalid-empty");
			Assert.That(vm.IsValid, Is.False, "an empty find text gates OK off");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
		}

		[AvaloniaTest]
		public void NonEmptyFindText_EnablesOk()
		{
			var (view, vm) = Show();
			vm.FindText = "cat";
			Capture(view, "FindReplace-02-populated");
			Assert.That(vm.IsValid, Is.True, "a non-empty find text clears the OK gate");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		// ----- regex disables + clears the literal-only options -----

		[AvaloniaTest]
		public void UseRegex_DisablesAndClearsLiteralOptions()
		{
			var (_, vm) = Show();
			vm.MatchCase = true;
			vm.MatchWholeWord = true;
			Assert.That(vm.LiteralOptionsEnabled, Is.True, "literal options usable in literal mode");

			vm.UseRegularExpressions = true;
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.LiteralOptionsEnabled, Is.False, "regex disables the literal-only options");
			Assert.That(vm.MatchCase, Is.False, "regex clears MatchCase");
			Assert.That(vm.MatchWholeWord, Is.False, "regex clears MatchWholeWord");
		}

		[AvaloniaTest]
		public void Seed_WithRegexAndLiteralOptions_ClearsTheLiteralOptions()
		{
			// A seed that violates the invariant is normalized at construction.
			var (_, vm) = Show(new FindReplacePattern
			{
				FindText = "x",
				UseRegularExpressions = true,
				MatchCase = true,
				MatchWholeWord = true
			});
			Assert.That(vm.MatchCase, Is.False);
			Assert.That(vm.MatchWholeWord, Is.False);
		}

		// ----- invalid-regex gating -----

		[AvaloniaTest]
		public void InvalidRegex_BlocksOk_AndSurfacesError()
		{
			var (view, vm) = Show();
			vm.UseRegularExpressions = true;
			vm.FindText = "("; // an unterminated group: invalid regex
			Capture(view, "FindReplace-04-invalid-regex");

			Assert.That(vm.IsValid, Is.False, "an invalid regex gates OK off");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
			Assert.That(vm.HasInvalidRegex, Is.True, "the invalid-regex message is surfaced");
			Assert.That(vm.ValidationErrors, Is.Not.Empty);
		}

		[AvaloniaTest]
		public void ValidRegex_PassesTheGate()
		{
			var (view, vm) = Show();
			vm.UseRegularExpressions = true;
			vm.FindText = "ca+t"; // valid regex
			Capture(view, "FindReplace-05-regex-valid");

			Assert.That(vm.IsValid, Is.True, "a valid regex passes the gate");
			Assert.That(vm.HasInvalidRegex, Is.False);
		}

		// ----- OK snapshots the POCO -----

		[AvaloniaTest]
		public void Ok_SnapshotsThePatternIntoResult()
		{
			var (_, vm) = Show();
			vm.FindText = "cat";
			vm.ReplaceText = "dog";
			vm.MatchCase = true;
			vm.MatchWholeWord = true;
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.Result, Is.Not.Null);
			Assert.That(vm.Result.FindText, Is.EqualTo("cat"));
			Assert.That(vm.Result.ReplaceText, Is.EqualTo("dog"));
			Assert.That(vm.Result.MatchCase, Is.True);
			Assert.That(vm.Result.MatchWholeWord, Is.True);
			Assert.That(vm.Result.UseRegularExpressions, Is.False);
		}

		[AvaloniaTest]
		public void Cancel_NeverSnapshotsAResult()
		{
			var (_, vm) = Show();
			vm.FindText = "cat";
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
			Assert.That(vm.Result, Is.Null, "Cancel never snapshots a result");
		}

		// ----- the view binds the controls -----

		[AvaloniaTest]
		public void View_HostsTheFindReplaceControls()
		{
			var (view, _) = Show();
			var ids = view.GetVisualDescendants()
				.Select(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c as Avalonia.Controls.Control))
				.Where(id => !string.IsNullOrEmpty(id))
				.ToList();
			Assert.That(ids, Does.Contain("FindReplace.FindText"));
			Assert.That(ids, Does.Contain("FindReplace.ReplaceText"));
			Assert.That(ids, Does.Contain("FindReplace.UseRegex"));
			Assert.That(ids, Does.Contain("FindReplace.Ok"));
		}

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.FindReplaceTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FindReplaceFindLabel, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FindReplaceInvalidRegex, Is.Not.Null.And.Not.Empty);
		}
	}
}
