// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lists
{
	internal sealed class ListsArea : IArea
	{
		private readonly IToolRepository m_toolRepository;

		/// <summary>
		/// Contructor used by Reflection to feed the tool repository to the area.
		/// </summary>
		/// <param name="toolRepository"></param>
		internal ListsArea(IToolRepository toolRepository)
		{
			m_toolRepository = toolRepository;
		}

		internal static RecordClerk CreateBasicClerkForListArea(IPropertyTable propertyTable, PossibilityListClerkParameters possibilityListClerkParameters)
		{
			var cache = propertyTable.GetValue<FdoCache>("cache");
			var recordList = new PossibilityRecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), possibilityListClerkParameters.OwningList);
			var sorter = new PropertyRecordSorter("ShortName");
			return new RecordClerk(possibilityListClerkParameters.ClerkIdentifier, recordList, sorter, "Default", null, true, true, new PossibilityTreeBarHandler(propertyTable, possibilityListClerkParameters.Expand, possibilityListClerkParameters.Hierarchical, possibilityListClerkParameters.IncludeAbbr, possibilityListClerkParameters.Ws));
		}

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
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			m_toolRepository.GetPersistedOrDefaultToolForArea(this).PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			m_toolRepository.GetPersistedOrDefaultToolForArea(this).FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			PropertyTable.SetProperty("InitialArea", MachineName, SettingsGroup.LocalSettings, true, false);

			var myCurrentTool = m_toolRepository.GetPersistedOrDefaultToolForArea(this);
			myCurrentTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => "lists";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Lists";
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool GetPersistedOrDefaultToolForArea()
		{
			return m_toolRepository.GetPersistedOrDefaultToolForArea(this);
		}

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		public string DefaultToolMachineName => "domainTypeEdit";

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					"domainTypeEdit",
					"anthroEdit",
					"complexEntryTypeEdit",
					"confidenceEdit",
					"chartmarkEdit",
					"charttempEdit",
					"educationEdit",
					"roleEdit",
					"featureTypesAdvancedEdit",
					"genresEdit",
					"lexRefEdit",
					"locationsEdit",
					"publicationsEdit",
					"morphTypeEdit",
					"peopleEdit",
					"positionsEdit",
					"restrictionsEdit",
					"semanticDomainEdit",
					"senseTypeEdit",
					"statusEdit",
					"textMarkupTagsEdit",
					"translationTypeEdit",
					"usageTypeEdit",
					"variantEntryTypeEdit",
					"recTypeEdit",
					"timeOfDayEdit",
					"reversalToolReversalIndexPOS"
				};

#if RANDYTODO
				// TODO: Add user-defined tools in some kind of generic list area that can work with user-defined lists.
				// TODO: That generic list tools will *not* be located by reflection in a plugin assembly like all other tools,
				// TODO: but it/they will be created by this area, as/if needed for each user-defined tool.

				// TODO: Q: should they be added to the tool repository?
				// TODO: A1: Probably, since the tool repository really only needs to be create once per project, but...
				// TODO:	In that case, then creation of the area and tool repository needs to be rethought. Work for another day....
#endif
				return m_toolRepository.AllToolsForAreaInOrder(myToolsInOrder, MachineName);
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Lists.ToBitmap();

		#endregion
	}
}
