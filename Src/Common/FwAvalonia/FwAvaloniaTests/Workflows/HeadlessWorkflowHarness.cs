// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests.Workflows
{
	/// <summary>
	/// Reusable headless-integration scaffolding for driving migrated Avalonia surfaces through real
	/// USER SCENARIOS and WORKFLOWS — the front-and-center test style for the whole WinForms→Avalonia
	/// program (all 13 phases), not just the browse table. The pieces:
	///
	///  • <see cref="HeadlessStage"/> — hosts one or more Avalonia controls in a headless window and
	///    pumps the dispatcher, so a test reads/acts on a realized visual tree exactly as a user would.
	///  • <see cref="BrowseTableDriver"/> / <see cref="LexicalEditorDriver"/> — page-object "drivers"
	///    that expose intent-level verbs (filter, clear, select, read cell, type, commit) over a hosted
	///    surface, so scenario tests read like a script and stay stable as the control internals change.
	///
	/// New surfaces (grid/tree, choosers, interlinear, dialogs) add a driver here and reuse the stage.
	/// Tests that need the REAL domain (LCModel clerk narrowing/sort/undo) use the parallel domain
	/// harness in xWorksTests; these drivers work over whichever row source / model they are handed
	/// (a real production adapter or an in-memory scenario store), so the same scenario script can run
	/// at either fidelity.
	/// </summary>
	public sealed class HeadlessStage
	{
		private readonly Window _window;

		private HeadlessStage(Control content)
		{
			_window = new Window { Width = 800, Height = 480, Content = content };
			_window.Show();
			Pump();
		}

		/// <summary>Hosts a single surface and returns the stage.</summary>
		public static HeadlessStage Show(Control surface) => new HeadlessStage(surface);

		/// <summary>Hosts two surfaces side by side (e.g. a list and its detail editor) in one window.</summary>
		public static HeadlessStage ShowSideBySide(Control left, Control right)
		{
			var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
			Grid.SetColumn(left, 0);
			Grid.SetColumn(right, 1);
			grid.Children.Add(left);
			grid.Children.Add(right);
			return new HeadlessStage(grid);
		}

		/// <summary>Runs all queued UI-thread work — call after any action that schedules layout/handlers.</summary>
		public void Pump() => Dispatcher.UIThread.RunJobs();

		internal static string ReadText(TextBlock block)
			=> block == null ? null
				: !string.IsNullOrEmpty(block.Text) ? block.Text
				: string.Concat(block.Inlines?.OfType<Run>().Select(r => r.Text) ?? Enumerable.Empty<string>());
	}

	/// <summary>
	/// Scenario driver for the owned browse table (<see cref="LexicalBrowseView"/>): the verbs a user
	/// performs — read row count, read a cell, select/sort/filter — without reaching into the control's
	/// visual tree from the test. Pumps the dispatcher after each acting verb so the visual tree is
	/// settled before the next assertion.
	/// </summary>
	public sealed class BrowseTableDriver
	{
		private readonly LexicalBrowseView _view;
		private readonly HeadlessStage _stage;

		public BrowseTableDriver(LexicalBrowseView view, HeadlessStage stage)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_stage = stage;
		}

		public LexicalBrowseView View => _view;

		/// <summary>The number of rows currently shown (after any active filter).</summary>
		public int RowCount => _view.RowList.ItemCount;

		public int SelectedRow => _view.SelectedRowIndex;

		/// <summary>Reads the realized cell text at (row, column); null when the row is not realized.</summary>
		public string CellText(int row, int column) => HeadlessStage.ReadText(
			_view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == $"BrowseCell.{row}.{column}"));

		public void SelectRow(int row) { _view.SelectedRowIndex = row; _stage?.Pump(); }

		public void Filter(int column, string text) { _view.ApplyFilter(column, text); _stage?.Pump(); }

		public void ClearFilter(int column) { _view.ApplyFilter(column, string.Empty); _stage?.Pump(); }

		public void FilterPreset(int column, BrowseFilterPreset preset)
		{ _view.ApplyFilterPreset(column, preset); _stage?.Pump(); }

		public void Sort(int column) { _view.SortByColumn(column); _stage?.Pump(); }

		public void CheckAll() { _view.CheckAll(); _stage?.Pump(); }

		public IReadOnlyCollection<int> CheckedRows => _view.CheckedRows;

		/// <summary>Re-reads rows from the source (what the host does after a model change).</summary>
		public void Refresh() { _view.Refresh(); _stage?.Pump(); }
	}

	/// <summary>
	/// Scenario driver for the lexical-edit (detail) surface (<see cref="LexicalEditRegionView"/>):
	/// reading and typing field values. Field editors stamp the automation id
	/// <c>{fieldAutomationId}.{ws}</c> (per writing system), so the driver locates a field's editor by
	/// its automation-id prefix.
	/// </summary>
	public sealed class LexicalEditorDriver
	{
		private readonly LexicalEditRegionView _view;
		private readonly HeadlessStage _stage;

		public LexicalEditorDriver(LexicalEditRegionView view, HeadlessStage stage)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_stage = stage;
		}

		public LexicalEditRegionView View => _view;

		private TextBox Editor(string fieldAutomationId) => _view.GetVisualDescendants().OfType<TextBox>()
			.FirstOrDefault(b => (AutomationProperties.GetAutomationId(b) ?? string.Empty)
				.StartsWith(fieldAutomationId + ".", StringComparison.Ordinal));

		/// <summary>The displayed text of a field's (first writing-system) editor.</summary>
		public string FieldText(string fieldAutomationId) => Editor(fieldAutomationId)?.Text;

		/// <summary>Types a value into a field's editor (drives the editor's own edit-context staging).</summary>
		public void Type(string fieldAutomationId, string value)
		{
			var box = Editor(fieldAutomationId);
			if (box != null)
			{
				box.Text = value;
				_stage?.Pump();
			}
		}
	}
}
