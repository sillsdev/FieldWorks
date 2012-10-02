/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeTreeNode.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides the base for all data entry field editors.

	This file contains class definitions for the following classes:
		AfDeFeNode : AfDeFieldEditor
			AfDeFeTreeNode : AfDeFeNode
			AfDeFeVectorNode : AfDeFeNode
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	AfDeFeNode Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeNode::AfDeFeNode()
{
	m_dtsExpanded = kdtsCollapsed;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeNode::~AfDeFeNode()
{
}


/*----------------------------------------------------------------------------------------------
	Initialize font after superclass initialization is done.
----------------------------------------------------------------------------------------------*/
void AfDeFeNode::Init()
{
	Assert(m_hvoObj); // Initialize should have been called first.

	CreateFont();
}


/*----------------------------------------------------------------------------------------------
	Draw to the given clip rectangle.
----------------------------------------------------------------------------------------------*/
void AfDeFeNode::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);

	SmartBstr sbstr;
	CheckHr(m_qtss->get_Text(&sbstr));
	StrAppBuf strb(sbstr.Chars());

	COLORREF clrFgOld = AfGfx::SetTextColor(hdc, m_chrp.clrFore);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);

	AfGfx::FillSolidRect(hdc, rcpClip, ::GetSysColor(COLOR_3DFACE));
	COLORREF clrBgOld = ::SetBkColor(hdc, ::GetSysColor(COLOR_3DFACE));
	//HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	::TextOut(hdc, rcpClip.left + 2, rcpClip.top + 1, strb.Chars(), strb.Length());

	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	AfGfx::SetBkColor(hdc, clrBgOld);
	AfGfx::SetTextColor(hdc, clrFgOld);
}


/*----------------------------------------------------------------------------------------------
	Set the height for the specified width, and return it.
----------------------------------------------------------------------------------------------*/
int AfDeFeNode::SetHeightAt(int dxpWidth)
{
	Assert(dxpWidth > 0);
	if (dxpWidth != m_dxpWidth)
	{
		m_dxpWidth = dxpWidth;
		// The height is set when the field is initialized and doesn't change here.
	}
	return m_dypHeight;
}


/*----------------------------------------------------------------------------------------------
	The field has changed, so make sure it is updated.
----------------------------------------------------------------------------------------------*/
void AfDeFeNode::UpdateField()
{
	m_qadsc->SetTreeHeader(this);
}

//:>********************************************************************************************
//:>	AfDeFeTreeNode Methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeTreeNode::AfDeFeTreeNode()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeTreeNode::~AfDeFeTreeNode()
{
}


/*----------------------------------------------------------------------------------------------
	Copy a tree node.

	@param padsc Pointer to the owning splitter window.
	@param nInd Indent level for node.

	@return Pointer to a new AfDeFeTreeNode with the same values as the original.
----------------------------------------------------------------------------------------------*/
AfDeFeTreeNode * AfDeFeTreeNode::CloneNode(AfDeSplitChild * padsc, int nInd)
{
	ITsStringPtr qtssLabel;
	ITsStringPtr qtssHelp;
	GetLabel(&qtssLabel);
	GetHelp(&qtssHelp);
	AfDeFeTreeNode * pdetn = NewObj AfDeFeTreeNode;
	pdetn->Initialize(m_hvoObj, m_flid, nInd, qtssLabel, qtssHelp, padsc, m_qfsp);
	pdetn->Init();
	pdetn->m_clsid = m_clsid;
	pdetn->m_hvoTree = m_hvoTree;
	pdetn->m_qtss = m_qtss;
	pdetn->m_dtsExpanded = kdtsCollapsed;

	return pdetn;
}

//:>********************************************************************************************
//:>	AfDeFeVectorNode Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeVectorNode::AfDeFeVectorNode()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeVectorNode::~AfDeFeVectorNode()
{
}


/*----------------------------------------------------------------------------------------------
	Initialize the class.

	@param hvoObj Id of object we are editing (the object that has m_flid).
	@param flid Id of the field we are editing.
	@param nIndent Level of nesting in the tree. 0 is top.
	@param ptssLabel Pointer to the label to show in the tree for this field.
	@param ptssHelp Pointer to the "What's this" help string associated with this field.
	@param padsc Pointer to the owning AfDeSplitChild window class.
	@param pfsp The FldSpec that defines this field (NULL is acceptable, but not recommended).
	@param ws Primary writing system for field contents.
	@param ows Primary old writing system for field contents.
----------------------------------------------------------------------------------------------*/
/*
void AfDeFeVectorNode::Initialize(int obj, int flid, int nIndent, ITsString * ptssLabel,
		ITsString * ptssHelp, AfDeSplitChild * padsc, FldSpec * pfsp, int ws, int ows)
{
	SuperClass::Initialize(obj, flid, nIndent, ptssLabel, ptssHelp, padsc, pfsp, ws, ows);
	m_hvoOwner = obj;
	m_flidOwner = flid;
}
*/
