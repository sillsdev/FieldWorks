// Copyright (c) 2004-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EntryDlgListener.cs
// Responsibility: Randy Regnier
using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.LexText.Controls
{
#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: InsertEntryDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Lexicon\
	// TODO: when the time comes to re-enable the Lexicon area tools that use it.
	// TODO: NB: Are Lexicon tools the only ones that allow entry inserts?
	//
	// TODO: Likely disposition: Dump InsertEntryDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the insertion.
	//
	/// <summary>
	/// Listener class for the InsertEntryDlg class.
	/// </summary>
	public class InsertEntryDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "InsertLexEntry"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

		public InsertEntryDlgListener()
		{
		}

	#endregion Construction and Initialization

#if RANDYTODO
	#region XCORE Message Handlers

		/// <summary>
		/// Handles the xWorks message to insert a new lexical entry.
		/// Invoked by the RecordClerk
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true, if we handled the message, otherwise false, if there was an unsupported 'classname' parameter</returns>
		public bool OnDialogInsertItemInVector(object argument)
		{
			CheckDisposed();

			Debug.Assert(argument != null && argument is XCore.Command);
			string className = XmlUtils.GetOptionalAttributeValue(
				(argument as Command).Parameters[0],
				"className");
			if (className == null || className != "LexEntry")
				return false;

			using (InsertEntryDlg dlg = new InsertEntryDlg())
			{
				FdoCache cache = m_propertyTable.GetValue<FdoCache>("cache");
				Debug.Assert(cache != null);
				dlg.SetDlgInfo(cache, m_mediator, m_propertyTable, m_persistProvider);
				if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
				{
					ILexEntry entry;
					bool newby;
					dlg.GetDialogInfo(out entry, out newby);
					// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
					m_mediator.SendMessage("JumpToRecord", entry.Hvo);
				}
			}
			return true; // We "handled" the message, regardless of what happened.
		}

	#endregion XCORE Message Handlers
#endif
	}
#endif

#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: MergeEntryDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Lexicon\Tools\Edit\
	// TODO: when the time comes to re-enable the Lexicon area "lexiconEdit" tool (Only one that uses it.).
	//
	// TODO: Likely disposition: Dump MergeEntryDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the merge.
	//
	public class MergeEntryDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "MergeEntry"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

		public MergeEntryDlgListener()
		{
		}

	#endregion Construction and Initialization

	#region XCORE Message Handlers

		/// <summary>
		/// Handles the xCore message to merge two lexical entries.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnMergeEntry(object argument)
		{
			CheckDisposed();

			return RunMergeEntryDialog(argument, true);
		}

		private bool RunMergeEntryDialog(object argument, bool fLoseNoTextData)
		{
			ICmObject obj = PropertyTable.GetValue<ICmObject>("ActiveClerkSelectedObject");
			Debug.Assert(obj != null);
			if (obj == null)
				return false;		// should never happen, but nothing we can do if it does!
			FdoCache cache = PropertyTable.GetValue<FdoCache>("cache");
			Debug.Assert(cache != null);
			Debug.Assert(cache == obj.Cache);
			ILexEntry currentEntry = obj as ILexEntry;
			if (currentEntry == null)
			{
				currentEntry = obj.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
			}
			Debug.Assert(currentEntry != null);
			if (currentEntry == null)
				return false;

			using (MergeEntryDlg dlg = new MergeEntryDlg())
			{
				dlg.SetDlgInfo(cache, PropertyTable, currentEntry);
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var survivor = dlg.SelectedObject as ILexEntry;
					Debug.Assert(survivor != currentEntry);
					UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoMergeEntry,
						LexTextControls.ksRedoMergeEntry, cache.ActionHandlerAccessor,
						() =>
							{
								survivor.MergeObject(currentEntry, fLoseNoTextData);
								survivor.DateModified = DateTime.Now;
							});
					MessageBox.Show(null,
						LexTextControls.ksEntriesHaveBeenMerged,
						LexTextControls.ksMergeReport,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
#if RANDYTODO
				// TODO: Publish doublet:
				// TODO:	1. "AboutToFollowLink" with null new value
				// TODO:	2. "FollowLink" with new FwLinkArgs instance.
				//Publisher.Publish("AboutToFollowLink", null);
#endif
					Publisher.Publish("JumpToRecord", survivor.Hvo);
				}
			}
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayMergeEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			var command = (Command)commandObject;
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Determines in which menus the Merge Entries command item can show up in.
		/// Should only be in the Lexicon area.
		/// </summary>
		/// <remarks>Obviously copied from another area that had more complex criteria for displaying its menu items.</remarks>
		/// <returns>true if Merge Entry ought to be displayed, false otherwise.</returns>
		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				if (areaChoice == null) return false; // happens at start up
				if ("lexicon" == areaChoice)
				{
					return PropertyTable.GetValue<string>("currentContentControl") == "lexiconEdit";
				}
				return false; //we are not in an area that wants to see the merge command
			}
		}
	#endregion XCORE Message Handlers
	}
#endif

#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: GoLinkEntryDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Lexicon\
	// TODO: when the time comes to re-enable the Lexicon area tools that use it.
	// TODO: Check "InFriendlyArea" property, as not all tools use this listener.
	//
	// TODO: Likely disposition: Dump GoLinkEntryDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers do the jump.
	//
	/// <summary>
	/// Listener class for the GoLinkEntryDlgListener class.
	/// </summary>
	public class GoLinkEntryDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "GoLinkLexEntry"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

		public GoLinkEntryDlgListener()
		{
		}

	#endregion Construction and Initialization

	#region XCORE Message Handlers

		/// <summary>
		/// Handles the xCore message to go to or link to a lexical entry.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoLexEntry(object argument)
		{
			CheckDisposed();

			using (var dlg = new EntryGoDlg())
			{
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				dlg.SetDlgInfo(cache, null, PropertyTable, Publisher);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					Publisher.Publish("JumpToRecord", dlg.SelectedObject.Hvo);
				}
			}
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayGotoLexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible. It is difficult because some commands, like this one, may be appropriate
		/// from more than 1 area.</remarks>
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				if (PropertyTable.GetValue<string>("ToolForAreaNamed_lexicon") == "reversalEditComplete")
					return false;

				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				string[] areas = new string[]{"lexicon"};
				foreach(string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						var obj = PropertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
						return (obj != null) && (obj.ClassID == LexDbTags.kClassId);
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}

	#endregion XCORE Message Handlers
	}
#endif
}
