// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Code;
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
		public static string ToStatusBar(this ICmObject me)
		{
			if (!me.IsValidObject)
			{
				return FwUtilsStrings.ksDeletedObject;
			}
			DateTime dt;
			var created = string.Empty;
			var modified = string.Empty;
			var myType = me.GetType();
			var pi = myType.GetProperty("DateCreated");
			if (pi != null)
			{
				dt = (DateTime)pi.GetValue(me, null);
				created = dt.ToString("dd/MMM/yyyy", DateTimeFormatInfo.InvariantInfo);
			}
			pi = myType.GetProperty("DateModified");
			if (pi != null)
			{
				dt = (DateTime)pi.GetValue(me, null);
				modified = dt.ToString("dd/MMM/yyyy", DateTimeFormatInfo.InvariantInfo);
			}
			return $"{created} {modified}";
		}

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
			return itemsTypeName != AddAsteriskBrackets(owningFieldName) ? itemsTypeName : stringTableName;
		}

		public static string ItemsTypeName(this ICmPossibilityList me)
		{
			var listName = me.Owner != null
				? me.Cache.DomainDataByFlid.MetaDataCache.GetFieldName(me.OwningFlid)
				: me.Name.BestAnalysisVernacularAlternative.Text;
			var itemsTypeName = StringTable.Table.GetString(listName, "PossibilityListItemTypeNames");
			return itemsTypeName != AddAsteriskBrackets(listName)
				? itemsTypeName
				: (me.PossibilitiesOS.Any()
					? StringTable.Table.GetString(me.PossibilitiesOS[0].GetType().Name, "ClassNames")
					: itemsTypeName);
		}

		private static string AddAsteriskBrackets(string baseData)
		{
			return $"*{baseData}*";
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

		public static ILexEntryType Create(this ILexEntryTypeFactory me, ICmPossibilityList owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			var lexEntryType = me.Create();
			owner.PossibilitiesOS.Add(lexEntryType);
			return lexEntryType;
		}

		public static ILexEntryType Create(this ILexEntryTypeFactory me, ILexEntryType owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			var lexEntryType = me.Create();
			owner.SubPossibilitiesOS.Add(lexEntryType);
			return lexEntryType;
		}

		public static ILexEntryInflType Create(this ILexEntryInflTypeFactory me, ILexEntryInflType owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			var lexEntryInflType = me.Create();
			owner.SubPossibilitiesOS.Add(lexEntryInflType);
			return lexEntryInflType;
		}

		public static ILexRefType Create(this ILexRefTypeFactory me, ICmPossibilityList owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			var lexRefType = me.Create();
			owner.PossibilitiesOS.Add(lexRefType);
			return lexRefType;
		}

		public static ILexRefType Create(this ILexRefTypeFactory me, ILexRefType owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			var lexRefType = me.Create();
			owner.SubPossibilitiesOS.Add(lexRefType);
			return lexRefType;
		}

		public static ICmPossibility Clone(this ICmPossibility me, ICmPossibilityList owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			if (!owner.PossibilitiesOS.Contains(me))
			{
				throw new InvalidOperationException($"Cannot clone a CmPossibility that is not owned by a list: '{me.Guid}'");
			}
			ICmPossibility newbie;
			switch (me.ClassID)
			{
				case CmPossibilityTags.kClassId:
					newbie = me.Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), owner);
					break;
				case CmCustomItemTags.kClassId:
					newbie = me.Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(Guid.NewGuid(), owner);
					break;
				default:
					throw new NotSupportedException($"Cloning is only supported for ICmPossibility and ICmCustomItem instances, but not for: '{me.ClassName}'.");
			}
			CopyCoreCmPossibilityInformation(me, newbie);
			ClonePossibilityChildren(me, newbie);

			return newbie;
		}

		private static ICmPossibility Clone(this ICmPossibility me, ICmPossibility owner)
		{
			Guard.AgainstNull(owner, nameof(owner));

			if (!owner.SubPossibilitiesOS.Contains(me))
			{
				throw new InvalidOperationException($"Cannot clone a CmPossibility that is not owned by another possibility: '{me.Guid}'");
			}
			ICmPossibility newbie = null;
			switch (me.ClassID)
			{
				case CmPossibilityTags.kClassId:
					newbie = me.Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), owner);
					break;
				case CmCustomItemTags.kClassId:
					newbie = me.Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(Guid.NewGuid(), (ICmCustomItem)owner);
					break;
				default:
					throw new NotSupportedException($"Cloning is only supported for ICmPossibility and ICmCustomItem instances, but not for: '{me.ClassName}'.");
			}
			CopyCoreCmPossibilityInformation(me, newbie);
			ClonePossibilityChildren(me, newbie);

			return newbie;
		}

		private static void ClonePossibilityChildren(ICmPossibility original, ICmPossibility copy)
		{
			// Subitems
			// Skip any with no names (e.g., "???").
			foreach (var subitem in original.SubPossibilitiesOS.Where(si => !si.ShortNameTSS.Text.Equals("???")))
			{
				copy.SubPossibilitiesOS.Add(Clone(subitem, copy));
			}
		}

		private static void CopyCoreCmPossibilityInformation(ICmPossibility original, ICmPossibility copy)
		{
			var ws = original.Name.AvailableWritingSystemIds[0];
			copy.Name.set_String(ws, original.Name.UiString);
			copy.Name.AnalysisDefaultWritingSystem = original.Name.AnalysisDefaultWritingSystem;
			copy.Name.VernacularDefaultWritingSystem = original.Name.VernacularDefaultWritingSystem;
			if (original.OwningFlid == CmPossibilityListTags.kflidPossibilities)
			{
				//Only the top one needs a name change
				copy.Name.set_String(ws, ChangeName(copy));
			}
			if (!string.IsNullOrEmpty(original.Abbreviation.UiString))
			{
				copy.Abbreviation.set_String(ws, original.Abbreviation.UiString);
			}
			if (!string.IsNullOrEmpty(original.Description.UiString))
			{
				copy.Description.set_String(ws, original.Description.UiString);
			}
			copy.StatusRA = original.StatusRA;
			if (copy.DiscussionOA != null)
			{
				copy.DiscussionOA = original.DiscussionOA;
			}
			copy.ConfidenceRA = original.ConfidenceRA;
			foreach (var person in original.ResearchersRC)
			{
				copy.ResearchersRC.Add(person);
			}
			foreach (var poss in original.RestrictionsRC)
			{
				copy.RestrictionsRC.Add(poss);
			}
		}

		private static string ChangeName(ICmPossibility obj)
		{
			var max = 0;
			// Captures the name up to a (Copy) and captures the duplicate number if it exists
			var regex = new Regex(@"(.[a-z0-9A-Z\s]*) \(Copy\) \((.[0-9]*)\)$");
			var match = regex.Match(obj.Name.UiString);
			//Only need the name up to the (Copy) so grab that if a duplicate else use original name
			var prefix = (match.Success) ? match.Groups[1].Value : obj.Name.UiString;
			//Find the highest duplicate number for that template
			foreach (var possibility in obj.OwningList.AllOwnedObjects)
			{
				match = regex.Match(possibility.ShortNameTSS.Text);
				if (match.Success && match.Groups[1].Value.Equals(prefix))
				{
					var temp = Convert.ToInt32(match.Groups[2].Value);
					if (temp > max)
					{
						max = temp;
					}
				}
			}
			max++; //Increment so the latest duplicate has the highest duplicate number
			return prefix + " (Copy) (" + max + ")";
		}
	}
}