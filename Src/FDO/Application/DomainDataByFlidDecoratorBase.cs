// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DomainDataByFlidDecoratorBase.cs
// Responsibility: Randy Regnier
// Last reviewed: never

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Application
{
	/// <summary>
	/// Implementation of the ISilDataAccess which works with a stateful FDO system for most data issues.
	/// For cases where there is a need to store 'fake' data (i.e., not properties of CmObjects),
	/// clients should subclass this class and override the relevant Get/Set methods
	/// and read/write that fake data in their own internal caches. They should pass the request through to
	/// the DomainDataByFlid (ISilDataAccess) for regular data access.
	/// </summary>
	[ComVisible(true)]
	public abstract class DomainDataByFlidDecoratorBase : SilDataAccessManagedBase, IVwNotifyChange, IRefreshable, ISuspendRefresh
	{
		private readonly ISilDataAccessManaged m_domainDataByFlid;

		private IFwMetaDataCacheManaged m_mdc;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="domainDataByFlid">The FDO DomainDataByFlid implementation,
		/// which is used to get the basic FDO data.</param>
		/// <remarks>
		/// The hvo values are true 'handles' in that they are valid for one session,
		/// but may not be the same integer for another session for the 'same' object.
		/// Therefore, one should not use them for multi-session identity.
		/// CmObject identity can only be guaranteed by using their Guids (or using '==' in code).
		/// </remarks>
		protected DomainDataByFlidDecoratorBase(ISilDataAccessManaged domainDataByFlid)
		{
			if (domainDataByFlid == null) throw new ArgumentNullException("domainDataByFlid");

			m_domainDataByFlid = domainDataByFlid;
		}

		/// <summary>
		/// Get the SDA which this one decorates.
		/// </summary>
		public ISilDataAccessManaged BaseSda
		{
			get { return m_domainDataByFlid; }
		}

		#region ISilDataAccess implementation

		#region Object Prop methods

		/// <summary>
		/// Obtain the value of an atomic reference property, including owner.
		///</summary>S
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public override int get_ObjectProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_ObjectProp(hvo, tag);
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
		public override void SetObjProp(int hvo, int tag, int hvoObj)
		{
			m_domainDataByFlid.SetObjProp(hvo, tag, hvoObj);
		}

		#endregion Object Prop methods

		#region Boolean methods

		/// <summary>
		/// Get the value of a boolean property.
		/// false if no value known for this property.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public override bool get_BooleanProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_BooleanProp(hvo, tag);
		}

		/// <summary>
		/// Change a boolean property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='n'> </param>
		public override void SetBoolean(int hvo, int tag, bool n)
		{
			m_domainDataByFlid.SetBoolean(hvo, tag, n);
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
		public override Guid get_GuidProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_GuidProp(hvo, tag);
		}

		/// <summary>
		/// Change a GUID property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='uid'> </param>
		public override void SetGuid(int hvo, int tag, Guid uid)
		{
			m_domainDataByFlid.SetGuid(hvo, tag, uid);
		}

		/// <summary>
		/// Get the object that has the given guid.
		/// S_FALSE if no value is known for this property. Hvo will be 0.
		/// Some implementations may return S_OK and set <i>pHvo</i> to zero.
		///</summary>
		/// <param name='uid'> </param>
		/// <returns></returns>
		public override int get_ObjFromGuid(Guid uid)
		{
			return m_domainDataByFlid.get_ObjFromGuid(uid);
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
		public override int get_IntProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_IntProp(hvo, tag);
		}

		/// <summary>
		/// Change an integer property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='n'> </param>
		public override void SetInt(int hvo, int tag, int n)
		{
			m_domainDataByFlid.SetInt(hvo, tag, n);
		}

		#endregion Int methods

		#region GenDate methods

		/// <summary>
		/// Get the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <returns>The generic date.</returns>
		public override GenDate get_GenDateProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_GenDateProp(hvo, tag);
		}

		/// <summary>
		/// Set the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="genDate">The generic date.</param>
		public override void SetGenDate(int hvo, int tag, GenDate genDate)
		{
			m_domainDataByFlid.SetGenDate(hvo, tag, genDate);
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
		public override string get_UnicodeProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_UnicodeProp(hvo, tag);
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
		public override void set_UnicodeProp(int obj, int tag, string bstr)
		{
			m_domainDataByFlid.set_UnicodeProp(obj, tag, bstr);
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
		public override void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			m_domainDataByFlid.SetUnicode(hvo, tag, _rgch, cch);
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
		public override void UnicodePropRgch(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*OLECHAR[]*/ _rgch, int cchMax, out int _cch)
		{
			m_domainDataByFlid.UnicodePropRgch(obj, tag, _rgch, cchMax, out _cch);
		}

		#endregion Unicode methods

		#region Time methods

		/// <summary> Read a time property.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns>Actually an SilTime.
		/// 0 if property not found.</returns>
		public override long get_TimeProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_TimeProp(hvo, tag);
		}

		/// <summary>
		/// Change a time property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='lln'> </param>
		public override void SetTime(int hvo, int tag, long lln)
		{
			m_domainDataByFlid.SetTime(hvo, tag, lln);
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
		public override long get_Int64Prop(int hvo, int tag)
		{
			return m_domainDataByFlid.get_Int64Prop(hvo, tag);
		}

		/// <summary>
		/// Change a long integer property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='lln'> </param>
		public override void SetInt64(int hvo, int tag, long lln)
		{
			m_domainDataByFlid.SetInt64(hvo, tag, lln);
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
		public override object get_UnknownProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_UnknownProp(hvo, tag);
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
		public override void SetUnknown(int hvo, int tag, object _unk)
		{
			m_domainDataByFlid.SetUnknown(hvo, tag, _unk);
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
		public override void BinaryPropRgb(int hvo, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*byte[]*/ _rgb, int cbMax, out int _cb)
		{
			m_domainDataByFlid.BinaryPropRgb(hvo, tag, _rgb, cbMax, out _cb);
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
		public override void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
		{
			m_domainDataByFlid.SetBinary(hvo, tag, _rgb, cb);
		}

		#endregion Binary methods

		#region String methods

		/// <summary>
		/// Read a (nonmultilingual) string property.
		/// an empty string, writing system 0, if property not found.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public override ITsString get_StringProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_StringProp(hvo, tag);
		}

		/// <summary>
		/// Change a stringvalued property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_tss'> </param>
		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			m_domainDataByFlid.SetString(hvo, tag, _tss);
		}

		/// <summary>
		/// Move a substring from one place to another.
		/// </summary>
		public override void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim,
			int hvoDst, int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			m_domainDataByFlid.MoveString(hvoSource, flidSrc, wsSrc, ichMin, ichLim, hvoDst,
				flidDst, wsDst, ichDest, fDstIsNew);
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
		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			return m_domainDataByFlid.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary>
		/// Method used to get a whole MultiString.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public override ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			return m_domainDataByFlid.get_MultiStringProp(hvo, tag);
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
		public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			m_domainDataByFlid.SetMultiStringAlt(hvo, tag, ws, _tss);
		}

		#endregion MultiString/MultiUnicode methods

		#region Vector methods

		/// <summary> Get the full contents of the specified sequence in one go. This version is deliberately NOT
		/// override, nor does it delegate directly to the wrapped class; rather, it uses a static method to share
		/// the conversion from the managed VecProp that is used by DomainDataByFlid. This means one less method
		/// that must be overidden for a vector property.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='chvoMax'> </param>
		/// <param name='_chvo'> </param>
		/// <param name='_rghvo'> </param>
		public override void VecProp(int hvo, int tag, int chvoMax, out int _chvo, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 2)] ArrayPtr/*long[]*/ _rghvo)
		{
			_chvo = DomainDataByFlidServices.ComVecPropFromManagedVecProp(VecProp(hvo, tag), hvo, tag, _rghvo, chvoMax);
		}

		/// <summary>
		/// Get the Ids of the entire vector property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		[ComVisible(false)]
		public override int[] VecProp(int hvo, int tag)
		{
			return m_domainDataByFlid.VecProp(hvo, tag);
		}

		/// <summary>
		/// Obtain one item from an object sequence or collection property.
		/// @error E_INVALIDARG if index is out of range.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='index'>Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;. </param>
		/// <returns></returns>
		public override int get_VecItem(int hvo, int tag, int index)
		{
			return m_domainDataByFlid.get_VecItem(hvo, tag, index);
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public override int get_VecSize(int hvo, int tag)
		{
			return m_domainDataByFlid.get_VecSize(hvo, tag);
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public override int get_VecSizeAssumeCached(int hvo, int tag)
		{
			return m_domainDataByFlid.get_VecSizeAssumeCached(hvo, tag);
		}

		/// <summary> Return the index of hvo in the flid vector of hvoOwn.</summary>
		/// <param name='hvoOwn'>The object ID of the owner.</param>
		/// <param name='flid'>The parameter on hvoOwn that owns hvo.</param>
		/// <param name='hvo'>The target object ID we are looking for.</param>
		/// <returns>
		/// The index, or -1 if not found.
		/// </returns>
		public override int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			return m_domainDataByFlid.GetObjIndex(hvoOwn, flid, hvo);
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
		public override void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			m_domainDataByFlid.MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner, tagDst, ihvoDstStart);
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
		public override void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			m_domainDataByFlid.MoveOwn(hvoSrcOwner, tagSrc, hvo, hvoDstOwner, tagDst, ihvoDstStart);
		}

		/// <summary>
		/// Replace the range of objects [ihvoMin, ihvoLim) in property tag of object hvoObj
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
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvoObj'> </param>
		/// <param name='tag'> </param>
		/// <param name='ihvoMin'> </param>
		/// <param name='ihvoLim'> </param>
		/// <param name='_rghvo'> </param>
		/// <param name='chvo'> </param>
		public override void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			m_domainDataByFlid.Replace(hvoObj, tag, ihvoMin, ihvoLim, _rghvo, chvo);
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
		public override void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_domainDataByFlid.BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		/// Begin a sequence of actions that will be treated as one task for the purposes
		/// of undo and redo. If there is already such a task in process, this sequence will be
		/// included (nested) in that one, and the descriptive strings will be ignored.
		///</summary>
		public override void BeginNonUndoableTask()
		{
			m_domainDataByFlid.BeginNonUndoableTask();
		}

		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		///</summary>
		public override void EndUndoTask()
		{
			m_domainDataByFlid.EndUndoTask();
		}

		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		///</summary>
		public override void EndNonUndoableTask()
		{
			m_domainDataByFlid.EndNonUndoableTask();
		}

		/// <summary>
		/// Continue the previous sequence. This is intended to be called from a place like
		/// OnIdle that performs "cleanup" operations that are really part of the previous
		/// sequence.
		///</summary>
		public override void ContinueUndoTask()
		{
			m_domainDataByFlid.ContinueUndoTask();
		}

		/// <summary>
		/// End the current sequence, and any outer ones that are in progress. This is intended
		/// to be used as a cleanup function to get everything back in sync.
		///</summary>
		public override void EndOuterUndoTask()
		{
			m_domainDataByFlid.EndOuterUndoTask();
		}

		/// <summary>
		///
		/// </summary>
		public override void Rollback()
		{
			m_domainDataByFlid.Rollback();
		}

		/// <summary>
		/// Break the current undo task into two at the current point. Subsequent actions will
		/// be part of the new task which will be assigned the given labels.
		///</summary>
		/// <param name='bstrUndo'> </param>
		/// <param name='bstrRedo'> </param>
		public override void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			m_domainDataByFlid.BreakUndoTask(bstrUndo, bstrRedo);
		}

		#endregion Undo/Redo methods

		#region Action Handling

		/// <summary>
		/// Return the IActionHandler that is being used to record undo information.
		/// May be NULL
		///</summary>
		/// <returns></returns>
		public override IActionHandler GetActionHandler()
		{
			return m_domainDataByFlid.GetActionHandler();
		}

		/// <summary>Member SetActionHandler</summary>
		/// <param name='actionhandler'>action handler</param>
		public override void SetActionHandler(IActionHandler actionhandler)
		{
			m_domainDataByFlid.SetActionHandler(actionhandler);
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
		public override object get_Prop(int hvo, int tag)
		{
			return m_domainDataByFlid.get_Prop(hvo, tag);
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
		public override bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			return m_domainDataByFlid.get_IsPropInCache(hvo, tag, cpt, ws);
		}

		#endregion Basic Prop methods

		#region Basic Object methods

		/// <summary>
		/// Delete the specified object.
		///</summary>
		/// <param name='hvoObj'> </param>
		public override void DeleteObj(int hvoObj)
		{
			m_domainDataByFlid.DeleteObj(hvoObj);
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
		public override void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			m_domainDataByFlid.DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
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
		/// <param name='_ss'> </param>
		public override void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			m_domainDataByFlid.InsertNew(hvoObj, tag, ihvo, chvo, _ss);
		}

		/// <summary>
		/// Make a new object owned in a particular position. The object is created immediately.
		/// (Actually in the database, in database implementations; this will roll back if
		/// the transaction is not committed.)
		/// If ord is &gt;= 0, the object is inserted in the appropriate place in the (presumed
		/// sequence) property, both in the database itself and in the data access object's
		/// internal cache, if that property is cached.
		/// If ord is &lt; 0, it is entered as a null into the database, which is appropriate for
		/// collection and atomic properties.
		/// Specifically, use 2 for an atomic property, and 1 for a collection; this will
		/// ensure that the cache is updated. You may use 3 if you know the property is not
		/// currently cached.
		/// The caller should also call PropChanged to notify interested parties.
		///</summary>
		/// <param name='clid'> </param>
		/// <param name='hvoOwner'> </param>
		/// <param name='tag'> </param>
		/// <param name='ord'> </param>
		/// <returns></returns>
		public override int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			return m_domainDataByFlid.MakeNewObject(clid, hvoOwner, tag, ord);
		}

		/// <summary>
		/// Test whether an HVO represents a valid object. For the DB-less cache,
		/// the object must be in the m_extantObjectsByHvo data member.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public override bool get_IsValidObject(int hvo)
		{
			return m_domainDataByFlid.get_IsValidObject(hvo);
		}

		/// <summary>
		/// This processes all atomic and sequence owning and reference props in the cache
		/// and removes the given hvo from any property where it is found. PropChanged is
		/// called on each modified property to notify interested parties.
		///</summary>
		/// <param name='hvo'> </param>
		public override void RemoveObjRefs(int hvo)
		{
			m_domainDataByFlid.RemoveObjRefs(hvo);
		}

		#endregion Basic Object methods

		#region Custom Field (Extra) methods

		/// <summary>Member InsertRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='hvoDst'>hvoDst</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public override void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			m_domainDataByFlid.InsertRelExtra(hvoSrc, tag, ihvo, hvoDst, bstrExtra);
		}

		/// <summary>Member UpdateRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public override void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			m_domainDataByFlid.UpdateRelExtra(hvoSrc, tag, ihvo, bstrExtra);
		}

		/// <summary>Member GetRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <returns>A System.String</returns>
		public override string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			return m_domainDataByFlid.GetRelExtra(hvoSrc, tag, ihvo);
		}
		#endregion Custom Field (Extra) methods

		#region Notification methods

		/// <summary>
		/// Request notification when properties change. The ${IVwNotifyChange#PropChanged}
		/// method will be called when the property changes (provided the client making the
		/// change properly calls ${#PropChanged}. Also, adds target to the list to be
		/// notified by SendPropChanged().
		///</summary>
		public override void AddNotification(IVwNotifyChange nchng)
		{
			m_domainDataByFlid.AddNotification(nchng);
			base.AddNotification(nchng);
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
		///
		/// FDO handles notification of all data changes, so this method does not pass the call on
		/// to the FDO SDA implementation.
		///
		/// Decorator sublasses should intercept the 'setter' calls and notify
		/// their views of the change.
		///</summary>
		/// <param name='_nchng'> </param>
		/// <param name='_ct'> </param>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='ivMin'> </param>
		/// <param name='cvIns'> </param>
		/// <param name='cvDel'> </param>
		public override void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			m_domainDataByFlid.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
		}

		/// <summary> Request removal from the lists of objects to notify when properties change. </summary>
		/// <param name='nchng'> </param>
		public override void RemoveNotification(IVwNotifyChange nchng)
		{
			m_domainDataByFlid.RemoveNotification(nchng);
			base.RemoveNotification(nchng);
		}

		/// <summary>Get display index for a given real index.</summary>
		public override int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			return m_domainDataByFlid.GetDisplayIndex(hvoOwn, flid, ihvo);
		}

		#endregion Notification methods

		#region Misc properties and methods

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object.
		///</summary>
		/// <returns>A ILgWritingSystemFactory</returns>
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_domainDataByFlid.WritingSystemFactory; }
			set { m_domainDataByFlid.WritingSystemFactory = value; }
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
		public override int get_WritingSystemsOfInterest(int cwsMax, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*int[]*/ _ws)
		{
			return m_domainDataByFlid.get_WritingSystemsOfInterest(cwsMax, _ws);
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
		public override bool IsDirty()
		{
			return m_domainDataByFlid.IsDirty();
		}

		/// <summary> Clear the dirty flag (typically after saving).</summary>
		public override void ClearDirty()
		{
			m_domainDataByFlid.ClearDirty();
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
		public override IFwMetaDataCache MetaDataCache
		{
			get { return m_mdc ?? m_domainDataByFlid.MetaDataCache; }
			set { m_domainDataByFlid.MetaDataCache = value; } // This will throw: 'NotSupportedException'.
		}

		/// <summary>
		/// Setup a (typically decorator) override MDC.
		/// This will affect additional layers of decorator, but not the main SDA.
		/// </summary>
		/// <param name="mdc"></param>
		public void SetOverrideMdc(IFwMetaDataCacheManaged mdc)
		{
			m_mdc = mdc;
			if (m_domainDataByFlid is DomainDataByFlidDecoratorBase)
				(m_domainDataByFlid as DomainDataByFlidDecoratorBase).SetOverrideMdc(mdc);
		}

		/// <summary>Member GetOutlineNumber</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='flid'>flid</param>
		/// <param name='fFinPer'>fFinPer</param>
		/// <returns>A System.String</returns>
		public override string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			return m_domainDataByFlid.GetOutlineNumber(hvo, flid, fFinPer);
		}

		/// <summary>
		/// Test whether the specified ID is in the range of dummy objects that have been
		/// allocated by this cache. Note that a true result does NOT guarantee that we have
		/// the necessary class information to create, say, an FDO object. You may want to
		/// also check IsValidObject (which in this case will be fast) if this returns true.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public override bool get_IsDummyId(int hvo)
		{
			return m_domainDataByFlid.get_IsDummyId(hvo);
		}

		#endregion Misc properties and methods

		#endregion ISilDataAccess implementation

		#region IStructuredTextDataAccess implementation

		/// <summary>
		/// Obtain the flid used for accessing the contents (a TsString) of a paragraph.
		/// </summary>
		int ParaContentsFlid
		{
			get { return StTxtParaTags.kflidContents; }
		}

		/// <summary>
		/// Obtain the flid used for accessing the Properties (TsTextProps) of a paragraph.
		/// </summary>
		int ParaPropertiesFlid
		{
			get { return StParaTags.kflidStyleRules; }
		}

		/// <summary>
		/// Obtain the flid used for accessing the paragraphs of a text.
		/// </summary>
		int TextParagraphsFlid
		{
			get { return StTextTags.kflidParagraphs; }
		}

		#endregion

		/// <summary>
		/// Some decorators may want to receive propchanged messages. In case our base does, pass it on.
		/// </summary>
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			var noteChange = m_domainDataByFlid as IVwNotifyChange;
			if (noteChange != null)
				noteChange.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		/// <summary>
		/// Update whatever needs it. In this case pass it on to any wrapped SDA which may need it.
		/// </summary>
		public virtual void Refresh()
		{
			if (BaseSda is IRefreshable)
				((IRefreshable)BaseSda).Refresh();
		}

		/// <summary>
		/// Pass it on to wrapped SDAs if they care.
		/// </summary>
		public virtual void SuspendRefresh()
		{
			if (BaseSda is ISuspendRefresh)
				((ISuspendRefresh)BaseSda).SuspendRefresh();
		}

		/// <summary>
		/// Pass it on to wrapped SDAs if they care.
		/// </summary>
		public virtual void ResumeRefresh()
		{
			if (BaseSda is ISuspendRefresh)
				((ISuspendRefresh)BaseSda).ResumeRefresh();
		}
	}

	/// <summary>
	/// This implementation of the ISilDataAccess interface is provided as a convenience for those who
	/// need to create custom data access classes. Most methods are implemented but throw a NotImplementedException
	/// when called. This allows subclasses to only implement the methods that they need.
	/// </summary>
	[ComVisible(true)]
	public abstract class SilDataAccessManagedBase : ISilDataAccessManaged
	{
		// Keep an independent record of things to notify of changes, since subclass methods may call
		// SendPropChanged() to notify these notifiees (only) of changes to fake properties.
		// These are only notifiees added by calling the decorator's NotifyChanges, not any added independently
		// to the class wrapped.
		private readonly HashSet<IVwNotifyChange> m_notifiees = new HashSet<IVwNotifyChange>(); // Things to notify of changes.

		#region ISilDataAccess implementation

		#region Object Prop methods

		/// <summary>
		/// Obtain the value of an atomic reference property, including owner.
		///</summary>S
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual int get_ObjectProp(int hvo, int tag)
		{
			throw new NotImplementedException();
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
		public virtual void SetObjProp(int hvo, int tag, int hvoObj)
		{
			throw new NotImplementedException();
		}

		#endregion Object Prop methods

		#region Boolean methods

		/// <summary>
		/// Get the value of a boolean property.
		/// false if no value known for this property.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual bool get_BooleanProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change a boolean property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='n'> </param>
		public virtual void SetBoolean(int hvo, int tag, bool n)
		{
			throw new NotImplementedException();
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
		public virtual Guid get_GuidProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change a GUID property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='uid'> </param>
		public virtual void SetGuid(int hvo, int tag, Guid uid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the object that has the given guid.
		/// S_FALSE if no value is known for this property. Hvo will be 0.
		/// Some implementations may return S_OK and set <i>pHvo</i> to zero.
		///</summary>
		/// <param name='uid'> </param>
		/// <returns></returns>
		public virtual int get_ObjFromGuid(Guid uid)
		{
			throw new NotImplementedException();
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
		public virtual int get_IntProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change an integer property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='n'> </param>
		public virtual void SetInt(int hvo, int tag, int n)
		{
			throw new NotImplementedException();
		}

		#endregion Int methods

		#region GenDate methods

		/// <summary>
		/// Get the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <returns>The generic date.</returns>
		public virtual GenDate get_GenDateProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Set the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="genDate">The generic date.</param>
		public virtual void SetGenDate(int hvo, int tag, GenDate genDate)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region DateTime methods
		/// <summary>
		/// Get the time property as a DateTime value.
		/// </summary>
		public DateTime get_DateTime(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change a time property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		public void SetDateTime(int hvo, int tag, DateTime dt)
		{
			throw new NotImplementedException();
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
		public virtual string get_UnicodeProp(int hvo, int tag)
		{
			throw new NotImplementedException();
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
		public virtual void set_UnicodeProp(int obj, int tag, string bstr)
		{
			throw new NotImplementedException();
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
		public virtual void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			throw new NotImplementedException();
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
		public virtual void UnicodePropRgch(int obj, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*OLECHAR[]*/ _rgch, int cchMax, out int _cch)
		{
			throw new NotImplementedException();
		}

		#endregion Unicode methods

		#region Time methods

		/// <summary> Read a time property.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns>Actually an SilTime.
		/// 0 if property not found.</returns>
		public virtual long get_TimeProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change a time property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='lln'> </param>
		public virtual void SetTime(int hvo, int tag, long lln)
		{
			throw new NotImplementedException();
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
		public virtual long get_Int64Prop(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change a long integer property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='lln'> </param>
		public virtual void SetInt64(int hvo, int tag, long lln)
		{
			throw new NotImplementedException();
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
		public virtual object get_UnknownProp(int hvo, int tag)
		{
			throw new NotImplementedException();
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
		public virtual void SetUnknown(int hvo, int tag, object _unk)
		{
			throw new NotImplementedException();
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
		public virtual void BinaryPropRgb(int hvo, int tag, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*byte[]*/ _rgb, int cbMax, out int _cb)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the binary data property of an object.
		///</summary>
		/// <param name='hvo'></param>
		/// <param name='tag'></param>
		/// <param name='rgb'>Contains the binary data</param>
		/// <returns>byte count in binary data property</returns>
		public int get_Binary(int hvo, int tag, out byte[] rgb)
		{
			throw new NotImplementedException();
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
		public virtual void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
		{
			throw new NotImplementedException();
		}

		#endregion Binary methods

		#region String methods

		/// <summary>
		/// Read a (nonmultilingual) string property.
		/// an empty string, writing system 0, if property not found.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual ITsString get_StringProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Change a stringvalued property of an object.
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='_tss'> </param>
		public virtual void SetString(int hvo, int tag, ITsString _tss)
		{
			throw new NotImplementedException();
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
		public virtual ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Method used to get a whole MultiString.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			throw new NotImplementedException();
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
		public virtual void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			throw new NotImplementedException();
		}

		#endregion MultiString/MultiUnicode methods

		#region Vector methods

		/// <summary> Get the full contents of the specified sequence in one go. This version is deliberately NOT
		/// virtual, nor does it delegate directly to the wrapped class; rather, it uses a static method to share
		/// the conversion from the managed VecProp that is used by DomainDataByFlid. This means one less method
		/// that must be overidden for a vector property.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='chvoMax'> </param>
		/// <param name='_chvo'> </param>
		/// <param name='_rghvo'> </param>
		public virtual void VecProp(int hvo, int tag, int chvoMax, out int _chvo, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 2)] ArrayPtr/*long[]*/ _rghvo)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the Ids of the entire vector property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		[ComVisible(false)]
		public virtual int[] VecProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Obtain one item from an object sequence or collection property.
		/// @error E_INVALIDARG if index is out of range.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='index'>Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;. </param>
		/// <returns></returns>
		public virtual int get_VecItem(int hvo, int tag, int index)
		{
			throw new NotImplementedException();
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual int get_VecSize(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual int get_VecSizeAssumeCached(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary> Return the index of hvo in the flid vector of hvoOwn.</summary>
		/// <param name='hvoOwn'>The object ID of the owner.</param>
		/// <param name='flid'>The parameter on hvoOwn that owns hvo.</param>
		/// <param name='hvo'>The target object ID we are looking for.</param>
		/// <returns>
		/// The index, or -1 if not found.
		/// </returns>
		public virtual int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			throw new NotImplementedException();
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
		public virtual void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			throw new NotImplementedException();
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
		public virtual void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Replace the range of objects [ihvoMin, ihvoLim) in property tag of object hvoObj
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
		/// The caller should also call PropChanged to notify interested parties,
		/// except where the change is being made to a newly created object.
		///</summary>
		/// <param name='hvoObj'> </param>
		/// <param name='tag'> </param>
		/// <param name='ihvoMin'> </param>
		/// <param name='ihvoLim'> </param>
		/// <param name='_rghvo'> </param>
		/// <param name='chvo'> </param>
		public virtual void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			throw new NotImplementedException();
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
		public virtual void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Begin a sequence of actions that will be treated as one task for the purposes
		/// of undo and redo. If there is already such a task in process, this sequence will be
		/// included (nested) in that one, and the descriptive strings will be ignored.
		///</summary>
		public virtual void BeginNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		///</summary>
		public virtual void EndUndoTask()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		///</summary>
		public virtual void EndNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Continue the previous sequence. This is intended to be called from a place like
		/// OnIdle that performs "cleanup" operations that are really part of the previous
		/// sequence.
		///</summary>
		public virtual void ContinueUndoTask()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End the current sequence, and any outer ones that are in progress. This is intended
		/// to be used as a cleanup function to get everything back in sync.
		///</summary>
		public virtual void EndOuterUndoTask()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public virtual void Rollback()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Break the current undo task into two at the current point. Subsequent actions will
		/// be part of the new task which will be assigned the given labels.
		///</summary>
		/// <param name='bstrUndo'> </param>
		/// <param name='bstrRedo'> </param>
		public virtual void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new NotImplementedException();
		}

		#endregion Undo/Redo methods

		#region Action Handling

		/// <summary>
		/// Return the IActionHandler that is being used to record undo information.
		/// May be NULL
		///</summary>
		/// <returns></returns>
		public virtual IActionHandler GetActionHandler()
		{
			return null;
		}

		/// <summary>Member SetActionHandler</summary>
		/// <param name='actionhandler'>action handler</param>
		public virtual void SetActionHandler(IActionHandler actionhandler)
		{
			throw new NotImplementedException();
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
		public virtual object get_Prop(int hvo, int tag)
		{
			throw new NotImplementedException();
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
		public virtual bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			throw new NotImplementedException();
		}

		#endregion Basic Prop methods

		#region Basic Object methods

		/// <summary>
		/// Delete the specified object.
		///</summary>
		/// <param name='hvoObj'> </param>
		public virtual void DeleteObj(int hvoObj)
		{
			throw new NotImplementedException();
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
		public virtual void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			throw new NotImplementedException();
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
		/// <param name='_ss'> </param>
		public virtual void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Make a new object owned in a particular position. The object is created immediately.
		/// (Actually in the database, in database implementations; this will roll back if
		/// the transaction is not committed.)
		/// If ord is &gt;= 0, the object is inserted in the appropriate place in the (presumed
		/// sequence) property, both in the database itself and in the data access object's
		/// internal cache, if that property is cached.
		/// If ord is &lt; 0, it is entered as a null into the database, which is appropriate for
		/// collection and atomic properties.
		/// Specifically, use 2 for an atomic property, and 1 for a collection; this will
		/// ensure that the cache is updated. You may use 3 if you know the property is not
		/// currently cached.
		/// The caller should also call PropChanged to notify interested parties.
		///</summary>
		/// <param name='clid'> </param>
		/// <param name='hvoOwner'> </param>
		/// <param name='tag'> </param>
		/// <param name='ord'> </param>
		/// <returns></returns>
		public virtual int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Test whether an HVO represents a valid object. For the DB-less cache,
		/// the object must be in the m_extantObjectsByHvo data member.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public virtual bool get_IsValidObject(int hvo)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This processes all atomic and sequence owning and reference props in the cache
		/// and removes the given hvo from any property where it is found. PropChanged is
		/// called on each modified property to notify interested parties.
		///</summary>
		/// <param name='hvo'> </param>
		public virtual void RemoveObjRefs(int hvo)
		{
			throw new NotImplementedException();
		}

		#endregion Basic Object methods

		#region Custom Field (Extra) methods

		/// <summary>Member InsertRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='hvoDst'>hvoDst</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public virtual void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			throw new NotImplementedException();
		}

		/// <summary>Member UpdateRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public virtual void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			throw new NotImplementedException();
		}

		/// <summary>Member GetRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <returns>A System.String</returns>
		public virtual string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			throw new NotImplementedException();
		}
		#endregion Custom Field (Extra) methods

		#region Notification methods

		/// <summary>
		/// Request notification when properties change. The ${IVwNotifyChange#PropChanged}
		/// method will be called when the property changes (provided the client making the
		/// change properly calls ${#PropChanged}. Also, adds target to the list to be
		/// notified by SendPropChanged().
		///</summary>
		public virtual void AddNotification(IVwNotifyChange nchng)
		{
			m_notifiees.Add(nchng);
		}

		/// <summary>
		/// This can be called by subclasses which want to send PropChanged to their private Notifiees.
		/// </summary>
		protected void SendPropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			foreach (IVwNotifyChange target in m_notifiees)
				target.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
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
		public virtual void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			throw new NotImplementedException();
		}

		/// <summary> Request removal from the lists of objects to notify when properties change. </summary>
		/// <param name='nchng'> </param>
		public virtual void RemoveNotification(IVwNotifyChange nchng)
		{
			m_notifiees.Remove(nchng);
		}

		/// <summary>
		/// Provide display index for object at the given real index.
		/// </summary>
		public virtual int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			// default implemenation: display index = real index
			return ihvo;
		}

		#endregion Notification methods

		#region Misc properties and methods

		/// <summary>
		/// Move a substring from one place to another.
		/// </summary>
		public virtual void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst,
			int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object.
		///</summary>
		/// <returns>A ILgWritingSystemFactory</returns>
		public virtual ILgWritingSystemFactory WritingSystemFactory
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
		public virtual int get_WritingSystemsOfInterest(int cwsMax, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*int[]*/ _ws)
		{
			throw new NotImplementedException();
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
		public virtual bool IsDirty()
		{
			throw new NotImplementedException();
		}

		/// <summary> Clear the dirty flag (typically after saving).</summary>
		public virtual void ClearDirty()
		{
			throw new NotImplementedException();
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
		public virtual IFwMetaDataCache MetaDataCache
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

		/// <summary>Member GetOutlineNumber</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='flid'>flid</param>
		/// <param name='fFinPer'>fFinPer</param>
		/// <returns>A System.String</returns>
		public virtual string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Test whether the specified ID is in the range of dummy objects that have been
		/// allocated by this cache. Note that a true result does NOT guarantee that we have
		/// the necessary class information to create, say, an FDO object. You may want to
		/// also check IsValidObject (which in this case will be fast) if this returns true.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public virtual bool get_IsDummyId(int hvo)
		{
			throw new NotImplementedException();
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

	/// <summary>
	/// A struct that can be used as the key in a Dictionary.
	/// This could be used for the ley in a subclass of FdoSilDataAccessDecoratorBase.
	/// </summary>
	[DebuggerDisplay("Hvo={m_hvo},Flid={m_flid}")]
	public struct HvoFlidKey
	{
		private readonly int m_hvo;
		private readonly int m_flid;

		/// <summary>
		///
		/// </summary>
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		///
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		public HvoFlidKey(int hvo, int flid)
		{
			m_hvo = hvo;
			m_flid = flid;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is HvoFlidKey))
				return false;

			var hfk = (HvoFlidKey)obj;
			return (hfk.m_hvo == m_hvo)
				   && (hfk.m_flid == m_flid);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return (m_hvo ^ m_flid);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("{0}^{1}", m_hvo, m_flid);
		}
	}

	/// <summary>
	///
	/// </summary>
	[DebuggerDisplay("Hvo={m_hvo},Flid={m_flid},WS={m_ws}")]
	public struct HvoFlidWSKey
	{
		private readonly int m_hvo;
		private readonly int m_flid;
		private readonly int m_ws;

		/// <summary>
		///
		/// </summary>
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		///
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		///
		/// </summary>
		public int Ws
		{
			get { return m_ws; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		public HvoFlidWSKey(int hvo, int flid, int ws)
		{
			m_hvo = hvo;
			m_flid = flid;
			m_ws = ws;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is HvoFlidWSKey))
				return false;

			var key = (HvoFlidWSKey)obj;
			return (key.m_hvo == m_hvo)
				   && (key.m_flid == m_flid)
				   && (key.m_ws == m_ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return (m_hvo ^ m_flid ^ m_ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("{0}^{1}^{2}", m_hvo, m_flid, m_ws);
		}
	}
}
