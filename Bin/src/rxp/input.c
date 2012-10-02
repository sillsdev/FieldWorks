#include <stdio.h>
#include <stdlib.h>
#include <assert.h>

#ifdef FOR_LT

#include "lt-memory.h"
#include "nsllib.h"

#define ERR(m) LT_ERROR(NECHAR,m)
#define ERR1(m,x) LT_ERROR1(NECHAR,m,x)
#define ERR2(m,x,y) LT_ERROR2(NECHAR,m,x,y)
#define ERR3(m,x,y,z) LT_ERROR3(NECHAR,m,x,y,z)

#define Malloc salloc
#define Realloc srealloc
#define Free sfree

#else

#include "system.h"
#define ERR(m) fprintf(stderr,m)
#define ERR1(m,x) fprintf(stderr,m,x)
#define ERR2(m,x,y) fprintf(stderr,m,x,y)
#define ERR3(m,x,y,z) fprintf(stderr,m,x,y,z)

#endif

#include "charset.h"
#include "string16.h"
#include "dtd.h"
#include "input.h"
#include "url.h"
#include "ctype16.h"

static void internal_reader(InputSource s);
static void external_reader(InputSource s);

InputSource SourceFromFILE16(const char8 *description, FILE16 *file16)
{
	Entity e;

	e = NewExternalEntity(0, 0, description, 0, 0);
	if(!strchr8(description, '/'))
	{
	char8 *base = default_base_url();
	EntitySetBaseURL(e, base);
	Free(base);
	}

	return NewInputSource(e, file16);
}

InputSource SourceFromStream(const char8 *description, FILE *file)
{
	FILE16 *file16;

	if(!(file16 = MakeFILE16FromFILE(file, "r")))
	return 0;

	return SourceFromFILE16(description, file16);
}

InputSource EntityOpen(Entity e)
{
	FILE16 *f16;
	char8 *r_url;

	if(e->type == ET_external)
	{
	const char8 *url = EntityURL(e);

	if(!url || !(f16 = url_open(url, 0, "r", &r_url)))
		return 0;
	if(r_url && !e->base_url)
		EntitySetBaseURL(e, r_url);
	Free(r_url);
	}
	else
	{
	f16 = MakeFILE16FromString((char *)e->text, -1, "r");
	}

	return NewInputSource(e, f16);
}


InputSource NewInputSource(Entity e, FILE16 *f16)
{
	InputSource source;

	if(!(source = Malloc(sizeof(*source))))
	return 0;

	source->line = 0;
	source->line_alloc = 0;
	source->line_length = 0;
	source->expecting_low_surrogate = 0;
	source->complicated_utf8_line = 0;
	source->line_is_incomplete = 0;
	source->next = 0;
	source->seen_eoe = 0;

	source->entity = e;

	source->reader =
	(e->type == ET_external) ? external_reader :internal_reader;
	source->map = xml_char_map;	/* 1.0 map unless changed by parser */

	source->file16 = f16;

	source->bytes_consumed = 0;
	source->bytes_before_current_line = 0;
	source->line_end_was_cr = 0;
	source->line_number = 0;
	source->not_read_yet = 1;
	source->read_carefully = 0;

	source->nextin = source->insize = 0;

	source->parent = 0;

	source->seen_error = 0;
	strcpy(source->error_msg, "no error (you should never see this)");

	return source;
}

void SourceClose(InputSource source)
{
	Fclose(source->file16);

	if(source->entity->type == ET_external)
	Free(source->line);
	Free(source);
}

int SourceLineAndChar(InputSource s, int *linenum, int *charnum)
{
	Entity e = s->entity, f = e->parent;

	if(e->type == ET_external)
	{
	*linenum = s->line_number;
	*charnum = s->next;
	return 1;
	}

	if(f && f->type == ET_external)
	{
	if(e->matches_parent_text)
	{
		*linenum = e->line_offset + s->line_number;
		*charnum = (s->line_number == 0 ? e->line1_char_offset : 0) +
			   s->next;
		return 1;
	}
	else
	{
		*linenum = e->line_offset;
		*charnum = e->line1_char_offset;
		return 0;
	}
	}

	if(f && f->matches_parent_text)
	{
	*linenum = f->line_offset + e->line_offset;
	*charnum = (e->line_offset == 0 ? f->line1_char_offset : 0) +
		e->line1_char_offset;
	return 0;
	}

	return -1;
}

void SourcePosition(InputSource s, Entity *entity, int *byte_offset)
{
	*entity = s->entity;
	*byte_offset = SourceTell(s);
}

int SourceTell(InputSource s)
{
#if CHAR_SIZE == 8
	return s->bytes_before_current_line + s->next;
#else
	switch(s->entity->encoding)
	{
	case CE_ISO_10646_UCS_2B:
	case CE_UTF_16B:
	case CE_ISO_10646_UCS_2L:
	case CE_UTF_16L:
	return s->bytes_before_current_line + 2 * s->next;
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
	return s->bytes_before_current_line + s->next;
	case CE_UTF_8:
	if(s->complicated_utf8_line)
	{
		/* examine earlier chars in line to see how many bytes they used */
		int i, c, n;

		/* We cache the last result to avoid N^2 slowness on very
		   long lines.  Thanks to Gait Boxman for suggesting this. */

		if(s->next < s->cached_line_char)
		{
		/* Moved backwards in line; doesn't happen, I think */
		s->cached_line_char = 0;
		s->cached_line_byte = 0;
		}

		n = s->cached_line_byte;
		for(i = s->cached_line_char; i < s->next; i++)
		{
		c = s->line[i];
		if(c <= 0x7f)
			n += 1;
		else if(c <= 0x7ff)
			n += 2;
		else if(c >= 0xd800 && c <= 0xdfff)
			/* One of a surrogate pair, count 2 each */
			n += 2;
		else if(c <= 0xffff)
			n += 3;
		else if(c <= 0x1ffff)
			n += 4;
		else if(c <= 0x3ffffff)
			n += 5;
		else
			n += 6;

		}

		s->cached_line_char = s->next;
		s->cached_line_byte = n;

		return s->bytes_before_current_line + n;
	}
	else
		return s->bytes_before_current_line + s->next;
	default:
	return -1;
	}
#endif
}

int SourceSeek(InputSource s, int byte_offset)
{
	s->line_length = 0;
	s->next = 0;
	s->seen_eoe = 0;
	s->bytes_consumed = s->bytes_before_current_line = byte_offset;
	s->nextin = s->insize = 0;
	/* XXX line number will be wrong! */
	s->line_number = -999999;
	return Fseek(s->file16, byte_offset, SEEK_SET);
}

/* reader for internal entities, doesn't need to do any encoding translation */

static void internal_reader(InputSource s)
{
	/* XXX reconsider use of FILE16 for internal entities */

	struct _FILE16 {
	void *handle;
	int handle2, handle3;
	/* we don't need the rest here */
	};

	Char *p;
	struct _FILE16 *f16 = (struct _FILE16 *)s->file16;

	if(!*(Char *)((char *)f16->handle + f16->handle2))
	{
	s->line_length = 0;
	return;
	}

	s->line = (Char *)((char *)f16->handle + f16->handle2);
	for(p=s->line; *p && *p != '\n'; p++)
	;
	if(*p)
	p++;
	f16->handle2 = (char *)p - (char *)f16->handle;
	s->line_length = p - s->line;

	s->bytes_before_current_line = f16->handle2;
	s->next = 0;
	if(s->not_read_yet)
	s->not_read_yet = 0;
	else
	s->line_number++;

	return;
}

/*
 * Translate bytes starting at s->inbuf[s->nextin] until end of line
 * or until s->nextin == s->insize.
 * The output is placed starting at s->line[s->nextout], which must
 * have enough space.
 * Returns zero at end of line or error, one if more input is needed.
 * In the case of an error (encoding error or illegal XML character) we
 * set s->seen_error and put a BADCHAR in the output as a marker.
 */


#define SETUP \
	int c;		/* can't use Char, it might be >0x10000 */ \
\
	/* local copies of fields of s, that are not modified */ \
\
	unsigned char * const inbuf = s->inbuf; \
	const int insize = s->insize; \
	const int startin = s->nextin; \
	Char * const outbuf = s->line; \
	unsigned char *map = s->map; \
\
	/* local copies of fields of s, that are modified (and restored) */ \
\
	int nextin = s->nextin; \
	int nextout = s->line_length; \
	int ignore_linefeed = s->ignore_linefeed; \

#define ERROR_CHECK \
	if(c == -1) \
	{ \
	/* There was an error.  Put a BADCHAR character (see input.h) in \
	   as a marker, and end the line. */ \
	outbuf[nextout++] = BADCHAR; \
	s->seen_error = 1; \
	goto end_of_line; \
	}

#define LINEFEED \
	if((c == '\n' || (c == 0x85 && map == xml_char_map_11)) && \
	   ignore_linefeed) \
	{ \
	/* Ignore lf at start of line if last line ended with cr */ \
	ignore_linefeed = 0; \
	s->bytes_before_current_line += (nextin - startin); \
	continue; \
	} \
\
	ignore_linefeed = 0; \
\
	if(c == '\r') \
	{ \
	s->line_end_was_cr = 1; \
	c = '\n'; \
	} \
	if((c == 0x85 || c == 0x2028) && map == xml_char_map_11) \
		c = '\n';

#define OUTPUT \
	outbuf[nextout++] = c; \
\
	if(c == '\n') \
		goto end_of_line

#define OUTPUT_WITH_SURROGATES \
	if(c >= 0x10000) \
	{ \
	/* Use surrogates */ \
	outbuf[nextout++] = ((c - 0x10000) >> 10) + 0xd800; \
	outbuf[nextout++] = ((c - 0x10000) & 0x3ff) + 0xdc00; \
	} \
	else \
	outbuf[nextout++] = c; \
\
	if(c == '\n') \
		goto end_of_line

#define MORE_BYTES \
 more_bytes: \
	s->nextin = nextin; \
	s->line_length = nextout; \
		s->ignore_linefeed = ignore_linefeed; \
	return 1 \

#define END_OF_LINE \
 end_of_line: \
	s->nextin = nextin; \
	s->line_length = nextout; \
		s->ignore_linefeed = ignore_linefeed; \
	return 0

#if CHAR_SIZE == 8

static int translate_8bit(InputSource s)
{
	SETUP;

	while(nextin < insize)
	{
	c = inbuf[nextin++];

	if(!is_xml_legal(c, map))
	{
		sprintf(s->error_msg,
			"Illegal character <0x%x> at file offset %d",
			c, s->bytes_consumed + nextin - startin - 1);
		c = -1;
	}

	ERROR_CHECK;

	LINEFEED;

	OUTPUT;
	}

	MORE_BYTES;

	END_OF_LINE;
}

#else

static int translate_latin(InputSource s)
{
	CharacterEncoding enc = s->entity->encoding;
	int *to_unicode = iso_to_unicode[enc - CE_ISO_8859_2];
	SETUP;

	while(nextin < insize)
	{
	c = to_unicode[inbuf[nextin++]];
	if(c == -1)
	{
		sprintf(s->error_msg,
			"Illegal byte <0x%x> for encoding %s at file offset %d",
			inbuf[nextin-1], CharacterEncodingName[enc],
			s->bytes_consumed + nextin - 1 - startin);
	}
	else if(!is_xml_legal(c, map))
	{
		sprintf(s->error_msg,
			"Illegal character <0x%x> "
			"immediately before file offset %d",
			c, s->bytes_consumed + nextin - startin);
		c = -1;
	}

	ERROR_CHECK;

	LINEFEED;

	OUTPUT;
	}

	MORE_BYTES;

	END_OF_LINE;
}

static int translate_latin1(InputSource s)
{
	SETUP;

	while(nextin < insize)
	{
	c = inbuf[nextin++];
	if(!is_xml_legal(c, map))
	{
		sprintf(s->error_msg,
			"Illegal character <0x%x> "
			"immediately before file offset %d",
			c, s->bytes_consumed + nextin - startin);
		c = -1;
	}

	ERROR_CHECK;

	LINEFEED;

	OUTPUT;
	}

	MORE_BYTES;

	END_OF_LINE;
}

static int translate_utf8(InputSource s)
{
	int more, i, mincode;
	SETUP;

	while(nextin < insize)
	{
	c = inbuf[nextin++];
	if(c <= 0x7f)
		goto gotit;
	else if(c <= 0xc0 || c >= 0xfe)
	{
		sprintf(s->error_msg,
		   "Illegal UTF-8 start byte <0x%x> at file offset %d",
			c, s->bytes_consumed + nextin - 1 - startin);
		c = -1;
		goto gotit;
	}
	else if(c <= 0xdf)
	{
		c &= 0x1f;
		more = 1;
		mincode = 0x80;
	}
	else if(c <= 0xef)
	{
		c &= 0x0f;
		more = 2;
		mincode = 0x800;
	}
	else if(c <= 0xf7)
	{
		c &= 0x07;
		more = 3;
		mincode = 0x10000;
	}
	else if(c <= 0xfb)
	{
		c &= 0x03;
		more = 4;
		mincode = 0x200000;
	}
	else
	{
		c &= 0x01;
		more = 5;
		mincode = 0x4000000;
	}
	if(nextin+more > insize)
	{
		nextin--;
		goto more_bytes;
	}
	s->complicated_utf8_line = 1;
	s->cached_line_char = 0;
	s->cached_line_byte = 0;

	for(i=0; i<more; i++)
	{
		int t = inbuf[nextin++];
		if((t & 0xc0) != 0x80)
		{
		c = -1;
		sprintf(s->error_msg,
			  "Illegal UTF-8 byte %d <0x%x> at file offset %d",
			i+2, t,
			s->bytes_consumed + nextin - 1 - startin);
		break;
		}
		c = (c << 6) + (t & 0x3f);
	}

	if(c < mincode && c != -1)
	{
		sprintf(s->error_msg,
			"Illegal (non-shortest) UTF-8 sequence for "
			"character <0x%x> "
			"immediately before file offset %d",
			c, s->bytes_consumed + nextin - startin);
		c = -1;
	}

	gotit:
	if(c >= 0 && !is_xml_legal(c, map))
	{
		sprintf(s->error_msg,
			"Illegal character <0x%x> "
			"immediately before file offset %d",
			c, s->bytes_consumed + nextin - startin);
		c = -1;
	}

	ERROR_CHECK;

	LINEFEED;

	OUTPUT_WITH_SURROGATES;

	if(c == '>' && s->read_carefully)
	{
		s->line_is_incomplete = 1;
		goto end_of_line;
	}
	}

	MORE_BYTES;

	END_OF_LINE;
}

static int translate_utf16(InputSource s)
{
	int le = (s->entity->encoding == CE_ISO_10646_UCS_2L ||
		  s->entity->encoding == CE_UTF_16L);
	SETUP;

	while(nextin < insize)
	{
	if(nextin+2 > insize)
		goto more_bytes;

	if(le)
		c = (inbuf[nextin+1] << 8) + inbuf[nextin];
	else
		c = (inbuf[nextin] << 8) + inbuf[nextin+1];
	nextin += 2;

	if(c >= 0xdc00 && c <= 0xdfff) /* low (2nd) surrogate */
	{
		if(s->expecting_low_surrogate)
		s->expecting_low_surrogate = 0;
		else
		{
		sprintf(s->error_msg,
			"Unexpected low surrogate <0x%x> "
			"at file offset %d",
			c, s->bytes_consumed + nextin - startin - 2);
		c = -1;
		}
	}
	else if(s->expecting_low_surrogate)
	{
		sprintf(s->error_msg,
			"Expected low surrogate but got <0x%x> "
			"at file offset %d",
			c, s->bytes_consumed + nextin - startin - 2);
		c = -1;
	}
	if(c >= 0xd800 && c <= 0xdbff) /* high (1st) surrogate */
		s->expecting_low_surrogate = 1;

	if(c >= 0 && !is_xml_legal(c, map) &&
	   /* surrogates are legal in utf-16 */
	   !(c >= 0xd800 && c <= 0xdfff))
	{
		sprintf(s->error_msg,
			"Illegal character <0x%x> "
			"immediately before file offset %d",
			c, s->bytes_consumed + nextin - startin);
		c = -1;
	}

	ERROR_CHECK;

	LINEFEED;

	OUTPUT;
	}

	MORE_BYTES;

	END_OF_LINE;
}

#endif

static void external_reader(InputSource s)
{
	int startin = s->nextin;
	int (*trans)(InputSource);
	int continuing_incomplete_line = s->line_is_incomplete;

	if(s->seen_error)
	return;

	s->line_is_incomplete = 0;
	if(!continuing_incomplete_line)
	{
	s->ignore_linefeed = s->line_end_was_cr;
	s->line_end_was_cr = 0;
	s->complicated_utf8_line = 0;
	s->line_length = 0;
	s->bytes_before_current_line = s->bytes_consumed;
	s->next = 0;
	}

#if CHAR_SIZE == 8
	trans = translate_8bit;
#else
	switch(s->entity->encoding)
	{
	case CE_ISO_646:	/* should really check for >127 in this case */
	case CE_ISO_8859_1:
	case CE_unspecified_ascii_superset:
	trans = translate_latin1;
	break;
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
	trans = translate_latin;
	break;
	case CE_UTF_8:
	trans = translate_utf8;
	break;
	case CE_ISO_10646_UCS_2B:
	case CE_UTF_16B:
	case CE_ISO_10646_UCS_2L:
	case CE_UTF_16L:
	trans=translate_utf16;
	break;
	default:
	assert(1==0);
	break;
	}
#endif

	while(1)
	{
	/* There are never more characters than bytes in the input */
	if(s->line_alloc < s->line_length + (s->insize - s->nextin))
	{
		s->line_alloc = s->line_length + (s->insize - s->nextin);
		s->line = Realloc(s->line, s->line_alloc * sizeof(Char));
	}

	if(trans(s) == 0)
	{
		s->bytes_consumed += (s->nextin - startin);
		if(s->not_read_yet)
		s->not_read_yet = 0;
		else if(!continuing_incomplete_line)
		s->line_number++;
		return;
	}
	else
	{
		int i, bytes_read, remaining = 0;

		/* more input needed */

		/* Copy down any partial character */

		remaining = s->insize - s->nextin;
		for(i=0; i<remaining; i++)
		s->inbuf[i] = s->inbuf[s->nextin + i];

		/* Get another block */

		s->bytes_consumed += (s->nextin - startin);

		bytes_read = Readu(s->file16,
				   s->inbuf+remaining, sizeof(s->inbuf)-remaining);
		s->nextin = startin = 0;

		if(bytes_read <= 0)
		{
		if(remaining > 0)
		{
			/* EOF or error in the middle of a character */
			sprintf(s->error_msg, "EOF or error inside character at "
					  "file offset %d",
				s->bytes_consumed + remaining);
			/* There must be space because there is unconsumed input */
			s->line[s->line_length++] = BADCHAR;
			s->seen_error = 1;
		}

		s->insize = 0;

		if(s->not_read_yet)
			s->not_read_yet = 0;
		else if(!continuing_incomplete_line)
			s->line_number++;

		return;
		}

		s->insize = bytes_read + remaining;
	}
	}
}

void determine_character_encoding(InputSource s)
{
	Entity e = s->entity;
	int nread;
	unsigned char *b = (unsigned char *)s->inbuf;

	b[0] = b[1] = b[2] = b[3] = 0;

	while(s->insize < 4)
	{
	nread = Readu(s->file16, s->inbuf + s->insize, 4 - s->insize);
	if(nread == -1)
		return;
	if(nread == 0)
		break;
	s->insize += nread;
	}

#if 0
	if(b[0] == 0 && b[1] == 0 && b[2] == 0 && b[3] == '<')
	e->encoding = CE_ISO_10646_UCS_4B;
	else if(b[0] == '<' && b[1] == 0 && b[2] == 0 && b[3] == 0)
	e->encoding = CE_ISO_10646_UCS_4L;
	else
#endif
	if(b[0] == 0xef && b[1] == 0xbb && b[2] == 0xbf)
	{
	e->encoding = CE_UTF_8;
	s->nextin = 3;
	s->bytes_consumed = 3;
	}
	else
	if(b[0] == 0xfe && b[1] == 0xff)
	{
	e->encoding = CE_UTF_16B;
	s->nextin = 2;
	s->bytes_consumed = 2;
	}
	else if(b[0] == 0 && b[1] == '<' && b[2] == 0 && b[3] == '?')
	e->encoding = CE_UTF_16B;
	else if(b[0] == 0xff && b[1] == 0xfe)
	{
	e->encoding = CE_UTF_16L;
	s->nextin = 2;
	s->bytes_consumed = 2;
	}
	else if(b[0] == '<' && b[1] == 0 && b[2] == '?' && b[3] == 0)
	e->encoding = CE_UTF_16L;
	else
	{
#if CHAR_SIZE == 8
	e->encoding = CE_unspecified_ascii_superset;
#else
		e->encoding = CE_UTF_8;
	s->read_carefully = 1;
#endif
	}
}

int get_with_fill(InputSource s)
{
	int old_length = s->next;
	int old_cu8l = s->complicated_utf8_line;
	int old_bbcl = s->bytes_before_current_line;
	int old_ln = s->line_number;

	assert(!s->seen_eoe);

	if(s->seen_error)
	{
	s->seen_eoe = 1;
	return XEOE;
	}

	s->reader(s);

	if(s->line_length == 0)
	{
	/* Restore old line */
	s->line_length = s->next = old_length;
	s->complicated_utf8_line = old_cu8l;
	s->bytes_before_current_line = old_bbcl;
	s->line_number = old_ln;
	s->seen_eoe = 1;
#if 0
	fprintf(stderr, "EOE on %s\n", EntityDescription(s->entity));
#endif
	return XEOE;
	}

	if(s->next == s->line_length)
	{
	/* "incomplete" line turned out to be at EOF */
#if 0
	fprintf(stderr, "EOE on %s\n", EntityDescription(s->entity));
#endif
	s->seen_eoe = 1;
	return XEOE;
	}

#if 0
	Fprintf(Stderr, "line (len %d, next %d): |%.*S|\n",
		s->line_length, s->next, s->line_length, s->line);
#endif

	return s->line[s->next++];
}
