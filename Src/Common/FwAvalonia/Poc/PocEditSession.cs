// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// A POC stand-in for the fenced LCModel edit session (see avalonia-edit-sessions). It captures
	/// the entry's editable values on construction; Commit keeps the edits, Cancel restores the
	/// captured snapshot. The product implementation will fence a real LCModel undo task; the POC
	/// proves the commit/cancel boundary semantics that the editors must drive.
	/// </summary>
	public sealed class PocEditSession : IEditSession
	{
		private readonly PocEntryDto _entry;
		private readonly Dictionary<WsAlternative, string> _originalText = new Dictionary<WsAlternative, string>();
		private readonly string _originalMorphTypeKey;
		private bool _closed;

		public PocEditSession(PocEntryDto entry)
		{
			_entry = entry;
			_originalMorphTypeKey = entry.MorphTypeKey;
			Snapshot(entry.LexemeForm);
			Snapshot(entry.SenseGloss);
		}

		private void Snapshot(IEnumerable<WsAlternative> alternatives)
		{
			foreach (var alt in alternatives)
			{
				_originalText[alt] = alt.Value;
			}
		}

		/// <summary>Whether the session is still open for edits.</summary>
		public bool IsOpen => !_closed;

		/// <summary>Commits the edits (the values already live on the DTO) and closes the session.</summary>
		public void Commit()
		{
			_closed = true;
		}

		/// <summary>Restores the captured snapshot and closes the session.</summary>
		public void Cancel()
		{
			foreach (var pair in _originalText)
			{
				pair.Key.Value = pair.Value;
			}

			_entry.MorphTypeKey = _originalMorphTypeKey;
			_closed = true;
		}
	}
}
