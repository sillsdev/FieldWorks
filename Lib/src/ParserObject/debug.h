/*----------------------------------------------------------------------------------------------
File: debug.h
Responsibility: various.
Last reviewed: Not yet.

Owner: Summer Institute of Linguistics, 7500 West Camp Wisdom Road, Dallas,
Texas 75237. (972)708-7400.

Notice: All rights reserved Worldwide. This material contains the valuable properties of the
Summer Institute of Linguistics of Dallas, Texas, United States of America, embodying
substantial creative efforts and confidential information, ideas and expressions, NO PART of
which may be reproduced or transmitted in any form or by any means, electronic, mechanical, or
otherwise, including photographic and recording or in connection with any information storage or
retrieval system without the permission in writing from the Summer Institute of Linguistics.
COPYRIGHT (C) 1998 by the Summer Institute of Linguistics. All rights reserved.

Description : Helpful declarations and definitions for debuging
----------------------------------------------------------------------------------------------*/
/*************************************************************************************
	Assert and debug definitions.
*************************************************************************************/
#ifdef _DEBUG
	#undef DEBUG
	#define DEBUG 1
	#undef NDEBUG
	#define DEFINE_THIS_FILE static char THIS_FILE[] = __FILE__;
	#undef THIS_FILE
	#define THIS_FILE __FILE__

#else  //!_DEBUG
	#undef DEBUG
	#undef NDEBUG
	#define NDEBUG 1
	#define DEFINE_THIS_FILE

	#define Debug(foo)
#endif //!_DEBUG

#include <assert.h>

#ifdef DEBUG
	#undef assert
	#define assert(exp) (void)((exp) || (_assert(#exp, THIS_FILE, __LINE__), 0))
	#ifdef DEBUG_NEW
		#undef new
		#define new DEBUG_NEW
	#endif
	#define Debug(exp) exp
#else
	#undef assert
	#define assert(exp)
	#define Debug(exp)
#endif //DEBUG

#define Assert(exp) assert(exp)
#define AssertPtr(pv) Assert(!::IsBadReadPtr((pv), sizeof(*(pv))))
#define AssertPtrN(pv) Assert(!(pv) || !::IsBadReadPtr((pv), sizeof(*(pv))))
#define AssertArray(pv, cv) Assert(cv >= 0 && !::IsBadReadPtr((pv), sizeof(*(pv)) * cv))

#define AssertPsz(psz) Assert(ValidPsz(psz))
#define AssertPszN(psz) Assert(!(psz) || ValidPsz(psz))

#define AssertObj(pv) Assert(!::IsBadReadPtr((pv), sizeof(*(pv))) && (pv)->AssertValid())
#define AssertObjN(pv) Assert(!(pv) || !::IsBadReadPtr((pv), sizeof(*(pv))) && (pv)->AssertValid())
