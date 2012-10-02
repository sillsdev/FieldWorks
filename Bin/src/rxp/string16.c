#ifdef FOR_LT

#include "lt-memory.h"

#define Malloc salloc
#define Realloc srealloc
#define Free sfree

#else

#include "system.h"

#endif

#include "charset.h"
#include "ctype16.h"
#include "string16.h"

int strcasecmp8(const char8 *s1, const char8 *s2)
{
	char8 c1, c2;

	while(1)
	{
	c1 = Toupper(*s1++);
	c2 = Toupper(*s2++);
	if(c1 == 0 && c2 == 0)
		return 0;
	if(c1 == 0)
		return -1;
	if(c2 == 0)
		return 1;
	if(c1 < c2)
		return -1;
	if(c1 > c2)
		return 1;
	}
}

int strncasecmp8(const char8 *s1, const char8 *s2, size_t n)
{
	char8 c1, c2;

	while(n-- > 0)
	{
	c1 = Toupper(*s1++);
	c2 = Toupper(*s2++);
	if(c1 == 0 && c2 == 0)
		return 0;
	if(c1 == 0)
		return -1;
	if(c2 == 0)
		return 1;
	if(c1 < c2)
		return -1;
	if(c1 > c2)
		return 1;
	}

	return 0;
}

char8 *strdup8(const char8 *s)
{
	char8 *buf;
	int len;

	len = strlen8(s);
	buf = Malloc(len + 1);
	if(!buf)
	return 0;

	strcpy8(buf, s);

	return buf;
}

/* Convert a Latin-1 string to UTF-16 (easy!) */

void translate_latin1_utf16(const char8 *from, char16 *to)
{
	while(*from)
	*to++ = (unsigned char)*from++; /* be sure not to sign extend */
	*to = 0;
}

/* Convert a UTF-16 string to Latin-1, replacing missing characters with Xs */

void translate_utf16_latin1(const char16 *from, char8 *to)
{
	while(*from)
	{
	char16 c = *from++;
	*to++ = c > 255 ? 'X' : c;
	}
	*to = 0;
}

/* Conversion functions that realloc the destination buffer; so if
   it's null the space will be malloced. */

char16 *translate_latin1_utf16_m(const char8 *from, char16 *to)
{
	to = Realloc(to, (strlen8(from) + 1) * sizeof(char16));
	if(to)
	translate_latin1_utf16(from, to);
	return to;
}

char8 *translate_utf16_latin1_m(const char16 *from, char8 *to)
{
	to = Realloc(to, strlen16(from) + 1);
	if(to)
	translate_utf16_latin1(from, to);
	return to;
}

char16 *strcpy16(char16 *s1, const char16 *s2)
{
	char16 *t = s1;

	while(*s2)
	*s1++ = *s2++;
	*s1 = 0;

	return t;
}

char16 *strncpy16(char16 *s1, const char16 *s2, size_t n)
{
	char16 *t = s1;

	while(n > 0 && *s2)
	{
	n--;
	*s1++ = *s2++;
	}

	/* Apparently strncpy is supposed to fill the destination with nulls,
	   not just null terminate */

	while(n-- > 0)
	*s1++ = 0;

	return t;
}

char16 *strdup16(const char16 *s)
{
	char16 *buf;
	int len;

	len = strlen16(s);
	buf = Malloc((len + 1) * sizeof(char16));
	if(!buf)
	return 0;

	strcpy16(buf, s);

	return buf;
}

size_t strlen16(const char16 *s)
{
	int len = 0;

	while(*s++)
	len++;

	return len;
}

char16 *strchr16(const char16 *s, int c)
{
	for( ; *s; s++)
	if(*s == c)
		return (char16 *)s;	/* Is const bogus or what? */

	return 0;
}

int strcmp16(const char16 *s1, const char16 *s2)
{
	char16 c1, c2;

	while(1)
	{
	c1 = *s1++;
	c2 = *s2++;
	if(c1 == 0 && c2 == 0)
		return 0;
#if 0
	/* char16 is unsigned, so we don't need this */
	if(c1 == 0)
		return -1;
	if(c2 == 0)
		return 1;
#endif
	if(c1 < c2)
		return -1;
	if(c1 > c2)
		return 1;
	}
}

int strncmp16(const char16 *s1, const char16 *s2, size_t n)
{
	char16 c1, c2;

	while(n-- > 0)
	{
	c1 = *s1++;
	c2 = *s2++;
	if(c1 == 0 && c2 == 0)
		return 0;
#if 0
	/* char16 is unsigned, so we don't need this */
	if(c1 == 0)
		return -1;
	if(c2 == 0)
		return 1;
#endif
	if(c1 < c2)
		return -1;
	if(c1 > c2)
		return 1;
	}

	return 0;
}

/* XXX only works for characters < 256 because Toupper does */

int strcasecmp16(const char16 *s1, const char16 *s2)
{
	char16 c1, c2;

	while(1)
	{
	c1 = Toupper(*s1++);
	c2 = Toupper(*s2++);
	if(c1 == 0 && c2 == 0)
		return 0;
#if 0
	/* char16 is unsigned, so we don't need this */
	if(c1 == 0)
		return -1;
	if(c2 == 0)
		return 1;
#endif
	if(c1 < c2)
		return -1;
	if(c1 > c2)
		return 1;
	}
}

int strncasecmp16(const char16 *s1, const char16 *s2, size_t n)
{
	char16 c1, c2;

	while(n-- > 0)
	{
	c1 = Toupper(*s1++);
	c2 = Toupper(*s2++);
	if(c1 == 0 && c2 == 0)
		return 0;
#if 0
	/* char16 is unsigned, so we don't need this */
	if(c1 == 0)
		return -1;
	if(c2 == 0)
		return 1;
#endif
	if(c1 < c2)
		return -1;
	if(c1 > c2)
		return 1;
	}

	return 0;
}

char16 *strcat16(char16 *s1, const char16 *s2)
{
	char16 *t = s1;

	s1 += strlen16(s1);
	strcpy16(s1, s2);

	return t;
}

char16 *strncat16(char16 *s1, const char16 *s2, size_t n)
{
	char16 *t = s1;

	s1 += strlen16(s1);

	/* Unlike strncpy, strncat *always* null terminates, and does not
	   fill with nulls. */

	while(n-- > 0 && *s2)
	*s1++ = *s2++;

	*s1 = 0;

	return t;
}

/* A very naive implementation */

char16 *strstr16(const char16 *s1, const char16 *s2)
{
	int len, first;

	first = s2[0];
	if(first == 0)
	return (char16 *)s1;

	len = strlen16(s2);

	while((s1 = strchr16(s1, first)))
	{
	if(strncmp16(s1, s2, len) == 0)
		return (char16 *)s1;
	else
		s1++;
	}

	return 0;
}
