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
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.InterlinearEdit
{
	/// <summary>
	/// ITool implementation for the "interlinearEdit" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class InterlinearEditTool : ITool
	{
		private InterlinearEditToolMenuHelper _toolMenuHelper;
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		private InterlinMaster _interlinMaster;
		[Import(AreaServices.TextAndWordsAreaMachineName)]
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_recordBrowseView = null;
			_interlinMaster = null;
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
			majorFlexComponentParameters.FlexComponentParameters.PropertyTable.SetDefault($"{AreaServices.ToolForAreaNamed_}{_area.MachineName}", MachineName, true);
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.InterlinearTexts, majorFlexComponentParameters.StatusBar, TextAndWordsArea.InterlinearTextsFactoryMethod);
			}
			var multiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "EditViewTextsMultiPane",
				ToolMachineName = MachineName,
				DefaultFixedPaneSizePoints = "145",
				DefaultPrintPane = "ITextContent",
				DefaultFocusControl = "InterlinMaster"
			};
			var root = XDocument.Parse(TextAndWordsResources.InterlinearEditToolParameters).Root;
			_recordBrowseView = new RecordBrowseView(root.Element("recordbrowseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_interlinMaster = new InterlinMaster(root.Element("interlinearmaster").Element("parameters"), majorFlexComponentParameters, _recordList);
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, multiPaneParameters, _recordBrowseView, "Texts", new PaneBar(), _interlinMaster, "Text", new PaneBar());
			_multiPane.FixedPanel = FixedPanel.Panel1;

			// Too early before now.
			// NB: The constructor will create the ToolUiWidgetParameterObject instance and register events.
			_toolMenuHelper = new InterlinearEditToolMenuHelper(this, majorFlexComponentParameters, _recordBrowseView, _recordList);
			_interlinMaster.FinishInitialization();
			_interlinMaster.BringToFront();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_interlinMaster.PrepareToRefresh();
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();

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
		public string MachineName => AreaServices.InterlinearEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Interlinear Texts";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.EditView.SetBackgroundColor(Color.Magenta);

		/// <summary>
		/// Get User-visible localizable name for class of object being deleted.
		/// </summary>
		public string UiDeleteObjectName => StringTable.Table.GetString(_recordList.CurrentObject.ClassName, "ClassNames");

		#endregion

		/// <summary>
		/// This class handles all interaction for the InterlinearEditTool for its menus, tool bars, plus all context menus that are used in Slices and PaneBars.
		/// </summary>
		private sealed class InterlinearEditToolMenuHelper : IDisposable
		{
			private ITool _tool;
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;

			internal InterlinearEditToolMenuHelper(ITool tool, MajorFlexComponentParameters majorFlexComponentParameters, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_tool = tool;
				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(_tool);
				// Do nothing registration is needed, in case there are any user controls that want to register.
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
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("StText", "ClassNames")));

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~InterlinearEditToolMenuHelper()
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
					_recordBrowseView.ContextMenuStrip?.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
				}
				_tool = null;
				_majorFlexComponentParameters = null;
				_recordBrowseView = null;
				_recordList = null;

				_isDisposed = true;
			}

			#endregion
		}
	}
}