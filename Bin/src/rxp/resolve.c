#include "catalog.h"

#include <stdlib.h>
#include <stdio.h>

#include "system.h"
#include "catutil.h"
#include "string.h"

static char *res_ext(Catalog catalog, char *file,
			 const char *public, const char *system, Prefer prefer);
static char *res_uri(Catalog catalog, char *file, const char *uri);
static int entry_compare(const void *a, const void *b);

/* used internally to indicate failure */
static char *fail = "fail";

char *ResolveExternalIdentifier(Catalog catalog,
				const char *public, const char *system,
				Prefer prefer)
{
	int i;
	char *temp;

	if(catalog_debug)
	fprintf(stderr,
		"resolving external identifier <%s> <%s> with prefer=%s\n",
		public ? public : "(null)", system ? system : "(null)",
		PreferName[prefer]);

	/* Normalize and unwrap */

	if(IsPublicidUrn(public))
	{
	if(!(temp = UnwrapPublicidUrn(public)) ||
	   !(public = NormalizePublic8(temp)))
		return 0;
	Free(temp);
	}
	else
	if(public && !(public = NormalizePublic8(public)))
		return 0;

	if(IsPublicidUrn(system))
	{
	if(!(temp = UnwrapPublicidUrn(system)) ||
	   /* NB normalize as public because that's what it will end up as! */
	   !(system = NormalizePublic8(temp)))
		return 0;
	Free(temp);

	if(public)
	{
		if(strcmp(public, system) == 0)
		{
		Free((void *)system);
		system = 0;
		}
		else
		{
		Fprintf(Stderr, "Unwrapped publicid-urn system id %s does not match public id %s, discarding\n",
			system, public);
		Free((void *)system);
		system = 0;
		}
	}
	else
	{
		public = system;
		system = 0;
	}
	}
	else
	if(system && !(system = NormalizeSystem8(system)))
		return 0;


	if(catalog_debug)
	fprintf(stderr,
		"after normalizing and unwrapping: <%s> <%s>\n",
		public ? public : "(null)", system ? system : "(null)");

	/* Search the catalogs in the path */

	for(i=0; i<VectorCount(catalog->path); i++)
	{
	char *result =
		res_ext(catalog, catalog->path[i], public, system, prefer);
	if(result == fail)
		return 0;
	if(result)
		return result;
	}

	return 0;
}

static char *res_ext(Catalog catalog, char *file,
			 const char *public, const char *system, Prefer prefer)
{
	int i;
	int len, best_length, prefix_length, nmatches;
	char *best_value = 0, *result = 0; /* init to please gcc */
	CatalogEntryFile cef;
	CatalogEntry entry;
	Vector(CatalogEntry, delegate_list);
	Prefer p;

	if(catalog_debug)
	fprintf(stderr, "looking for <%s> <%s> in %s\n",
		public ? public : "(null)",
		system ? system : "(null)",
		file);

	if(!(cef = GetCatalogEntryFile(catalog, file)))
	return fail;

	if(!system)
	goto try_public;	/* a gratuitous goto */

	/* Try for a matching system entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d system entries\n",
		VectorCount(cef->systemEntries));

	for(i=0; i<VectorCount(cef->systemEntries); i++)
	{
	entry = cef->systemEntries[i];
	if(strcmp(system, entry->match) == 0)
	{
		if(catalog_debug)
		fprintf(stderr, "matched %s, returning %s\n",
			entry->match, entry->value);
		return entry->value;
	}
	}

	/* Try for a matching rewriteSystem entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d rewriteSystem entries\n",
		VectorCount(cef->rewriteSystemEntries));

	best_length = 0;
	for(i=0; i<VectorCount(cef->rewriteSystemEntries); i++)
	{
	entry = cef->rewriteSystemEntries[i];
	len = strlen(entry->match);
	if(len > best_length &&
	   strncmp(system, entry->match, len) == 0)
	{
		best_length = len;
		best_value = entry->value;
	}
	}
	if(best_length > 0)
	{
	prefix_length = strlen(best_value);
	if(!(result = Malloc(prefix_length + strlen(system+best_length) + 1)))
		return fail;
	strcpy(result, best_value);
	strcpy(result+prefix_length, system+best_length);
	if(catalog_debug)
		fprintf(stderr, "best match %s (%d), returning %s\n",
			best_value, prefix_length, result);
	return result;
	}

	/* Try for a matching delegateSystem entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d delegateSystem entries\n",
		VectorCount(cef->delegateSystemEntries));

	VectorInit(delegate_list);
	for(i=0; i<VectorCount(cef->delegateSystemEntries); i++)
	{
	entry = cef->delegateSystemEntries[i];
	len = strlen(entry->match);
	if(strncmp(system, entry->match, len) == 0)
		if(!VectorPush(delegate_list, entry))
		return fail;
	}

	if((nmatches = VectorCount(delegate_list)) > 0)
	{
	qsort((void *)delegate_list, nmatches, sizeof(delegate_list[0]),
		  entry_compare);

	if(catalog_debug)
	{
		fprintf(stderr, "%d matches:\n", nmatches);
		for(i=0; i<nmatches; i++)
		fprintf(stderr, " %s -> %s\n",
			delegate_list[i]->match, delegate_list[i]->value);
	}

	for(i=0; i<nmatches; i++)
	{
		result =
		res_ext(catalog, delegate_list[i]->value, 0, system, prefer);
		if(result)
		break;
	}
	Free(delegate_list);
	return result;
	}

 try_public:

	if(!public)
	goto try_more_files;

	/* Try for a matching public entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d public entries\n",
		VectorCount(cef->publicEntries));

	for(i=0; i<VectorCount(cef->publicEntries); i++)
	{
	entry = cef->publicEntries[i];
	p = entry->prefer == PR_unspecified ? prefer : entry->prefer;
	if(system && p != PR_public)
		continue;
	if(strcmp(public, entry->match) == 0)
	{
		if(catalog_debug)
		fprintf(stderr, "matched %s, returning %s\n",
			entry->match, entry->value);
		return entry->value;
	}
	}

	/* Try for a matching delegatePublic entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d delegatePublic entries\n",
		VectorCount(cef->delegatePublicEntries));

	VectorInit(delegate_list);
	for(i=0; i<VectorCount(cef->delegatePublicEntries); i++)
	{
	entry = cef->delegatePublicEntries[i];
	p = entry->prefer == PR_unspecified ? prefer : entry->prefer;
	if(system && p != PR_public)
		continue;
	len = strlen(entry->match);
	if(strncmp(public, entry->match, len) == 0)
		if(!VectorPush(delegate_list, entry))
		return fail;
	}

	if((nmatches = VectorCount(delegate_list)) > 0)
	{
	qsort((void *)delegate_list, nmatches, sizeof(delegate_list[0]),
		  entry_compare);

	if(catalog_debug)
	{
		fprintf(stderr, "%d matches:\n", nmatches);
		for(i=0; i<nmatches; i++)
		fprintf(stderr, " %s -> %s\n",
			delegate_list[i]->match, delegate_list[i]->value);
	}

	for(i=0; i<nmatches; i++)
	{
		result =
		res_ext(catalog, delegate_list[i]->value, public, 0, prefer);
		if(result)
		break;
	}
	Free(delegate_list);
	return result;
	}

 try_more_files:

	/* Try any catalog files specified in nextCatalog entries */

	if(catalog_debug)
	fprintf(stderr, "trying %d nextCatalog entries\n",
		VectorCount(cef->nextCatalogEntries));

	result = 0;
	for(i=0; i<VectorCount(cef->nextCatalogEntries); i++)
	{
	result = res_ext(catalog, cef->nextCatalogEntries[i], public, system, prefer);
	if(result)
		break;
	}

	if(result)
	return result;

	return 0;
}

char *ResolveURI(Catalog catalog, const char *uri)
{
	int i, is_publicid_urn;
	char *temp;

	if(catalog_debug)
	fprintf(stderr, "resolving uri <%s>\n", uri);

	/* Normalize and unwrap */

	is_publicid_urn = IsPublicidUrn(uri);
	if(is_publicid_urn)
	{
	if(!(temp = UnwrapPublicidUrn(uri)) ||
	   !(uri = NormalizePublic8(temp)))
		return 0;
	Free(temp);
	}
	else
	uri = NormalizeSystem8(uri);

	if(catalog_debug)
	fprintf(stderr, "after normalizing and unwrapping: <%s>\n", uri);

	/* Search the catalogs in the path */

	for(i=0; i<VectorCount(catalog->path); i++)
	{
	char *result;

	if(is_publicid_urn)
		result = res_ext(catalog, catalog->path[i], uri, 0, PR_public);
	else
		result = res_uri(catalog, catalog->path[i], uri);
	if(result == fail)
		return 0;
	if(result)
		return result;
	}

	return 0;
}

static char *res_uri(Catalog catalog, char *file, const char *uri)
{
	int i;
	int len, best_length, prefix_length, nmatches;
	char *best_value = 0, *result = 0; /* init to please gcc */
	CatalogEntryFile cef;
	CatalogEntry entry;
	Vector(CatalogEntry, delegate_list);

	if(catalog_debug)
	fprintf(stderr, "looking for <%s> in %s\n", uri, file);

	if(!(cef = GetCatalogEntryFile(catalog, file)))
	return fail;

	/* Try for a matching uri entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d uri entries\n",
		VectorCount(cef->uriEntries));

	for(i=0; i<VectorCount(cef->uriEntries); i++)
	{
	entry = cef->uriEntries[i];
	if(strcmp(uri, entry->match) == 0)
	{
		if(catalog_debug)
		fprintf(stderr, "matched %s, returning %s\n",
			entry->match, entry->value);
		return entry->value;
	}
	}

	/* Try for a matching rewriteURI entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d rewriteURI entries\n",
		VectorCount(cef->rewriteURIEntries));

	best_length = 0;
	for(i=0; i<VectorCount(cef->rewriteURIEntries); i++)
	{
	entry = cef->rewriteURIEntries[i];
	len = strlen(entry->match);
	if(len > best_length &&
	   strncmp(uri, entry->match, len) == 0)
	{
		best_length = len;
		best_value = entry->value;
	}
	}
	if(best_length > 0)
	{
	prefix_length = strlen(best_value);
	if(!(result = Malloc(prefix_length + strlen(uri+best_length) + 1)))
		return fail;
	strcpy(result, best_value);
	strcpy(result+prefix_length, uri+best_length);
	if(catalog_debug)
		fprintf(stderr, "best match %s (%d), returning %s\n",
			best_value, prefix_length, result);
	return result;
	}

	/* Try for a matching delegateURI entry */

	if(catalog_debug)
	fprintf(stderr, "trying %d delegateURI entries\n",
		VectorCount(cef->delegateURIEntries));

	VectorInit(delegate_list);
	for(i=0; i<VectorCount(cef->delegateURIEntries); i++)
	{
	entry = cef->delegateURIEntries[i];
	len = strlen(entry->match);
	if(strncmp(uri, entry->match, len) == 0)
		if(!VectorPush(delegate_list, entry))
		return fail;
	}

	if((nmatches = VectorCount(delegate_list)) > 0)
	{
	qsort((void *)delegate_list, nmatches, sizeof(delegate_list[0]),
		  entry_compare);

	if(catalog_debug)
	{
		fprintf(stderr, "%d matches:\n", nmatches);
		for(i=0; i<nmatches; i++)
		fprintf(stderr, " %s -> %s\n",
			delegate_list[i]->match, delegate_list[i]->value);
	}

	for(i=0; i<nmatches; i++)
	{
		result = res_uri(catalog, delegate_list[i]->value, uri);
		if(result)
		break;
	}
	Free(delegate_list);
	return result;
	}

	/* Try any catalog files specified in nextCatalog entries */

	if(catalog_debug)
	fprintf(stderr, "trying %d nextCatalog entries\n",
		VectorCount(cef->nextCatalogEntries));

	result = 0;
	for(i=0; i<VectorCount(cef->nextCatalogEntries); i++)
	{
	result = res_uri(catalog, cef->nextCatalogEntries[i], uri);
	if(result)
		break;
	}

	if(result)
	return result;

	return 0;
}

static int entry_compare(const void *a, const void *b)
{
	/* longest first, i.e. long < short */
	return strlen((*(CatalogEntry *)b)->match) -
	   strlen((*(CatalogEntry *)a)->match);
}
