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
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.CollectWords
{
	/// <summary>
	/// ITool implementation for the "rapidDataEntry" tool in the "lexicon" area.
	/// </summary>
	internal sealed class RapidDataEntryTool : ITool
	{
		internal const string RDEwords = "RDEwords";
		private CollapsingSplitContainer _collapsingSplitContainer;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;
		private RecordClerk _nestedRecordClerk;

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

#if RANDYTODO
			// TODO: Came from Fork commit: "Get all tree-based tools to switch to selected item." 2016-08-12 14:29:42
			// TODO: Wait on other changes from fork that add all the guts to this method.
			var recordBar = new RecordBar(PropertyTable)
			{
				IsFlatList = false,
				Dock = DockStyle.Fill
			};
#endif
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
			PropertyTable.SetProperty("RecordListWidthGlobal", _collapsingSplitContainer.SplitterDistance, SettingsGroup.GlobalSettings, true, false);

#if RANDYTODO
			// If these removals are more permanent, then move up to the "RemoveObsoleteProperties" method on the main window.
			/*
			Q: Jason: "This won't result in us loosing track of current entries when switching between tools will it?
					I can't remember what this property is used for at the moment."
			A: Randy: "One of the changes (not integration stuff like all of these ones) I plan is to get all record
					clerk instances to come out of a repository like class, which creates them all and returns them, when requested.
					A tool will then activate them, when the tool is activated, and the tool will deactivate them, when the tool changes.
					That is something like what is done in 'develop' with the PropertyTable (sans creation).
					But, I'd like to see use of PropertyTable reduced to actual properties that are persisted,
					and not as yet another 'God-object' in the code that knows how to get anything (eg. LCMCache, service locator, etc).
					I'm not there yet, but that is where I'd like to go.

					So, I suspect those properties will eventually go away permanently, but I'm not there yet."
			*/
#endif
			PropertyTable.RemoveProperty(RecordClerk.ClerkSelectedObjectPropertyId(_nestedRecordClerk.Id));
			PropertyTable.RemoveProperty(RecordClerk.ClerkSelectedObjectPropertyId(_recordClerk.Id));

			PropertyTable.RemoveProperty("ActiveClerkOwningObject");
			PropertyTable.RemoveProperty("ActiveClerkSelectedObject");

			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);

			_recordBrowseView = null;
			_nestedRecordClerk = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var mainCollapsingSplitContainerAsControl = (Control)majorFlexComponentParameters.MainCollapsingSplitContainer;
			mainCollapsingSplitContainerAsControl.SuspendLayout();

			var root = XDocument.Parse(LexiconResources.RapidDataEntryToolParameters).Root;
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexiconArea.SemanticDomainList_LexiconArea, majorFlexComponentParameters.Statusbar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod);
			}
			var semanticDomainRdeTreeBarHandler = (SemanticDomainRdeTreeBarHandler)_recordClerk.BarHandler;
			var recordBar = new RecordBar(PropertyTable)
			{
				IsFlatList = false,
				Dock = DockStyle.Fill
			};
			_collapsingSplitContainer = new CollapsingSplitContainer();
			_collapsingSplitContainer.SuspendLayout();
			_collapsingSplitContainer.SecondCollapseZone = CollapsingSplitContainerFactory.BasicSecondCollapseZoneWidth;
			_collapsingSplitContainer.Dock = DockStyle.Fill;
			_collapsingSplitContainer.Orientation = Orientation.Vertical;
			_collapsingSplitContainer.FirstLabel = AreaResources.ksRecordListLabel;
			_collapsingSplitContainer.FirstControl = recordBar;
			_collapsingSplitContainer.SecondLabel = AreaResources.ksMainContentLabel;
			_collapsingSplitContainer.SplitterDistance = PropertyTable.GetValue<int>("RecordListWidthGlobal", SettingsGroup.GlobalSettings);

			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });
			var dataTree = new DataTree();
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var recordEditView = new RecordEditView(root.Element("recordeditview").Element("parameters"), XDocument.Parse(LexiconResources.HideAdvancedFeatureFields), majorFlexComponentParameters.LcmCache, _recordClerk, dataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			if (_nestedRecordClerk == null)
			{
				_nestedRecordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(RDEwords, majorFlexComponentParameters.Statusbar, RDEwordsFactoryMethod);
			}
			_recordBrowseView = new RecordBrowseView(root.Element("recordbrowseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _nestedRecordClerk);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				AreaMachineName = AreaMachineName,
				Id = "SemanticCategoryAndItems",
				ToolMachineName = MachineName,
				DefaultFocusControl = "RecordBrowseView",
				FirstControlParameters = new SplitterChildControlParameters { Control = recordEditView, Label = "Semantic Domain" },
				SecondControlParameters = new SplitterChildControlParameters { Control = _recordBrowseView, Label = "Details" }
			};
			var nestedMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, mainMultiPaneParameters);
			nestedMultiPane.SplitterDistance = PropertyTable.GetValue<int>($"MultiPaneSplitterDistance_{AreaMachineName}_{MachineName}_{mainMultiPaneParameters.Id}");
			_collapsingSplitContainer.SecondControl = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, recordEditViewPaneBar, nestedMultiPane);
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = _collapsingSplitContainer;
			_collapsingSplitContainer.ResumeLayout();
			mainCollapsingSplitContainerAsControl.ResumeLayout();
			recordEditView.BringToFront();
			recordBar.BringToFront();

			panelButton.DatTree = recordEditView.DatTree;

			// Too early before now.
			semanticDomainRdeTreeBarHandler.FinishInitialization(new PaneBar());
			recordEditView.FinishInitialization();
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
			PropertyTable.SetProperty("RecordListWidthGlobal", _collapsingSplitContainer.SplitterDistance, SettingsGroup.GlobalSettings, true, false);
		}

#endregion

#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => "rapidDataEntry";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "MEMORY ISSUES: Collect Words";
#endregion

#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static RecordClerk RDEwordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == RDEwords, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{RDEwords}'.");

			return new RecordClerk(clerkId,
				statusBar,
				new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true, cache.MetaDataCacheAccessor.GetFieldId2(CmSemanticDomainTags.kClassId, "ReferringSenses", false), cache.LanguageProject.SemanticDomainListOA, "ReferringSenses"),
				new PropertyRecordSorter("ShortName"),
				"Default",
				null,
				false,
				false,
				((IRecordClerkRepositoryForTools)RecordClerk.ActiveRecordClerkRepository).GetRecordClerk(LexiconArea.SemanticDomainList_LexiconArea, statusBar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod));
		}
	}
}