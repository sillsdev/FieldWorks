/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2009 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ComSmartPtrImpl.h
Responsibility: Linux team
Last reviewed:

	Smart pointer class implementation details.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ComSmartPtrImpl_H
#define ComSmartPtrImpl_H 1

#ifndef ComSmartPtr_H
#error "Need to include ComSmartPtr.h first"
#endif

#if !WIN32
_COM_SMARTPTR_TYPEDEF(IErrorInfo, __uuidof(IErrorInfo));
#endif

#include "StackDumper.h"

// Loads an interface for the provided CLSID.
template<typename _Interface>
	void ComSmartPtr<_Interface>::CreateInstance(const CLSID & clsid, DWORD dwClsContext)
{
	if (m_pobj)
	{
		m_pobj->Release();
		m_pobj = NULL;
	}
	try
	{
		CheckHr(CoCreateInstance(clsid, NULL, dwClsContext, GetIID(), (void **)&m_pobj));
	}
	catch(Throwable& thr)
	{
		StrUni stu;
		stu.Format(L"Internal error: could not create object with CLSID %g (HRESULT %x). %s",
			&clsid, thr.Result(), thr.Message());
		ThrowInternalError(thr.Result(), stu.Chars());
	}
}

#endif // ComSmartPtrImpl_H
