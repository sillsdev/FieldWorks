// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free input to the reusable Avalonia entry-search ("go") dialog — the kit replacement for the
	/// legacy <c>EntryGoDlg</c> FAMILY (its first concrete consumer is Merge Entry; the same dialog later re-skins
	/// for AddAllomorph / LinkEntryOrSense / LinkAllomorph / LinkMSA with only title/button/prompt/filter
	/// differences). The product edge (the LexText launcher) builds this from the live entry repository so the
	/// Avalonia layer never sees an <c>ICmObject</c>: it supplies a <see cref="Search"/> delegate (the SAME matching
	/// the legacy <c>EntryGoSearchEngine</c> uses, with the excluded entry already filtered out), the configurable
	/// title / OK-button / prompt / description-label text so each consumer can re-skin it, and the id to exclude
	/// (the current entry) is enforced by the launcher's search rather than re-checked here.
	/// </summary>
	public sealed class EntryGoDialogInput
	{
		/// <summary>
		/// The search delegate: given the typed query it returns the matching result rows (already excluding the
		/// current entry's <see cref="ExcludedId"/>). An empty/whitespace query may return an unfiltered/initial set
		/// (mirrors the legacy dialog priming the list with the starting form). Null means an always-empty list.
		/// </summary>
		public Func<string, IReadOnlyList<EntryGoSearchResult>> Search { get; set; }

		/// <summary>The id (legacy hvo string) of the current entry that must never appear as a match (parity with
		/// the legacy "exclude the starting entry" rule). Carried for the launcher's filter + defensive in-VM guard.</summary>
		public string ExcludedId { get; set; }

		/// <summary>The query the search box starts with (the legacy "launch with the current headword"); null = empty.</summary>
		public string InitialQuery { get; set; }

		/// <summary>The dialog title (localized by the launcher, e.g. "Merge Entry").</summary>
		public string Title { get; set; }

		/// <summary>The OK button text (localized by the launcher, e.g. "Merge"); null = the shared OK label.</summary>
		public string OkButtonText { get; set; }

		/// <summary>The prompt shown above the search box (localized); null/empty hides it.</summary>
		public string SearchPrompt { get; set; }

		/// <summary>The label shown above the description/preview pane (localized); null/empty hides the label.</summary>
		public string DescriptionLabel { get; set; }

		/// <summary>The help topic id for the Help button (null/empty hides Help). The launcher wires the goto.</summary>
		public string HelpTopic { get; set; }

		// ----- Opt-in entry/sense capability (the legacy LinkEntryOrSenseDlg Entry/Sense radio). Entry-only
		// consumers (Merge, AddAllomorph, LinkAllomorph, LinkMSA) leave these defaults and are unaffected. -----

		/// <summary>
		/// When true the dialog shows the Entry/Sense mode toggle (the legacy "Entry" / "Specific Sense" radios)
		/// and drives <see cref="SearchByMode"/> with the current mode. Defaults to false (entry-only, no toggle),
		/// so existing consumers keep a single search box returning entries.
		/// </summary>
		public bool ShowEntrySenseToggle { get; set; }

		/// <summary>
		/// When true the dialog starts (and, when <see cref="ShowEntrySenseToggle"/> is false, stays) in SENSE
		/// mode — the legacy <c>SelectSensesOnly</c> where the toggle is forced to "Specific Sense" and disabled.
		/// Combine with <see cref="ShowEntrySenseToggle"/> = false to lock the dialog to senses only.
		/// </summary>
		public bool SensesOnly { get; set; }

		/// <summary>
		/// The mode-aware search delegate used when <see cref="ShowEntrySenseToggle"/> or <see cref="SensesOnly"/>
		/// is set: given the typed query and whether sense mode is active, it returns entry rows (sense mode false)
		/// or sense rows (sense mode true). When null the dialog falls back to <see cref="Search"/> (entry-only).
		/// </summary>
		public Func<string, bool, IReadOnlyList<EntryGoSearchResult>> SearchByMode { get; set; }
	}
}
