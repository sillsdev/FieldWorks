// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
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
	[Export(AreaServices.NotebookAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class NotebookArea : IArea
	{
		[ImportMany(AreaServices.NotebookAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		internal const string Records = "records";
		private const string MyUiName = "Notebook";
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;
		[Import]
		private IPropertyTable _propertyTable;

		internal RecordClerk RecordClerk { get; set; }

		internal static XDocument LoadDocument(string resourceName)
		{
			var configurationDocument = XDocument.Parse(resourceName);
			configurationDocument.Root.Add(XElement.Parse(NotebookResources.NotebookBrowseColumnDefinitions));
			return configurationDocument;
		}

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
			_propertyTable.SetDefault(PropertyNameForToolName, DefaultToolMachineName, SettingsGroup.LocalSettings, true, false);
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters);
			_notebookAreaMenuHelper.Initialize();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			PersistedOrDefaultToolForArea.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			PersistedOrDefaultToolForArea.FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty(AreaServices.InitialArea, MachineName, SettingsGroup.LocalSettings, true, false);

			PersistedOrDefaultToolForArea.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.NotebookAreaMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => MyUiName;
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultToolForArea => _myTools.First(tool => tool.MachineName == _propertyTable.GetValue<string>(PropertyNameForToolName));

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		public string DefaultToolMachineName => AreaServices.NotebookAreaDefaultToolMachineName;

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					AreaServices.NotebookEditToolMachineName,
					AreaServices.NotebookBrowseToolMachineName,
					AreaServices.NotebookDocumentToolMachineName
				};
				return myToolsInOrder.Select(toolName => _myTools.First(tool => tool.MachineName == toolName)).ToList();
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
