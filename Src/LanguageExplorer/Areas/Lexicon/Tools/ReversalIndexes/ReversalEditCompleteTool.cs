// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary>
	/// ITool implementation for the "reversalEditComplete" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class ReversalEditCompleteTool : ITool
	{
		private ReversalEditCompleteToolMenuHelper _toolMenuHelper;
		private LcmCache _cache;
		private MultiPane _multiPane;
		private IRecordList _recordList;
		private XhtmlDocView _xhtmlDocView;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
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
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_cache = null;
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_xhtmlDocView = null;
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
			ReversalServices.EnsureReversalIndicesExist(_cache, _propertyTable);
			var currentGuid = RecordListServices.GetObjectGuidIfValid(_propertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LexiconArea.AllReversalEntries, majorFlexComponentParameters.StatusBar, ReversalServices.AllReversalEntriesFactoryMethod);
			}

			var root = XDocument.Parse(LexiconResources.ReversalEditCompleteToolParameters).Root;
			_xhtmlDocView = new XhtmlDocView(root.Element("docview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new ReversalEditCompleteToolMenuHelper(majorFlexComponentParameters, this, _recordList, _xhtmlDocView);
			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_toolMenuHelper.MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.LeftPanelMenuId, CreateMainPanelContextMenuStrip);
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.HideAdvancedListItemFields), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
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
			var panelMenu = new PanelMenu(_toolMenuHelper.MainPanelMenuContextMenuFactory, AreaServices.LeftPanelMenuId)
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

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, mainMultiPaneParameters, _xhtmlDocView, "Doc Reversals", docViewPaneBar, recordEditView, "Browse Entries", recordEditViewPaneBar);

			_xhtmlDocView.FinishInitialization();
			panelButton.MyDataTree = recordEditView.MyDataTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			_xhtmlDocView.OnPropertyChanged("ReversalIndexPublicationLayout");
			((IPostLayoutInit)_multiPane).PostLayoutInit();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish(LanguageExplorerConstants.ShowHiddenFields, true);
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
			reversalToolStripMenuItem.Checked = ((IReversalIndex)reversalToolStripMenuItem.Tag).Guid.ToString() == _propertyTable.GetValue<string>("ReversalIndexGuid");
		}

		private sealed class ReversalEditCompleteToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private CommonReversalIndexMenuHelper _commonReversalIndexMenuHelper;
			private XhtmlDocView _docView;
			internal PanelMenuContextMenuFactory MainPanelMenuContextMenuFactory { get; private set; }

			internal ReversalEditCompleteToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordList, XhtmlDocView docView)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(docView, nameof(docView));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_docView = docView;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => CanCmdFindAndReplaceText));
				_commonReversalIndexMenuHelper = new CommonReversalIndexMenuHelper(_majorFlexComponentParameters, recordList);
				_commonReversalIndexMenuHelper.SetupUiWidgets(toolUiWidgetParameterObject);
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				MainPanelMenuContextMenuFactory = new PanelMenuContextMenuFactory();
#if RANDYTODO
				// TODO: See LexiconEditTool for how to set up all manner of menus and tool bars.
#endif
			}

			private Tuple<bool, bool> CanCmdFindAndReplaceText => new Tuple<bool, bool>(true, true);

			private void EditFindMenu_Click(object sender, EventArgs e)
			{
				_docView.FindText();
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~ReversalEditCompleteToolMenuHelper()
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
					MainPanelMenuContextMenuFactory.Dispose();
					_commonReversalIndexMenuHelper.Dispose();
				}
				MainPanelMenuContextMenuFactory = null;
				_majorFlexComponentParameters = null;
				_commonReversalIndexMenuHelper = null;
				_docView = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}