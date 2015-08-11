// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace ListsAreaPlugin
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

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(IPropertyTable propertyTable,
			IPublisher publisher, ISubscriber subscriber,
			ICollapsingSplitContainer mainCollapsingSplitContainer,
			MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(IPropertyTable propertyTable,
			IPublisher publisher, ISubscriber subscriber,
			ICollapsingSplitContainer mainCollapsingSplitContainer,
			MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void FinishRefresh()
		{
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		/// <param name="propertyTable">The table that is about to be persisted.</param>
		public void EnsurePropertiesAreCurrent(IPropertyTable propertyTable)
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName
		{
			get { return "lists"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Lists"; }
		}

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool GetPersistedOrDefaultToolForArea(IPropertyTable propertyTable)
		{
			return m_toolRepository.GetPersistedOrDefaultToolForArea(propertyTable, this);
		}

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		public string DefaultToolMachineName
		{
			get { return "domainTypeEdit"; }
		}

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
		public Image Icon
		{
			get { return ListAreaResources.Lists.ToBitmap(); }
		}

		#endregion
	}
}
