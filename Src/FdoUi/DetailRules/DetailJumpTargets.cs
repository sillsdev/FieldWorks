// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The target of a detail row's jump command (message JumpToTool): the object the
	/// command's tool should show, resolved from the shown root, the row's object and the
	/// row's field.
	/// </summary>
	public static class DetailJumpTargets
	{
		/// <summary>The command whose concordance jump always targets the root entry.</summary>
		public const string RootEntryConcordanceCommandId = "CmdRootEntryJumpToConcordance";

		public const string ConcordanceTool = "concordance";
		public const string LexiconEditTool = "lexiconEdit";
		public const string NotebookEditTool = "notebookEdit";
		public const string InterlinearEditTool = "interlinearEdit";

		/// <summary>
		/// Resolves the jump target. In the concordance the row's field decides: it must be a
		/// real (non-virtual) field, and a form row must show the form itself. The lexicon
		/// jump goes to the entry owning the row's object when that is not the root; the
		/// notebook jump to the record owning it or associated with it; the interlinear jump
		/// to the row's text.
		/// </summary>
		/// <param name="commandId">The command's id; <see cref="RootEntryConcordanceCommandId"/>
		/// targets the root whatever the row.</param>
		/// <param name="fallback">Returned when the row offers no target, so the command
		/// stays unanswered: the command's own target when one was set, else empty.</param>
		/// <param name="root">The object the detail view shows; null when none.</param>
		/// <param name="target">The row's object, or the root when no row is current; null
		/// resolves to <paramref name="fallback"/>.</param>
		/// <param name="flid">The row's field; null when no row is current or the row is a
		/// header, 0 when the row's field is unknown.</param>
		public static DetailJumpTarget Resolve(string tool, string className, string commandId,
			Guid fallback, ICmObject root, ICmObject target, int? flid)
		{
			if (target == null)
				return new DetailJumpTarget(fallback);
			var owner = target.Owner;
			switch (tool)
			{
				case ConcordanceTool:
					return new DetailJumpTarget(ConcordanceTarget(className, commandId, fallback, root, target, flid));
				case LexiconEditTool:
					if (owner != null && owner != root && owner.ClassID == LexEntryTags.kClassId)
						return new DetailJumpTarget(owner.Guid);
					break;
				case NotebookEditTool:
					if (owner != null && owner.ClassID == RnGenericRecTags.kClassId)
						return new DetailJumpTarget(owner.Guid);
					if (target is IText text)
					{
						var record = text.AssociatedNotebookRecord;
						return record != null
							? new DetailJumpTarget(record.Guid)
							: new DetailJumpTarget(text.Guid, text);
					}
					break;
				case InterlinearEditTool:
					if (target.ClassID == TextTags.kClassId)
						return new DetailJumpTarget(target.Guid);
					break;
			}
			return new DetailJumpTarget(fallback);
		}

		private static Guid ConcordanceTarget(string className, string commandId, Guid fallback,
			ICmObject root, ICmObject target, int? flid)
		{
			var cache = target.Cache;
			if (flid.HasValue && (flid.Value == 0 || cache.MetaDataCacheAccessor.get_IsVirtual(flid.Value)))
				return fallback;
			switch (className)
			{
				case "LexEntry":
					if (root == null || root.ClassID != LexEntryTags.kClassId)
						break;
					if (commandId == RootEntryConcordanceCommandId)
						return root.Guid;
					if (target.ClassID == LexEntryRefTags.kClassId)
						return fallback;
					if (target.ClassID == LexEntryTags.kClassId)
						return target.Guid;
					var entry = target.OwnerOfClass<ILexEntry>();
					return entry == null ? fallback : entry.Guid;
				case "LexSense":
					if (target.ClassID == LexSenseTags.kClassId && ((ILexSense)target).Entry == root)
						return target.Guid;
					break;
				case "MoForm":
					if (cache.ClassIsOrInheritsFrom(target.ClassID, MoFormTags.kClassId)
						&& flid == MoFormTags.kflidForm)
					{
						return target.Guid;
					}
					break;
			}
			return fallback;
		}

		/// <summary>
		/// Creates the notebook record for a text that has none and associates the two; the
		/// jump then shows that record.
		/// </summary>
		public static IRnGenericRec AssociateWithNotebook(IText text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			text.AssociateWithNotebook(true);
			return text.AssociatedNotebookRecord;
		}
	}

	/// <summary>A resolved jump target.</summary>
	public sealed class DetailJumpTarget
	{
		public DetailJumpTarget(Guid guid, IText textWithoutRecord = null)
		{
			Guid = guid;
			TextWithoutRecord = textWithoutRecord;
		}

		/// <summary>The object to show, or the resolver's fallback when the row offers
		/// none.</summary>
		public Guid Guid { get; }

		/// <summary>
		/// The text the notebook jump targets although it has no record yet: the jump must
		/// first create one through <see cref="DetailJumpTargets.AssociateWithNotebook"/>
		/// and show that. Null for every other target.
		/// </summary>
		public IText TextWithoutRecord { get; }
	}
}
