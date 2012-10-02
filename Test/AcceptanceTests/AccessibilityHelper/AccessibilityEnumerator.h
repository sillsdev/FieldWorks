/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AccessibilityEnumerator.h
Responsibility: Eberhard Beilharz
Last reviewed:

Description:
	This file contains the class declarations for the following class:
		AccessibilityEnumerator - Enumerates the children of an IAccessible object.
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once

using namespace System;
using namespace System::Collections;
using namespace System::Windows::Forms;

namespace SIL
{
	namespace FieldWorks
	{
		namespace AcceptanceTests
		{
			namespace Framework
			{
				ref class AccessibilityHelperBase; //forward declaration

				public ref class AccessibilityEnumerator :
					public IEnumerator
				{
				public:
					AccessibilityEnumerator(AccessibilityHelperBase^ pAccParent);
					virtual ~AccessibilityEnumerator(void);

					property Object^ Current
					{
						virtual Object^ get();
					}
					virtual bool MoveNext();
					virtual void Reset();

				protected:
					IEnumVARIANT * m_pEnum;
					IAccessible * m_pAcc;
					VARIANT * m_pVarChild;
					AccessibilityHelperBase^ m_pParent;

				};
			}
		}
	}
}