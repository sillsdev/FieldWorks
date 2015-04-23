/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: HashMap.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file contains the default implementations of the default hashing and equality
	functors for the various hash map collection classes.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include <cstring>

#include "GrPlatform.h"
#include "UtilHashMap.h"
#include "GrDebug.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

namespace gr
{

/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Compute a hash value from the bits of an arbitrary object, and return the computed value.

	@param pKey Pointer to a block of memory (presumably an object of some sort).
	@param cbKey Number of bytes in the block of memory.
----------------------------------------------------------------------------------------------*/
int HashObj::operator () (void * pKey, int cbKey)
{
	if ((0L == pKey) || (cbKey <= 0))
		return 0;
	int nHash = 0;
	int i;
	if (0 == (cbKey % sizeof(int)))
	{
		int cn = cbKey / sizeof(int);
		int * pn = (int *)pKey;
		for (i = 0; i < cn; ++i)
			nHash += (nHash << 4) + *pn++;
	}
	else if (0 == (cbKey % sizeof(short)))
	{
		int csu = cbKey / sizeof(short);
		unsigned short * psu = (unsigned short *)pKey;
		for (i = 0; i < csu; ++i)
			nHash += (nHash << 4) + *psu++;
	}
	else
	{
		byte * pb = (byte *)pKey;
		for (i = 0; i < cbKey; ++i)
			nHash += (nHash << 4) + *pb++;
	}
	return nHash;
}

/*----------------------------------------------------------------------------------------------
	Compare the bits of two objects for being the same, returning true if the two objects have
	exactly the same bits, and otherwise returning false.

	@param pKey1 Pointer to a block of memory (presumably an object of some sort).
	@param pKey2 Pointer to another block of memory (presumably an object of some sort).
	@param cbKey Number of bytes in each block of memory.
----------------------------------------------------------------------------------------------*/
bool EqlObj::operator () (void * pKey1, void * pKey2, int cbKey)
{
	if (pKey1 == pKey2)
		return true;
	if ((0L == pKey1) || (0L == pKey2))
		return false;
	if (cbKey <= 0)
		return true;
	return (0 == memcmp(pKey1, pKey2, cbKey));
}


} //namespace gr
