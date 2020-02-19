// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.TestUtilities
{
	/// <summary></summary>
	public sealed class RealDataCache : IRealDataCache
	{
		#region Basic implementation
		#region Data members
		private int m_nextHvo = 1;
		private bool m_isDirty;
		private IFwMetaDataCache m_metaDataCache;
		private IActionHandler m_actionhandler;

		/// <summary>
		/// Cache for storing all class ids.
		/// This is an optimization, so we don;t have to ask the MDC all the time for all valids class ids.
		/// </summary>
		private readonly List<int> m_clids = new List<int>();

		#region Dictionary caches.

		/// <summary />
		private Dictionary<HvoFlidKey, object> m_basicObjectCache = new Dictionary<HvoFlidKey, object>();
		/// <summary />
		private Dictionary<HvoFlidWSKey, ITsString> m_extendedKeyCache = new Dictionary<HvoFlidWSKey, ITsString>();
		/// <summary />
		private Dictionary<HvoFlidKey, ITsString> m_basicITsStringCache = new Dictionary<HvoFlidKey, ITsString>();
		/// <summary />
		private Dictionary<HvoFlidKey, byte[]> m_basicByteArrayCache = new Dictionary<HvoFlidKey, byte[]>();
		/// <summary />
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
		internal int NextHvo => m_nextHvo++;

		/// <summary>
		/// Normally, the MDC should check the MDC to see that values are legal.
		/// When loading the XML file, we can skip the check, since the DTD says it is legal.
		/// This is a risk, but an acceptable one.
		/// </summary>
		internal bool CheckWithMDC { set; get; } = true;

		#endregion Properties

		#region Other methods

		private void MakeDirty(int hvo)
		{
			m_isDirty = true;
			m_timeStampCache[hvo] = DateTime.Now.Ticks;
		}

		private bool CheckForVirtual(int hvo, int tag)
		{
			// It may or may not be a virtual property,
			// so be safe and have client understand to leave the value in the cache.
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
			if (!CheckWithMDC)
			{
				return;
			}

			// NB: This will throw an exception, if the class hasn't already been put into the cache.
			// But then, CheckBasics should already have been called,
			// which would have thrown it before.
			var clid = get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			// First find out how many there are.
			var countAllFlidsOut = MetaDataCache.GetFields(clid, true, (int)CellarPropertyTypeFilter.All, 0, null);
			// Now get them for real.
			using (var flids = MarshalEx.ArrayToNative<int>(countAllFlidsOut))
			{
				countAllFlidsOut = MetaDataCache.GetFields(clid, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				var flids1 = MarshalEx.NativeToArray<int>(flids, countAllFlidsOut);
				var foundFlid = flids1.Any(flid => flid == tag);
				if (!foundFlid)
				{
					throw new ArgumentException($"Invalid 'tag' ({tag}) for 'hvo' ({hvo})");
				}
			}
		}

		/// <summary>
		/// Makes sure the mdc exists.
		/// </summary>
		/// <exception cref="ApplicationException">Thrown when the 'MetaDataCache' property is null.</exception>
		private void CheckForMetaDataCache()
		{
			if (m_metaDataCache == null)
			{
				throw new ApplicationException("The 'MetaDataCache' property must be set, before using the cache.");
			}
		}

		/// <summary>
		/// See if the given 'hvo' is valid.
		/// </summary>
		/// <param name="hvo"></param>
		private void CheckBasics(int hvo)
		{
			CheckForMetaDataCache();
			if (!CheckWithMDC)
			{
				return;
			}
			if (!get_IsValidObject(hvo))
			{
				throw new ArgumentException("Invalid 'hvo'.");
			}
		}

		private int GetFromIntCache(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_intCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_intCache[key];
			if (removeFromCache)
			{
				m_intCache.Remove(key);
			}
			return val;
		}

		private void AddToIntCache(int obj, int tag, int val, bool isIntType)
		{
			if (tag == (int)CmObjectFields.kflidCmObject_Class)
			{
				CheckForMetaDataCache();
				if (obj == 0)
				{
					throw new ArgumentException("Hvo cannot be zero.");
				}
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
							using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
							{
								MetaDataCache.GetClassIds(countAllClasses, clids);
								var uIds = MarshalEx.NativeToArray<int>(clids, countAllClasses);
								m_clids.AddRange(uIds);
							}
						}
						var isValidClid = m_clids.Contains(val);
						if (!isValidClid)
						{
							throw new ArgumentException($"The given class id of '{val}' is not valid in the model.");
						}
						break;
					default:
						// Make sure the hvo has the given tag.
						CheckHvoTagMatch(obj, tag);
						// Make sure an int is legal for the given tag.
						var flidType = MetaDataCache.GetFieldType(tag);
						if (flidType != (int)CellarPropertyType.Integer)
						{
							throw new ArgumentException($"Can only put integers in the tag/flid '{tag}'.");
						}
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
				{
					throw new ArgumentException($"Can't put the 'val' ({val}) into the 'tag' ({tag}).");
				}
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
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_longCache[key];
			if (removeFromCache)
			{
				m_longCache.Remove(key);
			}
			return val;
		}

		private void AddToLongCache(int obj, int tag, long val, CellarPropertyType flidType)
		{
			CheckBasics(obj);

			// Make sure the hvo has the given tag.
			CheckHvoTagMatch(obj, tag);
			// Make sure a long is legal for the given tag.
			var tagType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if (tagType != flidType)
			{
				throw new ArgumentException($"Can only put long integers in the tag/flid '{tag}'.");
			}
			m_longCache[new HvoFlidKey(obj, tag)] = val;
		}

		#endregion Other methods

		#endregion Basic implementation

		#region Interface implementations

		#region ISilDataAccess/IVwCacheDa implementation (Cache/Set/Get)

		#region Object Prop methods

		/// <inheritdoc />
		public ITsStrFactory TsStrFactory { get; set; }

		/// <inheritdoc />
		public void CacheObjProp(int obj, int tag, int val)
		{
			AddToIntCache(obj, tag, val, false);
		}

		/// <inheritdoc />
		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			CacheObjProp(hvo, tag, hvoObj);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public int get_ObjectProp(int hvo, int tag)
		{
			return GetFromIntCache(hvo, tag);
		}
		#endregion Object Prop methods

		#region Boolean methods

		/// <inheritdoc />
		public void CacheBooleanProp(int hvo, int tag, bool value)
		{
			CheckBasics(hvo);

			// Make sure the hvo has the given tag.
			CheckHvoTagMatch(hvo, tag);
			// Make sure an boolean is legal for the given tag.
			var flidType = MetaDataCache.GetFieldType(tag);
			if (flidType != (int)CellarPropertyType.Boolean)
			{
				throw new ArgumentException($"Can only put booleans in the tag/flid '{tag}'.");
			}
			// Made it here, so set it.
			m_boolCache[new HvoFlidKey(hvo, tag)] = value;
		}

		/// <inheritdoc />
		public void SetBoolean(int hvo, int tag, bool n)
		{
			CacheBooleanProp(hvo, tag, n);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public bool get_BooleanProp(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_boolCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_boolCache[key];
			if (removeFromCache)
			{
				m_boolCache.Remove(key);
			}
			return val;
		}

		#endregion Boolean methods

		#region Guid methods

		/// <inheritdoc />
		public void CacheGuidProp(int obj, int tag, Guid uid)
		{
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
					{
						throw new ArgumentException($"Can only put Guids in the tag/flid '{tag}'.");
					}
					break;
			}
			// Made it here, so set it.
			m_guidCache[new HvoFlidKey(obj, tag)] = uid;
		}

		/// <inheritdoc />
		public void SetGuid(int hvo, int tag, Guid uid)
		{
			CacheGuidProp(hvo, tag, uid);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public Guid get_GuidProp(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_guidCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_guidCache[key];
			if (removeFromCache)
			{
				m_guidCache.Remove(key);
			}
			return val;
		}

		/// <inheritdoc />
		public int get_ObjFromGuid(Guid uid)
		{
			return m_guidToHvo[uid];
		}

		#endregion Guid methods

		#region Int methods

		/// <inheritdoc />
		public void CacheIntProp(int obj, int tag, int val)
		{
			AddToIntCache(obj, tag, val, true);
		}

		/// <inheritdoc />
		public void SetInt(int hvo, int tag, int n)
		{
			CacheIntProp(hvo, tag, n);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public int get_IntProp(int hvo, int tag)
		{
			return GetFromIntCache(hvo, tag);
		}

		/// <inheritdoc />
		public int get_CachedIntProp(int obj, int tag, out bool isInCache)
		{
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

		#region Unicode methods

		/// <inheritdoc />
		public void CacheUnicodeProp(int obj, int tag, string val, int cch)
		{
			CheckBasics(obj);
			if (val.Length != cch)
			{
				throw new ArgumentException("Input string not the right length.");
			}
			CheckHvoTagMatch(obj, tag);
			// Make sure Unicode is legal for the given tag.
			var flidType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if (flidType != CellarPropertyType.Unicode)
			{
				throw new ArgumentException($"Can only put Unicode data in the tag/flid '{tag}'.");
			}
			m_basicStringCache[new HvoFlidKey(obj, tag)] = val;
		}

		/// <inheritdoc />
		public void SetUnicode(int hvo, int tag, string rgch, int cch)
		{
			CacheUnicodeProp(hvo, tag, rgch, cch);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			SetUnicode(obj, tag, bstr, bstr.Length);
		}

		/// <inheritdoc />
		public string get_UnicodeProp(int obj, int tag)
		{
			CheckBasics(obj);

			var key = new HvoFlidKey(obj, tag);
			var removeFromCache = false;
			if (!m_basicStringCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(obj, tag);
			}
			var val = m_basicStringCache[key];
			if (removeFromCache)
			{
				m_basicStringCache.Remove(key);
			}
			return val;
		}

		/// <inheritdoc />
		public void UnicodePropRgch(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*OLECHAR[]*/ rgch, int cchMax, out int cch)
		{
			CheckBasics(obj);

			var str = get_UnicodeProp(obj, tag);
			cch = str.Length;
			if (cchMax == 0)
			{
				return;
			}
			if (cch >= cchMax)
			{
				throw new ArgumentException("cch cannot be larger than cchMax");
			}
			MarshalEx.StringToNative(rgch, cchMax, str, true);
		}

		#endregion Unicode methods

		#region Time methods

		/// <inheritdoc />
		public void CacheTimeProp(int hvo, int tag, long val)
		{
			AddToLongCache(hvo, tag, val, CellarPropertyType.Time);
		}

		/// <inheritdoc />
		public void SetTime(int hvo, int tag, long lln)
		{
			CacheTimeProp(hvo, tag, lln);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public long get_TimeProp(int hvo, int tag)
		{
			return GetFromLongCache(hvo, tag);
		}

		#endregion Time methods

		#region Int64 methods

		/// <inheritdoc />
		public void CacheInt64Prop(int obj, int tag, long val)
		{
			AddToLongCache(obj, tag, val, CellarPropertyType.GenDate);
		}

		/// <inheritdoc />
		public void SetInt64(int hvo, int tag, long lln)
		{
			CacheInt64Prop(hvo, tag, lln);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public long get_Int64Prop(int hvo, int tag)
		{
			return GetFromLongCache(hvo, tag);
		}

		#endregion Int64 methods

		#region Unknown methods

		/// <inheritdoc />
		public void CacheUnknown(int obj, int tag, [MarshalAs(UnmanagedType.IUnknown)] object unk)
		{
			if (!(unk is ITsTextProps))
			{
				throw new ArgumentException("Only ITsTextProps COM interfaces are supported in the cache.");
			}
			CheckBasics(obj);
			CheckHvoTagMatch(obj, tag);
			// Make sure Binary is legal for the given tag.
			var flidType = MetaDataCache.GetFieldType(tag);
			if (flidType != (int)CellarPropertyType.Binary)
			{
				throw new ArgumentException($"Can only put IUnknown data in the tag/flid '{tag}' as Binary data.");
			}
			m_basicObjectCache[new HvoFlidKey(obj, tag)] = unk;
		}

		/// <inheritdoc />
		public void SetUnknown(int hvo, int tag, [MarshalAs(UnmanagedType.IUnknown)] object unk)
		{
			CacheUnknown(hvo, tag, unk);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		[return: MarshalAs(UnmanagedType.IUnknown)]
		public object get_UnknownProp(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_basicObjectCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_basicObjectCache[key];
			if (removeFromCache)
			{
				m_basicObjectCache.Remove(key);
			}
			return val;
		}

		#endregion Unknown methods

		#region Binary methods

		/// <inheritdoc />
		public void CacheBinaryProp(int obj, int tag, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] Byte[] rgb, int cb)
		{
			CheckBasics(obj);
			if (rgb.Length != cb)
			{
				throw new ArgumentException("Binary input not the right length.");
			}
			CheckHvoTagMatch(obj, tag);
			// Make sure Binary is legal for the given tag.
			var flidType = MetaDataCache.GetFieldType(tag);
			if (flidType != (int)CellarPropertyType.Binary)
			{
				throw new ArgumentException($"Can only put Binary data in the tag/flid '{tag}'.");
			}
			m_basicByteArrayCache[new HvoFlidKey(obj, tag)] = rgb;
		}

		/// <inheritdoc />
		public void SetBinary(int hvo, int tag, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] Byte[] rgb, int cb)
		{
			CacheBinaryProp(hvo, tag, rgb, cb);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public void BinaryPropRgb(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*byte[]*/ rgb, int cbMax, out int cb)
		{
			CheckBasics(obj);

			var key = new HvoFlidKey(obj, tag);
			var removeFromCache = false;
			if (!m_basicByteArrayCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(obj, tag);
			}
			var array = m_basicByteArrayCache[key];
			if (removeFromCache)
			{
				m_basicByteArrayCache.Remove(key);
			}
			cb = array.Length;
			if (cbMax == 0)
			{
				return;
			}
			if (cb > cbMax)
			{
				throw new ArgumentException("cb cannot be larger than cbMax");
			}
			MarshalEx.ArrayToNative(rgb, cbMax, array);
		}

		#endregion Binary methods

		#region String methods

		/// <inheritdoc />
		public void CacheStringProp(int obj, int tag, ITsString tss)
		{
			CheckBasics(obj);
			CheckHvoTagMatch(obj, tag);
			// Make sure Unicode is legal for the given tag.
			var flidType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if (flidType != CellarPropertyType.String)
			{
				throw new ArgumentException($"Can only put String data in the tag/flid '{tag}'.");
			}
			m_basicITsStringCache[new HvoFlidKey(obj, tag)] = tss;
		}

		/// <inheritdoc />
		public void SetString(int hvo, int tag, ITsString tss)
		{
			CacheStringProp(hvo, tag, tss);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public ITsString get_StringProp(int hvo, int tag)
		{
			CheckBasics(hvo);

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_basicITsStringCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_basicITsStringCache[key];
			if (removeFromCache)
			{
				m_basicITsStringCache.Remove(key);
			}
			return val;
		}

		#endregion String methods

		#region MultiString/MultiUnicode methods

		/// <inheritdoc />
		public void CacheStringAlt(int obj, int tag, int ws, ITsString tss)
		{
			CheckBasics(obj);
			// Make sure ws is not for a magic ws.
			// Magic WSes are all negative
			if (ws < 0)
			{
				throw new ArgumentException("Magic writing system invalid in string.");
			}
			if (ws == 0)
			{
				throw new ArgumentException("Writing system of zero is invalid in string.");
			}
			CheckHvoTagMatch(obj, tag);
			// Make sure ITsString is legal for the given tag.
			var flidType = (CellarPropertyType)MetaDataCache.GetFieldType(tag);
			if (flidType == CellarPropertyType.MultiUnicode || flidType == CellarPropertyType.MultiString)
			{
				m_extendedKeyCache[new HvoFlidWSKey(obj, tag, ws)] = tss;
			}
			else
			{
				throw new ArgumentException($"Can only put ITsString data in the tag/flid '{tag}'.");
			}
		}

		/// <inheritdoc />
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			CacheStringAlt(hvo, tag, ws, tss);
			MakeDirty(hvo);
		}

		/// <inheritdoc />
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			CheckBasics(hvo);

			var key = new HvoFlidWSKey(hvo, tag, ws);
			var removeFromCache = false;
			if (!m_extendedKeyCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var tss = m_extendedKeyCache[key];
			if (tss == null)
			{
				// Note: Normally, this would throw a KeyNotFoundException,
				// but the interface says we have to return an empty string.
				tss = TsStrFactory.EmptyString(ws);
				// If it is not a Compute every time virtual, go ahead and cache it
				if (!removeFromCache)
				{
					SetMultiStringAlt(hvo, tag, ws, tss); // Save it for next time.
				}
			}
			if (removeFromCache)
			{
				m_extendedKeyCache.Remove(key);
			}
			return tss;
		}

		/// <inheritdoc />
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			var tsms = new TsMultiString();
			foreach (var key in m_extendedKeyCache.Keys)
			{
				if (key.Hvo == hvo && key.Flid == tag)
				{
					tsms.set_String(key.Ws, m_extendedKeyCache[key]);
				}
			}
			return tsms;
		}

		#endregion MultiString/MultiUnicode methods

		#region Vector methods

		/// <inheritdoc />
		public void CacheVecProp(int obj, int tag, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] rghvo, int chvo)
		{
			if (rghvo.Length != chvo)
			{
				throw new ArgumentException("Lengths are not the same in the parameters: rghvo and chvo.");
			}
			if (CheckWithMDC)
			{
				CheckBasics(obj);
				CheckHvoTagMatch(obj, tag);
				foreach (var hvo in rghvo)
				{
					var clid = get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
					if (!MetaDataCache.get_IsValidClass(tag, clid))
					{
						throw new ArgumentException($"Cannot put class '{clid}' in the tag '{tag}'.");
					}
				}
			}
			m_vectorCache[new HvoFlidKey(obj, tag)] = new List<int>(rghvo);
		}

		/// <inheritdoc />
		public void VecProp(int hvo, int tag, int chvoMax, out int chvo, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 2)] ArrayPtr/*long[]*/ rghvo)
		{
			CheckBasics(hvo);
			chvo = 0;

			var key = new HvoFlidKey(hvo, tag);
			var removeFromCache = false;
			if (!m_vectorCache.ContainsKey(key))
			{
				removeFromCache = CheckForVirtual(hvo, tag);
			}
			var val = m_vectorCache[key];
			if (removeFromCache)
			{
				m_vectorCache.Remove(key);
			}
			if (val.Count > chvoMax)
			{
				throw new ArgumentException("The count is greater than the parameter 'chvo'.");
			}
			chvo = val.Count;
			MarshalEx.ArrayToNative(rghvo, chvoMax, val.ToArray());
		}

		/// <inheritdoc />
		public int get_VecItem(int hvo, int tag, int index)
		{
			var key = new HvoFlidKey(hvo, tag);
			var val = m_vectorCache[key];
			return val[index];
		}

		/// <inheritdoc />
		public int get_VecSize(int hvo, int tag)
		{
			var key = new HvoFlidKey(hvo, tag);
			List<int> collection;
			return m_vectorCache.TryGetValue(key, out collection) ? collection.Count : 0;
		}

		/// <inheritdoc />
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			return get_VecSize(hvo, tag);
		}

		/// <inheritdoc />
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			var key = new HvoFlidKey(hvoOwn, flid);
			var val = m_vectorCache[key];
			return val.IndexOf(hvo);
		}

		// The ones below here are for editing, so can wait.

		/// <inheritdoc />
		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] int[] rghvo, int chvo)
		{
			// Just leave this one unimplemented, since it purports to not intend to modify extant data.
			// Since the method is, in fact, intended to make a change to whatever is stored,
			// the client will just have to use a real 'setter method'.
			throw new NotSupportedException("'CacheReplace' is not supported. Use the 'Replace' method, instead.");
		}

		/// <inheritdoc />
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			throw new NotSupportedException("'MoveOwnSeq' is not supported.");
		}

		/// <inheritdoc />
		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			throw new NotSupportedException("'MoveOwn' is not supported.");
		}

		/// <inheritdoc />
		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] int[] rghvo, int chvo)
		{
			List<int> list;
			var key = new HvoFlidKey(hvoObj, tag);
			if (m_vectorCache.TryGetValue(key, out list))
			{
				while (ihvoLim > ihvoMin)
				{
					list.RemoveAt(ihvoMin);
					ihvoLim--;
				}
			}
			else
			{
				m_vectorCache[key] = list = new List<int>(chvo);
			}
			ihvoMin = Math.Min(ihvoMin, list.Count);
			list.InsertRange(ihvoMin, rghvo);

			for (var i = ihvoMin; i < list.Count; i++)
			{
				CacheIntProp(list[i], (int)CmObjectFields.kflidCmObject_OwnOrd, i);
			}
		}

		#endregion Vector methods

		#endregion ISilDataAccess/IVwCacheDa implementation (Cache/Set/Get)

		#region IStructuredTextDataAccess implementation

		/// <summary>
		/// Gets or sets the paragraph contents field id.
		/// </summary>
		public int ParaContentsFlid { set; get; }

		/// <summary>
		/// Gets or sets the paragraph properties field id.
		/// </summary>
		public int ParaPropertiesFlid { set; get; }

		/// <summary>
		/// Gets or sets the text paragraphs field id.
		/// </summary>
		public int TextParagraphsFlid { set; get; }
		#endregion

		#region ISilDataAccess implementation (Except Get/Set methods)

		#region Undo/Redo methods

		/// <inheritdoc />
		public void BeginNonUndoableTask()
		{
			throw new NotSupportedException("'BeginNonUndoableTask' is not supported.");
		}

		/// <inheritdoc />
		public void EndNonUndoableTask()
		{
			throw new NotSupportedException("'EndNonUndoableTask' is not supported.");
		}

		/// <inheritdoc />
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new NotSupportedException("'BeginUndoTask' is not supported.");
		}

		/// <inheritdoc />
		public void EndUndoTask()
		{
			throw new NotSupportedException("'EndUndoTask' is not supported.");
		}

		/// <inheritdoc />
		public void ContinueUndoTask()
		{
			throw new NotSupportedException("'ContinueUndoTask' is not supported.");
		}

		/// <inheritdoc />
		public void EndOuterUndoTask()
		{
			throw new NotSupportedException("'EndOuterUndoTask' is not supported.");
		}

		/// <inheritdoc />
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new NotSupportedException("'BreakUndoTask' is not supported.");
		}

		/// <inheritdoc />
		public void Rollback()
		{
			throw new NotSupportedException("'Rollback' is not supported.");
		}

		#endregion Undo/Redo methods

		#region Action Handling

		/// <inheritdoc />
		public IActionHandler GetActionHandler()
		{
			return m_actionhandler;
		}

		/// <inheritdoc />
		public void SetActionHandler(IActionHandler actionhandler)
		{
			m_actionhandler = actionhandler;
		}

		#endregion Action Handling

		#region Custom Field (Extra) methods

		/// <inheritdoc />
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			throw new NotSupportedException("'InsertRelExtra' is not supported.");
		}

		/// <inheritdoc />
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			throw new NotSupportedException("'UpdateRelExtra' is not supported.");
		}

		/// <inheritdoc />
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			throw new NotSupportedException("'GetRelExtra' is not supported.");
		}
		#endregion Custom Field (Extra) methods

		#region Basic Prop methods

		/// <inheritdoc />
		public object get_Prop(int hvo, int tag)
		{
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
					case CellarPropertyType.String:
						result = m_basicITsStringCache[key];
						break;
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiString:
						result = get_MultiStringProp(hvo, tag);
						break;
					case CellarPropertyType.Unicode:
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
				{
					result = ms;
				}
				else if (Marshal.IsComObject(ms))
				{
					Marshal.ReleaseComObject(ms);
				}
			}

			return result;
		}

		/// <inheritdoc />
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
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
				case CellarPropertyType.String:
					return m_basicITsStringCache.ContainsKey(key);
				case CellarPropertyType.MultiUnicode: // Fall through.
				case CellarPropertyType.MultiString:
					return m_extendedKeyCache.ContainsKey(keyWs);
				case CellarPropertyType.Unicode:
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

		/// <inheritdoc />
		public void DeleteObj(int hvoObj)
		{
			throw new NotSupportedException("'DeleteObj' is not supported.");
		}

		/// <inheritdoc />
		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			throw new NotSupportedException("'DeleteObjOwner' is not supported.");
		}

		/// <inheritdoc />
		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			throw new NotSupportedException("'InsertNew' is not supported.");
		}

		/// <inheritdoc />
		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			var hvoNew = NextHvo;
			CacheIntProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class, clid);
			SetGuid(hvoNew, (int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());
			if (hvoOwner <= 0)
			{
				return hvoNew;
			}
			SetObjProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner, hvoOwner);
			CacheIntProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid, tag);
			switch (ord)
			{
				case -1: // Collection
					var c = get_VecSize(hvoOwner, tag);
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

		/// <inheritdoc />
		public bool get_IsValidObject(int hvo)
		{
			if (hvo == 0)
			{
				throw new ArgumentException("'hvo' cannot be 0.");
			}
			// If it contains the key, it is valid.
			return m_intCache.ContainsKey(new HvoFlidKey(hvo, (int)CmObjectFields.kflidCmObject_Class));
		}

		/// <inheritdoc />
		public bool get_IsDummyId(int hvo)
		{
			if (hvo == 0)
			{
				throw new ArgumentException("'hvo' cannot be 0.");
			}
			return false;
		}

		/// <inheritdoc />
		public void RemoveObjRefs(int hvo)
		{
			throw new NotSupportedException("'RemoveObjRefs' is not supported.");
		}

		#endregion Basic Object methods

		#region Notification methods

		/// <inheritdoc />
		public void AddNotification(IVwNotifyChange _nchng)
		{ }

		/// <inheritdoc />
		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			throw new NotSupportedException("'PropChanged' is not supported.");
		}

		/// <inheritdoc />
		public void RemoveNotification(IVwNotifyChange _nchng)
		{ }

		/// <inheritdoc />
		public int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			// default implementation: display index = real index
			return ihvo;
		}

		#endregion Notification methods

		#region Misc methods

		/// <inheritdoc />
		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst, int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			throw new NotSupportedException("'MoveString' is not supported.");
		}

		/// <inheritdoc />
		public ILgWritingSystemFactory WritingSystemFactory { get; set; }

		/// <inheritdoc />
		public int get_WritingSystemsOfInterest(int cwsMax, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*int[]*/ _ws)
		{
			throw new NotSupportedException("'get_WritingSystemsOfInterest' is not supported.");
		}

		/// <inheritdoc />
		public bool IsDirty()
		{
			return m_isDirty;
		}

		/// <inheritdoc />
		public void ClearDirty()
		{
			m_isDirty = false;
		}

		/// <inheritdoc />
		public IFwMetaDataCache MetaDataCache
		{
			get
			{
				return m_metaDataCache;
			}
			set
			{
				if (value != null && !(value is MetaDataCache))
				{
					throw new ApplicationException("This cache only accepts the light version of a meta data cache.");
				}
				m_metaDataCache = value;
			}
		}
		#endregion Misc methods

		#endregion ISilDataAccess implementation (Except Get/Set methods)

		#region IVwCacheDa implementation (Except Caching methods)

		#region Virtual methods

		/// <inheritdoc />
		public void InstallVirtual(IVwVirtualHandler vh)
		{
			throw new NotSupportedException("'InstallVirtual' is not supported.");
		}

		/// <inheritdoc />
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			// Needs help, but it gets an unrelated test to pass.
			return null;
		}

		/// <inheritdoc />
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			throw new NotSupportedException("'GetVirtualHandlerName' is not supported.");
		}

		/// <inheritdoc />
		public void ClearVirtualProperties()
		{
			throw new NotSupportedException("'ClearVirtualProperties' is not supported.");
		}

		#endregion Virtual methods

		#region Clear Data methods

		/// <inheritdoc />
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			throw new NotSupportedException("'ClearInfoAbout' is not supported.");
		}

		/// <inheritdoc />
		public void ClearInfoAboutAll(int[] rghvo, int chvo, VwClearInfoAction cia)
		{
			throw new NotSupportedException("'ClearInfoAboutAll' is not supported.");
		}

		/// <inheritdoc />
		public void ClearAllData()
		{
			// Hash tables
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

		/// <inheritdoc />
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			throw new NotSupportedException("'GetOutlineNumber' is not supported.");
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

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
			// Therefore, you should call GC.SuppressFinalize to
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
			{
				// No need to run it more then once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				ClearAllData();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_metaDataCache = null;
			m_actionhandler = null;
			WritingSystemFactory = null;
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

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#endregion Interface implementations

		private struct HvoFlidKey
		{
			private int Hvo { get; }

			private int Flid { get; }

			internal HvoFlidKey(int hvo, int flid)
			{
				Hvo = hvo;
				Flid = flid;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is HvoFlidKey))
				{
					return false;
				}
				var hfk = (HvoFlidKey)obj;
				return hfk.Hvo == Hvo && hfk.Flid == Flid;
			}

			public override int GetHashCode()
			{
				return (Hvo ^ Flid);
			}

			public override string ToString()
			{
				return $"{Hvo}^{Flid}";
			}
		}

		private struct HvoFlidWSKey
		{
			internal int Hvo { get; }

			internal int Flid { get; }

			internal int Ws { get; }

			internal HvoFlidWSKey(int hvo, int flid, int ws)
			{
				Hvo = hvo;
				Flid = flid;
				Ws = ws;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is HvoFlidWSKey))
				{
					return false;
				}
				var key = (HvoFlidWSKey)obj;
				return key.Hvo == Hvo && key.Flid == Flid && key.Ws == Ws;
			}

			public override int GetHashCode()
			{
				return (Hvo ^ Flid ^ Ws);
			}

			public override string ToString()
			{
				return $"{Hvo}^{Flid}^{Ws}";
			}
		}
	}
}
