// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The ONE LCModel-backed <see cref="IRegionEditContext"/> session/validation
	/// implementation: owns the lazily opened fenced <see cref="LcmRegionEditSession"/> (opened
	/// on the first staged edit, committed/cancelled as one global undo step) and the shared
	/// required-lexeme validation rule, so the first-slice context and the full-entry composed
	/// context cannot drift apart. Derived contexts supply only the field write routing.
	/// </summary>
	public abstract class RegionEditContextBase : IRegionEditContext
	{
		private LcmRegionEditSession _session;

		protected RegionEditContextBase(LcmCache cache, ICmObject root)
		{
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
			RootObject = root ?? throw new ArgumentNullException(nameof(root));
		}

		protected LcmCache Cache { get; }

		/// <summary>The record this context edits — a LexEntry for the lexicon tool, but typed as any
		/// <see cref="ICmObject"/>. Derived contexts route writes against it.</summary>
		protected ICmObject RootObject { get; }

		/// <summary>Convenience for the lexicon path: the root typed as <see cref="ILexEntry"/>, or null
		/// when the root is some other class. Entry-specific logic (e.g. the lexeme-form validation rule)
		/// guards on this rather than assuming the root is an entry.</summary>
		protected ILexEntry Entry => RootObject as ILexEntry;

		/// <inheritdoc />
		public bool IsOpen => _session != null && _session.IsOpen;

		/// <inheritdoc />
		public abstract bool TrySetText(LexicalEditRegionField regionField, string ws, string value);

		/// <inheritdoc />
		public virtual bool TrySetRichText(LexicalEditRegionField regionField, string ws,
			RegionRichTextValue value) => false;

		/// <inheritdoc />
		public abstract bool TrySetOption(LexicalEditRegionField regionField, string optionKey);

		/// <inheritdoc />
		/// <remarks>Reference-vector editing exists only on composed regions; the first-slice
		/// fallback has no vector rows, so the base rejects.</remarks>
		public virtual bool TryAddReferenceItem(LexicalEditRegionField regionField, string optionKey) => false;

		/// <inheritdoc />
		public virtual bool TryRemoveReferenceItem(LexicalEditRegionField regionField, string optionKey) => false;

		/// <inheritdoc />
		public virtual IReadOnlyList<string> Validate()
		{
			// Validation seam (minimal rule set, deterministic order). The lexeme/citation-form rule
			// applies ONLY when the root is a LexEntry; other record classes (RnGenericRec, CmPossibility, …)
			// have no shared required-field rule here and may override this for their own (e.g. CmPossibility
			// Name/Abbreviation). A non-entry root therefore validates clean by default rather than NRE-ing on
			// Entry.LexemeFormOA.
			var errors = new List<string>();
			var entry = Entry;
			if (entry != null)
			{
				var lexeme = entry.LexemeFormOA?.Form?.VernacularDefaultWritingSystem?.Text;
				if (string.IsNullOrEmpty(lexeme))
					lexeme = entry.CitationForm.VernacularDefaultWritingSystem?.Text;
				if (string.IsNullOrWhiteSpace(lexeme))
					errors.Add(FwAvaloniaStrings.LexemeFormRequired);
			}
			else if (RootObject is ICmPossibility poss)
			{
				// A list item must keep some Name or Abbreviation text — the
				// parity of the legacy Lists editor's required-field guard.
				var name = poss.Name?.BestAnalysisAlternative?.Text;
				var abbr = poss.Abbreviation?.BestAnalysisAlternative?.Text;
				if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(abbr))
					errors.Add(FwAvaloniaStrings.PossibilityNameOrAbbreviationRequired);
			}
			return errors;
		}

		/// <inheritdoc />
		public void Commit()
		{
			_session?.Commit();
		}

		/// <inheritdoc />
		public void Cancel()
		{
			_session?.Cancel();
		}

		/// <summary>
		/// Opens the fenced session on the first staged edit; later calls are no-ops. The generic
		/// "Undo/Redo Edit Entry" label is used — the batch/bulk path that stages several fields with no
		/// single field to name. Single-field edits go through <see cref="EnsureOpen(string)"/> so the
		/// undo label names the field, mirroring the legacy per-slice "Undo change to {field}" labels.
		/// </summary>
		/// <summary>The generic undo/redo labels used when an edit opens the session without a single
		/// field to name. Defaults to the lexicon "Edit Entry" labels; a
		/// non-entry context overrides these so a Notebook/List/Grammar edit names its own record.</summary>
		protected virtual string DefaultUndoLabel => FwAvaloniaStrings.UndoEditEntry;
		protected virtual string DefaultRedoLabel => FwAvaloniaStrings.RedoEditEntry;

		protected void EnsureOpen()
		{
			EnsureOpen(null);
		}

		/// <summary>
		/// Opens the fenced session on the first staged edit, naming the field being edited in the
		/// undo/redo label when <paramref name="fieldLabel"/> is supplied (field-specific
		/// labels, e.g. "Undo change to Gloss"), falling back to the generic label otherwise. Because
		/// the session opens lazily on the FIRST staged edit, the field that opens it names the label;
		/// later same-session edits do not relabel it (the legacy single-step-per-gesture behavior).
		/// Later calls while a session is already open are no-ops (the label is fixed at open).
		/// </summary>
		protected void EnsureOpen(string fieldLabel)
		{
			if (IsOpen)
				return;
			string undoLabel, redoLabel;
			if (string.IsNullOrEmpty(fieldLabel))
			{
				undoLabel = DefaultUndoLabel;
				redoLabel = DefaultRedoLabel;
			}
			else
			{
				undoLabel = string.Format(CultureInfo.CurrentCulture,
					FwAvaloniaStrings.UndoChangeToFormat, fieldLabel);
				redoLabel = string.Format(CultureInfo.CurrentCulture,
					FwAvaloniaStrings.RedoChangeToFormat, fieldLabel);
			}
			_session = new LcmRegionEditSession(Cache, undoLabel, redoLabel);
		}

		/// <summary>
		/// Stages an arbitrary domain write inside THIS context's fenced session (opening it on the first
		/// edit), so a write routed from outside the registered setter dictionaries — e.g. a plugin
		/// editor's own field (the Reversal Entries plugin) — still rides the SAME undoable step as every
		/// other row in the region. A rejected write that opened the session here closes it again so an
		/// empty fence never strands the UOW write lock. A throwing setter rolls back a session this call
		/// opened, then rethrows.
		/// </summary>
		public bool Stage(Func<bool> setter)
		{
			return Stage(setter, null);
		}

		/// <summary>
		/// As <see cref="Stage(Func{bool})"/>, but names <paramref name="fieldLabel"/> in the undo/redo
		/// label when THIS call opens the session (field-specific labels). A write that joins an
		/// already-open session leaves that session's label untouched.
		/// </summary>
		public bool Stage(Func<bool> setter, string fieldLabel)
		{
			if (setter == null)
				return false;
			var wasOpen = IsOpen;
			EnsureOpen(fieldLabel);
			try
			{
				var staged = setter();
				if (!staged && !wasOpen)
					Cancel();
				return staged;
			}
			catch
			{
				if (!wasOpen)
					Cancel();
				throw;
			}
		}
	}
}
