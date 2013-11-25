/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: AccessibilityEnumerator.cpp
Responsibility: Eberhard Beilharz
Last reviewed:

Description:
	This file contains the class definition for the following class:
		AccessibilityEnumerator - Enumerates the children of an IAccessible object.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "StdAfx.h"
#include "AccessibilityHelperBase.h"
#include "AccessibilityEnumerator.h"
using namespace SIL::FieldWorks::AcceptanceTests::Framework;

// Constructor
AccessibilityEnumerator::AccessibilityEnumerator(AccessibilityHelperBase^ pAccParent)
{
	m_pParent = pAccParent;
	m_pAcc = m_pParent->Accessible;

	IEnumVARIANT* pEnum;
	m_pAcc->QueryInterface(IID_IEnumVARIANT, (void**) &pEnum);

	m_pEnum = pEnum;

	if(m_pEnum)
		m_pEnum->Reset();

	m_pVarChild = new VARIANT();
	VariantInit(m_pVarChild);
}

//Destructor
AccessibilityEnumerator::~AccessibilityEnumerator(void)
{
	if (m_pVarChild)
		VariantClear(m_pVarChild);
	delete m_pVarChild;
	m_pVarChild = NULL;

	if (m_pEnum)
		m_pEnum->Release();
}

Object^ AccessibilityEnumerator::Current::get()
{
	HRESULT hr;
	IDispatch* pDisp;
	IAccessible* paccChild = NULL;

	// Get IDispatch interface for the child
	if (m_pVarChild->vt == VT_I4)
	{
		pDisp = NULL;
		hr = m_pAcc->get_accChild(*m_pVarChild, &pDisp);
	}
	else
		pDisp = m_pVarChild->pdispVal;

	// Get IAccessible interface for the child
	if (pDisp)
	{
		hr = pDisp->QueryInterface(IID_IAccessible, (void**)&paccChild);
		hr = pDisp->Release();
	}

	// Get information about the child
	bool fRealIAccObj;
	if(paccChild)
	{
		// a real IAccessible object
		VariantInit(m_pVarChild);
		m_pVarChild->vt = VT_I4;
		m_pVarChild->lVal = CHILDID_SELF;
		fRealIAccObj = true;
	}
	else
	{
		// not a real IAccessible object
		paccChild = m_pAcc;
		fRealIAccObj = false;
	}

	AccessibilityHelperBase^ ah = nullptr;
	if (paccChild)
		ah = m_pParent->CreateAccessibilityHelper(
			gcnew AccessibilityHelperBase(paccChild, m_pVarChild, fRealIAccObj) );

	if (paccChild)
		paccChild->Release();

	return ah;
}

bool AccessibilityEnumerator::MoveNext()
{
	ULONG nFetched;
	HRESULT hr;

	if (!m_pEnum)
		return false;
	VariantClear(m_pVarChild);

	hr = m_pEnum->Next(1, m_pVarChild, &nFetched);

	return hr == S_OK && nFetched == 1;
}

void AccessibilityEnumerator::Reset()
{
	if(m_pEnum)
		m_pEnum->Reset();
}
