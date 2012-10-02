#include "catalog.h"

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "xmlparser.h"
#include "input.h"
#include "dtd.h"

InputSource catalog_entity_open(Entity ent, void *arg)
{
	Catalog catalog = arg;

	if(!ent->url)
	ent->url = ResolveExternalIdentifier(catalog,
						 ent->publicid, ent->systemid,
						 catalog->default_prefer);

#if 0
	fprintf(stderr, "catalog: %s %s -> %s\n",
		ent->publicid ? ent->publicid : "(no pubid)",
		ent->systemid ? ent->systemid : "(no sysid)",
		EntityURL(ent));
#endif

	return EntityOpen(ent);
}

void CatalogEnable(Parser p)
{
	char *path, *prefer;
	Catalog catalog;

	if(!(path = getenv("XML_CATALOG_FILES")))
	return;

	if(getenv("XML_CATALOG_DEBUG"))
	catalog_debug = 1;

	catalog = NewCatalog(path);
	if(!catalog)
	return;

	catalog->default_prefer = PR_system;
	if((prefer = getenv("XML_CATALOG_PREFER")))
	{
	if(strcmp(prefer, "system") == 0)
		catalog->default_prefer = PR_system;
	else if(strcmp(prefer, "public") == 0)
		catalog->default_prefer = PR_public;
	else
		fprintf(stderr, "bad XML_CATALOG_PREFER value \"%s\" ignored\n",
			prefer);
	}

	ParserSetEntityOpener(p, catalog_entity_open);
	ParserSetEntityOpenerArg(p, catalog);
}
