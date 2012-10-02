/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: explicit_instantiations.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Explicitly instantiate HashMap and Vector types used in the WRCompiler module.
-------------------------------------------------------------------------------*//*:End Ignore*/

// Standard includes for the module, gives the types we have to instantiate
#include "main.h"
#pragma hdrstop

// Standard includes for doing explicit instantiation
#include "HashMap_i.cpp"
#include "Vector_i.cpp"
#include "Set_i.cpp"

// Types we use:

template class Vector<bool>;
template class Vector<int>;
template class Vector<utf16>;
template class Vector<byte>;
template class Vector<long>;
template class Vector<unsigned int>;

template class Vector<StrAnsi>;
template class Vector<StrUni>;

template class Vector<ExpressionType>;
template class Vector<Symbol>;

template class Vector<GdlFeatureDefn*>;
template class Vector<GdlFeatureSetting*>;
template class Vector<GdlLanguageDefn*>;
template class Vector<GdlLangClass*>;
template class Vector<GrpLineAndFile>;
template class Vector<GdlExtName>;

template class Vector<GdlGlyphClassDefn*>;
template class Vector<GdlGlyphClassMember*>;
template class Vector<GdlGlyphAttrSetting*>;

//template class Vector<GdlIdentifierDefn*>;

template class Vector<GdlRuleTable*>;
template class Vector<GdlPass*>;
template class Vector<GdlRule*>;
template class Vector<GdlRuleItem*>;
template class Vector<GdlAttrValueSpec*>;
template class Vector<GdlAlias*>;

template class Vector<GdlExpression*>;
template class Vector<GdlSlotRefExpression*>;

template class Vector<GrcErrorItem*>;
template class Vector<GrcEnv>;

template class Vector<FsmMachineClass *>;
template class Vector<FsmState *>;

template class HashMap<utf16, int>;
template class HashMap<utf16, utf16>;
template class HashMap<Symbol, GdlAssignment*>;
template class HashMap<Symbol, GrcMasterValueList*>;
template class HashMap<Symbol, int>;
template class HashMap<StrAnsi, Symbol, HashStrAnsi, EqlStrAnsi>;
template class HashMap<int, GdlNameDefn*>;
template class HashMap<int, Vector<FsmMachineClass *> *>;

template class Set<int>;
template class Set<utf16>;
template class Set<unsigned int>;
template class Set<GdlGlyphDefn *>;
template class Set<GdlGlyphClassDefn *>;
template class Set<FsmMachineClass *>;
template class Set<Symbol>;
