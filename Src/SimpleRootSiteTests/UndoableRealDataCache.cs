// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.TestUtilities;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Undoable real data cache.
	/// </summary>
	public class UndoableRealDataCache : IRealDataCache
	{
		private RealDataCache m_cache;
		private bool m_isDirty;

		public UndoableRealDataCache()
		{
			m_cache = new RealDataCache();
		}

		private IActionHandler ActionHandler => GetActionHandler();

		private void MakeDirty()
		{
			m_isDirty = true;
		}

		public ITsStrFactory TsStrFactory { get; set; }

		/// <summary />
		public void CacheObjProp(int obj, int tag, int val)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache,
				new CacheInfo(ObjType.Object, obj, tag, m_cache.get_Prop(obj, tag)),
				new CacheInfo(ObjType.Object, obj, tag, val)));

			m_cache.CacheObjProp(obj, tag, val);
		}

		/// <summary />
		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			CacheObjProp(hvo, tag, hvoObj);
			MakeDirty();
		}

		/// <summary />
		public int get_ObjectProp(int hvo, int tag)
		{
			return m_cache.get_ObjectProp(hvo, tag);
		}

		/// <summary />
		public void CacheBooleanProp(int hvo, int tag, bool value)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.Bool, hvo, tag, m_cache.get_Prop(hvo, tag)), new CacheInfo(ObjType.Bool, hvo, tag, value)));
			m_cache.CacheBooleanProp(hvo, tag, value);
		}

		/// <summary />
		public void SetBoolean(int hvo, int tag, bool n)
		{
			CacheBooleanProp(hvo, tag, n);
			MakeDirty();
		}

		/// <summary />
		public bool get_BooleanProp(int hvo, int tag)
		{
			return m_cache.get_BooleanProp(hvo, tag);
		}

		/// <summary />
		public void CacheGuidProp(int obj, int tag, Guid uid)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.Guid, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.Guid, obj, tag, uid)));

			m_cache.CacheGuidProp(obj, tag, uid);
		}

		/// <summary />
		public void SetGuid(int hvo, int tag, Guid uid)
		{
			CacheGuidProp(hvo, tag, uid);
			MakeDirty();
		}

		/// <summary />
		public Guid get_GuidProp(int hvo, int tag)
		{
			return m_cache.get_GuidProp(hvo, tag);
		}

		/// <summary />
		public int get_ObjFromGuid(Guid uid)
		{
			return m_cache.get_ObjFromGuid(uid);
		}

		/// <summary />
		public void CacheIntProp(int obj, int tag, int val)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.Int, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.Int, obj, tag, val)));

			m_cache.CacheIntProp(obj, tag, val);
		}

		/// <summary />
		public void SetInt(int hvo, int tag, int n)
		{
			CacheIntProp(hvo, tag, n);
			MakeDirty();
		}

		/// <summary />
		public int get_IntProp(int hvo, int tag)
		{
			return m_cache.get_IntProp(hvo, tag);
		}

		/// <summary>
		/// Method to retrieve a particular int property if it is in the cache,
		/// and return a bool to say whether it was or not.
		/// Similar to ISilDataAccess::get_IntProp, but this method
		/// is guaranteed not to do a lazy load of the property
		/// and it makes it easier for .Net clients to see whether the property was loaded,
		/// because this info is not hidden in an HRESULT.
		/// </summary>
		public int get_CachedIntProp(int obj, int tag, out bool isInCache)
		{
			return m_cache.get_CachedIntProp(obj, tag, out isInCache);
		}

		/// <summary />
		public void CacheUnicodeProp(int obj, int tag, string val, int cch)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.String, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.String, obj, tag, val)));

			m_cache.CacheUnicodeProp(obj, tag, val, cch);
		}

		/// <summary />
		public void SetUnicode(int hvo, int tag, string rgch, int cch)
		{
			CacheUnicodeProp(hvo, tag, rgch, cch);
			MakeDirty();
		}

		/// <summary />
		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			CacheUnicodeProp(obj, tag, bstr, bstr.Length);
			MakeDirty();
		}

		/// <summary />
		public string get_UnicodeProp(int obj, int tag)
		{
			return m_cache.get_UnicodeProp(obj, tag);
		}

		/// <summary />
		public void UnicodePropRgch(int obj, int tag, ArrayPtr rgch, int cchMax, out int cch)
		{
			m_cache.UnicodePropRgch(obj, tag, rgch, cchMax, out cch);
		}

		/// <summary />
		public void CacheTimeProp(int obj, int tag, long val)
		{
			var before = new CacheInfo(ObjType.Time, obj, tag, m_cache.get_Prop(obj, tag));
			var after = new CacheInfo(ObjType.Time, obj, tag, val);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheTimeProp(obj, tag, val);
		}

		/// <summary />
		public void SetTime(int hvo, int tag, long lln)
		{
			CacheTimeProp(hvo, tag, lln);
			MakeDirty();
		}

		/// <summary />
		public long get_TimeProp(int hvo, int tag)
		{
			return m_cache.get_TimeProp(hvo, tag);
		}

		/// <summary />
		public void CacheInt64Prop(int obj, int tag, long val)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.Long, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.Long, obj, tag, val)));

			m_cache.CacheInt64Prop(obj, tag, val);
		}

		/// <summary />
		public void SetInt64(int hvo, int tag, long lln)
		{
			CacheInt64Prop(hvo, tag, lln);
			MakeDirty();
		}

		/// <summary />
		public long get_Int64Prop(int hvo, int tag)
		{
			return m_cache.get_Int64Prop(hvo, tag);
		}

		/// <summary />
		public void CacheUnknown(int obj, int tag, object unk)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.Object, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.Object, obj, tag, unk)));

			m_cache.CacheUnknown(obj, tag, unk);
		}

		/// <summary />
		public void SetUnknown(int hvo, int tag, object unk)
		{
			CacheUnknown(hvo, tag, unk);
			MakeDirty();
		}

		/// <summary />
		public object get_UnknownProp(int hvo, int tag)
		{
			return m_cache.get_UnknownProp(hvo, tag);
		}

		/// <summary />
		public void CacheBinaryProp(int obj, int tag, byte[] rgb, int cb)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.ByteArray, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.ByteArray, obj, tag, rgb)));

			m_cache.CacheBinaryProp(obj, tag, rgb, cb);
		}

		/// <summary />
		public void SetBinary(int hvo, int tag, byte[] rgb, int cb)
		{
			CacheBinaryProp(hvo, tag, rgb, cb);
			MakeDirty();
		}

		/// <summary />
		public void BinaryPropRgb(int obj, int tag, ArrayPtr rgb, int cbMax, out int cb)
		{
			m_cache.BinaryPropRgb(obj, tag, rgb, cbMax, out cb);
		}

		/// <summary />
		public void CacheStringProp(int obj, int tag, ITsString tss)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.BasicTsString, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.BasicTsString, obj, tag, tss)));

			m_cache.CacheStringProp(obj, tag, tss);
		}

		/// <summary />
		public void SetString(int hvo, int tag, ITsString tss)
		{
			CacheStringProp(hvo, tag, tss);
			MakeDirty();
		}

		/// <summary />
		public ITsString get_StringProp(int hvo, int tag)
		{
			return m_cache.get_StringProp(hvo, tag);
		}

		/// <summary />
		public void CacheStringAlt(int obj, int tag, int ws, ITsString tss)
		{
			var before = new CacheInfo(ObjType.ExtendedTsString, obj, tag, ws,
				m_cache.get_IsPropInCache(obj, tag, (int)CellarPropertyType.MultiString, ws) ? m_cache.get_MultiStringAlt(obj, tag, ws) : null);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, new CacheInfo(ObjType.ExtendedTsString, obj, tag, ws, tss)));

			m_cache.CacheStringAlt(obj, tag, ws, tss);
		}

		/// <summary />
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			CacheStringAlt(hvo, tag, ws, tss);
			MakeDirty();
		}

		/// <summary />
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			return m_cache.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary />
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			return m_cache.get_MultiStringProp(hvo, tag);
		}

		/// <summary />
		public void CacheVecProp(int obj, int tag, int[] rghvo, int chvo)
		{
			ActionHandler.AddAction(new CacheUndoAction(m_cache, new CacheInfo(ObjType.Vector, obj, tag, m_cache.get_Prop(obj, tag)), new CacheInfo(ObjType.Vector, obj, tag, rghvo)));

			m_cache.CacheVecProp(obj, tag, rghvo, chvo);
		}

		/// <summary>
		/// Get the full contents of the specified sequence in one go.
		/// </summary>
		public void VecProp(int obj, int tag, int chvoMax, out int chvo, ArrayPtr rghvo)
		{
			m_cache.VecProp(obj, tag, chvoMax, out chvo, rghvo);
		}

		/// <summary />
		public int get_VecItem(int hvo, int tag, int index)
		{
			return m_cache.get_VecItem(hvo, tag, index);
		}

		/// <summary />
		public int get_VecSize(int hvo, int tag)
		{
			return m_cache.get_VecSize(hvo, tag);
		}

		/// <summary />
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			return m_cache.get_VecSizeAssumeCached(hvo, tag);
		}

		/// <summary />
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			return m_cache.GetObjIndex(hvoOwn, flid, hvo);
		}

		/// <summary />
		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] rghvo, int chvo)
		{
			m_cache.CacheReplace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);
		}

		/// <summary />
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			m_cache.MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary />
		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			m_cache.MoveOwn(hvoSrcOwner, tagSrc, hvo, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary />
		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] rghvo, int chvo)
		{
			m_cache.Replace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);
		}

		/// <summary />
		public void BeginNonUndoableTask()
		{
			m_cache.BeginNonUndoableTask();
		}

		/// <summary />
		public void EndNonUndoableTask()
		{
			m_cache.EndNonUndoableTask();
		}

		/// <summary />
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_cache.BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary />
		public void EndUndoTask()
		{
			m_cache.EndUndoTask();
		}

		/// <summary />
		public void ContinueUndoTask()
		{
			m_cache.ContinueUndoTask();
		}

		/// <summary />
		public void EndOuterUndoTask()
		{
			m_cache.EndOuterUndoTask();
		}

		/// <summary />
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			m_cache.BreakUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary />
		public void Rollback()
		{
			m_cache.Rollback();
		}

		/// <summary />
		public IActionHandler GetActionHandler()
		{
			return m_cache.GetActionHandler();
		}

		/// <summary />
		public void SetActionHandler(IActionHandler actionhandler)
		{
			m_cache.SetActionHandler(actionhandler);
		}

		/// <summary />
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			m_cache.InsertRelExtra(hvoSrc, tag, ihvo, hvoDst, bstrExtra);
		}

		/// <summary />
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			m_cache.UpdateRelExtra(hvoSrc, tag, ihvo, bstrExtra);
		}

		/// <summary />
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			return m_cache.GetRelExtra(hvoSrc, tag, ihvo);
		}

		/// <summary />
		public object get_Prop(int hvo, int tag)
		{
			return m_cache.get_Prop(hvo, tag);
		}

		/// <summary />
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			return m_cache.get_IsPropInCache(hvo, tag, cpt, ws);
		}

		/// <summary />
		public void DeleteObj(int hvoObj)
		{
			m_cache.DeleteObj(hvoObj);
		}

		/// <summary />
		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			m_cache.DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
		}

		/// <summary />
		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			m_cache.InsertNew(hvoObj, tag, ihvo, chvo, _ss);
		}

		/// <summary />
		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			return m_cache.MakeNewObject(clid, hvoOwner, tag, ord);
		}

		/// <summary />
		public bool get_IsValidObject(int hvo)
		{
			return m_cache.get_IsValidObject(hvo);
		}

		/// <summary />
		public bool get_IsDummyId(int hvo)
		{
			return m_cache.get_IsDummyId(hvo);
		}

		/// <summary />
		public void RemoveObjRefs(int hvo)
		{
			m_cache.RemoveObjRefs(hvo);
		}

		/// <summary />
		public void AddNotification(IVwNotifyChange _nchng)
		{
			m_cache.AddNotification(_nchng);
		}

		/// <summary />
		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			m_cache.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
		}

		/// <summary />
		public void RemoveNotification(IVwNotifyChange _nchng)
		{
			m_cache.RemoveNotification(_nchng);
		}

		/// <summary />
		public int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			return m_cache.GetDisplayIndex(hvoOwn, flid, ihvo);
		}

		/// <summary>
		/// So far we haven't needed this for the purposes of CacheLight...it's used for multi-paragraph (and eventually drag/drop) editing.
		/// </summary>
		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst, int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			m_cache.MoveString(hvoSource, flidSrc, wsSrc, ichMin, ichLim, hvoDst, flidDst, wsDst, ichDest, fDstIsNew);
		}

		/// <summary>
		/// Return a list of the encodings that are of interest within the database.
		/// </summary>
		/// <param name="cwsMax">If cwsMax is zero, return the actual number (but no encodings).</param><param name="_ws">List of encodings, if cwsMax is greater than zero AND there was enough room to put them in</param>
		/// <returns>
		/// Return the actual number. If there is not enough room, throw an invalid argument exception.
		/// </returns>
		public int get_WritingSystemsOfInterest(int cwsMax, ArrayPtr _ws)
		{
			return m_cache.get_WritingSystemsOfInterest(cwsMax, _ws);
		}

		/// <summary />
		public bool IsDirty()
		{
			return m_isDirty;
		}

		/// <summary />
		public void ClearDirty()
		{
			m_isDirty = false;
		}

		/// <summary />
		public void InstallVirtual(IVwVirtualHandler vh)
		{
			m_cache.InstallVirtual(vh);
		}

		/// <summary />
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			return m_cache.GetVirtualHandlerId(tag);
		}

		/// <summary />
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			return m_cache.GetVirtualHandlerName(bstrClass, bstrField);
		}

		/// <summary />
		public void ClearVirtualProperties()
		{
			m_cache.ClearVirtualProperties();
		}

		/// <summary />
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			m_cache.ClearInfoAbout(hvo, cia);
		}

		/// <summary />
		public void ClearInfoAboutAll(int[] rghvo, int chvo, VwClearInfoAction cia)
		{
			m_cache.ClearInfoAboutAll(rghvo, chvo, cia);
		}

		/// <summary />
		public void ClearAllData()
		{
			m_cache.ClearAllData();
		}

		/// <summary />
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			return m_cache.GetOutlineNumber(hvo, flid, fFinPer);
		}

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~UndoableRealDataCache()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (disposing)
			{
				m_cache.Dispose();
			}
		}

		/// <summary>
		/// Gets or sets the paragraph contents field id.
		/// </summary>
		public int ParaContentsFlid
		{
			get { return m_cache.ParaContentsFlid; }
			set { m_cache.ParaContentsFlid = value; }
		}

		/// <summary>
		/// Gets or sets the paragraph properties field id.
		/// </summary>
		public int ParaPropertiesFlid
		{
			get { return m_cache.ParaPropertiesFlid; }
			set { m_cache.ParaPropertiesFlid = value; }
		}

		/// <summary>
		/// Gets or sets the text paragraphs field id.
		/// </summary>
		public int TextParagraphsFlid
		{
			get { return m_cache.TextParagraphsFlid; }
			set { m_cache.TextParagraphsFlid = value; }
		}

		/// <summary />
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_cache.WritingSystemFactory; }
			set { m_cache.WritingSystemFactory = value; }
		}

		/// <summary />
		public IFwMetaDataCache MetaDataCache
		{
			get { return m_cache.MetaDataCache; }
			set { m_cache.MetaDataCache = value; }
		}
	}
}