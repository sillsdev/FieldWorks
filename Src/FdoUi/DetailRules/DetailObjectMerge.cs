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
	/// The Merge command of a detail row (message DataTreeMerge): whether the row's object
	/// has something to merge into now, and the menu text that says so. Execution stays with
	/// <c>CmObjectUi.MergeUnderlyingObject</c>, which asks for the survivor.
	/// </summary>
	public static class DetailObjectMerge
	{
		/// <summary>The strings-table key of the text appended to a disabled Merge
		/// item.</summary>
		public const string CannotMergeKey = "(cannot merge this)";

		/// <summary>
		/// Whether <paramref name="target"/> can merge into another object now: it sits in an
		/// owning vector that holds another object of its class, or an allomorph of the lexeme
		/// form's class, or a subsense (which merges into its sense). A required vector keeps
		/// its last item.
		/// </summary>
		public static bool CanMergeNow(ICmObject target)
		{
			if (target == null || !target.IsValidObject)
				return false;
			var owner = target.Owner;
			var flid = target.OwningFlid;
			var cache = target.Cache;
			if (owner == null || !cache.IsVectorProperty(flid))
				return false;
			var classId = target.ClassID;
			if (flid == LexEntryTags.kflidAlternateForms)
			{
				var lexemeForm = ((ILexEntry)owner).LexemeFormOA;
				if (lexemeForm != null && lexemeForm.ClassID == classId)
					return true;
			}
			if (flid == LexSenseTags.kflidSenses)
				return true;
			var sda = cache.DomainDataByFlid;
			var count = sda.get_VecSize(owner.Hvo, flid);
			if (owner.IsFieldRequired(flid) && count < 2)
				return false;
			var repository = cache.ServiceLocator.ObjectRepository;
			for (var i = 0; i < count; i++)
			{
				var hvo = sda.get_VecItem(owner.Hvo, flid, i);
				if (hvo != target.Hvo && repository.GetObject(hvo).ClassID == classId)
					return true;
			}
			return false;
		}

		/// <summary>The Merge item's text: the configured label, with the cannot-merge note
		/// appended when <paramref name="canMerge"/> is false.</summary>
		public static string MergeLabel(string label, bool canMerge)
		{
			var text = label ?? string.Empty;
			return canMerge ? text : text + StringTable.Table.GetString(CannotMergeKey);
		}
	}
}
