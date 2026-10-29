// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Split command of a detail row (message DataTreeSplit, Move Sense to a New Entry):
	/// whether the row's object can move to a copy of its owner now. Execution stays with
	/// <c>CmObjectUi.MoveUnderlyingObjectToCopyOfOwner</c>.
	/// </summary>
	public static class DetailObjectSplit
	{
		/// <summary>
		/// Whether <paramref name="target"/> can move to a copy of its owner now: it sits in
		/// an owning vector and is either of its owner's class (a subsense) or one of at least
		/// two items there, so the owner keeps one.
		/// </summary>
		public static bool CanSplitNow(ICmObject target)
		{
			if (target == null || !target.IsValidObject)
				return false;
			var owner = target.Owner;
			var flid = target.OwningFlid;
			var cache = target.Cache;
			if (owner == null || !cache.IsVectorProperty(flid))
				return false;
			if (target.ClassID == owner.ClassID)
				return true;
			return cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid) >= 2;
		}
	}
}
