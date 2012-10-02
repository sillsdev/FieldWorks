// --------------------------------------------------------------------------------------------
// Copyright (C) 2008 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: SdaDecoratorBase (derived from SdaDecoratorBase.cs in the WW branch...should probably merge with that after re-architecture)
// --------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Implementation of the ISilDataAccess which passes most calls through to an other, usually the main, implementation
	/// of the same interface.
	/// Subclasses can override particular methods to change behavior.
	/// </summary>
	[ComVisible(true)]
	public abstract class SdaDecoratorBase : ISilDataAccess
	{
		private readonly ISilDataAccess m_baseSda;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="domainDataByFlid">The FDO DomainDataByFlid implementation,
		/// which is used to get the basic FDO data.</param>
		public SdaDecoratorBase(ISilDataAccess domainDataByFlid)
		{
			if (domainDataByFlid == null) throw new ArgumentNullException("domainDataByFlid");

			m_baseSda = domainDataByFlid;
		}

		/// <summary>
		/// Get the Sda that this one wraps.
		/// </summary>
		public ISilDataAccess BaseSda { get { return m_baseSda; } }

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
			return m_baseSda.get_ObjectProp(hvo, tag);
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
			m_baseSda.SetObjProp(hvo, tag, hvoObj);
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
			return m_baseSda.get_BooleanProp(hvo, tag);
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
			m_baseSda.SetBoolean(hvo, tag, n);
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
			return m_baseSda.get_GuidProp(hvo, tag);
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
			m_baseSda.SetGuid(hvo, tag, uid);
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
			return m_baseSda.get_ObjFromGuid(uid);
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
			return m_baseSda.get_IntProp(hvo, tag);
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
			m_baseSda.SetInt(hvo, tag, n);
		}

		#endregion Int methods

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
			return m_baseSda.get_UnicodeProp(hvo, tag);
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
			m_baseSda.set_UnicodeProp(obj, tag, bstr);
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
			m_baseSda.SetUnicode(hvo, tag, _rgch, cch);
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
			m_baseSda.UnicodePropRgch(obj, tag, _rgch, cchMax, out _cch);
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
			return m_baseSda.get_TimeProp(hvo, tag);
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
			m_baseSda.SetTime(hvo, tag, lln);
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
			return m_baseSda.get_Int64Prop(hvo, tag);
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
			m_baseSda.SetInt64(hvo, tag, lln);
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
			return m_baseSda.get_UnknownProp(hvo, tag);
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
			m_baseSda.SetUnknown(hvo, tag, _unk);
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
			m_baseSda.BinaryPropRgb(hvo, tag, _rgb, cbMax, out _cb);
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
			m_baseSda.SetBinary(hvo, tag, _rgb, cb);
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
			return m_baseSda.get_StringProp(hvo, tag);
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
			m_baseSda.SetString(hvo, tag, _tss);
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
			return m_baseSda.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary>
		/// Method used to get a whole MultiString.
		///</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			return m_baseSda.get_MultiStringProp(hvo, tag);
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
			m_baseSda.SetMultiStringAlt(hvo, tag, ws, _tss);
		}

		#endregion MultiString/MultiUnicode methods

		#region Vector methods

		/// <summary> Get the full contents of the specified sequence in one go.</summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <param name='chvoMax'> </param>
		/// <param name='_chvo'> </param>
		/// <param name='_rghvo'> </param>
		public virtual void VecProp(int hvo, int tag, int chvoMax, out int _chvo, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 2)] ArrayPtr/*long[]*/ _rghvo)
		{
			m_baseSda.VecProp(hvo, tag, chvoMax, out _chvo, _rghvo);
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
			return m_baseSda.get_VecItem(hvo, tag, index);
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual int get_VecSize(int hvo, int tag)
		{
			return m_baseSda.get_VecSize(hvo, tag);
		}

		/// <summary> Get the length of the specified sequence or collection property. </summary>
		/// <param name='hvo'> </param>
		/// <param name='tag'> </param>
		/// <returns></returns>
		public virtual int get_VecSizeAssumeCached(int hvo, int tag)
		{
			return m_baseSda.get_VecSizeAssumeCached(hvo, tag);
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
			return m_baseSda.GetObjIndex(hvoOwn, flid, hvo);
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
			m_baseSda.MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner, tagDst, ihvoDstStart);
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
			m_baseSda.Replace(hvoObj, tag, ihvoMin, ihvoLim, _rghvo, chvo);
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
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_baseSda.BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		/// End the current task sequence. If an outer sequence is in progress, that one will
		/// continue.
		///</summary>
		public void EndUndoTask()
		{
			m_baseSda.EndUndoTask();
		}

		/// <summary>
		/// Continue the previous sequence. This is intended to be called from a place like
		/// OnIdle that performs "cleanup" operations that are really part of the previous
		/// sequence.
		///</summary>
		public void ContinueUndoTask()
		{
			m_baseSda.ContinueUndoTask();
		}

		/// <summary>
		/// End the current sequence, and any outer ones that are in progress. This is intended
		/// to be used as a cleanup function to get everything back in sync.
		///</summary>
		public void EndOuterUndoTask()
		{
			m_baseSda.EndOuterUndoTask();
		}

		/// <summary>
		///
		/// </summary>
		public void Rollback()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Break the current undo task into two at the current point. Subsequent actions will
		/// be part of the new task which will be assigned the given labels.
		///</summary>
		/// <param name='bstrUndo'> </param>
		/// <param name='bstrRedo'> </param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			m_baseSda.BreakUndoTask(bstrUndo, bstrRedo);
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
			return m_baseSda.GetActionHandler();
		}

		/// <summary>Member SetActionHandler</summary>
		/// <param name='actionhandler'>action handler</param>
		public void SetActionHandler(IActionHandler actionhandler)
		{
			m_baseSda.SetActionHandler(actionhandler);
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
			return m_baseSda.get_Prop(hvo, tag);
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
			return m_baseSda.get_IsPropInCache(hvo, tag, cpt, ws);
		}

		#endregion Basic Prop methods

		#region Basic Object methods

		/// <summary>
		/// Delete the specified object.
		///</summary>
		/// <param name='hvoObj'> </param>
		public virtual void DeleteObj(int hvoObj)
		{
			m_baseSda.DeleteObj(hvoObj);
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
			m_baseSda.DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
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
			m_baseSda.InsertNew(hvoObj, tag, ihvo, chvo, _ss);
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
			return m_baseSda.MakeNewObject(clid, hvoOwner, tag, ord);
		}

		/// <summary>
		/// Test whether an HVO represents a valid object. For the DB-less cache,
		/// the object must be in the m_extantObjectsByHvo data member.
		///</summary>
		/// <param name='hvo'> </param>
		/// <returns></returns>
		public virtual bool get_IsValidObject(int hvo)
		{
			return m_baseSda.get_IsValidObject(hvo);
		}

		/// <summary>
		/// This processes all atomic and sequence owning and reference props in the cache
		/// and removes the given hvo from any property where it is found. PropChanged is
		/// called on each modified property to notify interested parties.
		///</summary>
		/// <param name='hvo'> </param>
		public virtual void RemoveObjRefs(int hvo)
		{
			m_baseSda.RemoveObjRefs(hvo);
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
			m_baseSda.InsertRelExtra(hvoSrc, tag, ihvo, hvoDst, bstrExtra);
		}

		/// <summary>Member UpdateRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <param name='bstrExtra'>bstrExtra</param>
		public virtual void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			m_baseSda.UpdateRelExtra(hvoSrc, tag, ihvo, bstrExtra);
		}

		/// <summary>Member GetRelExtra</summary>
		/// <param name='hvoSrc'>hvoSrc</param>
		/// <param name='tag'>tag</param>
		/// <param name='ihvo'>ihvo</param>
		/// <returns>A System.String</returns>
		public virtual string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			return m_baseSda.GetRelExtra(hvoSrc, tag, ihvo);
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
			m_baseSda.AddNotification(nchng);
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
		public virtual void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// TODO: Do not pass it on, as FDO handles notification on chages to any of its data properties.
			// Decorator
			m_baseSda.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
		}

		/// <summary> Request removal from the list of objects to notify when properties change. </summary>
		/// <param name='nchng'> </param>
		public void RemoveNotification(IVwNotifyChange nchng)
		{
			m_baseSda.RemoveNotification(nchng);
		}

		#endregion Notification methods

		#region Misc properties and methods

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object.
		///</summary>
		/// <returns>A ILgWritingSystemFactory</returns>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_baseSda.WritingSystemFactory; }
			set { m_baseSda.WritingSystemFactory = value; }
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
			return m_baseSda.get_WritingSystemsOfInterest(cwsMax, _ws);
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
			return m_baseSda.IsDirty();
		}

		/// <summary> Clear the dirty flag (typically after saving).</summary>
		public void ClearDirty()
		{
			m_baseSda.ClearDirty();
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
			get { return m_baseSda.MetaDataCache; }
			set { m_baseSda.MetaDataCache = value; } // This will throw: 'NotSupportedException'.
		}

		/// <summary>Member GetOutlineNumber</summary>
		/// <param name='hvo'>hvo</param>
		/// <param name='flid'>flid</param>
		/// <param name='fFinPer'>fFinPer</param>
		/// <returns>A System.String</returns>
		public virtual string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			return m_baseSda.GetOutlineNumber(hvo, flid, fFinPer);
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
			return m_baseSda.get_IsDummyId(hvo);
		}

		#endregion Misc properties and methods

		#endregion ISilDataAccess implementation
	}

}
