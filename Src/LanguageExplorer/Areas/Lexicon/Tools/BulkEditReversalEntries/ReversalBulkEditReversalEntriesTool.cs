// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Areas.Lexicon.Reversals;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditReversalEntries
{
	/// <summary>
	/// ITool implementation for the "reversalBulkEditReversalEntries" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class ReversalBulkEditReversalEntriesTool : ITool
	{
		private ReversalBulkToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		private LcmCache _cache;
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
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose these after the main UI stuff.
			_toolMenuHelper.Dispose();

			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_recordBrowseView = null;
			_cache = null;
			_toolMenuHelper = null;
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
			_reversalIndexRepository = _cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			_reversalIndexRepository.EnsureReversalIndicesExist(_cache, _propertyTable);
			var currentGuid = ReversalIndexServices.GetObjectGuidIfValid(_propertyTable, LanguageExplorerConstants.ReversalIndexGuid);
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LanguageExplorerConstants.AllReversalEntries, majorFlexComponentParameters.StatusBar, ReversalServices.AllReversalEntriesFactoryMethod);
			}
			_recordBrowseView = new RecordBrowseView(XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new ReversalBulkToolMenuHelper(majorFlexComponentParameters, this, _reversalIndexRepository, _currentReversalIndex, _recordBrowseView, _recordList);
			var browseViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu(AreaServices.LeftPanelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			browseViewPaneBar.AddControls(new List<Control> { panelMenu });

			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, _recordBrowseView, browseViewPaneBar);
			_toolMenuHelper.InitializePanelMenu(panelMenu);
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
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.ReversalBulkEditReversalEntriesMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.ReversalBulkEditReversalEntriesUiName);

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

		private sealed class ReversalBulkToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private CommonReversalIndexMenuHelper _commonReversalIndexMenuHelper;
			private PanelMenuContextMenuFactory _mainPanelMenuContextMenuFactory;
			private IReversalIndexRepository _reversalIndexRepository;
			private IReversalIndex _currentReversalIndex;
			private IRecordList _recordList;
			private IPropertyTable _propertyTable;
			private RecordBrowseView _recordBrowseView;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;

			internal ReversalBulkToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IReversalIndexRepository reversalIndexRepository, IReversalIndex currentReversalIndex, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(reversalIndexRepository, nameof(reversalIndexRepository));
				Guard.AgainstNull(currentReversalIndex, nameof(currentReversalIndex));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_reversalIndexRepository = reversalIndexRepository;
				_currentReversalIndex = currentReversalIndex;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				SetupUiWidgets(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				CreateBrowseViewContextMenu();
			}

			private void SetupUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
			{
				_commonReversalIndexMenuHelper = new CommonReversalIndexMenuHelper(_majorFlexComponentParameters, _recordList);
				_commonReversalIndexMenuHelper.SetupUiWidgets(toolUiWidgetParameterObject);
				_mainPanelMenuContextMenuFactory = new PanelMenuContextMenuFactory(); // Make our own, since the tool has no data tree.
				_mainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.LeftPanelMenuId, CreatePanelContextMenuStrip);
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("ReversalIndexEntry", StringTable.ClassNames)));
				contextMenuStrip.Opening += ContextMenuStrip_Opening;

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
			{
				_recordBrowseView.ContextMenuStrip.Visible = !_recordList.HasEmptyList;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreatePanelContextMenuStrip(string panelMenuId)
			{
				if (_reversalIndexRepository == null)
				{
					_reversalIndexRepository = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				}
				var contextMenuStrip = new ContextMenuStrip();
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
				// If allInstancesinRepository has any remaining instances, then they are not in the menu. Add them.
				foreach (var rei in _reversalIndexRepository.AllInstances())
				{
					var newMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ReversalIndex_Menu_Clicked, rei.ChooserNameTS.Text);
					newMenuItem.Tag = rei;
					if (rei == _currentReversalIndex)
					{
						if (_currentReversalIndex == rei)
						{
							newMenuItem.Checked = true;
						}
					}
				}
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ConfigureDictionary_Clicked, LexiconResources.ConfigureDictionary);

				return retVal;
			}

			private void ConfigureDictionary_Clicked(object sender, EventArgs e)
			{
				var mainWindow = _propertyTable.GetValue<IFwMainWnd>(FwUtils.window);
				if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)mainWindow, _recordList?.CurrentObject, "khtpConfigureReversalIndex", LanguageExplorerResources.Dictionary))
				{
					mainWindow.RefreshAllViews();
				}
			}

			private void ReversalIndex_Menu_Clicked(object sender, EventArgs e)
			{
				var contextMenuItem = (ToolStripMenuItem)sender;
				_currentReversalIndex = (IReversalIndex)contextMenuItem.Tag;
				_propertyTable.SetProperty(LanguageExplorerConstants.ReversalIndexGuid, _currentReversalIndex.Guid.ToString(), true, settingsGroup: SettingsGroup.LocalSettings);
				((ReversalListBase)_recordList).ChangeOwningObjectIfPossible();
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~ReversalBulkToolMenuHelper()
			{
				// The base class finalizer is called automatically.
				Dispose(false);
			}

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (_isDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					_commonReversalIndexMenuHelper.Dispose();
					_mainPanelMenuContextMenuFactory.Dispose(); // No Data Tree in this tool to dispose of it for us.
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
					_partiallySharedForToolsWideMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_commonReversalIndexMenuHelper = null;
				_mainPanelMenuContextMenuFactory = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_recordBrowseView = null;
				_recordList = null;
				_propertyTable = null;

				_isDisposed = true;
			}
			#endregion

			internal void InitializePanelMenu(PanelMenu panelMenu)
			{
				panelMenu.DataTreeMainPanelContextMenuFactory = _mainPanelMenuContextMenuFactory;
			}
		}
	}
}