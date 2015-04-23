/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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