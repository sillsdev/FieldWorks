#ifndef STDIO16_H
#define STDIO16_H

#include <stdarg.h>
#include <stdio.h>
#ifdef HAVE_LIBZ
#include "zlib.h"
#endif
#include "charset.h"

typedef struct _FILE16 FILE16;

extern STD_API FILE16 *Stdin, *Stdout, *Stderr;

STD_API FILE16 *MakeFILE16FromFILE(FILE *f, const char *type);
STD_API FILE16 *MakeFILE16FromFD(int fd, const char *type);
STD_API FILE16 *MakeFILE16FromString(void *buf, long size, const char *type);
STD_API FILE16 *MakeStringFILE16(const char *type);
STD_API void *StringFILE16String(FILE16 *file);
#ifdef WIN32
#ifdef SOCKETS_IMPLEMENTED
STD_API FILE16 *MakeFILE16FromWinsock(int sock, const char *type);
#endif
#endif
#ifdef HAVE_LIBZ
STD_API FILE16 *MakeFILE16FromGzip(gzFile file, const char *type);
#endif

STD_API int Readu(FILE16 *file, unsigned char *buf, int max_count);
STD_API int Writeu(FILE16 *file, unsigned char *buf, int count);
STD_API int Fclose(FILE16 *file);
STD_API int Fflush(FILE16 *file);
STD_API int Fseek(FILE16 *file, long offset, int ptrname);

STD_API int Ferror(FILE16 *file);
STD_API int Feof(FILE16 *file);

STD_API FILE *GetFILE(FILE16 *file);
STD_API void SetCloseUnderlying(FILE16 *file, int cu);
STD_API void SetFileEncoding(FILE16 *file, CharacterEncoding encoding);
STD_API CharacterEncoding GetFileEncoding(FILE16 *file);
STD_API void SetNormalizeLineEnd(FILE16 *file, int nle);

STD_API int Fprintf(FILE16 *file, const char *format, ...);
STD_API int Vfprintf(FILE16 *file, const char *format, va_list args);

STD_API int Printf(const char *format, ...);
STD_API int Vprintf(const char *format, va_list args);

STD_API int Sprintf(void *buf, CharacterEncoding enc, const char *format, ...);
STD_API int Snprintf(void *buf, size_t size, CharacterEncoding enc,
			 const char *format, ...);
STD_API int Vsprintf(void *buf, CharacterEncoding enc, const char *format,
		 va_list args);
STD_API int Vsnprintf(void *buf, size_t size, CharacterEncoding enc,
			  const char *format, va_list args);

STD_API int Getu(FILE16 *file);

STD_API int init_stdio16(void);
STD_API void deinit_stdio16(void);

#endif /* STDIO16_H */
