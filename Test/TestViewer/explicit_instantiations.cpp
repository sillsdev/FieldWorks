/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: explicit_instantiations.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Explicitly instantiate HashMap and Vector types used in TestViewer.
----------------------------------------------------------------------------------------------*/

// Standard includes for the module, gives the types we have to instantiate
#include "main.h"
#pragma hdrstop

// Standard includes for doing explicit instantiation:

#include "Vector_i.cpp"

template Vector<StrUni>;
template Vector<StrApp>;