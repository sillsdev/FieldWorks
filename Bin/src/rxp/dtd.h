#ifndef DTD_H
#define DTD_H

#include "charset.h"
#include "rxputil.h"
#include "namespaces.h"

/* Typedefs */

typedef struct dtd *Dtd;

typedef struct entity *Entity;

typedef struct element_definition *ElementDefinition;
typedef struct content_particle *ContentParticle;
typedef struct fsm *FSM;
typedef struct fsm_edge *FSMEdge;
typedef struct fsm_node *FSMNode;

typedef struct attribute_definition *AttributeDefinition;

typedef struct notation_definition *NotationDefinition;

/* Entities */

enum entity_type {ET_external, ET_internal};
typedef enum entity_type EntityType;

enum markup_language {ML_xml, ML_nsl, ML_unspecified};
typedef enum markup_language MarkupLanguage;

enum standalone_declaration {
	/* NB must match NSL's rmdCode */
	SDD_unspecified, SDD_no, SDD_yes, SDD_enum_count
};
typedef enum standalone_declaration StandaloneDeclaration;

extern const char8 *StandaloneDeclarationName[SDD_enum_count];

enum xml_version {
	XV_unknown = 0,		/* an unrecognised version */
	XV_1_0     = 100000,	/* 1.0 (including no version specified) */
	XV_1_1     = 100100		/* 1.1 */
};
typedef enum xml_version XMLVersion;

struct entity {
	/* All entities */

	const Char *name;		/* The name in the entity declaration */
	EntityType type;		/* ET_external or ET_internal */
	const char8 *base_url;	/* If different from expected */
	struct entity *next;	/* For chaining a document's entity defns */
	CharacterEncoding encoding;	/* The character encoding of the entity */
	Entity parent;		/* The entity in which it is defined */
	const char8 *url;		/* URL of entity */
	int is_externally_declared;	/* True if declared outside document entity */
	int is_internal_subset;	/* True if this is the internal subset */

	/* Internal entities */

	const Char *text;		/* Text of the entity */
	int line_offset;		/* Line offset of definition */
	int line1_char_offset;	/* Char offset on first line */
	int matches_parent_text;	/* False if might contain expanded PEs */

	/* External entities */

	const char8 *systemid;	/* Declared public ID */
	const char8 *publicid;	/* Declared public ID */
	NotationDefinition notation; /* Binary entity's declared notation */
	MarkupLanguage ml_decl;	/* XML, NSL or not specified */
	const char8 *version_decl;	/* XML declarations found in entity, if any  */
	CharacterEncoding encoding_decl;
	StandaloneDeclaration standalone_decl;
	const char8 *ddb_filename;	/* filename in NSL declaration */
	XMLVersion xml_version;
};

/* Elements */

#ifndef FOR_LT			/* This is also declared in nsl.h */

enum cp_type {
	CP_pcdata, CP_name, CP_seq, CP_choice, CP_enum_count
};
typedef enum cp_type CPType;

struct content_particle {
	enum cp_type type;
	char repetition;
	const Char *name;
	ElementDefinition element;
	int nchildren;
	struct content_particle **children;
};

#endif

extern XML_API const char8 *ContentParticleTypeName[CP_enum_count];


struct fsm {
	Vector(FSMNode, nodes);
	FSMNode start_node;
};

struct fsm_edge {
	void *label;
	struct fsm_node *source, *destination;
	int id;
};

struct fsm_node {
	FSM fsm;
	int mark;
	int end_node;
	int id;
	Vector(FSMEdge, edges);
};

enum content_type {
	/* NB this must match NSL's ctVals */
	CT_mixed, CT_any, CT_bogus1, CT_bogus2, CT_empty, CT_element, CT_enum_count
};
typedef enum content_type ContentType;

extern XML_API const char8 *ContentTypeName[CT_enum_count];

struct element_definition {
#ifdef FOR_LT
	NSL_Doctype_I *doctype;
	NSL_ElementSummary_I *eltsum;
#endif
	const Char *name;		/* The element name */
	int namelen;
	int tentative;
	ContentType type;		/* The declared content */
	Char *content;		/* Element content */
	struct content_particle *particle;
	int declared;		/* Was there a declaration for this? */
	int has_attlist;		/* Were there any ATTLIST declarations? */
	FSM fsm;
	AttributeDefinition *attributes;
	int nattributes, nattralloc;
	AttributeDefinition id_attribute; /* ID attribute, if it has one */
	AttributeDefinition xml_space_attribute; /* xml:space attribute, if it has one */
	AttributeDefinition xml_lang_attribute; /* xml:lang attribute, if it has one */
	AttributeDefinition xml_id_attribute; /* xml:id attribute, if it has one */
	AttributeDefinition notation_attribute; /* NOTATION attribute, if it has one */
	NSElementDefinition cached_nsdef;
	const Char *prefix, *local;
	int is_externally_declared;	/* True if declared outside document entity */
	int eltnum;
};

/* Attributes */

enum default_type {
	/* NB this must match NSL's NSL_ADefType */
	DT_required, DT_bogus1, DT_implied,
	DT_bogus2, DT_none, DT_fixed, DT_enum_count
};
typedef enum default_type DefaultType;

extern XML_API const char8 *DefaultTypeName[DT_enum_count];

enum attribute_type {
	/* NB this must match NSL's NSL_Attr_Dec_Value */
	AT_cdata, AT_bogus1, AT_bogus2, AT_nmtoken, AT_bogus3, AT_entity,
	AT_idref, AT_bogus4, AT_bogus5, AT_nmtokens, AT_bogus6, AT_entities,
	AT_idrefs, AT_id, AT_notation, AT_enumeration, AT_enum_count
};
typedef enum attribute_type AttributeType;

extern XML_API const char8 *AttributeTypeName[AT_enum_count];

struct attribute_definition {
#ifdef FOR_LT
	NSL_AttributeSummary attrsum;
#endif
	const Char *name;		/* The attribute name */
	int namelen;
	AttributeType type;		/* The declared type */
	Char **allowed_values;	/* List of allowed values, argv style */
	DefaultType default_type;	/* The type of the declared default */
	const Char *default_value;	/* The declared default value */
	int declared;		/* Was there a declaration for this? */
	const Char *ns_attr_prefix;	/* Prefix it defines if a namespace attr */
	NSAttributeDefinition cached_nsdef;
	const Char *prefix, *local;
	int is_externally_declared;	/* True if declared outside document entity */
	int attrnum;
};

/* Notations */

struct notation_definition {
	const Char *name;		/* The notation name */
	int tentative;
	const char8 *systemid;	/* System identifier */
	const char8 *publicid;	/* Public identifier */
	const char8 *url;
	Entity parent;		/* Entity that declares it (for base URL) */
	struct notation_definition *next;
};

/* DTDs */

struct dtd {
	const Char *name;		/* The doctype name */
	Entity internal_part, external_part;
	Entity entities;
	Entity parameter_entities;
	Entity predefined_entities;
#ifdef FOR_LT
	NSL_Doctype_I *doctype;
	struct element_definition fake_elt;
#endif
	ElementDefinition *elements;
	int nelements, neltalloc;
	NotationDefinition notations;
	NamespaceUniverse namespace_universe;
};

/* Public functions */

XML_API Dtd NewDtd(void);
XML_API void FreeDtd(Dtd dtd);

XML_API Entity NewExternalEntity(const Char *name,
			  const char8 *publicid, const char8 *systemid,
			  NotationDefinition notation,
			  Entity parent);
XML_API Entity NewExternalEntityN(const Char *name, int namelen,
			  const char8 *publicid, const char8 *systemid,
			  NotationDefinition notation,
			  Entity parent);
XML_API Entity NewInternalEntityN(const Char *name, int namelen,
			  const Char *text, Entity parent,
			  int line_offset, int line1_char_offset,
			  int matches_parent_text);
XML_API void FreeEntity(Entity e);

XML_API const char8 *EntityURL(Entity e);
XML_API const char8 *EntityDescription(Entity e);
XML_API void EntitySetBaseURL(Entity e, const char8 *url);
XML_API const char8 *EntityBaseURL(Entity e);

XML_API Entity DefineEntity(Dtd dtd, Entity entity, int pe);
XML_API Entity FindEntityN(Dtd dtd, const Char *name, int namelen, int pe);
XML_API Entity NextEntity(Dtd dtd, Entity previous);
XML_API Entity NextParameterEntity(Dtd dtd, Entity previous);

#define NewInternalEntity(name, test, parent, l, l1, mat) \
	NewInternalEntityN(name, name ? Strlen(name) : 0, test, parent, l, l1, mat)
#define FindEntity(dtd, name, pe) FindEntityN(dtd, name, Strlen(name), pe)

XML_API ElementDefinition DefineElementN(Dtd dtd, const Char *name, int namelen,
				 ContentType type, Char *content,
					 ContentParticle particle,
					 int declared);
XML_API ElementDefinition TentativelyDefineElementN(Dtd dtd,
						const Char *name, int namelen);
XML_API ElementDefinition RedefineElement(ElementDefinition e, ContentType type,
				  Char *content, ContentParticle particle,
					  int declared);
XML_API ElementDefinition FindElementN(Dtd dtd, const Char *name, int namelen);
XML_API ElementDefinition NextElementDefinition(Dtd dtd, ElementDefinition previous);
XML_API void FreeElementDefinition(ElementDefinition e);
XML_API void FreeContentParticle(ContentParticle cp);

#define DefineElement(dtd, name, type, content, particle, decl) \
	DefineElementN(dtd, name, Strlen(name), type, content, particle, decl)
#define TentativelyDefineElement(dtd, name) \
	TentativelyDefineElementN(dtd, name, Strlen(name))
#define FindElement(dtd, name) FindElementN(dtd, name, Strlen(name))

XML_API AttributeDefinition DefineAttributeN(ElementDefinition element,
					 const Char *name, int namelen,
					 AttributeType type, Char **allowed_values,
					 DefaultType default_type,
					 const Char *default_value,
						 int declared);
XML_API AttributeDefinition FindAttributeN(ElementDefinition element,
				   const Char *name, int namelen);
XML_API AttributeDefinition NextAttributeDefinition(ElementDefinition element,
						AttributeDefinition previous);
XML_API void FreeAttributeDefinition(AttributeDefinition a);

#define DefineAttribute(element, name, type, all, dt, dv, decl) \
	DefineAttributeN(element, name, Strlen(name), type, all, dt, dv, decl)
#define FindAttribute(element, name) \
	FindAttributeN(element, name, Strlen(name))

XML_API NotationDefinition DefineNotationN(Dtd dtd, const Char *name, int namelen,
				 const char8 *publicid, const char8 *systemid,
					   Entity parent);
XML_API NotationDefinition TentativelyDefineNotationN(Dtd dtd,
						  const Char *name, int namelen);
XML_API NotationDefinition RedefineNotation(NotationDefinition n,
				  const char8 *publicid, const char8 *systemid,
						Entity parent);
XML_API NotationDefinition FindNotationN(Dtd dtd, const Char *name, int namelen);
XML_API NotationDefinition NextNotationDefinition(Dtd dtd, NotationDefinition previous);
XML_API const char8 *NotationURL(NotationDefinition n);
XML_API void FreeNotationDefinition(NotationDefinition n);

#define DefineNotation(dtd, name, pub, sys, par) \
	DefineNotationN(dtd, name, Strlen(name), pub, sys, par)
#define TentativelyDefineNotation(dtd, name) \
	TentativelyDefineNotationN(dtd, name, Strlen(name))
#define FindNotation(dtd, name) FindNotationN(dtd, name, Strlen(name))

NSElementDefinition NamespacifyElementDefinition(ElementDefinition element,
						 Namespace ns);
NSAttributeDefinition
	NamespacifyGlobalAttributeDefinition(AttributeDefinition attribute,
					 Namespace ns);
NSAttributeDefinition
	NamespacifyElementAttributeDefinition(AttributeDefinition attribute,
					  NSElementDefinition nselt);

#endif /* DTD_H */
