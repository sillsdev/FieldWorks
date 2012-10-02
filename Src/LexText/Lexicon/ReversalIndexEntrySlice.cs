using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;
using System.ComponentModel;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// A slice to show the IReversalIndexEntry objects.
	/// </summary>
	public class ReversalIndexEntrySlice : ViewPropertySlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications.
		/// </summary>
		private ISilDataAccess m_sda;

		#region ReversalIndexEntrySlice class info
		/// <summary>
		/// Constructor.
		/// </summary>
		public ReversalIndexEntrySlice()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj"></param>
		public ReversalIndexEntrySlice(ICmObject obj) :
			base(new ReversalIndexEntrySliceView(obj.Hvo), obj, LexSenseTags.kflidReversalEntries)
		{
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sda != null)
					m_sda.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Therefore this method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			ReversalIndexEntrySliceView ctrl = new ReversalIndexEntrySliceView(Object.Hvo);
			ctrl.Cache = (FdoCache)Mediator.PropertyTable.GetValue("cache");
			Control = ctrl;
			//m_menuHandler = InflAffixTemplateMenuHandler.Create(ctrl, ConfigurationNode["deParams"]);
#if !Want
			//m_menuHandler.Init(this.Mediator, null);
#else
			//m_menuHandler.Init(null, null);
#endif
			//ctrl.SetContextMenuHandler(new InflAffixTemplateEventHandler(m_menuHandler.ShowSliceContextMenu));
			ctrl.Mediator = Mediator;
			m_sda = ctrl.Cache.DomainDataByFlid;
			m_sda.AddNotification(this);

			if (ctrl.RootBox == null)
				ctrl.MakeRoot();
		}

		#endregion ReversalIndexEntrySlice class info

		#region IVwNotifyChange methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The dafault behavior is for change watchers to call DoEffectsOfPropChange if the
		/// data for the tag being watched has changed.
		/// </summary>
		/// <param name="hvo">The object that was changed</param>
		/// <param name="tag">The property of the object that was changed</param>
		/// <param name="ivMin">the starting character index where the change occurred</param>
		/// <param name="cvIns">the number of characters inserted</param>
		/// <param name="cvDel">the number of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (hvo == m_obj.Hvo && tag == LexSenseTags.kflidReversalEntries)
			{
				ReversalIndexEntrySliceView ctrl = Control as ReversalIndexEntrySliceView;
				ctrl.ResetEntries();
			}
		}

		#endregion

		#region RootSite class

		public class ReversalIndexEntrySliceView : RootSiteControl
		{
			#region Events

			/// <summary>
			/// This allows either the launcher or the embedded view to communicate size changes to
			/// the embedding slice.
			/// </summary>
			public event FwViewSizeChangedEventHandler ViewSizeChanged;

			#endregion Events

			#region Constants

			// NOTE: The sense ID will be the main object ID being displayed.
			// We will add some dummy flids and IDS to the special cache,
			// so the VC will need to understand all of that.

			// Fake flids.
			public const int kFlidIndices = 5001; // "owner" will be the sense, and its ID.
			public const int kFlidEntries = 5002; // A dummy flid for the reversal index, which holds the entries for the main sense.
			//public const int kErrorMessage = 5003;

			//Fake Ids.
			public const int kDummyEntry = -10000;
			//public const int kDummyPhoneEnvID = -1;

			// View frags.
			public const int kFragMainObject = 1;
			public const int kFragIndices = 2;
			public const int kFragWsAbbr = 3;
			public const int kFragEntryForm = 4;
			public const int kFragIndexMain = 5;
			public const int kFragEntries = 6;

			#endregion Constants

			#region Data members

			private IContainer components;
			protected int m_dummyId;
			/// <summary>
			/// A decorated ISilDataAccess for use with the Views code.
			/// </summary>
			protected ReversalEntryDataAccess m_sdaRev;
			protected ReversalIndexEntryVc m_vc;
			protected int m_heightView;
			protected int m_hvoOldSelection;
			protected ITsStrFactory m_tsf = TsStrFactoryClass.Create();
			protected int m_hvoObj;
			protected ILexSense m_sense;
			protected List<IReversalIndex> m_usedIndices = new List<IReversalIndex>();

			#endregion // Data members

			public ReversalIndexEntrySliceView(int hvo)
			{
				components = new Container();
				m_hvoObj = hvo;
				m_dummyId = kDummyEntry;
				RightMouseClickedEvent += ReversalIndexEntrySliceView_RightMouseClickedEvent;
			}

			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				// Must not be run more than once.
				if (IsDisposed)
					return;

				base.Dispose(disposing);

				if (disposing)
				{
					if (components != null)
						components.Dispose();
					if (m_vc != null)
						m_vc.Dispose();
					if (m_usedIndices != null)
						m_usedIndices.Clear();
					if (m_sdaRev != null)
						m_sdaRev.ClearAllData();
				}

				m_sense = null;
				m_vc = null;
				if (m_tsf != null)
					System.Runtime.InteropServices.Marshal.ReleaseComObject(m_tsf);
				m_tsf = null;
				m_sdaRev = null;
				m_usedIndices = null;
			}

			private void ReversalIndexEntrySliceView_RightMouseClickedEvent(SimpleRootSite sender,
				FwRightMouseClickEventArgs e)
			{
				e.EventHandled = true;
				e.Selection.Install();
				var menu = components.ContextMenuStrip("contextMenu");
					string sMenuText = LexEdStrings.ksShowInReversalIndex;
				var item = new ToolStripMenuItem(sMenuText);
				item.Click += OnShowInReversalIndex;
					menu.Items.Add(item);
					menu.Show(this, e.MouseLocation);
				}

			private void OnShowInReversalIndex(object sender, EventArgs e)
			{
				TextSelInfo tsi = new TextSelInfo(m_rootb);
				int hvo = tsi.HvoAnchor;
				if (hvo == 0)
					return;
				// If the entry is a subentry, we have to find the main entry owned directly by
				// the reversal index.  Otherwise, the jump would go to the first entry in the
				// index.
				FdoCache cache = Cache;
				// If it's a new reversal that hasn't been converted to reality yet, convert it!
				// (See FWR-809.)
				if (!cache.ServiceLocator.IsValidObjectId(hvo))
				{
					try
					{
						int hvoReal = ConvertDummiesToReal(hvo);
						hvo = hvoReal;
						if (hvo == 0)
							return;
					}
					catch
					{
						return;
					}
				}
				IReversalIndexEntry rie = cache.ServiceLocator.GetObject(hvo) as IReversalIndexEntry;
				m_mediator.PostMessage("FollowLink", new FwLinkArgs("reversalToolEditComplete", rie.MainEntry.Guid));
			}

			/// <summary>
			/// Connect to the real data, when the control has lost focus.
			/// </summary>
			/// <param name="e"></param>
			protected override void OnLostFocus(EventArgs e)
			{
				WaitCursor wc = null;
				Form window = FindForm();
				if (window != null)
					wc = new WaitCursor(window);
				try
				{
					ConvertDummiesToReal(0);
					base.OnLostFocus(e);
				}
				finally
				{
					if (wc != null)
					{
						wc.Dispose();
						wc = null;
					}
				}
			}

			private int ConvertDummiesToReal(int hvoDummy)
			{
				// This somehow seems to happen quite often although it should not (LT-11162),
				// at least, not while we have any changes that really need to be saved, since we
				// should have lost focus and saved before doing anything that would cause a regenerate.
				// But let's not crash.
				var extensions = m_fdoCache.ActionHandlerAccessor as IActionHandlerExtensions;
				if (extensions != null && !extensions.CanStartUow)
				{
					return 0;
				}
				List<int> currentEntries = new List<int>();
				int countIndices = m_sdaRev.get_VecSize(m_sense.Hvo, kFlidIndices);
				int hvoReal = 0;
				Set<int> writingSystemsModified = new Set<int>();
				for (int i = 0; i < countIndices; ++i)
				{
					int hvoIndex = m_sdaRev.get_VecItem(m_sense.Hvo, kFlidIndices, i);
					IReversalIndex revIndex = m_fdoCache.ServiceLocator.GetInstance<IReversalIndexRepository>().GetObject(hvoIndex);
					writingSystemsModified.Add(m_fdoCache.ServiceLocator.WritingSystemManager.GetWsFromStr(revIndex.WritingSystem));
					int countRealEntries = m_sdaRev.get_VecSize(hvoIndex, kFlidEntries) - 1; // Skip the dummy entry at the end.
					// Go through it from the far end, since we may be deleting empty items.
					for (int j = countRealEntries - 1; j >= 0; --j)
					{
						int hvoEntry = m_sdaRev.get_VecItem(hvoIndex, kFlidEntries, j);
						// If hvoEntry is greater than 0, then it started as a real entry.
						// If hvoEntry is less than 0, it is clearly a new one.
						// The text may have changed for a real one, so check to see if is the same.
						// If it is the same, then just add it to the currentEntries array.
						// If it is different, or if it is a new one, then we need to
						// see if it already exists in the index.
						// If it exists, then add it to the currentEntries array.
						// If it does not exist, we have to create it, and add it to the currentEntries array.
						List<string> rgsFromDummy = new List<string>();
						if (GetReversalFormsAndCheckExisting(currentEntries, hvoIndex,
							m_fdoCache.ServiceLocator.WritingSystemManager.GetWsFromStr(revIndex.WritingSystem), j, hvoEntry, rgsFromDummy))
						{
							continue;
						}
						// At this point, we need to find or create one or more entries.
						int hvo = FindOrCreateReversalEntry(revIndex, rgsFromDummy, m_fdoCache);
						currentEntries.Add(hvo);
						if (hvoEntry == hvoDummy)
							hvoReal = hvo;
					}
				}
				// Reset the sense's ref. property to all the ids in the currentEntries array.
				int[] ids = currentEntries.ToArray();
				if (!m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().IsValidObjectId(m_sense.Hvo))
					return 0; // our object has been deleted while we weren't looking!
				int countEntries = m_fdoCache.DomainDataByFlid.get_VecSize(m_sense.Hvo, LexSenseTags.kflidReversalEntries);
				// Check the current state and don't save (or create an Undo stack item) if
				// nothing has changed.
				bool fChanged = true;
				if (countEntries == ids.Length)
				{
					fChanged = false;
					for (int i = 0; i < countEntries; ++i)
					{
						int id = m_fdoCache.DomainDataByFlid.get_VecItem(m_sense.Hvo, LexSenseTags.kflidReversalEntries, i);
						if (id != ids[i])
						{
							fChanged = true;
							break;
						}
					}
				}
				if (fChanged)
				{
					m_fdoCache.DomainDataByFlid.BeginUndoTask(LexEdStrings.ksUndoSetRevEntries,
						LexEdStrings.ksRedoSetRevEntries);
					m_sdaRev.Replace(m_sense.Hvo, LexSenseTags.kflidReversalEntries, 0, countEntries, ids, ids.Length);
					m_fdoCache.DomainDataByFlid.EndUndoTask();
				}
				return hvoReal;
			}

			/// <summary>
			/// Get the reversal index entry form(s), and check whether these are empty (link is
			/// being deleted) or the same as before (link is unchanged).  In either of these
			/// two cases, do what is needed and return true.  Otherwise return false (linked
			/// entry must be found or created).
			/// </summary>
			private bool GetReversalFormsAndCheckExisting(List<int> currentEntries, int hvoIndex,
				int wsIndex, int irieSense, int hvoEntry, List<string> rgsFromDummy)
			{
				string fromDummyCache = null;
				ITsString tss = m_sdaRev.get_MultiStringAlt(hvoEntry,
					ReversalIndexEntryTags.kflidReversalForm, wsIndex);
				if (tss != null)
					fromDummyCache = tss.Text;
				if (fromDummyCache != null)
					GetFormList(fromDummyCache, rgsFromDummy);
				if (rgsFromDummy.Count == 0)
				{
					// The entry at 'irieSense' is being deleted, so
					// remove it from the dummy cache.
					RemoveFromDummyCache(hvoIndex, irieSense);
					//fNeedPropChange = true;
					return true;
				}
				if (hvoEntry > 0)
				{
					IReversalIndexEntry rie =
						m_fdoCache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(hvoEntry);
					if (rgsFromDummy[rgsFromDummy.Count - 1] == rie.ReversalForm.get_String(wsIndex).Text)
					{
						// Check that all parents exist as specified.  If so, then the user didn't change
						// this entry link.
						bool fSame = true;
						for (int i = rgsFromDummy.Count - 2; i >= 0; --i)
						{
							if (rie.OwningFlid == ReversalIndexEntryTags.kflidSubentries)
							{
								rie = rie.Owner as IReversalIndexEntry;
								if (rgsFromDummy[i] == rie.ReversalForm.get_String(wsIndex).Text)
									continue;
							}
							fSame = false;
							break;
						}
						if (fSame)
						{
							// Add hvoEntry to the currentEntries array.
							currentEntries.Add(hvoEntry);
							//fNeedPropChange = true;
							return true;
						}
					}
				}
				return false;
			}

			/// <summary>
			/// Find the reversal index entry given by rgsForms, or if it doesn't exist, create
			/// it.  In either case, return its hvo.
			/// </summary>
			public static int FindOrCreateReversalEntry(IReversalIndex revIndex, List<string> rgsForms,
				FdoCache cache)
			{
				List<List<IReversalIndexEntry>> rgrieMatching = new List<List<IReversalIndexEntry>>(rgsForms.Count);
				// This could be SLOOOOOOOOOOW!  But I don't see a better way of doing it...
				for (int i = 0; i < rgsForms.Count; ++i)
					rgrieMatching.Add(new List<IReversalIndexEntry>());
				int wsIndex = cache.ServiceLocator.WritingSystemManager.GetWsFromStr(revIndex.WritingSystem);
				foreach (IReversalIndexEntry rie in revIndex.AllEntries)
				{
					string form = rie.ReversalForm.get_String(wsIndex).Text;
					int idx = rgsForms.IndexOf(form);
					if (idx >= 0)
						rgrieMatching[idx].Add(rie);
				}
				List<int> rghvoOwners = new List<int>(rgsForms.Count);
				rghvoOwners.Add(revIndex.Hvo);
				// The next two variables record the best partial match, if any.
				int maxLevel = 0;
				int maxOwner = revIndex.Hvo;
				int hvo = FindMatchingReversalEntry(rgsForms, rghvoOwners, rgrieMatching,
					0, ref maxLevel, ref maxOwner);
				if (hvo == 0)
				{
					cache.DomainDataByFlid.BeginUndoTask(LexEdStrings.ksCreateReversal,
						LexEdStrings.ksRecreateReversal);
					// Create whatever we need to since we didn't find a full match.
					ICmObject owner =
						cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(maxOwner);
					Debug.Assert(maxLevel < rgsForms.Count);
					IReversalIndexEntryFactory fact = cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
					for (int i = maxLevel; i < rgsForms.Count; ++i)
					{
						IReversalIndexEntry rie = fact.Create();
						if (owner is IReversalIndex)
						{
							(owner as IReversalIndex).EntriesOC.Add(rie);
						}
						else
						{
							Debug.Assert(owner is IReversalIndexEntry);
							(owner as IReversalIndexEntry).SubentriesOC.Add(rie);
						}
						rie.ReversalForm.set_String(wsIndex, rgsForms[i]);
						owner = rie;
						hvo = rie.Hvo;
					}
					Debug.Assert(hvo != 0);
					cache.DomainDataByFlid.EndUndoTask();
				}
				return hvo;
			}

			private static int FindMatchingReversalEntry(List<string> rgsForms,
				List<int> rghvoOwners, List<List<IReversalIndexEntry>> rgrieMatching,
				int idxForms, ref int maxLevel, ref int maxOwner)
			{
				foreach (IReversalIndexEntry rie in rgrieMatching[idxForms])
				{
					Debug.Assert(rie.ReversalIndex.Hvo == rghvoOwners[0]);
					if (rie.ReversalIndex.Hvo != rghvoOwners[0])
						continue;
					if (rie.Owner.Hvo != rghvoOwners[idxForms])
						continue;
					int level = idxForms + 1;
					if (level < rgsForms.Count)
					{
						if (level > maxLevel)
						{
							maxLevel = level;
							maxOwner = rie.Hvo;
						}
						// we have a match at this level: recursively check the next level.
						rghvoOwners.Add(rie.Hvo);
						int hvo = FindMatchingReversalEntry(rgsForms, rghvoOwners, rgrieMatching,
							level, ref maxLevel, ref maxOwner);
						if (hvo != 0)
							return hvo;
						rghvoOwners.RemoveAt(level);
					}
					else
					{
						// We have a match all the way down: return the hvo.
						return rie.Hvo;
					}
				}
				return 0;
			}

			/// <summary>
			/// Given a string purporting to be the LongName of a reversal index entry,
			/// split it into the sequence of individial RIE forms that it represents
			/// (from the top of the hierarchy down).
			/// </summary>
			public static void GetFormList(string longNameIn, List<string> forms)
			{
				string longName = longNameIn.Trim();
				forms.Clear();
				// allow the user to indicate subentries by separating words by ':'.
				// See LT-4665.
				string[] rgsDummy = longName.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < rgsDummy.Length; ++i)
				{
					rgsDummy[i] = rgsDummy[i].Trim();
					if (!String.IsNullOrEmpty(rgsDummy[i]))
						forms.Add(rgsDummy[i]);
				}
			}

			/// <summary>
			/// The main set of reversal entries has changed, so update the display.
			/// </summary>
			internal void ResetEntries()
			{
				if (m_rootb != null)
				{
					LoadDummyCache(true);
					m_rootb.Reconstruct();
				}
				else
				{
					if (m_sdaRev != null)
						m_sdaRev.ClearAllData();
					if (m_usedIndices != null)
						m_usedIndices.Clear();
				}
			}

			public override void MakeRoot()
			{
				CheckDisposed();

				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;

				// A crude way of making sure the property we want is loaded into the cache.
				m_sense = (ILexSense)m_fdoCache.ServiceLocator.GetObject(m_hvoObj);
				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				if (m_sdaRev == null)
					m_sdaRev = new ReversalEntryDataAccess(m_fdoCache.DomainDataByFlid as ISilDataAccessManaged);

				LoadDummyCache(false);

				// And maybe this too, at least by default?
				m_rootb.DataAccess = m_sdaRev;
				m_vc = new ReversalIndexEntryVc(m_usedIndices, m_fdoCache);

				// arg4 could be used to supply a stylesheet.
				m_rootb.SetRootObject(m_hvoObj,
					m_vc,
					kFragMainObject,
					m_rootb.Stylesheet);
				m_heightView = m_rootb.Height;
			}

			private void LoadDummyCache(bool doFullReload)
			{
				if (doFullReload)
				{
					// Doing everything in this in this block
					// and a Reconstruct call on m_rootb
					// fixes LT-5292.
					m_sdaRev.ClearAllData();
					m_usedIndices.Clear();
				}

				// Display the reversal indexes for the current set of analysis writing systems.
				List<IReversalIndexEntry> entries = new List<IReversalIndexEntry>();
				foreach (IReversalIndexEntry ide in m_sense.ReversalEntriesRC)
					entries.Add(ide);

				foreach (IWritingSystem ws in m_fdoCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				{
					IReversalIndex idx = null;
					foreach (IReversalIndex idxInner in m_fdoCache.LanguageProject.LexDbOA.ReversalIndexesOC)
					{
						if (idxInner.WritingSystem == ws.Id)
						{
							idx = idxInner;
							break;
						}
					}
					if (idx == null)
						continue;	// User must explicitly request another IReversalIndex (LT-4480).

					m_usedIndices.Add(idx);
					// Cache the WS for the index.
					m_sdaRev.CacheUnicodeProp(idx.Hvo, ReversalIndexTags.kflidWritingSystem, idx.WritingSystem, idx.WritingSystem.Length);
					// Cache the WS abbreviation in the dummy cache.
					m_sdaRev.CacheStringProp(ws.Handle, ReversalEntryDataAccess.kflidWsAbbr,
						m_fdoCache.TsStrFactory.MakeString(ws.Abbreviation, m_fdoCache.DefaultUserWs));

					// Cache entries used by the sense for idx.
					// Cache the vector of IDs for referenced reversal entries.
					// They are actually stored in one flid in the database,
					// but we need to split them up and store them in dummy flids.
					// We will need one dummy flid for each reversal index.
					// As the user adds new entries, the dummy ID data member server will keep
					// decrementing its count. The cache may end up with any number of IDs that
					// are less than m_dummyEntryId.
					List<int> entryIds = new List<int>();
					foreach (IReversalIndexEntry rie in idx.EntriesForSense(entries))
						entryIds.Add(rie.Hvo);
					int wsHandle = m_fdoCache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
					// Cache a dummy string for each WS.
					ITsString tssEmpty = m_tsf.EmptyString(wsHandle);
					m_sdaRev.CacheStringAlt(m_dummyId, ReversalIndexEntryTags.kflidReversalForm,
						wsHandle, tssEmpty);
					entryIds.Add(m_dummyId--);
					// Cache one or more entry IDs for each index.
					// The last one is a dummy entry, which allows the user to type new ones.
					m_sdaRev.CacheVecProp(idx.Hvo, kFlidEntries, entryIds.ToArray(), entryIds.Count);
				}

				// Cache the reversal index IDs in a dummy flid of the sense.
				int[] rIds = new int[m_usedIndices.Count];
				for (int i = 0; i < m_usedIndices.Count; ++i)
				{
					IReversalIndex idx = m_usedIndices[i];
					rIds[i] = idx.Hvo;
				}
				m_sdaRev.CacheVecProp(m_sense.Hvo, kFlidIndices, rIds, rIds.Length);

				// Cache the strings for each entry in the vector.
				foreach (IReversalIndexEntry ent in m_sense.ReversalEntriesRC)
				{
					int cform = ent.ReversalForm.StringCount;
					for (int i = 0; i < cform; ++i)
					{
						int ws;
						string form = ent.ReversalForm.GetStringFromIndex(i, out ws).Text;
						// If the entry is actually a subentry, display the form(s) of its
						// parent(s) as well, separating levels by a colon.  See LT-4665.
						if (ent.OwningFlid != ReversalIndexTags.kflidEntries)
						{
							StringBuilder bldr = new StringBuilder(form);
							for (IReversalIndexEntry rie = ent.OwningEntry; rie != null; rie = rie.OwningEntry)
							{
								form = rie.ReversalForm.get_String(ws).Text;
								if (form == null)
									form = String.Empty;
								bldr.Insert(0, ": ");
								bldr.Insert(0, form);
							}
							form = bldr.ToString();
						}
						if (form != null)
						{
							ITsString tss = m_tsf.MakeString(form, ws);
							m_sdaRev.CacheStringAlt(ent.Hvo, ReversalIndexEntryTags.kflidReversalForm, ws, tss);
						}
					}
				}
			}

			protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
			{
				CheckDisposed();

				if (vwselNew == null)
					return;

				base.HandleSelectionChange(rootb, vwselNew);

				ITsString tss;
				int ichAnchor;
				bool fAssocPrev;
				int hvoObj;
				int tag;
				int ws;
				vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
				int ichEnd;
				int hvoObjEnd;
				vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd, out tag, out ws);
				if (hvoObjEnd != hvoObj)
				{
					// Can't do much with a multi-object selection.
					CheckHeight();
					return;
				}

				// The next level out in the view should be the entry in the index.
				int hvoIndex, ihvoEntry, cpropPrevious, tagEntry;
				IVwPropertyStore vps;
				vwselNew.PropInfo(false, 1, out hvoIndex, out tagEntry, out ihvoEntry, out cpropPrevious, out vps);
				// And the next one is the relevant index.
				int hvoSense, tagIndex, ihvoIndex;
				vwselNew.PropInfo(false, 2, out hvoSense, out tagIndex, out ihvoIndex, out cpropPrevious, out vps);

				int count = m_sdaRev.get_VecSize(hvoIndex, kFlidEntries);
				int lastEntryHvo = m_sdaRev.get_VecItem(hvoIndex, kFlidEntries, count - 1);

				string oldForm = null;
				var wsString = m_sdaRev.get_UnicodeProp(hvoIndex, ReversalIndexTags.kflidWritingSystem);
				int wsIndex = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(wsString);
				ITsString tssEntry = m_sdaRev.get_MultiStringAlt(m_hvoOldSelection,
					ReversalIndexEntryTags.kflidReversalForm, wsIndex);
				if (tssEntry != null)
					oldForm = tssEntry.Text;
				if (m_hvoOldSelection != 0
					&& hvoObj != m_hvoOldSelection
					&& (oldForm == null || oldForm.Length  == 0))
				{
					// Remove the old string from the dummy cache, since its length is 0.
					for (int i = 0; i < count; ++i)
					{
						if (m_hvoOldSelection == m_sdaRev.get_VecItem(hvoIndex, kFlidEntries, i))
						{
							RemoveFromDummyCache(hvoIndex, i);
							break;
						}
					}
				}
				// If it's not the last index in the list, we can just go on editing it.
				if (hvoObj != lastEntryHvo)
				{
					m_hvoOldSelection = hvoObj;
					CheckHeight();
					return;
				}
				// Even if it's the last object, if it's empty we don't need to do anything.
				if (tss.Length == 0)
				{
					CheckHeight();
					return;
				}
				// Create a new object, and recreate a new empty object.
				count = m_sdaRev.get_VecSize(hvoIndex, kFlidEntries);
				// Assign a new dummy ID.
				m_dummyId--;
				// Insert it at the end of the list.
				m_sdaRev.CacheReplace(hvoIndex, kFlidEntries, count, count, new int[] {m_dummyId}, 1);
				// Set its 'form' to be an empty string in the appropriate writing system.
				ITsTextProps props = tss.get_PropertiesAt(0);
				int nVar;
				ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				ITsString tssEmpty = m_tsf.EmptyString(ws);
				m_sdaRev.CacheStringAlt(m_dummyId, ReversalIndexEntryTags.kflidReversalForm,
					ws, tssEmpty);
				// Refresh
				RootBox.PropChanged(hvoIndex, kFlidEntries, count, 1, 0);

				// Reset selection.
				SelLevInfo[] rgvsli = new SelLevInfo[2];
				rgvsli[0].cpropPrevious = 0;
				rgvsli[0].tag = kFlidEntries;
				rgvsli[0].ihvo = count - 1;
				rgvsli[1].cpropPrevious = 0;
				rgvsli[1].tag = kFlidIndices;
				rgvsli[1].ihvo = ihvoIndex;
				try
				{
					m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, tag, 0, ichAnchor, ichEnd, ws, fAssocPrev, -1, null, true);
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.ToString());
					throw;
				}

				m_hvoOldSelection = hvoObj;
				CheckHeight();
			}

			private void CheckHeight()
			{
				if (m_rootb != null)
				{
					int hNew = m_rootb.Height;
					if (m_heightView != hNew)
					{
						if (ViewSizeChanged != null)
						{
							ViewSizeChanged(this,
								new FwViewSizeEventArgs(hNew, m_rootb.Width));
						}
						m_heightView = hNew;
					}
				}
			}

			private void RemoveFromDummyCache(int hvoIndex, int index)
			{
				m_sdaRev.CacheReplace(hvoIndex, kFlidEntries, index, index + 1, new int[0], 0);
			}
		}

		#endregion RootSite class

		#region View constructor class

		public class ReversalIndexEntryVc : FwBaseVc, IDisposable
		{
			private List<IReversalIndex> m_usedIndices;
			ITsTextProps m_ttpLabel; // Props to use for ws name labels.
			ITsStrFactory m_tsf;

			public ReversalIndexEntryVc(List<IReversalIndex> usedIndices, FdoCache cache)
			{
				Cache = cache;
				m_usedIndices = usedIndices;
				m_ttpLabel = WritingSystemServices.AbbreviationTextProperties;
				m_tsf = TsStrFactoryClass.Create();
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~ReversalIndexEntryVc()
			{
				Dispose(false);
			}
			#endif

			/// <summary>
			/// Throw if the IsDisposed property is true
			/// </summary>
			public void CheckDisposed()
			{
				if (IsDisposed)
					throw new ObjectDisposedException(GetType().ToString(), "This object is being used after it has been disposed: this is an Error.");
			}

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
				if (fDisposing && !IsDisposed)
				{
					// Dispose managed resources here.
					if (m_usedIndices != null)
						m_usedIndices.Clear();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_cache = null;
				m_usedIndices = null;
				m_ttpLabel = null;
				m_tsf = null;
				IsDisposed = true;
			}
			#endregion

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				ISilDataAccess da = vwenv.DataAccess;
				switch (frag)
				{
					default:
					{
						Debug.Assert(false, "Unrecognized fragment.");
						break;
					}
					case ReversalIndexEntrySliceView.kFragMainObject:
					{
						// The hvo here is for the sense.

						// We use a table to display
						// encodings in column one and the strings in column two.
						// The table uses 100% of the available width.
						VwLength vlTable;
						vlTable.nVal = 10000;
						vlTable.unit = VwUnit.kunPercent100;
						// The width of the writing system column is determined from the width of the
						// longest one which will be displayed.
						int dxs;	// Width of displayed string.
						int dys;	// Height of displayed string (not used here).
						int dxsMax = 0;	// Max width required.
						ISilDataAccess sda = vwenv.DataAccess;
						foreach (IReversalIndex idx in m_usedIndices)
						{
							int wsHandle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
							vwenv.get_StringWidth(sda.get_StringProp(wsHandle, ReversalEntryDataAccess.kflidWsAbbr),
								m_ttpLabel,
								out dxs,
								out dys);
							dxsMax = Math.Max(dxsMax, dxs);
						}
						VwLength vlColWs; // 5-pt space plus max label width.
						vlColWs.nVal = dxsMax + 5000;
						vlColWs.unit = VwUnit.kunPoint1000;
						// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?
						// The Main column is relative and uses the rest of the space.
						VwLength vlColMain;
						vlColMain.nVal = 1;
						vlColMain.unit = VwUnit.kunRelative;

						vwenv.OpenTable(2, // Two columns.
							vlTable, // Table uses 100% of available width.
							0, // Border thickness.
							VwAlignment.kvaLeft, // Default alignment.
							VwFramePosition.kvfpVoid, // No border.
							VwRule.kvrlNone, // No rules between cells.
							0, // No forced space between cells.
							0, // No padding inside cells.
							false);
						// Specify column widths. The first argument is the number of columns,
						// not a column index. The writing system column only occurs at all if its
						// width is non-zero.
						vwenv.MakeColumns(1, vlColWs);
						vwenv.MakeColumns(1, vlColMain);

						vwenv.OpenTableBody();
						// Do vector of rows. Each row essentially is a reversal index, but shows other information.
						vwenv.AddObjVec(ReversalIndexEntrySliceView.kFlidIndices, this, ReversalIndexEntrySliceView.kFragIndices);
						vwenv.CloseTableBody();
						vwenv.CloseTable();
						break;
					}
					case ReversalIndexEntrySliceView.kFragIndexMain:
					{
						// First cell has writing system abbreviation displayed using m_ttpLabel.
						int wsHvo = 0;
						foreach (IReversalIndex idx in m_usedIndices)
						{
							if (idx.Hvo == hvo)
							{
								wsHvo = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
								break;
							}
						}
						Debug.Assert(wsHvo > 0, "Could not find writing system.");

						int wsOldDefault = DefaultWs;
						DefaultWs = wsHvo;

						// Cell 1 shows the ws abbreviation.
						vwenv.OpenTableCell(1,1);
						vwenv.Props = m_ttpLabel;
						vwenv.AddObj(wsHvo, this, ReversalIndexEntrySliceView.kFragWsAbbr);
						vwenv.CloseTableCell();

						// Second cell has the contents for the reversal entries.
						vwenv.OpenTableCell(1,1);
						// This displays the field flush right for RTL data, but gets arrow keys to
						// behave reasonably.  See comments on LT-5287.
						IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(DefaultWs);
						if (wsObj != null && wsObj.RightToLeftScript)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
								(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						}
						vwenv.OpenParagraph();
						// Do vector of entries in the second column.
						vwenv.AddObjVec(ReversalIndexEntrySliceView.kFlidEntries, this, ReversalIndexEntrySliceView.kFragEntries);
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();

						DefaultWs = wsOldDefault;
						break;
					}
					case ReversalIndexEntrySliceView.kFragEntryForm:
					{
						vwenv.AddStringAltMember(ReversalIndexEntryTags.kflidReversalForm, DefaultWs, this);
						int hvoCurrent = vwenv.CurrentObject();
						if (hvoCurrent > 0)
						{
							IReversalIndexEntry rie =
								m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(hvoCurrent);
							Debug.Assert(rie != null);
							List<IWritingSystem> rgWs = WritingSystemServices.GetReversalIndexWritingSystems(m_cache, rie.Hvo, false);
							int wsAnal = m_cache.DefaultAnalWs;
							ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
								(int)FwTextPropVar.ktpvDefault, wsAnal);
							tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
								(int)FwTextPropVar.ktpvEnum,
								(int)TptEditable.ktptNotEditable);
							tisb.Append(" [");
							int cstr = 0;
							ITsTextProps ttpBase = null;
							ITsTextProps ttpLabel = null;
							for (int i = 0; i < rgWs.Count; ++i)
							{
								int ws = rgWs[i].Handle;
								if (ws == DefaultWs)
									continue;
								string sForm = rie.ReversalForm.get_String(ws).Text;
								if (!string.IsNullOrEmpty(sForm))
								{
									if (cstr > 0)
										tisb.Append(", ");
									++cstr;
									string sWs = rgWs[i].Abbreviation;
									if (!string.IsNullOrEmpty(sWs))
									{
										if (ttpBase == null)
										{
											ITsPropsBldr tpbLabel = m_ttpLabel.GetBldr();
											tpbLabel.SetIntPropValues((int)FwTextPropType.ktptWs,
												(int)FwTextPropVar.ktpvDefault, wsAnal);
											ttpLabel = tpbLabel.GetTextProps();
											// We have to totally replace the properties set by ttpLabel.  The
											// simplest way is to create another ITsString with the simple base
											// property of only the default analysis writing system.
											ITsPropsBldr tpbBase = TsPropsBldrClass.Create();
											tpbBase.SetIntPropValues((int)FwTextPropType.ktptWs,
												(int)FwTextPropVar.ktpvDefault, wsAnal);
											ttpBase = tpbBase.GetTextProps();
										}
										ITsString tssWs = m_tsf.MakeStringWithPropsRgch(sWs, sWs.Length, ttpLabel);
										tisb.AppendTsString(tssWs);
										ITsString tssSpace = m_tsf.MakeStringWithPropsRgch(" ", 1, ttpBase);
										tisb.AppendTsString(tssSpace);
									}
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
										(int)FwTextPropVar.ktpvDefault, ws);
									tisb.Append(sForm);
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
										(int)FwTextPropVar.ktpvDefault, wsAnal);
								}
							}
							if (cstr > 0)
							{
								tisb.Append("]");
								ITsString tss = tisb.GetString();
								vwenv.AddString(tss);
							}
						}
						break;
					}
					case ReversalIndexEntrySliceView.kFragWsAbbr:
					{
						vwenv.AddString(da.get_StringProp(hvo, ReversalEntryDataAccess.kflidWsAbbr));
						break;
					}
				}
			}

			public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
			{
				CheckDisposed();

				ISilDataAccess da = vwenv.DataAccess;
				switch (frag)
				{
					default:
					{
						Debug.Assert(false, "Unrecognized fragment.");
						break;
					}
					case ReversalIndexEntrySliceView.kFragIndices:
					{
						// hvo here is the sense.
						int countRows = da.get_VecSize(hvo, tag);
						Debug.Assert(countRows == m_usedIndices.Count, "Mismatched number of indices.");
						for (int i = 0; i < countRows; ++i)
						{
							vwenv.OpenTableRow();

							int idxHvo = da.get_VecItem(hvo, tag, i);
							vwenv.AddObj(idxHvo, this, ReversalIndexEntrySliceView.kFragIndexMain);

							vwenv.CloseTableRow();
						}
						break;
					}
					case ReversalIndexEntrySliceView.kFragEntries:
					{
						int wsHvo = 0;
						foreach (IReversalIndex idx in m_usedIndices)
						{
							if (idx.Hvo == hvo)
							{
								wsHvo = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
								break;
							}
						}
						Debug.Assert(wsHvo > 0, "Could not find writing system.");
						int wsOldDefault = DefaultWs;
						DefaultWs = wsHvo;

						// hvo here is a reversal index.
						int countEntries = da.get_VecSize(hvo, ReversalIndexEntrySliceView.kFlidEntries);
						for (int j = 0; j < countEntries; ++j)
						{
							if (j != 0)
								vwenv.AddSeparatorBar();
							int entryHvo = da.get_VecItem(hvo, ReversalIndexEntrySliceView.kFlidEntries, j);
							vwenv.AddObj(entryHvo, this, ReversalIndexEntrySliceView.kFragEntryForm);
						}

						DefaultWs = wsOldDefault;
						break;
					}
				}
			}
		}
		#endregion View constructor class

		#region ISilDataAccess decorator class
		/// <summary>
		/// Decorated ISilDataAccess for accessing temporary, interactively edit reversal index
		/// entry (forms).
		/// </summary>
		public class ReversalEntryDataAccess : DomainDataByFlidDecoratorBase, IVwCacheDa
		{
			public const int kflidWsAbbr = 89999123;

			struct HvoWs
			{
				private int hvo;
				private int ws;
				internal HvoWs(int hvoIn, int wsIn)
				{
					this.hvo = hvoIn;
					this.ws = wsIn;
				}
			}
			Dictionary<HvoWs, ITsString> m_mapHvoWsRevForm = new Dictionary<HvoWs, ITsString>();
			Dictionary<int, int[]> m_mapIndexHvoEntryHvos = new Dictionary<int, int[]>();
			Dictionary<int, int[]> m_mapSenseHvoIndexHvos = new Dictionary<int, int[]>();
			Dictionary<int, ITsString> m_mapWsAbbr = new Dictionary<int, ITsString>();
			Dictionary<int, string> m_mapHvoIdxWs = new Dictionary<int, string>();

			public ReversalEntryDataAccess(ISilDataAccessManaged sda)
				: base(sda)
			{
			}

			#region ISilDataAccess overrides

			public override int get_VecSize(int hvo, int tag)
			{
				if (tag == ReversalIndexEntrySliceView.kFlidEntries)
				{
					int[] rghvo;
					if (m_mapIndexHvoEntryHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo.Length;
					}
					else
					{
						throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidEntries)");
					}
				}
				else if (tag == ReversalIndexEntrySliceView.kFlidIndices)
				{
					int[] rghvo;
					if (m_mapSenseHvoIndexHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo.Length;
					}
					else
					{
						throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidIndices)");
					}
				}
				else
				{
					return base.get_VecSize(hvo, tag);
				}
			}

			public override int get_VecItem(int hvo, int tag, int index)
			{
				if (tag == ReversalIndexEntrySliceView.kFlidEntries)
				{
					int[] rghvo;
					if (m_mapIndexHvoEntryHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo[index];
					}
					else
					{
						throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidEntries)");
					}
				}
				else if (tag == ReversalIndexEntrySliceView.kFlidIndices)
				{
					int[] rghvo;
					if (m_mapSenseHvoIndexHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo[index];
					}
					else
					{
						throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidIndices)");
					}
				}
				else
				{
					return base.get_VecItem(hvo, tag, index);
				}
			}

			public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
			{
				if (tag == ReversalIndexEntryTags.kflidReversalForm)
				{
					HvoWs key = new HvoWs(hvo, ws);
					ITsString tss;
					if (m_mapHvoWsRevForm.TryGetValue(key, out tss))
						return tss;
					else
						return null;
				}
				else
				{
					return base.get_MultiStringAlt(hvo, tag, ws);
				}
			}

			public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
			{
				if (tag == ReversalIndexEntryTags.kflidReversalForm)
				{
					HvoWs key = new HvoWs(hvo, ws);
					m_mapHvoWsRevForm[key] = _tss;
				}
				else
				{
					base.SetMultiStringAlt(hvo, tag, ws, _tss);
				}
			}

			public override ITsString get_StringProp(int hvo, int tag)
			{
				if (tag == kflidWsAbbr)
				{
					ITsString tss;
					if (m_mapWsAbbr.TryGetValue(hvo, out tss))
						return tss;
					else
						return null;
				}
				else
				{
					return base.get_StringProp(hvo, tag);
				}
			}

			public override string get_UnicodeProp(int hvo, int tag)
			{
				if (tag == ReversalIndexTags.kflidWritingSystem)
				{
					string val;
					if (m_mapHvoIdxWs.TryGetValue(hvo, out val))
						return val;
					else
						return null;
				}
				else
				{
					return base.get_UnicodeProp(hvo, tag);
				}
			}
			public override void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				if (tag == ReversalIndexEntrySliceView.kFlidEntries)
				{
					// What can we do here??
					base.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
				}
				else
				{
					base.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
				}
			}
			#endregion

			#region IVwCacheDa Members

			public void CacheBinaryProp(int obj, int tag, byte[] _rgb, int cb)
			{
				throw new NotImplementedException();
			}

			public void CacheBooleanProp(int obj, int tag, bool val)
			{
				throw new NotImplementedException();
			}

			public void CacheGuidProp(int obj, int tag, Guid uid)
			{
				throw new NotImplementedException();
			}

			public void CacheInt64Prop(int obj, int tag, long val)
			{
				throw new NotImplementedException();
			}

			public void CacheIntProp(int obj, int tag, int val)
			{
				throw new NotImplementedException();
			}

			public void CacheObjProp(int obj, int tag, int val)
			{
				throw new NotImplementedException();
			}

			public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
			{
				if (tag == ReversalIndexEntrySliceView.kFlidEntries)
				{
					int[] rghvoOld;
					if (m_mapIndexHvoEntryHvos.TryGetValue(hvoObj, out rghvoOld))
					{
						List<int> rghvoNew = new List<int>();
						rghvoNew.AddRange(rghvoOld);
						if (ihvoMin != ihvoLim)
							rghvoNew.RemoveRange(ihvoMin, ihvoLim - ihvoMin);
						if (chvo != 0)
							rghvoNew.InsertRange(ihvoMin, _rghvo);
						m_mapIndexHvoEntryHvos[hvoObj] = rghvoNew.ToArray();
					}
					else
					{
						throw new ArgumentException("data not stored for CacheReplace(ReversalIndexEntrySliceView.kFlidEntries)");
					}
				}
				//else if (tag == ReversalIndexEntrySliceView.kFlidIndices)
				//{	THIS IS NEVER USED...
				//}
				else
				{
					throw new ArgumentException("we can only handle ReversalIndexEntrySliceView.kFlidEntries here!");
				}
			}

			public void CacheStringAlt(int obj, int tag, int ws, ITsString _tss)
			{
				if (tag == ReversalIndexEntryTags.kflidReversalForm)
				{
					HvoWs key = new HvoWs(obj, ws);
					m_mapHvoWsRevForm[key] = _tss;
				}
				else
				{
					throw new ArgumentException("we can only handle ReversalIndexEntryTags.kflidReversalForm here!");
				}
			}

			public void CacheStringProp(int obj, int tag, ITsString _tss)
			{
				if (tag == kflidWsAbbr)
				{
					m_mapWsAbbr[obj] = _tss;
				}
				else
				{
					throw new ArgumentException("we can only handle LgWritingSystemTags.kflidAbbr here!");
				}
			}

			public void CacheTimeProp(int hvo, int tag, long val)
			{
				throw new NotImplementedException();
			}

			public void CacheUnicodeProp(int obj, int tag, string _rgch, int cch)
			{
				if (tag == ReversalIndexTags.kflidWritingSystem)
				{
					m_mapHvoIdxWs[obj] = _rgch;
				}
				else
				{
					throw new ArgumentException("we can only handle ReversalIndexTags.kflidWritingSystem here!");
				}
			}

			public void CacheUnknown(int obj, int tag, object _unk)
			{
				throw new NotImplementedException();
			}

			public void CacheVecProp(int obj, int tag, int[] rghvo, int chvo)
			{
				if (tag == ReversalIndexEntrySliceView.kFlidEntries)
					m_mapIndexHvoEntryHvos[obj] = rghvo;
				else if (tag == ReversalIndexEntrySliceView.kFlidIndices)
					m_mapSenseHvoIndexHvos[obj] = rghvo;
				else
					throw new ArgumentException("we can only handle ReversalIndexEntrySliceView fake flids here!");
			}

			public void ClearAllData()
			{
				m_mapHvoWsRevForm.Clear();
				m_mapIndexHvoEntryHvos.Clear();
				m_mapSenseHvoIndexHvos.Clear();
				m_mapWsAbbr.Clear();
				m_mapHvoIdxWs.Clear();
			}

			public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
			{
				throw new NotImplementedException();
			}

			public void ClearInfoAboutAll(int[] _rghvo, int chvo, VwClearInfoAction cia)
			{
				throw new NotImplementedException();
			}

			public void ClearVirtualProperties()
			{
				throw new NotImplementedException();
			}

			public IVwVirtualHandler GetVirtualHandlerId(int tag)
			{
				throw new NotImplementedException();
			}

			public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
			{
				throw new NotImplementedException();
			}

			public void InstallVirtual(IVwVirtualHandler _vh)
			{
				throw new NotImplementedException();
			}

			public int get_CachedIntProp(int obj, int tag, out bool _f)
			{
				throw new NotImplementedException();
			}

			#endregion
		}
		#endregion //ISilDataAccess decorator class
	}
}
