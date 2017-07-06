// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.XWorks;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal sealed class RespellerRecordList : RecordList
	{
#if RANDYTODO
		// TODO: Use this constructor or another one of the ones from superclass.
#endif
		/// <summary />
		internal RespellerRecordList(ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, ICmObject owner, string propertyName)
			: base(decorator, usingAnalysisWs, flid, owner, propertyName)
		{
		}

#if RANDYTODO
		public override void Init(XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList class="WfiWordform" field="Occurrences"/>
			BaseInit(recordListNode);
			var mdc = VirtualListPublisher.MetaDataCache;
			m_flid = mdc.GetFieldId2(WfiWordformTags.kClassId, "Occurrences", false);
			Sorter = new OccurrenceSorter
			{
				Cache = PropertyTable.GetValue<FdoCache>("cache"),
				SpecialDataAccess = VirtualListPublisher
			};
		}
#endif

		public override void ReloadList()
		{
			base.ReloadList();
			if (SortedObjects.Count > 0)
			{
				CurrentIndex = 0;
			}
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			// get the list from our decorated SDA.
			int[] objs = VirtualListPublisher.VecProp(m_owningObject.Hvo, m_flid);
			// copy the list to where it's expected to be found. (should this be necessary?)
			int chvo = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, VirtualFlid);
			VirtualListPublisher.Replace(m_owningObject.Hvo, VirtualFlid, 0, chvo, objs, objs.Length);
			return objs;
		}

		#region IVwNotifyChange implementation

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_owningObject != null && hvo == m_owningObject.Hvo && tag == m_flid)
			{
				ReloadList();
			}
		}

		#endregion IVwNotifyChange implementation
	}
}