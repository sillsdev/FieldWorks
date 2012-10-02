/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeInt.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides for nested field editors.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_INT_INCLUDED
#define AFDEFE_INT_INCLUDED 1

/*----------------------------------------------------------------------------------------------
	This class is used to edit integers in a data entry field.
	Hungarian: dei.
----------------------------------------------------------------------------------------------*/

class AfDeFeInt : public AfDeFeUni
{
public:
	typedef AfDeFeUni SuperClass;

	AfDeFeInt();
	~AfDeFeInt();

	virtual bool IsDirty();
	virtual bool SaveEdit();
	virtual void UpdateField();
	virtual bool ValidKeyUp(UINT wp);

protected:
};


#endif // AFDEFE_INT_INCLUDED
