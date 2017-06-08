// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lists;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Create and destroy a CollapsingSplitContainerFactory instance.
	/// </summary>
	internal static class CollapsingSplitContainerFactory
	{
		private const int BasicSecondCollapseZoneWidth = 144000;

		/// <summary>
		/// Create an instance of a CollapsingSplitContainer.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">The window's main CollapsingSplitContainer</param>
		/// <param name="verticalSplitter">'true' to have a vertical splitter or 'false' to have a horizontal splitter.</param>
		/// <param name="configurationParametersElement">Main parameters element.</param>
		/// <param name="sliceFilterDocument">Document that has Slice filtering information.</param>
		/// <param name="toolMachineName">Name of the tool being set up.</param>
		/// <param name="possibilityListClerkParameters">parameter object of data needed to create the clerk and it record list.</param>
		/// <param name="recordClerk">Output the RecordClerk, so caller can use it more easily.</param>
		/// <returns>A new instance of CollapsingSplitContainer, which has been placed into "SecondControl/Panel2" of <paramref name="mainCollapsingSplitContainer"/>.</returns>
		internal static CollapsingSplitContainer Create(FlexComponentParameters flexComponentParameters,
			ICollapsingSplitContainer mainCollapsingSplitContainer, bool verticalSplitter, XElement configurationParametersElement, XDocument sliceFilterDocument,
			string toolMachineName,
			PossibilityListClerkParameters possibilityListClerkParameters,
			out RecordClerk recordClerk)
		{
			var panelButton = new PanelButton(flexComponentParameters.PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(toolMachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			var parentSplitterPanelControl = mainCollapsingSplitContainer.SecondPanel;

			parentSplitterPanelControl.SuspendLayout();
			foreach (Control childControl in parentSplitterPanelControl.Controls)
			{
				childControl.Dispose();
			}
			parentSplitterPanelControl.Controls.Clear();

			recordClerk = ListsArea.CreateBasicClerkForListArea(flexComponentParameters.PropertyTable, possibilityListClerkParameters);
			recordClerk.InitializeFlexComponent(flexComponentParameters);
			var recordBar = new RecordBar
			{
				IsFlatList = false
			};
			recordBar.Clear();
			var recordEditView = new RecordEditView(configurationParametersElement, sliceFilterDocument, recordClerk);
			recordEditView.InitializeFlexComponent(flexComponentParameters);
			var paneBar = new PaneBar.PaneBar();
			paneBar.AddControls(new List<Control> { panelButton });

			var paneBarContainer = new PaneBarContainer(paneBar, recordEditView);
			var panel2ChildControlAsControl = (Control)paneBarContainer;

			var newCollapsingSplitContainer = new CollapsingSplitContainer
			{
				SecondCollapseZone = BasicSecondCollapseZoneWidth
			};
			newCollapsingSplitContainer.SuspendLayout();

			newCollapsingSplitContainer.Orientation = verticalSplitter ? Orientation.Vertical : Orientation.Horizontal;
			newCollapsingSplitContainer.FirstControl = recordBar;
			newCollapsingSplitContainer.FirstLabel = AreaResources.ksRecordListLabel;
			newCollapsingSplitContainer.SecondControl = panel2ChildControlAsControl;
			newCollapsingSplitContainer.SecondLabel = AreaResources.ksMainContentLabel;
			parentSplitterPanelControl.Controls.Add(newCollapsingSplitContainer);
			newCollapsingSplitContainer.Dock = DockStyle.Fill;
			paneBarContainer.InitializeFlexComponent(flexComponentParameters);

			newCollapsingSplitContainer.ResumeLayout();
			parentSplitterPanelControl.ResumeLayout();
			panelButton.BringToFront();
			recordBar.BringToFront();
			panel2ChildControlAsControl.BringToFront();

			newCollapsingSplitContainer.SplitterDistance = flexComponentParameters.PropertyTable.GetValue<int>("RecordListWidthGlobal");
			mainCollapsingSplitContainer.SecondControl = newCollapsingSplitContainer;
			panelButton.DatTree = recordEditView.DatTree;
			recordEditView.FinishInitialization();

			return newCollapsingSplitContainer;
		}

		/// <summary>
		/// Remove <paramref name="collapsingSplitContainer"/> from parent control and dispose it.
		/// </summary>
		/// <param name="mainCollapsingSplitContainer"></param>
		/// <param name="collapsingSplitContainer">The CollapsingSplitContainer to remove and dispose.</param>
		/// <param name="recordClerk">The RecordClerk data member to set to null.</param>
		internal static void RemoveFromParentAndDispose(ICollapsingSplitContainer mainCollapsingSplitContainer, ref CollapsingSplitContainer collapsingSplitContainer, ref RecordClerk recordClerk)
		{
			// Re-setting SecondControl, will dispose the child collapsingSplitContainer control.
			mainCollapsingSplitContainer.SecondControl = null;

			collapsingSplitContainer = null;

			// recordClerk is disposed by XWorksViewBase in the call "collapsingSplitContainer.Dispose()", but just set the variable to null here.
			// "recordClerk" is a data member of the caller. Rather than have every caller set its own data member to null,
			// we do it here for all of them.
			recordClerk = null;
		}
	}
}