// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Windows.Forms;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FdoUi.Dialogs;
using SIL.FieldWorks.Filters;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
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
	public class ReversalListener : IxCoreColleague, IDisposable
	{
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
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

		public virtual void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configurationParameters = configurationParameters;
			m_mediator.AddColleague(this);

			var cache = m_propertyTable.GetValue<LcmCache>("cache");
			var wsMgr = cache.ServiceLocator.WritingSystemManager;
			cache.DomainDataByFlid.BeginNonUndoableTask();
			var usedWses = new List<CoreWritingSystemDefinition>();
			foreach (IReversalIndex rev in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
			{
				usedWses.Add(wsMgr.Get(rev.WritingSystem));
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
				var revWs = wsMgr.Get(rev.WritingSystem);
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
			var reversalIndexGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_propertyTable, "ReversalIndexGuid");
			if (reversalIndexGuid == Guid.Empty)
			{
				// We haven't established the reversal index yet.  Choose the first one available.
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
			m_propertyTable.SetProperty("ReversalIndexGuid", ReversalIndexGuid.ToString(), true);
			m_propertyTable.SetPropertyPersistence("ReversalIndexGuid", true);
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
				var cache = m_propertyTable.GetValue<LcmCache>("cache");
				dlg.SetDlgInfo(cache, null, m_mediator, m_propertyTable);
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
				string clerkId = XmlUtils.GetMandatoryAttributeValue(m_configurationParameters, "clerk");
				string propertyName = RecordClerk.GetCorrespondingPropertyName(clerkId);
				RecordClerk clerk = m_propertyTable.GetValue<RecordClerk>(propertyName);
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

			var configObjectName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "configureObjectName", null);
			configObjectName = "ReversalIndex";
			var cache = m_propertyTable.GetValue<LcmCache>("cache");
			var reversalIndexConfigurations = SIL.FieldWorks.XWorks.DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(cache, configObjectName);

			// Add menu items that display the configuration name and send PropChanges with
			// the configuration path.
			foreach(var config in reversalIndexConfigurations)
			{
				display.List.Add(config.Key, config.Value, null, null);
			}
			DictionaryConfigurationUtils.RemoveAllReversalChoiceFromList(ref display);

			return true; // We handled this, no need to ask anyone else.
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
				string areaChoice = m_propertyTable.GetStringProperty("areaChoice", null);
				string toolFor = m_propertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);

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
		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode viewConfiguration)
		{
			CheckDisposed();

			base.Init(mediator, propertyTable, viewConfiguration);
			ChangeOwningObjectIfPossible();
		}

		private void ChangeOwningObjectIfPossible()
		{
			var newGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_propertyTable, "ReversalIndexGuid");
			try
			{
				ChangeOwningObject(newGuid);
			}
			catch
			{
				// Can't change an owner if we have a bad guid.
			}
		}

		private void ChangeOwningObject(Guid newGuid)
		{
			if (newGuid.Equals(Guid.Empty))
			{
				// We need to find another reversal index. Any will do.
				newGuid = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances().First().Guid;
				m_propertyTable.SetProperty("ReversalIndexGuid", newGuid.ToString(), true);
			}

			var ri = Cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if (ri == null)
			{
				return;
			}

			// This looks like our best chance to update a global "Current Reversal Index Writing System" value.
			WritingSystemServices.CurrentReversalWsId = Cache.WritingSystemFactory.GetWsFromStr(ri.WritingSystem);

			// Set the override writing system. LT-21198
			var layoutFinder = ((Sorter as GenRecordSorter)?.Comparer as StringFinderCompare)?.Finder as LayoutFinder;
			if (layoutFinder?.Vc != null)
			{
				var wsComparer = ((Sorter as GenRecordSorter)?.Comparer as StringFinderCompare)?.SubComparer as WritingSystemComparer;
				if (wsComparer != null)
				{
					layoutFinder.Vc.OverrideWs = Cache.WritingSystemFactory.GetWsFromStr(wsComparer.WsId);
				}
				else
				{
					layoutFinder.Vc.OverrideWs = WritingSystemServices.CurrentReversalWsId;
				}
			}

			try
			{
				ICmObject newOwningObj = NewOwningObject(ri);
				if (newOwningObj != OwningObject)
				{
					UpdateFiltersAndSortersIfNeeded(); // Load the index-specific sorter
					OnChangeSorter(); // Update the column headers with sort arrows
					OwningObject = newOwningObj; // This automatically reloads (and sorts) the list
					m_propertyTable.SetProperty("ActiveClerkOwningObject", newOwningObj, true);
					m_propertyTable.SetPropertyPersistence("ActiveClerkOwningObject", false);
					m_mediator.SendMessage("ClerkOwningObjChanged", this);
				}
			}
			finally
			{
				if (layoutFinder?.Vc != null)
				{
					layoutFinder.Vc.OverrideWs = 0;
				}
			}
		}

		/// <summary>
		/// returns the XmlNode which configures the FormColumn in the BrowseView associated with BulkEdit of ReversalEntries
		/// </summary>
		protected XmlNode BrowseViewFormCol
		{
			get
			{
				var path = Path.Combine(Path.Combine(Path.Combine(Path.Combine(FwDirectoryFinder.CodeDirectory,
																			   FwDirectoryFinder.ksFlexFolderName),
																  @"Configuration"),
													 @"Lexicon"),
										@"ReversalEntriesBulkEdit");
				var doc = new XmlDocument();
				doc.Load(Path.Combine(path, @"toolConfiguration.xml"));
				var columnNode = doc.SelectSingleNode(@"//column[@label='Reversal Form']");
				return columnNode;
			}
		}

		/// <summary>
		/// The reversal should not be checking the writing system when testing for sorter compatibility since
		/// that writing system is changed in the Clerk through events and the bulkedit and browse view share the same clerk.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <remarks>This method is only valid because there are no multi-lingual columns in the reversal views</remarks>
		/// <returns></returns>
		public override bool AreSortersCompatible(RecordSorter first, RecordSorter second)
		{
			if (first == null || second == null)
				return false;

			var secondSorter = second as GenRecordSorter;
			var firstSorter = first as GenRecordSorter;
			if (secondSorter == null || firstSorter == null)
				return first.CompatibleSorter(second);

			var sfcThis = firstSorter.Comparer as StringFinderCompare;
			var sfcOther = secondSorter.Comparer as StringFinderCompare;
			if (sfcThis == null || sfcOther == null)
				return false;
			if (!sfcThis.Finder.SameFinder(sfcOther.Finder))
				return false;
			return true;
		}

		/// <summary>
		/// The stored sorter files keep messing us up here, so we need to do a bit of post-deserialization processing.
		/// </summary>
		/// <returns>true if we restored something different from what was already there.</returns>
		protected override bool TryRestoreSorter(XmlNode clerkConfiguration, LcmCache cache)
		{
			var fakevc = new XmlBrowseViewBaseVc { SuppressPictures = true, Cache = Cache }; // SuppressPictures to make sure that we don't leak anything as this will not be disposed.
			if (base.TryRestoreSorter(clerkConfiguration, cache) && Sorter is GenRecordSorter)
			{
				var sorter = (GenRecordSorter)Sorter;
				var stringFinderComparer = sorter.Comparer as StringFinderCompare;
				if (stringFinderComparer != null)
				{
					var colSpec = ReflectionHelper.GetField(stringFinderComparer.Finder, "m_colSpec") as XmlNode ?? BrowseViewFormCol;
					sorter.Comparer = new StringFinderCompare(LayoutFinder.CreateFinder(Cache, colSpec, fakevc,
																						m_propertyTable.GetValue<IApp>("App")),
															stringFinderComparer.SubComparer);
				}
				return true;
			}
			if(Sorter is GenRecordSorter) // If we already have a GenRecordSorter, it's probably an existing, valid one.
				return false;
			// Try to create a sorter based on the current Reversal Index's WritingSystem
			var newGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_propertyTable, "ReversalIndexGuid");
			if(newGuid.Equals(Guid.Empty))
				return false;
			var ri = cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if(ri == null)
				return false;
			var writingSystem = (CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_Engine(ri.WritingSystem);
			m_list.Sorter = new GenRecordSorter(new StringFinderCompare(LayoutFinder.CreateFinder(Cache, BrowseViewFormCol, fakevc,
																								m_propertyTable.GetValue<IApp>("App")),
																		new WritingSystemComparer(writingSystem)));
			return true;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			var window = m_propertyTable.GetValue<FwXWindow>("window");
			if (window != null)
			{
				window.ClearInvalidatedStoredData();
			}
			switch (name)
			{
				default:
					base.OnPropertyChanged(name);
					break;
				case "ReversalIndexGuid":
					ChangeOwningObjectIfPossible();
					break;
				case "ToolForAreaNamed_lexicon":
					int rootIndex = GetRootIndex(m_list.CurrentIndex);
					JumpToIndex(rootIndex);
					base.OnPropertyChanged(name);
					break;
				case "ActiveClerk":
					RecordClerk activeClerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk");
					if (activeClerk == this)
						ChangeOwningObjectIfPossible();
					else
						base.OnPropertyChanged(name);
					break;
				case "ReversalIndexPublicationLayout":
					// When the user chooses a different reversal index configuration from the drop-down menu,
					// set the list of associated reversal index entries.
					DictionaryConfigurationUtils.SetReversalIndexGuidBasedOnReversalIndexConfiguration(m_propertyTable, Cache);
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

			if (Cache == null)
			{
				display.Enabled = false;
			}
			else
			{
				int cRevIdx = Cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;
				int cWs = Cache.ServiceLocator.WritingSystems.AllWritingSystems.Count();
				display.Enabled = cRevIdx < cWs;
			}
			display.Visible = true;
			return true;
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

			if (Cache == null)
			{
				display.Enabled = false;
			}
			else
			{
				int cRevIdx = Cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;
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

			var oldGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_propertyTable, "ReversalIndexGuid");
			if (oldGuid.Equals(Guid.Empty))
				return;

			if (Cache == null)
				return;
			IReversalIndex ri = (IReversalIndex)Cache.ServiceLocator.GetObject(oldGuid);
			DeleteReversalIndex(ri);
		}

		public void DeleteReversalIndex(IReversalIndex ri)
		{
			CheckDisposed();

			var mainWindow = m_propertyTable.GetValue<Form>("window");
			using (new WaitCursor(mainWindow))
			{
				using (var dlg = new ConfirmDeleteObjectDlg(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				{
					var ui = new CmObjectUi(ri);
					dlg.SetDlgInfo(ui, Cache, m_mediator, m_propertyTable);
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
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
					() =>
					{
						Cache.DomainDataByFlid.DeleteObj(ri.Hvo);
						int cobjNew;
						var idxNew = ReversalIndexAfterDeletion(Cache, out cobjNew);
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

		internal static IReversalIndex ReversalIndexAfterDeletion(LcmCache cache, out int cobjNew)
		{
			IReversalIndex newIdx;
			cobjNew = cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;
			if (cobjNew == 0)
			{
				// Big trouble ensues if we don't have any reversal indexes at all, so ...
				// Create a reversal index for the current default analysis writing system.
				var riRepo = cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				newIdx = riRepo.FindOrCreateIndexForWs(cache.DefaultAnalWs);

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
			if (Cache.ServiceLocator.GetObject(ReversalIndexGuid) is IReversalIndex)
			{
				m_propertyTable.SetProperty("ReversalIndexGuid", ReversalIndexGuid.ToString(), true);
				m_propertyTable.SetPropertyPersistence("ReversalIndexGuid", true);
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
