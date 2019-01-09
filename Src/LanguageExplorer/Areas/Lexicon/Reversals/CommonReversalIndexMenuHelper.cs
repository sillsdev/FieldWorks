// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Reversals
{
	/// <summary />
	internal sealed class CommonReversalIndexMenuHelper : IDisposable
	{
		private const int InsertReversalEntryImageIndex = 2;
		private const int FindReversalEntryImageIndex = 3;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList _recordList;
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		private ToolStripButton _insertReversalEntryToolStripButton;
		private ToolStripButton _insertGoToReversalEntryToolStripButton;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		/// <summary />
		public CommonReversalIndexMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			// It is really an instance of AllReversalEntriesRecordList.
			_recordList = recordList;
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(_majorFlexComponentParameters, recordList);
		}

		internal void Initialize()
		{
			_lexiconAreaMenuHelper.Initialize();
			SetupInsertToolbar();
			SetupEditMenu();
			SetupInsertMenu();
		}

		private void SetupEditMenu()
		{
			using (var lexEntryImages = new LexEntryImages())
			{
				// <command id="CmdGoToReversalEntry" label="_Find reversal entry..." message="GotoReversalEntry" icon="gotoReversalEntry" shortcut="Ctrl+F" a10status="Only used in two reversal tools in Lex area">
				_editMenu = MenuServices.GetEditMenu(_majorFlexComponentParameters.MenuStrip);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, GotoReversalEntryClicked, LexiconResources.FindReversalEntry, LexiconResources.FindReversalEntryTooltip, Keys.Control | Keys.F, lexEntryImages.buttonImages.Images[FindReversalEntryImageIndex]);
			}
			_editMenu.DropDownOpening += EditMenu_DropDownOpening;
		}

		private void EditMenu_DropDownOpening(object sender, EventArgs e)
		{
			SetEnabledStateForGotoReversalEntry(_newEditMenusAndHandlers[0].Item1);
		}

		private void SetupInsertMenu()
		{
			using (var lexEntryImages = new LexEntryImages())
			{
				// <command id="CmdInsertReversalEntry" label="Reversal Entry" message="InsertItemInVector" icon="reversalEntry" a10status="Only used in two reversal tools in Lex area">
				_insertMenu = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, InsertReversalEntryClicked, LexiconResources.ReversalEntry,
					LexiconResources.CreateNewReversalEntryTooltip, image: lexEntryImages.buttonImages.Images[InsertReversalEntryImageIndex], insertIndex: 0);
			}
		}

		private void SetupInsertToolbar()
		{
			using (var lexEntryImages = new LexEntryImages())
			{
				/*
					<command id="CmdInsertReversalEntry" label="Reversal Entry" message="InsertItemInVector" icon="reversalEntry" a10status="Only used in two reversal tools in Lex area">
					  <parameters className="ReversalIndexEntry" />
					</command>
				 */
				_insertReversalEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(InsertReversalEntryClicked, "insertReversalEntryToolStripButton", lexEntryImages.buttonImages.Images[InsertReversalEntryImageIndex], LexiconResources.CreateNewReversalEntryTooltip);
				// CmdGoToReversalEntry is used in:
				//		1. Main "Edit" menu.
				//		2. Insert toolbar.
				/*
					<command id="CmdGoToReversalEntry" label="_Find reversal entry..." message="GotoReversalEntry" icon="gotoReversalEntry" shortcut="Ctrl+F" a10status="Only used in two reversal tools in Lex area">
					  <parameters title="Go To Entry" formlabel="Go _To..." okbuttonlabel="_Go" />
					</command>
				 */
				_insertGoToReversalEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(GotoReversalEntryClicked, "insertGoToReversalEntryToolStripButton", lexEntryImages.buttonImages.Images[FindReversalEntryImageIndex], LexiconResources.FindReversalEntryTooltip);

				InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripItem> { _insertReversalEntryToolStripButton, _insertGoToReversalEntryToolStripButton });

				Application.Idle += Application_Idle;
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

		/// <summary />
		private void GotoReversalEntryClicked(object sender, EventArgs e)
		{
			using (var dlg = new ReversalEntryGoDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ReversalIndex = Entry.ReversalIndex;
				var cache = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
				dlg.SetDlgInfo(cache, null);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					// Can't Go to a subentry, so we have to go to its main entry.
					var selEntry = (IReversalIndexEntry)dlg.SelectedObject;
					LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(null, selEntry.Guid));
				}
			}
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			SetEnabledStateForGotoReversalEntry(_insertGoToReversalEntryToolStripButton);
		}

		private void SetEnabledStateForGotoReversalEntry(ToolStripItem tsi)
		{
			var rie = Entry;
			tsi.Enabled = rie != null && rie.Owner.Hvo != 0 && rie.ReversalIndex.EntriesOC.Any();
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
			var lp = m_propertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).LanguageProject;
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
				var cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
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
				Application.Idle -= Application_Idle;
				_lexiconAreaMenuHelper?.Dispose();
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertReversalEntryToolStripButton.Click -= InsertReversalEntryClicked;
				_insertReversalEntryToolStripButton.Dispose();
				_insertGoToReversalEntryToolStripButton.Click -= GotoReversalEntryClicked;
				_insertGoToReversalEntryToolStripButton.Dispose();
			}

			_majorFlexComponentParameters = null;
			_recordList = null;
			_lexiconAreaMenuHelper = null;
			_insertReversalEntryToolStripButton = null;
			_insertGoToReversalEntryToolStripButton = null;
			_editMenu = null;
			_newEditMenusAndHandlers = null;
			_insertMenu = null;
			_newInsertMenusAndHandlers = null;

			_isDisposed = true;
		}
		#endregion
	}
}
