#ifndef STRING16_H
#define STRING16_H

#include "charset.h"
#include <stddef.h>		/* for size_t */
#include <string.h>		/* for the usual 8-bit versions */

/* String functions */

STD_API char8 *strdup8(const char8 *s);
#define strchr8(s, c) strchr((s), c)
#define strlen8(s) strlen((s))
#define strcmp8(s1, s2) strcmp((s1), (s2))
#define strncmp8(s1, s2, n) strncmp((s1), (s2), (n))
#define strcpy8(s1, s2) strcpy((s1), (s2))
#define strncpy8(s1, s2, n) strncpy((s1), (s2), (n))

#define strcat8(s1, s2) strcat((s1), (s2))
#define strncat8(s1, s2, n) strncat((s1), (s2), (n))
STD_API int strcasecmp8(const char8 *, const char8 *);
STD_API int strncasecmp8(const char8 *, const char8 *, size_t);
#define strstr8(s1, s2) strstr(s1, s2)

STD_API char16 *strdup16(const char16 *s);
STD_API char16 *strchr16(const char16 *, int);
STD_API size_t strlen16(const char16 *);
STD_API int strcmp16(const char16 *, const char16 *);
STD_API int strncmp16(const char16 *, const char16 *, size_t);
STD_API char16 *strcpy16(char16 *, const char16 *);
STD_API char16 *strncpy16(char16 *, const char16 *, size_t);
STD_API char16 *strcat16(char16 *, const char16 *);
STD_API char16 *strncat16(char16 *, const char16 *, size_t);
STD_API int strcasecmp16(const char16 *, const char16 *);
STD_API int strncasecmp16(const char16 *, const char16 *, size_t);
STD_API char16 *strstr16(const char16 *, const char16 *);

STD_API void translate_latin1_utf16(const char8 *from, char16 *to);
STD_API void translate_utf16_latin1(const char16 *from, char8 *to);
STD_API char16 *translate_latin1_utf16_m(const char8 *from, char16 *to);
STD_API char8 *translate_utf16_latin1_m(const char16 *from, char8 *to);

#if CHAR_SIZE == 8
#define strdup_char8_to_Char(s) strdup8(s)
#define strdup_Char_to_char8(s) strdup8(s)
#define char8_to_Char(s, buf) (s)
#define Char_to_char8(s, buf) (s)
#else
#define strdup_char8_to_Char(s) translate_latin1_utf16_m((s), 0)
#define strdup_Char_to_char8(s) translate_utf16_latin1_m((s), 0)
#define char8_to_Char(s, buf) ((buf) = translate_latin1_utf16_m((s), (buf)))
#define Char_to_char8(s, buf) ((buf) = translate_utf16_latin1_m((s), (buf)))
#endif

#if CHAR_SIZE == 8

#define Strdup strdup8
#define Strchr strchr8
#define Strlen strlen8
#define Strcmp strcmp8
#define Strncmp strncmp8
#define Strcpy strcpy8
#define Strncpy strncpy8
#define Strcat strcat8
#define Strncat strncat8
#define Strcasecmp strcasecmp8
#define Strncasecmp strncasecmp8
#define Strstr strstr8

#else

#define Strdup strdup16
#define Strchr strchr16
#define Strlen strlen16
#define Strcmp strcmp16
#define Strncmp strncmp16
#define Strcpy strcpy16
#define Strncpy strncpy16
#define Strcat strcat16
#define Strncat strncat16
#define Strcasecmp strcasecmp16
#define Strncasecmp strncasecmp16
#define Strstr strstr16

#endif

#endif /* STRING16_H */
