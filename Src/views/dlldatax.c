// wrapper for dlldata.c

#ifdef _MERGE_PROXYSTUB // merge proxy stub DLL

#include "proxystub.c" // in Generic

#include "ViewsPs_d.c" // dlldata.c
#include "ViewsPs_p.c"
#include "ViewsPs_i.c"

#endif //_MERGE_PROXYSTUB
