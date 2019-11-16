// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools
{
	/// <summary>
	/// Shared for these lexicon area tools: LexiconEditTool, LexiconBrowseTool, LexiconDictionaryTool, and BulkEditEntriesOrSensesTool.
	/// </summary>
	internal sealed class SharedLexiconToolsUiWidgetHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList _recordList;

		internal SharedLexiconToolsUiWidgetHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_recordList = recordList;
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject, HashSet<Command> commands)
		{
			var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
			var toolsMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools];
			var insertToolBarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];

			foreach (var command in commands)
			{
				switch (command)
				{
					case Command.CmdGoToEntry:
						// <item command="CmdGoToEntry" />
						InsertPair(insertToolBarDictionary, toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit], Command.CmdGoToEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GoToEntry_Clicked, () => UiWidgetServices.CanSeeAndDo));
						break;
					case Command.CmdInsertLexEntry:
						// <item command="CmdInsertLexEntry" defaultVisible="false" />
						InsertPair(insertToolBarDictionary, insertMenuDictionary, Command.CmdInsertLexEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Entry_Clicked, () => UiWidgetServices.CanSeeAndDo));
						break;
					case Command.CmdConfigureDictionary:
						// <command id="CmdConfigureDictionary" label="{0}" message="ConfigureDictionary"/>
						toolsMenuDictionary.Add(Command.CmdConfigureDictionary, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Tools_Configure_Dictionary_Clicked, () => UiWidgetServices.CanSeeAndDo));
						_majorFlexComponentParameters.UiWidgetController.ToolsMenuDictionary[Command.CmdConfigureDictionary].Text = AreaResources.ConfigureDictionary;
						break;
					default:
						throw new ArgumentOutOfRangeException($"Dont know how to process command: '{command.ToString()}'");
				}
			}
		}

		private static void InsertPair(IDictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> toolBarDictionary, IDictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> menuDictionary, Command key, Tuple<EventHandler, Func<Tuple<bool, bool>>> currentTuple)
		{
			toolBarDictionary.Add(key, currentTuple);
			menuDictionary.Add(key, currentTuple);
		}

		private void Tools_Configure_Dictionary_Clicked(object sender, EventArgs e)
		{
			var mainWnd = _majorFlexComponentParameters.MainWindow;
			if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)mainWnd, _recordList.CurrentObject, "khtpConfigureDictionary", LanguageExplorerResources.Dictionary))
			{
				mainWnd.RefreshAllViews();
			}
		}

		private void GoToEntry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new EntryGoDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				var windowParameters = new WindowParams
				{
					m_btnText = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go),
					m_label = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go_To),
					m_title = LexiconResources.Go_To_Entry_Dlg_Title
				};
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, windowParameters);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
				{
					_recordList.JumpToRecord(dlg.SelectedObject.Hvo);
				}
			}
		}

		private void Insert_Entry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new InsertEntryDlg())
			{
				var mainWnd = _majorFlexComponentParameters.MainWindow;
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, PersistenceProviderFactory.CreatePersistenceProvider(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable));
				if (dlg.ShowDialog((Form)mainWnd) != DialogResult.OK)
				{
					return;
				}
				ILexEntry entry;
				bool newby;
				dlg.GetDialogInfo(out entry, out newby);
				// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
				mainWnd.RefreshAllViews();
				_recordList.JumpToRecord(entry.Hvo);
			}
		}

		#region IDisposable
		private bool _isDisposed;

		~SharedLexiconToolsUiWidgetHelper()
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
			_recordList = null;

			_isDisposed = true;
		}
		#endregion
	}
}