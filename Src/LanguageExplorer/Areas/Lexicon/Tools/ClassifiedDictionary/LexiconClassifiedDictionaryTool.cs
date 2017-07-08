// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.ClassifiedDictionary
{
	/// <summary>
	/// ITool implementation for the "lexiconClassifiedDictionary" tool in the "lexicon" area.
	/// </summary>
	internal sealed class LexiconClassifiedDictionaryTool : ITool
	{
		private PaneBarContainer _paneBarContainer;
		private RecordClerk _recordClerk;

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
			PaneBarContainerFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				majorFlexComponentParameters.DataNavigationManager,
				majorFlexComponentParameters.RecordClerkRepository,
				ref _paneBarContainer);
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var xmlDocViewPaneBar = new PaneBar();
			var semanticDomainRdeTreeBarHandler = new SemanticDomainRdeTreeBarHandler(PropertyTable, XDocument.Parse(LexiconResources.RapidDataEntryToolParameters).Root.Element("treeBarHandler"), xmlDocViewPaneBar);
			if (_recordClerk == null)
			{
				var decorator = new DictionaryPublicationDecorator(majorFlexComponentParameters.LcmCache, majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), CmPossibilityListTags.kflidPossibilities);
				var recordList = new PossibilityRecordList(decorator, majorFlexComponentParameters.LcmCache.LanguageProject.SemanticDomainListOA);
				_recordClerk = new RecordClerk("SemanticDomainList", recordList, new PropertyRecordSorter("ShortName"), "Default", null, false, false, semanticDomainRdeTreeBarHandler);
				_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
				majorFlexComponentParameters.RecordClerkRepository.AddRecordClerk(_recordClerk);
			}

			var panelButton = new PanelButton(PropertyTable, null, "ShowFailingItems-lexiconClassifiedDictionary", LexiconResources.Show_Unused_Items, LexiconResources.Show_Unused_Items)
			{
				Dock = DockStyle.Right
			};
			xmlDocViewPaneBar.AddControls(new List<Control> { panelButton });
			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				xmlDocViewPaneBar,
				new XmlDocView(XDocument.Parse(LexiconResources.LexiconClassifiedDictionaryParameters).Root, majorFlexComponentParameters.LcmCache, _recordClerk));

			// Too early before now.
			semanticDomainRdeTreeBarHandler.FinishInitialization();
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
			majorFlexComponentParameters.RecordClerkRepository.ActiveRecordClerk = _recordClerk;
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
		public string MachineName => "lexiconClassifiedDictionary";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Classified Dictionary";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.DocumentView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}
