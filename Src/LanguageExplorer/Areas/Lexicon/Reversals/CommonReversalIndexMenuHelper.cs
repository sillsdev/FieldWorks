// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Reversals
{
	/// <summary />
	internal sealed class CommonReversalIndexMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList _recordList;

		/// <summary />
		internal CommonReversalIndexMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_recordList = recordList;
		}

		internal void SetupUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			Guard.AgainstNull(toolUiWidgetParameterObject, nameof(toolUiWidgetParameterObject));

			var insertToolBarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
			// <command id="CmdGoToReversalEntry" label="_Find reversal entry..." message="GotoReversalEntry" icon="gotoReversalEntry" shortcut="Ctrl+F" a10status="Only used in two reversal tools in Lex area">
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit].Add(Command.CmdGoToReversalEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GotoReversalEntryClicked, ()=> CanCmdGoToReversalEntry));
			insertToolBarDictionary.Add(Command.CmdGoToReversalEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GotoReversalEntryClicked, () => CanCmdGoToReversalEntry));
			// <command id="CmdInsertReversalEntry" label="Reversal Entry" message="InsertItemInVector" icon="reversalEntry" a10status="Only used in two reversal tools in Lex area">
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert].Add(Command.CmdInsertReversalEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertReversalEntryClicked, () => UiWidgetServices.CanSeeAndDo));
			insertToolBarDictionary.Add(Command.CmdInsertReversalEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertReversalEntryClicked, ()=> UiWidgetServices.CanSeeAndDo));
			// <command id="CmdConfigureDictionary" label="{0}" message="ConfigureDictionary"/>
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools].Add(Command.CmdConfigureDictionary, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Tools_Configure_Dictionary_Clicked, () => UiWidgetServices.CanSeeAndDo));
			_majorFlexComponentParameters.UiWidgetController.ToolsMenuDictionary[Command.CmdConfigureDictionary].Text = LexiconResources.ReversalIndex;
		}

		private void Tools_Configure_Dictionary_Clicked(object sender, EventArgs e)
		{
			var mainWnd = _majorFlexComponentParameters.MainWindow;
			if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)mainWnd, _recordList.CurrentObject, "khtpConfigReversalIndex", LanguageExplorerResources.ReversalIndex))
			{
				mainWnd.RefreshAllViews();
			}
		}

		private void InsertReversalEntryClicked(object sender, EventArgs e)
		{
			// The new one will go into the index or into another entry,
			// depending on the owner of the selected entry, if one is selected.
			UowHelpers.UndoExtension(LexiconResources.ReversalEntry, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
			{
				var newReversalEntry = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
				var selectedEntry = _recordList.CurrentObject;
				if (selectedEntry == null || selectedEntry.Owner is IReversalIndex)
				{
					// Goes into index.
					((IReversalIndex)_recordList.OwningObject).EntriesOC.Add(newReversalEntry);
				}
				else
				{
					// Goes into entry.
					((IReversalIndexEntry)selectedEntry.Owner).SubentriesOS.Add(newReversalEntry);
				}
			});
		}

		private Tuple<bool, bool> CanCmdGoToReversalEntry
		{
			get
			{
				var rie = Entry;
				return new Tuple<bool, bool>(true, rie != null && rie.Owner.Hvo != 0 && rie.ReversalIndex.EntriesOC.Any());
			}
		}

		/// <summary />
		private void GotoReversalEntryClicked(object sender, EventArgs e)
		{
			using (var dlg = new ReversalEntryGoDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ReversalIndex = Entry.ReversalIndex;
				var cache = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<LcmCache>(FwUtils.cache);
				dlg.SetDlgInfo(cache, null);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					// Can't Go to a subentry, so we have to go to its main entry.
					var selEntry = (IReversalIndexEntry)dlg.SelectedObject;
					LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(null, selEntry.Guid));
				}
			}
		}

		private IReversalIndexEntry Entry => _recordList.CurrentObject as IReversalIndexEntry;

		#region IDisposable
		private bool _isDisposed;

		~CommonReversalIndexMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
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
