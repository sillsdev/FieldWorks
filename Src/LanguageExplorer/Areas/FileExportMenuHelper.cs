// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Set up File->Export menu for either an area XOR a tool in an area.
	/// </summary>
	internal sealed class FileExportMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;

		internal FileExportMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
		}

		/// <summary>
		/// Setup the File->Export menu.
		/// </summary>
		internal void SetupFileExportMenu(AreaUiWidgetParameterObject areaUiWidgetParameterObject)
		{
			// File->Export menu is visible and enabled in this area.
			areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.File].Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CommonFileExportMenu_Click, () => CanCmdExport));
		}

		/// <summary>
		/// Setup the File->Export menu.
		/// </summary>
		internal void SetupFileExportMenu(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			// File->Export menu is visible and enabled in this tool.
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File].Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CommonFileExportMenu_Click, () => CanCmdExport));
		}

		private Tuple<bool, bool> CanCmdExport => new Tuple<bool, bool>(true, !_majorFlexComponentParameters.LcmCache.GetManagedMetaDataCache().AreCustomFieldsAProblem(new[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }));

		private void CommonFileExportMenu_Click(object sender, EventArgs e)
		{
			// This handles the general case, if nobody else is handling it.
			// Areas/Tools that uses this code:
			// A. lexicon area: all 8 tools
			// B. textsWords area: Analyses, bulkEditWordforms, wordListConcordance
			// C. grammar area: all tools, except grammarSketch, which goes its own way
			// D. lists area: all 27 tools
			using (var dlg = new ExportDialog(_majorFlexComponentParameters))
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		#region IDisposable
		private bool _isDisposed;

		~FileExportMenuHelper()
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
			_majorFlexComponentParameters = null;

			_isDisposed = true;
		}
		#endregion
	}
}