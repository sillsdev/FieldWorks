/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: AccessibilityHelperBase.h
Responsibility: Eberhard Beilharz
Last reviewed:

Description:
	This file contains the class declarations for the following class:
		AccessibilityHelperBase - Managed wrapper for IAccessible with additional features
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#using <mscorlib.dll>

using namespace System;
using namespace System::Collections;
using namespace System::Windows::Forms;
using namespace System::Runtime::InteropServices;
using namespace SIL::FieldWorks::Common::COMInterfaces;

namespace SysWin32
{
	[DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet::Unicode)]
   extern "C" int FindWindow(String^ winClass, String^ winName);
}

namespace SIL
{
	namespace FieldWorks
	{
		namespace AcceptanceTests
		{
			namespace Framework
			{
				public ref class AccessibilityHelperBase
				{
				public:
					AccessibilityHelperBase(); // makes top window ah
					AccessibilityHelperBase(long hWnd);
					AccessibilityHelperBase(System::Drawing::Point ptScreen);
					AccessibilityHelperBase(String^ pchWindowName);
					AccessibilityHelperBase(
						IAccessible * pAcc, VARIANT * pVar, bool fRealAccObj)
					{
						::CoInitialize(NULL);
						m_pAcc = pAcc;
						m_pAcc->AddRef();
						m_pVariant = new VARIANT();
						VariantInit(m_pVariant);
						VariantCopy(m_pVariant, pVar);
						m_fRealAccessibleObject = fRealAccObj;
					}

					AccessibilityHelperBase(AccessibilityHelperBase^ pah)
					{
						::CoInitialize(NULL);
						m_pAcc = pah->m_pAcc;
						m_pAcc->AddRef();
						m_pVariant = new VARIANT();
						VariantInit(m_pVariant);
						VariantCopy(m_pVariant, pah->m_pVariant);
						m_fRealAccessibleObject = pah->m_fRealAccessibleObject;
					}

					// Get the enumerator for this accessible window.
					IEnumerator^ GetEnumerator();

					// Create a new AccessibilityHelperBase for return by enumerator.
					virtual AccessibilityHelperBase^ CreateAccessibilityHelper(
						AccessibilityHelperBase^ pah)
					{ return pah; }

					// Get the window handle for this accessible window.
					property virtual long HWnd
					{
						long get();
					}

					// Get the accessible name for this accessible window.
					property virtual String^ Name
					{
						String^ get();
					}

					// Get the accessible role for this accessible window.
					property virtual AccessibleRole Role
					{
						AccessibleRole get();
					}

					// Get the accessible states for this accessible window.
					property virtual AccessibleStates States
					{
						AccessibleStates get();
					}

					// Get the accessible keyboard shortcut for this accessible window.
					property virtual String^ Shortcut
					{
						String^ get();
					}

					// Get the accessible default action for this accessible window.
					property virtual String^ DefaultAction
					{
						String^ get();
					}

					// Get the value for this accessible window.
					property virtual String^ Value
					{
						String^ get();
					}

					// Get the IAccessible pointer.
					property IAccessible* Accessible
					{
						IAccessible* get() { return m_pAcc; }
					}

					// Can't figure how managed code can compare pointers (IAccessible is a
					// non-public managed class in managed code, at least with the headers we're using in VS2005)
					// so return something we can readily compare in managed code.
					property virtual long AccessibleLong
					{
						long get() { return (long)m_pAcc; }
					}

					property IOleServiceProvider^ ServiceProvider
					{
						IOleServiceProvider^ get()
						{
							//return (IOleServiceProvider *) m_pAcc;
							return nullptr;
						}
					}

					// Returns true if this is an accessible object that implements IAccessible.
					property virtual bool IsRealAccessibleObject
					{
						bool get() { return m_fRealAccessibleObject; }
					}

					// Returns the number of children.
					property virtual long ChildCount
					{
						long get();
					}

					// Performs the default action.
					virtual bool DoDefaultAction();

					// Send a Windows message to the window.
					virtual int SendWindowMessage(int wm, int wparam, int lparam);

					// Move the mouse over the center of this GUI element.
					virtual void MoveMouseOverMe();

					// Move the mouse over this GUI element.
					// dx and dy are "codes" for where to move to relative to
					// the left, top accLocation of the accessible object.
					// If the code is positive, the number represents the number
					// of pixels from the left, top edge to move to.
					// If one of these numbers is larger than the object
					// width or height, the mouse is moved toward the center.
					// If the code is negative, the number is the percent of
					// width and height to add to the left top edge to move to.
					virtual void MoveMouseOverMe(long dx, long dy);

					// Move the mouse relative to this GUI element.
					// dx and dy are "offsets" for moving to relative to
					// the left, top accLocation of the accessible object.
					// The offsets represent the number
					// of pixels from the left, top edge to move to.
					virtual void MoveMouseRelative(long dx, long dy);

					// Simulate a click at the center of the item.
					virtual void SimulateClick(); // left click

					// Simulate a click at the specified place on the item.
					// See MoveMouseOverMe for notes on dx and dy.
					virtual void SimulateClick(long dx, long dy); // left click

					// Simulate a click relative to the item.
					// See MoveMouseRelative for notes on dx and dy.
					virtual void SimulateClickRelative(long dx, long dy); // left click

					// Simulate a right click at the center of the item.
					virtual void SimulateRightClick();

					// Simulate a right click at the specified place on the item.
					// See MoveMouseOverMe for notes on dx and dy.
					virtual void SimulateRightClick(long dx, long dy);

					// Simulate a right click relative to the item.
					// See MoveMouseRelative for notes on dx and dy.
					virtual void SimulateRightClickRelative(long dx, long dy);

					// Make a simple selection of text in a FW view, an ID is returned
					virtual int MakeSimpleSel (bool fInitial, bool fEdit, bool fRange, bool fInstall);

					IOleServiceProvider^ Provider();

				protected:
					// Get the focused element ah if it's this ah or one of its descendants.
					property AccessibilityHelperBase^ FocusedAh
					{
						AccessibilityHelperBase^ get();
					}

					// Navigate to the specified element
					AccessibilityHelperBase^ Navigate1(AccessibleNavigation navDir);

					// Get the parent object
					property AccessibilityHelperBase^ Parent1
					{
						AccessibilityHelperBase^ get();
					}

					// Get the ah for the IDispatch.
					AccessibilityHelperBase^ getIDispatchAh(IDispatch * pDisp);

					// Get the ah for the VARIANT.
					AccessibilityHelperBase^ getVariantAh(VARIANT var);

					// Code shared by two constructors.
					void InitFromHwnd(HWND hWnd);

					IntPtr GetIUnknown()
					{
						return (IntPtr)m_pAcc;
					}

				// member variables
				protected:
					IAccessible* m_pAcc;

					VARIANT * m_pVariant;
					bool m_fRealAccessibleObject;
				};
			}
		}
	}
}
