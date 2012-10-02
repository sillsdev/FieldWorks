/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AccessibilityHelperBase.cpp
Responsibility: Eberhard Beilharz
Last reviewed:

Description:
	This file contains the class definition for the following class:
		AccessibilityHelperBase - Managed wrapper for IAccessible with additional features
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "stdafx.h"

#include "AccessibilityEnumerator.h"
#include "AccessibilityHelperBase.h"

using namespace SIL::FieldWorks::AcceptanceTests::Framework;

AccessibilityHelperBase::AccessibilityHelperBase()
{
	// get the top window - the one with focus
	HWND hwnd = (HWND)SysWin32::FindWindow(nullptr, nullptr);
	InitFromHwnd(hwnd);
}

AccessibilityHelperBase::AccessibilityHelperBase(long hWnd)
{
	InitFromHwnd((HWND)hWnd);
}

void AccessibilityHelperBase::InitFromHwnd(HWND hWnd)
{
	::CoInitialize(NULL);
	IAccessible* pAccWindow;
	HRESULT hr = ::AccessibleObjectFromWindow(hWnd, OBJID_WINDOW, IID_IAccessible,
		(void**)&pAccWindow);
	if (SUCCEEDED(hr))
		m_pAcc = pAccWindow;

	m_pVariant = new VARIANT;
	VariantInit(m_pVariant);
	m_pVariant->vt = VT_I4;
	m_pVariant->lVal = CHILDID_SELF;

	m_fRealAccessibleObject = true;
}

AccessibilityHelperBase::AccessibilityHelperBase(System::Drawing::Point ptScreen)
{
	::CoInitialize(NULL);
	IAccessible* pAccWindow;
	m_pVariant = new VARIANT;
	VariantInit(m_pVariant);
	POINT pt;
	pt.x = ptScreen.X;
	pt.y = ptScreen.Y;

	HRESULT hr = ::AccessibleObjectFromPoint(pt, &pAccWindow, m_pVariant);
	if (FAILED(hr))
		throw hr;

	m_pAcc = pAccWindow;
	m_fRealAccessibleObject = (m_pVariant->lVal == CHILDID_SELF);
}

// Find the top window with this name and try to make an AH on it. If no such window
// can be found, return an AH on the topmost window.
AccessibilityHelperBase::AccessibilityHelperBase(String^ strWindowName)
{
	HWND hwnd = (HWND)SysWin32::FindWindow(nullptr, strWindowName);
	if (!hwnd)
		hwnd = ::GetTopWindow(NULL);
	InitFromHwnd(hwnd);
}

// Get the enumerator for this accessible window.
IEnumerator^ AccessibilityHelperBase::GetEnumerator()
{
	return gcnew AccessibilityEnumerator(this);
}

// Get the window handle for this accessible window.
long AccessibilityHelperBase::HWnd::get()
{
	HWND hWnd;
	::WindowFromAccessibleObject(m_pAcc, &hWnd);
	if (hWnd == 0)
	{
		long xLeft, yTop, dxWidth, dyHeight;
		VARIANT vc;
		VariantInit(&vc);
		m_pAcc->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, vc);
		POINT pt;
		pt.x = xLeft + 1;
		pt.y = yTop + 1;
		return (long)::WindowFromPoint(pt);
	}
	//if (hWnd == 0)
	//{
	//	AccessibilityHelperBase * pah = get_Parent1();
	//	if (pah != NULL)
	//		return pah->get_HWnd();
	//}
	return (long)hWnd;
}

// Get the accessible name for this accessible window.
String^ AccessibilityHelperBase::Name::get()
{
	BSTR bstr;

	HRESULT hr = m_pAcc->get_accName(*m_pVariant, &bstr);

	if (SUCCEEDED(hr) && bstr)
	{
		String^ str = gcnew String(bstr);
		SysFreeString(bstr);
		return str;
	}

	return nullptr;
}

// Get the accessible role for this accessible window.
AccessibleRole AccessibilityHelperBase::Role::get()
{
	HRESULT hr;
	VARIANT varRetVal;

	VariantInit(&varRetVal);
	hr = m_pAcc->get_accRole(*m_pVariant, &varRetVal);

	if (FAILED(hr))
		return AccessibleRole::None;
	if (varRetVal.vt != VT_I4)
	{
		throw gcnew Exception("Got unexpected string from get_accRole");
	}

	AccessibleRole role = (AccessibleRole)varRetVal.lVal;
	VariantClear(&varRetVal);

	return role;
}

// Get the accessible states for this accessible window.
AccessibleStates AccessibilityHelperBase::States::get()
{
	HRESULT hr;
	VARIANT varRetVal;

	VariantInit(&varRetVal);
	hr = m_pAcc->get_accState(*m_pVariant, &varRetVal);

	if (FAILED(hr))
		return AccessibleStates::None;
	if (varRetVal.vt != VT_I4)
	{
		throw gcnew Exception("Got unexpected string from get_accState");
	}

	AccessibleStates states = (AccessibleStates)varRetVal.lVal;
	VariantClear(&varRetVal);

	return states;
}

// Get the accessible keyboard shortcut for this accessible window.
String^ AccessibilityHelperBase::Shortcut::get()
{
	BSTR bstr;
	HRESULT hr = m_pAcc->get_accKeyboardShortcut(*m_pVariant, &bstr);

	if (SUCCEEDED(hr) && bstr)
	{
		String^ str = gcnew String(bstr);
		SysFreeString(bstr);
		return str;
	}

	return nullptr;
}

// Get the accessible default action for this accessible window.
String^ AccessibilityHelperBase::DefaultAction::get()
{
	BSTR bstr;
	HRESULT hr = m_pAcc->get_accDefaultAction(*m_pVariant, &bstr);

	if (SUCCEEDED(hr) && bstr)
	{
		String^ str = gcnew String(bstr);
		SysFreeString(bstr);
		return str;
	}

	return nullptr;
}

// Get the value for this accessible window.
String^ AccessibilityHelperBase::Value::get()
{
	BSTR bstr;

	// first try to get the value from Accessibility
	HRESULT hr = m_pAcc->get_accValue(*m_pVariant, &bstr);

	if (SUCCEEDED(hr) && bstr)
	{
		String^ str = gcnew String(bstr);
		SysFreeString(bstr);
		return str;
	}

	// that failed, so now try to get it directly from the window
	HWND hWnd;
	hr = ::WindowFromAccessibleObject(m_pAcc, &hWnd);

	if (SUCCEEDED(hr))
	{
		TCHAR szString[1024];
		int nRet = ::GetWindowText(hWnd, szString, sizeof(szString));
		if (nRet > 0)
			return gcnew String(szString);
	}

	return nullptr;
}

// Get the ah for the IDispatch.
AccessibilityHelperBase^ AccessibilityHelperBase::getIDispatchAh(IDispatch * pDisp)
{
	// Get IAccessible interface for the element
	IAccessible* paccElement = NULL;
	VARIANT var;
	VariantInit(&var);
	if (pDisp)
	{
		HRESULT hr;
		hr = pDisp->QueryInterface(IID_IAccessible, (void**)&paccElement);
		hr = pDisp->Release();
	}

	// Get information about the child
	bool fRealIAccObj;
	if (paccElement)
	{
		// a real IAccessible object
		var.vt = VT_I4;
		var.lVal = CHILDID_SELF;
		fRealIAccObj = true;
	}
	else
	{
		// not a real IAccessible object
		paccElement = m_pAcc;
		fRealIAccObj = false;
	}

	AccessibilityHelperBase^ ah = nullptr;
	if (paccElement)
		ah = CreateAccessibilityHelper(
			gcnew AccessibilityHelperBase(paccElement, &var, fRealIAccObj) );

	if (paccElement)
		paccElement->Release();

	VariantClear(&var); // will not clear the copy in ah
	return ah;
}

// Get the ah for the variant.
AccessibilityHelperBase^ AccessibilityHelperBase::getVariantAh(VARIANT var)
{
	// Get IDispatch interface for the child
	IDispatch * pDisp;
	if (var.vt == VT_EMPTY)
		return nullptr;
	else if (var.vt == VT_I4)
	{
		pDisp = NULL;
		HRESULT hr = m_pAcc->get_accChild(var, &pDisp);
		if (FAILED(hr))
			return nullptr;
	}
	else
		pDisp = var.pdispVal;

	return getIDispatchAh(pDisp);
}

// Get the focused element ah if it's this ah or one of its descendants.
AccessibilityHelperBase^ AccessibilityHelperBase::FocusedAh::get()
{
	VARIANT varEnd;
	VariantInit(&varEnd);

	HRESULT hr = m_pAcc->get_accFocus(&varEnd);

	if (FAILED(hr))
		return nullptr;

	AccessibilityHelperBase^ ah = getVariantAh(varEnd);
	VariantClear(&varEnd); // will not clear the copy in the new ah
	return ah;
}

// Navigate to the specified element
AccessibilityHelperBase^ AccessibilityHelperBase::Navigate1(AccessibleNavigation navDir)
{
	VARIANT varEnd;
	VariantInit(&varEnd);

	HRESULT hr = m_pAcc->accNavigate((long)navDir, *m_pVariant, &varEnd);

	if (FAILED(hr))
		return nullptr;

	AccessibilityHelperBase^ ah = getVariantAh(varEnd);
	VariantClear(&varEnd); // will not clear the copy in the new ah
	return ah;
}

// Get the parent object
AccessibilityHelperBase^ AccessibilityHelperBase::Parent1::get()
{
	IDispatch * pDisp;
	HRESULT hr = m_pAcc->get_accParent(&pDisp);

	if (FAILED(hr))
		return nullptr;

	return getIDispatchAh(pDisp);
}

// Returns the number of children.
long AccessibilityHelperBase::ChildCount::get()
{
	long nRet;
	HRESULT hr = m_pAcc->get_accChildCount(&nRet);

	if (SUCCEEDED(hr))
	{
		return nRet;
	}
	return 0;
}

// Performs the default action.
bool AccessibilityHelperBase::DoDefaultAction()
{
	HRESULT hr = m_pAcc->accDoDefaultAction(*m_pVariant);

	if (SUCCEEDED(hr))
	{
		return true;
	}
	return false;
}

int AccessibilityHelperBase::SendWindowMessage(int wm, int wparam, int lparam)
{
	if (!m_fRealAccessibleObject)
	{
		return 0;
	}
	return ::SendMessage((HWND)HWnd, (UINT) wm, (WPARAM) wparam, (LPARAM) lparam);
}

extern void SimulateClickInternal();

extern void SimulateRightClickInternal();

void AccessibilityHelperBase::MoveMouseOverMe()
{
	long xLeft, yTop, cxWidth, cyHeight;
	HRESULT hr;
	hr = m_pAcc->accLocation(&xLeft,  &yTop,  &cxWidth,  &cyHeight,
								*m_pVariant);
	// Hover
	if(hr == S_OK)
	{
		// position the mouse over the item.
		SetCursorPos(xLeft + cxWidth/2, yTop + cyHeight/2);
	}
}

void AccessibilityHelperBase::MoveMouseOverMe(long dx, long dy)
{
	long xLeft, yTop, cxWidth, cyHeight;
	HRESULT hr;
	hr = m_pAcc->accLocation(&xLeft,  &yTop,  &cxWidth,  &cyHeight,
								*m_pVariant);
	// Hover
	if(hr == S_OK)
	{
		long xAdd = dx;
		long yAdd = dy;
		// interpret dx and dy
		if (dx < 0 || dx > cxWidth)  xAdd = (cxWidth*abs(dx))/100;
		if (dy < 0 || dy > cyHeight) yAdd = (cyHeight*abs(dy))/100;
		// position the mouse over the item.
		SetCursorPos(xLeft + xAdd, yTop + yAdd);
	}
}

void AccessibilityHelperBase::MoveMouseRelative(long dx, long dy)
{
	long xLeft, yTop, cxWidth, cyHeight;
	HRESULT hr;
	hr = m_pAcc->accLocation(&xLeft,  &yTop,  &cxWidth,  &cyHeight,
								*m_pVariant);
	// Hover
	if(hr == S_OK)
	{
		long xAdd = dx;
		long yAdd = dy;
		// position the mouse over the item.
		SetCursorPos(xLeft + xAdd, yTop + yAdd);
	}
}

void AccessibilityHelperBase::SimulateClick()
{
	MoveMouseOverMe();
	SimulateClickInternal(); // left click
}

void AccessibilityHelperBase::SimulateClick(long dx, long dy)
{
	MoveMouseOverMe(dx, dy);
	SimulateClickInternal(); // left click
}

void AccessibilityHelperBase::SimulateClickRelative(long dx, long dy)
{
	MoveMouseRelative(dx, dy);
	SimulateClickInternal(); // left click
}

void AccessibilityHelperBase::SimulateRightClick()
{
	MoveMouseOverMe();
	SimulateRightClickInternal();
}

void AccessibilityHelperBase::SimulateRightClick(long dx, long dy)
{
	MoveMouseOverMe(dx, dy);
	SimulateRightClickInternal();
}

void AccessibilityHelperBase::SimulateRightClickRelative(long dx, long dy)
{
	MoveMouseRelative(dx, dy);
	SimulateRightClickInternal();
}

int AccessibilityHelperBase::MakeSimpleSel (bool /*fInitial*/, bool /*fEdit*/, bool /*fRange*/,
											bool /*fInstall*/)
{
	int id = -1;

	return id;
}

IOleServiceProvider^ AccessibilityHelperBase::Provider()
{
	IOleServiceProvider^ psp;
	m_pAcc->QueryInterface(IID_IServiceProvider, (void **) &psp);
	return psp;
}

//
//#pragma unmanaged
//// The INPUT structure and SendInput methods apparently aren't handled in managed code.
//// Simulate a left-button-down, pause 100 ms, left button up near the center of the
//// item.
//#include <windows.h>
//void AccessibilityHelperBase::SimulateClick()
//{
//	long xLeft, yTop, cxWidth, cyHeight;
//	HRESULT hr;
//
//	hr = m_pAcc->accLocation(&xLeft,  &yTop,  &cxWidth,  &cyHeight,
//								*m_pVariant);
//
//	// Click
//	if(hr == S_OK)
//	{
//		INPUT input[2];
//		memset(input, 0, sizeof(input));
//
//		// position the mouse over the item.
//		SetCursorPos(xLeft + cxWidth/2, yTop + cyHeight/2);
//
//		// Fill the structure
//		input[0].type = INPUT_MOUSE;
//		input[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
//		input[0].mi.dwExtraInfo = 0;
//		input[0].mi.dx = 0;
//		input[0].mi.dy = 0;
//		input[0].mi.time = GetTickCount();
//
//		// All inputs are almost the same
//		memcpy(&input[1], &input[0], sizeof(INPUT));
//
//		// ... almost
//		input[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;
//
//		SendInput(1, input, sizeof(INPUT));
//		Sleep(100);
//		SendInput(1, input+1, sizeof(INPUT));
//	}
//}
//#pragma managed
