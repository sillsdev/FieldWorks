/*
 * Hand tailored config.h for windows. Adapted from Enchant's config.h.Win32.
 */

/* define ssize_t to int if <sys/types.h> doesn't define.*/
#ifndef ssize_t
#ifndef __ssize_t_defined
typedef int ssize_t;
#endif
#endif
/* #undef ssize_t */

#if defined(_MSC_VER)
#pragma warning(disable: 4996) /* The POSIX name for this item is deprecated. Instead, use the ISO C++ conformant name. */
#endif
