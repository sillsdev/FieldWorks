/* 	$Id: xmlparser.c,v 1.133 2005/06/17 18:14:34 richard Exp $
*/

#define DEBUG_FSM 0

#ifndef lint
static char vcid[] = "$Id: xmlparser.c,v 1.133 2005/06/17 18:14:34 richard Exp $";
#endif /* lint */

/*
 * XML (and nSGML) parser.
 * Author: Richard Tobin.
 */

#include <stdarg.h>
#include <stdlib.h>

#ifdef FOR_LT

#include "lt-memory.h"
#include "nsllib.h"

#define Malloc salloc
#define Realloc srealloc
#define Free sfree

#else

#include "system.h"

#endif

#include "charset.h"
#include "string16.h"
#include "ctype16.h"
#include "dtd.h"
#include "input.h"
#include "stdio16.h"
#include "url.h"
#include "namespaces.h"
#include "xmlparser.h"

#ifdef FOR_LT

#include "lt-hash.h"

typedef HashList *HashEntry;
typedef HashList HashEntryStruct;
#define create_hash_table NewSizedHashStruct
#define free_hash_table(table) FreeHashStructM((table), 1)
#define hash_map MapHashLists1
#define hash_set_value(entry, value) ((entry)->index = (value))
#define hash_get_value(entry) ((entry)->index)
#define hash_get_key(entry) ((entry)->word)
#define hash_get_key_len(entry) ((entry)->length * sizeof(Char))
#define HashMapRetType boolean

static HashEntry hash_find_or_add(HashTable table, const Char *key,
				  int key_len, int *foundp)
{
	HashEntry entry;

	key_len /= sizeof(Char);
	entry = FindWordInTableX(table, key, key_len);
	if(!entry)
	{
	*foundp = 0;
	entry = AddWordToTableXM(table, key, key_len);
	if(!entry)
		return 0;
	}
	else
	*foundp = 1;

	return entry;
}

#else

#include "hash.h"

#define hash_set_value(entry, _value) ((entry)->value = (_value))
#define hash_get_value(entry) ((entry)->value)
#define hash_get_key(entry) ((entry)->key)
#define hash_get_key_len(entry) ((entry)->key_len)
#define HashMapRetType void

#endif

static int transcribe(Parser p, int back, int count);
static void pop_while_at_eoe(Parser p);
static void maybe_uppercase(Parser p, Char *s);
static void maybe_uppercase_name(Parser p);
static int str_maybecase_cmp8(Parser p, const char8 *a, const char8 *b);
static int is_ascii_alpha(int c);
static int is_ascii_digit(int c);
static int parse_external_id(Parser p, int required,
				 char8 **publicid, char8 **systemid,
				 int preq, int sreq);
static int parse_conditional(Parser p, Entity ent);
static int parse_notation_decl(Parser p, Entity ent);
static int parse_entity_decl(Parser p, Entity ent, int line, int chpos,
				 Entity ext_ent);
static int parsing_internal(Parser p);
static int parsing_external_subset(Parser p);
static int parse_attlist_decl(Parser p, Entity ent);
static int parse_element_decl(Parser p, Entity ent);
static ContentParticle parse_cp(Parser p);
static ContentParticle parse_choice_or_seq(Parser p, Entity ent);
static ContentParticle parse_choice_or_seq_1(Parser p, int nchildren,
						 char sep, Entity ent);
static int check_content_decl(Parser p, ContentParticle cp);
static int check_content_decl_1(Parser p, ContentParticle cp);
static Char *stringify_cp(ContentParticle cp);
static void print_cp(ContentParticle cp, FILE16 *f);
static int size_cp(ContentParticle cp);
static int check_qualname_syntax(Parser p, const Char *name, const char *type);
static int parse_reference(Parser p, int pe, int expand, int allow_external);
static int parse_character_reference(Parser p, int expand);
static const char8 *escape(int c, char8 *buf);
static int parse_name(Parser p, const char8 *where);
static int parse_nmtoken(Parser p, const char8 *where);
static int looking_at(Parser p, const char8 *string);
static void clear_xbit(XBit xbit);
static int expect(Parser p, int expected, const char8 *where);
static int expect_dtd_whitespace(Parser p, const char8 *where);
static void skip_whitespace(InputSource s);
static int skip_dtd_whitespace(Parser p, int allow_pe);
static int parse_cdata(Parser p);
static int process_nsl_decl(Parser p);
static int process_xml_decl(Parser p);
static int parse_dtd(Parser p);
static int read_markupdecls(Parser p);
static int error(Parser p, const char8 *format, ...);
static int warn(Parser p, const char8 *format, ...);
static void verror(char8 *buf, int size, XBit bit, const char8 *format, va_list args);
enum literal_type {
	LT_cdata_attr, LT_tok_attr, LT_plain, LT_entity, LT_param_entity,
	LT_pubid
};
static int parse_string(Parser p, const char8 *where, enum literal_type type, int *normalised);
static int parse_pi(Parser p, Entity ent);
static int parse_comment(Parser p, int skip, Entity ent);
static int parse_pcdata(Parser p);
static int parse_starttag(Parser p);
Namespace LookupNamespace(NamespaceBinding dictionary, const Char *prefix);
static int process_namespace(Parser p,
				 AttributeDefinition d, const Char *value);
static int parse_attribute(Parser p);
static WhiteSpaceMode process_xml_space(Parser p, const Char *value);
static int parse_endtag(Parser p);
static int parse_markup(Parser p);
static int parse(Parser p);
static int parse_markupdecl(Parser p);
static int validate_dtd(Parser p);
static int validate_final(Parser p);
static HashMapRetType check_id(const HashEntryStruct *id_entry, void *p);
static int validate_attribute(Parser p, AttributeDefinition a, ElementDefinition e, const Char *value);
static int validate_xml_lang_attribute(Parser p, ElementDefinition e, const Char *value);
static int check_attribute_syntax(Parser p, AttributeDefinition a, ElementDefinition e, const Char *value, const char *message, int real_use);
static int check_attribute_token(Parser p, AttributeDefinition a, ElementDefinition e, const Char *value, int length, const char *message, int real_use);
#if not_yet
static int magically_transform_dtd(Parser p, Char *name, int namelen);
#endif

static struct element_definition pcdata_element;
const ElementDefinition Epsilon = 0, PCDataElement = &pcdata_element;

static FSM NewFSM(void);
void FreeFSM(FSM fsm);
static FSMNode AddNode(FSM fsm);
static FSMEdge AddEdge(FSMNode source, FSMNode destination, void *label);
static void UnMarkFSM(FSM fsm, int value);
static void DeleteNode(FSMNode node);
static void DeleteEdge(FSMEdge edge);
static void CleanupFSM(FSM fsm);
static void CleanupNode(FSMNode node);

#if DEBUG_FSM
static void PrintFSM(FILE16 *out, FSM fsm, int relabelled);
#endif
static int SimplifyFSM(FSM fsm);
static int add_epsilon_closure(FSMNode base, FSMNode node);
static FSMNode translate_particle(FSM fsm, ContentParticle cp, FSMNode next);
static FSMNode translate_particle_1(FSM fsm, ContentParticle cp, FSMNode next);
static FSMNode validate_content(FSMNode context, ElementDefinition e);
static int check_deterministic(Parser p, ElementDefinition element);
static int check_deterministic_1(Parser p, ElementDefinition element,
				  FSMNode node, ElementDefinition previous);

#define validity_error (p->seen_validity_error=1, ParserGetFlag(p, ErrorOnValidityErrors) ? error : warn)

#define namespace_error error
#define namespace_validity_error validity_error

#define require(x) if(x >= 0) {} else return -1
#define require0(x) if(x >= 0) {} else return 0

#define Consume(buf) (buf = 0, buf##size = 0)
#define ExpandBuf(buf, sz) \
	if(buf##size >= (sz)+1) {} else if((buf = Realloc(buf, (buf##size = sz + 1) * sizeof(Char)))) {} else return error(p, "System error")

#define CopyName(n) if((n = Malloc((p->namelen + 1)*sizeof(Char)))) {memcpy(n, p->name, p->namelen * sizeof(Char)); n[p->namelen] = 0;} else return error(p, "System error");

#define CopyName0(n) if((n = Malloc((p->namelen + 1)*sizeof(Char)))) {memcpy(n, p->name, p->namelen * sizeof(Char)); n[p->namelen] = 0;} else {error(p, "System error"); return 0;}

#define ifNF16wrong(p,b,l) if((p)->checker && NF16wrong==nf16checkL((p)->checker, (p)->source->line + (p)->source->next - (b), (l)))
#define NF16StartCheck(p) if((p)->checker) nf16checkStart((p)->checker)
#define NF16noStartCheck(p) if((p)->checker) nf16checkNoStart((p)->checker)

#if CHAR_SIZE == 8
#define tochar8(s) s
#define duptochar8(s) strdup8(s)
#else
#define tochar8(s) (p->transbuf = translate_utf16_latin1_m(s, p->transbuf))
#define duptochar8(s) translate_utf16_latin1_m(s, 0)
#endif

const char8 *XBitTypeName[XBIT_enum_count] = {
	"dtd",
	"start",
	"empty",
	"end",
	"eof",
	"pcdata",
	"pi",
	"comment",
	"cdsect",
	"error",
	"warning",
	"none"
};

static Entity xml_builtin_entity;
static Entity xml_predefined_entities;

static int parser_initialised = 0;

static Char xml_ns[] = {'h','t','t','p',':','/','/','w','w','w','.','w','3',
			'.','o','r','g','/','X','M','L','/','1','9','9','8',
			'/','n','a','m','e','s','p','a','c','e',0};
static Char xmlns_ns[] = {'h','t','t','p',':','/','/','w','w','w','.','w', '3',
			  '.','o','r','g','/','2','0','0','0','/','x', 'm','l',
			  'n','s','/',0};

int init_parser(void)
{
	Entity e, f;
	int i;
	static const Char lt[] = {'l','t',0}, ltval[] = {'&','#','6','0',';',0};
	static const Char gt[] = {'g','t',0}, gtval[] = {'>',0};
	static const Char amp[] = {'a','m','p',0},
			  ampval[] = {'&','#','3','8',';',0};
	static const Char apos[] = {'a','p','o','s',0}, aposval[] = {'\'',0};
	static const Char quot[] = {'q','u','o','t',0}, quotval[] = {'"',0};
	static const Char *builtins[5][2] = {
	{lt, ltval}, {gt, gtval}, {amp, ampval},
	{apos, aposval}, {quot, quotval}
	};

	if(parser_initialised)
	return 0;
	parser_initialised = 1;

	if(init_charset() == -1 ||
	   init_ctype16() == -1 ||
	   init_stdio16() == -1 ||
	   init_url() == -1 ||
	   init_namespaces() == -1)
	return -1;

	xml_builtin_entity = NewInternalEntity(0, 0, 0, 0, 0, 0);

	for(i=0, f=0; i<5; i++, f=e)
	{
	e = NewInternalEntity(builtins[i][0], builtins[i][1],
				  xml_builtin_entity, 0, 0, 0);
	if(!e)
		return -1;
	e->next = f;
	}

	xml_predefined_entities = e;

	return 0;
}

void deinit_parser(void)
{
	Entity e, f;

	if(!parser_initialised)
	return;
	parser_initialised = 0;

	deinit_charset();
	deinit_ctype16();
	deinit_stdio16();
	deinit_namespaces();
	deinit_url();

	for(e = xml_predefined_entities; e; e=f)
	{
	f = e->next;
	e->text = 0;		/* it wasn't malloced so we mustn't free it */
	FreeEntity(e);
	}

	FreeEntity(xml_builtin_entity);
}

static void skip_whitespace(InputSource s)
{
	int c;

	while((c = get(s)) != XEOE && is_xml_whitespace(c))
	;
	unget(s);
}

/*
 * Skip whitespace and (optionally) the start and end of PEs.  Return 1 if
 * there actually *was* some whitespace or a PE start/end, -1 if
 * an error occurred, 0 otherwise.
 */

static int skip_dtd_whitespace(Parser p, int allow_pe)
{
	int c;
	int got_some = 0;
	InputSource s = p->source;

	while(1)
	{
	c = get(s);

	if(c == XEOE)
	{
		got_some = 1;
		if(s->parent)
		{
		if(!allow_pe)
			return error(p,
				 "PE end not allowed here in internal subset");
		if(s->entity->type == ET_external)
			p->external_pe_depth--;
		ParserPop(p);
		s = p->source;
		}
		else
		{
		unget(s);	/* leave the final EOE waiting to be read */
		return got_some;
		}
	}
	else if(is_xml_whitespace(c))
	{
		got_some = 1;
	}
	else if(c == '%')
	{
		/* this complication is needed for <!ENTITY % ...
		   otherwise we could just assume it was a PE reference. */

		c = get(s); unget(s);
		if(c != XEOE && is_xml_namestart(c, p->map))
		{
		if(!allow_pe)
		{
			unget(s);	/* For error position */
			return error(p,
				 "PE ref not allowed here in internal subset");
		}
		require(parse_reference(p, 1, 1, 1));
		s = p->source;
		if(s->entity->type == ET_external)
			p->external_pe_depth++;
		got_some = 1;
		}
		else
		{
		unget(s);
		return got_some;
		}
	}
	else
	{
		unget(s);
		return got_some;
	}
	}
}

static int expect(Parser p, int expected, const char8 *where)
{
	int c;
	InputSource s = p->source;

	c = get(s);
	if(c != expected)
	{
	unget(s);		/* For error position */
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
	else
		return error(p, "Expected %s %s, but got %s",
			 escape(expected, p->escbuf[0]), where,
			 escape(c, p->escbuf[1]));
	}

	return 0;
}

/*
 * Expects whitespace or the start or end of a PE.
 */

static int expect_dtd_whitespace(Parser p, const char8 *where)
{
	int r = skip_dtd_whitespace(p, p->external_pe_depth > 0);

	if(r < 0)
	return -1;

	if(r == 0)
	return error(p, "Expected whitespace %s", where);

	return 0;
}

static void clear_xbit(XBit xbit)
{
	xbit->type = XBIT_none;
	xbit->s1 = 0;
	xbit->S1 = xbit->S2 = 0;
	xbit->attributes = 0;
	xbit->element_definition = 0;
	xbit->ns_dict = 0;
}

void FreeXBit(XBit xbit)
{
	Attribute a, b;

	if(xbit->S1) Free(xbit->S1);
	if(xbit->S2) Free(xbit->S2);
	if(xbit->type != XBIT_error && xbit->type != XBIT_warning && xbit->s1)
	Free(xbit->s1);
	if(xbit->ns_dict && xbit->nsowned)
	{
	int i;
	NamespaceBinding parent, ns = xbit->ns_dict;
	for(i=0; i<xbit->nsc; i++)
	{
		parent = ns->parent;
		Free(ns);
		ns = parent;
	}
	}

	for(a = xbit->attributes; a; a = b)
	{
	b = a->next;
	if(a->value) Free(a->value);
	Free(a);
	}
	clear_xbit(xbit);
}

/*
 * Returns 1 if the input matches string (and consume the input).
 * Otherwise returns 0 and leaves the input stream where it was.
 * Case-sensitivity depends on the CaseInsensitive flag.
 * A space character at end of string matches any (non-zero) amount of
 * whitespace; space are treated literally elsewhere.
 * Never reads beyond an end-of-line, except to consume
 * extra whitespace when the last character of string is a space.
 * Never reads beyond end-of-entity.
 */

static int looking_at(Parser p, const char8 *string)
{
	InputSource s = p->source;
	int c, d;
	int save = s->next;

	if(p->state == PS_error)
	/* we got a bad character before, don't try again */
	return 0;

	for(c = *string++; c; c = *string++)
	{
	if(at_eol(s))
		goto fail;		/* We would go over a line end */

	d = get(s);

	if(d == BADCHAR)
	{
		error(p, "Input error: %s", s->error_msg);
		goto fail;
	}

	if(c == ' ' && *string == 0)
	{
		if(d == XEOE || !is_xml_whitespace(d))
		goto fail;
		skip_whitespace(s);
	}
	else
		if((ParserGetFlag(p, CaseInsensitive) &&
		Toupper(d) != Toupper(c)) ||
		   (!ParserGetFlag(p, CaseInsensitive) && d != c))
		goto fail;
	}

	return 1;

fail:
	s->next = save;
	return 0;
}

static int parse_name(Parser p, const char8 *where)
{
	InputSource s = p->source;
	int c, i;

	c = get(s);
	if(c == BADCHAR)
	return error(p, "Input error: %s", s->error_msg);

	if(c == XEOE || !is_xml_namestart(c, p->map))
	{
	unget(s);		/* For error position */
	error(p, "Expected name, but got %s %s",
		  escape(c, p->escbuf[0]), where);
	return -1;
	}
	i = 1;

	while(c = get(s), (c != XEOE && is_xml_namechar(c, p->map)))
	i++;
	unget(s);

	p->name = s->line + s->next - i;
	p->namelen = i;

	NF16StartCheck(p);
	if(p->namechecker && NF16wrong==nf16checkL(p->namechecker,
						s->line + s->next - i, i))
		return error(p, "Name not normalized after %s", where);

	return 0;
}

static int parse_nmtoken(Parser p, const char8 *where)
{
	InputSource s = p->source;
	int c, i=0;

	c = get(s);
	if(c == BADCHAR)
	return error(p, "Input error: %s", s->error_msg);

	while(c !=XEOE && is_xml_namechar(c, p->map))
	{
	i++;
	c = get(s);
	}
	unget(s);

	if(i == 0)
	return error(p, "Expected nmtoken, but got %s %s",
			 escape(c, p->escbuf[0]), where);

	p->name = s->line + s->next - i;
	p->namelen = i;

	NF16StartCheck(p);
	if(p->namechecker && NF16wrong==nf16checkL(p->namechecker,
						s->line + s->next - i, i))
		return error(p, "nmtoken not normalized after %s", where);

	return 0;
}

/* Escape a character for printing n an error message. */

static const char8 *escape(int c, char8 *buf)
{
#if CHAR_SIZE == 8
	if(c != XEOE)
	c &= 0xff;
#endif

	if(c == XEOE)
	return "<EOE>";
	else if(c >= 33 && c <= 126)
	sprintf(buf, "%c", c);
	else if(c == ' ')
	sprintf(buf, "<space>");
	else
	sprintf(buf, "<0x%x>", c);

	return buf;
}

Parser NewParser(void)
{
	Parser p;
	static Char xml[] = {'x','m','l',0};

	if(init_parser() == -1)
	return 0;

	p = Malloc(sizeof(*p));
	if(!p)
	return 0;
	p->state = PS_prolog1;
	p->seen_validity_error = 0;
	p->document_entity = 0;	/* Set at first ParserPush */
	p->have_dtd = 0;
	p->standalone = SDD_unspecified;
	p->flags[0] = p->flags[1] = 0;
	p->source = 0;
	clear_xbit(&p->xbit);
#ifndef FOR_LT
	p->xbit.nchildren = 0;	/* These three should never be changed */
	p->xbit.children = 0;
	p->xbit.parent = 0;
#endif
	p->pbufsize = p->pbufnext = 0;
	p->pbuf = 0;
	p->save_pbufsize = p->save_pbufnext = 0;
	p->save_pbuf = 0;
	p->transbuf = 0;

	p->peeked = 0;
	p->dtd = NewDtd();
	p->dtd_callback = p->warning_callback = 0;
	p->entity_opener = 0;
	p->dtd_callback_arg = 0;
	p->warning_callback_arg = 0;
	p->entity_opener_arg = 0;
	p->external_pe_depth = 0;

	p->checker = 0;
	p->namechecker = 0;

	VectorInit(p->element_stack);

	p->base_ns.parent = 0;
	p->base_ns.prefix = xml;
	p->base_ns.namespace =
	FindNamespace(p->dtd->namespace_universe, xml_ns, 1);
	if(!p->base_ns.namespace)
	return 0;

	p->id_table = create_hash_table(100);
	if(!p->id_table)
	return 0;

	ParserSetFlag(p, XMLSyntax, 1);
	ParserSetFlag(p, XMLPredefinedEntities, 1);
	ParserSetFlag(p, XMLExternalIDs, 1);
	ParserSetFlag(p, XMLMiscWFErrors, 1);
	ParserSetFlag(p, ErrorOnUnquotedAttributeValues, 1);
	ParserSetFlag(p, XMLLessThan, 1);
	ParserSetFlag(p, ExpandGeneralEntities, 1);
	ParserSetFlag(p, ExpandCharacterEntities, 1);
	ParserSetFlag(p, NormaliseAttributeValues, 1);
	ParserSetFlag(p, WarnOnRedefinitions, 1);
	ParserSetFlag(p, TrustSDD, 1);
	ParserSetFlag(p, ReturnComments, 1);
	ParserSetFlag(p, MaintainElementStack, 1);
	ParserSetFlag(p, XMLSpace, 0);
	ParserSetFlag(p, XMLNamespaces, 0);
	ParserSetFlag(p, XML11CheckNF, 0);
	ParserSetFlag(p, XML11CheckExists, 0);

	/* These are set here because LTXML sometimes pushes an internal
	   entity (for string reading), and the version-determining code
	   never gets run. */
	p->xml_version = XV_1_0;
	p->map = xml_char_map;

	return p;
}

void FreeParser(Parser p)
{
	while (p->source)
	ParserPop(p);		/* Will close file */

	Free(p->pbuf);
	Free(p->save_pbuf);
	Free(p->transbuf);
	Free(p->element_stack);
	free_hash_table(p->id_table);
	if(p->checker)
		nf16checkDelete(p->checker);
	if(p->namechecker)
		nf16checkDelete(p->namechecker);

	Free(p);
}

InputSource ParserRootSource(Parser p)
{
	InputSource s;

	for(s=p->source; s && s->parent; s = s->parent)
	;

	return s;
}

Entity ParserRootEntity(Parser p)
{
	return ParserRootSource(p)->entity;
}

void ParserSetDtdCallbackArg(Parser p, void *arg)
{
	p->dtd_callback_arg = arg;
}

void ParserSetWarningCallbackArg(Parser p, void *arg)
{
	p->warning_callback_arg = arg;
}

void ParserSetEntityOpenerArg(Parser p, void *arg)
{
	p->entity_opener_arg = arg;
}

void ParserSetDtdCallback(Parser p, CallbackProc cb)
{
	p->dtd_callback = cb;
}

void ParserSetWarningCallback(Parser p, CallbackProc cb)
{
	p->warning_callback = cb;
}

void ParserSetEntityOpener(Parser p, EntityOpenerProc opener)
{
	p->entity_opener = opener;
}

#ifndef FOR_LT

XBit ReadXTree(Parser p)
{
	XBit bit, tree, child;
	XBit *children;

	bit = ReadXBit(p);

	switch(bit->type)
	{
	case XBIT_error:
	return bit;

	case XBIT_start:
	if(!(tree = Malloc(sizeof(*tree))))
	{
		error(p, "System error");
		return &p->xbit;
	}
	*tree = *bit;
	while(1)
	{
		child = ReadXTree(p);
		switch(child->type)
		{
		case XBIT_error:
		FreeXTree(tree);
		return child;

		case XBIT_eof:
		FreeXTree(tree);
		{
			error(p, "EOF in element");
			return &p->xbit;
		}

		case XBIT_end:
		if(child->element_definition != tree->element_definition)
		{
			const Char *name1 = tree->element_definition->name,
				   *name2 = child->element_definition->name;
			FreeXTree(tree);
			FreeXTree(child);
			error(p, "Mismatched end tag: expected </%S>, got </%S>",
			  name1, name2);
			return &p->xbit;
		}
		/* Transfer ns records to start bit so that ns gets freed
		   when the tree is freed, rather than now. */
		tree->nsowned = child->nsowned;
		child->nsowned = 0;
		FreeXTree(child);
		return tree;

		default:
		children = Realloc(tree->children,
				   (tree->nchildren + 1) * sizeof(XBit));
		if(!children)
		{
			FreeXTree(tree);
			FreeXTree(child);
			error(p, "System error");
			return &p->xbit;
		}
		child->parent = tree;
		children[tree->nchildren] = child;
		tree->nchildren++;
		tree->children = children;
		break;
		}
	}

	default:
	if(!(tree = Malloc(sizeof(*tree))))
	{
		error(p, "System error");
		return &p->xbit;
	}
	*tree = *bit;
	return tree;
	}
}

void FreeXTree(XBit tree)
{
	int i;
	XBitType type = tree->type;

	for(i=0; i<tree->nchildren; i++)
	FreeXTree(tree->children[i]);

	Free(tree->children);

	FreeXBit(tree);

	if(type == XBIT_error)
	/* error "trees" are always in the Parser structure, not malloced */
	return;

	Free(tree);
}

#endif /* (not) FOR_LT */

XBit ReadXBit(Parser p)
{
	if(p->peeked)
	p->peeked = 0;
	else
	parse(p);

	return &p->xbit;
}

XBit PeekXBit(Parser p)
{
	if(p->peeked)
	error(p, "Attempt to peek twice");
	else
	{
	parse(p);
	p->peeked = 1;
	}

	return &p->xbit;
}

int ParserPush(Parser p, InputSource source)
{
	Entity e = source->entity;

	if(!p->source && !p->document_entity)
	p->document_entity = e;

	source->parent = p->source;
	p->source = source;

	if(e->type == ET_internal)
	return 0;

	if(e != p->document_entity)
	source->map = p->map;

	/* Look at first few bytes of external entities to guess encoding,
	   then look for an XMLDecl or TextDecl.  */

	/* Check encoding even if we have already determined it for this
	   entity, because otherwise we might leave a BOM unread. */
	determine_character_encoding(source);

#if CHAR_SIZE == 8
	if(!EncodingIsAsciiSuperset(e->encoding))
	return error(p, "Unsupported character encoding %s",
			 CharacterEncodingName[e->encoding]);
#else
	if(e->encoding == CE_unknown)
	return error(p, "Unknown character encoding");
#endif

	get(source); unget(source);	/* To get the first line read */

	if(looking_at(p, "<?NSL "))
	{
	require(process_nsl_decl(p));
	source->read_carefully = 0;
	return 0;
	}

	if(looking_at(p, "<?xml "))
	{
	require(process_xml_decl(p));
	if(e == p->document_entity && !e->version_decl)
		return error(p, "XML declaration in document entity lacked "
				"version number");
	if(e != p->document_entity && e->standalone_decl != SDD_unspecified)
		return error(p, "Standalone attribute not allowed except in "
				"document entity");
	if(e != p->document_entity && e->encoding_decl == CE_unknown)
		return error(p, "Encoding declaration is required in text "
				"declaration");
	}

	else if(looking_at(p, "<?xml?"))
	return error(p, "Empty XML or text declaration");

	else if(looking_at(p, "<?XML "))
	return error(p, "Wrong case XML declaration, must be <?xml ...");

	else if(p->state == PS_error) /* looking_at may have set it */
	return -1;

	source->read_carefully = 0;

	if(e == p->document_entity)
	{
	p->xml_version = e->xml_version;
	if(p->xml_version >= XV_1_1)
	{
		ParserSetFlag(p, XML11Syntax, 1);
#if CHAR_SIZE == 16
		p->map = xml_char_map_11;
#endif
#if CHAR_SIZE == 16
		/* XXX is this the best place to do this? */
			if(ParserGetFlag(p, XML11CheckNF))
		{
			p->checker = nf16checkNew(ParserGetFlag(p, XML11CheckExists));
				NF16StartCheck(p);
			p->namechecker =
			nf16checkNew(ParserGetFlag(p, XML11CheckExists));
		}
#endif
	}
	else
		p->map = xml_char_map;

	source->map = p->map;
	}
	else if(e->xml_version > p->xml_version)
	{
	const char8 *doc_ver = p->document_entity->version_decl ?
						   p->document_entity->version_decl : "1.0";

	if(ParserGetFlag(p, XMLStrictWFErrors))
		return error(p, "Referenced entity has later version number "
				"(%s) than document entity (%s)",
			 e->version_decl, doc_ver);
	else
		warn(p, "Referenced entity has later version number "
			"(%s) than document entity (%s)",
		 e->version_decl, doc_ver);
	}
#if 0
	Fprintf(Stderr, "\npushing %s, map = %s\n",
		EntityDescription(e), source->map == xml_char_map ? "1.0" : "1.1");
#endif
	return 0;
}

void ParserPop(Parser p)
{
	InputSource source;

	source = p->source;
	p->source = source->parent;

	SourceClose(source);
}

/* Returns true if the source is at EOE. If so, the EOE will have been read. */

static int at_eoe(InputSource s)
{
	if(!at_eol(s))
	return 0;
	if(s->seen_eoe || get_with_fill(s) == XEOE)
	return 1;
	unget(s);
	return 0;
}

/* Pops any sources that are at EOE.  Leaves source buffer with at least
   one character in it (except at EOF, where it leaves the EOE unread). */

static void pop_while_at_eoe(Parser p)
{
	while(1)
	{
	InputSource s = p->source;

	if(!at_eoe(s))
		return;
	if(!s->parent)
	{
		unget(s);
		return;
	}
	ParserPop(p);
	}
}

void ParserSetFlag(Parser p, ParserFlag flag, int value)
{
	int flagset;
	unsigned int flagbit;

	flagset = (flag >> 5);
	flagbit = (1u << (flag & 31));

	if(value)
	p->flags[flagset] |= flagbit;
	else
	p->flags[flagset] &= ~flagbit;

	if(flag == XMLPredefinedEntities)
	{
	if(value)
		p->dtd->predefined_entities = xml_predefined_entities;
	else
		p->dtd->predefined_entities = 0;
	}
}

void ParserPerror(Parser p, XBit bit)
{
	int linenum, charnum;
	InputSource s, root;

	root = ParserRootSource(p);

	if(ParserGetFlag(p, SimpleErrorFormat))
	{
	const char8 *d, *e;

	d = EntityDescription(root->entity);
	e = d+strlen8(d);
	while(e > d && e[-1] != '/')
		--e;

	if(p->state == PS_validate_dtd)
		Fprintf(Stderr, "%s:-1(end of prolog):-1: ", e);
	else if(p->state == PS_validate_final)
		Fprintf(Stderr, "%s:-1(end of body):-1: ", e);
	else
		Fprintf(Stderr, "%s:%d:%d: ", e,root->line_number+1, root->next+1);

	if(bit->type == XBIT_warning)
		Fprintf(Stderr, "warning: ");
	Fprintf(Stderr, "%s\n", bit->error_message);

	return;
	}

	Fprintf(Stderr, "%s: %s\n",
		bit->type == XBIT_error ? "Error" : "Warning",
		bit->error_message);

	if(p->state == PS_validate_dtd || p->state == PS_validate_final)
	{
	Fprintf(Stderr, " (detected at end of %s of document %s)\n",
		p->state == PS_validate_final ? "body" : "prolog",
		EntityDescription(root->entity));

	return;
	}

	for(s=p->source; s; s=s->parent)
	{
	if(s->entity->name)
		Fprintf(Stderr, " in entity \"%S\"", s->entity->name);
	else
		Fprintf(Stderr, " in unnamed entity");

	switch(SourceLineAndChar(s, &linenum, &charnum))
	{
	case 1:
		Fprintf(Stderr, " at line %d char %d of", linenum+1, charnum+1);
		break;
	case 0:
		Fprintf(Stderr, " defined at line %d char %d of",
			linenum+1, charnum+1);
		break;
	case -1:
		Fprintf(Stderr, " defined in");
		break;
	}

	Fprintf(Stderr, " %s\n", EntityDescription(s->entity));
	}
}


static int parse(Parser p)
{
	int c;
	InputSource s;

	if(p->state == PS_end || p->state == PS_error)
	{
	/* After an error or EOF, just keep returning EOF */
	p->xbit.type = XBIT_eof;
	return 0;
	}

	clear_xbit(&p->xbit);

	if(p->state <= PS_prolog2 || p->state == PS_epilog)
	skip_whitespace(p->source);

restart:
	pop_while_at_eoe(p);
	s = p->source;
	SourcePosition(s, &p->xbit.entity, &p->xbit.byte_offset);

	switch(c = get(s))
	{
	case XEOE:
	if(p->state != PS_epilog)
		return error(p, "Document ends too soon");
	p->state = PS_end;
	p->xbit.type = XBIT_eof;
		NF16StartCheck(p);
	return 0;
	case '<':
		NF16StartCheck(p); /* only effective after markup */
	return parse_markup(p);
	case '&':
	if(ParserGetFlag(p, IgnoreEntities))
		goto pcdata;
	if(p->state <= PS_prolog2)
		return error(p, "Entity reference not allowed in prolog");
	if(looking_at(p, "#"))
	{
		/* a character reference - go back and parse as pcdata */
		unget(s);
		goto pcdata;
	}
	if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	if(ParserGetFlag(p, ExpandGeneralEntities))
	{
		/* an entity reference - push it and start again */
		require(parse_reference(p, 0, 1, 1));
			NF16StartCheck(p);
		goto restart;
	}
	/* not expanding general entities, so treat as pcdata */
	goto pcdata;
	case BADCHAR:
	return error(p, "Input error: %s", s->error_msg);
	default:
	pcdata:
	unget(s);
	return parse_pcdata(p);
	}
}

/* Called after reading '<' */

static int parse_markup(Parser p)
{
	InputSource s = p->source;
	int c = get(s);

	switch(c)
	{
	case '!':
	if(looking_at(p, "--"))
	{
		if(ParserGetFlag(p, ReturnComments))
		return parse_comment(p, 0, 0);
		else
		{
		require(parse_comment(p, 1, 0));
		/* XXX avoid recursion here */
		return parse(p);
		}
	}
	else if(looking_at(p, "DOCTYPE "))
		return parse_dtd(p);
	else if(looking_at(p, "[CDATA["))
		return parse_cdata(p);
	else if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	else
		return error(p, "Syntax error after <!");

	case '/':
	return parse_endtag(p);

	case '?':
	return parse_pi(p, 0);

	case BADCHAR:
	return error(p, "Input error: %s", s->error_msg);

	default:
	unget(s);
	if(!ParserGetFlag(p, XMLLessThan) &&
	   (c == XEOE || !is_xml_namestart(c, p->map)))
	{
		/* In nSGML, recognise < as stago only if followed by namestart */

		unget(s);	/* put back the < */
		return parse_pcdata(p);
	}
	return parse_starttag(p);
	}
}

static int parse_endtag(Parser p)
{
	ElementDefinition e;
	NSElementDefinition nse;
	Entity ent;

	p->xbit.type = XBIT_end;
	require(parse_name(p, "after </"));
	maybe_uppercase_name(p);

	if(ParserGetFlag(p, MaintainElementStack))
	{
	if(VectorCount(p->element_stack) <= 0)
		return error(p, "End tag </%.*S> outside of any element",
			 p->namelen, p->name);
	}

	if(ParserGetFlag(p, Validate))
	{
	struct element_info *info = &VectorLast(p->element_stack);
	ElementDefinition parent = info->definition;

	if(parent->type == CT_element && info->context &&
	   !info->context->end_node)
	{
		require(validity_error(p, "Content model for %S does not "
					  "allow it to end here",
				  parent->name));
	}
	}

	if(ParserGetFlag(p, MaintainElementStack))
	{
	ent = VectorLast(p->element_stack).entity;
	e = VectorLast(p->element_stack).definition;
	nse = VectorLast(p->element_stack).ns_definition;
	p->xbit.ns_dict = VectorLast(p->element_stack).ns;
	p->xbit.nsc = VectorLast(p->element_stack).nsc;
	p->xbit.nsowned = (p->xbit.ns_dict != &p->base_ns);
	(void)VectorPop(p->element_stack);

	if(p->namelen != e->namelen ||
	   memcmp(p->name, e->name, p->namelen * sizeof(Char)) != 0)
		return error(p, "Mismatched end tag: expected </%S>, got </%.*S>",
			 e->name, p->namelen, p->name);

	p->xbit.element_definition = e;
	p->xbit.ns_element_definition = nse;

	if(ent != p->source->entity)
		return error(p, "Element ends in different entity from that "
				"in which it starts");

	if(VectorCount(p->element_stack) == 0)
	{
		if(ParserGetFlag(p, Validate))
		{
		p->state = PS_validate_final;
		require(validate_final(p));
		}
		p->state = PS_epilog;
	}
	}
	else
	{
	e = FindElementN(p->dtd, p->name, p->namelen);
	p->xbit.element_definition = e;
	if(!p->xbit.element_definition)
		return error(p, "End tag for unknown element %.*S",
			 p->namelen, p->name);
	}

	skip_whitespace(p->source);
	NF16StartCheck(p);
	return expect(p, '>', "after name in end tag");
}

static int check_qualname_syntax(Parser p, const Char *name, const char *type)
{
	Char *t;

	t = Strchr(name, ':');

	if(!t)
	return 0;

	if(t == name)
	{
	require(namespace_error(p, "%s name %S has empty prefix", type, name));
	}
	else if(t[1] == 0)
	{
	require(namespace_error(p, "%s name %S has empty local part",
				type, name));
	}
	else if(!is_xml_namestart(t[1], p->map))
	{
	require(namespace_error(p, "%s name %S has illegal local part",
				type, name));
	}
	else if(Strchr(t+1, ':'))
	{
	require(namespace_error(p, "%s name %S has multiple colons",
				type, name));
	}

	return 0;
}

static int parse_starttag(Parser p)
{
	int c, is_top_level = 0;
	ElementDefinition e;
	AttributeDefinition d;
	Attribute a, aa, all_attrs;
	struct element_info *this_info = 0, *parent_info = 0;

	if(p->state == PS_epilog && !ParserGetFlag(p, AllowMultipleElements))
	return error(p, "Document contains multiple elements");

	if(p->state < PS_body)
	{
	if(ParserGetFlag(p, Validate))
	{
		p->state = PS_validate_dtd;
		require(validate_dtd(p));
	}
	is_top_level = 1;
	}

	p->state = PS_body;

	require(parse_name(p, "after <"));
	maybe_uppercase_name(p);

#if not_yet
	if(is_top_level && p->magic_prefix)
	require(magically_transform_dtd(p, p->name, p->namelen));
#endif

	e = FindElementN(p->dtd, p->name, p->namelen);
	if(!e || e->tentative)
	{
	if(p->have_dtd && ParserGetFlag(p, ErrorOnUndefinedElements))
		return error(p, "Start tag for undeclared element %.*S",
			 p->namelen, p->name);
	if(ParserGetFlag(p, Validate) &&
	   !(ParserGetFlag(p, RelaxedAny) &&
		 VectorCount(p->element_stack) != 0 &&
		 VectorLast(p->element_stack).definition->type == CT_any))
	{
		require(validity_error(p,
				   "Start tag for undeclared element %.*S",
				   p->namelen, p->name));
	}
	if(e)
		RedefineElement(e, CT_any, 0, 0, 0);
	else
	{
		if(!(e =
		 DefineElementN(p->dtd, p->name, p->namelen, CT_any, 0, 0, 0)))
		return error(p, "System error");
		if(ParserGetFlag(p, XMLNamespaces))
		{
		require(check_qualname_syntax(p, e->name, "Element"));
		}
	}
	}

	p->xbit.element_definition = e;

	if(ParserGetFlag(p, Validate))
	{
	if(VectorCount(p->element_stack) == 0)
	{
		if(Strcmp(p->dtd->name, e->name) != 0)
		{
		require(validity_error(p, "Root element is %S, should be %S",
					   e->name, p->dtd->name));
		}
	}
	else
	{
		struct element_info *info = &VectorLast(p->element_stack);
		ElementDefinition parent = info->definition;

		if(parent->type == CT_empty)
		{
		require(validity_error(p, "Content model for %S does not "
						  "allow anything here",
					   parent->name));
		}
		else if(info->context)
		{
		info->context = validate_content(info->context, e);
		if(!info->context)
		{
			require(validity_error(p, "Content model for %S does not "
						  "allow element %S here",
					   parent->name, e->name));
		}
		}
	}
	}

	while(1)
	{
	InputSource s = p->source;

	/* We could just do skip_whitespace here, but we will get a
	   better error message if we look a bit closer. */

	c = get(s);

	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);

	if(c !=XEOE && is_xml_whitespace(c))
	{
		skip_whitespace(s);
		c = get(s);
	}
	else if(c != '>' &&
		!(ParserGetFlag(p, XMLSyntax) && c == '/'))
	{
		unget(s);		/* For error position */
		return error(p, "Expected whitespace or tag end in start tag");
	}

	if(c == '>')
	{
		p->xbit.type = XBIT_start;
		break;
	}

	if((ParserGetFlag(p, XMLSyntax)) && c == '/')
	{
		require(expect(p, '>', "after / in start tag"));
		p->xbit.type = XBIT_empty;
		break;
	}

	unget(s);

	require(parse_attribute(p));
	}

	if(ParserGetFlag(p, MaintainElementStack))
	{
	if(p->xbit.type == XBIT_start)
	{
		if(!VectorPushNothing(p->element_stack))
		return error(p, "System error");
		if(VectorCount(p->element_stack) > 1)
		parent_info = &VectorLast(p->element_stack) - 1;
		this_info = &VectorLast(p->element_stack);
		this_info->definition = e;
		this_info->context = e->fsm ? e->fsm->start_node : 0;
		this_info->wsm = WSM_unspecified;
		this_info->ns = 0;
		this_info->entity = p->source->entity;
		/* Set these here even if not doing namespace processing, to
		   avoid rui errors from dbx. */
		this_info->ns_definition = 0;
		this_info->nsc = 0;
	}
	else
	{
		/* Is this element allowed to be empty? */

		if(ParserGetFlag(p, Validate) && e->fsm &&
		   !e->fsm->start_node->end_node)
		{
		require(validity_error(p, "Content model for %S does not "
					   "allow it to be empty",
					   e->name));
		}

		/* Is it the (empty) top-level element? */

		if(VectorCount(p->element_stack) == 0)
		{
		if(ParserGetFlag(p, Validate))
		{
			p->state = PS_validate_final;
			require(validate_final(p));
		}
		p->state = PS_epilog;
		}
		else
		parent_info = &VectorLast(p->element_stack);
	}
	}

	if(ParserGetFlag(p, Validate))
	{
	/* check for required attributes */

	AttributeDefinition d;
	Attribute a;

	for(d=NextAttributeDefinition(e, 0);
		d;
		d=NextAttributeDefinition(e, d))
	{
		if(d->default_type != DT_required)
		continue;
		for(a=p->xbit.attributes; a; a=a->next)
		if(a->definition == d)
			break;
		if(!a)
		{
		require(validity_error(p,
					   "Required attribute %S for element %S "
					   "is not present",
					   d->name, e->name));
		}
	}
	}

	/* Find defaulted attributes if we need them */

	/* p->xbit.attributes only points to actually present attributes
	   until the end of this function. */
	all_attrs = p->xbit.attributes;

	if(ParserGetFlag(p, ReturnDefaultedAttributes) ||
	   ParserGetFlag(p, XMLNamespaces))
	{

	for(d=NextAttributeDefinition(e, 0);
		d;
		d=NextAttributeDefinition(e, d))
	{
		if(!d->default_value)
		continue;
		for(a=p->xbit.attributes; a; a=a->next)
		if(a->definition == d)
			break;
		if(!a)
		{
		if(!(a = Malloc(sizeof(*a))))
			return error(p, "System error");
		a->definition = d;
		if(!(a->value = Strdup(d->default_value)))
			return error(p, "System error");
		a->specified = 0;
		a->quoted = 1;
		a->next = all_attrs;
		all_attrs = a;
		}
	}
	}

	/* Do some checks on defaulted attributes if validating */

	if(ParserGetFlag(p, Validate))
	{
	for(d=NextAttributeDefinition(e, 0);
		d;
		d=NextAttributeDefinition(e, d))
	{
		int ed, sem;

		if(!d->default_value)
		continue;

		/* Check no externally-declared defaults in standalone document,
		   and do "non-lexical" validation of some attribute types */

		ed = (p->standalone == SDD_yes && d->is_externally_declared);
		sem =
		(d->type == AT_entity || d->type == AT_entities ||
		 d->type == AT_id ||
		 d->type == AT_idref || d->type == AT_idrefs);

		if(ed || sem)
		{
		/* was it actually defaulted? */

		for(a=p->xbit.attributes; a; a=a->next)
			if(a->definition == d)
			break;
		if(a)
			/* no */
			continue;
		}

		if(sem)
		{
		require(check_attribute_syntax(p, d, e, d->default_value,
						   "defaulted value for attribute",
						   1));
		}

		if(ed)
		{
		require(validity_error(p, "Externally declared attribute %S "
			"for element %S defaulted in document declared standalone",
					   d->name, e->name));
		}
	}
	}

	/* Look for xml:space attribute */

	if(ParserGetFlag(p, XMLSpace))
	{
	d = e->xml_space_attribute;

	if(d)
	{
		for(a=p->xbit.attributes; a; a=a->next)
		if(a->definition == d)
		{
			p->xbit.wsm = process_xml_space(p, a->value);
			goto done;
		}

		if(d->default_type == DT_none || d->default_type == DT_fixed)
		{
		p->xbit.wsm = process_xml_space(p, d->default_value);
		goto done;
		}
	}

	p->xbit.wsm = parent_info ? parent_info->wsm : WSM_unspecified;

	done:
	if(this_info)
		this_info->wsm = p->xbit.wsm;
	}
	else
	p->xbit.wsm = WSM_unspecified;

	/* Look for xml:id attribute */

	if(ParserGetFlag(p, XMLID) && e->xml_id_attribute)
	{
	Char *s;

	d = e->xml_id_attribute;

	for(a=p->xbit.attributes; a; a=a->next)
		if(a->definition == d)
		break;
	if(!a)
		goto id_done;

	/* check that it's an NCName */

	if(!is_xml_namestart(a->value[0], p->map))
	{
		warn(p, "xml:id error: value \"%S\" does not start with a name start character",
		a->value);
		goto id_done;
	}

	for(s=a->value; *s; s++)
	{
		if(*s == ':')
		{
		warn(p, "xml:id error: value \"%S\" contains a colon", a->value);
		goto id_done;
		}
		else if(!is_xml_namechar(*s, p->map))
		{
		warn(p, "xml:id error: value \"%S\" contains a character which is not a name character",
			a->value);
		goto id_done;
		}
	}

	id_done:
	;
	}

	if(ParserGetFlag(p, XMLIDCheckUnique))
	{
	int found;
	HashEntry id_entry;

	for(a=p->xbit.attributes; a; a=a->next)
	{
		d = a->definition;
		if(d->type != AT_id)
		continue;
		if(ParserGetFlag(p, Validate) && d->declared)
		/* declared attributes will have been checked during validation */
		continue;

		id_entry = hash_find_or_add(p->id_table,
					a->value,
					Strlen(a->value)*sizeof(Char),
					&found);
		if(!id_entry)
		return error(p, "System error");

		if(!found)
		hash_set_value(id_entry, (void *)2);
		else
		warn(p, "xml:id error: duplicate ID attribute value %S",
			 a->value);
	}
	}

	if(ParserGetFlag(p, XMLNamespaces))
	{
	Attribute *attp;
	Namespace ns;
	NSElementDefinition nselt;
	NSAttributeDefinition nsattr;

	p->xbit.ns_dict = parent_info ? parent_info->ns : &p->base_ns;
	p->xbit.nsc = 0;

	/* Look for xmlns attributes */

	for(attp=&all_attrs; *attp; )
	{
		a = *attp;
		if(a->definition->ns_attr_prefix)
		{
		require(process_namespace(p, a->definition, a->value));
		p->xbit.nsc++;

		/* remove the attribute now we've processed it */

		if(!ParserGetFlag(p, ReturnNamespaceAttributes))
		{
			if(p->xbit.attributes == a)
			p->xbit.attributes = a->next;
			*attp = a->next;
			Free(a->value);
			Free(a);
		}
		else
			attp = &a->next;
		}
		else
		attp = &a->next;
	}

	p->xbit.nsowned = (p->xbit.type == XBIT_empty &&
			   p->xbit.ns_dict != &p->base_ns);

	/* Find namespace for element */

	if(e->prefix)
	{
		ns = LookupNamespace(p->xbit.ns_dict, e->prefix);
		if(!ns)
		{
		require(namespace_error(p,
					"Element name %S has unbound prefix",
					e->name));
		}
	}
	else
		ns = LookupNamespace(p->xbit.ns_dict, 0);

	nselt = 0;
	if(ns)
		if(!(nselt = NamespacifyElementDefinition(e, ns)))
		return error(p, "System error");

	p->xbit.ns_element_definition = nselt;

	if(this_info)
	{
		this_info->ns = p->xbit.ns_dict;
		this_info->nsc = p->xbit.nsc;
		this_info->ns_definition = nselt;
	}

	/* Find namespaces for attributes */

	for(a=all_attrs; a; a=a->next)
	{
		d = a->definition;
		nsattr = 0;

		if(!d->ns_attr_prefix) /* Ignore namespace attributes themselves */
		{
		if(d->prefix)
		{
			ns = LookupNamespace(p->xbit.ns_dict, d->prefix);
			if(!ns)
			{
			require(namespace_error(p,
					 "Attribute name %S has unbound prefix",
						d->name));
			}
			else
			if(!(nsattr =
				 NamespacifyGlobalAttributeDefinition(d, ns)))
				return error(p, "System error");
		}
		else if(nselt)
		{
			if(!(nsattr =
			 NamespacifyElementAttributeDefinition(d, nselt)))
			return error(p, "System error");
		}
		}

		a->ns_definition = nsattr;
	}

	/* Check for repeated qualified attributes */

	for(a=all_attrs; a; a=a->next)
	{
		d = a->definition;
		if(a->ns_definition && !a->ns_definition->element)
		for(aa=all_attrs; aa != a; aa=aa->next)
		{
			if(aa->ns_definition == a->ns_definition)
			{
			require(namespace_error(p,
					"Repeated attribute %S in namespace %S",
				 d->local, a->ns_definition->namespace->nsname));
			}
		}
	}

	/* Free defaulted attrs if we only got them for namespace stuff */

	if(!ParserGetFlag(p, ReturnDefaultedAttributes))
	{
		for(a=all_attrs; a != p->xbit.attributes; a = aa)
		{
		aa = a->next;
		Free(a->value);
		Free(a);
		}
		all_attrs = p->xbit.attributes;
	}
	}

	p->xbit.attributes = all_attrs;

	NF16StartCheck(p);
	return 0;
}

static int process_namespace(Parser p, AttributeDefinition d,const Char *value)
{
	NamespaceBinding nb;
	const Char *prefix;
	const Char *nsname;
	Namespace ns;

	static Char xmlns[] = {'x','m','l','n','s',0};
	static Char xml[] = {'x','m','l',0};

	int xml_prefix = 0, xmlns_prefix = 0;
	int xml_uri = 0, xmlns_uri = 0;

	prefix = *d->ns_attr_prefix ? d->ns_attr_prefix : 0;
	nsname = *value == 0 ? 0 : value;

	if(prefix && !nsname && p->xml_version < XV_1_1)
	{
	require(namespace_error(p,
				"Namespace declaration for %S has empty URI",
				prefix));
	}
	if(prefix)
	{
	if(Strcmp(prefix, xml) == 0)
		xml_prefix = 1;
	else if(Strcmp(prefix, xmlns) == 0)
		xmlns_prefix = 1;
	}

	if(nsname)
	{
	if(Strcmp(nsname, xml_ns) == 0)
		xml_uri = 1;
	else if(Strcmp(nsname, xmlns_ns) == 0)
		xmlns_uri = 1;
	}

	if(xml_prefix && !xml_uri)
	{
	require(namespace_error(p,
				"Declaration of xml prefix has wrong URI \"%S\"",
				nsname));
	}

	if(xmlns_prefix)
	{
	require(namespace_error(p,
				"Declaration of xmlns prefix is not allowed"));
	}

	if(xml_uri && !xml_prefix)
	{
	require(namespace_error(p, "Declaration of xml namespace with "
			" prefix \"%S\" (must be \"xml\")", prefix));
	}

	if(xmlns_uri)
	{
	require(namespace_error(p,
				 "Declaration of xmlns namespace is not allowed"));
	}

	if(nsname)
	{
	if(!(ns = FindNamespace(p->dtd->namespace_universe, nsname, 1)))
		return error(p, "System error");
	}
	else
	ns = 0;

	if(!(nb = Malloc(sizeof(*nb))))
	return error(p, "System error");

	nb->prefix = prefix;
	nb->namespace = ns;
	nb->parent = p->xbit.ns_dict;
	p->xbit.ns_dict = nb;

	return 0;
}

Namespace LookupNamespace(NamespaceBinding dictionary, const Char *prefix)
{
	NamespaceBinding n;

	for(n=dictionary; n; n=n->parent)
	{
	if(prefix == 0)
	{
		if(n->prefix == 0)
		return n->namespace;
	}
	else if(n->prefix && Strcmp(prefix, n->prefix) == 0)
		return n->namespace;
	}

	return 0;
}

static int parse_attribute(Parser p)
{
	InputSource s = p->source;
	ElementDefinition elt = p->xbit.element_definition;
	AttributeDefinition def;
	struct attribute *a;
	int c;
	int normalised = 0;
	static Char xmlns[] = {'x','m','l','n','s',0};

	require(parse_name(p, "for attribute"));
	maybe_uppercase_name(p);

	def = FindAttributeN(elt, p->name, p->namelen);
	if(!def)
	{
	if(p->have_dtd && ParserGetFlag(p, ErrorOnUndefinedAttributes))
		return error(p, "Undeclared attribute %.*S for element %S",
			 p->namelen, p->name, elt->name);
	if(ParserGetFlag(p, Validate) &&
	   (elt->declared || elt->has_attlist) &&
	   !(ParserGetFlag(p, AllowUndeclaredNSAttributes) &&
		 p->namelen >= 5 && Strncmp(p->name, xmlns, 5) == 0 &&
		 (p->namelen == 5 || p->name[5] == ':')))
	{
		require(validity_error(p,
				   "Undeclared attribute %.*S for element %S",
				   p->namelen, p->name, elt->name));
	}
	if(!(def = DefineAttributeN(elt, p->name, p->namelen,
					AT_cdata, 0, DT_implied, 0, 0)))
		return error(p, "System error");
	if(ParserGetFlag(p, XMLID) && elt->xml_id_attribute == def)
		def->type = AT_id;
	if(ParserGetFlag(p, XMLNamespaces))
	{
		require(check_qualname_syntax(p, def->name, "Attribute"));
	}
	}

	for(a = p->xbit.attributes; a; a = a->next)
	if(a->definition == def)
		return error(p, "Repeated attribute %.*S", p->namelen, p->name);

	if(!(a = Malloc(sizeof(*a))))
	return error(p, "System error");

	a->value = 0;		/* in case of error */
	a->next = p->xbit.attributes;
	p->xbit.attributes = a;
	a->definition = def;
	a->specified = 1;

	skip_whitespace(s);
	require(expect(p, '=', "after attribute name"));

	skip_whitespace(s);
	c = get(s);
	unget(s);
	switch(c)
	{
	case BADCHAR:
	case '"':
	case '\'':
	a->quoted = 1;
	require(parse_string(p, "in attribute value",
				 a->definition->type == AT_cdata ? LT_cdata_attr :
												   LT_tok_attr,
				 &normalised));
	a->value = p->pbuf;
	Consume(p->pbuf);
	break;
	default:
	if(ParserGetFlag(p, ErrorOnUnquotedAttributeValues))
		return error(p, "Value of attribute is unquoted");
	a->quoted = 0;
	require(parse_nmtoken(p, "in unquoted attribute value"));
	CopyName(a->value);
	break;
	}

	if(ParserGetFlag(p, Validate))
	{
	if(p->standalone == SDD_yes && normalised &&
	   a->definition->is_externally_declared)
	{
		require(validity_error(p, "Externally declared attribute %S for "
		   "element %S was normalised in document declared standalone",
				   a->definition->name, elt->name));
	}

	require(validate_attribute(p, a->definition, elt, a->value));
	}

	return 0;
}

static WhiteSpaceMode process_xml_space(Parser p, const Char *value)
{
	static Char _preserve[9] = {'p','r','e','s','e','r','v','e',0};
	static Char _default[8] = {'d','e','f','a','u','l','t',0};
	Char buf[9];
	const Char *v;
	int i;

	/* It's possible that it hasn't been normalised (sigh) */

	for(v=value; is_xml_whitespace(*v); v++)
	;
	for(i=0; i<8; i++)
	{
	if(!v[i] || is_xml_whitespace(v[i]))
		break;
	buf[i] = v[i];
	}
	buf[i] = '\0';
	for(; v[i]; i++)
	if(!is_xml_whitespace(v[i]))
		/* If you want validation, you know where to find it */
		return WSM_unspecified;

	if(Strcmp(v, _preserve) == 0)
	return WSM_preserve;
	if(Strcmp(v, _default) == 0)
	return WSM_default;
	return WSM_unspecified;
}

static int transcribe(Parser p, int back, int count)
{
	ExpandBuf(p->pbuf, p->pbufnext + count);
	memcpy(p->pbuf + p->pbufnext,
	   p->source->line + p->source->next - back,
	   count * sizeof(Char));
	p->pbufnext += count;
	return 0;
}

/* Called after pushing back the first character of the pcdata */

static int parse_pcdata(Parser p)
{
	int count = 0;
	int had_charref = 0;
	InputSource s;
	Char *buf;
	int next, buflen;

	if(p->state <= PS_prolog2)
	return error(p, "Character data not allowed in prolog");
	if(p->state == PS_epilog)
	return error(p, "Character data not allowed after body");

	s = p->source;
	buf = s->line;
	next = s->next;
	buflen = s->line_length;

	p->pbufnext = 0;

	while(1)
	{
	if(next == buflen)
	{
		s->next = next;
		if(count > 0)
		{
			ifNF16wrong(p,count,count)
			return error(p, "pcdata not normalized");
		require(transcribe(p, count, count));
		}
		count = 0;
		if(at_eoe(s))
		{
			NF16StartCheck(p);
		if(!ParserGetFlag(p, MergePCData))
			goto done;
		else
			pop_while_at_eoe(p);
			}
		s = p->source;
		buf = s->line;
		next = s->next;
		buflen = s->line_length;
		if(next == buflen)
		goto done;	/* must be EOF */
	}

	switch(buf[next++])
	{
	case BADCHAR:
		return error(p, "Input error: %s", s->error_msg);
	case '<':
		if(!ParserGetFlag(p, XMLLessThan))
		{
		/* In nSGML, don't recognise < as markup unless it looks ok */
		if(next == buflen)
			goto deflt;
		if(buf[next] != '!' && buf[next] != '/' && buf[next] != '?' &&
		   !is_xml_namestart(buf[next], p->map))
			goto deflt;
		}
		s->next = next;
		if(count > 0)
		{
			ifNF16wrong(p,count+1,count)
			return error(p, "pcdata not normalized");
		require(transcribe(p, count+1, count));
		}
		count = 0;
		if(!ParserGetFlag(p, ReturnComments) &&
		   buflen >= next + 3 &&
		   buf[next] == '!' && buf[next+1] == '-' && buf[next+2] == '-')
		{
		s->next = next + 3;
		require(parse_comment(p, 1, 0));
				NF16StartCheck(p);
		buflen = s->line_length;
		next = s->next;
		buf = s->line; 	/* thanks to robin@reportlab.com for this */
		}
		else
		{
		s->next = next-1;
		goto done;
		}
		break;
	case '&':
		if(ParserGetFlag(p, IgnoreEntities))
		goto deflt;
		if(!ParserGetFlag(p, MergePCData) &&
		   (p->pbufnext > 0  || count > 0))
		{
		/* We're returning references as separate bits, and we've
		 come to one, and we've already got some data to return,
		 so return what we've got and get the reference next time. */

		s->next = next-1;
		if(count > 0)
		{
			ifNF16wrong(p,count,count)
				return error(p, "pcdata not normalized");
			require(transcribe(p, count, count));
		}
		goto done;
		}
		if(buflen >= next+1 && buf[next] == '#')
		{
		/* It's a character reference */

		had_charref = 1;
		s->next = next+1;
		if(count > 0)
		{
			ifNF16wrong(p,count,count+2)
				return error(p,"pcdata not normalized");
			require(transcribe(p, count+2, count));
		}
		count = 0;
		require(parse_character_reference(p,
				   ParserGetFlag(p, ExpandCharacterEntities)));
		NF16StartCheck(p);
		next = s->next;

		if(!ParserGetFlag(p, MergePCData))
			goto done;
		}
		else
		{
		/* It's a general entity reference */

		s->next = next;
		if(count > 0)
		{
			ifNF16wrong(p,count,count+1)
				return error(p, "pcdata not normalized");
			require(transcribe(p, count+1, count));
		}
		count = 0;
		require(parse_reference(p, 0,
					   ParserGetFlag(p, ExpandGeneralEntities),
					1));
				NF16StartCheck(p);
		s = p->source;
		buf = s->line;
		buflen = s->line_length;
		next = s->next;

		if(!ParserGetFlag(p, MergePCData))
			goto done;
		}
		break;
	case ']':
		if(ParserGetFlag(p, XMLMiscWFErrors) &&
		   buflen >= next + 2 &&
		   buf[next] == ']' && buf[next+1] == '>')
		return error(p, "Illegal character sequence ']]>' in pcdata");
		/* fall through */
	default:
	deflt:
		count++;
		break;
	}
	}

  done:
	ExpandBuf(p->pbuf, 0);	/* In case we got nothing */
	p->pbuf[p->pbufnext++] = 0;
	p->xbit.type = XBIT_pcdata;
	p->xbit.pcdata_chars = p->pbuf;
	Consume(p->pbuf);
	p->xbit.pcdata_ignorable_whitespace = 0;

	if(ParserGetFlag(p, Validate))
	{
	ElementDefinition e = VectorLast(p->element_stack).definition;
	if(e->type == CT_empty)
	{
		require(validity_error(p, "PCDATA not allowed in EMPTY element %S",
				   e->name));
	}
	else if(e->type == CT_element)
	{
		Char *t;

		for(t = p->xbit.pcdata_chars; *t; t++)
		if(!is_xml_whitespace(*t))
			break;
		if(*t)
		{
		require(validity_error(p,
				 "Content model for %S does not allow PCDATA",
					   e->name));
		}
		else if(had_charref)
		{
		/* E15 to 2nd edition */
		require(validity_error(p,
			 "Content model for %S does not allow character reference",
					   e->name));
		}
		else
		{
		p->xbit.pcdata_ignorable_whitespace = 1;
		if(p->standalone == SDD_yes && e->is_externally_declared)
		{
			require(validity_error(p, "Ignorable whitespace in "
		"externally declared element %S in document declared standalone",
					   e->name));
		}
		}
	}
	}

	return 0;
}

/* Called after reading '<!--'.  Won't go over an entity end. */

static int parse_comment(Parser p, int skip, Entity ent)
{
	InputSource s = p->source;
	int c, c1=0, c2=0;
	int count = 0;
	NF16noStartCheck(p);

	if(ParserGetFlag(p, Validate) && VectorCount(p->element_stack) > 0)
	{
	ElementDefinition parent = VectorLast(p->element_stack).definition;

	if(parent->type == CT_empty)
	{
	   require(validity_error(p, "Comment not allowed in EMPTY element %S",
				  parent->name));
	}
	}

	if(!skip)
	p->pbufnext = 0;

	while((c = get(s)) != XEOE)
	{
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);

	count++;
	if(c1 == '-' && c2 == '-')
	{
		if(c == '>')
		break;
		unget(s);		/* For error position */
		return error(p, "-- in comment");
	}

	if(at_eol(s))
	{
		ifNF16wrong(p,count,count)
				return error(p, "comment not normalized");
		if(!skip)
		{
		require(transcribe(p, count, count));
		}
		count = 0;
	}
	c2 = c1; c1 = c;
	}

	/* XXX comment going over PE end should be only a validity error,
	   but we treat it as a WF error */

	if(c == XEOE)
	return error(p, "EOE in comment");

	ifNF16wrong(p,count,count-3)
		return error(p, "comment not normalized");
	NF16StartCheck(p);
	if(skip)
	return 0;

	require(transcribe(p, count, count-3));
	p->pbuf[p->pbufnext++] = 0;
	p->xbit.type = XBIT_comment;
	p->xbit.comment_chars = p->pbuf;
	Consume(p->pbuf);

	return 0;
}

static int parse_pi(Parser p, Entity ent)
{
	InputSource s = p->source;
	int c, c1=0;
	int count = 0;
	Char xml[] = {'x', 'm', 'l', 0};

	if(ParserGetFlag(p, Validate) && VectorCount(p->element_stack) > 0)
	{
	ElementDefinition parent = VectorLast(p->element_stack).definition;

	if(parent->type == CT_empty)
	{
		require(validity_error(p, "PI not allowed in EMPTY element %S",
				   parent->name));
	}
	}

	require(parse_name(p, "after <?"));
	CopyName(p->xbit.pi_name);

	p->pbufnext = 0;
	NF16noStartCheck(p);

	if(Strcasecmp(p->xbit.pi_name, xml) == 0)
	{
	if(ParserGetFlag(p, XMLStrictWFErrors))
		return error(p, "Misplaced xml declaration");
	else if(!ParserGetFlag(p, IgnorePlacementErrors))
		warn(p, "Misplaced xml declaration; treating as PI");
	}

	if(ParserGetFlag(p, XMLNamespaces) && Strchr(p->xbit.pi_name, ':'))
	{
	require(namespace_error(p, "PI name %S contains colon",
				p->xbit.pi_name));
	}

	/* Empty PI? */

	if(looking_at(p, ParserGetFlag(p, XMLSyntax) ? "?>" : ">"))
	{
	ExpandBuf(p->pbuf, 0);
	goto done;
	}
	if(p->state == PS_error)	/* looking_at may have set it */
	return -1;

	/* If non-empty, must be white space after name */

	c = get(s);
	if(c == BADCHAR)
	return error(p, "Input error: %s", s->error_msg);
	if(c == XEOE || !is_xml_whitespace(c))
	return error(p, "Expected whitespace after PI name");
	skip_whitespace(s);

	while((c = get(s)) != XEOE)
	{
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
	count++;
	if(c == '>' &&
	   (!ParserGetFlag(p, XMLSyntax) || c1 == '?'))
		break;
	if(at_eol(s))
	{
		ifNF16wrong(p,count,count)
				return error(p, "PI not normalized");
		require(transcribe(p, count, count));
		count = 0;
	}
	c1 = c;
	}

	/* XXX pi going over PE end should (perhaps?) only be a validity error,
	   but we treat it as a WF error */

	if(c == XEOE)
	return error(p, "EOE in PI");

	ifNF16wrong(p,count,count-(ParserGetFlag(p, XMLSyntax) ? 2 : 1))
		return error(p, "PI not normalized");
	require(transcribe(p, count, count-(ParserGetFlag(p, XMLSyntax) ? 2 : 1)));
done:
	p->pbuf[p->pbufnext++] = 0;
	p->xbit.type = XBIT_pi;
	p->xbit.pi_chars = p->pbuf;
	Consume(p->pbuf);

	NF16StartCheck(p);
	return 0;
}

static int parse_string(Parser p, const char8 *where, enum literal_type type, int *normalised)
{
	int c, quote;
	int count = 0;
	InputSource start_source, s;
	int changed = 0;

	/* entities cannot start with combiner, other things can */
	if (type==LT_param_entity||type==LT_entity) {
		NF16StartCheck(p);
	}
	else {
		NF16noStartCheck(p);
	}

	s = start_source = p->source;

	quote = get(s);
	if(quote == BADCHAR)
	return error(p, "Input error: %s", s->error_msg);
	if(quote != '\'' && quote != '"')
	{
	unget(s);		/* For error position */
	return error(p, "Expected quoted string %s, but got %s",
			 where, escape(quote, p->escbuf[0]));
	}

	p->pbufnext = 0;

	while(1)
	{
	switch(c = get(s))
	{
	case BADCHAR:
		return error(p, "Input error: %s", s->error_msg);

	case '\r':
	case '\n':
	case '\t':
		if(!((type == LT_pubid && c != '\t') || /* no tab in pubid */
		 ((type == LT_cdata_attr || type == LT_tok_attr) &&
		  ParserGetFlag(p, NormaliseAttributeValues))))
		{
		count++;
		break;
		}
		if(count > 0)
		{
			ifNF16wrong(p,count+1,count)
			return error(p, "not normalized: %s", where);
		require(transcribe(p, count+1, count));
		}
		count = 0;
		ExpandBuf(p->pbuf, p->pbufnext+1);
		p->pbuf[p->pbufnext++] = ' ';
			NF16noStartCheck(p); /* space resets normalization checking */
		break;

	case '<':
		if((type == LT_tok_attr || type == LT_cdata_attr) &&
		   ParserGetFlag(p, XMLMiscWFErrors))
		return error(p, "Illegal character '<' %s", where);
		count++;
		break;

	case XEOE:
		if(s == start_source)
		return error(p, "Quoted string goes past entity end");
		if(count > 0)
		{
			ifNF16wrong(p,count,count)
			return error(p, "not normalized: %s", where);
		require(transcribe(p, count, count));
		}
		count = 0;
		ParserPop(p);
		s = p->source;
		break;

	case '%':
		if(!(type == LT_entity || type == LT_param_entity))
		{
		count++;
		break;
		}
		if(count > 0)
		{
			ifNF16wrong(p,count+1,count)
			return error(p, "not normalized: %s", where);
		require(transcribe(p, count+1, count));
		}
		count = 0;
		if(p->external_pe_depth == 0)
		{
		unget(s);	/* For error position */
		return error(p, "PE ref not allowed here in internal subset");
		}
		require(parse_reference(p, 1, 1, 1));
		s = p->source;
		break;

	case '&':
		if(ParserGetFlag(p, IgnoreEntities))
		goto deflt;
		if(type == LT_plain || type == LT_pubid)
		{
		count++;
		break;
		}

		if(count > 0)
		{
			ifNF16wrong(p,count+1,count)
			return error(p, "not normalized: %s", where);
		require(transcribe(p, count+1, count));
		}
		count = 0;
		if(looking_at(p, "#"))
		/* We *must* expand character references in parameter
		   entity definitions otherwise the result when it is
		   used may be syntactically incorrect. */
		{
		require(parse_character_reference(p,
				 type == LT_param_entity ||
				 ParserGetFlag(p, ExpandCharacterEntities)));
		}
		else
		{
		if(p->state == PS_error) /* looking_at may have set it */
			return -1;
		require(parse_reference(p, 0,
				!(type == LT_entity || type == LT_param_entity) &&
				ParserGetFlag(p, ExpandGeneralEntities),
					!ParserGetFlag(p, XMLMiscWFErrors)));
		s = p->source;
		}
		break;

	default:
	deflt:
		if(c == quote && p->source == start_source)
		goto done;
		count++;
	}

	if(at_eol(s) && count > 0)
	{
		ifNF16wrong(p,count,count)
		return error(p, "not normalized: %s", where);
		require(transcribe(p, count, count));
		count = 0;
	}
	}

done:
	if(count > 0)
	{
	ifNF16wrong(p,count+1,count)
		return error(p, "not normalized: %s", where);
	require(transcribe(p, count+1, count));
	}
	else
	ExpandBuf(p->pbuf, p->pbufnext+1);
	p->pbuf[p->pbufnext++] = 0;

	if((ParserGetFlag(p, NormaliseAttributeValues) && type == LT_tok_attr) ||
	   type == LT_pubid)
	{
	Char *old, *new;

	new = old = p->pbuf;

	/* Skip leading whitespace */

	while(*old == ' ')
	{
		changed = 1;
		old++;
	}

	/* Compress whitespace */

	for( ; *old; old++)
	{
		if(*old == ' ')
		{
		/* NB can't be at start because we skipped whitespace */
		if(new[-1] == ' ')
			changed = 1;
		else
			*new++ = ' ';
		}
		else
		*new++ = *old;
	}

	/* Trim trailing space (only one possible because we compressed) */

	if(new > p->pbuf && new[-1] == ' ')
	{
		changed = 1;
		new--;
	}

	*new = 0;
	}

	if(normalised)
	*normalised = changed;

	return 0;
}

static int parse_dtd(Parser p)
{
	InputSource s = p->source;
	Entity parent = s->entity;
	Entity internal_part = 0, external_part = 0;
	Char *name;
	char8 *publicid = 0, *systemid = 0;
	struct xbit xbit;

	xbit = p->xbit;		/* copy start position */
	xbit.type = XBIT_dtd;

	require(parse_name(p, "for name in dtd"));
	CopyName(name);
	maybe_uppercase(p, name);

	if(ParserGetFlag(p, XMLNamespaces))
	{
	require(check_qualname_syntax(p, name, "Doctype"));
	}

	skip_whitespace(s);

	require(parse_external_id(p, 0, &publicid, &systemid,
				  ParserGetFlag(p, XMLExternalIDs),
				  ParserGetFlag(p, XMLExternalIDs)));

	if(systemid || publicid)
	{
	external_part = NewExternalEntityN(0,0, publicid, systemid, 0, parent);
	if(!external_part)
	{
		Free(name);
		return error(p, "System error");
	}
	skip_whitespace(s);
	}

	if(looking_at(p, "["))
	{
	int line = s->line_number, cpos = s->next;

	require(read_markupdecls(p));
	skip_whitespace(s);
	internal_part = NewInternalEntity(0, p->pbuf, parent, line, cpos, 1);
	Consume(p->pbuf);
	if(!internal_part)
	{
		Free(name);
		FreeEntity(external_part);
		return error(p, "System error");
	}
	internal_part->is_internal_subset = 1;
	}
	if(p->state == PS_error)	/* looking_at may have set it */
	return -1;

	require(expect(p, '>', "at end of dtd"));

	if(p->state == PS_prolog1)
	p->state = PS_prolog2;
	else
	{
	Free(name);
	FreeEntity(external_part);
	FreeEntity(internal_part);

	if(ParserGetFlag(p, XMLStrictWFErrors))
		return error(p, "Misplaced or repeated DOCTYPE declaration");
	else if(!ParserGetFlag(p, IgnorePlacementErrors))
		warn(p, "Misplaced or repeated DOCTYPE declaration");

	/* Ignore it and return the next bit */
	return parse(p);
	}

	if(p->dtd->name)
	{
	Free(name);
	FreeEntity(external_part);
	FreeEntity(internal_part);

	/* This happens if we manually set the dtd */
	return parse(p);
	}

	p->dtd->name = name;
	p->dtd->internal_part = internal_part;
	p->dtd->external_part = external_part;

	if(internal_part)
	{
	if(ParserGetFlag(p, TrustSDD) || ParserGetFlag(p, ProcessDTD))
	{
		ParseDtd(p, internal_part);
		if(p->xbit.type == XBIT_error)
		return -1;
	}
	}

	if(external_part)
	{
	if((ParserGetFlag(p, TrustSDD) &&
		(ParserGetFlag(p, Validate) || p->standalone != SDD_yes)) ||
	   (!ParserGetFlag(p, TrustSDD) &&
		ParserGetFlag(p, ProcessDTD)))
	{
		ParseDtd(p, external_part);
		if(p->xbit.type == XBIT_error)
		return -1;
	}
	}

	p->xbit = xbit;
	return 0;
}

static int read_markupdecls(Parser p)
{
	InputSource s = p->source;
	int depth=1;
	int c, d, hyphens=0;
	int count = 0;

	p->pbufnext = 0;

	while(1)
	{
	c = get(s);
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
	if(c == XEOE)
		return error(p, "EOE in DTD");
	if(c == '-')
		hyphens++;
	else
		hyphens = 0;

	count++;

	switch(c)
	{
	case ']':
		if(--depth == 0)
		{
		count--;	/* We don't want the final ']' */
		require(transcribe(p, count+1, count));
		p->pbuf[p->pbufnext++] = 0;
		return 0;
		}
		break;

	case '[':
		depth++;
		break;

	case '"':
	case '\'':
		while((d = get(s)) != XEOE)
		{
		if(d == BADCHAR)
			return error(p, "Input error: %s", s->error_msg);
		count++;
		if(at_eol(s))
		{
			require(transcribe(p, count, count));
			count = 0;
		}
		if(d == c)
			break;
		}
		if(d == XEOE)
		return error(p, "EOE in DTD");
		break;

	case '-':
		if(hyphens < 2)
		break;
		hyphens = 0;
		while((d = get(s)) != XEOE)
		{
		if(d == BADCHAR)
			return error(p, "Input error: %s", s->error_msg);
		count++;
		if(at_eol(s))
		{
			require(transcribe(p, count, count));
			count = 0;
		}
		if(d == '-')
			hyphens++;
		else
			hyphens = 0;
		if(hyphens == 2)
			break;
		}
		if(d == XEOE)
		return error(p, "EOE in DTD");
		hyphens = 0;
		break;

	default:
		break;
	}

	if(at_eol(s) && count > 0)
	{
		require(transcribe(p, count, count));
		count = 0;
	}
	}
}

static int process_nsl_decl(Parser p)
{
	InputSource s = p->source;
	int c, count = 0;

	s->entity->ml_decl = ML_nsl;

	/* The default character encoding for nSGML files is ascii-ash */
	if(s->entity->encoding == CE_UTF_8)
	s->entity->encoding = CE_unspecified_ascii_superset;

	/* Syntax is <?NSL DDB unquoted-filename 0> */

	if(!looking_at(p, "DDB "))
	{
	if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	return error(p, "Expected \"DDB\" in NSL declaration");
	}

	while(c = get(s), !is_xml_whitespace(c))
	switch(c)
	{
	case BADCHAR:
		return error(p, "Input error: %s", s->error_msg);

	case XEOE:
		return error(p, "EOE in NSL declaration");

	case '>':
		return error(p, "Syntax error in NSL declaration");

	default:
		count++;
	}

	p->pbufnext = 0;
	require(transcribe(p, count+1, count));
	p->pbuf[p->pbufnext++] = 0;

	skip_whitespace(s);
	if(!looking_at(p, "0>"))
	{
	if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	return error(p, "Expected \"0>\" at end of NSL declaration");
	}

	if(!(s->entity->ddb_filename = duptochar8(p->pbuf)))
	return error(p, "System error");

	return 0;
}

static int process_xml_decl(Parser p)
{
	InputSource s = p->source;
	enum {None, V, E, S} which, last = None;
	Char *Value, *cp;
	char8 *value;
	CharacterEncoding enc = CE_unknown;
	Char c;

	/*
	 * If we are reading an external entity, should the XML declaration
	 * (actually "text declaration") be included as part of the replacement
	 * text?  The standard does not as far as I can see define the
	 * replacement text of an external entity, but it says "a parsed
	 * entity's contents are referred to as its replacement text" and
	 * the production for extParsedEnt is "TextDecl? content".  If the
	 * "contents" of the entity are identified with "content" in the
	 * production, then clearly the text declaration is not part of the
	 * replacement text.  On the other hand, this would be an inconsistency
	 * between XML and SGML (which regards the declaration as just another
	 * processing instruction), and there aren't any of those.
	 *
	 * It seems quite reasonable to want to put an encoding declaration
	 * on an external entity containing only PCDATA, and this would be
	 * illegal if the text declaration were inserted.  Furthermore, it's
	 * way too much trouble to save the text declaration as well as parse it.
	 */

	s->entity->ml_decl = ML_xml;

	/* Save the string buffer because it may already be in use */
	p->save_pbuf = p->pbuf;
	p->save_pbufsize = p->pbufsize;
	p->save_pbufnext = p->pbufnext;
	Consume(p->pbuf);

	while(!looking_at(p, "?>"))
	{
	if(looking_at(p, "version"))
		which = V;
	else if(looking_at(p, "encoding"))
		which = E;
	else if(looking_at(p, "standalone"))
		which = S;
	else if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	else
		return error(p, "Expected \"version\", \"encoding\" or "
			 "\"standalone\" in XML declaration");

	if(which <= last)
	{
		if(ParserGetFlag(p, XMLStrictWFErrors))
		return error(p, "Repeated or misordered attributes "
					"in XML declaration");
		warn(p, "Repeated or misordered attributes in XML declaration");
	}
	last = which;

	skip_whitespace(s);
	require(expect(p, '=', "after attribute name in XML declaration"));
	skip_whitespace(s);

	require(parse_string(p, "for attribute value in XML declaration",
				 LT_plain, 0));

	maybe_uppercase(p, p->pbuf);
	Value = p->pbuf;

	if(which == E)
	{
		if(!is_ascii_alpha(Value[0]))
		return error(p, "Encoding name does not begin with letter");
		for(cp=Value+1; *cp; cp++)
		if(!is_ascii_alpha(*cp) && !is_ascii_digit(*cp) &&
		   *cp != '.' && *cp != '_' && *cp != '-')
			return error(p, "Illegal character %s in encoding name",
				 escape(*cp, p->escbuf[0]));

		value = tochar8(Value);

		enc = FindEncoding(value);
		if(enc == CE_unknown)
		return error(p, "Unknown declared encoding %s", value);

		if(EncodingsCompatible(p->source->entity->encoding, enc, &enc))
		{
#if CHAR_SIZE == 8
		/* We ignore the declared encoding in 8-bit mode,
		   and treat it as a random ascii superset. */
#else
		p->source->entity->encoding = enc;
#endif
		}
		else
		return error(p, "Declared encoding %s is incompatible with %s "
					"which was used to read it",
			 CharacterEncodingName[enc],
			 CharacterEncodingName[p->source->entity->encoding]);

		s->entity->encoding_decl = enc;
	}

	if(which == S)
	{
		value = tochar8(Value);

		if(str_maybecase_cmp8(p, value, "no") == 0)
		p->standalone = SDD_no;
		else if(str_maybecase_cmp8(p, value, "yes") == 0)
		p->standalone = SDD_yes;
		else
		return error(p, "Expected \"yes\" or \"no\" "
					"for standalone in XML declaration");

		s->entity->standalone_decl = p->standalone;
	}

	if(which == V)
	{
		for(cp=Value; *cp; cp++)
		if(!is_ascii_alpha(*cp) && !is_ascii_digit(*cp) &&
		   *cp != '.' && *cp != '_' && *cp != '-' && *cp != ':')
			return error(p, "Illegal character %s in version number",
				 escape(*cp, p->escbuf[0]));

		if(!s->entity->version_decl)
		{
		if(!(s->entity->version_decl = duptochar8(Value)))
			return error(p, "System error");

		if(strcmp8(s->entity->version_decl, "1.0") == 0)
			s->entity->xml_version = XV_1_0;
		else if(strcmp8(s->entity->version_decl, "1.1") == 0)
			s->entity->xml_version = XV_1_1;
		else
		{
			if(ParserGetFlag(p, XMLStrictWFErrors))
			return error(p, "Version number \"%s\" not supported",
					 s->entity->version_decl);
			warn(p, "Version number \"%s\" not supported, "
				"parsing as XML 1.1",
			 s->entity->version_decl);
			s->entity->xml_version = XV_1_1;
		}
		}
	}

	c = get(s);
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
	if(c == '?')
		unget(s);
	else if(!is_xml_whitespace(c))
		return error(p, "Expected whitespace or \"?>\" after attribute "
				"in XML declaration");
	skip_whitespace(s);
	}

	/* Restore the string buffer */
	Free(p->pbuf);
	p->pbuf = p->save_pbuf;
	p->pbufsize = p->save_pbufsize;
	p->pbufnext = p->save_pbufnext;
	Consume(p->save_pbuf);

	return 0;
}

static int parse_cdata(Parser p)
{
	InputSource s = p->source;
	int c, c1=0, c2=0;
	int count = 0;
	NF16StartCheck(p);

	if(p->state <= PS_prolog2)
	return error(p, "CDATA section not allowed in prolog");
	if(p->state == PS_epilog)
	return error(p, "CDATA section not allowed after body");
	if(ParserGetFlag(p, Validate))
	{
	ElementDefinition e = VectorLast(p->element_stack).definition;
	if(!(e->type == CT_mixed || e->type == CT_any))
	{
		require(validity_error(p, "CDATA section not allowed here"));
		VectorLast(p->element_stack).context = 0;
	}
	}

	p->pbufnext = 0;

	while((c = get(s)) != XEOE)
	{
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
	count++;
	if(c == '>' && c1 == ']' && c2 == ']')
		break;
	if(at_eol(s))
	{
			ifNF16wrong(p,count,count)
				return error(p, "CDATA section not normalized");
		require(transcribe(p, count, count));
		count = 0;
	}
	c2 = c1; c1 = c;
	}

	if(c == XEOE)
	return error(p, "EOE in CDATA section");

	ifNF16wrong(p,count,count)
		return error(p, "CDATA section not normalized");
	require(transcribe(p, count, count-3));
	p->pbuf[p->pbufnext++] = 0;
	p->xbit.type = XBIT_cdsect;
	p->xbit.cdsect_chars = p->pbuf;
	Consume(p->pbuf);

	NF16StartCheck(p);
	return 0;
}

XBit ParseDtd(Parser p, Entity e)
{
	InputSource source, save;

	if(e->type == ET_external && p->entity_opener)
	source = p->entity_opener(e, p->entity_opener_arg);
	else
	source = EntityOpen(e);
	if(!source)
	{
	error(p, "Couldn't open dtd entity %s", EntityDescription(e));
	return &p->xbit;
	}

	save = p->source;
	p->source = 0;
	if(ParserPush(p, source) == -1)
	return &p->xbit;

	p->have_dtd = 1;

	p->external_pe_depth = (source->entity->type == ET_external);

	while(parse_markupdecl(p) == 0)
	;

	p->external_pe_depth = 0;

	/* don't restore after error, so user can call ParserPerror */
	if(p->xbit.type != XBIT_error)
	{
	ParserPop(p);		/* to free the input source */
	p->source = save;
	}

	return &p->xbit;
}

/*
 * Returns 0 normally, -1 if error, 1 at EOF.
 */
static int parse_markupdecl(Parser p)
{
	InputSource s, t;
	int c;
	int cur_line, cur_char;
	Entity cur_ent, cur_ext_ent = 0;

	if(p->state == PS_error)
	return error(p, "Attempt to continue reading DTD after error");

	clear_xbit(&p->xbit);

	require(skip_dtd_whitespace(p, 1));	/* allow PE even in internal subset */
	s = p->source;
	SourcePosition(s, &p->xbit.entity, &p->xbit.byte_offset);

	cur_ent = s->entity;
	cur_line = s->line_number;
	cur_char = s->next;

	/* Find the current *external* entity, to use as base URI for system
	   identifiers */

	for(t = s; t; t = t->parent)
	if(t->entity->type == ET_external)
	{
		cur_ext_ent = t->entity;
		break;
	}
	if(!cur_ext_ent)
	cur_ext_ent = p->document_entity;

	c = get(s);
	switch(c)
	{
	case BADCHAR:
	return error(p, "Input error: %s", s->error_msg);
	case XEOE:
	p->xbit.type = XBIT_none;
	return 1;
	case '<':
	if(looking_at(p, "!ELEMENT"))
	{
		require(expect_dtd_whitespace(p, "after ELEMENT"));
		return parse_element_decl(p, cur_ent);
	}
	else if(looking_at(p, "!ATTLIST"))
	{
		require(expect_dtd_whitespace(p, "after ATTLIST"));
		return parse_attlist_decl(p, cur_ent);
	}
	else if(looking_at(p, "!ENTITY"))
	{
		require(expect_dtd_whitespace(p, "after ENTITY"));
		return parse_entity_decl(p, cur_ent, cur_line, cur_char,
					 cur_ext_ent);
	}
	else if(looking_at(p, "!NOTATION"))
	{
		require(expect_dtd_whitespace(p, "after NOTATION"));
		return parse_notation_decl(p, cur_ent);
	}
	else if(looking_at(p, "!["))
		return parse_conditional(p, cur_ent);
	else if(looking_at(p, "?"))
	{
		require(parse_pi(p, cur_ent));
		if(p->dtd_callback)
		p->dtd_callback(&p->xbit, p->dtd_callback_arg);
		else
		FreeXBit(&p->xbit);
		return 0;
	}
	else if(looking_at(p, "!--"))
	{
		if(ParserGetFlag(p, ReturnComments))
		{
		require(parse_comment(p, 0, cur_ent));
		if(p->dtd_callback)
			p->dtd_callback(&p->xbit, p->dtd_callback_arg);
		else
			FreeXBit(&p->xbit);
		return 0;
		}
		else
		return parse_comment(p, 1, cur_ent);
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	else
		return error(p, "Syntax error after < in dtd");
	default:
	unget(s);		/* For error position */
	return error(p, "Expected \"<\" in dtd, but got %s",
			 escape(c, p->escbuf[0]));
	}
}

static int parse_reference(Parser p, int pe, int expand, int allow_external)
{
	Entity e;
	InputSource s;

	require(parse_name(p, pe ? "for parameter entity" : "for entity"));
	require(expect(p, ';', "after entity name"));

	if(ParserGetFlag(p, Validate) && VectorCount(p->element_stack) > 0)
	{
	ElementDefinition parent = VectorLast(p->element_stack).definition;

	if(parent->type == CT_empty)
	{
	   require(validity_error(p, "Entity reference not allowed in EMPTY element %S",
				  parent->name));
	}
	}

	if(!expand)
	return transcribe(p, 1 + p->namelen + 1, 1 + p->namelen + 1);

	e = FindEntityN(p->dtd, p->name, p->namelen, pe);
	if(!e)
	{
	Char *buf;
	Char *q;
	int i;

	if(pe || ParserGetFlag(p, ErrorOnUndefinedEntities))
		return error(p, "Undefined%s entity %.*S",
			 pe ? " parameter" : "" ,
			 p->namelen > 50 ? 50 : p->namelen, p->name);

	warn(p, "Undefined%s entity %.*S",
		 pe ? " parameter" : "",
		 p->namelen > 50 ? 50 : p->namelen, p->name);

	/* Fake a definition for it */

	buf = Malloc((5 + p->namelen + 1 + 1) * sizeof(Char));
	if(!buf)
		return error(p, "System error");
	q = buf;
	*q++ = '&'; *q++ = '#'; *q++ = '3'; *q++ = '8'; *q++ = ';';
	for(i=0; i<p->namelen; i++)
		*q++ = p->name[i];
	*q++ = ';';
	*q++ = 0;

	if(!(e = NewInternalEntityN(p->name, p->namelen, buf, 0, 0, 0, 0)))
		return error(p, "System error");
	if(!DefineEntity(p->dtd, e, 0))
		return error(p, "System error");

	if(ParserGetFlag(p, XMLNamespaces) && Strchr(e->name, ':'))
	{
		require(namespace_error(p, "Entity name %S contains colon",
					e->name));
	}
	}

	if(e->type == ET_external && e->notation)
	return error(p, "Illegal reference to unparsed entity \"%S\"",
			 e->name);

	if(!allow_external && e->type == ET_external)
	return error(p, "Illegal reference to external entity \"%S\"",
			 e->name);

	for(s = p->source; s; s = s->parent)
	if(s->entity == e)
		return error(p, "Recursive reference to entity \"%S\"", e->name);

	if(p->standalone == SDD_yes &&
	   parsing_internal(p) && e->is_externally_declared)
	{
	/* This is a WF error by erratum 34 */
	require(error(p, "Internal reference to externally declared entity "
			  "\"%S\" in document declared standalone",
			  e->name));
	}
	else if(ParserGetFlag(p, Validate) && p->standalone == SDD_yes &&
		p->state == PS_body && e->is_externally_declared)
	{
	require(validity_error(p, "Reference to externally declared entity "
					  "\"%S\" in document declared standalone",
				   e->name));
	}

	if(e->type == ET_external && p->entity_opener)
	s = p->entity_opener(e, p->entity_opener_arg);
	else
	s = EntityOpen(e);
	if(!s)
	return error(p, "Couldn't open entity %S, %s",
			 e->name, EntityDescription(e));

	require(ParserPush(p, s));
	NF16StartCheck(p);

	return 0;
}

static int parse_character_reference(Parser p, int expand)
{
	InputSource s = p->source;
	int c, base = 10;
	int count = 0;
	unsigned int code = 0;
	Char *ch = s->line + s->next;

	if(looking_at(p, "x"))
	{
	ch++;
	base = 16;
	}
	if(p->state == PS_error)	/* looking_at may have set it */
	return -1;

	while((c = get(s)) != ';')
	{
	if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
	if((c >= '0' && c <= '9') ||
	   (base == 16 && ((c >= 'A' && c <= 'F') ||
			   (c >= 'a' && c <= 'f'))))
		count++;
	else
	{
		unget(s);		/* For error position */
		return error(p,
			 "Illegal character %s in base-%d character reference",
			 escape(c, p->escbuf[0]), base);
	}
	}

	if(!expand)
	return transcribe(p, 2 + (base == 16) + count + 1,
				 2 + (base == 16) + count + 1);

	while(count-- > 0)
	{
	c = *ch++;
	if(c >= '0' && c <= '9')
		code = code * base + (c - '0');
	else if(c >= 'A' && c <= 'F')
		code = code * base + 10 + (c - 'A');
	else
		code = code * base + 10 + (c - 'a');
	}

/* allow refs to C0 and C1 controls except NUL in XML 1.1 */
#define is_xml11_legal_control(c) \
	((c >= 0x01 && c <= 0x1f) || (c >= 0x7f && c <= 0x9f))

#if CHAR_SIZE == 8
	if(code > 255 ||
	   !(is_xml_legal(code, p->map) ||
	 (p->xml_version >= XV_1_1 && is_xml11_legal_control(code))))
	{
	if(ParserGetFlag(p, ErrorOnBadCharacterEntities))
		return error(p, "0x%x is not a valid 8-bit XML character", code);
	else
		warn(p, "0x%x is not a valid 8-bit XML character; ignored", code);
	return 0;
	}
#else
	if(!(is_xml_legal(code, p->map) |
	 (p->xml_version >= XV_1_1 && is_xml11_legal_control(code))))
	{
	if(ParserGetFlag(p, ErrorOnBadCharacterEntities))
		return error(p, "0x%x is not a valid UTF-16 XML character", code);
	else
		warn(p, "0x%x is not a valid UTF-16 XML character; ignored", code);
	return 0;
	}

	if(code >= 0x10000)
	{
	/* Use surrogates */

	ExpandBuf(p->pbuf, p->pbufnext+2);
	code -= 0x10000;

	p->pbuf[p->pbufnext++] = (code >> 10) + 0xd800;
	p->pbuf[p->pbufnext++] = (code & 0x3ff) + 0xdc00;
		if(p->checker && NF16wrong==nf16checkL(p->checker,
				p->pbuf + p->pbufnext - 2, 2))
		   return error(p, "numeric character reference not normalized");

	return 0;
	}
#endif

	ExpandBuf(p->pbuf, p->pbufnext+1);
	p->pbuf[p->pbufnext++] = code;
	if(p->checker && NF16wrong==nf16checkL(p->checker,
				p->pbuf + p->pbufnext - 1, 1))
	   return error(p, "numeric character reference not normalized");


	return 0;
}

/* Called after reading '<!ELEMENT ' */

static int parse_element_decl(Parser p, Entity ent)
{
	Char *name;
	ContentType type;
	ElementDefinition def;
	Entity tent;
	ContentParticle cp = 0;
	Char *content = 0;

	require(parse_name(p, "for name in element declaration"));
	CopyName(name);
	maybe_uppercase(p, name);

	require(expect_dtd_whitespace(p, "after name in element declaration"));

	if(looking_at(p, "EMPTY"))
	{
	type = CT_empty;
	content = 0;
	}
	else if(looking_at(p, "ANY"))
	{
	type = CT_any;
	content = 0;
	}
	else if(looking_at(p, "("))
	{
	unget(p->source);
	if(!(cp = parse_cp(p)) ||
	   check_content_decl(p, cp) < 0 ||
	   !(content = stringify_cp(cp)))
	{
		FreeContentParticle(cp);
		Free(content);
		Free(name);
		return -1;
	}

	if(cp->type == CP_choice && cp->children[0]->type == CP_pcdata)
		type = CT_mixed;
	else
		type = CT_element;
	{
	}
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
	return -1;
	else
	{
	Free(name);
	return error(p, "Expected \"EMPTY\", \"ANY\", or \"(\" after name in "
				"element declaration");
	}

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	tent = p->source->entity;
	require(expect(p, '>', "at end of element declaration"));
	if(ParserGetFlag(p, Validate) && tent != ent)
	{
	require(validity_error(p, "Element declaration ends in different "
					  "entity from that in which it starts"));
	}

	if((def = FindElement(p->dtd, name)))
	{
	if(def->tentative)
	{
		RedefineElement(def, type, content, cp, 1);
		if(parsing_external_subset(p))
		def->is_externally_declared = 1;
	}
	else
	{
	  FreeContentParticle(cp);
	  Free(content);
	  if(ParserGetFlag(p, Validate))
	  {
		  require(validity_error(p, "Element %S declared more than once",
					 name));
	  }
	  else if(ParserGetFlag(p, WarnOnRedefinitions))
		  warn(p, "Ignoring redeclaration of element %S", name);
	}
	}
	else
	{
	if (!(def = DefineElement(p->dtd, name, type, content, cp, 1))) {
		return error(p, "System error");
	};
	if(parsing_external_subset(p))
		def->is_externally_declared = 1;
	if(ParserGetFlag(p, XMLNamespaces))
	{
		require(check_qualname_syntax(p, name, "Element"));
	}
	}

	Free(name);

	return 0;
}

/* Content model parsing */

static ContentParticle parse_cp(Parser p)
{
	ContentParticle cp;
	Entity ent;

	ent = p->source->entity;
	if(looking_at(p, "("))
	{
	if(!(cp = parse_choice_or_seq(p, ent)))
		return 0;
	}
	else if(looking_at(p, "#PCDATA"))
	{
	if(!(cp = Malloc(sizeof(*cp))))
	{
		error(p, "System error");
		return 0;
	}

	cp->type = CP_pcdata;
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
	return 0;
	else
	{
	if(parse_name(p, "in content declaration") < 0)
		return 0;
	maybe_uppercase_name(p);

	if(!(cp = Malloc(sizeof(*cp))))
	{
		error(p, "System error");
		return 0;
	}

	cp->type = CP_name;
	if(!(cp->element = FindElementN(p->dtd, p->name, p->namelen)))
	{
		if(!(cp->element = TentativelyDefineElementN(p->dtd,
							 p->name, p->namelen)))
		{
		error(p, "System error");
		return 0;
		}
		if(ParserGetFlag(p, XMLNamespaces))
		if(check_qualname_syntax(p, cp->element->name, "Element") < 0)
			return 0;
	}
	cp->name = cp->element->name;
	}


	if(looking_at(p, "*"))
	cp->repetition = '*';
	else if(looking_at(p, "+"))
	cp->repetition = '+';
	else if(looking_at(p, "?"))
	cp->repetition = '?';
	else if(p->state == PS_error)	/* looking_at may have set it */
	return 0;
	else
	cp->repetition = 0;

	return cp;
}

/* Called after '(' */

static ContentParticle parse_choice_or_seq(Parser p, Entity ent)
{
	ContentParticle cp, cp1;


	require0(skip_dtd_whitespace(p, p->external_pe_depth > 0));

	if(!(cp1 = parse_cp(p)))
	return 0;

	require0(skip_dtd_whitespace(p, p->external_pe_depth > 0));

	if(!(cp = parse_choice_or_seq_1(p, 1, 0, ent)))
	FreeContentParticle(cp1);
	else
	cp->children[0] = cp1;

	return cp;
}

/* Called before '|', ',', or ')' */

static ContentParticle parse_choice_or_seq_1(Parser p, int nchildren,
						 char sep, Entity ent)
{
	ContentParticle cp = 0, cp1;
	int nsep = get(p->source);

	if(nsep == BADCHAR)
	{
	error(p, "Input error: %s", p->source->error_msg);
	return 0;
	}

	if(nsep == ')')
	{
	/* We've reached the end */

	if(ParserGetFlag(p, Validate) && p->source->entity != ent)
	{
		if(validity_error(p, "Content particle ends in different "
					 "entity from that in which it starts") < 0)
		return 0;
	}

	if(!(cp = Malloc(sizeof(*cp))) ||
	   !(cp->children = Malloc(nchildren * sizeof(cp))))
	{
		Free(cp);
		error(p, "System error");
		return 0;
	}

	/* The standard does not specify whether '(foo)' is a choice or a
	   sequence.  We make it a choice so that (#PCDATA) comes out as
	   a choice, like other mixed models. */
	/* Erratum E50 has now resolved this the other way, but I don't
	   see any reason to change it, since it makes no difference. */

	cp->type = sep == ',' ? CP_seq : CP_choice;
	cp->nchildren = nchildren;

	return cp;
	}

	if(nsep != '|' && nsep != ',')
	{
	error(p, "Expected | or , or ) in content declaration, got %s",
		  escape(nsep, p->escbuf[0]));
	return 0;
	}

	if(sep && nsep != sep)
	{
	error(p, "Content particle contains both | and ,");
	return 0;
	}

	require0(skip_dtd_whitespace(p, p->external_pe_depth > 0));

	if(!(cp1 = parse_cp(p)))
	return 0;

	require0(skip_dtd_whitespace(p, p->external_pe_depth > 0));

	if(!(cp = parse_choice_or_seq_1(p, nchildren+1, (char)nsep, ent)))
	FreeContentParticle(cp1);
	else
	cp->children[nchildren] = cp1;

	return cp;
}

/* Check content particle matches Mixed or children */

static int check_content_decl(Parser p, ContentParticle cp)
{
	int i, j;

	if(cp->type == CP_choice && cp->children[0]->type == CP_pcdata)
	{
	if(cp->children[0]->repetition != 0)
		return error(p, "Malformed mixed content declaration");
	for(i=1; i<cp->nchildren; i++)
		if(cp->children[i]->type != CP_name ||
		   cp->children[i]->repetition != 0)
		return error(p, "Malformed mixed content declaration");

	if(cp->repetition != '*' &&
	   !(cp->nchildren == 1 && cp->repetition == 0))
		return error(p, "Malformed mixed content declaration");

	if(ParserGetFlag(p, Validate))
	{
		for(i=1; i<cp->nchildren; i++)
		for(j=i+1; j<cp->nchildren; j++)
			if(Strcmp(cp->children[i]->name,
				  cp->children[j]->name) == 0)
			{
			require(validity_error(p,
					  "Type %S appears more than once in "
					  "mixed content declaration",
						   cp->children[i]->name));
			}
	}

	return 0;
	}
	else
	return check_content_decl_1(p, cp);
}

static int check_content_decl_1(Parser p, ContentParticle cp)
{
	int i;

	switch(cp->type)
	{
	case CP_pcdata:
	return error(p, "Misplaced #PCDATA in content declaration");
	case CP_seq:
	case CP_choice:
	for(i=0; i<cp->nchildren; i++)
		if(check_content_decl_1(p, cp->children[i]) < 0)
		return -1;
	return 0;
	default:
	return 0;
	}
}

/* Reconstruct the content model as a string */

static Char *stringify_cp(ContentParticle cp)
{
	int size = size_cp(cp);
	Char *s;
	FILE16 *f;

	if(!(s = Malloc((size+1) * sizeof(Char))) ||
	   !(f = MakeFILE16FromString(s, (size + 1) * sizeof(Char), "w")))
	{
	Free(s);
	return 0;
	}

	print_cp(cp, f);
	s[size] = 0;

	Fclose(f);

	return s;
}

static void print_cp(ContentParticle cp, FILE16 *f)
{
	int i;

	switch(cp->type)
	{
	case CP_pcdata:
	Fprintf(f, "#PCDATA");
	break;
	case CP_name:
	Fprintf(f, "%S", cp->name);
	break;
	case CP_seq:
	case CP_choice:
	Fprintf(f, "(");
	for(i=0; i<cp->nchildren; i++)
	{
		if(i != 0)
		Fprintf(f, cp->type == CP_seq ? "," : "|");
		print_cp(cp->children[i], f);
	}
	Fprintf(f, ")");
	break;
	default:
	break;
	}

	if(cp->repetition)
	Fprintf(f, "%c", cp->repetition);
}

static int size_cp(ContentParticle cp)
{
	int i, s;

	switch(cp->type)
	{
	case CP_pcdata:
	s = 7;
	break;
	case CP_name:
	s = Strlen(cp->name);
	break;
	default:
	s = 2;
	for(i=0; i<cp->nchildren; i++)
	{
		if(i != 0)
		s++;
		s += size_cp(cp->children[i]);
	}
	break;
	}

	if(cp->repetition)
	s++;

	return s;
}

void FreeContentParticle(ContentParticle cp)
{
	int i;

	if(!cp)
	return;

	switch(cp->type)
	{
	case CP_pcdata:
	break;
	case CP_name:
	/* The name is part of the element definition, so don't free it */
	break;
	case CP_seq:
	case CP_choice:
	for(i=0; i<cp->nchildren; i++)
		FreeContentParticle(cp->children[i]);
	Free(cp->children);
	break;
	default:
	break;
	}

	Free(cp);
}

/* Called after reading '<!ATTLIST ' */

static int parse_attlist_decl(Parser p, Entity ent)
{
	Char *name;
	ElementDefinition element;
	Entity tent;
	AttributeType type;
	DefaultType default_type;
	AttributeDefinition a;
	Char **allowed_values, *t;
	Char *default_value;
	int nvalues=0, i, j;
	static Char s_xml_space[] = {'x','m','l',':','s','p','a','c','e',0},
		s_default[]   = {'d','e','f','a','u','l','t',0},
		s_preserve[]   = {'p','r','e','s','e','r','v','e',0};

	require(parse_name(p, "for name in attlist declaration"));
	CopyName(name);
	maybe_uppercase(p, name);

	if(!(element = FindElement(p->dtd, name)))
	{
	if(!(element = TentativelyDefineElement(p->dtd, name)))
		return error(p, "System error");
	if(ParserGetFlag(p, XMLNamespaces))
	{
		require(check_qualname_syntax(p, element->name, "Element"));
	}
	}

	Free(name);

	if(looking_at(p, ">"))
	unget(p->source);
	else
	{
	if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	require(expect_dtd_whitespace(p,
				"after element name in attlist declaration"));
	}

	while(tent = p->source->entity, !looking_at(p, ">"))
	{
	if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	require(parse_name(p, "for attribute in attlist declaration"));
	CopyName(name);
	maybe_uppercase(p, name);

	require(expect_dtd_whitespace(p, "after name in attlist declaration"));

	if(looking_at(p, "CDATA"))
		type = AT_cdata;
	else if(looking_at(p, "IDREFS"))
		type = AT_idrefs;
	else if(looking_at(p, "IDREF"))
		type = AT_idref;
	else if(looking_at(p, "ID"))
		type = AT_id;
	else if(looking_at(p, "ENTITIES"))
		type = AT_entities;
	else if(looking_at(p, "ENTITY"))
		type = AT_entity;
	else if(looking_at(p, "NMTOKENS"))
		type = AT_nmtokens;
	else if(looking_at(p, "NMTOKEN"))
		type = AT_nmtoken;
	else if(looking_at(p, "NOTATION"))
		type = AT_notation;
	else if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	else
		type = AT_enumeration;

	if(type != AT_enumeration)
	{
		require(expect_dtd_whitespace(p, "after attribute type"));
	}

	if(type == AT_notation || type == AT_enumeration)
	{
		require(expect(p, '(',
			   "or keyword for type in attlist declaration"));

		nvalues = 0;
		p->pbufnext = 0;
		do
		{
		require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
		if(type == AT_notation)
		{
			require(parse_name(p,
				   "for notation value in attlist declaration"));
		}
		else
		{
			require(parse_nmtoken(p,
				   "for enumerated value in attlist declaration"));
		}
		maybe_uppercase_name(p);
		ExpandBuf(p->pbuf, p->pbufnext + p->namelen + 1);
		memcpy(p->pbuf+p->pbufnext,
			   p->name,
			   p->namelen * sizeof(Char));
		p->pbuf[p->pbufnext + p->namelen] = 0;
		p->pbufnext += (p->namelen + 1);
		nvalues++;
		require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
		}
		while(looking_at(p, "|"));

		if(p->state == PS_error)	/* looking_at may have set it */
		return -1;

		require(expect(p, ')',
		  "at end of enumerated value list in attlist declaration"));
		require(expect_dtd_whitespace(p, "after enumerated value list "
						 "in attlist declaration"));

		allowed_values = Malloc((nvalues+1)*sizeof(Char *));
		if(!allowed_values)
		return error(p, "System error");
		for(i=0, t=p->pbuf; i<nvalues; i++)
		{
		allowed_values[i] = t;
		while(*t++)
			;
		}
		allowed_values[nvalues] = 0;

		Consume(p->pbuf);
	}
	else
		allowed_values = 0;

	if(looking_at(p, "#REQUIRED"))
		default_type = DT_required;
	else if(looking_at(p, "#IMPLIED"))
		default_type = DT_implied;
	else if(looking_at(p, "#FIXED"))
	{
		default_type = DT_fixed;
		require(expect_dtd_whitespace(p, "after #FIXED"));
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	else
		default_type = DT_none;

	if(default_type == DT_fixed || default_type == DT_none)
	{
		require(parse_string(p,
				 "for default value in attlist declaration",
				 type == AT_cdata ? LT_cdata_attr :
									LT_tok_attr, 0));
		default_value = p->pbuf;
		Consume(p->pbuf);
		if(type != AT_cdata && type != AT_entity && type != AT_entities)
		maybe_uppercase(p, default_value);
	}
	else
		default_value = 0;

	if(FindAttribute(element, name))
	{
		if(ParserGetFlag(p, WarnOnRedefinitions))
		warn(p, "Ignoring redeclaration of attribute %S", name);
		if(allowed_values)
		{
		Free(allowed_values[0]);
		Free(allowed_values);
		}
		if(default_value)
		Free(default_value);

		goto done;
	}

	if(ParserGetFlag(p, Validate) && type == AT_id)
	{
		if(element->id_attribute)
		{
		require(validity_error(p,
					   "ID attribute %S declared for element"
					   " %S which already had one (%S)",
					   name, element->name,
					   element->id_attribute->name));
		}
		if(default_type != DT_implied && default_type != DT_required)
		{
		require(validity_error(p,
					"ID attribute %S must have declared "
					"default of #IMPLIED or #REQUIRED, not %s",
					   name, DefaultTypeName[default_type]));
		}
	}

	if(ParserGetFlag(p, Validate) &&
	   (type == AT_notation || type == AT_enumeration))
		/* Duplicate enumerated values were made invalid by
		   an erratum of 2 Nov 2000 */
	{
		for(i=0; i<nvalues; i++)
		for(j=i+1; j<nvalues; j++)
			if(Strcmp(allowed_values[i], allowed_values[j]) == 0)
			{
			require(validity_error(p,
						   "Enumerated attribute %S has "
						   "duplicate allowed value %S",
						   name,
						   allowed_values[i],
						   allowed_values[j]));
			break;

			}
	}

	if(ParserGetFlag(p, Validate) && type == AT_notation)
	{
		/* Requirement for at most one notation attribute was
		   added in the errata of 17 Feb 1999 */
		if(element->notation_attribute)
		{
		require(validity_error(p,
				  "NOTATION attribute %S declared for element"
					   " %S which already had one (%S)",
					   name, element->name,
					   element->notation_attribute->name));
		}
	}

	if(ParserGetFlag(p, Validate) && Strcmp(name, s_xml_space) == 0)
	{
		if(type != AT_enumeration)
		{
		require(validity_error(p,
			  "xml:space attribute must have enumerated type"));
		}
		else for(i=0; i<nvalues; i++)
		if(Strcmp(allowed_values[i], s_default) != 0 &&
		   Strcmp(allowed_values[i], s_preserve) != 0)
		{
			require(validity_error(p,
	"xml:space attribute values may only be \"default\" or \"preserve\""));
			break;
		}
	}

	/* It doesn't seem to be required that xml:lang be declared
	   NMTOKEN, so don't check it */

	a = DefineAttribute(element, name, type, allowed_values,
				default_type, default_value, 1);
	if(!a)
		return error(p, "System error");
	if(parsing_external_subset(p))
		a->is_externally_declared = 1;
	if(ParserGetFlag(p, XMLID) &&
	   element->xml_id_attribute == a && a->type != AT_id)
	{
		warn(p, "xml:id error: xml:id attribute must be declared as type ID");
		/* Fix the declaration so that we treat it as type ID */
		a->type = AT_id;
	}
	if(ParserGetFlag(p, XMLNamespaces))
	{
		require(check_qualname_syntax(p, a->name, "Attribute"));
	}

	done:
	Free(name);

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	}

	if(ParserGetFlag(p, Validate) && tent != ent)
	{
	require(validity_error(p, "Attlist declaration ends in different "
					  "entity from that in which it starts"));
	}

	return 0;
}

/* Used for external dtd part, entity definitions and notation definitions. */
/* NB PE references are not allowed here (why not?) */

static int parse_external_id(Parser p, int required,
				 char8 **publicid, char8 **systemid,
				 int preq, int sreq)
{
	InputSource s = p->source;
	int c;
	Char *cp;

	*publicid = 0;
	*systemid = 0;

	if(looking_at(p, "SYSTEM"))
	{
	if(!sreq)
	{
		skip_whitespace(s);
		c = get(s); unget(s);
		if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
		if(c != '"' && c != '\'')
		return 0;
	}
	else
	{
		require(expect_dtd_whitespace(p, "after SYSTEM"));
	}

	require(parse_string(p, "for system ID", LT_plain, 0));
	if(!(*systemid = duptochar8(p->pbuf)))
		return error(p, "System error");
	}
	else if(looking_at(p, "PUBLIC"))
	{
	if(!preq && !sreq)
	{
		skip_whitespace(s);
		c = get(s); unget(s);
		if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
		if(c != '"' && c != '\'')
		return 0;
	}
	else
	{
		require(expect_dtd_whitespace(p, "after PUBLIC"));
	}

	require(parse_string(p, "for public ID", LT_pubid, 0));

	for(cp=p->pbuf; *cp; cp++)
		if(!is_ascii_alpha(*cp) && !is_ascii_digit(*cp) &&
		   strchr8("-'()+,./:=?;!*#@$_% \r\n", *cp) == 0)
		return error(p, "Illegal character %s in public id",
				 escape(*cp, p->escbuf[0]));

	if(!(*publicid = duptochar8(p->pbuf)))
		return error(p, "System error");

	if(!sreq)
	{
		skip_whitespace(s);
		c = get(s); unget(s);
		if(c == BADCHAR)
		return error(p, "Input error: %s", s->error_msg);
		if(c != '"' && c != '\'')
		return 0;
	}
	else
	{
		require(expect_dtd_whitespace(p, "after public id"));
	}

	require(parse_string(p, "for system ID", LT_plain, 0));
	if(!(*systemid = duptochar8(p->pbuf)))
		return error(p, "System error");
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
		return -1;
	else if(required)
	return error(p, "Missing or malformed external ID");

	return 0;
}

/* Called after reading '<!ENTITY ' */

static int parse_entity_decl(Parser p, Entity ent, int line, int chpos,
				 Entity ext_ent)
{
	Entity e, old, tent;
	int pe, t, namelen;
	Char *name;

	pe = looking_at(p, "%");	/* If it were a PE ref, we would
				   already have pushed it */
	if(p->state == PS_error)	/* looking_at may have set it */
	return -1;

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	require(parse_name(p, "for name in entity declaration"));
	namelen = p->namelen;
	CopyName(name);

	if(ParserGetFlag(p, XMLNamespaces) && Strchr(name, ':'))
	{
	require(namespace_error(p, "Entity name %S contains colon", name));
	}

	require(expect_dtd_whitespace(p, "after name in entity declaration"));

	if(looking_at(p, "'") || looking_at(p, "\""))
	{
	Char *value;

	unget(p->source);
	require(parse_string(p, "for value in entity declaration",
				 pe ? LT_param_entity : LT_entity, 0));
	value = p->pbuf;
	Consume(p->pbuf);

	if(!(e = NewInternalEntity(name, value, ent, line, chpos, 0)))
		return error(p, "System error");
	if(parsing_external_subset(p))
		e->is_externally_declared = 1;
#if 0
	Fprintf(Stderr, "internal %s entity %S\n",
		pe ? "parameter" : "general", name);
	Fprintf(Stderr, "base: %s\nreplacement text: %S\n",
		e->base_url ? e->base_url : "<null>", e->text);
#endif
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
	return -1;
	else
	{
	char8 *publicid, *systemid;
	NotationDefinition notation = 0;

	require(parse_external_id(p, 1, &publicid, &systemid, 1, 1));

	require((t = skip_dtd_whitespace(p, p->external_pe_depth > 0)));
	if(looking_at(p, "NDATA"))
	{
		if(t == 0)
		return error(p, "Whitespace missing before NDATA");
		if(pe)
		return error(p, "NDATA not allowed for parameter entity");
		require(expect_dtd_whitespace(p, "after NDATA"));
		require(parse_name(p, "for notation name in entity declaration"));
		maybe_uppercase_name(p);
		notation = FindNotationN(p->dtd, p->name, p->namelen);
		if(!notation)
		{
		notation =
			TentativelyDefineNotationN(p->dtd, p->name, p->namelen);
		if(!notation)
			return error(p, "System error");
		if(ParserGetFlag(p, XMLNamespaces) &&
		   Strchr(notation->name, ':'))
		{
			require(namespace_error(p,
						"Notation name %S contains colon",
						notation->name));
		}
		}
	}
	if(p->state == PS_error)	/* looking_at may have set it */
		return -1;

	/* XXX we make the current external entity the parent so that
	   system IDs are resoved correctly.  Should we instead record
	   both parents? */
	if(!(e = NewExternalEntityN(name, namelen,
					publicid, systemid, notation, ext_ent)))
		return error(p, "System error");
	if(parsing_external_subset(p) || ent->is_externally_declared)
		e->is_externally_declared = 1;
#if 0
	Fprintf(Stderr, "external %s entity %S\n",
		pe ? "parameter" : "general", name);
	Fprintf(Stderr, "base: %s\nsystem identifier: %s\n",
		e->base_url ? e->base_url : "<null>", e->systemid);
#endif
	}

	Free(name);

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	tent = p->source->entity;
	require(expect(p, '>', "at end of entity declaration"));
	if(ParserGetFlag(p, Validate) && tent != ent)
	{
	require(validity_error(p, "Entity declaration ends in different "
					  "entity from that in which it starts"));
	}

	if((old = FindEntity(p->dtd, e->name, pe)))
	{
	if(old->parent == xml_builtin_entity)
	{
		if(e->type != ET_internal ||
		   (ParserGetFlag(p, ExpandCharacterEntities) &&
			Strcmp(e->text, old->text) != 0))
		warn(p, "Non-standard declaration of predefined "
				"entity %S (ignored)",
			 e->name);
	}
	else
	{
		if(ParserGetFlag(p, WarnOnRedefinitions))
		warn(p, "Ignoring redefinition of%s entity %S",
			 pe ? " parameter" : "", e->name);
	}

	FreeEntity(e);
	}
	else
	if(!DefineEntity(p->dtd, e, pe))
		return error(p, "System error");

	return 0;
}

static int parsing_internal(Parser p)
{
	Entity e = p->source->entity;

	if(e == p->document_entity)
	return 1;
	if(e->type == ET_external)
	return 0;
	if(e->is_externally_declared)
	return 0;
	return 1;
}

/* NB assumes we are parsing the DTD */

static int parsing_external_subset(Parser p)
{
	Entity e = p->source->entity;

	return !e->is_internal_subset;
}

/* Called after reading '<!NOTATION ' */

static int parse_notation_decl(Parser p, Entity ent)
{
	Char *name;
	char8 *publicid, *systemid;
	NotationDefinition def;
	Entity tent;

	require(parse_name(p, "for name in notation declaration"));
	CopyName(name);
	maybe_uppercase(p, name);

	require(expect_dtd_whitespace(p, "after name in notation declaration"));

	require(parse_external_id(p, 1, &publicid, &systemid, 1, 0));

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	tent = p->source->entity;
	require(expect(p, '>', "at end of notation declaration"));
	if(ParserGetFlag(p, Validate) && tent != ent)
	{
	require(validity_error(p, "Notation declaration ends in different "
					  "entity from that in which it starts"));
	}

	if((def = FindNotation(p->dtd, name)))
	{
	if(def->tentative)
		RedefineNotation(def, publicid, systemid, ent);
	else
		if(ParserGetFlag(p, WarnOnRedefinitions))
		{
		warn(p, "Ignoring redefinition of notation %S", name);
		if(publicid) Free(publicid);
		if(systemid) Free(systemid);
		}
	}
	else
	{
	if(!DefineNotation(p->dtd, name, publicid, systemid, ent))
		return error(p, "System error");
	if(ParserGetFlag(p, XMLNamespaces) && Strchr(name, ':'))
	{
		require(namespace_error(p, "Notation name %S contains colon",
					name));
	}
	}

	Free(name);

	return 0;
}

static int parse_conditional(Parser p, Entity ent)
{
	int depth=1;
	Entity tent;

	if(p->external_pe_depth == 0)
	return error(p, "Conditional section not allowed in internal subset");

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	if(looking_at(p, "INCLUDE"))
	{
	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));

	tent = p->source->entity;
	require(expect(p, '[', "at start of conditional section"));
	if(ParserGetFlag(p, Validate) && tent != ent)
	{
		require(validity_error(p, "[ of conditional section in "
					  "different entity from <!["));
	}

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));

	while(!looking_at(p, "]"))
	{
		switch(parse_markupdecl(p))
		{
		case 1:
		return error(p, "EOF in conditional section");
		case -1:
		return -1;
		}
		require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	}
	tent = p->source->entity;

	if(!looking_at(p, "]>"))
		return error(p, "]> required after ] in conditional section");

	if(ParserGetFlag(p, Validate) && tent != ent)
	{
		require(validity_error(p, "] of conditional section in "
					  "different entity from <!["));
	}
	}
	else if(looking_at(p, "IGNORE"))
	{
	/* Easy, because ]]> not even allowed in strings! */

	require(skip_dtd_whitespace(p, p->external_pe_depth > 0));
	tent = p->source->entity;
	require(expect(p, '[', "at start of conditional section"));
	if(ParserGetFlag(p, Validate) && tent != ent)
	{
		require(validity_error(p, "[ of conditional section in "
					  "different entity from <!["));
	}

	while(depth > 0)
	{
		switch(get(p->source))
		{
		case BADCHAR:
		return error(p, "Input error: %s", p->source->error_msg);
		case XEOE:
		if(p->source->parent)
			ParserPop(p);
		else
			return error(p, "EOF in ignored conditional section");
		break;
		case '<':
		if(looking_at(p, "!["))
			depth++;
		break;
		case ']':
		tent = p->source->entity;
		if(looking_at(p, "]>"))
			depth--;
		}
	}
	if(ParserGetFlag(p, Validate) && tent != ent)
	{
		require(validity_error(p, "]]> of conditional section in "
					  "different entity from <!["));
	}
	}
	else if(p->state == PS_error)	/* looking_at may have set it */
	return -1;
	else
	return error(p, "INCLUDE or IGNORE required in conditional section");

	return 0;
}

static void maybe_uppercase(Parser p, Char *s)
{
	if(ParserGetFlag(p, CaseInsensitive))
	while(*s)
	{
		*s = Toupper(*s);
		s++;
	}
}

static void maybe_uppercase_name(Parser p)
{
	int i;

	if(ParserGetFlag(p, CaseInsensitive))
	for(i=0; i<p->namelen; i++)
		p->name[i] = Toupper(p->name[i]);
}

static int str_maybecase_cmp8(Parser p, const char8 *a, const char8 *b)
{
	return
	ParserGetFlag(p, CaseInsensitive) ? strcasecmp8(a, b) : strcmp8(a, b);
}

static int is_ascii_alpha(int c)
{
	return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}

static int is_ascii_digit(int c)
{
	return c >= '0' && c <= '9';
}

/* Error handling */

static void verror(char8 *buf, int size, XBit bit, const char8 *format, va_list args)
{
	/* Print message before freeing xbit, so we can print data from it */
	Vsnprintf(buf, size, CE_ISO_8859_1, format, args);

	FreeXBit(bit);
	bit->type = XBIT_error;
	bit->error_message = buf;
}

static int error(Parser p, const char8 *format, ...)
{
	va_list args;

	va_start(args, format);
	verror(p->errbuf, sizeof(p->errbuf), &p->xbit, format, args);

	p->state = PS_error;

	return -1;
}

static int warn(Parser p, const char8 *format, ...)
{
	va_list args;
	struct xbit bit;

	clear_xbit(&bit);

	va_start(args, format);
	verror(p->errbuf, sizeof(p->errbuf), &bit, format, args);

	bit.type = XBIT_warning;

	if(p->warning_callback)
	p->warning_callback(&bit, p->warning_callback_arg);
	else
	ParserPerror(p, &bit);

	return 0;
}

/* Validity checks applied when the prolog is complete. */

static int validate_dtd(Parser p)
{
	Dtd d = p->dtd;
	ElementDefinition e;
	AttributeDefinition a;
	Entity ent;
	int i;

	if(!p->have_dtd)
	{
	if(!ParserGetFlag(p, NoNoDTDWarning))
	{
		require(validity_error(p,
				   "Document has no DTD, validating abandoned"));
	}
	ParserSetFlag(p, Validate, 0);
	return 0;
	}

	if(!(e = FindElement(d, d->name)) || e->tentative)
	{
	require(validity_error(p,
				   "Root element name %S not declared", d->name));
	}

	for(e = NextElementDefinition(d, 0); e; e = NextElementDefinition(d, e))
	if(e->type == CT_element || e->type == CT_mixed)
	{
		FSMNode endnode;
		e->fsm = NewFSM();
		if(!e->fsm)
		error(p, "System error");
		endnode = AddNode(e->fsm);
		if(!endnode)
		error(p, "System error");
		endnode->end_node = 1;
		e->fsm->start_node =
		translate_particle(e->fsm, e->particle, endnode);
		if(!e->fsm->start_node)
		error(p, "System error");
		if(e->type == CT_mixed)
		/* Mixed content may always be empty, even (#PCDATA) */
		e->fsm->start_node->end_node = 1;
#if DEBUG_FSM
		Printf("\nContent model for element %S is %S\n",
		   e->name, e->content);
		PrintFSM(Stdout, e->fsm, 0);
#endif
		SimplifyFSM(e->fsm);
		if(e->type == CT_element)
		{
		/* Don't do this for mixed content, to prevent extra error
		   message for (#PCDATA|a|a)* which we already reported */
		require(check_deterministic(p, e));
		}
#if DEBUG_FSM
		Printf("\nContent model for element %S is %S\n",
		   e->name, e->content);
		PrintFSM(Stdout, e->fsm, 1);
#endif
	}

	/* check all NDATA notations declared */

	for(ent = NextEntity(d, 0); ent; ent = NextEntity(d, ent))
	if(ent->notation && ent->notation->tentative)
	{
		require(validity_error(p, "In declaration of unparsed entity %S, "
					   "notation %S is undefined",
				   ent->name, ent->notation->name));
	}

	/* validate attribute defaults (do it here so all entities/notations
	   declared) and check notations in enumeration all declared */

	for(e = NextElementDefinition(d, 0); e; e = NextElementDefinition(d, e))
	for(a = NextAttributeDefinition(e, 0); a;
		a = NextAttributeDefinition(e, a))
	{
		if(a->default_value)
		{
		require(check_attribute_syntax(p, a, e, a->default_value,
						   "default value for attribute",
						   0));
		}
		if(a->type == AT_notation)
		{
		if(e->type == CT_empty)
		{
			require(validity_error(p,
					   "NOTATION attribute %S not allowed "
					   "on EMPTY element %S",
					   a->name, e->name));

		}

		for(i=0; a->allowed_values[i]; i++)
			if(!FindNotation(d, a->allowed_values[i]))
			{
			require(validity_error(p,
				  "In allowed values for attribute %S of "
				  "element %S, notation %S is not defined",
						   a->name, e->name,
						   a->allowed_values[i]));
			}
		}
	}

	return 0;
}

static int validate_final(Parser p)
{
	/* Check all IDs referred to were defined */

	hash_map(p->id_table, check_id, p);

	if(p->xbit.type == XBIT_error)
	return -1;

	return 0;
}

static HashMapRetType check_id(const HashEntryStruct *id_entry, void *pp)
{
	Parser p = (Parser)pp;

	if(!(int)hash_get_value(id_entry))
	validity_error(p,
			   "The ID %.*S was referred to but never defined",
			   hash_get_key_len(id_entry) / sizeof(Char),
			   hash_get_key(id_entry));

#ifdef FOR_LT
	return 1;
#endif
}

/* Determine whether an element is valid at this point.
 * Returns the new context, or NULL if invalid.
 */

static FSMNode validate_content(FSMNode context, ElementDefinition e)
{
	int i;

	for(i=0; i<VectorCount(context->edges); i++)
	if(context->edges[i]->label == e)
		return context->edges[i]->destination;

	return 0;
}

static FSM NewFSM(void)
{
	FSM fsm;

	if(!(fsm = Malloc(sizeof(*fsm))))
	return 0;
	VectorInit(fsm->nodes);
	fsm->start_node = 0;

	return fsm;
}

void FreeFSM(FSM fsm)
{
	int i,j;

	if(!fsm)
	return;

	for(i=0; i<VectorCount(fsm->nodes); i++)
	{
	FSMNode node = fsm->nodes[i];
	for(j=0; j<VectorCount(node->edges); j++)
		Free(node->edges[j]);
	Free(node->edges);
	Free(node);
	}

	Free(fsm->nodes);
	Free(fsm);
}

static FSMNode AddNode(FSM fsm)
{
	FSMNode node;

	if(!(node = Malloc(sizeof(*node))))
	return 0;
	node->fsm = fsm;
	node->mark = node->end_node = 0;
	node->id = VectorCount(fsm->nodes);
	VectorInit(node->edges);
	if(!VectorPush(fsm->nodes, node))
	return 0;

	return node;
}

static void DeleteNode(FSMNode node)
{
	int i;
	FSM fsm = node->fsm;

	fsm->nodes[node->id] = 0;
	for(i=0; i<VectorCount(node->edges); i++)
	Free(node->edges[i]);
	Free(node->edges);
	Free(node);
}

static void DeleteEdge(FSMEdge edge)
{
	edge->source->edges[edge->id] = 0;
	Free(edge);
}

/* After deleting nodes there will be null nodes in the node list.
   This function removes them. */

static void CleanupFSM(FSM fsm)
{
	int i, j;

	for(i=j=0; i<VectorCount(fsm->nodes); i++)
	{
	if(fsm->nodes[i])
	{
		if(i > j)
		{
		fsm->nodes[j] = fsm->nodes[i];
		fsm->nodes[j]->id = j;
		}
		j++;
	}
	}
	VectorCount(fsm->nodes) = j;
}

/* After deleting edges there will be null edges in the edge list.
   This function removes them. */

static void CleanupNode(FSMNode node)
{
	int i, j;

	for(i=j=0; i<VectorCount(node->edges); i++)
	{
	if(node->edges[i])
	{
		if(i > j)
		{
		node->edges[j] = node->edges[i];
		node->edges[j]->id = j;
		}
		j++;
	}
	}
	VectorCount(node->edges) = j;
}

static FSMEdge AddEdge(FSMNode source, FSMNode destination, void *label)
{
	FSMEdge edge;

	if(!(edge = Malloc(sizeof(*edge))))
	return 0;
	edge->label = label;
	edge->source = source;
	edge->destination = destination;
	edge->id = VectorCount(source->edges);
	if(!VectorPush(source->edges, edge))
	return 0;

	return edge;
}

static void UnMarkFSM(FSM fsm, int value)
{
	int i;

	for(i=0; i<VectorCount(fsm->nodes); i++)
	fsm->nodes[i]->mark &= ~value;
}

/* Remove all epsilon links from a FSM */

#define useful 1
#define busy 2

static int SimplifyFSM(FSM fsm)
{
	int i, j;
	FSMNode node;
	FSMEdge edge;

	/* First find all the useful nodes, ie those pointed to by a
	   non-epsilon edge. */

	fsm->start_node->mark |= useful;
	for(i=0; i<VectorCount(fsm->nodes); i++)
	{
	node = fsm->nodes[i];
	for(j=0; j<VectorCount(node->edges); j++)
	{
		edge = node->edges[j];
		if(edge->label != Epsilon)
		edge->destination->mark |= useful;
	}
	}

	/* Now add to each useful node all the non-epsilon edges of
	   the nodes in its epsilon-closure. */

	for(i=0; i<VectorCount(fsm->nodes); i++)
	{
	node = fsm->nodes[i];
	if(!(node->mark & useful))
		continue;
	node->mark |= busy;
	for(j=0; j<VectorCount(node->edges); j++)
	{
		edge = node->edges[j];
		if(edge->label == Epsilon)
		if(!add_epsilon_closure(node, edge->destination))
			return 0;
	}
	UnMarkFSM(fsm, busy);
	}

	/* Now remove all useless nodes and epsilon edges from useful nodes */

	for(i=0; i<VectorCount(fsm->nodes); i++)
	{
	node = fsm->nodes[i];
	if(node->mark & useful)
	{
		for(j=0; j<VectorCount(node->edges); j++)
		{
		edge = node->edges[j];
		if(edge->label == Epsilon)
			DeleteEdge(edge);
		}
		CleanupNode(node);
	}
	else
		DeleteNode(node);
	}
	CleanupFSM(fsm);

	UnMarkFSM(fsm, useful);

	/* Now change the edge labels to be ElementDefinitions instead of CPs */

	for(i=0; i<VectorCount(fsm->nodes); i++)
	{
	node = fsm->nodes[i];
	for(j=0; j<VectorCount(node->edges); j++)
	{
		edge = node->edges[j];
		if(edge->label == Epsilon || edge->label == PCDataElement)
		continue;
		edge->label = ((ContentParticle)edge->label)->element;
	}
	}

	return 1;
}

static int add_epsilon_closure(FSMNode base, FSMNode node)
{
	int i, j;
	FSMEdge edge, edge2;

	if(node->mark & busy)
	return 1;
	node->mark |= busy;

	if(node->end_node)
	base->end_node = 1;
	for(i=0; i<VectorCount(node->edges); i++)
	{
	edge = node->edges[i];
	if(edge->label == Epsilon)
	{
		if(!add_epsilon_closure(base, edge->destination))
		return 0;
	}
	else
	{
		/* Do we already have an edge corresponding to this very
		   content particle? */
		for(j=0; j<VectorCount(base->edges); j++)
		{
		edge2 = base->edges[j];
		if(edge2->label == edge->label &&
		   edge2->destination == edge->destination)
			break;
		}
		if(j == VectorCount(base->edges) &&
		   !AddEdge(base, edge->destination, edge->label))
		return 0;
	}
	}

	return 1;
}

#if DEBUG_FSM
static void PrintFSM(FILE16 *out, FSM fsm, int relabelled)
{
	int i, j;
	FSMNode node;
	FSMEdge edge;
	ElementDefinition elt;

	for(i=0; i<VectorCount(fsm->nodes); i++)
	{
	node = fsm->nodes[i];
	Fprintf(out, "%d", node->id);
	if(node == fsm->start_node)
		Fprintf(out, "S");
	if(node->end_node)
		Fprintf(out, "E");

	for(j=0; j<VectorCount(node->edges); j++)
	{
		edge = node->edges[j];
		if(edge->label == Epsilon)
		Fprintf(out, "\t{Epsilon} -> %d\n", edge->destination->id);
		else if(edge->label == PCDataElement)
		Fprintf(out, "\t#PCDATA -> %d\n", edge->destination->id);
		else
		{
		if(relabelled)
			elt = (ElementDefinition)edge->label;
		else
			elt = ((ContentParticle)edge->label)->element;
		Fprintf(out, "\t%S -> %d\n", elt->name, edge->destination->id);
		}
	}
	if(VectorCount(node->edges) == 0)
		printf("\n");
	}
}
#endif

static FSMNode translate_particle_1(FSM fsm, ContentParticle cp, FSMNode next)
{
	FSMNode node, n;
	int i;

	if(!(node = AddNode(fsm)))
	return 0;

	switch(cp->type)
	{
	case CP_name:
	/* We initially label the edges with the content particles, so
	   that we can recognise two "a" edges as being from different
	   CPs for the purpose of determinism checking.  We will change
	   the label to be the element definition later. */
	if(!AddEdge(node, next, cp))
		return 0;
	break;
	case CP_pcdata:
	if(!AddEdge(node, next, PCDataElement))
		return 0;
	break;
	case CP_choice:
	for(i=0; i<cp->nchildren; i++)
	{
		if(!(n = translate_particle(fsm, cp->children[i], next)) ||
		   !AddEdge(node, n, Epsilon))
		return 0;
	}
	break;
	case CP_seq:
	n = next;
	for(i=cp->nchildren-1; i>=0; i--)
	{
		if(!(n = translate_particle(fsm, cp->children[i], n)))
		return 0;
	}
	if(!AddEdge(node, n, Epsilon))
		return 0;
	break;
	default:
	break;
   }

	return node;
}

static FSMNode translate_particle(FSM fsm, ContentParticle cp, FSMNode next)
{
	FSMNode node1, node2, sub;

	switch(cp->repetition)
	{
	case 0:
	return translate_particle_1(fsm, cp, next);
	case '*':
	if(!(node1 = AddNode(fsm)) ||
	   !(sub = translate_particle_1(fsm, cp, node1)) ||
	   !AddEdge(node1, sub, Epsilon) ||
	   !AddEdge(node1, next, Epsilon))
		return 0;
	return node1;
	case '+':
	if(!(node1 = AddNode(fsm)) ||
	   !(node2 = AddNode(fsm)) ||
	   !(sub = translate_particle_1(fsm, cp, node2)) ||
	   !AddEdge(node1, sub, Epsilon) ||
	   !AddEdge(node2, sub, Epsilon) ||
	   !AddEdge(node2, next, Epsilon))
		return 0;
	return node1;
	case '?':
	if(!(node1 = AddNode(fsm)) ||
	   !(sub = translate_particle_1(fsm, cp, next)) ||
	   !AddEdge(node1, sub, Epsilon) ||
	   !AddEdge(node1, next, Epsilon))
		return 0;
	return node1;
	}

	return 0;			/* can't happen */
}

static int check_deterministic(Parser p, ElementDefinition element)
{
	int t;

	t = check_deterministic_1(p, element, element->fsm->start_node, 0);
	UnMarkFSM(element->fsm, busy);
	return t;
}

static int check_deterministic_1(Parser p, ElementDefinition element,
				 FSMNode node, ElementDefinition previous)
{
	int j, k;
	FSMEdge edge;
	Char empty_string[] = {0};

	if(node->mark & busy)
	return 0;
	node->mark |= busy;

	/* Does this node have two or more edges labelled the same? */

	for(j=0; j<VectorCount(node->edges); j++)
	{
	edge = node->edges[j];
	for(k=0; k<j; k++)
		if(node->edges[k]->label == edge->label)
		{
		require(validity_error(p,
			 "Content model for %S is not deterministic.   %s%S "
			 "there are multiple choices when the next element is %S.",
			 element->name,
			 previous ? "After element " : "At start of content",
			 previous ? previous->name : empty_string,
			 ((ElementDefinition)edge->label)->name));
		goto next;	/* Don't report more errors for this node */
		}
	}

next:

	/* Check its children */
	for(j=0; j<VectorCount(node->edges); j++)
	{
	edge = node->edges[j];
	require(check_deterministic_1(p, element, edge->destination,
					  (ElementDefinition)edge->label));
	}

	return 0;
}

static int validate_attribute(Parser p, AttributeDefinition a, ElementDefinition e, const Char *value)
{
	require(check_attribute_syntax(p, a, e, value, "attribute", 1));

	if(a->default_type == DT_fixed)
	if(Strcmp(value, a->default_value) != 0)
	{
		require(validity_error(p,
				   "The attribute %S of element %S does not "
				   "match the declared #FIXED value",
				   a->name, e->name));
	}

	if(a == e->xml_lang_attribute)
	{
	require(validate_xml_lang_attribute(p, e, value));
	}

	return 0;
}

static int validate_xml_lang_attribute(Parser p, ElementDefinition e, const Char *value)
{
	/* 1.1 will allow empty xml:lang values (and maybe 1.0 will be amended
	   to), and it no longer seems worth checking anything here. */
#if 0
	const Char *t;

	/* Look for the Langcode */

	if((value[0] == 'i' || value[0] == 'I' ||
	value[0] == 'x' || value[0] == 'X') &&
	   value[1] == '-')
	{
	/* IANA or user code */

	if(!is_ascii_alpha(value[2]))
		goto bad;
	for(t = value+3; is_ascii_alpha(*t); t++)
		;

	}
	else if(is_ascii_alpha(value[0]) && is_ascii_alpha(value[1]))
	{
	/* ISO639 code */
	t = value+2;
	}
	else
	goto bad;

	/* Look for a subcode */

	if(!*t)
	return 0;
	if(t[0] != '-' || !is_ascii_alpha(t[1]))
	goto bad;

	for(t=t+2; is_ascii_alpha(*t); t++)
	;

	if(!*t)
	return 0;

 bad:
	/* Not a validity error since erratum 73 */
	warn(p, "Dubious xml:lang attribute for element %S", e->name);
#endif
	return 0;
}

/* Check an attribute matches Name[s] or Nmtoken[s].
   Assume it has already been normalised (no leading or trailing
   whitespace, other whitespace normalised to single space). */

static int check_attribute_syntax(Parser p, AttributeDefinition a, ElementDefinition e, const Char *value, const char *message, int real_use)
{
	int nmchar = (a->type == AT_nmtoken || a->type == AT_nmtokens ||
		  a->type == AT_enumeration);
	int multiple = (a->type == AT_nmtokens || a->type == AT_entities ||
			a->type == AT_idrefs);

	const Char *q, *start = value;

	if(a->type == AT_cdata)
	return 0;		/* Nothing to check */

	if(!*value)
	{
	require(validity_error(p, "The %s %S of element %S "
					  "is declared as %s but is empty",
				   message, a->name, e->name,
				   AttributeTypeName[a->type]));
	return 0;
	}

	for(q=value; *q; q++)
	{
	if(!nmchar && q == start && !is_xml_namestart(*q, p->map))
	{
		require(validity_error(p, "The %s %S of element %S "
				   "is declared as %s but contains a token "
				   "that does not start with a name start character",
				   message, a->name, e->name,
				   AttributeTypeName[a->type]));
		return 0;
	}

	if(*q == ' ')
	{
		require(check_attribute_token(p, a, e, start, q-start, message,
					  real_use));
		start = q+1;

		if(!multiple)
		{
		require(validity_error(p, "The %s %S of element %S "
						  "is declared as %s but "
						  "contains more than one token",
					   message, a->name, e->name,
					   AttributeTypeName[a->type]));
		}
	}
	else if(!is_xml_namechar(*q, p->map))
	{
		require(validity_error(p, "The %s %S of element %S is declared "
					  "as %s but contains a character which "
					  "is not a name character",
				   message, a->name, e->name,
				   AttributeTypeName[a->type]));
		return 0;
	}
	}

	return check_attribute_token(p, a, e, start, q-start, message, real_use);
}

static int check_attribute_token(Parser p, AttributeDefinition a, ElementDefinition e, const Char *value, int length, const char *message, int real_use)
{
	Entity entity;
	NotationDefinition notation;
	int i, found;
	HashEntry id_entry;

	switch(a->type)
	{
	case AT_entity:
	case AT_entities:
	if(!real_use)
		return 0;		/* don't check defaults unless they're used */
	/* XXX Should maybe check for colons, but it must be invalid anyway
	   because otherwise the declaration would have been not-nwf */
	entity = FindEntityN(p->dtd, value, length, 0);
	if(!entity)
	{
		require(validity_error(p, "In the %s %S of element %S, "
					  "entity %.*S is undefined",
				   message, a->name, e->name, length, value));
	}
	else if(!entity->notation)
	{
		require(validity_error(p, "In the %s %S of element %S, "
					  "entity %.*S is not unparsed",
				   message, a->name, e->name, length, value));
	}
	break;
	case AT_id:
	if(!a->declared)
		/* don't validate undeclared xml:id attributes */
		return 0;
	/* fall through */
	case AT_idref:
	case AT_idrefs:
	if(!real_use)
		return 0;		/* don't check defaults unless they're used */
	id_entry = hash_find_or_add(p->id_table, value, length*sizeof(Char),
					&found);
	if(!id_entry)
		return error(p, "System error");
	if(!found)
	{
		hash_set_value(id_entry, (void *)(a->type == AT_id));
		if(ParserGetFlag(p, XMLNamespaces))
		for(i=0; i<length; i++)
			if(value[i] == ':')
			{
			require(namespace_validity_error(p, "ID %.*S contains colon", length, value));
			}
	}
	else if(a->type == AT_id)
	{
		int idinfo = (int)hash_get_value(id_entry);
		if(idinfo & 1)
		{
		require(validity_error(p, "Duplicate ID attribute value %.*S",
					   length, value));
		}
		else
		{
		if(idinfo & 2)
			warn(p, "xml:id error: duplicate ID attribute value %S", value);
		hash_set_value(id_entry, (void *)(idinfo | 1));
		}
	}
	break;
	case AT_notation:
	/* XXX Should maybe check for colons, but it must be invalid anyway
	   because otherwise the declaration would have been not-nwf */
	notation = FindNotationN(p->dtd, value, length);
	if(!notation)
	{
		require(validity_error(p, "In the %s %S of element %S, "
					  "notation %.*S is undefined",
				   message, a->name, e->name, length, value));
		break;
	}
	/* fall through */
	case AT_enumeration:
	for(i=0; a->allowed_values[i]; i++)
		if(Strncmp(value, a->allowed_values[i], length) == 0 &&
		   a->allowed_values[i][length] == 0)
		break;
	if(!a->allowed_values[i])
	{
		require(validity_error(p, "In the %s %S of element %S, "
					  "%.*S is not one of the allowed values",
				   message, a->name, e->name, length, value));
	}
	break;
	default:
	/* Nothing to check */
	break;
	}

	return 0;
}

#if not_yet
static int magically_transform_dtd(Parser p, Char *name, int namelen)
{
	int i;
	Char *prefix;

	for(i=0; i<namelen; i++)
	if(name[i] == ':')
		break;

	if(i < namelen)
	{
	if(!(prefix = Strndup(name, i)))
		return error(p, "System error");
	}
	else
	prefix = 0;

	require(ReprefixDtd(p->dtd, p->magic_prefix, prefix));

	Free(prefix);

	return 0;
}
#endif
