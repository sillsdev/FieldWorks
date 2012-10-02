/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwBaseVirtualHandler.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Provides a default implementation of IVwVirtualHandler to facilitate virtual properties in
	View data caches. Subclasses must at least implement Load().
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

VwBaseVirtualHandler::VwBaseVirtualHandler()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

VwBaseVirtualHandler::~VwBaseVirtualHandler()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwBaseVirtualHandler::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwVirtualHandler)
		*ppv = static_cast<IVwVirtualHandler *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwVirtualHandler);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

// Dummy factory for END_COM_METHOD macro.
static DummyFactory g_fact(_T("Sil.Views.VwBaseVh"));

//:>********************************************************************************************
//:>	IVwVirtualHandler methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
 Sets the name of the class that this is a virtual property of. Normally set by
 whatever creates the handler. The cache does not call this, so it may be
 left unimplemented if there is a more convenient way to initialize the
 property.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::put_ClassName(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstr)
	m_stuClass.Assign(bstr, BstrLen(bstr));
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Gets the name of the class that this is a virtual property of.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::get_ClassName(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	m_stuClass.GetBstr(pbstr);
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Sets the name of the field that this is a virtual property of.
 The cache does not call this, so it may be left unimplemented if there is a more
 convenient way to initialize the property.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::put_FieldName(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstr)
	m_stuField.Assign(bstr, BstrLen(bstr));
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Gets the name of the field that this is a virtual property of.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::get_FieldName(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	m_stuField.GetBstr(pbstr);
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Sets the identifier of the property (the value passed as tag to various methods
 of ISilDataAccess and IVwCacheDa). This is normally called by the cache when
 the handler is installed.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::put_Tag(PropTag tag)
{
	BEGIN_COM_METHOD;
	m_tag = tag;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Gets the identifier of the property (the value passed as tag to various methods
 of ISilDataAccess and IVwCacheDa).
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::get_Tag(PropTag * ptag)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptag);
	*ptag = m_tag;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Sets the type (from the CmTypes enumeration) of data stored in this virtual
 property. (NOT the type plus kcptVirtual.)
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::put_Type(int cpt)
{
	BEGIN_COM_METHOD;
	m_cpt = cpt;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Gets the type of the property.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::get_Type(int * pcpt)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcpt);
	*pcpt = m_cpt;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Sets the whether the handler can accept a request to write the property. Normally set by
 whatever creates the handler. The cache does not call this, so it may be left
 unimplemented if there is some other way to initialize it (for example, it may be
 inherent in a particular implementation{} the SQL reader implementation is never
 writeable.)
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::put_Writeable(ComBool f)
{
	BEGIN_COM_METHOD;
	m_fWriteable = f;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Gets the whether the property can accept a write request.

 Default impl says it cannot. If you override this you must also override Write().
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::get_Writeable(ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf); // Also sets to false.
	*pf = m_fWriteable;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Sets the whether the handler should be invoked every time the property value is
 wanted. This is useful for properties that are cheap to compute and change frequently.
 For properties that are expensive to compute and change less often, the plan is that
 the value returned will be cached, and the user will issue a refresh request when
 the UI appears to be out of date.
 The cache does not call this, so it may be left unimplemented if there is a more
 convenient way to initialize the property.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::put_ComputeEveryTime(ComBool f)
{
	BEGIN_COM_METHOD;
	m_fComputeEveryTime = f;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Gets the whether the property should be computed every time it is needed.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::get_ComputeEveryTime(ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf); // makes it false which is the default
	*pf = m_fComputeEveryTime;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Requests that the data for the property for a particular object should be loaded
 into the cache. The cache may be presumed to also implement ISilDataAccess{}
 if the property is known to be a database one, it may further be presumed to
 implement IVwOleDbDa.
 Note that, if ComputeEveryTime is true, the cache will remove the property value
 from the cache after reading it, to ensure that Load is called again next time.
---------------------------------------------------------------------------------------------*/
// Deliberately omitted; subclasses must implement
//STDMETHODIMP VwBaseVirtualHandler::Load(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda){}

/*----------------------------------------------------------------------------------------------
Requests that the given HVOs be written as the new value of the property.
This is used for both atomic and sequence properties; for atomic ones, chvo is
either zero or one.
If ComputeEveryTime is false, the cache will also write this value to itself,
AFTER calling this method. (Thus, to obtain the old value, you can read it from
the sda.)
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::Replace(HVO hvo, PropTag tag, int ihvoMin, int ihvoLim,
	HVO * prghvo, int chvo, ISilDataAccess * psda)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Requests that the given object be written as the new value of the property.
 This is currently used for string and MultiString properties (ws is zero for
 string ones), but may eventually be used for other types, so the argument
 type has been kept general.
 If ComputeEveryTime is false, the cache will also write this value to itself,
 AFTER calling this method. (Thus, to obtain the old value, you can read it from
 the sda.)
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::WriteObj(HVO hvo, PropTag tag, int ws, IUnknown * punk,
	ISilDataAccess * psda)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
Requests that the given Integer be written as the new value of the property.
This is used for all kinds of integer property, including regular ints and times.
If ComputeEveryTime is false, the cache will also write this value to itself,
AFTER calling this method. (Thus, to obtain the old value, you can read it from
the sda.)
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::WriteInt64(HVO hvo, PropTag tag, int64 val,
	ISilDataAccess * psda)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 Requests that the given bstr be written as the new value of the property.
 This is used for Unicode properties, and may eventually be used (with an
 exact 8-character value) for GUID ones, and possibly for binary ones also.
 If ComputeEveryTime is false, the cache will also write this value to itself,
 AFTER calling this method. (Thus, to obtain the old value, you can read it from
 the sda.)
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::WriteUnicode(HVO hvo, PropTag tag, BSTR bstr,
	 ISilDataAccess * psda)
 {
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}

/*----------------------------------------------------------------------------------------------
 This requests that data should be loaded into the cached so that this property may
 be efficiently computed for all the objects in the array.
 The main purpose of this is so that a single SQL query may be used to load the data
 for a longs sequence of objects, which can be much faster than issuing a separate
 query for each one.
 The method may actually load the cache with a value for property Tag for each object
 in prghvo. If this is done, Load() will never be called for any of these objects
 (possibly even if, pathologically in such a case, ComputeEveryTime is true).
 It may also do nothing, in which case, Load() will be called for every object when
 the value is (first) needed. This is fine if Load() works correctly and no
 performance problems result.
 It may also load other data (typically data on which the computation of property
 Tag is based) without actually setting a value for property Tag. In this case,
 Load will be called normally, but hopefully will work more efficiently because
 the other data is preloaded.

 This default does nothing; Load will work as if PreLoad had not been used.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::PreLoad(int chvo, HVO * prghvo, PropTag tag, int ws, IVwCacheDa * pcda)
{
	BEGIN_COM_METHOD;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
/*----------------------------------------------------------------------------------------------
 This method is not called by the cache and may be left unimplemented. Its use, if any,
 is up to a particular implementation. It is included in the interface so that standard
 implementations may be initialized without defining an additional interface.

 This default does nothing whatever with the information.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::Initialize(BSTR bstrData)
{
	BEGIN_COM_METHOD;
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}

/*----------------------------------------------------------------------------------------------
	This method may be implemented to inform callers that the results computed by
	a virtual handler are affected by a property change. (Many implementers do not
	implement this comprehensively and just return false.)
	This default implementation just answers false for everything.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::DoesResultDependOnProp(HVO hvoObj, HVO hvoChange,
	PropTag tag, int ws, ComBool * pfDepends)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfDepends);
	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}


/*----------------------------------------------------------------------------------------------
	This method may be implemented to load everything at once, even when ComputeEveryTime is
	true.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVirtualHandler::SetLoadForAllOfClass(ComBool fLoadAll)
{
	BEGIN_COM_METHOD;

	END_COM_METHOD(g_fact, IID_IVwVirtualHandler);
}
