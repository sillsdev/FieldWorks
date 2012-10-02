#ifndef NF16DATA_H
#define NF16DATA_H

/* Copyright 2003 Martin J. Du"rst, W3C; for details, see
 * http://www.w3.org/Consortium/Legal/2002/copyright-software-20021231
 */

/* data for normalization checking for UTF-16 */
/* Supports full normalization according to W3C Character Model */

typedef struct combining {
	unsigned int code:24;
	unsigned int class:8;
} combiningClass;

/* short because there is no recombination above bmp */
typedef struct recomb {
	unsigned short base;
	unsigned short follow;
} recombining;

typedef enum fl {
		HIGH,  /* high surrogates */
		loww,  /* low surrogate */
		NoNo,  /* does not exist */
		NOFC,  /* singleton/excluded */
		ReCo,  /* class > 0, recombining */
		NoRe,  /* class > 0, not recombining */
		COM0,  /* class==0, but composing */
		Hang,  /* hangul initial consonants */
		hAng,  /* hangul medial vowel */
		haNG,  /* hangul trailing consonants */
		HAng,  /* initial/medial syllable */
		Base,  /* base that combines */
		simp   /* nothing special */
} flag;


extern combiningClass combiningClasses[];
extern int combiningClassCount;

extern recombining recombiners[];
extern int recombinerCount;

extern unsigned char nf16flags[];

#endif /* NF16DATA_H */
