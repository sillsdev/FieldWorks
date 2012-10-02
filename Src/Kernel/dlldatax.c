// wrapper for dlldata.c

#ifdef _MERGE_PROXYSTUB // merge proxy stub DLL

#include "proxystub.c" // in Generic

#include "FwKernelPs_d.c" // dlldata.c
#include "FwKernelPs_p.c"
#include "FwKernelPs_i.c"

#endif //_MERGE_PROXYSTUB
