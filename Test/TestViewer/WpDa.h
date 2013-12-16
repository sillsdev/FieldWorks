/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: WpDa.h
Responsibility: Sharon Correll (first draft by John Thomson)
Last reviewed: never

Description:
	This class provides an implementation of ISilDataAccess for WorldPad, where the underlying
	data is a simple XML representation of a document consisting of a sequence of styled
	paragraphs.
	The object representing the whole database is always an StText with HVO 1.

	This first draft just makes a new (but not empty) dummy document.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef WPDA_INCLUDED
#define WPDA_INCLUDED 1

class WpDa;
class WpChildWnd;
typedef GenSmartPtr<WpDa> WpDaPtr;

/*----------------------------------------------------------------------------------------------
	The main customizeable document view constructor class.
	Hungarian: rdw.
----------------------------------------------------------------------------------------------*/
class WpDa : public VwCacheDa
{
public:
	typedef VwCacheDa SuperClass;

	WpDa();
	~WpDa();
	void InitNew(StrAnsi staFileName);
	void InitNewEmpty();
	void LoadIntoEmpty(StrAnsi staFileName, WpChildWnd * pwcw);
	bool IsEmpty();
	void ReadTextFromFile(StrAnsi staFileName, Vector<StrUni> & vstu);
	bool SaveToFile(StrAnsi staFileName);
	// Todo JohnT: methods to read and write files (text, xml, ...)

protected:
};

#endif // WPDA_INCLUDED
