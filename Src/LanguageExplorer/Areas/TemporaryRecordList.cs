// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas
{
	internal abstract class TemporaryRecordList : RecordList
	{
		/// <summary />
		internal TemporaryRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, RecordFilterParameterObject recordFilterParameterObject = null, RecordSorter defaultSorter = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject, defaultSorter)
		{
		}

		#region Overrides of RecordList

		public override void ActivateUI(bool updateStatusBar = true)
		{
			// by default, we won't publish that we're the "RecordList.RecordListRepository.ActiveRecordList" or other usual effects.
			// but we do want to say that we're being actively used in a gui.
			IsActiveInGui = true;
		}

		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return true; // assume this will be true, say for instance in the context of a dialog.
			}
			set
			{
				// Do not do anything here, unless you want to manage the "RecordList.RecordListRepository.ActiveRecordList" property.
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

			m_fEnableSendPropChanged = false;
		}

		#endregion Overrides of RecordList
	}
}