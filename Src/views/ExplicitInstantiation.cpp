/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ExplicitInstantiation.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Explicitly instantiate HashMap and vector types used in the Views module
-------------------------------------------------------------------------------*//*:End Ignore*/
// Standard includes for the module, gives the types we have to instantiate
#include "main.h"
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
template ComHashMap<VwPropertyStore::IntPropKey, VwPropertyStore>; // MapIPKPropStore (VwPropertyStore.h)
template ComHashMap<int, VwPropertyStore>; // MapEncPropStore (VwPropertyStore.h)
template Vector<VwPropertyStore::StrPropRec>; // VecStrProps (VwPropertyStore.h)
template Vector<VwBox *>; // BoxVec (Main.h)
template HashMap<VwBox *, Rect>; //FixupMap (Main.h)
template Vector<VwGroupBox *>; // GroupBoxVec (Main.h)
template Vector<VwPropertyStore *>; // PropsVec (VwEnv.h)
template Vector<int>; // IntVec (main.h and others)
template ComVector<VwAbstractNotifier>; // NotifierVec (Main.h)
template Set<VwBox *>; // BoxSet (Main.h)
template Vector<BuildRec>; // BuildVec (Main.h)
template Vector<VwEnv::NotifierStackItem *>; //NotifierDataVec (VwEnv.h)
template Vector<VwEnv::NotifierRec *>; // NotifierRecVec (VwEnv.h)
template Vector<HVO>; // HvoVec (main.h)
template Vector<long>; // LongVec (VwLazyBox.h)
template Vector<VwNotifier::PropBoxRec>; // PropBoxList (VwNotifier.h)
template ComHashMapStrUni<ITsTextProps>; // MapStrTtp; (VwPropertyStore.h)
template Vector<VwColumnSpec>; // ColSpecs (VwTable.h)
template Vector<VwTableCellBox *>; //VwTable.h
template ComMultiMap<VwBox *, VwAbstractNotifier>; // NotifierMap; (Main.h)
template ComVector<IVwViewConstructor>; //VwVcVec; (VwRootBox.h)
template ComMultiMap<HVO, VwAbstractNotifier>; // ObjNoteMap(VwRootBox.h)
template ComVector<ITsString>; // StringVec (VwEnv.h)
template Vector<VpsTssRec>; // VpsTssVec; (VwTxtSrc.h)
template ComHashMap<ITsTextProps *, VwPropertyStore>; // MapTtpPropStore;
template ComVector<ITsTextProps>; // TtpVec
template ComVector<IVwPropertyStore>; // VwPropsVec;
template Vector<unsigned char>;
template HashMap<int, int>;
template Vector<VwSelection *>; // SelVec;
template Vector<LgEndSegmentType>;
template Vector<char>;
template Vector<byte>;
template Vector<wchar>;
template Vector<GUID>;
template GpHashMap<int, VwPage>;
template MultiMap<VwBox *, int>; // BoxIntMultiMap; // Hungarian mmbi;
template Vector<VwPage *>;
template Vector<VwMoveablePileBox *>;
template HashMapStrUni<enchant::Dict *>;
template Vector<PageLine>; // PageLineVec; // Hungarian vln;
template Vector<Rect>;
