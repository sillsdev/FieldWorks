/*
 * Copyright Richard Tobin 1995-9.
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>

#include "system.h"
#include "hash.h"

struct hash_table {
	int entries;
	int buckets;
	struct hash_entry **bucket;
};

static unsigned int hash(const void *key, int len);
static void rehash(HashTable table);
static HashEntry hash_lookup(HashTable table, const void *key, int key_len,
				 int *foundp, int add);
static int key_compare(const void *key1, int key1_len,
			   const void *key2, int key2_len);
static void *key_copy(const void *key, int key_len);

/*
 * Create a hash table.
 * init_size is the initial number of buckets, it doesn't have to be "right".
 */

HashTable create_hash_table(int init_size)
{
	int s, i;
	HashTable table;

	table = Malloc(sizeof(*table));
	if(!table)
	return 0;

	for(s = 256; s < init_size; s <<= 1)
	;

	table->entries = 0;
	table->buckets = s;
	table->bucket = Malloc(s * sizeof(*table->bucket));
	if(!table->bucket)
	return 0;

	for(i=0; i<s; i++)
	table->bucket[i] = 0;

	return table;
}

/*
 * Free a hash table.
 */

void free_hash_table(HashTable table)
{
	int i;
	HashEntry entry, next;

	for(i=0; i<table->buckets; i++)
	for(entry = table->bucket[i]; entry; entry = next)
	{
		next = entry->next;
		Free(entry->key);
		Free(entry);
	}

	Free(table->bucket);
	Free(table);
}

int hash_count(HashTable table)
{
	return table->entries;
}

HashEntry hash_find(HashTable table, const void *key, int key_len)
{
	return hash_lookup(table, key, key_len, 0, 0);
}

HashEntry hash_find_or_add(HashTable table, const void *key, int key_len,
			   int *foundp)
{
	return hash_lookup(table, key, key_len, foundp, 1);
}

static HashEntry hash_lookup(HashTable table, const void *key, int key_len,
				 int *foundp, int add)
{
	HashEntry *entry, new;
	unsigned int h = hash(key, key_len);

	for(entry = &table->bucket[h % table->buckets];
	*entry;
	entry = &(*entry)->next)
	if(key_compare((*entry)->key, (*entry)->key_len, key, key_len) == 0)
		break;

	if(foundp)
	*foundp = (*entry != 0);

	if(*entry == 0 && add == 0)
	return 0;

	if(*entry != 0)
	return *entry;

	if(table->entries > table->buckets)	/* XXX arbitrary! */
	{
	rehash(table);
	return hash_lookup(table, key, key_len, foundp, add);
	}

	new = Malloc(sizeof(*new));
	if(!new)
	return 0;

	new->key = key_copy(key, key_len);
	new->key_len = key_len;
	new->value = 0;
	new->next = 0;

	table->entries++;

	*entry = new;

	return new;
}

void hash_remove(HashTable table, HashEntry entry)
{
	unsigned int h = hash(entry->key, entry->key_len);
	HashEntry *e;

	for(e = &table->bucket[h % table->buckets]; *e; e = &(*e)->next)
	if(*e == entry)
	{
		*e = entry->next;
		Free(entry);
		table->entries--;
		return;
	}

	fprintf(stderr, "Attempt to remove non-existent entry from table\n");
	abort();
}

void hash_map(HashTable table,
		  void (*function)(const HashEntryStruct *, void *), void *arg)
{
	int i;
	HashEntry entry;

	for(i=0; i<table->buckets; i++)
	for(entry = table->bucket[i]; entry; entry = entry->next)
		(*function)(entry, arg);
}

static void rehash(HashTable table)
{
	HashTable new;
	unsigned h;
	int i;
	HashEntry entry, next, *chain;

	/* XXX Should collect some statistics here */

	new = create_hash_table(2 * table->buckets);
	if(!new)
	return;

	for(i=0; i<table->buckets; i++)
	{
	for(entry = table->bucket[i]; entry; entry = next)
	{
		next = entry->next;
		h = hash(entry->key, entry->key_len);
		chain = &new->bucket[h % new->buckets];
		entry->next = *chain;
		*chain = entry;
		new->entries++;
	}
	}

	assert(new->entries == table->entries);

	Free(table->bucket);
	*table = *new;
	Free(new);
}

/*
 * Chris Torek's hash function.  I don't know whether it's any good for
 * this...
 */

static unsigned int hash(const void *key, int len)
{
	const char *k = key;
	unsigned int h = 0;		/* should probably be 32 bits */
	int i;

	for(i=0; i<len; i++)
	h = (h << 5) + h + k[i];

	return h;
}

static int key_compare(const void *key1, int key1_len,
			   const void *key2, int key2_len)
{
	if(key1_len != key2_len)
	return -1;
	return memcmp(key1, key2, key1_len);
}

static void *key_copy(const void *key, int key_len)
{
	void *copy;

	copy = Malloc(key_len);
	if(!copy)
	return 0;

	memcpy(copy, key, key_len);

	return copy;
}
