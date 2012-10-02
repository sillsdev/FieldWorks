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
//    dutil precompiled header.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <windows.h>
#include <strsafe.h>
#include <wininet.h>

#include "dutil.h"
#include "aclutil.h"
#include "cabcutil.h"
#include "cabutil.h"
#include "dirutil.h"
#include "fileutil.h"
#include "memutil.h"  // NOTE: almost everying is inlined so there is a small .cpp file
//#include "metautil.h" - see metautil.cpp why this *must* be commented out
#include "perfutil.h"
//#include "sqlutil.h" - see sqlutil.cpp why this *must* be commented out
#include "strutil.h"
#include <comutil.h>  // This header is needed for msxml2.h to compile correctly
#include <msxml2.h>   // This file is needed to include xmlutil.h
#include "xmlutil.h"
