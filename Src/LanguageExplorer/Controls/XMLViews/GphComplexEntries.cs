// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// GhostParentHelper subclass for the complex entry type field.
	/// - a ghost owner is considered childless although it may have variant EntryRefs if it has no complex form ones.
	/// </summary>
	internal class GphComplexEntries : GhostParentHelper
	{
		internal GphComplexEntries(ILcmServiceLocator services)
			: base(services, LexEntryTags.kClassId, LexEntryTags.kflidEntryRefs)
		{
		}

		/// <summary>
		/// Return true if we have no complex form EntryRef.
		/// Although the property for which this GPH is used initially contains only entries
		/// that have no complex form LER, a previous bulk edit might have created one.
		/// </summary>
		public override bool IsGhostOwnerChildless(int hvoItem)
		{
			return !m_services.GetInstance<ILexEntryRepository>().GetObject(hvoItem).EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtComplexForm).Take(1).Any();
		}

		/// <summary>
		/// We want specifically the first EntryRef of type complex form.
		/// </summary>
		internal override int GetFirstChildFromParent(int hvoParent)
		{
			return m_services.GetInstance<ILexEntryRepository>().GetObject(hvoParent).EntryRefsOS.First(ler => ler.RefType == LexEntryRefTags.krtComplexForm).Hvo;
		}

		/// <summary>
		/// Override to make the new object a complex one.
		/// </summary>
		internal override int CreateOwnerOfTargetProp(int hvoItem, int flidBasicProp)
		{
			var result = base.CreateOwnerOfTargetProp(hvoItem, flidBasicProp);
			GetSda().SetInt(result, LexEntryRefTags.kflidRefType, LexEntryRefTags.krtComplexForm);
			return result;
		}
	}
}