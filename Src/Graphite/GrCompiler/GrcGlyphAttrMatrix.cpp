/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcGlyphAttrMatrix.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implementation of classes that hold the glyph attribute assignments and ligature
	component assignments.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods: GrcGlyphAttrMatrix
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Get the attribute setter information for the given glyph and attribute.
----------------------------------------------------------------------------------------------*/
void GrcGlyphAttrMatrix::Get(utf16 wGlyphID, int nAttrID, int nStyle,
	GdlExpression ** ppexp, int * pnPR, int * pmPrUnits, bool * pfOverride, bool * pfShadow,
	GrpLineAndFile * plnf)
{
	GrcAssignment * pasgnx = m_prgasgnx + Index(wGlyphID, nAttrID, nStyle);
	*ppexp = pasgnx->m_pexp;
	*pnPR = pasgnx->m_nPointRadius;
	*pmPrUnits = pasgnx->m_mPrUnits;
	*pfOverride = pasgnx->m_fOverride;
	*pfShadow = pasgnx->m_fShadow;
	*plnf = pasgnx->m_lnf;
}


/*----------------------------------------------------------------------------------------------
	Get the expression for the given glyph and attribute.
----------------------------------------------------------------------------------------------*/
GdlExpression * GrcGlyphAttrMatrix::GetExpression(utf16 wGlyphID, int nAttrID, int nStyle)
{
	GrcAssignment * pasgnx = m_prgasgnx + Index(wGlyphID, nAttrID, nStyle);
	return pasgnx->m_pexp;
}


/*----------------------------------------------------------------------------------------------
	Set the attribute setter information for the given glyph and attribute.
----------------------------------------------------------------------------------------------*/
void GrcGlyphAttrMatrix::Set(utf16 wGlyphID, int nAttrID, int nStyle,
	GdlExpression * pexp, int nPR, int mPrUnits, bool fOverride, bool fShadow,
	GrpLineAndFile const& lnf)
{
	GrcAssignment * pasgnx = m_prgasgnx + Index(wGlyphID, nAttrID, nStyle);

	pasgnx->m_pexp = pexp;
	pasgnx->m_nPointRadius = nPR;
	pasgnx->m_mPrUnits = mPrUnits;
	pasgnx->m_fOverride = fOverride;
	pasgnx->m_fShadow = fShadow;
	pasgnx->m_lnf = lnf;
}


/*----------------------------------------------------------------------------------------------
	Clear a superfluous attribute for the given glyph (eg, gpoint, when x and y are defined).
----------------------------------------------------------------------------------------------*/
void GrcGlyphAttrMatrix::Clear(utf16 wGlyphID, int nAttrID, int nStyle)
{
	GrcAssignment * pasgnx = m_prgasgnx + Index(wGlyphID, nAttrID, nStyle);

	pasgnx->m_pexp = NULL;
}


/*----------------------------------------------------------------------------------------------
	Test whether a 'gpoint' attribute as a legitmate value. Should only be called for
	gpoint attributes.
	Currently not really needed, because we're using 0 as the "unset" value just like all
	the other attributes.
----------------------------------------------------------------------------------------------*/
bool GrcGlyphAttrMatrix::GpointDefined(utf16 wGlyphID, int nAttrID, int nStyle)
{
	return Defined(wGlyphID, nAttrID, nStyle);

//	if (!Defined(wGlyphID, nAttrID, nStyle))
//		return false;
//	GrcAssignment * pasgnx = m_prgasgnx + Index(wGlyphID, nAttrID, nStyle);
//	GdlExpression * pexp = pasgnx->Expression();
//	if (pexp == NULL)
//		return false;
//	int n;
//	if (!pexp->ResolveToInteger(&n, false))
//		return true;
//	if (n == kGpointNotSet)
//		return false;
//	return true;

}


/***********************************************************************************************
	Methods: GrcLigComponentList
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
GrcLigComponentList::GrcLigComponentList(int cvGlyphIDs)
{
	m_prgplcmap = new LigCompMap*[cvGlyphIDs];
	memset(m_prgplcmap, 0, cvGlyphIDs * isizeof(LigCompMap*));
	m_cvGlyphIDs = cvGlyphIDs;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
GrcLigComponentList::~GrcLigComponentList()
{
	for (int i = 0; i < m_cvGlyphIDs; ++i)
	{
		if (m_prgplcmap[i])
			delete m_prgplcmap[i];
	}
	delete[] m_prgplcmap;
}


/*----------------------------------------------------------------------------------------------
	Add a ligature component to the glyph's list, if it is not already there.
	Return the internal ID.
----------------------------------------------------------------------------------------------*/
int GrcLigComponentList::AddComponentFor(utf16 wGlyphID, Symbol psymComponent,
	GdlRenderer * prndr)
{
	Assert(psymComponent->IsGeneric());

	int nID = psymComponent->InternalID();

	if (FindComponentFor(wGlyphID, nID)) // creates a LigCompMap if necessary
		// already defined for this ligature
		return nID;

	//	Add the internal ID to the list of components for this glyph.
	LigCompMap * plcmap = m_prgplcmap[wGlyphID];
	plcmap->m_vinIDs.Push(nID);

	int cComp = plcmap->m_vinIDs.Size();
	prndr->SetNumLigComponents(cComp);

	return nID;
}


/*----------------------------------------------------------------------------------------------
	If the ligature component with the given ID has been defined for the given glyph,
	return true. Ensure that a LigCompMap is in place for the given glyph.
----------------------------------------------------------------------------------------------*/
bool GrcLigComponentList::FindComponentFor(utf16 wGlyphID, int nID)
{
	LigCompMap * plcmap = m_prgplcmap[wGlyphID];
	if (!plcmap)
	{
		plcmap = new LigCompMap();
		m_prgplcmap[wGlyphID] = plcmap;
	}

	for (int in = 0; in < plcmap->m_vinIDs.Size(); in++)
	{
		if (plcmap->m_vinIDs[in] == nID)
			return true;
	}

	return false;
}


//	Methods below are OBSOLETE--the defined-components list is now part of the
//	glyph attribute list.

/*----------------------------------------------------------------------------------------------
	Add the given symbol to the global list of defined ligature components, if it is
	not already there. Return the internal ID.
----------------------------------------------------------------------------------------------*/
//int GrcLigComponentList::AddComponent(Symbol psymComponent)
//{
//	Assert(psymComponent->IsGeneric());
//	int nID = FindComponentID(psymComponent);
//	if (nID == -1)
//	{
//		nID = m_vpsymDefinedComponents.Size();
//		m_vpsymDefinedComponents.Push(psymComponent);
//	}
//	return nID;
//}


/*----------------------------------------------------------------------------------------------
	Return the internal ID for the given ligature component, or -1 if it is
	not present (has never been defined for any glyph).
----------------------------------------------------------------------------------------------*/
//int GrcLigComponentList::FindComponentID(Symbol psymComponent)
//{
//	Assert(psymComponent->IsGeneric());
//	for (int inID = 0;
//		inID < m_vpsymDefinedComponents.Size();
//		inID++)
//	{
//		if (m_vpsymDefinedComponents[inID] == psymComponent)
//			return inID;
//	}
//	return -1;
//}
