/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
