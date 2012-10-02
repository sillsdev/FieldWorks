/*DO NOT EDIT
  this file is included from perllib.c to init static extensions */
#ifdef STATIC1
	"Win32CORE",
#undef STATIC1
#endif
#ifdef STATIC2
	EXTERN_C void boot_Win32CORE (pTHX_ CV* cv);
#undef STATIC2
#endif
#ifdef STATIC3
	newXS("Win32CORE::bootstrap", boot_Win32CORE, file);
#undef STATIC3
#endif
