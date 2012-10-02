//COPYRIGHT AND PERMISSION NOTICE
//
//Copyright (c) 1995-2001 International Business Machines Corporation and others
//All rights reserved.
//
//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the
//"Software"), to deal in the Software without restriction, including
//without limitation the rights to use, copy, modify, merge, publish,
//distribute, and/or sell copies of the Software, and to permit persons
//to whom the Software is furnished to do so, provided that the above
////copyright notice(s) and this permission notice appear in all copies of
//the Software and that both the above copyright notice(s) and this
//permission notice appear in supporting documentation.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
//OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
//HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL
//INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING
//FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
//NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
//WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//
//Except as contained in this notice, the name of a copyright holder
//shall not be used in advertising or otherwise to promote the sale, use
//or other dealings in this Software without prior written authorization
//of the copyright holder.
//
//--------------------------------------------------------------------------------

#pragma warning(disable: 4512)

#include <unicode/brkiter.h>
#include <unicode/calendar.h>
#include <unicode/caniter.h>
#include <unicode/chariter.h>
#include <unicode/choicfmt.h>
#include <unicode/coleitr.h>
#include <unicode/coll.h>
#include <unicode/datefmt.h>
#include <unicode/dbbi.h>
#include <unicode/dcfmtsym.h>
#include <unicode/decimfmt.h>
#include <unicode/dtfmtsym.h>
#include <unicode/fieldpos.h>
#include <unicode/fmtable.h>
#include <unicode/format.h>
#include <unicode/gregocal.h>
#include <unicode/locid.h>
#include <unicode/msgfmt.h>
#include <unicode/normlzr.h>
#include <unicode/numfmt.h>
#include <unicode/parseerr.h>
#include <unicode/parsepos.h>
#include <unicode/putil.h>
#if WIN32
#include <unicode/pwin32.h>
#else // WIN32
#include <unicode/platform.h>
#endif // WIN32
#include <unicode/rbbi.h>
#include <unicode/rbnf.h>
#include <unicode/regex.h>
#include <unicode/rep.h>
#include <unicode/resbund.h>
#include <unicode/schriter.h>
#include <unicode/search.h>
#include <unicode/simpletz.h>
#include <unicode/smpdtfmt.h>
#include <unicode/sortkey.h>
#include <unicode/strenum.h>
#include <unicode/stsearch.h>
#include <unicode/tblcoll.h>
#include <unicode/timezone.h>
#include <unicode/translit.h>
#include <unicode/ubidi.h>
#include <unicode/ubrk.h>
#include <unicode/ucal.h>
#include <unicode/ucat.h>
#include <unicode/uchar.h>
#include <unicode/uchriter.h>
#include <unicode/uclean.h>
#include <unicode/ucnv.h>
#include <unicode/ucnv_cb.h>
#include <unicode/ucnv_err.h>
#include <unicode/ucol.h>
#include <unicode/ucoleitr.h>
#include <unicode/uconfig.h>
#include <unicode/ucurr.h>
#include <unicode/udat.h>
#include <unicode/udata.h>
#include <unicode/uenum.h>
#include <unicode/uidna.h>
#include <unicode/uiter.h>
#include <unicode/uloc.h>
#include <unicode/umachine.h>
#include <unicode/umisc.h>
#include <unicode/umsg.h>
#include <unicode/unifilt.h>
#if U_ICU_VERSION_MAJOR_NUM<2 || (U_ICU_VERSION_MAJOR_NUM==2 && U_ICU_VERSION_MINOR_NUM<8)
#include <unicode/unifltlg.h>
#endif
#include <unicode/unifunct.h>
#include <unicode/unimatch.h>
#include <unicode/unirepl.h>
#include <unicode/uniset.h>
#include <unicode/unistr.h>
#include <unicode/unorm.h>
#include <unicode/unum.h>
#include <unicode/uobject.h>
#include <unicode/urename.h>
#include <unicode/urep.h>
#include <unicode/ures.h>
#include <unicode/uscript.h>
#include <unicode/usearch.h>
#include <unicode/uset.h>
#include <unicode/usetiter.h>
#include <unicode/ushape.h>
#include <unicode/ustring.h>
#include <unicode/utf.h>
#include <unicode/utf16.h>
#include <unicode/utf32.h>
#include <unicode/utf8.h>
#include <unicode/utf_old.h>
#include <unicode/utrans.h>
#include <unicode/utypes.h>
#include <unicode/uversion.h>
#if U_ICU_VERSION_MAJOR_NUM>2 || (U_ICU_VERSION_MAJOR_NUM==2 && U_ICU_VERSION_MINOR_NUM>=8)
// The following 4 files are new with version 2.8 of ICU.
#include <unicode/symtable.h>
#include <unicode/ulocdata.h>
#include <unicode/usprep.h>
#include <unicode/utrace.h>

// The following 15 files are new with version 3.4 of ICU (or since version 2.8).
#include <unicode/curramt.h>
#include <unicode/currunit.h>
#include <unicode/docmain.h>
#include <unicode/measfmt.h>
#include <unicode/measunit.h>
#include <unicode/measure.h>
#include <unicode/ucasemap.h>
#include <unicode/udeprctd.h>
#include <unicode/udraft.h>
#include <unicode/uobslete.h>
#include <unicode/uregex.h>
#include <unicode/ustdio.h>
#include <unicode/ustream.h>
#include <unicode/utext.h>
#include <unicode/utmscale.h>
#endif

#if U_ICU_VERSION_MAJOR_NUM>3 || (U_ICU_VERSION_MAJOR_NUM==3 && U_ICU_VERSION_MINOR_NUM>=6)
// The following 3 files are new with version 3.6 of ICU.
#include <unicode/ucsdet.h>
#include <unicode/uintrnal.h>
#include <unicode/usystem.h>
#endif
// The following 10 files are new with version 4.0 of ICU (or since version 3.6)
#include "unicode/basictz.h"
#include "unicode/dtptngen.h"
#include "unicode/dtrule.h"
#include "unicode/plurfmt.h"
#include "unicode/plurrule.h"
#include "unicode/rbtz.h"
#include "unicode/tzrule.h"
#include "unicode/tztrans.h"
#include "unicode/udatpg.h"
#include "unicode/vtzone.h"

#pragma warning(default: 4512)
