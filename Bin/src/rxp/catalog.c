#include "catalog.h"

#include "catutil.h"

#include "charset.h"
#include "string16.h"
#include "stdio16.h"
#include "dtd.h"
#include "url.h"
#include "input.h"
#include "xmlparser.h"
#include "namespaces.h"
#include "rxputil.h"

int catalog_debug = 0;

static int DoElement(XBit bit, Parser p, CatalogEntryFile c,
			 const char *base_uri, Prefer prefer);
static Parser OpenXMLDocument(char *uri);
static void CloseXMLDocument(Parser p);
static int SkipElement(XBit bit, Parser p);
static InputSource special_opener(Entity ent, void *arg);

typedef enum catalog_entry_type {
	CN_public, CN_system, CN_rewriteSystem, CN_delegatePublic,
	CN_delegateSystem, CN_uri, CN_rewriteURI, CN_delegateURI,
	CN_catalog, CN_group, CN_nextCatalog,
	CN_enum_count
} CatalogEntryType;

const char *PreferName[PR_enum_count] =
{
	"system", "public", "unspecified"
};

/* An empty catalog file, returned if a file cannot be read because of
   a resource error */
struct catalog_entry_file catalog_resource_error_data;
CatalogEntryFile catalog_resource_error = &catalog_resource_error_data;

#if 0
static const char *CatalogEntryTypeName[CN_enum_count] =
{
	"public", "system", "rewriteSystem", "delegatePublic",
	"delegateSystem", "uri", "rewriteURI", "delegateURI",
	"catalog", "group", "nextCatalog"
};
#endif

/*
 * Allocate a new catalog entry with the specified parameters.
 * Doesn't copy arguments.
 * Returns null if it can't be allocated.
 */

CatalogEntry NewCatalogEntry(char *match, char *value, Prefer prefer)
{
	CatalogEntry entry;

	if(!(entry = Malloc(sizeof(*entry))))
	return 0;

	entry->match = match;
	entry->value = value;
	entry->prefer = prefer;

	return entry;
}

/*
 * Free a catalog entry.
 * Frees the strings contained in it (which, NB, weren't copied).
 */

void FreeCatalogEntry(CatalogEntry entry)
{
	if(!entry)
	return;
	Free(entry->match);
	Free(entry->value);
	Free(entry);
}

/*
 * Free a catalog entry file.
 * Frees the entries contained in it.
 */

void FreeCatalogEntryFile(CatalogEntryFile c)
{
	int i;

	if(!c || c == catalog_resource_error)
	return;

	for(i=0; i<VectorCount(c->publicEntries); i++)
	FreeCatalogEntry(c->publicEntries[i]);
	Free(c->publicEntries);
	for(i=0; i<VectorCount(c->systemEntries); i++)
	FreeCatalogEntry(c->systemEntries[i]);
	Free(c->systemEntries);
	for(i=0; i<VectorCount(c->rewriteSystemEntries); i++)
	FreeCatalogEntry(c->rewriteSystemEntries[i]);
	Free(c->rewriteSystemEntries);
	for(i=0; i<VectorCount(c->delegatePublicEntries); i++)
	FreeCatalogEntry(c->delegatePublicEntries[i]);
	Free(c->delegatePublicEntries);
	for(i=0; i<VectorCount(c->delegateSystemEntries); i++)
	FreeCatalogEntry(c->delegateSystemEntries[i]);
	Free(c->delegateSystemEntries);
	for(i=0; i<VectorCount(c->uriEntries); i++)
	FreeCatalogEntry(c->uriEntries[i]);
	Free(c->uriEntries);
	for(i=0; i<VectorCount(c->rewriteURIEntries); i++)
	FreeCatalogEntry(c->rewriteURIEntries[i]);
	Free(c->rewriteURIEntries);
	for(i=0; i<VectorCount(c->delegateURIEntries); i++)
	FreeCatalogEntry(c->delegateURIEntries[i]);
	Free(c->delegateURIEntries);
	for(i=0; i<VectorCount(c->nextCatalogEntries); i++)
	Free(c->nextCatalogEntries[i]);
	Free(c->nextCatalogEntries);

	Free(c);
}

/*
 * Reads a catalog entry file from the specified URI.
 * Returns null in case of a fatal error, and a distinguished empty
 * catalog (catalog_resource_error) in the case of a resource error.
 */

CatalogEntryFile ReadCatalogEntryFile(char *catalog_uri)
{
	Parser p;
	CatalogEntryFile c;

	if(!(catalog_uri = NormalizeSystem8(catalog_uri)))
	return 0;

#if 0
	 Printf("reading %s\n", catalog_uri);
#endif

	if(!(p = OpenXMLDocument(catalog_uri)))
	return catalog_resource_error;

	if(!(c = Malloc(sizeof(*c))))
	return 0;

	VectorInit(c->publicEntries);
	VectorInit(c->systemEntries);
	VectorInit(c->rewriteSystemEntries);
	VectorInit(c->delegatePublicEntries);
	VectorInit(c->delegateSystemEntries);
	VectorInit(c->uriEntries);
	VectorInit(c->rewriteURIEntries);
	VectorInit(c->delegateURIEntries);
	VectorInit(c->nextCatalogEntries);

	while(1)
	{
	XBit bit;
	bit = ReadXBit(p);
	switch(bit->type)
	{
	case XBIT_eof:
		Free(catalog_uri);
		CloseXMLDocument(p);
		return c;
	case XBIT_error:
		Free(catalog_uri);
		ParserPerror(p, bit);
		FreeXBit(bit);
		FreeCatalogEntryFile(c);
		CloseXMLDocument(p);
		return catalog_resource_error;
	case XBIT_start:
	case XBIT_empty:
		if(DoElement(bit, p, c,
			 EntityBaseURL(p->document_entity),
			 PR_unspecified) == -1)
		{
		c = (p->state == PS_error) ? catalog_resource_error : 0;
		Free(catalog_uri);
		FreeCatalogEntryFile(c);
		CloseXMLDocument(p);
		return c;
		}
		break;
	default:
		FreeXBit(bit);
		break;
	}
	}
}

struct entry_info
{
	char *name;
	CatalogEntryType type;
	char *match_attr;
	char *value_attr;
	enum {norm_public, norm_system, norm_prefer, norm_none} norm_match;
} elements[] =
{
	{"public", CN_public, "publicId", "uri", norm_public,},
	{"system", CN_system, "systemId", "uri", norm_system},
	{"rewriteSystem", CN_rewriteSystem, "systemIdStartString", "rewritePrefix", norm_system},
	{"delegatePublic", CN_delegatePublic, "publicIdStartString", "catalog", norm_public},
	{"delegateSystem", CN_delegateSystem, "systemIdStartString", "catalog", norm_system},
	{"uri", CN_uri, "name", "uri", norm_system},
	{"rewriteURI", CN_rewriteURI, "uriStartString", "rewritePrefix", norm_system},
	{"delegateURI", CN_delegateURI, "uriStartString", "catalog", norm_system},
	{"nextCatalog", CN_nextCatalog, 0, "catalog", norm_none},
	{"catalog", CN_catalog, "prefer", 0, norm_prefer},
	{"group", CN_group, "prefer", 0, norm_prefer},
};
int nelements = sizeof(elements) / sizeof(elements[0]);

static int DoElement(XBit bit, Parser p, CatalogEntryFile c,
			 const char *base_uri, Prefer prefer)
{
	Char *ns;
	char *s;
	const Char *local;
	CatalogEntry entry;
	Attribute attr;
	char *new_base_uri = 0;
	char *match = 0, *value = 0;
	struct entry_info *info = 0; /* init only to please gcc */
	int i;

	if(bit->ns_element_definition == 0)
	/* non-namespaced element */
	return SkipElement(bit, p);

	ns = bit->ns_element_definition->namespace->nsname;
	if(strcmpC8(ns, "urn:oasis:names:tc:entity:xmlns:xml:catalog") != 0)
	/* wrong-namespace element */
	return SkipElement(bit, p);

	local = bit->ns_element_definition->name;

	for(i=0; i<nelements; i++)
	{
	info = &elements[i];
	if(strcmpC8(local, info->name) == 0)
		break;
	}

	if(i == nelements)
	{
	Fprintf(Stderr,
		"catalog warning: unknown element %S ignored\n", local);
	return SkipElement(bit, p);
	}

	for(attr = bit->attributes; attr; attr = attr->next)
	{
	NSAttributeDefinition nsa = attr->ns_definition;

	if(!nsa->element &&
	   strcmpC8(nsa->namespace->nsname,
			"http://www.w3.org/XML/1998/namespace") == 0 &&
	   strcmpC8(nsa->name, "base") == 0)
	{
		if(!(s = NormalizeSystem(attr->value)))
		   goto bad;
		if(!(new_base_uri = url_merge(s, base_uri, 0, 0, 0, 0)))
		goto bad;
		base_uri = new_base_uri;
		continue;
	}

	if(!nsa->element)
		continue;		/* not interested in namespaced attributes */

	if(strcmpC8(nsa->name, "id") == 0)
		continue;		/* not interested in ids */

	if(info->match_attr && strcmpC8(nsa->name, info->match_attr) == 0)
	{
		switch(info->norm_match)
		{
		case norm_public:
		match = NormalizePublic(attr->value);
		break;
		case norm_system:
		match = NormalizeSystem(attr->value);
		break;
		case norm_prefer:
		if(strcmpC8(attr->value, "system") == 0)
			prefer = PR_system;
		else if(strcmpC8(attr->value, "public") == 0)
			prefer = PR_public;
		else
		{
			Fprintf(Stderr,
				"catalog error: bad value \"%S\" for prefer attribute",
				attr->value);
			goto bad;
		}
		break;
		case norm_none:
		break;
		}
	}
	else if(info->value_attr && strcmpC8(nsa->name, info->value_attr) == 0)
	{
		if(!(s = NormalizeSystem(attr->value)))
		   goto bad;
		if(!(value = url_merge(s, base_uri, 0, 0, 0, 0)))
		goto bad;
	}
	else
	{
		Fprintf(Stderr,
			"catalog error: unknown attribute %S on element %S\n",
			nsa->name, local);
		goto bad;
	}
	}

	if(info->type == CN_catalog || info->type == CN_group)
	{
	int empty = (bit->type == XBIT_empty);

	FreeXBit(bit);

	if(empty)
		goto good;

	while(1)
	{
		bit = ReadXBit(p);
		switch(bit->type)
		{
		case XBIT_error:
		ParserPerror(p, bit);
		goto bad;
		case XBIT_start:
		case XBIT_empty:
		if(DoElement(bit, p, c, base_uri, prefer) == -1)
			goto bad;
		break;
		case XBIT_end:
		case XBIT_eof:
		goto good;
		default:
		FreeXBit(bit);
		break;
		}
	}
	goto good;
	}

	if(info->type == CN_nextCatalog)
	{
	if(!value)
	{
		Fprintf(Stderr,
	  "catalog error: missing catalog attribute for nextCatalog element\n");
		goto bad;
	}
	if(!VectorPush(c->nextCatalogEntries, value))
		goto bad;
	goto good;
	}

	if(!match)
	{
	Fprintf(Stderr, "catalog error: missing %s attribute for %S element\n",
		info->match_attr, local);
	goto bad;
	}

	if(!value)
	{
	Fprintf(Stderr, "catalog error: missing %s attribute for %S element\n",
		info->value_attr, local);
	goto bad;
	}

	if(!(entry = NewCatalogEntry(match, value, prefer)))
	goto bad;

	switch(info->type)
	{
	case CN_public:
	i = VectorPush(c->publicEntries, entry);
	break;
	case CN_system:
	i = VectorPush(c->systemEntries, entry);
	break;
	case CN_rewriteSystem:
	i = VectorPush(c->rewriteSystemEntries, entry);
	break;
	case CN_delegatePublic:
	i = VectorPush(c->delegatePublicEntries, entry);
	break;
	case CN_delegateSystem:
	i = VectorPush(c->delegateSystemEntries, entry);
	break;
	case CN_uri:
	i = VectorPush(c->uriEntries, entry);
	break;
	case CN_rewriteURI:
	i = VectorPush(c->rewriteURIEntries, entry);
	break;
	case CN_delegateURI:
	i = VectorPush(c->delegateURIEntries, entry);
	break;
	default:
	i = 0;
	break;
	}

	if(!i)
	{
	Free(entry);
	goto bad;
	}

 good:
	Free(new_base_uri);
	return 0;

 bad:
	FreeXBit(bit);
	Free(new_base_uri);
	Free(match);
	Free(value);
	return -1;
}

static Parser OpenXMLDocument(char *uri)
{
	Entity ent;
	InputSource source;
	Parser p;

	if(!(ent = NewExternalEntity(0, 0, uri, 0, 0)) ||
	   !(source = EntityOpen(ent)) ||
	   !(p = NewParser()))
	return 0;

	ParserSetFlag(p, XMLNamespaces, 1);
	ParserSetFlag(p, ReturnDefaultedAttributes, 1);
	ParserSetFlag(p, ErrorOnBadCharacterEntities, 1);
	ParserSetFlag(p, ErrorOnUndefinedEntities, 1);
	ParserSetFlag(p, XMLStrictWFErrors, 1);
	ParserSetFlag(p, WarnOnRedefinitions, 0);

	ParserSetEntityOpener(p, special_opener);

	/* for thread safety */
	p->dtd->namespace_universe = NewNamespaceUniverse();

	if(ParserPush(p, source) == -1)
	{
	ParserPerror(p, &p->xbit);
	return 0;
	}

	return p;
}

#include "catalog_dtd.c"

static InputSource special_opener(Entity ent, void *arg)
{
	if((ent->publicid && strcmp(ent->publicid, xml_catalog_public_id) == 0) ||
	   (ent->systemid && strcmp(ent->systemid, xml_catalog_system_id) == 0))
	{
	FILE16 *f16;
#if 0
	fprintf(stderr, "using built-in DTD for XML catalog\n");
#endif
	f16 = MakeFILE16FromString(xml_catalog_dtd,
				   sizeof(xml_catalog_dtd) - 1, "r");
	SetFileEncoding(f16, CE_ISO_8859_1);
	return NewInputSource(ent, f16);
	}

	return EntityOpen(ent);
}

static void CloseXMLDocument(Parser p)
{
	Entity ent = p->document_entity;
	FreeNamespaceUniverse(p->dtd->namespace_universe);
	FreeDtd(p->dtd);
	FreeParser(p);
	FreeEntity(ent);
}

static int SkipElement(XBit bit, Parser p)
{
	int empty = (bit->type == XBIT_empty);
	int depth = 1;

	FreeXBit(bit);

	if(empty)
	return 0;

	while(depth > 0)
	{
	bit = ReadXBit(p);
	switch(bit->type)
	{
	case XBIT_error:
		ParserPerror(p, bit);
		FreeXBit(bit);
		return -1;
	case XBIT_empty:
		break;
	case XBIT_start:
		depth++;
		break;
	case XBIT_end:
		depth--;
		break;
	default:
		break;
	}
	FreeXBit(bit);
	}

	return 0;
}
/*
 * Create a new catalog from a space-separated list of catalog file URIs.
 * (Why not colon-separated?  Because colons appear in URIs!)
 * XXX allow tab, linefeed etc.
 */

Catalog NewCatalog(char *path)
{
	Catalog catalog;
	char *p, *next;
	char *nfile, *uri;

	if(!(catalog = Malloc(sizeof *catalog)))
	return 0;

	VectorInit(catalog->path);
	VectorInit(catalog->cache);

	path = strdup8(path);	/* so we can modify it */
	if(!path)
	return 0;

	for(p = path; *p; p = next)
	{
	next = strchr(p, ' ');
	if(next)
	{
		*next++ = 0;
		while(*next == ' ')	/* allow multiple spaces */
		next++;
	}
	else
		next = p + strlen(p);

	if(!(nfile = NormalizeSystem8(p)))
		return 0;

	uri = url_merge(nfile, 0, 0, 0, 0, 0);
	Free(nfile);
	if(!uri)
	{
		Free(path);
		FreeCatalog(catalog);
		return 0;
	}

	if(!VectorPush(catalog->path, uri))
		return 0;
	}

	return catalog;
}

void FreeCatalog(Catalog catalog)
{
	int i;

	if(!catalog)
	return;

	for(i=0; i<VectorCount(catalog->path); i++)
	Free(catalog->path[i]);
	Free(catalog->path);

	for(i=0; i<VectorCount(catalog->cache); i++)
	{
	Free(catalog->cache[i]->uri);
	FreeCatalogEntryFile(catalog->cache[i]->cef);
	Free(catalog->cache[i]);
	}
	Free(catalog->cache);

	Free(catalog);
}

/* NB not thread safe! */
CatalogEntryFile GetCatalogEntryFile(Catalog catalog, char *catalog_uri)
{
	int i;
	CatalogEntryFile cef = 0;
	CachedEntryFile c = 0;

	for(i=0; i<VectorCount(catalog->cache); i++)
	if(strcmp(catalog->cache[i]->uri, catalog_uri) == 0)
		return catalog->cache[i]->cef;

	cef = ReadCatalogEntryFile(catalog_uri);
	if(!cef)
	return 0;
	if(!(c = Malloc(sizeof(*c))))
	return 0;
	if(!(c->uri = strdup8(catalog_uri)))
	return 0;
	c->cef = cef;
	if(!VectorPush(catalog->cache, c))
	return 0;

	return cef;
}
