// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	/// <remarks>
	/// Used only by LanguageExplorer.Areas.TextsAndWords.RespellerDlg
	/// </remarks>
	internal sealed class RespellerTemporaryRecordList : TemporaryRecordList
	{
		/// <summary />
		internal RespellerTemporaryRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, RecordFilterParameterObject recordFilterParameterObject = null, RecordSorter defaultSorter = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject, defaultSorter)
		{
		}

		#region Overrides of TemporaryRecordList/RecordList

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			var mdc = VirtualListPublisher.MetaDataCache;
			m_flid = mdc.GetFieldId2(WfiWordformTags.kClassId, "Occurrences", false);
			Sorter = new OccurrenceSorter
			{
				Cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache),
				SpecialDataAccess = VirtualListPublisher
			};
		}

		protected override void ReloadList()
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
			var objs = VirtualListPublisher.VecProp(OwningObject.Hvo, m_flid);
			// copy the list to where it's expected to be found. (should this be necessary?)
			var chvo = VirtualListPublisher.get_VecSize(OwningObject.Hvo, VirtualFlid);
			VirtualListPublisher.Replace(OwningObject.Hvo, VirtualFlid, 0, chvo, objs, objs.Length);
			return objs;
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (OwningObject != null && hvo == OwningObject.Hvo && tag == m_flid)
			{
				ReloadList();
			}
		}

		#endregion #region Overrides of TemporaryRecordList/RecordList
	}
}