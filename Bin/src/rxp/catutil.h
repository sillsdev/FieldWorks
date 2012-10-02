#include "charset.h"

extern char *NormalizePublic8(const char8 *public);
extern char *NormalizePublic16(const char16 *public);
extern char *NormalizePublic(const Char *public);

extern char *NormalizeSystem8(const char8 *system);
extern char *NormalizeSystem16(const char16 *system);
extern char *NormalizeSystem(const Char *system);

extern int IsPublicidUrn(const char *id);
extern char *UnwrapPublicidUrn(const char *id);

extern int toUTF8(int c, int *bytes);
extern int percent_escape(int c, char *buf);

extern int strcmpC8(const Char *s1, const char *s2);
