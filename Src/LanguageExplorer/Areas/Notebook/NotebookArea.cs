// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Notebook
{
	internal sealed class NotebookArea : IArea
	{
		internal const string Records = "records";
		private readonly IToolRepository _toolRepository;
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;

		internal RecordClerk RecordClerk { get; set; }

		/// <summary>
		/// Contructor used by Reflection to feed the tool repository to the area.
		/// </summary>
		/// <param name="toolRepository"></param>
		internal NotebookArea(IToolRepository toolRepository)
		{
			_toolRepository = toolRepository;
		}

		internal static XDocument LoadDocument(string resourceName)
		{
			var configurationDocument = XDocument.Parse(resourceName);
			configurationDocument.Root.Add(XElement.Parse(NotebookResources.NotebookBrowseColumnDefinitions));
			return configurationDocument;
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
			_notebookAreaMenuHelper.Dispose();
			_notebookAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters);
			_notebookAreaMenuHelper.Initialize();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_toolRepository.GetPersistedOrDefaultToolForArea(this).PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_toolRepository.GetPersistedOrDefaultToolForArea(this).FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			PropertyTable.SetProperty("InitialArea", MachineName, SettingsGroup.LocalSettings, true, false);

			var myCurrentTool = _toolRepository.GetPersistedOrDefaultToolForArea(this);
			myCurrentTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => "notebook";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Notebook";
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool GetPersistedOrDefaultToolForArea()
		{
			return _toolRepository.GetPersistedOrDefaultToolForArea(this);
		}

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		public string DefaultToolMachineName => "notebookEdit";

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					"notebookEdit",
					"notebookBrowse",
					"notebookDocument"
				};
				return _toolRepository.AllToolsForAreaInOrder(myToolsInOrder, MachineName);
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Notebook.ToBitmap();

		#endregion

		internal static RecordClerk NotebookFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == Records, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{Records}'.");

			return new RecordClerk(clerkId,
				statusBar,
				new RecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false, cache.MetaDataCacheAccessor.GetFieldId2(cache.LanguageProject.ResearchNotebookOA.ClassID, "AllRecords", false), cache.LanguageProject.ResearchNotebookOA, "AllRecords"),
				new PropertyRecordSorter("ShortName"),
				"Default",
				null,
				false,
				false);
		}
	}
}
