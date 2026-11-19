// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Promote and Demote commands of a notebook record (messages PromoteSubitemInVector,
	/// DemoteSubitemInVector and DemoteItemInVector): whether a record can move one level up
	/// or down the record outline now, the records that may take a demoted one, and the move.
	/// </summary>
	public static class RecordNesting
	{
		/// <summary>Whether <paramref name="target"/> is a subrecord, which can move up beside
		/// the record that owns it.</summary>
		public static bool CanPromote(ICmObject target)
			=> target is IRnGenericRec record && record.IsValidObject && record.Owner is IRnGenericRec;

		/// <summary>
		/// Makes the subrecord <paramref name="record"/> the sibling right after the record that
		/// owns it, a top-level record when that one is, in one undoable step.
		/// </summary>
		/// <returns>True when the record moved.</returns>
		public static bool Promote(IRnGenericRec record)
		{
			if (!CanPromote(record))
				return false;
			var owner = (IRnGenericRec)record.Owner;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(FdoUiStrings.ksUndoPromote, FdoUiStrings.ksRedoPromote,
				record.Cache.ActionHandlerAccessor, () =>
				{
					if (owner.Owner is IRnGenericRec ownerOfOwner)
						ownerOfOwner.SubRecordsOS.Insert(owner.OwnOrd + 1, record);
					else if (owner.Owner is IRnResearchNbk notebook)
						notebook.RecordsOC.Add(record);
					else
						throw new InvalidOperationException("A record is owned by neither a record nor a notebook.");
				});
			return true;
		}

		/// <summary>Whether <paramref name="record"/> is a top-level record of its notebook,
		/// which a promoted record that becomes one is shown as.</summary>
		public static bool IsTopLevel(IRnGenericRec record)
			=> record != null && record.IsValidObject && record.Owner is IRnResearchNbk;

		/// <summary>Whether the subrecord <paramref name="target"/> has a sibling to become a
		/// subrecord of.</summary>
		public static bool CanDemoteSubrecord(ICmObject target)
			=> target is IRnGenericRec record && record.IsValidObject && record.Owner is IRnGenericRec owner
				&& owner.SubRecordsOS.Count > 1;

		/// <summary>The records that may take the subrecord <paramref name="record"/>: its
		/// siblings, in order; empty when it cannot be demoted.</summary>
		public static IReadOnlyList<IRnGenericRec> SubrecordDemoteOwners(IRnGenericRec record)
			=> CanDemoteSubrecord(record)
				? ((IRnGenericRec)record.Owner).SubRecordsOS.Where(r => r != record).ToList()
				: new List<IRnGenericRec>();

		/// <summary>Whether the top-level record <paramref name="target"/> has another
		/// top-level record to become a subrecord of.</summary>
		public static bool CanDemoteRecord(ICmObject target)
			=> target is IRnGenericRec record && record.IsValidObject && record.Owner is IRnResearchNbk notebook
				&& notebook.RecordsOC.Count > 1;

		/// <summary>The records that may take the top-level record <paramref name="record"/>:
		/// the notebook's other top-level records; empty when it cannot be demoted.</summary>
		public static IReadOnlyList<IRnGenericRec> RecordDemoteOwners(IRnGenericRec record)
			=> CanDemoteRecord(record)
				? ((IRnResearchNbk)record.Owner).RecordsOC.Where(r => r != record).ToList()
				: new List<IRnGenericRec>();

		/// <summary>
		/// The record to take a demoted one: the only candidate without asking, else the one
		/// <paramref name="ask"/> picks; null when there is none or the user cancels.
		/// </summary>
		/// <param name="candidates">The records that may take it.</param>
		/// <param name="ask">Asks the user to pick among two or more candidates.</param>
		public static IRnGenericRec ChooseOwner(IReadOnlyList<IRnGenericRec> candidates,
			Func<IReadOnlyList<IRnGenericRec>, IRnGenericRec> ask)
		{
			if (candidates == null || candidates.Count == 0)
				return null;
			return candidates.Count == 1 ? candidates[0] : ask(candidates);
		}

		/// <summary>
		/// Makes <paramref name="record"/> the first subrecord of <paramref name="newOwner"/>, in
		/// one undoable step.
		/// </summary>
		/// <returns>True when the record moved.</returns>
		public static bool Demote(IRnGenericRec record, IRnGenericRec newOwner)
		{
			if (record == null || newOwner == null || !record.IsValidObject || !newOwner.IsValidObject)
				return false;
			if (newOwner == record)
				throw new ArgumentException("A record cannot own itself.", nameof(newOwner));
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(FdoUiStrings.ksUndoDemote, FdoUiStrings.ksRedoDemote,
				record.Cache.ActionHandlerAccessor, () => newOwner.SubRecordsOS.Insert(0, record));
			return true;
		}
	}
}
