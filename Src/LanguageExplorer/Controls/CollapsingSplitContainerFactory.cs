// Copyright (c) 2016-2018 SIL International
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
using SIL.LCModel;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Create and destroy a CollapsingSplitContainerFactory instance.
	/// </summary>
	internal static class CollapsingSplitContainerFactory
	{
		internal const int BasicSecondCollapseZoneWidth = 144000;

		/// <summary>
		/// Create an instance of a CollapsingSplitContainer for use by tools that are for CmPossibilities.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="dataNavigationManager"></param>
		/// <param name="recordClerkRepository"></param>
		/// <param name="mainCollapsingSplitContainer">The window's main CollapsingSplitContainer</param>
		/// <param name="verticalSplitter">'true' to have a vertical splitter or 'false' to have a horizontal splitter.</param>
		/// <param name="configurationParametersElement">Main parameters element.</param>
		/// <param name="sliceFilterDocument">Document that has Slice filtering information.</param>
		/// <param name="toolMachineName">Name of the tool being set up.</param>
		/// <param name="possibilityListClerkParameters">parameter object of data needed to create the clerk and its record list.</param>
		/// <param name="cache">The LCM cache.</param>
		/// <param name="recordClerk">Output the RecordClerk, so caller can use it more easily.</param>
		/// <returns>A new instance of CollapsingSplitContainer, which has been placed into "SecondControl/Panel2" of <paramref name="mainCollapsingSplitContainer"/>.</returns>
		internal static CollapsingSplitContainer Create(FlexComponentParameters flexComponentParameters,
			DataNavigationManager dataNavigationManager, IRecordClerkRepository recordClerkRepository,
			ICollapsingSplitContainer mainCollapsingSplitContainer, bool verticalSplitter, XElement configurationParametersElement, XDocument sliceFilterDocument,
			string toolMachineName,
			PossibilityListClerkParameters possibilityListClerkParameters,
			LcmCache cache,
			ref RecordClerk recordClerk)
		{
			if (recordClerk == null)
			{
				recordClerk = ListsArea.CreateBasicClerkForListArea(flexComponentParameters.PropertyTable,
					possibilityListClerkParameters);
				// It is initialized in the following "Create" method.
				recordClerkRepository.AddRecordClerk(recordClerk);
			}
			else
			{
				recordClerk = recordClerkRepository.GetRecordClerk(possibilityListClerkParameters.ClerkIdentifier);
			}
			var retVal = Create(flexComponentParameters, mainCollapsingSplitContainer, verticalSplitter, configurationParametersElement, sliceFilterDocument, toolMachineName, cache, ref recordClerk);
			dataNavigationManager.Clerk = recordClerk;
			recordClerkRepository.ActiveRecordClerk = recordClerk;
			return retVal;
		}

		/// <summary>
		/// Create an instance of a CollapsingSplitContainer.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">The window's main CollapsingSplitContainer</param>
		/// <param name="verticalSplitter">'true' to have a vertical splitter or 'false' to have a horizontal splitter.</param>
		/// <param name="configurationParametersElement">Main parameters element.</param>
		/// <param name="sliceFilterDocument">Document that has Slice filtering information.</param>
		/// <param name="toolMachineName">Name of the tool being set up.</param>
		/// <param name="cache">The LCM cache.</param>
		/// <param name="recordClerk">RecordClerk to use with the container.</param>
		/// <returns>A new instance of CollapsingSplitContainer, which has been placed into "SecondControl/Panel2" of <paramref name="mainCollapsingSplitContainer"/>.</returns>
		internal static CollapsingSplitContainer Create(FlexComponentParameters flexComponentParameters,
			ICollapsingSplitContainer mainCollapsingSplitContainer, bool verticalSplitter, XElement configurationParametersElement, XDocument sliceFilterDocument,
			string toolMachineName,
			LcmCache cache,
			ref RecordClerk recordClerk)
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

			recordClerk.InitializeFlexComponent(flexComponentParameters);
			var recordBar = new RecordBar(flexComponentParameters.PropertyTable)
			{
				IsFlatList = false
			};
			var recordEditView = new RecordEditView(configurationParametersElement, sliceFilterDocument, cache, recordClerk);
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

			newCollapsingSplitContainer.SplitterDistance = flexComponentParameters.PropertyTable.GetValue<int>("RecordListWidthGlobal", SettingsGroup.GlobalSettings);
			mainCollapsingSplitContainer.SecondControl = newCollapsingSplitContainer;
			panelButton.DatTree = recordEditView.DatTree;
			recordEditView.FinishInitialization();

			return newCollapsingSplitContainer;
		}

		/// <summary>
		/// Remove <paramref name="collapsingSplitContainer"/> from parent control and dispose it and set clerk to null.
		/// </summary>
		internal static void RemoveFromParentAndDispose(ICollapsingSplitContainer mainCollapsingSplitContainer, DataNavigationManager dataNavigationManager, IRecordClerkRepository recordClerkRepository, ref CollapsingSplitContainer collapsingSplitContainer)
		{
			// Re-setting SecondControl, will dispose the child collapsingSplitContainer control.
			mainCollapsingSplitContainer.SecondControl = null;
			dataNavigationManager.Clerk = null;
			recordClerkRepository.ActiveRecordClerk = null;
			collapsingSplitContainer = null;
		}
	}
}