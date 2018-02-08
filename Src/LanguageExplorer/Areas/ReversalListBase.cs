// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using LanguageExplorer.LcmUi.Dialogs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas
{
	internal abstract class ReversalListBase : RecordList
	{
		internal ReversalListBase(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, RecordFilterParameterObject recordFilterParameterObject = null, RecordSorter defaultSorter = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject, defaultSorter)
		{
		}

		#region Overrides of RecordList

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			ChangeOwningObjectIfPossible();
		}

		/// <summary />
		/// <returns><c>true</c> if we changed or initialized a new sorter,
		/// <c>false</c>if the one installed matches the one we had stored to persist.</returns>
		protected override bool TryRestoreSorter()
		{
			var fakevc = new XmlBrowseViewBaseVc
			{
				SuppressPictures = true, // SuppressPictures to make sure that we don't leak anything as this will not be disposed.
				DataAccess = VirtualListPublisher,
				Cache = m_cache
			};
			if (base.TryRestoreSorter() && Sorter is GenRecordSorter)
			{
				var sorter = (GenRecordSorter)Sorter;
				var stringFinderComparer = sorter.Comparer as StringFinderCompare;
				if (stringFinderComparer != null)
				{
					var colSpec = ReflectionHelper.GetField(stringFinderComparer.Finder, "m_colSpec") as XElement ?? BrowseViewFormCol;
					sorter.Comparer = new StringFinderCompare(LayoutFinder.CreateFinder(m_cache, colSpec, fakevc, PropertyTable.GetValue<IApp>("App")), stringFinderComparer.SubComparer);
				}
				return true;
			}
			if (Sorter is GenRecordSorter) // If we already have a GenRecordSorter, it's probably an existing, valid one.
			{
				return false;
			}
			// Try to create a sorter based on the current Reversal Index's WritingSystem
			var newGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (newGuid.Equals(Guid.Empty))
			{
				return false;
			}
			var ri = m_cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if (ri == null)
			{
				return false;
			}
			var writingSystem = (CoreWritingSystemDefinition)m_cache.WritingSystemFactory.get_Engine(ri.WritingSystem);
			Sorter = new GenRecordSorter(new StringFinderCompare(LayoutFinder.CreateFinder(m_cache, BrowseViewFormCol, fakevc, PropertyTable.GetValue<IApp>("App")), new WritingSystemComparer(writingSystem)));
			return true;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public override void OnPropertyChanged(string name)
		{
			var window = PropertyTable.GetValue<IFwMainWnd>("window");
			if (window != null)
			{
#if RANDYTODO
// TODO: add to interface?
				window.ClearInvalidatedStoredData();
#endif
			}
			switch (name)
			{
				default:
					base.OnPropertyChanged(name);
					break;
				case "ReversalIndexGuid":
					ChangeOwningObjectIfPossible();
					break;
				case AreaServices.ToolForAreaNamed_ + AreaServices.LexiconAreaMachineName:
					var rootIndex = GetRootIndex(CurrentIndex);
					JumpToIndex(rootIndex);
					base.OnPropertyChanged(name);
					break;
			}
		}

		#endregion

#if RANDYTODO
/// <summary>
/// This is enabled whenever the ReversalList is active.
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
			var newGuid = CreateNewReversalIndex();
			if (newGuid == Guid.Empty)
			{
				return;
			}
			ChangeOwningObject(newGuid);
			var guid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (guid.Equals(Guid.Empty) || !guid.Equals(newGuid))
			{
				SetReversalIndexGuid(newGuid);
			}
		}

		private Guid CreateNewReversalIndex()
		{
			if (m_cache?.LanguageProject?.LexDbOA == null)
			{
				return Guid.Empty;
			}

			IReversalIndex newReversalIndex = null;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				newReversalIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexFactory>().Create();
				m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.Add(newReversalIndex);
			});
			return newReversalIndex?.Guid ?? Guid.Empty;
		}

		/// <summary />
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

		private void SetReversalIndexGuid(Guid reversalIndexGuid)
		{
			if (m_cache.ServiceLocator.GetObject(reversalIndexGuid) is IReversalIndex)
			{
				PropertyTable.SetProperty("ReversalIndexGuid", reversalIndexGuid.ToString(), SettingsGroup.LocalSettings, true, false);
			}
		}

#if RANDYTODO
	/// <summary>
	/// This is enabled whenever the ReversalList is active.
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
			var oldGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (oldGuid.Equals(Guid.Empty))
			{
				return;
			}

			if (m_cache == null)
			{
				return;
			}
			DeleteReversalIndex((IReversalIndex)m_cache.ServiceLocator.GetObject(oldGuid));
		}

		/// <summary />
		public void DeleteReversalIndex(IReversalIndex ri)
		{
			var mainWindow = PropertyTable.GetValue<Form>("window");
			using (new WaitCursor(mainWindow))
			{
				using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				{
					var ui = new CmObjectUi(ri);
					ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					dlg.SetDlgInfo(ui, m_cache, PropertyTable);
					dlg.TopMessage = LanguageExplorerResources.ksDeletingThisRevIndex;
					dlg.BottomQuestion = LanguageExplorerResources.ksReallyWantToDeleteRevIndex;
					if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
					{
						ReallyDeleteReversalIndex(ri);
					}
				}
			}
		}

		/// <summary />
		protected virtual void ReallyDeleteReversalIndex(IReversalIndex ri)
		{
			try
			{
				Debug.Assert(ri.Hvo == OwningObject.Hvo);
				// can't reload deleted list! (LT-5353)
				ListModificationInProgress = true;
				// We're about to do a MasterRefresh which clobbers the Undo stack,
				// so we might as well make this UOW not undoable
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
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
				ListModificationInProgress = false;
			}
			// Without this, stale data can still display in the BulkEditSenses tool if you
			// recreate the deleted reversal index.
			PropertyTable.GetValue<IFwMainWnd>("window").RefreshAllViews();
		}

		/// <summary>
		/// Returns the index of the root object whose descendent is the object at lastValidIndex.
		/// </summary>
		private int GetRootIndex(int lastValidIndex)
		{
			var item = SortItemAt(lastValidIndex);
			if (item == null)
			{
				return lastValidIndex;
			}
			var parentIndex = IndexOfParentOf(item.KeyObject);

			return parentIndex == -1 ? lastValidIndex : GetRootIndex(parentIndex);
		}

		/// <summary>
		/// Returns the XmlNode which configures the FormColumn in the BrowseView associated with BulkEdit of ReversalEntries
		/// </summary>
		protected XElement BrowseViewFormCol
		{
			get
			{
				var doc = XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters);
				return doc.Root.Element("columns").Elements("column").First(col => col.Attribute("label").Value == "Reversal Form");
			}
		}

		internal void ChangeOwningObjectIfPossible()
		{
			try
			{
				ChangeOwningObject(ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid"));
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
				newGuid = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances().First().Guid;
				PropertyTable.SetProperty("ReversalIndexGuid", newGuid.ToString(), true, true);
			}

			var ri = m_cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if (ri == null)
			{
				return;
			}

			// This looks like our best chance to update a global "Current Reversal Index Writing System" value.
			WritingSystemServices.CurrentReversalWsId = m_cache.WritingSystemFactory.GetWsFromStr(ri.WritingSystem);

			// Generate and store the expected path to a configuration file specific to this reversal index.  If it doesn't
			// exist, code elsewhere will make up for it.
			var layoutName = Path.Combine(LcmFileHelper.GetConfigSettingsDir(m_cache.ProjectId.ProjectFolder), "ReversalIndex", ri.ShortName + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			PropertyTable.SetProperty("ReversalIndexPublicationLayout", layoutName, true, true);

			var newOwningObj = NewOwningObject(ri);
			if (ReferenceEquals(newOwningObj, OwningObject))
			{
				return;
			}
			UpdateFiltersAndSortersIfNeeded(); // Load the index-specific sorter
			OnChangeSorter(); // Update the column headers with sort arrows
			OwningObject = newOwningObj; // This automatically reloads (and sorts) the list
			PropertyTable.SetProperty("ActiveListOwningObject", newOwningObj, false, true);
			Publisher.Publish("RecordListOwningObjChanged", this);
			Publisher.Publish("MasterRefresh", null);
		}

		/// <summary />
		protected abstract ICmObject NewOwningObject(IReversalIndex ri);
	}
}