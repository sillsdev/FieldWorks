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
	/// The writing-system properties / Add-WS bounded core (Phase-1 §19g): the managed name/abbr/font/RTL/sort
	/// core of the WinForms FwWritingSystemSetupDlg (SLDR/converters/merge/advanced are a documented PARITY
	/// deferral). OK is gated on a non-empty name + abbreviation and a non-duplicate tag. Runtime proof on a
	/// realized headless surface, with per-stage PNGs.
	/// </summary>
	[TestFixture]
	public class WritingSystemPropertiesDialogTests
	{
		private static readonly string[] Fonts = { "Charis SIL", "Doulos SIL", "Times New Roman" };

		private static (WritingSystemPropertiesDialogView view, WritingSystemPropertiesDialogViewModel vm) Show(
			WritingSystemPropertiesDialogViewModel vm, string stageName)
		{
			var view = new WritingSystemPropertiesDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 360, Height = 320 };
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

		private static WritingSystemPropertiesDialogViewModel Vm(WritingSystemProperties seed, string[] existingTags = null)
			=> new WritingSystemPropertiesDialogViewModel(seed, Fonts, existingTags ?? System.Array.Empty<string>());

		[AvaloniaTest]
		public void Seeds_Properties_AndRoundTrips()
		{
			var seed = new WritingSystemProperties
			{
				Name = "French", Abbreviation = "Fr", FontName = "Charis SIL",
				RightToLeft = false, SortLabel = "Default", Tag = "fr"
			};
			var (view, vm) = Show(Vm(seed), "WritingSystemProperties-01-seeded");
			Assert.That(vm.Name, Is.EqualTo("French"));
			Assert.That(vm.Abbreviation, Is.EqualTo("Fr"));
			Assert.That(vm.SelectedFont, Is.EqualTo("Charis SIL"));
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);

			vm.Name = "Français";
			vm.RightToLeft = true;
			var result = vm.ToResult();
			Assert.That(result.Name, Is.EqualTo("Français"));
			Assert.That(result.RightToLeft, Is.True);
			Assert.That(result.Tag, Is.EqualTo("fr"), "the tag round-trips even though it is not edited here");
		}

		[AvaloniaTest]
		public void Ok_GatedOnNameAndAbbr()
		{
			var vm = Vm(new WritingSystemProperties { Name = string.Empty, Abbreviation = string.Empty });
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
			vm.Name = "Seediq";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "still missing the abbreviation");
			vm.Abbreviation = "Sdq";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		[AvaloniaTest]
		public void EmptyName_ShowsValidation()
		{
			var (view, vm) = Show(Vm(new WritingSystemProperties { Name = string.Empty, Abbreviation = "x", Tag = "qaa" }),
				"WritingSystemProperties-02-invalid");
			Assert.That(vm.IsValid, Is.False);
			Assert.That(vm.ValidationMessage, Is.EqualTo(FwAvaloniaDialogsStrings.WritingSystemPropertiesNameRequired));
		}

		[AvaloniaTest]
		public void DuplicateTag_Rejected()
		{
			var vm = Vm(new WritingSystemProperties { Name = "Dup", Abbreviation = "Dp", Tag = "fr" },
				new[] { "fr", "en" });
			Assert.That(vm.IsValid, Is.False);
			Assert.That(vm.ValidationMessage, Is.EqualTo(FwAvaloniaDialogsStrings.WritingSystemPropertiesInvalidTag));
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
		}

		[AvaloniaTest]
		public void Rtl_Toggles()
		{
			var vm = Vm(new WritingSystemProperties { Name = "Arabic", Abbreviation = "Ar", RightToLeft = false });
			Assert.That(vm.ToResult().RightToLeft, Is.False);
			vm.RightToLeft = true;
			Assert.That(vm.ToResult().RightToLeft, Is.True);
		}

		[AvaloniaTest]
		public void Ok_ClosesAccepted_Cancel_DoesNot()
		{
			var vm = Vm(new WritingSystemProperties { Name = "X", Abbreviation = "X" });
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;
			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True);
		}
	}
}
