// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.1) — an LCModel-free description of a cell to insert/replace in a
	/// rule formula: its <see cref="RuleCellKind"/> plus the GUID of the target object (phoneme / natural
	/// class / boundary) the cell references. The view supplies this (its per-cell chooser, task 2.2, picks
	/// the GUID); the xWorks/Morphology handler resolves the GUID to the real LCModel object and builds the
	/// matching <c>IPhSimpleContext</c> (design Decision 1: the view never touches LCModel).
	/// </summary>
	public sealed class RuleCellSpec
	{
		public RuleCellSpec(RuleCellKind kind, Guid? targetGuid)
		{
			Kind = kind;
			TargetGuid = targetGuid;
		}

		/// <summary>What kind of cell to create.</summary>
		public RuleCellKind Kind { get; }

		/// <summary>The referenced object's GUID (phoneme/natural-class/boundary). Required for the reference
		/// kinds; a pure slot/variable carries none.</summary>
		public Guid? TargetGuid { get; }

		/// <summary>
		/// Encode this spec as an <c>FwOptionPicker</c> option key — a kind prefix + the target GUID
		/// (<c>"P:&lt;guid&gt;"</c> phoneme, <c>"N:"</c> natural class, <c>"B:"</c> boundary) — so the picked
		/// option round-trips back to a spec the handler can apply. The xWorks options projection builds the
		/// same keys; the view decodes the committed key with <see cref="FromOptionKey"/>.
		/// </summary>
		public string ToOptionKey() => Prefix(Kind) + ":" + (TargetGuid?.ToString() ?? string.Empty);

		/// <summary>Decode an option key built by <see cref="ToOptionKey"/> back into a spec, or null if the
		/// key is malformed / an unknown kind / (for a reference kind) carries no parseable GUID.</summary>
		public static RuleCellSpec FromOptionKey(string key)
		{
			if (string.IsNullOrEmpty(key) || key.Length < 2 || key[1] != ':')
				return null;
			var rest = key.Substring(2);
			Guid? guid = Guid.TryParse(rest, out var g) ? g : (Guid?)null;
			switch (key[0])
			{
				case 'P': return guid == null ? null : new RuleCellSpec(RuleCellKind.Phoneme, guid);
				case 'N': return guid == null ? null : new RuleCellSpec(RuleCellKind.NaturalClass, guid);
				case 'B': return guid == null ? null : new RuleCellSpec(RuleCellKind.Boundary, guid);
				default: return null;
			}
		}

		private static string Prefix(RuleCellKind kind)
		{
			switch (kind)
			{
				case RuleCellKind.Phoneme: return "P";
				case RuleCellKind.NaturalClass: return "N";
				case RuleCellKind.Boundary: return "B";
				default: return "S";
			}
		}
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 2.1) — the editor's cell-mutation seam. The Avalonia
	/// <see cref="RuleFormulaRegionEditor"/> raises these intents (insert at / delete / reorder / replace a
	/// cell in a named section); the xWorks/Morphology handler applies them to the rule inside the region's
	/// fenced edit session as ONE undoable step per gesture, then re-projects so the grid re-renders from
	/// domain truth. This interface is LCModel-free so the view stays engine-isolated; each method returns
	/// false (and stages nothing) when the gesture is invalid or unsupported for the section.
	/// </summary>
	public interface IRuleCellCommandSink
	{
		/// <summary>Insert a new cell built from <paramref name="spec"/> at <paramref name="index"/> in the
		/// section playing <paramref name="role"/>.</summary>
		bool InsertCell(RuleSectionRole role, int index, RuleCellSpec spec);

		/// <summary>Delete the cell at <paramref name="index"/> in the section playing <paramref name="role"/>.</summary>
		bool DeleteCell(RuleSectionRole role, int index);

		/// <summary>Move the cell at <paramref name="from"/> to <paramref name="to"/> within the section.</summary>
		bool MoveCell(RuleSectionRole role, int from, int to);

		/// <summary>Replace the cell at <paramref name="index"/> with one built from <paramref name="spec"/>.</summary>
		bool SetCell(RuleSectionRole role, int index, RuleCellSpec spec);
	}
}
