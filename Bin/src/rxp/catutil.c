#include "catutil.h"

#include <stdio.h>
#include <string.h>
#include <ctype.h>

#include "charset.h"
#include "string16.h"
#include "stdio16.h"
#include "rxputil.h"

static char *norm_pub(const char8 *public8, const char16 *public16);
static char *norm_sys(const char8 *system8, const char16 *system16);

char *NormalizePublic(const Char *public)
{
#if CHAR_SIZE == 8
	return NormalizePublic8(public);
#else
	return NormalizePublic16(public);
#endif
}

char *NormalizePublic16(const char16 *public)
{
	return norm_pub(0, public);
}

char *NormalizePublic8(const char8 *public)
{
	return norm_pub(public, 0);
}

char *norm_pub(const char8 *public8, const char16 *public16)
{
	int len = public8 ? strlen(public8) : strlen16(public16);
	int i, j, c, in_space;
	char *new_public;

	if(!(new_public = Malloc(len+1)))
	return 0;

	in_space = 1;
	for(i=j=0; i<len; i++)
	{
	int c = public8 ? (unsigned char)public8[i] : public16[i];
	if(c > 127)
	{
		if(public8)
		Fprintf(Stderr,
			"catalog error: non-ascii character in public id %s\n",
			public8);
		else
		Fprintf(Stderr,
			   "catalog error: non-ascii character in public id %ls\n",
			public16);

		Free(new_public);
		return 0;
	}
	if(c == ' ' || c == '\t' || c == '\r' || c == '\n')
	{
		if(!in_space)
		new_public[j++] = ' ';
		in_space = 1;
	}
	else
	{

		new_public[j++] = c;
		in_space = 0;
	}
	}

	while(j > 0)
	{
	c = new_public[j-1];
	if(c == ' ' || c == '\t' || c == '\r' || c == '\n')
		j--;
	else
		break;
	}

	new_public[j] = 0;

	return new_public;
}

char *NormalizeSystem(const Char *system)
{
#if CHAR_SIZE == 8
	return NormalizeSystem8(system);
#else
	return NormalizeSystem16(system);
#endif
}

char *NormalizeSystem16(const char16 *system)
{
	return norm_sys(0, system);
}

char *NormalizeSystem8(const char8 *system)
{
	return norm_sys(system, 0);
}

char *norm_sys(const char8 *system8, const char16 *system16)
{
	int len = system8 ? strlen(system8) : strlen16(system16);
	int i, j;
	int c;
	Vector(char, new_system);
	char escbuf[13];		/* up to 4 UTF-8 bytes * 3 + null  */
	char *p;

	VectorInit(new_system);

	for(i=j=0; i<len; i++)
	{
	c = system8 ? (unsigned char)system8[i] : system16[i];

	if(c > 0x110000)
	{
		/* shouldn't happen if it came from an XML document */
		Fprintf(Stderr,
			"catalog error: unicode character u+%x > u+110000\n", c);
		return 0;
	}
	else if(c >= 0xd800 && c <= 0xdbff)
	{
		/* surrogates */

		int d;
		if(i == len)
		{
		Fprintf(Stderr,
			"catalog error: unterminated surrogate pair\n", c);
		return 0;
		}
		d = system8 ? (unsigned char)system8[++i] : system16[++i];
		if(d < 0xdc00 || d > 0xdfff)
		{
		Fprintf(Stderr,
			"catalog error: unterminated surrogate pair\n", c);
		return 0;
		}
		percent_escape(0x10000 + ((c - 0xd800) << 10) + (d - 0xdc00),
			   escbuf);
	}
	else if(c >= 0xdc00 && c <= 0xdfff)
	{
		/* bogus surrogates */

		Fprintf(Stderr, "catalog error: bad first surrogate u+%x\n", c);
		return 0;
	}
	else if(c < 0x20 || c >= 0x80)
	{
		/* controls and non-ascii */

		percent_escape(c, escbuf);
	}
	else
	{
		/* excluded ascii characters */

		switch(c)
		{
		case ' ':
		case '<':
		case '>':
		case '\\':
		case '^':
		case '`':
		case '{':
		case '|':
		case '}':
		case 127:
		percent_escape(c, escbuf);
		break;
		default:
		if(!VectorPush(new_system, c))
			return 0;
		continue;
		break;
		}
	}

	/* copy the escaped characters */

	for(p = escbuf; *p; p++)
		if(!VectorPush(new_system, *p))
		return 0;
	}

	if(!VectorPush(new_system, 0))
	return 0;

	return new_system;
}

int toUTF8(int c, int *bytes)
{
	if(c < 0)
	return -1;

	if(c < 0x80)
	{
	bytes[0] = c;
	return 1;
	}

	if(c < 0x800)
	{
	bytes[0] = 0xc0 + (c >> 6);
	bytes[1] = 0x80 + (c & 0x3f);
	return 2;
	}

	if(c < 0x10000)
	{
	bytes[0] = 0xe0 + (c >> 12);
	bytes[1] = 0x80 + ((c >> 6) & 0x3f);
	bytes[2] = 0x80 + (c & 0x3f);
	return 3;
	}

	if(c < 0x200000)
	{
	bytes[0] = 0xf0 + (c >> 18);
	bytes[1] = 0x80 + ((c >> 12) & 0x3f);
	bytes[2] = 0x80 + ((c >> 6) & 0x3f);
	bytes[3] = 0x80 + (c & 0x3f);
	return 4;
	}

	if(c < 0x4000000)
	{
	bytes[0] = 0xf8 + (c >> 24);
	bytes[1] = 0x80 + ((c >> 18) & 0x3f);
	bytes[2] = 0x80 + ((c >> 12) & 0x3f);
	bytes[3] = 0x80 + ((c >> 6) & 0x3f);
	bytes[4] = 0x80 + (c & 0x3f);
	return 5;
	}

/*    if(c < 0x80000000) always true! */
	{
	bytes[0] = 0xfc + (c >> 30);
	bytes[1] = 0x80 + ((c >> 24) & 0x3f);
	bytes[2] = 0x80 + ((c >> 18) & 0x3f);
	bytes[3] = 0x80 + ((c >> 12) & 0x3f);
	bytes[4] = 0x80 + ((c >> 6) & 0x3f);
	bytes[5] = 0x80 + (c & 0x3f);
	return 6;
	}
}

int percent_escape(int c, char *buf)
{
	int nbytes, i;
	int bytes[6];

	if((nbytes = toUTF8(c, bytes)) == -1)
	return -1;

	for(i=0; i<nbytes; i++)
	{
	/* XXX upper case?? */
	sprintf(buf, "%%%2x", bytes[i]);
	buf += 3;
	}

	*buf = 0;

	return nbytes * 3;
}

int IsPublicidUrn(const char *id)
{
#if 0
	return id && strncasecmp(id, "urn:publicid:", 13) == 0;
#else
	/* guess who doesn't provide strncasecmp */
	static char *p = "urn:publicid:";
	int i;

	if(!id)
	return 0;
	for(i=0; p[i]; i++)
	if(tolower(id[i]) != p[i])
		return 0;

	return 1;
#endif
}

char *UnwrapPublicidUrn(const char *id)
{
	int i, j, len, extra = 0;
	char *result;

	id += 13;			/* skip over urn:publicid: */

	for(i=0; id[i]; i++)
	if(id[i] == ':' || id[i] == ';')
		extra++;
	len = i;

	if(!(result = Malloc(len + extra + 1)))
	return 0;

	for(i=j=0; i<len; i++)
	{
	switch(id[i])
	{
	case '+':
		result[j++] = ' ';
		break;
	case ':':
		result[j++] = '/';
		result[j++] = '/';
		break;
	case ';':
		result[j++] = ':';
		result[j++] = ':';
		break;
	case '%':
		if(id[i+1] == '2' && (id[i+2] == 'B' || id[i+2] == 'b'))
		result[j++] = '+';
		else if(id[i+1] == '3' && (id[i+2] == 'A' || id[i+2] == 'a'))
		result[j++] = ':';
		else if(id[i+1] == '2' && (id[i+2] == 'F' || id[i+2] == 'f'))
		result[j++] = '/';
		else if(id[i+1] == '3' && (id[i+2] == 'B' || id[i+2] == 'b'))
		result[j++] = ';';
		else if(id[i+1] == '2' &&  id[i+2] == '7')
		result[j++] = '\'';
		else if(id[i+1] == '3' && (id[i+2] == 'F' || id[i+2] == 'f'))
		result[j++] = '?';
		else if(id[i+1] == '2' &&  id[i+2] == '3')
		result[j++] = '#';
		else if(id[i+1] == '2' &&  id[i+2] == '5')
		result[j++] = '%';
		else
		{
		result[j++] = id[i];
		break;
		}
		i += 2;
		break;
	default:
		result[j++] = id[i];
		break;
	}
	}

	result[j] = 0;

	return result;
}

int strcmpC8(const Char *s1, const char *s2)
{
	Char c1;
	char c2;

	while(1)
	{
	c1 = *s1++;
	c2 = (unsigned char)*s2++;
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
