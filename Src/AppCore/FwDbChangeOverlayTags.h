/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: File: FwDbChangeOverlayTags.h
Responsibility: Rand Burgett
Last reviewed:

Description:
	These classes provide String Crawlers for changing overlay tag GUIDs in the database.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FWDBCHANGEOVERLAYTAGS_H
#define FWDBCHANGEOVERLAYTAGS_H 1


/*----------------------------------------------------------------------------------------------
	Fix all the string formats for merging two possibility items.
----------------------------------------------------------------------------------------------*/
class FwDbChangeOverlayTags : public DbStringCrawler
{
	typedef DbStringCrawler SuperClass;
public:
	FwDbChangeOverlayTags(HashMap<GUID,GUID> & hmguidTagguidPss)
		: DbStringCrawler(false, true, false, false), m_hmguidTagguidPss(hmguidTagguidPss)
	{
		m_fDontCloseMainWnd = true;		// No main window to close yet!
	}
	virtual bool ProcessFormatting(ComVector<ITsTextProps> & vqttp);
	virtual bool ProcessBytes(Vector<byte> & vbFmt);

protected:
	HashMap<GUID,GUID> & m_hmguidTagguidPss;
};


/*----------------------------------------------------------------------------------------------
	Fix all the string formats for deleting a possibility item.
----------------------------------------------------------------------------------------------*/
class FwDbDeleteOverlayTags : public DbStringCrawler
{
	typedef DbStringCrawler SuperClass;
public:
	FwDbDeleteOverlayTags(GUID * pguidTag)
		: DbStringCrawler(false, true, false, false), m_pguidTag(pguidTag)
	{
		m_fDontCloseMainWnd = true;		// No main window to close yet!
	}
	virtual bool ProcessFormatting(ComVector<ITsTextProps> & vqttp);
	virtual bool ProcessBytes(Vector<byte> & vbFmt);

protected:
	GUID * m_pguidTag;
};

#endif /*FWDBCHANGEOVERLAYTAGS_H*/
