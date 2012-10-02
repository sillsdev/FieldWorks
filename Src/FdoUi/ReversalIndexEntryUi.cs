using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// ReversalIndexEntryUi provides UI-specific methods for the ReversalIndexEntryUi class.
	/// </summary>
	public class ReversalIndexEntryUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a PartOfSpeech.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeUi, which is
		/// passed an obj anyway.
		/// </summary>
		/// <param name="obj"></param>
		public ReversalIndexEntryUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IReversalIndexEntry);
		}

		internal ReversalIndexEntryUi() {}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeReversalEntry;
			wp.m_label = FdoUiStrings.ksEntries;

			IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(m_cache, Object.Hvo);
			int wsIndex = rie.ReversalIndex.WritingSystemRAHvo;
			foreach (int rieInnerHvo in rie.ReversalIndex.AllEntries)
			{
				IReversalIndexEntry rieInner = ReversalIndexEntry.CreateFromDBObject(m_cache, rieInnerHvo);
				if (rieInner.Hvo != Object.Hvo)
				{
					mergeCandidates.Add(
						new DummyCmObject(
						rieInner.Hvo,
						rieInner.ShortName,
						wsIndex));
				}
			}
			guiControl = "MergeReversalEntryList";
			helpTopic = "khtpMergeReversalEntry";
			return new DummyCmObject(m_hvo, rie.ShortName, wsIndex);
		}

		/// <summary>
		/// Merge the underling objects. This method handles the transaction, then delegates
		/// the actual merge to MergeObject. If the flag is true, we merge
		/// strings and owned atomic objects; otherwise, we don't change any that aren't null
		/// to begin with.
		/// </summary>
		/// <param name="fLoseNoTextData"></param>
		protected override void ReallyMergeUnderlyingObject(int survivorHvo, bool fLoseNoTextData)
		{
			ICmObject survivor = CmObject.CreateFromDBObject(m_cache, survivorHvo);

			base.ReallyMergeUnderlyingObject(survivorHvo, fLoseNoTextData);

			// Update virtual prop on survivor, so it is displayed properly.
			// Reloading the VH and calling PropChanged fixes LT-6274.
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache, "ReversalIndexEntry", "ReferringSenses");
			int flid = vh.Tag;
			vh.Load(survivor.Hvo, flid, 0, m_cache.VwCacheDaAccessor);
			m_cache.MainCacheAccessor.PropChanged(
				null,
				(int)PropChangeType.kpctNotifyAll,
				survivor.Hvo,
				flid,
				0,
				0,
				0);
		}

		protected override void ReallyDeleteUnderlyingObject()
		{
			IReversalIndexEntry rei = Object as IReversalIndexEntry;
			int hvoGoner = rei.Hvo;
			int hvoIndex = rei.ReversalIndex.Hvo;
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache, "ReversalIndex", "AllEntries");
			int flid = vh.Tag;

			base.ReallyDeleteUnderlyingObject();

			// Remove goner from the cache.
			vh.Load(hvoIndex, flid, 0, m_cache.VwCacheDaAccessor);

			m_cache.MainCacheAccessor.PropChanged(
				null,
				(int)PropChangeType.kpctNotifyAll,
				hvoIndex,
				flid,
				0,
				0,
				1);
		}
	}
}
