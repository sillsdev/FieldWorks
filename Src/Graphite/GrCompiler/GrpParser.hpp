#ifndef INC_GrpParser_hpp_
#define INC_GrpParser_hpp_

#include "antlr/config.hpp"
/*
 * ANTLR-generated file resulting from grammar c:\fw\src\graphite\grcompiler\grpparser.g
 *
 * Terence Parr, MageLang Institute
 * with John Lilley, Empathy Software
 * ANTLR Version 2.6.0; 1996-1999
 */

#include "antlr/TokenStream.hpp"
#include "antlr/TokenBuffer.hpp"
#include "antlr/LLkParser.hpp"


//	Header stuff here
void AddGlobalError(bool, int nID, std::string, int nLine);
class GrpTokenStreamFilter;

class GrpParser : public LLkParser
 {

//	Customized code:
public:
	//	Record the token stream filter, which supplies the line-and-file information
	//	to error messages.
	GrpTokenStreamFilter * m_ptsf;
	void init(GrpTokenStreamFilter & tsf);

	void reportError(const ParserException& ex);

	void reportError(const std::string& s)
	{
		AddGlobalError(true, 104, s.c_str(), 0);
	}
	void reportWarning(const std::string& s)
	{
		AddGlobalError(false, 504, s.c_str(), 0);
	}
protected:
	GrpParser(TokenBuffer& tokenBuf, int k);
public:
	GrpParser(TokenBuffer& tokenBuf);
protected:
	GrpParser(TokenStream& lexer, int k);
public:
	GrpParser(TokenStream& lexer);
	GrpParser(const ParserSharedInputState& state);
	public: void renderDescription();
	public: void declarationList();
	public: void globalDecl();
	public: void topDecl();
	public: void topEnvironDecl();
	public: void tableDecl();
	public: void identDot();
	public: void exprList();
	public: void expr();
	public: void directives();
	public: void directiveList();
	public: void directive();
	public: void tableName();
	public: void tableGlyph();
	public: void tableFeature();
	public: void tableLanguage();
	public: void tableSub();
	public: void tableJust();
	public: void tablePos();
	public: void tableLineBreak();
	public: void tableOther();
	public: void nameEnv();
	public: void nameSpecList();
	public: void nameSpecStruct();
	public: void nameSpecFlat();
	public: void signedInt();
	public: void stringDefn();
	public: void stringFunc();
	public: void glyphEnv();
	public: void glyphEntry();
	public: void glyphContents();
	public: void glyphAttrs();
	public: void glyphSpec();
	public: void attributes();
	public: void attrItemList();
	public: void attrItemFlat();
	public: void attrItemStruct();
	public: void codepointFunc();
	public: void glyphidFunc();
	public: void postscriptFunc();
	public: void unicodeFunc();
	public: void unicodeCodepoint();
	public: void pseudoFunc();
	public: void codepointList();
	public: void codepointItem();
	public: void charOrIntOrRange();
	public: void intOrRange();
	public: void unicodeIntOrRange();
	public: void featureEnv();
	public: void featureSpecList();
	public: void featureSpecStruct();
	public: void featureSpecFlat();
	public: void languageEnv();
	public: void languageSpecList();
	public: void languageSpec();
	public: void languageSpecItem();
	public: void languageItemList();
	public: void languageCodeList();
	public: void subEntry();
	public: void subIf();
	public: void subRule();
	public: void subPass();
	public: void subEnv();
	public: void subEntryList();
	public: void subElseIfList();
	public: void subElseIf();
	public: void subLhs();
	public: void subRhs();
	public: void context();
	public: void subLhsRange();
	public: void subLhsList();
	public: void subLhsItem();
	public: void subRhsItem();
	public: void selectorAfterAt();
	public: void associations();
	public: void selector();
	public: void alias();
	public: void slotIndicator();
	public: void assocsList();
	public: void slotIndicatorAfterAt();
	public: void posEntry();
	public: void posIf();
	public: void posRule();
	public: void posPass();
	public: void posEnv();
	public: void posEntryList();
	public: void posElseIfList();
	public: void posElseIf();
	public: void posRhs();
	public: void posRhsRange();
	public: void posRhsList();
	public: void posRhsItem();
	public: void contextRange();
	public: void contextList();
	public: void contextItem();
	public: void constraint();
	public: void otherEntry();
	public: void attrAssignOp();
	public: void function();
	public: void conditionalExpr();
	public: void logicalOrExpr();
	public: void logicalAndExpr();
	public: void comparativeExpr();
	public: void additiveExpr();
	public: void multiplicativeExpr();
	public: void unaryExpr();
	public: void singleExpr();
	public: void arithFunction();
	public: void lookupExpr();
	public: void selectorExpr();
	public: void clusterExpr();
private:
	static const char* _tokenNames[];

	static const unsigned long _tokenSet_0_data_[];
	static const BitSet _tokenSet_0;
	static const unsigned long _tokenSet_1_data_[];
	static const BitSet _tokenSet_1;
	static const unsigned long _tokenSet_2_data_[];
	static const BitSet _tokenSet_2;
	static const unsigned long _tokenSet_3_data_[];
	static const BitSet _tokenSet_3;
	static const unsigned long _tokenSet_4_data_[];
	static const BitSet _tokenSet_4;
	static const unsigned long _tokenSet_5_data_[];
	static const BitSet _tokenSet_5;
	static const unsigned long _tokenSet_6_data_[];
	static const BitSet _tokenSet_6;
	static const unsigned long _tokenSet_7_data_[];
	static const BitSet _tokenSet_7;
	static const unsigned long _tokenSet_8_data_[];
	static const BitSet _tokenSet_8;
	static const unsigned long _tokenSet_9_data_[];
	static const BitSet _tokenSet_9;
	static const unsigned long _tokenSet_10_data_[];
	static const BitSet _tokenSet_10;
	static const unsigned long _tokenSet_11_data_[];
	static const BitSet _tokenSet_11;
	static const unsigned long _tokenSet_12_data_[];
	static const BitSet _tokenSet_12;
	static const unsigned long _tokenSet_13_data_[];
	static const BitSet _tokenSet_13;
	static const unsigned long _tokenSet_14_data_[];
	static const BitSet _tokenSet_14;
	static const unsigned long _tokenSet_15_data_[];
	static const BitSet _tokenSet_15;
	static const unsigned long _tokenSet_16_data_[];
	static const BitSet _tokenSet_16;
	static const unsigned long _tokenSet_17_data_[];
	static const BitSet _tokenSet_17;
	static const unsigned long _tokenSet_18_data_[];
	static const BitSet _tokenSet_18;
	static const unsigned long _tokenSet_19_data_[];
	static const BitSet _tokenSet_19;
	static const unsigned long _tokenSet_20_data_[];
	static const BitSet _tokenSet_20;
	static const unsigned long _tokenSet_21_data_[];
	static const BitSet _tokenSet_21;
	static const unsigned long _tokenSet_22_data_[];
	static const BitSet _tokenSet_22;
	static const unsigned long _tokenSet_23_data_[];
	static const BitSet _tokenSet_23;
	static const unsigned long _tokenSet_24_data_[];
	static const BitSet _tokenSet_24;
	static const unsigned long _tokenSet_25_data_[];
	static const BitSet _tokenSet_25;
	static const unsigned long _tokenSet_26_data_[];
	static const BitSet _tokenSet_26;
	static const unsigned long _tokenSet_27_data_[];
	static const BitSet _tokenSet_27;
	static const unsigned long _tokenSet_28_data_[];
	static const BitSet _tokenSet_28;
	static const unsigned long _tokenSet_29_data_[];
	static const BitSet _tokenSet_29;
	static const unsigned long _tokenSet_30_data_[];
	static const BitSet _tokenSet_30;
	static const unsigned long _tokenSet_31_data_[];
	static const BitSet _tokenSet_31;
	static const unsigned long _tokenSet_32_data_[];
	static const BitSet _tokenSet_32;
	static const unsigned long _tokenSet_33_data_[];
	static const BitSet _tokenSet_33;
	static const unsigned long _tokenSet_34_data_[];
	static const BitSet _tokenSet_34;
	static const unsigned long _tokenSet_35_data_[];
	static const BitSet _tokenSet_35;
	static const unsigned long _tokenSet_36_data_[];
	static const BitSet _tokenSet_36;
	static const unsigned long _tokenSet_37_data_[];
	static const BitSet _tokenSet_37;
	static const unsigned long _tokenSet_38_data_[];
	static const BitSet _tokenSet_38;
	static const unsigned long _tokenSet_39_data_[];
	static const BitSet _tokenSet_39;
	static const unsigned long _tokenSet_40_data_[];
	static const BitSet _tokenSet_40;
	static const unsigned long _tokenSet_41_data_[];
	static const BitSet _tokenSet_41;
	static const unsigned long _tokenSet_42_data_[];
	static const BitSet _tokenSet_42;
	static const unsigned long _tokenSet_43_data_[];
	static const BitSet _tokenSet_43;
	static const unsigned long _tokenSet_44_data_[];
	static const BitSet _tokenSet_44;
	static const unsigned long _tokenSet_45_data_[];
	static const BitSet _tokenSet_45;
	static const unsigned long _tokenSet_46_data_[];
	static const BitSet _tokenSet_46;
	static const unsigned long _tokenSet_47_data_[];
	static const BitSet _tokenSet_47;
	static const unsigned long _tokenSet_48_data_[];
	static const BitSet _tokenSet_48;
	static const unsigned long _tokenSet_49_data_[];
	static const BitSet _tokenSet_49;
	static const unsigned long _tokenSet_50_data_[];
	static const BitSet _tokenSet_50;
	static const unsigned long _tokenSet_51_data_[];
	static const BitSet _tokenSet_51;
	static const unsigned long _tokenSet_52_data_[];
	static const BitSet _tokenSet_52;
	static const unsigned long _tokenSet_53_data_[];
	static const BitSet _tokenSet_53;
	static const unsigned long _tokenSet_54_data_[];
	static const BitSet _tokenSet_54;
	static const unsigned long _tokenSet_55_data_[];
	static const BitSet _tokenSet_55;
	static const unsigned long _tokenSet_56_data_[];
	static const BitSet _tokenSet_56;
	static const unsigned long _tokenSet_57_data_[];
	static const BitSet _tokenSet_57;
	static const unsigned long _tokenSet_58_data_[];
	static const BitSet _tokenSet_58;
	static const unsigned long _tokenSet_59_data_[];
	static const BitSet _tokenSet_59;
	static const unsigned long _tokenSet_60_data_[];
	static const BitSet _tokenSet_60;
	static const unsigned long _tokenSet_61_data_[];
	static const BitSet _tokenSet_61;
	static const unsigned long _tokenSet_62_data_[];
	static const BitSet _tokenSet_62;
	static const unsigned long _tokenSet_63_data_[];
	static const BitSet _tokenSet_63;
	static const unsigned long _tokenSet_64_data_[];
	static const BitSet _tokenSet_64;
	static const unsigned long _tokenSet_65_data_[];
	static const BitSet _tokenSet_65;
	static const unsigned long _tokenSet_66_data_[];
	static const BitSet _tokenSet_66;
	static const unsigned long _tokenSet_67_data_[];
	static const BitSet _tokenSet_67;
	static const unsigned long _tokenSet_68_data_[];
	static const BitSet _tokenSet_68;
	static const unsigned long _tokenSet_69_data_[];
	static const BitSet _tokenSet_69;
	static const unsigned long _tokenSet_70_data_[];
	static const BitSet _tokenSet_70;
	static const unsigned long _tokenSet_71_data_[];
	static const BitSet _tokenSet_71;
	static const unsigned long _tokenSet_72_data_[];
	static const BitSet _tokenSet_72;
	static const unsigned long _tokenSet_73_data_[];
	static const BitSet _tokenSet_73;
	static const unsigned long _tokenSet_74_data_[];
	static const BitSet _tokenSet_74;
	static const unsigned long _tokenSet_75_data_[];
	static const BitSet _tokenSet_75;
	static const unsigned long _tokenSet_76_data_[];
	static const BitSet _tokenSet_76;
	static const unsigned long _tokenSet_77_data_[];
	static const BitSet _tokenSet_77;
	static const unsigned long _tokenSet_78_data_[];
	static const BitSet _tokenSet_78;
	static const unsigned long _tokenSet_79_data_[];
	static const BitSet _tokenSet_79;
	static const unsigned long _tokenSet_80_data_[];
	static const BitSet _tokenSet_80;
	static const unsigned long _tokenSet_81_data_[];
	static const BitSet _tokenSet_81;
	static const unsigned long _tokenSet_82_data_[];
	static const BitSet _tokenSet_82;
	static const unsigned long _tokenSet_83_data_[];
	static const BitSet _tokenSet_83;
	static const unsigned long _tokenSet_84_data_[];
	static const BitSet _tokenSet_84;
	static const unsigned long _tokenSet_85_data_[];
	static const BitSet _tokenSet_85;
	static const unsigned long _tokenSet_86_data_[];
	static const BitSet _tokenSet_86;
};

#endif /*INC_GrpParser_hpp_*/
