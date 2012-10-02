#ifndef NAMESPACES_H
#define NAMESPACES_H

#include "charset.h"
#include "rxputil.h"

#ifdef __cplusplus
/* unfortunately the word "namespace" is reserved in C++ */
#define RXP_NAMESPACE name_space
#else
#define RXP_NAMESPACE namespace
#endif

typedef struct namespace_universe *NamespaceUniverse;
typedef struct RXP_NAMESPACE *Namespace;
typedef struct ns_element_definition *NSElementDefinition;
typedef struct ns_attribute_definition *NSAttributeDefinition;

struct namespace_universe {
	Vector(Namespace, namespaces);
};

struct RXP_NAMESPACE {
	Char *nsname;
	NamespaceUniverse universe;
	Vector(NSElementDefinition, elements);
	Vector(NSAttributeDefinition, attributes);
	int nsnum;
};

struct ns_element_definition {
	const Char *name;
	Namespace RXP_NAMESPACE;
	Vector(NSAttributeDefinition, attributes);
	int eltnum;
};

struct ns_attribute_definition {
	Namespace RXP_NAMESPACE;
	NSElementDefinition element;
	const Char *name;
	int attrnum;
};

XML_API int init_namespaces(void);
XML_API void deinit_namespaces(void);
XML_API int reinit_namespaces(void);

XML_API NamespaceUniverse NewNamespaceUniverse(void);
XML_API Namespace NewNamespace(NamespaceUniverse universe, const Char *nsname);
XML_API void FreeNamespaceUniverse(NamespaceUniverse universe);

XML_API NSElementDefinition DefineNSElement(Namespace ns, const Char *name);
XML_API NSAttributeDefinition
	DefineNSGlobalAttribute(Namespace ns, const Char *name);
XML_API NSAttributeDefinition
	 DefineNSElementAttribute(NSElementDefinition element, const Char *name);

XML_API Namespace
	FindNamespace(NamespaceUniverse universe, const Char *nsname, int create);
XML_API NSElementDefinition
	FindNSElementDefinition(Namespace ns, const Char *name, int create);
XML_API NSAttributeDefinition
	FindNSGlobalAttributeDefinition(Namespace ns,
					const Char *name, int create);
XML_API NSAttributeDefinition
	FindNSElementAttributeDefinition(NSElementDefinition element,
					 const Char *name, int create);

XML_API Namespace
	NextNamespace(NamespaceUniverse universe, Namespace previous);
XML_API NSElementDefinition
	NextNSElementDefinition(Namespace ns, NSElementDefinition previous);
XML_API NSAttributeDefinition
	NextNSGlobalAttributeDefinition(Namespace ns,
					NSAttributeDefinition previous);
XML_API NSAttributeDefinition
	NextNSElementAttributeDefinition(NSElementDefinition element,
					 NSAttributeDefinition previous);

#endif /* NAMESPACES_H */
