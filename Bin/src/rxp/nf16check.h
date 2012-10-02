#ifndef NF16CHECK_H
#define NF16CHECK_H

/* Copyright 2003 Martin J. Du"rst, W3C; for details, see
 * http://www.w3.org/Consortium/Legal/2002/copyright-software-20021231
 */

/* Normalization checking for UTF-16 */
/* Supports full normalization according to W3C Character Model */


#ifndef CHARSET_H
/*  If we're being included in an RXP .c file, this will already
	have been typedef'd in charset.h. */
typedef unsigned short char16;
#endif

typedef enum nf16start {
	NF16Start,     /* check including start condition */
	NF16noStart,   /* don't check start condition */
	NF16continue,  /* continue checking */
	NF16error      /* error, wait for next start */
} nf16start;

typedef struct nf16checker {
	unsigned int starter;       /* starter of a run that may be combined */
	int          starterflag;   /* declared as int to not expose NF16data.h */
	nf16start    startP;        /* start/continue/error condition */
	int          lastclass;     /* combining class */
	unsigned int high;          /* high surrogate not yet analyzed */
	int          exists;        /* 1: check that character exists; 0: don't check */
} *NF16Checker;


typedef enum nf16result {
	NF16wrong,
	NF16okay
} nf16res;

/* create one per thread/entity/whatever */
NF16Checker nf16checkNew (int exists);

void nf16checkDelete (NF16Checker checker);

/* set start state */
void nf16checkStart   (NF16Checker checker);
void nf16checkNoStart (NF16Checker checker);

/* change checking of existing characters */
void nf16checkExists (NF16Checker checker, int exists);

/* general check function, s is null-delimited */
nf16res nf16check (NF16Checker checker, char16* s);

/* variant of nf16check, s_length gives length of s */
nf16res nf16checkL (NF16Checker checker, char16* s, int s_length);

#endif /* NF16CHECK_H */
