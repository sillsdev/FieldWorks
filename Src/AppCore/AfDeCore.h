/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFieldEditor.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This brings together all of the data entry classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDE_CORE_INCLUDED
#define AFDE_CORE_INCLUDED 1

#define kwidDwTree 1000

class AfDeSplitChild;
typedef GenSmartPtr<AfDeSplitChild> AfDeSplitChildPtr;
class AfDeRecSplitChild;
typedef GenSmartPtr<AfDeRecSplitChild> AfDeRecSplitChildPtr;
class AfDeFeNode;
typedef GenSmartPtr<AfDeFeNode> AfDeFeNodePtr;
class AfDeFeTreeNode;
typedef GenSmartPtr<AfDeFeTreeNode> AfDeFeTreeNodePtr;
class AfDeFeVectorNode;
typedef GenSmartPtr<AfDeFeVectorNode> AfDeFeVectorNodePtr;
class AfDeFeUni;
typedef GenSmartPtr<AfDeFeUni> AfDeFeUniPtr;
class AfDeFeString;
typedef GenSmartPtr<AfDeFeString> AfDeFeStringPtr;
class AfDeFeComboBox;
typedef GenSmartPtr<AfDeFeComboBox> AfDeFeComboBoxPtr;
class AfDeFeEdBoxBut;
typedef GenSmartPtr<AfDeFeEdBoxBut> AfDeFeEdBoxButPtr;
class AfDeFeCliRef;
typedef GenSmartPtr<AfDeFeCliRef> AfDeFeCliRefPtr;
class AfDeFeGenDate;
typedef GenSmartPtr<AfDeFeGenDate> AfDeFeGenDatePtr;
class AfDeFieldEditor;
typedef GenSmartPtr<AfDeFieldEditor> AfDeFieldEditorPtr;
class AfDeFeTime;
typedef GenSmartPtr<AfDeFeTime> AfDeFeTimePtr;
class AfDeFeInt;
typedef GenSmartPtr<AfDeFeInt> AfDeFeIntPtr;
class AfDocSplitChild;
typedef GenSmartPtr<AfDocSplitChild> AfDocSplitChildPtr;
class VwCustDocVc;
typedef GenSmartPtr<VwCustDocVc> VwCustDocVcPtr;
class VwCustBrowseVc;
typedef GenSmartPtr<VwCustBrowseVc> VwCustBrowseVcPtr;


// The state of a field editor may be one of these.
enum DeTreeState
{
	kdtsFixed,
	kdtsExpanded,
	kdtsCollapsed,

	kdtsLim
};


// Constant values for data entry screens.
enum
{
	// This is the width around the tree border where the mouse turns to a drag icon.
	kdxpActiveTreeBorder = 3,
	kdxpMinTreeWidth = 3, // Minimum size for tree view.
	kdxpMinDataWidth = 120, // Minimum size for data view.
	kdxpRtTreeGap = 3, // Gap between text and the right tree border.
	kdxpIconWid = 5, // Width of the minus or plus sign.
	kdzpIconGap = 1, // Width/height of gap between icon and box.
	kdypIconHeight = kdxpIconWid, // Height of plus sign.
	kdxpBoxWid = kdxpIconWid + 2 * kdzpIconGap + 2, // Width of the control box.
	kdypBoxHeight = kdxpBoxWid, // Try making the box square.
	kdxpTextGap = 2, // From line to label text.
	kdxpShortLineLen = 7, // Length of line from box to text.
	kdxpIndDist = kdxpBoxWid + kdxpShortLineLen + kdxpTextGap,
	kdypBoxCtr = kdypBoxHeight / 2, // Location for horizontal line of minus/plus/line
	kdxpBoxCtr = kdxpBoxWid / 2, // Location of vertical line of plus/line
	kdxpLongLineLen = kdxpBoxCtr + kdxpShortLineLen, // Horz. line to text w/o box.
	kdxpLeftMargin = 2, // Gap at the far left of everything.
};


typedef Vector<AfDeFieldEditor *> DeEditorVec;


/***********************************************************************************************
	Data entry headers. (Including these Af headers in AfCore.h causes circularity problems
	with TssTreeView being unrecognized by DeTreeView, since TssWidgets.h calls AfCore.h.)
***********************************************************************************************/
#include "TssWidgets.h"
#include "AfDeFieldEditor.h"
#include "AfDeFeTreeNode.h"
#include "AfDeFeUni.h"
#include "AfDeFeEdBoxBut.h"
#include "AfDeFeCliRef.h"
#include "AfDeFeGenDate.h"
#include "AfDeSplitChild.h"
#include "AfDeFeComboBox.h" // After AfDeSplitChild.h
#include "AfDeFeVw.h"
#include "VwBaseVc.h" // Before AfDeFeTags.h
#include "VwCustomVc.h"
#include "AfBrowseSplitChild.h"
#include "AfDocSplitChild.h"
#include "AfDeFeString.h" // After AfDeFeVw.h and VwBaseVc.h
#include "AfDeFeTags.h"
#include "AfDeFeRefs.h"
#include "StVc.h"
#include "AfDeFeSt.h"
#include "AfDeFeTime.h"
#include "AfDeFeInt.h"

#endif // AFDE_CORE_INCLUDED
