// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.WordListConcordance
{
	/// <summary>
	/// ITool implementation for the "wordListConcordance" tool in the "textsWords" area.
	/// </summary>
	internal sealed class WordListConcordanceTool : ITool
	{
		private AreaWideMenuHelper _areaWideMenuHelper;
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private const string OccurrencesOfSelectedWordform = "OccurrencesOfSelectedWordform";
		private IRecordClerkRepositoryForTools _clerkRepositoryForTools;
		private MultiPane _outerMultiPane;
		private RecordBrowseView _mainRecordBrowseView;
		private MultiPane _nestedMultiPane;
		private RecordBrowseView _nestedRecordBrowseView;
		private RecordClerk _recordClerkProvidingOwner;
		private RecordClerk _mainRecordClerk;
		private InterlinMasterNoTitleBar _interlinMasterNoTitleBar;

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
			_areaWideMenuHelper.Dispose();
			_textAndWordsAreaMenuHelper.Dispose();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _outerMultiPane);
			_mainRecordBrowseView = null;
			_nestedMultiPane = null;
			_nestedRecordBrowseView = null;
			_interlinMasterNoTitleBar = null;
			_areaWideMenuHelper = null;
			_textAndWordsAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_clerkRepositoryForTools = majorFlexComponentParameters.RecordClerkRepositoryForTools;
			if (_recordClerkProvidingOwner == null)
			{
				_recordClerkProvidingOwner = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.Statusbar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
			_areaWideMenuHelper = new AreaWideMenuHelper(majorFlexComponentParameters, _recordClerkProvidingOwner);
			_areaWideMenuHelper.SetupFileExportMenu();
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();

			if (_mainRecordClerk == null)
			{
				_mainRecordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(OccurrencesOfSelectedWordform, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}

			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				AreaMachineName = AreaMachineName,
				Id = "LineAndTextMultiPane",
				ToolMachineName = MachineName,
				DefaultFixedPaneSizePoints = "50%",
				FirstControlParameters = new SplitterChildControlParameters(), // Control (RecordBrowseView) added below. Leave Label null.
				SecondControlParameters = new SplitterChildControlParameters() // Control (InterlinMasterNoTitleBar) added below. Leave Label null.
			};
			var root = XDocument.Parse(TextAndWordsResources.WordListConcordanceToolParameters).Root;
			root.Element("wordList").Element("parameters").Element("includeColumns").ReplaceWith(XElement.Parse(TextAndWordsResources.WordListColumns));
			root.Element("wordOccurrenceListUpper").Element("parameters").Element("includeColumns").ReplaceWith(XElement.Parse(TextAndWordsResources.ConcordanceColumns).Element("columns"));
			_nestedRecordBrowseView = new RecordBrowseView(root.Element("wordOccurrenceListUpper").Element("parameters"), majorFlexComponentParameters.LcmCache, _mainRecordClerk);
			nestedMultiPaneParameters.FirstControlParameters.Control = _nestedRecordBrowseView;
			_interlinMasterNoTitleBar = new InterlinMasterNoTitleBar(root.Element("wordOccurrenceListLower").Element("parameters"), majorFlexComponentParameters.LcmCache, _mainRecordClerk, MenuServices.GetFileMenu(majorFlexComponentParameters.MenuStrip), MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			nestedMultiPaneParameters.SecondControlParameters.Control = _interlinMasterNoTitleBar;
			_nestedMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPaneParameters);
			_mainRecordBrowseView = new RecordBrowseView(root.Element("wordList").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerkProvidingOwner);

			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
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
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerkProvidingOwner;
			majorFlexComponentParameters.RecordClerkRepositoryForTools.ActiveRecordClerk = _mainRecordClerk;
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
			_mainRecordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_mainRecordClerk.VirtualListPublisher).Refresh();
			_recordClerkProvidingOwner.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerkProvidingOwner.VirtualListPublisher).Refresh();
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
		public string MachineName => "wordListConcordance";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Word List Concordance";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "textsWords";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static RecordClerk FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == OccurrencesOfSelectedWordform, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{OccurrencesOfSelectedWordform}'.");

			return new RecordClerk(clerkId,
				statusBar,
				new RecordList(new ConcDecorator(cache.ServiceLocator), false, ConcDecorator.kflidWfOccurrences),
				new PropertyRecordSorter("ShortName"),
				"Default",
				null,
				true,
				true,
				((IRecordClerkRepositoryForTools)RecordClerk.ActiveRecordClerkRepository).GetRecordClerk(TextAndWordsArea.ConcordanceWords, statusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod));
		}
	}
}
