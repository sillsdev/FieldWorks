// Copyright (c) 2004-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EntryDlgListener.cs
// Responsibility: Randy Regnier
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
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

		public override void Init(Mediator mediator, PropertyTable propertyTable, System.Xml.XmlNode configurationParameters)
		{
			base.Init(mediator, propertyTable, configurationParameters);
			Subscriber.Subscribe(EventConstants.DialogInsertItemInVector, DialogInsertItemInVector, propertyTable.GetWindow());
		}

		#endregion Construction and Initialization

		#region IDisposable
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Subscriber.Unsubscribe(EventConstants.DialogInsertItemInVector, DialogInsertItemInVector);
			}
			base.Dispose(disposing);
		}
		#endregion IDisposable

		#region XCORE Message Handlers

		/// <summary>
		/// Handles the message to insert a new lexical entry.
		/// Invoked by the RecordClerk
		/// </summary>
		/// <param name="obj">Object that contains the xCore Command object and has a ReturnValue. The
		/// ReturnValue is true if we handled the message.</param>
		private void DialogInsertItemInVector(object obj)
		{
			CheckDisposed();

			if (!(obj is ReturnObject retObj) ||
				!(retObj.Data is Command command))
			{
				Debug.Assert(false, "Received unexpected object type.");
				return;
			}
			// Return if already handled by another Subscriber.
			if (retObj.ReturnValue)
			{
				return;
			}
			// Only handle "LexEntry" class.
			string className = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "className");
			if (className != "LexEntry")
			{
				return;
			}

			LcmCache cache = m_propertyTable.GetValue<LcmCache>("cache");
			Debug.Assert(cache != null);

			// New-UI gate (mirrors the Options dialog gate): in New mode launch the Avalonia Insert Entry dialog;
			// Legacy mode (and the Interlinear/affix-slot callers, which use other SetDlgInfo overloads directly)
			// keep the WinForms InsertEntryDlg. Both paths MasterRefresh + JumpToRecord to the created entry.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaInsertEntryDialog(cache);
				retObj.ReturnValue = true; // We "handled" the message, regardless of what happened.
				return;
			}

			using (InsertEntryDlg dlg = new InsertEntryDlg())
			{
				dlg.SetDlgInfo(cache, m_mediator, m_propertyTable, m_persistProvider);
				if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
				{
					ILexEntry entry;
					bool newby;
					dlg.GetDialogInfo(out entry, out newby);
					// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
#pragma warning disable 618 // suppress obsolete warning
					m_mediator.SendMessage("MasterRefresh", null);
					m_mediator.SendMessage("JumpToRecord", entry.Hvo);
#pragma warning restore 618
				}
			}
			retObj.ReturnValue = true; // We "handled" the message, regardless of what happened.
		}

		// NoInlining keeps the Avalonia assembly load out of the gated caller's JIT (Legacy loader isolation).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowAvaloniaInsertEntryDialog(LcmCache cache)
		{
			var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
			var (entry, newby) = LcmInsertEntryDialogLauncher.Show(cache, m_mediator, m_propertyTable,
				Form.ActiveForm, tssForm: null, helpProvider: helpProvider);
			// Jump to the resulting entry whether it was newly created OR an existing entry the user chose from
			// the matching-entries pane (the legacy "Go to similar entry" outcome, newby == false). The legacy
			// WinForms path likewise JumpToRecord's for both. A MasterRefresh is only needed for a new entry.
			if (entry != null)
			{
#pragma warning disable 618 // suppress obsolete warning
				if (newby)
					m_mediator.SendMessage("MasterRefresh", null);
				m_mediator.SendMessage("JumpToRecord", entry.Hvo);
#pragma warning restore 618
			}
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
			ICmObject obj = m_propertyTable.GetValue<ICmObject>("ActiveClerkSelectedObject");
			Debug.Assert(obj != null);
			if (obj == null)
				return false;		// should never happen, but nothing we can do if it does!
			LcmCache cache = m_propertyTable.GetValue<LcmCache>("cache");
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

			// New-UI gate (mirrors the Insert Entry / Options dialog gates): in New mode launch the Avalonia Merge
			// Entry dialog (the reusable entry-search/"go" kit dialog); Legacy mode keeps the WinForms MergeEntryDlg.
			// Both paths merge the current entry INTO the chosen survivor in one undoable step, then JumpToRecord.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaMergeEntryDialog(cache, currentEntry, fLoseNoTextData);
				return true;
			}

			using (MergeEntryDlg dlg = new MergeEntryDlg())
			{
				Debug.Assert(argument != null && argument is Command);
				dlg.SetDlgInfo(cache, m_mediator, m_propertyTable, currentEntry);
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
#pragma warning disable 618 // suppress obsolete warning
					m_mediator.SendMessage("JumpToRecord", survivor.Hvo);
#pragma warning restore 618
				}
			}
			return true;
		}

		public virtual bool OnDisplayMergeEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			var command = (Command)commandObject;
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

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
				string areaChoice = m_propertyTable.GetStringProperty("areaChoice", null);
				if (areaChoice == null) return false; // happens at start up
				if ("lexicon" == areaChoice)
				{
					string tool = m_propertyTable.GetStringProperty("currentContentControl", null);
					if (tool == "lexiconEdit") return true;
					return false;
				}
				return false; //we are not in an area that wants to see the merge command
			}
		}

		// NoInlining keeps the Avalonia assembly load out of the gated caller's JIT (Legacy loader isolation).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowAvaloniaMergeEntryDialog(LcmCache cache, ILexEntry currentEntry, bool fLoseNoTextData)
		{
			var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
			var survivor = LcmMergeEntryDialogLauncher.Show(cache, m_mediator, m_propertyTable, currentEntry,
				Form.ActiveForm, fLoseNoTextData, helpProvider);
			if (survivor != null)
			{
				MessageBox.Show(null,
					LexTextControls.ksEntriesHaveBeenMerged,
					LexTextControls.ksMergeReport,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
#pragma warning disable 618 // suppress obsolete warning
				m_mediator.SendMessage("JumpToRecord", survivor.Hvo);
#pragma warning restore 618
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

			var cache = m_propertyTable.GetValue<LcmCache>("cache");
			Debug.Assert(cache != null);

			// New-UI gate (mirrors the Insert Entry / Merge Entry gates): in New mode launch the Avalonia
			// Go-to-Entry dialog (the reusable entry-search/"go" kit dialog, no starting entry to exclude); Legacy
			// mode keeps the WinForms EntryGoDlg. Both paths just navigate to the chosen entry (no side effects).
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaGoToEntryDialog(cache);
				return true;
			}

			using (var dlg = new EntryGoDlg())
			{
				dlg.SetDlgInfo(cache, null, m_mediator, m_propertyTable);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
#pragma warning disable 618 // suppress obsolete warning
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedObject.Hvo);
#pragma warning restore 618
				}
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
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				if (m_propertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null) == "reversalEditComplete")
					return false;

				string areaChoice = m_propertyTable.GetStringProperty("areaChoice",
					null);
				string[] areas = new string[]{"lexicon"};
				foreach(string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						var obj = m_propertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
						return (obj != null) && (obj.ClassID == LexDbTags.kClassId);
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}

		// NoInlining keeps the Avalonia assembly load out of the gated caller's JIT (Legacy loader isolation).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowAvaloniaGoToEntryDialog(LcmCache cache)
		{
			var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
			var entry = LcmGoToEntryDialogLauncher.Show(cache, m_mediator, m_propertyTable,
				Form.ActiveForm, helpProvider);
			if (entry != null)
			{
#pragma warning disable 618 // suppress obsolete warning
				m_mediator.BroadcastMessageUntilHandled("JumpToRecord", entry.Hvo);
#pragma warning restore 618
			}
		}

		#endregion XCORE Message Handlers
	}
}
