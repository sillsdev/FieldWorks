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
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists.Tools.SemanticDomainEdit
{
	/// <summary>
	/// ITool implementation for the "semanticDomainEdit" tool in the "lists" area.
	/// </summary>
	[Export(AreaServices.ListsAreaMachineName, typeof(ITool))]
	internal sealed class SemanticDomainEditTool : ITool
	{
		private const string SemanticDomainList_ListArea = "SemanticDomainList_ListArea";
		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
		private SemanticDomainEditMenuHelper _toolMenuHelper;
		private IRecordList _recordList;
		[Import(AreaServices.ListsAreaMachineName)]
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
			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();
			_toolMenuHelper = null;
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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(SemanticDomainList_ListArea, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}

			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_toolMenuHelper = new SemanticDomainEditMenuHelper(majorFlexComponentParameters, this, majorFlexComponentParameters.LcmCache.LanguageProject.SemanticDomainListOA, _recordList, dataTree);
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				true, XDocument.Parse(ListResources.SemanticDomainEditParameters).Root, XDocument.Parse(ListResources.ListToolsSliceFilters), MachineName,
				majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);

			// Too early before now.
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("ShowHiddenFields", true);
			}
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
		public string MachineName => AreaServices.SemanticDomainEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Semantic Domains";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == SemanticDomainList_ListArea, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{SemanticDomainList_ListArea}'.");

			/*
            <clerk id="SemanticDomainList">
              <recordList owner="LangProject" property="SemanticDomainList">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.PossibilityRecordList" />
              </recordList>
              <treeBarHandler assemblyPath="xWorks.dll" expand="false" hierarchical="true" includeAbbr="true" ws="best analysis" class="SIL.FieldWorks.XWorks.SemanticDomainRdeTreeBarHandler" altTitleId="SemanticDomain-Plural" />
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new TreeBarHandlerAwarePossibilityRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				cache.LanguageProject.SemanticDomainListOA, new PossibilityTreeBarHandler(flexComponentParameters.PropertyTable, false, true, true, "best analysis"));
		}

		private sealed class SemanticDomainEditMenuHelper : IDisposable
		{
			private readonly MajorFlexComponentParameters _majorFlexComponentParameters;
			private readonly ICmPossibilityList _list;
			private readonly IRecordList _recordList;

			internal SemanticDomainEditMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, ICmPossibilityList list, IRecordList recordList, DataTree dataTree)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(list, nameof(list));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(dataTree, nameof(dataTree));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_list = list;
				_recordList = recordList;
				SetupToolUiWidgets(tool, dataTree);
			}

			private void SetupToolUiWidgets(ITool tool, DataTree dataTree)
			{
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
				var insertToolbarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
				// <command id="CmdInsertSemDom" label="_Semantic Domain" message="InsertItemInVector" icon="AddItem">
				// <command id="CmdDataTree-Insert-SemanticDomain" label="Insert subdomain" message="DataTreeInsert" icon="AddSubItem">
				// Insert menu & tool bar
				insertMenuDictionary.Add(Command.CmdInsertSemDom, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertSemDom_Click, ()=> CanCmdInsertSemDom));
				insertToolbarDictionary.Add(Command.CmdInsertSemDom, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertSemDom_Click, () => CanCmdInsertSemDom));
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_SemanticDomain, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_SemanticDomain_Click, () => CanCmdDataTree_Insert_SemanticDomain));
				insertToolbarDictionary.Add(Command.CmdDataTree_Insert_SemanticDomain, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_SemanticDomain_Click, () => CanCmdDataTree_Insert_SemanticDomain));

				/*
					<part id="CmSemanticDomain-Detail-Questions" type="Detail">
						<slice label="Questions" menu="mnuDataTree-InsertQuestion">
						  <seq field="Questions"/>
						</slice>
					</part>
				*/
				dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_InsertQuestion, Create_mnuDataTree_InsertQuestion);
				/*
					<part id="CmDomainQ-Detail-QuestionAllA" type="Detail">
						<slice field="Question" label="Question" editor="multistring" ws="all analysis" menu="mnuDataTree-DeleteQuestion">
						</slice>
					</part>
				*/
				dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_DeleteQuestion, Create_mnuDataTree_DeleteQuestion);
				// <menu id="mnuDataTree-SubSemanticDomain">
				dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubSemanticDomain, Create_mnuDataTree_SubSemanticDomain);

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubSemanticDomain(Slice slice, string contextMenuId)
			{
				Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubSemanticDomain, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubSemanticDomain}', but got '{contextMenuId}' instead.");

				// Start: <menu id="mnuDataTree-SubSemanticDomain">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ListsAreaConstants.mnuDataTree_SubSemanticDomain
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
					<item command="CmdDataTree-Insert-SemanticDomain" />
				*/
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Insert_SemanticDomain_Click, ListResources.Insert_Subdomain, image: AreaResources.AddSubItem.ToBitmap());

				// End: <menu id="mnuDataTree-SubSemanticDomain">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_InsertQuestion(Slice slice, string contextMenuId)
			{
				Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_InsertQuestion, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_InsertQuestion}', but got '{contextMenuId}' instead.");

				// Start: <menu id="mnuDataTree-InsertQuestion">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ListsAreaConstants.mnuDataTree_InsertQuestion
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
					<command id="CmdDataTree-Insert-Question" label="Insert Question" message="DataTreeInsert" icon="AddSubItem">
					  <parameters field="Questions" className="CmDomainQ" />
					</command> // Insert_Question
				*/
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, InsertQuestion_Clicked, ListResources.Insert_Question, image: AreaResources.AddSubItem.ToBitmap());

				// End: <menu id="mnuDataTree-InsertQuestion">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void InsertQuestion_Clicked(object sender, EventArgs e)
			{
				var currentSemanticDomain = (ICmSemanticDomain)_recordList.CurrentObject;
				var cache = currentSemanticDomain.Cache;
				UowHelpers.UndoExtension(ListResources.Insert_Question, cache.ActionHandlerAccessor, () =>
				{
					currentSemanticDomain.QuestionsOS.Add(cache.ServiceLocator.GetInstance<ICmDomainQFactory>().Create());
				});
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_DeleteQuestion(Slice slice, string contextMenuId)
			{
				Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_DeleteQuestion, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_DeleteQuestion}', but got '{contextMenuId}' instead.");

				// Start: <menu id="mnuDataTree-Delete-Question">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ListsAreaConstants.mnuDataTree_DeleteQuestion
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
					<command id="CmdDataTree-Delete-Question" label="Delete Question" message="DataTreeDelete" icon="Delete">
						<parameters field="Questions" className="CmDomainQ" />
					</command>
				*/
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, ListResources.Delete_Question, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree-Delete-Question">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private static Tuple<bool, bool> CanCmdInsertSemDom => new Tuple<bool, bool>(true, true);

			private void CmdInsertSemDom_Click(object sender, EventArgs e)
			{
				var newPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create(Guid.NewGuid(), _list);
				if (newPossibility != null)
				{
					_recordList.UpdateRecordTreeBar();
				}
			}

			private Tuple<bool, bool> CanCmdDataTree_Insert_SemanticDomain => new Tuple<bool, bool>(true, _recordList.CurrentObject != null);

			private void CmdDataTree_Insert_SemanticDomain_Click(object sender, EventArgs e)
			{
				var newPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create(Guid.NewGuid(), (ICmSemanticDomain)_recordList.CurrentObject);
				if (newPossibility != null)
				{
					_recordList.UpdateRecordTreeBar();
				}
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~SemanticDomainEditMenuHelper()
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
				}

				_isDisposed = true;
			}
			#endregion
		}
	}
}