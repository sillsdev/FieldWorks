// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EntryDlgListener.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Xml;
using System.Windows.Forms;
using System.Collections;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Listener class for the InsertEntryDlg class.
	/// </summary>
	public class InsertEntryDlgListener : DlgListenerBase
	{
		#region Properties

		protected override string PersistentLabel
		{
			get { return "InsertLexEntry"; }
		}

		#endregion Properties

		#region Construction and Initialization

		public InsertEntryDlgListener()
		{
		}

		#endregion Construction and Initialization

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
				(argument as XCore.Command).Parameters[0],
				"className");
			if (className == null || className != "LexEntry")
				return false;

			using (InsertEntryDlg dlg = new InsertEntryDlg())
			{
				FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				Debug.Assert(cache != null);
				dlg.SetDlgInfo(cache, m_mediator, m_persistProvider);
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					int entryID;
					bool newby;
					dlg.GetDialogInfo(out entryID, out newby);
					// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
					m_mediator.SendMessage("JumpToRecord", entryID);
				}
			}
			return true; // We "handled" the message, regardless of what happened.
		}

		#endregion XCORE Message Handlers
	}

	public class MergeEntryDlgListener : DlgListenerBase
	{
		#region Properties

		protected override string PersistentLabel
		{
			get { return "MergeEntry"; }
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
			ICmObject obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject") as ICmObject;
			Debug.Assert(obj != null);
			if (obj == null)
				return false;		// should never happen, but nothing we can do if it does!
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			Debug.Assert(cache != null);
			Debug.Assert(cache == obj.Cache);
			ILexEntry currentEntry = obj as ILexEntry;
			if (currentEntry == null)
			{
				int hvoEntry = cache.GetOwnerOfObjectOfClass(obj.Hvo, LexEntry.kclsidLexEntry);
				if (hvoEntry != 0)
					currentEntry = LexEntry.CreateFromDBObject(cache, hvoEntry);
			}
			Debug.Assert(currentEntry != null);
			if (currentEntry == null)
				return false;

			using (MergeEntryDlg dlg = new MergeEntryDlg())
			{
				Debug.Assert(argument != null && argument is XCore.Command);
				dlg.SetDlgInfo(cache, m_mediator, currentEntry);
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					int entryID = dlg.SelectedID;
					ILexEntry survivor = LexEntry.CreateFromDBObject(cache, entryID);
					Debug.Assert(survivor != currentEntry);
					cache.BeginUndoTask(SIL.FieldWorks.LexText.Controls.LexTextControls.ksUndoMergeEntry,
						SIL.FieldWorks.LexText.Controls.LexTextControls.ksRedoMergeEntry);
					// If lexeme forms differ, make the source lexeme form an allomorph of the target entry.
					if (survivor.LexemeFormOA.Form.VernacularDefaultWritingSystem !=
						currentEntry.LexemeFormOA.Form.VernacularDefaultWritingSystem)
					{
						survivor.AlternateFormsOS.Append(currentEntry.LexemeFormOA.Hvo);
					}
					survivor.MergeObject(currentEntry, fLoseNoTextData);
					cache.EndUndoTask();
					survivor.DateModified = DateTime.Now;
					MessageBox.Show(null,
						SIL.FieldWorks.LexText.Controls.LexTextControls.ksEntriesHaveBeenMerged,
						SIL.FieldWorks.LexText.Controls.LexTextControls.ksMergeReport,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					m_mediator.SendMessage("JumpToRecord", entryID);
				}
			}
			return true;
		}

		public virtual bool OnDisplayMergeEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible. It is difficult because some commands, like this one, may be appropriate
		/// from more than 1 area.</remarks>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				if (m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null) == "reversalEditComplete")
					return false;

				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice",
					null);
				string[] areas = new string[]{"lexicon"};
				foreach(string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						CmObject obj = (CmObject)m_mediator.PropertyTable.GetValue("ActiveClerkOwningObject");
						if (obj == null || obj.ClassID != LexDb.kClassId)
							return false;
						obj = (CmObject)m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject");
						return (obj != null && obj.ClassID != LexSense.kClassId); // Disable in bulk edit.
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}

		#endregion XCORE Message Handlers
	}

	/// <summary>
	/// Listener class for the GoLinkEntryDlgListener class.
	/// </summary>
	public class GoLinkEntryDlgListener : DlgListenerBase
	{
		#region Properties

		protected override string PersistentLabel
		{
			get { return "GoLinkLexEntry"; }
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

			using (GoDlg dlg = new GoDlg())
			{
				FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				Debug.Assert(cache != null);
				dlg.SetDlgInfo(cache, null, m_mediator);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedID);
			}
			return true;
		}

		public virtual bool OnDisplayGotoLexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible. It is difficult because some commands, like this one, may be appropriate
		/// from more than 1 area.</remarks>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				if (m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null) == "reversalEditComplete")
					return false;

				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice",
					null);
				string[] areas = new string[]{"lexicon"};
				foreach(string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						CmObject obj = (CmObject)m_mediator.PropertyTable.GetValue("ActiveClerkOwningObject");
						return (obj!=null) && (obj.ClassID == LexDb.kClassId);
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}

		#endregion XCORE Message Handlers

		/// <summary>
		/// Initialize the IxCoreColleague object.
		/// </summary>
		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			base.Init(mediator, configurationParameters);
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
		}
	}
}
