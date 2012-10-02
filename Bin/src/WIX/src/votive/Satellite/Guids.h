/***************************************************************************
		 Copyright (c) Microsoft Corporation, All rights reserved.
	This code sample is provided "AS IS" without warranty of any kind,
	it is not recommended for use in a production environment.
***************************************************************************/

// Guids.h
//

// do not use #pragma once - used by ctc compiler
#ifndef __GUIDS_H_
#define __GUIDS_H_

#ifndef _CTC_GUIDS_


// guidPersistanceSlot ID for our Tool Window
// {36820bf5-c746-4259-8fb3-29fbbf5ac87f}
DEFINE_GUID(GUID_guidPersistanceSlot,
0x36820BF5, 0xC746, 0x4259, 0x8F, 0xB3, 0x29, 0xFB, 0xBF, 0x5A, 0xC8, 0x7F);

#define guidWixVsPkgPkg   CLSID_WixVsPkgPackage

// Command set guid for our commands (used with IOleCommandTarget)
// {8efbe3d5-0b13-4aad-9da7-70fa45589aae}
DEFINE_GUID(guidWixVsPkgCmdSet,
0x8EFBE3D5, 0xB13, 0x4AAD, 0x9D, 0xA7, 0x70, 0xFA, 0x45, 0x58, 0x9A, 0xAE);

#else  // _CTC_GUIDS

#define guidWixVsPkgPkg      { 0xB0AB1F0F, 0x7B08, 0x47FD, { 0x8E, 0x7C, 0xA5, 0xC0, 0xEC, 0x85, 0x55, 0x68 } }
//#define guidWixVsPkgPkg      { 0xB739BB7F, 0x68B6, 0x4CA1, { 0xB2, 0xF0, 0x84, 0xFB, 0x39, 0x10, 0x9C, 0x6E } }
#define guidWixVsPkgCmdSet	  { 0x8EFBE3D5, 0xB13, 0x4AAD, { 0x9D, 0xA7, 0x70, 0xFA, 0x45, 0x58, 0x9A, 0xAE } }

#endif // _CTC_GUIDS_

#endif // __GUIDS_H_
