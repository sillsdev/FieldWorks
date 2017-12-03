// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Works;
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
		private const string ComplexConcOccurrencesOfSelectedUnit = "complexConcOccurrencesOfSelectedUnit";
		private MultiPane _concordanceContainer;
		private ComplexConcControl _complexConcControl;
		private RecordBrowseView _recordBrowseView;
		private IRecordClerk _recordClerk;
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
			_textAndWordsAreaMenuHelper.Dispose();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _concordanceContainer);

			_complexConcControl = null;
			_recordBrowseView = null;
			_interlinMasterNoTitleBar = null;
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
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(ComplexConcOccurrencesOfSelectedUnit, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();
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
			_interlinMasterNoTitleBar = new InterlinMasterNoTitleBar(root.Element("ITextControl").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerk, MenuServices.GetFileMenu(majorFlexComponentParameters.MenuStrip), MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			mainConcordanceContainerParameters.SecondControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _interlinMasterNoTitleBar);

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
			_complexConcControl = new ComplexConcControl((MatchingConcordanceItems)_recordClerk)
			{
				Dock = DockStyle.Fill
			};
			nestedMultiPaneParameters.FirstControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _complexConcControl);
			_recordBrowseView = new RecordBrowseView(root.Element("wordOccurrenceList").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerk);
			nestedMultiPaneParameters.SecondControlParameters.Control = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, _recordBrowseView);
			// Nested MP is created by call to MultiPaneFactory.CreateConcordanceContainer
			_concordanceContainer = MultiPaneFactory.CreateConcordanceContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, mainConcordanceContainerParameters, nestedMultiPaneParameters);

			_interlinMasterNoTitleBar.FinishInitialization();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
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

		private static IRecordClerk FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == ComplexConcOccurrencesOfSelectedUnit, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{ComplexConcOccurrencesOfSelectedUnit}'.");

			return new MatchingConcordanceItems(clerkId, statusBar, new ConcDecorator(cache.ServiceLocator));
		}
	}
}
