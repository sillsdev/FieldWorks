/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ExplicitInstantiation.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Explicitly instantiate HashMap and vector types used in the Views module
-------------------------------------------------------------------------------*//*:End Ignore*/
// Standard includes for the module, gives the types we have to instantiate
#include "Main.h"
#pragma hdrstop

// Standard includes for doing explicit instantiation
#include "HashMap_i.cpp"
#include "ComHashMap_i.cpp"
#include "Vector_i.cpp"
#include "Set_i.cpp"
#include "ComMultiMap_i.cpp"
#include "GpHashMap_i.cpp"
#include "MultiMap_i.cpp"

// Types we use.
template class ComHashMap<VwPropertyStore::IntPropKey, VwPropertyStore>; // MapIPKPropStore (VwPropertyStore.h)
template class ComHashMap<int, VwPropertyStore>; // MapEncPropStore (VwPropertyStore.h)
template class Vector<VwPropertyStore::StrPropRec>; // VecStrProps (VwPropertyStore.h)
template class Vector<VwBox *>; // BoxVec (Main.h)
template class HashMap<VwBox *, Rect>; //FixupMap (Main.h)
template class Vector<VwGroupBox *>; // GroupBoxVec (Main.h)
template class Vector<VwPropertyStore *>; // PropsVec (VwEnv.h)
template class Vector<int>; // IntVec (main.h and others)
template class ComVector<VwAbstractNotifier>; // NotifierVec (Main.h)
template class Set<VwBox *>; // BoxSet (Main.h)
template class Vector<BuildRec>; // BuildVec (Main.h)
template class Vector<VwEnv::NotifierStackItem *>; //NotifierDataVec (VwEnv.h)
template class Vector<VwEnv::NotifierRec *>; // NotifierRecVec (VwEnv.h)
#ifdef WIN32
template class Vector<HVO>; // HvoVec (main.h) - same as Vector<int>
#endif
template class Vector<long>; // LongVec (VwLazyBox.h)
template class Vector<VwNotifier::PropBoxRec>; // PropBoxList (VwNotifier.h)
template class ComHashMapStrUni<ITsTextProps>; // MapStrTtp; (VwPropertyStore.h)
template class Vector<VwColumnSpec>; // ColSpecs (VwTable.h)
template class Vector<VwTableCellBox *>; //VwTable.h
template class ComMultiMap<VwBox *, VwAbstractNotifier>; // NotifierMap; (Main.h)
template class ComVector<IVwViewConstructor>; //VwVcVec; (VwRootBox.h)
template class ComMultiMap<HVO, VwAbstractNotifier>; // ObjNoteMap(VwRootBox.h)
template class ComVector<ITsString>; // StringVec (VwEnv.h)
template class Vector<VpsTssRec>; // VpsTssVec; (VwTxtSrc.h)
template class ComHashMap<ITsTextProps *, VwPropertyStore>; // MapTtpPropStore;
template class ComVector<ITsTextProps>; // TtpVec
template class ComVector<IVwPropertyStore>; // VwPropsVec;
template class Vector<unsigned char>;
template class HashMap<int, int>;
template class Vector<VwSelection *>; // SelVec;
template class Vector<LgEndSegmentType>;
template class Vector<char>;
#ifdef WIN32
template class Vector<byte>;
#endif
template class Vector<wchar>;
template class Vector<GUID>;
template class GpHashMap<int, VwPage>;
template class MultiMap<VwBox *, int>; // BoxIntMultiMap; // Hungarian mmbi;
template class Vector<VwPage *>;
template class Vector<VwMoveablePileBox *>;
template class Vector<PageLine>; // PageLineVec; // Hungarian vln;
template class Vector<Rect>;
