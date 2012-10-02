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
#include "namespaces.h"
#include "rxputil.h"

static void FreeNamespace(Namespace ns);
static void FreeNSElementDefinition(NSElementDefinition element);
static void FreeNSAttributeDefinition(NSAttributeDefinition attribute);

NamespaceUniverse global_universe = 0;

int init_namespaces(void)
{
	if(global_universe)
	return 0;

	if(!(global_universe = NewNamespaceUniverse()))
	return -1;

	return 0;
}

void deinit_namespaces(void)
{
	if(global_universe)
	FreeNamespaceUniverse(global_universe);
	global_universe = 0;
}

int reinit_namespaces(void)
{
	deinit_namespaces();
	return init_namespaces();
}

NamespaceUniverse NewNamespaceUniverse(void)
{
	NamespaceUniverse u;

	if(!(u = Malloc(sizeof(*global_universe))))
	return 0;
	VectorInit(u->namespaces);

	return u;
}

void FreeNamespaceUniverse(NamespaceUniverse universe)
{
	int i;

	if(!universe)
	universe = global_universe;

	for(i=VectorCount(universe->namespaces)-1; i>=0; --i)
	FreeNamespace(universe->namespaces[i]);

	Free(universe->namespaces);
	Free(universe);
}

Namespace NewNamespace(NamespaceUniverse universe, const Char *nsname)
{
	Namespace ns;

	if(!universe)
	universe = global_universe;

	if(!(ns = Malloc(sizeof(*ns))))
	return 0;
	if(!(ns->nsname = Strdup(nsname)))
	return 0;
	ns->nsnum = VectorCount(universe->namespaces);
	if(!VectorPush(universe->namespaces, ns))
	return 0;
	ns->universe = universe;
	VectorInit(ns->elements);
	VectorInit(ns->attributes);

	return ns;
}

static void FreeNamespace(Namespace ns)
{
	int i;

	for(i=VectorCount(ns->elements)-1; i>=0; --i)
	FreeNSElementDefinition(ns->elements[i]);

	for(i=VectorCount(ns->attributes)-1; i>=0; --i)
	FreeNSAttributeDefinition(ns->attributes[i]);

	Free(ns->nsname);
	Free(ns->elements);
	Free(ns->attributes);
	Free(ns);
}

NSElementDefinition DefineNSElement(Namespace ns, const Char *name)
{
	NSElementDefinition e;

	if(!(e = Malloc(sizeof(*e))))
	return 0;
	if(!(e->name = Strdup(name)))
	return 0;
	e->eltnum = VectorCount(ns->elements);
	if(!VectorPush(ns->elements, e))
	return 0;
	e->namespace = ns;
	VectorInit(e->attributes);

	return e;
}

static void FreeNSElementDefinition(NSElementDefinition element)
{
	int i;


	for(i=VectorCount(element->attributes)-1; i>=0; --i)
	FreeNSAttributeDefinition(element->attributes[i]);

	Free(element->attributes);
	Free((Char *)element->name); /* cast to get rid of const */
	Free(element);
}

NSAttributeDefinition
	DefineNSGlobalAttribute(Namespace ns, const Char *name)
{
	NSAttributeDefinition a;

	if(!(a = Malloc(sizeof(*a))))
	return 0;
	if(!(a->name = Strdup(name)))
	return 0;
	a->attrnum = VectorCount(ns->attributes);
	if(!VectorPush(ns->attributes, a))
	return 0;
	a->namespace = ns;
	a->element = 0;

	return a;
}

NSAttributeDefinition
	 DefineNSElementAttribute(NSElementDefinition element, const Char *name)
{
	NSAttributeDefinition a;
	Namespace ns = element->namespace;

	if(!(a = Malloc(sizeof(*a))))
	return 0;
	if(!(a->name = Strdup(name)))
	return 0;
	a->attrnum = VectorCount(element->attributes);
	if(!VectorPush(element->attributes, a))
	return 0;
	a->namespace = ns;
	a->element = element;

	return a;
}

static void FreeNSAttributeDefinition(NSAttributeDefinition attribute)
{
	Free((Char *)attribute->name); /* cast to get rid of const */
	Free(attribute);
}

Namespace
	FindNamespace(NamespaceUniverse universe, const Char *nsname, int create)
{
	int i;

	if(!universe)
	universe = global_universe;

	for(i=VectorCount(universe->namespaces)-1; i>=0; --i)
	if(Strcmp(nsname, universe->namespaces[i]->nsname) == 0)
		return universe->namespaces[i];

	if(create)
	return NewNamespace(universe, nsname);

	return 0;
}

NSElementDefinition
	FindNSElementDefinition(Namespace ns, const Char *name, int create)
{
	int i;

	for(i=VectorCount(ns->elements)-1; i>=0; --i)
	if(Strcmp(name, ns->elements[i]->name) == 0)
		return ns->elements[i];

	if(create)
	return DefineNSElement(ns, name);

	return 0;
}

NSAttributeDefinition
	FindNSGlobalAttributeDefinition(Namespace ns, const Char *name, int create)
{
	int i;

	for(i=VectorCount(ns->attributes)-1; i>=0; --i)
	if(Strcmp(name, ns->attributes[i]->name) == 0)
		return ns->attributes[i];

	if(create)
	return DefineNSGlobalAttribute(ns, name);

	return 0;
}

NSAttributeDefinition
	FindNSElementAttributeDefinition(NSElementDefinition element,
					 const Char *name, int create)
{
	int i;

	for(i=VectorCount(element->attributes)-1; i>=0; --i)
	if(Strcmp(name, element->attributes[i]->name) == 0)
		return element->attributes[i];

	if(create)
	return DefineNSElementAttribute(element, name);

	return 0;
}

Namespace
	NextNamespace(NamespaceUniverse universe, Namespace previous)
{
	int next;

	if(!universe)
	universe = global_universe;
	next = previous ? previous->nsnum + 1 : 0;
	return next < VectorCount(universe->namespaces) ?
			universe->namespaces[next] : 0;
}

NSElementDefinition
	NextNSElementDefinition(Namespace ns, NSElementDefinition previous)
{
	int next = previous ? previous->eltnum + 1: 0;

	return next < VectorCount(ns->elements) ? ns->elements[next] : 0;
}

NSAttributeDefinition
	NextNSGlobalAttributeDefinition(Namespace ns,
					NSAttributeDefinition previous)
{
	int next = previous ? previous->attrnum + 1: 0;

	return next < VectorCount(ns->attributes) ? ns->attributes[next] : 0;
}

NSAttributeDefinition
	NextNSElementAttributeDefinition(NSElementDefinition element,
					 NSAttributeDefinition previous)
{
	int next = previous ? previous->attrnum + 1: 0;

	return next < VectorCount(element->attributes) ? element->attributes[next] : 0;
}
