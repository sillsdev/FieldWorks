// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Areas.Grammar.Tools.GrammarSketch
{
	/// <summary>
	/// ITool implementation for the "grammarSketch" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class GrammarSketchTool : ITool
	{
		private GrammarSketchHtmlViewer _grammarSketchHtmlViewer;
		private GrammarSketchToolMenuHelper _toolMenuHelper;
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
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = null;
			_grammarSketchHtmlViewer = null;
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
			_toolMenuHelper = new GrammarSketchToolMenuHelper(majorFlexComponentParameters, this);
			_grammarSketchHtmlViewer = new GrammarSketchHtmlViewer
			{
				Dock = DockStyle.Fill
			};
			StatusBarPanelServices.SetStatusPanelRecordNumber(majorFlexComponentParameters.StatusBar, string.Empty);
			_grammarSketchHtmlViewer.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = _grammarSketchHtmlViewer;
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
		public string MachineName => AreaServices.GrammarSketchMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.GrammarSketchUiName);

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

		#endregion
		/// <summary>
		/// Handle creation and use of the grammar sketch tool menus.
		/// </summary>
		private sealed class GrammarSketchToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;

			internal GrammarSketchToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));

				_majorFlexComponentParameters = majorFlexComponentParameters;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File].Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(FileExportMenu_Click, () => CanCmdExport));
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			}

			private static Tuple<bool, bool> CanCmdExport => new Tuple<bool, bool>(true, true);

			private void FileExportMenu_Click(object sender, EventArgs e)
			{
				using (var dlg = new ExportDialog(_majorFlexComponentParameters.StatusBar))
				{
					dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					dlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<Form>(FwUtils.window));
				}
			}

			private static Tuple<bool, bool> CanCmdRefresh => new Tuple<bool, bool>(true, false);

			#region IDisposable
			private bool _isDisposed;

			~GrammarSketchToolMenuHelper()
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
					return; // No need to do it more than once.
				}

				if (disposing)
				{
				}
				_majorFlexComponentParameters = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}