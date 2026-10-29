// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Demote and Promote commands of a sense (messages DemoteSense and PromoteSense):
	/// whether the sense can move one level down or up the sense outline now, and the move.
	/// </summary>
	public static class SenseNesting
	{
		/// <summary>
		/// Whether <paramref name="target"/> can become a subsense of a sibling: it sits in
		/// the senses of an entry or a sense beside at least one other sense.
		/// </summary>
		public static bool CanDemote(ICmObject target)
		{
			if (target == null || !target.IsValidObject)
				return false;
			var flid = target.OwningFlid;
			if (flid != LexEntryTags.kflidSenses && flid != LexSenseTags.kflidSenses)
				return false;
			return target.Cache.DomainDataByFlid.get_VecSize(target.Owner.Hvo, flid) > 1;
		}

		/// <summary>
		/// Makes <paramref name="sense"/> the last subsense of the sibling before it, or of
		/// the sibling after it when it comes first, in one undoable step.
		/// </summary>
		/// <returns>True when the sense moved.</returns>
		public static bool Demote(ILexSense sense)
		{
			if (!CanDemote(sense))
				return false;
			var siblings = sense.Owner is ILexEntry entry ? entry.SensesOS : ((ILexSense)sense.Owner).SensesOS;
			var index = sense.IndexInOwner;
			var newOwner = siblings[index == 0 ? 1 : index - 1];
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoDemote, FdoUiStrings.ksRedoDemote, sense,
				() => newOwner.SensesOS.Add(sense));
			return true;
		}

		/// <summary>Whether <paramref name="target"/> is a subsense, which can move up
		/// beside the sense that owns it.</summary>
		public static bool CanPromote(ICmObject target)
			=> target != null && target.IsValidObject && target.OwningFlid == LexSenseTags.kflidSenses;

		/// <summary>
		/// Makes the subsense <paramref name="sense"/> the sibling right after the sense that
		/// owns it, in one undoable step.
		/// </summary>
		/// <returns>True when the sense moved.</returns>
		public static bool Promote(ILexSense sense)
		{
			if (!CanPromote(sense))
				return false;
			var oldOwner = (ILexSense)sense.Owner;
			var index = oldOwner.IndexInOwner + 1;
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoPromote, FdoUiStrings.ksRedoPromote, sense, () =>
			{
				if (oldOwner.Owner is ILexEntry entry)
					entry.SensesOS.Insert(index, sense);
				else
					((ILexSense)oldOwner.Owner).SensesOS.Insert(index, sense);
			});
			return true;
		}
	}
}
