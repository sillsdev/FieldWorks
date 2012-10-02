using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Implements ISilDataAccess by wrapping another instance. Updates will fail until AllowWrites is set to true.
	/// </summary>
	public class WriteProtectedDataAccess : ISilDataAccess, IVwCacheDa
	{
		internal ISilDataAccess m_sda;
		bool m_fAllowWrites = false;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="sda"></param>
		public WriteProtectedDataAccess(ISilDataAccess sda)
		{
			m_sda = sda;
		}

		/// <summary>
		/// Set true to allow update operations to propagate to the embedded SDA.
		/// </summary>
		public bool AllowWrites
		{
			get { return m_fAllowWrites; }
			set { m_fAllowWrites = value; }
		}

		/// <summary>
		/// A change was made to the data.
		/// </summary>
		public event EventHandler UpdatePerformed;

		/// <summary>
		/// Called when an update is attempted.
		/// </summary>
		protected virtual void VerifyUpdate(int obj, int tag)
		{
			Assert.IsTrue(m_fAllowWrites, "Write attempted outside scope of Begin/EndUndoTask (object " + obj + " tag " + tag + ")");
			if (UpdatePerformed != null)
				UpdatePerformed(this, new EventArgs());
		}

		#region ISilDataAccess Members

		/// <summary>
		///
		/// </summary>
		/// <param name="_nchng"></param>
		public void AddNotification(IVwNotifyChange _nchng)
		{
			m_sda.AddNotification(_nchng);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrUndo"></param>
		/// <param name="bstrRedo"></param>
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_sda.BeginUndoTask(bstrUndo, bstrRedo);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_rgb"></param>
		/// <param name="cbMax"></param>
		/// <param name="_cb"></param>
		public void BinaryPropRgb(int obj, int tag, ArrayPtr _rgb, int cbMax, out int _cb)
		{
			m_sda.BinaryPropRgb(obj, tag, _rgb, cbMax, out _cb);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrUndo"></param>
		/// <param name="bstrRedo"></param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			m_sda.BreakUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		///
		/// </summary>
		public void ClearDirty()
		{
			m_sda.ClearDirty();
		}

		/// <summary>
		///
		/// </summary>
		public void ContinueUndoTask()
		{
			m_sda.ContinueUndoTask();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		public virtual void DeleteObj(int hvoObj)
		{
			VerifyUpdate(hvoObj, -1);
			m_sda.DeleteObj(hvoObj);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		public virtual void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			VerifyUpdate(hvoOwner, tag);
			m_sda.DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
		}

		/// <summary>
		///
		/// </summary>
		public void EndOuterUndoTask()
		{
			m_sda.EndOuterUndoTask();
		}

		/// <summary>
		///
		/// </summary>
		public void EndUndoTask()
		{
			m_sda.EndUndoTask();
		}

		/// <summary>
		///
		/// </summary>
		public void Rollback()
		{
			m_sda.Rollback();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IActionHandler GetActionHandler()
		{
			return m_sda.GetActionHandler();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwn"></param>
		/// <param name="flid"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			return m_sda.GetObjIndex(hvoOwn, flid, hvo);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="fFinPer"></param>
		/// <returns></returns>
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			return m_sda.GetOutlineNumber(hvo, flid, fFinPer);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <returns></returns>
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			return m_sda.GetRelExtra(hvoSrc, tag, ihvo);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvo"></param>
		/// <param name="_ss"></param>
		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			VerifyUpdate(hvoObj, tag);
			m_sda.InsertNew(hvoObj, tag, ihvo, chvo, _ss);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="hvoDst"></param>
		/// <param name="bstrExtra"></param>
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			VerifyUpdate(hvoSrc, tag);
			m_sda.InsertRelExtra(hvoSrc, tag, ihvo, hvoDst, bstrExtra);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool IsDirty()
		{
			return m_sda.IsDirty();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="tag"></param>
		/// <param name="ord"></param>
		/// <returns></returns>
		public virtual int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			VerifyUpdate(hvoOwner, tag);
			return m_sda.MakeNewObject(clid, hvoOwner, tag, ord);
		}

		/// <summary>
		///
		/// </summary>
		public IFwMetaDataCache MetaDataCache
		{
			get
			{
				return m_sda.MetaDataCache;
			}
			set
			{
				m_sda.MetaDataCache = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrcOwner"></param>
		/// <param name="tagSrc"></param>
		/// <param name="ihvoStart"></param>
		/// <param name="ihvoEnd"></param>
		/// <param name="hvoDstOwner"></param>
		/// <param name="tagDst"></param>
		/// <param name="ihvoDstStart"></param>
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			VerifyUpdate(hvoSrcOwner, tagSrc);
			VerifyUpdate(hvoDstOwner, tagDst);
			m_sda.MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrcOwner"></param>
		/// <param name="tagSrc"></param>
		/// <param name="hvo"></param>
		/// <param name="hvoDstOwner"></param>
		/// <param name="tagDst"></param>
		/// <param name="ihvoDstStart"></param>
		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			VerifyUpdate(hvoSrcOwner, tagSrc);
			VerifyUpdate(hvoDstOwner, tagDst);
			m_sda.MoveOwn(hvoSrcOwner, tagSrc, hvo, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="_nchng"></param>
		/// <param name="_ct"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			m_sda.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="_nchng"></param>
		public void RemoveNotification(IVwNotifyChange _nchng)
		{
			m_sda.RemoveNotification(_nchng);
		}

		/// <summary>
		/// Although the name sounds like an update operation, it's really just about
		/// cleaning up the cache.
		/// </summary>
		/// <param name="hvo"></param>
		public void RemoveObjRefs(int hvo)
		{
			m_sda.RemoveObjRefs(hvo);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="_rghvo"></param>
		/// <param name="chvo"></param>
		public virtual void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			VerifyUpdate(hvoObj, tag);
			m_sda.Replace(hvoObj, tag, ihvoMin, ihvoLim, _rghvo, chvo);
		}

		/// <summary>
		/// Although the name has a Set, it doesn't actually modify the data.
		/// </summary>
		/// <param name="_acth"></param>
		public void SetActionHandler(IActionHandler _acth)
		{
			m_sda.SetActionHandler(_acth);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="_rgb"></param>
		/// <param name="cb"></param>
		public void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetBinary(hvo, tag, _rgb, cb);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="n"></param>
		public void SetBoolean(int hvo, int tag, bool n)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetBoolean(hvo, tag, n);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="uid"></param>
		public void SetGuid(int hvo, int tag, Guid uid)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetGuid(hvo, tag, uid);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="n"></param>
		public void SetInt(int hvo, int tag, int n)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetInt(hvo, tag, n);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="lln"></param>
		public void SetInt64(int hvo, int tag, long lln)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetInt64(hvo, tag, lln);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_tss"></param>
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetMultiStringAlt(hvo, tag, ws, _tss);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="hvoObj"></param>
		public virtual void SetObjProp(int hvo, int tag, int hvoObj)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetObjProp(hvo, tag, hvoObj);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="_tss"></param>
		public void SetString(int hvo, int tag, ITsString _tss)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetString(hvo, tag, _tss);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="lln"></param>
		public void SetTime(int hvo, int tag, long lln)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetTime(hvo, tag, lln);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="_rgch"></param>
		/// <param name="cch"></param>
		public void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetUnicode(hvo, tag, _rgch, cch);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="_unk"></param>
		public void SetUnknown(int hvo, int tag, object _unk)
		{
			VerifyUpdate(hvo, tag);
			m_sda.SetUnknown(hvo, tag, _unk);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_rgch"></param>
		/// <param name="cchMax"></param>
		/// <param name="_cch"></param>
		public void UnicodePropRgch(int obj, int tag, ArrayPtr _rgch, int cchMax, out int _cch)
		{
			VerifyUpdate(obj, tag);
			m_sda.UnicodePropRgch(obj, tag, _rgch, cchMax, out _cch);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="bstrExtra"></param>
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			VerifyUpdate(hvoSrc, tag);
			m_sda.UpdateRelExtra(hvoSrc, tag, ihvo, bstrExtra);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="chvoMax"></param>
		/// <param name="_chvo"></param>
		/// <param name="_rghvo"></param>
		public void VecProp(int hvo, int tag, int chvoMax, out int _chvo, ArrayPtr _rghvo)
		{
			m_sda.VecProp(hvo, tag, chvoMax, out _chvo, _rghvo);
		}

		/// <summary>
		///
		/// </summary>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return m_sda.WritingSystemFactory;
			}
			set
			{
				m_sda.WritingSystemFactory = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public bool get_BooleanProp(int hvo, int tag)
		{
			return m_sda.get_BooleanProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public Guid get_GuidProp(int hvo, int tag)
		{
			return m_sda.get_GuidProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public long get_Int64Prop(int hvo, int tag)
		{
			return m_sda.get_Int64Prop(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int get_IntProp(int hvo, int tag)
		{
			return m_sda.get_IntProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="cpt"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			return m_sda.get_IsPropInCache(hvo, tag, cpt, ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public bool get_IsDummyId(int hvo)
		{
			return m_sda.get_IsDummyId(hvo);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public bool get_IsValidObject(int hvo)
		{
			return m_sda.get_IsValidObject(hvo);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			return m_sda.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			return m_sda.get_MultiStringProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="uid"></param>
		/// <returns></returns>
		public int get_ObjFromGuid(Guid uid)
		{
			return m_sda.get_ObjFromGuid(uid);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int get_ObjectProp(int hvo, int tag)
		{
			return m_sda.get_ObjectProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public object get_Prop(int hvo, int tag)
		{
			return m_sda.get_Prop(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public ITsString get_StringProp(int hvo, int tag)
		{
			return m_sda.get_StringProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public long get_TimeProp(int hvo, int tag)
		{
			return m_sda.get_TimeProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public string get_UnicodeProp(int obj, int tag)
		{
			return m_sda.get_UnicodeProp(obj, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public object get_UnknownProp(int hvo, int tag)
		{
			return m_sda.get_UnknownProp(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public int get_VecItem(int hvo, int tag, int index)
		{
			return m_sda.get_VecItem(hvo, tag, index);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int get_VecSize(int hvo, int tag)
		{
			return m_sda.get_VecSize(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			return m_sda.get_VecSizeAssumeCached(hvo, tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cwsMax"></param>
		/// <param name="_ws"></param>
		/// <returns></returns>
		public int get_WritingSystemsOfInterest(int cwsMax, ArrayPtr _ws)
		{
			return m_sda.get_WritingSystemsOfInterest(cwsMax, _ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="bstr"></param>
		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			VerifyUpdate(obj, tag);
			m_sda.set_UnicodeProp(obj, tag, bstr);
		}

		#endregion

		#region IVwCacheDa Members

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_rgb"></param>
		/// <param name="cb"></param>
		public void CacheBinaryProp(int obj, int tag, byte[] _rgb, int cb)
		{
			(m_sda as IVwCacheDa).CacheBinaryProp(obj, tag, _rgb, cb);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		public void CacheBooleanProp(int obj, int tag, bool val)
		{
			(m_sda as IVwCacheDa).CacheBooleanProp(obj, tag, val);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="uid"></param>
		public void CacheGuidProp(int obj, int tag, Guid uid)
		{
			(m_sda as IVwCacheDa).CacheGuidProp(obj, tag, uid);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		public void CacheInt64Prop(int obj, int tag, long val)
		{
			(m_sda as IVwCacheDa).CacheInt64Prop(obj, tag, val);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		public void CacheIntProp(int obj, int tag, int val)
		{
			(m_sda as IVwCacheDa).CacheIntProp(obj, tag, val);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		public void CacheObjProp(int obj, int tag, int val)
		{
			(m_sda as IVwCacheDa).CacheObjProp(obj, tag, val);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="_rghvo"></param>
		/// <param name="chvo"></param>
		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			(m_sda as IVwCacheDa).CacheReplace(hvoObj, tag, ihvoMin, ihvoLim, _rghvo, chvo);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_tss"></param>
		public void CacheStringAlt(int obj, int tag, int ws, ITsString _tss)
		{
			(m_sda as IVwCacheDa).CacheStringAlt(obj, tag, ws, _tss);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_rgchTxt"></param>
		/// <param name="cchTxt"></param>
		/// <param name="_rgbFmt"></param>
		/// <param name="cbFmt"></param>
		public void CacheStringFields(int obj, int tag, string _rgchTxt, int cchTxt, byte[] _rgbFmt, int cbFmt)
		{
			(m_sda as IVwCacheDa).CacheStringFields(obj, tag, _rgchTxt, cchTxt, _rgbFmt, cbFmt);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_tss"></param>
		public void CacheStringProp(int obj, int tag, ITsString _tss)
		{
			(m_sda as IVwCacheDa).CacheStringProp(obj, tag, _tss);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		public void CacheTimeProp(int hvo, int tag, long val)
		{
			(m_sda as IVwCacheDa).CacheTimeProp(hvo, tag, val);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_rgch"></param>
		/// <param name="cch"></param>
		public void CacheUnicodeProp(int obj, int tag, string _rgch, int cch)
		{
			(m_sda as IVwCacheDa).CacheUnicodeProp(obj, tag, _rgch, cch);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_unk"></param>
		public void CacheUnknown(int obj, int tag, object _unk)
		{
			(m_sda as IVwCacheDa).CacheUnknown(obj, tag, _unk);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		public void CacheVecProp(int obj, int tag, int[] rghvo, int chvo)
		{
			(m_sda as IVwCacheDa).CacheVecProp(obj, tag, rghvo, chvo);
		}

		/// <summary>
		///
		/// </summary>
		public void ClearAllData()
		{
			ClearAllData();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="cia"></param>
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			(m_sda as IVwCacheDa).ClearInfoAbout(hvo, cia);
		}

		/// <summary>Member ClearInfoAbout</summary>
		/// <param name='rghvo'>hvo</param>
		/// <param name="chvo"></param>
		/// <param name='cia'>cia</param>
		public void ClearInfoAboutAll(int[] rghvo, int chvo, VwClearInfoAction cia)
		{
			(m_sda as IVwCacheDa).ClearInfoAboutAll(rghvo, chvo, cia);
		}


		/// <summary>
		///
		/// </summary>
		public void ClearVirtualProperties()
		{
			(m_sda as IVwCacheDa).ClearVirtualProperties();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			return (m_sda as IVwCacheDa).GetVirtualHandlerId(tag);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrClass"></param>
		/// <param name="bstrField"></param>
		/// <returns></returns>
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			return (m_sda as IVwCacheDa).GetVirtualHandlerName(bstrClass, bstrField);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="_vh"></param>
		public void InstallVirtual(IVwVirtualHandler _vh)
		{
			(m_sda as IVwCacheDa).InstallVirtual(_vh);
		}

		/// <summary>
		///
		/// </summary>
		public void ResumePropChanges()
		{
			ResumePropChanges();
		}

		/// <summary>
		///
		/// </summary>
		public void SuppressPropChanges()
		{
			SuppressPropChanges();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		/// <param name="_f"></param>
		/// <returns></returns>
		public int get_CachedIntProp(int obj, int tag, out bool _f)
		{
			return (m_sda as IVwCacheDa).get_CachedIntProp(obj, tag, out _f);
		}

		#endregion
	}

	/// <summary>
	/// This class imposes further checks: it is preset with expected final values of
	/// certain properties. Modifying other properties is an error. VerifyExpectedChanges
	/// may be called at the end to verify all expected changes occurred.
	/// Note that (following YAGNI) only some of the logically useful Expect messages are implemented.
	/// </summary>
	public class CheckedUpdateDataAccess : WriteProtectedDataAccess
	{

		Set<NewValInfo> m_allowedProps = new Set<NewValInfo>();
		List<ReplaceVecInfo> m_newVecVals = new List<ReplaceVecInfo>();
		Set<int> m_expectedDeleteObjects = new Set<int>();
		List<ReplaceAtomicInfo> m_newAtomicVals = new List<ReplaceAtomicInfo>();
		List<CreateObjectInfo> m_createObjectVals = new List<CreateObjectInfo>();
		List<StringAltInfo> m_expectedStringAlts = new List<StringAltInfo>();
		List<UnicodePropInfo> m_expectedUnicodeProps = new List<UnicodePropInfo>();

		List<int> m_unmappedNewObjectIds = new List<int>();
		Dictionary<int, int> m_newObjectSubstitutions = new Dictionary<int, int>();
		const int kfirstNewObjectId = 50000000;
		int m_nextNewObjectId = kfirstNewObjectId;

		/// <summary>
		/// Get an ID that can stand in result value lists for an object we expect to be created.
		/// </summary>
		/// <returns></returns>
		public int GetNewObjectId()
		{
			return m_nextNewObjectId++;
		}

		bool IsNewObjectId(int val)
		{
			return val >= kfirstNewObjectId && val < m_nextNewObjectId;
		}

		int GetRealObjectId(int val)
		{
			if (!IsNewObjectId(val))
				return val;
			int result;
			if (m_newObjectSubstitutions.TryGetValue(val, out result))
				return result;
			return 0;
		}

		int MapObjectId(int val, int expected)
		{
			int realId = GetRealObjectId(val);
			if (realId != 0)
				return realId;
			if (m_unmappedNewObjectIds.Contains(expected))
			{
				m_unmappedNewObjectIds.Remove(expected);
				m_newObjectSubstitutions[val] = expected;
				return expected;
			}
			Assert.Fail("Cannot match an expected new object to one of those actually created");
			return 0; // not reachable
		}

		/// <summary>
		///  Make one.
		/// </summary>
		/// <param name="sda"></param>
		public CheckedUpdateDataAccess(ISilDataAccess sda)
			: base(sda)
		{
		}

		/// <summary>
		/// Expect the specified number of objects to be created in the specified property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="count"></param>
		/// <param name="message"></param>
		public void ExpectCreateObjectInCollection(int hvo, int tag, int count, string message)
		{
			CreateObjectInfo info = new CreateObjectInfo(hvo, tag, message, count);
			m_allowedProps.Add(info);
			m_createObjectVals.Add(info);
		}


		/// <summary>
		/// Notifies the system that we expect the SUT to leave the cache with the specified value
		/// for the specified alternative.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="value"></param>
		/// <param name="ws"></param>
		/// <param name="message"></param>
		public void ExpectStringAlt(int hvo, int tag, int ws, ITsString value, string message)
		{
			StringAltInfo info = new StringAltInfo(hvo, tag, ws, value, message);
			m_allowedProps.Add(info);
			m_expectedStringAlts.Add(info);
		}
		/// <summary>
		/// Notifies the system that we expect the SUT to leave the cache with the specified value
		/// of a Unicode property (or big-unicode).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="value"></param>
		/// <param name="message"></param>
		public void ExpectUnicode(int hvo, int tag, string value, string message)
		{
			UnicodePropInfo info = new UnicodePropInfo(hvo, tag, value, message);
			m_allowedProps.Add(info);
			m_expectedUnicodeProps.Add(info);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="tag"></param>
		/// <param name="ord"></param>
		/// <returns></returns>
		public override int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			int hvoNew = base.MakeNewObject(clid, hvoOwner, tag, ord);

			foreach (CreateObjectInfo info in m_createObjectVals)
			{
				if (info.hvo != hvoOwner || info.tag != tag)
					continue;
				Assert.IsTrue(info.count > 0, "Too many objects created");
				info.count--;
				m_unmappedNewObjectIds.Add(hvoNew);
				return hvoNew;
			}
			Assert.Fail("Trying to create object in an unexpected property");
			return 0; //unreachable.
		}

		/// <summary>
		/// In this subclass, all writes are invalid unless the appropriate method
		/// has been overridden to allow them if expected.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tag"></param>
		protected override void VerifyUpdate(int obj, int tag)
		{
			base.VerifyUpdate(obj, tag);
			if (m_expectedDeleteObjects.Contains(obj))
				return; // Don't worry about modifications to objects we expect to delete.
			if (m_unmappedNewObjectIds.Contains(obj))
				return; // Assume newly created objects can be modified.
			Assert.IsTrue(m_allowedProps.Contains(new NewValInfo(obj, tag, null)),
				"Write attempted on unexpected property or unsupported property type (object " + obj + " tag " + tag + ")");
		}

		/// <summary>
		/// Call after exercising the SUT to verify all expected changes occurred.
		/// </summary>
		public void VerifyExpectedChanges()
		{
			SetupNewObjectMap();
			VerifyVecVals();
			VerifyDeletedObjects();
			VerifyAtomicVals();
			VerifyCreateObjectInCollection();
			VerifyStringAlts();
			VerifyUnicodeProps();
		}

		private void VerifyCreateObjectInCollection()
		{
			foreach (CreateObjectInfo info in m_createObjectVals)
				Assert.AreEqual(0, info.count,
					"did not create as many objects as expected in object " + info.hvo + " tag " + info.tag);
		}

		private void VerifyStringAlts()
		{
			foreach (StringAltInfo info in m_expectedStringAlts)
			{
				ITsString tssAlt = m_sda.get_MultiStringAlt(GetRealObjectId(info.hvo), info.tag, info.ws);
				Assert.IsTrue(info.tssAlt.Equals(tssAlt), info.message + "(" + info.tssAlt.Text + " != " + tssAlt.Text);
			}
		}
		private void VerifyUnicodeProps()
		{
			foreach (UnicodePropInfo info in m_expectedUnicodeProps)
			{
				string actual = m_sda.get_UnicodeProp(GetRealObjectId(info.hvo), info.tag);
				Assert.AreEqual(info.newVal, actual, info.message);
			}
		}

		/// <summary>
		/// Expect changes in the specified vector.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="message">inserted into any Assert for bad value</param>
		/// <param name="newValue"></param>
		public void ExpectVector(int hvo, int tag, int[] newValue, string message)
		{
			ReplaceVecInfo info = new ReplaceVecInfo(hvo, tag, message, newValue);
			m_allowedProps.Add(info);
			m_newVecVals.Add(info);
		}

		void SetupNewObjectMap()
		{
			if (m_unmappedNewObjectIds.Count == 0)
				return; // no new objects to map
			List<NewValInfo> tryAgain = new List<NewValInfo>();
			tryAgain.AddRange(m_newVecVals.ToArray());
			tryAgain.AddRange(m_newAtomicVals.ToArray());
			while (tryAgain.Count != 0)
			{
				List<NewValInfo> tryNow = tryAgain;
				tryAgain = new List<NewValInfo>();
				foreach (NewValInfo eachNewVal in tryNow)
				{
					if (IsNewObjectId(eachNewVal.hvo) && !m_newObjectSubstitutions.ContainsKey(eachNewVal.hvo))
					{
						// property OF a new object we haven't found elsewhere...we can't deal with it.
						tryAgain.Add(eachNewVal);
						continue;
					}
					int hvoSrc = MapObjectId(eachNewVal.hvo, 0);
					if (eachNewVal is ReplaceAtomicInfo)
					{
						int val = (eachNewVal as ReplaceAtomicInfo).newVal;
						int expected = m_sda.get_ObjectProp(hvoSrc, eachNewVal.tag);
						MapObjectId(val, expected);
					}
					else
					{
						ReplaceVecInfo info = eachNewVal as ReplaceVecInfo;
						int chvo = m_sda.get_VecSize(hvoSrc, info.tag);
						Assert.AreEqual(info.newVal.Length, chvo, info.message + " (length)");
						for (int i = 0; i < chvo; i++)
						{
							int expected = m_sda.get_VecItem(hvoSrc, info.tag, i);
							MapObjectId(info.newVal[i], expected);
						}
					}
				}
				if (tryAgain.Count == tryNow.Count)
					Assert.Fail("could not resolve all new object IDs");
			}
		}

		void VerifyVecVals()
		{
			foreach (ReplaceVecInfo info in m_newVecVals)
			{
				int chvo = m_sda.get_VecSize(GetRealObjectId(info.hvo), info.tag);
				Assert.AreEqual(info.newVal.Length, chvo, info.message + " (length)");
				for (int i = 0; i < chvo; i++)
					Assert.AreEqual(GetRealObjectId(info.newVal[i]),
						m_sda.get_VecItem(GetRealObjectId(info.hvo), info.tag, i), info.message + " (item " + i + ")");
			}
		}

		/// <summary>
		/// Note the expected value of an atomic property after the test.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="newVal"></param>
		/// <param name="message"></param>
		public void ExpectAtomic(int hvo, int tag, int newVal, string message)
		{
			ReplaceAtomicInfo info = new ReplaceAtomicInfo(hvo, tag, message, newVal);
			m_allowedProps.Add(info);
			m_newAtomicVals.Add(info);
		}

		private void VerifyAtomicVals()
		{
			foreach (ReplaceAtomicInfo info in m_newAtomicVals)
			{
				int hvo = m_sda.get_ObjectProp(GetRealObjectId(info.hvo), info.tag);
				Assert.AreEqual(GetRealObjectId(info.newVal), hvo, info.message);
			}
		}

		/// <summary>
		/// Expect each listed object to be deleted (exactly once!).
		/// Multiple calls are allowed. Duplicate listed objects are OK (but each may still only be deleted once!).
		/// For now, this allows it to be deleted using either DeleteObj or DeleteObjOwner, even if the owning
		/// property has not been specified as modified.
		/// Enhance JohnT: allowing DeleteObjOwner should arguably not be allowed for sequences without
		/// fully specifying the expected new value.
		/// </summary>
		/// <param name="expected"></param>
		public void ExpectDeleteObjects(int[] expected)
		{
			m_expectedDeleteObjects.AddRange(expected);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		public override void DeleteObj(int hvoObj)
		{
			Assert.IsTrue(m_expectedDeleteObjects.Contains(hvoObj), "unexpected object being deleted");
			m_expectedDeleteObjects.Remove(hvoObj);
			base.DeleteObj(hvoObj);
		}

		/// <summary>
		/// Verify that it's one of the objects we're allowed to delete.
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		public override void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			Assert.IsTrue(m_expectedDeleteObjects.Contains(hvoObj), "unexpected object being deleted");
			m_expectedDeleteObjects.Remove(hvoObj);
			m_allowedProps.Add(new NewValInfo(hvoOwner, tag, ""));
			base.DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
		}

		void VerifyDeletedObjects()
		{
			Assert.AreEqual(0, m_expectedDeleteObjects.Count, "some expected objects were not deleted!");
		}

	}

	class NewValInfo
	{
		internal int hvo;
		internal int tag;
		internal string message;

		public NewValInfo(int hvo, int tag, string message)
		{
			this.hvo = hvo;
			this.tag = tag;
			this.message = message;
		}

		public override bool Equals(object obj)
		{
			NewValInfo other = obj as NewValInfo;
			if (other == null)
				return false;
			return this.hvo == other.hvo && this.tag == other.tag;
		}

		public override int GetHashCode()
		{
			return this.hvo + this.tag;
		}
	}

	class ReplaceVecInfo : NewValInfo
	{
		public ReplaceVecInfo(int hvo, int tag, string message, int[] newVal)
			: base(hvo, tag, message)
		{
			this.newVal = newVal;
		}
		internal int[] newVal;
	}

	class ReplaceAtomicInfo : NewValInfo
	{
		public ReplaceAtomicInfo(int hvo, int tag, string message, int newVal)
			: base(hvo, tag, message)
		{
			this.newVal = newVal;
		}
		internal int newVal;
	}

	class StringAltInfo : NewValInfo
	{
		public StringAltInfo(int hvo, int tag, int ws, ITsString tssAlt, string message)
			: base(hvo, tag, message)
		{
			this.tssAlt = tssAlt;
			this.ws = ws;
		}
		internal int ws;
		internal ITsString tssAlt;
	}

	class UnicodePropInfo : NewValInfo
	{
		public UnicodePropInfo(int hvo, int tag, string newVal, string message)
			: base(hvo, tag, message)
		{
			this.newVal = newVal;
		}
		internal string newVal;
	}

	class CreateObjectInfo : NewValInfo
	{
		public CreateObjectInfo(int hvo, int tag, string message, int count)
			: base(hvo, tag, message)
		{
			this.count = count;
		}
		internal int count;
	}
}
