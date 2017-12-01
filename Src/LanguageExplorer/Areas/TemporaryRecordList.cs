// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Works;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas
{
	internal class TemporaryRecordList : RecordList
	{
		/// <summary />
		internal TemporaryRecordList(string id, StatusBar statusBar, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
			: base(id, statusBar, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
		}
		/// <summary />
		internal TemporaryRecordList(string id, StatusBar statusBar, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, ICmObject owner, string propertyName)
			: base(id, statusBar, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion, decorator, usingAnalysisWs, flid, owner, propertyName)
		{
		}

		#region Overrides of RecordList

		public override void ActivateUI(bool useRecordTreeBar, bool updateStatusBar = true)
		{
			// by default, we won't publish that we're the "RecordClerk.RecordClerkRepository.ActiveRecordClerk" or other usual effects.
			// but we do want to say that we're being actively used in a gui.
			_isActiveInGui = true;
		}

		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return true; // assume this will be true, say for instance in the context of a dialog.
			}
			set
			{
				// Do not do anything here, unless you want to manage the "RecordClerk.RecordClerkRepository.ActiveRecordClerk" property.
			}
		}

		public override void OnPropertyChanged(string name)
		{
			// Objects of this class do not respond to property changes.
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			EnableSendPropChanged = false;
		}

		#endregion Overrides of RecordList
	}
}
