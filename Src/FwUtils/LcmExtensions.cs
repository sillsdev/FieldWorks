// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.FwUtils
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
			return itemsTypeName != "*" + owningFieldName + "*" ? itemsTypeName : stringTableName;
		}

		public static string ItemsTypeName(this ICmPossibilityList me)
		{
			var listName = me.Owner != null
				? me.Cache.DomainDataByFlid.MetaDataCache.GetFieldName(me.OwningFlid)
				: me.Name.BestAnalysisVernacularAlternative.Text;
			var itemsTypeName = StringTable.Table.GetString(listName, "PossibilityListItemTypeNames");
			return itemsTypeName != "*" + listName + "*"
				? itemsTypeName
				: (me.PossibilitiesOS.Any()
					? StringTable.Table.GetString(me.PossibilitiesOS[0].GetType().Name, "ClassNames")
					: itemsTypeName);
		}

		public static string ItemTypeName(this ICmCustomItem me)
		{
			return StringTable.Table.GetString(me.GetType().Name, "ClassNames");
		}

		public static bool TryGetFieldId(this IFwMetaDataCacheManaged me, string className, string fieldName, out int flid, bool includeBaseClasses = true)
		{
			if (me.FieldExists(className, fieldName, includeBaseClasses))
			{
				var classId = me.GetClassId(className);
				flid = me.GetFieldId2(classId, fieldName, includeBaseClasses);
				return true;
			}
			flid = 0;
			return false;
		}

		public static bool TryGetFieldId(this IFwMetaDataCacheManaged me, int classId, string fieldName, out int flid, bool includeBaseClasses = true)
		{
			if (me.FieldExists(classId, fieldName, includeBaseClasses))
			{
				flid = me.GetFieldId2(classId, fieldName, includeBaseClasses);
				return true;
			}
			flid = 0;
			return false;
		}

		public static IFwMetaDataCacheManaged GetManagedMetaDataCache(this LcmCache me)
		{
			return me.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
		}

		public static IFwMetaDataCacheManaged GetManagedMetaDataCache(this ISilDataAccess me)
		{
			// Theoretically it could be null.
			return me.MetaDataCache as IFwMetaDataCacheManaged;
		}

		public static IFwMetaDataCacheManaged GetManagedMetaDataCache(this IFwMetaDataCache me)
		{
			// Theoretically it could be null.
			return me as IFwMetaDataCacheManaged;
		}

		public static ISilDataAccessManaged GetManagedSilDataAccess(this LcmCache me)
		{
			return (ISilDataAccessManaged)me.DomainDataByFlid;
		}
	}
}