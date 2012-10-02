/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TssWidgets.h
Responsibility: Ken Zook
Last reviewed:

	Main header file for the TSS Widgets.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TSSWIDGETS_H
#define TSSWIDGETS_H 1

#include "Common.h"

//JohnT added these as AfCore now includes AfTagOverlay which needs some view stuff
#include "FwKernelTlb.h"
//#include "LanguageTlb.h"	// subsumed by FwKernelTlb.h
#include "ViewsTlb.h"

#include "AfCore.h"

#include "TssListBox.h"
#include "TssEdit.h"
#include "TssListView.h"
#include "TssTreeView.h"
#include "TssCombo.h"

#include ".\res\NetworkTreeViewRes.h"
#include "NetworkTreeView.h"

#endif // !TSSWIDGETS_H
