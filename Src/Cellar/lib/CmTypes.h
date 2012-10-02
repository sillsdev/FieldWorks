/*************
* CmTypes.h
*************/
/*
* Notes made with the // type of remark have been changed, so that the
* SQL Server parse won't trip over them.
*/
/*
* Definitions for object types. Make sure both lists are consistent!
* We are also including magic encodings here so the constants can be used in SQL code
* as well as C++ code.
*/

#ifdef CMCG_SQL_DEFNS

#define kcptNil 0
#define kcptMin 1 /* not used except to indicate smallest value allowed */
#define kcptBoolean 1
#define kcptInteger 2
#define kcptNumeric 3
#define kcptFloat 4
#define kcptTime 5
#define kcptGuid 6
#define kcptImage 7
#define kcptGenDate 8
#define kcptBinary 9

#define kcptString 13
#define kcptMultiString 14

/*
* Warning! Do NOT use kcptUnicode for language data (either vernacular or analysis).
* It is NOT parallel to kcptMultiUnicode; that has a WS specified for each alternative,
* but kcptUnicode has NO WS information. Support for kcptUnicode being used as strings is
* spotty but should not be counted on. Use kcptString for short language strings and
* kcptBigString for long ones. There is NO good way to represent a monolingual string
* that is forced to have only one writing system along its length; if we need that, let's
* add it, but please don't abuse kcptUnicode. (JohnT)
*/

#define kcptUnicode 15
#define kcptMultiUnicode 16
#define kcptBigString 17
#define kcptMultiBigString 18
#define kcptBigUnicode 19
#define kcptMultiBigUnicode 20  /* not currently implemented in Flex */

#define kcptMinObj 23
#define kcptOwningAtom 23
#define kcptReferenceAtom 24
#define kcptOwningCollection 25
#define kcptReferenceCollection 26
#define kcptOwningSequence 27
#define kcptReferenceSequence 28
#define kcptLim 29 /* not used except to indicate largest value allowed. */

/*
* if the above values are changed these values need to be recalculated; these values
* are 1 shifted to the left the number of bits equal to the corresponding property
* type constant; ex. kfcptOwningAtom = 1 << kcptOwningAtom
*/

#define kfcptOwningAtom 8388608
#define kfcptReferenceAtom 16777216
#define kfcptOwningCollection 33554432
#define kfcptReferenceCollection 67108864
#define kfcptOwningSequence 134217728
#define kfcptReferenceSequence 268435456
#define kgrfcptOwning 176160768
#define kgrfcptReference 352321536
#define kgrfcptAll 528482304

/*
* These are magic writing system numbers that are not legal encodings, but are used to signal
* the application to get the appropriate writing system from the application. For example,
* a language project has a list of one or more analysis encodings. kwsAnal would
* tell the program to use the first writing system in this list.
*
* ****** IMPORTANT ******
* These constants are mirrored in LangProject.cs and need to be kept syncd.
* On 8/24 the value of kwsLim in LangProject.cs is -16 and the value here is -7.
* ****** IMPORTANT ******
*/

#define kwsAnal 0xffffffff /* (-1) The first analysis writing system. */
#define kwsVern 0xfffffffe /* (-2) The first vernacular writing system. */
#define kwsAnals 0xfffffffd /* (-3) All analysis writing system. */
#define kwsVerns 0xfffffffc /* (-4) All vernacular writing system. */
#define kwsAnalVerns 0xfffffffb /* (-5) All analysis then All vernacular writing system. */
#define kwsVernAnals 0xfffffffa /* (-6) All vernacular then All analysis writing system. */
/* added 8/24 to get in sync with LangProject.cs */
#define kwsFirstAnal			0xfffffff9 /* (-7) The first available analysis ws with data in the current sequence. */
#define kwsFirstVern			0xfffffff8 /* (-8) The first available vernacular ws in the current sequence. */
#define kwsFirstAnalOrVern		0xfffffff7 /* (-9) The first available analysis ws with data in the current sequence, or the first available vernacular ws in that sequence. */
#define kwsFirstVernOrAnal		0xfffffff6 /* (-10) The first available vernacular ws with data in the current sequence, or the first available analysis ws in that sequence. */
#define kwsPronunciation		0xfffffff5 /* (-11) The first pronunciation writing system. */
#define kwsFirstPronunciation	0xfffffff4 /* (-12) The first pronunciation writing system with data. */
#define kwsPronunciations		0xfffffff3 /* (-13) All pronunciation writing systems. */
#define kwsReversalIndex		0xfffffff2 /* (-14) The primary writing system for the current reversal index. */
#define kwsAllReversalIndex		0xfffffff1 /* (-15) The full list of writing systems for the current reversal index. */
#define kwsLim					0xfffffff0 /* (-16) One beyond the last magic value. */


#endif /* CMCG_SQL_DEFNS */

#ifdef CMCG_SQL_ENUM

	kcptNil = 0,
	kcptMin = 1, /* for range checking */
	kcptBoolean = 1,
	kcptInteger = 2,
	kcptNumeric = 3,
	kcptFloat = 4,
	kcptTime = 5,
	kcptGuid = 6,
	kcptImage = 7,
	kcptGenDate = 8,
	kcptBinary = 9,

	kcptString = 13,
	kcptMultiString = 14,
	kcptUnicode = 15,
	kcptMultiUnicode = 16,
	kcptBigString = 17,
	kcptMultiBigString = 18,
	kcptBigUnicode = 19,
	kcptMultiBigUnicode = 20,

	kcptMinObj = 23,
	kcptOwningAtom = 23,
	kcptReferenceAtom = 24,
	kcptOwningCollection = 25,
	kcptReferenceCollection = 26,
	kcptOwningSequence = 27,
	kcptReferenceSequence = 28,
	kcptLim = 29, /* limit except for virtuals */

	/* The compiler gives bad values if we use calculations instead of literals. */
	kfcptOwningAtom = 8388608, /* 1 << kcptOwningAtom, */
	kfcptReferenceAtom = 16777216, /* 1 << kcptReferenceAtom, */
	kfcptOwningCollection = 33554432, /* 1 << kcptOwningCollection, */
	kfcptReferenceCollection = 67108864, /* 1 << kcptReferenceCollection, */
	kfcptOwningSequence = 134217728, /* 1 << kcptOwningSequence, */
	kfcptReferenceSequence = 268435456, /* 1 << kcptReferenceSequence, */
	kgrfcptOwning = 176160768, /* kcptOwningAtom | kcptOwningCollection | kcptOwningSequence, */
	kgrfcptReference = 352321536, /* kcptReferenceAtom | kcptReferenceCollection | kcptReferenceSequence, */
	kgrfcptAll = 528482304, /* kgrfcptOwning | kgrfcptReference, */

	/*
	* These are magic writing system numbers that are not legal encodings, but are used to signal
	* the application to get the appropriate writing system from the application. For example,
	* a language project has a list of one or more analysis encodings. kwsAnal would
	* tell the program to use the first writing system in this list. */
	kwsAnal = 0xffffffff, /* (-1) The first analysis writing system. */
	kwsVern = 0xfffffffe, /* (-2) The first vernacular writing system. */
	kwsAnals = 0xfffffffd, /* (-3) All analysis writing system. */
	kwsVerns = 0xfffffffc, /* (-4) All vernacular writing system. */
	kwsAnalVerns = 0xfffffffb, /* (-5) All analysis then All vernacular writing system. */
	kwsVernAnals = 0xfffffffa, /* (-6) All vernacular then All analysis writing system. */
	/*
	* Note these are defined and used in LangProject.cs and used in Flex. We haven't implemented
	* any of this in C++ or SQL yet, so these are commented out for now.
	* <summary>(-7) The first available analysis ws in the current sequence.</summary>
	* public const int kwsFirstAnal = -7;
	* <summary>(-8) The first available vernacular ws in the current sequence.</summary>
	* public const int kwsFirstVern = -8;
	* <summary>(-9) The first available analysis ws in the current sequence,
	* or the first available vernacular ws in that sequence.</summary>
	* public const int kwsFirstAnalOrVern = -9;
	* <summary>(-10) The first available vernacular ws in the current sequence,
	* or the first available analysis ws in that sequence.</summary>
	* public const int kwsFirstVernOrAnal = -10;
	* <summary>The first pronunciation writing system.</summary>
	*	public const int kwsPronunciation = -11;
	* <summary>The first pronunciation writing system with data.</summary>
	*	public const int kwsFirstPronunciation = -12;
	* <summary>All pronunciation writing systems.</summary>
	*	public const int kwsPronunciations = -13;
	*/

	kwsLim = 0xfffffff9, /* (-7) One beyond the last magic value. */

#endif /* CMCG_SQL_ENUM */