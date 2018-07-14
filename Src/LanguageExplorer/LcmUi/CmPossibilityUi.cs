// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special UI behaviors for the CmPossibility class.
	/// </summary>
	public class CmPossibilityUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a CmPossibility.
		/// Review JohnH (JohnT): should we declare the argument to be CmPossibility?
		/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
		/// passed an obj anyway.
		/// </summary>
		protected CmPossibilityUi(ICmPossibility obj)
			: base(obj)
		{
		}

		internal CmPossibilityUi() { }

		internal static CmPossibilityUi MakeLcmModelUiObject(ICmPossibility obj)
		{
			return new CmPossibilityUi(obj);
		}

		public static CmObjectUi MakeLcmModelUiObject(LcmCache cache, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			Guard.AgainstNull(cache, nameof(cache));

			return CheckAndReportProblemAddingSubitem(cache, hvoOwner) ? null : MakeLcmModelUiObject(classId, hvoOwner, flid, insertionPosition, cache);
		}

		/// <summary>
		/// Gets a special VC that knows to use the abbr for the shortname, etc.
		/// </summary>
		public override IVwViewConstructor Vc
		{
			get
			{
				if (m_vc == null)
				{
					m_vc = new CmNameAbbrObjVc(m_cache, CmPossibilityTags.kflidName, CmPossibilityTags.kflidAbbreviation);
				}
				return base.Vc;
			}
		}

#if RANDYTODO
		public override bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			var command = (Command)commandObject;
			string className = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "className");

			int specifiedClsid = 0;
			// There are some special commands with dummy class names (WordPartOfSpeech is one).
			// I (JohnT) don't know why we did that, but apparently everything else works for those
			// commands if we just leave specifiedClsid zero (code takes the second branch of the if below,
			// since CmObject does not inherit from CmPossibility, and depending on the override
			// usually it isn't displayed, but there may be some override that does.
			if (m_cache.GetManagedMetaDataCache().ClassExists(className))
				specifiedClsid = m_cache.DomainDataByFlid.MetaDataCache.GetClassId(className);

			var cp = (ICmPossibility) Object;
			var owningList = cp.OwningList;
			if (m_cache.ClassIsOrInheritsFrom(specifiedClsid, CmPossibilityTags.kClassId) ||
				specifiedClsid == LexEntryTypeTags.kClassId)	// these appear in 2 separate lists.
			{
				IFwMetaDataCache mdc = m_cache.DomainDataByFlid.MetaDataCache;
				// make sure our object matches the CmPossibilityList flid name.
				int owningFlid = owningList.OwningFlid;
				string owningFieldName = owningFlid == 0 ? "" : mdc.GetFieldName(owningFlid);
				string owningClassName = owningFlid == 0 ? "" : mdc.GetOwnClsName(owningFlid);
				string commandListOwnerName = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "ownerClass");
				string commandListFieldName = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "ownerField");
				display.Visible = display.Enabled = (commandListFieldName == owningFieldName && commandListOwnerName == owningClassName);
			}
			else
			{
				// we should have a specific class name to match on.
				display.Visible = display.Enabled = ShouldDisplayMenuForClass(specifiedClsid, display);
			}

			// try to insert the name of list into our display.Text for commands with label="{0}"
			FormatDisplayTextWithListName(m_cache, owningList, ref display);
			if (display.Enabled)
				command.TargetId = GuidForJumping(commandObject);
			return true;
		}

		public static string FormatDisplayTextWithListName(LcmCache cache,
			ICmPossibilityList pssl, ref UIItemDisplayProperties display)
		{
			string listName = pssl.Owner != null ? cache.DomainDataByFlid.MetaDataCache.GetFieldName(pssl.OwningFlid) : pssl.Name.BestAnalysisVernacularAlternative.Text;
			string itemTypeName = GetPossibilityDisplayName(pssl);
			if (itemTypeName != "*" + listName + "*")
			{
				string formattedText = string.Format(display.Text, itemTypeName);
				display.Text = formattedText;
			}
			return display.Text;
		}
#endif

		public static string GetPossibilityDisplayName(ICmPossibilityList list)
		{
			var listName = list.Owner != null ? list.Cache.DomainDataByFlid.MetaDataCache.GetFieldName(list.OwningFlid)
				: list.Name.BestAnalysisVernacularAlternative.Text;
			var itemsTypeName = StringTable.Table.GetString(listName, "PossibilityListItemTypeNames");
			if (itemsTypeName != "*" + listName + "*")
			{
				return itemsTypeName;
			}
			return list.PossibilitiesOS.Count > 0 ? StringTable.Table.GetString(list.PossibilitiesOS[0].GetType().Name, "ClassNames") : itemsTypeName;
		}

		public override string DisplayNameOfClass
		{
			get
			{
				var poss = (ICmPossibility)MyCmObject;
				var owningList = poss.OwningList;
				if (owningList.OwningFlid == 0)
				{
					return StringTable.Table.GetString(poss.GetType().Name, "ClassNames");
				}
				var owningFieldName = m_cache.DomainDataByFlid.MetaDataCache.GetFieldName(owningList.OwningFlid);
				var itemsTypeName = GetPossibilityDisplayName(owningList);
				return itemsTypeName != "*" + owningFieldName + "*" ? itemsTypeName : StringTable.Table.GetString(poss.GetType().Name, "ClassNames");
			}
		}

		/// <summary>
		/// Check whether it is OK to add a possibility to the specified item. If not, report the
		/// problem to the user and return true.
		/// </summary>
		private static bool CheckAndReportProblemAddingSubitem(LcmCache cache, int hvoItem)
		{
			var possItem = cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoItem);
			if (possItem == null)
			{
				return false; // not detecting problems with moving other kinds of things.
			}
			var rootPoss = possItem.MainPossibility;
			var hvoRootItem = rootPoss.Hvo;
			var hvoPossList = rootPoss.OwningList.Hvo;

			// If we get here hvoPossList is a possibility list and hvoRootItem is a top level item in that list
			// and possItem is, or is a subpossibility of, that top level item.

			// 1. Check to see if hvoRootItem is a chart template containing our target.
			// If so, hvoPossList is owned in the chart templates property.
			if (CheckAndReportBadDiscourseTemplateAdd(cache, possItem.Hvo, hvoRootItem, hvoPossList))
			{
				return true;
			}

			// 2. Check to see if hvoRootItem is a TextMarkup TagList containing our target (i.e. a Tag type).
			// If so, hvoPossList is owned in the text markup tags property.
			return CheckAndReportBadTagListAdd(cache, possItem.Hvo, hvoRootItem, hvoPossList);
		}

		private static bool CheckAndReportBadTagListAdd(LcmCache cache, int hvoItem, int hvoRootItem, int hvoPossList)
		{
			if (cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoPossList).OwningFlid != LangProjectTags.kflidTextMarkupTags)
			{
				return false; // some other list we don't care about.
			}

			// Confirm the two-level rule.
			if (hvoItem != hvoRootItem)
			{
				MessageBox.Show(LcmUiStrings.ksMarkupTagsTooDeep, LcmUiStrings.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			return false;
		}

		private static bool CheckAndReportBadDiscourseTemplateAdd(LcmCache cache, int hvoItem, int hvoRootItem, int hvoList)
		{
			if (cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoList).OwningFlid != DsDiscourseDataTags.kflidConstChartTempl)
			{
				return false; // some other list we don't care about.
			}
			// We can't turn a column into a group if it's in use.
			var poss = cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoItem);
			using (var col = new CmPossibilityUi(poss))
			{
				// If the item doesn't already have children, we can only add them if it isn't already in use
				// as a column: we don't want to change a column into a group. Thus, if there are no
				// children, we generally call the same routine as when deleting.
				// However, that routine has a special case to prevent deletion of the default template even
				// if NOT in use...and we must not prevent adding to that when it is empty! Indeed any
				// empty CHART can always be added to, so only if col's owner is a CmPossibility (it's not a root
				// item in the templates list) do we need to check for it being in use.
				if (poss.SubPossibilitiesOS.Count == 0 && poss.Owner is ICmPossibility && col.CheckAndReportProtectedChartColumn())
				{
					return true;
				}
			}
			// Finally, we have to confirm the three-level rule.
			var owner = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).Owner;
			if (hvoItem != hvoRootItem && owner != null && owner.Hvo != hvoRootItem)
			{
				MessageBox.Show(LcmUiStrings.ksTemplateTooDeep, LcmUiStrings.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			return false;
		}

		public override bool CanDelete(out string cannotDeleteMsg)
		{
			if (!CanModifyChartColumn(out cannotDeleteMsg))
			{
				return false;
			}

			return CanDeleteTextMarkupTag(out cannotDeleteMsg) && base.CanDelete(out cannotDeleteMsg);
		}

		public bool CheckAndReportProtectedChartColumn()
		{
			string msg;
			if (!CanModifyChartColumn(out msg))
			{
				MessageBoxUtils.Show(msg, LcmUiStrings.ksWarningCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			return false;
		}

		private bool CanModifyChartColumn(out string msg)
		{
			var poss = (ICmPossibility) MyCmObject;
			if (poss.IsDefaultDiscourseTemplate)
			{
				msg = LcmUiStrings.ksCantDeleteDefaultDiscourseTemplate;
				return false;
			}

			if (poss.IsThisOrDescendantInUseAsChartColumn)
			{
				var rootPossibility = (ICmPossibility) MyCmObject;
				while (rootPossibility.Owner is ICmPossibility)
				{
					rootPossibility = (ICmPossibility) rootPossibility.Owner;
				}
				var chart = rootPossibility.Services.GetInstance<IDsChartRepository>().InstancesWithTemplate(rootPossibility).First();
				var textName = ((IDsConstChart)chart).BasedOnRA.Title.BestAnalysisVernacularAlternative.Text;
				// This is an actual column; it's a problem if it has instances
				msg = string.Format(LcmUiStrings.ksCantModifyTemplateInUse, textName);
				return false;
			}

			msg = null;
			return true;
		}

		private bool CanDeleteTextMarkupTag(out string msg)
		{
			var poss = (ICmPossibility) MyCmObject;
			if (poss.IsOnlyTextMarkupTag)
			{
				msg = LcmUiStrings.ksCantDeleteLastTagList;
				return false;
			}

			ITextTag usedTag = poss.Services.GetInstance<ITextTagRepository>().GetByTextMarkupTag(poss).FirstOrDefault();
			if (usedTag != null)
			{
				string textName = null;
				if (usedTag.BeginSegmentRA != null)
				{
					var ws = usedTag.Cache.LangProject.DefaultWsForMagicWs(WritingSystemServices.kwsFirstAnalOrVern);
					var text = (IStText) usedTag.BeginSegmentRA.Owner.Owner;
					textName = text.Title.get_String(ws).Text;
					if (string.IsNullOrEmpty(textName))
					{
						textName = text.ShortName;
					}
				}
				msg = string.Format(poss.SubPossibilitiesOS.Count == 0 ? LcmUiStrings.ksCantDeleteMarkupTagInUse
					: LcmUiStrings.ksCantDeleteMarkupTypeInUse, textName);
				return false;
			}

			msg = null;
			return true;
		}
	}
}