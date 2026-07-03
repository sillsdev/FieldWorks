// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// advanced-entry-view (Configure-Columns P1): the browse Configure-Columns dialog VM is the LCModel-free
	/// two-list editor. Add appends the selected available column (refusing an exact duplicate), Remove drops
	/// the selected shown column but refuses the LAST one, MoveUp/MoveDown reorder the shown list, and OK
	/// snapshots the ordered shown keys into ResultKeys (Cancel leaves it null).
	/// </summary>
	[TestFixture]
	public class ConfigureColumnsDialogTests
	{
		private static IReadOnlyList<ColumnChoiceItem> Catalog() => new[]
		{
			new ColumnChoiceItem("form", "Lexeme Form"),
			new ColumnChoiceItem("gloss", "Gloss"),
			new ColumnChoiceItem("cf", "Citation Form")
		};

		private static ConfigureColumnsDialogViewModel Vm(params string[] shown)
			=> new ConfigureColumnsDialogViewModel(Catalog(), shown);

		[Test]
		public void Add_AppendsSelectedAvailable_AndRefusesDuplicate()
		{
			var vm = Vm("form");
			vm.SelectedAvailable = vm.Available.First(c => c.Key == "gloss");
			vm.AddCommand.Execute(null);
			Assert.That(vm.Shown.Select(s => s.Key), Is.EqualTo(new[] { "form", "gloss" }));

			// Adding the same available column again is refused (already shown).
			vm.AddCommand.Execute(null);
			Assert.That(vm.Shown.Select(s => s.Key), Is.EqualTo(new[] { "form", "gloss" }));
		}

		[Test]
		public void Remove_DropsSelected_ButRefusesTheLast()
		{
			var vm = Vm("form", "gloss");
			vm.SelectedShown = vm.Shown.First(s => s.Key == "form");
			vm.RemoveCommand.Execute(null);
			Assert.That(vm.Shown.Select(s => s.Key), Is.EqualTo(new[] { "gloss" }));

			// Removing the last column is refused (a browse needs at least one).
			vm.SelectedShown = vm.Shown.First();
			vm.RemoveCommand.Execute(null);
			Assert.That(vm.Shown.Count, Is.EqualTo(1));
			Assert.That(vm.CanRemoveSelected, Is.False);
		}

		[Test]
		public void MoveUp_MoveDown_ReorderTheShownList()
		{
			var vm = Vm("form", "gloss", "cf");
			vm.SelectedShown = vm.Shown.First(s => s.Key == "cf");
			vm.MoveUpCommand.Execute(null);
			Assert.That(vm.Shown.Select(s => s.Key), Is.EqualTo(new[] { "form", "cf", "gloss" }));

			vm.SelectedShown = vm.Shown.First(s => s.Key == "form");
			vm.MoveDownCommand.Execute(null);
			Assert.That(vm.Shown.Select(s => s.Key), Is.EqualTo(new[] { "cf", "form", "gloss" }));
		}

		[Test]
		public void Ok_SnapshotsTheOrderedShownKeys()
		{
			var vm = Vm("form", "gloss");
			vm.SelectedShown = vm.Shown.First(s => s.Key == "form");
			vm.MoveDownCommand.Execute(null);

			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ResultKeys, Is.EqualTo(new[] { "gloss", "form" }));
		}

		[Test]
		public void Cancel_NeverSnapshotsResultKeys()
		{
			var vm = Vm("form", "gloss");
			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
			Assert.That(vm.ResultKeys, Is.Null);
		}

		[AvaloniaTest]
		public void View_HostsTheTwoListsAndButtons()
		{
			var vm = Vm("form", "gloss");
			var view = new ConfigureColumnsDialogView { DataContext = vm };
			AvaloniaDialogTestHarness.Realize(view, 560, 380, "ConfigureColumns-01-initial");

			// Add a column from the available list, then snapshot the populated/reordered shown list.
			vm.SelectedAvailable = vm.Available.First(c => c.Key == "cf");
			vm.AddCommand.Execute(null);
			AvaloniaDialogTestHarness.Recapture(view, "ConfigureColumns-02-column-added");

			var ids = view.GetVisualDescendants()
				.Select(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c as Control))
				.Where(id => !string.IsNullOrEmpty(id))
				.ToList();
			Assert.That(ids, Does.Contain("ConfigureColumns.Available"));
			Assert.That(ids, Does.Contain("ConfigureColumns.Shown"));
			Assert.That(ids, Does.Contain("ConfigureColumns.Add"));
			Assert.That(ids, Does.Contain("ConfigureColumns.Remove"));
			Assert.That(ids, Does.Contain("ConfigureColumns.MoveUp"));
			Assert.That(ids, Does.Contain("ConfigureColumns.MoveDown"));
			Assert.That(ids, Does.Contain("ConfigureColumns.Ok"));
		}

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.ConfigureColumnsTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.ConfigureColumnsAdd, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.ConfigureColumnsNeedsAColumn, Is.Not.Null.And.Not.Empty);
		}
	}
}
