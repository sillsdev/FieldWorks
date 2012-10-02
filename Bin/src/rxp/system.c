#include <stdlib.h>
#include "system.h"
#include "stdio16.h"

void *Malloc(int bytes)
{
	void *mem = malloc(bytes);
	if(!mem)
	Fprintf(Stderr, "malloc failed\n");
	return mem;
}

void *Realloc(void *mem, int bytes)
{
	mem = mem ? realloc(mem, bytes) : malloc(bytes);
	if(!mem)
	Fprintf(Stderr, "realloc failed\n");
	return mem;
}

void Free(void *mem)
{
	free(mem);
}
