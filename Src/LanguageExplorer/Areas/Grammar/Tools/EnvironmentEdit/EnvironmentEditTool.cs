// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Grammar.Tools.EnvironmentEdit
{
	/// <summary>
	/// ITool implementation for the "EnvironmentEdit" tool in the "grammar" area.
	/// </summary>
	internal sealed class EnvironmentEditTool : ITool
	{
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			MultiPaneFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				ref _multiPane,
				ref _recordClerk);
			_recordBrowseView = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var doc = XDocument.Parse(GrammarResources.EnvironmentEditToolParameters);
			var cache = PropertyTable.GetValue<FdoCache>("cache");
			_recordClerk = new RecordClerk("environments", new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true, PhPhonDataTags.kflidEnvironments, cache.LanguageProject.PhonologicalDataOA, "Environments"), new PropertyRecordSorter("ShortName"), "Default", null, false, false);
			_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			_recordBrowseView = new RecordBrowseView(doc.Root.Element("browseview").Element("parameters"), _recordClerk);
#if RANDYTODO
			// TODO: Set up 'dataTreeMenuHandler' to handle menu events.
			// TODO: Install menus and connect them to event handlers. (See "CreateContextMenuStrip" method for where the menus are.)
#endif
			var recordEditView = new RecordEditView(doc.Root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.CompleteFilter), _recordClerk);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "EnvironmentItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};

			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_recordBrowseView, "Browse", new PaneBar(),
				recordEditView, "Details", recordEditViewPaneBar);

			panelButton.DatTree = recordEditView.DatTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_recordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerk.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => "EnvironmentEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Environments";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "grammar";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}
