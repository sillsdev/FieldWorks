using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices; // needed for Marshal
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// A slice to show the ReversalIndexEntry objects.
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
		/// <param name="hvoObj"></param>
		public ReversalIndexEntrySlice(int hvoObj) : base(new ReversalIndexEntrySliceView(hvoObj),
					hvoObj,
					(int)LexSense.LexSenseTags.kflidReversalEntries)
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
			m_sda = ctrl.Cache.MainCacheAccessor;
			m_sda.AddNotification(this);

			if (ctrl.RootBox == null)
				ctrl.MakeRoot();
		}

		#endregion ReversalIndexEntrySlice class info

		#region IVwNotifyChange methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The dafault behavior is for change wathcers to call DoEffectsOfPropChange if the
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

			if (hvo == m_obj.Hvo && tag == (int)LexSense.LexSenseTags.kflidReversalEntries)
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

			protected int m_dummyId;
			// A cache used to interact with the Views code,
			// but which is not the one in the FdoCache.
			protected IVwCacheDa m_vwCache;
			// A cast of m_vwCache.
			protected ISilDataAccess m_silCache;
			protected ReversalIndexEntryVc m_vc = null;
			protected int m_heightView = 0;
			protected int m_hvoOldSelection = 0;
			protected ITsStrFactory m_tsf = TsStrFactoryClass.Create();
			protected int m_hvoObj;
			protected ILexSense m_sense;
			protected List<IReversalIndex> m_usedIndices = new List<IReversalIndex>();

			#endregion Data members

			public ReversalIndexEntrySliceView(int hvo)
			{
				m_hvoObj = hvo;
				m_dummyId = kDummyEntry;
				this.RightMouseClickedEvent += new FwRightMouseClickEventHandler(ReversalIndexEntrySliceView_RightMouseClickedEvent);
			}

			private void ReversalIndexEntrySliceView_RightMouseClickedEvent(SimpleRootSite sender,
				FwRightMouseClickEventArgs e)
			{
				e.EventHandled = true;
				e.Selection.Install();
				ContextMenuStrip menu = new ContextMenuStrip();
				string sMenuText = LexEdStrings.ksShowInReversalIndex;
				ToolStripMenuItem item = new ToolStripMenuItem(sMenuText);
				item.Click += new EventHandler(OnShowInReversalIndex);
				menu.Items.Add(item);
				menu.Show(this, e.MouseLocation);
			}

			private void OnShowInReversalIndex(object sender, EventArgs e)
			{
				TextSelInfo tsi = new TextSelInfo(m_rootb);
				int hvo = tsi.HvoAnchor;
				if (hvo < 0)
					hvo = ConvertDummiesToReal(hvo);
				if (hvo == 0)
					return;
				// If the entry is a subentry, we have to find the main entry owned directly by
				// the reversal index.  Otherwise, the jump would go to the first entry in the
				// index.
				FdoCache cache = Cache;
				IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(cache, hvo);
				hvo = rie.MainEntry.Hvo;
				m_mediator.PostMessage("FollowLink",
					SIL.FieldWorks.FdoUi.FwLink.Create("reversalToolEditComplete",
					cache.GetGuidFromId(hvo), cache.ServerName, cache.DatabaseName));
			}

			/// <summary>
			/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
			/// (See LT-8656 and LT-9119.)
			/// </summary>
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				if (m_fdoCache.VerifyValidObject(m_hvoObj))
					base.OnKeyPress(e);
				else
					e.Handled = true;
			}

			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				base.Dispose(disposing);

				if (disposing)
				{
					if (m_vc != null)
						m_vc.Dispose();
					if (m_usedIndices != null)
						m_usedIndices.Clear();
				}

				m_sense = null;
				m_vc = null;
				if (m_tsf != null)
					System.Runtime.InteropServices.Marshal.ReleaseComObject(m_tsf);
				m_tsf = null;
				m_silCache = null;
				if (m_vwCache != null)
				{
					m_vwCache.ClearAllData();
					System.Runtime.InteropServices.Marshal.ReleaseComObject(m_vwCache);
					m_vwCache = null;
				}
				m_usedIndices = null;
			}

			/// <summary>
			/// Connect to the real data, when the control has lost focus.
			/// </summary>
			/// <param name="e"></param>
			protected override void OnLostFocus(EventArgs e)
			{
				Form window = FindForm();
				if (window != null)
					window.Cursor = Cursors.WaitCursor;

				try
				{
					ConvertDummiesToReal(0);
					base.OnLostFocus(e);
				}
				finally
				{
					Cache.EnableUndo = true;
					if (window != null)
						window.Cursor = Cursors.Default;
				}
			}

			private int ConvertDummiesToReal(int hvoDummy)
			{
				List<int> currentEntries = new List<int>();
				int countIndices = m_silCache.get_VecSize(m_sense.Hvo, kFlidIndices);
				int hvoReal = 0;
				Set<int> writingSystemsModified = new Set<int>();
				for (int i = 0; i < countIndices; ++i)
				{
					int hvoIndex = m_silCache.get_VecItem(m_sense.Hvo, kFlidIndices, i);
					ReversalIndex revIndex = ReversalIndex.CreateFromDBObject(m_fdoCache, hvoIndex) as ReversalIndex;
					writingSystemsModified.Add(revIndex.WritingSystemRAHvo);
					int countRealEntries = m_silCache.get_VecSize(hvoIndex, kFlidEntries) - 1; // Skip the dummy entry at the end.
					// Go through it from the far end, since we may be deleting empty items.
					for (int j = countRealEntries - 1; j >= 0; --j)
					{
						int hvoEntry = m_silCache.get_VecItem(hvoIndex, kFlidEntries, j);
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
							revIndex.WritingSystemRAHvo, j, hvoEntry, rgsFromDummy))
						{
							continue;
						}
						// At this point, we need to find or create one or more entries.
						int hvo = revIndex.FindOrCreateReversalEntry(rgsFromDummy);
						currentEntries.Add(hvo);
						if (hvoEntry == hvoDummy)
							hvoReal = hvo;
					}
				}
				// Reset the sense's ref. property to all the ids in the currentEntries array.
				int[] ids = currentEntries.ToArray();
				int senseFlid = (int)LexSense.LexSenseTags.kflidReversalEntries;
				if (!m_fdoCache.IsValidObject(m_sense.Hvo))
					return 0; // our object has been deleted while we weren't looking!
				int countEntries = m_fdoCache.GetVectorSize(m_sense.Hvo, senseFlid);
				// Check the current state and don't save (or create an Undo stack item) if
				// nothing has changed.
				bool fChanged = true;
				if (countEntries == ids.Length)
				{
					fChanged = false;
					for (int i = 0; i < countEntries; ++i)
					{
						int id = m_fdoCache.GetVectorItem(m_sense.Hvo, senseFlid, i);
						if (id != ids[i])
						{
							fChanged = true;
							break;
						}
					}
				}
				if (fChanged)
				{
					m_fdoCache.BeginUndoTask(LexEdStrings.ksUndoSetRevEntries,
						LexEdStrings.ksRedoSetRevEntries);
					m_fdoCache.ReplaceReferenceProperty(m_sense.Hvo, senseFlid, 0, countEntries, ref ids);
					m_fdoCache.EndUndoTask();
					// Also recompute and issue a PropChanged for the virtual property which may be displaying these
					// senses as a string in bulk edit.
					IVwVirtualHandler vh = m_fdoCache.VwCacheDaAccessor.GetVirtualHandlerName("LexSense",
						LexSenseReversalEntriesTextHandler.StandardFieldName);
					if (vh != null)
					{
						foreach (int ws in writingSystemsModified)
						{
							vh.Load(m_sense.Hvo, vh.Tag, ws, m_fdoCache.VwCacheDaAccessor);
							m_fdoCache.PropChanged(m_sense.Hvo, vh.Tag, ws, 0, 0);
						}
					}
				}
				return hvoReal;
			}

			/// <summary>
			/// Get the reversal index entry form(s), and check whether these are empty (link is
			/// being deleted) or the same as before (link is unchanged).  In either of these
			/// two cases, do what is needed and return true.  Otherwise return false (linked
			/// entry must be found or created).
			/// </summary>
			/// <param name="currentEntries"></param>
			/// <param name="hvoIndex"></param>
			/// <param name="wsIndex"></param>
			/// <param name="irieSense"></param>
			/// <param name="hvoEntry"></param>
			/// <param name="rgsFromDummy"></param>
			/// <returns></returns>
			private bool GetReversalFormsAndCheckExisting(List<int> currentEntries, int hvoIndex,
				int wsIndex, int irieSense, int hvoEntry, List<string> rgsFromDummy)
			{
				string fromDummyCache = null;
				ITsString tss = m_silCache.get_MultiStringAlt(hvoEntry,
					(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm, wsIndex);
				if (tss != null)
					fromDummyCache = tss.Text;
				if (fromDummyCache != null)
					ReversalIndexEntry.GetFormList(fromDummyCache, rgsFromDummy);
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
					IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(m_fdoCache, hvoEntry);
					if (rgsFromDummy[rgsFromDummy.Count - 1] == rie.ReversalForm.GetAlternative(wsIndex))
					{
						// Check that all parents exist as specified.  If so, then the user didn't change
						// this entry link.
						bool fSame = true;
						for (int i = rgsFromDummy.Count - 2; i >= 0; --i)
						{
							if (rie.OwningFlid == (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidSubentries)
							{
								rie = ReversalIndexEntry.CreateFromDBObject(m_fdoCache, rie.OwnerHVO);
								if (rgsFromDummy[i] == rie.ReversalForm.GetAlternative(wsIndex))
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
			/// The main set of reversal entries has changed, so update the display.
			/// </summary>
			internal void ResetEntries()
			{
				LoadDummyCache(true);
				// I don't know how the rootbox can become null, but LTB-543, LTB-700, and
				// LTB-876 all demonstrate that it can.  So try to create a new one if we can.
				try
				{
					if (m_rootb == null)
					{
						if (FwApp.App != null && FwApp.App.MainWindows.Count > 0 &&
							m_fdoCache != null && !m_fdoCache.IsDisposed && m_hvoObj != 0)
						{
							MakeRoot();
						}
					}
				}
				catch
				{
					m_rootb = null;
				}
				if (m_rootb != null)
					m_rootb.Reconstruct();
			}

			public override void MakeRoot()
			{
				CheckDisposed();

				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;

				// A crude way of making sure the property we want is loaded into the cache.
				m_sense = LexSense.CreateFromDBObject(m_fdoCache, m_hvoObj);
				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				if (m_vwCache == null)
				{
					m_vwCache = VwCacheDaClass.Create();
					m_silCache = (ISilDataAccess)m_vwCache;
					m_silCache.WritingSystemFactory = m_fdoCache.LanguageWritingSystemFactoryAccessor;
				}

				LoadDummyCache(false);

				// And maybe this too, at least by default?
				m_rootb.DataAccess = m_silCache;
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

					// m_vwCache is a 'sandbox' cache, not the real one in m_fdoCache,
					// so we can wipe it out with impunity,
					// since we re-fill it with all it needs in the rest of this method.
					m_vwCache.ClearAllData();
					m_usedIndices.Clear();
				}

				// Display the reversal indexes for the current set of analysis writing systems.
				List<IReversalIndexEntry> entries = new List<IReversalIndexEntry>();
				foreach (IReversalIndexEntry ide in m_sense.ReversalEntriesRC)
					entries.Add(ide);

				foreach (ILgWritingSystem ws in m_fdoCache.LangProject.CurAnalysisWssRS)
				{
					IReversalIndex idx = null;
					foreach (IReversalIndex idxInner in m_fdoCache.LangProject.LexDbOA.ReversalIndexesOC)
					{
						if (idxInner.WritingSystemRAHvo == ws.Hvo)
						{
							idx = idxInner;
							break;
						}
					}
					if (idx == null)
						continue;	// User must explicitly request another ReversalIndex (LT-4480).

					m_usedIndices.Add(idx);
					// Cache the WS for the index.
					m_vwCache.CacheIntProp(idx.Hvo,
						(int)ReversalIndex.ReversalIndexTags.kflidWritingSystem,
						idx.WritingSystemRAHvo);
					// Cache the WS abbreviation in the dummy cache.
					m_vwCache.CacheStringProp(ws.Hvo,
						(int)LgWritingSystem.LgWritingSystemTags.kflidAbbr,
						LgWritingSystem.UserAbbr(m_fdoCache, ws.Hvo));

					// Cache entries used by the sense for idx.
					// Cache the vector of IDs for referenced reversal entries.
					// They are actually stored in one flid in the database,
					// but we need to split them up and store them in dummy flids.
					// We will need one dummy flid for each reversal index.
					// As the user adds new entries, the dummy ID data member server will keep
					// decrementing its count. The cache may end up with any number of IDs that
					// are less than m_dummyEntryId.
					List<int> entryIds = idx.EntriesForSense(entries);
					// Cache a dummy string for each WS.
					ITsString tssEmpty = m_tsf.MakeString("", idx.WritingSystemRAHvo);
					m_vwCache.CacheStringAlt(m_dummyId, (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm,
						idx.WritingSystemRAHvo, tssEmpty);
					entryIds.Add(m_dummyId--);
					// Cache one or more entry IDs for each index.
					// The last one is a dummy entry, which allows the user to type new ones.
					m_vwCache.CacheVecProp(idx.Hvo, kFlidEntries, entryIds.ToArray(), entryIds.Count);
				}

				// Cache the reversal index IDs in a dummy flid of the sense.
				int[] rIds = new int[m_usedIndices.Count];
				for (int i = 0; i < m_usedIndices.Count; ++i)
				{
					IReversalIndex idx = m_usedIndices[i];
					rIds[i] = idx.Hvo;
				}
				m_vwCache.CacheVecProp(m_sense.Hvo, kFlidIndices, rIds, rIds.Length);

				// Cache the strings for each entry in the vector.
				Set<NamedWritingSystem> rgAllDbWs = Cache.LangProject.GetDbNamedWritingSystems();
				foreach (IReversalIndexEntry ent in m_sense.ReversalEntriesRC)
				{
					foreach (NamedWritingSystem nws in rgAllDbWs)
					{
						// It's a pity that MultiUnicodeAccessor lacks an iterator that returns
						// both the string and the ws.
						string form = ent.ReversalForm.GetAlternative(nws.Hvo);
						// If the entry is actually a subentry, display the form(s) of its
						// parent(s) as well, separating levels by a colon.  See LT-4665.
						if (ent.OwningFlid != (int)ReversalIndex.ReversalIndexTags.kflidEntries)
						{
							StringBuilder bldr = new StringBuilder(form);
							for (IReversalIndexEntry rie = ent.OwningEntry; rie != null; rie = rie.OwningEntry)
							{
								form = rie.ReversalForm.GetAlternative(nws.Hvo);
								if (form == null)
									form = String.Empty;
								bldr.Insert(0, ": ");
								bldr.Insert(0, form);
							}
							form = bldr.ToString();
						}
						if (form != null)
						{
							ITsString tss = m_tsf.MakeString(form, nws.Hvo);
							m_vwCache.CacheStringAlt(ent.Hvo,
								(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm,
								nws.Hvo, tss);
						}
					}
				}
			}

			protected override void CallOnTyping(IVwGraphics vg, string str, int cchBackspace, int cchDelForward, char chFirst, Rectangle rcSrcRoot, Rectangle rcDstRoot)
			{
				base.CallOnTyping(vg, str, cchBackspace, cchDelForward, chFirst, rcSrcRoot, rcDstRoot);
				Cache.EnableUndo = false;	// Things have changed in a way we can't Undo.
			}

			public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
			{
				CheckDisposed();

				if (vwselNew == null)
					return;

				base.SelectionChanged(rootb, vwselNew);

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

				int count = m_silCache.get_VecSize(hvoIndex, kFlidEntries);
				int lastEntryHvo = m_silCache.get_VecItem(hvoIndex, kFlidEntries, count - 1);

				//string oldForm = m_silCache.get_UnicodeProp(m_hvoOldSelection, (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidForm);
				string oldForm = null;
				int wsIndex = m_silCache.get_IntProp(hvoIndex, (int)ReversalIndex.ReversalIndexTags.kflidWritingSystem);
				ITsString tssEntry = m_silCache.get_MultiStringAlt(m_hvoOldSelection,
					(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm, wsIndex);
				if (tssEntry != null)
					oldForm = tssEntry.Text;
				if (m_hvoOldSelection != 0
					&& hvoObj != m_hvoOldSelection
					&& (oldForm == null || oldForm.Length  == 0))
				{
					// Remove the old string from the dummy cache, since its length is 0.
					for (int i = 0; i < count; ++i)
					{
						if (m_hvoOldSelection == m_silCache.get_VecItem(hvoIndex, kFlidEntries, i))
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
				count = m_silCache.get_VecSize(hvoIndex, kFlidEntries);
				// Assign a new dummy ID.
				m_dummyId--;
				// Insert it at the end of the list.
				m_vwCache.CacheReplace(hvoIndex, kFlidEntries, count, count, new int[] {m_dummyId}, 1);
				Cache.EnableUndo = false;	// Things have changed in a way we can't Undo.
				// Set its 'form' to be an empty string in the appropriate writing system.
				ITsTextProps props = tss.get_PropertiesAt(0);
				int nVar;
				ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				//m_vwCache.CacheUnicodeProp(m_dummyId, (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidForm, String.Empty, 0);
				//m_vwCache.CacheIntProp(m_dummyId, (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidWritingSystem, ws);
				ITsString tssEmpty = m_tsf.MakeString("", ws);
				m_vwCache.CacheStringAlt(m_dummyId, (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm,
					ws, tssEmpty);
				// Refresh
				m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					hvoIndex, kFlidEntries, count, 1, 0);

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
				m_vwCache.CacheReplace(hvoIndex, kFlidEntries, index, index + 1, new int[0], 0);
				m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					hvoIndex, kFlidEntries, index, 0, 1);
				Cache.EnableUndo = false;	// Things have changed in a way we can't Undo.
			}
		}

		#endregion RootSite class

		#region View constructor class

		public class ReversalIndexEntryVc : VwBaseVc
		{
			private List<IReversalIndex> m_usedIndices;
			ITsTextProps m_ttpLabel; // Props to use for ws name labels.
			FdoCache m_cache;
			ITsStrFactory m_tsf;

			public ReversalIndexEntryVc(List<IReversalIndex> usedIndices, FdoCache cache)
			{
				m_cache = cache;
				m_usedIndices = usedIndices;
				m_ttpLabel = LgWritingSystem.AbbreviationTextProperties;
				m_tsf = TsStrFactoryClass.Create();
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
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				if (disposing)
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

				base.Dispose(disposing);
			}

			#endregion IDisposable override

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
						ISilDataAccess silDaCache = vwenv.DataAccess;
						IVwCacheDa vwCache = silDaCache as IVwCacheDa;
						foreach (IReversalIndex idx in m_usedIndices)
						{
							vwenv.get_StringWidth(silDaCache.get_StringProp(idx.WritingSystemRAHvo, (int)LgWritingSystem.LgWritingSystemTags.kflidAbbr),
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
						foreach (ReversalIndex idx in m_usedIndices)
						{
							if (idx.Hvo == hvo)
							{
								wsHvo = idx.WritingSystemRAHvo;
								break;
							}
						}
						Debug.Assert(wsHvo > 0, "Could not find writing system.");

						int wsOldDefault = this.DefaultWs;
						this.DefaultWs = wsHvo;

						// Cell 1 shows the ws abbreviation.
						vwenv.OpenTableCell(1,1);
						vwenv.Props = m_ttpLabel;
						vwenv.AddObj(wsHvo, this, ReversalIndexEntrySliceView.kFragWsAbbr);
						vwenv.CloseTableCell();

						// Second cell has the contents for the reversal entries.
						vwenv.OpenTableCell(1,1);
						// This displays the field flush right for RTL data, but gets arrow keys to
						// behave reasonably.  See comments on LT-5287.
						IWritingSystem lgws = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(this.DefaultWs);
						if (lgws != null && lgws.RightToLeft)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
								(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						}
						vwenv.OpenParagraph();
						// Do vector of entries in the second column.
						vwenv.AddObjVec(ReversalIndexEntrySliceView.kFlidEntries, this, ReversalIndexEntrySliceView.kFragEntries);
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();

						this.DefaultWs = wsOldDefault;
						break;
					}
					case ReversalIndexEntrySliceView.kFragEntryForm:
					{
						vwenv.AddStringAltMember((int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm,
							this.DefaultWs, this);
						int hvoCurrent = vwenv.CurrentObject();
						if (hvoCurrent > 0)
						{
							IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(m_cache, hvoCurrent);
							Debug.Assert(rie != null);
							int[] rgWs = m_cache.LangProject.GetReversalIndexWritingSystems(rie.Hvo, false);
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
							for (int i = 0; i < rgWs.Length; ++i)
							{
								int ws = rgWs[i];
								if (ws == this.DefaultWs)
									continue;
								string sForm = rie.ReversalForm.GetAlternative(ws);
								if (sForm != null && sForm.Length != 0)
								{
									if (cstr > 0)
										tisb.Append(", ");
									++cstr;
									string sWs = m_cache.GetMultiUnicodeAlt(ws,
										(int)LgWritingSystem.LgWritingSystemTags.kflidAbbr, wsAnal,
										"LgWritingSystem_Abbr");
									if (sWs != null && sWs.Length != 0)
									{
										ITsString tssWs = m_tsf.MakeStringWithPropsRgch(sWs, sWs.Length, m_ttpLabel);
										tisb.AppendTsString(tssWs);
										// We have to totally replace the properties set by m_ttpLabel.  The
										// simplest way is to create another ITsString with the simple base
										// property of only the default analysis writing system.
										if (ttpBase == null)
										{
											ITsPropsBldr tpbBase = TsPropsBldrClass.Create();
											tpbBase.SetIntPropValues((int)FwTextPropType.ktptWs,
												(int)FwTextPropVar.ktpvDefault, wsAnal);
											ttpBase = tpbBase.GetTextProps();
										}
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
						vwenv.AddString(da.get_StringProp(hvo, (int)LgWritingSystem.LgWritingSystemTags.kflidAbbr));
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
								wsHvo = idx.WritingSystemRAHvo;
								break;
							}
						}
						Debug.Assert(wsHvo > 0, "Could not find writing system.");
						int wsOldDefault = this.DefaultWs;
						this.DefaultWs = wsHvo;

						// hvo here is a reversal index.
						int countEntries = da.get_VecSize(hvo, ReversalIndexEntrySliceView.kFlidEntries);
						for (int j = 0; j < countEntries; ++j)
						{
							if (j != 0)
								vwenv.AddSeparatorBar();
							int entryHvo = da.get_VecItem(hvo, ReversalIndexEntrySliceView.kFlidEntries, j);
							vwenv.AddObj(entryHvo, this, ReversalIndexEntrySliceView.kFragEntryForm);
						}

						this.DefaultWs = wsOldDefault;
						break;
					}
				}
			}
		}

		#endregion View constructor class
	}
}
