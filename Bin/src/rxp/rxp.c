#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>

#if defined(WIN32) && ! defined(__CYGWIN__)
#include <io.h>
#include <fcntl.h>
#endif

#ifndef WIN32
#define TIME_LIMIT
#endif

#ifdef TIME_LIMIT
#include <signal.h>
#include <sys/types.h>
#include <sys/time.h>
#include <sys/resource.h>
#endif

#include "system.h"
#include "charset.h"
#include "string16.h"
#include "dtd.h"
#include "url.h"
#include "input.h"
#include "xmlparser.h"
#include "stdio16.h"
#include "version.h"
#include "namespaces.h"
#include "infoset-print.h"
#include "catalog.h"

int attr_compare(const void *a, const void *b);
void print_tree(Parser p, XBit bit);
void print_bit(Parser p, XBit bit);
void print_ns_attrs(NamespaceBinding ns, int count);
void print_namespaces(NamespaceBinding ns);
void print_attrs(ElementDefinition e, Attribute a);
void print_text(Char *text, int is_attr);
int printable(int c);
void print_special(int c);
void print_text_bit(Char *text);
void dtd_cb(XBit bit, void *arg);
void dtd_cb2(XBit bit, void *arg);
void print_canonical_dtd(Parser p, const Char *name);
InputSource entity_open(Entity ent, void *arg);
static const char8 *minimal_uri(const char8 *uri, const char8 *base);

int verbose = 0, expand = 1, nsgml = 0,
	attr_defaults = 0, merge = 0, strict_xml = 0, tree = 0, validate = 0,
	xml_space = 0, namespaces = 0, simple_error = 0, experiment = 0,
	read_dtd = 0, unicode_check = 0, xml_id = 0;
enum {o_unspec, o_none, o_bits, o_plain, o_can1, o_can2, o_can3, o_infoset, o_diff, o_diff2} output_format = o_unspec;
char *enc_name = 0, *base_uri = 0, *my_dtd_name, *my_dtd_sysid = 0;
CharacterEncoding encoding = CE_unknown;
InputSource source = 0;
int need_canonical_dtd = 0;
int xml_version;
char * err_file = 0;

#ifdef TIME_LIMIT
void time_exceeded(int sig);
int time_limit = 0;
#endif

#define canonical_output (output_format >= o_can1)

Vector(XBit, bits);
Vector(XBit, dtd_bits);

int main(int argc, char **argv)
{
	int i;
	Parser p;
	char *s, *url;
	Entity ent = 0;

	/* Sigh... guess which well-known system doesn't have getopt() */

	for(i = 1; i < argc; i++)
	{
	if(argv[i][0] != '-')
		break;
	for(s = &argv[i][1]; *s; s++)
		switch(*s)
		{
		case 'v':
		verbose = 1;
		break;
		case 'V':
		validate++;
		break;
		case 'a':
		attr_defaults = 1;
		break;
		case 'e':
		fprintf(stderr, "warning: -e flag is obsolete, entities "
					"are expanded unless -E is specified\n");
		break;
		case 'E':
		expand = 0;
		break;
		case 'b':
		output_format = o_bits;
		break;
		case 'o':
		if(++i >= argc)
		{
			fprintf(stderr, "-o requires argument\n");
			return 1;
		}
		switch(argv[i][0])
		{
		case 'b':
			output_format = o_bits;
			break;
		case 'p':
			output_format = o_plain;
			break;
		case '0':
			output_format = o_none;
			break;
		case '1':
			output_format = o_can1;
			break;
		case '2':
			output_format = o_can2;
			need_canonical_dtd = 1;
			break;
		case '3':
			output_format = o_can3;
			need_canonical_dtd = 1;
			break;
		case 'i':
			output_format = o_infoset;
			namespaces = 1;
			attr_defaults = 1;
			merge = 0;
			break;
		case 'd':
			output_format = o_diff;
			break;
		case 'D':
			output_format = o_diff2;
			break;
		default:
			fprintf(stderr, "bad output format %s\n", argv[i]);
			return 1;
		}
		break;
		case 's':
		output_format = o_none;
		break;
		case 'n':
		nsgml = 1;
		break;
		case 'N':
		namespaces = 1;
		break;
		case 'c':
		if(++i >= argc)
		{
			fprintf(stderr, "-c requires argument\n");
			return 1;
		}
		enc_name = argv[i];
		break;
		case 'm':
		merge = 1;
		break;
		case 't':
		tree = 1;
		break;
		case 'x':
		strict_xml = 1;
		attr_defaults = 1;
		break;
		case 'R':
#ifdef TIME_LIMIT
		if(++i >= argc)
		{
			fprintf(stderr, "-R requires argument\n");
			return 1;
		}
		time_limit = atoi(argv[i]);
#else
		fprintf(stderr, "-R not supported on this system\n");
		return 1;
#endif
		break;
		case 'S':
		xml_space = 1;
		break;
		case 'z':
		simple_error = 1;
		break;
		case 'u':
		if(++i >= argc)
		{
			fprintf(stderr, "-u requires argument\n");
			return 1;
		}
		base_uri = argv[i];
		break;
		case 'U':
		if(++i >= argc)
		{
			fprintf(stderr, "-U requires argument\n");
			return 1;
		}
		switch(argv[i][0])
		{
		case '0':
			unicode_check = 0;
			break;
		case '1':
			unicode_check = 1;
			break;
		case '2':
			unicode_check = 2;
			break;
		default:
			fprintf(stderr, "bad Unicode check level %s\n", argv[i]);
			return 1;
		}
		break;
		case 'd':
		read_dtd = 1;
		break;
		case 'D':
		if(i+2 >= argc)
		{
			fprintf(stderr, "-D requires 2 arguments\n");
			return 1;
		}
		my_dtd_name = argv[++i];
		my_dtd_sysid = argv[++i];
		break;
		case 'i':
		xml_id = 1;
		break;
		case 'I':
		xml_id = 2;
		break;
		case '.':
		experiment = 1;
		break;
		case 'f':
		if(++i >= argc)
		{
			fprintf(stderr, "-f requires argument\n");
			return 1;
		}
		err_file = argv[i];
		break;
		default:
		fprintf(stderr,
			"usage: rxp [-abeiImnNRsStvVx] [-o b|0|1|2|3|i|d] [-U 0|1|2] [-c encoding] [-D name sysid] [-f error_file] [-u base_uri] [url]\n");
		return 1;
		}
	}

#ifdef TIME_LIMIT
	if(time_limit > 0)
	{
	struct rlimit r;
	r.rlim_cur = r.rlim_max = time_limit;
	signal(SIGXCPU, time_exceeded);
	if(setrlimit(RLIMIT_CPU, &r) < 0)
	{
		perror("setrlimit");
		return 1;
	}
	}
#endif

	init_parser();

	if(verbose)
	fprintf(stderr, "%s\n", rxp_version_string);

	if(err_file)
	{
	FILE * fp = freopen(err_file, "w", stderr);
	if(fp == NULL)
	{
		printf("Error assigning %s to stderr!\n", err_file);
		exit(1);
	}
	}

	p = NewParser();

	/* For use in CGI scripts, so user-supplied name is not parsed as
	   command argument */
	url = getenv("RXPURL");

	if(!url)
	/* don't use catalogue in CGI scripts */
	CatalogEnable(p);

	if(!url && i < argc)
	url = argv[i];

	if(url)
	{
	ent = NewExternalEntity(0, 0, url, 0, 0);
	if(ent)
	{
		if(p->entity_opener)
		source = p->entity_opener(ent, p->entity_opener_arg);
		else
		source = EntityOpen(ent);
	}
	}
	else
	source = SourceFromStream("<stdin>", stdin);

	if(!source)
	return 1;

	if(base_uri)
	{
	/* Merge with default base URI so we can use a filename as base */
	base_uri = url_merge(base_uri, 0, 0, 0, 0, 0);
	EntitySetBaseURL(source->entity, base_uri);
	free(base_uri);
	}

	if(validate)
	ParserSetFlag(p, Validate, 1);
	if(validate > 1)
	ParserSetFlag(p, ErrorOnValidityErrors, 1);

	if(xml_id)
	ParserSetFlag(p, XMLID, 1);
	if(xml_id > 1)
	ParserSetFlag(p, XMLIDCheckUnique, 1);

	if(read_dtd)
	{
	ParserSetFlag(p, TrustSDD, 0);
	ParserSetFlag(p, ProcessDTD, 1);
	}

	if(xml_space)
	ParserSetFlag(p, XMLSpace, 1);

	if(namespaces)
	ParserSetFlag(p, XMLNamespaces, 1);

	if(experiment)
	{
	ParserSetFlag(p, RelaxedAny, 1);
	ParserSetFlag(p, AllowUndeclaredNSAttributes, 1);
	}

	if(output_format == o_bits)
	{
	ParserSetDtdCallback(p, dtd_cb);
	ParserSetDtdCallbackArg(p, p);
	}

	if(output_format == o_infoset)
	{
	ParserSetFlag(p, ReturnNamespaceAttributes, 1);
	ParserSetDtdCallback(p, dtd_cb2);
	ParserSetDtdCallbackArg(p, p);
	}

	ParserSetFlag(p, SimpleErrorFormat, simple_error);

	if(attr_defaults)
	ParserSetFlag(p, ReturnDefaultedAttributes, 1);

	if(!expand)
	{
	ParserSetFlag(p, ExpandGeneralEntities, 0);
	ParserSetFlag(p, ExpandCharacterEntities, 0);
	}

	if(merge)
	ParserSetFlag(p, MergePCData, 1);

	if(nsgml)
	{
	ParserSetFlag(p, XMLSyntax, 0);
	ParserSetFlag(p, XMLPredefinedEntities, 0);
	ParserSetFlag(p, XMLExternalIDs, 0);
	ParserSetFlag(p, XMLMiscWFErrors, 0);
	ParserSetFlag(p, TrustSDD, 0);
	ParserSetFlag(p, ErrorOnUnquotedAttributeValues, 0);
	ParserSetFlag(p, ExpandGeneralEntities, 0);
	ParserSetFlag(p, ExpandCharacterEntities, 0);
/*	ParserSetFlag(p, TrimPCData, 1); */
	}

	if(strict_xml)
	{
	ParserSetFlag(p, ErrorOnBadCharacterEntities, 1);
	ParserSetFlag(p, ErrorOnUndefinedEntities, 1);
	ParserSetFlag(p, XMLStrictWFErrors, 1);
	ParserSetFlag(p, WarnOnRedefinitions, 0);

	}

	if(unicode_check)
	{
	ParserSetFlag(p, XML11CheckNF, 1);
	if(unicode_check == 2)
		ParserSetFlag(p, XML11CheckExists, 1);
	}

	if(ParserPush(p, source) == -1)
	{
	ParserPerror(p, &p->xbit);
	return 1;
	}

	if(my_dtd_name)
	{
	Entity my_dtd = NewExternalEntity(0, 0, my_dtd_sysid, 0, source->entity);
	p->dtd->name = strdup_char8_to_Char(my_dtd_name);
	p->dtd->internal_part = 0;
	p->dtd->external_part = my_dtd;
	ParseDtd(p, my_dtd);
	if(p->xbit.type == XBIT_error)
	{
		if(my_dtd_sysid[0] == '-')
		fprintf(stderr, "warning: DTD System ID starts with \"-\";"
					" did you forget the name argument?\n");
		print_bit(p, &p->xbit);
		return 1;
	}
	}

	if(enc_name)
	{
	encoding = FindEncoding(enc_name);

	if(encoding == CE_unknown)
	{
		fprintf(stderr, "unknown encoding %s\n", enc_name);
		return 1;
	}
	}
	else if(strict_xml)
	encoding = CE_UTF_8;
	else
	encoding = source->entity->encoding;

	SetFileEncoding(Stdout, encoding);

	if(output_format == o_unspec)
	{
	if(strict_xml)
		output_format = o_can1;
	else
		output_format = o_plain;
	}

	if(verbose)
	fprintf(stderr, "Input encoding %s, output encoding %s\n",
		CharacterEncodingNameAndByteOrder[source->entity->encoding],
		CharacterEncodingNameAndByteOrder[encoding]);

	xml_version = p->xml_version;

	if((source->entity->ml_decl == ML_xml || encoding != CE_UTF_8) &&
	   output_format == o_plain)
	{
	Printf("<?xml");

	if(source->entity->version_decl)
		Printf(" version=\"%s\"", source->entity->version_decl);
	else
		Printf(" version=\"1.0\"");

	if(encoding == CE_unspecified_ascii_superset)
	{
		if(source->entity->encoding_decl != CE_unknown)
		Printf(" encoding=\"%s\"",
			   CharacterEncodingName[source->entity->encoding_decl]);
	}
	else
		Printf(" encoding=\"%s\"",
		   CharacterEncodingName[encoding]);

	if(source->entity->standalone_decl != SDD_unspecified)
		Printf(" standalone=\"%s\"",
		   StandaloneDeclarationName[source->entity->standalone_decl]);

	Printf("?>\n");
	}

	if(source->entity->ml_decl == ML_xml && canonical_output &&
	   xml_version > XV_1_0)
	Printf("<?xml version=\"%s\"?>", source->entity->version_decl);

	VectorInit(bits);

	while(1)
	{
	XBit bit;

	if(output_format == o_infoset)
	{
		bit = ReadXTree(p);
		if(bit->type == XBIT_dtd)
		{
		bit->children = dtd_bits;
		bit->nchildren = VectorCount(dtd_bits);
		}
		if(bit->type == XBIT_error)
		ParserPerror(p, bit);
		else if(bit->type == XBIT_eof)
		{
		infoset_print(Stdout, p, bits, VectorCount(bits));
		return 0;
		}
		else
		VectorPush(bits, bit);
	}
	else if(tree)
	{
		bit = ReadXTree(p);
		print_tree(p, bit);
	}
	else
	{
		bit = ReadXBit(p);
		print_bit(p, bit);
	}
	if(bit->type == XBIT_eof)
	{
		int status = p->seen_validity_error ? 2 : 0;

		if(output_format == o_plain ||
		   output_format == o_diff)
		Printf("\n");

		/* Not necessary, but helps me check for leaks */
		if(tree)
		FreeXTree(bit);
		else
		FreeXBit(bit);
		FreeDtd(p->dtd);
		FreeParser(p);
		if(ent)
		FreeEntity(ent);
		deinit_parser();

		return status;
	}
	if(bit->type == XBIT_error)
		return 1;
	if(tree)
		FreeXTree(bit);
	else
	{
		if(output_format != o_infoset)
		FreeXBit(bit);
	}
	}
}

void print_tree(Parser p, XBit bit)
{
	int i;
	struct xbit endbit;

	print_bit(p, bit);
	if(bit->type == XBIT_start)
	{
	for(i=0; i<bit->nchildren; i++)
		print_tree(p, bit->children[i]);
	endbit.type = XBIT_end;
	endbit.element_definition = bit->element_definition;
	endbit.ns_element_definition = bit->ns_element_definition;
	endbit.byte_offset = -1;
	print_bit(p, &endbit);
	}
}

void print_bit(Parser p, XBit bit)
{
	const char *sys, *pub;
	char *ws[] = {"u", "d", "p"};

	if(output_format == o_none && bit->type != XBIT_error)
	return;

	if(output_format == o_bits)
	{
	Printf("At %d: ", bit->byte_offset);
	switch(bit->type)
	{
	case XBIT_eof:
		Printf("EOF\n");
		break;
	case XBIT_error:
		ParserPerror(p, bit);
		break;
	case XBIT_dtd:
		sys = pub = "<none>";
		if(p->dtd->external_part)
		{
		if(p->dtd->external_part->publicid)
			pub = p->dtd->external_part->publicid;
		if(p->dtd->external_part->systemid)
			sys = p->dtd->external_part->systemid;
		}
		Printf("doctype: %S pubid %s sysid %s\n", p->dtd->name, pub, sys);
		break;
	case XBIT_start:
		if(namespaces && bit->ns_element_definition)
		Printf("start: {%S}%S ",
			   bit->ns_element_definition->namespace->nsname,
			   bit->element_definition->local);
		else
		Printf("start: %S ", bit->element_definition->name);
		if(xml_space)
		Printf("(ws=%s) ", ws[bit->wsm]);
		print_attrs(0, bit->attributes);
		print_namespaces(bit->ns_dict);
		Printf("\n");
		break;
	case XBIT_empty:
		if(namespaces && bit->ns_element_definition)
		Printf("empty: {%S}%S ",
			   bit->ns_element_definition->namespace->nsname,
			   bit->element_definition->local);
		else
		Printf("empty: %S ", bit->element_definition->name);
		if(xml_space)
		Printf("(ws=%s) ", ws[bit->wsm]);
		print_attrs(0, bit->attributes);
		print_namespaces(bit->ns_dict);
		Printf("\n");
		break;
	case XBIT_end:
		if(namespaces && bit->ns_element_definition)
		Printf("end: {%S}%S ",
			   bit->ns_element_definition->namespace->nsname,
			   bit->element_definition->local);
		else
		Printf("end: %S ", bit->element_definition->name);
		Printf("\n");
		break;
	case XBIT_pi:
		Printf("pi: %S: ", bit->pi_name);
		print_text_bit(bit->pi_chars);
		Printf("\n");
		break;
	case XBIT_cdsect:
		Printf("cdata: ");
		print_text_bit(bit->cdsect_chars);
		Printf("\n");
		break;
	case XBIT_pcdata:
		Printf("pcdata: ");
		print_text_bit(bit->pcdata_chars);
		Printf("\n");
		break;
	case XBIT_comment:
		Printf("comment: ");
		print_text_bit(bit->comment_chars);
		Printf("\n");
		break;
	default:
		fprintf(stderr, "***%s\n", XBitTypeName[bit->type]);
		exit(1);
		break;
	}
	}
	else
	{
	switch(bit->type)
	{
	case XBIT_eof:
		break;
	case XBIT_error:
		ParserPerror(p, bit);
		break;
	case XBIT_dtd:
		if(canonical_output)
		/* no doctype in canonical XML */
		break;
		Printf("<!DOCTYPE %S", p->dtd->name);
		if(p->dtd->external_part)
		{
		if(p->dtd->external_part->publicid)
			Printf(" PUBLIC \"%s\"", p->dtd->external_part->publicid);
		else if(p->dtd->external_part->systemid)
			Printf(" SYSTEM");
		if(p->dtd->external_part->systemid)
			Printf(" \"%s\"", p->dtd->external_part->systemid);
		}
		if(p->dtd->internal_part)
		Printf(" [%S]", p->dtd->internal_part->text);
		Printf(">\n");
		break;
	case XBIT_start:
	case XBIT_empty:
		if(need_canonical_dtd)
		print_canonical_dtd(p, bit->element_definition->name);
		Printf("<%S", bit->element_definition->name);
		print_attrs(bit->element_definition, bit->attributes);
		print_ns_attrs(bit->ns_dict, bit->nsc);
		if(bit->type == XBIT_start)
		Printf(">");
		else if(canonical_output && output_format < o_diff)
		Printf("></%S>", bit->element_definition->name);
		else if(output_format == o_diff)
		Printf("></%S>", bit->element_definition->name);
		else if(output_format > o_diff)
		Printf(">\n</%S>", bit->element_definition->name);
		else
		Printf("/>");
		if(output_format == o_diff2)
		Printf("\n");
		break;
	case XBIT_end:
		Printf("</%S>", bit->element_definition->name);
		if(output_format == o_diff2)
		Printf("\n");
		break;
	case XBIT_pi:
		Printf("<?%S %S%s",
		   bit->pi_name, bit->pi_chars, nsgml ? ">" : "?>");
		if((p->state <= PS_prolog2 && !canonical_output) ||
		   output_format == o_diff2)
		Printf("\n");
		break;
	case XBIT_cdsect:
		if(canonical_output)
		/* Print CDATA sections as plain PCDATA in canonical XML */
		print_text(bit->cdsect_chars, 0);
		else
		Printf("<![CDATA[%S]]>", bit->cdsect_chars);
		if(output_format == o_diff2)
		Printf("\n");
		break;
	case XBIT_pcdata:
		if(output_format == o_diff2)
		{
		if(bit->pcdata_chars[0] == '\n')
			/* we have already printed a linefeed */
			print_text(bit->pcdata_chars+1, 0);
		else
			print_text(bit->pcdata_chars, 0);
		if(bit->pcdata_chars[Strlen(bit->pcdata_chars) - 1] != '\n')
			Printf("\n");
		}
		else if(output_format != o_can3 || !bit->pcdata_ignorable_whitespace)
		print_text(bit->pcdata_chars, 0);
		break;
	case XBIT_comment:
		if(canonical_output)
		/* no comments in canonical XML */
		break;
		Printf("<!--%S-->", bit->comment_chars);
		if(p->state <= PS_prolog2 || output_format == o_diff2)
		Printf("\n");
		break;
	default:
		fprintf(stderr, "\n***%s\n", XBitTypeName[bit->type]);
		exit(1);
		break;
	}
	}
}

int attr_compare(const void *a, const void *b)
{
	return Strcmp((*(Attribute *)a)->definition->name,
		  (*(Attribute *)b)->definition->name);
}

void print_attrs(ElementDefinition e, Attribute a)
{
	Attribute b;
	Attribute *aa;
	int i, n = 0;

	for(b=a; b; b=b->next)
	n++;

	if(n == 0)
	return;

	aa = malloc(n * sizeof(*aa));

	for(i=0, b=a; b; i++, b=b->next)
	aa[i] = b;

	if(canonical_output)
	qsort((void *)aa, n, sizeof(*aa), attr_compare);

	for(i=0; i<n; i++)
	{
	if(output_format == o_bits && namespaces &&
	   aa[i]->ns_definition && !aa[i]->ns_definition->element)
		Printf(" {%S}%S=\"",
		   aa[i]->ns_definition->namespace->nsname,
		   aa[i]->definition->local);
	else
		Printf(" %S=\"", aa[i]->definition->name);
	print_text(aa[i]->value, 1);
	Printf("\"");
	}

	free(aa);
}

void print_text_bit(Char *text)
{
	int i;

	for(i=0; i<50 && text[i]; i++)
	if(text[i] == '\n' || text[i] == '\r')
		text[i] = '~';
	Printf("%.50S", text);
}

void dtd_cb(XBit bit, void *arg)
{
	Printf("In DTD: ");
	print_bit(arg, bit);
	FreeXBit(bit);
}

void dtd_cb2(XBit bit, void *arg)
{
	XBit copy;

	if(bit->type == XBIT_comment)
	return;

	copy = malloc(sizeof(*copy));

	*copy = *bit;
	VectorPush(dtd_bits, copy);
}

void print_text(Char *text, int is_attr)
{
	Char *pc, *last;

	if(output_format == o_bits  || !expand)
	{
	Printf("%S", text);
	return;
	}

	for(pc = last = text; *pc; pc++)
	{
	int c = *pc, type = 0;

	if(c == '&' || c == '<' || c == '>' || c == '"' ||
	   c == 0x0d ||
	   (c > 127 && !printable(c)))
		type = 1;
	else if(c == 9 || c == 10)
		type = 2;
	else if(c < 0x20 || (c >= 0x7f && c < 0xa0) ||
		c == 0x85 || c == 0x2028)
		type = 3;

	if(type == 1 ||
	   (canonical_output && output_format < o_diff && type == 2) ||
	   (is_attr && type == 2) ||
	   (xml_version > XV_1_0 && type == 3))
	{
		if(pc > last)
		Printf("%.*S", pc - last, last);
		print_special(c);
		last = pc+1;
	}
	}

	if(pc > last)
	Printf("%.*S", pc - last, last);
}

int printable(int c)
{
	int tablenum;

	switch(encoding)
	{
	case CE_unspecified_ascii_superset:
	case CE_ISO_646:
	default:
	return c <= 127;

	case CE_ISO_8859_1:
	return c <= 255;

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
	tablenum = (encoding - CE_ISO_8859_2);
	return c <= iso_max_val[tablenum] && unicode_to_iso[tablenum][c] != '?';

	case CE_UTF_8:
	case CE_UTF_16B:
	case CE_UTF_16L:
	return 1;

	case CE_ISO_10646_UCS_2B:
	case CE_ISO_10646_UCS_2L :
	return c <= 0xffff;
	}
}

void print_special(int c)
{
	switch(c)
	{
	case '<':
	Printf("&lt;");
	break;
	case '>':
	Printf("&gt;");
	break;
	case '&':
	Printf("&amp;");
	break;
	case '"':
	Printf("&quot;");
	break;
	default:
	Printf(canonical_output ? "&#%d;" : "&#x%02x;", c);
	}
}

InputSource entity_open(Entity ent, void *arg)
{
	if(ent->publicid &&
	   strcmp(ent->publicid, "-//RMT//DTD just a test//EN") == 0)
	{
	FILE *f;
	FILE16 *f16;

	if((f = fopen("/tmp/mydtd", "r")))
	{
		if(!(f16 = MakeFILE16FromFILE(f, "r")))
		return 0;
		SetCloseUnderlying(f16, 1);

		return NewInputSource(ent, f16);
	}
	}

	return EntityOpen(ent);
}

void print_ns_attrs(NamespaceBinding ns, int count)
{
	NamespaceBinding n;
	static Char empty[] = {0};

	if(!namespaces)
	return;

	for(n=ns; count>0; n=n->parent,--count)
	{
	/* Don't need to worry about duplicates, because that could only
	   happen if there were repeated attributes. */
	if(n->prefix)
		Printf(" xmlns:%S=\"%S\"", n->prefix,
		   n->namespace ? n->namespace->nsname : empty);
	else
		Printf(" xmlns=\"%S\"", n->namespace ? n->namespace->nsname : empty);
	}
}

void print_namespaces(NamespaceBinding ns)
{
	NamespaceBinding m, n;
	static Char null[] = {'[','n','u','l','l',']',0};

	if(!namespaces)
	return;

	for(n=ns; n; n=n->parent)
	{
	for(m=ns; m!=n; m=m->parent)
	{
		if(m->prefix == n->prefix)
		goto done;
		if(m->prefix && n->prefix && Strcmp(m->prefix, n->prefix) == 0)
		goto done;
	}
	if(n->prefix)
		Printf(" %S->%S", n->prefix, n->namespace ? n->namespace->nsname : null);
	else
		Printf(" [default]->%S", n->namespace ? n->namespace->nsname : null);

	done:
	;
	}
}

int notation_compare(const void *a, const void *b)
{
	return Strcmp((*(NotationDefinition *)a)->name,
		  (*(NotationDefinition *)b)->name);
}

int entity_compare(const void *a, const void *b)
{
	return Strcmp((*(Entity *)a)->name, (*(Entity *)b)->name);
}

void print_canonical_dtd(Parser p, const Char *name)
{
	int i, nnot, nent;
	NotationDefinition not, *nots;
	Entity ent, *ents;
	const char8 *uri;

	need_canonical_dtd = 0;

	for(nnot=0, not=NextNotationDefinition(p->dtd, 0); not;
	not=NextNotationDefinition(p->dtd, not))
	if(!not->tentative)
		nnot++;

	nots = malloc(nnot * sizeof(*nots));
	for(i=0, not=0; i<nnot; )
	{
	not = NextNotationDefinition(p->dtd, not);
	if(!not->tentative)
		nots[i++] = not;
	}

	for(nent=0, ent=NextEntity(p->dtd, 0); ent; ent=NextEntity(p->dtd, ent))
	if(ent->notation)
		nent++;

	ents = malloc(nent * sizeof(*ents));
	for(i=0, ent=0; i<nent; )
	{
	ent = NextEntity(p->dtd, ent);
	if(ent->notation)
		ents[i++] = ent;
	}

	if((output_format != o_can3 && nnot == 0) ||
	   (output_format == o_can3 && nnot + nent == 0))
	/* Don't produce a DOCTYPE if there's nothing to go in it.
	(The definition doesn't say this, but that's what the Oasis
	 test output has.) */
	return;

	qsort((void *)nots, nnot, sizeof(*nots), notation_compare);

	qsort((void *)ents, nent, sizeof(*ents), entity_compare);

	Printf("<!DOCTYPE %S [\n", name);

	/* NB the canonical forms definition says that double quotes should
	   be used for the system/public ids, but the Oasis test output
	   has single quotes, so we do that. */

	for(i=0; i<nnot; i++)
	{
	not = nots[i];

	if(not->systemid)
		uri = minimal_uri(NotationURL(not), EntityURL(p->document_entity));
	else
		uri = 0;

	Printf("<!NOTATION %S ", not->name);
	if(not->publicid)
	{
		Printf("PUBLIC '%s'", not->publicid);
		if(uri)
		Printf(" '%s'", uri);
	}
	else
		Printf("SYSTEM '%s'", uri);
	Printf(">\n");
	}

	if(output_format == o_can3)
	for(i=0; i<nent; i++)
	{
		ent = ents[i];

		uri = minimal_uri(EntityURL(ent), EntityURL(p->document_entity));

		Printf("<!ENTITY %S ", ent->name);
		if(ent->publicid)
		Printf("PUBLIC '%s' '%s'", ent->publicid, uri);
		else
		Printf("SYSTEM '%s'", uri);
		Printf(" NDATA %S>\n", ent->notation->name);
	}

	Printf("]>\n");
}

/*
 * Find minimal URI relative to some base URI.
 * This is a hack for the benefit of canonical forms, don't rely on it
 * for anything else.
 */
static const char8 *minimal_uri(const char8 *uri, const char8 *base)
{
	const char8 *u, *b;

	/* Find common prefix */

	for(u=uri, b=base; *u == *b; u++, b++)
	;

	/* Go back to a slash */

	while(u >= uri && *u != '/')
	u--, b--;

	if(*u != '/')
	return uri;		/* nothing in common */

	if(strchr(b+1, '/'))
	return uri;		/* too hard */

	return u+1;
}

#ifdef TIME_LIMIT
void time_exceeded(int sig)
{
	fprintf(stderr, "CPU time limit (%d seconds) exceeded, sorry\n",
		time_limit);
	exit(1);
}
#endif
