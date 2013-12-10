// --------------------------------------------------------------------------------------------
// Copyright (C) 2008 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: DomainDataByFlid.cs
// Responsibility: Randy Regnier
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.Application.Impl
{
	/// <summary>
	/// Implementation of the ISilDataAccess which works with a stateful FDO system,
	/// rather than actually being a cache. If at all possible,
	/// clients should work directly with CmObjects and not this class,
	/// as it will be faster to do that.
	/// </summary>
	[ComVisible(true)]
	internal sealed class DomainDataByFlid : ISilDataAccessManaged
	{
		private readonly ICmObjectRepository m_cmObjectRepository;
		private readonly IStTextRepository m_stTxtRepository;
		private readonly IFwMetaDataCacheManaged m_mdc;
		private readonly ISilDataAccessHelperInternal m_uowService;
		private readonly ITsStrFactory m_tsf;
		private readonly ILgWritingSystemFactory m_wsf;

		/// <summary>
		/// Constructor. Called by service locator factory (by reflection).
		/// </summary>
		/// <remarks>
		/// The hvo values are true 'handles' in that they are valid for one session,
		/// but may not be the same integer for another session for the 'same' object.
		/// Therefore, one should not use them for multi-session identity.
		/// CmObject identity can only be guaranteed by using their Guids (or using '==' in code).
		/// </remarks>
		internal DomainDataByFlid(ICmObjectRepository cmObjectRepository, IStTextRepository stTxtRepository,
			IFwMetaDataCacheManaged mdc, ISilDataAccessHelperInternal uowService,
			ITsStrFactory tsf, ILgWritingSystemFactory wsf)
		{
			if (cmObjectRepository == null) throw new ArgumentNullException("cmObjectRepository");
			if (stTxtRepository == null) throw new ArgumentNullException("stTxtRepository");
			if (mdc == null) throw new ArgumentNullException("mdc");
			if (uowService == null) throw new ArgumentNullException("uowService");
			if (tsf == null) throw new ArgumentNullException("tsf");
			if (wsf == null) throw new ArgumentNullException("wsf");

			m_cmObjectRepository = cmObjectRepository;
			m_stTxtRepository = stTxtRepository;
			m_mdc = mdc;
			m_uowService = uowService;
			m_tsf = tsf;
			m_wsf = wsf;
		}

		#region Other methods

		/// <summary>
		/// Get the Ids of the entire vector property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		[ComVisible(false)]
		public int[] VecProp(int hvo, int tag)
		{
			return (from obj in GetInternalInterfaceForObject(hvo).GetVectorProperty(tag) select obj.Hvo).ToArray();
		}

		/// <summary>
		/// Get the requested object and return its ICmObjectInternal interface.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		private ICmObjectInternal GetInternalInterfaceForObject(int hvo)
		{
			return (ICmObjectInternal)m_cmObjectRepository.GetObject(hvo);
		}

		#endregion Other methods

		#region ISilDataAccess implementation

		#region Object Prop methods

		/// <summary>
		/// Obtain the value of an atomic reference property, including owner.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public int get_ObjectProp(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetObjectProperty(tag);
		}

		/// <summary>
		/// Change the value of an atomic REFERENCE property. (Use ${MakeNewObject} or
		/// ${DeleteObjOwner} to make similar changes to owning atomic properties.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='hvoObj'> </param>
		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			ICmObject newValue = null;
			if (hvoObj != FdoCache.kNullHvo)
				newValue = m_cmObjectRepository.GetObject(hvoObj);
			GetInternalInterfaceForObject(hvo).SetProperty(tag, newValue, true);
		}

		#endregion Object Prop methods

		#region Boolean methods

		/// <summary>
		/// Get the value of a boolean property.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public bool get_BooleanProp(int hvo, int tag)
		{
			// No. The SDA supports the model, not ill-behaved clients,
			// who can't tell an int from a bool.
			// The client needs to be fixed to call the right SDA method.
			// If the client can't be bothered to call the right SDA method,
			// it can call the new application service (IntBoolPropertyConverter),
			// which does this switch work.
			//switch ((CellarPropertyType)MetaDataCache.GetFieldType((uint)tag))
			//{
			//    case CellarPropertyType.Boolean:
			//        return GetInternalInterfaceForObject(hvo).GetBoolProperty(tag);
			//    case CellarPropertyType.Integer:
			//        return GetInternalInterfaceForObject(hvo).GetIntegerValue(tag) > 0;
			//    default:
			//        throw new ArgumentException("tag must be an integer or boolean property", "tag");
			//}
			return GetInternalInterfaceForObject(hvo).GetBoolProperty(tag);
		}

		/// <summary>
		/// Change a boolean property of an object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='n'> </param>
		public void SetBoolean(int hvo, int tag, bool n)
		{
			// No. The SDA supports the model, not ill-behaved clients,
			// who can't tell an int from a bool.
			// The client needs to be fixed to call the right SDA method.
			// If the client can't be bothered to call the right SDA method,
			// it can call the new application service (IntBoolPropertyConverter),
			// which does this switch work.
			//switch ((CellarPropertyType)MetaDataCache.GetFieldType((uint)tag))
			//{
			//    case CellarPropertyType.Boolean:
			//GetInternalInterfaceForObject(hvo).SetProperty(tag, n, true);
			//        break;
			//    case CellarPropertyType.Integer:
			//        GetInternalInterfaceForObject(hvo).SetProperty(tag, n ? 1 : 0, true);
			//        break;
			//    default:
			//        throw new ArgumentException("tag must be an integer or boolean property", "tag");
			//}
			GetInternalInterfaceForObject(hvo).SetProperty(tag, n, true);
		}

		#endregion Boolean methods

		#region Guid methods

		/// <summary>
		/// Get the value of a property whose value is a GUID.
		/// S_FALSE if no value is known for this property. pguid will be GUID_NULL,
		/// all zeros.
		/// Some implementations may return S_OK and set <i>puid</i> to zero.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public Guid get_GuidProp(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetGuidProperty(tag);
		}

		/// <summary>
		/// Change a GUID property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='uid'> </param>
		public void SetGuid(int hvo, int tag, Guid uid)
		{
			GetInternalInterfaceForObject(hvo).SetProperty(tag, uid, true);
		}

		/// <summary>
		/// Get the object that has the given guid.
		/// S_FALSE if no value is known for this property. Hvo will be 0.
		/// Some implementations may return S_OK and set <i>pHvo</i> to zero.
		///</summary>
		/// <param name='uid'> </param>
		/// <returns></returns>
		public int get_ObjFromGuid(Guid uid)
		{
			return m_cmObjectRepository.GetObject(uid).Hvo;
		}

		#endregion Guid methods

		#region Int methods

		/// <summary>
		/// Get the value of a integer property. May also be used for enumerations.
		/// 0 if no value known for this property.
		/// ENHANCE JohnT: shouldn't it also return S_FALSE?
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public int get_IntProp(int hvo, int tag)
		{
			try
			{
				return GetInternalInterfaceForObject(hvo).GetIntegerValue(tag);
			}
			catch (KeyNotFoundException)
			{
				// Special case: kflidClass may be used to find out whether the object is valid.
				if (tag == CmObjectTags.kflidClass)
					return 0;
				throw;
			}
		}

		/// <summary>
		/// Change an integer property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='n'> </param>
		public void SetInt(int hvo, int tag, int n)
		{
			GetInternalInterfaceForObject(hvo).SetProperty(tag, n, true);
		}

		#endregion Int methods

		#region GenDate methods

		/// <summary>
		/// Get the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <returns>The generic date.</returns>
		public GenDate get_GenDateProp(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetGenDateProperty(tag);
		}

		/// <summary>
		/// Set the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="genDate">The generic date.</param>
		public void SetGenDate(int hvo, int tag, GenDate genDate)
		{
			GetInternalInterfaceForObject(hvo).SetProperty(tag, genDate, true);
		}

		#endregion

		#region DateTime methods
		/// <summary>
		/// Get the time property as a DateTime value.
		/// </summary>
		public DateTime get_DateTime(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetTimeProperty(tag);
		}

		/// <summary>
		/// Change a time property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		public void SetDateTime(int hvo, int tag, DateTime dt)
		{
			GetInternalInterfaceForObject(hvo).SetProperty(tag, dt, true);
		}
		#endregion

		#region Unicode methods

		/// <summary>
		/// Read a Unicode string property. (Note also ${#UnicodePropRgch} if you don't want
		/// a BSTR allocated.
		/// NULL, S_FALSE if property not found. Some implementations may return
		/// S_OK and empty string if property not found.
		/// Sets a Unicode string property.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public string get_UnicodeProp(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetStringProperty(tag);
		}

		/// <summary>
		/// Read a Unicode string property. (Note also ${#UnicodePropRgch} if you don't want
		/// a BSTR allocated.
		/// NULL, S_FALSE if property not found. Some implementations may return
		/// S_OK and empty string if property not found.
		/// Sets a Unicode string property.
		///</summary>
		/// <param name='obj'> </param>
		/// <param name='tag'> </param>
		/// <param name='bstr'> </param>
		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			GetInternalInterfaceForObject(obj).SetProperty(tag, bstr, true);
		}

		/// <summary>
		/// Change a Unicode property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_rgch'> </param>
		/// <param name='cch'> </param>
		public void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			// Pass to 'set_UnicodeProp' method and ignore 'cch';
			set_UnicodeProp(hvo, tag, _rgch);
		}

		/// <summary>
		/// Read a Unicode string property. See ${get_UnicodeProp} for a BSTR result.
		/// @error E_FAIL if buffer too small.
		/// pcch 0, S_FALSE if property not found (or S_OK from some implementations).
		///</summary>
		/// <param name='obj'> </param>
		/// <param name='tag'> </param>
		/// <param name='_rgch'>Buffer for result. Pass NULL to inquire length. </param>
		/// <param name='cchMax'>Buffer length for result. Pass 0 to inquire length. </param>
		/// <param name='_cch'> </param>
		public void UnicodePropRgch(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*OLECHAR[]*/ _rgch, int cchMax, out int _cch)
		{
			var str = get_UnicodeProp(obj, tag);
			_cch = str.Length;
			if (cchMax == 0 || _rgch == null)
				return; // Caller only wants the size, in a first call.

			if (_cch >= cchMax)
				throw new ArgumentException("_cch cannot be larger than cchMax");

			MarshalEx.StringToNative(_rgch, cchMax, str, true);
		}

		#endregion Unicode methods

		#region Time methods

		/// <summary> Read a time property.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns>Actually an SilTime. 0 if property not found.</returns>
		public long get_TimeProp(int hvo, int tag)
		{
			DateTime dt = GetInternalInterfaceForObject(hvo).GetTimeProperty(tag);
			return SilTime.ConvertToSilTime(dt);
		}

		/// <summary>
		/// Change a time property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='lln'> </param>
		public void SetTime(int hvo, int tag, long lln)
		{
			DateTime dt = SilTime.ConvertFromSilTime(lln);
			GetInternalInterfaceForObject(hvo).SetProperty(tag, dt, true);
		}

		#endregion Time methods

		#region Int64 methods

		/// <summary>
		/// Get the value of a 64bit integer property (often actually a time).
		/// 0 if no value known for this property.
		/// ENHANCE JohnT: shouldn't it also return S_FALSE?
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public long get_Int64Prop(int hvo, int tag)
		{
			// If this is for GenDate properties,
			// then FDO needs more work, as it generates GenDates as int32 not int64.
			throw new NotImplementedException("'get_Int64Prop' not implemented yet.");
		}

		/// <summary>
		/// Change a long integer property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='lln'> </param>
		public void SetInt64(int hvo, int tag, long lln)
		{
			throw new NotImplementedException("'SetInt64' not implemented yet.");
		}

		#endregion Int64 methods

		#region Unknown methods

		/// <summary>
		/// Get an object which is typically a non-CmObject derived from a Binary field.
		/// It is up to each SilDataAccess what kinds of objects it can persist in this way.
		/// The current ones mostly use this for ${ITsTextProps}.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public object get_UnknownProp(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetITsTextPropsProperty(tag);
		}

		/// <summary>
		/// Change a binary property of an object to a suitable representation of
		/// the object represented by the IUnknown. Particular implementations may
		/// differ in the range of object types supported. The current implementation
		/// only handles ITsTextProps objects.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_unk'> </param>
		public void SetUnknown(int hvo, int tag, object _unk)
		{
			if (_unk != null && !(_unk is ITsTextProps))
				throw new ArgumentException("We only support ITsTextProps objects in this property.");

			GetInternalInterfaceForObject(hvo).SetProperty(tag, _unk as ITsTextProps, true);
		}

		#endregion Unknown methods

		#region Binary methods

		/// <summary>
		/// Get the value of a binary property.
		/// @param prgb, cbMax Buffer to which to copy data. May pass NULL, 0 to request
		/// required length.
		/// @error E_FAIL if buffer is too small (other than zero length).
		/// S_FALSE if no value is known for this property. pcb will be zero.
		/// Some implementations may return S_OK and set <i>pcb</i> to zero.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_rgb'> </param>
		/// <param name='cbMax'> </param>
		/// <param name='_cb'>Indicates how many bytes of binary data were read. </param>
		public void BinaryPropRgb(int hvo, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*byte[]*/ _rgb, int cbMax, out int _cb)
		{
			var array = GetInternalInterfaceForObject(hvo).GetBinaryProperty(tag);
			_cb = array.Length;
			if (cbMax == 0)
				return;
			if (_cb > cbMax)
				throw new ArgumentException("cb cannot be larger than cbMax");

			MarshalEx.ArrayToNative(_rgb, cbMax, array);
		}

		/// <summary>
		/// Get the binary data property of an object.
		///</summary>
		/// <param name='hvo'></param>
		/// <param name='tag'></param>
		/// <param name='rgb'>Contains the binary data</param>
		/// <returns>byte count in binary data property</returns>
		[ComVisible(false)]
		public int get_Binary(int hvo, int tag, out byte[] rgb)
		{
			rgb = GetInternalInterfaceForObject(hvo).GetBinaryProperty(tag);
			if (rgb == null)
				return 0;
			return rgb.Length;
		}

		/// <summary>
		/// Change a binary data property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_rgb'> </param>
		/// <param name='cb'> </param>
		public void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
		{
			GetInternalInterfaceForObject(hvo).SetProperty(tag, _rgb, true);
		}

		#endregion Binary methods

		#region String methods

		/// <summary>
		/// Read a (nonmultilingual) string property.
		/// return an empty string, in the appropriate default writing system for the property, if value not found.
		///</summary>
		public ITsString get_StringProp(int hvo, int tag)
		{
			var obj = GetInternalInterfaceForObject(hvo);
			var result = obj.GetITsStringProperty(tag);
			if (result != null)
				return result;
			int ws;
			switch (m_mdc.GetFieldWs(tag))
			{
				case WritingSystemServices.kwsAnal:
					ws = obj.Cache.DefaultAnalWs;
					break;
				case WritingSystemServices.kwsVern:
					ws = obj.Cache.DefaultVernWs;
					break;
				default:
					ws = WritingSystemFactory.UserWs; // a desperate default.
					break;
			}
			return m_tsf.EmptyString(ws);
		}

		/// <summary>
		/// Change a stringvalued property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_tss'> </param>
		public void SetString(int hvo, int tag, ITsString _tss)
		{
			GetInternalInterfaceForObject(hvo).SetProperty(tag, _tss, true);
		}

		#endregion String methods

		#region MultiString/MultiUnicode methods

		/// <summary>
		/// Get the value of one alternative of a Multilingual alternation.
		/// an empty string in the correct writing system if no value recorded.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='ws'> </param>
		/// <returns></returns>
		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			ITsMultiString tms = get_MultiStringProp(hvo, tag);
			if (tms == null)
				return m_tsf.MakeString("", ws);
			else
				return tms.get_String(ws);
		}

		/// <summary>
		/// Method used to get a whole MultiString.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetITsMultiStringProperty(tag);
		}

		/// <summary>
		/// Change one alternative of a multilingual string property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='ws'> </param>
		/// <param name='_tss'> </param>
		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			get_MultiStringProp(hvo, tag).set_String(ws, _tss);
		}

		#endregion MultiString/MultiUnicode methods

		#region Vector methods

		/// <summary> Get the full contents of the specified sequence in one go.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='chvoMax'> </param>
		/// <param name='_chvo'> </param>
		/// <param name='_rghvo'> </param>
		public void VecProp(int hvo, int tag, int chvoMax, out int _chvo, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 2)] ArrayPtr/*long[]*/ _rghvo)
		{
			_chvo = DomainDataByFlidServices.ComVecPropFromManagedVecProp(VecProp(hvo, tag), hvo, tag, _rghvo, chvoMax);
		}

		/// <summary>
		/// Obtain one item from an object sequence or collection property.
		/// @error E_INVALIDARG if index is out of range.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='index'>Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;. </param>
		/// <returns></returns>
		public int get_VecItem(int hvo, int tag, int index)
		{
			return GetInternalInterfaceForObject(hvo).GetVectorItem(tag, index);
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public int get_VecSize(int hvo, int tag)
		{
			return GetInternalInterfaceForObject(hvo).GetVectorSize(tag);
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			// It's all the same in a stateful FDO world.
			return get_VecSize(hvo, tag);
		}

		/// <summary> Return the index of hvo in the flid vector of hvoOwn.</summary>
		/// <param name='hvoOwn'>The object ID of the owner.</param>
		/// <param name='flid'>The parameter on hvoOwn that owns hvo.</param>
		/// <param name='hvo'>The target object ID we are looking for.</param>
		/// <returns>
		/// The index, or -1 if not found.
		/// </returns>
		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			return GetInternalInterfaceForObject(hvoOwn).GetObjIndex(flid, hvo);
		}

		/// <summary>
		/// Change the owner of a range of objects in a sequence (given by the indexes
		/// ihvoStart and ihvoEnd) and insert them in another sequence. The "ord" values
		/// change accordingly (first one to ihvoDstStart).
		/// ENHANCE JohnT: there does not appear to be any corresponding way to move an object
		/// from one atomic owning property to another.
		/// The caller should also call PropChanged to notify interested parties.
		///</summary>
		/// <param name='hvoSrcOwner'> </param>
		/// <param name='tagSrc'> </param>
		/// <param name='ihvoStart'> </param>
		/// <param name='ihvoEnd'> </param>
		/// <param name='hvoDstOwner'> </param>
		/// <param name='tagDst'> </param>
		/// <param name='ihvoDstStart'> </param>
		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			var srcObj = GetInternalInterfaceForObject(hvoSrcOwner);
			var srcObjs = srcObj.GetVectorProperty(tagSrc);
			var objsToAdd = from obj in srcObjs.Skip(ihvoStart).Take(ihvoEnd - ihvoStart + 1) select obj;
			var dstObj = GetInternalInterfaceForObject(hvoDstOwner);
			dstObj.Replace(tagDst, ihvoDstStart, 0, objsToAdd);
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
			var ownee = GetInternalInterfaceForObject(hvo);
			var dstObj = GetInternalInterfaceForObject(hvoDstOwner);
			switch ((CellarPropertyType)MetaDataCache.GetFieldType(tagDst))
			{
				case CellarPropertyType.OwningCollection:
					dstObj.Replace(tagDst, new ICmObject[0], new ICmObject[] { ownee });
					break;
				case CellarPropertyType.OwningSequence:
					dstObj.Replace(tagDst, ihvoDstStart, 0, new ICmObject[] { ownee });
					break;
				case CellarPropertyType.OwningAtomic:
					dstObj.SetProperty(tagDst, ownee, true);
					break;
				default:
					throw new ArgumentException("tagDst must be an owning property", "tagDst");
			}
		}

		/// <summary>
		/// Replace the range of objects (indexes) [ihvoMin, ihvoLim) in property tag of object hvoObj
		/// with the sequence of chvo objects at prghvo. (prghvo may be null if chvo is zero;
		/// this amounts to a deletion).
		/// Use this for REFERENCE sequences and collections; use methods like ${#MoveOwnSeq},
		/// ${#MakeNewObject}, or ${#DeleteObjOwner} to make similar changes to owning sequences
		/// and collections.
		/// The actual objects deleted will be the ones at the specified positions in the cache.
		/// Therefore if you are using a collection it is important to be sure that the way the
		/// items are ordered is going to give the effect you expect. (Indeed, even for sequences,
		/// you could load things into the cache in some order other than by their ord field,
		/// though this would be unusual.)
		/// The caller should wrap the action in an UndoableUnitOfWork.
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvoObj'> </param>
		/// <param name='tag'> </param>
		/// <param name='ihvoMin'> </param>
		/// <param name='ihvoLim'> </param>
		/// <param name='_rghvo'> </param>
		/// <param name='chvo'> </param>
		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			var obj = GetInternalInterfaceForObject(hvoObj);
			IEnumerable<ICmObject> objsToAdd;
			if (_rghvo == null && chvo == 0)
			{
				objsToAdd = from hvo in new int[] {0}.Take(chvo)
							select m_cmObjectRepository.GetObject(hvo);
			}
			else
			{
				objsToAdd = from hvo in _rghvo.Take(chvo)
								select m_cmObjectRepository.GetObject(hvo);
			}

			switch ((CellarPropertyType)MetaDataCache.GetFieldType(tag))
			{
				case CellarPropertyType.ReferenceCollection:
					var oldObjects = obj.GetVectorProperty(tag);
					// does this even make sense for collections? should we restrict this method
					// to only allow replacing a whole collection?
					var objsToDel = from item in oldObjects.Skip(ihvoMin).Take(ihvoLim - ihvoMin) select item;
					obj.Replace(tag, objsToDel, objsToAdd);
					break;
				case CellarPropertyType.ReferenceSequence:
					obj.Replace(tag, ihvoMin, ihvoLim - ihvoMin, objsToAdd);
					break;
				default:
					throw new ArgumentException("tag must be a reference collection or reference sequence property", "tag");
			}
		}

		#endregion Vector methods

		#region Undo/Redo methods

		/// <summary>
		/// Begin a sequence of actions that will be treated as one task for the purposes
		/// of undo and redo. If there is already such a task in process, this sequence will be
		/// included (nested) in that one, and the descriptive strings will be ignored.
		///</summary>
		/// <param name='bstrUndo'>Short description of an action. This is intended to appear on the
		/// "undo" menu item (e.g. "Typing" or "Clear") </param>
		/// <param name='bstrRedo'>Short description of an action. This is intended to appear on the
		/// "redo" menu item (e.g. "Typing" or "Clear"). Usually, this is the same as &lt;i&gt;bstrUndo&lt;/i&gt; </param>
		/// <remarks>
		/// This method must not be called before the ending 'EndUndoTask' method has been called.
		/// That is, this ISILDataAccess implementation does not support 'nested' tasks.
		/// </remarks>
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_uowService.CurrentUndoStack.BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		///</summary>
		public void EndUndoTask()
		{
			m_uowService.CurrentUndoStack.EndUndoTask();
		}

		/// <summary>
		/// Begin a sequence on non-undoable tasks.
		/// </summary>
		public void BeginNonUndoableTask()
		{
			m_uowService.ActiveUndoStack.BeginNonUndoableTask();
		}

		/// <summary>
		/// End a non-undoable task.
		/// </summary>
		public void EndNonUndoableTask()
		{
			m_uowService.ActiveUndoStack.EndNonUndoableTask();
		}

		/// <summary>
		/// Continue the previous sequence. This is intended to be called from a place like
		/// OnIdle that performs "cleanup" operations that are really part of the previous
		/// sequence.
		///</summary>
		public void ContinueUndoTask()
		{
			m_uowService.ActiveUndoStack.ContinueUndoTask();
		}

		/// <summary>
		/// End the current sequence, and any outer ones that are in progress. This is intended
		/// to be used as a cleanup function to get everything back in sync.
		///</summary>
		public void EndOuterUndoTask()
		{
			m_uowService.ActiveUndoStack.EndOuterUndoTask();
		}

		/// <summary>
		///
		/// </summary>
		public void Rollback()
		{
			m_uowService.ActiveUndoStack.Rollback(0);
		}

		/// <summary>
		/// Break the current undo task into two at the current point. Subsequent actions will
		/// be part of the new task which will be assigned the given labels.
		///</summary>
		/// <param name='bstrUndo'> </param>
		/// <param name='bstrRedo'> </param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			m_uowService.ActiveUndoStack.BreakUndoTask(bstrUndo, bstrRedo);
		}

		#endregion Undo/Redo methods

		#region Action Handling

		/// <summary>
		/// Return the IActionHandler that is being used to record undo information.
		/// May be NULL
		///</summary>
		/// <returns></returns>
		public IActionHandler GetActionHandler()
		{
			return m_uowService.ActiveUndoStack;
		}

		/// <summary>Member SetActionHandler</summary>
		/// <param name='actionhandler'>action handler</param>
		public void SetActionHandler(IActionHandler actionhandler)
		{
			//m_ah = actionhandler;
			// I (RandyR) think a better way to suppress undo activity
			// would be to add a new 'BeginNonUndoableTask' method.
			// which would commit any earlier undoable work,
			// and ignore following tasks until the terminating End Task method call.
			// This would allow this class to keep track of new/modified/deleted CmObjects
			// that must be committed to the BEP.
			throw new NotSupportedException("'SetActionHandler' not supported.");
		}

		#endregion Action Handling

		#region Custom Field (Extra) methods

		#endregion Custom Field (Extra) methods

		#region Basic Prop methods

		/// <summary>
		/// Read an arbitrary property as a variant. The view subsystem does not care what
		/// kind of value you put in the variant. However, when you use AddProp, the view
		/// subsystem calls back to Prop to get the variant which is then passed to
		/// DisplayVariant. DisplayVariant must be prepared to handle whatever Prop puts there.
		/// If you put an IUnknown or IDispatch value in the variant, the Views code will
		/// call Release once on that object. Normally you should pass a ${SmartVariant}.
		/// S_FALSE (and variant VTEMPTY) if no known value for this property, or
		/// if the implemetation does not know how to represent it as a variant. Current
		/// implementations can represent 32 and 64 bit integers and (nonmultilingual) strings.
		/// Some implementations may return S_OK and variant VTEMPTY.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public object get_Prop(int hvo, int tag)
		{
			object retval;
			var obj = GetInternalInterfaceForObject(hvo);
			switch ((CellarPropertyType)m_mdc.GetFieldType(tag))
			{
				default:
					// case CellarPropertyType.Float: // Fall through
					// case CellarPropertyType.Image: // Fall through
					// case CellarPropertyType.Numeric:
					throw new ArgumentException("Unused data tyep.");

					// Basic data types.
				case CellarPropertyType.Binary:
					retval = obj.GetBinaryProperty(tag);
					break;
				case CellarPropertyType.Boolean:
					retval = obj.GetBoolProperty(tag);
					break;
				case CellarPropertyType.Guid:
					retval = obj.GetGuidProperty(tag);
					break;
				case CellarPropertyType.GenDate: // Fall through, since a GenDate is an int.
				case CellarPropertyType.Integer:
					retval = obj.GetIntegerValue(tag);
					break;
				case CellarPropertyType.Time:
					retval = obj.GetTimeProperty(tag).Ticks;
					break;

					// String types.
				case CellarPropertyType.Unicode:
					retval = obj.GetStringProperty(tag);
					break;
				case CellarPropertyType.String:
					retval = obj.GetITsStringProperty(tag);
					break;
				case CellarPropertyType.MultiString: // Fall through.
				case CellarPropertyType.MultiUnicode:
					retval = get_MultiStringProp(hvo, tag);
					break;

					// Atomic properties (owning or reference).
				case CellarPropertyType.OwningAtomic: // Fall through
				case CellarPropertyType.ReferenceAtomic:
					// Will be "FdoCache.kNullHvo" for a null value
					retval = obj.GetObjectProperty(tag);
					break;

					// Vector Properties (sequence or collection, owning or reference).
				case CellarPropertyType.OwningCollection: // Fall through
				case CellarPropertyType.OwningSequence: // Fall through
				case CellarPropertyType.ReferenceCollection: // Fall through
				case CellarPropertyType.ReferenceSequence:
					retval = (from item in obj.GetVectorProperty(tag) select item.Hvo).ToArray();
					break;
			}
			return retval;
		}

		/// <summary>
		/// Find out whether a particular property is cached.
		/// Eventually we may support using kcptNil as 'any' but not yet.
		/// cpt is a member of the defines in CmTypes.h
		/// ws is ignored unless cpt is kcptMulti...
		///</summary>
		/// <param name='hvo'></param>
		/// <param name='tag'></param>
		/// <param name='cpt'></param>
		/// <param name='ws'></param>
		/// <returns></returns>
		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			// The implementation here will check for a match between the hvo and the tag,
			// to make sure they are compatible. If they are not, then expect an exception
			// from the MetaDataCache. If they are compatible, then you get 'true',
			// whether the actual value is null (reference type data only) or has data.
			// The caller will need to worry about the possible null value.

			// Make sure the object has the flid.
			var flid = m_mdc.GetFieldId2(
				// Throws an exception, if the object does not exist.
				m_cmObjectRepository.GetObject(hvo).ClassID,
				// Throws an exception, if there is no such tag for any class of object.
				m_mdc.GetFieldName(tag),
				// Check superclasses.
				true);

			if (flid <= 0)
				throw new ArgumentException("'hvo' does not have the 'tag' property.");

			return true; // Whew! What a guantlet.
		}

		#endregion Basic Prop methods

		#region Basic Object methods

		/// <summary>
		/// Delete the specified object.
		///</summary>
		/// <param name='hvoObj'> </param>
		public void DeleteObj(int hvoObj)
		{
			// Will throw an exception, if not in Dictionary.
			var goner = m_cmObjectRepository.GetObject(hvoObj);
			DeleteObjOwner((goner.Owner == null) ? 0 : goner.Owner.Hvo,
						   goner.Hvo,
						   goner.OwningFlid,
						   0);
		}

		/// <summary>
		/// Delete an object and clean up the owning property and any inbound references to
		/// the soon to be deleted object.
		///</summary>
		/// <param name='hvoOwner'>Hvo of the owning object.</param>
		/// <param name='hvoObj'>Hvo of the object to delete.</param>
		/// <param name='tag'>The owning field of object to delete.</param>
		/// <param name='ihvo'>It doesn't matter what value if given for 'ivho' by
		/// clients such as the Views code, as long as it isnt FdoCache.kFDODeletingObjectIndex.
		/// The caller should also call PropChanged to notify interested parties. </param>
		/// <remarks>
		/// NB: This method will also clean up any inbound references to the deleted object.
		/// This is not specified by the interface documents,
		/// but is done for data integrity purposes in the DB-less FDO system.
		/// </remarks>
		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			m_cmObjectRepository.GetObject(hvoObj).Delete();
		}

		/// <summary>
		/// Insert chvo new objects after the one at ihvo, which functions as a pattern.
		/// Typically used when splitting a paragraph at ihvo.
		/// The new objects should generally be similar to the one at ihvo, except that
		/// the main text property that forms the paragraph body should be empty.
		/// If the object has a paragraph style property, the new objects should have
		/// the same style as the one at ihvo, except that, if a stylesheet is passed,
		/// each successive paragraph inserted should have the appropriate next style
		/// for the one named in the previous paragraph.
		/// The caller should also call PropChanged to notify interested parties.
		///</summary>
		/// <param name='hvoObj'> </param>
		/// <param name='tag'> </param>
		/// <param name='ihvo'> </param>
		/// <param name='chvo'> </param>
		/// <param name='ss'> </param>
		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet ss)
		{
			if (chvo != 1 || tag != StTextTags.kflidParagraphs)
				throw new NotImplementedException("'InsertNew' not implemented yet except for adding one paragraph to a text.");
			IStText text = m_cmObjectRepository.GetObject(hvoObj) as IStText;
			if (text == null)
				throw new InvalidOperationException("'InsertNew' asked to add a paragraph to something that is not an StText");
			IStTxtPara oldPara = text[ihvo];
			int clid = oldPara.ClassID;
			int hvoNew = MakeNewObject(clid, hvoObj, tag, ihvo + 1);
			IStTxtPara newPara = (IStTxtPara)m_cmObjectRepository.GetObject(hvoNew);
			string newStyleName = oldPara.StyleName;
			if (ss != null && !string.IsNullOrEmpty(newStyleName))
			{
				string nextStyleName = ss.GetNextStyle(oldPara.StyleName);
				if (!string.IsNullOrEmpty(nextStyleName))
					newStyleName = nextStyleName;
			}
			if (!string.IsNullOrEmpty(newStyleName))
				newPara.StyleName = newStyleName;
		}

		/// <summary>
		/// Make a new object owned in a particular position. The object is created immediately.
		/// (Actually in the database, in database implementations; this will roll back if
		/// the transaction is not committed.)
		/// If ord is &gt;= 0, the object is inserted in the appropriate place in the (presumed
		/// sequence) property, both in the database itself and in the data access object's
		/// internal cache, if that property is cached.
		/// If ord is less than 0, the property must not be a sequence; use -2 for an atomic property,
		/// and -1 for a collection.
		///</summary>
		/// <param name='clid'> </param>
		/// <param name='hvoOwner'> </param>
		/// <param name='tag'> </param>
		/// <param name='ord'> </param>
		/// <returns></returns>
		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			if ((clid == StTxtParaTags.kClassId || clid == ScrTxtParaTags.kClassId) && tag == StTextTags.kflidParagraphs)
			{
				// The new object is a paragraph owned by an StText. This requires special
				// handling to create the correct paragraph type (ScrTxtPara or StTxtPara).
				IStText objOwner = m_stTxtRepository.GetObject(hvoOwner);
				Debug.Assert(ord >= 0 && ord <= objOwner.ParagraphsOS.Count);

				// Try to get the default paragraph style for the new paragraph
				string paraStyleName = null;
				// Watch out for the case (FWR-3477) where paragraph is owned by a CmPossibility first!
				if (objOwner.OwnerOfClass<IScripture>() != null && !(objOwner.Owner is ICmPossibility))
				{
					// Paragraph will be owned by Scripture. We need to find the correct
					// style for the new paragraph.
					switch (objOwner.OwningFlid)
					{
						case ScrSectionTags.kflidHeading:
							paraStyleName = (((IScrSection)objOwner.Owner).IsIntro) ? ScrStyleNames.IntroSectionHead :
								ScrStyleNames.SectionHead;
							break;
						case ScrSectionTags.kflidContent:
							paraStyleName = (((IScrSection)objOwner.Owner).IsIntro) ? ScrStyleNames.IntroParagraph :
								ScrStyleNames.NormalParagraph;
							break;
						case ScrBookTags.kflidTitle:
							paraStyleName = ScrStyleNames.MainBookTitle;
							break;
						case ScrBookTags.kflidFootnotes:
							paraStyleName = ScrStyleNames.NormalFootnoteParagraph;
							break;
						case ScrScriptureNoteTags.kflidQuote:
						case ScrScriptureNoteTags.kflidResponses:
						case ScrScriptureNoteTags.kflidDiscussion:
						case ScrScriptureNoteTags.kflidResolution:
						case ScrScriptureNoteTags.kflidRecommendation:
							paraStyleName = ScrStyleNames.Remark;
							break;
						default:
							throw new ArgumentException("Unsupported Scripture paragraph owner.");
					}
				}

				return objOwner.InsertNewTextPara(ord, paraStyleName).Hvo;
			}

			var owner = (ICmObjectInternal)m_cmObjectRepository.GetObject(hvoOwner);
			IFdoFactoryInternal factory = (IFdoFactoryInternal)owner.Cache.ServiceLocator.GetInstance(
				GetServicesFromFWClass.GetFactoryTypeFromFWClassID(
				owner.Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>(),
				clid));
			var obj = factory.CreateInternal();
			switch(ord)
			{
				case -1:
					owner.Replace(tag, new ICmObject[0], new[] {obj});
					break;
				case -2:
					owner.SetProperty(tag, obj, true);
					break;
				default:
					owner.Replace(tag, ord, 0, new[] {obj});
					break;
			}
			return obj.Hvo;
		}

		/// <summary>
		/// Test whether an HVO represents a valid object. For the DB-less cache,
		/// the object must be in the m_extantObjectsByHvo data member.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public bool get_IsValidObject(int hvo)
		{
			if (hvo == 0)
				throw new ArgumentException("'hvo' cannot be 0.");

			if (!m_cmObjectRepository.IsValidObjectId(hvo))
				return false;
			var obj = GetInternalInterfaceForObject(hvo);

			// It's just possible that, though we can find it, it's not a truly valid object.
			return (obj.Hvo != (int)SpecialHVOValues.kHvoObjectDeleted && obj.Hvo != (int)SpecialHVOValues.kHvoUninitializedObject);
		}

		/// <summary>
		/// This processes all atomic and sequence owning and reference props in the cache
		/// and removes the given hvo from any property where it is found. PropChanged is
		/// called on each modified property to notify interested parties.
		///</summary>
		/// <param name='hvo'> </param>
		public void RemoveObjRefs(int hvo)
		{
			throw new NotImplementedException("'RemoveObjRefs' not implemented (and won't be).");
		}

		#endregion Basic Object methods

		#region Custom Field (Extra) methods

		/// <summary>Member InsertRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='hvoDst'>hvoDst</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			throw new NotImplementedException("'InsertRelExtra' not implemented yet.");
		}

		/// <summary>Member UpdateRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			throw new NotImplementedException("'UpdateRelExtra' not implemented yet.");
		}

		/// <summary>Member GetRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <returns>A System.String</returns>
		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			throw new NotImplementedException("'GetRelExtra' not implemented yet.");
		}
		#endregion Custom Field (Extra) methods

		#region Notification methods

		/// <summary>
		/// Request notification when properties change. The ${IVwNotifyChange#PropChanged}
		/// method will be called when the property changes (provided the client making the
		/// change properly calls ${#PropChanged}.
		///</summary>
		/// <param name='nchng'> </param>
		public void AddNotification(IVwNotifyChange nchng)
		{
			m_uowService.AddNotification(nchng);
		}

		/// <summary>
		/// Notify clients who have requested it that the specified property is changing.
		/// The last five arguments indicate the nature of the change, as in
		/// ${IVwNotifyChange#PropChanged}. In general, that method will be called for
		/// all clients that have requested notification. Certain variations in this
		/// process can be made using the first two arguments.
		/// If pct is kpctNotifyAll, the first argument is ignored, and all clients are
		/// notified in an arbitrary order. (Currently this is also the default behavior
		/// if some unrecognized constant is passed. This may eventually become an error.)
		/// If pct is kpctNotifyMeThenAll, then the object indicated by the first argument
		/// is notified first. This allows the main focus window to update first.
		/// If pct is kpctNotifyAllButMe, then the object indicated by the first argument
		/// is not notified at all, even if it is listed as requesting notification. This
		/// is useful when the object making the change has already done the work that it
		/// would normally do when receiving such a notification.
		///</summary>
		/// <param name='_nchng'> </param>
		/// <param name='_ct'> </param>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='ivMin'> </param>
		/// <param name='cvIns'> </param>
		/// <param name='cvDel'> </param>
		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			throw new NotSupportedException("PropChanged use is not supported.");
		}

		/// <summary> Request removal from the list of objects to notify when properties change. </summary>
		/// <param name='nchng'> </param>
		public void RemoveNotification(IVwNotifyChange nchng)
		{
			m_uowService.RemoveNotification(nchng);
		}

		/// <summary>Gets the display index when given the real index for an object</summary>
		public int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			// Default implementation display index = real index
			return ihvo;
		}
		#endregion Notification methods

		#region Misc properties and methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We implement a limited version of this method for now...the only case that actually occurs.
		/// The idea is to move text from a specified source substring (specified by the first five arguments)
		/// to a specified destination (specified by the last four). The ws arguments should be zero for
		/// non-multilingual properties.
		/// Currently we only support moving between the contents of DIFFERENT paragraphs.
		/// </summary>
		/// <param name="hvoSource">The HVO of the source that contains the string to move</param>
		/// <param name="flidSrc">The field id of the source contents</param>
		/// <param name="wsSrc">The writing system of the source</param>
		/// <param name="ichMin">The beginning character offset</param>
		/// <param name="ichLim">The limit of the string to move</param>
		/// <param name="hvoDst">The HVO of the destination that contains the string to move</param>
		/// <param name="flidDst">The field id of the destination paragraph</param>
		/// <param name="wsDst">The writing system of the destination</param>
		/// <param name="ichDest">The character offset in the destination where the string will be moved</param>
		/// <param name="fDstIsNew">True if the destination paragraph is a brand new paragraph. In this
		/// case the segment translation for the partially moved segment doesn't need to be
		/// copied over to the destination paragraph because the source paragraph will be keeping
		/// its segment translation.</param>
		/// ------------------------------------------------------------------------------------
		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst,
			int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			if (wsSrc != 0 || wsDst != 0 || flidSrc != StTxtParaTags.kflidContents || flidDst != StTxtParaTags.kflidContents
				|| hvoSource == hvoDst)
			{
				throw new NotImplementedException("MoveString so far only handles moves from the contents of one StTxtPara to the contents of a different one.");
			}
			ICmObject srcObj = m_cmObjectRepository.GetObject(hvoSource);
			ICmObject dstObj = m_cmObjectRepository.GetObject(hvoDst);
			// Refactor JohnT: the balance of this method could well become a static method, somewhere like DomainServices.StringServices,
			// to move text between the contents of two StTxtParas. This would be good if we need it elsewhere.
			if (!(srcObj is StTxtPara))
				throw new ArgumentException("MoveString source object has the wrong type for the specified property", "hvoSource/flidSrc");
			if (!(dstObj is StTxtPara))
				throw new ArgumentException("MoveString destination object has the wrong type for the specified property", "hvoDest/flidDst");
			var source = srcObj as StTxtPara;
			var dest = dstObj as StTxtPara;
			StTxtPara.MoveContents(source, ichMin, ichLim, dest, ichDest, fDstIsNew);
		}

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object.
		///</summary>
		/// <returns>A ILgWritingSystemFactory</returns>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_wsf; }
			set { throw new NotSupportedException("'WritingSystemFactory' 'setter' not supported."); }
		}

		/// <summary>
		/// Fill in the given array with the encodings this database finds interesting, up
		/// to the given max, and return the number obtainedeg, vernacular plus analysis
		/// encodings. (Currently this is used by the Styles dialog to flesh out the fonts tab.)
		/// ENHANCE JohnT: Replace with a method or methods asking for specifc kinds of encodings?
		/// Return a list of the encodings that are of interest within the database.
		/// If cwsMax is zero, return the actual number (but no encodings).
		/// If there is not enough room, return E_INVALIDARG.
		///</summary>
		/// <param name='cwsMax'> </param>
		/// <param name='_ws'> </param>
		/// <returns></returns>
		public int get_WritingSystemsOfInterest(int cwsMax, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*int[]*/ _ws)
		{
			throw new NotSupportedException("'get_WritingSystemsOfInterest' not needed in new architecture.");
		}

		/// <summary>
		/// A method that indicates if the cache has changed since it was first loaded by means
		/// of Set methods. Basically what this means is that client code has called one
		/// of the property modification methods (eg. "Set" methods, NewObject, DeleteObject,
		/// MoveOwnSeq, or Replace methods).
		/// ENHANCE JohnT: It would be nice to have
		/// a way to retrieve information about what changed.
		///</summary>
		/// <returns></returns>
		public bool IsDirty()
		{
			throw new NotSupportedException("'IsDirty' not needed in new architecture.");
		}

		/// <summary> Clear the dirty flag (typically after saving). </summary>
		public void ClearDirty()
		{
			throw new NotSupportedException("'ClearDirty' not supported.");
		}

		/// <summary>
		/// Get the meta data cache, if any. Type IUnknown is used to avoid circularity
		/// between FieldWorks components in type definitions.
		/// (Arguably these functions would make more sense in IVwCachDa. But they are
		/// very parallel to the writing system factory methods, which are well established
		/// in this interface.)
		///</summary>
		/// <remarks>Setting is required by the interface, but this implementation does not allow that.</remarks>
		/// <returns>A IFwMetaDataCache </returns>
		public IFwMetaDataCache MetaDataCache
		{
			get { return m_mdc; }
			set { throw new NotSupportedException("Setting the MetaDataCache property is not allowed."); }
		}

		/// <summary>Member GetOutlineNumber</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='flid'>flid</param>
		/// <param name='fFinPer'>fFinPer</param>
		/// <returns>A System.String</returns>
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			throw new NotSupportedException("'GetOutlineNumber' not needed in the new architecture.");
		}

		/// <summary>
		/// Test whether the specified ID is in the range of dummy objects that have been
		/// allocated by this cache. Note that a true result does NOT guarantee that we have
		/// the necessary class information to create, say, an FDO object. You may want to
		/// also check IsValidObject (which in this case will be fast) if this returns true.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public bool get_IsDummyId(int hvo)
		{
			throw new NotSupportedException("'get_IsDummyId' not supported.");
		}

		#endregion Misc properties and methods

		#endregion ISilDataAccess implementation

		#region IStructuredTextDataAccess implementation

		/// <summary>
		/// Obtain the flid used for accessing the contents (a TsString) of a paragraph.
		/// </summary>
		int IStructuredTextDataAccess.ParaContentsFlid
		{
			get { return StTxtParaTags.kflidContents; }
		}

		/// <summary>
		/// Obtain the flid used for accessing the Properties (TsTextProps) of a paragraph.
		/// </summary>
		int IStructuredTextDataAccess.ParaPropertiesFlid
		{
			get { return StParaTags.kflidStyleRules; }
		}

		/// <summary>
		/// Obtain the flid used for accessing the paragraphs of a text.
		/// </summary>
		int IStructuredTextDataAccess.TextParagraphsFlid
		{
			get { return StTextTags.kflidParagraphs; }
		}

		#endregion
	}
}
