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
#define AssertPtrSize(pv, cv) Assert(cv >= 0 && !::IsBadReadPtr((pv), sizeof(*(pv)) * cv))
#define AssertPsz(psz) Assert(!::IsBadStringPtr(psz, 0x10000000))
#define AssertPszN(psz) Assert(!(psz) || !::IsBadStringPtr(psz, 0x10000000))
