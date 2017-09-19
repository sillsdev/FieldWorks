// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// ITool implementation for the "lexiconEdit" tool in the "lexicon" area.
	/// </summary>
	internal sealed class LexiconEditTool : ITool
	{
		private LexiconEditToolMenuHelper _lexiconEditToolMenuHelper;
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private MultiPane _innerMultiPane;
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

			PropertyTable.SetDefault($"ToolForAreaNamed_{AreaMachineName}", MachineName, SettingsGroup.LocalSettings, true, false);
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
			_lexiconEditToolMenuHelper.Dispose();

			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
			_innerMultiPane = null;
			_lexiconEditToolMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexiconArea.Entries, majorFlexComponentParameters.Statusbar, LexiconArea.EntriesFactoryMethod);
			}

			var root = XDocument.Parse(LexiconResources.LexiconBrowseParameters).Root;
			// Modify the basic parameters for this tool.
			root.Attribute("id").Value = "lexentryList";
			root.Add(new XAttribute("defaultCursor", "Arrow"), new XAttribute("hscroll", "true"));

			var overrides = XElement.Parse(LexiconResources.LexiconBrowseOverrides);
			// Add one more element to 'overrides'.
			overrides.Add(new XElement("column", new XAttribute("layout", "DefinitionsForSense"), new XAttribute("visibility", "menu")));
			var columnsElement = XElement.Parse(LexiconResources.LexiconBrowseDialogColumnDefinitions);
			OverrideServices.OverrideVisibiltyAttributes(columnsElement, overrides);
			root.Add(columnsElement);

			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordClerk);

			 var dataTree = new DataTree();
			_lexiconEditToolMenuHelper = new LexiconEditToolMenuHelper(majorFlexComponentParameters, dataTree, _recordClerk);

			var recordEditView = new RecordEditView(XElement.Parse(LexiconResources.LexiconEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordClerk, dataTree);
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				AreaMachineName = AreaMachineName,
				DefaultFixedPaneSizePoints = "60",
				Id = "TestEditMulti",
				ToolMachineName = MachineName,
				FirstControlParameters = new SplitterChildControlParameters
				{
					Control = new RecordDocXmlView(XDocument.Parse(LexiconResources.LexiconEditRecordDocViewParameters).Root, majorFlexComponentParameters.LcmCache, _recordClerk, (StatusBarProgressPanel)majorFlexComponentParameters.Statusbar.Panels[LanguageExplorerConstants.StatusBarPanelProgressBar]), Label = "Dictionary"
				},
				SecondControlParameters = new SplitterChildControlParameters
				{
					Control = recordEditView, Label = "Details"
				}
			};
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "LexItemsAndDetailMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "DictionaryPubPreview"
			};
			var paneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);

			var panelMenu = new PanelMenu(_lexiconEditToolMenuHelper.SliceContextMenuFactory, LexiconEditToolMenuHelper.panelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			paneBar.AddControls(new List<Control> { panelMenu, panelButton });
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_recordBrowseView, "Browse", new PaneBar(),
				_innerMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPaneParameters), "Dictionary & Details", paneBar);
			_innerMultiPane.Panel1Collapsed = !PropertyTable.GetValue<bool>(LexiconEditToolMenuHelper.Show_DictionaryPubPreview);
			_lexiconEditToolMenuHelper.InnerMultiPane = _innerMultiPane;
			panelButton.DatTree = recordEditView.DatTree;

			// Too early before now.
			_lexiconEditToolMenuHelper.Initialize();
			recordEditView.FinishInitialization();
			((RecordDocXmlView)nestedMultiPaneParameters.FirstControlParameters.Control).ReallyShowRecordNow();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
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
		public string MachineName => "lexiconEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Lexicon Edit";
#endregion

#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}