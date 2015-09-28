// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FdoUi.Dialogs;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;
using SIL.Utils;

namespace LanguageExplorer.Areas.Lexicon
{
#if RANDYTODO
	// TODO: This has two subclasses:
	// TODO:	LanguageExplorer.Areas.Lexicon.ReversalEntryClerk
	// TODO:	LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS.ReversalClerk
	// TODO: Figure out a better way to organize this so the DAG is intact.
	// TODO: See comment, below, on use of "CreateReversalIndexDlg"
	// TODO: and how it causes circular namespace dependency problems.
#endif
	/// <summary>
	/// This clerk is used to deal with POSes/Entries of a IReversalIndex.
	/// It's subclasses do the object-specific kinds of work.
	/// </summary>
	internal abstract class ReversalClerk : RecordClerk
	{
		/// <summary>
		/// Contructor.
		/// </summary>
		/// <param name="id">Clerk id/name.</param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		internal ReversalClerk(string id, RecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
			: base(id, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
		}

		#region Overrides of RecordClerk

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public override void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			base.InitializeFlexComponent(propertyTable, publisher, subscriber);

			ChangeOwningObjectIfPossible();
		}

		#endregion

		private void ChangeOwningObjectIfPossible()
		{
			var newGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
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
				PropertyTable.SetProperty("ReversalIndexGuid", newGuid.ToString(), true, true);
			}

			var ri = Cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if (ri == null)
			{
				return;
			}

			var layoutName = String.Format("publishReversal-{0}", ri.WritingSystem);
			PropertyTable.SetProperty("ReversalIndexPublicationLayout", layoutName, true, true);

			ICmObject newOwningObj = NewOwningObject(ri);
			if (newOwningObj != OwningObject)
			{
				UpdateFiltersAndSortersIfNeeded(); // Load the index-specific sorter
				OnChangeSorter(); // Update the column headers with sort arrows
				OwningObject = newOwningObj; // This automatically reloads (and sorts) the list
				PropertyTable.SetProperty("ActiveClerkOwningObject", newOwningObj, false, true);
				Publisher.Publish("ClerkOwningObjChanged", this);
			}
		}

		/// <summary>
		/// returns the XmlNode which configures the FormColumn in the BrowseView associated with BulkEdit of ReversalEntries
		/// </summary>
		protected XmlNode BrowseViewFormCol
		{
			get
			{
#if RANDYTODO
	// TODO: This just won't work in the new world order!
#endif
				var path = Path.Combine(Path.Combine(Path.Combine(Path.Combine(FwDirectoryFinder.CodeDirectory,
					FwDirectoryFinder.ksFlexFolderName),
					@"Configuration"),
					@"Lexicon"),
					@"ReversalEntriesBulkEdit");
				var doc = new XmlDocument();
				doc.Load(Path.Combine(path, @"toolConfiguration.xml"));
				var columnNode = doc.SelectSingleNode(@"//column[@label='Form']");
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
		protected override bool TryRestoreSorter()
		{
			var fakevc = new XmlBrowseViewBaseVc
			{
				SuppressPictures = true, // SuppressPictures to make sure that we don't leak anything as this will not be disposed.
				Cache = Cache
			};
			if (base.TryRestoreSorter() && Sorter is GenRecordSorter)
			{
				var sorter = (GenRecordSorter)Sorter;
				var stringFinderComparer = sorter.Comparer as StringFinderCompare;
				if (stringFinderComparer != null)
				{
					var colSpec = ReflectionHelper.GetField(stringFinderComparer.Finder, "m_colSpec") as XmlNode ?? BrowseViewFormCol;
					sorter.Comparer = new StringFinderCompare(LayoutFinder.CreateFinder(Cache, colSpec, fakevc,
						PropertyTable.GetValue<IApp>("App")),
						stringFinderComparer.SubComparer);
				}
				return true;
			}
			if(Sorter is GenRecordSorter) // If we already have a GenRecordSorter, it's probably an existing, valid one.
				return false;
			// Try to create a sorter based on the current Reversal Index's WritingSystem
			var newGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if(newGuid.Equals(Guid.Empty))
				return false;
			var ri = Cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if(ri == null)
				return false;
			var writingSystem = (IWritingSystem)Cache.WritingSystemFactory.get_Engine(ri.WritingSystem);
			m_list.Sorter = new GenRecordSorter(new StringFinderCompare(LayoutFinder.CreateFinder(Cache, BrowseViewFormCol, fakevc,
				PropertyTable.GetValue<IApp>("App")),
				new WritingSystemComparer(writingSystem)));
			return true;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			var window = PropertyTable.GetValue<IFwMainWnd>("window");
			if (window != null)
			{
#if RANDYTODO
	// TODO: add to interface?
				window.ClearInvalidatedStoredData();
#endif
			}
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
					RecordClerk activeClerk = PropertyTable.GetValue<RecordClerk>("ActiveClerk");
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
			var parentIndex = m_list.IndexOfParentOf(item.KeyObject);

			return parentIndex == -1 ? lastValidIndex : GetRootIndex(parentIndex);
		}

#if RANDYTODO
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
#endif

		/// <summary />
		public virtual void OnInsertReversalIndex(object argument)
		{
			CheckDisposed();

			Guid newGuid = CreateNewReversalIndex();
			if (newGuid != Guid.Empty)
			{
				ChangeOwningObject(newGuid);
				var guid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
				if (guid.Equals(Guid.Empty) || !guid.Equals(newGuid))
					SetReversalIndexGuid(newGuid);
			}
		}

		private Guid CreateNewReversalIndex()
		{
			if (Cache == null)
				return Guid.Empty;
			if (Cache.LanguageProject == null)
				return Guid.Empty;
			if (Cache.LanguageProject.LexDbOA == null)
				return Guid.Empty;
#if RANDYTODO
			// TODO: Having this here, with the class down in say Areas (a better location),
			// TODO: Then results in this class referencing LanguageExplorer.Areas.Lexicon
			// TODO: & that namespace also referencing Areas (circular refs, not good).
			// TODO: Figure out how to not do this here.
#endif
			using (CreateReversalIndexDlg dlg = new CreateReversalIndexDlg())
			{
				dlg.Init(Cache);
				// Don't bother if all languages already have a reversal index!
				if (dlg.PossibilityCount > 0)
				{
					if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
					{
						int hvo = dlg.NewReversalIndexHvo;
						return Cache.ServiceLocator.GetObject(hvo).Guid;
					}
				}
			}
			return Guid.Empty;
		}

#if RANDYTODO
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
#endif

		/// <summary />
		public virtual void OnDeleteReversalIndex(object argument)
		{
			CheckDisposed();

			var oldGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (oldGuid.Equals(Guid.Empty))
				return;

			if (Cache == null)
				return;
			IReversalIndex ri = (IReversalIndex)Cache.ServiceLocator.GetObject(oldGuid);
			DeleteReversalIndex(ri);
		}

		/// <summary />
		public void DeleteReversalIndex(IReversalIndex ri)
		{
			CheckDisposed();

			var mainWindow = PropertyTable.GetValue<Form>("window");
			using (new WaitCursor(mainWindow))
			{
				using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				{
					var ui = new CmObjectUi(ri);
					ui.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
					dlg.SetDlgInfo(ui, Cache, PropertyTable);
					dlg.TopMessage = LanguageExplorerResources.ksDeletingThisRevIndex;
					dlg.BottomQuestion = LanguageExplorerResources.ksReallyWantToDeleteRevIndex;
					if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
						ReallyDeleteReversalIndex(ri);
				}
			}
		}

		/// <summary />
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
			Publisher.Publish("MasterRefresh", null);
		}

		/// <summary />
		internal static IReversalIndex ReversalIndexAfterDeletion(FdoCache cache, out int cobjNew)
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

		private void SetReversalIndexGuid(Guid reversalIndexGuid)
		{
			if (Cache.ServiceLocator.GetObject(reversalIndexGuid) is IReversalIndex)
			{
				PropertyTable.SetProperty("ReversalIndexGuid", reversalIndexGuid.ToString(), true, true);
			}
		}

		/// <summary />
		abstract protected ICmObject NewOwningObject(IReversalIndex ri);
	}
}