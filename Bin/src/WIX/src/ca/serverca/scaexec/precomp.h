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
//    Precompiled header for Server execution CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <windows.h>
#include <msiquery.h>
#include <strsafe.h>
#include <oleauto.h>

#include <Iads.h>
#include <activeds.h>
#include <lm.h>        // NetApi32.lib
#include <LMaccess.h>
#include <LMErr.h>
#include <Dsgetdc.h>
#include <wincrypt.h>

#include "wcautil.h"
#include "aclutil.h"
#include "dirutil.h"
#include "fileutil.h"
#include "memutil.h"
#include "metautil.h"
#include "perfutil.h"
#include "strutil.h"
#include "sqlutil.h"

#include "CustomMsiErrors.h"
#include "scasmbexec.h"
#include "..\inc\sca.h"
#include "..\inc\scacost.h"
