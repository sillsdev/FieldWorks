/*
 * An implementation of printf that
 * - allows printing of 16-bit unicode characters and strings
 * - translates output to a specified character set
 *
 * "char8" is 8 bits and contains ISO-Latin-1 (or ASCII) values
 * "char16" is 16 bits and contains UTF-16 values
 * "Char" is char8 or char16 depending on whether CHAR_SIZE is 8 or 16
 *
 * Author: Richard Tobin
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>

#ifdef FOR_LT

#include "lt-memory.h"
#include "nsl-err.h"

#define ERR(m) LT_ERROR(NECHAR,m)
#define ERR1(m,x) LT_ERROR1(NECHAR,m,x)
#define ERR2(m,x,y) LT_ERROR2(NECHAR,m,x,y)

#define Malloc salloc
#define Realloc srealloc
#define Free sfree

#else

#include "system.h"

#define ERR(m) fprintf(stderr,m)
#define ERR1(m,x) fprintf(stderr,m,x)
#define ERR2(m,x,y) fprintf(stderr,m,x,y)
#endif

#if defined(WIN32) && !defined(__CYGWIN__)
#undef boolean
#include <winsock.h>
#include <fcntl.h>
#include <io.h>
#else
#include <unistd.h>
#endif

#include "charset.h"
#include "string16.h"
#include "stdio16.h"

/* When we return -1 for non-io errors, we set errno to 0 to avoid confusion */
#include <errno.h>

#define BufferSize 4096

static char8 null = 0;

typedef int ReadProc(FILE16 *file, unsigned char *buf, int max_count);
typedef int WriteProc(FILE16 *file, const unsigned char *buf, int count);
typedef int SeekProc(FILE16 *file, long offset, int ptrname);
typedef int FlushProc(FILE16 *file);
typedef int CloseProc(FILE16 *file);

struct _FILE16 {
	void *handle;
	int handle2, handle3;
	ReadProc *read;
	WriteProc *write;
	SeekProc *seek;
	FlushProc *flush;
	CloseProc *close;
	int flags;
	CharacterEncoding enc;
	char16 save;
	/* There are incount unread bytes starting at inbuf[inoffset] */
	unsigned char inbuf[4096];
	int incount, inoffset;
};

#define FILE16_read             0x01
#define FILE16_write            0x02
#define FILE16_close_underlying 0x04
#define FILE16_crlf		0x08
#define FILE16_needs_binary     0x8000
#define FILE16_error		0x4000
#define FILE16_eof		0x2000

static void filbuf(FILE16 *file);

static int FileRead(FILE16 *file, unsigned char *buf, int max_count);
static int FileWrite(FILE16 *file, const unsigned char *buf, int count);
static int FileSeek(FILE16 *file, long offset, int ptrname);
static int FileClose(FILE16 *file);
static int FileFlush(FILE16 *file);

static int FDRead(FILE16 *file, unsigned char *buf, int max_count);
static int FDWrite(FILE16 *file, const unsigned char *buf, int count);
static int FDSeek(FILE16 *file, long offset, int ptrname);
static int FDClose(FILE16 *file);
static int FDFlush(FILE16 *file);

static int StringRead(FILE16 *file, unsigned char *buf, int max_count);
static int StringWrite(FILE16 *file, const unsigned char *buf, int count);
static int StringWriteTrunc(FILE16 *file, const unsigned char *buf, int count);
static int StringSeek(FILE16 *file, long offset, int ptrname);
static int StringClose(FILE16 *file);
static int StringFlush(FILE16 *file);

static int MStringRead(FILE16 *file, unsigned char *buf, int max_count);
static int MStringWrite(FILE16 *file, const unsigned char *buf, int count);
static int MStringSeek(FILE16 *file, long offset, int ptrname);
static int MStringClose(FILE16 *file);
static int MStringFlush(FILE16 *file);

#if defined(WIN32) && ! defined(__CYGWIN__)
#ifdef SOCKETS_IMPLEMENTED
static int WinsockRead(FILE16 *file, unsigned char *buf, int max_count);
static int WinsockWrite(FILE16 *file, const unsigned char *buf, int count);
static int WinsockSeek(FILE16 *file, long offset, int ptrname);
static int WinsockClose(FILE16 *file);
static int WinsockFlush(FILE16 *file);
#endif
#endif

#ifdef HAVE_LIBZ
static int GzipRead(FILE16 *file, unsigned char *buf, int max_count);
static int GzipWrite(FILE16 *file, const unsigned char *buf, int count);
static int GzipSeek(FILE16 *file, long offset, int ptrname);
static int GzipClose(FILE16 *file);
static int GzipFlush(FILE16 *file);
#endif

FILE16 *Stdin,  *Stdout, *Stderr;
static int Stdin_open = 0, Stdout_open = 0, Stderr_open = 0;

int init_stdio16(void) {
	if(!Stdin_open)
	{
	if(!(Stdin = MakeFILE16FromFILE(stdin, "r")))
		return -1;
	SetFileEncoding(Stdin, CE_ISO_8859_1);
	Stdin_open = 1;
	}
	if(!Stdout_open)
	{
	if(!(Stdout = MakeFILE16FromFILE(stdout, "w")))
		return -1;
	SetFileEncoding(Stdout, CE_ISO_8859_1);
	Stdout_open = 1;
	}
	if(!Stderr_open)
	{
	if(!(Stderr = MakeFILE16FromFILE(stderr, "w")))
		return -1;
	SetFileEncoding(Stderr, CE_ISO_8859_1);
	Stderr_open = 1;
	}

	return 0;
}

void deinit_stdio16(void) {
	if(Stdin_open) Fclose(Stdin);
	if(Stdout_open) Fclose(Stdout);
	if(Stderr_open) Fclose(Stderr);
}

/* Return the size (in bytes) of nul in the given encoding */

static int NullSize(CharacterEncoding enc)
{
	switch(enc)
	{
	default:
	return 1;

	case CE_UTF_16B:
	case CE_ISO_10646_UCS_2B:
	case CE_UTF_16L:
	case CE_ISO_10646_UCS_2L:
	return 2;
	}
}

/* Output an ASCII buffer in the specified encoding */

/* In fact, we don't translate the buffer at all if we are outputting
   an 8-bit encoding, and we treat it as Latin-1 is we are outputting
   a 16-bit encoding.  This means that all the various ASCII supersets
   will be passed through unaltered in the usual case, since we don't
   translate them on input either. */

static int ConvertASCII(const char8 *buf, int count, FILE16 *file)
{
	/* It *could* be that every character is linefeed... */
	unsigned char outbuf[BufferSize*4];
	unsigned char c;
	int i, j;

	switch(file->enc)
	{
	case CE_ISO_646:
	case CE_ISO_8859_1:
	case CE_ISO_8859_2:
	case CE_ISO_8859_3:
	case CE_ISO_8859_4:
	case CE_ISO_8859_5:
	case CE_ISO_8859_6:
	case CE_ISO_8859_7:
	case CE_ISO_8859_8:
	case CE_ISO_8859_9:
	case CE_ISO_8859_10:
	case CE_ISO_8859_11:
	case CE_ISO_8859_13:
	case CE_ISO_8859_14:
	case CE_ISO_8859_15:
	case CE_unspecified_ascii_superset:
	if(file->flags & FILE16_crlf)
	{
		for(i=j=0; i<count; i++)
		{
		c = buf[i];
		if(c == '\n')
			outbuf[j++] = '\r';
		outbuf[j++] = c;
		}
		return Writeu(file, outbuf, j);
	}
	else
		return Writeu(file, (unsigned char *)buf, count);

	case CE_UTF_8:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		outbuf[j++] = '\r';
		if(c < 128)
		outbuf[j++] = c;
		else
		{
		outbuf[j++] = 0xc0 + (c >> 6);
		outbuf[j++] = 0x80 + (c & 0x3f);
		}
	}
	return Writeu(file, outbuf, j);

	case CE_UTF_16B:
	case CE_ISO_10646_UCS_2B:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		{
		outbuf[j++] = 0;
		outbuf[j++] = '\r';
		}
		outbuf[j++] = 0;
		outbuf[j++] = c;
	}
	return Writeu(file, outbuf, j);

	case CE_UTF_16L:
	case CE_ISO_10646_UCS_2L:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		{
		outbuf[j++] = '\r';
		outbuf[j++] = 0;
		}
		outbuf[j++] = c;
		outbuf[j++] = 0;
	}
	return Writeu(file, outbuf, j);

	default:
	  ERR2("Bad output character encoding %d (%s)\n",
		file->enc,
		file->enc < CE_enum_count ? CharacterEncodingName[file->enc] :
									"unknown");
	errno = 0;
	return -1;
	}
}

/* Output a UTF-16 buffer in the specified encoding */

static int ConvertUTF16(const char16 *buf, int count, FILE16 *file)
{
	/* It *could* be that every character is linefeed... */
	unsigned char outbuf[BufferSize*4];
	char16 c;
	int i, j, tablenum, max;
	char8 *from_unicode;
	char32 big;

	switch(file->enc)
	{
	case CE_ISO_646:		/* should really check for >127 */
	case CE_ISO_8859_1:
	case CE_unspecified_ascii_superset:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		outbuf[j++] = '\r';
		if(c < 256)
		outbuf[j++] = (unsigned char)c;
		else
		outbuf[j++] = '?';
	}
	return Writeu(file, outbuf, j);

	case CE_ISO_8859_2:
	case CE_ISO_8859_3:
	case CE_ISO_8859_4:
	case CE_ISO_8859_5:
	case CE_ISO_8859_6:
	case CE_ISO_8859_7:
	case CE_ISO_8859_8:
	case CE_ISO_8859_9:
	case CE_ISO_8859_10:
	case CE_ISO_8859_11:
	case CE_ISO_8859_13:
	case CE_ISO_8859_14:
	case CE_ISO_8859_15:
	tablenum = (file->enc - CE_ISO_8859_2);
	max = iso_max_val[tablenum];
	from_unicode = unicode_to_iso[tablenum];
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		outbuf[j++] = '\r';
		if(c <= max)
		outbuf[j++] = (unsigned char)from_unicode[c];
		else
		outbuf[j++] = '?';
	}
	return Writeu(file, outbuf, j);

	case CE_UTF_8:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		outbuf[j++] = '\r';
		if(c < 0x80)
		outbuf[j++] = (unsigned char)c;
		else if(c < 0x800)
		{
		outbuf[j++] = 0xc0 + (c >> 6);
		outbuf[j++] = 0x80 + (c & 0x3f);
		}
		else if(c >= 0xd800 && c <= 0xdbff)
		file->save = c;
		else if(c >= 0xdc00 && c <= 0xdfff)
		{
		big = 0x10000 +
			((file->save - 0xd800) << 10) + (c - 0xdc00);
		outbuf[j++] = 0xf0 + (big >> 18);
		outbuf[j++] = 0x80 + ((big >> 12) & 0x3f);
		outbuf[j++] = 0x80 + ((big >> 6) & 0x3f);
		outbuf[j++] = 0x80 + (big & 0x3f);
		}
		else
		{
		outbuf[j++] = 0xe0 + (c >> 12);
		outbuf[j++] = 0x80 + ((c >> 6) & 0x3f);
		outbuf[j++] = 0x80 + (c & 0x3f);
		}
	}
	return Writeu(file, outbuf, j);

	case CE_UTF_16B:
	case CE_ISO_10646_UCS_2B:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		{
		outbuf[j++] = 0;
		outbuf[j++] = '\r';
		}
		outbuf[j++] = (c >> 8);
		outbuf[j++] = (c & 0xff);

	}
	return Writeu(file, outbuf, j);

	case CE_UTF_16L:
	case CE_ISO_10646_UCS_2L:
	for(i=j=0; i<count; i++)
	{
		c = buf[i];
		if(c == '\n' && (file->flags & FILE16_crlf))
		{
		outbuf[j++] = '\r';
		outbuf[j++] = 0;
		}
		outbuf[j++] = (c & 0xff);
		outbuf[j++] = (c >> 8);

	}
	return Writeu(file, outbuf, j);

	default:
	  ERR2("Bad output character encoding %d (%s)\n",
		file->enc,
		file->enc < CE_enum_count ? CharacterEncodingName[file->enc] :
									"unknown");
	errno = 0;
	return -1;
	}
}

#if 0
int Readu(FILE16 *file, unsigned char *buf, int max_count)
{
	return file->read(file, buf, max_count);
}
#endif

int Writeu(FILE16 *file, unsigned char *buf, int count)
{
	int ret;

	ret = file->write(file, buf, count);
	if(ret < 0)
	file->flags |= FILE16_error;

	return ret;
}

int Fclose(FILE16 *file)
{
	int ret = 0;

	ret = file->close(file);
	Free(file);

	if(file == Stdin)
	Stdin_open = 0;
	else if(file == Stdout)
	Stdout_open = 0;
	else if(file == Stderr)
	Stderr_open = 0;

	return ret;
}

int Fseek(FILE16 *file, long offset, int ptrname)
{
	file->incount = file->inoffset = 0;
	file->flags &= ~(FILE16_error | FILE16_eof);
	return file->seek(file, offset, ptrname);
}

int Fflush(FILE16 *file)
{
	return file->flush(file);
}

int Ferror(FILE16 *file)
{
	return file->flags & FILE16_error;
}

int Feof(FILE16 *file)
{
	return file->flags & FILE16_eof;
}

int Getu(FILE16 *file)
{
	filbuf(file);
	if(file->flags & (FILE16_error | FILE16_eof))
	return EOF;
	file->incount--;
	return file->inbuf[file->inoffset++];
}

int Readu(FILE16 *file, unsigned char *buf, int max_count)
{
	int count=0, lump;

	while(count < max_count)
	{
	filbuf(file);
	if(file->flags & FILE16_error)
		return 0;
	if(file->flags & FILE16_eof)
		break;
	lump = max_count - count;
	if(lump > file->incount)
		lump = file->incount;
	memcpy(buf+count, file->inbuf+file->inoffset, lump);
	count += lump;
	file->inoffset += lump;
	file->incount -= lump;
	}

	return count;
}

/* After calling filbuf, either file->incount > 0 or file->flags has
   FILE16_eof or FILE16_error set. */

static void filbuf(FILE16 *file)
{
	int ret;

	if(file->incount > 0)
	return;

	ret = file->read(file, file->inbuf, sizeof(file->inbuf));
	if(ret < 0)
	file->flags |= FILE16_error;
	else if(ret == 0)
	file->flags |= FILE16_eof;
	else
	{
	file->inoffset = 0;
	file->incount = ret;
	}
}

FILE *GetFILE(FILE16 *file)
{
	if(file->read == FileRead)
	return (FILE *)file->handle;
	else
	return 0;
}

void SetCloseUnderlying(FILE16 *file, int cu)
{
	if(cu)
	file->flags |= FILE16_close_underlying;
	else
	file->flags &= ~FILE16_close_underlying;
}

void SetFileEncoding(FILE16 *file, CharacterEncoding encoding)
{
	file->enc = encoding;
}

CharacterEncoding GetFileEncoding(FILE16 *file)
{
	return file->enc;
}

void SetNormalizeLineEnd(FILE16 *file, int nle)
{
#if defined(WIN32)
	if(nle)
	file->flags |= FILE16_crlf;
	else
	file->flags &= ~FILE16_crlf;
#endif
}

int Fprintf(FILE16 *file, const char *format, ...)
{
	va_list args;
	va_start(args, format);
	return Vfprintf(file, format, args);
}

int Printf(const char *format, ...)
{
	va_list args;
	va_start(args, format);
	return Vfprintf(Stdout, format, args);
}

int Sprintf(void *buf, CharacterEncoding enc, const char *format, ...)
{
	va_list args;
	va_start(args, format);
	return Vsprintf(buf, enc, format, args);
}

int Snprintf(void *buf, size_t size, CharacterEncoding enc,
		 const char *format, ...)
{
	va_list args;
	va_start(args, format);
	return Vsnprintf(buf, size, enc, format, args);
}

int Vprintf(const char *format, va_list args)
{
	return Vfprintf(Stdout, format, args);
}

int Vsprintf(void *buf, CharacterEncoding enc, const char *format,
		 va_list args)
{
	int nchars;
	FILE16 file = {0, 0, -1, 0, StringWrite, 0, StringFlush, StringClose, FILE16_write};

	file.handle = buf;
	file.enc = enc;

	nchars = Vfprintf(&file, format, args);
	file.close(&file);		/* Fclose would try to free file */

	return nchars;
}

int Vsnprintf(void *buf, size_t size, CharacterEncoding enc,
		  const char *format, va_list args)
{
	int nchars;
	FILE16 file = {0, 0, -1, 0, StringWriteTrunc, 0, StringFlush, StringClose, FILE16_write};

	file.handle = buf;
	file.enc = enc;
	file.handle3 = size - NullSize(enc); /* make sure we can null-terminate */

	nchars = Vfprintf(&file, format, args);
	file.handle3 = size;	/* ready for null-termination */
	file.close(&file);		/* Fclose would try to free file */

	return nchars;
}

#define put(x) {nchars++; if(count == sizeof(buf)) {if(ConvertASCII(buf, count, file) == -1) return -1; count = 0;} buf[count++] = x;}

int Vfprintf(FILE16 *file, const char *format, va_list args)
{
	char8 buf[BufferSize];
	int count = 0;
	int c, i, n, width, prec;
	char fmt[200];
	char8 val[200];
	const char8 *start;
	const char8 *p;
	const char16 *q;
#if CHAR_SIZE == 16
	char16 cbuf[1];
#endif
	int mflag, pflag, sflag, hflag, zflag;
	int l, h, L, ll;
	int nchars = 0;

	while((c = *format++))
	{
	if(c != '%')
	{
		put(c);
		continue;
	}

	start = format-1;
	width = 0;
	prec = -1;
	mflag=0, pflag=0, sflag=0, hflag=0, zflag=0;
	l=0, h=0, L=0, ll=0;

	while(1)
	{
		switch(c = *format++)
		{
		case '-':
		mflag = 1;
		break;
		case '+':
		pflag = 1;
		break;
		case ' ':
		sflag = 1;
		break;
		case '#':
		hflag = 1;
		break;
		case '0':
		zflag = 1;
		break;
		default:
		goto flags_done;
		}
	}
	flags_done:

	if(c == '*')
	{
		width = va_arg(args, int);
		c = *format++;
	}
	else if(c >= '0' && c <= '9')
	{
		width = c - '0';
		while((c = *format++) >= '0' && c <= '9')
		width = width * 10 + c - '0';
	}

	if(c == '.')
	{
		c = *format++;
		if(c == '*')
		{
		prec = va_arg(args, int);
		c = *format++;
		}
		else if(c >= '0' && c <= '9')
		{
		prec = c - '0';
		while((c = *format++) >= '0' && c <= '9')
			prec = prec * 10 + c - '0';
		}
		else
		prec = 0;
	}

	switch(c)
	{
	case 'l':
		l = 1;
		c = *format++;
#ifdef HAVE_LONG_LONG
		if(c == 'l')
		{
		l = 0;
		ll = 1;
		c = *format++;
		}
#endif
		break;
	case 'h':
		h = 1;
		c = *format++;
		break;
#ifdef HAVE_LONG_DOUBLE
	case 'L':
		L = 1;
		c = *format++;
		break;
#endif
	}

	if(format - start + 1 > sizeof(fmt))
	{
	  ERR("Printf: format specifier too long");
		errno = 0;
		return -1;
	}

	strncpy(fmt, start, format - start);
	fmt[format - start] = '\0';

	/* XXX should check it fits in val */

	switch(c)
	{
	case 'n':
		*va_arg(args, int *) = nchars;
		break;
	case 'd':
	case 'i':
	case 'o':
	case 'u':
	case 'x':
	case 'X':
		if(h)
		sprintf(val, fmt, va_arg(args, int)); /* promoted to int */
		else if(l)
		sprintf(val, fmt, va_arg(args, long));
#ifdef HAVE_LONG_LONG
		else if(ll)
		sprintf(val, fmt, va_arg(args, long long));
#endif
		else
		sprintf(val, fmt, va_arg(args, int));
		for(p=val; *p; p++)
		put(*p);
		break;
	case 'f':
	case 'e':
	case 'E':
	case 'g':
	case 'G':
#ifdef HAVE_LONG_DOUBLE
		if(L)
		sprintf(val, fmt, va_arg(args, long double));
		else
#endif
		sprintf(val, fmt, va_arg(args, double));
		for(p=val; *p; p++)
		put(*p);
		break;
	case 'c':
#if CHAR_SIZE == 16
		if(ConvertASCII(buf, count, file) == -1)
		return -1;
		count = 0;
		cbuf[0] = va_arg(args, int);
		if(ConvertUTF16(cbuf, 1, file) == -1)
		return -1;
#else
			put(va_arg(args, int));
#endif
		break;
	case 'p':
		sprintf(val, fmt, va_arg(args, void *));
		for(p=val; *p; p++)
		put(*p);
		break;
	case '%':
		put('%');
		break;
	case 's':
		if(l)
		{
		static char16 sNULL[] = {'(','N','U','L','L',')',0};
#if CHAR_SIZE == 16
		string:
#endif
		q = va_arg(args, char16 *);
		if(!q)
			q = sNULL;
		for(n=0; n != prec && q[n]; n++)
			;
		if(n < width && !mflag)
			for(i=width-n; i>0; i--)
			put(' ');
		if(ConvertASCII(buf, count, file) == -1)
			return -1;
		count = 0;
		nchars += n;
		for(i = n; i > 0; i -= BufferSize)
		{
			/* ConvertUTF16 can only handle <= BufferSize chars */
			if(ConvertUTF16(q, i > BufferSize ? BufferSize : i, file) == -1)
			return -1;
			q += BufferSize;
		}
		}
		else
		{
#if CHAR_SIZE == 8
		string:
#endif
		p = va_arg(args, char8 *);
		if(!p)
			p = "(null)";
		for(n=0; n != prec && p[n]; n++)
			;
		if(n < width && !mflag)
			for(i=width-n; i>0; i--)
			put(' ');
		for(i=0; i<n; i++)
			put(p[i]);
		}
		if(n < width && mflag)
		for(i=width-n; i>0; i--)
			put(' ');
		break;
	case 'S':
		goto string;
	default:
	  ERR1("unknown format character %c\n", c);
		errno = 0;
		return -1;
	}
	}

	if(count > 0)
	if(ConvertASCII(buf, count, file) == -1)
		return -1;

	return nchars;
}

static FILE16 *MakeFILE16(const char *type)
{
	FILE16 *file;

	if(!(file = Malloc(sizeof(*file))))
	return 0;

	file->flags = 0;
	if(*type == 'r')
	{
	file->flags |= FILE16_read;
	type++;
	}
	if(*type == 'w')
	file->flags |= FILE16_write;

	file->enc = InternalCharacterEncoding;

	file->incount = file->inoffset = 0;

	return file;
}

FILE16 *MakeFILE16FromFD(int fd, const char *type)
{
	FILE16 *file;

	if(!(file = MakeFILE16(type)))
	return 0;

#if defined(WIN32)
	file->flags |= FILE16_crlf;
#endif

	file->read = FDRead;
	file->write = FDWrite;
	file->seek = FDSeek;
	file->close = FDClose;
	file->flush = FDFlush;
	file->handle2 = fd;

	return file;
}

static int FDRead(FILE16 *file, unsigned char *buf, int max_count)
{
	int fd = file->handle2;
	int count = 0;

	count = read(fd, buf, max_count);

	return count;
}

static int FDWrite(FILE16 *file, const unsigned char *buf, int count)
{
	int fd = file->handle2;
	int ret;

	while(count > 0)
	{
	ret = write(fd, buf, count);
	if(ret < 0)
		return ret;
	count -= ret;
	buf += ret;
	}

	return 0;
}

static int FDSeek(FILE16 *file, long offset, int ptrname)
{
	int fd = file->handle2;

	return lseek(fd, offset, ptrname);
}

static int FDClose(FILE16 *file)
{
	int fd = file->handle2;

	return (file->flags & FILE16_close_underlying) ? close(fd) : 0;
}

static int FDFlush(FILE16 *file)
{
	return 0;
}

FILE16 *MakeFILE16FromFILE(FILE *f, const char *type)
{
	FILE16 *file;

	if(!(file = MakeFILE16(type)))
	return 0;

#if defined(WIN32)
	/* We need to do Windows-style lf <-> crlf conversion ourselves,
	   because the standard i/o library doesn't know about 16-bit
	   characters. We don't want it to insert cr before output bytes
	   that just happen to look like linefeed, or change input byte
	   sequences that happen to look like crlf. */

	file->flags |= FILE16_crlf;

	/* So we have to switch the underlying file into binary mode, but
	   we probably don't want to automatically do that for Stdin/out/err
	   which are opened though the user may not want them (and may
	   want to keep stdin/out/err in text mode).  So we just flag
	   it as needing to be switched when the first i/o is done. */

	file->flags |= FILE16_needs_binary;
#endif

	file->read = FileRead;
	file->write = FileWrite;
	file->seek = FileSeek;
	file->close = FileClose;
	file->flush = FileFlush;
	file->handle = f;

	return file;
}

static int FileRead(FILE16 *file, unsigned char *buf, int max_count)
{
	FILE *f = file->handle;
	int count = 0;

#if defined(WIN32)
	if(file->flags & FILE16_needs_binary)
	{
#ifdef __CYGWIN__
	setmode(fileno(f), _O_BINARY);
#else
	_setmode(fileno(f), _O_BINARY);
#endif
	file->flags &= ~FILE16_needs_binary;
	}
#endif

	/* Terminal EOF is sticky for fread() on Linux, even though feof() is true.
	   How can they have got this wrong and never noticed it?
	   So check feof() here.
	*/
	if(feof(f))
	return 0;

	count = fread(buf, 1, max_count, f);

	return ferror(f) ? -1 : count;
}

static int FileWrite(FILE16 *file, const unsigned char *buf, int count)
{
	FILE *f = file->handle;

#if defined(WIN32)
	if(file->flags & FILE16_needs_binary)
	{
#ifdef __CYGWIN__
	setmode(fileno(f), _O_BINARY);
#else
	_setmode(fileno(f), _O_BINARY);
#endif
	file->flags &= ~FILE16_needs_binary;
	}
#endif

	if(count == 0)
	return 0;
	return fwrite(buf, 1, count, f) == 0 ? -1 : 0;
}

static int FileSeek(FILE16 *file, long offset, int ptrname)
{
	FILE *f = file->handle;

	return fseek(f, offset, ptrname);
}

static int FileClose(FILE16 *file)
{
	FILE *f = file->handle;

	return (file->flags & FILE16_close_underlying) ? fclose(f) : 0;
}

static int FileFlush(FILE16 *file)
{
	FILE *f = file->handle;

	return fflush(f);
}

FILE16 *MakeFILE16FromString(void *buf, long size, const char *type)
{
	FILE16 *file;

	if(!(file = MakeFILE16(type)))
	return 0;

	file->read = StringRead;
	file->write = StringWrite;
	file->seek = StringSeek;
	file->close = StringClose;
	file->flush = StringFlush;

	file->handle = buf;
	file->handle2 = 0;
	file->handle3 = size;

	return file;
}

static int StringRead(FILE16 *file, unsigned char *buf, int max_count)
{
	char *p = (char *)file->handle + file->handle2;

	if(file->handle3 >= 0 && file->handle2 + max_count > file->handle3)
	max_count = file->handle3 - file->handle2;

	if(max_count <= 0)
	return 0;

	memcpy(buf, p, max_count);
	file->handle2 += max_count;

	return max_count;
}

static int StringWrite(FILE16 *file, const unsigned char *buf, int count)
{
	char *p = (char *)file->handle + file->handle2;

	if(file->handle3 >= 0 && file->handle2 + count > file->handle3)
	return -1;

	memcpy(p, buf, count);
	file->handle2 += count;

	return 0;
}

/* Like StringWrite, but ignores overflow rather than returning an error.
   Used for Vsnprintf. */

static int StringWriteTrunc(FILE16 *file, const unsigned char *buf, int count)
{
	char *p = (char *)file->handle + file->handle2;

	if(file->handle3 >= 0 && file->handle2 + count > file->handle3)
	/* XXX This doesn't really work; a character might be truncated.
	   Safe for fixed-size encodings if buffer size is a multiple
	   of the character size. */
	count = file->handle3 - file->handle2;

	memcpy(p, buf, count);
	file->handle2 += count;

	return 0;
}

static int StringSeek(FILE16 *file, long offset, int ptrname)
{
	switch(ptrname)
	{
	case SEEK_CUR:
	offset = file->handle2 + offset;
	break;
	case SEEK_END:
	if(file->handle3 < 0)
		return -1;
	offset = file->handle3 + offset;
	break;
	}

	if(file->handle3 >= 0 && offset > file->handle3)
	return -1;

	file->handle2 = offset;

	return 0;
}

static int StringClose(FILE16 *file)
{
	if(file->flags & FILE16_write)
	ConvertASCII(&null, 1, file); /* null terminate */

	if(file->flags & FILE16_close_underlying)
	Free((char *)file->handle);

	return 0;
}

static int StringFlush(FILE16 *file)
{
	if(file->flags & FILE16_write)
	{
	/* null terminate, but leave position unchanged */
	int save = file->handle2;
	ConvertASCII(&null, 1, file);
	file->handle2 = save;
	}

	return 0;
}

FILE16 *MakeStringFILE16(const char *type)
{
	FILE16 *file;

	if(!(file = MakeFILE16(type)))
	return 0;

	file->read = MStringRead;
	file->write = MStringWrite;
	file->seek = MStringSeek;
	file->close = MStringClose;
	file->flush = MStringFlush;

	file->handle = 0;
	file->handle2 = 0;
	file->handle3 = 0;

	return file;
}

void *StringFILE16String(FILE16 *file)
{
	return file->handle;
}

static int MStringRead(FILE16 *file, unsigned char *buf, int max_count)
{
	return 0;
}

static int MStringWrite(FILE16 *file, const unsigned char *buf, int count)
{
	char *p;

	if(file->handle2 + count > file->handle3)
	{
	int newsize = (file->handle3 == 0 ? 32 : file->handle3);

	while(file->handle2 + count > newsize)
		newsize *= 2;

	file->handle = Realloc(file->handle, newsize);
	if(!file->handle)
		return -1;
	file->handle3 = newsize;
	}

	p = (char *)file->handle + file->handle2;
	memcpy(p, buf, count);
	file->handle2 += count;

	return 0;
}

static int MStringSeek(FILE16 *file, long offset, int ptrname)
{
	switch(ptrname)
	{
	case SEEK_CUR:
	offset = file->handle2 + offset;
	break;
	case SEEK_END:
	if(file->handle3 < 0)
		return -1;
	offset = file->handle3 + offset;
	break;
	}

	if(file->handle3 >= 0 && offset > file->handle3)
	return -1;

	file->handle2 = offset;

	return 0;
}

static int MStringClose(FILE16 *file)
{
	Free(file->handle);

	return 0;
}

static int MStringFlush(FILE16 *file)
{
	if(file->flags & FILE16_write)
	{
	/* null terminate, but leave position unchanged */
	int save = file->handle2;
	ConvertASCII(&null, 1, file);
	file->handle2 = save;
	}

	return 0;
}


#if defined(WIN32) && ! defined(__CYGWIN__)
#ifdef SOCKETS_IMPLEMENTED

FILE16 *MakeFILE16FromWinsock(int sock, const char *type)
{
	FILE16 *file;

	if(!(file = MakeFILE16(type)))
	return 0;

	file->flags |= FILE16_crlf;

	file->read = WinsockRead;
	file->write = WinsockWrite;
	file->seek = WinsockSeek;
	file->close = WinsockClose;
	file->flush = WinsockFlush;
	file->handle2 = sock;

	return file;
}

static int WinsockRead(FILE16 *file, unsigned char *buf, int max_count)
{
	int f = file->handle2;
	int count;

	/* "Relax" said the nightman, we are programmed to recv() */
	count = recv(f, buf, max_count, 0);

	return count;
}

static int WinsockWrite(FILE16 *file, const unsigned char *buf, int count)
{
	int f = file->handle2;

	return send(f, buf, count ,0);
}

static int WinsockSeek(FILE16 *file, long offset, int ptrname)
{
	return -1;
}

static int WinsockClose(FILE16 *file)
{
	int f = file->handle2;

	if(file->flags & FILE16_close_underlying)
	closesocket(f);

	return 0;
}

static int WinsockFlush(FILE16 *file)
{
	return 0;
}

#endif
#endif

#ifdef HAVE_LIBZ

FILE16 *MakeFILE16FromGzip(gzFile f, const char *type)
{
	FILE16 *file;

	if(!(file = MakeFILE16(type)))
	return 0;

	file->read = GzipRead;
	file->write = GzipWrite;
	file->seek = GzipSeek;
	file->close = GzipClose;
	file->flush = GzipFlush;
	file->handle = (void *)f;

	return file;
}

static int GzipRead(FILE16 *file, unsigned char *buf, int max_count)
{
	gzFile f = (gzFile)file->handle;
	int count = 0;
	int gzerr;
	const char *errorString;

	count = gzread(f, buf, max_count);

	errorString = gzerror(f, &gzerr);
	if(gzerr != 0 && gzerr != Z_STREAM_END)
	return -1;

	return count;
}

static int GzipWrite(FILE16 *file, const unsigned char *buf, int count)
{
	gzFile f = (gzFile)file->handle;
	int gzerr;
	const char *errorString;

	count = gzwrite(f, (char *)buf, count);

	errorString = gzerror(f, &gzerr);
	if(gzerr != 0 && gzerr != Z_STREAM_END)
	return -1;

	return count;
}

static int GzipSeek(FILE16 *file, long offset, int ptrname)
{
	return -1;
}

static int GzipClose(FILE16 *file)
{
	gzFile f = (gzFile)file->handle;

	return (file->flags & FILE16_close_underlying) ? gzclose(f) : 0;
}

static int GzipFlush(FILE16 *file)
{
	return 0;
}

#endif

#ifdef test

int main(int argc, char **argv)
{
	short s=3;
	int n, c;
	char16 S[] = {'w', 'o', 'r', 'l', 'd', ' ', '£' & 0xff, 0xd841, 0xdc42, 0};

	n=Printf(argv[1], s, 98765432, &c, 5.3, 3.2L, "÷hello", S);
	printf("\nreturned %d, c=%d\n", n, c);
	n=Printf(argv[1], s, 98765432, &c, 5.3, 3.2L, "÷hello", S);
	printf("\nreturned %d, c=%d\n", n, c);
	n=Printf(argv[1], s, 98765432, &c, 5.3, 3.2L, "÷hello", S);
	printf("\nreturned %d, c=%d\n", n, c);
	n=Printf(argv[1], s, 98765432, &c, 5.3, 3.2L, "÷hello", S);
	printf("\nreturned %d, c=%d\n", n, c);

	return 0;
}

#endif
