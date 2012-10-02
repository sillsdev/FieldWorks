/* Copyright 2003 Martin J. Du"rst, W3C; for details, see
 * http://www.w3.org/Consortium/Legal/2002/copyright-software-20021231
 */

/* Normalization checking for UTF-16 */
/* Supports full normalization according to W3C Character Model */

#include <stdlib.h>
#include "nf16check.h"
#include "nf16data.h"

#define getflag(c) ((flag) ((c)&0x1 ? nf16flags[(c)>>1]&0xF : nf16flags[(c)>>1]>>4))


/* create a checker object */
NF16Checker nf16checkNew (int exists)
{
	NF16Checker checker;

	checker = (NF16Checker)malloc(sizeof(*checker));
	if (!checker)
		return 0;

	checker->starter     = 0;
	checker->starterflag = simp;
	checker->startP      = NF16noStart;
	checker->lastclass   = 0;
	checker->exists      = exists;

	return checker;
}


/* delete a checker object */
void nf16checkDelete (NF16Checker checker)
{
	if (checker)
		free(checker);
}


/* change checking of existing characters */
void nf16checkExists (NF16Checker checker, int exists)
{
	checker->exists = exists;
}

/* set start state to NF16Start */
void nf16checkStart   (NF16Checker checker)
{
	checker->starter     = 0;
	checker->starterflag = simp;
	checker->startP      = NF16Start;
	checker->lastclass   = 0;
}

/* set start state to NF16noStart */
void nf16checkNoStart (NF16Checker checker)
{
	checker->starter     = 0;
	checker->starterflag = simp;
	checker->startP      = NF16noStart;
	checker->lastclass   = 0;
}



/* check recombination */
static int recombines (unsigned int b, unsigned int f)
{
	int low  = 0;                     /* lowest inside non-checked area */
	int high = recombinerCount;       /* lowest outside    checked area */
	while (low < high) {
		int middle  = (low+high) / 2;  /* binary search */
		int middleB = recombiners[middle].base;
		if (b == middleB) {
			int middleF = recombiners[middle].follow;
			if (f == middleF)      return 1;
			else if (f < middleF)  high = middle;
			else                   low  = middle + 1;
		}
		else if (b < middleB)  high = middle;
		else                   low = middle+1;
	}
	return  (recombiners[low].base == b && recombiners[low].follow == f);
}


/* finding combining class of c */
static int getclass (unsigned int c)
{
	int low  = 0;                     /* lowest inside non-checked area */
	int high = combiningClassCount;   /* lowest outside    checked area */
	while (low < high) {
		int middle = (low+high) / 2;  /* binary search */
		unsigned int middleC = combiningClasses[middle].code;
		if (c == middleC)
			return combiningClasses[middle].class;
		if (c < middleC)
			high = middle;
		else
			low = middle+1;
	}
	/* we only get called for values that have an answer */
	return combiningClasses[low].class;
}


/* general check function, s is null-delimited */
nf16res nf16check (NF16Checker checker, char16* s)
{
	 /* how much does it save us to copy into local variables ? */
	unsigned int starter     = checker->starter;
	flag         starterflag = (flag) checker->starterflag;
	nf16start    startP      = checker->startP;
	int          lastclass   = checker->lastclass;
	char16       c;
	flag         f;
	int          class;

	if (startP == NF16error)
		return NF16okay;      /* only return one error per sequence */

	if (startP==NF16continue) {
		starter     = checker->starter;
		starterflag = (flag) checker->starterflag;
		if ((startP = checker->startP)==NF16error)
			return NF16okay;      /* only return one error per sequence */
		lastclass   = checker->lastclass;
	}

	while ((c = *s++)) {
	  GETFLAG:
		f = getflag(c);
	  NEWFLAG:
		switch (f) {
		  case HIGH:  /* high surrogate */
			checker->high = c;
			continue;
		  case loww:  /* low surrogate */
			/* combine with high surrogate */
			c = ((checker->high-0xD800)<<10) + (c-0xDC00) + 0x10000;
			/* check in pieces */
			if      (c < 0x10900)  goto GETFLAG; /* still in main table */
			else if (c < 0x1D000)  f = NoNo;
			else if (c < 0x1D800)  f = getflag(c-(0x1D000-0x10900));
			else if (c < 0x20000)  f = NoNo;
			else if (c < 0x2A6D7)  f = simp;
			else if (c < 0x2F800)  f = NoNo;
			else if (c < 0x2FA1D)  f = NOFC;
			else if (c ==0xE0001)  f = simp;
			else if (c < 0xE0020)  f = NoNo;
			else if (c < 0xE0080)  f = simp;
			else if (c < 0xE0100)  f = NoNo;
			else if (c < 0xE01F0)  f = simp;
			else                   f = NoNo;
			goto NEWFLAG;   /* start again with switch */
		  case NOFC:    /* singleton/excluded */
			goto WRONG;
		  case ReCo:    /* class > 0, recombining */
			if (startP==NF16Start)  goto WRONG;
			class = getclass (c);
			if (lastclass > class)  goto WRONG;
			/* do not check for lastclass==class to take blocking into account */
			if (lastclass < class && starterflag==Base && recombines(starter, c))
				goto WRONG;
			lastclass = class;
			startP = NF16continue;
			continue;
		  case NoRe:    /* class > 0, not recombining */
			if (startP==NF16Start)  goto WRONG;
			class = getclass (c);
			if (lastclass > class)  goto WRONG;
			lastclass = class;
			startP = NF16continue;
			continue;
		  case COM0:   /* class==0, but composing */
			if (startP==NF16Start)  goto WRONG;
			if (starterflag==Base && recombines(starter, c))
				goto WRONG;
			break;    /* class 0 can be a starter */
		  case Hang:  /* hangul initial consonants */
			break;
		  case HAng:  /* initial/medial syllable */
			break;
		  case hAng:  /* hangul medial vowel */
			if (startP==NF16Start || starterflag == Hang)
				goto WRONG;
			break;
		  case haNG:  /* hangul trailing consonants */
			if (startP==NF16Start || starterflag == HAng)
				goto WRONG;
			break;
		  case NoNo:  /* does not exist */
			if (checker->exists)
				return  NF16wrong;
			break;
		  case Base:  /* base that combines */
		  case simp:  /* nothing special */
			break;    /* could add inner loop here, too */
		}
		starter     = c;
		starterflag = f;
		lastclass   = 0;
		startP      = NF16continue;
	}
	checker->starter     = starter;
	checker->starterflag = (int) starterflag;
	checker->startP      = startP;
	checker->lastclass   = lastclass;
	return NF16okay;

  WRONG:
	checker->startP      = NF16error;
	return NF16wrong;
}


/* variant of nf16check, s_length gives length of s */
nf16res nf16checkL (NF16Checker checker, char16* s, int length)
{
	 /* how much does it save us to copy into local variables ? */
	unsigned int starter     = checker->starter;
	flag         starterflag = (flag) checker->starterflag;
	nf16start    startP      = checker->startP;
	int          lastclass   = checker->lastclass;
	char16       c;
	flag         f;
	int          class;

	if (startP == NF16error)
		return NF16okay;      /* only return one error per sequence */

	while (length--) {
		c = *s++;
	  GETFLAG:
		f = getflag(c);
	  NEWFLAG:
		switch (f) {
		  case HIGH:  /* high surrogate */
			checker->high = c;
			continue;
		  case loww:  /* low surrogate */
			/* combine with high surrogate */
			c = ((checker->high-0xD800)<<10) + (c-0xDC00) + 0x10000;
			/* check in pieces */
			if      (c < 0x10900)  goto GETFLAG; /* still in main table */
			else if (c < 0x1D000)  f = NoNo;
			else if (c < 0x1D800)  f = getflag(c-(0x1D000-0x10900));
			else if (c < 0x20000)  f = NoNo;
			else if (c < 0x2A6D7)  f = simp;
			else if (c < 0x2F800)  f = NoNo;
			else if (c < 0x2FA1D)  f = NOFC;
			else if (c ==0xE0001)  f = simp;
			else if (c < 0xE0020)  f = NoNo;
			else if (c < 0xE0080)  f = simp;
			else if (c < 0xE0100)  f = NoNo;
			else if (c < 0xE01F0)  f = simp;
			else                   f = NoNo;
			goto NEWFLAG;   /* start again with switch */
		  case NOFC:    /* singleton/excluded */
			goto WRONG;
		  case ReCo:    /* class > 0, recombining */
			if (startP==NF16Start)  goto WRONG;
			class = getclass (c);
			if (lastclass > class)  goto WRONG;
			/* do not check for lastclass==class to take blocking into account */
			if (lastclass < class && starterflag==Base && recombines(starter, c))
				goto WRONG;
			lastclass = class;
			startP = NF16continue;
			continue;
		  case NoRe:    /* class > 0, not recombining */
			if (startP==NF16Start)  goto WRONG;
			class = getclass (c);
			if (lastclass > class)  goto WRONG;
			lastclass = class;
			startP = NF16continue;
			continue;
		  case COM0:   /* class==0, but composing */
			if (startP==NF16Start)  goto WRONG;
			if (starterflag==Base && recombines(starter, c))
				goto WRONG;
			break;    /* class 0 can be a starter */
		  case Hang:  /* hangul initial consonants */
			break;
		  case HAng:  /* initial/medial syllable */
			break;
		  case hAng:  /* hangul medial vowel */
			if (startP==NF16Start || starterflag == Hang)
				goto WRONG;
			break;
		  case haNG:  /* hangul trailing consonants */
			if (startP==NF16Start || starterflag == HAng)
				goto WRONG;
			break;
		  case NoNo:  /* does not exist */
			if (checker->exists)
				return  NF16wrong;
			break;
		  case Base:  /* base that combines */
		  case simp:  /* nothing special */
			break;    /* could add inner loop here, too */
		}
		starter     = c;
		starterflag = f;
		lastclass   = 0;
		startP      = NF16continue;
	}
	checker->starter     = starter;
	checker->starterflag = (int) starterflag;
	checker->startP      = startP;
	checker->lastclass   = lastclass;
	return NF16okay;

  WRONG:
	checker->startP      = NF16error;
	return NF16wrong;
}
