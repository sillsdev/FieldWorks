// Copyright (c) 2017-1018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Restore some LCM behavior that went away, when LCM went away.
	/// </summary>
	public static class LcmExtensions
	{
		public static string ItemTypeName(this ICmPossibility me)
		{
			// This is the code that method did for ICmPossibility.
			var stringTableName = StringTable.Table.GetString(me.GetType().Name, "ClassNames");
			var owningList = me.OwningList;
			if (owningList.OwningFlid == 0)
			{
				return stringTableName;
			}
			var owningFieldName = me.Cache.DomainDataByFlid.MetaDataCache.GetFieldName(owningList.OwningFlid);
			var itemsTypeName = owningList.ItemsTypeName();
			return itemsTypeName != "*" + owningFieldName + "*"
				? itemsTypeName
				: stringTableName;
		}

		public static string ItemsTypeName(this ICmPossibilityList me)
		{
			var listName = me.Owner != null
				? me.Cache.DomainDataByFlid.MetaDataCache.GetFieldName(me.OwningFlid)
				: me.Name.BestAnalysisVernacularAlternative.Text;
			var itemsTypeName = StringTable.Table.GetString(listName, "PossibilityListItemTypeNames");
			return itemsTypeName != "*" + listName + "*"
				? itemsTypeName
				: (me.PossibilitiesOS.Count > 0
					? StringTable.Table.GetString(me.PossibilitiesOS[0].GetType().Name, "ClassNames")
					: itemsTypeName);
		}

		public static string ItemTypeName(this ICmCustomItem me)
		{
			return StringTable.Table.GetString(me.GetType().Name, "ClassNames");
		}
	}
}
