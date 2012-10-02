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

template<> const GUID __uuidof(IMyClass)("293a4785-92ed-41f9-8091-8b697bc9151c");
