﻿// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary>
	/// ITool implementation for the "reversalEditComplete" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class ReversalEditCompleteTool : ITool
	{
		private ReversalEditCompleteToolMenuHelper _reversalEditCompleteToolMenuHelper;
		private const string panelMenuId = "left";
		private LcmCache _cache;
		private MultiPane _multiPane;
		private IRecordList _recordList;
		private XhtmlDocView _xhtmlDocView;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		private DataTreeStackContextMenuFactory _dataTreeStackContextMenuFactory;
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
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_reversalEditCompleteToolMenuHelper.Dispose();

			_cache = null;
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_xhtmlDocView = null;
			_dataTreeStackContextMenuFactory = null;
			_reversalEditCompleteToolMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_cache = majorFlexComponentParameters.LcmCache;
			var currentGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(_propertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(LexiconArea.AllReversalEntries, majorFlexComponentParameters.Statusbar, LexiconArea.AllReversalEntriesFactoryMethod);
			}

			var root = XDocument.Parse(LexiconResources.ReversalEditCompleteToolParameters).Root;
			_xhtmlDocView = new XhtmlDocView(root.Element("docview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			_reversalEditCompleteToolMenuHelper = new ReversalEditCompleteToolMenuHelper(majorFlexComponentParameters, _xhtmlDocView, _recordList);
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree();
			dataTree.DataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreateMainPanelContextMenuStrip);
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.HideAdvancedListItemFields), majorFlexComponentParameters.LcmCache, _recordList, dataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "ReversalIndexItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};
			var docViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu(dataTree.DataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory, panelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			docViewPaneBar.AddControls(new List<Control> { panelMenu });
			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_xhtmlDocView, "Doc Reversals", docViewPaneBar, // XhtmlDocView
				recordEditView, "Browse Entries", recordEditViewPaneBar); // RecordEditView

			_xhtmlDocView.FinishInitialization();
			panelButton.MyDataTree = recordEditView.MyDataTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			_reversalEditCompleteToolMenuHelper.Initialize();
			_xhtmlDocView.OnPropertyChanged("ReversalIndexPublicationLayout");
			((IPostLayoutInit)_multiPane).PostLayoutInit();
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
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_xhtmlDocView.PublicationDecorator.Refresh();
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
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
		public string MachineName => AreaServices.ReversalEditCompleteMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "BUGGY: Reversal Indexes";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			var contextMenuStrip = new ContextMenuStrip();

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);

			if (_reversalIndexRepository == null)
			{
				_reversalIndexRepository = _cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			}
			var allInstancesinRepository = _reversalIndexRepository.AllInstances().ToDictionary(rei => rei.Guid);
			foreach (var rei in allInstancesinRepository.Values)
			{
				var newMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ReversalIndex_Menu_Clicked, rei.ChooserNameTS.Text);
				newMenuItem.Tag = rei;
			}

			return retVal;
		}

		private void ReversalIndex_Menu_Clicked(object sender, EventArgs e)
		{
			var contextMenuItem = (ToolStripMenuItem)sender;
			_currentReversalIndex = (IReversalIndex)contextMenuItem.Tag;
			_propertyTable.SetProperty("ReversalIndexGuid", _currentReversalIndex.Guid.ToString(), true, settingsGroup: SettingsGroup.LocalSettings);
			((ReversalListBase)_recordList).ChangeOwningObjectIfPossible();
			SetCheckedState(contextMenuItem);
		}

		private void SetCheckedState(ToolStripMenuItem reversalToolStripMenuItem)
		{
			var currentTag = (IReversalIndex)reversalToolStripMenuItem.Tag;
			reversalToolStripMenuItem.Checked = (currentTag.Guid.ToString() == _propertyTable.GetValue<string>("ReversalIndexGuid"));
		}
	}
}