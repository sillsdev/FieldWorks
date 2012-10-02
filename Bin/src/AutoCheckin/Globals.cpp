#include <windows.h>

#include "Globals.h"

char * gpszClient = NULL;
char * gpszUser = NULL;
char * gpszRoot = NULL;
int * gpnChangeLists = NULL;
char ** gppszChangeListsComments = NULL;
int gcclChangeLists = 0;
char * gpszCheckinUser = NULL;
char * gpszCheckinDate = NULL;
char * gpszCheckinComment = NULL;
int gnCheckinChangeList = 0;
bool gfAutoResolve = true;