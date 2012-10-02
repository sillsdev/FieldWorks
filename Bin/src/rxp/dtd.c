#include <string.h>

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
#include "dtd.h"
#include "url.h"

extern void FreeFSM(FSM fsm);	/* XXX should be in header */

const char8 *DefaultTypeName[DT_enum_count] = {
	"#REQUIRED",
	"bogus1",
	"#IMPLIED",
	"bogus2",
	"NONE",
	"#FIXED",
};

const char8 *ContentParticleTypeName[CP_enum_count] = {
	"#PCDATA",
	"NAME",
	"SEQUENCE",
	"CHOICE"
};

const char8 *ContentTypeName[CT_enum_count] = {
	"MIXED",
	"ANY",
	"bogus1",
	"bogus2",
	"EMPTY",
	"ELEMENT"
};

const char8 *AttributeTypeName[AT_enum_count] = {
	"CDATA",
	"bogus1",
	"bogus2",
	"NMTOKEN",
	"bogus3",
	"ENTITY",
	"IDREF",
	"bogus4",
	"bogus5",
	"NMTOKENS",
	"bogus6",
	"ENTITIES",
	"IDREFS",
	"ID",
	"NOTATION",
	"ENUMERATION"
};

const char8 *StandaloneDeclarationName[SDD_enum_count] = {
	"unspecified", "no", "yes"
};

static Char *Strndup(const Char *old, int len)
{
	Char *new = Malloc((len+1) * sizeof(Char));

	if(!new)
	return 0;

	memcpy(new, old, len * sizeof(Char));
	new[len] = 0;

	return new;
}

/* DTDs */

Dtd NewDtd(void)
{
	Dtd d;

	if(!(d = Malloc(sizeof(*d))))
	return 0;

	d->name = 0;
	d->internal_part = 0;
	d->external_part = 0;
	d->entities = 0;
	d->parameter_entities = 0;
	d->predefined_entities = 0;
#ifdef FOR_LT
	d->doctype = 0;
#endif
	d->nelements = 0;
	d->neltalloc = 20;
	d->elements = Malloc(d->neltalloc * sizeof(ElementDefinition));
	if(!d->elements)
	return 0;
	d->notations = 0;
	d->namespace_universe = 0;	/* the global universe */

	return d;
}

/* Free a DTD and everything in it */

void FreeDtd(Dtd dtd)
{
	Entity ent, ent1;
	int i;
	NotationDefinition not, not1;

	if(!dtd)
	return;

	/* Free the name */

	Free((Char *)dtd->name);	/* cast is to get rid of const */

	/* Free the entities */

	FreeEntity(dtd->internal_part);
	FreeEntity(dtd->external_part);

	for(ent = dtd->entities; ent; ent = ent1)
	{
	ent1 = ent->next;	/* get it before we free ent! */
	FreeEntity(ent);
	}

	for(ent = dtd->parameter_entities; ent; ent = ent1)
	{
	ent1 = ent->next;
	FreeEntity(ent);
	}

	/* The predefined entities are shared, so we don't free them */

	/* Free the elements */

	for(i=0; i<dtd->nelements; i++)
	FreeElementDefinition(dtd->elements[i]); /* Frees the attributes too */

	Free(dtd->elements);

	/* Free the notations */

	for(not = dtd->notations; not; not = not1)
	{
	not1 = not->next;
	FreeNotationDefinition(not);
	}

	/* Free the dtd itself */

	Free(dtd);
}

/* Entities */

/*
 * Make a new entity.  The name and IDs are copied.
 */

Entity NewExternalEntity(const Char *name, const char8 *publicid,
			  const char8 *systemid, NotationDefinition notation,
			  Entity parent)
{
	if(systemid && !(systemid = strdup8(systemid)))
	return 0;
	if(publicid && !(publicid = strdup8(publicid)))
	return 0;
	return NewExternalEntityN(name, name ? Strlen(name) : 0, publicid,
				  systemid, notation, parent);
}

/* NB doesn't copy IDs */

Entity NewExternalEntityN(const Char *name, int namelen, const char8 *publicid,
			  const char8 *systemid, NotationDefinition notation,
			  Entity parent)
{
	Entity e;

	if(!(e = Malloc(sizeof(*e))))
	return 0;
	if(name && !(name = Strndup(name, namelen)))
		return 0;

	e->type = ET_external;
	e->name = name;
	e->base_url = 0;
	e->encoding = CE_unknown;
	e->next = 0;
	e->parent = parent;

	e->publicid = publicid;
	e->systemid = systemid;
	e->notation = notation;

	e->ml_decl = ML_unspecified;
	e->version_decl = 0;
	e->encoding_decl = CE_unknown;
	e->standalone_decl = SDD_unspecified;
	e->ddb_filename = 0;
	e->xml_version = XV_1_0;	/* 1.0 unless otherwise specified */

	e->url = 0;

	e->is_externally_declared = 0;
	e->is_internal_subset = 0;

	return (Entity)e;
}

Entity NewInternalEntityN(const Char *name, int namelen,
			  const Char *text, Entity parent,
			  int line_offset, int line1_char_offset,
			  int matches_parent_text)
{
	Entity e;

	if(!(e = Malloc(sizeof(*e))))
	return 0;
	if(name)
	if(!(name = Strndup(name, namelen)))
		return 0;

	e->type = ET_internal;
	e->name = name;
	e->base_url = 0;
	e->encoding = InternalCharacterEncoding;
	e->next = 0;
	e->parent = parent;

	e->text = text;
	e->line_offset = line_offset;
	e->line1_char_offset = line1_char_offset;
	e->matches_parent_text = matches_parent_text;

	e->url = 0;

	e->is_externally_declared = 0;
	e->is_internal_subset = 0;

	/* These should not really be accessed for an internal entity, but
	   because the NSL layer uses internal entities to implement reading
	   from strings, sometimes they are. */

	e->publicid = 0;
	e->systemid = 0;
	e->notation = 0;

	e->ml_decl = ML_unspecified;
	e->version_decl = 0;
	e->encoding_decl = CE_unknown;
	e->standalone_decl = SDD_unspecified;
	e->ddb_filename = 0;

	return (Entity)e;
}

void FreeEntity(Entity e)
{
	if(!e)
	return;

	Free((void *)e->name);	/* The casts are to get rid of the const */
	Free((void *)e->base_url);
	Free((void *)e->url);

	switch(e->type)
	{
	case ET_internal:
	Free((void *)e->text);
	break;
	case ET_external:
	Free((void *)e->systemid);
	Free((void *)e->publicid);
	Free((void *)e->version_decl);
	Free((void *)e->ddb_filename);
	break;
	}

	Free(e);
}

const char8 *EntityURL(Entity e)
{
	/* Have we already determined the URL? */

	if(e->url)
	return e->url;

	if(e->type == ET_internal)
	{
	if(e->parent)
	{
		const char8 *url = EntityURL(e->parent);
		if(url)
		e->url = strdup8(url);
	}
	}
	else
	e->url = url_merge(e->systemid,
			   e->parent ? EntityBaseURL(e->parent) : 0,
			   0, 0, 0, 0);

	return e->url;
}

/* Returns the URL of the entity if it has one, otherwise the system ID.
   It will certainly have a URL if it was successfully opened by EntityOpen.
   Intended for error messages, so never returns NULL. */

const char8 *EntityDescription(Entity e)
{
	if(e->url)
	return e->url;

	if(e->type == ET_external)
	return e->systemid;

	if(e->parent)
	return EntityDescription(e->parent);

	return "<unknown>";
}

void EntitySetBaseURL(Entity e, const char8 *url)
{
	if(e->base_url)
	Free((void *)e->base_url);
	e->base_url = strdup8(url);
}

const char8 *EntityBaseURL(Entity e)
{
	if(e->base_url)
	return e->base_url;

	if(e->type == ET_internal)
	{
	if(e->parent)
		return EntityBaseURL(e->parent);
	else
		return 0;
	}

	return EntityURL(e);
}

Entity DefineEntity(Dtd dtd, Entity e, int pe)
{
	if(pe)
	{
	e->next = dtd->parameter_entities;
	dtd->parameter_entities = e;
	}
	else
	{
	e->next = dtd->entities;
	dtd->entities = e;
	}

	return e;
}

Entity FindEntityN(Dtd dtd, const Char *name, int namelen, int pe)
{
	Entity e;

	if(!pe)
	for(e = dtd->predefined_entities; e; e = e->next)
		if(Strncmp(name, e->name, namelen) == 0 && e->name[namelen] == 0)
		return e;


	for(e = pe ? dtd->parameter_entities : dtd->entities; e; e=e->next)
	if(Strncmp(name, e->name, namelen) == 0 && e->name[namelen] == 0)
		return e;

	return 0;
}

Entity NextEntity(Dtd dtd, Entity previous)
{
	return previous ? previous->next : dtd->entities;
}

Entity NextParameterEntity(Dtd dtd, Entity previous)
{
	return previous ? previous->next : dtd->parameter_entities;
}

/* Elements */

/*
 * Define a new element.  The name is copied, the content model is not,
 * but it is freed when the element definition is freed!
 */

ElementDefinition DefineElementN(Dtd dtd, const Char *name, int namelen,
				 ContentType type, Char *content,
				 ContentParticle particle, int declared)
{
	ElementDefinition e;
	Char *t;
#ifdef FOR_LT
	RHTEntry *entry;
#endif

	if(!(e = Malloc(sizeof(*e))))
	return 0;

	e->eltnum = dtd->nelements++;
	if(e->eltnum >= dtd->neltalloc)
	{
	dtd->neltalloc *= 2;
	dtd->elements =
	   Realloc(dtd->elements, dtd->neltalloc * sizeof(ElementDefinition));
	if(!dtd->elements)
		return 0;
	}
	dtd->elements[e->eltnum] = e;

#ifdef FOR_LT
	entry = DeclareElement(dtd->doctype, name, namelen, 0, (ctVals)type);
	if (!entry)
	return 0;

	e->doctype = dtd->doctype;
	e->eltsum =
	(NSL_ElementSummary_I *)(dtd->doctype->permanentBase+entry->eval);

	*(short *)&(e->eltsum->omitEnd) = e->eltnum;

	name = (Char *)dtd->doctype->elements+entry->keyptr;
#else
	if(!(name = Strndup(name, namelen)))
	return 0;
#endif

	e->tentative = 0;
	e->name = name;
	e->namelen = namelen;
	e->type = type;
	e->content = content;
	e->particle = particle;
	e->declared = declared;
	e->has_attlist = 0;
	e->fsm = 0;
	e->nattributes = 0;
	e->nattralloc = 20;
	e->attributes = Malloc(e->nattralloc * sizeof(AttributeDefinition));
	if(!e->attributes)
	return 0;
	e->id_attribute = 0;
	e->xml_space_attribute = 0;
	e->xml_lang_attribute = 0;
	e->xml_id_attribute = 0;
	e->notation_attribute = 0;
	e->cached_nsdef = 0;
	e->is_externally_declared = 0;

	if((t = Strchr(name, ':')))
	{
	if(!(e->prefix = Strndup(name, t - name)))
		return 0;
	e->local = t+1;
	}
	else
	{
	e->local = name;
	e->prefix = 0;
	}

	return e;
}

ElementDefinition TentativelyDefineElementN(Dtd dtd,
						const Char *name, int namelen)
{
	ElementDefinition e;

	if(!(e = DefineElementN(dtd, name, namelen, CT_any, 0, 0, 1)))
	return 0;

	e->tentative = 1;

	return e;
}

ElementDefinition RedefineElement(ElementDefinition e, ContentType type,
				  Char *content, ContentParticle particle, int declared)
{
#ifdef FOR_LT
	e->eltsum->contentType = type;
#endif

	e->tentative = 0;
	e->type = type;
	e->content = content;
	e->particle = particle;
	e->declared = declared;

	return e;
}

ElementDefinition FindElementN(Dtd dtd, const Char *name, int namelen)
{
	ElementDefinition e;

#ifdef FOR_LT
	/* Derived from FindElementAndName */

	RHTEntry *entry;
	NSL_ElementSummary_I *eltsum;

	if(!dtd->doctype)
	return 0;

	entry = rsearch(name, namelen, dtd->doctype->elements);
	if(!entry)
	return 0;
	eltsum = (NSL_ElementSummary_I *)(dtd->doctype->permanentBase+entry->eval);

	if(!dtd->doctype->XMLMode)
	{
	/* NSL mode - we have to fake a structure */

	e = &dtd->fake_elt;
	e->doctype = dtd->doctype;
	e->name = (Char *)dtd->doctype->elements+entry->keyptr;
	e->eltsum = eltsum;
	e->tentative = 0;

	return e;
	}

	return dtd->elements[*(short *)&(eltsum->omitEnd)];
#else
	int i;

	for(i=dtd->nelements-1; i>=0; i--)
	{
	e = dtd->elements[i];
	if(namelen == e->namelen && *name == *e->name &&
	   memcmp(name, e->name, namelen*sizeof(Char)) == 0)
		return e;
	}

	return 0;
#endif
}

NSElementDefinition NamespacifyElementDefinition(ElementDefinition element,
						 Namespace ns)
{
	if(element->cached_nsdef && element->cached_nsdef->namespace == ns)
	return element->cached_nsdef;

	element->cached_nsdef = FindNSElementDefinition(ns, element->local, 1);
	return element->cached_nsdef;
}

ElementDefinition NextElementDefinition(Dtd dtd, ElementDefinition previous)
{
	int next = previous ? previous->eltnum+1 : 0;
	return next < dtd->nelements ? dtd->elements[next] : 0;
}

/* Free an element definition and its attribute definitions */

void FreeElementDefinition(ElementDefinition e)
{
	int i;

	if(!e)
	return;

	/* Free the attributes */

	for(i=0; i<e->nattributes; i++)
	FreeAttributeDefinition(e->attributes[i]);
	Free(e->attributes);


#ifndef FOR_LT
	/* Free the element name (kept elsewhere in NSL) */

	Free((void *)e->name);
#endif

	Free((void *)e->prefix);

	/* Free the content model */
	Free(e->content);
	FreeContentParticle(e->particle);
	FreeFSM(e->fsm);

	/* Free the ElementDefinition itself */
	Free(e);
}

/* Attributes */

/*
 * Define a new attribute.  The name is copied, the allowed values and
 * default are not, but they are freed when the attribute definition is freed!
 */

AttributeDefinition
	DefineAttributeN(ElementDefinition element, const Char *name, int namelen,
			 AttributeType type, Char **allowed_values,
			 DefaultType default_type, const Char *default_value,
			 int declared)
{
#ifdef FOR_LT
	int nav = 0;
	Char **avp;
	NSL_Doctype_I *doctype = element->doctype;
#endif
	AttributeDefinition a;
	static Char xml_space[] = {'x','m','l',':','s','p','a','c','e',0};
	static Char xml_lang[] = {'x','m','l',':','l','a','n','g',0};
	static Char xml_id[] = {'x','m','l',':','i','d',0};
	static Char xmlns[] = {'x','m','l','n','s',0};
	Char *t;

	if(!(a= Malloc(sizeof(*a))))
	return 0;

	a->attrnum = element->nattributes++;
	if(a->attrnum >= element->nattralloc)
	{
	element->nattralloc *= 2;
	element->attributes =
	   Realloc(element->attributes,
		   element->nattralloc * sizeof(AttributeDefinition));
	if(!element->attributes)
		return 0;
	}
	element->attributes[a->attrnum] = a;

#ifdef FOR_LT
	if(allowed_values)
	{
	for(avp = allowed_values; *avp; avp++)
		nav++;
	}

	if (!(name = DeclareAttr(doctype, name, namelen,
				 (NSL_Attr_Declared_Value)type,
				 allowed_values?allowed_values[0]:0, nav,
				 (NSL_ADefType)default_type, default_value,
				 &element->eltsum, element->name)))
	return 0;

	a->attrsum = FindAttrSpec(element->eltsum, doctype, name);
#else
	if(!(name = Strndup(name, namelen)))
	return 0;
#endif

	a->name = name;
	a->namelen = namelen;
	a->type = type;
	a->allowed_values = allowed_values;
	a->default_type = default_type;
	a->default_value = default_value;
	a->declared = declared;
	if(declared)
	element->has_attlist = 1;
	a->is_externally_declared = 0;

	if(a->type == AT_id && !element->id_attribute)
	element->id_attribute = a;
	else if(a->type == AT_notation && !element->notation_attribute)
	element->notation_attribute = a;

	if(Strcmp(name, xml_space) == 0)
	element->xml_space_attribute = a;
	else if(Strcmp(name, xml_lang) == 0)
	element->xml_lang_attribute = a;
	else if(Strcmp(name, xml_id) == 0)
	element->xml_id_attribute = a;

	a->cached_nsdef = 0;

	if((t = Strchr(name, ':')))
	{
	if(!(a->prefix = Strndup(name, t - name)))
		return 0;
	a->local = t+1;
	if(Strcmp(a->prefix, xmlns) == 0)
		a->ns_attr_prefix = a->local;
	else
		a->ns_attr_prefix = 0;
	}
	else
	{
	a->local = name;
	a->prefix = 0;
	if(Strcmp(name, xmlns) == 0)
		a->ns_attr_prefix = name+5; /* point to the null */
	else
		a->ns_attr_prefix = 0;
	}

	return a;
}

AttributeDefinition FindAttributeN(ElementDefinition element,
				   const Char *name, int namelen)
{
	int i;

#ifdef FOR_LT
	if(!(name = AttrUniqueName(element->doctype, name, namelen)))
	return 0;
	if(!element->doctype->XMLMode)
	/* XXX Hack for NSL documents.
	   Return the attribute summary itself.
	   Nothing in the RXP layer had better look at it! */
	return (AttributeDefinition)FindAttrSpec(element->eltsum,
						 element->doctype,
						 name);
	if(!FindAttrSpecAndNumber(element->eltsum, element->doctype, name, &i))
	return 0;
	if(i < 0)
	i = element->nattributes + i;

	return element->attributes[i];
#else
	for(i=element->nattributes-1; i>=0; i--)
	{
	AttributeDefinition a = element->attributes[i];

	if(namelen == a->namelen &&
	   memcmp(name, a->name, namelen * sizeof(Char)) == 0)
		return a;
	}

	return 0;
#endif
}

NSAttributeDefinition
	NamespacifyGlobalAttributeDefinition(AttributeDefinition attribute,
					 Namespace ns)
{
	if(attribute->cached_nsdef &&
	   !attribute->cached_nsdef->element &&
	   attribute->cached_nsdef->namespace == ns)
	return attribute->cached_nsdef;

	attribute->cached_nsdef =
	FindNSGlobalAttributeDefinition(ns, attribute->local, 1);
	return attribute->cached_nsdef;
}

NSAttributeDefinition
	NamespacifyElementAttributeDefinition(AttributeDefinition attribute,
					  NSElementDefinition nselt)
{
	if(attribute->cached_nsdef &&
	   attribute->cached_nsdef->element &&
	   attribute->cached_nsdef->element == nselt)
	return attribute->cached_nsdef;

	attribute->cached_nsdef =
	FindNSElementAttributeDefinition(nselt, attribute->local, 1);
	return attribute->cached_nsdef;
}

AttributeDefinition NextAttributeDefinition(ElementDefinition element,
						AttributeDefinition previous)
{
	int next = previous ? previous->attrnum+1 : 0;
	return next < element->nattributes ? element->attributes[next] : 0;
}

/* Free an attribute definition */

void FreeAttributeDefinition(AttributeDefinition a)
{
	if(!a)
	return;

#ifndef FOR_LT
	/* These things are stored elsewhere in NSL */

	/* Free the name */

	Free((void *)a->name);

	/* Free the allowed values - we rely on them being allocated as in
	   xmlparser.c: the Char * pointers point into one single block. */

	if(a->allowed_values)
	Free(a->allowed_values[0]);

	/* Free the default value */

	Free((void *)a->default_value);

#endif
	/* NSL doesn't ever get its hands on this */

	Free(a->allowed_values);

	Free((void *)a->prefix);

	/* Free the attribute definition itself */

	Free(a);
}

/* Notations */

/*
 * Define a new notation.  The name is copied, the system and public ids are
 * not, but they are freed when the notation definition is freed!
 */

NotationDefinition DefineNotationN(Dtd dtd, const Char *name, int namelen,
				  const char8 *publicid, const char8 *systemid,
				   Entity parent)
{
	NotationDefinition n;

	if(!(n = Malloc(sizeof(*n))) || !(name = Strndup(name, namelen)))
	return 0;

	n->name = name;
	n->tentative = 0;
	n->systemid = systemid;
	n->publicid = publicid;
	n->url = 0;
	n->parent = parent;
	n->next = dtd->notations;
	dtd->notations = n;

	return n;
}

NotationDefinition TentativelyDefineNotationN(Dtd dtd,
						  const Char *name, int namelen)
{
	NotationDefinition n;

	if(!(n = DefineNotationN(dtd, name, namelen, 0, 0, 0)))
	return 0;

	n->tentative = 1;

	return n;
}

NotationDefinition RedefineNotation(NotationDefinition n,
				  const char8 *publicid, const char8 *systemid,
					Entity parent)
{
	n->tentative = 0;
	n->systemid = systemid;
	n->publicid = publicid;
	n->parent = parent;

	return n;
}

NotationDefinition FindNotationN(Dtd dtd, const Char *name, int namelen)
{
	NotationDefinition n;

	for(n = dtd->notations; n; n = n->next)
	if(Strncmp(name, n->name, namelen) == 0 && n->name[namelen] == 0)
		return n;

	return 0;
}

NotationDefinition NextNotationDefinition(Dtd dtd, NotationDefinition previous)
{
	return previous ? previous->next : dtd->notations;
}

const char8 *NotationURL(NotationDefinition n)
{
	if(n->url)
	return n->url;

	n->url = url_merge(n->systemid,
			   n->parent ? EntityBaseURL(n->parent) : 0,
			   0, 0, 0, 0);

	return n->url;
}

void FreeNotationDefinition(NotationDefinition n)
{
	if(!n)
	return;

	/* Free the name */

	Free((void *)n->name);

	/* Free the ids */

	Free((void *)n->systemid);
	Free((void *)n->publicid);
	Free((void *)n->url);

	/* Free the notation definition itself */

	Free(n);
}
