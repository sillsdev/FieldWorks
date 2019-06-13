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
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookDocument
{
	/// <summary>
	/// ITool implementation for the "notebookDocument" tool in the "notebook" area.
	/// </summary>
	[Export(AreaServices.NotebookAreaMachineName, typeof(ITool))]
	internal sealed class NotebookDocumentTool : ITool
	{
		private NotebookDocumentToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private IRecordList _recordList;
		[Import(AreaServices.NotebookAreaMachineName)]
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
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(NotebookArea.Records, majorFlexComponentParameters.StatusBar, NotebookArea.NotebookFactoryMethod);
			}
			// NB: The constructor will create the ToolUiWidgetParameterObject instance and register events.
			// NB: This has to be done ahead of XmlDocView, since it registers handlers as a UserControl, which will throw if the tool has been set up yet.
			_toolMenuHelper = new NotebookDocumentToolMenuHelper(majorFlexComponentParameters, this, _recordList);
			// NB: XmlDocView adds user control handler.
			var docView = new XmlDocView(XDocument.Parse(NotebookResources.NotebookDocumentParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, docView);
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
		public string MachineName => AreaServices.NotebookDocumentToolMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Document";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.DocumentView.SetBackgroundColor(Color.Magenta);

		/// <summary>
		/// Get User-visible localizable name for class of object being deleted.
		/// </summary>
		public string UiDeleteObjectName => StringTable.Table.GetString(_recordList.CurrentObject.ClassName, "ClassNames");

		#endregion

		private sealed class NotebookDocumentToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private ITool _tool;
			private SharedNotebookToolsUiWidgetMenuHelper _sharedNotebookToolsUiWidgetMenuHelper;
			private IRecordList _recordList;

			internal NotebookDocumentToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_tool = tool;
				_recordList = recordList;
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(_tool);
				SetupToolUiWidgets(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			}

			private void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
			{
				_sharedNotebookToolsUiWidgetMenuHelper = new SharedNotebookToolsUiWidgetMenuHelper(_majorFlexComponentParameters, _recordList);
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => CanCmdFindAndReplaceText));
				toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => CanCmdFindAndReplaceText));
				_sharedNotebookToolsUiWidgetMenuHelper.SetupToolUiWidgets(toolUiWidgetParameterObject, new HashSet<Command> { Command.CmdExport, Command.CmdInsertRecord, Command.CmdInsertSubrecord, Command.CmdInsertSubsubrecord });
			}

			private static Tuple<bool, bool> CanCmdFindAndReplaceText => new Tuple<bool, bool>(true, true);

			private void EditFindMenu_Click(object sender, EventArgs e)
			{
				_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).ShowFindReplaceDialog(false, _majorFlexComponentParameters.MainWindow.ActiveView as IVwRootSite, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.MainWindow as Form);
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~NotebookDocumentToolMenuHelper()
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
					_sharedNotebookToolsUiWidgetMenuHelper?.Dispose();
				}
				_majorFlexComponentParameters = null;
				_tool = null;
				_sharedNotebookToolsUiWidgetMenuHelper = null;
				_recordList = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}