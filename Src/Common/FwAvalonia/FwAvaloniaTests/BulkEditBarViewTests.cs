// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// Phase-1 bulk-edit (List Choice) bar: the VM holds target/value state and gates Apply on a non-empty
	/// checked set, and the bar's Preview/Apply route through to the host seam. LCModel-free — the fake host
	/// stands in for the product edge (RecordBrowseView over the clerk-backed row source).
	/// </summary>
	[TestFixture]
	public class BulkEditBarViewTests
	{
		// A LCModel-free host: two list-choice targets, options per target, a settable checked count, and
		// recorders for the preview/apply/clear calls so the tests assert the bar drives the right path.
		private sealed class FakeBulkEditHost : IBulkEditBarHost
		{
			public int CheckedRowCount { get; set; }
			public readonly List<(int Column, RegionChoiceOption Option)> Previews
				= new List<(int, RegionChoiceOption)>();
			public readonly List<(int Column, RegionChoiceOption Option)> Applies
				= new List<(int, RegionChoiceOption)>();
			public int ClearCount;

			public IReadOnlyList<BulkEditTarget> ListChoiceTargets() => new[]
			{
				new BulkEditTarget(2, "Morph Type"),
				new BulkEditTarget(3, "Other Choice")
			};

			public IReadOnlyList<RegionChoiceOption> OptionsFor(int column) => column == 2
				? new[] { new RegionChoiceOption("g1", "root"), new RegionChoiceOption("g2", "stem") }
				: new[] { new RegionChoiceOption("h1", "alpha") };

			public void Preview(int column, RegionChoiceOption option) => Previews.Add((column, option));
			public void ClearPreview() => ClearCount++;
			public void Apply(int column, RegionChoiceOption option) => Applies.Add((column, option));

			// ----- Phase 2: Bulk Copy -----
			public readonly List<(int Source, int Target, BulkCopyMode Mode)> CopyPreviews
				= new List<(int, int, BulkCopyMode)>();
			public readonly List<(int Source, int Target, BulkCopyMode Mode)> CopyApplies
				= new List<(int, int, BulkCopyMode)>();

			public IReadOnlyList<BulkEditTarget> CopySourceColumns() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form"),
				new BulkEditTarget(2, "Morph Type")
			};

			public IReadOnlyList<BulkEditTarget> CopyTargets() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form")
			};

			public void PreviewCopy(int sourceColumn, int targetColumn, BulkCopyMode mode)
				=> CopyPreviews.Add((sourceColumn, targetColumn, mode));

			public void ApplyCopy(int sourceColumn, int targetColumn, BulkCopyMode mode)
				=> CopyApplies.Add((sourceColumn, targetColumn, mode));

			// ----- Phase 3: Bulk Clear -----
			public readonly List<int> ClearPreviews = new List<int>();
			public readonly List<int> ClearApplies = new List<int>();

			public IReadOnlyList<BulkEditTarget> ClearTargets() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form")
			};

			public void PreviewClear(int targetColumn) => ClearPreviews.Add(targetColumn);
			public void ApplyClear(int targetColumn) => ClearApplies.Add(targetColumn);

			// ----- Delete Rows (destructive mode of the Delete tab) -----
			// CanDeleteRows toggles whether the bar offers the Delete-Rows mode at all. DeletePreviewCount /
			// DeleteApplyCount record routing; DeletableCount / DeletedCount model the host's deletable/deleted
			// numbers so the VM can be exercised without an LCModel cache.
			public bool CanDeleteRows { get; set; } = true;
			public int DeletePreviewCount;
			public int DeleteApplyCount;
			public int DeletableCount = 2;
			public int DeletedCount = 2;

			public int PreviewDeleteRows()
			{
				DeletePreviewCount++;
				return DeletableCount;
			}

			public int ApplyDeleteRows()
			{
				DeleteApplyCount++;
				return DeletedCount;
			}

			// ----- Find/Replace Phase 1: Bulk Replace -----
			public readonly List<(int Target, BulkReplaceSpec Spec)> ReplacePreviews
				= new List<(int, BulkReplaceSpec)>();
			public readonly List<(int Target, BulkReplaceSpec Spec)> ReplaceApplies
				= new List<(int, BulkReplaceSpec)>();
			// What the next Setup… dialog "returns" (null models a Cancel); how many times it was invoked.
			public BulkReplaceSpec NextSetupResult;
			public int SetupCount;
			public BulkReplaceSpec LastSetupSeed;

			public IReadOnlyList<BulkEditTarget> ReplaceTargets() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form")
			};

			public BulkReplaceSpec ShowFindReplaceSetup(BulkReplaceSpec current)
			{
				SetupCount++;
				LastSetupSeed = current;
				return NextSetupResult;
			}

			public void PreviewReplace(int targetColumn, BulkReplaceSpec spec)
				=> ReplacePreviews.Add((targetColumn, spec));

			public void ApplyReplace(int targetColumn, BulkReplaceSpec spec)
				=> ReplaceApplies.Add((targetColumn, spec));

			// ----- Process / Transduce -----
			// A deterministic, EncConverters-free converter for the headless tests: upper-cases the input.
			internal sealed class UpperConverter : IBulkTransduceConverter
			{
				public UpperConverter(string name) { Name = name; }
				public string Name { get; }
				public string Convert(string input) => (input ?? string.Empty).ToUpperInvariant();
			}

			public readonly List<(int Source, int Target, IBulkTransduceConverter Conv, BulkCopyMode Mode)> TransducePreviews
				= new List<(int, int, IBulkTransduceConverter, BulkCopyMode)>();
			public readonly List<(int Source, int Target, IBulkTransduceConverter Conv, BulkCopyMode Mode)> TransduceApplies
				= new List<(int, int, IBulkTransduceConverter, BulkCopyMode)>();
			// The list the next Setup… "returns" (null models unavailable/cancel); how many times it was invoked.
			public IReadOnlyList<IBulkTransduceConverter> NextSetupConverters;
			public int TransduceSetupCount;
			// What AvailableConverters reports (defaults to one converter so the tab is usable by default).
			public IReadOnlyList<IBulkTransduceConverter> Converters
				= new IBulkTransduceConverter[] { new UpperConverter("UPPER"), new UpperConverter("ALT") };

			public IReadOnlyList<BulkEditTarget> TransduceSourceColumns() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form"),
				new BulkEditTarget(2, "Morph Type")
			};

			public IReadOnlyList<BulkEditTarget> TransduceColumns() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form")
			};

			public IReadOnlyList<IBulkTransduceConverter> AvailableConverters() => Converters;

			public IReadOnlyList<IBulkTransduceConverter> LaunchConverterSetup()
			{
				TransduceSetupCount++;
				return NextSetupConverters;
			}

			public void PreviewTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter, BulkCopyMode mode)
				=> TransducePreviews.Add((sourceColumn, targetColumn, converter, mode));

			public void ApplyTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter, BulkCopyMode mode)
				=> TransduceApplies.Add((sourceColumn, targetColumn, converter, mode));

			// ----- Click Copy -----
			public readonly List<(int Source, int Target, int Row, int Offset, ClickCopyMode Mode, string Sep, bool Append)> ClickCopies
				= new List<(int, int, int, int, ClickCopyMode, string, bool)>();

			public IReadOnlyList<BulkEditTarget> ClickCopyTargets() => new[]
			{
				new BulkEditTarget(0, "Lexeme Form"),
				new BulkEditTarget(1, "Citation Form")
			};

			public void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, int charOffset,
				ClickCopyMode mode, string separator, bool append)
				=> ClickCopies.Add((sourceColumn, targetColumn, rowIndex, charOffset, mode, separator, append));
		}

		private static BulkEditBarView Show(FakeBulkEditHost host, out BulkEditBarViewModel vm)
		{
			vm = new BulkEditBarViewModel(host);
			var bar = new BulkEditBarView(vm);
			var window = new Window { Content = bar, Width = 600, Height = 80 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return bar;
		}

		// ----- Visual snapshots: the bar in each tab (one PNG per tab) -----

		[AvaloniaTest]
		public void Snapshot_EachTab_RendersTheBarCleanly()
		{
			// A host with checked rows so Apply-side controls render enabled, can-delete on so the Delete tab
			// offers both modes, and the default converter list so the Transduce tab is populated.
			var host = new FakeBulkEditHost { CheckedRowCount = 3, CanDeleteRows = true };
			var vm = new BulkEditBarViewModel(host);
			var bar = new BulkEditBarView(vm);
			var window = new Window { Content = bar, Width = 640, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			void CaptureTab(int index, string stage)
			{
				bar.Tabs.SelectedIndex = index;
				Dispatcher.UIThread.RunJobs();
				window.UpdateLayout();
				Dispatcher.UIThread.RunJobs();
				DialogSnapshot.Capture(window, "BulkEditBar-" + stage);
				DialogLayoutAssert.AssertNoCrowding(bar);
			}

			CaptureTab(0, "01-listchoice");
			CaptureTab(1, "02-bulkcopy");
			CaptureTab(2, "03-clearfield");

			// The Delete tab's destructive mode is reached through the Clear tab's mode combo.
			bar.Tabs.SelectedIndex = 2;
			Dispatcher.UIThread.RunJobs();
			vm.BulkClear.Mode = BulkDeleteMode.DeleteRows;
			bar.DeleteWhatCombo.SelectedItem = BulkDeleteMode.DeleteRows;
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "BulkEditBar-04-deleterows");
			DialogLayoutAssert.AssertNoCrowding(bar);

			CaptureTab(3, "05-replace");
			CaptureTab(4, "06-transduce");
			CaptureTab(5, "07-clickcopy");

			Assert.That(bar.Tabs.ItemCount, Is.EqualTo(6), "all six bulk-edit tabs render");
		}

		[AvaloniaTest]
		public void Vm_SeedsFirstTarget_AndExposesItsOptions()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);

			Assert.That(vm.Targets, Has.Count.EqualTo(2));
			Assert.That(vm.SelectedTarget?.Label, Is.EqualTo("Morph Type"), "the first eligible target is seeded");
			Assert.That(vm.Options.Select(o => o.Name), Is.EquivalentTo(new[] { "root", "stem" }),
				"the value options are the selected target's options");
		}

		[AvaloniaTest]
		public void Apply_IsDisabled_UntilTargetValueAndCheckedRows()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			var bar = Show(host, out var vm);

			Assert.That(vm.CanApply, Is.False, "no value chosen and nothing checked");
			Assert.That(bar.ApplyButton.IsEnabled, Is.False);

			vm.SelectedOption = vm.Options.First(); // value chosen, but still nothing checked
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.CanApply, Is.False, "still nothing checked");
			Assert.That(bar.ApplyButton.IsEnabled, Is.False);

			host.CheckedRowCount = 3;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.CanApply, Is.True, "target + value + checked rows enables Apply");
			Assert.That(bar.ApplyButton.IsEnabled, Is.True);
			Assert.That(bar.PreviewButton.IsEnabled, Is.True);
		}

		[AvaloniaTest]
		public void PreviewButton_RoutesToHost_WithSelectedTargetAndValue()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 2 };
			var bar = Show(host, out var vm);
			vm.SelectedOption = vm.Options.First(o => o.Key == "g2");
			Dispatcher.UIThread.RunJobs();

			bar.PreviewButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.Previews, Has.Count.EqualTo(1));
			Assert.That(host.Previews[0].Column, Is.EqualTo(2), "previews the selected target's column");
			Assert.That(host.Previews[0].Option.Key, Is.EqualTo("g2"), "previews the chosen value");
		}

		[AvaloniaTest]
		public void ApplyButton_RoutesToHost_WhenEnabled()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 5 };
			var bar = Show(host, out var vm);
			vm.SelectedOption = vm.Options.First();
			Dispatcher.UIThread.RunJobs();

			bar.ApplyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.Applies, Has.Count.EqualTo(1));
			Assert.That(host.Applies[0].Column, Is.EqualTo(2));
			Assert.That(host.Applies[0].Option.Key, Is.EqualTo("g1"));
		}

		[AvaloniaTest]
		public void ChangingTargetOrValue_ClearsThePendingPreview()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);

			vm.SelectedOption = vm.Options.First();
			Assert.That(host.ClearCount, Is.EqualTo(1), "choosing a value clears the pending preview");

			vm.SelectedTarget = vm.Targets[1];
			Assert.That(host.ClearCount, Is.EqualTo(2), "switching target clears the pending preview");
			Assert.That(vm.SelectedOption, Is.Null, "switching target resets the chosen value");
			Assert.That(vm.Options.Select(o => o.Name), Is.EquivalentTo(new[] { "alpha" }),
				"the value options follow the new target");
		}

		[AvaloniaTest]
		public void Apply_NoOp_WhenNotEnabled()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			Show(host, out var vm);
			vm.SelectedOption = vm.Options.First();

			vm.Apply(); // CanApply is false (nothing checked)
			Assert.That(host.Applies, Is.Empty, "Apply is a no-op while disabled");
		}

		// ----- Phase 2: Bulk Copy tab -----

		[AvaloniaTest]
		public void BulkCopy_Vm_SeedsSourceAndTarget_FromHost_AndDefaultsToAppend()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);
			var copy = vm.BulkCopy;

			Assert.That(copy.Sources.Select(s => s.Label),
				Is.EquivalentTo(new[] { "Lexeme Form", "Citation Form", "Morph Type" }),
				"every column is a copy source candidate");
			Assert.That(copy.Targets.Select(t => t.Label),
				Is.EquivalentTo(new[] { "Lexeme Form", "Citation Form" }),
				"only the host's eligible writable columns are copy targets");
			// The seeded source is a DISTINCT column from the seeded target (the first target is column 0, so
			// the source seeds to the first non-0 column) and the default mode is Append.
			Assert.That(copy.SelectedTarget?.Column, Is.EqualTo(0));
			Assert.That(copy.SelectedSource?.Column, Is.Not.EqualTo(copy.SelectedTarget?.Column),
				"the seeded source differs from the seeded target so the initial pair is usable");
			Assert.That(copy.Mode, Is.EqualTo(BulkCopyMode.Append));
		}

		[AvaloniaTest]
		public void BulkCopy_Apply_IsDisabled_UntilDistinctSourceTargetAndCheckedRows()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			var bar = Show(host, out var vm);
			var copy = vm.BulkCopy;

			// Distinct source/target are seeded but nothing is checked.
			Assert.That(copy.CanApply, Is.False, "nothing checked");
			Assert.That(bar.CopyApplyButton.IsEnabled, Is.False);

			host.CheckedRowCount = 4;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();
			Assert.That(copy.CanApply, Is.True, "distinct source/target + checked rows enables Apply");
			Assert.That(bar.CopyApplyButton.IsEnabled, Is.True);
			Assert.That(bar.CopyPreviewButton.IsEnabled, Is.True);

			// Source == target disables again.
			copy.SelectedSource = copy.Sources.First(s => s.Column == copy.SelectedTarget.Column);
			Dispatcher.UIThread.RunJobs();
			Assert.That(copy.CanApply, Is.False, "a self-copy (source == target) is disallowed");
			Assert.That(bar.CopyApplyButton.IsEnabled, Is.False);
		}

		[AvaloniaTest]
		public void BulkCopy_PreviewAndApply_RouteToHost_WithSourceTargetAndMode()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 3 };
			var bar = Show(host, out var vm);
			var copy = vm.BulkCopy;
			copy.SelectedSource = copy.Sources.First(s => s.Column == 2); // Morph Type
			copy.SelectedTarget = copy.Targets.First(t => t.Column == 1); // Citation Form
			copy.Mode = BulkCopyMode.Replace;
			Dispatcher.UIThread.RunJobs();

			bar.CopyPreviewButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			bar.CopyApplyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.CopyPreviews, Has.Count.EqualTo(1));
			Assert.That(host.CopyPreviews[0], Is.EqualTo((2, 1, BulkCopyMode.Replace)));
			Assert.That(host.CopyApplies, Has.Count.EqualTo(1));
			Assert.That(host.CopyApplies[0], Is.EqualTo((2, 1, BulkCopyMode.Replace)));
		}

		[AvaloniaTest]
		public void BulkCopy_ChangingSourceTargetOrMode_ClearsThePendingPreview()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var copy = vm.BulkCopy;
			var clearAtStart = host.ClearCount;

			copy.SelectedSource = copy.Sources.First(s => s.Column == 2);
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 1), "changing source clears the preview");

			copy.SelectedTarget = copy.Targets.First(t => t.Column == 1);
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 2), "changing target clears the preview");

			copy.Mode = BulkCopyMode.DoNothingIfNonEmpty;
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 3), "changing mode clears the preview");
		}

		[AvaloniaTest]
		public void BulkCopy_Apply_NoOp_WhenSourceEqualsTarget()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 5 };
			Show(host, out var vm);
			var copy = vm.BulkCopy;
			copy.SelectedTarget = copy.Targets.First(t => t.Column == 0);
			copy.SelectedSource = copy.Sources.First(s => s.Column == 0); // same as target

			copy.Apply();
			Assert.That(host.CopyApplies, Is.Empty, "Apply is a no-op when source == target");
		}

		// ----- Phase 3: Bulk Clear tab -----

		[AvaloniaTest]
		public void BulkClear_Vm_SeedsFirstTarget_FromHostEligibleColumns()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);
			var clear = vm.BulkClear;

			Assert.That(clear.Targets.Select(t => t.Label),
				Is.EquivalentTo(new[] { "Lexeme Form", "Citation Form" }),
				"only the host's eligible writable columns are clear targets");
			Assert.That(clear.SelectedTarget?.Column, Is.EqualTo(0), "the first eligible target is seeded");
		}

		[AvaloniaTest]
		public void BulkClear_Apply_IsDisabled_UntilTargetAndCheckedRows()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			var bar = Show(host, out var vm);
			var clear = vm.BulkClear;

			// A target is seeded but nothing is checked.
			Assert.That(clear.CanApply, Is.False, "nothing checked");
			Assert.That(bar.ClearApplyButton.IsEnabled, Is.False);

			host.CheckedRowCount = 3;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();
			Assert.That(clear.CanApply, Is.True, "a chosen target + checked rows enables Apply");
			Assert.That(bar.ClearApplyButton.IsEnabled, Is.True);
			Assert.That(bar.ClearPreviewButton.IsEnabled, Is.True);
		}

		[AvaloniaTest]
		public void BulkClear_PreviewAndApply_RouteToHost_WithSelectedTarget()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 4 };
			var bar = Show(host, out var vm);
			var clear = vm.BulkClear;
			clear.SelectedTarget = clear.Targets.First(t => t.Column == 1); // Citation Form
			Dispatcher.UIThread.RunJobs();

			bar.ClearPreviewButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			bar.ClearApplyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.ClearPreviews, Is.EqualTo(new[] { 1 }), "previews the selected target column");
			Assert.That(host.ClearApplies, Is.EqualTo(new[] { 1 }), "applies to the selected target column");
		}

		[AvaloniaTest]
		public void BulkClear_ChangingTarget_ClearsThePendingPreview()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var clear = vm.BulkClear;
			var clearAtStart = host.ClearCount;

			clear.SelectedTarget = clear.Targets.First(t => t.Column == 1);
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 1), "changing target clears the pending preview");
		}

		[AvaloniaTest]
		public void BulkClear_Apply_NoOp_WhenNotEnabled()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			Show(host, out var vm);

			vm.BulkClear.Apply(); // CanApply is false (nothing checked)
			Assert.That(host.ClearApplies, Is.Empty, "Apply is a no-op while disabled");
		}

		// ----- Delete tab: Delete-Rows (destructive) mode -----

		[AvaloniaTest]
		public void DeleteTab_StartsInClearFieldMode_AndOffersBothModes_WhenHostCanDelete()
		{
			var host = new FakeBulkEditHost { CanDeleteRows = true };
			var bar = Show(host, out var vm);

			Assert.That(vm.BulkClear.Mode, Is.EqualTo(BulkDeleteMode.ClearField), "starts in the non-destructive mode");
			Assert.That(vm.BulkClear.IsDeleteRows, Is.False);
			Assert.That(bar.DeleteWhatCombo.ItemCount, Is.EqualTo(2), "both Clear Field and Delete Rows are offered");
			// In Clear-Field mode the target column combo is shown and Apply reads the (non-delete) caption.
			Assert.That(bar.ClearTargetCombo.IsVisible, Is.True);
			Assert.That(bar.ClearApplyButton.Content, Is.EqualTo(FwAvaloniaStrings.BulkEditApply));
		}

		[AvaloniaTest]
		public void DeleteTab_OmitsDeleteRowsMode_WhenHostCannotDelete()
		{
			var host = new FakeBulkEditHost { CanDeleteRows = false };
			var bar = Show(host, out var vm);

			Assert.That(bar.DeleteWhatCombo.ItemCount, Is.EqualTo(1), "only Clear Field is offered (no destructive option)");
			Assert.That(vm.BulkClear.CanDeleteRows, Is.False);
		}

		[AvaloniaTest]
		public void DeleteTab_SwitchingToDeleteRows_TogglesControls_AndReCaptionsApply()
		{
			var host = new FakeBulkEditHost { CanDeleteRows = true, CheckedRowCount = 3 };
			var bar = Show(host, out var vm);
			var clearAtStart = host.ClearCount;

			vm.BulkClear.Mode = BulkDeleteMode.DeleteRows;
			// Drive the view's mode-UI swap (the combo handler does this in the real UI; call it directly here).
			bar.DeleteWhatCombo.SelectedItem = BulkDeleteMode.DeleteRows;
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.BulkClear.IsDeleteRows, Is.True);
			Assert.That(host.ClearCount, Is.GreaterThan(clearAtStart), "switching mode clears any pending preview");
			Assert.That(bar.ClearTargetCombo.IsVisible, Is.False, "the target column is irrelevant in Delete-Rows mode");
			Assert.That(bar.ClearApplyButton.Content, Is.EqualTo(FwAvaloniaStrings.BulkDeleteApply),
				"Apply re-captions to Delete");
		}

		[AvaloniaTest]
		public void DeleteRows_Apply_IsDisabled_UntilCheckedRows()
		{
			var host = new FakeBulkEditHost { CanDeleteRows = true, CheckedRowCount = 0 };
			var bar = Show(host, out var vm);
			vm.BulkClear.Mode = BulkDeleteMode.DeleteRows;
			bar.DeleteWhatCombo.SelectedItem = BulkDeleteMode.DeleteRows;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.BulkClear.CanApply, Is.False, "nothing checked");
			Assert.That(bar.ClearApplyButton.IsEnabled, Is.False);

			host.CheckedRowCount = 2;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.BulkClear.CanApply, Is.True, "checked rows + can-delete enables Delete");
			Assert.That(bar.ClearApplyButton.IsEnabled, Is.True);
		}

		[AvaloniaTest]
		public void DeleteRows_PreviewAndApply_RouteToHost_DeletePath_NotClearPath()
		{
			var host = new FakeBulkEditHost { CanDeleteRows = true, CheckedRowCount = 3 };
			var bar = Show(host, out var vm);
			vm.BulkClear.Mode = BulkDeleteMode.DeleteRows;
			bar.DeleteWhatCombo.SelectedItem = BulkDeleteMode.DeleteRows;
			Dispatcher.UIThread.RunJobs();

			bar.ClearPreviewButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			bar.ClearApplyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.DeletePreviewCount, Is.EqualTo(1), "Preview routes to the delete-rows path");
			Assert.That(host.DeleteApplyCount, Is.EqualTo(1), "Apply routes to the delete-rows path");
			Assert.That(host.ClearPreviews, Is.Empty, "the clear-field path is NOT used in Delete-Rows mode");
			Assert.That(host.ClearApplies, Is.Empty);
		}

		[AvaloniaTest]
		public void DeleteRows_Apply_NoOp_WhenNothingChecked()
		{
			var host = new FakeBulkEditHost { CanDeleteRows = true, CheckedRowCount = 0 };
			Show(host, out var vm);
			vm.BulkClear.Mode = BulkDeleteMode.DeleteRows;

			vm.BulkClear.Apply(); // CanApply is false (nothing checked)
			Assert.That(host.DeleteApplyCount, Is.EqualTo(0), "Delete is a no-op while disabled (no confirmation, no delete)");
		}

		// ----- Find/Replace Phase 1: Bulk Replace tab -----

		[AvaloniaTest]
		public void BulkReplace_Vm_SeedsFirstTarget_AndStartsWithNoPattern()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);
			var replace = vm.BulkReplace;

			Assert.That(replace.Targets.Select(t => t.Label),
				Is.EquivalentTo(new[] { "Lexeme Form", "Citation Form" }),
				"only the host's eligible writable columns are replace targets");
			Assert.That(replace.SelectedTarget?.Column, Is.EqualTo(0), "the first eligible target is seeded");
			Assert.That(replace.Spec.FindText, Is.Empty, "no find pattern yet");
		}

		[AvaloniaTest]
		public void BulkReplace_Apply_IsDisabled_UntilTargetFindTextAndCheckedRows()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			var bar = Show(host, out var vm);
			var replace = vm.BulkReplace;

			Assert.That(replace.CanApply, Is.False, "no find text and nothing checked");
			Assert.That(bar.ReplaceApplyButton.IsEnabled, Is.False);

			// Setup returns a spec with find text, but still nothing is checked.
			host.NextSetupResult = new BulkReplaceSpec { FindText = "foo", ReplaceText = "bar" };
			replace.Setup();
			Dispatcher.UIThread.RunJobs();
			Assert.That(replace.CanApply, Is.False, "find text set, but still nothing checked");
			Assert.That(bar.ReplaceApplyButton.IsEnabled, Is.False);

			host.CheckedRowCount = 3;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();
			Assert.That(replace.CanApply, Is.True, "target + find text + checked rows enables Apply");
			Assert.That(bar.ReplaceApplyButton.IsEnabled, Is.True);
			Assert.That(bar.ReplacePreviewButton.IsEnabled, Is.True);
		}

		[AvaloniaTest]
		public void BulkReplace_Setup_RecordsTheEditedSpec_AndUpdatesSummary()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			var bar = Show(host, out var vm);
			var replace = vm.BulkReplace;

			host.NextSetupResult = new BulkReplaceSpec { FindText = "cat", ReplaceText = "dog" };
			replace.Setup();
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.SetupCount, Is.EqualTo(1), "Setup opened the dialog once");
			Assert.That(replace.Spec.FindText, Is.EqualTo("cat"), "the edited spec is recorded");
			Assert.That(replace.Summary, Does.Contain("cat").And.Contains("dog"), "the summary reflects the pattern");
			Assert.That(bar.ReplaceSummary.Text, Does.Contain("cat"), "the bar's summary text is updated");
		}

		[AvaloniaTest]
		public void BulkReplace_SetupCancel_LeavesSpecUntouched()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var replace = vm.BulkReplace;

			host.NextSetupResult = new BulkReplaceSpec { FindText = "x", ReplaceText = "y" };
			replace.Setup();
			Assert.That(replace.Spec.FindText, Is.EqualTo("x"));

			// A Cancel (null) leaves the prior spec in place.
			host.NextSetupResult = null;
			replace.Setup();
			Assert.That(replace.Spec.FindText, Is.EqualTo("x"), "Cancel does not change the spec");
		}

		[AvaloniaTest]
		public void BulkReplace_PreviewAndApply_RouteToHost_WithTargetAndSpec()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 4 };
			var bar = Show(host, out var vm);
			var replace = vm.BulkReplace;
			replace.SelectedTarget = replace.Targets.First(t => t.Column == 1);
			host.NextSetupResult = new BulkReplaceSpec { FindText = "a", ReplaceText = "b" };
			replace.Setup();
			Dispatcher.UIThread.RunJobs();

			bar.ReplacePreviewButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			bar.ReplaceApplyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.ReplacePreviews, Has.Count.EqualTo(1));
			Assert.That(host.ReplacePreviews[0].Target, Is.EqualTo(1), "previews the selected target column");
			Assert.That(host.ReplacePreviews[0].Spec.FindText, Is.EqualTo("a"), "previews the chosen spec");
			Assert.That(host.ReplaceApplies, Has.Count.EqualTo(1));
			Assert.That(host.ReplaceApplies[0].Target, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void BulkReplace_ChangingTargetOrSetup_ClearsThePendingPreview()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var replace = vm.BulkReplace;
			var clearAtStart = host.ClearCount;

			replace.SelectedTarget = replace.Targets.First(t => t.Column == 1);
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 1), "changing target clears the preview");

			host.NextSetupResult = new BulkReplaceSpec { FindText = "z" };
			replace.Setup();
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 2), "a new pattern clears the preview");
		}

		[AvaloniaTest]
		public void BulkReplace_Apply_NoOp_WhenNotEnabled()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			Show(host, out var vm);
			host.NextSetupResult = new BulkReplaceSpec { FindText = "q" };
			vm.BulkReplace.Setup();

			vm.BulkReplace.Apply(); // CanApply is false (nothing checked)
			Assert.That(host.ReplaceApplies, Is.Empty, "Apply is a no-op while disabled");
		}

		// ----- Process / Transduce tab -----

		[AvaloniaTest]
		public void Transduce_TabRenders_WithSourceConverterTargetPickers_AndSetupButton()
		{
			var host = new FakeBulkEditHost();
			var bar = Show(host, out var vm);
			var trd = vm.BulkTransduce;

			// The pickers are seeded from the host and the Setup button is present.
			Assert.That(bar.TransduceSourceCombo, Is.Not.Null);
			Assert.That(bar.TransduceConverterCombo, Is.Not.Null);
			Assert.That(bar.TransduceTargetCombo, Is.Not.Null);
			Assert.That(bar.TransduceSetupButton, Is.Not.Null);
			Assert.That(trd.Targets.Select(t => t.Label),
				Is.EquivalentTo(new[] { "Lexeme Form", "Citation Form" }),
				"only the host's eligible writable columns are transduce targets");
			Assert.That(trd.Converters.Select(c => c.Name), Is.EquivalentTo(new[] { "UPPER", "ALT" }),
				"the converter picker is the host's available converters");
			Assert.That(trd.SelectedConverter?.Name, Is.EqualTo("UPPER"), "the first converter is seeded");
			Assert.That(trd.SelectedTarget?.Column, Is.EqualTo(0), "the first eligible target is seeded");
			Assert.That(trd.SelectedSource?.Column, Is.Not.EqualTo(trd.SelectedTarget?.Column),
				"the seeded source differs from the seeded target so the initial pair is usable");
			Assert.That(trd.Mode, Is.EqualTo(BulkCopyMode.Append), "default mode is Append");
		}

		[AvaloniaTest]
		public void Transduce_Setup_RefreshesConverterList_PreservingSelectionByName()
		{
			// §19f.3: Setup launches the EncConverters dialog (host hook) and re-publishes the refreshed list,
			// preserving the current selection by NAME when it still exists.
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			host.NextSetupConverters = new IBulkTransduceConverter[]
			{
				new FakeBulkEditHost.UpperConverter("ALT"),
				new FakeBulkEditHost.UpperConverter("NEWCONV")
			};
			var bar = Show(host, out var vm);
			var trd = vm.BulkTransduce;
			trd.SelectedConverter = trd.Converters.First(c => c.Name == "ALT"); // selection that survives setup
			Dispatcher.UIThread.RunJobs();

			trd.Setup();
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.TransduceSetupCount, Is.EqualTo(1), "Setup launches the converter-management hook once");
			Assert.That(trd.Converters.Select(c => c.Name), Is.EquivalentTo(new[] { "ALT", "NEWCONV" }),
				"the refreshed list is published (the new converter appears)");
			Assert.That(trd.SelectedConverter?.Name, Is.EqualTo("ALT"),
				"the prior selection is preserved by name across the refresh");
		}

		[AvaloniaTest]
		public void Transduce_Setup_Cancelled_LeavesConverterListUntouched()
		{
			// A null setup result (cancel / unavailable) leaves the picker untouched.
			var host = new FakeBulkEditHost { CheckedRowCount = 1, NextSetupConverters = null };
			var bar = Show(host, out var vm);
			var trd = vm.BulkTransduce;
			var before = trd.Converters.Select(c => c.Name).ToList();

			trd.Setup();
			Dispatcher.UIThread.RunJobs();

			Assert.That(trd.Converters.Select(c => c.Name), Is.EqualTo(before), "a cancelled Setup is a no-op on the list");
		}

		[AvaloniaTest]
		public void Transduce_Apply_IsDisabled_UntilSourceConverterTargetAndCheckedRows()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			var bar = Show(host, out var vm);
			var trd = vm.BulkTransduce;

			// Distinct source/target + a converter are seeded but nothing is checked.
			Assert.That(trd.CanApply, Is.False, "nothing checked");
			Assert.That(bar.TransduceApplyButton.IsEnabled, Is.False);

			host.CheckedRowCount = 4;
			bar.RefreshEnablement();
			Dispatcher.UIThread.RunJobs();
			Assert.That(trd.CanApply, Is.True, "source + converter + distinct target + checked rows enables Apply");
			Assert.That(bar.TransduceApplyButton.IsEnabled, Is.True);
			Assert.That(bar.TransducePreviewButton.IsEnabled, Is.True);

			// No converter disables again.
			trd.SelectedConverter = null;
			Dispatcher.UIThread.RunJobs();
			Assert.That(trd.CanApply, Is.False, "no converter chosen disables Apply");
			Assert.That(bar.TransduceApplyButton.IsEnabled, Is.False);
		}

		[AvaloniaTest]
		public void Transduce_Apply_IsDisabled_WhenSourceEqualsTarget()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 3 };
			var bar = Show(host, out var vm);
			var trd = vm.BulkTransduce;

			trd.SelectedSource = trd.Sources.First(s => s.Column == trd.SelectedTarget.Column);
			Dispatcher.UIThread.RunJobs();
			Assert.That(trd.CanApply, Is.False, "a self-transduce (source == target) is disallowed");
			Assert.That(bar.TransduceApplyButton.IsEnabled, Is.False);
		}

		[AvaloniaTest]
		public void Transduce_PreviewAndApply_RouteToHost_WithSourceConverterTargetAndMode()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 3 };
			var bar = Show(host, out var vm);
			var trd = vm.BulkTransduce;
			trd.SelectedSource = trd.Sources.First(s => s.Column == 2); // Morph Type
			trd.SelectedTarget = trd.Targets.First(t => t.Column == 1); // Citation Form
			trd.SelectedConverter = trd.Converters.First(c => c.Name == "ALT");
			trd.Mode = BulkCopyMode.Replace;
			Dispatcher.UIThread.RunJobs();

			bar.TransducePreviewButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			bar.TransduceApplyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(host.TransducePreviews, Has.Count.EqualTo(1));
			Assert.That(host.TransducePreviews[0].Source, Is.EqualTo(2));
			Assert.That(host.TransducePreviews[0].Target, Is.EqualTo(1));
			Assert.That(host.TransducePreviews[0].Conv.Name, Is.EqualTo("ALT"));
			Assert.That(host.TransducePreviews[0].Mode, Is.EqualTo(BulkCopyMode.Replace));
			Assert.That(host.TransduceApplies, Has.Count.EqualTo(1));
			Assert.That(host.TransduceApplies[0].Target, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void Transduce_ChangingSourceConverterTargetOrMode_ClearsThePendingPreview()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var trd = vm.BulkTransduce;
			var clearAtStart = host.ClearCount;

			trd.SelectedSource = trd.Sources.First(s => s.Column == 2);
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 1), "changing source clears the preview");

			trd.SelectedTarget = trd.Targets.First(t => t.Column == 1);
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 2), "changing target clears the preview");

			trd.SelectedConverter = trd.Converters.First(c => c.Name == "ALT");
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 3), "changing converter clears the preview");

			trd.Mode = BulkCopyMode.DoNothingIfNonEmpty;
			Assert.That(host.ClearCount, Is.EqualTo(clearAtStart + 4), "changing mode clears the preview");
		}

		[AvaloniaTest]
		public void Transduce_Setup_RepublishesTheRefreshedConverterList_PreservingSelectionByName()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var trd = vm.BulkTransduce;
			Assert.That(trd.SelectedConverter?.Name, Is.EqualTo("UPPER"));

			// Setup returns a refreshed list that still contains a converter named "UPPER" plus a new one.
			host.NextSetupConverters = new IBulkTransduceConverter[]
			{
				new FakeBulkEditHost.UpperConverter("UPPER"),
				new FakeBulkEditHost.UpperConverter("NEW")
			};
			trd.Setup();

			Assert.That(host.TransduceSetupCount, Is.EqualTo(1), "Setup launched the dialog once");
			Assert.That(trd.Converters.Select(c => c.Name), Is.EquivalentTo(new[] { "UPPER", "NEW" }),
				"the refreshed list is republished");
			Assert.That(trd.SelectedConverter?.Name, Is.EqualTo("UPPER"),
				"the selection is preserved by name across the refresh");
		}

		[AvaloniaTest]
		public void Transduce_Setup_Cancel_LeavesConverterListUntouched()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 1 };
			Show(host, out var vm);
			var trd = vm.BulkTransduce;

			host.NextSetupConverters = null; // models unavailable / cancel
			trd.Setup();
			Assert.That(trd.Converters.Select(c => c.Name), Is.EquivalentTo(new[] { "UPPER", "ALT" }),
				"a null Setup result leaves the converter list untouched");
		}

		[AvaloniaTest]
		public void Transduce_Apply_NoOp_WhenNotEnabled()
		{
			var host = new FakeBulkEditHost { CheckedRowCount = 0 };
			Show(host, out var vm);

			vm.BulkTransduce.Apply(); // CanApply is false (nothing checked)
			Assert.That(host.TransduceApplies, Is.Empty, "Apply is a no-op while disabled");
		}

		// ----- Click Copy tab -----

		[AvaloniaTest]
		public void ClickCopy_TabRenders_WithTargetPicker_ModeRadios_AndSeparator()
		{
			var host = new FakeBulkEditHost();
			var bar = Show(host, out var vm);
			var cc = vm.ClickCopy;

			Assert.That(bar.ClickCopyTargetCombo, Is.Not.Null);
			Assert.That(bar.ClickCopyWordRadio, Is.Not.Null);
			Assert.That(bar.ClickCopyReorderRadio, Is.Not.Null);
			Assert.That(bar.ClickCopySeparatorBox, Is.Not.Null);
			Assert.That(bar.ClickCopyAppendRadio, Is.Not.Null);
			Assert.That(bar.ClickCopyOverwriteRadio, Is.Not.Null);
			Assert.That(cc.Targets.Select(t => t.Label),
				Is.EquivalentTo(new[] { "Lexeme Form", "Citation Form" }),
				"only the host's eligible writable columns are click-copy targets");
			Assert.That(cc.SelectedTarget?.Column, Is.EqualTo(0), "the first eligible target is seeded");
			Assert.That(cc.Mode, Is.EqualTo(ClickCopyMode.Word), "Word is the default mode");
			Assert.That(cc.Append, Is.True, "Append is the default directivity");
			Assert.That(cc.Separator, Is.EqualTo(" "), "the default separator is a single space");
		}

		[AvaloniaTest]
		public void ClickCopy_IsReady_OnlyWithATargetChosen()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);
			var cc = vm.ClickCopy;

			Assert.That(cc.IsReady, Is.True, "a target is seeded, so a click can copy");
			cc.SelectedTarget = null;
			Assert.That(cc.IsReady, Is.False, "no target chosen → a click copies nothing");
		}

		[AvaloniaTest]
		public void ClickCopy_SelectingTab_ActivatesTheMode_AndRaisesTheEvent()
		{
			var host = new FakeBulkEditHost();
			var bar = Show(host, out var vm);
			var states = new List<bool>();
			bar.ClickCopyActiveChanged += (_, active) => states.Add(active);

			bar.Tabs.SelectedItem = bar.ClickCopyTab;
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.ClickCopy.IsActive, Is.True, "selecting the Click Copy tab activates the mode");
			Assert.That(states, Does.Contain(true), "the active-changed event fired with true");

			bar.Tabs.SelectedIndex = 0; // back to List Choice
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.ClickCopy.IsActive, Is.False, "leaving the tab deactivates the mode");
			Assert.That(states, Does.Contain(false), "the active-changed event fired with false");
		}

		[AvaloniaTest]
		public void ClickCopy_Copy_RoutesToHost_WithSourceTargetRowModeSeparatorAndDirectivity()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);
			var cc = vm.ClickCopy;
			cc.SelectedTarget = cc.Targets.First(t => t.Column == 1); // Citation Form
			cc.Mode = ClickCopyMode.Reorder;
			cc.Separator = "; ";
			cc.Append = false; // overwrite

			// A simulated click on a SOURCE cell at (row 4, column 0), clicked character offset 6.
			cc.Copy(rowIndex: 4, sourceColumn: 0, charOffset: 6);

			Assert.That(host.ClickCopies, Has.Count.EqualTo(1));
			Assert.That(host.ClickCopies[0], Is.EqualTo((0, 1, 4, 6, ClickCopyMode.Reorder, "; ", false)),
				"the clicked character offset threads through to the host so Word/Reorder can resolve the word");
		}

		[AvaloniaTest]
		public void ClickCopy_Copy_NoOp_WhenNoTarget_OrSourceEqualsTarget()
		{
			var host = new FakeBulkEditHost();
			Show(host, out var vm);
			var cc = vm.ClickCopy;

			// Source == target (column 0): a self-copy is rejected.
			cc.SelectedTarget = cc.Targets.First(t => t.Column == 0);
			cc.Copy(rowIndex: 1, sourceColumn: 0);
			Assert.That(host.ClickCopies, Is.Empty, "clicking the target column itself is a no-op");

			// No target chosen: nothing to copy into.
			cc.SelectedTarget = null;
			cc.Copy(rowIndex: 1, sourceColumn: 2);
			Assert.That(host.ClickCopies, Is.Empty, "no target → no copy");
		}

		[AvaloniaTest]
		public void ClickCopy_InactiveTab_LeavesModeInactive()
		{
			// The activation gate lives on the table (it only forwards cell clicks while ClickCopyActive). This
			// asserts the bar/VM side: with the Click Copy tab NOT selected the mode is inactive, so the host
			// wiring would never call Copy. Selecting the tab flips IsActive on so the table starts forwarding.
			// (The table-side click suppression is covered by LexicalBrowseView tests.)
			var host = new FakeBulkEditHost();
			var bar = Show(host, out var vm);

			Assert.That(vm.ClickCopy.IsActive, Is.False, "Click Copy is inactive until its tab is selected");
			Assert.That(host.ClickCopies, Is.Empty, "no copy happened just by rendering the bar");

			bar.Tabs.SelectedItem = bar.ClickCopyTab;
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.ClickCopy.IsActive, Is.True, "selecting the tab activates the mode so clicks forward");
		}
	}
}
