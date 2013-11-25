/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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