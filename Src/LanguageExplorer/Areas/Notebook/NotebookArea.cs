// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
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

		internal IRecordList MyRecordList { get; set; }

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
			// This will also remove any event handlers set up by the active tool,
			// and any of the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveAreaHandlers();
			var activeTool = ActiveTool;
			ActiveTool = null;
			activeTool?.Deactivate(majorFlexComponentParameters);
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
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.NotebookAreaDefaultToolMachineName, true);

			var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(this);
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters);
			_notebookAreaMenuHelper.InitializeAreaWideMenus(areaUiWidgetParameterObject);
			majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			PersistedOrDefaultTool.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			PersistedOrDefaultTool.FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty(AreaServices.InitialArea, MachineName, true, settingsGroup: SettingsGroup.LocalSettings);

			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
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
		public ITool PersistedOrDefaultTool => _myTools.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, AreaServices.NotebookAreaDefaultToolMachineName));

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IReadOnlyList<ITool> AllToolsInOrder
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

		/// <summary>
		/// Set the active tool for the area, or null, if no tool is active.
		/// </summary>
		public ITool ActiveTool { get; set; }

		#endregion

		internal static IRecordList NotebookFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == Records, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{Records}'.");
			/*
            <clerk id="records">
              <recordList owner="RnResearchNbk" property="AllRecords">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.RecordList" />
              </recordList>
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new RecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false,
				new VectorPropertyParameterObject(cache.LanguageProject.ResearchNotebookOA, "AllRecords", cache.MetaDataCacheAccessor.GetFieldId2(cache.LanguageProject.ResearchNotebookOA.ClassID, "AllRecords", false)));
		}

		/// <summary>
		/// Handle creation and use of Notebook area menus.
		/// </summary>
		private sealed class NotebookAreaMenuHelper
		{
			private IArea _area;
			private readonly MajorFlexComponentParameters _majorFlexComponentParameters;

			internal NotebookAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

				_majorFlexComponentParameters = majorFlexComponentParameters;
			}

			internal void InitializeAreaWideMenus(AreaUiWidgetParameterObject areaUiWidgetParameterObject)
			{
				_area = areaUiWidgetParameterObject.Area;
				// Add Edit menu item that is available in all Notebook tools.
				areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Edit].Add(Command.CmdGoToRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GotoRecord_Clicked, () => CanCmdGoToRecord));
				// File->Export menu is visible and maybe enabled in this tool. (Area)
				areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.File].Add(Command.CmdImportSFMNotebook, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportSFMNotebook_Clicked, () => CanCmdImportSFMNotebook));

				areaUiWidgetParameterObject.ToolBarItemsForArea[ToolBar.Insert].Add(Command.CmdGoToRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GotoRecord_Clicked, () => CanCmdGoToRecord));
			}

			private Tuple<bool, bool> CanCmdGoToRecord => new Tuple<bool, bool>(true, true);

			private void GotoRecord_Clicked(object sender, EventArgs e)
			{
				/*
					<command id="CmdGoToRecord" label="_Find Record..." message="GotoRecord" icon="goToRecord" shortcut="Ctrl+F" >
					  <parameters title="Go To Record" formlabel="Go _To..." okbuttonlabel="_Go" />
					</command>
				*/
				using (var dlg = new RecordGoDlg())
				{
					dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					var cache = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
					dlg.SetDlgInfo(cache, null);
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(_area.ActiveTool.MachineName, dlg.SelectedObject.Guid));
					}
				}
			}

			private Tuple<bool, bool> CanCmdImportSFMNotebook => new Tuple<bool, bool>(true, true);

			private void ImportSFMNotebook_Clicked(object sender, EventArgs e)
			{
				using (var importWizardDlg = new NotebookImportWiz())
				{
					AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
				}
			}
		}
	}
}