/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: explicit_instantiations.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Explicitly instantiate HashMap and Vector types used in WorldPad.
-------------------------------------------------------------------------------*//*:End Ignore*/

// Standard includes for the module, gives the types we have to instantiate
#include "main.h"
#pragma hdrstop

// Standard includes for doing explicit instantiation:
#include "Vector_i.cpp"
#include "Set_i.cpp"
#include "HashMap_i.cpp"


template Vector<int>;
template Vector<char>;
template Vector<byte>;
template Vector<wchar>;
template Vector<void *>;
template Vector<StrUni>;
template Vector<StrAnsi>;
template Vector<StrApp>;
template Vector<WpWrSysDlg::WsData>;
template Vector<AfMainWndPtr>;
template Vector<HvoClsid>;
template Vector<AfToolBarPtr>;
template Vector<AfExportStyleSheet>;
template Vector<WpWrSysDlg::FeatList>;
template Set<int>;
template HashMap<HVO, StrUni>;
