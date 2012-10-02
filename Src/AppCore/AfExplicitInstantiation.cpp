/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfExplicitInstantiation.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Explicitly instantiate collection types used in AppCore
-------------------------------------------------------------------------------*//*:End Ignore*/

// Standard includes for the module, gives the types we have to instantiate
#include "main.h"
#pragma hdrstop

// Standard includes for doing explicit instantiation of collection classes:
#include "Vector_i.cpp"
#include "Set_i.cpp"
#include "HashMap_i.cpp"
#include "GpHashMap_i.cpp"
#include "ComHashMap_i.cpp"

template ComHashMapStrUni<ITsTextProps>; // StyleMap.

template ComVector<ITsString>; // VecTss; // Hungarian vtss
template ComVector<ITsTextProps>;
template ComVector<IVwPropertyStore>; // VwPropsVec;
template ComVector<IVwRootBox>;

template GpHashMap<ClsLevel, RecordSpec>;

template HashMap<long, int, HashObj, EqlObj>;
template HashMap<ClsLevel, Vector<HVO> *>;
template HashMap<GUID, FilterVc::HotLinkInfo>;
template HashMap<int, int>;
template HashMap<OLECHAR, OLECHAR>;
template HashMapStrUni<Vector<StrUni> >;	// AfApp - command line processing.
template HashMapStrUni<StrUni>;				// AfDbApp - command line processing
template HashMap<int,RecMainWnd::FieldData>;
// Needed for WpStylesheet, even though it is in WorldPad's explicit_instantiation.?.
template HashMap<HVO, StrUni>;

template Set<int>;
template Set<UserViewType>;
template Set<StrUni,HashStrUni,EqlStrUni>;

template Vector<ACCEL>;
template Vector<AfMenuMgrUtils::AccelKeyInfo>;
template Vector<achar>;
template Vector<AfClientWndPtr>;
template Vector<AfDbInfoPtr>;
//template Vector<AfDeFieldEditor *>; // Needed by AfDeSplitChild, but can't be here or WorldPad fails.
template Vector<AfDialogViewPtr>;
template Vector<AfMainWndPtr>;
template Vector<RecMainWndPtr>;
template Vector<AfListBarPtr>;
template Vector<AfLpInfoPtr>;
template Vector<AfMdiClientWnd *>;
template Vector<AfMenuMgr::ExpandedMenu>;
template Vector<AfMenuMgrUtils::AccelTableInfo>;
template Vector<AfMenuMgrUtils::CidToImag>;
template Vector<AfToolBarPtr>;
template Vector<AfViewBarShell *>;
template Vector<AppFilterInfo>;
template Vector<AppOverlayInfo>;
template Vector<AppSortInfo>;
template Vector<BlockSpec>; // BlockVec;
template Vector<BlockSpec *>;
template Vector<BlockSpecPtr>;
template Vector<bool>;
template Vector<byte>;
template Vector<Vector<byte> >;		//  Hungarian: vvb
template Vector<BYTE>;
template Vector<ChrpInheritance>;
template Vector<CmdExec::CmdHndEntry>;
template Vector<CmdPtr>;
template Vector<ColWidthInfo>; // ColWidthVec; // Hungarian: vcwi
template Vector<DWORD>;
template Vector<WsStyleInfo>; // WsStyleVec; // Hungarian vesi
template Vector<FilterMenuNodePtr>;
template Vector<FilterMenuNodeVec>;
template Vector<FilterPatternInfo>;
template Vector<FldSpec>;
template Vector<FldSpecPtr>;
template Vector<FmtFntDlg::FeatList>;
template Vector<FwFilterDlg::FilterInfo>;
template Vector<HvoClsid>;
template Vector<HMENU>;
template Vector<HTREEITEM>;
template Vector<HWND>;
template Vector<int>;
template Vector<TlsOptDlgOvr::OverlayTagInfo>;
template Vector<PossItemInfo>;
template Vector<PossListInfoPtr>;
template Vector<PossNameType>;
template Vector<Rect>;
template Vector<HVO>;
template Vector<Set<HVO> >;
template Vector<Set<int> >;
template Vector<SmartBstr>;
template Vector<SortKeyHvos>;
template Vector<SortMenuNodePtr>;
template Vector<SortMenuNodeVec>;
template Vector<StrApp>;
template Vector<StrUni>; // VecStrUni; // Hungarian vstu
template Vector<StyleInfo>;
template Vector<SysLangInfo>;
template Vector<TBBUTTON>;
template Vector<TlsObject>;
template Vector<TlsOptDlgOvr::OverlayInfo>;
template Vector<TlsView>;
template Vector<UserViewSpecPtr>; // UserViewSpecVec; // Hungarian vuvs
template Vector<unsigned char>;
template Vector<unsigned int>;
template Vector<ushort>;
template Vector<VwLength>;
template Vector<VwSelLevInfo>; // VecSelInfo;
template Vector<wchar>;
template Vector<SyncInfo>;
template Vector<AfViewBarBase *>;
