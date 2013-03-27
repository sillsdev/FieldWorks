/*
 *	Filename
 *
 *	Description
 *
 *	Neil Mayhew - 06 Sep 2006
 *
 *	$Id$
 */

#include "testGenericLib.h"
#include "COMBase.h"

struct IMyClass : public IUnknown
{
};

class MyClass : public COMBase::Unknown<IMyClass>
{
};

int main(int argc, char** argv)
{
	MyClass* myClass = new MyClass;

	return myClass->Release();
}

DEFINE_UUIDOF(IMyClass,0x293a4785,0x92ed,0x41f9,0x80,0x91,0x8b,0x69,0x7b,0xc9,0x15,0x1c);
