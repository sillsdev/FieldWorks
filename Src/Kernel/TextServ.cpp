/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TextServ.cpp
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Implementation of the TextServ TLS globals.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE


ulong TextServGlobals::s_luTls;

/*----------------------------------------------------------------------------------------------
	Provides hooks into module level events for managing our thread local storage data.
	Hungarian: tse.
----------------------------------------------------------------------------------------------*/
class TextServEntry : public ModuleEntry
{
public:
	virtual void ProcessAttach(void)
	{
		TextServGlobals::ProcessAttach();
	}

	virtual void ProcessDetach(void)
	{
		TextServGlobals::ProcessDetach();
	}

	virtual void ThreadDetach(void)
	{
		TextServGlobals::ThreadDetach();
	}

	virtual void ThreadAttach(void)
	{
		TextServGlobals::ThreadAttach();
	}
};

TextServEntry g_tse;
CRITICAL_SECTION TextServGlobals::g_crs;
TsgVec TextServGlobals::g_vptsg;


/*----------------------------------------------------------------------------------------------
	Allocate our TLS slot.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ProcessAttach(void)
{
	s_luTls = TlsAlloc();
	if (0xFFFFFFFF == s_luTls)
		ThrowHr(WarnHr(E_FAIL));
	InitializeCriticalSection(&g_crs);
}


/*----------------------------------------------------------------------------------------------
	Free our TLS slot, and all the allocated memory. Note that this may not be called in
	the same thread as ProcessAttach.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ProcessDetach(void)
{
	EnterCriticalSection(&g_crs);
	for (int itsg = 0; itsg < g_vptsg.Size(); itsg++)
		delete g_vptsg[itsg];
	LeaveCriticalSection(&g_crs);
	// Assume other threads are gone...
	DeleteCriticalSection(&g_crs);
	TlsFree(s_luTls);
	s_luTls = 0xFFFFFFFF;
}

/*----------------------------------------------------------------------------------------------
	Note the thread is attached. Currently there is nothing to do.
	Note that we don't get this call for the thread that calls ProcessAttach.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ThreadAttach(void)
{
}


/*----------------------------------------------------------------------------------------------
	Free our thread local data. Note that we are not guaranteed this is called for every
	thread; it is called if a thread is explicitly killed, but not if the library is unloaded
	or the process ends.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ThreadDetach(void)
{
	TextServGlobals * ptsg = reinterpret_cast<TextServGlobals *>(TlsGetValue(s_luTls));
	AssertPtrN(ptsg);
	if (ptsg)
	{
		delete ptsg;
		TlsSetValue(s_luTls, NULL);
		RemoveFromTsgVec(ptsg);
	}
}

/*----------------------------------------------------------------------------------------------
	Add to the vector of blocks to free if we get a ProcessDetach.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::AddToTsgVec(TextServGlobals * ptsg)
{
	EnterCriticalSection(&g_crs);
	g_vptsg.Push(ptsg);
	LeaveCriticalSection(&g_crs);
}
/*----------------------------------------------------------------------------------------------
	Free our thread local data.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::RemoveFromTsgVec(TextServGlobals * ptsg)
{
	EnterCriticalSection(&g_crs);
	for (int itsg = 0; itsg < g_vptsg.Size(); itsg++)
	{
		if (g_vptsg[itsg] == ptsg)
		{
			g_vptsg.Delete(itsg);
			break;
		}
	}
	LeaveCriticalSection(&g_crs);
}


/*----------------------------------------------------------------------------------------------
	Get our thread local data.
----------------------------------------------------------------------------------------------*/
TextServGlobals * TextServGlobals::GetTsGlobals(void)
{
	TextServGlobals * ptsg = reinterpret_cast<TextServGlobals *>(TlsGetValue(s_luTls));
	AssertPtrN(ptsg);

	if (!ptsg)
	{
		ptsg = NewObj TextServGlobals;
		TlsSetValue(s_luTls, ptsg);
		AddToTsgVec(ptsg);
	}

	return ptsg;
}


/*----------------------------------------------------------------------------------------------
	Increment or decrement the count of active clients of the empty string. When the count
	becomes zero, the string is freed.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ActivateEmptyString(bool fActive)
{
	TextServGlobals * ptsg = GetTsGlobals();

	if (fActive)
	{
		Assert(ptsg->m_cactActive >= 0);
		ptsg->m_cactActive++;
	}
	else
	{
		Assert(ptsg->m_cactActive > 0);
		if (ptsg->m_cactActive > 0 && !--ptsg->m_cactActive)
			ptsg->m_qtssEmpty.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Get the empty string for this thread.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::GetEmptyString(ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);

	TextServGlobals * ptsg = GetTsGlobals();

	if (!ptsg->m_qtssEmpty)
	{
		if (ptsg->m_cactActive <= 0)
		{
			Warn("Empty string requested with no active clients");
			TsStrSingle::Create(NULL, 0, NULL, pptss);
		}
		TsStrSingle::Create(NULL, 0, NULL, &ptsg->m_qtssEmpty);
	}

	*pptss = ptsg->m_qtssEmpty;
	AddRefObj(*pptss);
}

#include <vector_i.cpp>
template Vector<TextServGlobals *>; // TsgVec; // Hungarian vtsg
