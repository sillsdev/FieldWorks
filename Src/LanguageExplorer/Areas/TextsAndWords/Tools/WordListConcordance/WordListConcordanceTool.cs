// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.WordListConcordance
{
	/// <summary>
	/// ITool implementation for the "wordListConcordance" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class WordListConcordanceTool : ITool
	{
		private WordListConcordanceToolMenuHelper _toolMenuHelper;
		private const string OccurrencesOfSelectedWordform = "OccurrencesOfSelectedWordform";
		private MultiPane _outerMultiPane;
		private RecordBrowseView _mainRecordBrowseView;
		private MultiPane _nestedMultiPane;
		private RecordBrowseView _nestedRecordBrowseView;
		private IRecordList _recordListProvidingOwner;
		private IRecordList _subservientRecordList;
		private InterlinMasterNoTitleBar _interlinMasterNoTitleBar;
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
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _outerMultiPane);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_mainRecordBrowseView = null;
			_nestedMultiPane = null;
			_nestedRecordBrowseView = null;
			_interlinMasterNoTitleBar = null;
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
			var recordListRepository = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
			if (_recordListProvidingOwner == null)
			{
				_recordListProvidingOwner = recordListRepository.GetRecordList(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.StatusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
			if (_subservientRecordList == null)
			{
				_subservientRecordList = recordListRepository.GetRecordList(OccurrencesOfSelectedWordform, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			_toolMenuHelper = new WordListConcordanceToolMenuHelper(majorFlexComponentParameters, this, _recordListProvidingOwner, _subservientRecordList);
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				Area = _area,
				Id = "LineAndTextMultiPane",
				ToolMachineName = MachineName,
				DefaultFixedPaneSizePoints = "50%",
				FirstControlParameters = new SplitterChildControlParameters(), // Control (RecordBrowseView) added below. Leave Label null.
				SecondControlParameters = new SplitterChildControlParameters() // Control (InterlinMasterNoTitleBar) added below. Leave Label null.
			};
			var root = XDocument.Parse(TextAndWordsResources.WordListConcordanceToolParameters).Root;
			root.Element("wordList").Element("parameters").Element("includeColumns").ReplaceWith(XElement.Parse(TextAndWordsResources.WordListColumns));
			root.Element("wordOccurrenceListUpper").Element("parameters").Element("includeColumns").ReplaceWith(XElement.Parse(TextAndWordsResources.ConcordanceColumns).Element("columns"));
			_nestedRecordBrowseView = new RecordBrowseView(root.Element("wordOccurrenceListUpper").Element("parameters"), majorFlexComponentParameters.LcmCache, _subservientRecordList);
			nestedMultiPaneParameters.FirstControlParameters.Control = _nestedRecordBrowseView;
			_interlinMasterNoTitleBar = new InterlinMasterNoTitleBar(root.Element("wordOccurrenceListLower").Element("parameters"), majorFlexComponentParameters, _subservientRecordList);
			nestedMultiPaneParameters.SecondControlParameters.Control = _interlinMasterNoTitleBar;
			_nestedMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPaneParameters);
			_mainRecordBrowseView = new RecordBrowseView(root.Element("wordList").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordListProvidingOwner, majorFlexComponentParameters.UiWidgetController);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "WordsAndOccurrencesMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "wordOccurrenceList",
				SecondCollapseZone = 180000
			};
			_outerMultiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, mainMultiPaneParameters, _mainRecordBrowseView, "Concordance", new PaneBar(), _nestedMultiPane, "Tabs", new PaneBar());

			_toolMenuHelper.SetupUiWidgets(this, _mainRecordBrowseView);
			// The next method call will add UserControl event handlers.
			_interlinMasterNoTitleBar.FinishInitialization();
			majorFlexComponentParameters.DataNavigationManager.RecordList = _recordListProvidingOwner;
			majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList = _subservientRecordList;
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_mainRecordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
			_nestedRecordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
			_interlinMasterNoTitleBar.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_subservientRecordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_subservientRecordList.VirtualListPublisher).Refresh();
			_recordListProvidingOwner.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordListProvidingOwner.VirtualListPublisher).Refresh();
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
		public string MachineName => AreaServices.WordListConcordanceMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.WordListConcordanceUiName);

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

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == OccurrencesOfSelectedWordform, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{OccurrencesOfSelectedWordform}'.");
			/*
            <clerk id="OccurrencesOfSelectedWordform" clerkProvidingOwner="concordanceWords">
              <recordList class="WfiWordform" field="Occurrences">
                <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" key="WordformOccurrences" />
              </recordList>
              <sortMethods />
            </clerk>
          </clerks>
			*/
			var concDecorator = new ConcDecorator(cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexComponentParameters);
			return new SubservientRecordList(recordListId, statusBar, concDecorator, false, ConcDecorator.kflidWfOccurrences,
				flexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.ConcordanceWords, statusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod));
		}

		private sealed class WordListConcordanceToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private IRecordList _recordListProvidingOwner;
			private IRecordList _subservientRecordList;
			private FileExportMenuHelper _fileExportMenuHelper;
			private RecordBrowseView _mainRecordBrowseView;

			internal WordListConcordanceToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordListProvidingOwner, IRecordList subservientRecordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordListProvidingOwner, nameof(recordListProvidingOwner));
				Guard.AgainstNull(subservientRecordList, nameof(subservientRecordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordListProvidingOwner = recordListProvidingOwner;
				_subservientRecordList = subservientRecordList;

				_fileExportMenuHelper = new FileExportMenuHelper(majorFlexComponentParameters);
			}

			internal void SetupUiWidgets(ITool tool, RecordBrowseView mainRecordBrowseView)
			{
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(mainRecordBrowseView, nameof(mainRecordBrowseView));

				_mainRecordBrowseView = mainRecordBrowseView;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_fileExportMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				// NB: The nested browse view on the right has no popup menu.
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
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				// <item command="CmdWordformJumpToAnalyses"/> AreaServices.AnalysesMachineName
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdWordformJumpToAnalyses_Click, AreaResources.Show_in_Word_Analyses);

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("WfiWordform", StringTable.ClassNames)));

				// End: <menu id="mnuBrowseView" (partial) >
				_mainRecordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void CmdWordformJumpToAnalyses_Click(object sender, EventArgs e)
			{
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(AreaServices.AnalysesMachineName, _recordListProvidingOwner.CurrentObject.Guid));
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordListProvidingOwner.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			#region IDisposable
			private bool _isDisposed;

			~WordListConcordanceToolMenuHelper()
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
					_mainRecordBrowseView.ContextMenuStrip?.Dispose();
					_mainRecordBrowseView.ContextMenuStrip = null;
				}
				_majorFlexComponentParameters = null;
				_fileExportMenuHelper = null;
				_recordListProvidingOwner = null;
				_subservientRecordList = null;
				_mainRecordBrowseView = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}