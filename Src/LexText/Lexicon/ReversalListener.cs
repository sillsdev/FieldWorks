using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// A listener class for reversal issues.
	/// This class currently handles these issues:
	/// 1. 'Find' dlg for reversal entries.
	/// 2.
	/// </summary>
	[XCore.MediatorDispose]
	public class ReversalListener : IxCoreColleague, IFWDisposable
	{
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		private XCore.Mediator m_mediator;
		private XmlNode m_configurationParameters;

		public ReversalListener()
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ReversalListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
				{
					// Not sure why this is retrieved from the mediator and not used,
					// so commenting out for now.
					// FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
					m_mediator.RemoveColleague(this);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_configurationParameters = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		public virtual void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			m_mediator.AddColleague(this);

			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			List<ILgWritingSystem> usedWses = new List<ILgWritingSystem>();
			using (new SuppressSubTasks(cache))
			{
				foreach (IReversalIndex rev in cache.LangProject.LexDbOA.ReversalIndexesOC)
				{
					// This benign looking check of the count appears to do nothing,
					// but it is very important as it ensures the vector is cached. Without it,
					// crashes turn up in bulk deletion, as no PropChanged is sent,
					// as the vector is not in the cache.
					int entryCount = 0;
					entryCount = rev.EntriesOC.Count;
					usedWses.Add(rev.WritingSystemRA);
					if (rev.PartsOfSpeechOA == null)
						(rev as ReversalIndex).InitNewInternal();
					if (rev.PartsOfSpeechOA.ItemClsid != PartOfSpeech.kClassId)
						rev.PartsOfSpeechOA.ItemClsid = PartOfSpeech.kClassId;
				}
				List<IReversalIndex> corruptReversalIndices = new List<IReversalIndex>();
				foreach (IReversalIndex rev in cache.LangProject.LexDbOA.ReversalIndexesOC)
				{
					// Make sure each index has a name, if it is available from the writing system.
					if (rev.WritingSystemRA == null)
					{
						// Delete a bogus ReversalIndex that has no writing system.
						// But, for now only store them for later deletion,
						// as immediate removal will wreck the looping.
						corruptReversalIndices.Add(rev);
						continue;
					}
					ILgWritingSystem revWs = rev.WritingSystemRA;
					string sql = string.Format("SELECT Ws, Txt FROM LgWritingSystem_Name WHERE Obj = {0}", revWs.Hvo);
					IOleDbCommand odc = DbOps.MakeRowSet(cache, sql, null);
					try
					{
						bool fMoreRows;
						odc.NextRow(out fMoreRows);
						while (fMoreRows)
						{
							int wsHvo = DbOps.ReadInt(odc, 0);
							string nameWs = DbOps.ReadString(odc, 1);
							string nameRev = rev.Name.GetAlternative(wsHvo);
							if ((nameRev == null || nameRev == String.Empty)
								&& (nameWs != null && nameWs.Length > 0))
							{
								rev.Name.SetAlternative(nameWs, wsHvo);
							}
							odc.NextRow(out fMoreRows);
						}
					}
					finally
					{
						DbOps.ShutdownODC(ref odc);
					}
				}
				// Delete any corrupt reversal indices.
				foreach (IReversalIndex rev in corruptReversalIndices)
				{
					rev.DeleteUnderlyingObject();
				}
			}
			// Set up for the reversal index combo box or dropdown menu.
			int firstId = 0;
			List<int> reversalIds = cache.LangProject.LexDbOA.CurrentReversalIndices;
			if (reversalIds.Count > 0)
				firstId = reversalIds[0];
			else if (cache.LangProject.LexDbOA.ReversalIndexesOC.Count > 0)
				firstId = cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray[0];
			if (firstId > 0)
			{
				SetReversalIndexHvo(firstId);
			}
		}

		private void SetReversalIndexHvo(int reversalIndexHvo)
		{
			m_mediator.PropertyTable.SetProperty("ReversalIndexHvo", reversalIndexHvo.ToString());
			m_mediator.PropertyTable.SetPropertyPersistence("ReversalIndexHvo", false);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(this);
			return targets.ToArray();
		}

		#endregion IxCoreColleague implementation

		#region XCore Message handlers

		#region Go Dlg
		/// <summary>
		/// Handles the xCore message to go to a reversal entry.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoReversalEntry(object argument)
		{
			CheckDisposed();

			using (ReversalEntryGoDlg dlg = new ReversalEntryGoDlg())
			{
				List<IReversalIndexEntry> filteredEntries = new List<IReversalIndexEntry>();
				filteredEntries.Add(Entry);
				WindowParams wp = new WindowParams();
				wp.m_btnText = LexEdStrings.ks_GoTo;
				wp.m_label = LexEdStrings.ks_Find_;
				wp.m_title = LexEdStrings.ksFindRevEntry;
				dlg.SetDlgInfo(m_mediator, wp, filteredEntries); // , false
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					// Can't Go to a subentry, so we have to go to its main entry.
					FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
					IReversalIndexEntry selEntry = ReversalIndexEntry.CreateFromDBObject(cache, dlg.SelectedID);
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", selEntry.MainEntry.Hvo);
				}
			}
			return true;
		}

		private IReversalIndexEntry Entry
		{
			get
			{
				IReversalIndexEntry rie = null;
				string clerkId = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "clerk");
				string propertyName = RecordClerk.GetCorrespondingPropertyName(clerkId);
				RecordClerk clerk = (RecordClerk)m_mediator.PropertyTable.GetValue(propertyName);
				if (clerk != null)
					rie = clerk.CurrentObject as IReversalIndexEntry;
				return rie;
			}
		}

		public virtual bool OnDisplayGotoReversalEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			IReversalIndexEntry rie = Entry;
			if (rie == null || rie.OwnerHVO == 0)
			{
				display.Enabled = display.Visible = false;
			}
			else
			{
				display.Enabled = rie.ReversalIndex.EntriesOC.Count > 1 && InFriendlyArea;
				display.Visible = InFriendlyArea;
			}
			return true; //we've handled this
		}
		#endregion Go Dlg

		#region Reversal Index Combo

		/// <summary>
		/// Called (by xcore) to control display params of the reversal index menu, e.g. whether it should be enabled.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayReversalIndexHvo(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

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
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayReversalIndexList(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			display.List.Clear();
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			// List all existing reversal indexes.  (LT-4479, as amended)
			//ReversalIndex riOwner = this.ReversalIndex;
			foreach (IReversalIndex ri in cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				display.List.Add(ri.ShortName, ri.Hvo.ToString(), null, null);
			}
			display.List.Sort();
			return true; // We handled this, no need to ask anyone else.
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument"></param>
		public virtual bool OnInsertReversalIndex_FORCE(object argument)
		{
			CheckDisposed();

			int id = CreateNewReversalIndex(false);
			if (id > 0)
				SetReversalIndexHvo(id);
			return true;
		}

		private int CreateNewReversalIndex(bool allowCancel)
		{
			//if (m_cache == null)
			//    m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			//if (m_cache == null)
			//    return 0;
			//if (m_cache.LangProject == null)
			//    return 0;
			//if (m_cache.LangProject.LexDbOA == null)
			//    return 0;
			using (CreateReversalIndexDlg dlg = new CreateReversalIndexDlg())
			{
				FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				dlg.Init(cache, allowCancel);
				// Don't bother if all languages already have a reversal index!
				if (dlg.PossibilityCount > 0)
				{
					if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
						return dlg.NewReversalIndexHvo;
				}
			}
			return 0;
		}

		#endregion Reversal Index Combo

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				string toolFor = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);

				return areaChoice == "lexicon" && toolFor.StartsWith("reversalTool");
			}
		}

		#endregion XCore Message handlers
	}

	/// <summary>
	/// This clerk is used to deal with POSes/Entries of a ReversalIndex.
	/// It's subclasses do the object-specific kinds of work.
	/// </summary>
	public abstract class ReversalClerk : RecordClerk
	{
		FdoCache m_cache = null;

		public override void Init(Mediator mediator, XmlNode viewConfiguration)
		{
			CheckDisposed();

			base.Init(mediator, viewConfiguration);
			if (mediator != null)
				m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			ChangeOwningObjectIfPossible();
		}

		private void ChangeOwningObjectIfPossible()
		{
			string stringHvo = m_mediator.PropertyTable.GetStringProperty("ReversalIndexHvo", "0");
			if (stringHvo != "????")
				ChangeOwningObject(Convert.ToInt32(stringHvo));
		}

		private void ChangeOwningObject(int id)
		{
			if (id > 0)
			{
				if (m_cache == null)
					m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				IReversalIndex ri = ReversalIndex.CreateFromDBObject(m_cache, id);
				ICmObject newOwningObj = NewOwningObject(ri);
				if (newOwningObj != OwningObject)
				{
					OwningObject = newOwningObj;
					m_mediator.PropertyTable.SetProperty("ActiveClerkOwningObject", newOwningObj, false);
					m_mediator.PropertyTable.SetPropertyPersistence("ActiveClerkOwningObject", false);
					m_mediator.SendMessage("ClerkOwningObjChanged", this);
				}
			}
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch(name)
			{
				default:
					base.OnPropertyChanged(name);
					break;
				case "ReversalIndexHvo":
					ChangeOwningObjectIfPossible();
					break;
				case "ActiveClerk":
					RecordClerk activeClerk = (RecordClerk)m_mediator.PropertyTable.GetValue("ActiveClerk");
					if (activeClerk == this)
						ChangeOwningObjectIfPossible();
					else
						base.OnPropertyChanged(name);
					break;
			}
		}

		/// <summary>
		/// This is enabled whenever the ReversalClerk is active.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertReversalIndex(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_cache == null)
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (m_cache == null)
			{
				display.Enabled = false;
			}
			else
			{
				int cRevIdx = m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count;
				int cWs = m_cache.LangProject.GetDbNamedWritingSystems().Count;
				display.Enabled = cRevIdx < cWs;
			}
			display.Visible = true;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument"></param>
		public virtual void OnInsertReversalIndex(object argument)
		{
			CheckDisposed();

			int id = CreateNewReversalIndex();
			if (id > 0)
			{
				ChangeOwningObject(id);
				string sHvo = m_mediator.PropertyTable.GetStringProperty("ReversalIndexHvo", null);
				if (sHvo == null || sHvo != id.ToString())
					SetReversalIndexHvo(id);
			}
		}

		private int CreateNewReversalIndex()
		{
			if (m_cache == null)
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (m_cache == null)
				return 0;
			if (m_cache.LangProject == null)
				return 0;
			if (m_cache.LangProject.LexDbOA == null)
				return 0;
			using (CreateReversalIndexDlg dlg = new CreateReversalIndexDlg())
			{
				dlg.Init(m_cache);
				// Don't bother if all languages already have a reversal index!
				if (dlg.PossibilityCount > 0)
				{
					if (dlg.ShowDialog() == DialogResult.OK)
						return dlg.NewReversalIndexHvo;
				}
			}
			return 0;
		}

		/// <summary>
		/// This is enabled whenever the ReversalClerk is active.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDeleteReversalIndex(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_cache == null)
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (m_cache == null)
			{
				display.Enabled = false;
			}
			else
			{
				int cRevIdx = m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count;
				display.Enabled = cRevIdx > 0;
			}
			display.Visible = true;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument"></param>
		public virtual void OnDeleteReversalIndex(object argument)
		{
			CheckDisposed();

			string sHvo = m_mediator.PropertyTable.GetStringProperty("ReversalIndexHvo", null);
			if (sHvo == null)
				return;
			int hvo = Convert.ToInt32(sHvo);
			if (hvo <= 0)
				return;
			if (m_cache == null)
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (m_cache == null)
				return;
			IReversalIndex ri = ReversalIndex.CreateFromDBObject(m_cache, hvo);
			DeleteReversalIndex(ri);
		}

		public void DeleteReversalIndex(IReversalIndex ri)
		{
			CheckDisposed();

			Form mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
			mainWindow.Cursor = Cursors.WaitCursor;
			using (ConfirmDeleteObjectDlg dlg = new ConfirmDeleteObjectDlg())
			{
				SIL.FieldWorks.FdoUi.CmObjectUi ui = new SIL.FieldWorks.FdoUi.CmObjectUi(ri);
				dlg.SetDlgInfo(ui, m_cache, m_mediator);
				dlg.TopMessage = LexEdStrings.ksDeletingThisRevIndex;
				dlg.BottomQuestion = LexEdStrings.ksReallyWantToDeleteRevIndex;
				if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
					ReallyDeleteReversalIndex(ri);
			}
			mainWindow.Cursor = Cursors.Default;
		}

		protected virtual void ReallyDeleteReversalIndex(IReversalIndex ri)
		{
			try
			{
				Debug.Assert(ri.Hvo == m_list.OwningObject.Hvo);
				m_list.ListModificationInProgress = true;	// can't reload deleted list! (LT-5353)
				// Clear out any virtual data stored in the cache for this reversal index.
				IVwCacheDa cda = m_cache.MainCacheAccessor as IVwCacheDa;
				IVwVirtualHandler vh = cda.GetVirtualHandlerName("LexSense",
					LexSenseReversalEntriesTextHandler.StandardFieldName);
				if (vh != null)
				{
					int wsDel = ri.WritingSystemRAHvo;
					IEnumerator<IReversalIndexEntry> erie = ri.EntriesOC.GetEnumerator();
					while (erie.MoveNext())
					{
						foreach (LinkedObjectInfo loi in erie.Current.BackReferences)
						{
							if (loi.RelObjClass == (int)LexSense.kClassId)
								vh.Load(loi.RelObjId, vh.Tag, wsDel, cda);
						}
					}
				}
				m_cache.BeginUndoTask(LexEdStrings.ksUndoDelete, LexEdStrings.ksRedoDelete);
				int cobjOld = m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count;
				ri.DeleteUnderlyingObject();
				int cobjNew;
				int hvoIdxNew = ReversalIndexAfterDeletion(m_cache, out cobjNew);
				m_cache.EndUndoTask();
				SetReversalIndexHvo(hvoIdxNew);
				// Without this, switching to the ReversalEntries tool can crash if it was
				// displaying data from the deleted reversal entry.
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
					m_cache.LangProject.LexDbOAHvo,
					(int)LexDb.LexDbTags.kflidReversalIndexes,
					0, cobjNew, cobjOld);
			}
			finally
			{
				m_list.ListModificationInProgress = false;
			}
			// Can't do this until ListModificationInProgress flag is cleared because it
			// redisplays everything after reloading the list: if the list isn't actually
			// reloaded, it tries to display a deleted object -- CRASH!
			ChangeOwningObjectIfPossible();
			// Without this, stale data can still display in the BulkEditSenses tool if you
			// recreate the deleted reversal index.
			m_mediator.SendMessage("MasterRefresh", null);
		}

		internal static int ReversalIndexAfterDeletion(FdoCache cache, out int cobjNew)
		{
			int hvoIdxNew = 0;
			cobjNew = cache.LangProject.LexDbOA.ReversalIndexesOC.Count;
			if (cobjNew == 0)
			{
				// Big trouble ensues if we don't have any reversal indexes at all, so ...
				// Create a reversal index for the current default analysis writing system.
				IReversalIndex newIdx = cache.LangProject.LexDbOA.ReversalIndexesOC.Add(new ReversalIndex());
				ILgWritingSystem wsAnalysis = cache.LangProject.CurAnalysisWssRS[0];
				newIdx.WritingSystemRA = wsAnalysis;
				newIdx.Name.AnalysisDefaultWritingSystem = wsAnalysis.Name.AnalysisDefaultWritingSystem;
				hvoIdxNew = newIdx.Hvo;
				cobjNew = 1;
			}
			else
			{
				// Regardless, we need to change the reversal index hvo since the old one just
				// disappeared.
				hvoIdxNew = cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray[0];
			}
			return hvoIdxNew;
		}

		private void SetReversalIndexHvo(int reversalIndexHvo)
		{
			m_mediator.PropertyTable.SetProperty("ReversalIndexHvo", reversalIndexHvo.ToString());
			m_mediator.PropertyTable.SetPropertyPersistence("ReversalIndexHvo", false);
		}

		abstract protected ICmObject NewOwningObject(IReversalIndex ri);
	}

	/// <summary>
	/// This clerk is used to deal with the entries of a ReversalIndex.
	/// </summary>
	public class ReversalEntryClerk : ReversalClerk
	{
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri;
		}
	}
}
