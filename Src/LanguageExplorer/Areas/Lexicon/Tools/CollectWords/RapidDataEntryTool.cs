﻿// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.CollectWords
{
	/// <summary>
	/// ITool implementation for the "rapidDataEntry" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class RapidDataEntryTool : ITool
	{
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		private BrowseViewContextMenuFactory _browseViewContextMenuFactory;
		internal const string RDEwords = "RDEwords";
		private CollapsingSplitContainer _collapsingSplitContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		private IRecordList _nestedRecordList;
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;
		[Import]
		private IPropertyTable _propertyTable;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetProperty("RecordListWidthGlobal", _collapsingSplitContainer.SplitterDistance, true, settingsGroup: SettingsGroup.GlobalSettings);

#if RANDYTODO
			// If these removals are more permanent, then move up to the "RemoveObsoleteProperties" method on the main window.
			/*
			Q: Jason: "This won't result in us loosing track of current entries when switching between tools will it?
					I can't remember what this property is used for at the moment."
			A: Randy: "One of the changes (not integration stuff like all of these ones) I plan is to get all record
					record list instances to come out of a repository like class, which creates them all and returns them, when requested.
					A tool will then activate them, when the tool is activated, and the tool will deactivate them, when the tool changes.
					That is something like what is done in 'develop' with the PropertyTable (sans creation).
					But, I'd like to see use of PropertyTable reduced to actual properties that are persisted,
					and not as yet another 'God-object' in the code that knows how to get anything (eg. LCMCache, service locator, etc).
					I'm not there yet, but that is where I'd like to go.

					So, I suspect those properties will eventually go away permanently, but I'm not there yet."
			*/
#endif
			_propertyTable.RemoveProperty(RecordList.RecordListSelectedObjectPropertyId(_nestedRecordList.Id));
			_propertyTable.RemoveProperty(RecordList.RecordListSelectedObjectPropertyId(_recordList.Id));

			_propertyTable.RemoveProperty("ActiveListOwningObject");
			_propertyTable.RemoveProperty("ActiveListSelectedObject");

			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);

			// Dispose after the main UI stuff.
			_browseViewContextMenuFactory.Dispose();
			_lexiconAreaMenuHelper.Dispose();

			_recordBrowseView = null;
			_nestedRecordList = null;
			_lexiconAreaMenuHelper = null;
			_browseViewContextMenuFactory = null;
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
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(LexiconArea.SemanticDomainList_LexiconArea, majorFlexComponentParameters.Statusbar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod);
			}
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(majorFlexComponentParameters, _recordList);
			_browseViewContextMenuFactory = new BrowseViewContextMenuFactory();
#if RANDYTODO
			// TODO: Set up factory method for the browse view.
#endif

			var recordBar = new RecordBar(_propertyTable)
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
			_collapsingSplitContainer.SplitterDistance = _propertyTable.GetValue<int>("RecordListWidthGlobal", SettingsGroup.GlobalSettings);

			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });
			var dataTree = new DataTree();
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var recordEditView = new RecordEditView(root.Element("recordeditview").Element("parameters"), XDocument.Parse(LexiconResources.HideAdvancedFeatureFields), majorFlexComponentParameters.LcmCache, _recordList, dataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			if (_nestedRecordList == null)
			{
				_nestedRecordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(RDEwords, majorFlexComponentParameters.Statusbar, RDEwordsFactoryMethod);
			}
			_recordBrowseView = new RecordBrowseView(root.Element("recordbrowseview").Element("parameters"), _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _nestedRecordList);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				Area = _area,
				Id = "SemanticCategoryAndItems",
				ToolMachineName = MachineName,
				DefaultFocusControl = "RecordBrowseView",
				FirstControlParameters = new SplitterChildControlParameters { Control = recordEditView, Label = "Semantic Domain" },
				SecondControlParameters = new SplitterChildControlParameters { Control = _recordBrowseView, Label = "Details" }
			};
			var nestedMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, mainMultiPaneParameters);
			nestedMultiPane.SplitterDistance = _propertyTable.GetValue<int>($"MultiPaneSplitterDistance_{_area.MachineName}_{MachineName}_{mainMultiPaneParameters.Id}");
			_collapsingSplitContainer.SecondControl = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPane, recordEditViewPaneBar);
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = _collapsingSplitContainer;
			_collapsingSplitContainer.ResumeLayout();
			mainCollapsingSplitContainerAsControl.ResumeLayout();
			recordEditView.BringToFront();
			recordBar.BringToFront();

			panelButton.MyDataTree = recordEditView.MyDataTree;

			// Too early before now.
			((ISemanticDomainTreeBarHandler)_recordList.MyTreeBarHandler).FinishInitialization(new PaneBar());
			recordEditView.FinishInitialization();
			_lexiconAreaMenuHelper.Initialize();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("ShowHiddenFields", true);
			}
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
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty("RecordListWidthGlobal", _collapsingSplitContainer.SplitterDistance, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.RapidDataEntryMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Collect Words";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static IRecordList RDEwordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == RDEwords, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{RDEwords}'.");
			/*
            <clerk id="RDEwords" clerkProvidingOwner="SemanticDomainList">
              <recordList class="CmSemanticDomain" field="ReferringSenses" />
              <sortMethods />
            </clerk>
			*/
			return new SubservientRecordList(recordListId, statusBar,
				cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				new VectorPropertyParameterObject(cache.LanguageProject.SemanticDomainListOA, "ReferringSenses", cache.MetaDataCacheAccessor.GetFieldId2(CmSemanticDomainTags.kClassId, "ReferringSenses", false)),
				((IRecordListRepositoryForTools)RecordList.ActiveRecordListRepository).GetRecordList(LexiconArea.SemanticDomainList_LexiconArea, statusBar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod));
		}
	}
}