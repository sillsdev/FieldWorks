#include "GrPlatform.h"


#ifdef _WIN32
#include "windows.h"

namespace gr
{

size_t Platform_UnicodeToANSI(const utf16 * prgchwSrc, size_t cchwSrc, char * prgchsDst, size_t cchsDst)
{
	return ::WideCharToMultiByte(CP_ACP, 0, prgchwSrc, cchwSrc, prgchsDst, cchsDst, NULL, NULL);
}

size_t Platform_AnsiToUnicode(const char * prgchsSrc, size_t cchsSrc, utf16 * prgchwDst, size_t cchwDst)
{
	return ::MultiByteToWideChar(CP_ACP, 0, prgchsSrc, cchsSrc, prgchwDst, cchwDst);
}

size_t Platform_8bitToUnicode(int nCodePage, const char * prgchsSrc, int cchsSrc,
	utf16 * prgchwDst, int cchwDst)
{
	return ::MultiByteToWideChar(nCodePage, 0, prgchsSrc, cchsSrc, prgchwDst, cchwDst);
}

size_t utf8len(const char *s)
{
	return mbstowcs(NULL,s,0);
}


utf16 *utf16cpy(utf16 *dest, const utf16 *src)
{
	return wcscpy(dest, src);
}

utf16 *utf16ncpy(utf16 *dest, const utf16 *src, size_t n)
{
	return wcsncpy(dest, src, n);
}

utf16 *utf16ncpy(utf16 *dest, const char *src, size_t n)
{
	#ifdef UTF16DEBUG
	std::cerr << "utf16ncpy8: " << src << std::endl;
	#endif
	Platform_AnsiToUnicode(src, strlen(src), dest, n);
	return dest;
}

//size_t wcslen(const wchar_t *s);
size_t utf16len(const utf16 *s)
{
	return wcslen(s);
}

int utf16cmp(const utf16 *s1, const utf16 *s2)
{
	return wcscmp(s1, s2);
}

int utf16ncmp(const utf16 *s1, const utf16 *s2, size_t n)
{
	return wcsncmp(s1, s2, n);
}


int utf16cmp(const utf16 *s1, const char *s2)
{
	while (*s1 && s2)
	{
		if (*s1 < *s2)
		{
			return -1;
		}
		if (*s1 > *s2)
		{
			return 1;
		}
		*s1++;
		*s2++;
	}
	if (*s1) return -1;
	else if (*s2) return 1;
	return 0;
}

}
#else // not _WIN32

// Standard Headers
#include <cerrno>
#include <cstdlib>
#include <cstring>
#include <iomanip>
#include <iostream>
#include <sstream>
// Platform headers
#include <iconv.h>


namespace gr
{

#ifdef UTF16DEBUG
void utf16Output(const utf16 *input)
{
	utf16	src_utf;
	while(src_utf = *input++)
	{
		std::cerr << char(0xff & src_utf);
		if (src_utf > 255)
			std::cerr << char(src_utf >> 8);
	}

	std::cerr.flush();
}
#endif

utf16 *utf16cpy(utf16 *dest, const utf16 *src)
{
	size_t srcsize = utf16len(src);
	memcpy(dest, src, sizeof(utf16) * srcsize);
	return dest;
}


utf16 *utf16ncpy(utf16 *dest, const utf16 *src, size_t n)
{
	memcpy(dest, src, sizeof(utf16) * std::min(utf16len(src, n), n));
	return dest;
}

utf16 *utf16ncpy(utf16 *dest, const char *src, size_t n)
{
	Platform_AnsiToUnicode(src, strlen(src), dest, n);
	return dest;
}

size_t utf16len(const utf16 *s)
{
	// assumes NULL terminated strings
	const utf16 *start = s;
	for (; *s; ++s);

	return s - start;
}

size_t utf16len(const utf16 *s, size_t n)
{
	const utf16 *start = s;
	for (; *s && s - start <= n; ++s) ;

	return s - start;
}

size_t utf8len(const char *s)
{
	static const int is_initial[] = {1, 1, 0, 1};
	// assumes NULL terminated strings
	size_t length = 0;
	for (; *s; ++s)
		length += is_initial[*s >> 6];

	return length;
}


int utf16cmp(const utf16 *s1, const utf16 *s2)
{
	#ifdef UTF16DEBUG
	std::cerr << "utf16cmp16: comparing";
	utf16Output(s1);
	std::cerr << " and ";
	utf16Output(s1);
	std::cerr << std::endl;
	#endif
	while (*s1 && s2)
	{
		if (*s1 < *s2)
		{
			return -1;
		}
		if (*s1 > *s2)
		{
			return 1;
		}
		*s1++;
		*s2++;
	}
	if (*s1) return -1;
	else if (*s2) return 1;
	return 0;
}

int utf16ncmp(const utf16 *s1, const utf16 *s2, size_t n)
{
	#ifdef UTF16DEBUG
	std::cerr << "utf16cmp16: comparing";
	utf16Output(s1);
	std::cerr << " and ";
	utf16Output(s1);
	std::cerr << std::endl;
	#endif
	size_t p=0;
	while (*s1 && s2 && (p<n))
	{
		if (*s1 < *s2)
		{
			return -1;
		}
		if (*s1 > *s2)
		{
			return 1;
		}
		*s1++;
		*s2++;
		p++;
	}
	if (p==n) return 0;
	else if (*s1) return -1;
	else if (*s2) return 1;
	return 0;
}

int utf16cmp(const utf16 *s1, const char *s2)
{
	#ifdef UTF16DEBUG
	std::cerr << "utf16cmp: " << s1 << "::" << s2 << std::endl;
	#endif
	// let's just assume that both are ascii for now
	while (*s1 && s2)
	{
		if (*s1 < *s2)
		{
			return -1;
		}
		if (*s1 > *s2)
		{
			return 1;
		}
		*s1++;
		*s2++;
	}
	if (*s1) return -1;
	else if (*s2) return 1;
	return 0;
}


size_t Platform_UnicodeToANSI(const utf16 *src, size_t src_len, char *dest, size_t dest_len)
{
	if (!dest)
		return src_len;

	// Unicode to the 1252 codepage.  If codepoint value is above 255 then
	//  output '?' instead.
	utf16	src_utf;
	for (size_t n_chars = std::min(src_len, dest_len); n_chars; --n_chars)
		*dest++ = (src_utf = *src++) > 255 ? '?' : char(src_utf);

	if (dest_len > src_len)
		*dest = '\0';

	return dest_len;
}


size_t Platform_AnsiToUnicode(const char * src, size_t src_len, utf16 * dest, size_t dest_len)
{
	if (!dest)
		return src_len;

	// What is commonly refered to ANSI is close enough to Latin 1 codepage to
	//  allow just copy the 8-bit values into the low 8-bits of the UTF values.
	for (size_t n_chars = std::min(src_len, dest_len); n_chars; --n_chars)
		*dest++ = utf16(*src++);

	if (dest_len > src_len)
		*dest = '\0';

	return dest_len;
}

size_t Platform_8bitToUnicode(int nCodePage, const char * prgchsSrc, int cchsSrc,
	utf16 * prgchwDst, int cchwDst)
{
	return Platform_AnsiToUnicode(prgchsSrc, cchsSrc, prgchwDst, cchwDst);
}

unsigned short MultiByteToWideChar(unsigned long code_page, unsigned long,
		const char * source, size_t src_count,
		unsigned short * dest, size_t dst_count)
{
	if (!dest)	// An error condition is indicated by 0
		return 0;

	if (src_count == size_t(-1))     // When passed -1 it should find the
	src_count = strlen(source);  // length of the source it's self

	if (dst_count == 0)		// We cannot find the completed size so esitmate it.
	return src_count*3;

	std::ostringstream oss; oss << "CP" << code_page;
	iconv_t cdesc = iconv_open("UCS-2", oss.str().c_str()); // tocode, fromcode

	if (cdesc == iconv_t(-1))
	{
	std::cerr << program_invocation_short_name
				  << ": iconv_open(\"UCS-2\", \"" << oss.str() << "\"): "
				  << strerror(errno) << std::endl;
	exit(1);
	}

	char *dst_ptr = reinterpret_cast<char *>(dest);
	ICONV_CONST char *src_ptr = const_cast<char *>(source);
	dst_count *= sizeof(unsigned short);
	const size_t dst_size = dst_count;

	iconv(cdesc, &src_ptr, &src_count, &dst_ptr, &dst_count);
	iconv_close(cdesc);

	return dst_size - dst_count;
}


} // namespace gr

char * itoa(int value, char *string, int radix)
{
	std::ostringstream oss;

	oss << std::setbase(radix) << value;

	// We don't get passed the size of the destionation buffer which is very
	//  unwise, so plump for a reasonable value.  I don't reckon Graphite
	//  needs more than 64 didits of output.
	// std::string::copy doesn't null terminate the string to don't forget to
	//  do it.
	string[oss.str().copy(string, 63)] = '\0';

	return string;
}



#endif // _WIN32
