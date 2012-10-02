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
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
using SIL.Utils;

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
				currentEntry = obj.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
			}
			Debug.Assert(currentEntry != null);
			if (currentEntry == null)
				return false;

			using (MergeEntryDlg dlg = new MergeEntryDlg())
			{
				Debug.Assert(argument != null && argument is Command);
				dlg.SetDlgInfo(cache, m_mediator, currentEntry);
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
					m_mediator.SendMessage("JumpToRecord", survivor.Hvo);
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
						var obj = (ICmObject)m_mediator.PropertyTable.GetValue("ActiveClerkOwningObject");
						if (obj == null || obj.ClassID != LexDbTags.kClassId)
							return false;
						obj = (ICmObject)m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject");
						return (obj != null && obj.ClassID != LexSenseTags.kClassId); // Disable in bulk edit.
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

			using (var dlg = new EntryGoDlg())
			{
				var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				Debug.Assert(cache != null);
				dlg.SetDlgInfo(cache, null, m_mediator);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedObject.Hvo);
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
						var obj = (ICmObject)m_mediator.PropertyTable.GetValue("ActiveClerkOwningObject");
						return (obj != null) && (obj.ClassID == LexDbTags.kClassId);
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
			//FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
		}
	}
}
