// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The product <see cref="IDetailEditContext"/> for the LexEntry first slice (tasks 6.8/6.10):
	/// stages writes directly into LCModel inside a lazily opened <see cref="LcmDetailEditSession"/>
	/// (fenced undo task, owned by <see cref="DetailEditContextBase"/>), validates required fields,
	/// and commits/cancels the fence. Field names match the compiled first-slice definition
	/// (`Form`/`Gloss`/`MorphType`). Detached DTO editing remains preview-only; this context is the
	/// real domain write path.
	/// </summary>
	public sealed class LexiconFirstSliceEditContext : DetailEditContextBase
	{
		public LexiconFirstSliceEditContext(ILexEntry entry, LcmCache cache)
			: base(cache, entry)
		{
		}

		/// <inheritdoc />
		public override bool TrySetText(DetailField detailField, string ws, string value)
		{
			if (detailField != null && detailField.Values.Any(v => v.RequiresRichEditor))
				return false;

			switch (detailField?.Field)
			{
				case "Form":
				{
					if (!TryResolveWsHandle(ws, vernacular: true, out var wsHandle))
						return false;
					// ITEM 1: name the field in the undo label (e.g. "Undo change to Lexeme Form").
					EnsureOpen(FieldLabel(detailField));
					var text = TsStringUtils.MakeString(value ?? string.Empty, wsHandle);
					// Mirror the read fallback: entries without a lexeme form object edit the citation form.
					if (Entry.LexemeFormOA != null)
						Entry.LexemeFormOA.Form.set_String(wsHandle, text);
					else
						Entry.CitationForm.set_String(wsHandle, text);
					return true;
				}
				case "Gloss":
				{
					if (Entry.SensesOS.Count == 0)
						return false;
					if (!TryResolveWsHandle(ws, vernacular: false, out var wsHandle))
						return false;
					EnsureOpen(FieldLabel(detailField));
					Entry.SensesOS[0].Gloss.set_String(wsHandle, TsStringUtils.MakeString(value ?? string.Empty, wsHandle));
					return true;
				}
				default:
					return false;
			}
		}

		public override bool TrySetRichText(DetailField detailField, string ws,
			DetailRichTextValue value)
		{
			if (detailField != null && detailField.Values.Any(v => !v.CanEditRichText))
				return false;

			switch (detailField?.Field)
			{
				case "Form":
				{
					if (!TryResolveWsHandle(ws, vernacular: true, out var wsHandle))
						return false;
					EnsureOpen(FieldLabel(detailField));
					var text = DetailRichTextAdapter.ToTsString(value, Cache.WritingSystemFactory, wsHandle);
					if (Entry.LexemeFormOA != null)
						Entry.LexemeFormOA.Form.set_String(wsHandle, text);
					else
						Entry.CitationForm.set_String(wsHandle, text);
					return true;
				}
				case "Gloss":
				{
					if (Entry.SensesOS.Count == 0)
						return false;
					if (!TryResolveWsHandle(ws, vernacular: false, out var wsHandle))
						return false;
					EnsureOpen(FieldLabel(detailField));
					Entry.SensesOS[0].Gloss.set_String(wsHandle,
						DetailRichTextAdapter.ToTsString(value, Cache.WritingSystemFactory, wsHandle));
					return true;
				}
				default:
					return false;
			}
		}

		// Tasks 6.2/6.13 (multi-WS write path): each per-WS row writes its own alternative. The row
		// addresses its writing system by the unique IETF tag (DetailWsValue.WsTag/ws.Id) first;
		// the user-editable Abbreviation (which can collide) and the legacy "vern"/"anal" aliases
		// from the fixed first-slice definition are accepted as fallbacks. Any OTHER unknown key is
		// rejected -- a silent write to the DEFAULT alternative is worse than no
		// write, and it matches ComposedDetailEditContext, which also rejects unknown keys.
		private bool TryResolveWsHandle(string ws, bool vernacular, out int wsHandle)
		{
			var container = Cache.ServiceLocator.WritingSystems;
			var systems = vernacular ? container.CurrentVernacularWritingSystems : container.CurrentAnalysisWritingSystems;
			foreach (var def in systems)
			{
				if (def.Id == ws)
				{
					wsHandle = def.Handle;
					return true;
				}
			}

			foreach (var def in systems)
			{
				if (def.Abbreviation == ws)
				{
					wsHandle = def.Handle;
					return true;
				}
			}

			if (ws == "vern" || ws == "anal")
			{
				wsHandle = vernacular ? Cache.DefaultVernWs : Cache.DefaultAnalWs;
				return true;
			}

			wsHandle = 0;
			return false;
		}

		/// <inheritdoc />
		public override bool TrySetOption(DetailField detailField, string optionKey)
		{
			if (detailField?.Field != "MorphType" || Entry.LexemeFormOA == null)
				return false;
			if (!Guid.TryParse(optionKey, out var guid))
				return false;

			var repository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			if (!repository.TryGetObject(guid, out var morphType))
				return false;

			EnsureOpen(FieldLabel(detailField));
			Entry.LexemeFormOA.MorphTypeRA = morphType;
			return true;
		}

		// ITEM 1: the human-readable field label that names the undo step, falling back to the field name.
		private static string FieldLabel(DetailField detailField)
			=> string.IsNullOrEmpty(detailField?.Label) ? detailField?.Field : detailField.Label;
	}
}
