#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    Precompiled header for Public Scheduling CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <windows.h>
#include <msiquery.h>
#include <strsafe.h>
#include <comadmin.h>
#include <ntsecapi.h>
#include <aclapi.h>
#include <mq.h>

#include "wcautil.h"
#include "memutil.h"
#include "strutil.h"

#include "..\inc\pcacost.h"
#include "..\inc\pcaerr.h"
#include "pcautilexec.h"
#include "cpiutilexec.h"
#include "cpipartexec.h"
#include "cpipartroleexec.h"
#include "cpiappexec.h"
#include "cpiapproleexec.h"
#include "cpiasmexec.h"
#include "cpisubsexec.h"
#include "mqiexec.h"
