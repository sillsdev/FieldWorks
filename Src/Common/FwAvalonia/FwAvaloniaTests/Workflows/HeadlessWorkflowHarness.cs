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
	/// program (all 13 phases). The pieces:
	///
	///  • <see cref="HeadlessStage"/> — hosts one or more Avalonia controls in a headless window and
	///    pumps the dispatcher, so a test reads/acts on a realized visual tree exactly as a user would.
	///  • <see cref="LexicalEditorDriver"/> — a page-object "driver"
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
