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
	/// The Duplicate command of a detail row (message DataTreeCopy): whether an object can be
	/// duplicated, the copy of its properties onto another object, and the whole duplicate.
	/// </summary>
	public static class DetailObjectDuplication
	{
		/// <summary>Whether <paramref name="original"/> can copy its properties onto another
		/// object of its class.</summary>
		public static bool CanCopy(ICmObject original)
			=> original != null && original.IsValidObject && original is ICloneableCmObject;

		/// <summary>
		/// Whether <paramref name="original"/> can be duplicated: it can copy its properties
		/// and sits in an owning sequence or collection, which takes the copy.
		/// </summary>
		public static bool CanDuplicate(ICmObject original)
		{
			if (!CanCopy(original) || original.Owner == null)
				return false;
			switch ((CellarPropertyType)original.Cache.MetaDataCacheAccessor.GetFieldType(original.OwningFlid))
			{
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.OwningCollection:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Copies the properties of <paramref name="original"/> onto <paramref name="copy"/>
		/// in one undoable step.
		/// </summary>
		public static void CopyProperties(ICmObject original, ICmObject copy, string undoText, string redoText)
		{
			UndoableUnitOfWorkHelper.Do(undoText, redoText, original.Cache.ActionHandlerAccessor,
				() => ((ICloneableCmObject)original).SetCloneProperties(copy));
		}

		/// <summary>
		/// Duplicates <paramref name="original"/> in one undoable step: a new object of its
		/// class beside it, right after it in a sequence, takes a copy of its properties. The
		/// copy is created as the model creates it, with no step a class adds to its Insert.
		/// </summary>
		/// <returns>The copy; null when <see cref="CanDuplicate"/> refuses.</returns>
		public static ICmObject Duplicate(ICmObject original, string undoText, string redoText)
		{
			if (!CanDuplicate(original))
				return null;
			var cache = original.Cache;
			var owner = original.Owner;
			var flid = original.OwningFlid;
			var position = DetailObjectInsertion.InsertionPosition(owner, flid, new[] { original.Hvo });
			ICmObject copy = null;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, cache.ActionHandlerAccessor, () =>
			{
				var hvo = cache.DomainDataByFlid.MakeNewObject(original.ClassID, owner.Hvo, flid, position);
				copy = cache.ServiceLocator.ObjectRepository.GetObject(hvo);
				((ICloneableCmObject)original).SetCloneProperties(copy);
			});
			return copy;
		}
	}
}
