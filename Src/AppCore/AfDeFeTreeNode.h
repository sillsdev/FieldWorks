/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeTreeNode.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This file contains class declarations for the following classes:
		AfDeFeNode : AfDeFieldEditor - base class for tree node editors.
			AfDeFeTreeNode : AfDeFeNode - Provides for nested field editors.
			AfDeFeVectorNode : AfDeFeNode - Provides for nested field editors
				for sequences or collections.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_TREENODE_INCLUDED
#define AFDEFE_TREENODE_INCLUDED 1


/*----------------------------------------------------------------------------------------------
	This node is used to provide common behavior for tree structures in data entry editors.

	@h3{Hungarian: den}
----------------------------------------------------------------------------------------------*/
class AfDeFeNode : public AfDeFieldEditor
{
public:
	typedef AfDeFieldEditor SuperClass;

	AfDeFeNode();
	~AfDeFeNode();

	virtual void UpdateField();

	virtual void Init();
	void Draw(HDC hdc, const Rect & rcpClip);
	int SetHeightAt(int dxpWidth);

	OutlineNumSty GetOutlineSty()
	{
		return m_qfsp->m_ons;
	}

	DeTreeState GetExpansion()
	{
		return m_dtsExpanded;
	}

	void SetExpansion(DeTreeState dts)
	{
		Assert(dts == kdtsExpanded || dts == kdtsCollapsed);
		m_dtsExpanded = dts;
	}

	void SetContents(ITsString * ptss)
	{
		AssertPtr(ptss);
		m_qtss = ptss;
	}

	void GetContents(ITsString ** pptss)
	{
		AssertPtr(pptss);
		*pptss = m_qtss;
		AddRefObj(*pptss);
	}

	HVO GetTreeObj()
	{
		return m_hvoTree;
	}


	void SetTreeObj(HVO hvo)
	{
		m_hvoTree = hvo;
	}

protected:
	DeTreeState m_dtsExpanded; // The current state of the tree node.
	ITsStringPtr m_qtss; // The text to display for this field.
	HVO m_hvoTree; // The object id for the top object in the tree.
};


/*----------------------------------------------------------------------------------------------
	This node is used to provide tree structures in data entry editors.

	@h3{Hungarian: detn}
----------------------------------------------------------------------------------------------*/
class AfDeFeTreeNode : public AfDeFeNode
{
public:
	typedef AfDeFeNode SuperClass;

	AfDeFeTreeNode();
	~AfDeFeTreeNode();

	int GetClsid()
	{
		return m_clsid;
	}

	void SetClsid(int clsid)
	{
		m_clsid = clsid;
	}

	AfDeFeTreeNode * CloneNode(AfDeSplitChild * padsc, int nInd);

protected:
	int m_clsid; // Class of the object we are holding.
};


/*----------------------------------------------------------------------------------------------
	This class implements AfDeFeVectorNode

	@h3{Hungarian: devn}
----------------------------------------------------------------------------------------------*/
class AfDeFeVectorNode : public AfDeFeNode
{
public:
	typedef AfDeFeNode SuperClass;

	AfDeFeVectorNode();
	virtual ~AfDeFeVectorNode();

protected:
};

#endif // AFDEFE_TREENODE_INCLUDED
