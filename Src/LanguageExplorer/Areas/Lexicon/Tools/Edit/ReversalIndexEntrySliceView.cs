// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using System.Linq;
using SIL.Extensions;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal class ReversalIndexEntrySliceView : RootSiteControl
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
		/// <summary />
		public const int kFlidIndices = 5001; // "owner" will be the sense, and its ID.
		/// <summary />
		public const int kFlidEntries = 5002; // A dummy flid for the reversal index, which holds the entries for the main sense.
		//Fake Ids.
		/// <summary />
		public const int kDummyEntry = -10000;
		// View frags.
		/// <summary />
		public const int kFragMainObject = 1;
		/// <summary />
		public const int kFragIndices = 2;
		/// <summary />
		public const int kFragWsAbbr = 3;
		/// <summary />
		public const int kFragEntryForm = 4;
		/// <summary />
		public const int kFragIndexMain = 5;
		/// <summary />
		public const int kFragEntries = 6;

		#endregion Constants

		#region Data members

		private IContainer components;
		/// <summary />
		protected int m_dummyId;
		/// <summary>
		/// A decorated ISilDataAccess for use with the Views code.
		/// </summary>
		protected ReversalEntryDataAccess m_sdaRev;
		/// <summary />
		protected ReversalIndexEntryVc m_vc;
		/// <summary />
		protected int m_heightView;
		/// <summary />
		protected int m_hvoOldSelection;
		protected int m_hvoObj;
		/// <summary />
		protected ILexSense m_sense;
		/// <summary />
		protected List<IReversalIndex> m_usedIndices = new List<IReversalIndex>();

		#endregion // Data members

		/// <summary />
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
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
				m_vc?.Dispose();
				m_usedIndices?.Clear();
				m_sdaRev?.ClearAllData();
			}

			m_usedIndices = null;
			m_sense = null;
			m_vc = null;
			m_sdaRev = null;
			m_usedIndices = null;
		}

		private void ReversalIndexEntrySliceView_RightMouseClickedEvent(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			e.EventHandled = true;
			e.Selection.Install();
			var menu = components.ContextMenuStrip("contextMenu");
			var sMenuText = LanguageExplorerResources.ksShowInReversalIndex;
			var item = new ToolStripMenuItem(sMenuText);
			item.Click += OnShowInReversalIndex;
			menu.Items.Add(item);
			menu.Show(this, e.MouseLocation);
		}

		/// <summary />
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button == MouseButtons.Left && (ModifierKeys & Keys.Control) == Keys.Control)
			{
				// Control-click: go straight to the indicated object.
				OnShowInReversalIndex(this, new EventArgs());
			}
		}

		private void OnShowInReversalIndex(object sender, EventArgs e)
		{
			var tsi = new TextSelInfo(m_rootb);
			var hvo = tsi.HvoAnchor;
			if (hvo == 0)
			{
				return;
			}
			// If the entry is a subentry, we have to find the main entry owned directly by
			// the reversal index.  Otherwise, the jump would go to the first entry in the
			// index.
			// If it's a new reversal that hasn't been converted to reality yet, convert it!
			// (See FWR-809.)
			if (!Cache.ServiceLocator.IsValidObjectId(hvo))
			{
				try
				{
					var hvoReal = ConvertDummiesToReal(hvo);
					hvo = hvoReal;
					if (hvo == 0)
					{
						return;
					}
				}
				catch
				{
					return;
				}
			}
			LinkHandler.JumpToTool(Publisher, new FwLinkArgs(AreaServices.ReversalEditCompleteMachineName, ((IReversalIndexEntry)Cache.ServiceLocator.GetObject(hvo)).MainEntry.Guid));
		}

		/// <summary>
		/// Connect to the real data, when the control has lost focus.
		/// </summary>
		protected override void OnLostFocus(EventArgs e)
		{
			WaitCursor wc = null;
			var window = FindForm();
			if (window != null)
			{
				wc = new WaitCursor(window);
			}
			try
			{
				ConvertDummiesToReal(0);
				base.OnLostFocus(e);
			}
			finally
			{
				wc?.Dispose();
			}
		}

		private int ConvertDummiesToReal(int hvoDummy)
		{
			// This somehow seems to happen quite often although it should not (LT-11162),
			// at least, not while we have any changes that really need to be saved, since we
			// should have lost focus and saved before doing anything that would cause a regenerate.
			// But let's not crash.
			var extensions = Cache.ActionHandlerAccessor as IActionHandlerExtensions;
			if ((extensions != null && !extensions.CanStartUow) || !m_sense.IsValidObject) //users might quickly realize a mistake and delete the sense before we have converted our dummy.
			{
				return 0;
			}

			var currentEntries = new List<int>();
			var countIndices = m_sdaRev.get_VecSize(m_sense.Hvo, kFlidIndices);
			var hvoReal = 0;
			var writingSystemsModified = new HashSet<int>();
			for (var i = 0; i < countIndices; ++i)
			{
				var hvoIndex = m_sdaRev.get_VecItem(m_sense.Hvo, kFlidIndices, i);
				var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().GetObject(hvoIndex);
				writingSystemsModified.Add(Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(revIndex.WritingSystem));
				var countRealEntries = m_sdaRev.get_VecSize(hvoIndex, kFlidEntries) - 1; // Skip the dummy entry at the end.
				// Go through it from the far end, since we may be deleting empty items.
				for (var j = countRealEntries - 1; j >= 0; --j)
				{
					var hvoEntry = m_sdaRev.get_VecItem(hvoIndex, kFlidEntries, j);
					// If hvoEntry is greater than 0, then it started as a real entry.
					// If hvoEntry is less than 0, it is clearly a new one.
					// The text may have changed for a real one, so check to see if is the same.
					// If it is the same, then just add it to the currentEntries array.
					// If it is different, or if it is a new one, then we need to
					// see if it already exists in the index.
					// If it exists, then add it to the currentEntries array.
					// If it does not exist, we have to create it, and add it to the currentEntries array.
					var rgsFromDummy = new List<string>();
					if (GetReversalFormsAndCheckExisting(currentEntries, hvoIndex, Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(revIndex.WritingSystem), j, hvoEntry, rgsFromDummy))
					{
						continue;
					}
					// At this point, we need to find or create one or more entries. The hvo returned may be the hvo of a subentry.
					var hvo = FindOrCreateReversalEntry(revIndex, rgsFromDummy, Cache);
					currentEntries.Add(hvo);
					if (hvoEntry == hvoDummy)
					{
						hvoReal = hvo;
					}
				}
			}
			// Reset the sense's ref. property to all the ids in the currentEntries array.
			currentEntries.Reverse();
			var ids = currentEntries.ToArray();
			var removedEntries = new List<int>();
			if (!Cache.ServiceLocator.GetInstance<ICmObjectRepository>().IsValidObjectId(m_sense.Hvo))
			{
				return 0; // our object has been deleted while we weren't looking!
			}
			var countEntries = Cache.DomainDataByFlid.get_VecSize(m_sense.Hvo, Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalIndexEntryBackRefs);
			// Check the current state and don't save (or create an Undo stack item) if
			// nothing has changed.
			var fChanged = ids.Length != countEntries;
			for (var i = 0; i < countEntries; ++i)
			{
				var id = Cache.DomainDataByFlid.get_VecItem(m_sense.Hvo, Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalIndexEntryBackRefs, i);
				if (ids.IndexOf(id) != i)
				{
					fChanged = true;
					if (!ids.Contains(id))
					{
						removedEntries.Add(id);
					}
				}
			}
			if (fChanged)
			{
				// Add the sense to the reversal index entry
				Cache.DomainDataByFlid.BeginUndoTask(LanguageExplorerResources.ksUndoSetRevEntries, LanguageExplorerResources.ksRedoSetRevEntries);
				foreach (var id in ids)
				{
					var rie = Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(id);
					if (!rie.SensesRS.Contains(m_sense))
					{
						rie.SensesRS.Add(m_sense);
					}
				}
				Cache.DomainDataByFlid.EndUndoTask();
				if (removedEntries.Count > 0)
				{
					// Remove the sense from the reversal index entry and delete the entry if the SensesRS property is empty
					Cache.DomainDataByFlid.BeginUndoTask(LanguageExplorerResources.ksUndoDeleteRevFromSense, LanguageExplorerResources.ksRedoDeleteRevFromSense);
					foreach (var entry in removedEntries)
					{
						var rie = Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(entry);
						rie.SensesRS.Remove(m_sense);
						if (rie.SensesRS.Count == 0 && rie.SubentriesOS.Count == 0)
						{
							Cache.DomainDataByFlid.DeleteObj(rie.Hvo);
						}
						Cache.DomainDataByFlid.EndUndoTask();
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
		private bool GetReversalFormsAndCheckExisting(List<int> currentEntries, int hvoIndex, int wsIndex, int irieSense, int hvoEntry, List<string> rgsFromDummy)
		{
			string fromDummyCache = null;
			var tss = m_sdaRev.get_MultiStringAlt(hvoEntry, ReversalIndexEntryTags.kflidReversalForm, wsIndex);
			if (tss != null)
			{
				fromDummyCache = tss.Text;
			}

			if (fromDummyCache != null)
			{
				GetFormList(fromDummyCache, rgsFromDummy);
			}
			if (rgsFromDummy.Count == 0)
			{
				// The entry at 'irieSense' is being deleted, so
				// remove it from the dummy cache.
				RemoveFromDummyCache(hvoIndex, irieSense);
				//fNeedPropChange = true;
				return true;
			}

			if (hvoEntry <= 0)
			{
				return false;
			}
			var rie = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(hvoEntry);
			if (rgsFromDummy[rgsFromDummy.Count - 1] != rie.ReversalForm.get_String(wsIndex).Text)
			{
				return false;
			}
			// Check that all parents exist as specified.  If so, then the user didn't change
			// this entry link.
			var fSame = true;
			for (var i = rgsFromDummy.Count - 2; i >= 0; --i)
			{
				if (rie.OwningFlid == ReversalIndexEntryTags.kflidSubentries)
				{
					rie = rie.Owner as IReversalIndexEntry;
					if (rgsFromDummy[i] == rie.ReversalForm.get_String(wsIndex).Text)
					{
						continue;
					}
				}
				fSame = false;
				break;
			}
			if (fSame)
			{
				// Add hvoEntry to the currentEntries array.
				currentEntries.Add(hvoEntry);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Find the reversal index entry given by rgsForms, or if it doesn't exist, create
		/// it.  In either case, return its hvo.
		/// </summary>
		public static int FindOrCreateReversalEntry(IReversalIndex revIndex, List<string> rgsForms, LcmCache cache)
		{
			var rgrieMatching = new List<List<IReversalIndexEntry>>(rgsForms.Count);
			// This could be SLOOOOOOOOOOW!  But I don't see a better way of doing it...
			for (var i = 0; i < rgsForms.Count; ++i)
			{
				rgrieMatching.Add(new List<IReversalIndexEntry>());
			}
			var wsIndex = cache.ServiceLocator.WritingSystemManager.GetWsFromStr(revIndex.WritingSystem);
			foreach (var rie in revIndex.AllEntries)
			{
				var form = rie.ReversalForm.get_String(wsIndex).Text;
				var idx = rgsForms.IndexOf(form);
				if (idx >= 0)
				{
					rgrieMatching[idx].Add(rie);
				}
			}
			var rghvoOwners = new List<int>(rgsForms.Count);
			rghvoOwners.Add(revIndex.Hvo);
			// The next two variables record the best partial match, if any.
			var maxLevel = 0;
			var maxOwner = revIndex.Hvo;
			var hvo = FindMatchingReversalEntry(rgsForms, rghvoOwners, rgrieMatching, 0, ref maxLevel, ref maxOwner);
			if (hvo != 0)
			{
				return hvo;
			}
			cache.DomainDataByFlid.BeginUndoTask(LanguageExplorerResources.ksCreateReversal, LanguageExplorerResources.ksRecreateReversal);
			// Create whatever we need to since we didn't find a full match.
			var owner = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(maxOwner);
			Debug.Assert(maxLevel < rgsForms.Count);
			var fact = cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			for (var i = maxLevel; i < rgsForms.Count; ++i)
			{
				var rie = fact.Create();
				if (owner is IReversalIndex)
				{
					(owner as IReversalIndex).EntriesOC.Add(rie);
				}
				else
				{
					Debug.Assert(owner is IReversalIndexEntry);
					(owner as IReversalIndexEntry).SubentriesOS.Add(rie);
				}
				rie.ReversalForm.set_String(wsIndex, rgsForms[i]);
				owner = rie;
				hvo = rie.Hvo;
			}
			Debug.Assert(hvo != 0);
			cache.DomainDataByFlid.EndUndoTask();
			return hvo;
		}

		private static int FindMatchingReversalEntry(List<string> rgsForms,
			List<int> rghvoOwners, List<List<IReversalIndexEntry>> rgrieMatching,
			int idxForms, ref int maxLevel, ref int maxOwner)
		{
			foreach (var rie in rgrieMatching[idxForms])
			{
				Debug.Assert(rie.ReversalIndex.Hvo == rghvoOwners[0]);
				if (rie.ReversalIndex.Hvo != rghvoOwners[0])
				{
					continue;
				}

				if (rie.Owner.Hvo != rghvoOwners[idxForms])
				{
					continue;
				}
				var level = idxForms + 1;
				if (level < rgsForms.Count)
				{
					if (level > maxLevel)
					{
						maxLevel = level;
						maxOwner = rie.Hvo;
					}
					// we have a match at this level: recursively check the next level.
					rghvoOwners.Add(rie.Hvo);
					var hvo = FindMatchingReversalEntry(rgsForms, rghvoOwners, rgrieMatching, level, ref maxLevel, ref maxOwner);
					if (hvo != 0)
					{
						return hvo;
					}
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
			var longName = longNameIn.Trim();
			forms.Clear();
			// allow the user to indicate subentries by separating words by ':'.
			// See LT-4665.
			var rgsDummy = longName.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < rgsDummy.Length; ++i)
			{
				rgsDummy[i] = rgsDummy[i].Trim();
				if (!string.IsNullOrEmpty(rgsDummy[i]))
				{
					forms.Add(rgsDummy[i]);
				}
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
				m_sdaRev?.ClearAllData();
				m_usedIndices?.Clear();
			}
		}

		/// <summary />
		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			// A crude way of making sure the property we want is loaded into the cache.
			m_sense = (ILexSense)m_cache.ServiceLocator.GetObject(m_hvoObj);

			base.MakeRoot();

			if (m_sdaRev == null)
			{
				m_sdaRev = new ReversalEntryDataAccess(m_cache.DomainDataByFlid as ISilDataAccessManaged) { TsStrFactory = TsStringUtils.TsStrFactory };
			}

			LoadDummyCache(false);

			// And maybe this too, at least by default?
			m_rootb.DataAccess = m_sdaRev;
			m_vc = new ReversalIndexEntryVc(m_usedIndices, m_cache);

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
			var entries = new List<IReversalIndexEntry>();
			foreach (var ide in m_sense.ReferringReversalIndexEntries)
			{
				entries.Add(ide);
			}

			foreach (var ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
			{
				IReversalIndex idx = null;
				foreach (var idxInner in m_cache.LanguageProject.LexDbOA.ReversalIndexesOC)
				{
					if (idxInner.WritingSystem == ws.Id)
					{
						idx = idxInner;
						break;
					}
				}
				if (idx == null)
				{
					continue;   // User must explicitly request another IReversalIndex (LT-4480).
				}

				m_usedIndices.Add(idx);
				// Cache the WS for the index.
				m_sdaRev.CacheUnicodeProp(idx.Hvo, ReversalIndexTags.kflidWritingSystem, idx.WritingSystem, idx.WritingSystem.Length);
				// Cache the WS abbreviation in the dummy cache.
				m_sdaRev.CacheStringProp(ws.Handle, ReversalEntryDataAccess.kflidWsAbbr, TsStringUtils.MakeString(ws.Abbreviation, m_cache.DefaultUserWs));

				// Cache entries used by the sense for idx.
				// Cache the vector of IDs for referenced reversal entries.
				// They are actually stored in one flid in the database,
				// but we need to split them up and store them in dummy flids.
				// We will need one dummy flid for each reversal index.
				// As the user adds new entries, the dummy ID data member server will keep
				// decrementing its count. The cache may end up with any number of IDs that
				// are less than m_dummyEntryId.
				var entryIds = new List<int>();
				foreach (var rie in idx.EntriesForSense(entries))
				{
					entryIds.Add(rie.Hvo);
				}
				var wsHandle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
				// Cache a dummy string for each WS.
				var tssEmpty = TsStringUtils.EmptyString(wsHandle);
				m_sdaRev.CacheStringAlt(m_dummyId, ReversalIndexEntryTags.kflidReversalForm, wsHandle, tssEmpty);
				entryIds.Add(m_dummyId--);
				// Cache one or more entry IDs for each index.
				// The last one is a dummy entry, which allows the user to type new ones.
				m_sdaRev.CacheVecProp(idx.Hvo, kFlidEntries, entryIds.ToArray(), entryIds.Count);
			}

			// Cache the reversal index IDs in a dummy flid of the sense.
			var rIds = new int[m_usedIndices.Count];
			for (var i = 0; i < m_usedIndices.Count; ++i)
			{
				var idx = m_usedIndices[i];
				rIds[i] = idx.Hvo;
			}
			m_sdaRev.CacheVecProp(m_sense.Hvo, kFlidIndices, rIds, rIds.Length);

			// Cache the strings for each entry in the vector.
			foreach (var ent in m_sense.ReferringReversalIndexEntries)
			{
				var cform = ent.ReversalForm.StringCount;
				for (var i = 0; i < cform; ++i)
				{
					int ws;
					var form = ent.ReversalForm.GetStringFromIndex(i, out ws).Text;
					// If the entry is actually a subentry, display the form(s) of its
					// parent(s) as well, separating levels by a colon.  See LT-4665.
					if (ent.OwningFlid != ReversalIndexTags.kflidEntries)
					{
						var bldr = new StringBuilder(form);
						for (var rie = ent.OwningEntry; rie != null; rie = rie.OwningEntry)
						{
							form = rie.ReversalForm.get_String(ws).Text ?? string.Empty;
							bldr.Insert(0, ": ");
							bldr.Insert(0, form);
						}
						form = bldr.ToString();
					}
					if (form != null)
					{
						var tss = TsStringUtils.MakeString(form, ws);
						m_sdaRev.CacheStringAlt(ent.Hvo, ReversalIndexEntryTags.kflidReversalForm, ws, tss);
					}
				}
			}
		}

		/// <summary />
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			if (vwselNew == null)
			{
				return;
			}

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

			var count = m_sdaRev.get_VecSize(hvoIndex, kFlidEntries);
			var lastEntryHvo = m_sdaRev.get_VecItem(hvoIndex, kFlidEntries, count - 1);
			string oldForm = null;
			var wsString = m_sdaRev.get_UnicodeProp(hvoIndex, ReversalIndexTags.kflidWritingSystem);
			var wsIndex = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(wsString);
			var tssEntry = m_sdaRev.get_MultiStringAlt(m_hvoOldSelection, ReversalIndexEntryTags.kflidReversalForm, wsIndex);
			if (tssEntry != null)
				oldForm = tssEntry.Text;
			if (m_hvoOldSelection != 0 && hvoObj != m_hvoOldSelection && string.IsNullOrEmpty(oldForm))
			{
				// Remove the old string from the dummy cache, since its length is 0.
				for (var i = 0; i < count; ++i)
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
			m_sdaRev.CacheReplace(hvoIndex, kFlidEntries, count, count, new[] { m_dummyId }, 1);
			// Set its 'form' to be an empty string in the appropriate writing system.
			var props = tss.get_PropertiesAt(0);
			int nVar;
			ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			var tssEmpty = TsStringUtils.EmptyString(ws);
			m_sdaRev.CacheStringAlt(m_dummyId, ReversalIndexEntryTags.kflidReversalForm, ws, tssEmpty);
			// Refresh
			RootBox.PropChanged(hvoIndex, kFlidEntries, count, 1, 0);

			// Reset selection.
			var rgvsli = new SelLevInfo[2];
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
			if (m_rootb == null)
			{
				return;
			}
			var hNew = m_rootb.Height;
			if (m_heightView == hNew)
			{
				return;
			}

			ViewSizeChanged?.Invoke(this, new FwViewSizeEventArgs(hNew, m_rootb.Width));
			m_heightView = hNew;
		}

		private void RemoveFromDummyCache(int hvoIndex, int index)
		{
			m_sdaRev.CacheReplace(hvoIndex, kFlidEntries, index, index + 1, new int[0], 0);
		}
	}
}