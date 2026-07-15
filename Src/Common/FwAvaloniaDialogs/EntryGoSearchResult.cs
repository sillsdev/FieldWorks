// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// A single lightweight, LCModel-free result row for the reusable Avalonia entry-search ("go") dialog — the
	/// kit replacement for one row of the legacy <c>EntryGoDlg</c>/<c>BaseGoDlg</c> matching-entries browser. The
	/// product edge (the LexText launcher) maps a matched <c>ILexEntry</c> into this row, so the Avalonia layer
	/// never sees an <c>ICmObject</c>: <see cref="Id"/> is the entry's stable identity (hvo string, the legacy
	/// <c>FwObjectSelectionEventArgs.Hvo</c>), <see cref="Text"/> is the headword/display the list shows, and
	/// <see cref="Description"/> is the longer description the preview/description pane shows for the selected row.
	/// </summary>
	public sealed class EntryGoSearchResult
	{
		public EntryGoSearchResult(string id, string text, string description = null)
		{
			Id = id;
			Text = text;
			Description = description ?? string.Empty;
			IsSense = false;
			SubText = string.Empty;
			DescriptionContent = null;
		}

		/// <summary>
		/// Builds a result row that may represent either an entry or a sense (the opt-in Link-Entry-or-Sense
		/// capability). <paramref name="isSense"/> marks a sense row (the dialog returns its <paramref name="id"/>
		/// as a sense id); <paramref name="subText"/> is the secondary line shown under the headword (e.g. the
		/// sense gloss) so a sense row reads "headword / gloss" without changing the entry-only rows. The
		/// entry-only constructor above leaves <see cref="IsSense"/> false and <see cref="SubText"/> empty, so
		/// existing consumers (Merge, AddAllomorph, LinkAllomorph, LinkMSA) are unaffected.
		/// </summary>
		public EntryGoSearchResult(string id, string text, bool isSense, string subText = null, string description = null)
		{
			Id = id;
			Text = text;
			Description = description ?? string.Empty;
			IsSense = isSense;
			SubText = subText ?? string.Empty;
			DescriptionContent = null;
		}

		/// <summary>
		/// Builds a result row that carries a RICH extended-description payload for the right-side description
		/// region (the advanced entry view): <paramref name="descriptionContent"/> is an arbitrary content object —
		/// an Avalonia <c>Control</c> (formatted text, a picture, a composite preview) or any object the right
		/// region's <c>ContentControl</c> can present — shown for the highlighted row INSTEAD of the plain
		/// <paramref name="description"/> string. <paramref name="description"/> is still supplied as the
		/// plain-text fallback (and accessible name) so a consumer that later drops the rich payload, or a host
		/// that cannot realize it, degrades gracefully to the one-line text. The entry-only and entry/sense
		/// constructors above leave <see cref="DescriptionContent"/> null, so those rows keep showing the plain
		/// string and existing consumers are unaffected.
		/// </summary>
		public EntryGoSearchResult(string id, string text, object descriptionContent, string description = null,
			bool isSense = false, string subText = null)
		{
			Id = id;
			Text = text;
			Description = description ?? string.Empty;
			IsSense = isSense;
			SubText = subText ?? string.Empty;
			DescriptionContent = descriptionContent;
		}

		/// <summary>The result's stable identity (the legacy hvo as a string); the dialog returns this on OK.</summary>
		public string Id { get; }

		/// <summary>The display text shown in the results list (the entry's headword).</summary>
		public string Text { get; }

		/// <summary>The longer description shown in the description/preview pane when this row is selected.</summary>
		public string Description { get; }

		/// <summary>
		/// An optional RICH extended-description payload for the right-side description region — an Avalonia
		/// <c>Control</c> (formatted text, a picture, a composite preview) or any object a <c>ContentControl</c> can
		/// present. When non-null the right region shows this for the highlighted row; when null it falls back to the
		/// plain <see cref="Description"/> string (see <see cref="HasDescriptionContent"/>). Kept as a loosely-typed
		/// <c>object</c> so the kit stays UI-toolkit-light and LCModel-free: consumers that only have a headword +
		/// plain gloss leave it null and the region degrades to text.
		/// </summary>
		public object DescriptionContent { get; }

		/// <summary>True when this row carries a rich <see cref="DescriptionContent"/> payload (vs. plain text only).</summary>
		public bool HasDescriptionContent => DescriptionContent != null;

		/// <summary>
		/// True when this row represents a SENSE rather than an entry (the legacy LinkEntryOrSenseDlg "Specific
		/// Sense" path). The launcher reads this on OK to resolve the chosen id as a sense. Defaults to false so
		/// entry-only consumers keep returning entries.
		/// </summary>
		public bool IsSense { get; }

		/// <summary>
		/// A secondary display line shown under <see cref="Text"/> (the sense gloss for a sense row); empty for an
		/// entry row, where only the headword shows. See <see cref="HasSubText"/>.
		/// </summary>
		public string SubText { get; }

		/// <summary>True when there is a non-empty <see cref="SubText"/> (the gloss line) to show.</summary>
		public bool HasSubText => !string.IsNullOrEmpty(SubText);

		/// <summary>The results list binds to <see cref="Text"/>; ToString keeps simple list rendering correct too.</summary>
		public override string ToString() => Text ?? string.Empty;
	}
}
