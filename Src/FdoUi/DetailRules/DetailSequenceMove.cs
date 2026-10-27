// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Move Up and Move Down commands of a detail row: whether the row's object can move
	/// one place within its owning sequence, and the move itself.
	/// </summary>
	public static class DetailSequenceMove
	{
		/// <summary>
		/// Whether <paramref name="target"/> sits in an owning sequence, so the move items
		/// apply to it at all: false for an unowned object and for one in an atomic or
		/// collection field.
		/// </summary>
		public static bool IsInSequence(ICmObject target)
		{
			if (target == null || !target.IsValidObject || target.Owner == null || target.OwningFlid <= 0)
				return false;
			return (CellarPropertyType)target.Cache.MetaDataCacheAccessor.GetFieldType(target.OwningFlid)
				== CellarPropertyType.OwningSequence;
		}

		/// <summary>
		/// Whether <paramref name="target"/> can move one place up: it is in a sequence and
		/// not first. The first slot of an entry's EntryRefs belongs to a complex form, so
		/// while the first ref is one the second cannot move up.
		/// </summary>
		public static bool CanMoveUp(ICmObject target)
		{
			if (!IsInSequence(target))
				return false;
			var index = IndexOf(target);
			if (index <= 0)
				return false;
			if (index == 1 && target.OwningFlid == LexEntryTags.kflidEntryRefs)
				return ((ILexEntry)target.Owner).EntryRefsOS[0].ComplexEntryTypesRS.Count == 0;
			return true;
		}

		/// <summary>Whether <paramref name="target"/> can move one place down: it is in a
		/// sequence and not last.</summary>
		public static bool CanMoveDown(ICmObject target)
		{
			if (!IsInSequence(target))
				return false;
			var index = IndexOf(target);
			return index >= 0 && index + 1 < Count(target);
		}

		/// <summary>Moves <paramref name="target"/> one place up in one undoable step.</summary>
		/// <returns>True when it moved.</returns>
		public static bool MoveUp(ICmObject target, string undoText, string redoText)
		{
			if (!CanMoveUp(target))
				return false;
			var index = IndexOf(target);
			Move(target, index, index - 1, undoText, redoText);
			return true;
		}

		/// <summary>Moves <paramref name="target"/> one place down in one undoable
		/// step.</summary>
		/// <returns>True when it moved.</returns>
		public static bool MoveDown(ICmObject target, string undoText, string redoText)
		{
			if (!CanMoveDown(target))
				return false;
			var index = IndexOf(target);
			// MoveOwnSeq inserts before the destination, so one place down is two on.
			Move(target, index, index + 2, undoText, redoText);
			return true;
		}

		private static void Move(ICmObject target, int index, int destination, string undoText, string redoText)
		{
			var cache = target.Cache;
			var owner = target.Owner.Hvo;
			var flid = target.OwningFlid;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, cache.ActionHandlerAccessor,
				() => cache.DomainDataByFlid.MoveOwnSeq(owner, flid, index, index, owner, flid, destination));
		}

		private static int IndexOf(ICmObject target)
			=> target.Cache.DomainDataByFlid.GetObjIndex(target.Owner.Hvo, target.OwningFlid, target.Hvo);

		private static int Count(ICmObject target)
			=> target.Cache.DomainDataByFlid.get_VecSize(target.Owner.Hvo, target.OwningFlid);
	}
}
