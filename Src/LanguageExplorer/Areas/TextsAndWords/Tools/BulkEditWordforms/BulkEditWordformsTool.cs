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
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.BulkEditWordforms
{
	/// <summary>
	/// ITool implementation for the "bulkEditWordforms" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditWordformsTool : ITool
	{
		private BulkEditWordformsToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		[Import(AreaServices.TextAndWordsAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by any of the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_recordBrowseView = null;
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
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.StatusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
			var root = XDocument.Parse(TextAndWordsResources.BulkEditWordformsToolParameters).Root;
			root.Element("includeColumns").ReplaceWith(XElement.Parse(TextAndWordsResources.WordListColumns));
			var columns = root.Element("columns");
			var currentColumn = columns.Elements("column").First(col => col.Attribute("label").Value == "Form");
			currentColumn.Attribute("width").Value = "80000";
			currentColumn.Attribute("ws").Value = "$ws=vernacular";
			currentColumn.Attribute("cansortbylength").Value = "true";
			currentColumn.Add(new XAttribute("transduce", "WfiWordform.Form"));
			currentColumn.Add(new XAttribute("editif", "!FormIsUsedWithWs"));
			currentColumn.Element("span").Element("string").Attribute("ws").Value = "$ws=vernacular";
			currentColumn = columns.Elements("column").First(col => col.Attribute("label").Value == "Word Glosses");
			currentColumn.Attribute("width").Value = "80000";
			currentColumn = columns.Elements("column").First(col => col.Attribute("label").Value == "Spelling Status");
			currentColumn.Add(new XAttribute("width", "65000"));
			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, _recordBrowseView);
			_toolMenuHelper = new BulkEditWordformsToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList);
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
		public string MachineName => AreaServices.BulkEditWordformsMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Bulk Edit Wordforms";
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

		private sealed class BulkEditWordformsToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private FileExportMenuHelper _fileExportMenuHelper;
			private PartiallySharedTextsAndWordsToolsMenuHelper _partiallySharedTextsAndWordsToolsMenuHelper;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
			private List<ToolStripMenuItem> _jumpMenus;

			internal BulkEditWordformsToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(majorFlexComponentParameters, recordList);
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_fileExportMenuHelper = new FileExportMenuHelper(majorFlexComponentParameters);
				_fileExportMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
				_partiallySharedTextsAndWordsToolsMenuHelper = new PartiallySharedTextsAndWordsToolsMenuHelper(majorFlexComponentParameters);
				_partiallySharedTextsAndWordsToolsMenuHelper.AddMenusForExpectedTextAndWordsTools(toolUiWidgetParameterObject);
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				_jumpMenus = new List<ToolStripMenuItem>(2);
				CreateBrowseViewContextMenu();
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

				// <command id="CmdWordformJumpToAnalyses" label="Show in Word Analyses" message="JumpToTool">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Category_Edit);
				menu.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.AnalysesMachineName, _recordList };
				_jumpMenus.Add(menu);

				// <command id="CmdWordformJumpToConcordance" label="Show Wordform in Concordance" message="JumpToTool">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Wordform_in_Concordance);
				menu.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.ConcordanceMachineName, _recordList };
				_jumpMenus.Add(menu);

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("WfiWordform", "ClassNames")));
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

			#region Implementation of IDisposable
			private bool _isDisposed;

			~BulkEditWordformsToolMenuHelper()
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
					_fileExportMenuHelper.Dispose();
					var jumpEventHandler = _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool);
					foreach (var menu in _jumpMenus)
					{
						menu.Click -= jumpEventHandler;
						menu.Dispose();
					}
					_jumpMenus.Clear();
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
					_partiallySharedForToolsWideMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_recordBrowseView = null;
				_recordList = null;
				_fileExportMenuHelper = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_partiallySharedTextsAndWordsToolsMenuHelper = null;
				_jumpMenus = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}