// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.LexiconProblems
{
	/// <summary>
	/// ITool implementation for the "lexiconProblems" tool in the "grammar" area.
	/// </summary>
	internal sealed class LexiconProblemsTool : ITool
	{
		private const string LexProblems = "lexProblems";
		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar on the left has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
		private RecordClerk _recordClerk;
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private DataTree _dataTree;

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
			_sliceContextMenuFactory.Dispose();
			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);
			_sliceContextMenuFactory = null;
			_dataTree = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory = new SliceContextMenuFactory();
			var root = XDocument.Parse(GrammarResources.LexiconProblemsParameters).Root;
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexProblems, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_dataTree = new DataTree(_sliceContextMenuFactory);
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				true,
				root.Element("parameters"),
				null,
				MachineName,
				majorFlexComponentParameters.LcmCache,
				_recordClerk,
				_dataTree);
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
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
		public string MachineName => "lexiconProblems";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Problems";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "grammar";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static RecordClerk FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == LexProblems, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{LexProblems}'.");

			var probAnnFilter = new ProblemAnnotationFilter();
			probAnnFilter.Init(cache, XDocument.Parse(GrammarResources.LexiconProblemsParameters).Root.Element("filterElement"));
			return new RecordClerk(clerkId,
				statusBar,
				new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true, LangProjectTags.kflidAnnotations, cache.LanguageProject, "Annotations"),
				new PropertyRecordSorter("ShortName"),
				"Default",
				probAnnFilter,
				true,
				true,
				new RecordBarListHandler(flexComponentParameters.PropertyTable, true, true, false, "best analorvern"));
		}
	}
}
