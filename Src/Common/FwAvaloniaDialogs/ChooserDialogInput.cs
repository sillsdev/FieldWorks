// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>How the chooser selects: a single item (atomic field) or several (reference vector).</summary>
	public enum ChooserSelectionMode
	{
		/// <summary>One choice (the legacy single-select ReallySimpleListChooser): Enter/click commits one item.</summary>
		Single,

		/// <summary>Many choices (the legacy multi-check chooser): rows carry checkboxes and the checked set is returned.</summary>
		Multi
	}

	/// <summary>
	/// The LCModel-free input to the reusable Avalonia chooser dialog — the Phase 1 (flat list) replacement for
	/// the legacy <c>ReallySimpleListChooser</c>/<c>SimpleListChooser</c>. The product edge (the LexText launcher)
	/// builds this from a possibility list / reference target set so the Avalonia layer never sees an
	/// <c>ICmObject</c>: candidates are plain <see cref="RegionChoiceOption"/>s (key = guid string, name = display
	/// text, optional <see cref="RegionChoiceOption.Depth"/> rendered as a FLAT indented list — NOT a tree in
	/// Phase 1), and the current selection / result are exchanged as guid-string keys.
	/// </summary>
	public sealed class ChooserDialogInput
	{
		/// <summary>The selectable options (flat; <see cref="RegionChoiceOption.Depth"/> renders as indentation).</summary>
		public IReadOnlyList<RegionChoiceOption> Candidates { get; set; } = Array.Empty<RegionChoiceOption>();

		/// <summary>Single- vs multi-select. Drives whether the embedded picker shows checkboxes + an Add button.</summary>
		public ChooserSelectionMode SelectionMode { get; set; } = ChooserSelectionMode.Single;

		/// <summary>Keys (guid strings) selected on open; the empty string selects the <see cref="EmptyKey"/> row.</summary>
		public IReadOnlyList<string> InitialSelectedKeys { get; set; } = Array.Empty<string>();

		/// <summary>
		/// When true an "&lt;Empty&gt;" option is led in front of the candidates so the user can atomically clear an
		/// atomic field (the legacy chooser's "&lt;Empty&gt;" / "&lt;Not Sure&gt;" row). Its key is <see cref="EmptyKey"/>
		/// (the empty string) so the result reports a clear distinctly from "no choice made".
		/// </summary>
		public bool AllowEmpty { get; set; }

		/// <summary>
		/// Optional server/lexicon-style search delegate forwarded to the embedded picker: given the typed query it
		/// returns the candidate set to show. Null means the picker filters <see cref="Candidates"/> in memory
		/// (case-insensitive contains).
		/// </summary>
		public Func<string, IReadOnlyList<RegionChoiceOption>> SearchCandidates { get; set; }

		/// <summary>
		/// When true the candidates are presented as a COLLAPSIBLE TREE (Phase 2) built from the
		/// <see cref="RegionChoiceOption.Depth"/> sequence — a candidate's children are the following candidates with
		/// Depth+1 until Depth drops back (possibility lists arrive in document order with Depth, which fully
		/// determines the tree). When false (the default) the candidates render as the Phase 1 FLAT indented list
		/// (the shared <c>FwOptionPicker</c>), unchanged. When a search term is active the hierarchical view falls
		/// back to the flat filtered results list and returns to the tree once the term is cleared.
		/// </summary>
		public bool Hierarchical { get; set; }

		/// <summary>The prompt shown above the list (localized by the caller); null/empty hides the prompt.</summary>
		public string Prompt { get; set; }

		/// <summary>The help topic id for the dialog's Help button (null/empty hides Help). Phase 1 carries it only.</summary>
		public string HelpTopic { get; set; }

		/// <summary>
		/// When true OK is gated off until at least one item is chosen (the legacy "you must pick something" chooser).
		/// When false an empty selection is a valid OK (e.g. an atomic field that <see cref="AllowEmpty"/> may clear).
		/// </summary>
		public bool ForbidEmptySelection { get; set; }

		/// <summary>The reserved key for the leading "&lt;Empty&gt;" option: the empty string, distinct from any guid.</summary>
		public const string EmptyKey = "";
	}
}
