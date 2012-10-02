/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: PreCompiler.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to implement the pre-compiler, which does error checking and adjustments.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Classes and Glyphs
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Do the pre-compilation tasks for the classes and glyphs. Return false if
	compilation cannot continue due to an unrecoverable error.
----------------------------------------------------------------------------------------------*/
bool GrcManager::PreCompileClassesAndGlyphs(GrcFont * pfont)
{
//	MarkUnusedGlyphMetrics();

	if (!GeneratePseudoGlyphs(pfont))
		return false;

	if (!m_prndr->AssignGlyphIDs(pfont, m_wGlyphIDLim, m_hmActualForPseudo))
		return false;

	//	Do this after assigning glyph IDs, since above routine is not smart enough
	//	to handle the fact that we are putting psuedos in the ANY class by means of glyphid().
	if (!AddAllGlyphsToTheAnyClass(pfont, m_hmActualForPseudo))
		return false;

	//	Do this before assigning internal glyph attr IDs, because we only want to assign
	//	IDs for justify levels that are being used.
	if (!MaxJustificationLevel(&m_nMaxJLevel))
		return false;

	if (!AssignInternalGlyphAttrIDs())
		return false;

//	SetGlyphMetricsFromFont(pfont);

	if (!AssignGlyphAttrsToClassMembers(pfont))
		return false;

	if (!ProcessGlyphAttributes(pfont))
		return false;

	if (!m_prndr->FixGlyphAttrsInRules(this, pfont))
		return false;

	//	Delay assigning internal IDs to classes until we have checked the validity of the
	//	table and pass structure.

	if (!FinalGlyphAttrResolution(pfont))
		return false;

	if (!StorePseudoToActualAsGlyphAttr())
		return false;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the generation of pseudo glyphs. Return false if compilation cannot continue
	due to an unrecoverable error.
----------------------------------------------------------------------------------------------*/
bool GrcManager::GeneratePseudoGlyphs(GrcFont * pfont)
{
	Set<GdlGlyphDefn*> setpglfExplicitPseudos;
	int cExplicitPseudos = m_prndr->ExplicitPseudos(setpglfExplicitPseudos);

	Vector<unsigned int> vnAutoUnicode;
	Vector<utf16> vwAutoGlyphID;
	int cAutoPseudos = (m_prndr->AutoPseudo()) ?
		pfont->AutoPseudos(vnAutoUnicode, vwAutoGlyphID) :
		0;

	utf16 wFirstFree = pfont->FirstFreeGlyph();
	m_wGlyphIDLim = wFirstFree;
	int cwFree = kMaxGlyphsPerFont - wFirstFree;

	if (cwFree < 2)
	{
		char rgch[20];
		itoa(kMaxGlyphsPerFont - 3, rgch, 10);
		g_errorList.AddError(4101, NULL,
			"Font exceeds maximum of ", rgch, " used glyphs",
			GrpLineAndFile(0, 0, ""));
		return false;	// terminate compilation
	}

	if (cExplicitPseudos + cAutoPseudos + 2 > cwFree)	// + 2 for line-break pseudo & non-existent pseudo
	{
		g_errorList.AddError(4102, NULL,
			"Insufficient free glyphs in font to assign pseudo glyphs.",
			GrpLineAndFile(0, 0, ""));
		return true;	// continue compilation
	}

	//	Define the line-break character, and make a glyph class to hold it.
	m_wLineBreak = wFirstFree++;
	////m_wLineBreak = 127;		// for testing
	GdlGlyphClassDefn * pglfcLb = AddAnonymousClass(GrpLineAndFile(0, 0, ""));
	GdlGlyphClassMember * pglfd =
		pglfcLb->AddGlyphToClass(GrpLineAndFile(), kglftGlyphID, (int)m_wLineBreak);
	GdlGlyphDefn * pglf = dynamic_cast<GdlGlyphDefn *>(pglfd);
	Assert(pglf);
	pglf->SetNoRangeCheck();
	Symbol psymLb = m_psymtbl->FindSymbol("#");
	psymLb->SetData(pglfcLb);

	//	Handle explicit pseudos.
	utf16 wFirstPseudo = wFirstFree;
	m_nMaxPseudoUnicode = 0;
	Set<unsigned int> setnUnicode; // to recognize duplicates
	for (Set<GdlGlyphDefn*>::iterator itset = setpglfExplicitPseudos.Begin();
		itset != setpglfExplicitPseudos.End();
		++itset)
	{
		GdlGlyphDefn * pglfPseudo = *itset;
		pglfPseudo->SetAssignedPseudo(wFirstFree++);

		unsigned int nUnicode = pglfPseudo->UnicodeInput();
		if (nUnicode == 0)
			;	// no Unicode input specified
		else if (setnUnicode.IsMember(nUnicode))
		{
			//	Duplicate pseudo mapping.
			g_errorList.AddError(4103, pglfPseudo,
				StrAnsi("Duplicate Unicode input -> pseudo assignment."));
		}
		else
		{
			m_vnUnicodeForPseudo.Push(nUnicode);
			m_vwPseudoForUnicode.Push(pglfPseudo->AssignedPseudo());
			setnUnicode.Insert(nUnicode);
			m_nMaxPseudoUnicode = max(m_nMaxPseudoUnicode, nUnicode);
		}
	}
	//	Handle auto-pseudos.
	Assert(vnAutoUnicode.Size() == vwAutoGlyphID.Size());
	m_wFirstAutoPseudo = wFirstFree;
	for (int iw = 0; iw < vnAutoUnicode.Size(); iw++)
	{
		utf16 wAssigned = wFirstFree++;
		CreateAutoPseudoGlyphDefn(wAssigned, vnAutoUnicode[iw], vwAutoGlyphID[iw]);
	}

	if (wFirstFree - wFirstPseudo >= kMaxPseudos)
	{
		char rgch1[20];
		char rgch2[20];
		itoa(wFirstFree - wFirstPseudo, rgch1, 10);
		itoa(kMaxPseudos - 1, rgch2, 10);
		g_errorList.AddError(4104, NULL,
			"Number of pseudo-glyphs (",
			rgch1,
			") exceeds maximum of ",
			rgch2);
	}
	else
	{
		SortPseudoMappings();
	}

	m_wPhantom = wFirstFree++;	// phantom glyph before the beginning of the input

	m_cwGlyphIDs = wFirstFree;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Fill in the set with the explicit pseudo-glyphs in the class database. Return the
	number found. Record an error if the pseudo has an invalid output function (ie, more
	than one glyph specified).
----------------------------------------------------------------------------------------------*/
int GdlRenderer::ExplicitPseudos(Set<GdlGlyphDefn *> & setpglf)
{
	for (int iglfc = 0; iglfc < m_vpglfc.Size(); iglfc++)
		m_vpglfc[iglfc]->ExplicitPseudos(setpglf);
	return setpglf.Size();
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::ExplicitPseudos(Set<GdlGlyphDefn *> & setpglf)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
		m_vpglfdMembers[iglfd]->ExplicitPseudos(setpglf);
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::ExplicitPseudos(Set<GdlGlyphDefn *> & setpglf)
{
	if (m_glft == kglftPseudo)
	{
		Assert(m_pglfOutput);
		GdlGlyphDefn * p = this;	// kludge until Set can handle const args.
		setpglf.Insert(p);
	}
}

/*----------------------------------------------------------------------------------------------
	Create a glyph definition to hold an auto-pseudo glyph. We create a bogus glyph class to
	put it in, just for our convenience. It's just as if they had typed:

		bogus = pseudo(glyphid(<wGlyphID>), <nUnicode>)
----------------------------------------------------------------------------------------------*/
void GrcManager::CreateAutoPseudoGlyphDefn(utf16 wAssigned, int nUnicode, utf16 wGlyphID)
{
	GdlGlyphDefn * pglfOutput = new GdlGlyphDefn(kglftGlyphID, wGlyphID);
	GdlGlyphDefn * pglf = new GdlGlyphDefn(kglftPseudo, pglfOutput, nUnicode);
	pglf->SetAssignedPseudo(wAssigned);

	GdlGlyphClassDefn * pglfc = new GdlGlyphClassDefn();
	pglfc->AddMember(pglf);
	m_prndr->AddGlyphClass(pglfc);

	m_vnUnicodeForPseudo.Push(nUnicode);
	m_vwPseudoForUnicode.Push(wAssigned);
}

/*----------------------------------------------------------------------------------------------
	Return the pseudo-glyph assigned to the given Unicode value, or 0 if none.
----------------------------------------------------------------------------------------------*/
int GrcManager::PseudoForUnicode(int nUnicode)
{
	for (int iw = 0; iw < m_vnUnicodeForPseudo.Size(); iw++)
	{
		if (m_vnUnicodeForPseudo[iw] == nUnicode)
			return m_vwPseudoForUnicode[iw];
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Return the actual glyph ID for the given pseudo-glyph, or 0 if none.
----------------------------------------------------------------------------------------------*/
int GrcManager::ActualForPseudo(utf16 wPseudo)
{
	utf16 wActual = 0;
	if (m_hmActualForPseudo.Retrieve(wPseudo, &wActual))
		return wActual;
	else
		return 0;

//	return m_prndr->ActualForPseudo(wPseudo);
}

/*--------------------------------------------------------------------------------------------*/
int GdlRenderer::ActualForPseudo(utf16 wPseudo)
{
	for (int ipglfc = 0; ipglfc < m_vpglfc.Size(); ipglfc++)
	{
		utf16 wActual = m_vpglfc[ipglfc]->ActualForPseudo(wPseudo);
		if (wActual != 0)
			return wActual;
	}
	return 0;
}

/*--------------------------------------------------------------------------------------------*/
int GdlGlyphClassDefn::ActualForPseudo(utf16 wPseudo)
{
	for (int ipglfd = 0; ipglfd < m_vpglfdMembers.Size(); ipglfd++)
	{
		utf16 wActual = m_vpglfdMembers[ipglfd]->ActualForPseudo(wPseudo);
		if (wActual != 0)
			return wActual;
	}
	return 0;
}

/*--------------------------------------------------------------------------------------------*/
int GdlGlyphDefn::ActualForPseudo(utf16 wPseudo)
{
	if (m_glft == kglftPseudo && m_wPseudo == wPseudo && m_pglfOutput)
	{
		utf16 wOutput = m_pglfOutput->m_vwGlyphIDs[0];
		return wOutput;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Sort the unicode-to-pseudo mappings in order of the unicode values.
----------------------------------------------------------------------------------------------*/
void GrcManager::SortPseudoMappings()
{
	Assert(m_vnUnicodeForPseudo.Size() == m_vwPseudoForUnicode.Size());

	for (int i1 = 0; i1 < m_vnUnicodeForPseudo.Size() - 1; i1++)
	{
		unsigned int nTmp = m_vnUnicodeForPseudo[i1];

		for (int i2 = i1 + 1; i2 < m_vnUnicodeForPseudo.Size(); i2++)
		{
			if (m_vnUnicodeForPseudo[i2] < nTmp)
			{
				//	Swap
				m_vnUnicodeForPseudo[i1] = m_vnUnicodeForPseudo[i2];
				m_vnUnicodeForPseudo[i2] = nTmp;
				nTmp = m_vnUnicodeForPseudo[i1];

				utf16 wTmp = m_vwPseudoForUnicode[i1];
				m_vwPseudoForUnicode[i1] = m_vwPseudoForUnicode[i2];
				m_vwPseudoForUnicode[i2] = wTmp;
			}
		}
	}
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Define the ANY class to include all glyphs. This must be done after we've set up
	the pseudo-glyphs and defined the phantom glyph, because they must be included too.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AddAllGlyphsToTheAnyClass(GrcFont * pfont,
	HashMap<utf16, utf16> & hmActualForPseudo)
{
	Symbol psym = m_psymtbl->FindSymbol("ANY");
	GdlGlyphClassDefn * pglfcAny = psym->GlyphClassDefnData();
	Assert(pglfcAny);

	GdlGlyphDefn * pglf = new GdlGlyphDefn(kglftGlyphID, (utf16)0, m_cwGlyphIDs - 1);
	pglfcAny->AddMember(pglf);

	pglfcAny->AssignGlyphIDs(pfont, m_cwGlyphIDs, hmActualForPseudo);

	return true;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Determine the glyph ID equivalents for each glyph definition; ie, convert Unicode,
	codepoints, postscript to glyph ID.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::AssignGlyphIDs(GrcFont * pfont, utf16 wGlyphIDLim,
	HashMap<utf16, utf16> & hmActualForPseudo)
{
	for (int iglfc = 0; iglfc < m_vpglfc.Size(); iglfc++)
		m_vpglfc[iglfc]->AssignGlyphIDs(pfont, wGlyphIDLim, hmActualForPseudo);

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AssignGlyphIDs(GrcFont * pfont, utf16 wGlyphIDLim,
	HashMap<utf16, utf16> & hmActualForPseudo)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->AssignGlyphIDsToClassMember(pfont, wGlyphIDLim,
			hmActualForPseudo);
	}
}

/*----------------------------------------------------------------------------------------------
	Determine the glyph ID equivalents for the recipient by virtue of its being a member
	of a class. Only do this for simple glyphs; classes are handled separately.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AssignGlyphIDsToClassMember(GrcFont * pfont, utf16 wGlyphIDLim,
	HashMap<utf16, utf16> & hmActualForPseudo, bool fLookUpPseudos)
{
	//	Do nothing; this class will be handled separately at the top level.
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::AssignGlyphIDsToClassMember(GrcFont * pfont, utf16 wGlyphIDLim,
	HashMap<utf16, utf16> & hmActualForPseudo, bool fLookUpPseudos)
{
	Assert(m_vwGlyphIDs.Size() == 0);

	utf16 w;
	unsigned int n;
	utf16 wGlyphID;
	unsigned int nUnicode;
	utf16 wFirst, wLast;

	bool fIgnoreBad = g_cman.IgnoreBadGlyphs();

	switch (m_glft)
	{
	case kglftGlyphID:
		if (m_nFirst > m_nLast)
			g_errorList.AddError(4105,this,
				"Invalid glyph ID range");

		wFirst = (utf16)m_nFirst;
		wLast = (utf16)m_nLast;
		for (w = wFirst; w <= wLast; ++w)
		{
			if (!m_fNoRangeCheck && w >= wGlyphIDLim)
				g_errorList.AddError(4106, this,
					"Glyph ID out of range: ",
					GlyphIDString(w));
			else
				m_vwGlyphIDs.Push(w);

			// Since incrementing 0xFFFF will produce zero:
			if (w == 0xFFFF)
				break;
		}
		break;

	case kglftUnicode:
		if (m_nFirst > m_nLast)
			g_errorList.AddError(4107, this,
				"Invalid Unicode range");

		for (n = m_nFirst; n <= m_nLast; ++n)
		{
			if (n == 0x0000FFFE || n == 0x0000FFFF)
			{
				g_errorList.AddError(4108, this, "U+",
					CodepointIDString(n),
					" is not a valid Unicode codepoint");
				wGlyphID = 0;
			}
			else
			{
				if (!fLookUpPseudos || (wGlyphID = g_cman.PseudoForUnicode(n)) == 0)
					wGlyphID = pfont->GlyphFromCmap(n, this);
				if (wGlyphID == 0)
				{
					if (fIgnoreBad)
					{
						g_errorList.AddWarning(4501, this,
							"Unicode character not present in cmap: U+",
							CodepointIDString(n), "; definition will be ignored");
						m_vwGlyphIDs.Push(kBadGlyph);
					}
					else
						g_errorList.AddError(4109, this,
							"Unicode character not present in cmap: U+",
							CodepointIDString(n));
				}
				else
					m_vwGlyphIDs.Push(wGlyphID);
			}

			// Since incrementing 0xFFFFFFFF will produce zero:
			if (n == 0xFFFFFFFF)
				break;
		}
		break;

	case kglftPostscript:
		wGlyphID = pfont->GlyphFromPostscript(m_sta, this, !fIgnoreBad);
		if (wGlyphID == 0)
		{
			if (fIgnoreBad)
			{
				g_errorList.AddWarning(4502, this,
					"Invalid postscript name: ",
					m_sta, "; definition will be ignored");
				m_vwGlyphIDs.Push(kBadGlyph);
			}
			else
				g_errorList.AddError(4110, this,
					"Invalid postscript name: ",
					m_sta);
		}
		else
			m_vwGlyphIDs.Push(wGlyphID);
		break;

	case kglftCodepoint:
		char rgchCdPg[20];
		itoa(m_wCodePage, rgchCdPg, 10);
		if (m_nFirst == 0 && m_nLast == 0)
		{
			for (int ich = 0; ich < m_sta.Length(); ich++)
			{
				char rgchCdPt[2] = {0,0};
				rgchCdPt[0] = m_sta.GetAt(ich);
				nUnicode = pfont->UnicodeFromCodePage(m_wCodePage, m_sta[ich], this);
				if (nUnicode == 0)
					g_errorList.AddError(4111, this,
						"Codepoint '",
						rgchCdPt,
						"' not valid for codepage ",
						rgchCdPg);
				else
				{
					if (!fLookUpPseudos || (wGlyphID = g_cman.PseudoForUnicode(nUnicode)) == 0)
						wGlyphID = pfont->GlyphFromCmap(nUnicode, this);
					if (wGlyphID == 0)
					{
						if (fIgnoreBad)
						{
							g_errorList.AddWarning(4503, this,
								"Unicode character U+",
								GlyphIDString(nUnicode),
								" (ie, codepoint '",
								rgchCdPt,
								"' in codepage ",
								rgchCdPg,
								") not present in cmap; definition will be ignored");
							m_vwGlyphIDs.Push(kBadGlyph);
						}
						else
							g_errorList.AddError(4112, this,
								"Unicode character U+",
								GlyphIDString(nUnicode),
								" (ie, codepoint '",
								rgchCdPt,
								"' in codepage ",
								rgchCdPg,
								") not present in cmap");
					}
					else
						m_vwGlyphIDs.Push(wGlyphID);
				}
			}
		}
		else
		{
			if (m_nFirst > m_nLast)
				g_errorList.AddError(4113, this,
					StrAnsi("Invalid codepoint range"));

			utf16 wFirst = (utf16)m_nFirst;
			utf16 wLast = (utf16)m_nLast;

			for (w = wFirst; w <= wLast; w++)
			{
				nUnicode = pfont->UnicodeFromCodePage(m_wCodePage, w, this);
				if (nUnicode == 0)
					g_errorList.AddError(4114, this,
						"Codepoint 0x",
						GlyphIDString(w),
						" not valid for codepage ",
						rgchCdPg);
				else
				{
					wGlyphID = pfont->GlyphFromCmap(nUnicode, this);
					if (wGlyphID == 0)
					{
						if (fIgnoreBad)
						{
							g_errorList.AddWarning(4504, this,
								"Unicode character U+",
								GlyphIDString(nUnicode),
								" (ie, codepoint 0x",
								GlyphIDString(w),
								" in codepage ",
								rgchCdPg,
								") not present in cmap; definition will be ignored");
							m_vwGlyphIDs.Push(kBadGlyph);
						}
						else
							g_errorList.AddError(4115, this,
								"Unicode character U+",
								GlyphIDString(nUnicode),
								" (ie, codepoint 0x",
								GlyphIDString(w),
								" in codepage ",
								rgchCdPg,
								") not present in cmap");
					}
					else
						m_vwGlyphIDs.Push(wGlyphID);
				}
			}

			// Since incrementing 0xFFFF will produce zero:
			if (w == 0xFFFF)
				break;
		}
		break;

	case kglftPseudo:
		Assert(m_nFirst == 0);
		Assert(m_nLast == 0);
		Assert(m_pglfOutput);
		//	While we're at it, determine the output glyph ID. Record an error if there
		//	is more than one, or none, or the glyph ID == 0.
		m_pglfOutput->AssignGlyphIDsToClassMember(pfont, wGlyphIDLim, hmActualForPseudo, false);
		if (m_pglfOutput->m_vwGlyphIDs.Size() > 1)
		{
			if (fIgnoreBad)
			{
				g_errorList.AddWarning(4505, this,
					"Pseudo-glyph -> glyph ID mapping results in more than one glyph; definition will be ignored");
				m_vwGlyphIDs.Push(kBadGlyph);
			}
			else
				g_errorList.AddError(4116, this,
					"Pseudo-glyph -> glyph ID mapping results in more than one glyph");
		}
		else if (m_pglfOutput->m_vwGlyphIDs.Size() == 0)
		{
			if (fIgnoreBad)
			{
				g_errorList.AddWarning(4506, this,
					"Pseudo-glyph -> glyph ID mapping results in no valid glyph; definition will be ignored");
				m_vwGlyphIDs.Push(kBadGlyph);
			}
			else
				g_errorList.AddError(4117, this,
					"Pseudo-glyph -> glyph ID mapping results in no valid glyph");
		}
		else if (m_pglfOutput->m_vwGlyphIDs[0] == 0)
		{
			if (fIgnoreBad)
			{
				g_errorList.AddWarning(4507, this,
					"Pseudo-glyph cannot be mapped to glyph ID 0; definition will be ignored");
				m_vwGlyphIDs.Push(kBadGlyph);
			}
			else
				g_errorList.AddError(4118, this,
					"Pseudo-glyph cannot be mapped to glyph ID 0");
		}
		else
		{
			//	It is the assigned pseudo glyph ID which is the 'contents' of this glyph defn.
			m_vwGlyphIDs.Push(m_wPseudo);

			//	Store the pseudo-to-actual assignment in the map.
			hmActualForPseudo.Insert(m_wPseudo, m_pglfOutput->m_vwGlyphIDs[0], true);
		}
		break;

	default:
		Assert(false);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the number of glyph IDs per class.
----------------------------------------------------------------------------------------------*/
int GdlGlyphClassDefn::GlyphIDCount()
{
	int c = 0;
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
		 c += m_vpglfdMembers[iglfd]->GlyphIDCount();
	return c;
}

/*--------------------------------------------------------------------------------------------*/
int GdlGlyphDefn::GlyphIDCount()
{
	int cGlyph = 0;
	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		if (m_vwGlyphIDs[iw] != kBadGlyph)
			cGlyph++;
	}
	return cGlyph;
}

/**********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Calculate the highest justification level used. If justification is not referenced at all,
	the result = -2; -1 means only non-leveled attributes are used (justify.stretch, etc).
----------------------------------------------------------------------------------------------*/
bool GrcManager::MaxJustificationLevel(int * pnJLevel)
{
	*pnJLevel = -2; // no reference to justification

	m_prndr->MaxJustificationLevel(&m_nMaxJLevel);
	m_fBasicJust = (m_nMaxJLevel == -2);
	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::MaxJustificationLevel(int * pnJLevel)
{
	//	Glyph atrributes:
	for (int ipglfc = 0; ipglfc < m_vpglfc.Size(); ipglfc++)
	{
		m_vpglfc[ipglfc]->MaxJustificationLevel(pnJLevel);
	}
	//	Rules:
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->MaxJustificationLevel(pnJLevel);
		if (*pnJLevel >= 3)
			return;
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::MaxJustificationLevel(int * pnJLevel)
{
	//	For each attribute assignment in the value list:
	for (int ipglfa = 0; ipglfa < m_vpglfaAttrs.Size(); ipglfa++)
	{
		Symbol psym = m_vpglfaAttrs[ipglfa]->GlyphSymbol();
		int n = psym->JustificationLevel();
		if (n > 3)
			g_errorList.AddError(4119, this,
				"Only 3 levels of justification are supported.");
		*pnJLevel = max(*pnJLevel, n);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::MaxJustificationLevel(int * pnJLevel)
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->MaxJustificationLevel(pnJLevel);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::MaxJustificationLevel(int * pnJLevel)
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->MaxJustificationLevel(pnJLevel);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::MaxJustificationLevel(int * pnJLevel)
{
	// Note: justify attributes are illegal in rule-level constraints.

	for (int iprit = 0; iprit < m_vprit.Size(); iprit++)
	{
		m_vprit[iprit]->MaxJustificationLevel(pnJLevel);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::MaxJustificationLevel(int * pnJLevel)
{
	if (m_pexpConstraint)
	{
		int n = -2;
		m_pexpConstraint->MaxJustificationLevel(&n);
		if (n > 3)
			g_errorList.AddError(4120, this,
				"Only 3 levels of justification are supported.");
		*pnJLevel = max(*pnJLevel, n);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::MaxJustificationLevel(int * pnJLevel)
{
	GdlRuleItem::MaxJustificationLevel(pnJLevel);

	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		int n = -2;
		m_vpavs[ipavs]->MaxJustificationLevel(&n);
		if (n > 3)
			g_errorList.AddError(4121, this,
				"Only 3 levels of justification are supported.");
		*pnJLevel = max(*pnJLevel, n);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::MaxJustificationLevel(int * pnJLevel)
{
	int n = m_psymName->JustificationLevel();
	if (n > 3)
		g_errorList.AddError(4122, this,
			"Only 3 levels of justification are supported.");
	*pnJLevel = max(*pnJLevel, n);
}

/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Assign an internal ID for each glyph attribute. Specifically, the ID is assigned to the
	generic form of the glyph attribute, and the class-specific versions make use of it.

	The IDs are assigned with the first batch belonging to the component bases; the next batch
	being the corresponding component fields in a specified order, the justification
	attributes, and finally and all other glyph	attributes following. So the list might look
	like this:

	0:	component.X
	1:	component.Y
	2:	component.Z
	3:	component.X.top
	4:	component.X.bottom
	5:	component.X.left
	6:	component.X.right
	7:	component.Y.top
	8:	component.Y.bottom
	9:	component.Y.left
	10:	component.Y.right
	11:	component.Z.top
	12:	component.Z.bottom
	13:	component.Z.left
	14:	component.Z.right
	15:	pointA.x
	16:	pointA.y
		etc.

	So given the total number of components, we can find the list of components,
	the corresponding component box fields, and the other glyph attributes.

	Review: should we include glyph metrics in this list too? (At least the used ones).
----------------------------------------------------------------------------------------------*/
bool GrcManager::AssignInternalGlyphAttrIDs()
{
	//	Assign the first batch of IDs to component bases (ie, component.X).
	m_psymtbl->AssignInternalGlyphAttrIDs(m_psymtbl, m_vpsymGlyphAttrs, 1, -1, -1);
	m_cpsymComponents = m_vpsymGlyphAttrs.Size();

	//	Assign the next batch to component box fields. (ie, component.X.top/bottom/left/right).
	m_psymtbl->AssignInternalGlyphAttrIDs(m_psymtbl, m_vpsymGlyphAttrs, 2, m_cpsymComponents, -1);

	//	Assign the next batch to the justification attributes.
	m_psymtbl->AssignInternalGlyphAttrIDs(m_psymtbl, m_vpsymGlyphAttrs, 3, -1, NumJustLevels());

	//	Finally, assign IDs to everything else.
	m_psymtbl->AssignInternalGlyphAttrIDs(m_psymtbl, m_vpsymGlyphAttrs, 4, -1, -1);

	if (m_vpsymGlyphAttrs.Size() >= kMaxGlyphAttrs)
	{
		char rgch1[20];
		char rgch2[20];
		itoa(m_vpsymGlyphAttrs.Size(), rgch1, 10);
		itoa(kMaxGlyphAttrs - 1, rgch2, 10);
		g_errorList.AddError(4123, NULL,
			"Number of glyph attributes (",
			rgch1,
			") exceeds maximum of ",
			rgch2);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Loop through the symbol table, assigning internal IDs to each glyph attribute.
	Arguments:
		psymtblMain			- main, top-level symbol table
		vpsymGlyphAttrIDs	- list of assigned symbols
		nPass				- 1: process component bases;
							  2: process component box fields;
							  3: process justification attributes
							  4: process everything else
		cComponents			- only used on pass 2
		cJLevels			- only used on pass 3
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTable::AssignInternalGlyphAttrIDs(GrcSymbolTable * psymtblMain,
	Vector<Symbol> & vpsymGlyphAttrIDs, int nPass, int cComponents, int cJLevels)
{
	if (nPass == 3)
	{
		//	Justification attributes must be put in a specific order, with the corresponding
		//	attributes for the various levels contiguous. Eg, if cJLevels = 2:
		//		justify.0.stretch
		//		justify.1.stretch
		//		justify.2.stretch
		//		justify.0.shrink
		//		justify.1.shrink
		//		justify.2.shrink
		//		justify.0.step
		//		etc.
		//	(Actually, there is a series of "high-word" stretch values that come immediately
		//	after stretch, which are used to hold bits 16-32 of any very large stretch values.)
		Vector<StrAnsi> vstaJAttr;
		vstaJAttr.Push("stretch");
		vstaJAttr.Push("stretchHW");	// high word, for large stretch values
		vstaJAttr.Push("shrink");
		vstaJAttr.Push("step");
		vstaJAttr.Push("weight");
		int istaJAttr;
		for (istaJAttr = 0; istaJAttr < vstaJAttr.Size(); istaJAttr++)
		{
			int nLevel;
			for (nLevel = 0; nLevel < cJLevels; nLevel++)
			{
				char rgchLev[20];
				itoa(nLevel, rgchLev, 10);
				//GrcStructName xnsJAttr("justify", rgchLev, vstaJAttr[istaJAttr]);
				GrcStructName xnsJAttr("justify", vstaJAttr[istaJAttr]);

				Symbol psymJAttr = FindSymbol(xnsJAttr);
				int id = AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymJAttr);
			}
		}
		//	Don't bother with the non-leveled attributes since they will be replaced by
		//	the leveled ones.
		return true;
	}

	for (SymbolTableMap::iterator it = m_hmstasymEntries.Begin();
		it != m_hmstasymEntries.End();
		++it)
	{
		Symbol psym = it.GetValue();

		if (psym->m_psymtblSubTable)
		{
			if (!psym->IsGeneric() && nPass == 1 && psym->IsComponentBase())
			{
				Symbol psymGeneric = psym->Generic();
				if (!psymGeneric)
				{
					//	Undefined glyph attribute--ignore.
				}
				else
				{
					AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymGeneric);
					psym->SetInternalID(psymGeneric->InternalID());
				}

				//	Don't process the component fields until we have processed all the
				//	component bases.
			}
			else
				psym->m_psymtblSubTable->AssignInternalGlyphAttrIDs(psymtblMain,
					vpsymGlyphAttrIDs, nPass, cComponents, cJLevels);
		}
		else if (!psym->IsGeneric() &&
			psym->FitsSymbolType(ksymtGlyphAttr))
			// || it->FitsSymbolType(ksymtGlyphMetric) && it->Used()
		{
			Symbol psymGeneric = psym->Generic();
			bool f = psym->FitsSymbolType(ksymtGlyphAttr);
			if (!psymGeneric)
				// Probably because this was a glyph metric--already gave an error.
				continue;
			Assert(psymGeneric);

			if (nPass == 2 && psym->IsComponentBoxField())
			{
				int ipsymOffset;
				StrAnsi sta = psym->LastField();
				if (sta == "top")
					ipsymOffset = 0;
				else if (sta == "bottom")
					ipsymOffset = 1;
				else if (sta == "left")
					ipsymOffset = 2;
				else if (sta == "right")
					ipsymOffset = 3;
				else
					Assert(false);

				Assert(ipsymOffset < kFieldsPerComponent);

				int nBaseID = psym->BaseLigComponent()->InternalID();
				int ipsym = cComponents + (nBaseID * kFieldsPerComponent) + ipsymOffset;

				int i = AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymGeneric, ipsym);
				Assert(i == ipsym);
				psymGeneric->SetInternalID(ipsym);
				psym->SetInternalID(psymGeneric->InternalID());
			}
			else if (nPass == 4 && !psym->IsComponentBoxField())
			{
				AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymGeneric);
				psym->SetInternalID(psymGeneric->InternalID());

				//	Is this an attribute that might need to be converted to gpoint?
				int iv = psym->FieldIndex(StrAnsi("gpath"));
				iv = (iv == -1) ? psym->FieldIndex("x") : iv;
				iv = (iv == -1) ? psym->FieldIndex("y") : iv;
				if (iv > -1)
				{
					//	We are going to convert all 'gpath' attributes to 'gpoint',
					//	so create that attribute too. And we might convert x/y coordinates
					//	to gpoint.
					//	(We have to do this before we create the matrix to hold all
					//	the glyph attribute values--in AssignGlyphAttrsToClassMembers--
					//	so that that routine will make room for it.)
					GrcStructName xns;
					psym->GetStructuredName(&xns);
					xns.DeleteField(iv);
					xns.InsertField(iv, StrAnsi("gpoint"));
					Symbol psymGPoint =
						psymtblMain->AddGlyphAttrSymbol(xns, psym->LineAndFile(), kexptNumber);

					Symbol psymGPointGeneric = psymGPoint->Generic();
					Assert(psymGPointGeneric);
					AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymGPointGeneric);
					psymGPoint->SetInternalID(psymGPointGeneric->InternalID());
				}
				iv = psym->FieldIndex(StrAnsi("gpoint"));
				if (iv > -1)
				{
					//	We might need to convert gpoint to x/y.
					GrcStructName xns;
					psym->GetStructuredName(&xns);
					xns.DeleteField(iv);
					xns.InsertField(iv, StrAnsi("x"));
					Symbol psymX =
						psymtblMain->AddGlyphAttrSymbol(xns, psym->LineAndFile(), kexptMeas);

					Symbol psymXGeneric = psymX->Generic();
					Assert(psymXGeneric);
					AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymXGeneric);
					psymX->SetInternalID(psymXGeneric->InternalID());

					xns.DeleteField(iv);
					xns.InsertField(iv, StrAnsi("y"));
					Symbol psymY =
						psymtblMain->AddGlyphAttrSymbol(xns, psym->LineAndFile(), kexptMeas);

					Symbol psymYGeneric = psymY->Generic();
					Assert(psymYGeneric);
					AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psymYGeneric);
					psymY->SetInternalID(psymYGeneric->InternalID());
				}
			}
		}
		else if (nPass == 4 && psym->FitsSymbolType(ksymtGlyphAttr)
			&& psym->FieldCount() == 1
			&& (psym->FieldIs(0, "directionality")
				|| psym->FieldIs(0, "breakweight")
				|| psym->FieldIs(0, "*actualForPseudo*")))
		{
			AddGlyphAttrSymbolInMap(vpsymGlyphAttrIDs, psym);
		}
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Add the generic symbol into the map that indicates internal glyph attribute IDs, if
	it is not already there. Return the internal ID.
----------------------------------------------------------------------------------------------*/
int GrcSymbolTable::AddGlyphAttrSymbolInMap(Vector<Symbol> & vpsymGlyphAttrIDs,
	Symbol psymGeneric)
{
	for (int ipsym = 0; ipsym < vpsymGlyphAttrIDs.Size(); ipsym++)
	{
		if (vpsymGlyphAttrIDs[ipsym] == psymGeneric)
			return ipsym;
	}

	psymGeneric->SetInternalID(vpsymGlyphAttrIDs.Size());
	vpsymGlyphAttrIDs.Push(psymGeneric);
	return psymGeneric->InternalID();
}


int GrcSymbolTable::AddGlyphAttrSymbolInMap(Vector<Symbol> & vpsymGlyphAttrIDs,
	Symbol psymGeneric, int ipsymToAssign)
{
	if (vpsymGlyphAttrIDs.Size() > ipsymToAssign)
	{
		Assert(vpsymGlyphAttrIDs[ipsymToAssign] == NULL ||
			vpsymGlyphAttrIDs[ipsymToAssign] == psymGeneric);
		psymGeneric->SetInternalID(ipsymToAssign);
		vpsymGlyphAttrIDs[ipsymToAssign] = psymGeneric;
		return ipsymToAssign;
	}
	else
	{
		//	Add blank slots.
		while (vpsymGlyphAttrIDs.Size() < ipsymToAssign)
			vpsymGlyphAttrIDs.Push(NULL);

		Assert(ipsymToAssign == vpsymGlyphAttrIDs.Size());
		psymGeneric->SetInternalID(ipsymToAssign);
		vpsymGlyphAttrIDs.Push(psymGeneric);
		return ipsymToAssign;
	}
}



/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Assign the glyph attributes to all the glyphs in all the classes.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AssignGlyphAttrsToClassMembers(GrcFont * pfont)
{
	Assert(m_pgax == NULL);

	int cGlyphAttrs = m_vpsymGlyphAttrs.Size();
	int cStdStyles = max(m_vpsymStyles.Size(), 1);
	Assert(cStdStyles == 1);	// for now
	m_pgax = new GrcGlyphAttrMatrix(m_cwGlyphIDs, cGlyphAttrs, cStdStyles);

	//	Array of pointers to ligature component maps, if any.
	m_plclist = new GrcLigComponentList(m_cwGlyphIDs);

	//	List of system-defined glyph attributes:
	//	directionality = 0
	Vector<Symbol> vpsymSysDefined;
	Vector<int> vnSysDefValues;
	vpsymSysDefined.Push(SymbolTable()->FindSymbol("directionality"));
	vnSysDefValues.Push(0);
	//	breakweight = letter
	vpsymSysDefined.Push(SymbolTable()->FindSymbol("breakweight"));
	vnSysDefValues.Push(klbLetterBreak);
	// justify.weight = 1
	if (NumJustLevels() > 0)
	{
		GrcStructName xnsJWeight("justify", "weight");
		vpsymSysDefined.Push(SymbolTable()->FindSymbol(xnsJWeight));
		vnSysDefValues.Push(1);
		// Other justify attrs have default of zero, and so do not need to be initialized.
	}

	m_prndr->AssignGlyphAttrDefaultValues(pfont, m_pgax, m_cwGlyphIDs,
		vpsymSysDefined, vnSysDefValues, m_vpexpModified,
		m_vpsymGlyphAttrs);

	m_prndr->AssignGlyphAttrsToClassMembers(m_pgax, m_plclist);

	if (m_cpsymComponents >= kMaxComponents)
	{
		char rgchMax[20];
		itoa(kMaxComponents - 1, rgchMax, 10);
		char rgchCount[20];
		itoa(m_cpsymComponents, rgchCount, 10);
		g_errorList.AddError(4124, NULL,
			"Total number of ligature components (",
			rgchCount,
			") exceeds maximum of ",
			rgchMax);
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
	GrcLigComponentList * plclist)
{
	for (int ipglfc = 0; ipglfc < m_vpglfc.Size(); ipglfc++)
	{
		m_vpglfc[ipglfc]->AssignGlyphAttrsToClassMembers(pgax, this, plclist);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
	GdlRenderer * prndr, GrcLigComponentList * plclist)
{
	AssignGlyphAttrsToClassMembers(pgax, prndr, plclist, m_vpglfaAttrs);
}

/*----------------------------------------------------------------------------------------------
	Assign the given glyph attributes to all the glyphs in the class.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
	GdlRenderer * prndr, GrcLigComponentList * plclist,
	Vector<GdlGlyphAttrSetting *> & vpglfaAttrs)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->AssignGlyphAttrsToClassMembers(pgax, prndr, plclist,
			vpglfaAttrs);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
	GdlRenderer * prndr, GrcLigComponentList * plclist,
	Vector<GdlGlyphAttrSetting *> & vpglfaAttrs)
{
	//	For each attribute assignment in the value list:
	for (int ipglfa = 0; ipglfa < vpglfaAttrs.Size(); ipglfa++)
	{
		Symbol psym = vpglfaAttrs[ipglfa]->GlyphSymbol();
		Assert(!psym->IsGeneric());
		int nGlyphAttrID = psym->InternalID();

		//	The new attribute assignment:
		GdlAssignment * pasgnValue = vpglfaAttrs[ipglfa]->Assignment();
		GdlExpression * pexpNew = pasgnValue->Expression();
		int nPRNew = pasgnValue->PointRadius();
		int mPrUnitsNew = pasgnValue->PointRadiusUnits();
		bool fOverrideNew = pasgnValue->Override();
		GrpLineAndFile lnfNew = pasgnValue->LineAndFile();
		int nStmtNoNew = lnfNew.PreProcessedLine();

		//	For each glyph ID covered by this definition's range:
		for (int iwGlyphID = 0; iwGlyphID < m_vwGlyphIDs.Size(); iwGlyphID++)
		{
			if (m_vwGlyphIDs[iwGlyphID] == kBadGlyph) // invalid glyph
				continue;

			//	Compare the new assignment with the previous.
			GdlExpression * pexpOld;
			int nPROld;
			int mPrUnitsOld;
			bool fOverrideOld, fShadow;
			GrpLineAndFile lnfOld;
			pgax->Get(m_vwGlyphIDs[iwGlyphID], nGlyphAttrID,
				&pexpOld, &nPROld, &mPrUnitsOld, &fOverrideOld, &fShadow, &lnfOld);
			int nStmtNoOld = lnfOld.PreProcessedLine();
			Assert(!fShadow);

			if (pexpOld == NULL ||
				(nStmtNoNew > nStmtNoOld && fOverrideNew) ||
				(nStmtNoOld > nStmtNoNew && !fOverrideOld))
			{
				//	The current attribute assignment overrides the previous--set it.
				pgax->Set(m_vwGlyphIDs[iwGlyphID], nGlyphAttrID,
					pexpNew, nPRNew, mPrUnitsNew, fOverrideNew, false, lnfNew);
			}

			//	If this glyph is a ligature, add the given component to its list.
			int ivComponent = psym->FieldIndex(StrAnsi("component"));
			if (ivComponent > -1)
			{
				plclist->AddComponentFor(m_vwGlyphIDs[iwGlyphID],
					psym->Generic()->BaseLigComponent(), prndr);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Set the (non-zero) system-defined glyph attributes to default values for all the glyphs.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::AssignGlyphAttrDefaultValues(GrcFont * pfont,
	GrcGlyphAttrMatrix * pgax, int cwGlyphs,
	Vector<Symbol> & vpsymSysDefined, Vector<int> & vnSysDefValues,
	Vector<GdlExpression *> & vpexpExtra,
	Vector<Symbol> & vpsymGlyphAttrs)
{
	bool fGotCharPropEng;
	ILgCharacterPropertyEnginePtr qchprpeng;

#ifdef GR_FW
	try
	{
		qchprpeng.CreateInstance(CLSID_LgIcuCharPropEngine);
		fGotCharPropEng = true;
	}
	catch (...)
	{
		fGotCharPropEng = false;
	}
	ComBool fIsSep;
#else
	qchprpeng = NULL;
	fGotCharPropEng = false;
	bool fIsSep = false;
#endif

	Assert(vpsymSysDefined.Size() == vnSysDefValues.Size());

	for (int i = 0; i < vpsymSysDefined.Size(); i++) // loop over attributes
	{
		bool fError = false;
		Symbol psym = vpsymSysDefined[i];
		int nValue = vnSysDefValues[i];

		int nGlyphAttrID = psym->InternalID();

		//	Set all values to the defaults for the corresponding Unicode character.

		GrcFont::iterator fit;
		int iUni;
		for (iUni = 0, fit = pfont->Begin(); fit != pfont->End(); ++fit, ++iUni) // loop over chars
		{
			int nUnicode = *fit;
			int wGlyphID = pfont->GlyphFromCmap(nUnicode, NULL);
			if (wGlyphID > 0)
			{
				//  Read from character properties database.
				//	How do we handle values from the Private Use Area? Just hard-code the
				//	range to skip?
				int nUnicodeStd;
				HRESULT hr;

				if (psym->LastFieldIs("breakweight"))
				{
					if (fGotCharPropEng)
					{
						hr = qchprpeng->get_IsSeparator(nUnicode, &fIsSep);
					}
					else
					{
						if (nUnicode == 0x0020 || nUnicode == 0x00A0 || nUnicode == 0x1680
							|| nUnicode == 0x180E
							|| (nUnicode >= 0x2000 && nUnicode <= 0x200A) || nUnicode == 0x202F
							|| nUnicode == 0x205F || nUnicode == 0x3000)
						{
							fIsSep = 1;
						}
						else
							fIsSep = 0;
						hr = S_OK;
					}
					nUnicodeStd = (fIsSep) ? klbWordBreak : nValue;
				}
				else if (psym->LastFieldIs("directionality"))
				{
//					g_errorList.AddWarning(4508, NULL, "Bidi = ", Bidi() ? "1" : "0");
					if (!Bidi())
					{
						// Don't really care about the failure, since there's no Bidi pass.
						hr = S_OK;
						nUnicodeStd = kdircNeutral;
					}
					else if (fGotCharPropEng)
					{
						LgBidiCategory bidi;
						hr = qchprpeng->get_BidiCategory(nUnicode, &bidi);
						if (!FAILED(hr))
							nUnicodeStd = ConvertBidiCode(bidi, nUnicode);
					}
					else
						hr = E_FAIL;
				}
				else
					break;	// ...out of the character loop; this is not an attribute
							// it makes sense to read from the db

				if (FAILED(hr))
				{
					//	Supply a few basic defaults that we know about.
					if (psym->LastFieldIs("breakweight"))
					{
						nUnicodeStd = -1;
						switch (nUnicode)
						{
						case kchwSpace:		nUnicodeStd = klbWordBreak; break;
						case kchwHyphen:	nUnicodeStd = klbHyphenBreak; break;
						default:
							break;
						}
						hr = (nUnicodeStd == -1) ? E_FAIL : S_OK;
					}
					else if (psym->LastFieldIs("directionality"))
					{
						nUnicodeStd = -1;
						switch (nUnicode)
						{
						case kchwSpace:	nUnicodeStd = kdircWhiteSpace; break;
						case kchwLRM:	nUnicodeStd = kdircL; break;
						case kchwRLM:	nUnicodeStd = kdircR; break;
						case kchwLRO:	nUnicodeStd = kdircLRO; break;
						case kchwRLO:	nUnicodeStd = kdircRLO; break;
						case kchwLRE:	nUnicodeStd = kdircLRE; break;
						case kchwRLE:	nUnicodeStd = kdircRLE; break;
						case kchwPDF:	nUnicodeStd = kdircPDF; break;
						default:
							break;
						}
						hr = (nUnicodeStd == -1) ? E_FAIL : S_OK;
					}
				}

				if (FAILED(hr))
				{
					if (!fError)
					{
						g_errorList.AddWarning(4509, NULL,
							"Unable to initialize ",
							psym->FullName(),
							" glyph attribute from Unicode char props database");
					}
					fError = true;
				}
				else if (!pgax->Defined(wGlyphID, nGlyphAttrID))
				{
					GdlExpression * pexpDefault = new GdlNumericExpression(nUnicodeStd);
					vpexpExtra.Push(pexpDefault);
					pgax->Set(wGlyphID, nGlyphAttrID,
						pexpDefault, 0, 0, false, false, GrpLineAndFile());
				}
			}
		}

		if (nValue == 0)
			continue;	// don't need to set zero values explicitly

		//	Now set any remaining attributes that weren't handled above to the standard defaults.
		for (int wGlyphID = 0; wGlyphID < cwGlyphs; wGlyphID++)
		{
			if (!pgax->Defined(wGlyphID, nGlyphAttrID))
			{
				GdlExpression * pexpDefault = new GdlNumericExpression(nValue);
				vpexpExtra.Push(pexpDefault);
				pgax->Set(wGlyphID, nGlyphAttrID,
					pexpDefault, 0, 0, false, false, GrpLineAndFile());
			}
		}
	}

	//	Assign 'kGpointNotSet' as the default value for all gpoint attributes. We use
	//	a special value for this to distinguish the situation of gpoint = 0, which
	//	may be a legitimate value.
//	for (int ipsym = 0; ipsym < vpsymGlyphAttrs.Size(); ipsym++)
//	{
//		Symbol psym = vpsymGlyphAttrs[ipsym];
//		int nGlyphAttrID = psym->InternalID();
//		if (psym->LastFieldIs("gpoint"))
//		{
//			for (int wGlyphID = 0; wGlyphID < cwGlyphs; wGlyphID++)
//			{
//				if (!pgax->Defined(wGlyphID, nGlyphAttrID))
//				{
//					GdlExpression * pexpDefault = new GdlNumericExpression(kGpointNotSet);
//					vpexpExtra.Push(pexpDefault);
//					pgax->Set(wGlyphID, nGlyphAttrID,
//						pexpDefault, 0, 0, false, false, GrpLineAndFile());
//				}
//			}
//		}
//	}

}


/*----------------------------------------------------------------------------------------------
	Convert the bidi categories defined by the character properties engine to those used
	by Graphite.
----------------------------------------------------------------------------------------------*/
DirCode GdlRenderer::ConvertBidiCode(LgBidiCategory bic, utf16 wUnicode)
{
	StrAnsi staCode;

	switch (bic)
	{
	case kbicL:			return kdircL;
	case kbicLRE:		return kdircLRE; // staCode = "LRE"; break;
	case kbicLRO:		return kdircLRO; // staCode = "LRO"; break;
	case kbicR:			return kdircR;
	case kbicAL:		return kdircRArab;
	case kbicRLE:		return kdircRLE; // staCode = "RLE"; break;
	case kbicRLO:		return kdircRLO; // staCode = "RLO"; break;
	case kbicPDF:		return kdircPDF; // staCode = "PDF"; break;

	case kbicEN:		return kdircEuroNum;
	case kbicES:		return kdircEuroSep;
	case kbicET:		return kdircEuroTerm;
	case kbicAN:		return kdircArabNum;
	case kbicCS:		return kdircComSep;

	case kbicNSM:		return kdircNSM;
	case kbicBN:		return kdircBndNeutral;
	case kbicB:			staCode = "B"; break;
	case kbicS:			staCode = "S"; break;

	case kbicWS:		return kdircWhiteSpace;
	case kbicON:		return kdircNeutral;
	default:
		char rgch[20];
		itoa(bic, rgch, 10);
		staCode = rgch;
		break;
	}

	if (Bidi())
	{
		g_errorList.AddWarning(4510, NULL,
			"Default Unicode bidi char type for 0x",
			GdlGlyphDefn::GlyphIDString(wUnicode), " = ", staCode,
			", which is not handled; char will be treated as neutral (ON)");
	}
	// otherwise the issue is irrelevant; don't bother with the warning

	return kdircNeutral;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Process each glyph attribute assignment for each glyph:
	* make sure that the statements are appropriate for the context of the glyph table
		(rather than a rule table);
	* do type checking;
	* convert g-paths to g-points.
	* convert x/y coordinates to g-points and vice versa.
----------------------------------------------------------------------------------------------*/
bool GrcManager::ProcessGlyphAttributes(GrcFont * pfont)
{
	int cStdStyles = max(m_vpsymStyles.Size(), 1);

	for (utf16 wGlyphID = 0; wGlyphID < m_cwGlyphIDs; wGlyphID++)
	{
		for (int iAttrID = 0; iAttrID < m_vpsymGlyphAttrs.Size(); iAttrID++)
		{
			for (int iStyle = 0; iStyle < cStdStyles; iStyle++)
			{
				GdlExpression * pexp;
				int nPR;
				int munitPR;
				bool fOverride, fShadow;
				GrpLineAndFile lnf;
				m_pgax->Get(wGlyphID, iAttrID,
					&pexp, &nPR, &munitPR, &fOverride, &fShadow, &lnf);
				Assert(!fShadow);

				if (!pexp)
					continue;

				Symbol psymAttr = m_vpsymGlyphAttrs[iAttrID];

				bool fOkay = pexp->TypeCheck(psymAttr->ExpType());
				if (!fOkay)
					g_errorList.AddWarning(4511, pexp,
						"Inconsistent or inappropriate type in glyph attribute: ",
						psymAttr->FullName(),
						lnf);

				pexp->GlyphAttrCheck();

				GdlExpression * pexpNew = pexp->SimplifyAndUnscale(wGlyphID, pfont);
				Assert(pexpNew);
				if (pexpNew && pexpNew != pexp)
				{
					m_vpexpModified.Push(pexpNew);	// so we can delete it later
					m_pgax->Set(wGlyphID, iAttrID,
						pexpNew, nPR, munitPR, fOverride, false, lnf);
				}

				//	Convert g-paths to g-points
				int nGPathValue;
				int ivGPath = psymAttr->FieldIndex("gpath");
				if (ivGPath > -1)
				{
					if (ivGPath != psymAttr->FieldCount() - 1)
					{
						//	not of the form <class-name>.<point-name>.gpath = X
						g_errorList.AddError(4125, pexp,
							"Invalid use of gpath attribute: ",
							psymAttr->FullName(),
							lnf);
					}
					else if (!pexpNew->ResolveToInteger(&nGPathValue, false))
					{
						g_errorList.AddError(4126, pexp,
							"Invalid value for gpath attribute--must be an integer: ",
							psymAttr->FullName(),
							lnf);
					}
					else
					{
						//	Find the corresponding gpoint attribute.
						Symbol psymGPoint = psymAttr->PointSisterField("gpoint");
						Assert(psymGPoint);

						//	Set its value.
						utf16 wActual = ActualForPseudo(wGlyphID);
						if (wActual == 0)
							wActual = wGlyphID;
						int nGPointValue = pfont->ConvertGPathToGPoint(wActual, nGPathValue, pexp);
						if (nGPointValue == -1)
						{
							char rgch[20];
							itoa(nGPointValue, rgch, 10);
							g_errorList.AddWarning(4512, NULL,
								"Invalid path for glyph 0x",
								GdlGlyphDefn::GlyphIDString(wGlyphID),
								": ",
								rgch,
								lnf);
							nGPointValue = 0;
						}

						pexpNew = new GdlNumericExpression(nGPointValue);
						pexpNew->CopyLineAndFile(*pexp);

						m_vpexpModified.Push(pexpNew);	// so we can delete it later
						m_pgax->Set(wGlyphID, psymGPoint->InternalID(),
							pexpNew, nPR, munitPR, fOverride, false, lnf);
					}
				}
			}
		}

		ConvertBetweenXYAndGpoint(pfont, wGlyphID);

		// Just in case, since incrementing 0xFFFF will produce zero.
		if (wGlyphID == 0xFFFF)
			break;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Convert x/y point coordinates to an actual on-curve point if there is one that matches
	closely (within PointRadius).

	On the other hand, if the specified g-point is the single point on its curve, convert it
	to x/y coordinates (due to the fact that single-point curves disappear from the final
	API point list). Actually do this in all cases, because g-points aren't available in the
	Linux version of the engine.

	We do this in a separate loop after both the x and y fields have been processed,
	simplified to unscaled integers, etc.
----------------------------------------------------------------------------------------------*/
void GrcManager::ConvertBetweenXYAndGpoint(GrcFont * pfont, utf16 wGlyphID)
{
	int cStdStyles = max(m_vpsymStyles.Size(), 1);
	utf16 wActual = ActualForPseudo(wGlyphID);
	if (wActual == 0)
		wActual = wGlyphID;

	for (int iAttrID = 0; iAttrID < m_vpsymGlyphAttrs.Size(); iAttrID++)
	{
		for (int iStyle = 0; iStyle < cStdStyles; iStyle++)
		{
			Symbol psymAttr = m_vpsymGlyphAttrs[iAttrID];

			if (psymAttr->LastFieldIs("x"))
			{
				Symbol psymAttrX = psymAttr;
				Symbol psymAttrY = psymAttrX->PointSisterField("y");
				if (!psymAttrY)
					continue; // need both x and y to convert
				int iAttrIDx = iAttrID;
				int iAttrIDy = psymAttrY->InternalID();

				GdlExpression * pexpX;
				int nPRx;
				int munitPRx;
				bool fOverride, fShadowX, fShadowY;
				GrpLineAndFile lnf;
				m_pgax->Get(wGlyphID, iAttrIDx,
					&pexpX, &nPRx, &munitPRx, &fOverride, &fShadowX, &lnf);

				if (!pexpX)
					continue;
				int nX;
				if (!pexpX->ResolveToInteger(&nX, false))
					continue;

				GdlExpression * pexpY;
				int nPRy;
				int munitPRy;
				m_pgax->Get(wGlyphID, iAttrIDy,
					&pexpY, &nPRy, &munitPRy, &fOverride, &fShadowY, &lnf);

				if (!pexpY)
					continue;
				int nY;
				if (!pexpY->ResolveToInteger(&nY, false))
					continue;

				Assert(fShadowX == fShadowY);
				if (fShadowX || fShadowY)
					continue;

				//	Find the corresponding gpoint attribute.
				Symbol psymGPoint = psymAttrX->PointSisterField("gpoint");
				Assert(psymGPoint);

				nPRx = pfont->ScaledToAbsolute(nPRx, munitPRx);
				nPRy = pfont->ScaledToAbsolute(nPRy, munitPRy);

				//	See if we can find a corresponding on-curve point.
				int nGPointValue = pfont->GetPointAtXY(wActual, nX, nY, max(nPRx, nPRy), pexpX);
				if (nGPointValue > -1 && !pfont->IsPointAlone(wActual, nGPointValue, pexpX))
				{
					//	We found one. Set the value of the gpoint field.
					GdlExpression * pexpGpoint = new GdlNumericExpression(nGPointValue);
					pexpGpoint->CopyLineAndFile(*pexpX);

					m_vpexpModified.Push(pexpGpoint);	// so we can delete it later
					m_pgax->Set(wGlyphID, psymGPoint->InternalID(),
						pexpGpoint, 0, munitPRx, fOverride, false, lnf);

					//	Clear the x and y fields.
					m_pgax->Set(wGlyphID, iAttrIDx, NULL, 0, munitPRx, true, false, lnf);
					m_pgax->Set(wGlyphID, iAttrIDy, NULL, 0, munitPRy, true, false, lnf);

					//	Don't delete the actual expressions, because they are owned by the
					//	original assignment statements, which will delete them.
				}
			}
			else if (psymAttr->LastFieldIs("gpoint"))
			{
				Symbol psymAttrGpoint = psymAttr;

				int iAttrIDgpoint = iAttrID;
				GdlExpression * pexpGpoint;
				int nPR;
				int munitPR;
				bool fOverride, fShadow;
				GrpLineAndFile lnf;
				m_pgax->Get(wGlyphID, iAttrIDgpoint,
					&pexpGpoint, &nPR, &munitPR, &fOverride, &fShadow, &lnf);
				Assert(!fShadow);

				if (!pexpGpoint)
					continue;
				int nGPoint;
				if (!pexpGpoint->ResolveToInteger(&nGPoint, false))
					continue;

				//	Convert gpoint to x/y. On Linux gpoint will never work.
				//	It won't work on Windows either if the point comprises a single-point path,
				//	so in that case, delete the gpoint setting altogether. In other cases,
				//	leave both around, so if gpoint doesn't work, the engine can fall back
				//	to x/y.
				int mX, mY;
				if (pfont->GetXYAtPoint(wActual, nGPoint, &mX, &mY, pexpGpoint))
				{
					//	Create equivalent x/y statements.

					Symbol psymX = psymAttrGpoint->PointSisterField("x");
					Symbol psymY = psymAttrGpoint->PointSisterField("y");
					Assert(psymX);
					Assert(psymY);
					int nIDX = psymX->InternalID();
					int nIDY = psymY->InternalID();

					GdlExpression * pexpX = new GdlNumericExpression(mX, kmunitUnscaled);
					pexpX->CopyLineAndFile(*pexpGpoint);

					GdlExpression * pexpY = new GdlNumericExpression(mY, kmunitUnscaled);
					pexpY->CopyLineAndFile(*pexpGpoint);

					m_vpexpModified.Push(pexpX);	// so we can delete them later
					m_vpexpModified.Push(pexpY);

					if (m_pgax->Defined(wGlyphID, nIDX) && m_pgax->Defined(wGlyphID, nIDY))
					{
						Symbol psymBasePt = psymAttr->BasePoint();
						g_errorList.AddWarning(4513, pexpGpoint,
							"Both x/y coordinates and gpoint are defined for ",
							psymBasePt->FullName(),
							" for glyph 0x",
							GdlGlyphDefn::GlyphIDString(wGlyphID),
							"; only gpoint will be used");
					}

					bool fDeleteGpoint = pfont->IsPointAlone(wActual, nGPoint, pexpGpoint);

					// Store the new expressions as glyph attribute assignments.
					m_pgax->Set(wGlyphID, nIDX, pexpX, nPR, munitPR,
						fOverride, !fDeleteGpoint, lnf);
					m_pgax->Set(wGlyphID, nIDY, pexpY, nPR, munitPR,
						fOverride, !fDeleteGpoint, lnf);

					if (fDeleteGpoint)
					{
						//	Since this is a single-point path, it won't show up in the
						//	hinted glyph from the graphics object, so we really have to
						//	use the x/y coordinates. So clear the gpoint field.
						m_pgax->Set(wGlyphID, iAttrIDgpoint, NULL, 0, munitPR, true, false, lnf);
					}

					//	Don't delete the actual expression, because it is owned by the
					//	original assignment statements, which will delete it.
				}
			}
		}
	}
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	First, flatten any slot attributes that are reading points to use integers instead
	(ie, change { attach.at = udap } to { attach.at { x = udap.x; y = udap.y }} ).
	Then substitute the internal glyph attribute IDs into the rules. Do error checking, making
	sure glyph attributes are defined appropriately where expected.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont)
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->FixGlyphAttrsInRules(pcman, pfont);
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont)
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->FixGlyphAttrsInRules(pcman, pfont);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont)
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->FixGlyphAttrsInRules(pcman, pfont);
	}

	//	While we're at it...
	FixFeatureTestsInPass(pfont);
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont)
{
	//	Make a list of all the input-classes in the rule, for checking for the definition
	//	of glyph attributes in constraints and attribute-setters.
	Vector<GdlGlyphClassDefn *> vpglfcInClasses;
	int iprit;
	for (iprit = 0; iprit < m_vprit.Size();	iprit++)
	{
		Symbol psymInput = m_vprit[iprit]->m_psymInput;
		if (psymInput &&
			(psymInput->FitsSymbolType(ksymtClass) ||
				psymInput->FitsSymbolType(ksymtSpecialLb)) &&
			!psymInput->LastFieldIs(GdlGlyphClassDefn::Undefined()))
		{
			GdlGlyphClassDefn * pglfc = psymInput->GlyphClassDefnData();
			Assert(pglfc);
			vpglfcInClasses.Push(pglfc);
		}
		else
			//	invalid input class
			vpglfcInClasses.Push(NULL);
	}
	Assert(m_vprit.Size() == vpglfcInClasses.Size());

	//	Flatten slot attributes that use points to use integers instead. Do this
	//	entire process before fixing glyph attrs, because there can be some interaction between
	//	slots in a rule (eg, attach.to/at). So we can be sure at that point what state
	//	things are in.
	for (iprit = 0; iprit < m_vprit.Size(); iprit++)
	{
		m_vprit[iprit]->FlattenPointSlotAttrs(pcman);
	}

	//	While we're at it, fix the feature tests. Do this before fixing the glyph attributes,
	//	because it is possible to have feature tests embedded in conditional statements.
	FixFeatureTestsInRules(pfont);

	//	Now do the fixes, error checks, etc.
	for (iprit = 0; iprit < m_vprit.Size(); iprit++)
	{
		m_vprit[iprit]->FixGlyphAttrsInRules(pcman, vpglfcInClasses, this, iprit);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::FixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit)
{
	if (!m_psymInput)
		return;

	GdlGlyphClassDefn * pglfcIn = m_psymInput->GlyphClassDefnData();
	if (!pglfcIn)
		return;	// invalid class

	//	Process constraint
	if (m_pexpConstraint)
		m_pexpConstraint->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLineBreakItem::FixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit)
{
	//	method on superclass: process constraints.
	GdlRuleItem::FixGlyphAttrsInRules(pcman, vpglfcInClasses, prule, irit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::FixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit)
{
	//	method on superclass: process constraints.
	GdlRuleItem::FixGlyphAttrsInRules(pcman, vpglfcInClasses, prule, irit);

	bool fXYAt = false;			// true if the attach.at statements need to include x/y
	bool fGpointAt = false;		// true if the attach.at statements need to include gpoint
	bool fXYWith = false;		// true if the attach.with statements need to include x/y
	bool fGpointWith = false;	// true if the attach.with statements need to include gpoint
								// ...due to the way the glyph attributes are defined

	bool fAttachTo = false;		// true if an attach.to statement is present
	bool fAttachAtX = false;	// true if an attach.at.x/gpoint statement is present
	bool fAttachAtY = false;	// true if an attach.at.y/gpoint statement is present
	bool fAttachWithX = false;	// true if an attach.with.x/gpoint statement is present
	bool fAttachWithY = false;	// true if an attach.with.y/gpoint statement is present

	bool fDidAttachAt = false;	// did checks for flattened attach.at
	bool fDidAttachWith = false; // did checks for flattened attach.with

	Symbol psymOutput = OutputClassSymbol();

	//	Process attribute-setting statements.
	int ipavs;
	for (ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		GdlAttrValueSpec * pavs = m_vpavs[ipavs];

		//	Check that appropriate glyph attributes exist for the slot attributes that
		//	are making use of them.
		Symbol psym = pavs->m_psymName;
		Assert(psym->FitsSymbolType(ksymtSlotAttr));

		if (psym->IsAttachAtField())
		{
			StrAnsi staT = psym->LastField();
			fAttachAtX = fAttachAtX || staT == "x" || staT == "gpoint" || staT == "gpath";
			fAttachAtY = fAttachAtY || staT == "y" || staT == "gpoint" || staT == "gpath";

			if (staT == "gpath")
			{
				g_errorList.AddError(4127, this,
					"Cannot use gpath function within a rule");
				continue;
			}

			//	The engine currently can't handle single-point paths, and within the rule, we
			//	don't have a meaningful and consistent way to change such gpoint statements to
			//	x/y coordinates. So disallow gpoint statements in rules. If we find a way
			//	to make the engine handle single-point paths, take this code out.
			if (staT == "gpoint")
			{
				int nTmp;
				if (pavs->m_pexpValue->ResolveToInteger(&nTmp, false)) // constant, not glyph attr
				{
					g_errorList.AddError(4128, this,
						"Cannot use gpoint function within a rule");
					continue;
				}
			}

			if (fDidAttachAt && pavs->Flattened())
				//	The precompiler flattened the attach.at command into separate fields;
				//	in that case we don't need to check them twice.
				continue;

			//	The value of attach.at must be defined for the class of the slot
			//	receiving the attachment, not this slot.
			int srAttachToValue = AttachToSettingValue();	// 1-based
			if (srAttachToValue == -1)
				g_errorList.AddWarning(4514, this,
					"Attachment checks could not be done for value of attach.at");
			else if (srAttachToValue == -2)
			{
				fAttachTo = true;
				Assert(false);	// a VERY strange thing to happen.
				g_errorList.AddError(4129, this,
					"Inappropriate value of attach.to");
			}
			else
			{
				fAttachTo = true;
				if (pavs->Flattened())
				{
					pavs->CheckAttachAtPoint(pcman, vpglfcInClasses, srAttachToValue-1,
						&fXYAt, &fGpointAt);
					fDidAttachAt = true;
				}
				else
					pavs->FixGlyphAttrsInRules(pcman, vpglfcInClasses, srAttachToValue-1,
						psymOutput);
			}
		}

		else if (psym->IsAttachWithField())
		{
			StrAnsi staT = psym->LastField();
			fAttachWithX = fAttachWithX || staT == "x" || staT == "gpoint" || staT == "gpath";
			fAttachWithY = fAttachWithY || staT == "y" || staT == "gpoint" || staT == "gpath";

			if (staT == "gpath")
			{
				g_errorList.AddError(4130, this,
					"Cannot use gpath function within a rule");
				continue;
			}

			if (fDidAttachWith && pavs->Flattened())
				//	The precompiler flattened the attach.with command into separate fields;
				//	in that case we don't need to check them twice.
				continue;

			if (!fAttachTo)
				fAttachTo = (AttachToSettingValue() != -1);

			if (pavs->Flattened())
			{
				pavs->CheckAttachWithPoint(pcman, vpglfcInClasses, irit,
					&fXYWith, &fGpointWith);
				fDidAttachWith = true;
			}
			else
				pavs->FixGlyphAttrsInRules(pcman, vpglfcInClasses, irit, psymOutput);
		}

		else if (psym->IsComponentRef())
		{
			CheckCompBox(pcman, psym);
		}

		else if (psymOutput == NULL)
		{	// error condition
		}
		else
		{
			if (psym->IsAttachTo())
			{
				GdlSlotRefExpression * pexpSR =
					dynamic_cast<GdlSlotRefExpression *>(pavs->m_pexpValue);
				if (pexpSR)
				{
					int srAttachTo = pexpSR->SlotNumber();
					if (srAttachTo == 0)
					{
						// no attachment
					}
					else if (prule->NumberOfSlots() <= srAttachTo - 1)
					{
						//	slot out of range--error will be produced later
					}
					else if (!dynamic_cast<GdlSetAttrItem *>(prule->Item(srAttachTo - 1)))
						g_errorList.AddError(4131, this,
							"Cannot attach to an item in the context");
				}
			}
			pavs->FixGlyphAttrsInRules(pcman, vpglfcInClasses, irit, psymOutput);
		}
	}

	if ((fAttachTo || fAttachAtX || fAttachAtY || fAttachWithX || fAttachWithY) &&
		(!fAttachTo || !fAttachAtX || !fAttachAtY || !fAttachWithX || !fAttachWithY))
	{
		if ((fAttachAtX || fAttachAtY) && !fAttachTo)
			g_errorList.AddError(4132, this,
				"Cannot specify attach.at without attach.to");
		else
			g_errorList.AddWarning(4515, this,
				"Incomplete attachment specification");
	}

	//	Delete any superfluous attach commands (that were added in FlattenPointSlotAttrs
	//	but not needed); ie, either the x/y point fields or the gpath field. It's possible
	//	that we need to keep both versions, if one set of glyphs uses one and another set
	//	uses the other.
	for (ipavs = m_vpavs.Size(); --ipavs >= 0; )
	{
		bool fDeleteThis = false;
		Symbol psym = m_vpavs[ipavs]->m_psymName;
		if (psym->IsAttachAtField() && m_vpavs[ipavs]->Flattened())
		{
			if (!fXYAt && (psym->LastFieldIs("x") || psym->LastFieldIs("y")))
				//	Keep attach.at.gpoint; throw away x/y.
				fDeleteThis = true;
			else if (!fGpointAt && psym->LastFieldIs("gpoint"))
				//	Keep attach.at.x/y; throw away gpoint.
				fDeleteThis = true;
		}
		else if (psym->IsAttachWithField() && m_vpavs[ipavs]->Flattened())
		{
			if (!fXYWith && (psym->LastFieldIs("x") || psym->LastFieldIs("y")))
				//	Keep attach.with.gpoint; throw away x/y.
				fDeleteThis = true;
			else if (!fGpointWith && psym->LastFieldIs("gpoint"))
				//	Keep attach.with.x/y; throw away gpoint;
				fDeleteThis = true;
		}

		if (fDeleteThis)
		{
			delete m_vpavs[ipavs];
			m_vpavs.Delete(ipavs);
		}
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::FixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit)
{
	GdlSetAttrItem::FixGlyphAttrsInRules(pcman, vpglfcInClasses, prule, irit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::FixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit, Symbol psymOutClass)
{
	Assert(psymOutClass->FitsSymbolType(ksymtClass) ||
		psymOutClass->FitsSymbolType(ksymtSpecialUnderscore) ||
		psymOutClass->FitsSymbolType(ksymtSpecialAt));

	if (m_pexpValue)
	{
		m_pexpValue->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
		m_pexpValue->LookupExpCheck(false);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the symbol for the output class to use in checking the rule item. Specifically,
	if a substitution item has a selector and no class (ie, is something like '@2') return
	the class from the appropriate selected item. Return NULL if there is no input class,
	or the selector was invalid (in which cases an error was already recorded).
----------------------------------------------------------------------------------------------*/
Symbol GdlSetAttrItem::OutputClassSymbol()
{
	return OutputSymbol();
}

/*--------------------------------------------------------------------------------------------*/
Symbol GdlSubstitutionItem::OutputClassSymbol()
{
	if (m_psymOutput->FitsSymbolType(ksymtSpecialAt))
	{
		if (!m_pritSelInput)
			return NULL;
		return m_pritSelInput->m_psymInput;
	}

	return m_psymOutput;
}


/*----------------------------------------------------------------------------------------------
	Flatten any slot attributes that use points to use integers instead. That is, replace
	them with versions in terms of the fields of a point that are appropriate. So instead of

	  { attach.with = point1 }

	we generate

	  { attach.with {
			x = point1.x; y = point1.y;
			gpoint = point1.gpoint;
			xoffset = point1.xoffset; yoffset = point1.yoffset }
	  }

	We create as many of the above five as are defined in the symbol table, and later delete
	the superfluous one(s) (x/y or gpoint).
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::FlattenPointSlotAttrs(GrcManager * pcman)
{
	//	No attribute setters to worry about.
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::FlattenPointSlotAttrs(GrcManager * pcman)
{
	Vector<GdlAttrValueSpec *> vpavsNew;
	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		m_vpavs[ipavs]->FlattenPointSlotAttrs(pcman, vpavsNew);
	}
	m_vpavs.Clear();
	vpavsNew.CopyTo(m_vpavs);
}

/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::FlattenPointSlotAttrs(GrcManager * pcman,
	Vector<GdlAttrValueSpec *> & vpavsNew)
{
	if (m_psymName->IsBogusSlotAttr())	// eg, shift.gpath
	{
		int nTmp;
		if ((m_psymName->LastFieldIs("xoffset") || m_psymName->LastFieldIs("yoffset")) &&
			m_pexpValue->ResolveToInteger(&nTmp, false) && nTmp == 0)
		{	// ignore
		}
		else
			g_errorList.AddError(4133, this,
				"Invalid slot attribute: ",
				m_psymName->FullName());
		delete this;
	}
	else if (m_psymName->FitsSymbolType(ksymtSlotAttrPt))	// attach.at, shift, etc.
	{
		if (m_psymName->IsReadOnlySlotAttr())
		{
			//	Eventually will produce an error--for now, just pass through as is.
			vpavsNew.Push(this);
			return;
		}

		if (m_psymOperator->FullName() != "=")
		{
			//	Can't use +=, -= with entire points.
			g_errorList.AddError(4134, this,
				"Invalid point arithmetic; fields must be calculated independently in order to use ",
				m_psymOperator->FullName());
			return;
		}

		Symbol psymX = m_psymName->SubField("x");
		Assert(psymX);
		Symbol psymY = m_psymName->SubField("y");
		Assert(psymY);
		Symbol psymGpoint = m_psymName->SubField("gpoint");
		Assert(psymGpoint);
		Symbol psymXoffset = m_psymName->SubField("xoffset");
		Assert(psymXoffset);
		Symbol psymYoffset = m_psymName->SubField("yoffset");
		Assert(psymYoffset);

		GdlExpression * pexpX = NULL;
		GdlExpression * pexpY = NULL;
		GdlExpression * pexpGpoint = NULL;
		GdlExpression * pexpXoffset = NULL;
		GdlExpression * pexpYoffset = NULL;

		bool fExpOkay = m_pexpValue->PointFieldEquivalents(pcman,
			&pexpX, &pexpY, &pexpGpoint, &pexpXoffset, &pexpYoffset);
		if (!fExpOkay)
		{
			g_errorList.AddError(4135, this,
				"Invalid point arithmetic");
			delete this;
			return;
		}
		else if (!pexpX && !pexpY && !pexpGpoint && !pexpYoffset && !pexpYoffset)
		{
			GdlLookupExpression * pexpLookup =
				dynamic_cast<GdlLookupExpression *>(m_pexpValue);
			Assert(pexpLookup);
			if (pexpLookup->Name()->FitsSymbolType(ksymtGlyphAttr))
				g_errorList.AddError(4136, this,
					"Glyph attribute is not a point: ",
					pexpLookup->Name()->FullName());
			else
				g_errorList.AddError(4137, this,
					"Undefined glyph attribute: ",
					pexpLookup->Name()->FullName());
			delete this;
			return;
		}

		GdlAttrValueSpec * pavs;
		if (pexpX)
		{
			pavs = new GdlAttrValueSpec(psymX, m_psymOperator, pexpX);
			pavs->CopyLineAndFile(*this);
			pavs->SetFlattened(true);
			vpavsNew.Push(pavs);
		}
		if (pexpY)
		{
			pavs = new GdlAttrValueSpec(psymY, m_psymOperator, pexpY);
			pavs->CopyLineAndFile(*this);
			pavs->SetFlattened(true);
			vpavsNew.Push(pavs);
		}
		if (pexpGpoint)
		{
			if (psymGpoint->IsBogusSlotAttr())
				delete pexpGpoint;
			else
			{
				pavs = new GdlAttrValueSpec(psymGpoint, m_psymOperator, pexpGpoint);
				pavs->CopyLineAndFile(*this);
				pavs->SetFlattened(true);
				vpavsNew.Push(pavs);
			}
		}
		if (pexpXoffset)
		{
			if (psymXoffset->IsBogusSlotAttr())
				delete pexpXoffset;
			else
			{
				pavs = new GdlAttrValueSpec(psymXoffset, m_psymOperator, pexpXoffset);
				pavs->CopyLineAndFile(*this);
				pavs->SetFlattened(true);
				vpavsNew.Push(pavs);
			}
		}
		if (pexpYoffset)
		{
			if (psymYoffset->IsBogusSlotAttr())
				delete pexpYoffset;
			else
			{
				pavs = new GdlAttrValueSpec(psymYoffset, m_psymOperator, pexpYoffset);
				pavs->CopyLineAndFile(*this);
				pavs->SetFlattened(true);
				vpavsNew.Push(pavs);
			}
		}

		delete this;	// replaced
	}
	else
		vpavsNew.Push(this);
}


/*----------------------------------------------------------------------------------------------
	Check that the given glyph attribute is defined for every glyph that this
	class subsumes. Also check that all the necessary point fields or
	ligature component box fields are defined.
	Arguments
		pgdlAvsOrExp			- for error message--attr value spec or expression
		psymtbl					- global symbol table
		pgax					- glyph attr matrix
		psymGlyphAttr			- attribute to check for
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::CheckExistenceOfGlyphAttr(GdlObject * pgdlAvsOrExp,
	GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->CheckExistenceOfGlyphAttr(pgdlAvsOrExp, psymtbl, pgax,
			psymGlyphAttr);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::CheckExistenceOfGlyphAttr(GdlObject * pgdlAvsOrExp,
	GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr)
{
	int nGlyphAttrID = psymGlyphAttr->InternalID();
	bool fGpoint = psymGlyphAttr->LastFieldIs("gpoint");

	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		if (m_vwGlyphIDs[iw] == kBadGlyph)
			continue;

		utf16 wGlyphID = m_vwGlyphIDs[iw];
		if ((fGpoint && !pgax->GpointDefined(wGlyphID, nGlyphAttrID)) ||
			(!fGpoint && !pgax->Defined(wGlyphID, nGlyphAttrID)))
		{
			g_errorList.AddError(4138, pgdlAvsOrExp,
				StrAnsi("Glyph attribute '"),
				psymGlyphAttr->FullName(),
				StrAnsi("' is not defined for glyph 0x"),
				GlyphIDString(wGlyphID));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the value of the attach.to setting, -1 if it was not found, or -2 if the value
	was not slot reference.
----------------------------------------------------------------------------------------------*/
int GdlSetAttrItem::AttachToSettingValue()
{
	for (int iavs = 0; iavs < m_vpavs.Size(); iavs++)
	{
		Symbol psym = m_vpavs[iavs]->m_psymName;
		if (psym->IsAttachTo())
		{
			GdlSlotRefExpression * pexpSR =
				dynamic_cast<GdlSlotRefExpression *>(m_vpavs[iavs]->m_pexpValue);
			if (pexpSR)
				return pexpSR->SlotNumber();
			else
				return -2;
		}
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	The recipient is the setting of the attach.at slot attribute.
	Check that the attachment point in the value is defined for all glyphs subsumed by
	pglfc, which is the slot being attached to.
----------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::CheckAttachAtPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool *pfGpoint)
{
	Assert(!m_psymName->LastFieldIs("gpath"));	// caller checked for this

	m_pexpValue->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit,
		pfXY, pfGpoint);
}

/*----------------------------------------------------------------------------------------------
	The recipient is the setting of the attach.with slot attribute.
	Check that the attachment point in the value is defined for all glyphs subsumed by
	pglfc, which is the slot being attached.
----------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::CheckAttachWithPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
	Assert(!m_psymName->LastFieldIs("gpath"));	// caller checked for this

	m_pexpValue->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit,
		pfXY, pfGpoint);
}

/*----------------------------------------------------------------------------------------------
	Check that the necessary fields of the given glyph attribute--an attachment point--
	are defined for all the glyphs subsumed by this class.
	Arguments:
		pgdlAvsOrExp			- for error message--attr value spec or expression
		psymtbl					- global symbol table
		pgax					- glyph attr matrix
		psymGlyphAttr			- attribute to check for
		pfXY					- return true if x and y are defined
		pfGpoint				- return true if gpoint is defined
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::CheckCompleteAttachmentPoint(GdlObject * pgdlAvsOrExp,
	GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr,
	bool * pfXY, bool * pfGpoint)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->CheckCompleteAttachmentPoint(pgdlAvsOrExp, psymtbl, pgax,
			psymGlyphAttr, pfXY, pfGpoint);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::CheckCompleteAttachmentPoint(GdlObject * pgdlAvsOrExp,
	GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr,
	bool * pfXY, bool * pfGpoint)
{
	Symbol psymX = psymGlyphAttr->SubField("x");
	Symbol psymY = psymGlyphAttr->SubField("y");
	Symbol psymGpoint = psymGlyphAttr->SubField("gpoint");
	//Symbol psymXoffset = psymGlyphAttr->SubField("xoffset");
	//Symbol psymYoffset = psymGlyphAttr->SubField("yoffset");

	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		utf16 wGlyphID = m_vwGlyphIDs[iw];

		if (wGlyphID == kBadGlyph)
			continue;

		if (psymGpoint && pgax->GpointDefined(wGlyphID, psymGpoint->InternalID()))
		{
			bool fShadowX, fShadowY;
			bool fAlsoX = (psymX && pgax->DefinedButMaybeShadow(wGlyphID, psymX->InternalID(), &fShadowX));
			bool fAlsoY = (psymY && pgax->DefinedButMaybeShadow(wGlyphID, psymY->InternalID(), &fShadowY));
			// Error already handled in ConvertBetweenXYAndGpoint
//			if (fAlsoX && !fShadowY && fAlsoY && !fShadowY)
//			{
//				g_errorList.AddWarning(4516, pgdlAvsOrExp,
//					"Both x/y coordinates and gpoint are defined for ",
//					psymGlyphAttr->FullName(),
//					" for glyph 0x",
//					GlyphIDString(wGlyphID),
//					"; only gpoint will be used");
//			}

			*pfGpoint = true;

			if (fAlsoX && fShadowX && fAlsoY && fShadowY)
			{
				*pfXY = true;
			}
			else
			{
				if (fAlsoX)
					pgax->Clear(wGlyphID, psymX->InternalID());
				if (fAlsoY)
					pgax->Clear(wGlyphID, psymY->InternalID());
			}
		}
		else if (psymX && pgax->Defined(wGlyphID, psymX->InternalID()) &&
			psymY && pgax->Defined(wGlyphID, psymY->InternalID()))
		{
			*pfXY = true;
		}
		else
		{
			g_errorList.AddWarning(4517, pgdlAvsOrExp,
				"Point '",
				psymGlyphAttr->FullName(),
				"' not completely defined for glyph 0x",
				GlyphIDString(wGlyphID));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	The recipient is a slot that is having a component.???.ref attribute set. Check to make
	sure that the appropriate component box is fully defined for all glyphs.
----------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::CheckCompBox(GrcManager * pcman, Symbol psymCompRef)
{
	Assert(psymCompRef->IsComponentRef());

	GdlGlyphClassDefn * pglfc = OutputSymbol()->GlyphClassDefnData();
	if (!pglfc)
		return;

	Symbol psymBaseComp = psymCompRef->BaseLigComponent();

	pglfc->CheckCompBox(this, pcman->SymbolTable(), pcman->GlyphAttrMatrix(), psymBaseComp);
}

/*----------------------------------------------------------------------------------------------
	Check to make sure that the given component's box has been fully defined
	for all the subsumed glyphs.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::CheckCompBox(GdlObject * pgdlSetAttrItem,
	GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymCompRef)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->CheckCompBox(pgdlSetAttrItem, psymtbl, pgax, psymCompRef);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::CheckCompBox(GdlObject * pgdlSetAttrItem,
	GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymCompRef)
{
	Symbol psymTop = psymCompRef->SubField("top");
	Symbol psymBottom = psymCompRef->SubField("bottom");
	Symbol psymLeft = psymCompRef->SubField("left");
	Symbol psymRight = psymCompRef->SubField("right");

	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		utf16 wGlyphID = m_vwGlyphIDs[iw];

		if (wGlyphID == kBadGlyph)
			continue;

		if (!psymTop || !pgax->Defined(wGlyphID, psymTop->InternalID()))
		{
			g_errorList.AddError(4139, pgdlSetAttrItem,
				"Top of box for ",
				psymCompRef->FullName(),
				" not defined for glyph 0x",
				GlyphIDString(wGlyphID));
		}
		if (!psymBottom || !pgax->Defined(wGlyphID, psymBottom->InternalID()))
		{
			g_errorList.AddError(4140, pgdlSetAttrItem,
				"Bottom of box for ",
				psymCompRef->FullName(),
				" not defined for glyph 0x",
				GlyphIDString(wGlyphID));
		}
		if (!psymLeft || !pgax->Defined(wGlyphID, psymLeft->InternalID()))
		{
			g_errorList.AddError(4141, pgdlSetAttrItem,
				"Left of box for ",
				psymCompRef->FullName(),
				" not defined for glyph 0x",
				GlyphIDString(wGlyphID));
		}
		if (!psymRight || !pgax->Defined(wGlyphID, psymRight->InternalID()))
		{
			g_errorList.AddError(4142, pgdlSetAttrItem,
				"Right of box for ",
				psymCompRef->FullName(),
				" not defined for glyph 0x",
				GlyphIDString(wGlyphID));
		}
	}
}

/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Process all the feature testing-constraints: supply the feature contexts to the values
	(ie, in the test ligatures == some, the "some" must be converted to the value for
	'some' setting of the ligatures feature. Record an error if the setting is undefined.
----------------------------------------------------------------------------------------------*/
void GdlRule::FixFeatureTestsInRules(GrcFont * pfont)
{
	for (int ipexp = 0; ipexp < m_vpexpConstraints.Size(); ipexp++)
	{
		m_vpexpConstraints[ipexp]->FixFeatureTestsInRules(pfont);
		m_vpexpConstraints[ipexp]->LookupExpCheck(true);
	}

	for (int irit = 0; irit < m_vprit.Size(); irit++)
	{
		m_vprit[irit]->FixFeatureTestsInRules(pfont);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::FixFeatureTestsInRules(GrcFont * pfont)
{
	if (m_pexpConstraint)
	{
		m_pexpConstraint->FixFeatureTestsInRules(pfont);
		m_pexpConstraint->LookupExpCheck(false);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::FixFeatureTestsInRules(GrcFont * pfont)
{
	GdlRuleItem::FixFeatureTestsInRules(pfont);

	for (int iavs = 0; iavs < this->m_vpavs.Size(); iavs++)
		m_vpavs[iavs]->FixFeatureTestsInRules(pfont);
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::FixFeatureTestsInPass(GrcFont * pfont)
{
	for (int ipexp = 0; ipexp < m_vpexpConstraints.Size(); ipexp++)
	{
		m_vpexpConstraints[ipexp]->FixFeatureTestsInRules(pfont);
		m_vpexpConstraints[ipexp]->LookupExpCheck(true);
	}
}
/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::FixFeatureTestsInRules(GrcFont * pfont)
{
	// Particularly handle feature tests in conditional statements.
	m_pexpValue->FixFeatureTestsInRules(pfont);
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Resolve all glyph attribute definitions to integers.
----------------------------------------------------------------------------------------------*/
bool GrcManager::FinalGlyphAttrResolution(GrcFont * pfont)
{
	int cStdStyles = max(m_vpsymStyles.Size(), 1);

	//Symbol psymJStr = m_psymtbl->FindSymbol(GrcStructName("justify", "0", "stretch"));
	Symbol psymJStr = m_psymtbl->FindSymbol(GrcStructName("justify", "stretch"));
	int nAttrIdJStr = psymJStr->InternalID();
	//Symbol psymJShr = m_psymtbl->FindSymbol(GrcStructName("justify", "0", "shrink"));
	Symbol psymJShr = m_psymtbl->FindSymbol(GrcStructName("justify", "shrink"));
	int nAttrIdJShr = psymJShr->InternalID();
	//Symbol psymJStep = m_psymtbl->FindSymbol(GrcStructName("justify", "0", "step"));
	Symbol psymJStep = m_psymtbl->FindSymbol(GrcStructName("justify", "step"));
	int nAttrIdJStep = psymJStep->InternalID();
	//Symbol psymJWeight = m_psymtbl->FindSymbol(GrcStructName("justify", "0", "weight"));
	Symbol psymJWeight = m_psymtbl->FindSymbol(GrcStructName("justify", "weight"));
	int nAttrIdJWeight = psymJWeight->InternalID();

	for (utf16 wGlyphID = 0; wGlyphID < m_cwGlyphIDs; wGlyphID++)
	{
		for (int iAttrID = 0; iAttrID < m_vpsymGlyphAttrs.Size(); iAttrID++)
		{
			for (int iStyle = 0; iStyle < cStdStyles; iStyle++)
			{
				Set<Symbol> setpsym;
				GdlExpression * pexp;
				GdlExpression * pexpNew;
				int nPR;
				int munitPR;
				bool fOverride, fShadow;
				GrpLineAndFile lnf;
				m_pgax->Get(wGlyphID, iAttrID, &pexp, &nPR, &munitPR, &fOverride, &fShadow, &lnf);
				if (pexp)
				{
					bool fCanSub;
					pexpNew =
						pexp->SimplifyAndUnscale(m_pgax, wGlyphID, setpsym, pfont, true, &fCanSub);

					int nMinValue, nMaxValue;
					MinAndMaxGlyphAttrValues(iAttrID,
						NumJustLevels(), nAttrIdJStr, nAttrIdJShr, nAttrIdJStep, nAttrIdJWeight,
						&nMinValue, &nMaxValue);

					if (pexpNew && pexpNew != pexp)
					{
						m_vpexpModified.Push(pexpNew);	// so we can delete it later
						m_pgax->Set(wGlyphID, iAttrID,
							pexpNew, nPR, munitPR, fOverride, false, lnf);
						pexp = pexpNew;
					}
					int n;
					if (!pexp->ResolveToInteger(&n, false))
					{
						g_errorList.AddError(4143, pexp,
							"Could not resolve definition of glyph attribute ",
							m_vpsymGlyphAttrs[iAttrID]->FullName(),
							" for glyph 0x",
							GdlGlyphDefn::GlyphIDString(wGlyphID));
					}
					else if (n <= nMinValue)
					{
						char rgch1[20];
						char rgch2[20];
						itoa(n, rgch1, 10);
						itoa(nMinValue + 1, rgch2, 10);
						g_errorList.AddError(4144, pexp,
							"Value of glyph attribute ",
							m_vpsymGlyphAttrs[iAttrID]->FullName(),
							" for glyph 0x",
							GdlGlyphDefn::GlyphIDString(wGlyphID),
							" = ", rgch1,
							"; minimum is ",
							rgch2);
					}
					else if (n >= nMaxValue)
					{
						char rgch1[20];
						char rgch2[20];
						itoa(n, rgch1, 10);
						itoa(nMaxValue - 1, rgch2, 10);
						g_errorList.AddError(4145, pexp,
							"Value of glyph attribute ",
							m_vpsymGlyphAttrs[iAttrID]->FullName(),
							" for glyph 0x",
							GdlGlyphDefn::GlyphIDString(wGlyphID),
							" = ", rgch1,
							"; maximum is ",
							rgch2);
					}
					else if (n == 0 && m_vpsymGlyphAttrs[iAttrID]->LastFieldIs("gpoint"))
					{
						//	Replace gpoint = 0 with a special value, since we use zero to
						//	indicate "no legitimate value."
						pexp->SetSpecialZero();
					}
				}
			}
		}

		// Just in case, since incrementing 0xFFFF will produce zero.
		if (wGlyphID == 0xFFFF)
			break;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the minimum and maximum values for the given attribute (actually this is the
	minimum - 1 and the maximum + 1).
----------------------------------------------------------------------------------------------*/
void GrcManager::MinAndMaxGlyphAttrValues(int nAttrID,
	int cJLevels, int nAttrIdJStr, int nAttrIdJShr, int nAttrIdJStep, int nAttrIdJWeight,
	int * pnMin, int * pnMax)
{
	*pnMin = kMinGlyphAttrValue;
	*pnMax = kMaxGlyphAttrValue;
	if (nAttrIdJStr <= nAttrID && nAttrID < nAttrIdJStr + cJLevels)
	{
		//	justify.stretch
		*pnMin = -1;
		*pnMax = 0x40000000;
	}
	else if (nAttrIdJShr <= nAttrID && nAttrID < nAttrIdJShr + cJLevels)
	{
		//	justify.shrink
		*pnMin = -1;
	}
	else if (nAttrIdJStep <= nAttrID && nAttrID < nAttrIdJStep + cJLevels)
	{
		//	justify.step
		*pnMin = -1;
	}
	else if (nAttrIdJWeight <= nAttrID && nAttrID < nAttrIdJWeight + cJLevels)
	{
		//	justify.weight
		*pnMin = 0;
		*pnMax = 255;
	}
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	For each pseudo glyph in the system, store the associated actual glyph ID as a glyph
	attribute, specifically as the value of the glyph attribute "*actualForPseudo*.
----------------------------------------------------------------------------------------------*/
bool GrcManager::StorePseudoToActualAsGlyphAttr()
{
	Symbol psym = m_psymtbl->FindSymbol("*actualForPseudo*");
	Assert(psym);
	int nAttrID = psym->InternalID();

	m_prndr->StorePseudoToActualAsGlyphAttr(m_pgax, nAttrID, m_vpexpModified);

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::StorePseudoToActualAsGlyphAttr(GrcGlyphAttrMatrix * pgax, int nAttrID,
	Vector<GdlExpression *> & vpexpExtra)
{
	for (int iglfc = 0; iglfc < m_vpglfc.Size(); iglfc++)
		m_vpglfc[iglfc]->StorePseudoToActualAsGlyphAttr(pgax, nAttrID, vpexpExtra);
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::StorePseudoToActualAsGlyphAttr(GrcGlyphAttrMatrix * pgax, int nAttrID,
	Vector<GdlExpression *> & vpexpExtra)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
		m_vpglfdMembers[iglfd]->StorePseudoToActualAsGlyphAttr(pgax, nAttrID,vpexpExtra);
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::StorePseudoToActualAsGlyphAttr(GrcGlyphAttrMatrix * pgax, int nAttrID,
	Vector<GdlExpression *> & vpexpExtra)
{
	if (m_glft == kglftPseudo && m_pglfOutput && m_pglfOutput->m_vwGlyphIDs.Size() > 0)
	{
		utf16 wOutput = m_pglfOutput->m_vwGlyphIDs[0];
		GdlExpression * pexp = new GdlNumericExpression(wOutput);
		vpexpExtra.Push(pexp);
		pgax->Set(m_wPseudo, nAttrID, pexp, 0, 0, true, false, GrpLineAndFile(0, 0, ""));
	}
}

/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the first glyph in the class. Also return a flag indicating if there was more
	than one.
----------------------------------------------------------------------------------------------*/
unsigned int GdlGlyphClassDefn::FirstGlyphInClass(bool * pfMoreThanOne)
{
	if (m_vpglfdMembers.Size() == 0)
		return 0;
	int n = 0;
	int iglfd;
	for (iglfd = 0; n == 0 && iglfd < m_vpglfdMembers.Size(); iglfd++)
		n = m_vpglfdMembers[iglfd]->FirstGlyphInClass(pfMoreThanOne);
	if (m_vpglfdMembers.Size() > 1)
		*pfMoreThanOne = true;
	return n;
}

/*--------------------------------------------------------------------------------------------*/
unsigned int GdlGlyphDefn::FirstGlyphInClass(bool * pfMoreThanOne)
{
	// This could be more accurate (for instance, it won't exactly handle a class with all
	// bad glyphs except for one good one), but the more-than-one flag is just there for the
	// sake of giving a warning, so this is good enough.
	if (m_vwGlyphIDs.Size() > 1)
		*pfMoreThanOne = true;
	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		if (m_vwGlyphIDs[iw] == kBadGlyph)
			continue;
		return m_vwGlyphIDs[iw];
	}
	return 0; // pathological?
}
