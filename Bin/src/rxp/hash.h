/*
 * Copyright Richard Tobin 1995-9.
 */

#ifndef HASH_H
#define HASH_H

typedef struct hash_entry {
	void *key;
	int key_len;
	void *value;
	struct hash_entry *next;
} HashEntryStruct;

typedef HashEntryStruct *HashEntry;
typedef struct hash_table *HashTable;

HashTable create_hash_table(int init_size);
void free_hash_table(HashTable table);
HashEntry hash_find(HashTable table, const void *key, int key_len);
HashEntry hash_find_or_add(HashTable table, const void *key, int key_len, int *foundp);
void hash_remove(HashTable table, HashEntry entry);
void hash_map(HashTable table,
		  void (*function)(const HashEntryStruct *, void *), void *arg);
int hash_count(HashTable table);

#endif /* HASH_H */
