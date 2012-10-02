// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CacheBase.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region Hashtable with timestamps
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a Hashtable that also stores the timestamp when an entry is stored
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TimeStampHashtable : Hashtable
	{
		#region Member variables
		private bool m_fDirty = false;
		private Hashtable m_htTimeStamps;
		#endregion

		#region C'tor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="capacity"></param>
		/// <param name="loadFactor"></param>
		/// ------------------------------------------------------------------------------------
		public TimeStampHashtable(int capacity, float loadFactor): base(capacity, loadFactor)
		{
			m_htTimeStamps = new Hashtable(capacity, loadFactor);
		}
		#endregion

		#region Overriden methods for getting/setting
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override object this[object key]
		{
			get {return base[key]; }
			set
			{
				base[key] = value;
				SetTimeStamp(key, DateTime.Now);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// ------------------------------------------------------------------------------------
		public override void Add(object key, object value)
		{
			base.Add(key, value);
			SetTimeStamp(key, DateTime.Now);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Clear()
		{
			base.Clear ();
			m_htTimeStamps.Clear();
			m_fDirty = false;
		}

		// REVIEW (EberhardB): we might need to override Clone and/or CopyTo if we need it
		#endregion

		#region Additional methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the timestamp for specified key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public DateTime GetTimeStamp(object key)
		{
			if (key is CacheKey)
				return (DateTime)m_htTimeStamps[((CacheKey)key).Hvo];
			else
				return (DateTime)m_htTimeStamps[key];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the timestamp for specified key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// ------------------------------------------------------------------------------------
		public void SetTimeStamp(object key, DateTime value)
		{
			if (key is CacheKey)
				m_htTimeStamps[((CacheKey)key).Hvo] = value;
			else
				m_htTimeStamps[key] = value;

			m_fDirty = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if hashtable was modified
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDirty
		{
			get { return m_fDirty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the modified flag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearDirty()
		{
			m_fDirty = false;
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Basic cache class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CacheBase : IVwCacheDa, ISilDataAccess, IVwOleDbDa, IFWDisposable
	{
		#region Member variables
		private const string strObjNotFound = "Requested property not found in cache";

		/// <summary>The real cache</summary>
		protected TimeStampHashtable m_htCache;

		/// <summary>List of virtual props</summary>
		protected Hashtable m_htVirtualProps;

		/// <summary>Tags for virtual props. The current value is chosen to match the VwCacheDa implementation of InstallVirtual,
		/// but that is not absolutely essential. It IS essential to produce positive numbers that don't clash with any real props.</summary>
		protected int m_NextTag = 0x7f000000;

		/// <summary>writing system factory</summary>
		protected ILgWritingSystemFactory m_wsf;

		/// <summary>the illicit meta data cache</summary>
		protected IFwMetaDataCache m_mdc;

		private List<IVwNotifyChange> m_vvnc = new List<IVwNotifyChange>();

		/// <summary>The action handler object</summary>
		protected IActionHandler m_acth;

		/// <summary>Last assigned HVO</summary>
		protected static int s_lastHvo = 10000;

		/// <summary>
		/// This value allows get_AutoloadPolicy to return what set_AutoloadPolicy wrote.
		/// Otherwise it is not used in this dummy implementation.
		/// </summary>
		protected AutoloadPolicies m_autoloadPolicy;

		/// <summary>Next assigned dummy HVO</summary>
		protected static int s_hvoNextDummy = -1000000;
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CacheBase"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public CacheBase(IFwMetaDataCache mdc)
		{
			// REVIEW (EberhardB): we might have to review the initial values
			m_htCache = new TimeStampHashtable(100, (float)0.5);
			m_htVirtualProps = new Hashtable();
			m_wsf = null;
			m_mdc = mdc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="hvoToAppend"></param>
		/// ------------------------------------------------------------------------------------
		public void AppendToFdoVector(int hvoOwner, int flid, int hvoToAppend)
		{
			CheckDisposed();
			List<int> hvos;
			if (get_VecSize(hvoOwner, flid) > 0)
				hvos = new List<int>((int[])get_Prop(hvoOwner, flid));
			else
				hvos = new List<int>();

			hvos.Add(hvoToAppend);
			CacheVecProp(hvoOwner, flid, hvos.ToArray(), hvos.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the HVO into fdo vector.
		/// </summary>
		/// <param name="hvoOwner">The hvo owner.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="hvoToAppend">The hvo to append.</param>
		/// <param name="index">The index.</param>
		/// ------------------------------------------------------------------------------------
		public void InsertIntoFdoVector(int hvoOwner, int flid, int hvoToAppend, int index)
		{
			CheckDisposed();
			List<int> hvos;
			if (get_VecSize(hvoOwner, flid) > 0)
				hvos = new List<int>((int[])get_Prop(hvoOwner, flid));
			else
				hvos = new List<int>();

			hvos.Insert(index, hvoToAppend);
			CacheVecProp(hvoOwner, flid, hvos.ToArray(), hvos.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the owner, classId and field id directly in the cache. Using the methods that
		/// FdoCache provides doesn't work because it requires a MetaDataCache which in turn
		/// requires a OleDbEncap (which we haven't yet ported to C#).
		/// </summary>
		/// <param name="hvo">The HVO of the object</param>
		/// <param name="hvoOwner">The HVO of the owner of the object</param>
		/// <param name="classId">The classId of the object</param>
		/// <param name="flid">The field Id of the object</param>
		/// <param name="ownOrd">The ordinal number in the sequence (if any)</param>
		/// ------------------------------------------------------------------------------------
		public void SetBasicProps(int hvo, int hvoOwner, int classId, int flid, int ownOrd)
		{
			CheckDisposed();
			SetObjProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, hvoOwner);
			SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, classId);
			SetInt(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid, flid);
			SetInt(hvo, (int)CmObjectFields.kflidCmObject_OwnOrd, ownOrd);
		}

		#region ISilDataAccess methods

		#region Methods for retrieving objects from the cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of an atomic object property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of the atomic object property, or 0 if value is not found
		/// in the cache.</returns>
		/// ------------------------------------------------------------------------------------
		public int get_ObjectProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			int retVal;
			object obj = Get(hvo, tag, CellarModuleDefns.kcptOwningAtom);
			if (obj != null && obj is int)
				retVal = (int)obj;
			else
				retVal = 0;
			return retVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of a vector item.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <param name="index">Index of vector item</param>
		/// <returns>Returns the value of a vector item, or 0 if the vector is not found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int get_VecItem(int hvo, int tag, int index)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is int[])
			{
				int[] array = (int[])obj;
				if (index < 0 || index >= array.Length)
					throw new ArgumentException("index out of range");
				int hvoItem = array[index];
				return hvoItem;
			}
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the size of a vector.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the size of a vector, or 0 if the vector itself, cannot be found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int get_VecSize(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is int[])
				return ((int[])obj).Length;
			return 0;
		}


		/// <summary>
		/// Test whether an HVO represents a valid object. For the simple memory cache,
		/// any HVO is potentially valid, and true will be returned.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public bool get_IsValidObject(int hvo)
		{
			CheckDisposed();
			return true;
		}

		/// <summary>
		/// Test whether an HVO represents a dummy object. For the simple memory cache,
		/// all HVOs are considered full objects, so none are dummies.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public bool get_IsDummyId(int hvo)
		{
			CheckDisposed();
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for a virtual property. If so, load and return the object and indicate whether
		/// it is a 'ComputeEveryTime'.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object TryVirtual(CacheKey key)
		{
			IVwVirtualHandler vh = GetVirtualHandlerId(key.Tag);
			object obj = null;
			if (vh != null)
			{
				vh.Load(key.Hvo, key.Tag, 0, this);
				bool fComputeEveryTime = vh.ComputeEveryTime;
				obj = this[key];
				if (fComputeEveryTime)
				{
					// vh.Load put it in the cache, but since we want to compute always
					// we can't let it there.
					this[key] = null;
				}
			}
			return obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the size of a vector, and is guaranteed to not at the database even if the vector is empty.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the size of a vector, or 0 if the vector itself, cannot be found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			CheckDisposed();
			//Review Eberhard(Hatton):I don't know if this need special code or not.
			return get_VecSize(hvo, tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the full contents of the specified sequence in one go.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="chvoMax">Max size of the array</param>
		/// <param name="chvo">[out]Number of elements in array</param>
		/// <param name="rghvo">Array</param>
		/// ------------------------------------------------------------------------------------
		public void VecProp(int hvo, int tag, int chvoMax, out int chvo,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(ArrayPtrMarshaler))]
			ArrayPtr/*int[]*/ rghvo)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			chvo = 0;
			object obj = Get(hvo, tag);
			if (obj != null && obj is int[])
			{
				int[] array = (int[])obj;
				chvo = array.Length;
				if (chvo >= chvoMax)
					throw new ArgumentException("chvo cannot be larger than chvoMax");
				MarshalEx.ArrayToNative(rghvo, chvoMax, array);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the value of a binary property.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="rgb">Buffer to which to copy data. May pass <c>null</c> to request
		/// required length.</param>
		/// <param name="cbMax">Length of buffer to which to copy data. May pass 0 to request
		/// required length.</param>
		/// <param name="cb">[out] Length of filled in elements in buffer.</param>
		/// ------------------------------------------------------------------------------------
		public void BinaryPropRgb(int hvo, int tag,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(ArrayPtrMarshaler))]
			ArrayPtr/*byte[]*/ rgb, int cbMax, out int cb)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			cb = 0;
			object obj = Get(hvo, tag);
			if (obj != null && obj is byte[])
			{
				byte[] array = (byte[])obj;
				cb = array.Length;
				if (cbMax == 0)
					return;

				if (cb >= cbMax)
					throw new COMException("cb cannot be larger than cbMax");
				MarshalEx.ArrayToNative(rgb, cbMax, array);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtains the value of guid property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of a guid property, or an empty guid if the object
		/// cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		public System.Guid get_GuidProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is Guid)
				return (Guid)obj;

			return Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtains the object this guid belongs to
		/// </summary>
		/// <param name="guid"></param>
		/// <returns>object</returns>
		/// ------------------------------------------------------------------------------------
		public int get_ObjFromGuid(Guid guid)
		{
			CheckDisposed();
			foreach (DictionaryEntry entry in m_htCache)
			{
				if (entry.Value is Guid && (Guid)entry.Value == guid)
				{
					CacheKey cacheKey = (CacheKey)entry.Key;
					if (cacheKey.Tag == (int)CmObjectFields.kflidCmObject_Guid)
						return cacheKey.Hvo;
				}
			}

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtains the value of integer property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of an integer property, or 0 if the object cannot be
		/// found.</returns>
		/// ------------------------------------------------------------------------------------
		public int get_IntProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is int)
				return (int)obj;

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtains the value of boolean property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of a boolean property, or <c>false</c> if the object
		/// cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		public bool get_BooleanProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is bool)
				return (bool)obj;

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtains the value of Int 64 property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of an int 64 property, or 0 if the object cannot be
		/// found.</returns>
		/// ------------------------------------------------------------------------------------
		public long get_Int64Prop(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is long)
				return (long)obj;
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of one alternative of a Multilingual alternation (whatever that
		/// means).
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <param name="ws">The string's writing system id</param>
		/// <returns>Returns the value of one alternative of a Multilingual alternation, or
		/// an empty TsString if the object cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag, ws);
			if (obj != null && obj is ITsString)
				return (ITsString)obj;

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(string.Empty, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of ITsString property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of a ITsString property, or null if the object
		/// cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString get_StringProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is ITsString)
				return (ITsString)obj;

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			Debug.Assert(m_wsf != null);
			return tsf.MakeString(string.Empty, m_wsf.UserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of a unicode string property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of a unicode string property, or an empty string if the
		/// object cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		public string get_UnicodeProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is string)
				return (string)obj;
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a Unicode string property.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="rgch">Buffer to which to copy data. May pass <c>null</c> to request
		/// required length.</param>
		/// <param name="cchMax">Length of buffer to which to copy data. May pass 0 to request
		/// required length.</param>
		/// <param name="cch">[out] Length of filled in elements in buffer.</param>
		/// ------------------------------------------------------------------------------------
		public void UnicodePropRgch(int hvo, int tag,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(ArrayPtrMarshaler))]
			ArrayPtr/*OLECHAR[]*/ rgch, int cchMax, out int cch)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			cch = 0;
			object obj = Get(hvo, tag);
			if (obj != null && obj is string)
			{
				string str = (string)obj;
				cch = str.Length;
				if (cchMax == 0)
					return;

				if (cch >= cchMax)
					throw new COMException("cch cannot be larger than cchMax");

				MarshalEx.StringToNative(rgch, cchMax, str, true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of ITsMultiString property. Not implemented.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Throws an <see cref="NotImplementedException"/> exception (<c>E_NOTIMPL)
		/// </c></returns>
		/// ------------------------------------------------------------------------------------
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Obtains an arbitrary property as an object.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the object property, or null if the object cannot be found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public object get_Prop(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null)
				return obj;

			// now try to get a ObjProp
			obj = Get(hvo, tag, CellarModuleDefns.kcptOwningAtom);
			if (obj != null)
				return obj;

			//
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the value of time property.
		/// </summary>
		/// <param name="hvo">The Hvo</param>
		/// <param name="tag">The tag</param>
		/// <returns>Returns the value of a time property, or 0 if the object
		/// cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		public long get_TimeProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			if (obj != null && obj is long)
				return (long)obj;
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an object which is typically a non-CmObject derived from a Binary field.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <returns>Requested object, or <c>null</c> if object not found in the cache or if
		/// the object isn't a COM object.</returns>
		/// <remarks>We check for object being a ITsTextProps to retain compatibility with
		/// C++ implementation.</remarks>
		/// ------------------------------------------------------------------------------------
		public object get_UnknownProp(int hvo, int tag)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			object obj = Get(hvo, tag);
			// for compatibility with C++ we check for ITsTextProps
			if (obj is ITsTextProps)
				return obj;
			return null;
		}
		#endregion

		#region Methods for setting objects
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the value of an atomic REFERENCE property.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="hvoObj">The referenced object</param>
		/// <remarks><p>Use <see cref="MakeNewObject"/> or <see cref="DeleteObjOwner"/> to make
		/// similar changes to owning atomic properties.</p>
		/// <p>The caller should also call <see cref="PropChanged"/> to notify interested
		/// parties, except where the change is being made to a newly created object.</p>
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, (int)CellarModuleDefns.kcptOwningAtom, hvoObj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a binary data property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="rgb">Binary property</param>
		/// <param name="cb">Length of <paramref name="rgb"/></param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetBinary(int hvo, int tag,
			[MarshalAs(UnmanagedType.LPArray)] byte[] rgb, int cb)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(rgb.Length == cb);
			Set(hvo, tag, rgb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a GUID property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="uid">GUID property</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetGuid(int hvo, int tag, System.Guid uid)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, uid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an integer property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="n">Integer property</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetInt(int hvo, int tag, int n)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, n);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a boolean property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="f">Boolean property</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetBoolean(int hvo, int tag, bool f)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, f);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a long integer property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="lln">Long integer property</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetInt64(int hvo, int tag, long lln)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, lln);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set one alternative of a multilingual string property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="ws">writing system id</param>
		/// <param name="tss">Multilingual string</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, ws, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a string-valued property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="tss">Multilingual string</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetString(int hvo, int tag, ITsString tss)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a time property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="lln">Long integer property</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetTime(int hvo, int tag, long lln)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, lln);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a Unicode string property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="bstr">Unicode string</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void set_UnicodeProp(int hvo, int tag, string bstr)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, bstr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a Unicode string property of an object.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="rgch">Unicode string</param>
		/// <param name="cch">Length of the string</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetUnicode(int hvo, int tag, string rgch, int cch)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(rgch.Length == cch);
			Set(hvo, tag, rgch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a binary property of an object to a suitable representation of the object
		/// represented by the IUnknown.
		/// </summary>
		/// <param name="hvo">hvo</param>
		/// <param name="tag">tag</param>
		/// <param name="unk">The IUnknown object</param>
		/// <remarks>The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SetUnknown(int hvo, int tag,
			[MarshalAs(UnmanagedType.IUnknown)] object unk)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Set(hvo, tag, unk);
		}
		#endregion

		#region Undo handling

		/// <summary>
		/// How many times more often BeginUndoTask was called then EndUndoTask
		/// </summary>
		public int UndoLevel;
		/// <summary>
		/// most recently passed to BeginUndoTask when level is zero
		/// </summary>
		public string OuterUndo;
		/// <summary>
		/// most recently passed to BeginUndoTask when level is zero
		/// </summary>
		public string OuterRedo;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Begin a sequence of actions that will be treated as one task for the purposes
		/// of undo and redo. If there is already such a task in process, this sequence will be
		/// included (nested) in that one, and the descriptive strings will be ignored.
		/// </summary>
		/// <param name="bstrUndo">Short description of an action.  This is intended to appear
		/// on the "undo" menu item (e.g. "Typing" or "Clear")</param>
		/// <param name="bstrRedo">Short description of an action.  This is intended to appear
		/// on the "redo" menu item (e.g. "Typing" or "Clear").  Usually, this is the same as
		/// <paramref name="bstrUndo"/>.</param>
		/// ------------------------------------------------------------------------------------
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			CheckDisposed();
			if (UndoLevel == 0)
			{
				OuterUndo = bstrUndo;
				OuterRedo = bstrRedo;
			}
			UndoLevel++;
			if (m_acth != null) // is MockActionHandler)
				m_acth.BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndUndoTask()
		{
			CheckDisposed();
			UndoLevel--;
			if (m_acth != null) // is MockActionHandler)
				m_acth.EndUndoTask();
			// nothing to do here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Continue the previous sequence. This is intended to be called from a place like
		/// OnIdle that performs "clean-up" operations that are really part of the previous
		/// sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ContinueUndoTask()
		{
			CheckDisposed();
			// nothing to do here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the current sequence, and any outer ones that are in progress. This is intended
		/// to be used as a clean-up function to get everything back in sync.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndOuterUndoTask()
		{
			CheckDisposed();
			// nothing to do here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Break the current undo task into two at the current point. Subsequent actions will
		/// be part of the new task which will be assigned the given labels.
		/// </summary>
		/// <param name="bstrUndo">Short description of an action.  This is intended to appear
		/// on the "undo" menu item (e.g. "Typing" or "Clear")</param>
		/// <param name="bstrRedo">Short description of an action.  This is intended to appear
		/// on the "redo" menu item (e.g. "Typing" or "Clear").  Usually, this is the same as
		/// <paramref name="bstrUndo"/>.</param>
		/// ------------------------------------------------------------------------------------
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			CheckDisposed();
			// nothing to do here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Rollback()
		{
			CheckDisposed();
			while (UndoLevel > 0)
				UndoLevel--;
			if (m_acth != null) // is MockActionHandler)
				m_acth.Rollback(0);
			// nothing to do here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the <see cref="IActionHandler"/> that is being used to record undo
		/// information.
		/// </summary>
		/// <returns>The <see cref="IActionHandler"/> that is being used to record undo
		/// information. May be <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public IActionHandler GetActionHandler()
		{
			CheckDisposed();
			return m_acth;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the <see cref="IActionHandler"/> that is being used to record undo information.
		/// </summary>
		/// <param name="acth">The <see cref="IActionHandler"/> object. May be <c>null</c>.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void SetActionHandler(IActionHandler acth)
		{
			CheckDisposed();
			m_acth = acth;
		}
		#endregion

		#region Update handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="nchng"></param>
		/// ------------------------------------------------------------------------------------
		public void AddNotification(IVwNotifyChange nchng)
		{
			CheckDisposed();
			m_vvnc.Add(nchng);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This looks like it was copied from C++ because it was, okay, Tim? (Tim says "no")
		/// </summary>
		/// <param name="nchng"></param>
		/// <param name="ct"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// ------------------------------------------------------------------------------------
		public void PropChanged(IVwNotifyChange nchng, int ct, int hvo, int tag, int ivMin,
			int cvIns, int cvDel)
		{
			CheckDisposed();
			if (nchng == null && (int)PropChangeType.kpctNotifyAll != ct)
				throw new ArgumentNullException("Don't pass null as the IVwNotifyChange parameter unless ct is kpctNotifyAll");

			if ((int)PropChangeType.kpctNotifyMeThenAll == ct)
				nchng.PropChanged(hvo, tag, ivMin, cvIns, cvDel);

			// Can't use foreach since chng.PropChanged() might add or remove notifcations.
			for (int ichng = 0; ichng < m_vvnc.Count; ichng++)
			{
				IVwNotifyChange chng = m_vvnc[ichng];
				if (chng != nchng || (int)PropChangeType.kpctNotifyAll == ct)
					chng.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="nchng"></param>
		/// ------------------------------------------------------------------------------------
		public void RemoveNotification(IVwNotifyChange nchng)
		{
			CheckDisposed();
			m_vvnc.Remove(nchng);
		}

		#endregion

		#region Deleting and Creating objects
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		/// ------------------------------------------------------------------------------------
		public void DeleteObj(int hvoObj)
		{
			CheckDisposed();
			RemoveCachedProperties(hvoObj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// ------------------------------------------------------------------------------------
		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			CheckDisposed();

			Debug.Assert(tag != 0);
			CacheKey key = new CacheKey(hvoOwner, tag);
			if (ihvo == -2)
			{
				// atomic property
				m_htCache.Remove(key);
			}
			else
			{
				int[] hvos = (int[])m_htCache[key];
				if (hvos != null)
				{
					// Need to search for it and remove it
					int[] newHvos = new int[hvos.Length - 1];
					int currIndex = 0;
					for (int i = 0; i < hvos.Length; i++)
					{
						if (hvos[i] != hvoObj)
							newHvos[currIndex++] = hvos[i];
					}

					m_htCache[key] = newHvos;
				}
			}
			RemoveCachedProperties(hvoObj);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>Checks a field ID to see if it is an owning property.</summary>
		/// <param name="flid">Field ID to be checked.</param>
		/// <returns>
		/// true, if flid is an owning property, otherwise false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsOwningProperty(int flid)
		{
			CheckDisposed();
			int iType = m_mdc.GetFieldType((uint)flid);
			return ((iType == (int)FieldType.kcptOwningCollection)
				|| (iType == (int)FieldType.kcptOwningSequence)
				|| (iType == (int)FieldType.kcptOwningAtom));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		private void RemoveCachedProperties(int hvo)
		{
			if (hvo == 0)
				return;

			// Remove references from the cache of atomic object properties.
			if (m_htCache.Count != 0)
			{
				ArrayList keysToDelete = new ArrayList();
				ArrayList valuesToDelete = new ArrayList();
				foreach (DictionaryEntry entry in m_htCache)
				{
					CacheKey key = (CacheKey)entry.Key;
					int cacheHvo = 0;
					if (entry.Value is int)
						cacheHvo = (int)entry.Value;

					if (entry.Value is int[] && IsOwningProperty(key.Tag) && key.Hvo == hvo)
					{
						foreach (int clifford in (int[])entry.Value)
						{
							keysToDelete.Add(key);
							valuesToDelete.Add(clifford);
						}
					}
					else if (key.Hvo == hvo || cacheHvo == hvo)
					{
						keysToDelete.Add(key);
						valuesToDelete.Add(cacheHvo);
					}
				}

				for (int i = 0; i < keysToDelete.Count; i++)
				{
					CacheKey key = (CacheKey)keysToDelete[i];
					// Delete the reference stored in the loop above.  (Deleting it inside that loop
					// would invalidate the iterator.)
					m_htCache.Remove(key);

					// Recursively delete references for an owned object.
					if (key.Hvo == hvo && IsOwningProperty(key.Tag))
						RemoveCachedProperties((int)valuesToDelete[i]);
				}
			}
			m_htCache.Remove(new CacheKey(hvo, (int)CmObjectFields.kflidCmObject_Class));

			m_htCache.Remove(new CacheKey(hvo, (int)CmObjectFields.kflidCmObject_Guid));

			// ENHANCE (TimS): if we find that just removing the atomic properties isn't
			// suffecient then we need to remove the rest of the references in the cache.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="flid"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvo"></param>
		/// <param name="ss"></param>
		/// ------------------------------------------------------------------------------------
		public void InsertNew(int hvoObj, int flid, int ihvo, int chvo, IVwStylesheet ss)
		{
			CheckDisposed();
			if (flid != (int)StText.StTextTags.kflidParagraphs)
				throw new NotImplementedException("Cannot use InsertNew to insert anything but paragraphs");

			int hvoPara = get_VecItem(hvoObj, flid, ihvo);
			Guid guidTextProps = typeof(ITsTextProps).GUID;
			ITsTextProps ttp = (ITsTextProps)get_UnknownProp(hvoPara, (int)StPara.StParaTags.kflidStyleRules);

			//int[] array = new int[chvo];
			for (int i = 0; i < chvo; i++)
			{
				int hvo = MakeNewObject(StTxtPara.kClassId, hvoObj, flid, ihvo + i + 1);
				if (ss == null && ttp != null)
					SetUnknown(hvo, (int)StPara.StParaTags.kflidStyleRules, ttp);
			}

			if (ss != null && ttp != null)
			{
				for (int i = 0; i < chvo; i++)
				{
					// this part came from the real C++ cache and is not implemented yet.
					//					CheckHr(qttp->GetStrPropValue(kspNamedStyle, &sbstr));
					//					SmartBstr sbstrNew;
					//					CheckHr(pss->GetNextStyle(sbstr, &sbstrNew));
					//					if (sbstrNew != sbstr && sbstrNew.Length())
					//					{
					//						ITsPropsBldrPtr qtpb;
					//						CheckHr(qttp->GetBldr(&qtpb));
					//						CheckHr(qtpb->SetStrPropValue(kspNamedStyle, sbstrNew));
					//						CheckHr(qtpb->GetTextProps(&qttp));
					//					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="tag"></param>
		/// <param name="ord"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			CheckDisposed();
			int hvo = NewHvo(clid);

			// This mimics what the real DB does. Sequence indices are 1-based.
			int ownOrd = (ord >= 0)? ord + 1 : ord;

			SetBasicProps(hvo, hvoOwner, clid, tag, ownOrd);

			if (ord >= -1)
			{
				int[] hvos = (int[])Get(hvoOwner, tag);
				if (ord > 0 && hvos == null)
					return hvo;
				if (ord == -1)
					ord = (hvos == null ? 0 : hvos.Length);

				if (hvos == null)
					hvos = new int[0];

				List<int> hvosNew = new List<int>(hvos);
				hvosNew.Insert(ord, hvo);
				CacheVecProp(hvoOwner, tag, hvosNew.ToArray(), hvosNew.Count);
			}
			else if (ord == -2)
			{
				CacheObjProp(hvoOwner, tag, hvo);
			}
			return hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the owner of a range of objects in a sequence (given by the indexes
		/// ihvoStart and ihvoEnd) and insert them in another sequence.  The "ord" values
		/// change accordingly (first one to ihvoDstStart).
		/// </summary>
		/// <param name="hvoSrcOwner"></param>
		/// <param name="tagSrc"></param>
		/// <param name="ihvoStart"></param>
		/// <param name="ihvoEnd"></param>
		/// <param name="hvoDstOwner"></param>
		/// <param name="tagDst"></param>
		/// <param name="ihvoDstStart"></param>
		/// ------------------------------------------------------------------------------------
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd,
			int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			CheckDisposed();
			object objSrc = Get(hvoSrcOwner, tagSrc);
			if (objSrc == null)
				throw new ArgumentException("src not found in cache");

			if (objSrc is int[])
			{
				int[] srcVec = (int[])objSrc;
				int countToMove = ihvoEnd - ihvoStart + 1;

				if (ihvoStart < 0 || ihvoStart >= srcVec.Length || ihvoEnd < ihvoStart ||
					ihvoEnd >= srcVec.Length)
				{
					throw new ArgumentException("Invalid indexes");
				}

				// copy hvos to new place
				int[] srcToCopyVec = new int[countToMove];
				Array.Copy(srcVec, ihvoStart, srcToCopyVec, 0, countToMove);
				Replace(hvoDstOwner, tagDst, ihvoDstStart, ihvoDstStart, srcToCopyVec,
					srcToCopyVec.Length);

				// remove original copy
				int[] newSrcVec = new int[srcVec.Length - countToMove];
				if (ihvoStart != 0)
					Array.Copy(srcVec, 0, newSrcVec, 0, ihvoStart);
				if (ihvoEnd + 1 < srcVec.Length)
					Array.Copy(srcVec, ihvoEnd + 1, newSrcVec, ihvoStart, srcVec.Length - countToMove);
				Set(hvoSrcOwner, tagSrc, newSrcVec);
			}
			else
				throw new ArgumentException("src is not an integer array");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the owner of a range of objects in a sequence (given by the indexes
		/// ihvoStart and ihvoEnd) and insert them in another sequence.  The "ord" values
		/// change accordingly (first one to ihvoDstStart).
		/// </summary>
		/// <param name="hvoSrcOwner"></param>
		/// <param name="tagSrc"></param>
		/// <param name="hvo"></param>
		/// <param name="hvoDstOwner"></param>
		/// <param name="tagDst"></param>
		/// <param name="ihvoDstStart"></param>
		/// ------------------------------------------------------------------------------------
		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst,
			int ihvoDstStart)
		{
			CheckDisposed();
			int iSrcType = m_mdc.GetFieldType((uint) tagSrc);
			int iDstType = m_mdc.GetFieldType((uint) tagDst);

			switch (iSrcType)
			{
				case (int)FieldType.kcptOwningAtom:
					CacheKeyEx key = new CacheKeyEx(hvoSrcOwner, tagSrc, (int)CellarModuleDefns.kcptOwningAtom);
					m_htCache.Remove(key);
					break;

				case (int)FieldType.kcptOwningCollection:
				case (int)FieldType.kcptOwningSequence:
					object objSrc = Get(hvoSrcOwner, tagSrc);
					if (objSrc == null)
						throw new ArgumentException("src not found in cache");
					if (objSrc is int[])
					{
						int[] srcVec = (int[])objSrc;
						int ihvo = GetObjIndex(hvoSrcOwner, tagSrc, hvo);

						int[] newSrcVec = new int[srcVec.Length - 1];
						if (ihvo != 0)
							Array.Copy(srcVec, 0, newSrcVec, 0, ihvo);
						if (ihvo + 1 < srcVec.Length)
							Array.Copy(srcVec, ihvo + 1, newSrcVec, ihvo, srcVec.Length - 1);
						Set(hvoSrcOwner, tagSrc, newSrcVec);
					}
					break;
			}

			switch (iDstType)
			{
				case (int)FieldType.kcptOwningAtom:
					CacheObjProp(hvoDstOwner, tagDst, hvo);
					break;

				case (int)FieldType.kcptOwningCollection:
				case (int)FieldType.kcptOwningSequence:
					object objDst = Get(hvoDstOwner, tagDst);
					int[] newDstVec = null;
					if (objDst == null)
					{
						newDstVec = new int[1];
					}
					else if (objDst is int[])
					{
						int[] dstVec = (int[])objDst;
						newDstVec = new int[dstVec.Length + 1];
						Array.Copy(dstVec, newDstVec, dstVec.Length);
					}
					if (newDstVec != null)
					{
						newDstVec[newDstVec.Length - 1] = hvo;
						Set(hvoDstOwner, tagDst, newDstVec);
					}
					break;
			}

			SetObjProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, hvoDstOwner);
			SetInt(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid, tagDst);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		/// ------------------------------------------------------------------------------------
		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim,
			[MarshalAs(UnmanagedType.LPArray)] int[] rghvo, int chvo)
		{
			CheckDisposed();
			IVwVirtualHandler vh = GetVirtualHandlerId(tag);
			if (vh != null)
				vh.Replace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo, this);
			else
			{
				// get the vector for this property
				object obj = Get(hvoObj, tag);

				// if the vector is non-existent, then store the new vector
				if (obj == null)
				{
					Set(hvoObj, tag, rghvo);
					SetCmObjectOwnSeqFields(hvoObj, tag, rghvo);
				}
				else if (obj is int[])
				{
					// if the object is an int vector then do the replace
					int[] oldVector = obj as int[];
					int newLength = (oldVector.Length == 0 ? chvo : oldVector.Length - ihvoLim + ihvoMin + chvo);
					int[] newVector = new int[newLength];

					// copy the first part of the old array
					Array.Copy(oldVector, 0, newVector, 0, ihvoMin);
					//copy the new stuff
					Array.Copy(rghvo, 0, newVector, ihvoMin, chvo);
					// copy the end part of the old array
					if (oldVector.Length >= ihvoLim)
						Array.Copy(oldVector, ihvoLim, newVector, ihvoMin + chvo, oldVector.Length - ihvoLim);

					SetCmObjectOwnSeqFields(hvoObj, tag, newVector);

					// store the new vector in the cache
					Set(hvoObj, tag, newVector);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether the given flid is for an owning property, and if so, sets the
		/// owner, flid, and sequence numbers for all elements of the given vector, which is
		/// assumed to be the vector of all objects owned by hvoOwner in the given flid. This
		/// method should be called whenever a sequence of objects is moved from one
		/// (potentially owning) sequence to another.
		/// </summary>
		/// <param name="hvoOwner">The ID of the object which owns the sequence</param>
		/// <param name="flid">The field ID of the sequence property</param>
		/// <param name="rghvo">Array of objects in the sequence</param>
		/// ------------------------------------------------------------------------------------
		private void SetCmObjectOwnSeqFields(int hvoOwner, int flid, int[] rghvo)
		{
			if (IsOwningProperty(flid))
			{
				for (int i = 0; i < rghvo.Length; i++)
				{
					int newHvo = rghvo[i];
					SetObjProp(newHvo, (int)CmObjectFields.kflidCmObject_Owner, hvoOwner);
					SetObjProp(newHvo, (int)CmObjectFields.kflidCmObject_OwnFlid, flid);
					SetObjProp(newHvo, (int)CmObjectFields.kflidCmObject_OwnOrd, i);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		public void RemoveObjRefs(int hvo)
		{
			CheckDisposed();
		}
		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();
				return m_wsf;
			}
			set
			{
				CheckDisposed();
				m_wsf = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cwsMax"></param>
		/// <param name="ws">writing system id</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int get_WritingSystemsOfInterest(int cwsMax,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(ArrayPtrMarshaler))]
			ArrayPtr/*int[]*/ ws)
		{
			CheckDisposed();
			return 0;
		}

		#region Not implemented methods
		// The following methods are nowhere used, so we don't implement them right now.
		// If we need them, we can port them from the existing C++ code.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="hvoDst"></param>
		/// <param name="bstrExtra"></param>
		/// ------------------------------------------------------------------------------------
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			CheckDisposed();
			throw new NotImplementedException(
				"ISilDataAccess.InsertRelExtra is not yet ported from C++");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="bstrExtra"></param>
		/// ------------------------------------------------------------------------------------
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			CheckDisposed();
			throw new NotImplementedException(
				"ISilDataAccess.UpdateRelExtra is not yet ported from C++");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			CheckDisposed();
			throw new NotImplementedException(
				"ISilDataAccess.GetRelExtra is not yet ported from C++");
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find out whether a particular property (with a particular type) is cached.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="cpt">is a member of <see cref="CellarModuleDefns"/> (defined
		/// in CmTypes.h)</param>
		/// <param name="ws">Writing system id. Ignored unless <paramref name="cpt"/> is kcptMulti</param>
		/// <returns></returns>
		/// <remarks>Eventually we may support using kcptNil as 'any' but not yet.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			CheckDisposed();
			bool fRet = true;
			CacheKey key;
			// create the key that fits to the type
			switch ((CellarModuleDefns)cpt)
			{
				case CellarModuleDefns.kcptMultiString:
				case CellarModuleDefns.kcptMultiBigString:
				case CellarModuleDefns.kcptMultiUnicode:
				case CellarModuleDefns.kcptMultiBigUnicode:
					key = new CacheKeyEx(hvo, tag, ws);
					break;
				case CellarModuleDefns.kcptOwningAtom:
				case CellarModuleDefns.kcptReferenceAtom:
					key = new CacheKeyEx(hvo, tag, (int)CellarModuleDefns.kcptOwningAtom);
					break;
				default:
					key = new CacheKey(hvo, tag);
					break;
			}

			if (!m_htCache.Contains(key))
				fRet = false;
			else
			{
				object obj = m_htCache[key];

				// now check if the object has the expected type
				switch ((CellarModuleDefns)cpt)
				{
					case CellarModuleDefns.kcptBoolean:
					case CellarModuleDefns.kcptInteger:
					case CellarModuleDefns.kcptNumeric:
						fRet &= obj is int;
						break;
					case CellarModuleDefns.kcptFloat:
						fRet &= obj is float;
						break;
					case CellarModuleDefns.kcptTime:
						fRet &= (obj is DateTime || obj is long);
						break;
					case CellarModuleDefns.kcptGuid:
						fRet &= obj is Guid;
						break;
					case CellarModuleDefns.kcptImage:
					case CellarModuleDefns.kcptGenDate:
						// REVIEW (EberhardB): would it be benefical to enable this?
						// Not implemented yet (i.e. we don't want to break our tests, although
						// our cache could handle these types)
						fRet = false;
						break;
					case CellarModuleDefns.kcptBinary:
						fRet &= obj is byte[];
						break;
					case CellarModuleDefns.kcptMultiString:
					case CellarModuleDefns.kcptMultiBigString:
					case CellarModuleDefns.kcptMultiUnicode:
					case CellarModuleDefns.kcptMultiBigUnicode:
					case CellarModuleDefns.kcptString:
					case CellarModuleDefns.kcptBigString:
						fRet &= obj is ITsString;
						break;
					case CellarModuleDefns.kcptUnicode:
					case CellarModuleDefns.kcptBigUnicode:
						fRet &= obj is string;
						break;
					case CellarModuleDefns.kcptOwningAtom:
					case CellarModuleDefns.kcptReferenceAtom:
						fRet &= obj is int;
						break;
					case CellarModuleDefns.kcptOwningCollection:
					case CellarModuleDefns.kcptReferenceCollection:
					case CellarModuleDefns.kcptOwningSequence:
					case CellarModuleDefns.kcptReferenceSequence:
						fRet &= obj is int[];
						break;
					case CellarModuleDefns.kcptNil:
					default:
						throw new ArgumentException("Not a valid property type");
				}
			}
			return fRet;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates if the cache has changed since it was first loaded by means of Set*
		/// methods.  Basically what this means is that client code has called one of the
		/// property modification methods (eg. "Set" methods, <c>DeleteObject*</c>,
		/// <see cref="ISilDataAccess.MakeNewObject"/>, <see cref="MoveOwnSeq"/>, or <c>Replace</c>
		/// methods).
		/// </summary>
		/// <returns><c>true</c> if cache has changed; <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsDirty()
		{
			CheckDisposed();
			return m_htCache.IsDirty;
		}

		/// <summary>
		/// Clear the dirty condition (typically after saving).
		/// </summary>
		public void ClearDirty()
		{
			CheckDisposed();
			m_htCache.ClearDirty();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the meta data cache, if any. Type IUnknown is used to avoid circularity
		/// between FieldWorks components in type definitions.
		/// (Arguably these functions would make more sense in IVwCachDa. But they are
		/// very parallel to the writing system factory methods, which are wellestablished
		/// in this interface.)
		/// Set the meta data cache.
		/// (Note that currently this is most commonly done in the Init method of IVwOleDbDa.
		/// A setter is added here so that nondatabase caches can have metadata.)
		/// </summary>
		/// <value></value>
		/// <returns>A IFwMetaDataCache </returns>
		/// ------------------------------------------------------------------------------------
		public IFwMetaDataCache MetaDataCache
		{
			get
			{
				CheckDisposed();
				return m_mdc;
			}
			set
			{
				CheckDisposed();
				m_mdc = value;
			}
		}

		#endregion

		#endregion

		#region IVwCacheDa methods
		#region Caching objects
		/// ------------------------------------------------------------------------------------
		/// <summary>Cache the value of an atomic object property (owning or reference does
		/// not matter since we are not changing the underlying data).</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='obj'>value</param>
		/// ------------------------------------------------------------------------------------
		public void CacheObjProp(int hvo, int tag, int obj)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);

			Cache(hvo, tag, (int)CellarModuleDefns.kcptOwningAtom, obj);
			// REVIEW: There's more stuff in VwCacheDa::CacheObjProp. Do we need it?
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache the value of a collection or sequence object property (owning or
		/// reference does not matter since we are not changing the underlying data).</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='rghvo'>Array of values</param>
		/// <param name='chvo'>Number of elements in array</param>
		/// ------------------------------------------------------------------------------------
		public void CacheVecProp(int hvo, int tag,
			[MarshalAs(UnmanagedType.LPArray)] int[] rghvo, int chvo)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(chvo == rghvo.Length);

			Cache(hvo, tag, rghvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a binary property.</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>Tag</param>
		/// <param name='rgb'>Array with binary data</param>
		/// <param name='cb'>Number of elements in array</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheBinaryProp(int hvo, int tag,
			[MarshalAs(UnmanagedType.LPArray)] byte[] rgb, int cb)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(cb == rgb.Length);

			Cache(hvo, tag, rgb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a GUID.</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>Tag</param>
		/// <param name='guid'>Guid</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheGuidProp(int hvo, int tag, System.Guid guid)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, guid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a 64-bit integer value</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>64-bit integer value</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheInt64Prop(int hvo, int tag, long val)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a 32-bit integer value</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>32-bit integer value</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheIntProp(int hvo, int tag, int val)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a boolean value</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>Boolean value</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheBooleanProp(int hvo, int tag, bool val)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a <see cref="ITsString"/> property with an encoding.</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='ws'>Encoding</param>
		/// <param name='tss'>TsString object</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, ws, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a <see cref="ITsString"/> property.</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgchTxt'>Text</param>
		/// <param name='cchTxt'>Length of the <paramref name="rgchTxt"/> parameter (in bytes)</param>
		/// <param name='rgbFmt'>Formatting</param>
		/// <param name='cbFmt'>Length of the <paramref name="rgbFmt"/> parameter</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheStringFields(int hvo, int tag, string rgchTxt, int cchTxt,
			[MarshalAs(UnmanagedType.LPArray)] byte[] rgbFmt, int cbFmt)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(cchTxt == rgchTxt.Length);
			Debug.Assert(cbFmt == rgbFmt.Length);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.DeserializeStringRgb(rgchTxt, rgbFmt, cbFmt);
			CacheStringProp(hvo, tag, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a <see cref="ITsString"/> property.</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='tss'>TsString object</param>
		/// <remarks>Methods used for initially loading the cache with object PROPERTY
		/// information (excluding reference information).  Note that after loading the cache,
		/// these methods should NOT be used but rather the Set* methods.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CacheStringProp(int hvo, int tag, ITsString tss)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a Time Property.</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='val'>val</param>
		/// ------------------------------------------------------------------------------------
		public void CacheTimeProp(int hvo, int tag, long val)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache a Unicode Property</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='rgch'>Unicode string</param>
		/// <param name='cch'>Number of characters in <paramref name='rgch'/></param>
		/// ------------------------------------------------------------------------------------
		public void CacheUnicodeProp(int hvo, int tag, string rgch, int cch)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(cch == rgch.Length);
			Cache(hvo, tag, rgch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Cache an Unknown object</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='unk'>unk</param>
		/// ------------------------------------------------------------------------------------
		public void CacheUnknown(int hvo, int tag,
			[MarshalAs(UnmanagedType.IUnknown)] object unk)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Cache(hvo, tag, unk);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Method to retrieve a particular int property if it is in the cache, and
		/// return a bool to say whether it was or not. Similar to
		/// <see cref="ISilDataAccess.get_IntProp"/>, but this method is guaranteed not to do a
		/// lazy load of the property.</summary>
		/// <param name='hvo'>Hvo</param>
		/// <param name='tag'>tag</param>
		/// <param name='fInCache'><c>true</c> if the data was found in the cache, otherwise
		/// <c>false</c></param>
		/// <returns>The requested int property if <paramref name='fInCache'/> is <c>true</c>,
		/// otherwise <c>0</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public int get_CachedIntProp(int hvo, int tag, out bool fInCache)
		{
			CheckDisposed();
			int val = get_IntProp(hvo, tag);
			fInCache = (val != 0);
			return val;
		}

		#endregion

		#region Deleting and creating objects
		/// ------------------------------------------------------------------------------------
		/// <summary>Member CacheReplace</summary>
		/// <param name='hvo'>hvoObj</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvoMin'>ihvoMin</param>
		/// <param name='ihvoLim'>ihvoLim</param>
		/// <param name='rghvo'>rghvo</param>
		/// <param name='chvo'>chvo</param>
		/// ------------------------------------------------------------------------------------
		public void CacheReplace(int hvo, int tag, int ihvoMin, int ihvoLim,
			[MarshalAs(UnmanagedType.LPArray)] int[] rghvo, int chvo)
		{
			CheckDisposed();
			object obj = Get(hvo, tag);
			int[] newItems;
			if (obj != null && obj is int[])
			{
				int[] oldItems = obj as int[];
				newItems = new int[ oldItems.Length - (ihvoLim - ihvoMin) + rghvo.Length];
				Array.Copy(oldItems, 0, newItems, 0, ihvoMin);
				Array.Copy(rghvo, 0, newItems, ihvoMin, rghvo.Length);
				Array.Copy(oldItems, ihvoLim, newItems, ihvoMin + rghvo.Length, oldItems.Length - ihvoLim);
			}
			else
			{
				newItems = rghvo; // assume it was previously empty.
			}
			CacheVecProp(hvo, tag, newItems, newItems.Length);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>Return the index of an object in a vector (or 0 if the property is atomic).
		/// </summary>
		/// <param name='hvoOwn'>The object that contains flid.</param>
		/// <param name='flid'>The property on hvoOwn that holds hvo.</param>
		/// <param name='hvo'>The object contained in the flid property of hvoOwn.</param>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			CheckDisposed();
			Debug.Assert(hvoOwn != 0);
			Debug.Assert(flid != 0);
			Debug.Assert(hvo != 0);

			try
			{
				// If it isn't an atomic property, ignore any resulting error.
				if (get_ObjectProp(hvoOwn, flid) == hvo)
					return 0;
			}
			catch
			{
			}

			object obj = Get(hvoOwn, flid);
			if (obj != null && obj is int[])
			{
				ArrayList list = new ArrayList((int[])obj);
				return list.IndexOf(hvo);
			}

			return (obj == null) ? -1 : 0;

			//			int chvo = get_VecSize(hvoOwn, flid);
			//			for (int ihvo = 0; ihvo < chvo; ++ihvo)
			//			{
			//				if (get_VecItem(hvoOwn, flid, ihvo) == hvo)
			//					return ihvo;
			//			}
			//			return (chvo != 0) ? -1 : 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Return a string giving an outline number such as 1.2.3 based on position in
		/// the owning hierarcy.
		/// </summary>
		/// <param name='hvo'>The object for which we want an outline number.</param>
		/// <param name='flid'>The property on hvo's owner that holds hvo.</param>
		/// <param name='fFinPer'>True if you want a final period appended to the string.</param>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(flid != 0);

			string sNum = "";
			int hvoOwn;
			int ihvo;
			while (true)
			{
				hvoOwn = get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
				if (hvoOwn == 0)
					break;
				ihvo = GetObjIndex(hvoOwn, flid, hvo);
				if (ihvo < 0)
					break;
				if (sNum.Length == 0)
					sNum = string.Format("{0}", ihvo + 1);
				else
					sNum = string.Format("{0}.{1}", ihvo + 1, sNum);
				hvo = hvoOwn;
			}
			if (fFinPer)
				sNum += ".";
			return sNum;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Remove from the cache all information about this object and, if the second
		/// argument is <c>kciaRemoveObjectAndOwnedInfo</c> or <c>kciaRemoveAllObjectInfo</c>,
		/// everything it owns.</summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="cia"></param>
		/// <remarks>
		/// <p>Note that this is not absolutely guaranteed to work. It tells the system that you
		/// no longer need this information cached. However, whether it can find the information
		/// efficiently enough to actually do the deletion depends on whether the implementation
		/// has a MetaDataCache that can tell it what properties the object has, and in the
		/// case of owned objects, it will only find children that are accessible through
		/// properties that are in the cache.</p>
		/// <p>Note that the property that owns this object is not modified.</p>
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			CheckDisposed();
			// This default implementation has no MetaDataCache and so can't do anything useful.
		}
		/// <summary>Member ClearInfoAbout</summary>
		/// <param name='rghvo'>hvo</param>
		/// <param name="chvo"></param>
		/// <param name='cia'>cia</param>
		public void ClearInfoAboutAll(int[] rghvo, int chvo, VwClearInfoAction cia)
		{
			CheckDisposed();
			// This default implementation has no MetaDataCache and so can't do anything useful.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This dummy implementation does nothing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearAllData()
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement virtual properties
		/// </summary>
		/// <param name="vh"></param>
		/// ------------------------------------------------------------------------------------
		public void InstallVirtual(IVwVirtualHandler vh)
		{
			CheckDisposed();
			vh.Tag = ++m_NextTag;
			string key = vh.ClassName + ':' + vh.FieldName;
			m_htVirtualProps.Add(key, vh);
			m_mdc.AddVirtualProp(vh.ClassName, vh.FieldName, (uint)vh.Tag, vh.Type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look up a virtual handler by tag
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			CheckDisposed();
			foreach (IVwVirtualHandler vh in m_htVirtualProps.Values)
			{
				if (vh.Tag == tag)
					return vh;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look up a virtual handler by name
		/// </summary>
		/// <param name="bstrClass"></param>
		/// <param name="bstrField"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			CheckDisposed();
			string key = bstrClass + ':' + bstrField;
			return m_htVirtualProps[key] as IVwVirtualHandler;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy interface method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearVirtualProperties()
		{
			CheckDisposed();
		}
		#endregion

		#region Native C# methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a property from the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual object this[CacheKey key]
		{
			get
			{
				CheckDisposed();
				return m_htCache[key];
			}
			set
			{
				CheckDisposed();
				Debug.Assert(key.Tag != 0);
				m_htCache[key] = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a property in the cache
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="obj">The property</param>
		/// ------------------------------------------------------------------------------------
		public void Set(int hvo, int tag, object obj)
		{
			CheckDisposed();
			this[new CacheKey(hvo, tag)] = obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a property in the cache
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="other">Additional key property</param>
		/// <param name="obj">The property</param>
		/// ------------------------------------------------------------------------------------
		public void Set(int hvo, int tag, int other, object obj)
		{
			CheckDisposed();
			this[new CacheKeyEx(hvo, tag, other)] = obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a property in the cache without modifying the dirty flag.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="obj">The property</param>
		/// ------------------------------------------------------------------------------------
		public void Cache(int hvo, int tag, object obj)
		{
			CheckDisposed();
			this[new CacheKey(hvo, tag)] = obj;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a property in the cache without modifying the dirty flag.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="other">Additional key property</param>
		/// <param name="obj">The property</param>
		/// ------------------------------------------------------------------------------------
		public void Cache(int hvo, int tag, int other, object obj)
		{
			CheckDisposed();
			this[new CacheKeyEx(hvo, tag, other)] = obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a property from the cache
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <returns>The object, or <c>null</c> if not in the cache</returns>
		/// ------------------------------------------------------------------------------------
		public object Get(int hvo, int tag)
		{
			CheckDisposed();
			CacheKey key = new CacheKey(hvo, tag);
			object obj = this[key];
			if (obj == null)
				obj = TryVirtual(key);
			return obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a property from the cache
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="other"></param>
		/// <returns>The object, or <c>null</c> if not in the cache</returns>
		/// ------------------------------------------------------------------------------------
		public object Get(int hvo, int tag, int other)
		{
			CheckDisposed();
			CacheKeyEx key = new CacheKeyEx(hvo, tag, other);
			object obj = this[key];
			if (obj == null)
				obj = TryVirtual(key);
			return obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a property from the cache
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="cpt">The type of the property</param>
		/// <returns>The object, or <c>null</c> if not in the cache</returns>
		/// ------------------------------------------------------------------------------------
		public object Get(int hvo, int tag, CellarModuleDefns cpt)
		{
			CheckDisposed();
			return Get(hvo, tag, (int)cpt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the <see cref="CacheBase"/> contains a specific property.
		/// </summary>
		/// <param name="key">The key</param>
		/// <returns><c>true</c> if the <see cref="CacheBase"/> contains the property.</returns>
		/// ------------------------------------------------------------------------------------
		public bool Contains(CacheKey key)
		{
			CheckDisposed();
			return m_htCache.Contains(key);
		}
		#endregion

		#region IVwOleDbDa Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is relevant only if we're talking to a database that another program may have
		/// changed behind our backs.  This won't happen in our test scenarios.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="cpt"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public void UpdatePropIfCached(int hvo, int tag, int cpt, int ws)
		{
			CheckDisposed();
			return;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the owner of the object
		/// </summary>
		/// <param name="hvo">HVO of object</param>
		/// <returns>Returns the HVO of the owner</returns>
		/// ------------------------------------------------------------------------------------
		public int get_ObjOwner(int hvo)
		{
			CheckDisposed();
			return get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetIdFromGuid(ref Guid guid)
		{
			CheckDisposed();
			return get_ObjFromGuid(guid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the class id of the object
		/// </summary>
		/// <param name="hvo">HVO of object</param>
		/// <returns>Returns the class id</returns>
		/// ------------------------------------------------------------------------------------
		public int get_ObjClid(int hvo)
		{
			CheckDisposed();
			return get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owning flid of the object
		/// </summary>
		/// <param name="hvo">HVO of object</param>
		/// <returns>Returns the flid</returns>
		/// ------------------------------------------------------------------------------------
		public int get_ObjOwnFlid(int hvo)
		{
			CheckDisposed();
			return get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="_rghvo"></param>
		/// <param name="_rgclsid"></param>
		/// <param name="chvo"></param>
		/// <param name="_dts"></param>
		/// <param name="_advi"></param>
		/// <param name="fIncludeOwnedObjects"></param>
		/// ------------------------------------------------------------------------------------
		public void LoadData(int[] _rghvo, int[] _rgclsid, int chvo, IVwDataSpec _dts, IAdvInd _advi, bool fIncludeOwnedObjects)
		{
			CheckDisposed();
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		public void CreateDummyID(out int hvo)
		{
			CheckDisposed();
			hvo = s_hvoNextDummy--;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			CheckDisposed();
			m_htCache.Clear();
			m_htVirtualProps.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the timestamp
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		public void CacheCurrTimeStamp(int hvo)
		{
			CheckDisposed();
			SetTimeStamp(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No need to do anything in here.
		/// </summary>
		/// <remarks>Removes all COM references to the ActionHandler and frees certain
		/// resources. This should be called before the application ends to avoid circular
		/// reference issues.</remarks>
		/// ------------------------------------------------------------------------------------
		public void Close()
		{
			CheckDisposed();
			m_acth = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since we don't deal with a real database, there is no point in checking the cached
		/// timestamp against the timestamp in the database.
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		public void CheckTimeStamp(int hvo)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads FieldWorks data into the cache from the result set defined by the SQL
		/// statement. In this implementation we do nothing here.
		/// </summary>
		/// <param name="bstrSqlStmt"></param>
		/// <param name="_dcs"></param>
		/// <param name="hvoBase"></param>
		/// <param name="nrowMax"></param>
		/// <param name="_advi"></param>
		/// <param name="fNotifyChange"></param>
		/// ------------------------------------------------------------------------------------
		public void Load(string bstrSqlStmt, IDbColSpec _dcs, int hvoBase, int nrowMax, IAdvInd _advi, bool fNotifyChange)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the timestamp for object
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		public void SetTimeStamp(int hvo)
		{
			CheckDisposed();
			m_htCache.SetTimeStamp(hvo, DateTime.Now);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear the dirty flag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			CheckDisposed();
			m_htCache.ClearDirty();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the autoload policy. In this test implementation it has no effect.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AutoloadPolicies AutoloadPolicy
		{
			get
			{
				CheckDisposed();
				return m_autoloadPolicy;
			}
			set
			{
				CheckDisposed();
				m_autoloadPolicy = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SuppressPropChanges()
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResumePropChanges()
		{
			CheckDisposed();
		}
		#endregion

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
		private bool m_isDisposed = false;

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
		~CacheBase()
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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** CacheBase 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Clear();
				if (m_vvnc != null)
					m_vvnc.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_htCache = null;
			m_htVirtualProps = null;
			if (m_wsf != null)
				m_wsf.Shutdown();
			m_wsf = null;
			m_mdc = null;
			m_vvnc = null;
			m_acth = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Additional methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new BOGUS Hvo for the mock book, section, para, etc.
		/// </summary>
		/// <returns>A new BOGUS Hvo</returns>
		/// ------------------------------------------------------------------------------------
		public int NewHvo(int classid)
		{
			CheckDisposed();
			int newHvo = ++s_lastHvo;

			SetInt(newHvo, (int)CmObjectFields.kflidCmObject_Class, classid);
			SetGuid(newHvo, (int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());
			return newHvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the object <paramref name="hvoSrc"/> to the field
		/// <paramref name="flidDestOwner"/> owned by <paramref name="hvoDestOwner"/>
		/// </summary>
		/// <param name="hvoSrc">The object to copy</param>
		/// <param name="hvoDestOwner">The new owner</param>
		/// <param name="flidDestOwner">The field in which to copy</param>
		/// <param name="hvoDstStart">The ID of the object before which the copied object will
		/// be inserted, for owning sequences. This must be -1 for fields that are not owning
		/// sequences. If -1 for owning sequences, the object will be appended to the list.
		/// </param>
		/// <returns>HVO of the new copied object</returns>
		/// ------------------------------------------------------------------------------------
		public int CopyObject(int hvoSrc, int hvoDestOwner, int flidDestOwner, int hvoDstStart)
		{
			CheckDisposed();
			ArrayList toAdd = new ArrayList();

			int hvoDest = CopyObject(hvoSrc, hvoDestOwner, flidDestOwner, toAdd);

			foreach (DictionaryEntry entry in toAdd)
				this[(CacheKey)entry.Key] = entry.Value;

			int type = m_mdc.GetFieldType((uint)flidDestOwner);
			Debug.Assert(type == (int)FieldType.kcptOwningSequence || hvoDstStart == -1,
				"Cannot insert an object into a specific position unless the field is an owning sequence.");

			if (type == (int)FieldType.kcptOwningCollection ||
				type == (int)FieldType.kcptOwningSequence)
			{
				object obj = Get(hvoDestOwner, flidDestOwner);

				List<int> vector = new List<int>();
				if (obj != null)
					vector.AddRange((int[])obj);

				if (type == (int)FieldType.kcptOwningCollection || hvoDstStart == -1)
					vector.Add(hvoDest);
				else
					vector.Insert(vector.IndexOf(hvoDstStart), hvoDest);

				Set(hvoDestOwner, flidDestOwner, vector.ToArray());
			}

			return hvoDest;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the object <paramref name="hvoSrc"/> to the field
		/// <paramref name="flidDestOwner"/> owned by <paramref name="hvoDestOwner"/>
		/// </summary>
		/// <param name="hvoSrc">The object to copy</param>
		/// <param name="hvoDestOwner">The new owner</param>
		/// <param name="flidDestOwner">The field in which to copy</param>
		/// <param name="toAdd"></param>
		/// <returns>HVO of the new copied object</returns>
		/// ------------------------------------------------------------------------------------
		protected int CopyObject(int hvoSrc, int hvoDestOwner, int flidDestOwner, ArrayList toAdd)
		{
			int classId = get_IntProp(hvoSrc, (int)CmObjectFields.kflidCmObject_Class);
			int hvoDest = NewHvo(classId);

			object newValue;
			Hashtable htTemp = (Hashtable)m_htCache.Clone();
			foreach (DictionaryEntry entry in htTemp)
			{
				CacheKey key = entry.Key as CacheKey;
				if (key.Hvo == hvoSrc)
				{
					newValue = entry.Value;
					if (key.Tag == (int)CmObjectFields.kflidCmObject_OwnFlid)
						newValue = flidDestOwner;
					else if (key.Tag == (int)CmObjectFields.kflidCmObject_Owner)
						newValue = hvoDestOwner;
					else if (key.Tag == (int)CmObjectFields.kflidCmObject_Guid)
						newValue = Guid.NewGuid();
					else if (key.Tag > classId * 1000 && key.Tag < (classId + 1) * 1000)
					{
						int type = m_mdc.GetFieldType((uint)key.Tag);
						if (type == (int)FieldType.kcptOwningAtom ||
							type == (int)FieldType.kcptOwningCollection ||
							type == (int)FieldType.kcptOwningSequence)
						{
							// copy the referenced object
							if (entry.Value is int[])
							{
								List<int> vals = new List<int>();
								foreach (int hvo in (int[])entry.Value)
									vals.Add(CopyObject(hvo, hvoDest, key.Tag, toAdd));

								newValue = vals.ToArray();
							}
							else
								newValue = CopyObject((int)entry.Value, hvoDest, key.Tag, toAdd);
						}
					}

					if (entry.Key is CacheKeyEx)
					{
						toAdd.Add(new DictionaryEntry(new CacheKeyEx(hvoDest, key.Tag,
							((CacheKeyEx)entry.Key).Other), newValue));
					}
					else
					{
						toAdd.Add(new DictionaryEntry(new CacheKey(hvoDest, key.Tag), newValue));
					}
				}
			}

			return hvoDest;
		}
		#endregion Additional methods
	}
}
