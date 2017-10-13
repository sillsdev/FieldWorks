// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.InterlinearEdit
{
	/// <summary>
	/// ITool implementation for the "interlinearEdit" tool in the "textsWords" area.
	/// </summary>
	internal sealed class InterlinearEditTool : ITool
	{
		private const string InterlinearTexts = "interlinearTexts";
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;
		private InterlinMaster _interlinMaster;

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

			PropertyTable.SetDefault($"ToolForAreaNamed_{AreaMachineName}", MachineName, SettingsGroup.LocalSettings, true, false);
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
			_interlinMaster = null;
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
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(InterlinearTexts, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			var multiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "EditViewTextsMultiPane",
				ToolMachineName = MachineName,
				DefaultFixedPaneSizePoints = "145",
				DefaultPrintPane = "ITextContent",
				DefaultFocusControl = "InterlinMaster"
			};
			var root = XDocument.Parse(TextAndWordsResources.InterlinearEditToolParameters).Root;
			_recordBrowseView = new RecordBrowseView(root.Element("recordbrowseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerk);
			_interlinMaster = new InterlinMaster(root.Element("interlinearmaster").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerk, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				multiPaneParameters,
				_recordBrowseView, "Texts", new PaneBar(),
				_interlinMaster, "Text", new PaneBar());

			_multiPane.FixedPanel = FixedPanel.Panel1;

			// Too early before now.
			_interlinMaster.FinishInitialization();
			_interlinMaster.BringToFront();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_interlinMaster.PrepareToRefresh();
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
		public string MachineName => "interlinearEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Interlinear Texts";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "textsWords";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.EditView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static RecordClerk FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == InterlinearTexts, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{InterlinearTexts}'.");

			return new InterlinearTextsRecordClerk(statusBar, cache.LanguageProject, new InterestingTextsDecorator(cache.ServiceLocator, flexComponentParameters.PropertyTable));
		}
	}
}