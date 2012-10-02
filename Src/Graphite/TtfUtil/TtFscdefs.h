/*
	Original File:       fscdefs.h

	Copyright:  c 1988-1990 by Apple Computer, Inc., all rights reserved.
	Modified 4/25/2000 by Alan Ward
*/

/*
*/
#define ONEFIX      ( 1L << 16 )
#define ONEFRAC     ( 1L << 30 )
#define ONEHALFFIX  0x8000L

#ifdef __int8_t_defined
typedef int8_t int8;
typedef uint8_t uint8;
typedef int16_t int16;
typedef uint16_t uint16;
typedef int32_t int32;
typedef uint32_t uint32;
#else
/* On 64 bit architecture this may be wrong */
typedef char int8;
typedef unsigned char uint8;
typedef short int16;
typedef unsigned short uint16;
typedef long int32;
typedef unsigned long uint32;
#endif

typedef int16 FUnit;
typedef uint16 uFUnit;

typedef int32 Fixed;
typedef int32 Fract;

#ifndef SHORTMUL
#define SHORTMUL(a,b)   (int32)((int32)(a) * (b))
#endif

#ifndef SHORTDIV
#define SHORTDIV(a,b)   (int32)((int32)(a) / (b))
#endif

inline float F2Dot14(short a) {return (a >> 14) + (a & 0x3FFF) / (float)0x4000;}


#ifdef FSCFG_BIG_ENDIAN /* target byte order matches Motorola 68000 */
inline uint16 swapw(int16 a) {return a;};
inline uint32 swapl(int32 a) {return a;};
inline int16 swapws(int16 a) {return a;}
inline int32 swapls(int32 a) {return a;};
#else
 /* Portable code to extract a short or a long from a 2- or 4-byte buffer */
 /* which was encoded using Motorola 68000 (TrueType "native") byte order. */
// if p were signed, the last element in the p array would be signed extended
// use the first two for unsigned fields and the last two for signed fields
inline uint16 swapw(int16 a) {unsigned char * p = (unsigned char *)&a; return p[0]<<8 | p[1];};
inline uint32 swapl(int32 a) {unsigned char * p = (unsigned char *)&a; return p[0]<<24 | p[1]<<16 | p[2]<<8 | p[3];};
inline int16 swapws(int16 a) {unsigned char * p = (unsigned char *)&a; return p[0]<<8 | p[1];};
inline int32 swapls(int32 a) {unsigned char * p = (unsigned char *)&a; return p[0]<<24 | p[1]<<16 | p[2]<<8 | p[3];};
#endif
