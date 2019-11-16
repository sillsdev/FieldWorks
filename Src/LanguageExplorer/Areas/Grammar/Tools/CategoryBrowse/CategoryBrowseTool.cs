// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.CategoryBrowse
{
	/// <summary>
	/// ITool implementation for the "categoryBrowse" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class CategoryBrowseTool : ITool
	{
		private CategoryBrowseToolMenuHelper _toolMenuHelper;
		private const string CategoriesWithoutTreeBarHandler = "categories_withoutTreeBarHandler";
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		[Import(AreaServices.GrammarAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			_toolMenuHelper.Dispose();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_toolMenuHelper = null;
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
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(CategoriesWithoutTreeBarHandler, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			_recordBrowseView = new RecordBrowseView(XDocument.Parse(GrammarResources.GrammarCategoryBrowserParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new CategoryBrowseToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList);
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, _recordBrowseView);
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
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
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
		public string MachineName => AreaServices.CategoryBrowseMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.CategoryBrowseUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == CategoriesWithoutTreeBarHandler, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{CategoriesWithoutTreeBarHandler}'.");
			/*
            <clerk id="categories">
              <recordList owner="LangProject" property="PartsOfSpeech">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.PossibilityRecordList" />
              </recordList>
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new PossibilityRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), cache.LanguageProject.PartsOfSpeechOA);
		}

		private sealed class CategoryBrowseToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private ISharedEventHandlers _sharedEventHandlers;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private ToolStripMenuItem _jumpMenu1;
			private ToolStripMenuItem _jumpMenu2;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;

			internal CategoryBrowseToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
				_partiallySharedForToolsWideMenuHelper.SetupCmdInsertPossibility(toolUiWidgetParameterObject, ()=> UiWidgetServices.CanSeeAndDo);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				CreateBrowseViewContextMenu();
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

				// <command id="CmdPOSJumpToDefault" label="Show in Category Edit" message="JumpToTool">
				_jumpMenu1 = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Category_Edit);
				_jumpMenu1.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.PosEditMachineName, _recordList };
				// <command id="CmdPOSJumpToConcordance" label="Show Category in Concordance" message="JumpToTool">
				_jumpMenu2 = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Category_in_Concordance);
				_jumpMenu2.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.ConcordanceMachineName, _recordList };
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("PartOfSpeech", StringTable.ClassNames)));

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~CategoryBrowseToolMenuHelper()
			{
				// The base class finalizer is called automatically.
				Dispose(false);
			}

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (_isDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					_jumpMenu1.Click -= _sharedEventHandlers.Get(AreaServices.JumpToTool);
					_jumpMenu2.Click -= _sharedEventHandlers.Get(AreaServices.JumpToTool);
					_jumpMenu1.Dispose();
					_jumpMenu2.Dispose();
					_recordBrowseView.ContextMenuStrip.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
					_partiallySharedForToolsWideMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_sharedEventHandlers = null;
				_recordBrowseView = null;
				_recordList = null;
				_jumpMenu1 = null;
				_jumpMenu2 = null;
				_partiallySharedForToolsWideMenuHelper = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}