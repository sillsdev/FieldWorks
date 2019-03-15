// Copyright (c) 2012-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.BulkEditPhonemes
{
	/// <summary />
	internal partial class AssignFeaturesToPhonemes : RecordBrowseView
	{
		/// <summary />
		public AssignFeaturesToPhonemes()
		{
			InitializeComponent();
		}

		/// <summary />
		public AssignFeaturesToPhonemes(UiWidgetController uiWidgetController, XElement browseViewDefinitions, BrowseViewContextMenuFactory browseViewContextMenuFactory, LcmCache cache, IRecordList recordList)
			: base(uiWidgetController, browseViewDefinitions, browseViewContextMenuFactory, cache, recordList)
		{
			InitializeComponent();
		}

		#region Overrides of RecordBrowseView

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			var bulkEditBar = BrowseViewer.BulkEditBar;
			// We want a custom name for the tab, the operation label, and the target item
			// Now we use good old List Choice.  bulkEditBar.ListChoiceTab.Text = LanguageExplorerResources.ksAssignFeaturesToPhonemes;
			bulkEditBar.OperationLabel.Text = LanguageExplorerResources.ksListChoiceDesc;
			bulkEditBar.TargetFieldLabel.Text = LanguageExplorerResources.ksTargetFeature;
			bulkEditBar.ChangeToLabel.Text = LanguageExplorerResources.ksChangeTo;
		}

		#endregion

		protected override BrowseViewer CreateBrowseViewer(UiWidgetController uiWidgetController, XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			return new BrowseViewerPhonologicalFeatures(uiWidgetController, nodeSpec, hvoRoot, cache, sortItemProvider, sda);
		}
	}
}