/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Enchant.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains one declaration needed by the enchant header.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#include "enchant++.h"


// define the broker instance
enchant::Broker enchant::Broker::m_instance;
