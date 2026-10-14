// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Delete command of a detail row: whether the row's object may be deleted now, and
	/// the menu text that says so. Execution stays with <c>CmObjectUi.DeleteUnderlyingObject</c>.
	/// </summary>
	public static class DetailObjectDeletion
	{
		/// <summary>The strings-table key of the text appended to a disabled Delete
		/// item.</summary>
		public const string CannotDeleteKey = "(cannot delete this)";

		/// <summary>
		/// Whether <paramref name="target"/> may be deleted: always when unowned or in a field
		/// its owner does not require; never from a required atomic field; from a required
		/// vector only while it is not the last item.
		/// </summary>
		public static bool CanDeleteNow(ICmObject target)
		{
			if (target == null)
				return false;
			var owner = target.Owner;
			if (owner == null)
				return true;
			var flid = target.OwningFlid;
			if (!owner.IsFieldRequired(flid))
				return true;
			if (!target.Cache.IsVectorProperty(flid))
				return false;
			return target.Cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid) > 1;
		}

		/// <summary>
		/// The Delete item's text: the configured label, with the cannot-delete note appended
		/// when <paramref name="canDelete"/> is false and <c>{0}</c> replaced by the target's
		/// localized class name.
		/// </summary>
		/// <param name="target">The object to delete; null leaves any <c>{0}</c> in
		/// place.</param>
		public static string DeleteLabel(string label, ICmObject target, bool canDelete)
		{
			var text = label ?? string.Empty;
			if (!canDelete)
				text += StringTable.Table.GetString(CannotDeleteKey);
			if (text.Contains("{0}") && target != null)
				text = string.Format(text, StringTable.Table.GetString(target.ClassName, "ClassNames"));
			return text;
		}
	}
}
