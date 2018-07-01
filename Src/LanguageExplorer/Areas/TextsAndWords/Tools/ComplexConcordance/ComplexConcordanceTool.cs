// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	/// <summary>
	/// ITool implementation for the "complexConcordance" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class ComplexConcordanceTool : ITool
	{
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private PartiallySharedMenuHelper _partiallySharedMenuHelper;
		private BrowseViewContextMenuFactory _browseViewContextMenuFactory;
		internal const string ComplexConcOccurrencesOfSelectedUnit = "complexConcOccurrencesOfSelectedUnit";
		private MultiPane _concordanceContainer;
		private ComplexConcControl _complexConcControl;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _concordanceContainer);

			// Dispose after the main UI stuff.
			_browseViewContextMenuFactory.Dispose();
			_partiallySharedMenuHelper.Dispose();
			_textAndWordsAreaMenuHelper.Dispose();

			_complexConcControl = null;
			_recordBrowseView = null;
			_interlinMasterNoTitleBar = null;
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
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(ComplexConcOccurrencesOfSelectedUnit, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();
			_browseViewContextMenuFactory = new BrowseViewContextMenuFactory();
#if RANDYTODO
			// TODO: Set up factory method for the browse view.
#endif
			var mainConcordanceContainerParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "WordsAndOccurrencesMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "wordOccurrenceList",
				DefaultFocusControl = "ComplexConcControl",
				SecondCollapseZone = 144000,
				FirstControlParameters = new SplitterChildControlParameters(), // Leave its Control as null. Will be a newly created MultiPane, the controls of which are in "nestedMultiPaneParameters"
				SecondControlParameters = new SplitterChildControlParameters() // Control (PaneBarContainer+InterlinMasterNoTitleBar) added below. Leave Label null.
			};
			var root = XDocument.Parse(TextAndWordsResources.ComplexConcordanceToolParameters).Root;
			var columns = XElement.Parse(TextAndWordsResources.ConcordanceColumns).Element("columns");
			root.Element("wordOccurrenceList").Element("parameters").Element("includeCordanceColumns").ReplaceWith(columns);
			_interlinMasterNoTitleBar = new InterlinMasterNoTitleBar(root.Element("ITextControl").Element("parameters"), majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.LcmCache, _recordList, MenuServices.GetFileMenu(majorFlexComponentParameters.MenuStrip), MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			mainConcordanceContainerParameters.SecondControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _interlinMasterNoTitleBar);
			_partiallySharedMenuHelper = new PartiallySharedMenuHelper(majorFlexComponentParameters, _interlinMasterNoTitleBar, _recordList);

			// This will be the nested MultiPane that goes into mainConcordanceContainerParameters.FirstControlParameters.Control
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				Area = _area,
				Id = "PatternAndTextMultiPane",
				ToolMachineName = MachineName,
				FirstCollapseZone = 110000,
				SecondCollapseZone = 180000,
				DefaultFixedPaneSizePoints = "50%",
				FirstControlParameters = new SplitterChildControlParameters(), // Control (PaneBarContainer+ConcordanceControl) added below. Leave Label null.
				SecondControlParameters = new SplitterChildControlParameters() // Control (PaneBarContainer+RecordBrowseView) added below. Leave Label null.
			};
			_complexConcControl = new ComplexConcControl((MatchingConcordanceItems)_recordList)
			{
				Dock = DockStyle.Fill
			};
			nestedMultiPaneParameters.FirstControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _complexConcControl);
			_recordBrowseView = new RecordBrowseView(root.Element("wordOccurrenceList").Element("parameters"), _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _recordList);
			nestedMultiPaneParameters.SecondControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _recordBrowseView);
			// Nested MP is created by call to MultiPaneFactory.CreateConcordanceContainer
			_concordanceContainer = MultiPaneFactory.CreateConcordanceContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, mainConcordanceContainerParameters, nestedMultiPaneParameters);

			_interlinMasterNoTitleBar.FinishInitialization();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_interlinMasterNoTitleBar.PrepareToRefresh();
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
		public string MachineName => AreaServices.ComplexConcordanceMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Complex Concordance";
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
			Require.That(recordListId == ComplexConcOccurrencesOfSelectedUnit, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{ComplexConcOccurrencesOfSelectedUnit}'.");
			/*
            <clerk id="complexConcOccurrencesOfSelectedUnit" allowDeletions="false">
              <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.OccurrencesOfSelectedUnit" />
              <recordList class="LangProject" field="ConcOccurrences">
                <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.MatchingConcordanceItems" />
                <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
              </recordList>
              <sortMethods />
            </clerk>
			*/
			var concDecorator = new ConcDecorator(cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexComponentParameters);
			return new MatchingConcordanceItems(recordListId, statusBar, concDecorator);
		}
	}
}
