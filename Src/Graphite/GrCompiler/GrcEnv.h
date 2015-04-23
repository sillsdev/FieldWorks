/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcEnv.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GRC_ENV_INCLUDED
#define GRC_ENV_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GrcEnv
Description: The environment contains a list of directives for processing the input.
Hungarian: env
----------------------------------------------------------------------------------------------*/
class GrcEnv
{
public:
	//	Constructor:
	GrcEnv()
	{
		m_psymTable = NULL;
		m_nPass = 0;
		m_mUnits = 1000;
		m_nPointRadius = 2;
		m_mPrUnits = 1000;
		m_nMaxRuleLoop = 5;
		m_nMaxBackup = 0;
		m_fAttrOverride = true;
		m_wCodePage = 1252;
	}

	//	Copy constructor:
	GrcEnv(const GrcEnv & env)
	{
		m_psymTable = env.m_psymTable;
		m_nPass = env.m_nPass;
		m_mUnits = env.m_mUnits;
		m_nPointRadius = env.m_nPointRadius;
		m_mPrUnits = env.m_mPrUnits;
		m_nMaxRuleLoop = env.m_nMaxRuleLoop;
		m_nMaxBackup = env.m_nMaxBackup;
		m_fAttrOverride = env.m_fAttrOverride;
		m_wCodePage = env.m_wCodePage;
	}

	//	Getters:
	Symbol Table()				{ return m_psymTable; }
	int Pass()					{ return m_nPass; }
	int MUnits()				{ return m_mUnits; }
	int PointRadius()			{ return m_nPointRadius; }
	int PointRadiusUnits()		{ return m_mPrUnits; }
	int MaxRuleLoop()			{ return m_nMaxRuleLoop; }
	int MaxBackup()				{ return m_nMaxBackup; }
	bool AttrOverride()			{ return m_fAttrOverride; }
	utf16 CodePage()			{ return m_wCodePage; }

	//	Setters:
	void SetTable(Symbol psym)			{ m_psymTable = psym; }
	void SetPass(int n)					{ m_nPass = n; }
	void SetMUnits(int m)				{ m_mUnits = m; }
	void SetPointRadius(int n, int m)	{ m_nPointRadius = n; m_mPrUnits = m; }
	void SetMaxRuleLoop(int n)			{ m_nMaxRuleLoop = n; }
	void SetMaxBackup(int n)			{ m_nMaxBackup = n; }
	void SetAttrOverride(bool f)		{ m_fAttrOverride = f; }
	void SetCodePage(utf16 w)			{ m_wCodePage = w; }

protected:
	Symbol m_psymTable;
	int m_nPass;
	int m_mUnits;
	int m_nPointRadius;
	int m_mPrUnits;	// units for m_nPointRadius
	int m_nMaxRuleLoop;
	int m_nMaxBackup;
	bool m_fAttrOverride;
	utf16 m_wCodePage;

};


#endif // GRC_ENV_INCLUDED
