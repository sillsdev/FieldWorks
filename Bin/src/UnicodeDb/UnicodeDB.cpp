/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: UnicodeDB.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains a stand-alone program, a console application, which writes the
	database file required by LgCharacterPropertyEngine, by reading data from the
	UnicodeData.txt, SpecialCasing.txt and LineBreak.txt files, all provided by the Unicode
	consortium.
	This version is designed to read the files as of version 3.0 of the standard.

	To run the program, give it a command line like
		UnicodeDB UnicodeData.txt SpecialCasing.txt LineBreak.txt
	The file names are of course either absolute or relative to the working directory you
	have set.
	The output header file LgCharPropertyInit.h must be moved or copied to the same directory
	as LgCharacterPropertyEngine.cpp.
----------------------------------------------------------------------------------------------*/
// UnicodeDB main file

#include <fstream>
#include <cstdlib>
#include <iostream>
#include "main.h"

#ifndef BOOL
typedef int BOOL;
#endif

typedef enum
{
	kccLu, // = Letter, Uppercase
	kccLl, // = Letter, Lowercase
	kccLt, // = Letter, Titlecase
	kccLm, // = Letter, Modifier
	kccLo, // = Letter, Other

	kccMn, // = Mark, Non-Spacing
	kccMc, // = Mark, Spacing Combining
	kccMe, // = Mark, Enclosing

	kccNd, // = Number, Decimal Digit
	kccNl, // = Number, Letter
	kccNo, // = Number, Other

	kccZs, // = Separator, Space
	kccZl, // = Separator, Line
	kccZp, // = Separator, Paragraph

	kccCc, // = Other, Control
	kccCf, // = Other, Format
	kccCs, // = Other, Surrogate
	kccCo, // = Other, Private Use
	kccCn, // = Other, Not Assigned

	kccPc, // = Punctuation, Connector
	kccPd, // = Punctuation, Dash
	kccPs, // = Punctuation, Open
	kccPe, // = Punctuation, Close
	kccPi, // = Punctuation, Initial quote (may behave like Ps or Pe depending
		   //				  on usage)
	kccPf, // = Punctuation, Final quote (may behave like Ps or Pe depending
		   //				  on usage)
	kccPo, // = Punctuation, Other

	kccSm, // = Symbol, Math
	kccSc, // = Symbol, Currency
	kccSk, // = Symbol, Modifier
	kccSo, // = Symbol, Other
} LgChGenCat;  // Hungarian: gc

// Bidirectional Categories
typedef enum
{
	// Strong
	kbicL,   // Left-Right; most alphabetic chars, etc.
	kbicLRE, // Left-Right Embedding
	kbicLRO, // Left-Right Override
	kbicR,   // Right-Left; Hebrew and its punctuation
	kbicAL,  // Right-Left Arabic
	kbicRLE, // Right-Left Embedding
	kbicRLO, // Right-Left Override
	kbicPDF, // Pop Directional Format

	// Weak
	kbicEN,  //	European Number
	kbicES,  //	European Number Separator
	kbicET,  //	European Number Terminator
	kbicAN,  //	Arabic Number
	kbicCS,  //	Common Number Separator

	// Separators:
	kbicNSM, // Non-Spacing Mark
	kbicBN,  // Boundary Neutral
	kbicB,   //	Paragraph Separator
	kbicS,   //	Segment Separator

	// Neutrals:
	kbicWS,  //	Whitespace
	kbicON,  //	Other Neutrals ; All other characters: punctuation, symbols
} LgBidiCat;  // Hungarian: bic

// Line Breaking Properties
typedef enum
{
			 // '*' indicates normative property
	klbpAI,  //   Ambiguous (Alphabetic or Ideograph)
	klbpAL,  //   Ordinary Alphabetic
	klbpB2,  //   Break Opportunity Before and After
	klbpBA,  //   Break Opportunity After
	klbpBB,  //   Break Opportunity Before
	klbpBK,  // * Mandatory Break
	klbpCB,  // * Contingent Break Opportunity
	klbpCL,  //   Closing
	klbpCM,  // * Combining Marks
	klbpCR,  // * Carriage Return
	klbpEX,  //   Exclamation
	klbpGL,  // * Non-breaking ("Glue")
	klbpHY,  //   Hyphen
	klbpID,  //   Ideographic
	klbpIN,  //   Inseparable
	klbpIS,  //   Infix Separator (Numeric)
	klbpLF,  // * Line Feed
	klbpNS,  //   Non Starter
	klbpNU,  //   Numeric
	klbpOP,  //   Opening
	klbpPO,  //   Postfix (Numeric)
	klbpPR,  //   Prefix (Numeric)
	klbpQU,  //   Ambiguous Quotation
	klbpSA,  //   Complex Context
	klbpSG,  // * Surrogates
	klbpSP,  // * Space
	klbpSY,  //   Symbols Allowing Breaks
	klbpXX,  //   Unknown (used for unassigned characters)
	klbpZW   // * Zero Width Space
} LgLBP;  // Hungarian lbp

// Codes for whole-number values
typedef enum {
	klcpfNoNumericVal = 63, // Digit field when no numeric value
	klcpfFraction = 62,     // Special case fractional value.
	klcpfFifty = 21,        // Values 0-20 occur; also the values here...
	klcpfHundred = 22,
	klcpfFiveHundred = 23,
	klcpfThousand = 24,
	klcpfFiveThousand = 25,
	klcpfTenThousand = 26,
	klcpfThirty = 27,
	klcpfForty = 28,
	klcpfSixty = 29,
	klcpfSeventy = 30,
	klcpfEighty = 31,
	klcpfNinety = 32
} LgChPrNumVals;    // Hungarian lcpv

// For the coded CC data from the bit field we need a conversion to the actual CC values.
// This is implemented using a 15-element byte array to which the bitfield value provides
// an index. Note that the bitfield value 15 itself is an indication that the EXTRA table
// should be used.
byte rgbCombClass[15] = {0, 230, 220, 9, 1, 7, 130, 107, 122, 202, 222, 228, 232, 8, 103};

/*
------------------------------------------------------------------------------------------------
| 31    30    29    28    27    26    25    24    23    22    21    20    19    18    17    16
|      S p a r e        |UnAss|   Line Breaking Property    |    Combining Class    |SCase| Bi-
------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------
  15    14    13    12    11    10     9     8     7     6     5     4     3     2     1     0 |
 directional Category   |         Whole number property     |Mirror|      General Category     |
------------------------------------------------------------------------------------------------

*/
// Structure with bit fields used to compress several Unicode properties. 32 bits in all.
struct LgCharPropsFlds {
	LgChGenCat    gc         : 5;    // General Category
	bool          fmirrored  : 1;    // Mirrored
	uint          nv         : 6;    // Whole number property (excludes fractions)
	LgBidiCat     bidi       : 5;    // Bidirectional category
	bool          fspecialC  : 1;    // Special casing flag
	uint          ucC        : 4;    // Combining class (value 15 indicates table look-up)
	LgLBP         lbp        : 5;    // Line Breaking Property
	bool          fnotAssign : 1;    // Flag indicating the character is not assigned in Unicode
	bool                     : 4;    // Spare
};

class Processor
{
protected:
	std::ifstream m_strmIn;
	std::ifstream m_strmIn2; // specialCasing
	std::ifstream m_strmIn3; // Line breaking properties
	std::ofstream m_strmOut;
	wchar_t m_chCodePoint; // of current line
	char m_rgchName[256]; // should be enough for any char name!
	LgChGenCat m_gc;
	int m_nCombClass;
	LgBidiCat m_bic;
	int m_cchDecomp; // Count of decomposition chars
	wchar_t m_rgchwDecomp[100]; // Actual decomposition
	BOOL m_fHasDecimal;
	int m_nDecimalVal;
	BOOL m_fHasDigit;
	int m_nDigitVal;
	BOOL m_fHasNumeric;
	int m_nNumeratorVal;
	BOOL m_fHasDenom;
	int m_nDenomVal;
	BOOL m_fMirrored;
	BOOL m_fHasUpper;
	wchar_t m_chUpper;
	BOOL m_fHasLower;
	wchar_t m_chLower;
	BOOL m_fHasTitle;
	wchar_t m_chTitle;
	wchar_t m_chOtherCase; // regular other case, computed by MakeBitField
	LgLBP m_uLBP; // Line Breaking Property

	int m_cbNamePage; // Count of number of chars used for character names on page.

	int m_chLast;  // chCodePoint for last line read

	LgCharPropsFlds m_suBitField; // for interpretation see LgCharPropFields
	LgCharPropsFlds m_suBitFieldUnassigned; // for dummy entries

	//-----------------------------------------------------------------------
	// The following variables match almost exactly the variables in
	// LgCharacterPropertyEngine. The only difference is that arrays which are
	// allocated in the engine are pre-declared here.
	// For simplicity in making the code match, we use the same exact variable
	// names, although this means we have 'prgch' prefixes where we ought to
	// have rgch ones.

#define MAXBITFIELDS 15000 // number of distinct character entries we can handle.
							// the current file needs 12500.
	int m_csuBitFields;
	// These tables give the main Unicode properties we are interested in.
	// A special entry, always at index 0, gives properties for unknown characters
	LgCharPropsFlds m_prgsuBitFields[MAXBITFIELDS]; // Bit fields as in LgCharPropsFlds

	// This table gives an index into m_prgsuBitFields based on the most significant
	// byte of the Unicode character we want info about.
	// The info about a character with given msb and lsb is typically at
	// m_prgsuBitFields[m_rgisuMinPage[msb] + lsb].
	// This allows us to reuse the same entries for pages with the same properties.
	unsigned short m_rgisuMinPage[256];

	// This table gives the number of character entries for a given page - 1.
	// (The -1 allows us to indicate we have 256 characters using the maximum
	// unsigned byte, 255; we don't need 0, because we never have 0 entries
	// for a page, always at least 1.)
	// If a character's lsb > m_rgcchPage[msb], then its data is found at
	// m_prgsuBitFields[m_rgisuMinPage[msb] + m_rgcchPage[msb]]
	// This allows us to take advantage of pages where many or all characters are
	// the same, and also to truncate pages where not all 256 characters are valid.
	// In the latter case, we just add one dummy character with the appropriate
	// characteristics for "not a character" and let it stand for the rest of the page.
	byte m_rgcchPage[256];

	// Other case table. This is indexed the same as m_prgsuBitFields and has the same
	// number or entries. Each gives the lsb of the other case equivalent of the
	// character in question. (Case of msb of other case character being different
	// is specially handled).
	byte m_prgbOtherCase[MAXBITFIELDS];

	// Special case information.
	// This array consists of variable-length records. Each record starts with
	// the character to be converted.
	// Most commonly this is followed with a single character, the only conversion
	// required for the base character. For example, if the base is lower case,
	// the following character is its title and upper case equivalent. If upper case,
	// its title case is the same and its lower case is the one following character.
	// Special cases:
	//	-- following ch is 0001: The base character has two differnt alternates,
	//		which follow. For example, if lower case, its upper and title case
	//		equivalents follow. If upper, its lower and title case equivalents follow
	//		If title, its upper and lower case equivalents follow.
	//  -- following ch is 0002. The base character has a conversion that involves
	//		more than one character. The two conversions follow, each null-terminated.
#define MAXSPECIALCASE 2000 // Currently about 1000 needed
	int m_cchSpecialCaseTableSize;
	OLECHAR m_prgchSpecialCaseTable[MAXSPECIALCASE];

	// Combining class information.
#define MAXCCEXTRA 60 // Extra Combining Class values which can't be stored in the 4-bit field.
					  // Unicode 3.0 needs 42
	int m_ccCExtra; // count of extra CC values.
	// Lower 16 bits are the code value; higher 16 bits contain the Combining Class value.
	// Note that the actual values are all less than 256.
	uint m_prguCombClassExtra[MAXCCEXTRA];

	// Character name info.
	char m_rgchsNameData[400000]; // Currently need about 200K
	char * m_pchsNameData; // place to put next data in rgchsNameData
	unsigned int m_rgiNames[MAXBITFIELDS]; // indexes into rgchsNameData

	// Character decomposition info
	// In Unicode 3.0, 3483 characters have decompositions.
	// The decompositions take up 6055 characters.
	// The worst page (33) has 703 decomposition characters. Page f9 has a (one-char)
	// decomposition for every character.
	// There are 1470 1-char decomps, 1572 2-char, 360 3-char, 62 4-char, 15 5-char, 2 6-char,
	// 1 8-char and 1 18-char (U+FDFA).
	// Based on this, we have come up with the following design, which occupies about 14K
	// of RAM.
	// m_pchDecompositions contains the concatenation of all the decompositions.
	// Each 16-bit quantity uses 13 bits for an index into the decompositions array,
	// and three bits to indicate a length. 0-6 encoded directly; 7 => null termination
#define MAXDECOMP 10000 // Currently 6057
	int m_cchDecompositions; // TODO JohnL: should this be m_pch... as in above comment?
	OLECHAR m_prgchDecompositions[MAXDECOMP];
	unsigned short m_prgsuDecompIndex[MAXBITFIELDS];

	// This data is not yet in LgCharacterPropertyEngine as we don't have interface
	// that demands the fractions. Keep it here for now in case we need it.
#define MAX_FRACTIONS 100
	wchar_t m_rgchFractionChars[MAX_FRACTIONS];
	unsigned short m_rgsuNumerators[MAX_FRACTIONS];
	unsigned short m_rgsuDenominators[MAX_FRACTIONS];
	int m_cFractions;


public:
	// discard input until you have read the specified character
	void SkipTo(char c)
	{
		SkipTo(m_strmIn, c);
	}

	void SkipTo(std::istream & strm, char c)
	{
		char cIn;
		strm.get(cIn);
		while(cIn != c && !strm.eof())
			strm.get(cIn);
	}

	void GetCategory(char c1, char c2)
	{
		switch(c1)
		{
		case 'L':
			switch(c2)
			{
			case 'u':
				m_gc = kccLu;
				break;
			case 'l':
				m_gc = kccLl;
				break;
			case 't':
				m_gc = kccLt;
				break;
			case 'm':
				m_gc = kccLm;
				break;
			case 'o':
				m_gc = kccLo;
				break;
			default:
				goto error;
			}
			break;
		case 'M':
			switch(c2)
			{
			case 'n':
				m_gc = kccMn;
				break;
			case 'c':
				m_gc = kccMc;
				break;
			case 'e':
				m_gc = kccMe;
				break;
			default:
				goto error;
			}
			break;
		case 'N':
			switch(c2)
			{
			case 'd':
				m_gc = kccNd;
				break;
			case 'l':
				m_gc = kccNl;
				break;
			case 'o':
				m_gc = kccNo;
				break;
			default:
				goto error;
			}
			break;
		case 'Z':
			switch(c2)
			{
			case 's':
				m_gc = kccZs;
				break;
			case 'l':
				m_gc = kccZl;
				break;
			case 'p':
				m_gc = kccZp;
				break;
			default:
				goto error;
			}
			break;
		case 'C':
			switch(c2)
			{
			case 'c':
				m_gc = kccCc;
				break;
			case 'f':
				m_gc = kccCf;
				break;
			case 's':
				m_gc = kccCs;
				break;
			case 'o':
				m_gc = kccCo;
				break;
			case 'n':
				m_gc = kccCn;
				break;
			default:
				goto error;
			}
			break;
		case 'P':
			switch(c2)
			{
			case 'c':
				m_gc = kccPc;
				break;
			case 'd':
				m_gc = kccPd;
				break;
			case 's':
				m_gc = kccPs;
				break;
			case 'e':
				m_gc = kccPe;
				break;
			case 'i':
				m_gc = kccPi;
				break;
			case 'f':
				m_gc = kccPf;
				break;
			case 'o':
				m_gc = kccPo;
				break;
			default:
				goto error;
			}
			break;
		case 'S':
			switch(c2)
			{
			case 'm':
				m_gc = kccSm;
				break;
			case 'c':
				m_gc = kccSc;
				break;
			case 'k':
				m_gc = kccSk;
				break;
			case 'o':
				m_gc = kccSo;
				break;
			default:
				goto error;
			}
			break;
		default:
			goto error;
		}
		return;
error:
		printf("Bad General Category %c%c", c1, c2);
		int ch;
		std::cin >> ch;
		exit(1);

	}

	void FatalError(char * problem)
	{
		printf(problem);
		int ch;
		std::cin >> ch;
		exit(1);
	}

/*----------------------------------------------------------------------------------------------
	Reads the bidirectional field and converts to enumerations.
	Assumes that the field is never null (which is true).
----------------------------------------------------------------------------------------------*/
	void ReadBIDI()
	{
		char c1;
		char c2;
		char c3 = 0;

		m_strmIn >> c1 >> c2;
		if (c2 != ';')
		{
			m_strmIn >> c3;
			if (c3 != ';')
				SkipTo(';');
		}
		switch(c1)
		{
		case 'L':
			switch(c2)
			{
			case ';':
				m_bic = kbicL;
				break;
			case 'R':
				switch(c3)
				{
				case 'E':
					m_bic = kbicLRE;
					break;
				case 'O':
					m_bic = kbicLRO;
					break;
				default:
					goto error;
				}
				break;
			default:
				goto error;
			}
			break;
		case 'R':
			switch(c2)
			{
			case ';':
				m_bic = kbicR;
				break;
			case 'L':
				switch(c3)
				{
				case 'E':
					m_bic = kbicRLE;
					break;
				case 'O':
					m_bic = kbicRLO;
					break;
				default:
					goto error;
				}
				break;
			default:
				goto error;
			}
			break;
		case 'E':
			switch(c2)
			{
			case 'N':
				m_bic = kbicEN;
				break;
			case 'S':
				m_bic = kbicES;
				break;
			case 'T':
				m_bic = kbicET;
				break;
			default:
				goto error;
			}
			break;
		case 'A':
			switch(c2)
			{
			case 'N':
				m_bic = kbicAN;
				break;
			case 'L':
				m_bic = kbicAL;
				break;
			default:
				goto error;
			}
			break;
		case 'C':
			switch(c2)
			{
			case 'S':
				m_bic = kbicCS;
				break;
			default:
				goto error;
			}
			break;
		case 'B':
			switch(c2)
			{
			case ';':
				m_bic = kbicB;
				break;
			case 'N':
				m_bic = kbicBN;
				break;
			default:
				goto error;
			}
			break;
		case 'S':
			switch(c2)
			{
			case ';':
				m_bic = kbicS;
				break;
			default:
				goto error;
			}
			break;
		case 'W':
			switch(c2)
			{
			case 'S':
				m_bic = kbicWS;
				break;
			default:
				goto error;
			}
			break;
		case 'O':
			switch(c2)
			{
			case 'N':
				m_bic = kbicON;
				break;
			default:
				goto error;
			}
			break;
		case 'P':
			switch(c2)
			{
			case 'D':
				switch(c3)
				{
				case 'F':
					m_bic = kbicPDF;
					break;
				default:
					goto error;
				}
				break;
			default:
				goto error;
			}
			break;
		case 'N':
			switch(c2)
			{
			case 'S':
				switch(c3)
				{
				case 'M':
					m_bic = kbicNSM;
					break;
				default:
					goto error;
				}
				break;
			default:
				goto error;
			}
			break;
		default:
			goto error;
		}
		return;
error:
		printf("Bad Bidirectional Category %c%c", c1, c2);
		exit(1);

	}

	void ReadLBP()
	{
		char c1;
		char c2;

		m_strmIn3 >> c1 >> c2;
		if (c2 != ';') // C2 shouldn't be ';' in Unicode 3 because all LBPs are 2 characters
			SkipTo(m_strmIn3, ';');
		switch(c1)
		{
		case 'A':
			switch(c2)
			{
			case 'I':
				m_uLBP = klbpAI;
				break;
			case 'L':
				m_uLBP = klbpAL;
				break;
			default: goto error;
			}
			break;
		case 'B':
			switch(c2)
			{
			case '2':
				m_uLBP = klbpB2;
				break;
			case 'A':
				m_uLBP = klbpBA;
				break;
			case 'B':
				m_uLBP = klbpBB;
				break;
			case 'K':
				m_uLBP = klbpBK;
				break;
			default: goto error;
			}
			break;
		case 'C':
			switch(c2)
			{
			case 'B':
				m_uLBP = klbpCB;
				break;
			case 'L':
				m_uLBP = klbpCL;
				break;
			case 'M':
				m_uLBP = klbpCM;
				break;
			case 'R':
				m_uLBP = klbpCR;
				break;
			default: goto error;
			}
			break;
		case 'E':
			switch(c2)
			{
			case 'X':
				m_uLBP = klbpEX;
				break;
			default: goto error;
			}
			break;
		case 'G':
			switch(c2)
			{
			case 'L':
				m_uLBP = klbpGL;
				break;
			default: goto error;
			}
			break;
		case 'H':
			switch(c2)
			{
			case 'Y':
				m_uLBP = klbpHY;
				break;
			default: goto error;
			}
			break;
		case 'I':
			switch(c2)
			{
			case 'D':
				m_uLBP = klbpID;
				break;
			case 'N':
				m_uLBP = klbpIN;
				break;
			case 'S':
				m_uLBP = klbpIS;
				break;
			default: goto error;
			}
			break;
		case 'L':
			switch(c2)
			{
			case 'F':
				m_uLBP = klbpLF;
				break;
			default: goto error;
			}
			break;
		case 'N':
			switch(c2)
			{
			case 'S':
				m_uLBP = klbpNS;
				break;
			case 'U':
				m_uLBP = klbpNU;
				break;
			default: goto error;
			}
			break;
		case 'O':
			switch(c2)
			{
			case 'P':
				m_uLBP = klbpOP;
				break;
			default: goto error;
			}
			break;
		case 'P':
			switch(c2)
			{
			case 'O':
				m_uLBP = klbpPO;
				break;
			case 'R':
				m_uLBP = klbpPR;
				break;
			default: goto error;
			}
			break;
		case 'Q':
			switch(c2)
			{
			case 'U':
				m_uLBP = klbpQU;
				break;
			default: goto error;
			}
			break;
		case 'S':
			switch(c2)
			{
			case 'A':
				m_uLBP = klbpSA;
				break;
			case 'G':
				m_uLBP = klbpSG;
				break;
			case 'P':
				m_uLBP = klbpSP;
				break;
			case 'Y':
				m_uLBP = klbpSY;
				break;
			default: goto error;
			}
			break;
		case 'X':
			switch(c2)
			{
			case 'X':
				m_uLBP = klbpXX;
				break;
			default: goto error;
			}
			break;
		case 'Z':
			switch(c2)
			{
			case 'W':
				m_uLBP = klbpZW;
				break;
			default: goto error;
			}
			break;
		default:
			goto error;
		}
		return;
error:
		printf("Bad Line Break Property %c%c", c1, c2);
		exit(1);
	}



	uint GetCCCode(int nCC)
	{
		uint ucode = 0;
		while (ucode < 15 && nCC != rgbCombClass[ucode])
		{
			++ucode;
		}
		return ucode;
	}


	void MakeBitfieldEntry()
	{
		// Begin with a bit field initialised to unassigned.
		// Note: works initially (when m_suBitFieldUnassigned is not initialised) as most
		// fields are filled in because of the initial conditions set up by Run().
		// The fnotAssign bit and the fspecialC bit are cleared specially in Run().
		m_suBitField = m_suBitFieldUnassigned;

		// Remove the unassigned bit
		m_suBitField.fnotAssign = false;

		// General category.
		m_suBitField.gc = m_gc;

		// By default the 'other case' equivalent is the ch itself
		m_chOtherCase = m_chCodePoint;

		// special case-change behavior
		// TODO: read special file for case mappings that are not 1:1, and set this
		// bit for those also.

		// Lower case with title not the same as regular upper
		if (m_gc == kccLl && (m_chUpper != m_chTitle))
		{
			m_suBitField.fspecialC = true;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chCodePoint;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = 1;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chUpper;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chTitle;
		}

		// Upper case with title case (different from itself)
		else if (m_gc == kccLu && (m_fHasTitle))
		{
			m_suBitField.fspecialC = true;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chCodePoint;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = 1;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chLower;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chTitle;
		}

		// Title case (lower is always different from upper)
		else if (m_gc == kccLt)
		{
			m_suBitField.fspecialC = true;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chCodePoint;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = 1;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chUpper;
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chLower;
		}
		else if (m_gc == kccLu || m_gc == kccLl)
		{
			// It is a regular cased character with only one alternate case character
			if (m_fHasUpper)
				m_chOtherCase = m_chUpper;
			else if (m_fHasLower)
				m_chOtherCase = m_chLower;
			if ((m_chOtherCase & 0xff00) != (m_chCodePoint & 0xff00))
			{
				// other case is on a different code page
				m_suBitField.fspecialC = true;
				m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chCodePoint;
				m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chOtherCase;
			}
		}

		if (m_cchSpecialCaseTableSize >= MAXSPECIALCASE)
		{
			FatalError("Unicode has changed disastrously (MAXSPECIALCASE/1)");
			exit(1);
		}

		// Now store Line Breaking Property in the appropriate bitfield.
		m_suBitField.lbp = m_uLBP;

		// Bidi category
		m_suBitField.bidi = m_bic;

		// Mirrored
		m_suBitField.fmirrored = m_fMirrored;

		// Digit values
		uint nDigVal = klcpfNoNumericVal;
		if (m_fHasNumeric)
		{
			if (m_fHasDenom)
			{
				nDigVal = klcpfFraction;
				m_rgchFractionChars[m_cFractions] = m_chCodePoint;
				m_rgsuNumerators[m_cFractions] = m_nNumeratorVal;
				m_rgsuDenominators[m_cFractions++] = m_nDenomVal;
				if (m_cFractions >= MAX_FRACTIONS)
				{
					FatalError("Unicode has changed disastrously (MAXFRACTIONS)");
					exit(1);
				}
			} // has denominator
			else
			{	// simple integer value
				if ((m_fHasDecimal && m_nDecimalVal != m_nNumeratorVal) ||
					(m_fHasDigit && m_nDigitVal != m_nNumeratorVal))
				{
					// We are not set up to store different values--it doesn't make sense
					FatalError("Unicode has changed disastrously (Number/digit fields)");
					exit(1);
				}
				switch(m_nNumeratorVal) // always has this if numeric at all
				{
				case 30:
					nDigVal = klcpfThirty;
					break;
				case 40:
					nDigVal = klcpfForty;
					break;
				case 50:
					nDigVal = klcpfFifty;
					break;
				case 60:
					nDigVal = klcpfSixty;
					break;
				case 70:
					nDigVal = klcpfSeventy;
					break;
				case 80:
					nDigVal = klcpfEighty;
					break;
				case 90:
					nDigVal = klcpfNinety;
					break;
				case 100:
					nDigVal = klcpfHundred;
					break;
				case 500:
					nDigVal = klcpfFiveHundred;
					break;
				case 1000:
					nDigVal = klcpfThousand;
					break;
				case 5000:
					nDigVal = klcpfFiveThousand;
					break;
				case 10000:
					nDigVal = klcpfTenThousand;
					break;
//				case -1:
//					nDigVal = klcpfMinusOne;
//					break;
				default:
					if (m_nNumeratorVal < 0 || m_nNumeratorVal > 20)
					{
						// Can't handle other numbers outside this range
						FatalError("Unicode has changed disastrously (number out of range)");
						exit(1);
					}
					nDigVal = m_nNumeratorVal;
				}
			}
		}
		m_suBitField.nv = nDigVal;

		// Make Combining Class field, and table entry if needed.
		m_suBitField.ucC = 0;
		if (m_nCombClass)
		{
			m_suBitField.ucC = GetCCCode(m_nCombClass); // GetCCCode returns 15 if not found
			if (m_suBitField.ucC == 15)
			{
				m_prguCombClassExtra[m_ccCExtra] = m_chCodePoint | (m_nCombClass << 16);
				++m_ccCExtra;
				if (m_ccCExtra >= MAXCCEXTRA)
				{
					FatalError("Unicode has changed disastrously (MAXCCEXTRA)");
					exit(1);
				}
			}
		}
	}

	BOOL ReadRecord()
	{
		wchar_t chtemp; // temp to store code point from Line Break file, for check

		m_strmIn >> std::ws;
		if (m_strmIn.eof())
			return false;
		m_strmIn3 >> std::ws;
		if (m_strmIn3.eof())
		{
			FatalError("EOF of Line Break file encountered prematurely");
			exit(1);
		}
		// actual code point (field 0)
		m_strmIn >> std::hex >> m_chCodePoint;
		SkipTo(';');

		// case equivalents are unchanged unless we find a value
		m_chUpper = m_chLower = m_chTitle = m_chCodePoint;

		// character name (field 1)
		m_strmIn.get(m_rgchName, 256, ';');
		SkipTo(';');

		// general category (field 2)
		char c1;
		char c2;
		m_strmIn >> c1 >> c2;
		GetCategory(c1, c2);
		SkipTo(';');

		// combining class (field 3)
		m_strmIn >> std:: dec >> m_nCombClass;
		SkipTo(';');

		// BIDI category (field 4)
		ReadBIDI(); // includes skipping semicolon

		// Character decomposition mapping (field 5)
		if (m_strmIn.peek() == '<')
		{
			// some decompositions begin with a sort of comment in angle brackets
			SkipTo('>');
		}
		m_cchDecomp = 0;
		m_strmIn >> std::ws;
		while (m_strmIn.peek() != ';' && !m_strmIn.eof())
		{
			m_strmIn >> std::hex >> m_rgchwDecomp[m_cchDecomp++];
			m_strmIn >> std::ws;
		}
		SkipTo(';');

		// decimal digit value (field 6)
		m_strmIn >> std::ws;
		m_fHasDecimal = m_strmIn.peek() != ';';
		if(m_fHasDecimal)
			m_strmIn >> std::dec >> m_nDecimalVal;
		SkipTo(';');

		// digit value (field 7)
		m_strmIn >> std::ws;
		m_fHasDigit = m_strmIn.peek() != ';';
		if(m_fHasDigit)
			m_strmIn >> m_nDigitVal;
		SkipTo(';');

		// numeric value (field 8)
		m_fHasDenom = false;
		m_strmIn >> std::ws;
		m_fHasNumeric = m_strmIn.peek() != ';';
		if (m_fHasNumeric)
		{
			m_strmIn >> std::dec >> m_nNumeratorVal;
			m_strmIn >> std::ws;
			if (m_strmIn.peek() == '/')
			{
				SkipTo('/');
				m_fHasDenom = true;
				m_strmIn >> std::dec >> m_nDenomVal;
			}
		}
		SkipTo(';');

		// Mirrored (field 9)
		m_strmIn >> c1;
		m_fMirrored = (c1 == 'Y');
		SkipTo(';');

		// Unicode 1 name, not interesting (field 10)
		SkipTo(';');

		// Comment field. Not currently used. (field 11)
		SkipTo(';');

		// Upper case (field 12)
		m_strmIn >> std::ws;
		m_fHasUpper = m_strmIn.peek() != ';';
		if(m_fHasUpper)
			m_strmIn >> std::hex >> m_chUpper;
		SkipTo(';');

		// Lower case (field 13)
		m_strmIn >> std::ws;
		m_fHasLower = m_strmIn.peek() != ';';
		if(m_fHasLower)
			m_strmIn >> std::hex >> m_chLower;
		SkipTo(';');

		// Title case (field 14)
		m_fHasTitle = !isspace(m_strmIn.peek()); // end of line
		if(m_fHasTitle)
			m_strmIn >> std::hex >> m_chTitle;
		m_strmIn.get(); // skips newline

		// Now read the Line Break Property from m_strmIn3
		m_strmIn3 >> std::hex >> chtemp;
		if (chtemp != m_chCodePoint)
		{
			FatalError("Code Point out of step in Line Break file");
			exit(1);
		}
		SkipTo(m_strmIn3, ';');
		ReadLBP();
		SkipTo(m_strmIn3, '\n'); // skips newline

		return true;
	}

	// Make a dummy entry for an unassigned character.
	void MakeDummy()
	{
		m_prgsuBitFields[m_csuBitFields] = m_suBitFieldUnassigned;
		m_prgsuDecompIndex[m_csuBitFields] = 0; // 0 length for no decomposition
		m_prgbOtherCase[m_csuBitFields] = 0;     // there is no other case, so 0 will do
		// the name is empty; point at the null the very start of the buffer
		m_rgiNames[m_csuBitFields] = 0;

		m_csuBitFields++;
		if (m_csuBitFields >= MAXBITFIELDS)
		{
			FatalError("Unicode has changed disastrously (MAXBITFIELDS)");
			exit(1);
		}
	}

	// Advance m_strmIn to the start of the next line
	void NextLine(std::istream & strm)
	{
		char chsBuf[1000];
		strm.get(chsBuf, 1000);
	}

	// Get a character sequence represented as a list of space-separated
	// hex values, terminated by non-hex-digit. Return the number obtained.
	// Don't eat the non-hex terminating character
	int GetCharSeq(std::istream & strm, wchar_t * prgchDest)
	{
		strm >> std::ws;
		wchar_t * pch = prgchDest;
		while(isxdigit(strm.peek()))
		{
			strm >> *pch++;
			strm >> std::ws;
		}
		*pch = 0;
		return pch - prgchDest;

	}

	int TableIndex(int ch)
	{
		if (ch > 0xffff)
		{
			// Not a character we have any info about. Use the default properties at
			// index 0, unless we find some writing-system dependent override.
			return 0;
		}

		int msb = ch >> 8;
		int lsb = ch & 0xff;

		// cchPage is actually one less than the number of entries for the page
		int cchPage = m_rgcchPage[msb];
		if (lsb > cchPage)
			lsb = cchPage;

		return m_rgisuMinPage[msb] + lsb;
	}

	// m_strmIn has been switched to the special casing file. Process it.
	void DoSpecialCasing()
	{
		wchar_t rgchLower[256];
		wchar_t rgchTitle[256];
		wchar_t rgchUpper[256];
		wchar_t * pchField1; // first alternate
		wchar_t * pchField2;

		for(;!m_strmIn2.eof();)
		{
			m_strmIn2 >> std::ws;
			if (!isxdigit(m_strmIn2.peek()))
			{
				NextLine(m_strmIn2);
				continue;
			}
			m_strmIn2 >> std::hex >> m_chCodePoint;
			SkipTo(m_strmIn2,';');

			GetCharSeq(m_strmIn2, rgchLower);
			SkipTo(m_strmIn2,';');
			GetCharSeq(m_strmIn2, rgchTitle);
			SkipTo(m_strmIn2,';');
			GetCharSeq(m_strmIn2, rgchUpper);
			SkipTo(m_strmIn2,';');

			m_strmIn2 >> std::ws;
			if (m_strmIn2.peek() != '#')
			{
				// rule has conditions; for now ignore it.
				// (Note: currently only two rules have conditions:
				// final greek sigma, and a special case for Turkish.)
				// Review: actually another potential way to not have a condition
				// is an immediate end of line. But how do I look for that
				// AND the comment? skip white space will skip the new line...
				NextLine(m_strmIn2);
				continue;
			}
				NextLine(m_strmIn2);

			//-------------------------------------------------------------------
			// Represent the rule in our tables.

			// Mark it special
			// Review: what if it is already special? We will find the simpler
			// special case before the one we are now creating! Does that ever
			// happen in the actual tables?
			m_prgsuBitFields[TableIndex(m_chCodePoint)].fspecialC = true;

			int cc = m_prgsuBitFields[TableIndex(m_chCodePoint)].gc;
			switch(cc)
			{
			case kccLl:
				pchField1 = rgchUpper;
				pchField2 = rgchTitle;
				break;
			case kccLu:
				pchField1 = rgchLower;
				pchField2 = rgchTitle;
				break;
			case kccLt:
				pchField1 = rgchUpper;
				pchField2 = rgchLower;
				break;
			default:
				FatalError("special case conversion for caseless letter!");
				exit(1);
			}
			// See whether it was already special
			if (m_chCodePoint <= 3)
			{
				FatalError("special case conversion for code point <= 3!");
				exit(1);
			}
			OLECHAR * pch;
			for(pch = m_prgchSpecialCaseTable;
				pch < m_prgchSpecialCaseTable + m_cchSpecialCaseTableSize;)
			{
				if (*pch == m_chCodePoint)
				{
					break; // found the one we want!
				}
				// Otherwise advance by the necessary amount. First skip the base character.
				pch++;
				// a value of 1 marks a character that is special because it has two
				// single-character alternates.
				switch (*pch)
				{
				case 1:
					// a value of 1 marks a character that is special because it has two
					// single-character alternates. We skip a total of 3 chars (1, first, second)
					pch += 3;
					break;
				case 3:
					// skip the 3, two chars, then two null terminated strings.
					// The first three chars get skipped as part of the first string.
					// fall through
				case 2:
					// skip the 2, then two null-terminated strings.
					// The 2 can conveniently be skipped as part of the first string.
					while (*pch)
						pch++;
					pch++;
					while (*pch)
						pch++;
					pch++;
					break;
				default:
					// skip just one character
					pch++;
					break;
				}
			}
			// Insert the actual code point. This needs to be after the loop above
			// (otherwise, it will try to process the incomplete record),
			// but before the following if, which adds stuff after it.
			m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = m_chCodePoint;

			OLECHAR chFirstOther; // values we need if we go to mode 3
			OLECHAR chSecondOther;
			int cchDel;
			// To tell whether the loop above found something, we check whether pch got past
			// the limit. Note that the char inserted above changed the limit.
			if (pch < m_prgchSpecialCaseTable + m_cchSpecialCaseTableSize - 1)
			{
				// character is already special. How?
				switch (*(pch + 1))
				{
				case 1:
					// two different one-char equivalents follow
					cchDel = 4;
					chFirstOther = *(pch + 2);
					chSecondOther = *(pch + 3);
					break;
				case 2:
				case 3:
					FatalError("duplicate special case information!");
					exit(1);
				default:
					// one other-case character follows
					cchDel = 2;
					chFirstOther = chSecondOther = *(pch+1);
					break;
				}
				// Delete the old special case record. It might be the very last thing, in
				// which case we must not call memmove with 0 items.
				int cchMove = m_cchSpecialCaseTableSize - (pch + cchDel - m_prgchSpecialCaseTable);
				if (cchMove)
					::memmove(pch, pch + cchDel, cchMove * isizeof(OLECHAR));
				m_cchSpecialCaseTableSize -= cchDel;
				m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = 3;
				// 3 signifies that there follows the first alternate, the second alternate,
				// then the multi-char null-terminated strings
				m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = chFirstOther;
				m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = chSecondOther;
			}
			else
			{
				// Simple case for multi-char replacement
				m_prgchSpecialCaseTable[m_cchSpecialCaseTableSize++] = 2;
			}

			// two values, each null-terminated
			wcscpy(m_prgchSpecialCaseTable + m_cchSpecialCaseTableSize, pchField1);
			m_cchSpecialCaseTableSize += wcslen(pchField1) + 1;
			wcscpy(m_prgchSpecialCaseTable + m_cchSpecialCaseTableSize, pchField2);
			m_cchSpecialCaseTableSize += wcslen(pchField2) + 1;
			if (m_cchSpecialCaseTableSize >= MAXSPECIALCASE)
			{
				FatalError("Unicode has changed disastrously (MAXSPECIALCASE/2)");
				exit(1);
			}
		}
	}

	// Do the stuff that should happen at the end of a page, if we are at the
	// end of a page. In this code we have to make chCodePoint an int because
	// we call this method once with 0x10000 to finish out the last page.
	// Otherwise, it is the same as m_chCodePoint. Return true if the main loop
	// should continue at once, false to go on with the rest of the body.
	BOOL FinishPage(int chCodePoint)
	{
		int msbNew = chCodePoint >> 8;
		bool fMarkedEndPage = false;
		if (chCodePoint != m_chLast + 1)
		{
			// We have at least one unassigned character.
			// If the new character is on the same page, we need to fill in
			// a dummy entry for each missing code point.
			// If the new character is on a different page, we need
			//	- if the old character is not the last on its page, a single dummy
			//		to represent the rest of that page
			//  - if the new character is not the first of its page, dummies
			//		to fill the gap
			// If the characters are more than one page apart, we can make
			// dummy page entries for the missing pages.
			int msbLast = m_chLast >> 8;
			if (msbLast != msbNew && (m_chLast & 0xff) != 0xff)
			{
				// fill in dummies on last page
				MakeDummy();
				int ichMinLastPage = m_rgisuMinPage[msbLast];
				int cchPage = m_csuBitFields - ichMinLastPage;
				Assert(cchPage < 257);
				m_rgcchPage[msbLast] = cchPage - 1;
				fMarkedEndPage = true;
				// Now we have the tables filled in up to the end of that page,
				// so pretend that was the last character
				m_chLast |= 255;
				// Empty pages never take up slots, so the next slot
				// will certainly be the start of the page containing chCodePoint
				// (unless it is a special page, in which case it gets overwritten
				// when we read the next record).
				if (msbNew < 256)
					m_rgisuMinPage[msbNew] = m_csuBitFields;
			}
			// We don't need to do anything for the end-of-range entries
			// in the special ranges. The proper stuff gets filled in by the
			// "for ipage" loop when we encounter the first character in the following
			// range. We need to treat these specially or we will get a whole
			// page filled with unassigned characters. We also skip the start
			// of the private use high surrogate and low surrogate areas,
			// because we treat all the surrogates as one big range.
			if (msbNew == 0x4d || msbNew == 0x9f || msbNew == 0xd7 || msbNew == 0xdb
				|| msbNew == 0xdc || msbNew == 0xdf || msbNew == 0xf8 || msbNew == 0x100)
			{
				return true;
			}
			for (int ipage = msbLast + 1; ipage < msbNew; ipage++)
			{
				// Fill in any complete pages of dummy info
				// These may be pages of unassigned characters, or they may be
				// one of the special ranges.
				// **UPDATE 3.0: 3400;<CJK Ideograph Extension A, First>;Lo;0;L;;;;;N;;;;;
				// **UPDATE 3.0: 4DB5;<CJK Ideograph Extension A, Last>;Lo;0;L;;;;;N;;;;;
				//	4E00;<CJK Ideograph, First>;Lo;0;L;;;;;N;;;;;
				//	9FA5;<CJK Ideograph, Last>;Lo;0;L;;;;;N;;;;;
				//	AC00;<Hangul Syllable, First>;Lo;0;L;;;;;N;;;;;
				//	D7A3;<Hangul Syllable, Last>;Lo;0;L;;;;;N;;;;;
				//	D800;<Non Private Use High Surrogate, First>;Cs;0;L;;;;;N;;;;;
				//	DB7F;<Non Private Use High Surrogate, Last>;Cs;0;L;;;;;N;;;;;
				//	DB80;<Private Use High Surrogate, First>;Cs;0;L;;;;;N;;;;;
				//	DBFF;<Private Use High Surrogate, Last>;Cs;0;L;;;;;N;;;;;
				//	DC00;<Low Surrogate, First>;Cs;0;L;;;;;N;;;;;
				//	DFFF;<Low Surrogate, Last>;Cs;0;L;;;;;N;;;;;
				//	E000;<Private Use, First>;Co;0;L;;;;;N;;;;;
				//	F8FF;<Private Use, Last>;Co;0;L;;;;;N;;;;;
				// Note: to avoid a messy special table for the last page of
				// the CJK and Hangul areas, we report the whole of page 4d as
				// CJK Extension, the whole of page 9F as CJK, and the whole of D7 as Hangul.
				int isuBitFieldDummy = 0; // for unassigned
				if (ipage >= 0x34 && ipage <= 0x4d)      // CJK Extension
					isuBitFieldDummy = m_rgisuMinPage[0x34];
				else if (ipage >= 0x4e && ipage <= 0x9f) // CJK
					isuBitFieldDummy = m_rgisuMinPage[0x4e];
				else if (ipage >= 0xac && ipage <= 0xd7) // Hangul
					isuBitFieldDummy = m_rgisuMinPage[0xac];
				else if (ipage >= 0xd8 && ipage <= 0xdf) // Surrogates (high and low same)
					isuBitFieldDummy = m_rgisuMinPage[0xd8];
				else if (ipage >= 0xe0 && ipage <= 0xf8) // Private use
					isuBitFieldDummy = m_rgisuMinPage[0xe0];
				m_rgisuMinPage[ipage] = isuBitFieldDummy;
				m_rgcchPage[ipage] = 0; // one dummy character represents page
				fMarkedEndPage = true;
				// We have now dealt will all characters on ipage
				// (Note that we may well do 0 iterations of this loop,
				// in which case, m_chLast does not get changed. That is correct
				// for a small gap within a page.)
				m_chLast = (OLECHAR) ((ipage << 8) + 0xff);
			}
			if (msbLast != msbNew)
			{
				// start a new page for the current character
				m_rgisuMinPage[msbNew] = m_csuBitFields;
				// If we have not marked off the end of the previous page yet, do it.
				// The only way that can happen at this point is if the previous page
				// was full. In fact, only page ff has this problem, where
				// the previous page is full and this page does not start on 00.
				if (!fMarkedEndPage)
				{
					m_rgcchPage[msbNew - 1] = 255;
				}

			}
			// If there is still a gap, fill it in with unassigned characters.
			// Note that we already recorded the start of the page containing
			// chCodePoint, if we passed that.
			for (int i = m_chLast + 1; i < chCodePoint; i++)
			{
				MakeDummy();
			}
		}
		else if ((chCodePoint & 0xff) == 0)
		{
			// Starting new page after filling the previous one entirely
			// (or the very first page)
			int msbNew = chCodePoint >> 8; // page we are starting
			m_rgisuMinPage[msbNew] = m_csuBitFields;
			if (msbNew)
			{
				// We have not marked off the end of the previous page yet; do it
				m_rgcchPage[msbNew - 1] = 255;
			}

		}
		return false;
	}

	void Run(int argc, char *argv[ ])
	{
		m_strmIn.open(argv[1]);
		if (!m_strmIn)
		{
			FatalError("Unicode data file not found\n");
			exit(1);
		}
		m_strmIn3.open(argv[3]);
		if (!m_strmIn3)
		{
			FatalError("Line Break file not found\n");
			exit(1);
		}
		m_strmOut.open("LgCharPropertyInit.h"); // name of header file needed for making engine
		if (!m_strmOut)
		{
			FatalError("Cannot create output file\n");
			exit(1);
		}

		// Make a dummy entry in all the tables for an undefined character
		m_gc = kccCn;  //Other, not assigned
		m_nCombClass = 0;
		m_bic = kbicL; // Database seems to make unknown chars L by default
		m_cchDecomp = 0; // no decomposition known
		m_fHasDecimal = m_fHasDigit = m_fHasNumeric = false;
		m_fMirrored = false;
		m_fHasUpper = m_fHasLower = m_fHasTitle = false;
		m_cchSpecialCaseTableSize = 0; // none in this table yet
		m_uLBP = klbpXX; // recommended value for unassigned (see Unicode technical report #14)

		MakeBitfieldEntry();
		m_suBitField.fnotAssign = true;
		m_suBitField.fspecialC = false;
		m_suBitFieldUnassigned = m_suBitField;

		m_csuBitFields = 0;
		m_cchDecompositions = 0; // none in it either
		m_ccCExtra = 0;          // no extra combining class values yet
		m_pchsNameData = m_rgchsNameData;
		*m_pchsNameData++ = 0; // initial null for missing names to point at
		m_cFractions = 0;

		MakeDummy(); // very first entry is a dummy for unassigned pages

		m_chLast = -1; // Looks one smaller and on different page

		m_cbNamePage = 0; // no name bytes used up yet

		while (ReadRecord())
		{
			if (FinishPage(m_chCodePoint))
				continue;
			int msbNew = m_chCodePoint >> 8;
			m_chLast = m_chCodePoint;
			if (msbNew == 0x34 || msbNew == 0x4e || msbNew == 0xac ||
				msbNew == 0xd8 || msbNew == 0xe0)
			{
				// We have just made a special header that is supposed to stand for
				// its whole page
				m_rgcchPage[msbNew] = 0; // one dummy character represents page
				m_chLast |= 0xff; // and we have processed the whole page
			}

			// Make an entry for this character.
			MakeBitfieldEntry();
			m_prgsuBitFields[m_csuBitFields] = m_suBitField;

			// Store low byte of other case character code (specials already handled)
			m_prgbOtherCase[m_csuBitFields] = (unsigned char) m_chOtherCase;

			if (m_cchDecomp)
			{
				int cchEnc; // encoded character count in 3 bits
				cchEnc = m_cchDecomp;
				if (m_cchDecomp > 6)
					cchEnc = 7;
				m_prgsuDecompIndex[m_csuBitFields] = (unsigned short)
					((cchEnc << 13) + m_cchDecompositions);
				wcsncpy(m_prgchDecompositions + m_cchDecompositions,
					m_rgchwDecomp, m_cchDecomp);
				m_cchDecompositions += m_cchDecomp;
				if (m_cchDecomp > 6)
				{
					// Null terminate the string
					*(m_prgchDecompositions + m_cchDecompositions) = 0;
					m_cchDecompositions++;
				}
			}
			else
			{
				// no decomposition
				m_prgsuDecompIndex[m_csuBitFields] = 0;
			}

			// Name
			m_rgiNames[m_csuBitFields] = m_pchsNameData - m_rgchsNameData;
			strcpy(m_pchsNameData, m_rgchName);
			m_pchsNameData += strlen(m_rgchName) + 1;

			m_csuBitFields++;
			if (m_csuBitFields >= MAXBITFIELDS)
			{
				FatalError("Unicode has changed disastrously (MAXBITFIELDS)");
				exit(1);
			}
		}
		FinishPage(0x10000); // all the normal end-of-page stuff to finish out the last page.

		m_strmIn.close();
		m_strmIn3.close();

		m_strmIn2.open(argv[2]);
		if (!m_strmIn2)
		{
			FatalError("Special casing file not found\n");
			exit(1);
		}
		DoSpecialCasing();
		m_strmIn2.close();

		int i; // loop variable for output of many arrays

		// Write the header file containing the variable initializations.
		// Write a header
		m_strmOut << "/*-----------------------------------------------------------------------------------*//*:Ignore\n";
		m_strmOut << "File: LgCharPropertyInit.h\n";
		m_strmOut << "Variable initialization for LgCharPropertyEngine\n";
		m_strmOut << "This file is generated by Fieldworks\\bin\\UnicodeDb.\n";
		m_strmOut << "Do not edit by hand.\n";
		m_strmOut << "----------------------------------------------------------------------------------------------*/\n\n";

		// Initialize m_csuBitFields
		m_strmOut << "int LgCharacterPropertyEngine::g_ccpfBitFields = " << m_csuBitFields << ";\n";

		// Make a static array of values for g_prgcpfBitFields to point at
		m_strmOut <<
			"__declspec(allocate(\"datacode\")) static const LgCharPropsFlds g_rgcpfBitFields[" <<
			m_csuBitFields << "] =\n{\n";
		for (i = 0; i < m_csuBitFields; i++)
		{
			m_strmOut << "\t" << "{" <<
				(m_prgsuBitFields[i].gc & 0x1f) << ", " <<
				m_prgsuBitFields[i].fmirrored << ", " <<
				m_prgsuBitFields[i].nv << ", " <<
				(m_prgsuBitFields[i].bidi & 0x1f) << ", " <<
				m_prgsuBitFields[i].fspecialC << ", " <<
				m_prgsuBitFields[i].ucC << ", " <<
				(m_prgsuBitFields[i].lbp & 0x1f) << ", " <<
				m_prgsuBitFields[i].fnotAssign << "},\n";
		}
		m_strmOut << "};\n";

		// Make a static array of values for g_prgbOtherCase to point at
		m_strmOut << "__declspec(allocate(\"datacode\")) static const byte g_rgbOtherCase[" <<
			m_csuBitFields << "] =\n{\n";
		for (i = 0; i < m_csuBitFields; i++)
		{
			m_strmOut << "\t(byte)'\\x" << std::hex <<int(m_prgbOtherCase[i]) << std::dec << "',\n";
		}
		m_strmOut << "};\n";

		// Initialize g_cuCombClassExtraSize
		m_strmOut << "int LgCharacterPropertyEngine::g_cuCombClassExtraSize = " <<
			m_ccCExtra << ";\n";

		// Make a static array of values for g_prguCombClassExtra to point at
		m_strmOut <<
			"__declspec(allocate(\"datacode\")) static const uint g_rguCombClassExtra[" <<
			m_ccCExtra << "] =\n{\n";
		for (i = 0; i < m_ccCExtra; i++)
		{
			m_strmOut << "\t" << m_prguCombClassExtra[i] << ",\n";
		}
		m_strmOut << "};\n";

		// Make an initializer for g_rgbCombClass
		m_strmOut << "byte LgCharacterPropertyEngine::g_rgbCombClass[15] =\n{";
		for (i = 0; i<15; ++i)
		{
			m_strmOut << (uint)rgbCombClass[i];
			if (i < 14)
				m_strmOut << ", ";
		}
		m_strmOut << "};\n\n";

		// Make a static array of values for g_prgichDecompIndex to point at
		m_strmOut << "__declspec(allocate(\"datacode\")) static const unsigned short g_rgichDecompIndex[" << m_csuBitFields << "] =\n{\n";
		for (i = 0; i < m_csuBitFields; i++)
		{
			m_strmOut << "\t" << m_prgsuDecompIndex[i] << ",\n";
		}
		m_strmOut << "};\n";

		// Make a static array of values for g_prgpNames to point at
		// To get the data into a code segment, each individual string has to be declared in its own
		// variable; then, the array of pointers is initialized to point at the variables.
		// All the empty strings share 'name0'
		for (i = 0; i < m_csuBitFields; i++)
		{
			if (i == 0 || strlen(m_rgchsNameData + m_rgiNames[i]))
			{
				m_strmOut << "\t__declspec(allocate(\"datacode\")) const char name" << i <<
					"[] = \"" << m_rgchsNameData + m_rgiNames[i] << "\";\n";
			}
		}

		m_strmOut << "__declspec(allocate(\"datacode\")) static const char * g_rgpNames[" << m_csuBitFields << "] =\n{\n";
		for (i = 0; i < m_csuBitFields; i++)
		{
			if (strlen(m_rgchsNameData + m_rgiNames[i]))
			{
				m_strmOut << "\tname" << i << ",\n";
			}
			else
			{
				m_strmOut << "\tname0,\n";
			}

		}
		m_strmOut << "};\n";

		// Make an initializer for g_rgicpfMinPage
		m_strmOut << "unsigned short LgCharacterPropertyEngine::g_rgicpfMinPage[256] =\n{\n";
		for (i = 0; i < 256; i++)
		{
			m_strmOut << "\t" << m_rgisuMinPage[i] << ",\n";
		}
		m_strmOut << "};\n";

		// Make an initializer for g_rgcchPage
		m_strmOut << "byte LgCharacterPropertyEngine::g_rgcchPage[256] =\n{\n";
		for (i = 0; i < 256; i++)
		{
			m_strmOut << "\t(byte)'\\x" << std::hex <<int(m_rgcchPage[i]) << std::dec << "',\n";
		}
		m_strmOut << "};\n";

		// Initialize g_cchSpecialCaseTableSize
		m_strmOut << "int LgCharacterPropertyEngine::g_cchSpecialCaseTableSize = " <<
			m_cchSpecialCaseTableSize << ";\n";

		// Make a static array of values for g_prgchSpecialCaseTable to point at
		m_strmOut <<
			"__declspec(allocate(\"datacode\")) static const unsigned short g_rgchSpecialCaseTable[" <<
			m_cchSpecialCaseTableSize << "] =\n{\n";
		for (i = 0; i < m_cchSpecialCaseTableSize; i++)
		{
			m_strmOut << "\t" << m_prgchSpecialCaseTable[i] << ",\n";
		}
		m_strmOut << "};\n";

		// Initialize g_cchDecompositions
		m_strmOut << "int LgCharacterPropertyEngine::g_cchDecompositions = " <<
			m_cchDecompositions << ";\n";

		// Make a static array of values for g_prgchDecompositions to point at
		m_strmOut <<
			"__declspec(allocate(\"datacode\")) static const unsigned short g_rgchDecompositions[" <<
			m_cchDecompositions << "] =\n{\n";
		for (i = 0; i < m_cchDecompositions; i++)
		{
			m_strmOut << "\t" << m_prgchDecompositions[i] << ",\n";
		}
		m_strmOut << "};\n";

		// We want to have Surveyor ignore this entire file. Otherwise it eats up over half an hour
		// calculating useless information on Unicode "name#" methods.
		m_strmOut << "\n/*:End Ignore*/\n";

		m_strmOut.close();

		//int ch;
		//std::cin >> ch;
	}
};

main( int argc, char *argv[ ])
{
	if (argc != 4)
	{
		printf("Usage: UnicodeDB UnicodeDataFile SpecialCasingFile LineBreakFile\n");
		exit(1);
	}

	Processor proc;
	proc.Run(argc, argv);
	return 0;
}
