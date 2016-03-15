// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lists;
using LanguageExplorer.Controls.PaneBar;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
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
		/// <param name="propertyTable">The property table</param>
		/// <param name="publisher">The Publisher</param>
		/// <param name="subscriber">The Subscriber</param>
		/// <param name="mainCollapsingSplitContainer">The window's main CollapsingSplitContainer</param>
		/// <param name="verticalSplitter">'true' to have a vertical splitter or 'false' to have a horizontal splitter.</param>
		/// <param name="configurationParametersElement">Main parameters element.</param>
		/// <param name="sliceFilterDocument">Document that has Slice filtering information.</param>
		/// <param name="toolMachineName">Name of the tool being set up.</param>
		/// <param name="clerkIdentifier">Identifier for new RecordClerk.</param>
		/// <param name="owningList">Possibility list the owns itmes being shown/edited.</param>
		/// <param name="expand">'true' to expand the tree view or 'false' to only show top level of items.</param>
		/// <param name="hierarchical">'true' if the list has sub-possibilities or 'false' for a flat list.</param>
		/// <param name="includeAbbr">'true' to show possibility abbreviations, otherwise 'false' to only show full names.</param>
		/// <param name="ws">Writing System used to dispaly the </param>
		/// <returns>A new instance of CollapsingSplitContainer, which has been placed into "SecondControl/Panel2" of <paramref name="mainCollapsingSplitContainer"/>.</returns>
		internal static CollapsingSplitContainer Create(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber,
			ICollapsingSplitContainer mainCollapsingSplitContainer, bool verticalSplitter, XElement configurationParametersElement, XDocument sliceFilterDocument,
			string toolMachineName,
			string clerkIdentifier, ICmPossibilityList owningList, bool expand, bool hierarchical, bool includeAbbr, string ws)
		{
			var panelButton = new PanelButton(propertyTable, null, ListsArea.CreateShowHiddenFieldsPropertyName(toolMachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
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

			var recordClerk = ListsArea.CreateBasicClerkForListArea(propertyTable, clerkIdentifier,
				owningList,
				expand, hierarchical, includeAbbr, ws);
			recordClerk.InitializeFlexComponent(propertyTable, publisher, subscriber);
			var recordBar = new RecordBar
			{
				IsFlatList = false
			};
			recordBar.Clear();
			var recordEditView = new RecordEditView(configurationParametersElement, sliceFilterDocument, recordBar.TreeView, recordClerk);
			recordEditView.InitializeFlexComponent(propertyTable, publisher, subscriber);
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
			paneBarContainer.InitializeFlexComponent(propertyTable, publisher, subscriber);

			newCollapsingSplitContainer.ResumeLayout();
			parentSplitterPanelControl.ResumeLayout();
			panelButton.BringToFront();
			recordBar.BringToFront();
			panel2ChildControlAsControl.BringToFront();

			newCollapsingSplitContainer.SplitterDistance = propertyTable.GetValue<int>("RecordListWidthGlobal");
			mainCollapsingSplitContainer.SecondControl = newCollapsingSplitContainer;
			recordEditView.MainPaneBar = paneBarContainer.PaneBar;
			panelButton.DatTree = recordEditView.DatTree;
			recordEditView.FinishInitialization();

			return newCollapsingSplitContainer;
		}

		/// <summary>
		/// Remove <paramref name="collapsingSplitContainer"/> from parent control and dispose it.
		/// </summary>
		/// <param name="collapsingSplitContainer">The CollapsingSplitContainer to remove and dispose.</param>
		internal static void RemoveFromParentAndDispose(ref CollapsingSplitContainer collapsingSplitContainer)
		{
			var parentCollapsingSplitContainer = (CollapsingSplitContainer)collapsingSplitContainer.Parent.Parent;
			var parentControl = parentCollapsingSplitContainer.Panel2;
			parentControl.SuspendLayout();
			parentControl.Controls.Remove(collapsingSplitContainer);
			collapsingSplitContainer.Dispose();
			// Add a temporary placeholder Panel in main splitter's right pane.
			// "SecondControl" cannot be set to null, and its old value ("collapsingSplitContainer") has now been disposed.
			// If another controll is added as "SecondControl", the temporary Panel will be disposed.
			parentCollapsingSplitContainer.SecondControl = new Panel();
			parentControl.ResumeLayout();

			collapsingSplitContainer = null;
		}
	}
}