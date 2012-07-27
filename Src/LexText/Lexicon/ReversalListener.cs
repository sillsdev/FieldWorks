using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Filters;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// A listener class for reversal issues.
	/// This class currently handles these issues:
	/// 1. 'Find' dlg for reversal entries.
	/// 2.
	/// </summary>
	[MediatorDispose]
	public class ReversalListener : IxCoreColleague, IFWDisposable
	{
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		private Mediator m_mediator;
		private XmlNode m_configurationParameters;

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

		private int instanceID = 0x00000F0;

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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

			var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			cache.DomainDataByFlid.BeginNonUndoableTask();
			var usedWses = new List<IWritingSystem>();
			foreach (IReversalIndex rev in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
			{
				var ws = cache.ServiceLocator.WritingSystemManager.get_Engine(rev.WritingSystem);
				usedWses.Add((IWritingSystem)ws);
				if (rev.PartsOfSpeechOA == null)
					rev.PartsOfSpeechOA = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				rev.PartsOfSpeechOA.ItemClsid = PartOfSpeechTags.kClassId;
			}
			List<IReversalIndex> corruptReversalIndices = new List<IReversalIndex>();
			foreach (IReversalIndex rev in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
			{
				// Make sure each index has a name, if it is available from the writing system.
				if (string.IsNullOrEmpty(rev.WritingSystem))
				{
					// Delete a bogus IReversalIndex that has no writing system.
					// But, for now only store them for later deletion,
					// as immediate removal will wreck the looping.
					corruptReversalIndices.Add(rev);
					continue;
				}
				IWritingSystem revWs = cache.ServiceLocator.WritingSystemManager.Get(rev.WritingSystem);
				// TODO WS: is DisplayLabel the right thing to use here?
				rev.Name.SetAnalysisDefaultWritingSystem(revWs.DisplayLabel);
			}
			// Delete any corrupt reversal indices.
			foreach (IReversalIndex rev in corruptReversalIndices)
			{
				MessageBox.Show("Need to delete a corrupt reversal index (no writing system)", "Self-correction");
				cache.LangProject.LexDbOA.ReversalIndexesOC.Remove(rev);	// does this accomplish anything?
			}

			// Set up for the reversal index combo box or dropdown menu.
			Guid firstGuid = Guid.Empty;
			List<IReversalIndex> reversalIds = cache.LanguageProject.LexDbOA.CurrentReversalIndices;
			if (reversalIds.Count > 0)
				firstGuid = reversalIds[0].Guid;
			else if (cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count > 0)
				firstGuid = cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToGuidArray()[0];
			if (firstGuid != Guid.Empty)
			{
				SetReversalIndexGuid(firstGuid);
			}
			cache.DomainDataByFlid.EndNonUndoableTask();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int) ColleaguePriority.Medium; }
		}


		private void SetReversalIndexGuid(Guid ReversalIndexGuid)
		{
			m_mediator.PropertyTable.SetProperty("ReversalIndexGuid", ReversalIndexGuid.ToString());
			m_mediator.PropertyTable.SetPropertyPersistence("ReversalIndexGuid", true);
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
			using (var dlg = new ReversalEntryGoDlg())
			{
				dlg.ReversalIndex = Entry.ReversalIndex;
				var cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
				dlg.SetDlgInfo(cache, null, m_mediator); // , false
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					// Can't Go to a subentry, so we have to go to its main entry.
					var selEntry = (IReversalIndexEntry) dlg.SelectedObject;
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
			if (rie == null || rie.Owner.Hvo == 0)
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
		/// <param name="parameter"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayReversalIndexList(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			display.List.Clear();
			var cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
			// List all existing reversal indexes.  (LT-4479, as amended)
			//IReversalIndex riOwner = this.IReversalIndex;
			foreach (IReversalIndex ri in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
			{
				display.List.Add(ri.ShortName, ri.Guid.ToString(), null, null);
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

			Guid newGuid = CreateNewReversalIndex(false);
			if (newGuid != Guid.Empty)
			{
				SetReversalIndexGuid(newGuid);
			}
			return true;
		}

		private Guid CreateNewReversalIndex(bool allowCancel)
		{
			using (var dlg = new CreateReversalIndexDlg())
			{
				var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				dlg.Init(cache, allowCancel);
				// Don't bother if all languages already have a reversal index!
				if (dlg.PossibilityCount > 0)
				{
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						int hvo = dlg.NewReversalIndexHvo;
						return cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).Guid;
					}
				}
			}
			return Guid.Empty;
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
	/// This clerk is used to deal with POSes/Entries of a IReversalIndex.
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
			string stringGuid = m_mediator.PropertyTable.GetStringProperty("ReversalIndexGuid", null);
			if (stringGuid != null)
			{
				try
				{
					Guid newGuid = new Guid(stringGuid);
					ChangeOwningObject(newGuid);
				}
				catch
				{
					// Can't change an owner if we have a bad guid.
				}
			}
		}

		private void ChangeOwningObject(Guid newGuid)
		{
			if (newGuid != Guid.Empty )
			{
				if (m_cache == null)
					m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				IReversalIndex ri = m_cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
				if (ri == null)
				{
					return;
				}
				ResetListSorter(ri);

				ICmObject newOwningObj = NewOwningObject(ri);
				if (newOwningObj != OwningObject)
				{
					OwningObject = newOwningObj;
					m_mediator.PropertyTable.SetProperty("ActiveClerkOwningObject", newOwningObj, true);
					m_mediator.PropertyTable.SetPropertyPersistence("ActiveClerkOwningObject", false);
					m_mediator.SendMessage("ClerkOwningObjChanged", this);
				}
			}
		}

		/// <summary>
		/// Resets the sorter so that it will use the writing system of the reversal index.
		/// </summary>
		/// <param name="ri"></param>
		private void ResetListSorter(IReversalIndex ri)
		{
			var sorter = Sorter as GenRecordSorter;
			if(sorter != null)
			{
				var stringFinderComparer = sorter.Comparer as StringFinderCompare;
				if(stringFinderComparer != null)
				{
					var writingSystem = (IWritingSystem) Cache.WritingSystemFactory.get_Engine(ri.WritingSystem);
					var comparer = new StringFinderCompare(stringFinderComparer.Finder, new WritingSystemComparer(writingSystem));
					sorter.Comparer = comparer;
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
				case "ReversalIndexGuid":
					ChangeOwningObjectIfPossible();
					break;
				case "ToolForAreaNamed_lexicon" :
					int rootIndex = GetRootIndex(m_list.CurrentIndex);
					JumpToIndex(rootIndex);
					base.OnPropertyChanged(name);
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
		/// Returns the index of the root object whose descendent is the object at lastValidIndex.
		/// </summary>
		/// <param name="lastValidIndex"></param>
		/// <returns></returns>
		private int GetRootIndex(int lastValidIndex)
		{
			var item = m_list.SortItemAt(lastValidIndex);
			if (item == null)
				return lastValidIndex;
			var parentIndex = m_list.IndexOfParentOf(item.KeyObject, Cache);

			return parentIndex == -1 ? lastValidIndex : GetRootIndex(parentIndex);
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
				int cRevIdx = m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;
				int cWs = m_cache.ServiceLocator.WritingSystems.AllWritingSystems.Count();
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

			Guid newGuid = CreateNewReversalIndex();
			if (newGuid != Guid.Empty)
			{
				ChangeOwningObject(newGuid);
				string sGuid = m_mediator.PropertyTable.GetStringProperty("ReversalIndexGuid", null);
				if (sGuid == null || !sGuid.Equals(newGuid.ToString()))
					SetReversalIndexGuid(newGuid);
			}
		}

		private Guid CreateNewReversalIndex()
		{
			if (m_cache == null)
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (m_cache == null)
				return Guid.Empty;
			if (m_cache.LanguageProject == null)
				return Guid.Empty;
			if (m_cache.LanguageProject.LexDbOA == null)
				return Guid.Empty;
			using (CreateReversalIndexDlg dlg = new CreateReversalIndexDlg())
			{
				dlg.Init(m_cache);
				// Don't bother if all languages already have a reversal index!
				if (dlg.PossibilityCount > 0)
				{
					if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
					{
						int hvo = dlg.NewReversalIndexHvo;
						return m_cache.ServiceLocator.GetObject(hvo).Guid;
					}
				}
			}
			return Guid.Empty;
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
				int cRevIdx = m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;
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

			string sGuid = m_mediator.PropertyTable.GetStringProperty("ReversalIndexGuid", null);
			if (sGuid == null)
				return;
			Guid oldGuid;
			try
			{
				oldGuid = new Guid(sGuid);
			}
			catch
			{
				return; // Can't delete an index if we have a bad guid.
			}
			if (m_cache == null)
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (m_cache == null)
				return;
			IReversalIndex ri = (IReversalIndex)m_cache.ServiceLocator.GetObject(oldGuid);
			DeleteReversalIndex(ri);
		}

		public void DeleteReversalIndex(IReversalIndex ri)
		{
			CheckDisposed();

			var mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
			using (new WaitCursor(mainWindow))
			{
				using (var dlg = new ConfirmDeleteObjectDlg(m_mediator.HelpTopicProvider))
				{
					var ui = new CmObjectUi(ri);
					dlg.SetDlgInfo(ui, m_cache, m_mediator);
					dlg.TopMessage = LexEdStrings.ksDeletingThisRevIndex;
					dlg.BottomQuestion = LexEdStrings.ksReallyWantToDeleteRevIndex;
					if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
						ReallyDeleteReversalIndex(ri);
				}
			}
		}

		protected virtual void ReallyDeleteReversalIndex(IReversalIndex ri)
		{
			try
			{
				Debug.Assert(ri.Hvo == m_list.OwningObject.Hvo);
				m_list.ListModificationInProgress = true;	// can't reload deleted list! (LT-5353)
				// We're about to do a MasterRefresh which clobbers the Undo stack,
				// so we might as well make this UOW not undoable
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
					() =>
					{
						m_cache.DomainDataByFlid.DeleteObj(ri.Hvo);
						int cobjNew;
						var idxNew = ReversalIndexAfterDeletion(m_cache, out cobjNew);
						SetReversalIndexGuid(idxNew.Guid);
					});
				ChangeOwningObjectIfPossible();
			}
			finally
			{
				m_list.ListModificationInProgress = false;
			}
			// Without this, stale data can still display in the BulkEditSenses tool if you
			// recreate the deleted reversal index.
			m_mediator.SendMessage("MasterRefresh", null);
		}

		internal static IReversalIndex ReversalIndexAfterDeletion(FdoCache cache, out int cobjNew)
		{
			IReversalIndex newIdx;
			cobjNew = cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;
			if (cobjNew == 0)
			{
				// Big trouble ensues if we don't have any reversal indexes at all, so ...
				// Create a reversal index for the current default analysis writing system.
				newIdx = cache.ServiceLocator.GetInstance<IReversalIndexFactory>().Create();
				cache.LanguageProject.LexDbOA.ReversalIndexesOC.Add(newIdx);
				IWritingSystem wsAnalysis = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
				newIdx.WritingSystem = wsAnalysis.Id;
				// The above line 'appears' to set the Analysis ws Name, but apparently doesn't really.
				// TODO WS: is the DisplayLabel the correct thing to use here?
				newIdx.Name.SetAnalysisDefaultWritingSystem(wsAnalysis.DisplayLabel);
				newIdx.PartsOfSpeechOA = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				newIdx.PartsOfSpeechOA.ItemClsid = PartOfSpeechTags.kClassId;

				cobjNew = 1;
			}
			else
			{
				// Regardless, we need to change the reversal index hvo since the old one just
				// disappeared.
				newIdx = cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToArray()[0];
			}
			return newIdx;
		}

		private void SetReversalIndexGuid(Guid ReversalIndexGuid)
		{
			if (m_cache.ServiceLocator.GetObject(ReversalIndexGuid) is IReversalIndex)
			{
				m_mediator.PropertyTable.SetProperty("ReversalIndexGuid", ReversalIndexGuid.ToString());
				m_mediator.PropertyTable.SetPropertyPersistence("ReversalIndexGuid", true);
			}
		}

		abstract protected ICmObject NewOwningObject(IReversalIndex ri);
	}

	/// <summary>
	/// This clerk is used to deal with the entries of a IReversalIndex.
	/// </summary>
	public class ReversalEntryClerk : ReversalClerk
	{
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri;
		}
	}
}
