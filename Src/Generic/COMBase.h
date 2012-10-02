/*
 *	COMBase.h
 *
 *	Template base class for implementing COM classes
 *
 *	Neil Mayhew - 06 Sep 2006
 *
 *	$Id$
 */

#ifndef _COMBASE_H_
#define _COMBASE_H_

#include <COM.h>
#include <COMInterfaces.h>
#include <WinError.h>

namespace COMBase
{
	template<class I>
	class Unknown : public I
	{
	public:
		virtual UCOMINT32 AddRef();
		virtual UCOMINT32 Release();
		virtual HRESULT QueryInterface(REFIID riid, void** ppv);
	protected:
		Unknown();
		virtual ~Unknown();
	private:
		UCOMINT32 m_cref;
	};

	template<class I>
	Unknown<I>::Unknown()
		: m_cref(1)
	{
	}

	template<class I>
	Unknown<I>::~Unknown()
	{
	}

	template<class I>
	UCOMINT32 Unknown<I>::AddRef()
	{
		//Assert(m_cref > 0);
		::InterlockedIncrement(&m_cref);
		return m_cref;
	}

	template<class I>
	UCOMINT32 Unknown<I>::Release()
	{
		//Assert(m_cref > 0);
		UCOMINT32 cref = ::InterlockedDecrement(&m_cref);
		if (!cref)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	template<class I>
	HRESULT Unknown<I>::QueryInterface(REFIID iid, void** ppv)
	{
		//AssertPtr(ppv);
		if (!ppv)
			return /*WarnHr*/(E_POINTER);
		*ppv = NULL;

		if (iid == __uuidof(IUnknown))
		{
			*ppv = static_cast<IUnknown*>(this);
		}
		else if (iid == __uuidof(I))
		{
			*ppv = static_cast<I*>(this);
		}
		/*else if (iid == IID_ISupportErrorInfo)
		{
			*ppv = NewObj CSupportErrorInfo(
				static_cast<IUnknown*>(static_cast<I*>(this)), __uuidof(I));
			return S_OK;
		}*/
		else
		{
			return E_NOINTERFACE;
		}
		reinterpret_cast<IUnknown*>(*ppv)->AddRef();
		return S_OK;
	}
}

#endif//_COMBASE_H_
