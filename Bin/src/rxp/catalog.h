#include "xmlparser.h"
#include "rxputil.h"

typedef enum prefer {
	PR_system, PR_public, PR_unspecified,
	PR_enum_count
} Prefer;

extern const char *PreferName[PR_enum_count];

typedef struct catalog_entry {
	char *match;
	char *value;
	Prefer prefer;
} *CatalogEntry;

typedef struct catalog_entry_file {
	Vector(CatalogEntry, publicEntries);
	Vector(CatalogEntry, systemEntries);
	Vector(CatalogEntry, rewriteSystemEntries);
	Vector(CatalogEntry, delegatePublicEntries);
	Vector(CatalogEntry, delegateSystemEntries);
	Vector(CatalogEntry, uriEntries);
	Vector(CatalogEntry, rewriteURIEntries);
	Vector(CatalogEntry, delegateURIEntries);
	Vector(char *, nextCatalogEntries);
} *CatalogEntryFile;

typedef struct cached_entry_file {
	char *uri;
	CatalogEntryFile cef;
} *CachedEntryFile;

typedef struct catalog {
	Vector(char *, path);
	Vector(CachedEntryFile, cache);
	/* This is not used by API; entity resolver caches value from env here: */
	Prefer default_prefer;
} *Catalog;

extern int catalog_debug;

extern Catalog NewCatalog(char *path);
extern void FreeCatalog(Catalog catalog);

extern CatalogEntry NewCatalogEntry(char *match, char *value, Prefer prefer);
extern void FreeCatalogEntry(CatalogEntry entry);

extern CatalogEntryFile catalog_resource_error;
extern CatalogEntryFile ReadCatalogEntryFile(char *catalog_uri);
extern void FreeCatalogEntryFile(CatalogEntryFile c);

extern CatalogEntryFile GetCatalogEntryFile(Catalog catalog, char *catalog_uri);

extern char *ResolveExternalIdentifier(Catalog catalog,
					   const char *public, const char *system,
					   Prefer prefer);
extern char *ResolveURI(Catalog catalog, const char *uri);

extern void CatalogEnable(Parser p);
