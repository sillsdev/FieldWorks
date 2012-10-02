using System;
using System.Collections.Generic;
using System.Runtime.InteropServices; // needed for Marshal
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Diagnostics;
using SIL.CoreImpl;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary></summary>
	public sealed class RealDataCache : ISilDataAccess, IVwCacheDa, IStructuredTextDataAccess, IFWDisposable
	{
		#region Basic implementation

		#region Data members

		private bool m_checkWithMDC = true;
		private int m_nextHvo = 1;
		private bool m_isDirty;
		private IFwMetaDataCache m_metaDataCache;
		private IActionHandler m_actionhandler;
		private ILgWritingSystemFactory m_lgWritingSystemFactory;

		// Field ids that need to be set if a test uses them--e.g. if it calls ReplaceWithTsString
		private int m_paraContentsFlid;
		private int m_paraPropertiesFlid;
		private int m_textParagraphsFlid;

		/// <summary>
		/// Cache for storing all class ids.
		/// This is an optimization, so we don;t have to ask the MDC all the time for all valids class ids.
		/// </summary>
		private readonly List<int> m_clids = new List<int>();

		#region Dictionary caches.

		/// <summary></summary>
		private Dictionary<HvoFlidKey, object> m_basicObjectCache = new Dictionary<HvoFlidKey, object>();
		/// <summary></summary>
		private Dictionary<HvoFlidWSKey, ITsString> m_extendedKeyCache = new Dictionary<HvoFlidWSKey, ITsString>();
		/// <summary></summary>
		private Dictionary<HvoFlidKey, ITsString> m_basicITsStringCache = new Dictionary<HvoFlidKey, ITsString>();
		/// <summary></summary>
		private Dictionary<HvoFlidKey, byte[]> m_basicByteArrayCache = new Dictionary<HvoFlidKey, byte[]>();
		/// <summary></summary>
		private readonly Dictionary<HvoFlidKey, string> m_basicStringCache = new Dictionary<HvoFlidKey, string>();
		/// <summary>
		/// This is for storing Guids.
		/// </summary>
		private Dictionary<HvoFlidKey, Guid> m_guidCache = new Dictionary<HvoFlidKey, Guid>();
		/// <summary>
		/// This is for storing ids with a quick lookup using the Guid as the key.
		/// </summary>
		private Dictionary<Guid, int> m_guidToHvo = new Dictionary<Guid, int>();
		/// <summary>
		/// This is for storing int property values.
		/// </summary>
		private Dictionary<HvoFlidKey, int> m_intCache = new Dictionary<HvoFlidKey, int>();
		/// <summary>
		/// This is for storing long property values.
		/// This will be for Int64 and Time data
		/// </summary>
		private Dictionary<HvoFlidKey, long> m_longCache = new Dictionary<HvoFlidKey, long>();
		/// <summary>
		/// This is for storing boolean property values.
		/// </summary>
		private Dictionary<HvoFlidKey, bool> m_boolCache = new Dictionary<HvoFlidKey, bool>();
		/// <summary>
		/// This is for storing vector property values (sequence or collection properties).
		/// </summary>
		private Dictionary<HvoFlidKey, List<int>> m_vectorCache = new Dictionary<HvoFlidKey, List<int>>();
		/// <summary></summary>
		private Dictionary<int, long> m_timeStampCache = new Dictionary<int, long>();

		#endregion Dictionary caches.

		#endregion Data members

		#region Properties

		/// <summary>
		/// Get the next available Hvo. It automatically increments with each call.
		/// </summary>
		internal int NextHvo
		{
			get
			{
				CheckDisposed();
				return m_nextHvo++;
			}
		}

		/// <summary>
		/// Normally, the MDC should check the MDC to see that values are legal.
		/// When loading the XML file, we can skip the check, since the DTD says it is legal.
		/// This is a risk, but an acceptable one.
		/// </summary>
		internal bool CheckWithMDC
		{
			set
			{
				CheckDisposed();
				m_checkWithMDC = value;
			}
			get
			{
				CheckDisposed();
				return m_checkWithMDC;
			}
		}

		#endregion Properties

		#region Construction and Initialization

		#endregion Construction and Initialization

		#region Other methods

		private void MakeDirty(int hvo)
		{
			m_isDirty = true;
			m_timeStampCache[hvo] = DateTime.Now.Ticks;
		}

		private bool CheckForVirtual(int hvo, int tag)
		{
			// It may or may not be a virtual property,
			// so be safe and hace client understand to leave the value in the cache.
			var removeFromCache = false;

			var vh = GetVirtualHandlerId(tag);
			if (vh != null)
			{
				// It may be a ComputeEveryTime virtual property,
				// or it may be the first time for a load once virtual handler.
				vh.Load(hvo, tag, 0, this);
				removeFromCache = vh.ComputeEveryTime;
			}

			return removeFromCache;
		}

		private void CheckHvoTagMatch(int hvo, int tag)
		{
			if (!m_checkWithMDC)
				return;

			// NB: This will throw an exception, if the class hasn't already been put into the cache.
			// But then, CheckBasics should already have been called,
			// which would have thrown it before.
			var clid = get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			// First find out how many there are.
			var countAllFlidsOut = MetaDataCache.GetFields(clid, true,
				(int)CellarPropertyTypeFilter.All, 0, null);
			// Now get them for real.
			using (var flids = MarshalEx.ArrayToNative(countAllFlidsOut, typeof(int)))
			{
				var foundFlid = false;
				countAllFlidsOut = MetaDataCache.GetFields(clid, true,
					(int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				var flids1 = (int[])MarshalEx.NativeToArray(flids, countAllFlidsOut, typeof(int));
				for (var i = 0; i < flids1.Length; ++i)
				{
					var flid = flids1[i];
					if (flid != tag) continue;

					foundFlid = true;
					break;
				}
				if (!foundFlid)
					throw new ArgumentException(String.Format("Invalid 'tag' ({0}) for 'hvo' ({1})", tag, hvo));
			}
		}

		/// <summary>
		/// Makes sure the mdc exists.
		/// </summary>
		/// <exception cref="ApplicationException">Thrown when the 'MetaDataCache' property is null.</exception>
		private void CheckForMetaDataCache()
		{
			if (m_metaDataCache == null)
				throw new ApplicationException("The 'MetaDataCache' property must be set, before using the cache.");
		}

		/// <summary>
		/// See if the given 'hvo' is valid.
		/// </summary>
		/// <param name="hvo"></param>
		private void CheckBasics(int hvo)
		{
			CheckForMetaDataCache();
			if (!m_checkWithMDC)
				return;
			if (!get_IsValidObject(hvo))
				throw new ArgumentException("Invalid 'hvo'.");
		}

		private int GetFromIntCache(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_intCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var val = m_intCache[key];
			if (removeFromCache)
				m_intCache.Remove(key);
			return val;
		}

		private void AddToIntCache(int obj, int tag, int val, bool isIntType)
		{
			if (tag == (int)CmObjectFields.kflidCmObject_Class)
			{
				CheckForMetaDataCache();
				if (obj == 0)
					throw new ArgumentException("Hvo cannot be zero.");
			}
			else
			{
				CheckBasics(obj);
			}

			if (isIntType)
			{
				// Ordinary integer data.
				switch (tag)
				{
					case (int)CmObjectFields.kflidCmObject_Class:
						// Make sure 'val' is a valid clid in the metadata cache.
						if (m_clids.Count == 0)
						{
							var countAllClasses = MetaDataCache.ClassCount;
							using (var clids = MarshalEx.ArrayToNative(countAllClasses, typeof(int)))
							{
								MetaDataCache.GetClassIds(countAllClasses, clids);
								var uIds = (int[])MarshalEx.NativeToArray(clids, countAllClasses, typeof(int));
								m_clids.AddRange(uIds);
							}
						}
						var isValidClid = m_clids.Contains(val);
						if (!isValidClid)
							throw new ArgumentException(String.Format("The given class id of '{0}' is not valid in the model.", val));
						break;
					default:
						// Make sure the hvo has the given tag.
						CheckHvoTagMatch(obj, tag);
						// Make sure an int is legal for the given tag.
						var flidType = MetaDataCache.GetFieldType(tag);
						if (flidType != (int)CellarPropertyType.Integer)
							throw new ArgumentException(String.Format("Can only put integers in the tag/flid '{0}'.", tag));
						break;
				}
			}
			else
			{
				// It must be an atomic reference property, including owner.
				// Check that the tag is valid for the given hvo.
				switch (tag)
				{
					// All objects have this tag.
					case (int)CmObjectFields.kflidCmObject_Owner:
						break;
					default:
						CheckHvoTagMatch(obj, tag);
						break;
				}

				var clid = get_IntProp(val, (int)CmObjectFields.kflidCmObject_Class);
				if (!MetaDataCache.get_IsValidClass(tag, clid))
					throw new ArgumentException(String.Format("Can't put the 'val' ({0}) into the 'tag' ({1}).", val, tag));
			}

			// Made it here, so set it.
			m_intCache[new HvoFlidKey(obj, tag)] = val;
		}

		private long GetFromLongCache(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_longCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var val = m_longCache[key];
			if (removeFromCache)
				m_longCache.Remove(key);
			return val;
		}

		private void AddToLongCache(int obj, int tag, long val, CellarPropertyType flidType)
		{
			CheckBasics(obj);

			// Make sure the hvo has the given tag.
			CheckHvoTagMatch(obj, tag);
			// Make sure a long is legal for the given tag.
			CellarPropertyType tagType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if (tagType != flidType)
				throw new ArgumentException(String.Format("Can only put long integers in the tag/flid '{0}'.", tag));
			m_longCache[new HvoFlidKey(obj, tag)] = val;
		}

		#endregion Other methods

		#endregion Basic implementation

		#region Interface implementations

		#region ISilDataAccess/IVwCacheDa implementation (Cache/Set/Get)

		#region Object Prop methods (DONE)

		/// <summary>Member CacheObjProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>val</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheObjProp(int obj, int tag, int val)
		{
			CheckDisposed();

			AddToIntCache(obj, tag, val, false);
		}

		/// <summary>Member SetObjProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='hvoObj'>hvoObj</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			CheckDisposed();

			CacheObjProp(hvo, tag, hvoObj);
			MakeDirty(hvo);
		}

		/// <summary>Member get_ObjectProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Int32</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public int get_ObjectProp(int hvo, int tag)
		{
			CheckDisposed();

			return GetFromIntCache(hvo, tag);
		}
		#endregion Object Prop methods

		#region Boolean methods (DONE)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the boolean prop.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="value">if set to <c>true</c> [value].</param>
		/// <remarks>IVwCacheDa method</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheBooleanProp(int hvo, int tag, bool value)
		{
			CheckDisposed();

			CheckBasics(hvo);

			// Make sure the hvo has the given tag.
			CheckHvoTagMatch(hvo, tag);
			// Make sure an boolean is legal for the given tag.
			var flidType = MetaDataCache.GetFieldType(tag);
			if (flidType != (int)CellarPropertyType.Boolean)
				throw new ArgumentException(String.Format("Can only put booleans in the tag/flid '{0}'.", tag));

			// Made it here, so set it.
			m_boolCache[new HvoFlidKey(hvo, tag)] = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member SetBoolean
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="n">n</param>
		/// <remarks>ISilDataAccess method</remarks>
		/// ------------------------------------------------------------------------------------
		public void SetBoolean(int hvo, int tag, bool n)
		{
			CheckDisposed();

			CacheBooleanProp(hvo, tag, n);
			MakeDirty(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member get_BooleanProp
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <returns>A System.Boolean</returns>
		/// <remarks>ISilDataAccess method</remarks>
		/// ------------------------------------------------------------------------------------
		public bool get_BooleanProp(int hvo, int tag)
		{
			CheckDisposed();

			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_boolCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var val = m_boolCache[key];
			if (removeFromCache)
				m_boolCache.Remove(key);
			return val;
		}

		#endregion Boolean methods

		#region Guid methods (DONE)

		/// <summary>Member CacheGuidProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='uid'>uid</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheGuidProp(int obj, int tag, Guid uid)
		{
			CheckDisposed();

			CheckBasics(obj);
			switch (tag)
			{
				case (int)CmObjectFields.kflidCmObject_Guid:
					// Also put it into the reverse cache.
					m_guidToHvo[uid] = obj;
					break;
				default:
					// Make sure the obj has the given tag.
					CheckHvoTagMatch(obj, tag);
					// Make sure a Guid is legal for the given tag.
					var flidType = MetaDataCache.GetFieldType(tag);
					if (flidType != (int)CellarPropertyType.Guid)
						throw new ArgumentException(String.Format("Can only put Guids in the tag/flid '{0}'.", tag));
					break;
			}
			// Made it here, so set it.
			m_guidCache[new HvoFlidKey(obj, tag)] = uid;
		}

		/// <summary>Member SetGuid</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='uid'>uid</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetGuid(int hvo, int tag, Guid uid)
		{
			CheckDisposed();

			CacheGuidProp(hvo, tag, uid);
			MakeDirty(hvo);
		}

		/// <summary>Member get_GuidProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Guid</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public Guid get_GuidProp(int hvo, int tag)
		{
			CheckDisposed();

			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_guidCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var val = m_guidCache[key];
			if (removeFromCache)
				m_guidCache.Remove(key);
			return val;
		}

		/// <summary>Member get_ObjFromGuid</summary>
		/// <param name='uid'>uid</param>
		/// <returns>A System.Int32</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public int get_ObjFromGuid(Guid uid)
		{
			CheckDisposed();

			return m_guidToHvo[uid];
		}

		#endregion Guid methods

		#region Int methods (DONE)

		/// <summary>Member CacheIntProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>val</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheIntProp(int obj, int tag, int val)
		{
			CheckDisposed();

			AddToIntCache(obj, tag, val, true);
		}

		/// <summary>Member SetInt</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='n'>n</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetInt(int hvo, int tag, int n)
		{
			CheckDisposed();

			CacheIntProp(hvo, tag, n);
			MakeDirty(hvo);
		}

		/// <summary>Member get_IntProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Int32</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public int get_IntProp(int hvo, int tag)
		{
			CheckDisposed();

			return GetFromIntCache(hvo, tag);
		}

		/// <summary>
		/// Method to retrieve a particular int property if it is in the cache,
		/// and return a bool to say whether it was or not.
		/// Similar to ISilDataAccess::get_IntProp, but this method
		/// is guaranteed not to do a lazy load of the property
		/// and it makes it easier for .Net clients to see whether the property was loaded,
		/// because this info is not hidden in an HRESULT.
		/// </summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='isInCache'>isInCache</param>
		/// <returns>A System.Int32</returns>
		/// <remarks>IVwCacheDa method</remarks>
		public int get_CachedIntProp(int obj, int tag, out bool isInCache)
		{
			CheckDisposed();

			var val = 0;
			isInCache = true;
			try
			{
				val = get_IntProp(obj, tag);
			}
			catch (KeyNotFoundException)
			{
				// We'll take it to mean it isn't in the cache.
				// Eat this exception.
				isInCache = false;
			}
			return val;
		}

		#endregion Int methods

		#region Unicode methods (DONE)

		/// <summary>Member CacheUnicodeProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>val</param>
		/// <param name='cch'>cch</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheUnicodeProp(int obj, int tag, string val, int cch)
		{
			CheckDisposed();

			CheckBasics(obj);
			if (val.Length != cch)
				throw new ArgumentException("Input string not the right length.");
			CheckHvoTagMatch(obj, tag);
			// Make sure Unicode is legal for the given tag.
			var flidType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if ((flidType != CellarPropertyType.Unicode) && (flidType != CellarPropertyType.BigUnicode))
				throw new ArgumentException(String.Format("Can only put Unicode data in the tag/flid '{0}'.", tag));

			m_basicStringCache[new HvoFlidKey(obj, tag)] = val;
		}

		/// <summary>Member SetUnicode</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgch'>rgch</param>
		/// <param name='cch'>cch</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetUnicode(int hvo, int tag, string rgch, int cch)
		{
			CheckDisposed();

			CacheUnicodeProp(hvo, tag, rgch, cch);
			MakeDirty(hvo);
		}

		/// <summary>Member set_UnicodeProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='bstr'>bstr</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			CheckDisposed();

			SetUnicode(obj, tag, bstr, bstr.Length);
		}

		/// <summary>Member get_UnicodeProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.String</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public string get_UnicodeProp(int obj, int tag)
		{
			CheckDisposed();

			CheckBasics(obj);

			var key = new HvoFlidKey(obj, tag);
			var removeFromCache = false;
			if (!m_basicStringCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(obj, tag);
			var val = m_basicStringCache[key];
			if (removeFromCache)
				m_basicStringCache.Remove(key);
			return val;
		}

		/// <summary>Member UnicodePropRgch</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgch'>rgch</param>
		/// <param name='cchMax'>cchMax</param>
		/// <param name='cch'>cch</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void UnicodePropRgch(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*OLECHAR[]*/ rgch, int cchMax, out int cch)
		{
			CheckDisposed();

			CheckBasics(obj);

			var str = get_UnicodeProp(obj, tag);
			cch = str.Length;
			if (cchMax == 0)
				return;
			if (cch >= cchMax)
				throw new ArgumentException("cch cannot be larger than cchMax");

			MarshalEx.StringToNative(rgch, cchMax, str, true);
		}

		#endregion Unicode methods

		#region Time methods (DONE)

		/// <summary>Member CacheTimeProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>val</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheTimeProp(int hvo, int tag, long val)
		{
			CheckDisposed();

			AddToLongCache(hvo, tag, val, CellarPropertyType.Time);
		}

		/// <summary>Member SetTime</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='lln'>lln</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetTime(int hvo, int tag, long lln)
		{
			CheckDisposed();

			CacheTimeProp(hvo, tag, lln);
			MakeDirty(hvo);
		}

		/// <summary>Member get_TimeProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Int64</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public long get_TimeProp(int hvo, int tag)
		{
			CheckDisposed();

			return GetFromLongCache(hvo, tag);
		}

		#endregion Time methods

		#region Int64 methods (DONE)

		/// <summary>Member CacheInt64Prop</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>val</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheInt64Prop(int obj, int tag, long val)
		{
			CheckDisposed();

			AddToLongCache(obj, tag, val, CellarPropertyType.GenDate);
		}

		/// <summary>Member SetInt64</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='lln'>lln</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetInt64(int hvo, int tag, long lln)
		{
			CheckDisposed();

			CacheInt64Prop(hvo, tag, lln);
			MakeDirty(hvo);
		}

		/// <summary>Member get_Int64Prop</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Int64</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public long get_Int64Prop(int hvo, int tag)
		{
			CheckDisposed();

			return GetFromLongCache(hvo, tag);
		}

		#endregion Int64 methods

		#region Unknown methods (DONE)

		/// <summary>Member CacheUnknown</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='unk'>unk</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheUnknown(int obj, int tag, [MarshalAs(UnmanagedType.IUnknown)] object unk)
		{
			CheckDisposed();

			if (!(unk is ITsTextProps))
				throw new ArgumentException("Only ITsTextProps COM interfaces are supported in the cache.");

			CheckBasics(obj);
			CheckHvoTagMatch(obj, tag);
			// Make sure Binary is legal for the given tag.
			int flidType = MetaDataCache.GetFieldType(tag);
			if (flidType != (int)CellarPropertyType.Binary)
				throw new ArgumentException(String.Format("Can only put IUnknown data in the tag/flid '{0}' as Binary data.", tag));

			m_basicObjectCache[new HvoFlidKey(obj, tag)] = unk;
		}

		/// <summary>Member SetUnknown</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='unk'>unk</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetUnknown(int hvo, int tag, [MarshalAs(UnmanagedType.IUnknown)] object unk)
		{
			CheckDisposed();

			CacheUnknown(hvo, tag, unk);
			MakeDirty(hvo);
		}

		/// <summary>Member get_UnknownProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.IntPtr</returns>
		/// <remarks>ISilDataAccess method</remarks>
		[return: MarshalAs(UnmanagedType.IUnknown)]
		public object get_UnknownProp(int hvo, int tag)
		{
			CheckDisposed();

			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_basicObjectCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var val = m_basicObjectCache[key];
			if (removeFromCache)
				m_basicObjectCache.Remove(key);
			return val;
		}

		#endregion Unknown methods

		#region Binary methods (DONE)

		/// <summary>Member CacheBinaryProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgb'>rgb</param>
		/// <param name='cb'>cb</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheBinaryProp(int obj, int tag, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] Byte[] rgb, int cb)
		{
			CheckDisposed();

			CheckBasics(obj);
			if (rgb.Length != cb)
				throw new ArgumentException("Binary input not the right length.");

			CheckHvoTagMatch(obj, tag);
			// Make sure Binary is legal for the given tag.
			var flidType = MetaDataCache.GetFieldType(tag);
			if (flidType != (int)CellarPropertyType.Binary)
				throw new ArgumentException(String.Format("Can only put Binary data in the tag/flid '{0}'.", tag));

			m_basicByteArrayCache[new HvoFlidKey(obj, tag)] = rgb;
		}

		/// <summary>Member SetBinary</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgb'>rgb</param>
		/// <param name='cb'>cb</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetBinary(int hvo, int tag, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] Byte[] rgb, int cb)
		{
			CheckDisposed();

			CacheBinaryProp(hvo, tag, rgb, cb);
			MakeDirty(hvo);
		}

		/// <summary>Member BinaryPropRgb</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgb'>rgb</param>
		/// <param name='cbMax'>cbMax</param>
		/// <param name='cb'>cb</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void BinaryPropRgb(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*byte[]*/ rgb, int cbMax, out int cb)
		{
			CheckDisposed();

			CheckBasics(obj);

			var key = new HvoFlidKey(obj, tag);
			var removeFromCache = false;
			if (!m_basicByteArrayCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(obj, tag);
			var array = m_basicByteArrayCache[key];
			if (removeFromCache)
				m_basicByteArrayCache.Remove(key);
			cb = array.Length;
			if (cbMax == 0)
				return;
			if (cb > cbMax)
				throw new ArgumentException("cb cannot be larger than cbMax");

			MarshalEx.ArrayToNative(rgb, cbMax, array);
		}

		#endregion Binary methods

		#region String methods (DONE)

		/// <summary>Member CacheStringProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='tss'>_tss</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheStringProp(int obj, int tag, ITsString tss)
		{
			CheckDisposed();

			CheckBasics(obj);
			CheckHvoTagMatch(obj, tag);
			// Make sure Unicode is legal for the given tag.
			var flidType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if ((flidType != CellarPropertyType.BigString) && (flidType != CellarPropertyType.String))
				throw new ArgumentException(String.Format("Can only put String data in the tag/flid '{0}'.", tag));

			m_basicITsStringCache[new HvoFlidKey(obj, tag)] = tss;
		}

		/// <summary>Member SetString</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='tss'>tss</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetString(int hvo, int tag, ITsString tss)
		{
			CheckDisposed();

			CacheStringProp(hvo, tag, tss);
			MakeDirty(hvo);
		}

		/// <summary>Member get_StringProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A ITsString</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public ITsString get_StringProp(int hvo, int tag)
		{
			CheckDisposed();

			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_basicITsStringCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var val = m_basicITsStringCache[key];
			if (removeFromCache)
				m_basicITsStringCache.Remove(key);
			return val;
		}

		#endregion String methods

		#region MultiString/MultiUnicode methods (DONE)

		/// <summary>Member CacheStringAlt</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='ws'>ws</param>
		/// <param name='tss'>tss</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheStringAlt(int obj, int tag, int ws, ITsString tss)
		{
			CheckDisposed();

			CheckBasics(obj);
			// Make sure ws is not for a magic ws.
			// Magic WSes are all negative
			if (ws < 0)
				throw new ArgumentException("Magic writing system invalid in string.");
			if (ws == 0)
				throw new ArgumentException("Writing system of zero is invalid in string.");

			CheckHvoTagMatch(obj, tag);
			// Make sure ITsString is legal for the given tag.
			var flidType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if ((flidType == CellarPropertyType.MultiUnicode) ||
				(flidType == CellarPropertyType.MultiBigUnicode) ||
				(flidType == CellarPropertyType.MultiString) ||
				(flidType == CellarPropertyType.MultiBigString))
			{
				m_extendedKeyCache[new HvoFlidWSKey(obj, tag, ws)] = tss;
			}
			else
			{
				throw new ArgumentException(String.Format("Can only put ITsString data in the tag/flid '{0}'.", tag));
			}
		}

		/// <summary>Member SetMultiStringAlt</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='ws'>ws</param>
		/// <param name='tss'>tss</param>
		/// <remarks>ISilDataAccess method</remarks>
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			CheckDisposed();

			CacheStringAlt(hvo, tag, ws, tss);
			MakeDirty(hvo);
		}

		/// <summary>Member get_MultiStringAlt</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='ws'>ws</param>
		/// <returns>A ITsString</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			CheckDisposed();

			CheckBasics(hvo);

			var key = new HvoFlidWSKey(hvo, tag, ws);
			var removeFromCache = false;
			if (!m_extendedKeyCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			var tss = m_extendedKeyCache[key];
			if (tss == null)
			{
				// Note: Normally, this would throw a KeyNotFoundException,
				// but the interface says we have to return an empty string.
				var tsf = TsStrFactoryClass.Create();
				tss = tsf.MakeString(string.Empty, ws);
				// If it is not a Compute every time virtual, go ahead and cache it
				if (!removeFromCache)
					SetMultiStringAlt(hvo, tag, ws, tss); // Save it for next time.
			}
			if (removeFromCache)
				m_extendedKeyCache.Remove(key);
			return tss;
		}

		/// <summary>Member get_MultiStringProp</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A ITsMultiString</returns>
		/// <remarks>ISilDataAccess method</remarks>
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			CheckDisposed();

			var tsms = TsMultiStringClass.Create();
			foreach (var key in m_extendedKeyCache.Keys)
			{
				if (key.Hvo == hvo && key.Flid == tag)
					tsms.set_String(key.Ws, m_extendedKeyCache[key]);
			}
			return tsms;
		}

		#endregion MultiString/MultiUnicode methods

		#region Vector methods

		/// <summary>Member CacheVecProp</summary>
		/// <param name='obj'>obj</param>
		/// <param name='tag'>tag</param>
		/// <param name='rghvo'>rghvo</param>
		/// <param name='chvo'>chvo</param>
		public void CacheVecProp(int obj, int tag, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] rghvo, int chvo)
		{
			CheckDisposed();

			if (rghvo.Length != chvo)
				throw new ArgumentException("Lengths are not the same in the parameters: rghvo and chvo.");
			if (m_checkWithMDC)
			{
				CheckBasics(obj);
				CheckHvoTagMatch(obj, tag);
				for (var i = 0; i < rghvo.Length; ++i)
				{
					var hvo = rghvo[i];
					var clid = get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
					if (!MetaDataCache.get_IsValidClass(tag, clid))
						throw new ArgumentException(String.Format("Cannot put class '{0}' in the tag '{1}'.", clid, tag));
					/* Assume the 'setters' have worried about this.
					// Make sure two owing properties are the same, if tag is owing.
					// (int)CmObjectFields.kflidCmObject_Owner
					// (int)CmObjectFields.kflidCmObject_OwnFlid
					if (tag == CellarPropertyType.OwningCollection
						|| tag == CellarPropertyType.OwningSequence)
					{
						int currentOwner = get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
						int ownFlid = (int)get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
					}
					*/
				}
			}

			m_vectorCache[new HvoFlidKey(obj, tag)] = new List<int>(rghvo);
		}

		/// <summary>Get the full contents of the specified sequence in one go.</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='chvoMax'>chvoMax</param>
		/// <param name='chvo'>chvo</param>
		/// <param name='rghvo'>rghvo</param>
		public void VecProp(int hvo, int tag, int chvoMax, out int chvo, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 2)] ArrayPtr/*long[]*/ rghvo)
		{
			CheckDisposed();

			CheckBasics(hvo);
			chvo = 0;

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_vectorCache.ContainsKey(key))
				removeFromCache = CheckForVirtual(hvo, tag);
			List<int> val = m_vectorCache[key];
			if (removeFromCache)
				m_vectorCache.Remove(key);

			if (val.Count > chvoMax)
				throw new ArgumentException("The count is greater than the parameter 'chvo'.");
			chvo = val.Count;
			MarshalEx.ArrayToNative(rghvo, chvoMax, val.ToArray());
		}

		/// <summary>Member get_VecItem</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='index'>index</param>
		/// <returns>A System.Int32</returns>
		public int get_VecItem(int hvo, int tag, int index)
		{
			CheckDisposed();

			var key = new HvoFlidKey(hvo, tag);
			var val = m_vectorCache[key];
			return val[index];
		}

		/// <summary>Member get_VecSize</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Int32</returns>
		public int get_VecSize(int hvo, int tag)
		{
			CheckDisposed();

			var key = new HvoFlidKey(hvo, tag);
			List<int> collection;
			return m_vectorCache.TryGetValue(key, out collection) ? collection.Count : 0;
		}

		/// <summary>Member get_VecSizeAssumeCached</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Int32</returns>
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			CheckDisposed();

			return get_VecSize(hvo, tag);
		}

		/// <summary>Member GetObjIndex</summary>
		/// <param name='hvoOwn'>hvoOwn</param>
		/// <param name='flid'>flid</param>
		/// <param name='hvo'>hvo</param>
		/// <returns>A System.Int32</returns>
		/// <remarks>IVwCacheDa method</remarks>
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			CheckDisposed();

			var key = new HvoFlidKey(hvoOwn, flid);
			var val = m_vectorCache[key];
			return val.IndexOf(hvo);
		}

		// The ones below here are for editing, so can wait.

		/// <summary>Member CacheReplace</summary>
		/// <param name='hvoObj'>hvoObj</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvoMin'>ihvoMin</param>
		/// <param name='ihvoLim'>ihvoLim</param>
		/// <param name='rghvo'>rghvo</param>
		/// <param name='chvo'>chvo</param>
		/// <remarks>IVwCacheDa method</remarks>
		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] int[] rghvo, int chvo)
		{
			CheckDisposed();

			// Just leave this one unimplemented, since it purports to not intend to modify extant data.
			// Since the method is, in fact, intended to make a change to whatever is stored,
			// the client will just have to use a real 'setter method'.
			throw new NotImplementedException("'CacheReplace' not implemented. Use the 'Replace' method, instead.");
		}

		/// <summary>Member MoveOwnSeq</summary>
		/// <param name='hvoSrcOwner'>hvoSrcOwner</param>
		/// <param name='tagSrc'>tagSrc</param>
		/// <param name='ihvoStart'>ihvoStart</param>
		/// <param name='ihvoEnd'>ihvoEnd</param>
		/// <param name='hvoDstOwner'>hvoDstOwner</param>
		/// <param name='tagDst'>tagDst</param>
		/// <param name='ihvoDstStart'>ihvoDstStart</param>
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			CheckDisposed();

			throw new NotImplementedException("'MoveOwnSeq' not implemented yet.");
		}

		/// <summary>Member MoveOwn</summary>
		/// <param name='hvoSrcOwner'>hvoSrcOwner</param>
		/// <param name='tagSrc'>tagSrc</param>
		/// <param name='hvo'>hvo</param>
		/// <param name='hvoDstOwner'>hvoDstOwner</param>
		/// <param name='tagDst'>tagDst</param>
		/// <param name='ihvoDstStart'>ihvoDstStart</param>
		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			CheckDisposed();

			throw new NotImplementedException("'MoveOwn' not implemented yet.");
		}

		/// <summary>Member Replace</summary>
		/// <param name='hvoObj'>hvoObj</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvoMin'>ihvoMin</param>
		/// <param name='ihvoLim'>ihvoLim</param>
		/// <param name='rghvo'>rghvo</param>
		/// <param name='chvo'>chvo</param>
		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] int[] rghvo, int chvo)
		{
			CheckDisposed();

			List<int> list;
			HvoFlidKey key = new HvoFlidKey(hvoObj, tag);
			if (m_vectorCache.TryGetValue(key, out list))
			{
				while (ihvoLim > ihvoMin)
				{
					list.RemoveAt(ihvoMin);
					ihvoLim--;
				}
			}
			else
				m_vectorCache[key] = list = new List<int>(chvo);

			ihvoMin = Math.Min(ihvoMin, list.Count);
			list.InsertRange(ihvoMin, rghvo);

			for (int i = ihvoMin; i < list.Count; i++)
				CacheIntProp(list[i], (int)CmObjectFields.kflidCmObject_OwnOrd, i);
		}

		#endregion Vector methods

		#endregion ISilDataAccess/IVwCacheDa implementation (Cache/Set/Get)

		#region IStructuredTextDataAccess implementation

		/// <summary>
		/// Gets or sets the paragraph contents field id.
		/// </summary>
		public int ParaContentsFlid
		{
			set { m_paraContentsFlid = value; }
			get { return m_paraContentsFlid; }
		}

		/// <summary>
		/// Gets or sets the paragraph properties field id.
		/// </summary>
		public int ParaPropertiesFlid
		{
			set { m_paraPropertiesFlid = value; }
			get { return m_paraPropertiesFlid; }
		}

		/// <summary>
		/// Gets or sets the text paragraphs field id.
		/// </summary>
		public int TextParagraphsFlid
		{
			set { m_textParagraphsFlid = value; }
			get { return m_textParagraphsFlid; }
		}
		#endregion

		#region ISilDataAccess implementation (Except Get/Set methods)

		#region Undo/Redo methods

		/// <summary>Member BeginNonUndoableTask</summary>
		public void BeginNonUndoableTask()
		{
			CheckDisposed();

			throw new NotImplementedException("'BeginNonUndoableTask' not implemented yet.");
		}

		/// <summary>Member EndNonUndoableTask</summary>
		public void EndNonUndoableTask()
		{
			CheckDisposed();

			throw new NotImplementedException("'EndNonUndoableTask' not implemented yet.");
		}

		/// <summary>Member BeginUndoTask</summary>
		/// <param name='bstrUndo'>bstrUndo</param>
		/// <param name='bstrRedo'>bstrRedo</param>
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			CheckDisposed();

			throw new NotImplementedException("'BeginUndoTask' not implemented yet.");
		}

		/// <summary>Member EndUndoTask</summary>
		public void EndUndoTask()
		{
			CheckDisposed();

			throw new NotImplementedException("'EndUndoTask' not implemented yet.");
		}

		/// <summary>Member ContinueUndoTask</summary>
		public void ContinueUndoTask()
		{
			CheckDisposed();

			throw new NotImplementedException("'ContinueUndoTask' not implemented yet.");
		}

		/// <summary>Member EndOuterUndoTask</summary>
		public void EndOuterUndoTask()
		{
			CheckDisposed();

			throw new NotImplementedException("'EndOuterUndoTask' not implemented yet.");
		}

		/// <summary>Member BreakUndoTask</summary>
		/// <param name='bstrUndo'>bstrUndo</param>
		/// <param name='bstrRedo'>bstrRedo</param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			CheckDisposed();

			throw new NotImplementedException("'BreakUndoTask' not implemented yet.");
		}

		/// <summary>Member Rollback</summary>
		public void Rollback()
		{
			CheckDisposed();

			throw new NotImplementedException("'Rollback' not implemented yet.");
		}

		#endregion Undo/Redo methods

		#region Action Handling

		/// <summary>Member GetActionHandler</summary>
		/// <returns>A IActionHandler</returns>
		public IActionHandler GetActionHandler()
		{
			CheckDisposed();

			return m_actionhandler;
		}

		/// <summary>Member SetActionHandler</summary>
		/// <param name='actionhandler'>action handler</param>
		public void SetActionHandler(IActionHandler actionhandler)
		{
			CheckDisposed();

			m_actionhandler = actionhandler;
		}

		#endregion Action Handling

		#region Custom Field (Extra) methods

		/// <summary>Member InsertRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='hvoDst'>hvoDst</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			CheckDisposed();

			throw new NotImplementedException("'InsertRelExtra' not implemented yet.");
		}

		/// <summary>Member UpdateRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			CheckDisposed();

			throw new NotImplementedException("'UpdateRelExtra' not implemented yet.");
		}

		/// <summary>Member GetRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <returns>A System.String</returns>
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			CheckDisposed();

			throw new NotImplementedException("'GetRelExtra' not implemented yet.");
		}
		#endregion Custom Field (Extra) methods

		#region Basic Prop methods

		/// <summary>Member get_Prop</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <returns>A System.Object</returns>
		public object get_Prop(int hvo, int tag)
		{
			CheckDisposed();

			object result = null;
			var fieldType = MetaDataCache.GetFieldType(tag);
			if (get_IsPropInCache(hvo, tag, fieldType, 0))
			{
				var key = new HvoFlidKey(hvo, tag);
				switch ((CellarPropertyType)fieldType)
				{
					case CellarPropertyType.Boolean:
						result = m_boolCache[key];
						break;
					case CellarPropertyType.Integer:
						result = m_intCache[key];
						break;
					case CellarPropertyType.Numeric:
						break; // m_intCache.ContainsKey(key);
					case CellarPropertyType.Float:
						break; // m_intCache.ContainsKey(key);
					case CellarPropertyType.Time:
						result = m_longCache[key];
						break;
					case CellarPropertyType.Guid:
						result = m_guidCache[key];
						break;
					case CellarPropertyType.Image:
						break; //  m_intCache.ContainsKey(key);
					case CellarPropertyType.GenDate:
						break; //  m_intCache.ContainsKey(key);
					case CellarPropertyType.Binary:
						break;
					case CellarPropertyType.String: // Fall through.
					case CellarPropertyType.BigString:
						result = m_basicITsStringCache[key];
						break;
					case CellarPropertyType.MultiUnicode: // Fall through.
					case CellarPropertyType.MultiBigUnicode: // Fall through.
					case CellarPropertyType.MultiString: // Fall through.
					case CellarPropertyType.MultiBigString:
						result = get_MultiStringProp(hvo, tag);
						break;
					case CellarPropertyType.Unicode: // Fall through.
					case CellarPropertyType.BigUnicode:
						result = m_basicStringCache[key];
						break;

					case CellarPropertyType.OwningAtomic: // Fall through
					case CellarPropertyType.ReferenceAtomic:
						return m_intCache[key];

					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.ReferenceCollection:
					case CellarPropertyType.OwningSequence:
					case CellarPropertyType.ReferenceSequence:
						result = m_vectorCache[key];
						break;
				}
			}
			else
			{
				var ms = get_MultiStringProp(hvo, tag);
				if (ms.StringCount > 0)
					result = ms;
			}

			return result;
		}

		/// <summary>Member get_IsPropInCache</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='cpt'>cpt</param>
		/// <param name='ws'>ws</param>
		/// <returns>A System.Boolean</returns>
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			CheckDisposed();

			var key = new HvoFlidKey(hvo, tag);
			var keyWs = new HvoFlidWSKey(hvo, tag, ws);
			switch ((CellarPropertyType)cpt)
			{
				default:
					throw new ArgumentException("Invalid field type (cpt).");
				case CellarPropertyType.Boolean:
					return m_boolCache.ContainsKey(key);
				case CellarPropertyType.Integer:
					return m_intCache.ContainsKey(key);
				case CellarPropertyType.Numeric:
					return false; // m_intCache.ContainsKey(key);
				case CellarPropertyType.Float:
					return false; // m_intCache.ContainsKey(key);
				case CellarPropertyType.Time:
					return m_longCache.ContainsKey(key);
				case CellarPropertyType.Guid:
					return m_guidCache.ContainsKey(key);
				case CellarPropertyType.Image:
					return false; //  m_intCache.ContainsKey(key);
				case CellarPropertyType.GenDate:
					return false; //  m_intCache.ContainsKey(key);
				case CellarPropertyType.Binary:
					return false;
				case CellarPropertyType.String: // Fall through.
				case CellarPropertyType.BigString:
					return m_basicITsStringCache.ContainsKey(key);
				case CellarPropertyType.MultiUnicode: // Fall through.
				case CellarPropertyType.MultiBigUnicode: // Fall through.
				case CellarPropertyType.MultiString: // Fall through.
				case CellarPropertyType.MultiBigString:
					return m_extendedKeyCache.ContainsKey(keyWs);
				case CellarPropertyType.Unicode: // Fall through.
				case CellarPropertyType.BigUnicode:
					return m_basicStringCache.ContainsKey(key);

				case CellarPropertyType.OwningAtomic:
					return m_intCache.ContainsKey(key);
				case CellarPropertyType.ReferenceAtomic:
					return m_intCache.ContainsKey(key);
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceSequence:
					return m_vectorCache.ContainsKey(key);
			}
		}

		#endregion Basic Prop methods

		#region Basic Object methods

		/// <summary>Member DeleteObj</summary>
		/// <param name='hvoObj'>hvoObj</param>
		public void DeleteObj(int hvoObj)
		{
			CheckDisposed();

			throw new NotImplementedException("'DeleteObj' not implemented yet.");
		}

		/// <summary>Member DeleteObjOwner</summary>
		/// <param name='hvoOwner'>hvoOwner</param>
		/// <param name='hvoObj'>hvoObj</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			CheckDisposed();

			throw new NotImplementedException("'DeleteObjOwner' not implemented yet.");
		}

		/// <summary>Member InsertNew</summary>
		/// <param name='hvoObj'>hvoObj</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='chvo'>chvo</param>
		/// <param name='_ss'>_ss</param>
		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			CheckDisposed();

			throw new NotImplementedException("'InsertNew' not implemented yet.");
		}

		/// <summary>Member MakeNewObject</summary>
		/// <param name='clid'>clid</param>
		/// <param name='hvoOwner'>hvoOwner</param>
		/// <param name='tag'>tag</param>
		/// <param name='ord'>ord</param>
		/// <returns>A System.Int32</returns>
		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			CheckDisposed();
			int hvoNew = NextHvo;

			CacheIntProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class, clid);
			SetGuid(hvoNew, (int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());
			if (hvoOwner <= 0)
				return hvoNew;

			SetObjProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner, hvoOwner);
			CacheIntProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid, tag);
			switch (ord)
			{
				case -1: // Collection
					int c = get_VecSize(hvoOwner, tag);
					Replace(hvoOwner, tag, c, c, new[] { hvoNew }, 1);
					break;
				case -2: // Atomic
					SetObjProp(hvoOwner, tag, hvoNew);
					CacheIntProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnOrd, 0);
					break;
				default: // Sequence
					Debug.Assert(ord >= 0);
					Replace(hvoOwner, tag, ord, ord, new[] { hvoNew }, 1);
					break;
			}
			return hvoNew;
		}

		/// <summary>Member get_IsValidObject</summary>
		/// <param name='hvo'>hvo</param>
		/// <returns>A System.Boolean</returns>
		public bool get_IsValidObject(int hvo)
		{
			CheckDisposed();

			if (hvo == 0)
				throw new ArgumentException("'hvo' cannot be 0.");

			// If it contains the key, it is valid.
			return m_intCache.ContainsKey(new HvoFlidKey(hvo, (int)CmObjectFields.kflidCmObject_Class));
		}

		/// <summary>Member get_IsDummyId</summary>
		/// <param name='hvo'>hvo</param>
		/// <returns>A System.Boolean</returns>
		public bool get_IsDummyId(int hvo)
		{
			CheckDisposed();

			if (hvo == 0)
				throw new ArgumentException("'hvo' cannot be 0.");

			return false; //(Review JohnT: is this right? Does this cache have dummy objects?)
		}

		/// <summary>Member RemoveObjRefs</summary>
		/// <param name='hvo'>hvo</param>
		public void RemoveObjRefs(int hvo)
		{
			CheckDisposed();

			throw new NotImplementedException("'RemoveObjRefs' not implemented yet.");
		}

		#endregion Basic Object methods

		#region Notification methods

		/// <summary>Member AddNotification</summary>
		/// <param name='_nchng'>_nchng</param>
		public void AddNotification(IVwNotifyChange _nchng)
		{
			CheckDisposed();
			// No-op
		}

		/// <summary>Member PropChanged</summary>
		/// <param name='_nchng'>_nchng</param>
		/// <param name='_ct'>_ct</param>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='ivMin'>ivMin</param>
		/// <param name='cvIns'>cvIns</param>
		/// <param name='cvDel'>cvDel</param>
		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			throw new NotImplementedException("'PropChanged' not implemented yet.");
		}

		/// <summary>Member RemoveNotification</summary>
		/// <param name='_nchng'>_nchng</param>
		public void RemoveNotification(IVwNotifyChange _nchng)
		{
			CheckDisposed();
			// No-op
		}

		/// <summary>Member GetDisplayIndex</summary>
		/// <param name='hvoOwn'>hvoOwn</param>
		/// <param name='flid'>flid</param>
		/// <param name='ihvo'>ihvo</param>
		/// <returns>A System.Int32</returns>
		public int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			CheckDisposed();
			// default implementation: display index = real index
			return ihvo;
		}

		#endregion Notification methods

		#region Misc methods

		/// <summary>
		/// So far we haven't needed this for the purposes of CacheLight...it's used for multi-paragraph (and eventually drag/drop) editing.
		/// </summary>
		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst,
			int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			throw new NotImplementedException();
		}

		/// <summary>Member get_WritingSystemFactory</summary>
		/// <returns>A ILgWritingSystemFactory</returns>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				return m_lgWritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				m_lgWritingSystemFactory = value;
			}
		}

		/// <summary>Return a list of the encodings that are of interest within the database.</summary>
		/// <param name='cwsMax'>If cwsMax is zero, return the actual number (but no encodings).</param>
		/// <param name='_ws'>List of encodings, if cwsMax is greater than zero AND there was enough room to put them in</param>
		/// <returns>Return the actual number. If there is not enough room, throw an invalid argument exception.</returns>
		public int get_WritingSystemsOfInterest(int cwsMax, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*int[]*/ _ws)
		{
			CheckDisposed();

			// See MDC->GetFields for example of thorwing when too small.
			// CustViewDa::get_WritingSystemsOfInterest for details on code to port.
			// This depends on the vector methods being implemented
			// At least: get_VecSize/get_VecSizeAssumeCached, but perhaps get_VecItem/VecProp?.
			throw new NotImplementedException("'get_WritingSystemsOfInterest' not implemented yet.");
		}

		/// <summary>Member IsDirty</summary>
		/// <returns>A System.Boolean</returns>
		public bool IsDirty()
		{
			CheckDisposed();

			return m_isDirty;
		}

		/// <summary>Member ClearDirty</summary>
		public void ClearDirty()
		{
			CheckDisposed();

			m_isDirty = false;
		}

		/// <summary>Member get_MetaDataCache</summary>
		/// <returns>A IFwMetaDataCache</returns>
		public IFwMetaDataCache MetaDataCache
		{
			get
			{
				CheckDisposed();

				return m_metaDataCache;
			}
			set
			{
				CheckDisposed();

				if (value != null && !(value is MetaDataCache))
					throw new ApplicationException("This cache only accepts the light version of a meta data cache.");
				if (m_metaDataCache != value)
				{
					// TODO: Figure out what needs to happen if it is different.
					// Surely, the cached data will have to go.
				}
				m_metaDataCache = value;
			}
		}
		#endregion Misc methods

		#endregion ISilDataAccess implementation (Except Get/Set methods)

		#region IVwCacheDa implementation (Except Caching methods)

		#region Virtual methods

		/// <summary>Member InstallVirtual</summary>
		/// <param name='vh'>vh</param>
		public void InstallVirtual(IVwVirtualHandler vh)
		{
			CheckDisposed();

			throw new NotImplementedException("'InstallVirtual' not implemented yet.");
		}

		/// <summary>Member GetVirtualHandlerId</summary>
		/// <param name='tag'>tag</param>
		/// <returns>A IVwVirtualHandler</returns>
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			CheckDisposed();

			// Needs help, but it gets an unrelated test to pass.
			return null;
		}

		/// <summary>Member GetVirtualHandlerName</summary>
		/// <param name='bstrClass'>bstrClass</param>
		/// <param name='bstrField'>bstrField</param>
		/// <returns>A IVwVirtualHandler</returns>
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			CheckDisposed();

			throw new NotImplementedException("'GetVirtualHandlerName' not implemented yet.");
		}

		/// <summary>Member ClearVirtualProperties</summary>
		public void ClearVirtualProperties()
		{
			CheckDisposed();

			throw new NotImplementedException("'ClearVirtualProperties' not implemented yet.");
		}

		#endregion Virtual methods

		#region Clear Data methods

		/// <summary>Member ClearInfoAbout</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='cia'>cia</param>
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			CheckDisposed();

			throw new NotImplementedException("'ClearInfoAbout' not implemented yet.");
		}
		/// <summary>Member ClearInfoAbout</summary>
		/// <param name='rghvo'>hvo</param>
		/// <param name="chvo"></param>
		/// <param name='cia'>cia</param>
		public void ClearInfoAboutAll(int[] rghvo, int chvo, VwClearInfoAction cia)
		{
			CheckDisposed();

			throw new NotImplementedException("'ClearInfoAboutAll' not implemented yet.");
		}

		/// <summary>Member ClearAllData</summary>
		public void ClearAllData()
		{
			CheckDisposed();

			// Hastables
			m_basicObjectCache.Clear();

			// Dictionaries.
			m_extendedKeyCache.Clear();
			m_basicITsStringCache.Clear();
			m_basicByteArrayCache.Clear();
			m_basicStringCache.Clear();
			m_guidCache.Clear();
			m_guidToHvo.Clear();
			m_intCache.Clear();
			m_longCache.Clear();
			m_boolCache.Clear();
			m_vectorCache.Clear();

			// No sense in keeping the times on all that data that was just cleared out.
			m_timeStampCache.Clear();
		}

		#endregion Clear Data methods

		/// <summary>Member GetOutlineNumber</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='flid'>flid</param>
		/// <param name='fFinPer'>fFinPer</param>
		/// <returns>A System.String</returns>
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			CheckDisposed();

			throw new NotImplementedException("'GetOutlineNumber' not implemented yet.");
		}

		#endregion IVwCacheDa implementation (Except Caching methods)

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~RealDataCache()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				ClearAllData();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_metaDataCache = null;
			m_actionhandler = null;
			m_lgWritingSystemFactory = null;
			m_basicObjectCache = null;
			m_extendedKeyCache = null;
			m_basicITsStringCache = null;
			m_basicByteArrayCache = null;
			m_guidCache = null;
			m_guidToHvo = null;
			m_intCache = null;
			m_longCache = null;
			m_boolCache = null;
			m_vectorCache = null;
			m_timeStampCache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#endregion Interface implementations
	}

		#region Other internal classes

		#region Cache key structs

		internal struct HvoFlidKey
		{
			private readonly int m_hvo;
			private readonly int m_flid;

			public int Hvo
			{
				get { return m_hvo; }
			}

			public int Flid
			{
				get { return m_flid; }
			}

			public HvoFlidKey(int hvo, int flid)
			{
				m_hvo = hvo;
				m_flid = flid;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is HvoFlidKey))
					return false;

				var hfk = (HvoFlidKey)obj;
				return (hfk.m_hvo == m_hvo)
					&& (hfk.m_flid == m_flid);
			}

			public override int GetHashCode()
			{
				return (m_hvo ^ m_flid);
			}

			public override string ToString()
			{
				return String.Format("{0}^{1}", m_hvo, m_flid);
			}
		}

		internal struct HvoFlidWSKey
		{
			private readonly int m_hvo;
			private readonly int m_flid;
			private readonly int m_ws;

			public int Hvo
			{
				get { return m_hvo; }
			}

			public int Flid
			{
				get { return m_flid; }
			}

			public int Ws
			{
				get { return m_ws; }
			}

			public HvoFlidWSKey(int hvo, int flid, int ws)
			{
				m_hvo = hvo;
				m_flid = flid;
				m_ws = ws;
			}
			public override bool Equals(object obj)
			{
				if (!(obj is HvoFlidWSKey))
					return false;

				var key = (HvoFlidWSKey)obj;
				return (key.m_hvo == m_hvo)
					&& (key.m_flid == m_flid)
					&& (key.m_ws == m_ws);
			}

			public override int GetHashCode()
			{
				return (m_hvo ^ m_flid ^ m_ws);
			}

			public override string ToString()
			{
				return String.Format("{0}^{1}^{2}", m_hvo, m_flid, m_ws);
			}
		}

		#endregion Cache key structs

		#endregion Other internal classes
	}
