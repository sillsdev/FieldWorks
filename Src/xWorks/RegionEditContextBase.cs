// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The ONE LCModel-backed <see cref="IRegionEditContext"/> session/validation implementation
	/// (review finding C): owns the lazily opened fenced <see cref="LcmRegionEditSession"/> (opened
	/// on the first staged edit, committed/cancelled as one global undo step) and the shared
	/// required-lexeme validation rule, so the first-slice context and the full-entry composed
	/// context cannot drift apart. Derived contexts supply only the field write routing.
	/// </summary>
	public abstract class RegionEditContextBase : IRegionEditContext
	{
		private LcmRegionEditSession _session;

		protected RegionEditContextBase(LcmCache cache, ILexEntry entry)
		{
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
			Entry = entry ?? throw new ArgumentNullException(nameof(entry));
		}

		protected LcmCache Cache { get; }

		protected ILexEntry Entry { get; }

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
		/// <remarks>Reference-vector editing (6.3) exists only on composed regions; the first-slice
		/// fallback has no vector rows, so the base rejects.</remarks>
		public virtual bool TryAddReferenceItem(LexicalEditRegionField regionField, string optionKey) => false;

		/// <inheritdoc />
		public virtual bool TryRemoveReferenceItem(LexicalEditRegionField regionField, string optionKey) => false;

		/// <inheritdoc />
		public IReadOnlyList<string> Validate()
		{
			// Validation seam (minimal rule set, deterministic order): an entry must keep some
			// lexeme/citation form text.
			var errors = new List<string>();
			var lexeme = Entry.LexemeFormOA?.Form?.VernacularDefaultWritingSystem?.Text;
			if (string.IsNullOrEmpty(lexeme))
				lexeme = Entry.CitationForm.VernacularDefaultWritingSystem?.Text;
			if (string.IsNullOrWhiteSpace(lexeme))
				errors.Add(FwAvaloniaStrings.LexemeFormRequired);
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

		/// <summary>Opens the fenced session on the first staged edit; later calls are no-ops.</summary>
		protected void EnsureOpen()
		{
			if (IsOpen)
				return;
			_session = new LcmRegionEditSession(Cache, FwAvaloniaStrings.UndoEditEntry, FwAvaloniaStrings.RedoEditEntry);
		}
	}
}
