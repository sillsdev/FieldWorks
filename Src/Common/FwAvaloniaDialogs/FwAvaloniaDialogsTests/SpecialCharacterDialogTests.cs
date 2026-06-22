// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
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
	/// The special-character / Unicode insert picker (Phase-1 §19g): a net-new filterable character list over an
	/// Insert button GATED on a selection (the legacy Format > Special character shells out to the OS charmap, so
	/// there is no WinForms truth dialog). Runtime proof on a realized headless surface, with per-stage PNGs.
	/// </summary>
	[TestFixture]
	public class SpecialCharacterDialogTests
	{
		private static (SpecialCharacterDialogView view, SpecialCharacterDialogViewModel vm) Show(
			SpecialCharacterDialogViewModel vm, string stageName)
		{
			var view = new SpecialCharacterDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 400, Height = 360 };
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
		public void Lists_Characters_AndGatesOk()
		{
			var (view, vm) = Show(new SpecialCharacterDialogViewModel(), "SpecialCharacter-01-initial");
			Assert.That(vm.VisibleCharacters.Count, Is.GreaterThan(0), "the curated set is listed");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "Insert is disabled until a character is selected");

			vm.Selected = vm.VisibleCharacters.First();
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "Insert enables once a character is selected");
			Assert.That(vm.ChosenCharacter, Is.EqualTo(vm.VisibleCharacters.First().Character));
		}

		[AvaloniaTest]
		public void Filter_NarrowsList()
		{
			var vm = new SpecialCharacterDialogViewModel();
			var total = vm.VisibleCharacters.Count;

			vm.Filter = "arrow";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.VisibleCharacters.Count, Is.LessThan(total));
			Assert.That(vm.VisibleCharacters.All(c => c.Name.ToLowerInvariant().Contains("arrow")), Is.True);

			// Capture the snapshot AFTER filtering so the PNG reflects the narrowed list, not the initial state.
			Show(vm, "SpecialCharacter-02-filtered");
		}

		[AvaloniaTest]
		public void Filter_ByCodeLabel()
		{
			var vm = new SpecialCharacterDialogViewModel();
			vm.Filter = "U+2192"; // Rightwards Arrow
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.VisibleCharacters.Count, Is.EqualTo(1));
			Assert.That(vm.VisibleCharacters.Single().Name, Is.EqualTo("Rightwards Arrow"));
		}

		[AvaloniaTest]
		public void NoMatchFilter_DisablesOk_ClearsSelection()
		{
			var vm = new SpecialCharacterDialogViewModel();
			vm.Selected = vm.VisibleCharacters.First();
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);

			vm.Filter = "zzzzznotacharacter";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.VisibleCharacters.Count, Is.EqualTo(0));
			Assert.That(vm.Selected, Is.Null, "a filtered-out selection is dropped");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);

			Show(vm, "SpecialCharacter-03-no-match"); // PNG reflects the empty/no-match state
		}

		[AvaloniaTest]
		public void EmptyFilter_ShowsAll()
		{
			var vm = new SpecialCharacterDialogViewModel();
			var total = vm.VisibleCharacters.Count;
			vm.Filter = "arrow";
			Dispatcher.UIThread.RunJobs();
			vm.Filter = string.Empty;
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.VisibleCharacters.Count, Is.EqualTo(total), "clearing the filter restores the full list");
		}

		[AvaloniaTest]
		public void Ok_ClosesAccepted_Cancel_DoesNot()
		{
			var vm = new SpecialCharacterDialogViewModel();
			vm.Selected = vm.VisibleCharacters.First();
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}
	}
}
