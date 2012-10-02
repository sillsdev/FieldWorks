#ifdef FOR_LT
#include "lt-defs.h"
#else
#include "system.h"
#endif

#include "charset.h"
#include "ctype16.h"

static void init_xml_chartypes(void);
static void init_xml_chartypes_11(void);

unsigned char xml_char_map[1 << CHAR_SIZE];
#if CHAR_SIZE == 16
/* we always use the 1.0 map in 8-bit mode */
unsigned char xml_char_map_11[1 << CHAR_SIZE];
#endif

int init_ctype16(void)
{
	static int init_done = 0;

	if(!init_done)
	{
	init_xml_chartypes();
	init_xml_chartypes_11();
	init_done = 1;
	}

	return 0;
}

void deinit_ctype16(void)
{
	/* nothing to do */
}

/* XXX Tolower and Toupper only work for ISO-Latin-1 */

int Tolower(int c)
{
	if(c < 0)			/* Sigh, char is probably signed */
	c &= 0xff;

	if((c >= 'A' && c <= 'Z') ||
	   (c >= 192 && c <= 214) ||
	   (c >= 216 && c <= 222))
	return c + 32;
	else
	return c;
}

int Toupper(int c)
{
	if(c < 0)			/* Sigh, char is probably signed */
	c &= 0xff;

	if((c >= 'a' && c <= 'z') ||
	   (c >= 224 && c <= 246) ||
	   (c >= 248 && c <= 254))
	return c - 32;
	else
	return c;
}

/*
 * Notes:
 * XML spec says 255 is legal even though SGML doesn't.
 * XML spec says 181 (mu for units) is a letter - is this right?
 * XML spec says 183 bullet is an extender and thus a namechar.
 */

static void init_xml_chartypes(void)
{
	int i;

	for(i = 0; i < 1 << CHAR_SIZE; i++)
	xml_char_map[i] = 0;

	/* Legal ASCII characters */

	xml_char_map['\t'] |= xml_legal;
	xml_char_map['\r'] |= xml_legal;
	xml_char_map['\n'] |= xml_legal;
	for(i = ' '; i <= 127; i++)
	xml_char_map[i] |= xml_legal;

	/* ASCII Letters */

	for(i = 'a'; i <= 'z'; i++)
	xml_char_map[i] |= (xml_namestart | xml_namechar);
	for(i = 'A'; i <= 'Z'; i++)
	xml_char_map[i] |= (xml_namestart | xml_namechar);

	/* ASCII Digits */

	for(i = '0'; i <= '9'; i++)
	xml_char_map[i] |= xml_namechar;

	/* ASCII Whitespace */

	xml_char_map[' '] |= xml_whitespace;
	xml_char_map['\t'] |= xml_whitespace;
	xml_char_map['\r'] |= xml_whitespace;
	xml_char_map['\n'] |= xml_whitespace;

	/* ASCII Others */

	xml_char_map['_'] |= (xml_namestart | xml_namechar);
	xml_char_map[':'] |= (xml_namestart | xml_namechar);

	xml_char_map['.'] |= xml_namechar;
	xml_char_map['-'] |= xml_namechar;

#if CHAR_SIZE == 8

	/* Treat all non-ASCII chars as namestart, so we don't complain
	   about legal documents. */

	for(i = 0x80; i <= 0xff; i++)
	xml_char_map[i] |= (xml_namestart | xml_namechar | xml_legal);

#else

	/* Legal Unicode characters */

	for(i = 0x80; i <= 0xd7ff; i++)
	xml_char_map[i] |= xml_legal;
	for(i = 0xe000; i <= 0xfffd; i++)
	xml_char_map[i] |= xml_legal;

	/* Latin-1 Letters */

	for(i = 0xc0; i <= 0xd6; i++)
	xml_char_map[i] |= (xml_namestart | xml_namechar);
	for(i = 0xd8; i <= 0xf6; i++)
	xml_char_map[i] |= (xml_namestart | xml_namechar);
	for(i = 0xf8; i <= 0xff; i++)
	xml_char_map[i] |= (xml_namestart | xml_namechar);

	/* Latin-1 Others */

	xml_char_map[0xb7] |= xml_namechar; /* raised dot (an extender) */

	/* Derived from HTML text of PR-xml-971208 */

	/* BaseChar */

#define X (xml_namestart | xml_namechar)

	for(i = 0x0100; i <= 0x0131; i++) xml_char_map[i] |= X;
	for(i = 0x0134; i <= 0x013e; i++) xml_char_map[i] |= X;
	for(i = 0x0141; i <= 0x0148; i++) xml_char_map[i] |= X;
	for(i = 0x014a; i <= 0x017e; i++) xml_char_map[i] |= X;
	for(i = 0x0180; i <= 0x01c3; i++) xml_char_map[i] |= X;
	for(i = 0x01cd; i <= 0x01f0; i++) xml_char_map[i] |= X;
	for(i = 0x01f4; i <= 0x01f5; i++) xml_char_map[i] |= X;
	for(i = 0x01fa; i <= 0x0217; i++) xml_char_map[i] |= X;
	for(i = 0x0250; i <= 0x02a8; i++) xml_char_map[i] |= X;
	for(i = 0x02bb; i <= 0x02c1; i++) xml_char_map[i] |= X;
	xml_char_map[0x0386] |= X;
	for(i = 0x0388; i <= 0x038a; i++) xml_char_map[i] |= X;
	xml_char_map[0x038c] |= X;
	for(i = 0x038e; i <= 0x03a1; i++) xml_char_map[i] |= X;
	for(i = 0x03a3; i <= 0x03ce; i++) xml_char_map[i] |= X;
	for(i = 0x03d0; i <= 0x03d6; i++) xml_char_map[i] |= X;
	xml_char_map[0x03da] |= X;
	xml_char_map[0x03dc] |= X;
	xml_char_map[0x03de] |= X;
	xml_char_map[0x03e0] |= X;
	for(i = 0x03e2; i <= 0x03f3; i++) xml_char_map[i] |= X;
	for(i = 0x0401; i <= 0x040c; i++) xml_char_map[i] |= X;
	for(i = 0x040e; i <= 0x044f; i++) xml_char_map[i] |= X;
	for(i = 0x0451; i <= 0x045c; i++) xml_char_map[i] |= X;
	for(i = 0x045e; i <= 0x0481; i++) xml_char_map[i] |= X;
	for(i = 0x0490; i <= 0x04c4; i++) xml_char_map[i] |= X;
	for(i = 0x04c7; i <= 0x04c8; i++) xml_char_map[i] |= X;
	for(i = 0x04cb; i <= 0x04cc; i++) xml_char_map[i] |= X;
	for(i = 0x04d0; i <= 0x04eb; i++) xml_char_map[i] |= X;
	for(i = 0x04ee; i <= 0x04f5; i++) xml_char_map[i] |= X;
	for(i = 0x04f8; i <= 0x04f9; i++) xml_char_map[i] |= X;
	for(i = 0x0531; i <= 0x0556; i++) xml_char_map[i] |= X;
	xml_char_map[0x0559] |= X;
	for(i = 0x0561; i <= 0x0586; i++) xml_char_map[i] |= X;
	for(i = 0x05d0; i <= 0x05ea; i++) xml_char_map[i] |= X;
	for(i = 0x05f0; i <= 0x05f2; i++) xml_char_map[i] |= X;
	for(i = 0x0621; i <= 0x063a; i++) xml_char_map[i] |= X;
	for(i = 0x0641; i <= 0x064a; i++) xml_char_map[i] |= X;
	for(i = 0x0671; i <= 0x06b7; i++) xml_char_map[i] |= X;
	for(i = 0x06ba; i <= 0x06be; i++) xml_char_map[i] |= X;
	for(i = 0x06c0; i <= 0x06ce; i++) xml_char_map[i] |= X;
	for(i = 0x06d0; i <= 0x06d3; i++) xml_char_map[i] |= X;
	xml_char_map[0x06d5] |= X;
	for(i = 0x06e5; i <= 0x06e6; i++) xml_char_map[i] |= X;
	for(i = 0x0905; i <= 0x0939; i++) xml_char_map[i] |= X;
	xml_char_map[0x093d] |= X;
	for(i = 0x0958; i <= 0x0961; i++) xml_char_map[i] |= X;
	for(i = 0x0985; i <= 0x098c; i++) xml_char_map[i] |= X;
	for(i = 0x098f; i <= 0x0990; i++) xml_char_map[i] |= X;
	for(i = 0x0993; i <= 0x09a8; i++) xml_char_map[i] |= X;
	for(i = 0x09aa; i <= 0x09b0; i++) xml_char_map[i] |= X;
	xml_char_map[0x09b2] |= X;
	for(i = 0x09b6; i <= 0x09b9; i++) xml_char_map[i] |= X;
	for(i = 0x09dc; i <= 0x09dd; i++) xml_char_map[i] |= X;
	for(i = 0x09df; i <= 0x09e1; i++) xml_char_map[i] |= X;
	for(i = 0x09f0; i <= 0x09f1; i++) xml_char_map[i] |= X;
	for(i = 0x0a05; i <= 0x0a0a; i++) xml_char_map[i] |= X;
	for(i = 0x0a0f; i <= 0x0a10; i++) xml_char_map[i] |= X;
	for(i = 0x0a13; i <= 0x0a28; i++) xml_char_map[i] |= X;
	for(i = 0x0a2a; i <= 0x0a30; i++) xml_char_map[i] |= X;
	for(i = 0x0a32; i <= 0x0a33; i++) xml_char_map[i] |= X;
	for(i = 0x0a35; i <= 0x0a36; i++) xml_char_map[i] |= X;
	for(i = 0x0a38; i <= 0x0a39; i++) xml_char_map[i] |= X;
	for(i = 0x0a59; i <= 0x0a5c; i++) xml_char_map[i] |= X;
	xml_char_map[0x0a5e] |= X;
	for(i = 0x0a72; i <= 0x0a74; i++) xml_char_map[i] |= X;
	for(i = 0x0a85; i <= 0x0a8b; i++) xml_char_map[i] |= X;
	xml_char_map[0x0a8d] |= X;
	for(i = 0x0a8f; i <= 0x0a91; i++) xml_char_map[i] |= X;
	for(i = 0x0a93; i <= 0x0aa8; i++) xml_char_map[i] |= X;
	for(i = 0x0aaa; i <= 0x0ab0; i++) xml_char_map[i] |= X;
	for(i = 0x0ab2; i <= 0x0ab3; i++) xml_char_map[i] |= X;
	for(i = 0x0ab5; i <= 0x0ab9; i++) xml_char_map[i] |= X;
	xml_char_map[0x0abd] |= X;
	xml_char_map[0x0ae0] |= X;
	for(i = 0x0b05; i <= 0x0b0c; i++) xml_char_map[i] |= X;
	for(i = 0x0b0f; i <= 0x0b10; i++) xml_char_map[i] |= X;
	for(i = 0x0b13; i <= 0x0b28; i++) xml_char_map[i] |= X;
	for(i = 0x0b2a; i <= 0x0b30; i++) xml_char_map[i] |= X;
	for(i = 0x0b32; i <= 0x0b33; i++) xml_char_map[i] |= X;
	for(i = 0x0b36; i <= 0x0b39; i++) xml_char_map[i] |= X;
	xml_char_map[0x0b3d] |= X;
	for(i = 0x0b5c; i <= 0x0b5d; i++) xml_char_map[i] |= X;
	for(i = 0x0b5f; i <= 0x0b61; i++) xml_char_map[i] |= X;
	for(i = 0x0b85; i <= 0x0b8a; i++) xml_char_map[i] |= X;
	for(i = 0x0b8e; i <= 0x0b90; i++) xml_char_map[i] |= X;
	for(i = 0x0b92; i <= 0x0b95; i++) xml_char_map[i] |= X;
	for(i = 0x0b99; i <= 0x0b9a; i++) xml_char_map[i] |= X;
	xml_char_map[0x0b9c] |= X;
	for(i = 0x0b9e; i <= 0x0b9f; i++) xml_char_map[i] |= X;
	for(i = 0x0ba3; i <= 0x0ba4; i++) xml_char_map[i] |= X;
	for(i = 0x0ba8; i <= 0x0baa; i++) xml_char_map[i] |= X;
	for(i = 0x0bae; i <= 0x0bb5; i++) xml_char_map[i] |= X;
	for(i = 0x0bb7; i <= 0x0bb9; i++) xml_char_map[i] |= X;
	for(i = 0x0c05; i <= 0x0c0c; i++) xml_char_map[i] |= X;
	for(i = 0x0c0e; i <= 0x0c10; i++) xml_char_map[i] |= X;
	for(i = 0x0c12; i <= 0x0c28; i++) xml_char_map[i] |= X;
	for(i = 0x0c2a; i <= 0x0c33; i++) xml_char_map[i] |= X;
	for(i = 0x0c35; i <= 0x0c39; i++) xml_char_map[i] |= X;
	for(i = 0x0c60; i <= 0x0c61; i++) xml_char_map[i] |= X;
	for(i = 0x0c85; i <= 0x0c8c; i++) xml_char_map[i] |= X;
	for(i = 0x0c8e; i <= 0x0c90; i++) xml_char_map[i] |= X;
	for(i = 0x0c92; i <= 0x0ca8; i++) xml_char_map[i] |= X;
	for(i = 0x0caa; i <= 0x0cb3; i++) xml_char_map[i] |= X;
	for(i = 0x0cb5; i <= 0x0cb9; i++) xml_char_map[i] |= X;
	xml_char_map[0x0cde] |= X;
	for(i = 0x0ce0; i <= 0x0ce1; i++) xml_char_map[i] |= X;
	for(i = 0x0d05; i <= 0x0d0c; i++) xml_char_map[i] |= X;
	for(i = 0x0d0e; i <= 0x0d10; i++) xml_char_map[i] |= X;
	for(i = 0x0d12; i <= 0x0d28; i++) xml_char_map[i] |= X;
	for(i = 0x0d2a; i <= 0x0d39; i++) xml_char_map[i] |= X;
	for(i = 0x0d60; i <= 0x0d61; i++) xml_char_map[i] |= X;
	for(i = 0x0e01; i <= 0x0e2e; i++) xml_char_map[i] |= X;
	xml_char_map[0x0e30] |= X;
	for(i = 0x0e32; i <= 0x0e33; i++) xml_char_map[i] |= X;
	for(i = 0x0e40; i <= 0x0e45; i++) xml_char_map[i] |= X;
	for(i = 0x0e81; i <= 0x0e82; i++) xml_char_map[i] |= X;
	xml_char_map[0x0e84] |= X;
	for(i = 0x0e87; i <= 0x0e88; i++) xml_char_map[i] |= X;
	xml_char_map[0x0e8a] |= X;
	xml_char_map[0x0e8d] |= X;
	for(i = 0x0e94; i <= 0x0e97; i++) xml_char_map[i] |= X;
	for(i = 0x0e99; i <= 0x0e9f; i++) xml_char_map[i] |= X;
	for(i = 0x0ea1; i <= 0x0ea3; i++) xml_char_map[i] |= X;
	xml_char_map[0x0ea5] |= X;
	xml_char_map[0x0ea7] |= X;
	for(i = 0x0eaa; i <= 0x0eab; i++) xml_char_map[i] |= X;
	for(i = 0x0ead; i <= 0x0eae; i++) xml_char_map[i] |= X;
	xml_char_map[0x0eb0] |= X;
	for(i = 0x0eb2; i <= 0x0eb3; i++) xml_char_map[i] |= X;
	xml_char_map[0x0ebd] |= X;
	for(i = 0x0ec0; i <= 0x0ec4; i++) xml_char_map[i] |= X;
	for(i = 0x0f40; i <= 0x0f47; i++) xml_char_map[i] |= X;
	for(i = 0x0f49; i <= 0x0f69; i++) xml_char_map[i] |= X;
	for(i = 0x10a0; i <= 0x10c5; i++) xml_char_map[i] |= X;
	for(i = 0x10d0; i <= 0x10f6; i++) xml_char_map[i] |= X;
	xml_char_map[0x1100] |= X;
	for(i = 0x1102; i <= 0x1103; i++) xml_char_map[i] |= X;
	for(i = 0x1105; i <= 0x1107; i++) xml_char_map[i] |= X;
	xml_char_map[0x1109] |= X;
	for(i = 0x110b; i <= 0x110c; i++) xml_char_map[i] |= X;
	for(i = 0x110e; i <= 0x1112; i++) xml_char_map[i] |= X;
	xml_char_map[0x113c] |= X;
	xml_char_map[0x113e] |= X;
	xml_char_map[0x1140] |= X;
	xml_char_map[0x114c] |= X;
	xml_char_map[0x114e] |= X;
	xml_char_map[0x1150] |= X;
	for(i = 0x1154; i <= 0x1155; i++) xml_char_map[i] |= X;
	xml_char_map[0x1159] |= X;
	for(i = 0x115f; i <= 0x1161; i++) xml_char_map[i] |= X;
	xml_char_map[0x1163] |= X;
	xml_char_map[0x1165] |= X;
	xml_char_map[0x1167] |= X;
	xml_char_map[0x1169] |= X;
	for(i = 0x116d; i <= 0x116e; i++) xml_char_map[i] |= X;
	for(i = 0x1172; i <= 0x1173; i++) xml_char_map[i] |= X;
	xml_char_map[0x1175] |= X;
	xml_char_map[0x119e] |= X;
	xml_char_map[0x11a8] |= X;
	xml_char_map[0x11ab] |= X;
	for(i = 0x11ae; i <= 0x11af; i++) xml_char_map[i] |= X;
	for(i = 0x11b7; i <= 0x11b8; i++) xml_char_map[i] |= X;
	xml_char_map[0x11ba] |= X;
	for(i = 0x11bc; i <= 0x11c2; i++) xml_char_map[i] |= X;
	xml_char_map[0x11eb] |= X;
	xml_char_map[0x11f0] |= X;
	xml_char_map[0x11f9] |= X;
	for(i = 0x1e00; i <= 0x1e9b; i++) xml_char_map[i] |= X;
	for(i = 0x1ea0; i <= 0x1ef9; i++) xml_char_map[i] |= X;
	for(i = 0x1f00; i <= 0x1f15; i++) xml_char_map[i] |= X;
	for(i = 0x1f18; i <= 0x1f1d; i++) xml_char_map[i] |= X;
	for(i = 0x1f20; i <= 0x1f45; i++) xml_char_map[i] |= X;
	for(i = 0x1f48; i <= 0x1f4d; i++) xml_char_map[i] |= X;
	for(i = 0x1f50; i <= 0x1f57; i++) xml_char_map[i] |= X;
	xml_char_map[0x1f59] |= X;
	xml_char_map[0x1f5b] |= X;
	xml_char_map[0x1f5d] |= X;
	for(i = 0x1f5f; i <= 0x1f7d; i++) xml_char_map[i] |= X;
	for(i = 0x1f80; i <= 0x1fb4; i++) xml_char_map[i] |= X;
	for(i = 0x1fb6; i <= 0x1fbc; i++) xml_char_map[i] |= X;
	xml_char_map[0x1fbe] |= X;
	for(i = 0x1fc2; i <= 0x1fc4; i++) xml_char_map[i] |= X;
	for(i = 0x1fc6; i <= 0x1fcc; i++) xml_char_map[i] |= X;
	for(i = 0x1fd0; i <= 0x1fd3; i++) xml_char_map[i] |= X;
	for(i = 0x1fd6; i <= 0x1fdb; i++) xml_char_map[i] |= X;
	for(i = 0x1fe0; i <= 0x1fec; i++) xml_char_map[i] |= X;
	for(i = 0x1ff2; i <= 0x1ff4; i++) xml_char_map[i] |= X;
	for(i = 0x1ff6; i <= 0x1ffc; i++) xml_char_map[i] |= X;
	xml_char_map[0x2126] |= X;
	for(i = 0x212a; i <= 0x212b; i++) xml_char_map[i] |= X;
	xml_char_map[0x212e] |= X;
	for(i = 0x2180; i <= 0x2182; i++) xml_char_map[i] |= X;
	for(i = 0x3041; i <= 0x3094; i++) xml_char_map[i] |= X;
	for(i = 0x30a1; i <= 0x30fa; i++) xml_char_map[i] |= X;
	for(i = 0x3105; i <= 0x312c; i++) xml_char_map[i] |= X;
	for(i = 0xac00; i <= 0xd7a3; i++) xml_char_map[i] |= X;

	/* CombiningChar */

#undef X
#define X xml_namechar

	for(i = 0x0300; i <= 0x0345; i++) xml_char_map[i] |= X;
	for(i = 0x0360; i <= 0x0361; i++) xml_char_map[i] |= X;
	for(i = 0x0483; i <= 0x0486; i++) xml_char_map[i] |= X;
	for(i = 0x0591; i <= 0x05a1; i++) xml_char_map[i] |= X;
	for(i = 0x05a3; i <= 0x05b9; i++) xml_char_map[i] |= X;
	for(i = 0x05bb; i <= 0x05bd; i++) xml_char_map[i] |= X;
	xml_char_map[0x05bf] |= X;
	for(i = 0x05c1; i <= 0x05c2; i++) xml_char_map[i] |= X;
	xml_char_map[0x05c4] |= X;
	for(i = 0x064b; i <= 0x0652; i++) xml_char_map[i] |= X;
	xml_char_map[0x0670] |= X;
	for(i = 0x06d6; i <= 0x06dc; i++) xml_char_map[i] |= X;
	for(i = 0x06dd; i <= 0x06df; i++) xml_char_map[i] |= X;
	for(i = 0x06e0; i <= 0x06e4; i++) xml_char_map[i] |= X;
	for(i = 0x06e7; i <= 0x06e8; i++) xml_char_map[i] |= X;
	for(i = 0x06ea; i <= 0x06ed; i++) xml_char_map[i] |= X;
	for(i = 0x0901; i <= 0x0903; i++) xml_char_map[i] |= X;
	xml_char_map[0x093c] |= X;
	for(i = 0x093e; i <= 0x094c; i++) xml_char_map[i] |= X;
	xml_char_map[0x094d] |= X;
	for(i = 0x0951; i <= 0x0954; i++) xml_char_map[i] |= X;
	for(i = 0x0962; i <= 0x0963; i++) xml_char_map[i] |= X;
	for(i = 0x0981; i <= 0x0983; i++) xml_char_map[i] |= X;
	xml_char_map[0x09bc] |= X;
	xml_char_map[0x09be] |= X;
	xml_char_map[0x09bf] |= X;
	for(i = 0x09c0; i <= 0x09c4; i++) xml_char_map[i] |= X;
	for(i = 0x09c7; i <= 0x09c8; i++) xml_char_map[i] |= X;
	for(i = 0x09cb; i <= 0x09cd; i++) xml_char_map[i] |= X;
	xml_char_map[0x09d7] |= X;
	for(i = 0x09e2; i <= 0x09e3; i++) xml_char_map[i] |= X;
	xml_char_map[0x0a02] |= X;
	xml_char_map[0x0a3c] |= X;
	xml_char_map[0x0a3e] |= X;
	xml_char_map[0x0a3f] |= X;
	for(i = 0x0a40; i <= 0x0a42; i++) xml_char_map[i] |= X;
	for(i = 0x0a47; i <= 0x0a48; i++) xml_char_map[i] |= X;
	for(i = 0x0a4b; i <= 0x0a4d; i++) xml_char_map[i] |= X;
	for(i = 0x0a70; i <= 0x0a71; i++) xml_char_map[i] |= X;
	for(i = 0x0a81; i <= 0x0a83; i++) xml_char_map[i] |= X;
	xml_char_map[0x0abc] |= X;
	for(i = 0x0abe; i <= 0x0ac5; i++) xml_char_map[i] |= X;
	for(i = 0x0ac7; i <= 0x0ac9; i++) xml_char_map[i] |= X;
	for(i = 0x0acb; i <= 0x0acd; i++) xml_char_map[i] |= X;
	for(i = 0x0b01; i <= 0x0b03; i++) xml_char_map[i] |= X;
	xml_char_map[0x0b3c] |= X;
	for(i = 0x0b3e; i <= 0x0b43; i++) xml_char_map[i] |= X;
	for(i = 0x0b47; i <= 0x0b48; i++) xml_char_map[i] |= X;
	for(i = 0x0b4b; i <= 0x0b4d; i++) xml_char_map[i] |= X;
	for(i = 0x0b56; i <= 0x0b57; i++) xml_char_map[i] |= X;
	for(i = 0x0b82; i <= 0x0b83; i++) xml_char_map[i] |= X;
	for(i = 0x0bbe; i <= 0x0bc2; i++) xml_char_map[i] |= X;
	for(i = 0x0bc6; i <= 0x0bc8; i++) xml_char_map[i] |= X;
	for(i = 0x0bca; i <= 0x0bcd; i++) xml_char_map[i] |= X;
	xml_char_map[0x0bd7] |= X;
	for(i = 0x0c01; i <= 0x0c03; i++) xml_char_map[i] |= X;
	for(i = 0x0c3e; i <= 0x0c44; i++) xml_char_map[i] |= X;
	for(i = 0x0c46; i <= 0x0c48; i++) xml_char_map[i] |= X;
	for(i = 0x0c4a; i <= 0x0c4d; i++) xml_char_map[i] |= X;
	for(i = 0x0c55; i <= 0x0c56; i++) xml_char_map[i] |= X;
	for(i = 0x0c82; i <= 0x0c83; i++) xml_char_map[i] |= X;
	for(i = 0x0cbe; i <= 0x0cc4; i++) xml_char_map[i] |= X;
	for(i = 0x0cc6; i <= 0x0cc8; i++) xml_char_map[i] |= X;
	for(i = 0x0cca; i <= 0x0ccd; i++) xml_char_map[i] |= X;
	for(i = 0x0cd5; i <= 0x0cd6; i++) xml_char_map[i] |= X;
	for(i = 0x0d02; i <= 0x0d03; i++) xml_char_map[i] |= X;
	for(i = 0x0d3e; i <= 0x0d43; i++) xml_char_map[i] |= X;
	for(i = 0x0d46; i <= 0x0d48; i++) xml_char_map[i] |= X;
	for(i = 0x0d4a; i <= 0x0d4d; i++) xml_char_map[i] |= X;
	xml_char_map[0x0d57] |= X;
	xml_char_map[0x0e31] |= X;
	for(i = 0x0e34; i <= 0x0e3a; i++) xml_char_map[i] |= X;
	for(i = 0x0e47; i <= 0x0e4e; i++) xml_char_map[i] |= X;
	xml_char_map[0x0eb1] |= X;
	for(i = 0x0eb4; i <= 0x0eb9; i++) xml_char_map[i] |= X;
	for(i = 0x0ebb; i <= 0x0ebc; i++) xml_char_map[i] |= X;
	for(i = 0x0ec8; i <= 0x0ecd; i++) xml_char_map[i] |= X;
	for(i = 0x0f18; i <= 0x0f19; i++) xml_char_map[i] |= X;
	xml_char_map[0x0f35] |= X;
	xml_char_map[0x0f37] |= X;
	xml_char_map[0x0f39] |= X;
	xml_char_map[0x0f3e] |= X;
	xml_char_map[0x0f3f] |= X;
	for(i = 0x0f71; i <= 0x0f84; i++) xml_char_map[i] |= X;
	for(i = 0x0f86; i <= 0x0f8b; i++) xml_char_map[i] |= X;
	for(i = 0x0f90; i <= 0x0f95; i++) xml_char_map[i] |= X;
	xml_char_map[0x0f97] |= X;
	for(i = 0x0f99; i <= 0x0fad; i++) xml_char_map[i] |= X;
	for(i = 0x0fb1; i <= 0x0fb7; i++) xml_char_map[i] |= X;
	xml_char_map[0x0fb9] |= X;
	for(i = 0x20d0; i <= 0x20dc; i++) xml_char_map[i] |= X;
	xml_char_map[0x20e1] |= X;
	for(i = 0x302a; i <= 0x302f; i++) xml_char_map[i] |= X;
	xml_char_map[0x3099] |= X;
	xml_char_map[0x309a] |= X;


	/* Digit */

#undef X
#define X xml_namechar

	for(i = 0x0660; i <= 0x0669; i++) xml_char_map[i] |= X;
	for(i = 0x06f0; i <= 0x06f9; i++) xml_char_map[i] |= X;
	for(i = 0x0966; i <= 0x096f; i++) xml_char_map[i] |= X;
	for(i = 0x09e6; i <= 0x09ef; i++) xml_char_map[i] |= X;
	for(i = 0x0a66; i <= 0x0a6f; i++) xml_char_map[i] |= X;
	for(i = 0x0ae6; i <= 0x0aef; i++) xml_char_map[i] |= X;
	for(i = 0x0b66; i <= 0x0b6f; i++) xml_char_map[i] |= X;
	for(i = 0x0be7; i <= 0x0bef; i++) xml_char_map[i] |= X;
	for(i = 0x0c66; i <= 0x0c6f; i++) xml_char_map[i] |= X;
	for(i = 0x0ce6; i <= 0x0cef; i++) xml_char_map[i] |= X;
	for(i = 0x0d66; i <= 0x0d6f; i++) xml_char_map[i] |= X;
	for(i = 0x0e50; i <= 0x0e59; i++) xml_char_map[i] |= X;
	for(i = 0x0ed0; i <= 0x0ed9; i++) xml_char_map[i] |= X;
	for(i = 0x0f20; i <= 0x0f29; i++) xml_char_map[i] |= X;


	/* Extender */

#undef X
#define X xml_namechar

	xml_char_map[0x02d0] |= X;
	xml_char_map[0x02d1] |= X;
	xml_char_map[0x0387] |= X;
	xml_char_map[0x0640] |= X;
	xml_char_map[0x0e46] |= X;
	xml_char_map[0x0ec6] |= X;
	xml_char_map[0x3005] |= X;
	for(i = 0x3031; i <= 0x3035; i++) xml_char_map[i] |= X;
	for(i = 0x309d; i <= 0x309e; i++) xml_char_map[i] |= X;
	for(i = 0x30fc; i <= 0x30fe; i++) xml_char_map[i] |= X;

	/* Ideographic */

#undef X
#define X (xml_namestart | xml_namechar)

	for(i = 0x4e00; i <= 0x9fa5; i++) xml_char_map[i] |= X;
	xml_char_map[0x3007] |= X;
	for(i = 0x3021; i <= 0x3029; i++) xml_char_map[i] |= X;

#undef X

#endif
}

static void init_xml_chartypes_11(void)
{
#if CHAR_SIZE == 16

	int i;

	for(i = 0; i < 1 << CHAR_SIZE; i++)
	xml_char_map_11[i] = 0;

	/* Legal ASCII characters */

	xml_char_map_11['\t'] |= xml_legal;
	xml_char_map_11['\r'] |= xml_legal;
	xml_char_map_11['\n'] |= xml_legal;
	for(i = ' '; i <= 126; i++)	/* not DEL in 1.1 */
	xml_char_map_11[i] |= xml_legal;

	/* ASCII Letters */

	for(i = 'a'; i <= 'z'; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 'A'; i <= 'Z'; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);

	/* ASCII Digits */

	for(i = '0'; i <= '9'; i++)
	xml_char_map_11[i] |= xml_namechar;

	/* ASCII Whitespace */

	xml_char_map_11[' '] |= xml_whitespace;
	xml_char_map_11['\t'] |= xml_whitespace;
	xml_char_map_11['\r'] |= xml_whitespace;
	xml_char_map_11['\n'] |= xml_whitespace;

	/* ASCII Others */

	xml_char_map_11['_'] |= (xml_namestart | xml_namechar);
	xml_char_map_11[':'] |= (xml_namestart | xml_namechar);

	xml_char_map_11['.'] |= xml_namechar;
	xml_char_map_11['-'] |= xml_namechar;

	/* Legal Unicode characters */

	xml_char_map_11[0x85] |= xml_legal;

	for(i = 0xa0; i <= 0xd7ff; i++)
	xml_char_map_11[i] |= xml_legal;
	for(i = 0xe000; i <= 0xfffd; i++)
	xml_char_map_11[i] |= xml_legal;

	/* Name characters from productions 4 and 4a */

	xml_char_map_11[0xb7] |= xml_namechar;
	for(i = 0xc0; i <= 0xd6; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0xd8; i <= 0xf6; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0xf8; i <= 0x2ff; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0x300; i <= 0x36f; i++)
	xml_char_map_11[i] |= xml_namechar;
	for(i = 0x370; i <= 0x037d; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0x37f; i <= 0x01fff; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0x200c; i <= 0x200d; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0x203f; i <= 0x2040; i++)
	xml_char_map_11[i] |= xml_namechar;
	for(i = 0x2070; i <= 0x218f; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0x2c00; i <= 0x2fef; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0x3001; i <= 0xd7ff; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0xf900; i <= 0xfdcf; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	for(i = 0xfdf0; i <= 0xfffd; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);

	/* Allow both 0x10000 - 0xeffff and the corresponding surrogates in
	   names, since we actually test UTF-16 codes rather than Unicode
	   code points.   If the surrogates weren't legally paired, they
	   would already have been rejected.
	*/

	for(i = 0x1; i <= 0xe; i++)
	xml_char_map_11[i] |= xml_nameblock;

	/* All low surrogates */
	for(i = 0xdc00; i <= 0xdfff; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);
	/* High surrogates corresponding to codes up to 0xeffff */
	for(i = 0xd800; i <= 0xdb7f; i++)
	xml_char_map_11[i] |= (xml_namestart | xml_namechar);

#endif
}
