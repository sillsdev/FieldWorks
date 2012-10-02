// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoUndoActions.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO
{
	#region Class ClearInfoOnCommitUndoAction
	/// <summary>
	/// This class clears all info about its object when committed. It is used for clearing out
	/// info about dummy objects that can't be restored from the database if Undo reinstates them.
	/// Therefore we should not forget about them until we can no longer Undo.
	/// </summary>
	internal class ClearInfoOnCommitUndoAction : UndoActionBase
	{
		FdoCache m_cache;
		int m_hvo;
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		public ClearInfoOnCommitUndoAction(FdoCache cache, int hvo)
		{
			m_cache = cache;
			m_hvo = hvo;
		}
		#region Overrides of UndoActionBase

		/// <summary>
		///
		/// </summary>
		public override void Commit()
		{
			m_cache.VwCacheDaAccessor.ClearInfoAbout(m_hvo, VwClearInfoAction.kciaRemoveAllObjectInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Undo(bool fRefreshPending)
		{
			return true;
		}

		#endregion
	}
	#endregion

	#region Class CacheReplaceOneUndoAction
	/// <summary>
	/// Handle Undo of a change to a phony object in the cache, consisting of replacing
	/// a value with another at a specified index in a specified property.
	/// </summary>
	public class CacheReplaceOneUndoAction : UndoActionBase
	{
		FdoCache m_cache;
		ISilDataAccess m_sda;
		BaseVirtualHandler m_bvh;
		int m_hvo;
		int m_flid;
		int m_index;
		int m_oldValue;
		int[] m_newValues;
		int m_cvDel;

		/// <summary>
		/// Make one. Call DoIt to actually make the change.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="newValues"></param>
		public CacheReplaceOneUndoAction(FdoCache cache, int hvo, int flid, int ihvoMin, int ihvoLim, int[] newValues)
		{
			m_cache = cache;
			m_sda = cache.MainCacheAccessor;
			m_hvo = hvo;
			m_flid = flid;
			IVwVirtualHandler vh;
			if (m_cache.TryGetVirtualHandler(m_flid, out vh))
			{
				m_bvh = vh as BaseVirtualHandler;
			}
			m_index = ihvoMin;
			m_newValues = newValues;
			m_cvDel = ihvoLim - ihvoMin;
			Debug.Assert(m_cvDel >= 0 && m_cvDel <= 1, "Currently only support deleting one item at a time.");
			if (m_cvDel > 0)
				m_oldValue = m_sda.get_VecItem(hvo, flid, ihvoMin);
		}

		/// <summary>
		/// Make one, and do the action (with notification).
		/// </summary>
		public static void SetItUp(FdoCache cache, int hvo, int flid, int ihvoMin, int ihvoLim, int[] newValues)
		{
			CacheReplaceOneUndoAction action = new CacheReplaceOneUndoAction(cache, hvo, flid, ihvoMin, ihvoLim,newValues);
			action.DoIt();
		}

		/// <summary>
		/// Do the CacheReplace operation and DoNotify.
		/// </summary>
		public void DoIt()
		{
			DoIt(true);
		}

		/// <summary>
		/// Do CacheReplace operation
		/// </summary>
		/// <param name="fDoNotify">if true, issues PropChanged</param>
		public void DoIt(bool fDoNotify)
		{
			DoIt(m_newValues, m_cvDel, fDoNotify);
		}

		private void DoIt(int[] newValues, int cvDel)
		{
			DoIt(newValues, cvDel, true);
		}

		private void DoIt(int[] newValues, int cvDel, bool fDoNotify)
		{
			//int cItems = m_sda.get_VecSize(m_hvo, m_flid);
			//if (cItems == 0 && m_index > cItems && m_cache.MetaDataCacheAccessor.get_IsVirtual((uint)m_flid))
			//	return;	// probably a virtual property that has been cleared. assume we no longer care about it.
			if (m_bvh.IsPropInCache(m_sda, m_hvo, 0) && (m_index + cvDel) <= m_sda.get_VecSize(m_hvo, m_flid))
			{
				(m_sda as IVwCacheDa).CacheReplace(m_hvo, m_flid, m_index, m_index + cvDel, newValues, newValues.Length);
				// if we deleted from the end of the
				if (fDoNotify)
					DoNotify(newValues.Length, cvDel);
			}
			else
			{
				Debug.WriteLine(String.Format("Couldn't do CacheReplace for hvo {0} flid {1} (VecSize=={5}) for index {2} cvDel {3}, cNewValues {4}",
					m_hvo, m_flid, m_index, cvDel, newValues.Length,
					m_bvh.IsPropInCache(m_sda, m_hvo, 0) ? m_sda.get_VecSize(m_hvo, m_flid).ToString() : "not in cache"));
			}
		}

		void DoNotify(int cvIns, int cvDel)
		{
			// It's possible that in a single undo task we create a paragraph and segment it and convert the segments to real ones
			// (e.g., paste a paragraph in TE in segmented BT view). We Undo the creation of the segments, then Undo the creation
			// of the paragraph, and then come back and try to issue a PropChanged on our property of the deleted paragraph.
			// This can fail (e.g., TE-7904).
			if (!m_cache.IsValidObject(m_hvo))
				return;
			// note: we must use FdoCache.PropChanged rather than sda.PropChanged, so that we can make use of PropChangedHandling
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_hvo, m_flid, m_index, cvIns, cvDel);
		}

		#region Overrides of UndoActionBase

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool IsDataChange()
		{
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Redo(bool fRefreshPending)
		{
			DoIt();
			return true;
		}

		/// <summary>
		///
		/// </summary>
		public override bool SuppressNotification
		{
			set
			{
				if (value)
					(m_sda as IVwCacheDa).SuppressPropChanges();
				else
					(m_sda as IVwCacheDa).ResumePropChanges();
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Undo(bool fRefreshPending)
		{
			if (m_cvDel > 0)
				DoIt(new int[] { m_oldValue }, m_newValues.Length);
			else
				DoIt(new int[0], m_newValues.Length);
			return true;
		}

		#endregion
	}
	#endregion

	#region Class CacheObjPropUndoAction
	/// <summary>
	/// Handle Undo of a change to a phony object in the cache, consisting of replacing
	/// a value with another in a specified atomic property.
	/// </summary>
	public class CacheObjPropUndoAction : UndoActionBase
	{
		ISilDataAccess m_sda;
		int m_hvo;
		int m_flid;
		int m_newValue;
		int m_oldValue;

		/// <summary>
		/// Make one AND make the change to the cache.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="newValue"></param>
		public CacheObjPropUndoAction(ISilDataAccess sda, int hvo, int flid, int newValue)
		{
			m_sda = sda;
			m_hvo = hvo;
			m_flid = flid;
			m_newValue = newValue;
			m_oldValue = 0;
			// Since this is used for fake props, if it isn't already cached, it doesn't have an old value.
			if (sda.get_IsPropInCache(hvo, flid, (int)CellarModuleDefns.kcptReferenceAtom, 0))
				m_oldValue = sda.get_ObjectProp(hvo, flid);
			DoIt(m_newValue);
		}

		/// <summary>
		/// Make one, do the action, add it to the action handler; or if there is no action handler, just
		/// do it.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="newValue"></param>
		public static void SetItUp(FdoCache cache, int hvo, int flid, int newValue)
		{
			CacheObjPropUndoAction action = new CacheObjPropUndoAction(cache.MainCacheAccessor, hvo, flid, newValue);
			if (cache.ActionHandlerAccessor != null)
				cache.ActionHandlerAccessor.AddAction(action);
		}

		void DoIt(int val)
		{
			(m_sda as IVwCacheDa).CacheObjProp(m_hvo, m_flid, val);
			DoNotify();
		}

		void DoNotify()
		{
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvo, m_flid, 0, 1, 1);
		}

		#region Overrides of UndoActionBase
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool IsDataChange()
		{
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Redo(bool fRefreshPending)
		{
			DoIt(m_newValue);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		public override bool SuppressNotification
		{
			set
			{
				if (value)
					(m_sda as IVwCacheDa).SuppressPropChanges();
				else
					(m_sda as IVwCacheDa).ResumePropChanges();
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Undo(bool fRefreshPending)
		{
			DoIt(m_oldValue);
			return true;
		}

		#endregion
	}
	#endregion

	#region PropChangedInfo struct
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information about a PropChanged
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct PropChangedInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropChangedInfo"/> struct.
		/// </summary>
		/// <param name="hvo">ID of the object that has changed.</param>
		/// <param name="tag">The property (flid) that has changed.</param>
		/// <param name="ivMin">For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.</param>
		/// <param name="cvIns">For vectors, the number of items inserted.
		/// For atomic objects, 1 if an item was added.
		/// Otherwise (including basic properties), 0.</param>
		/// <param name="cvDel">For vectors, the number of items deleted.
		/// For atomic objects, 1 if an item was deleted.
		/// Otherwise (including basic properties), 0.</param>
		/// ------------------------------------------------------------------------------------
		public PropChangedInfo(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			this.hvo = hvo;
			this.tag = tag;
			this.ivMin = ivMin;
			this.cvIns = cvIns;
			this.cvDel = cvDel;
		}

		/// <summary>ID of the object that has changed.</summary>
		public int hvo;
		/// <summary>The property (flid) that has changed.</summary>
		public int tag;
		/// <summary>For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.</summary>
		public int ivMin;
		/// <summary>For vectors, the number of items inserted. For atomic objects, 1 if an
		/// item was added. Otherwise (including basic properties), 0.</summary>
		public int cvIns;
		/// <summary>For vectors, the number of items deleted. For atomic objects, 1 if an
		/// item was deleted. Otherwise (including basic properties), 0.</summary>
		public int cvDel;
	}
	#endregion

	#region PropChangedUndoAction class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo/Redo action for additional PropChanged calls. Can take a list of PropChanged
	/// parameters and fires them in the Undo/Redo case.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PropChangedUndoAction : UndoActionBase
	{
		#region Member variables
		private FdoCache m_cache;
		private bool m_fForUndo;
		private PropChangeType m_pct;
		private List<PropChangedInfo> m_propChangeds;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropChangedUndoAction"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fForUndo"><c>true</c> for Undo, <c>false</c> for Redo.</param>
		/// <param name="pct">Type of property change notification.</param>
		/// <param name="propChangeds">The prop changeds.</param>
		/// ------------------------------------------------------------------------------------
		public PropChangedUndoAction(FdoCache cache, bool fForUndo, PropChangeType pct,
			List<PropChangedInfo> propChangeds)
		{
			m_cache = cache;
			m_fForUndo = fForUndo;
			m_pct = pct;
			m_propChangeds = propChangeds;
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fires the prop changeds.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FirePropChangeds()
		{
			foreach (PropChangedInfo propChanged in m_propChangeds)
				m_cache.PropChanged(null, m_pct, propChanged.hvo, propChanged.tag, propChanged.ivMin,
					propChanged.cvIns, propChanged.cvDel);
		}
		#endregion

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending"><c>true</c> if a refresh is pending</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			if (m_fForUndo && !fRefreshPending)
				FirePropChangeds();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending"><c>true</c> if a refresh is pending</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			if (!m_fForUndo && !fRefreshPending)
				FirePropChangeds();
			return true;
		}
		#endregion
	}
	#endregion

	#region Class ReloadVirtualHandlerUndoAction
	/// <summary>
	/// This undo action reloads a virtual handler.
	/// </summary>
	public class ReloadVirtualHandlerUndoAction : UndoActionBase
	{
		FdoCache m_cache;
		bool m_fForUndo;
		IVwVirtualHandler m_vh;
		int m_hvo;
		int m_tag;
		int m_ws;

		/// <summary>
		/// Make one AND make the change to the cache.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fForUndo"><c>true</c> for Undo, <c>false</c> for Redo.</param>
		/// <param name="vh">The vh.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ws">The ws.</param>
		public ReloadVirtualHandlerUndoAction(FdoCache cache, bool fForUndo, IVwVirtualHandler vh, int hvo, int tag, int ws)
		{
			m_cache = cache;
			m_fForUndo = fForUndo;
			m_vh = vh;
			m_hvo = hvo;
			m_tag = tag;
			m_ws = ws;
		}

		#region Overrides of UndoActionBase
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool IsDataChange()
		{
			return false;
		}

		void Reload()
		{
			m_vh.Load(m_hvo, m_tag, m_ws, m_cache.VwCacheDaAccessor);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Undo(bool fRefreshPending)
		{
			if (m_fForUndo && !fRefreshPending)
				Reload();
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Redo(bool fRefreshPending)
		{
			if (!m_fForUndo && !fRefreshPending)
				Reload();
			return true;
		}

		#endregion
	}
	#endregion
}
