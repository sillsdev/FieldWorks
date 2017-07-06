// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	/// <summary>
	/// ITool implementation for the "complexConcordance" tool in the "textsWords" area.
	/// </summary>
	internal sealed class ComplexConcordanceTool : ITool
	{
		private MultiPane _concordanceContainer;
		private ComplexConcControl _complexConcControl;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;
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
			MultiPaneFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				ref _concordanceContainer,
				ref _recordClerk);

			_complexConcControl = null;
			_recordBrowseView = null;
			_interlinMasterNoTitleBar = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var doc = XDocument.Parse(TextAndWordsResources.ComplexConcordanceToolParameters);
			var columns = XElement.Parse(TextAndWordsResources.ConcordanceColumns).Element("columns");
			doc.Root.Element("wordOccurrenceList").Element("parameters").Element("includeCordanceColumns").ReplaceWith(columns);
			var cache = PropertyTable.GetValue<LcmCache>("cache");
			var decorator = new ConcDecorator(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), cache.ServiceLocator);
			_recordClerk = new OccurrencesOfSelectedUnit("complexConcOccurrencesOfSelectedUnit", decorator);
			_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			var mainConcordanceContainerParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "WordsAndOccurrencesMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "wordOccurrenceList",
				DefaultFocusControl = "ComplexConcControl",
				SecondCollapseZone = 144000,
				FirstControlParameters = new SplitterChildControlParameters(), // Leave its Control as null. Will be a newly created MultiPane, the controls of which are in "nestedMultiPaneParameters"
				SecondControlParameters = new SplitterChildControlParameters() // Control (PaneBarContainer+InterlinMasterNoTitleBar) added below. Leave Label null.
			};
			_interlinMasterNoTitleBar = new InterlinMasterNoTitleBar(doc.Root.Element("ITextControl").Element("parameters"), _recordClerk);
			mainConcordanceContainerParameters.SecondControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _interlinMasterNoTitleBar);

			// This will be the nested MultiPane that goes into mainConcordanceContainerParameters.FirstControlParameters.Control
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				AreaMachineName = AreaMachineName,
				Id = "PatternAndTextMultiPane",
				ToolMachineName = MachineName,
				FirstCollapseZone = 110000,
				SecondCollapseZone = 180000,
				DefaultFixedPaneSizePoints = "50%",
				FirstControlParameters = new SplitterChildControlParameters(), // Control (PaneBarContainer+ConcordanceControl) added below. Leave Label null.
				SecondControlParameters = new SplitterChildControlParameters() // Control (PaneBarContainer+RecordBrowseView) added below. Leave Label null.
			};
			_complexConcControl = new ComplexConcControl((OccurrencesOfSelectedUnit) _recordClerk)
			{
				Dock = DockStyle.Fill
			};
			nestedMultiPaneParameters.FirstControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _complexConcControl);
			_recordBrowseView = new RecordBrowseView(doc.Root.Element("wordOccurrenceList").Element("parameters"), _recordClerk);
			nestedMultiPaneParameters.SecondControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _recordBrowseView);
			// Nested MP is created by call to MultiPaneFactory.CreateConcordanceContainer
			_concordanceContainer = MultiPaneFactory.CreateConcordanceContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, mainConcordanceContainerParameters, nestedMultiPaneParameters);

			_interlinMasterNoTitleBar.FinishInitialization();
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
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
			_recordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerk.VirtualListPublisher).Refresh();
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
		public string MachineName => "complexConcordance";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Complex Concordance";
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
	}
}
