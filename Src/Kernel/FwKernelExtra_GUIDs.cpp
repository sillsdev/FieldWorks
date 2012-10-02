/*
 *	$Id$
 *
 *	This (handmade) file contains GUIDs that are defined in header files
 *	using VC++ __declspec, ie not in IDL
 *
 *	Neil Mayhew - 2007-11-22
 */

#include <COM.h>

class ITsStringRaw;
class ITsTextPropsRaw;

template<> const GUID __uuidof(ITsStringRaw)("2AC0CB90-B14B-11d2-B81D-00400541F9DA");
template<> const GUID __uuidof(ITsTextPropsRaw)("31BDA5F0-D286-11d3-9BBC-00400541F9E9");
