// Copyright (c) 2013-2018 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// ICU error codes
	/// </summary>
	public enum IcuErrorCodes
	{
		/// <summary>A resource bundle lookup returned a fallback result (not an error)</summary>
		USING_FALLBACK_WARNING = -128,
		/// <summary>Start of information results (semantically successful)</summary>
		ERROR_WARNING_START = -128,
		/// <summary>A resource bundle lookup returned a result from the root locale (not an error)</summary>
		USING_DEFAULT_WARNING = -127,
		/// <summary>A SafeClone operation required allocating memory (informational only)</summary>
		SAFECLONE_ALLOCATED_WARNING = -126,
		/// <summary>ICU has to use compatibility layer to construct the service. Expect performance/memory usage degradation. Consider upgrading</summary>
		STATE_OLD_WARNING = -125,
		/// <summary>An output string could not be NUL-terminated because output length==destCapacity.</summary>
		STRING_NOT_TERMINATED_WARNING = -124,
		/// <summary>Number of levels requested in getBound is higher than the number of levels in the sort key</summary>
		SORT_KEY_TOO_SHORT_WARNING = -123,
		/// <summary>This converter alias can go to different converter implementations</summary>
		AMBIGUOUS_ALIAS_WARNING = -122,
		/// <summary>ucol_open encountered a mismatch between UCA version and collator image version, so the collator was constructed from rules. No impact to further function</summary>
		DIFFERENT_UCA_VERSION = -121,
		/// <summary>This must always be the last warning value to indicate the limit for UErrorCode warnings (last warning code +1)</summary>
		ERROR_WARNING_LIMIT,
		/// <summary>No error, no warning.</summary>

		ZERO_ERROR = 0,
		/// <summary>No error, no warning.</summary>
		NoErrors = ZERO_ERROR,
		/// <summary>Start of codes indicating failure</summary>
		ILLEGAL_ARGUMENT_ERROR = 1,
		/// <summary>The requested resource cannot be found</summary>
		MISSING_RESOURCE_ERROR = 2,
		/// <summary>Data format is not what is expected</summary>
		INVALID_FORMAT_ERROR = 3,
		/// <summary>The requested file cannot be found</summary>
		FILE_ACCESS_ERROR = 4,
		/// <summary>Indicates a bug in the library code</summary>
		INTERNAL_PROGRAM_ERROR = 5,
		/// <summary>Unable to parse a message (message format)</summary>
		MESSAGE_PARSE_ERROR = 6,
		/// <summary>Memory allocation error</summary>
		MEMORY_ALLOCATION_ERROR = 7,
		/// <summary>Trying to access the index that is out of bounds</summary>
		INDEX_OUTOFBOUNDS_ERROR = 8,
		/// <summary>Equivalent to Java ParseException</summary>
		PARSE_ERROR = 9,
		/// <summary>Character conversion: Unmappable input sequence. In other APIs: Invalid character.</summary>
		INVALID_CHAR_FOUND = 10,
		/// <summary>Character conversion: Incomplete input sequence.</summary>
		TRUNCATED_CHAR_FOUND = 11,
		/// <summary>Character conversion: Illegal input sequence/combination of input units.</summary>
		ILLEGAL_CHAR_FOUND = 12,
		/// <summary>Conversion table file found, but corrupted</summary>
		INVALID_TABLE_FORMAT = 13,
		/// <summary>Conversion table file not found</summary>
		INVALID_TABLE_FILE = 14,
		/// <summary>A result would not fit in the supplied buffer</summary>
		BUFFER_OVERFLOW_ERROR = 15,
		/// <summary>Requested operation not supported in current context</summary>
		UNSUPPORTED_ERROR = 16,
		/// <summary>an operation is requested over a resource that does not support it</summary>
		RESOURCE_TYPE_MISMATCH = 17,
		/// <summary>ISO-2022 illlegal escape sequence</summary>
		ILLEGAL_ESCAPE_SEQUENCE = 18,
		/// <summary>ISO-2022 unsupported escape sequence</summary>
		UNSUPPORTED_ESCAPE_SEQUENCE = 19,
		/// <summary>No space available for in-buffer expansion for Arabic shaping</summary>
		NO_SPACE_AVAILABLE = 20,
		/// <summary>Currently used only while setting variable top, but can be used generally</summary>
		CE_NOT_FOUND_ERROR = 21,
		/// <summary>User tried to set variable top to a primary that is longer than two bytes</summary>
		PRIMARY_TOO_LONG_ERROR = 22,
		/// <summary>ICU cannot construct a service from this state, as it is no longer supported</summary>
		STATE_TOO_OLD_ERROR = 23,
		/// <summary>
		/// There are too many aliases in the path to the requested resource.
		/// It is very possible that a circular alias definition has occured
		/// </summary>
		TOO_MANY_ALIASES_ERROR = 24,
		/// <summary>UEnumeration out of sync with underlying collection</summary>
		ENUM_OUT_OF_SYNC_ERROR = 25,
		/// <summary>Unable to convert a UChar* string to char* with the invariant converter.</summary>
		INVARIANT_CONVERSION_ERROR = 26,
		/// <summary>Requested operation can not be completed with ICU in its current state</summary>
		INVALID_STATE_ERROR = 27,
		/// <summary>Collator version is not compatible with the base version</summary>
		COLLATOR_VERSION_MISMATCH = 28,
		/// <summary>Collator is options only and no base is specified</summary>
		USELESS_COLLATOR_ERROR = 29,
		/// <summary>Attempt to modify read-only or constant data.</summary>
		NO_WRITE_PERMISSION = 30,
		/// <summary>This must always be the last value to indicate the limit for standard errors</summary>
		STANDARD_ERROR_LIMIT,
		/*
		 * the error code range 0x10000 0x10100 are reserved for Transliterator
		 */
		/// <summary>Missing '$' or duplicate variable name</summary>
		BAD_VARIABLE_DEFINITION = 0x10000,
		/// <summary>Start of Transliterator errors</summary>
		PARSE_ERROR_START = 0x10000,
		/// <summary>Elements of a rule are misplaced</summary>
		MALFORMED_RULE,
		/// <summary>A UnicodeSet pattern is invalid</summary>
		MALFORMED_SET,
		/// <summary>UNUSED as of ICU 2.4</summary>
		MALFORMED_SYMBOL_REFERENCE,
		/// <summary>A Unicode escape pattern is invalid</summary>
		MALFORMED_UNICODE_ESCAPE,
		/// <summary>A variable definition is invalid</summary>
		MALFORMED_VARIABLE_DEFINITION,
		/// <summary>A variable reference is invalid</summary>
		MALFORMED_VARIABLE_REFERENCE,
		/// <summary>UNUSED as of ICU 2.4</summary>
		MISMATCHED_SEGMENT_DELIMITERS,
		/// <summary>A start anchor appears at an illegal position</summary>
		MISPLACED_ANCHOR_START,
		/// <summary>A cursor offset occurs at an illegal position</summary>
		MISPLACED_CURSOR_OFFSET,
		/// <summary>A quantifier appears after a segment close delimiter</summary>
		MISPLACED_QUANTIFIER,
		/// <summary>A rule contains no operator</summary>
		MISSING_OPERATOR,
		/// <summary>UNUSED as of ICU 2.4</summary>
		MISSING_SEGMENT_CLOSE,
		/// <summary>More than one ante context</summary>
		MULTIPLE_ANTE_CONTEXTS,
		/// <summary>More than one cursor</summary>
		MULTIPLE_CURSORS,
		/// <summary>More than one post context</summary>
		MULTIPLE_POST_CONTEXTS,
		/// <summary>A dangling backslash</summary>
		TRAILING_BACKSLASH,
		/// <summary>A segment reference does not correspond to a defined segment</summary>
		UNDEFINED_SEGMENT_REFERENCE,
		/// <summary>A variable reference does not correspond to a defined variable</summary>
		UNDEFINED_VARIABLE,
		/// <summary>A special character was not quoted or escaped</summary>
		UNQUOTED_SPECIAL,
		/// <summary>A closing single quote is missing</summary>
		UNTERMINATED_QUOTE,
		/// <summary>A rule is hidden by an earlier more general rule</summary>
		RULE_MASK_ERROR,
		/// <summary>A compound filter is in an invalid location</summary>
		MISPLACED_COMPOUND_FILTER,
		/// <summary>More than one compound filter</summary>
		MULTIPLE_COMPOUND_FILTERS,
		/// <summary>A "::id" rule was passed to the RuleBasedTransliterator parser</summary>
		INVALID_RBT_SYNTAX,
		/// <summary>UNUSED as of ICU 2.4</summary>
		INVALID_PROPERTY_PATTERN,
		/// <summary>A 'use' pragma is invlalid</summary>
		MALFORMED_PRAGMA,
		/// <summary>A closing ')' is missing</summary>
		UNCLOSED_SEGMENT,
		/// <summary>UNUSED as of ICU 2.4</summary>
		ILLEGAL_CHAR_IN_SEGMENT,
		/// <summary>Too many stand-ins generated for the given variable range</summary>
		VARIABLE_RANGE_EXHAUSTED,
		/// <summary>The variable range overlaps characters used in rules</summary>
		VARIABLE_RANGE_OVERLAP,
		/// <summary>A special character is outside its allowed context</summary>
		ILLEGAL_CHARACTER,
		/// <summary>Internal transliterator system error</summary>
		INTERNAL_TRANSLITERATOR_ERROR,
		/// <summary>A "::id" rule specifies an unknown transliterator</summary>
		INVALID_ID,
		/// <summary>A "&amp;fn()" rule specifies an unknown transliterator</summary>
		INVALID_FUNCTION,
		/// <summary>The limit for Transliterator errors</summary>
		PARSE_ERROR_LIMIT,
		/*
		* the error code range 0x10100 0x10200 are reserved for formatting API parsing error
		*/
		/// <summary>Syntax error in format pattern</summary>
		UNEXPECTED_TOKEN = 0x10100,
		/// <summary>Start of format library errors</summary>
		FMT_PARSE_ERROR_START = 0x10100,
		/// <summary>More than one decimal separator in number pattern</summary>
		MULTIPLE_DECIMAL_SEPARATORS,
		/// <summary>More than one exponent symbol in number pattern</summary>
		MULTIPLE_EXPONENTIAL_SYMBOLS,
		/// <summary>Grouping symbol in exponent pattern</summary>
		MALFORMED_EXPONENTIAL_PATTERN,
		/// <summary>More than one percent symbol in number pattern</summary>
		MULTIPLE_PERCENT_SYMBOLS,
		/// <summary>More than one permill symbol in number pattern</summary>
		MULTIPLE_PERMILL_SYMBOLS,
		/// <summary>More than one pad symbol in number pattern</summary>
		MULTIPLE_PAD_SPECIFIERS,
		/// <summary>Syntax error in format pattern</summary>
		PATTERN_SYNTAX_ERROR,
		/// <summary>Pad symbol misplaced in number pattern</summary>
		ILLEGAL_PAD_POSITION,
		/// <summary>Braces do not match in message pattern</summary>
		UNMATCHED_BRACES,
		/// <summary>UNUSED as of ICU 2.4</summary>
		UNSUPPORTED_PROPERTY,
		/// <summary>UNUSED as of ICU 2.4</summary>
		UNSUPPORTED_ATTRIBUTE,
		/// <summary>Argument name and argument index mismatch in MessageFormat functions.</summary>
		ARGUMENT_TYPE_MISMATCH,
		/// <summary>Duplicate keyword in PluralFormat.</summary>
		DUPLICATE_KEYWORD,
		/// <summary>Undefined Plural keyword.</summary>
		UNDEFINED_KEYWORD,
		/// <summary>Missing DEFAULT rule in plural rules.</summary>
		DEFAULT_KEYWORD_MISSING,
		/// <summary>The limit for format library errors</summary>
		FMT_PARSE_ERROR_LIMIT,
		/*
 * the error code range 0x10200 0x102ff are reserved for Break Iterator related error
 */
		/// <summary>An internal error (bug) was detected.</summary>
		BRK_INTERNAL_ERROR = 0x10200,
		/// <summary>Start of codes indicating Break Iterator failures</summary>
		BRK_ERROR_START = 0x10200,
		/// <summary>Hex digits expected as part of a escaped char in a rule.</summary>
		BRK_HEX_DIGITS_EXPECTED,
		/// <summary>Missing ';' at the end of a RBBI rule.</summary>
		BRK_SEMICOLON_EXPECTED,
		/// <summary>Syntax error in RBBI rule.</summary>
		BRK_RULE_SYNTAX,
		/// <summary>UnicodeSet witing an RBBI rule missing a closing ']'.</summary>
		BRK_UNCLOSED_SET,
		/// <summary>Syntax error in RBBI rule assignment statement.</summary>
		BRK_ASSIGN_ERROR,
		/// <summary>RBBI rule $Variable redefined.</summary>
		BRK_VARIABLE_REDFINITION,
		/// <summary>Mis-matched parentheses in an RBBI rule.</summary>
		BRK_MISMATCHED_PAREN,
		/// <summary>Missing closing quote in an RBBI rule.</summary>
		BRK_NEW_LINE_IN_QUOTED_STRING,
		/// <summary>Use of an undefined $Variable in an RBBI rule.</summary>
		BRK_UNDEFINED_VARIABLE,
		/// <summary>Initialization failure.  Probable missing ICU Data.</summary>
		BRK_INIT_ERROR,
		/// <summary>Rule contains an empty Unicode Set.</summary>
		BRK_RULE_EMPTY_SET,
		/// <summary>!!option in RBBI rules not recognized.</summary>
		BRK_UNRECOGNIZED_OPTION,
		/// <summary>The {nnn} tag on a rule is mal formed</summary>
		BRK_MALFORMED_RULE_TAG,
		/// <summary>This must always be the last value to indicate the limit for Break Iterator failures</summary>
		BRK_ERROR_LIMIT,
		/*
 * The error codes in the range 0x10300-0x103ff are reserved for regular expression related errrs
 */
		/// <summary>An internal error (bug) was detected.</summary>
		REGEX_INTERNAL_ERROR = 0x10300,
		/// <summary>Start of codes indicating Regexp failures</summary>
		REGEX_ERROR_START = 0x10300,
		/// <summary>Syntax error in regexp pattern.</summary>
		REGEX_RULE_SYNTAX,
		/// <summary>RegexMatcher in invalid state for requested operation</summary>
		REGEX_INVALID_STATE,
		/// <summary>Unrecognized backslash escape sequence in pattern</summary>
		REGEX_BAD_ESCAPE_SEQUENCE,
		/// <summary>Incorrect Unicode property</summary>
		REGEX_PROPERTY_SYNTAX,
		/// <summary>Use of regexp feature that is not yet implemented.</summary>
		REGEX_UNIMPLEMENTED,
		/// <summary>Incorrectly nested parentheses in regexp pattern.</summary>
		REGEX_MISMATCHED_PAREN,
		/// <summary>Decimal number is too large.</summary>
		REGEX_NUMBER_TOO_BIG,
		/// <summary>Error in {min,max} interval</summary>
		REGEX_BAD_INTERVAL,
		/// <summary>In {min,max}, max is less than min.</summary>
		REGEX_MAX_LT_MIN,
		/// <summary>Back-reference to a non-existent capture group.</summary>
		REGEX_INVALID_BACK_REF,
		/// <summary>Invalid value for match mode flags.</summary>
		REGEX_INVALID_FLAG,
		/// <summary>Look-Behind pattern matches must have a bounded maximum length.</summary>
		REGEX_LOOK_BEHIND_LIMIT,
		/// <summary>Regexps cannot have UnicodeSets containing strings.</summary>
		REGEX_SET_CONTAINS_STRING,
		/// <summary>Octal character constants must be &lt;= 0377.</summary>
		REGEX_OCTAL_TOO_BIG,
		/// <summary>Missing closing bracket on a bracket expression.</summary>
		REGEX_MISSING_CLOSE_BRACKET,
		/// <summary>In a character range [x-y], x is greater than y.</summary>
		REGEX_INVALID_RANGE,
		/// <summary>Regular expression backtrack stack overflow.</summary>
		REGEX_STACK_OVERFLOW,
		/// <summary>Maximum allowed match time exceeded.</summary>
		REGEX_TIME_OUT,
		/// <summary>Matching operation aborted by user callback fn.</summary>
		REGEX_STOPPED_BY_CALLER,
		/// <summary>This must always be the last value to indicate the limit for regexp errors</summary>
		REGEX_ERROR_LIMIT,

		/*
		 * The error code in the range 0x10400-0x104ff are reserved for IDNA related error codes
		 */
		/// <summary />
		IDNA_PROHIBITED_ERROR = 0x10400,
		/// <summary>Start of codes indicating IDNA failures</summary>
		IDNA_ERROR_START = 0x10400,
		/// <summary />
		IDNA_UNASSIGNED_ERROR,
		/// <summary />
		IDNA_CHECK_BIDI_ERROR,
		/// <summary />
		IDNA_STD3_ASCII_RULES_ERROR,
		/// <summary />
		IDNA_ACE_PREFIX_ERROR,
		/// <summary />
		IDNA_VERIFICATION_ERROR,
		/// <summary />
		IDNA_LABEL_TOO_LONG_ERROR,
		/// <summary />
		IDNA_ZERO_LENGTH_LABEL_ERROR,
		/// <summary />
		IDNA_DOMAIN_NAME_TOO_LONG_ERROR,
		/// <summary>This must always be the last value to indicate the limit for IDNA errors</summary>
		IDNA_ERROR_LIMIT,
		/*
		 * Aliases for StringPrep
		 */
		/// <summary />
		STRINGPREP_PROHIBITED_ERROR = IDNA_PROHIBITED_ERROR,
		/// <summary />
		STRINGPREP_UNASSIGNED_ERROR = IDNA_UNASSIGNED_ERROR,
		/// <summary />
		STRINGPREP_CHECK_BIDI_ERROR = IDNA_CHECK_BIDI_ERROR,

		/// <summary>This must always be the last value to indicate the limit for UErrorCode (last error code +1)</summary>
		ERROR_LIMIT = IDNA_ERROR_LIMIT
	}
}