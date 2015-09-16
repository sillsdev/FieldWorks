// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
{
#if RANDYTODO
	// TODO: This class is not in the main config files, so never created.
	// TODO: That doesn't prevent two code clients from thinking it *is* used, of course:
	// TODO: XmlDocView & RecordClerk ask if the clerk is SubitemRecordClerk here and there,
	// TODO: but it never is.
#endif
	/// <summary>
	/// A SubItemRecordClerk has an additional notion of the current item. Within the current item of the
	/// RecordList, a smaller item may be selected. For example, the main list may be of top-level
	/// RnGenericRecords, but the SubItemRecordClerk can track owned records.
	/// Currently, the subitem must be owned by the top-level item, and displayed in the document view
	/// using direct owning relationships. Possible subitems are configured by noting the property
	/// that can contain them (possibly recursively).
	/// </summary>
	public class SubitemRecordClerk : RecordClerk
	{
		internal int SubitemFlid { get; private set; }
		private readonly string m_className;
		private readonly string m_fieldName;

		/// <summary>
		/// Contructor.
		/// </summary>
		/// <param name="id">Clerk id/name.</param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		/// <param name="className"></param>
		/// <param name="fieldName"></param>
		internal SubitemRecordClerk(string id, RecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, string className, string fieldName)
			: base(id, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
			if (string.IsNullOrWhiteSpace(className))
			{
				throw new ArgumentNullException("className");
			}
			if (string.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentNullException("fieldName");
			}
			m_className = className.Trim();
			m_fieldName = fieldName.Trim();
		}

		#region Overrides of RecordClerk

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public override void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			base.InitializeFlexComponent(propertyTable, publisher, subscriber);

			SubitemFlid = Cache.MetaDataCacheAccessor.GetFieldId(m_className, m_fieldName, true);
		}

		#endregion

		public ICmObject Subitem { get; set; }
		public bool UsedToSyncRelatedClerk { get; set; }

		internal override void SetSubitem(ICmObject subitem)
		{
			base.SetSubitem(subitem);
			Subitem = subitem;
		}

		internal override void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e, IVwSelection sel)
		{
			base.ViewChangedSelectedRecord(e, sel);
			UsedToSyncRelatedClerk = false;
			if (sel == null)
				return;
			// See if we can make an appropriate Subitem selection.
			var clevels = sel.CLevels(false);
			if (clevels < 2)
				return; // paranoia.
			// The object we get with level = clevels - 1 is the root of the whole view, which is of no interest.
			// The one with clevels - 2 is one of the objects in the top level of the list.
			// We get that initially, along with the tag that determines whether we can drill deeper.
			// Starting with clevels - 3, if there are that many, we keep getting more levels
			// as long as there are some and the previous level had the right tag.
			int hvoObj, tag, ihvo, cpropPrevious;
			IVwPropertyStore vps;
			sel.PropInfo(false, clevels - 2, out hvoObj, out tag, out ihvo,
				out cpropPrevious, out vps);
			int hvoTarget = hvoObj;
			for (int index = clevels - 3; index >= 0 && tag == SubitemFlid; index --)
			{
				sel.PropInfo(false, index, out hvoTarget, out tag, out ihvo,
					out cpropPrevious, out vps);
			}
			if (hvoTarget != hvoObj)
			{
				// we did some useful drilling.
				Subitem = Cache.ServiceLocator.GetObject(hvoTarget);
			}
			else
			{
				Subitem = null; // no relevant subitem.
			}
		}

		protected override void ClearInvalidSubitem()
		{
			if (Subitem == null)
				return; // nothing to do.
			if (!Subitem.IsOwnedBy(CurrentObject))
				Subitem = null; // not valid to try to select it as part of selecting current object.
		}
	}
}
