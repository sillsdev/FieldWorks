// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Grammar.Tools.BulkEditPhonemes
{
	/// <summary>
	/// ITool implementation for the "bulkEditPhonemes" tool in the "grammar" area.
	/// </summary>
	internal sealed class BulkEditPhonemesTool : ITool
	{
		private PaneBarContainer _paneBarContainer;
		private AssignFeaturesToPhonemes _assignFeaturesToPhonemesView;
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
				ref _paneBarContainer,
				ref _recordClerk);
			_assignFeaturesToPhonemesView = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var doc = XDocument.Parse(GrammarResources.BulkEditPhonemesToolParameters);
			var cache = PropertyTable.GetValue<FdoCache>("cache");
			if (cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Count == 0)
			{
				// Pathological...this helps the memory-only backend mainly, but makes others self-repairing.
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor, () =>
				{
					cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				});
			}
			_recordClerk = new RecordClerk("phonemes", new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true, PhPhonemeSetTags.kflidPhonemes, cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0], "Phonemes"), new PropertyRecordSorter("ShortName"), "Default", null, false, false);
			_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			_assignFeaturesToPhonemesView = new AssignFeaturesToPhonemes(doc.Root, _recordClerk);

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				_assignFeaturesToPhonemesView);
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_assignFeaturesToPhonemesView.BrowseViewer.BrowseView.PrepareToRefresh();
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
		public string MachineName => "bulkEditPhonemes";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Bulk Edit Phoneme Features";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "grammar";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}
