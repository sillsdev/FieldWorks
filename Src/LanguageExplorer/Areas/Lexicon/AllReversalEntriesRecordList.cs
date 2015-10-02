// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon
{
#if RANDYTODO
	// TODO: Only used in:
/*
<clerk id="AllReversalEntries">
	<dynamicloaderinfo assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Dumpster.ReversalEntryClerk" />
	<recordList owner="ReversalIndex" property="AllEntries">
		<dynamicloaderinfo assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Areas.Lexicon.AllReversalEntriesRecordList" />
	</recordList>
	<filters />
	<sortMethods>
		<sortMethod label="Form" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
	</sortMethods>
</clerk>
"AllReversalEntries" used in:
1. <tool label="Reversal Indexes" value="reversalEditComplete" icon="SideBySideView"> (x2)
2. <tool label="Bulk Edit Reversal Entries" value="reversalBulkEditReversalEntries" icon="BrowseView">
3. <listener assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Dumpster.ReversalListener">
	<parameters clerk="AllReversalEntries" />
</listener>
*/
#endif
	/// <summary>
	/// Summary description for AllReversalEntriesRecordList.
	/// </summary>
	/// <remarks>
	/// Can't be sealed, since a test derives from the class.
	/// </remarks>
	internal class AllReversalEntriesRecordList : RecordList
	{
		/// <summary />
		internal AllReversalEntriesRecordList(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base(decorator, true, ReversalIndexTags.kflidEntries, reversalIndex, "AllEntries")
		{
			m_flid = ReversalIndexTags.kflidEntries; //LT-12577 a record list needs a real flid.
			m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
			m_oldLength = 0;
		}

		/// <summary />
		public override bool CanInsertClass(string className)
		{
			if (base.CanInsertClass(className))
				return true;
			return className == "ReversalIndexEntry";
		}

		/// <summary />
		public override bool CreateAndInsert(string className)
		{
			if (className != "ReversalIndexEntry")
				return base.CreateAndInsert(className);
			m_newItem = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
			var ri = (IReversalIndex)m_owningObject;
			ri.EntriesOC.Add(m_newItem);
			var extensions = m_cache.ActionHandlerAccessor as IActionHandlerExtensions;
			if (extensions != null)
				extensions.DoAtEndOfPropChanged(SelectNewItem);
			return true;
		}

		private IReversalIndexEntry m_newItem;

		void SelectNewItem()
		{
			Clerk.OnJumpToRecord(m_newItem.Hvo);
		}

		/// <summary>
		/// Get the current reversal index guid.  If there is none, create a new reversal index
		/// since there must not be any.  This fixes LT-6653.
		/// </summary>
		/// <returns></returns>
		internal static Guid GetReversalIndexGuid(IPropertyTable propertyTable, IPublisher publisher)
		{
			var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");

			if (riGuid.Equals(Guid.Empty))
			{
				try
				{
					publisher.Publish("InsertReversalIndex_FORCE", null);
					riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
				}
				catch
				{
					return Guid.Empty;
				}
			}
			return riGuid;
		}

		/// <summary />
		protected override IEnumerable<int> GetObjectSet()
		{
			IReversalIndex ri = m_owningObject as IReversalIndex;
			Debug.Assert(ri != null && ri.IsValidObject, "The owning IReversalIndex object is invalid!?");
			// Review: is there a better to to convert from List<Subclass> to List<Class>???
			List<IReversalIndexEntry> rgrie = ri.AllEntries;
			var rgcmo = new List<int>(rgrie.Count);
			foreach (IReversalIndexEntry rie in rgrie)
				rgcmo.Add(rie.Hvo);
			return rgcmo;
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// </summary>
		public override void DeleteCurrentObject(ProgressState state, ICmObject thingToDelete)
		{
			CheckDisposed();

			base.DeleteCurrentObject(state, thingToDelete);

			ReloadList();
		}

		/// <summary />
		internal protected override string PropertyTableId(string sorterOrFilter)
		{
			var reversalPub = PropertyTable.GetValue<string>("ReversalIndexPublicationLayout");
			if (reversalPub == null)
				return null; // there is no current Reversal Index; don't try to find Properties (sorter & filter) for a nonexistant Reversal Index
			var reversalLang = reversalPub.Substring(reversalPub.IndexOf('-') + 1); // strip initial "publishReversal-"

			// Dependent lists do not have owner/property set. Rather they have class/field.
			string className = VirtualListPublisher.MetaDataCache.GetOwnClsName((int)m_flid);
			string fieldName = VirtualListPublisher.MetaDataCache.GetFieldName((int)m_flid);
			if (String.IsNullOrEmpty(PropertyName) || PropertyName == fieldName)
			{
				return String.Format("{0}.{1}-{2}_{3}", className, fieldName, reversalLang, sorterOrFilter);
			}
			else
			{
				return String.Format("{0}.{1}-{2}_{3}", className, PropertyName, reversalLang, sorterOrFilter);
			}
		}
	}
}
