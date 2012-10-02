/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CleCustomExport.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the File / Export Dialog customization class.

		CleCustomExport : AfCustomExport
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CLECUSTOMEXPORT_H_INCLUDED
#define CLECUSTOMEXPORT_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides custom methods used in exporting from AfApp based applications.  It
	should be subclassed by specific applications as needed.

	Hungarian: clcex
----------------------------------------------------------------------------------------------*/
class CleCustomExport : public AfCustomExport
{
public:
	// The default constructor is used only for testing GetEnumString.  All other methods
	// crash and burn without the AfLpInfo or the AfMainWnd objects!
	CleCustomExport()
	{
		m_cref = 1;
	}
	CleCustomExport(AfLpInfo * plpi, AfMainWnd * pafw);
	~CleCustomExport();

	// IFwCustomExport methods implemented specifically for the List Editor.
	STDMETHOD(GetEnumString)(int flid, int itss, BSTR * pbstrName);
	STDMETHOD(GetActualLevel)(int nLevel, int hvoRec, int ws, int * pnActualLevel);

protected:
	// Nothing needed yet beyond what the base class already has.
};
typedef GenSmartPtr<CleCustomExport> CleCustomExportPtr;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkcle.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif  // !CLECUSTOMEXPORT_H_INCLUDED
