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
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	/// <summary>
	/// ITool implementation for the "AdhocCoprohibitionRuleEdit" tool in the "grammar" area.
	/// </summary>
	/// <remarks>
	/// This is the comment from the data model file about an "AdhocCoprohibition" (actual class "MoAdhocProhib":
	///
	/// This abstract class is intended to capture co-occurrence restrictions between morphemes or allomorphs which cannot be captured
	/// using morphosyntactic or phonological restrictions. Linguistically speaking, this is perhaps as bad a kludge as you can imagine.
	/// We allow it here for reasons of stealth-to-wealth work, with the understanding that the program will twist the user's arm into
	/// getting rid of these later on (perhaps by flagging each of these constraints with a Warning). The technique is borrowed from
	/// AMPLE's ad hoc pairs. Note however that, Aronoff (1976: 53) gives as an example of a negative constraint the fact that the
	/// suffix -ness does not attach to adjectives of the form X-ate, X-ant, or X-ent. So maybe this isn't such a kludge after all.
	/// On the other hand, Aronoff's examples are largely statistical generalizations, that is, tendencies - as opposed to hard constraints.
	///
	/// It may be that we should also have positive cooccurrence constraints. Aronoff (1976: 63) lists a number of "forms of the base"
	/// which are compatible with the English prefix un-, among them X-en (where -en is the past participle suffix), X-ing, and X-able,
	/// which would be examples of positive constraints. However, un- also attaches to a good many monomorphemic stems (roots, e.g. unhappy),
	/// so it may be that this is not a real generalization.
	///
	/// In addition to the attributes below, there should probably be an attribute to point to analyses which are ruled out by these constraints.
	/// These could be either grammatical words for which the parser would generate an incorrect analysis if it were not for this constraint,
	/// or ungrammatical words which the user has supplied, and which would be parsed if not for this constraint. It may even be desirable to
	/// allow individual constraints to be turned off in the parsing of such examples, in order to verify that the constraint works, and that
	/// it is (still) needed. However, the need for such an attr is probably more general than this class; see my email of 18 Jan 2000.
	/// </remarks>
	internal sealed class AdhocCoprohibitionRuleEditTool : ITool
	{
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane, ref _recordClerk);
			_recordBrowseView = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var root = XDocument.Parse(GrammarResources.AdhocCoprohibitionRuleEditToolParameters).Root;
			var cache = PropertyTable.GetValue<FdoCache>("cache");
			var flexComponentParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			_recordClerk = new RecordClerk("adhocCoprohibitions", new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true, MoMorphDataTags.kflidAdhocCoProhibitions, cache.LanguageProject.MorphologicalDataOA, "AdhocCoprohibitions"), new PropertyRecordSorter("ShortName"), "Default", null, false, false);
			_recordClerk.InitializeFlexComponent(flexComponentParameters);
			_recordBrowseView = new RecordBrowseView(root.Element("browseview").Element("parameters"), _recordClerk);
#if RANDYTODO
			// TODO: Set up 'dataTreeMenuHandler' to handle menu events.
			// TODO: Install menus and connect them to event handlers. (See "CreateContextMenuStrip" method for where the menus are.)
#endif
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.CompleteFilter), _recordClerk);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "AdhocCoprohibItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};

			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				flexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_recordBrowseView, "Browse", new PaneBar(),
				recordEditView, "Details", recordEditViewPaneBar);

			panelButton.DatTree = recordEditView.DatTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
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
		public string MachineName => "AdhocCoprohibitionRuleEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Ad hoc Rules";

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
	}
}
