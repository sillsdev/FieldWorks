/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrCommon.h
Responsibility: Sharon Correll
Last reviewed: not yet

Description:
	FieldWorks-only version of the common generic header file.
----------------------------------------------------------------------------------------------*/

#include "common.h"
#include "GrPlatform.h"

#ifndef GrAssert
//#include <cassert>
#define GrAssert(exp) Assert(exp)
#endif

using namespace gr;

using std::max;
using std::min;