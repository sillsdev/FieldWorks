/* TODO: entity start/end markers, xml:base */

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>

#include "system.h"
#include "charset.h"
#include "ctype16.h"
#include "string16.h"
#include "dtd.h"
#include "input.h"
#include "xmlparser.h"
#include "stdio16.h"
#include "version.h"
#include "namespaces.h"
#include "infoset-print.h"
#include "hash.h"

#define INDENT_STEP 2

static void children(FILE16 *f, int level, Dtd dtd, XBit *bits, int nbits,
			 HashTable id_table);
static void notations(FILE16 *f, int level, Dtd dtd);
static void unparsed_entities(FILE16 *f, int level, Dtd dtd, Entity docent);
static void baseURI(FILE16 *f, int level, const char8 *uri);
static void standalone(FILE16 *f, int level, StandaloneDeclaration sd);
static void version(FILE16 *f, int level, const char8 *version);
static void item(FILE16 *f, int level, Dtd dtd, XBit bit,
		 HashTable id_table);
static void xdtd(FILE16 *f, int level, Dtd dtd, XBit bit);
static void element(FILE16 *f, int level, Dtd dtd, XBit bit,
			HashTable id_table);
static void pi(FILE16 *f, int level, Dtd dtd, XBit bit);
static void cdsect(FILE16 *f, int level, Dtd dtd, XBit bit);
static void pcdata(FILE16 *f, int level, Dtd dtd, XBit bit);
static void comment(FILE16 *f, int level, Dtd dtd, XBit bit);
static void name(FILE16 *f, int level,
		 const Char *nsname, const Char *local, const Char *prefix);
static void document_element(FILE16 *f, int level, XBit bit);
static void character(FILE16 *f, int level, Dtd dtd, char *ecw, int c);
static void attributes(FILE16 *f, int level, Dtd dtd, XBit bit,
			   HashTable id_table);
static void attribute(FILE16 *f, int level, Dtd dtd, Attribute a,
			  HashTable id_table);
static void namespace_attributes(FILE16 *f, int level, Dtd dtd, XBit bit,
				 HashTable id_table);
static void inscope_namespaces(FILE16 *f, int level, Dtd dtd, XBit bit);
#if 0
static void internal_entity(FILE16 *f, int level, Entity entity);
static void external_entity(FILE16 *f, int level, Entity entity);
#endif
static void unparsed_entity(FILE16 *f, int level, Entity entity);

static void simple(FILE16 *f, int level, char *name, const char *value);
static void Simple(FILE16 *f, int level, char *name, const Char *value);
static void pointer(FILE16 *f, int level, char *name, Char *id);
static Char *make_id(const char *type, const Char *name, int count);
static void indent(FILE16 *f, int level);

static void escape(FILE16 *f, const char8 *text);
static void Escape(FILE16 *f, const Char *text);
static void escape1(FILE16 *f, int c);

static Char **tokenize(Char *string, int *count);
static void find_ids(Dtd dtd, XBit *bits, int nbits, HashTable id_table,
			 int *counter);

struct xbit bogus_bit;

static Char xmlns_ns[] = {'h','t','t','p',':','/','/','w','w','w','.','w', '3',
			  '.','o','r','g','/','2','0','0','0','/','x', 'm','l',
			  'n','s','/',0};

void infoset_print(FILE16 *f, Parser p, XBit *bits, int nbits)
{
	int i;
	Dtd dtd = p->dtd;
	HashTable id_table;
	int counter = 1;

	id_table = create_hash_table(100);
	find_ids(dtd, bits, nbits, id_table, &counter);

	Fprintf(f, "<document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\n          xmlns=\"http://www.w3.org/2001/05/XMLInfoset\">\n");

	children(f, 1, dtd, bits, nbits, id_table);

	for(i=0; i<nbits; i++)
	if(bits[i]->type == XBIT_start || bits[i]->type == XBIT_empty)
		document_element(f, 1, bits[i]);

	notations(f, 1, dtd);

	unparsed_entities(f, 1, dtd, p->document_entity);

	baseURI(f, 1, EntityBaseURL(p->document_entity));

	simple(f, 1, "characterEncodingScheme",
	   CharacterEncodingName[p->document_entity->encoding]);

	standalone(f, 1, p->standalone);

	version(f, 1, p->document_entity->version_decl);

	simple(f, 1, "allDeclarationsProcessed", "true");

	Fprintf(f, "</document>\n");

	free_hash_table(id_table);
}

static void children(FILE16 *f, int level, Dtd dtd, XBit *bits, int nbits,
			 HashTable id_table)
{
	int i;

	if(nbits == 0)
	{
	indent(f, level);
	Fprintf(f, "<children/>\n");
	}
	else
	{
	indent(f, level);
	Fprintf(f, "<children>\n");
	for(i=0; i<nbits; i++)
		item(f, level+1, dtd, bits[i], id_table);
	indent(f, level);
	Fprintf(f, "</children>\n");
	}
}

static void document_element(FILE16 *f, int level, XBit bit)
{
	pointer(f, level, "documentElement", bit->S1);
}

static void notations(FILE16 *f, int level, Dtd dtd)
{
	NotationDefinition n;
	Char *id;

	indent(f, level);
	Fprintf(f, "<notations>\n");

	for(n=NextNotationDefinition(dtd, 0); n; n=NextNotationDefinition(dtd, n))
	{
	if(n->tentative)
		continue;

	id = make_id("notation", n->name, 0);

	indent(f, level+1);
	Fprintf(f, "<notation id=\"%S\">\n", id);
	free(id);

	Simple(f, level+2, "name", n->name);

	simple(f, level+2, "systemIdentifier", n->systemid);

	simple(f, level+2, "publicIdentifier", n->publicid);

	simple(f, level+2, "declarationBaseURI", EntityBaseURL(n->parent));

	indent(f, level+1);
	Fprintf(f, "</notation>\n");
	}

	indent(f, level);
	Fprintf(f, "</notations>\n");
}

static void unparsed_entities(FILE16 *f, int level, Dtd dtd, Entity docent)
{
	Entity e;

	indent(f, level);
	Fprintf(f, "<unparsedEntities>\n");

	for(e=NextEntity(dtd, 0); e; e=NextEntity(dtd, e))
	if(e->notation)
	{
		unparsed_entity(f, level+1, e);
	}

	indent(f, level);
	Fprintf(f, "</unparsedEntities>\n");
}

#if 0

static void internal_entity(FILE16 *f, int level, Entity entity)
{
	Char *id = make_id("entity", entity->name, 0);

	indent(f, level);
	Fprintf(f, "<internalEntity id=\"%S\">\n", id);
	free(id);

	Simple(f, level+1, "name", entity->name);

	Simple(f, level+1, "content", entity->text);

	indent(f, level);
	Fprintf(f, "</internalEntity>\n");
}

static void external_entity(FILE16 *f, int level, Entity entity)
{
	Char docent[] = {'d', 'o', 'c', 'e', 'n', 't', 0};
	Char *id = entity->name ? make_id("entity", entity->name, 0) : docent;

	indent(f, level);
	Fprintf(f, "<externalEntity id=\"%S\">\n", id);
	if(entity->name)
	free(id);

	Simple(f, level+1, "name", entity->name);

	simple(f, level+1, "systemIdentifier", entity->systemid);

	simple(f, level+1, "publicIdentifier", entity->publicid);

	simple(f, level+1, "baseURI", EntityBaseURL(entity));

	simple(f, level+1, "charset", CharacterEncodingName[entity->encoding]);

	indent(f, level);
	Fprintf(f, "</externalEntity>\n");
}

#endif

static void unparsed_entity(FILE16 *f, int level, Entity entity)
{
	Char *id = make_id("entity", entity->name, 0);

	indent(f, level);
	Fprintf(f, "<unparsedEntity id=\"%S\">\n", id);
	free(id);

	Simple(f, level+1, "name", entity->name);

	simple(f, level+1, "systemIdentifier", entity->systemid);

	simple(f, level+1, "publicIdentifier", entity->publicid);

	simple(f, level+1, "declarationBaseURI", EntityBaseURL(entity->parent));

	Simple(f, level+1, "notationName", entity->notation->name);

	if(entity->notation->tentative)
	simple(f, level+1, "notation", 0);
	else
	{
	id = make_id("notation", entity->notation->name, 0);
	pointer(f, level+1, "notation", id);
	free(id);
	}

	indent(f, level);
	Fprintf(f, "</unparsedEntity>\n");
}

static void baseURI(FILE16 *f, int level, const char8 *uri)
{
	simple(f, level, "baseURI", uri);
}

static void standalone(FILE16 *f, int level, StandaloneDeclaration sd)
{
	simple(f, level, "standalone",
	   sd == SDD_unspecified ? 0 : StandaloneDeclarationName[sd]);
}

static void version(FILE16 *f, int level, const char8 *version)
{
	simple(f, level, "version", version);
}

static void simple(FILE16 *f, int level, char *name, const char *value)
{
	indent(f, level);

	if(value)
	{
	Fprintf(f, "<%s>", name);
	escape(f, value);
	Fprintf(f, "</%s>\n", name);
	}
	else
	Fprintf(f, "<%s xsi:nil=\"true\"/>\n", name);
}

static void Simple(FILE16 *f, int level, char *name, const Char *value)
{
	indent(f, level);

	if(value)
	{
	Fprintf(f, "<%s>", name);
	Escape(f, value);
	Fprintf(f, "</%s>\n", name);
	}
	else
	Fprintf(f, "<%s xsi:nil=\"true\"/>\n", name);
}

static void pointer(FILE16 *f, int level, char *name, Char *id)
{
	indent(f, level);

	Fprintf(f, "<%s><pointer ref=\"%S\"/></%s>\n", name, id, name);
}

static void indent(FILE16 *f, int level)
{
	int i;

	for(i=0; i<level * INDENT_STEP; i++)
	Fprintf(f, " ");
}

static void item(FILE16 *f, int level, Dtd dtd, XBit bit,
		 HashTable id_table)

{
	switch(bit->type)
	{
	case XBIT_dtd:
		xdtd(f, level, dtd, bit);
		break;
	case XBIT_start:
		element(f, level, dtd, bit, id_table);
		break;
	case XBIT_empty:
		element(f, level, dtd, bit, id_table);
		break;
	case XBIT_pi:
		pi(f, level, dtd, bit);
		break;
	case XBIT_cdsect:
		cdsect(f, level, dtd, bit);
		break;
	case XBIT_pcdata:
		pcdata(f, level, dtd, bit);
		break;
	case XBIT_comment:
		comment(f, level, dtd, bit);
		break;
	default:
		fprintf(stderr, "***%s\n", XBitTypeName[bit->type]);
		exit(1);
		break;
	}
}

static void xdtd(FILE16 *f, int level, Dtd dtd, XBit bit)
{
	indent(f, level);
	Fprintf(f, "<documentTypeDeclaration>\n");

	simple(f, level+1, "systemIdentifier",
	   dtd->external_part ? dtd->external_part->systemid : 0);

	simple(f, level+1, "publicIdentifier",
	   dtd->external_part ? dtd->external_part->publicid : 0);

	children(f, level+1, dtd, bit->children, bit->nchildren, 0);

	indent(f, level);
	Fprintf(f, "</documentTypeDeclaration>\n");
}

static void element(FILE16 *f, int level, Dtd dtd, XBit bit,
			HashTable id_table)
{
	indent(f, level);
	if(bit->S1)
	Fprintf(f, "<element id=\"%S\">\n", bit->S1);
	else
	Fprintf(f, "<element>\n");


	name(f, level+1,
	 bit->ns_element_definition ?
	   bit->ns_element_definition->namespace->nsname : 0,
	 bit->element_definition->local,
	 bit->element_definition->prefix);

	children(f, level+1, dtd, bit->children, bit->nchildren, id_table);

	attributes(f, level+1, dtd, bit, id_table);

	namespace_attributes(f, level+1, dtd, bit, id_table);

	inscope_namespaces(f, level+1, dtd, bit);

	baseURI(f, 1, EntityBaseURL(bit->entity)); /* XXX xml:base */

	indent(f, level);
	Fprintf(f, "</element>\n");
}

static void pi(FILE16 *f, int level, Dtd dtd, XBit bit)
{
	NotationDefinition notation;
	Char *id;

	indent(f, level);
	Fprintf(f, "<processingInstruction>\n");

	Simple(f, level+1, "target", bit->pi_name);

	Simple(f, level+1, "content", bit->pi_chars);

	baseURI(f, level+1, EntityBaseURL(bit->entity)); /* XXX xml:base */

	notation = FindNotation(dtd, bit->pi_name);
	if(notation && !notation->tentative)
	{
	id = make_id("notation", notation->name, 0);
	pointer(f, level+1, "notation", id);
	free(id);
	}
	else
	simple(f, level+1, "notation", 0);


	indent(f, level);
	Fprintf(f, "</processingInstruction>\n");
}

static void cdsect(FILE16 *f, int level, Dtd dtd, XBit bit)
{
	Char *p;

#if 0
	indent(f, level);
	Fprintf(f, "<cDATAStartMarker/>\n");
#endif

	for(p=bit->pcdata_chars; *p; p++)
	character(f, level, dtd, "false", *p);

#if 0
	indent(f, level);
	Fprintf(f, "<cDATAEndMarker/>\n");
#endif
}

static void pcdata(FILE16 *f, int level, Dtd dtd, XBit bit)
{
	Char *p;
	char *ecw;

	ecw = !bit->parent->element_definition->declared ? 0 :
		bit->parent->element_definition->type == CT_element ? "true" :
															  "false";

	for(p=bit->pcdata_chars; *p; p++)
	character(f, level, dtd, is_xml_whitespace(*p) ? ecw : "false", *p);
}

static void character(FILE16 *f, int level, Dtd dtd, char *ecw, int c)
{
	indent(f, level);
	Fprintf(f, "<character>\n");

	indent(f, level+1);
	Fprintf(f, "<characterCode>%d</characterCode>\n", c);

	simple(f, level+1, "elementContentWhitespace", ecw);

	indent(f, level);
	Fprintf(f, "</character>\n");
}

static void comment(FILE16 *f, int level, Dtd dtd, XBit bit)
{
	indent(f, level);
	Fprintf(f, "<comment>\n");

	Simple(f, level+1, "content", bit->comment_chars);

	indent(f, level);
	Fprintf(f, "</comment>\n");
}

static void attributes(FILE16 *f, int level, Dtd dtd, XBit bit, HashTable id_table)
{
	Attribute a;

	indent(f, level);
	Fprintf(f, "<attributes>\n");

	for(a=bit->attributes; a; a=a->next)
	if(!a->definition->ns_attr_prefix)
		attribute(f, level+1, dtd, a, id_table);

	indent(f, level);
	Fprintf(f, "</attributes>\n");
}

static void namespace_attributes(FILE16 *f, int level, Dtd dtd, XBit bit, HashTable id_table)
{
	Attribute a;

	indent(f, level);
	Fprintf(f, "<namespaceAttributes>\n");

	for(a=bit->attributes; a; a=a->next)
	if(a->definition->ns_attr_prefix)
		attribute(f, level+1, dtd, a, id_table);

	indent(f, level);
	Fprintf(f, "</namespaceAttributes>\n");
}

static void attribute(FILE16 *f, int level, Dtd dtd, Attribute a,
			  HashTable id_table)
{
	Char *nsname;
	Char **token = 0;
	int ntokens = 0, i;
	Char **id = 0;
	NotationDefinition not;
	Entity ent;
	HashEntry entry;

	indent(f, level);
	Fprintf(f, "<attribute>\n");

	if(a->ns_definition)
	nsname = a->ns_definition->namespace->nsname;
	else if(a->definition->ns_attr_prefix)
	nsname = xmlns_ns;
	else
	nsname = 0;

	name(f, level+1,
	 nsname,
	 a->definition->local,
	 a->definition->prefix);

	Simple(f, level+1, "normalizedValue", a->value);

	simple(f, level+1, "specified", a->specified ? "true" : "false");

	simple(f, level+1, "attributeType",
	   /* accept undeclared ID type to make xml:id work */
	   (a->definition->declared || a->definition->type != AT_cdata) ?
		 AttributeTypeName[a->definition->type] : 0);

	switch(a->definition->type)
	{
	case AT_idref:
	case AT_idrefs:
	token = tokenize(a->value, &ntokens);
	if((a->definition->type == AT_idref && ntokens != 1) ||
	   (a->definition->type == AT_idrefs && ntokens == 0))
		goto bad;
	for(i=0; i<ntokens; i++)
	{
		entry = hash_find(id_table, token[i],
				  Strlen(token[i])*sizeof(Char));
		if(!entry || entry->value == &bogus_bit)
		goto bad;
	}
	id = malloc(ntokens * sizeof(*id));
	for(i=0; i<ntokens; i++)
	{
		entry = hash_find(id_table, token[i],
				  Strlen(token[i])*sizeof(Char));
		id[i] = ((XBit)entry->value)->S1;
	}
	goto good;
	break;

	case AT_entity:
	case AT_entities:
	token = tokenize(a->value, &ntokens);
	if((a->definition->type == AT_entity && ntokens != 1) ||
	   (a->definition->type == AT_entities && ntokens == 0))
		goto bad;
	for(i=0; i<ntokens; i++)
	{
		ent = FindEntity(dtd, token[i], 0);
		if(!ent || !ent->notation)
		goto bad;
	}
	id = malloc(ntokens * sizeof(*id));
	for(i=0; i<ntokens; i++)
	{
		ent = FindEntity(dtd, token[i], 0);
		id[i] = make_id("entity", ent->name, 0);
	}
	goto good;
	break;

	case AT_notation:
	token = tokenize(a->value, &ntokens);
	if(ntokens != 1)
		goto bad;
	not = FindNotation(dtd, token[0]);
	if(!not || not->tentative)
		goto bad;
	id = malloc(ntokens * sizeof(*id));
	id[0] = make_id("notation", not->name, 0);
	goto good;
	break;

	bad:
	free(token);
	free(id);
	default:
	simple(f, level+1, "references", 0);
	break;

	good:
	indent(f, level+1);
	Fprintf(f, "<references>\n");
	for(i=0; i<ntokens; i++)
	{
		indent(f, level+2);
		Fprintf(f, "<pointer ref=\"%S\"/>\n", id[i]);
		if(a->definition->type != AT_idref &&
		   a->definition->type != AT_idrefs)
		free(id[i]);
	}
	indent(f, level+1);
	Fprintf(f, "</references>\n");
	free(token);
	free(id);
	}

	indent(f, level);
	Fprintf(f, "</attribute>\n");
}

static void inscope_namespaces(FILE16 *f, int level, Dtd dtd, XBit bit)
{
	NamespaceBinding nsb, nsb2;
	static Char empty[] = {0};

	indent(f, level);
	Fprintf(f, "<inScopeNamespaces>\n");

	for(nsb = bit->ns_dict; nsb; nsb = nsb->parent)
	{
	if(!nsb->namespace)
		continue;		/* unbinding */

	for(nsb2 = bit->ns_dict; nsb2 != nsb; nsb2 = nsb2->parent)
		if((!nsb->prefix && !nsb2->prefix) ||
		   (nsb->prefix && nsb2->prefix &&
		Strcmp(nsb->prefix, nsb2->prefix) == 0))
		goto dup;

	indent(f, level+1);
	Fprintf(f, "<namespace>\n");

	Simple(f, level+2, "prefix", nsb->prefix ? nsb->prefix : empty);

	Simple(f, level+2, "namespaceName", nsb->namespace->nsname);

	indent(f, level+1);
	Fprintf(f, "</namespace>\n");
	dup:
	continue;
	}

	indent(f, level);
	Fprintf(f, "</inScopeNamespaces>\n");
}

static void name(FILE16 *f, int level,
		 const Char *nsname, const Char *local, const Char *prefix)
{
	Simple(f, level, "namespaceName", nsname);
	Simple(f, level, "localName", local);
	Simple(f, level, "prefix", prefix);
}

static Char *make_id(const char *type, const Char *name, int count)
{
	Char *id = malloc((strlen(type) + 1 + Strlen(name) + 10) * sizeof(Char));
	int i;

	if(count > 0)
	Sprintf(id, InternalCharacterEncoding, "%s-%S-%d", type, name, count);
	else
	Sprintf(id, InternalCharacterEncoding, "%s-%S", type, name);

	/* Change colon to hyphen so it is a namespace-valid ID.  Don't
	   worry about uniqueness (eg foo:bar vs foo_bar) since they will
	   have different count values anway. */

	for(i=0; id[i]; i++)
	if(id[i] == ':')
		id[i] = '-';

	return id;
}

static void escape(FILE16 *f, const char8 *text)
{
	while(*text)
	escape1(f, *text++);
}

static void Escape(FILE16 *f, const Char *text)
{
	while(*text)
	escape1(f, *text++);
}

static void escape1(FILE16 *f, int c)
{
	switch(c)
	{
	case '&':
	Fprintf(f, "&amp;");
	break;
	case '<':
	Fprintf(f, "&lt;");
	break;
	case '"':
	Fprintf(f, "&quot;");
	break;
	default:
	Fprintf(f, "%c", c);
	break;
	}
}

static Char **tokenize(Char *string, int *count)
{
	int i;
	Char *t;
	Char **tok;

	for(t=string, i=0; *t; i++)
	{
	while(*t != ' ' && *t != 0)
		t++;
	if(*t == ' ')
		t++;
	}

	*count = i;

	if(i == 0)
	return 0;

	tok = malloc(i * sizeof(*tok));

	for(t=string, i=0; *t; i++)
	{
	tok[i] = t;

	while(*t != ' ' && *t != 0)
		t++;
	if(*t == ' ')
		*t++ = 0;
	}

	return tok;
}

static void find_ids(Dtd dtd, XBit *bits, int nbits, HashTable id_table,
			 int *counter)
{
	HashEntry entry;
	int i, found;
	Attribute a;

	for(i=0; i<nbits; i++)
	{
	XBit bit = bits[i];

	if(bit->type != XBIT_start &&
	   bit->type != XBIT_empty)
		continue;

	bit->S1 = make_id("element", bit->element_definition->name,
				  (*counter)++);

	for(a=bit->attributes; a; a=a->next)
	{
		if(a->definition->type != AT_id)
		continue;
		entry = hash_find_or_add(id_table,
					 a->value,
					 Strlen(a->value)*sizeof(Char),
					 &found);
		if(found)
		/* Duplicate */
		entry->value = &bogus_bit;
		else
		entry->value = bit;
	}

	find_ids(dtd, bit->children, bit->nchildren, id_table,
		 counter);
	}
}
