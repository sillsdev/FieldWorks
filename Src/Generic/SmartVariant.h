/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: SmartVariant.h
Responsibility: Ken Zook
Last reviewed:

	Smart variant class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef SmartVariant_H
#define SmartVariant_H 1

/*----------------------------------------------------------------------------------------------
	Smart VARIANT class. This handles automatically initializing and clearing a variant.
----------------------------------------------------------------------------------------------*/
class SmartVariant : public VARIANT
{
protected:

	// Set smart variant to Error state and throw an exception. Does not return.
	void _Fail(void)
	{
		Clear();
		vt = VT_ERROR;
		scode = WarnHr(E_OUTOFMEMORY);
		ThrowHr(E_OUTOFMEMORY);
	}

public:
	// Destructor.
	~SmartVariant(void)
	{
		Clear();
	}

	bool Error()
	{
		return vt == VT_ERROR;
	}

	void Clear(void)
	{
		switch (vt)
		{
		case VT_EMPTY:
		case VT_NULL:
		case VT_I2:
		case VT_I4:
		case VT_I8:
		case VT_R4:
		case VT_R8:
		case VT_CY:
		case VT_DATE:
		case VT_BOOL:
		case VT_I1:
		case VT_UI1:
		case VT_UI2:
		case VT_UI4:
		case VT_INT:
		case VT_UINT:
			vt = VT_EMPTY;
			break;

		default:
			VariantClear(this);
			break;
		}
		// StrBase<XChar>::GetBstr Asserts that bstrVal is NULL, so this must be cleared.
		bstrVal = NULL;

	}

	SmartVariant(void)
	{
		vt = VT_EMPTY;
		// StrBase<XChar>::GetBstr Asserts that bstrVal is NULL, so this must be cleared.
		bstrVal = NULL;
	}

	SmartVariant(const SmartVariant & svar)
	{
		vt = VT_EMPTY;
		Assign(svar);
	}

	SmartVariant(const VARIANT & var)
	{
		vt = VT_EMPTY;
		Assign(var);
	}

	SmartVariant & operator=(const SmartVariant & svar)
	{
		Assign(svar);
		return *this;
	}

	SmartVariant & operator=(const VARIANT & var)
	{
		Assign(var);
		return *this;
	}

	void Assign(const VARIANT & var)
	{
		if (FAILED(::VariantCopy(this, const_cast<VARIANT *>(&var))))
			_Fail();
	}

	void SetInteger(int n)
	{
		Clear();
		vt = VT_I4;
		lVal = n;
	}

	void SetInt64(int64 lln)
	{
		Clear();
		// vt = VT_CY;
		vt = VT_I8; // availabe now, fits our use better
		cyVal.int64 = lln;
	}

	void SetDouble(double dbl)
	{
		Clear();
		vt = VT_R8;
		dblVal = dbl;
	}

	void SetBool(bool f)
	{
		Clear();
		vt = VT_BOOL;
		boolVal = f ? VARIANT_TRUE : VARIANT_FALSE;
	}

	void SetDispatch(IDispatch * pdisp)
	{
		AssertPtrN(pdisp);
		AddRefObj(pdisp);
		Clear();
		vt = VT_DISPATCH;
		pdispVal = pdisp;
	}

	void SetUnknown(IUnknown * punk)
	{
		AssertPtrN(punk);
		AddRefObj(punk);
		Clear();
		vt = VT_UNKNOWN;
		punkVal = punk;
	}

	void SetBstr(BSTR bstr)
	{
		AssertBstrN(bstr);
		SetChars(bstr, BstrLen(bstr));
	}

	void SetString(LPCOLESTR psz)
	{
		AssertPszN(psz);
		SetChars(psz, StrLen(psz));
	}

	void SetChars(const OLECHAR * prgch, int cch)
	{
		AssertArray(prgch, cch);
		BSTR bstr;
		if (!cch)
			bstr = NULL;
		else if ((bstr = ::SysAllocStringLen(prgch, cch)) == NULL)
			_Fail();
		Clear();
		vt = VT_BSTR;
		bstrVal = bstr;
		return;
	}

	void GetInteger(int * pn);
	void GetBoolean(bool * pf);
	void GetInt64(int64 * plln);
	void GetSilTime(SilTime * pstim);
	void GetDouble(double * pdbl);
	bool GetObject(REFIID iid, void ** ppv);
	void GetObjectOrNull(REFIID iid, void ** ppv);
};

#endif // !SmartVariant_H
