// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// IArea implementation for the main, and thus only required, Area: "lexicon".
	/// </summary>
	internal sealed class LexiconArea : IArea
	{
		private const string khomographconfiguration = "HomographConfiguration";
		private readonly IToolRepository m_toolRepository;

		/// <summary>
		/// Contructor used by Reflection to feed the tool repository to the area.
		/// </summary>
		/// <param name="toolRepository"></param>
		internal LexiconArea(IToolRepository toolRepository)
		{
			m_toolRepository = toolRepository;
		}

		internal static RecordClerk CreateBasicClerkForLexiconArea(FdoCache cache)
		{
			var mdc = cache.MetaDataCacheAccessor;
			var lexDb = cache.LanguageProject.LexDbOA;
			var recordList = new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false, mdc.GetFieldId2(lexDb.ClassID, "Entries", false), lexDb, "Entries");
			var sorters = new Dictionary<string, PropertyRecordSorter>
			{
				{RecordClerk.kDefault, new PropertyRecordSorter("ShortName")},
				{"PrimaryGloss", new PropertyRecordSorter("PrimaryGloss")}
			};
			return new RecordClerk("entries", recordList, sorters, null, false, false);
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

			if (m_toolRepository.Publisher == null)
			{
				m_toolRepository.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			}

			// Restore HomographConfiguration settings.
			string hcSettings;
			if (!PropertyTable.TryGetValue(khomographconfiguration, out hcSettings)) return;

			var serviceLocator = PropertyTable.GetValue<FdoCache>("cache").ServiceLocator;
			var hc = serviceLocator.GetInstance<HomographConfiguration>();
			hc.PersistData = hcSettings;

			PropertyTable.SetDefault("SelectedPublication", "Main Dictionary", SettingsGroup.LocalSettings, true, true);
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => "lexicon";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Lexical Tools";

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
			PropertyTable.SetDefault("Show_DictionaryPubPreview", true, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("Show_reversalIndexEntryList", true, SettingsGroup.LocalSettings, false, false);
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

			var serviceLocator = PropertyTable.GetValue<FdoCache>("cache").ServiceLocator;
			var hc = serviceLocator.GetInstance<HomographConfiguration>();
			PropertyTable.SetProperty(khomographconfiguration, hc.PersistData, true, false);

			var myCurrentTool = m_toolRepository.GetPersistedOrDefaultToolForArea(this);
			myCurrentTool.EnsurePropertiesAreCurrent();
		}

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
		public string DefaultToolMachineName => "lexiconEdit";

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					"lexiconEdit",
					"lexiconBrowse",
					"lexiconDictionary",
					"rapidDataEntry",
					"lexiconClassifiedDictionary",
					"bulkEditEntriesOrSenses",
					"reversalEditComplete",
					"reversalBulkEditReversalEntries"
				};
				return m_toolRepository.AllToolsForAreaInOrder(myToolsInOrder, MachineName);
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Lexicon32.ToBitmap();

		#endregion
	}
}
