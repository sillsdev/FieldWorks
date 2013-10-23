// Copyright (c) 2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	public class SimpleActionHandler: IActionHandler
	{
		private List<List<IUndoAction>> m_UndoTasks = new List<List<IUndoAction>>();
		private List<IUndoAction> m_OpenTask;

		public IVwRootBox RootBox { get; set; }

		#region IActionHandler implementation

		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_OpenTask = new List<IUndoAction>();
		}

		public void EndUndoTask()
		{
			m_UndoTasks.Add(m_OpenTask);
			m_OpenTask = null;
		}

		public void ContinueUndoTask()
		{
			if (m_OpenTask == null && m_UndoTasks.Count > 0)
			{
				m_OpenTask = m_UndoTasks[m_UndoTasks.Count - 1];
				m_UndoTasks.Remove(m_OpenTask);
			}
			if (m_OpenTask == null)
				BeginUndoTask(null, null);
		}

		public void EndOuterUndoTask()
		{
			throw new NotImplementedException();
		}

		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			EndUndoTask();
			BeginUndoTask(bstrUndo, bstrRedo);
		}

		public void BeginNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		public void EndNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		public void CreateMarkIfNeeded(bool fCreateMark)
		{
			throw new NotImplementedException();
		}

		public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction _uact)
		{
			throw new NotImplementedException();
		}

		public void AddAction(IUndoAction act)
		{
			if (m_OpenTask == null)
				return;
			var cacheAction = act as UndoableRealDataCache.CacheUndoAction;
			if (cacheAction != null)
				cacheAction.RootBox = RootBox;
			m_OpenTask.Add(act);
		}

		public string GetUndoText()
		{
			throw new NotImplementedException();
		}

		public string GetUndoTextN(int iAct)
		{
			throw new NotImplementedException();
		}

		public string GetRedoText()
		{
			throw new NotImplementedException();
		}

		public string GetRedoTextN(int iAct)
		{
			throw new NotImplementedException();
		}

		public bool CanUndo()
		{
			return m_UndoTasks.Count > 0 || (m_OpenTask != null && m_OpenTask.Count > 0);
		}

		public bool CanRedo()
		{
			return false;
		}

		public UndoResult Undo()
		{
			if (m_UndoTasks.Count <= 0)
				throw new ApplicationException("No undo tasks");
			var actions = m_UndoTasks[m_UndoTasks.Count - 1];
			m_UndoTasks.Remove(actions);
			bool ok = true;
			foreach (var action in actions)
				ok = action.Undo();
			return ok ? UndoResult.kuresSuccess : UndoResult.kuresFailed;
		}

		public UndoResult Redo()
		{
			throw new NotImplementedException();
		}

		public void Rollback(int nDepth)
		{
			if (m_OpenTask == null)
				throw new ApplicationException("No open undo task");
			foreach (var action in m_OpenTask)
				action.Undo();
			m_OpenTask = null;
		}

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public void Close()
		{
			m_UndoTasks.Clear();
		}

		public int Mark()
		{
			throw new NotImplementedException();
		}

		public bool CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
		{
			throw new NotImplementedException();
		}

		public void DiscardToMark(int hMark)
		{
			throw new NotImplementedException();
		}

		public bool get_TasksSinceMark(bool fUndo)
		{
			throw new NotImplementedException();
		}

		public int CurrentDepth
		{
			get
			{
				return m_OpenTask == null ? 0 : 1;
			}
		}

		public int TopMarkHandle
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int UndoableActionCount
		{
			get
			{
				return m_UndoTasks[m_UndoTasks.Count - 1].Count;
			}
		}

		public int UndoableSequenceCount
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int RedoableSequenceCount
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IUndoGrouper UndoGrouper
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public bool IsUndoOrRedoInProgress
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool SuppressSelections
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion


	}

	/// <summary>
	/// Undoable real data cache.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "UseCorrectDisposeSignaturesRule",
		Justification = "Unit test")]
	public class UndoableRealDataCache : IRealDataCache
	{
		public enum ObjType
		{
			Object,
			BasicTsString,
			ExtendedTsString,
			ByteArray,
			String,
			Guid,
			Int,
			Long,
			Bool,
			Vector,
			Time,
		}

		public class CacheInfo
		{
			/// <summary/>
			public CacheInfo(ObjType type, int hvo, int flid, object obj)
			{
				Type = type;
				Hvo = hvo;
				Flid = flid;
				Object = obj;
			}

			/// <summary/>
			public CacheInfo(ObjType type, int hvo, int flid, int ws, object obj)
				: this(type, hvo, flid, obj)
			{
				Ws = ws;
			}

			/// <summary/>
			public ObjType Type { get; private set; }
			/// <summary/>
			public int Hvo { get; private set; }
			/// <summary/>
			public int Flid { get; private set; }
			/// <summary/>
			public int Ws { get; private set; }
			/// <summary/>
			public object Object { get; private set; }
		}

		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification = "m_Cache is a reference")]
		public class CacheUndoAction: IUndoAction
		{
			private IRealDataCache m_Cache;
			private CacheInfo m_Before;
			private CacheInfo m_After;

			/// <summary/>
			public CacheUndoAction(IRealDataCache cache, CacheInfo before,
				CacheInfo after)
			{
				m_Cache = cache;
				m_Before = before;
				m_After = after;
			}

			private void CacheObject(CacheInfo cacheInfo)
			{
				switch (cacheInfo.Type)
				{
					case ObjType.Object:
						m_Cache.CacheUnknown(cacheInfo.Hvo, cacheInfo.Flid, cacheInfo.Object);
						break;
					case ObjType.BasicTsString:
						m_Cache.CacheStringProp(cacheInfo.Hvo, cacheInfo.Flid, cacheInfo.Object as ITsString);
						break;
					case ObjType.ExtendedTsString:
						m_Cache.CacheStringAlt(cacheInfo.Hvo, cacheInfo.Flid, cacheInfo.Ws, cacheInfo.Object as ITsString);
						break;
					case ObjType.ByteArray:
						byte[] array = cacheInfo.Object as byte[];
						m_Cache.CacheBinaryProp(cacheInfo.Hvo, cacheInfo.Flid, array, array.Length);
						break;
					case ObjType.String:
						var str = cacheInfo.Object as string;
						m_Cache.CacheUnicodeProp(cacheInfo.Hvo, cacheInfo.Flid, str, str.Length);
						break;
					case ObjType.Guid:
						m_Cache.CacheGuidProp(cacheInfo.Hvo, cacheInfo.Flid, (Guid)cacheInfo.Object);
						break;
					case ObjType.Int:
						m_Cache.CacheIntProp(cacheInfo.Hvo, cacheInfo.Flid, (int)cacheInfo.Object);
						break;
					case ObjType.Long:
						m_Cache.CacheInt64Prop(cacheInfo.Hvo, cacheInfo.Flid, (long)cacheInfo.Object);
						break;
					case ObjType.Bool:
						m_Cache.CacheBooleanProp(cacheInfo.Hvo, cacheInfo.Flid, (bool)cacheInfo.Object);
						break;
					case ObjType.Vector:
						int[] vector = cacheInfo.Object as int[];
						m_Cache.CacheVecProp(cacheInfo.Hvo, cacheInfo.Flid, vector, vector.Length);
						break;
					case ObjType.Time:
						m_Cache.CacheTimeProp(cacheInfo.Hvo, cacheInfo.Flid, (long)cacheInfo.Object);
						break;
				}
			}

			public IVwRootBox RootBox { get; set; }

			#region IUndoAction implementation

			public bool Undo()
			{
				CacheObject(m_Before);
				RootBox.PropChanged(m_Before.Hvo, m_Before.Flid, 0, 1000, 1000);
				return true;
			}

			public bool Redo()
			{
				CacheObject(m_After);
				return true;
			}

			public void Commit()
			{
			}

			public bool IsDataChange
			{
				get { return true; }
			}

			public bool IsRedoable
			{
				get { return m_After != null; }
			}

			public bool SuppressNotification
			{
				set { }
			}

			#endregion
		}

		private RealDataCache m_cache;
		private bool m_isDirty;

		public UndoableRealDataCache()
		{
			m_cache = new RealDataCache();
		}

		private IActionHandler ActionHandler
		{
			get { return GetActionHandler(); }
		}

		private void MakeDirty()
		{
			m_isDirty = true;
		}


		/// <summary>
		/// Member CacheObjProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="val">val</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheObjProp(int obj, int tag, int val)
		{
			var before = new CacheInfo(ObjType.Object, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Object, obj, tag, val);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheObjProp(obj, tag, val);
		}

		/// <summary>
		/// Member SetObjProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="hvoObj">hvoObj</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			CacheObjProp(hvo, tag, hvoObj);
			MakeDirty();
		}

		/// <summary>
		/// Member get_ObjectProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public int get_ObjectProp(int hvo, int tag)
		{
			return m_cache.get_ObjectProp(hvo, tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the boolean prop.
		/// </summary>
		/// <param name="hvo">The hvo.</param><param name="tag">The tag.</param><param name="value">if set to <c>true</c> [value].</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheBooleanProp(int hvo, int tag, bool value)
		{
			var before = new CacheInfo(ObjType.Bool, hvo, tag, m_cache.get_Prop(hvo, tag));
			var after = new CacheInfo(ObjType.Bool, hvo, tag, value);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheBooleanProp(hvo, tag, value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member SetBoolean
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="n">n</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetBoolean(int hvo, int tag, bool n)
		{
			CacheBooleanProp(hvo, tag, n);
			MakeDirty();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member get_BooleanProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Boolean
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public bool get_BooleanProp(int hvo, int tag)
		{
			return m_cache.get_BooleanProp(hvo, tag);
		}

		/// <summary>
		/// Member CacheGuidProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="uid">uid</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheGuidProp(int obj, int tag, Guid uid)
		{
			var before = new CacheInfo(ObjType.Guid, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Guid, obj, tag, uid);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheGuidProp(obj, tag, uid);
		}

		/// <summary>
		/// Member SetGuid
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="uid">uid</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetGuid(int hvo, int tag, Guid uid)
		{
			CacheGuidProp(hvo, tag, uid);
			MakeDirty();
		}

		/// <summary>
		/// Member get_GuidProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Guid
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public Guid get_GuidProp(int hvo, int tag)
		{
			return m_cache.get_GuidProp(hvo, tag);
		}

		/// <summary>
		/// Member get_ObjFromGuid
		/// </summary>
		/// <param name="uid">uid</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public int get_ObjFromGuid(Guid uid)
		{
			return m_cache.get_ObjFromGuid(uid);
		}

		/// <summary>
		/// Member CacheIntProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="val">val</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheIntProp(int obj, int tag, int val)
		{
			var before = new CacheInfo(ObjType.Int, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Int, obj, tag, val);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheIntProp(obj, tag, val);
		}

		/// <summary>
		/// Member SetInt
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="n">n</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetInt(int hvo, int tag, int n)
		{
			CacheIntProp(hvo, tag, n);
			MakeDirty();
		}

		/// <summary>
		/// Member get_IntProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public int get_IntProp(int hvo, int tag)
		{
			return m_cache.get_IntProp(hvo, tag);
		}

		/// <summary>
		/// Method to retrieve a particular int property if it is in the cache,
		///             and return a bool to say whether it was or not.
		///             Similar to ISilDataAccess::get_IntProp, but this method
		///             is guaranteed not to do a lazy load of the property
		///             and it makes it easier for .Net clients to see whether the property was loaded,
		///             because this info is not hidden in an HRESULT.
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="isInCache">isInCache</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public int get_CachedIntProp(int obj, int tag, out bool isInCache)
		{
			return m_cache.get_CachedIntProp(obj, tag, out isInCache);
		}

		/// <summary>
		/// Member CacheUnicodeProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="val">val</param><param name="cch">cch</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheUnicodeProp(int obj, int tag, string val, int cch)
		{
			var before = new CacheInfo(ObjType.String, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.String, obj, tag, val);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheUnicodeProp(obj, tag, val, cch);
		}

		/// <summary>
		/// Member SetUnicode
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="rgch">rgch</param><param name="cch">cch</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetUnicode(int hvo, int tag, string rgch, int cch)
		{
			CacheUnicodeProp(hvo, tag, rgch, cch);
			MakeDirty();
		}

		/// <summary>
		/// Member set_UnicodeProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="bstr">bstr</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			CacheUnicodeProp(obj, tag, bstr, bstr.Length);
			MakeDirty();
		}

		/// <summary>
		/// Member get_UnicodeProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param>
		/// <returns>
		/// A System.String
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public string get_UnicodeProp(int obj, int tag)
		{
			return m_cache.get_UnicodeProp(obj, tag);
		}

		/// <summary>
		/// Member UnicodePropRgch
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="rgch">rgch</param><param name="cchMax">cchMax</param><param name="cch">cch</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void UnicodePropRgch(int obj, int tag, ArrayPtr rgch, int cchMax, out int cch)
		{
			m_cache.UnicodePropRgch(obj, tag, rgch, cchMax, out cch);
		}

		/// <summary>
		/// Member CacheTimeProp
		/// </summary>
		/// <param name="obj">hvo</param><param name="tag">tag</param><param name="val">val</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheTimeProp(int obj, int tag, long val)
		{
			var before = new CacheInfo(ObjType.Time, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Time, obj, tag, val);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheTimeProp(obj, tag, val);
		}

		/// <summary>
		/// Member SetTime
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="lln">lln</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetTime(int hvo, int tag, long lln)
		{
			CacheTimeProp(hvo, tag, lln);
			MakeDirty();
		}

		/// <summary>
		/// Member get_TimeProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Int64
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public long get_TimeProp(int hvo, int tag)
		{
			return m_cache.get_TimeProp(hvo, tag);
		}

		/// <summary>
		/// Member CacheInt64Prop
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="val">val</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheInt64Prop(int obj, int tag, long val)
		{
			var before = new CacheInfo(ObjType.Long, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Long, obj, tag, val);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheInt64Prop(obj, tag, val);
		}

		/// <summary>
		/// Member SetInt64
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="lln">lln</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetInt64(int hvo, int tag, long lln)
		{
			CacheInt64Prop(hvo, tag, lln);
			MakeDirty();
		}

		/// <summary>
		/// Member get_Int64Prop
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Int64
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public long get_Int64Prop(int hvo, int tag)
		{
			return m_cache.get_Int64Prop(hvo, tag);
		}

		/// <summary>
		/// Member CacheUnknown
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="unk">unk</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheUnknown(int obj, int tag, object unk)
		{
			var before = new CacheInfo(ObjType.Object, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Object, obj, tag, unk);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheUnknown(obj, tag, unk);
		}

		/// <summary>
		/// Member SetUnknown
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="unk">unk</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetUnknown(int hvo, int tag, object unk)
		{
			CacheUnknown(hvo, tag, unk);
			MakeDirty();
		}

		/// <summary>
		/// Member get_UnknownProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.IntPtr
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public object get_UnknownProp(int hvo, int tag)
		{
			return m_cache.get_UnknownProp(hvo, tag);
		}

		/// <summary>
		/// Member CacheBinaryProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="rgb">rgb</param><param name="cb">cb</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheBinaryProp(int obj, int tag, byte[] rgb, int cb)
		{
			var before = new CacheInfo(ObjType.ByteArray, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.ByteArray, obj, tag, rgb);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheBinaryProp(obj, tag, rgb, cb);
		}

		/// <summary>
		/// Member SetBinary
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="rgb">rgb</param><param name="cb">cb</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetBinary(int hvo, int tag, byte[] rgb, int cb)
		{
			CacheBinaryProp(hvo, tag, rgb, cb);
			MakeDirty();
		}

		/// <summary>
		/// Member BinaryPropRgb
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="rgb">rgb</param><param name="cbMax">cbMax</param><param name="cb">cb</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void BinaryPropRgb(int obj, int tag, ArrayPtr rgb, int cbMax, out int cb)
		{
			m_cache.BinaryPropRgb(obj, tag, rgb, cbMax, out cb);
		}

		/// <summary>
		/// Member CacheStringProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="tss">_tss</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheStringProp(int obj, int tag, ITsString tss)
		{
			var before = new CacheInfo(ObjType.BasicTsString, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.BasicTsString, obj, tag, tss);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheStringProp(obj, tag, tss);
		}

		/// <summary>
		/// Member SetString
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="tss">tss</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetString(int hvo, int tag, ITsString tss)
		{
			CacheStringProp(hvo, tag, tss);
			MakeDirty();
		}

		/// <summary>
		/// Member get_StringProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A ITsString
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public ITsString get_StringProp(int hvo, int tag)
		{
			return m_cache.get_StringProp(hvo, tag);
		}

		/// <summary>
		/// Member CacheStringAlt
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="ws">ws</param><param name="tss">tss</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheStringAlt(int obj, int tag, int ws, ITsString tss)
		{
			var before = new CacheInfo(ObjType.ExtendedTsString, obj, tag, ws,
				m_cache.get_IsPropInCache(obj, tag, (int)CellarPropertyType.MultiString, ws) ?
				m_cache.get_MultiStringAlt(obj, tag, ws) : null);
			var after =  new CacheInfo(ObjType.ExtendedTsString, obj, tag, ws, tss);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheStringAlt(obj, tag, ws, tss);
		}

		/// <summary>
		/// Member SetMultiStringAlt
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="ws">ws</param><param name="tss">tss</param>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			CacheStringAlt(hvo, tag, ws, tss);
			MakeDirty();
		}

		/// <summary>
		/// Member get_MultiStringAlt
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="ws">ws</param>
		/// <returns>
		/// A ITsString
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			return m_cache.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary>
		/// Member get_MultiStringProp
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A ITsMultiString
		/// </returns>
		/// <remarks>
		/// ISilDataAccess method
		/// </remarks>
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			return m_cache.get_MultiStringProp(hvo, tag);
		}

		/// <summary>
		/// Member CacheVecProp
		/// </summary>
		/// <param name="obj">obj</param><param name="tag">tag</param><param name="rghvo">rghvo</param><param name="chvo">chvo</param>
		public void CacheVecProp(int obj, int tag, int[] rghvo, int chvo)
		{
			var before = new CacheInfo(ObjType.Vector, obj, tag, m_cache.get_Prop(obj, tag));
			var after =  new CacheInfo(ObjType.Vector, obj, tag, rghvo);
			ActionHandler.AddAction(new CacheUndoAction(m_cache, before, after));

			m_cache.CacheVecProp(obj, tag, rghvo, chvo);
		}

		/// <summary>
		/// Get the full contents of the specified sequence in one go.
		/// </summary>
		/// <param name="obj">hvo</param><param name="tag">tag</param><param name="chvoMax">chvoMax</param><param name="chvo">chvo</param><param name="rghvo">rghvo</param>
		public void VecProp(int obj, int tag, int chvoMax, out int chvo, ArrayPtr rghvo)
		{
			m_cache.VecProp(obj, tag, chvoMax, out chvo, rghvo);
		}

		/// <summary>
		/// Member get_VecItem
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="index">index</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int get_VecItem(int hvo, int tag, int index)
		{
			return m_cache.get_VecItem(hvo, tag, index);
		}

		/// <summary>
		/// Member get_VecSize
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int get_VecSize(int hvo, int tag)
		{
			return m_cache.get_VecSize(hvo, tag);
		}

		/// <summary>
		/// Member get_VecSizeAssumeCached
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			return m_cache.get_VecSizeAssumeCached(hvo, tag);
		}

		/// <summary>
		/// Member GetObjIndex
		/// </summary>
		/// <param name="hvoOwn">hvoOwn</param><param name="flid">flid</param><param name="hvo">hvo</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			return m_cache.GetObjIndex(hvoOwn, flid, hvo);
		}

		/// <summary>
		/// Member CacheReplace
		/// </summary>
		/// <param name="hvoObj">hvoObj</param><param name="tag">tag</param><param name="ihvoMin">ihvoMin</param><param name="ihvoLim">ihvoLim</param><param name="rghvo">rghvo</param><param name="chvo">chvo</param>
		/// <remarks>
		/// IVwCacheDa method
		/// </remarks>
		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] rghvo, int chvo)
		{
			m_cache.CacheReplace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);
		}

		/// <summary>
		/// Member MoveOwnSeq
		/// </summary>
		/// <param name="hvoSrcOwner">hvoSrcOwner</param><param name="tagSrc">tagSrc</param><param name="ihvoStart">ihvoStart</param><param name="ihvoEnd">ihvoEnd</param><param name="hvoDstOwner">hvoDstOwner</param><param name="tagDst">tagDst</param><param name="ihvoDstStart">ihvoDstStart</param>
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner,
			int tagDst, int ihvoDstStart)
		{
			m_cache.MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary>
		/// Member MoveOwn
		/// </summary>
		/// <param name="hvoSrcOwner">hvoSrcOwner</param><param name="tagSrc">tagSrc</param><param name="hvo">hvo</param><param name="hvoDstOwner">hvoDstOwner</param><param name="tagDst">tagDst</param><param name="ihvoDstStart">ihvoDstStart</param>
		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			m_cache.MoveOwn(hvoSrcOwner, tagSrc, hvo, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary>
		/// Member Replace
		/// </summary>
		/// <param name="hvoObj">hvoObj</param><param name="tag">tag</param><param name="ihvoMin">ihvoMin</param><param name="ihvoLim">ihvoLim</param><param name="rghvo">rghvo</param><param name="chvo">chvo</param>
		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] rghvo, int chvo)
		{
			m_cache.Replace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);
		}

		/// <summary>
		/// Member BeginNonUndoableTask
		/// </summary>
		public void BeginNonUndoableTask()
		{
			m_cache.BeginNonUndoableTask();
		}

		/// <summary>
		/// Member EndNonUndoableTask
		/// </summary>
		public void EndNonUndoableTask()
		{
			m_cache.EndNonUndoableTask();
		}

		/// <summary>
		/// Member BeginUndoTask
		/// </summary>
		/// <param name="bstrUndo">bstrUndo</param><param name="bstrRedo">bstrRedo</param>
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_cache.BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		/// Member EndUndoTask
		/// </summary>
		public void EndUndoTask()
		{
			m_cache.EndUndoTask();
		}

		/// <summary>
		/// Member ContinueUndoTask
		/// </summary>
		public void ContinueUndoTask()
		{
			m_cache.ContinueUndoTask();
		}

		/// <summary>
		/// Member EndOuterUndoTask
		/// </summary>
		public void EndOuterUndoTask()
		{
			m_cache.EndOuterUndoTask();
		}

		/// <summary>
		/// Member BreakUndoTask
		/// </summary>
		/// <param name="bstrUndo">bstrUndo</param><param name="bstrRedo">bstrRedo</param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			m_cache.BreakUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		/// Member Rollback
		/// </summary>
		public void Rollback()
		{
			m_cache.Rollback();
		}

		/// <summary>
		/// Member GetActionHandler
		/// </summary>
		/// <returns>
		/// A IActionHandler
		/// </returns>
		public IActionHandler GetActionHandler()
		{
			return m_cache.GetActionHandler();
		}

		/// <summary>
		/// Member SetActionHandler
		/// </summary>
		/// <param name="actionhandler">action handler</param>
		public void SetActionHandler(IActionHandler actionhandler)
		{
			m_cache.SetActionHandler(actionhandler);
		}

		/// <summary>
		/// Member InsertRelExtra
		/// </summary>
		/// <param name="hvoSrc">hvoSrc</param><param name="tag">tag</param><param name="ihvo">ihvo</param><param name="hvoDst">hvoDst</param><param name="bstrExtra">bstrExtra</param>
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			m_cache.InsertRelExtra(hvoSrc, tag, ihvo, hvoDst, bstrExtra);
		}

		/// <summary>
		/// Member UpdateRelExtra
		/// </summary>
		/// <param name="hvoSrc">hvoSrc</param><param name="tag">tag</param><param name="ihvo">ihvo</param><param name="bstrExtra">bstrExtra</param>
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			m_cache.UpdateRelExtra(hvoSrc, tag, ihvo, bstrExtra);
		}

		/// <summary>
		/// Member GetRelExtra
		/// </summary>
		/// <param name="hvoSrc">hvoSrc</param><param name="tag">tag</param><param name="ihvo">ihvo</param>
		/// <returns>
		/// A System.String
		/// </returns>
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			return m_cache.GetRelExtra(hvoSrc, tag, ihvo);
		}

		/// <summary>
		/// Member get_Prop
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param>
		/// <returns>
		/// A System.Object
		/// </returns>
		public object get_Prop(int hvo, int tag)
		{
			return m_cache.get_Prop(hvo, tag);
		}

		/// <summary>
		/// Member get_IsPropInCache
		/// </summary>
		/// <param name="hvo">hvo</param><param name="tag">tag</param><param name="cpt">cpt</param><param name="ws">ws</param>
		/// <returns>
		/// A System.Boolean
		/// </returns>
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			return m_cache.get_IsPropInCache(hvo, tag, cpt, ws);
		}

		/// <summary>
		/// Member DeleteObj
		/// </summary>
		/// <param name="hvoObj">hvoObj</param>
		public void DeleteObj(int hvoObj)
		{
			m_cache.DeleteObj(hvoObj);
		}

		/// <summary>
		/// Member DeleteObjOwner
		/// </summary>
		/// <param name="hvoOwner">hvoOwner</param><param name="hvoObj">hvoObj</param><param name="tag">tag</param><param name="ihvo">ihvo</param>
		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			m_cache.DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
		}

		/// <summary>
		/// Member InsertNew
		/// </summary>
		/// <param name="hvoObj">hvoObj</param><param name="tag">tag</param><param name="ihvo">ihvo</param><param name="chvo">chvo</param><param name="_ss">_ss</param>
		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			m_cache.InsertNew(hvoObj, tag, ihvo, chvo, _ss);
		}

		/// <summary>
		/// Member MakeNewObject
		/// </summary>
		/// <param name="clid">clid</param><param name="hvoOwner">hvoOwner</param><param name="tag">tag</param><param name="ord">ord</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			return m_cache.MakeNewObject(clid, hvoOwner, tag, ord);
		}

		/// <summary>
		/// Member get_IsValidObject
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <returns>
		/// A System.Boolean
		/// </returns>
		public bool get_IsValidObject(int hvo)
		{
			return m_cache.get_IsValidObject(hvo);
		}

		/// <summary>
		/// Member get_IsDummyId
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <returns>
		/// A System.Boolean
		/// </returns>
		public bool get_IsDummyId(int hvo)
		{
			return m_cache.get_IsDummyId(hvo);
		}

		/// <summary>
		/// Member RemoveObjRefs
		/// </summary>
		/// <param name="hvo">hvo</param>
		public void RemoveObjRefs(int hvo)
		{
			m_cache.RemoveObjRefs(hvo);
		}

		/// <summary>
		/// Member AddNotification
		/// </summary>
		/// <param name="_nchng">_nchng</param>
		public void AddNotification(IVwNotifyChange _nchng)
		{
			m_cache.AddNotification(_nchng);
		}

		/// <summary>
		/// Member PropChanged
		/// </summary>
		/// <param name="_nchng">_nchng</param><param name="_ct">_ct</param><param name="hvo">hvo</param><param name="tag">tag</param><param name="ivMin">ivMin</param><param name="cvIns">cvIns</param><param name="cvDel">cvDel</param>
		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns,
			int cvDel)
		{
			m_cache.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
		}

		/// <summary>
		/// Member RemoveNotification
		/// </summary>
		/// <param name="_nchng">_nchng</param>
		public void RemoveNotification(IVwNotifyChange _nchng)
		{
			m_cache.RemoveNotification(_nchng);
		}

		/// <summary>
		/// Member GetDisplayIndex
		/// </summary>
		/// <param name="hvoOwn">hvoOwn</param><param name="flid">flid</param><param name="ihvo">ihvo</param>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			return m_cache.GetDisplayIndex(hvoOwn, flid, ihvo);
		}

		/// <summary>
		/// So far we haven't needed this for the purposes of CacheLight...it's used for multi-paragraph (and eventually drag/drop) editing.
		/// </summary>
		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst,
			int flidDst, int wsDst, int ichDest, bool fDstIsNew)
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

		/// <summary>
		/// Member IsDirty
		/// </summary>
		/// <returns>
		/// A System.Boolean
		/// </returns>
		public bool IsDirty()
		{
			return m_isDirty;
		}

		/// <summary>
		/// Member ClearDirty
		/// </summary>
		public void ClearDirty()
		{
			m_isDirty = false;
		}

		/// <summary>
		/// Member InstallVirtual
		/// </summary>
		/// <param name="vh">vh</param>
		public void InstallVirtual(IVwVirtualHandler vh)
		{
			m_cache.InstallVirtual(vh);
		}

		/// <summary>
		/// Member GetVirtualHandlerId
		/// </summary>
		/// <param name="tag">tag</param>
		/// <returns>
		/// A IVwVirtualHandler
		/// </returns>
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			return m_cache.GetVirtualHandlerId(tag);
		}

		/// <summary>
		/// Member GetVirtualHandlerName
		/// </summary>
		/// <param name="bstrClass">bstrClass</param><param name="bstrField">bstrField</param>
		/// <returns>
		/// A IVwVirtualHandler
		/// </returns>
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			return m_cache.GetVirtualHandlerName(bstrClass, bstrField);
		}

		/// <summary>
		/// Member ClearVirtualProperties
		/// </summary>
		public void ClearVirtualProperties()
		{
			m_cache.ClearVirtualProperties();
		}

		/// <summary>
		/// Member ClearInfoAbout
		/// </summary>
		/// <param name="hvo">hvo</param><param name="cia">cia</param>
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			m_cache.ClearInfoAbout(hvo, cia);
		}

		/// <summary>
		/// Member ClearInfoAbout
		/// </summary>
		/// <param name="rghvo">hvo</param><param name="chvo"/><param name="cia">cia</param>
		public void ClearInfoAboutAll(int[] rghvo, int chvo, VwClearInfoAction cia)
		{
			m_cache.ClearInfoAboutAll(rghvo, chvo, cia);
		}

		/// <summary>
		/// Member ClearAllData
		/// </summary>
		public void ClearAllData()
		{
			m_cache.ClearAllData();
		}

		/// <summary>
		/// Member GetOutlineNumber
		/// </summary>
		/// <param name="hvo">hvo</param><param name="flid">flid</param><param name="fFinPer">fFinPer</param>
		/// <returns>
		/// A System.String
		/// </returns>
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			return m_cache.GetOutlineNumber(hvo, flid, fFinPer);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		///             All public Properties and Methods should call this
		///             before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			m_cache.CheckDisposed();
		}

		/// <summary/>
		/// <remarks>
		/// Must not be virtual.
		/// </remarks>
		public void Dispose()
		{
			m_cache.Dispose();
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

		/// <summary>
		/// Member get_WritingSystemFactory
		/// </summary>
		/// <returns>
		/// A ILgWritingSystemFactory
		/// </returns>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_cache.WritingSystemFactory; }
			set { m_cache.WritingSystemFactory = value; }
		}

		/// <summary>
		/// Member get_MetaDataCache
		/// </summary>
		/// <returns>
		/// A IFwMetaDataCache
		/// </returns>
		public IFwMetaDataCache MetaDataCache
		{
			get { return m_cache.MetaDataCache; }
			set { m_cache.MetaDataCache = value; }
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_cache.IsDisposed; }
		}
	}
}
