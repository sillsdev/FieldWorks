// Copyright (c) 2005-2019 SIL International
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
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert].Add(Command.CmdInsertReversalEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertReversalEntryClicked, () => CanCmdInsertReversalEntry));
			insertToolBarDictionary.Add(Command.CmdInsertReversalEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertReversalEntryClicked, ()=> CanCmdInsertReversalEntry));
			// <command id="CmdConfigureDictionary" label="{0}" message="ConfigureDictionary"/>
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools].Add(Command.CmdConfigureDictionary, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Tools_Configure_Dictionary_Clicked, () => CanCmdConfigureDictionary));
			_majorFlexComponentParameters.UiWidgetController.ToolsMenuDictionary[Command.CmdConfigureDictionary].Text = LexiconResources.ReversalIndex;
		}

		private static Tuple<bool, bool> CanCmdConfigureDictionary => new Tuple<bool, bool>(true, true);

		private void Tools_Configure_Dictionary_Clicked(object sender, EventArgs e)
		{
			var mainWnd = _majorFlexComponentParameters.MainWindow;
			if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)mainWnd, _recordList.CurrentObject, "khtpConfigReversalIndex", LanguageExplorerResources.ReversalIndex))
			{
				mainWnd.RefreshAllViews();
			}
		}

		private static Tuple<bool, bool> CanCmdInsertReversalEntry => new Tuple<bool, bool>(true, true);

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

#if RANDYTODO
		/// <summary>
		/// Called (by xcore) to control display params of the reversal index menu, e.g. whether it should be enabled.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayReversalIndexHvo(object commandObject, ref UIItemDisplayProperties display)
		{
			// Do NOT check InFriendlyArea. This menu should be enabled in every context where it occurs at all.
			// And, it gets tested during creation of the pane bar, BEFORE the properties InFriendlyArea uses
			// are set, so we get inaccurate answers.
			display.Enabled = true; // InFriendlyArea;
			display.Visible = display.Enabled;

			return true; // We dealt with it.
		}

		/// <summary>
		/// This is called when XCore wants to display something that relies on the list with the
		/// id "ReversalIndexList"
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayReversalIndexList(object parameter, ref UIListDisplayProperties display)
		{
			display.List.Clear();
			var lp = m_propertyTable.GetValue<LcmCache>(FwUtils.cache).LanguageProject;
			// List all existing reversal indexes.  (LT-4479, as amended)
			// But only for analysis wss
			foreach (IReversalIndex ri in from ri in lp.LexDbOA.ReversalIndexesOC
										  where lp.AnalysisWss.Contains(ri.WritingSystem)
										  select ri)
			{
				display.List.Add(ri.ShortName, ri.Guid.ToString(), null, null);
			}
			display.List.Sort();
			return true; // We handled this, no need to ask anyone else.
		}

		/// <summary>
		/// decide whether to enable this tree delete Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayDataTreeMerge(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanMergeOrMove;
			if(!display.Enabled)
				display.Text += StringTable.Table.GetString("(cannot merge this)");

			return true;//we handled this, no need to ask anyone else.
		}

		public bool OnPromoteReversalindexEntry(object cmd)
		{
			//Command command = (Command) cmd;
			Slice slice = m_dataTree.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			if (slice != null)
			{
				LcmCache cache = m_dataTree.Cache;
				IReversalIndexEntry entry = slice.Object as IReversalIndexEntry;
				ICmObject newOwner = entry.Owner.Owner;
				UndoableUnitOfWorkHelper.Do(((Command)cmd).UndoText, ((Command)cmd).RedoText, newOwner,
					() =>
						{
							switch (newOwner.ClassID)
							{
								default:
									throw new ArgumentException("Illegal class.");
								case ReversalIndexTags.kClassId:
									{
										IReversalIndex ri = (IReversalIndex) newOwner;
										ri.EntriesOC.Add(entry);
										break;
									}
								case ReversalIndexEntryTags.kClassId:
									{
										IReversalIndexEntry rie = (IReversalIndexEntry) newOwner;
										rie.SubentriesOS.Add(entry);
										break;
									}
							}
						});
				// We may need to notify everyone that a virtual property changed.
				//NotifyVirtualChanged(cache, slice);
			}
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayPromoteReversalindexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice == null
				|| slice.Object == null
				|| slice.Object.OwningFlid == ReversalIndexTags.kflidEntries) // Can't promote a top-level entry.
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = true;
			}
			return true; //we've handled this
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveReversalindexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice == null || slice.Object == null)
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = CanMergeOrMove;
				display.Visible = InFriendlyArea;
			}
			return true; //we've handled this
		}

		/// <summary />
		public bool OnMoveReversalindexEntry(object cmd)
		{
			using (var dlg = new ReversalEntryGoDlg())
			{
				Slice slice = m_dataTree.CurrentSlice;
				var currentEntry = (IReversalIndexEntry) slice.Object;
				dlg.ReversalIndex = currentEntry.ReversalIndex;
				AddEntryAndChildrenRecursively(dlg.FilteredReversalEntryHvos, currentEntry);
				IReversalIndexEntry owningEntry = currentEntry.OwningEntry;
				if (owningEntry != null)
					dlg.FilteredReversalEntryHvos.Add(owningEntry.Hvo);
				dlg.SetHelpTopic("khtpMoveReversalEntry");
				var wp = new WindowParams { m_btnText = LanguageExplorerResources.ks_MoveEntry, m_title = LanguageExplorerResources.ksMoveRevEntry };
				var cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
				dlg.SetDlgInfo(cache, wp, PropertyTable, Publisher, Subscriber);
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var newOwner = (IReversalIndexEntry) dlg.SelectedObject;
					UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveRevEntry,
						LanguageExplorerResources.ksRedoMoveRevEntry, Cache.ActionHandlerAccessor,
						() =>
						{
							newOwner.MoveIfNeeded(currentEntry);
							newOwner.SubentriesOS.Add(currentEntry);
						});
					var recordList = RecordList.ActiveRecordListRepository.ActiveRecordList;
					recordList?.RemoveItemsFor(currentEntry.Hvo);
					// Note: PropChanged should happen on the old owner and the new while completing the unit of work.
					// Have to jump to a main entry, as RecordList doesn't know anything about subentries.
					Publisher.Publish("JumpToRecord", newOwner.MainEntry.Hvo);
				}
			}
			return true;
		}

		private static void AddEntryAndChildrenRecursively(ICollection<int> hvos, IReversalIndexEntry entry)
		{
			hvos.Add(entry.Hvo);
			entry.AllOwnedObjects.Where(obj => obj is IReversalIndexEntry).ForEach(subentry => hvos.Add(subentry.Hvo));
		}

		private bool CanMergeOrMove
		{
			get
			{
				Debug.Assert(m_dataTree != null && m_dataTree.CurrentSlice != null, "Current slice information is null, how can this menu be drawing?");
				// ReSharper disable HeuristicUnreachableCode
				// ReSharper disable ConditionIsAlwaysTrueOrFalse
				//ReSharper lies.
				if (m_dataTree == null || m_dataTree.CurrentSlice == null)
				{
					return false; //merge is not possible if all is not right with the world
				}
				// ReSharper restore ConditionIsAlwaysTrueOrFalse
				// ReSharper restore HeuristicUnreachableCode
				IReversalIndexEntry rie = m_dataTree.CurrentSlice.Object as IReversalIndexEntry;
				if(rie == null)
					return false;
				// Merge and move are possible if we have more than one entry.
				if (rie.ReversalIndex.EntriesOC.Count > 1)
					return true;
				if (rie.ReversalIndex.EntriesOC.Count == 0)
					return false;
				// exactly 1 top-level entry.
				return rie.ReversalIndex.EntriesOC.ToArray()[0].SubentriesOS.Count > 0;
			}
		}

		/// <summary />
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		private bool InFriendlyArea => (PropertyTable.GetValue<string>(AreaServices.AreaChoice) == AreaServices.LexiconAreaMachineName && PropertyTable.GetValue<string>($"{AreaServices.ToolForAreaNamed_}_{AreaServices.LexiconAreaMachineName}") == AreaServices.ReversalEditCompleteMachineName);
#endif

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
