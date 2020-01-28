// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace SIL.FieldWorks.Common.FwUtils
{
#if RANDYTODO
	// TODO: Move these to LCM?
#endif
	/// <summary>
	/// Restore some LCM behavior that went away, when LCM went away.
	/// </summary>
	public static class LcmExtensions
	{
		private static bool s_reversalIndicesAreKnownToExist;

		/// <summary>
		/// Get a message suitable for display in a status bar.
		/// </summary>
		public static string ToStatusBar(this ICmObject me)
		{
			if (!me.IsValidObject)
			{
				return FwUtilsStrings.ksDeletedObject;
			}
			const string dateFormat = "dd/MMM/yyyy";
			DateTime dt;
			var created = string.Empty;
			var modified = string.Empty;
			var myType = me.GetType();
			var pi = myType.GetProperty("DateCreated");
			if (pi != null)
			{
				dt = (DateTime)pi.GetValue(me, null);
				created = dt.ToString(dateFormat, DateTimeFormatInfo.InvariantInfo);
			}
			pi = myType.GetProperty("DateModified");
			if (pi != null)
			{
				dt = (DateTime)pi.GetValue(me, null);
				modified = dt.ToString(dateFormat, DateTimeFormatInfo.InvariantInfo);
			}
			return $"{created} {modified}";
		}

		public static string DisplayNameOfClass(this ICmObject me)
		{
			var className = me.ClassName;
			var displayNameOfClass = StringTable.Table.GetString(className, StringTable.ClassNames);
			if (me is ICmCustomItem)
			{
				return displayNameOfClass;
			}
			if (me is ICmPossibility)
			{
				return ((ICmPossibility)me).ItemTypeName();
			}
			if (displayNameOfClass == $"*{className}*")
			{
				displayNameOfClass = className;
			}
			string alternateDisplayName;
			var featureSystem = me.OwnerOfClass(FsFeatureSystemTags.kClassId) as IFsFeatureSystem;
			if (featureSystem != null)
			{
				var searchSpace = featureSystem.OwningFlid == LangProjectTags.kflidPhFeatureSystem ? "Phonological" : "List";
				alternateDisplayName = StringTable.Table.GetString($"Feature-{searchSpace}", StringTable.AlternativeTypeNames);
				if (alternateDisplayName != $"*Feature-{searchSpace}*")
				{
					return alternateDisplayName;
				}
			}
			switch (me.OwningFlid)
			{
				case MoStemNameTags.kflidRegions:
					alternateDisplayName = StringTable.Table.GetString($"{className}-MoStemName", StringTable.AlternativeTypeNames);
					if (alternateDisplayName != $"*{className}-MoStemName*")
					{
						return alternateDisplayName;
					}
					break;
			}
			return displayNameOfClass;
		}

		/// <summary>
		/// Try to get the WS from a selection range.
		/// </summary>
		/// <returns>Return '0' if:
		/// 1) there is not text at all, 2) <paramref name="ichMin"/> is not less than <paramref name="ichLim"/>
		/// 2) the selected text is multilingual.
		/// Otherwise, return the WS of the entire text.</returns>
		public static int GetWsFromString(this ITsString me, int ichMin, int ichLim)
		{
			if (me.Length == 0 || ichMin >= ichLim)
			{
				return 0;
			}
			var runMin = me.get_RunAt(ichMin);
			var runMax = me.get_RunAt(ichLim - 1);
			var ws = me.get_WritingSystem(runMin);
			if (runMin == runMax)
			{
				return ws;
			}
			for (var i = runMin + 1; i <= runMax; ++i)
			{
				var wsT = me.get_WritingSystem(i);
				if (wsT != ws)
				{
					return 0;
				}
			}
			return ws;
		}

		public static bool IsMultilingual(this IFwMetaDataCache me, int flid)
		{
			switch ((CellarPropertyType)(me.GetFieldType(flid) & (int)CellarPropertyTypeFilter.VirtualMask))
			{
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Determine whether we need to parse any of the text's paragraphs.
		/// </summary>
		public static bool HasParagraphNeedingParse(this IStText me)
		{
			return me.ParagraphsOS.Cast<IStTxtPara>().Any(para => !para.ParseIsCurrent);
		}

		/// <summary>
		/// todo: add progress bar.
		/// typically a delegate for NonUndoableUnitOfWorkHelper
		/// </summary>
		public static void LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(this IStText me, bool forceParse)
		{
			using (var pp = new ParagraphParser(me.Cache))
			{
				if (forceParse)
				{
					foreach (var para in me.ParagraphsOS.Cast<IStTxtPara>())
					{
						pp.ForceParse(para);
					}
				}
				else
				{
					foreach (var para in me.ParagraphsOS.Cast<IStTxtPara>().Where(
						para => !para.ParseIsCurrent))
					{
						pp.Parse(para);
					}
				}
			}
			var services = new AnalysisGuessServices(me.Cache);
			services.GenerateEntryGuesses(me);
		}

		public static bool NotebookRecordRefersToThisText(this IText me, out IRnGenericRec referringRecord)
		{
			referringRecord = me.AssociatedNotebookRecord;
			return referringRecord != null;
		}

		public static string ItemTypeName(this ICmPossibility me)
		{
			// This is the code that method did for ICmPossibility.
			var stringTableName = StringTable.Table.GetString(me.GetType().Name, StringTable.ClassNames);
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
			var itemsTypeName = StringTable.Table.GetString(listName, StringTable.PossibilityListItemTypeNames);
			return itemsTypeName != AddAsteriskBrackets(listName)
				? itemsTypeName
				: (me.PossibilitiesOS.Any()
					? StringTable.Table.GetString(me.PossibilitiesOS[0].GetType().Name, StringTable.ClassNames)
					: itemsTypeName);
		}

		public static List<ICmPossibility> AllPossibilities(this ICmPossibilityList me)
		{
			var allPossibilities = new List<ICmPossibility>();
			foreach (var possibility in me.PossibilitiesOS)
			{
				allPossibilities.Add(possibility);
				var myPossibilities = possibility.AllPossibilities();
				if (myPossibilities.Any())
				{
					allPossibilities.AddRange(myPossibilities);
				}
			}
			return allPossibilities;
		}

		public static string GetPossibilityDisplayName(this ICmPossibilityList me)
		{
			var listName = me.Owner != null ? me.Cache.DomainDataByFlid.MetaDataCache.GetFieldName(me.OwningFlid) : me.Name.BestAnalysisVernacularAlternative.Text;
			var itemsTypeName = StringTable.Table.GetString(listName, StringTable.PossibilityListItemTypeNames);
			if (itemsTypeName != $"*{listName}*")
			{
				return itemsTypeName;
			}
			return me.PossibilitiesOS.Count > 0 ? StringTable.Table.GetString(me.PossibilitiesOS[0].GetType().Name, StringTable.ClassNames) : itemsTypeName;
		}

		private static List<ICmPossibility> AllPossibilities(this ICmPossibility me)
		{
			var allSubPossibilities = new List<ICmPossibility>();
			foreach (var subPossibility in me.SubPossibilitiesOS)
			{
				allSubPossibilities.Add(subPossibility);
				var mySubPossibilities = subPossibility.AllPossibilities();
				if (mySubPossibilities.Any())
				{
					allSubPossibilities.AddRange(mySubPossibilities);
				}
			}
			return allSubPossibilities;
		}

		private static string AddAsteriskBrackets(string baseData)
		{
			return $"*{baseData}*";
		}

		public static string ItemTypeName(this ICmCustomItem me)
		{
			return StringTable.Table.GetString(me.GetType().Name, StringTable.ClassNames);
		}

		public static bool AreCustomFieldsAProblem(this IFwMetaDataCacheManaged me, int[] clsids)
		{
			var rePunct = new Regex(@"\p{P}");
			foreach (var clsid in clsids)
			{
				var flids = me.GetFields(clsid, true, (int)CellarPropertyTypeFilter.All);
				foreach (var flid in flids)
				{
					if (!me.IsCustom(flid))
					{
						continue;
					}
					var name = me.GetFieldName(flid);
					if (!rePunct.IsMatch(name))
					{
						continue;
					}
					var msg = string.Format(FwUtilsStrings.PunctInFieldNameWarning, name);
					// The way this is worded, 'Yes' means go on with the export. We won't bother them reporting
					// other messed-up fields. A 'no' answer means don't continue, which means it's a problem.
					return (MessageBox.Show(Form.ActiveForm, msg, FwUtilsStrings.PunctInfieldNameCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes);
				}
			}
			return false; // no punctuation in custom fields.
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

		public static ICmPossibility Clone(this ICmPossibility me)
		{
			var owner = me.Owner;
			Require.That(owner != null, $"Cannot clone a CmPossibility that is not owned: '{me.Guid}'");
			Require.That(me.ClassID == CmPossibilityTags.kClassId, $"This only allows for cloning CmPossibility instance, but not instances of '{me.ClassName}'.");

			var newbie = owner is ICmPossibilityList ? me.Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), (ICmPossibilityList)owner) : me.Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), (ICmPossibility)owner);
			CopyCoreCmPossibilityInformation(me, newbie);

			// Sub-items
			// Skip any with no names (e.g., "???").
			foreach (var subitem in me.SubPossibilitiesOS.Where(si => !si.ShortNameTSS.Text.Equals("???")))
			{
				newbie.SubPossibilitiesOS.Add(subitem.Clone());
			}

			return newbie;
		}

		public static ICmCustomItem Clone(this ICmCustomItem me)
		{
			var owner = me.Owner;
			Require.That(me.Owner != null, $"Cannot clone a CmCustomItem that is not owned: '{me.Guid}'");

			var newbie = owner is ICmPossibilityList ? (ICmCustomItem)me.Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(Guid.NewGuid(), (ICmPossibilityList)owner) : (ICmCustomItem)me.Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(Guid.NewGuid(), (ICmCustomItem)owner);
			CopyCoreCmPossibilityInformation(me, newbie);

			// Sub-items
			// Skip any with no names (e.g., "???").
			foreach (var subitem in me.SubPossibilitiesOS.Where(si => !si.ShortNameTSS.Text.Equals("???")))
			{
				newbie.SubPossibilitiesOS.Add(subitem.Clone());
			}

			return newbie;
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

		public static Guid GetOrCreateWsGuid(this IReversalIndexRepository me, CoreWritingSystemDefinition wsObj, LcmCache cache)
		{
			var mHvoRevIdx = me.FindOrCreateIndexForWs(wsObj.Handle).Hvo;
			return cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(mHvoRevIdx).Guid;
		}

		public static void EnsureReversalIndicesExist(this IReversalIndexRepository me, LcmCache cache, IPropertyTable propertyTable)
		{
			if (s_reversalIndicesAreKnownToExist)
			{
				return;
			}
			var wsMgr = cache.ServiceLocator.WritingSystemManager;
			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
			{
				var usedWses = new List<CoreWritingSystemDefinition>();
				foreach (var coreWritingSystemDefinition in cache.LanguageProject.CurrentAnalysisWritingSystems)
				{
					var currentReversalIndex = me.FindOrCreateIndexForWs(coreWritingSystemDefinition.Handle);
					usedWses.Add(wsMgr.Get(currentReversalIndex.WritingSystem));
				}
				var corruptReversalIndices = new List<IReversalIndex>();
				foreach (var rev in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
				{
					// Make sure each index has a name, if it is available from the writing system.
					if (string.IsNullOrEmpty(rev.WritingSystem))
					{
						// Delete a bogus IReversalIndex that has no writing system.
						// But, for now only store them for later deletion,
						// as immediate removal will wreck the looping.
						corruptReversalIndices.Add(rev);
						continue;
					}
					if (string.IsNullOrWhiteSpace(rev.Name.AnalysisDefaultWritingSystem.Text))
					{
						var revWs = wsMgr.Get(rev.WritingSystem);
						// TODO WS: is DisplayLabel the right thing to use here?
						rev.Name.SetAnalysisDefaultWritingSystem(revWs.DisplayLabel);
					}
				}
				// Delete any corrupt reversal indices.
				foreach (var rev in corruptReversalIndices)
				{
					MessageBox.Show("Need to delete a corrupt reversal index (no writing system)", "Self-correction");
					// does this accomplish anything?
					cache.LangProject.LexDbOA.ReversalIndexesOC.Remove(rev);
				}
				// Set up for the reversal index combo box or dropdown menu.
				var reversalIndexGuid = ReversalIndexServices.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
				if (reversalIndexGuid == Guid.Empty)
				{
					// We haven't established the reversal index yet. Choose the first one available.
					var firstGuid = Guid.Empty;
					var reversalIds = cache.LanguageProject.LexDbOA.CurrentReversalIndices;
					if (reversalIds.Any())
					{
						firstGuid = reversalIds[0].Guid;
					}
					else if (cache.LanguageProject.LexDbOA.ReversalIndexesOC.Any())
					{
						firstGuid = cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToGuidArray()[0];
					}
					if (firstGuid != Guid.Empty)
					{
						propertyTable.SetProperty("ReversalIndexGuid", firstGuid.ToString(), true, true, SettingsGroup.LocalSettings);
					}
				}
			});
			s_reversalIndicesAreKnownToExist = true;
		}

		public static ICmPossibilityList GetTaggingList(this ILangProject me)
		{
			var result = me.TextMarkupTagsOA;
			if (result != null)
			{
				return result;
			}
			// Create the containing object and lists.
			result = me.GetDefaultTextTagList();
			me.TextMarkupTagsOA = result;
			return result;
		}

		public static int[] AnalysisWsIds(this ILangProject me)
		{
			return me.CurrentAnalysisWritingSystems.Select(ws => ws.Handle).ToArray();
		}

		public static ITsString WsLabel(this WritingSystemManager me, int ws, int defaultWs)
		{
			var wsObj = me.Get(ws);
			var abbr = TsStringUtils.MakeString(wsObj.Abbreviation, defaultWs, "Language Code");
			var tsb = abbr.GetBldr();
			tsb.SetProperties(0, tsb.Length, FwUtils.LanguageCodeTextProps(defaultWs));
			return tsb.GetString();
		}

		/// <summary>
		/// WritingSystemServices.GetWritingSystem and WritingSystemServices.GetAllWritingSystems got divested without knowing about XElement,
		/// so support such a conversion here, until those methods have overloads that do take XElement instances.
		/// </summary>
		public static XmlNode ConvertElement(this XElement me)
		{
			var doc = new XmlDocument();
			doc.LoadXml(me.GetOuterXml());
			return doc.FirstChild;
		}
	}
}