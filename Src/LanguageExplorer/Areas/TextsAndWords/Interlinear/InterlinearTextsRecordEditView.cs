// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal sealed class InterlinearTextsRecordEditView : RecordEditView
	{
		public InterlinearTextsRecordEditView(InfoPane infoPane, XElement configurationParametersElement, LcmCache cache, IRecordList recordList, DataTree dataTree, ToolStripMenuItem printMenu)
			: base(configurationParametersElement, XDocument.Parse(AreaResources.VisibilityFilter_All), cache, recordList, dataTree, printMenu)
		{
			(DatTree as StTextDataTree).InfoPane = infoPane;
		}

		#region Overrides of RecordEditView
		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			ReadParameters();
			SetupDataContext();
			ShowRecord();
		}
		#endregion
	}
}