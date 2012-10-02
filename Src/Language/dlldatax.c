// wrapper for dlldata.c

#ifdef _MERGE_PROXYSTUB // merge proxy stub DLL

#include "proxystub.c" // in Generic

#include "LanguagePs_d.c" // dlldata.c
#include "LanguagePs_p.c"
#include "LanguagePs_i.c"

#endif //_MERGE_PROXYSTUB
