/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RnCustomExport.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the File / Export Dialog customization class.

		RnCustomExport : AfCustomExport
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RNCUSTOMEXPORT_H_INCLUDED
#define RNCUSTOMEXPORT_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides custom methods used in exporting from Data Notebook.

	Hungarian: rcex
----------------------------------------------------------------------------------------------*/
class RnCustomExport : public AfCustomExport
{
public:
	RnCustomExport(AfLpInfo * plpi, AfMainWnd * pafw);
	~RnCustomExport();

	// IFwCustomExport methods implemented specifically for Data Notebook.
	STDMETHOD(BuildSubItemsString)(IFwFldSpec * pffsp, int hvoRec, int ws, ITsString ** pptss);
	STDMETHOD(BuildObjRefSeqString)(IFwFldSpec * pffsp, int hvoRec, int ws, ITsString ** pptss);
	STDMETHOD(BuildObjRefAtomicString)(IFwFldSpec * pffsp, int hvoRec, int ws,
		ITsString ** pptss);
	STDMETHOD(BuildExpandableString)(IFwFldSpec * pffsp, int hvoRec, int ws,
		ITsString ** pptss);
	STDMETHOD(GetEnumString)(int flid, int itss, BSTR * pbstrName);
	STDMETHOD(BuildRecordTags)(int nLevel, int hvo, int clid, BSTR * pbstrStartTag,
		BSTR * pbstrEndTag);

protected:
	// Nothing beyond the base class so far.
};
typedef GenSmartPtr<RnCustomExport> RnCustomExportPtr;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mknb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif  // !RNCUSTOMEXPORT_H_INCLUDED
