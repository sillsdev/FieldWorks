/*
 *	$Id$
 *
 *	This (handmade) file contains GUIDs that are defined in header files
 *	using VC++ __declspec, ie not in IDL
 *
 *	Neil Mayhew - 2007-11-22
 */

#include <COM.h>

class VwEnv;
class VwTextSelection;
class VwPictureSelection;

template<> const GUID __uuidof(VwEnv)("A4C6D3CC-12A7-49fb-A838-51E7970C1C3D");
template<> const GUID __uuidof(VwTextSelection)("102AACD1-AE7E-11d3-9BAF-00400541F9E9");
template<> const GUID __uuidof(VwPictureSelection)("6AFD893B-6336-48a8-953A-3A6C2879F721");
