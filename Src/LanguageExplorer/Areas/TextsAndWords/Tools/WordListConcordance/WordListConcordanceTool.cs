// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
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
		private AreaWideMenuHelper _areaWideMenuHelper;
		private BrowseViewContextMenuFactory _browseViewContextMenuFactory;
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private PartiallySharedMenuHelper _partiallySharedMenuHelper;
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _outerMultiPane);

			// Dispose after the main UI stuff.
			_browseViewContextMenuFactory.Dispose();
			_partiallySharedMenuHelper.Dispose();
			_areaWideMenuHelper.Dispose();
			_textAndWordsAreaMenuHelper.Dispose();

			_mainRecordBrowseView = null;
			_nestedMultiPane = null;
			_nestedRecordBrowseView = null;
			_interlinMasterNoTitleBar = null;
			_areaWideMenuHelper = null;
			_textAndWordsAreaMenuHelper = null;
			_partiallySharedMenuHelper = null;
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
			var recordListRepository = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
			if (_recordListProvidingOwner == null)
			{
				_recordListProvidingOwner = recordListRepository.GetRecordList(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.StatusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
			_areaWideMenuHelper = new AreaWideMenuHelper(majorFlexComponentParameters, _recordListProvidingOwner);
			_areaWideMenuHelper.SetupFileExportMenu();
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();
			_browseViewContextMenuFactory = new BrowseViewContextMenuFactory();
#if RANDYTODO
			// TODO: Set up factory method for the browse view.
#endif

			if (_subservientRecordList == null)
			{
				_subservientRecordList = recordListRepository.GetRecordList(OccurrencesOfSelectedWordform, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}

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
			_nestedRecordBrowseView = new RecordBrowseView(root.Element("wordOccurrenceListUpper").Element("parameters"), _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _subservientRecordList);
			nestedMultiPaneParameters.FirstControlParameters.Control = _nestedRecordBrowseView;
			_interlinMasterNoTitleBar = new InterlinMasterNoTitleBar(root.Element("wordOccurrenceListLower").Element("parameters"), majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.LcmCache, _subservientRecordList, MenuServices.GetFileMenu(majorFlexComponentParameters.MenuStrip), MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			nestedMultiPaneParameters.SecondControlParameters.Control = _interlinMasterNoTitleBar;
			_nestedMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPaneParameters);
			_mainRecordBrowseView = new RecordBrowseView(root.Element("wordList").Element("parameters"), _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _recordListProvidingOwner);
			_partiallySharedMenuHelper = new PartiallySharedMenuHelper(majorFlexComponentParameters, _interlinMasterNoTitleBar, _recordListProvidingOwner);

			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "WordsAndOccurrencesMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "wordOccurrenceList",
				SecondCollapseZone = 180000
			};

			_outerMultiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_mainRecordBrowseView, "Concordance", new PaneBar(),
				_nestedMultiPane, "Tabs", new PaneBar());

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
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Word List Concordance";

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
			Require.That(recordListId == OccurrencesOfSelectedWordform, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{OccurrencesOfSelectedWordform}'.");
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
			return new SubservientRecordList(recordListId, statusBar,
				concDecorator, false,
				ConcDecorator.kflidWfOccurrences,
				flexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.ConcordanceWords, statusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod));
		}
	}
}
