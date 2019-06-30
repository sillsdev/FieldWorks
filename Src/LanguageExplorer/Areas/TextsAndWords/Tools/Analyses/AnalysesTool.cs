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
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	/// <summary>
	/// ITool implementation for the "Analyses" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class AnalysesTool : ITool
	{
		private AnalysesToolMenuHelper _toolMenuHelper;
		private MultiPane _multiPane;
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

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
			var root = XDocument.Parse(TextAndWordsResources.WordListParameters).Root;
			var columnsElement = XElement.Parse(TextAndWordsResources.WordListColumns);
			var overriddenColumnElement = columnsElement.Elements("column").First(column => column.Attribute("label").Value == "Form");
			overriddenColumnElement.Attribute("width").Value = "25%";
			overriddenColumnElement = columnsElement.Elements("column").First(column => column.Attribute("label").Value == "Word Glosses");
			overriddenColumnElement.Attribute("width").Value = "25%";
			// LT-8373.The point of these overrides: By default, enable User Analyses for "Word Analyses"
			overriddenColumnElement = columnsElement.Elements("column").First(column => column.Attribute("label").Value == "User Analyses");
			overriddenColumnElement.Attribute("visibility").Value = "always";
			overriddenColumnElement.Add(new XAttribute("width", "15%"));
			root.Add(columnsElement);
			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName), false));
			var recordEditView = new RecordEditView(XElement.Parse(TextAndWordsResources.AnalysesRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				new MultiPaneParameters
				{
					Orientation = Orientation.Vertical,
					Area = _area,
					Id = "WordsAndAnalysesMultiPane",
					ToolMachineName = MachineName
				}, _recordBrowseView, "WordList", new PaneBar(), recordEditView, "SingleWord", new PaneBar());
			using (var gr = _multiPane.CreateGraphics())
			{
				_multiPane.Panel2MinSize = Math.Max((int)(180000 * gr.DpiX) / MiscUtils.kdzmpInch, CollapsingSplitContainer.kCollapseZone);
			}
			// Too early before now.
			_toolMenuHelper = new AnalysesToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList);
			recordEditView.FinishInitialization();
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
		public string MachineName => AreaServices.AnalysesMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => AreaServices.AnalysesUiName;
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

		private sealed class AnalysesToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private FileExportMenuHelper _fileExportMenuHelper;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
			private PartiallySharedTextsAndWordsToolsMenuHelper _partiallySharedTextsAndWordsToolsMenuHelper;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private ToolStripMenuItem _jumpMenu;

			internal AnalysesToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;

				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_fileExportMenuHelper = new FileExportMenuHelper(majorFlexComponentParameters);
				_fileExportMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
				_partiallySharedTextsAndWordsToolsMenuHelper = new PartiallySharedTextsAndWordsToolsMenuHelper(majorFlexComponentParameters);
				_partiallySharedTextsAndWordsToolsMenuHelper.AddMenusForExpectedTextAndWordsTools(toolUiWidgetParameterObject);
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and tool bars.
#endif
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
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

				// <command id="CmdWordformJumpToConcordance" label="Show Wordform in Concordance" message="JumpToTool">
				_jumpMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Wordform_in_Concordance);
				_jumpMenu.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.ConcordanceMachineName, _recordList };

				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("WfiWordform", StringTable.ClassNames)));
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

			~AnalysesToolMenuHelper()
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
					_jumpMenu.Click -= _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool);
					_jumpMenu.Dispose();
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
					_partiallySharedForToolsWideMenuHelper.Dispose();
					_fileExportMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_recordBrowseView = null;
				_recordList = null;
				_jumpMenu = null;
				_partiallySharedTextsAndWordsToolsMenuHelper = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_fileExportMenuHelper = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}