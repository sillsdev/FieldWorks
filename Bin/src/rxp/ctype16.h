#ifndef CTYPE16_H
#define CTYPE16_H

#ifndef FOR_LT
#define STD_API
#endif

/* XML character types */

STD_API int init_ctype16(void);
STD_API void deinit_ctype16(void);
STD_API int Toupper(int c);
STD_API int Tolower(int c);

extern STD_API unsigned char xml_char_map[];

#define xml_legal      0x01
#define xml_namestart  0x02
#define xml_namechar   0x04
#define xml_whitespace 0x08
#define xml_nameblock  0x10

#if CHAR_SIZE == 8

/* And with 0xff so that it works if char is signed */
#define is_xml_legal(c,map) (map[(int)(c) & 0xff] & xml_legal)
#define is_xml_namestart(c,map) (map[(int)(c) & 0xff] & xml_namestart)
#define is_xml_namechar(c,map) (map[(int)(c) & 0xff] & xml_namechar)
#define is_xml_whitespace(c) (xml_char_map[(int)(c) & 0xff] & xml_whitespace)

#define xml_char_map_11 0

#else

extern STD_API unsigned char xml_char_map_11[];

/* Note!  these macros evaluate their argument more than once! */

#define is_xml_legal(c, map) \
  (c < 0x10000 ? (map[c] & xml_legal)     : c < 0x110000)

/*
 * We call is_xml_namechar and is_xml_namestart with UTF-16 codes, rather
 * than (32-bit) Unicode code numbers.  So we allow those surrogates that
 * can occur in (XML 1.1) names; luckily we only need to reject certain
 * high surrogates.
 * (But it also works for codes above 2^16.)
 */

#define is_xml_namestart(c,map) \
  (c < 0x10000 ? (map[c] & xml_namestart) : (map[c >> 16] & xml_nameblock))

#define is_xml_namechar(c,map) \
  (c < 0x10000 ? (map[c] & xml_namechar)  : (map[c >> 16] & xml_nameblock))

/* NB whitespace map is the same for 1.0 and 1.1 */

#define is_xml_whitespace(c) \
  (c < 0x10000 && (xml_char_map[c] & xml_whitespace))

#endif

#endif /* CTYPE16_H */
